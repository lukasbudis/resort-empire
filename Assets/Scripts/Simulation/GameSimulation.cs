
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ResortEmpire
{
    public class GameSimulation
    {
        private readonly Dictionary<NeedType, BuildingService> buildingServices = new();
        private readonly Dictionary<NeedType, NeedStateMachine> needStates = new();
        private readonly Dictionary<NeedType, NeedState> needStateValues = new();

        // BuildingService.TotalServed counts across waves, so S2 subtracts the value at wave start.
        private readonly Dictionary<NeedType, int> servedAtWaveStart = new();
        private readonly Dictionary<NeedType, int> servedThisWave = new();
        private readonly int bottleneckThreshold;

        public int TickCount { get; private set; } = 0;
        public bool IsPaused { get; private set; } = false;

        public WaveModel CurrentWave { get; private set; }

        public bool AllNeedsSatisfied =>
            CurrentWave != null &&
            CurrentWave.Phase == WavePhase.Draining &&
            needStates.Values.All(state => state.State == NeedState.Satisfied);

        // Needs of the current wave only (FR-36).
        public IReadOnlyDictionary<NeedType, NeedState> NeedStates => needStateValues;
        public IReadOnlyDictionary<NeedType, int> ServedThisWave => servedThisWave;

        public event Action<NeedType, NeedState> NeedStateChanged;

        // The threshold comes from config.json (needStateConfig.bottleneckThreshold).
        public GameSimulation(
            int bottleneckThreshold = NeedStateMachine.DefaultBottleneckThreshold)
        {
            if (bottleneckThreshold < 0)
                throw new ArgumentOutOfRangeException(nameof(bottleneckThreshold));

            this.bottleneckThreshold = bottleneckThreshold;
        }

        private WorkerAllocation workers;
        private readonly Dictionary<string, NeedType> workerBuildingNeeds = new(StringComparer.Ordinal);
        public IReadOnlyDictionary<string, string> WorkerAssignments => workers?.Assignments;
        public event Action<string, string, string> WorkerMoved;

        // Inicializacia prebehne raz pred prvou vlnou, iba pre dostupne budovy.
        public void InitializeWorkers(
            IReadOnlyDictionary<string, NeedType> buildingNeeds,
            IEnumerable<global::WorkerInitialState> initialWorkers,
            float unstaffedPerformanceMultiplier)
        {
            if (workers != null || CurrentWave != null)
                throw new InvalidOperationException("Workerov treba inicializovat raz pred prvou vlnou.");
            if (buildingNeeds == null) throw new ArgumentNullException(nameof(buildingNeeds));
            var services = new Dictionary<string, BuildingService>(StringComparer.Ordinal);
            foreach (var pair in buildingNeeds)
            {
                if (!buildingServices.TryGetValue(pair.Value, out var service))
                    throw new ArgumentException("Budova nema registrovanu obsluhu.", nameof(buildingNeeds));
                services.Add(pair.Key, service);
            }
            var initialized = new WorkerAllocation(services, initialWorkers, unstaffedPerformanceMultiplier);
            foreach (var pair in buildingNeeds)
                workerBuildingNeeds.Add(pair.Key, pair.Value);
            workers = initialized;
            workers.WorkerMoved += (workerId, from, to) => WorkerMoved?.Invoke(workerId, from, to);
        }

        public bool HasWorker(string buildingId) => workers?.HasWorker(buildingId) ?? false;
        public string GetWorkerId(string buildingId) => RequireWorkers().GetWorkerId(buildingId);
        public string GetWorkerBuildingId(string workerId) => RequireWorkers().GetWorkerBuildingId(workerId);
        public bool CanMoveWorker(string fromBuildingId, string toBuildingId) =>
            workers?.CanMoveWorker(fromBuildingId, toBuildingId) ?? false;
        public bool TryMoveWorker(string fromBuildingId, string toBuildingId) =>
            workers?.TryMoveWorker(fromBuildingId, toBuildingId) ?? false;

        public string GetBuildingIdForNeed(NeedType need)
        {
            foreach (var pair in workerBuildingNeeds)
                if (pair.Value == need) return pair.Key;
            return null;
        }

        private WorkerAllocation RequireWorkers() =>
            workers ?? throw new InvalidOperationException("Workeri nie su inicializovani.");
        public void RegisterBuildingService(NeedType need, BuildingService service)
        {
            if (service == null)
                throw new ArgumentNullException(nameof(service));

            if (workers != null)
                throw new InvalidOperationException("Po inicializacii Workerov sa obsluhy nemenia.");

            buildingServices[need] = service;
        }

        public void StartWave(
            Dictionary<NeedType, int> demand,
            IEnumerable<NeedType> availableNeeds,
            float arrivalSeconds = 90f)
        {
            CurrentWave = new WaveModel(
                demand,
                availableNeeds,
                arrivalSeconds
            );

            CurrentWave.NeedArrived += OnNeedArrived;

            needStates.Clear();
            needStateValues.Clear();
            servedAtWaveStart.Clear();
            servedThisWave.Clear();

            foreach (var need in CurrentWave.TotalDemand.Keys)
            {
                var machine = new NeedStateMachine(bottleneckThreshold);
                needStates[need] = machine;
                needStateValues[need] = machine.State;

                servedAtWaveStart[need] = buildingServices.TryGetValue(need, out var service)
                    ? service.TotalServed
                    : 0;
                servedThisWave[need] = 0;
            }
        }

        public NeedState GetNeedState(NeedType need)
        {
            if (!needStates.TryGetValue(need, out var state))
                throw new ArgumentException($"Need {need} is not part of the current wave.", nameof(need));

            return state.State;
        }

        private void OnNeedArrived(NeedType need, int count)
        {
            if (!buildingServices.ContainsKey(need))
                throw new InvalidOperationException($"No building service registered for need {need}.");

            if (buildingServices.TryGetValue(need, out var service))
            {
                service.AddArrivals(count);
            }
        }

        public void Step()
        {
            if (IsPaused)
            {
                return;
            }

            TickCount++;

            CurrentWave?.Step();

            foreach (var service in buildingServices.Values)
            {
                service.Step();
            }

            UpdateServedCounts();
            UpdateNeedStates();
        }

        private void UpdateServedCounts()
        {
            foreach (var pair in servedAtWaveStart)
            {
                if (buildingServices.TryGetValue(pair.Key, out var service))
                    servedThisWave[pair.Key] = service.TotalServed - pair.Value;
            }
        }

        private void UpdateNeedStates()
        {
            if (CurrentWave == null)
                return;

            WaveModel wave = CurrentWave;
            bool arrivalClosed = wave.Phase == WavePhase.Draining;
            var changes = new List<(NeedType need, NeedState state)>();

            foreach (var pair in needStates)
            {
                buildingServices.TryGetValue(pair.Key, out var service);

                if (pair.Value.Update(
                    arrivalClosed,
                    service?.WaitingCount ?? 0,
                    service?.ActiveCount ?? 0,
                    service?.ServiceRatePerTick ?? 0.0,
                    wave.GetArrivalRatePerTick(pair.Key)))
                {
                    changes.Add((pair.Key, pair.Value.State));
                }
            }

            foreach (var change in changes)
                needStateValues[change.need] = change.state;

            // Raised after the loop, so a handler may start the next wave;
            // the remaining changes of the finished wave are then dropped.
            foreach (var change in changes)
            {
                if (CurrentWave != wave)
                    break;

                NeedStateChanged?.Invoke(change.need, change.state);
            }
        }

        public void Pause()
        {
            IsPaused = true;
        }

        public void Resume()
        {
            IsPaused = false;
        }

        public void UpdateFastMode()
        {
            foreach (var (needType, service) in buildingServices)
            {
                string needTypeString = needType switch
                    {
                        NeedType.Rooms => "rooms",
                        NeedType.Food => "food",
                        NeedType.Drinks => "drinks",
                        NeedType.Relax => "relax",
                        NeedType.OceanView => "ocean_view",
                        _ => needType.ToString().ToLower()
                    };

                // Fast mode meni iba cas, nie vykon podla povodnej konfiguracie.
                service.UpdateFastMode(
                    UtilConfig.getServiceTimeByNeedType(needTypeString),
                    service.PerformanceMultiplier
                    );
            }
        }
    }
}
using System;
using System.Collections.Generic;

namespace ResortEmpire
{
    public enum GameState
    {
        AwaitingWave,
        WaveInProgress,
        AllWavesCompleted
    }

    public class GameCycle
    {
        private readonly GameSimulation simulation;
        private readonly global::WavesConfig[] waves;
        private float waveTimer; //Odstanene read only kvoli fastMode
        private readonly Dictionary<string, NeedType> buildingNeeds;
        private readonly HashSet<string> availableBuildingIds;

        private int nextWaveIndex;

        public GameState CurrentState { get; private set; } = GameState.AwaitingWave;
        public int CurrentWaveNumber => nextWaveIndex;
        public int ActiveWaveId { get; private set; }

        public event Action<GameState> GameStateChanged;
        public event Action<int> WaveCompleted;

        public GameCycle(
            GameSimulation simulation, 
            global::WavesConfig[] waves, 
            float waveTimer,
            Dictionary<string, NeedType> buildingNeeds,
            HashSet<string> availableBuildingIds)
        {
            this.simulation = simulation ?? throw new ArgumentNullException(nameof(simulation));
            this.waves = waves ?? throw new ArgumentNullException(nameof(waves));
            this.waveTimer = waveTimer;
            this.buildingNeeds = buildingNeeds ?? throw new ArgumentNullException(nameof(buildingNeeds));
            this.availableBuildingIds = availableBuildingIds ?? throw new ArgumentNullException(nameof(availableBuildingIds));
        }

        private bool TryGetAvailableService(string buildingId, out BuildingService service)
        {
            service = null;
            return buildingId != null && availableBuildingIds.Contains(buildingId)
                && buildingNeeds.TryGetValue(buildingId, out var need)
                && simulation.TryGetBuildingService(need, out service);
        }

        public bool TryGetBuildingSeats(string buildingId, out int level, out int capacity)
        {
            bool found = TryGetAvailableService(buildingId, out var service);
            level = service?.Level ?? 0;
            capacity = service?.Capacity ?? 0;
            return found;
        }

        public bool TryIncreaseBuildingCapacity(string buildingId) =>
            CurrentState == GameState.AwaitingWave
            && TryGetAvailableService(buildingId, out var service)
            && service.TryIncreaseCapacity();

        public void StartNextWave()
        {
            if (CurrentState != GameState.AwaitingWave)
                return;

            if (nextWaveIndex >= waves.Length)
            {
                SetGameState(GameState.AllWavesCompleted);
                return;
            }

            global::WavesConfig wave = waves[nextWaveIndex];

            if (wave == null || wave.needs == null)
                throw new InvalidOperationException($"Vlna na indexe {nextWaveIndex} nemá definované needy.");

            var availableNeeds = new HashSet<NeedType>();
            foreach (string buildingId in availableBuildingIds)
            {
                if (buildingNeeds.TryGetValue(buildingId, out var need))
                    availableNeeds.Add(need);
            }

            var demand = new Dictionary<NeedType, int>();
            foreach (NeedType need in availableNeeds)
                demand[need] = GetDemand(wave.needs, need);

            simulation.StartWave(demand, availableNeeds, waveTimer);

            ActiveWaveId = wave.wave_id;
            nextWaveIndex++;

            SetGameState(GameState.WaveInProgress);
        }

        public void UpdateStep()
        {
            if (CurrentState != GameState.WaveInProgress || simulation.IsPaused)
                return;

            simulation.Step();

            if (simulation.AllNeedsSatisfied)
            {
                CompleteWave();
            }
        }

        private void CompleteWave()
        {
            int completedWaveId = ActiveWaveId;
            ActiveWaveId = 0;

            GameState nextState = nextWaveIndex < waves.Length
                ? GameState.AwaitingWave
                : GameState.AllWavesCompleted;

            SetGameState(nextState);
            WaveCompleted?.Invoke(completedWaveId);
        }

        private void SetGameState(GameState newState)
        {
            if (CurrentState == newState)
                return;

            CurrentState = newState;
            GameStateChanged?.Invoke(newState);
        }

        // Nakup uz overil podmienky oblasti a cenu. Tu sa meni len simulacia.
        public bool TryAddPurchasedBuilding(string buildingId)
        {
            if (CurrentState != GameState.AwaitingWave || string.IsNullOrWhiteSpace(buildingId) ||
                availableBuildingIds.Contains(buildingId) || !buildingNeeds.TryGetValue(buildingId, out var need) ||
                !simulation.TryAddPurchasedBuilding(buildingId, need))
                return false;

            availableBuildingIds.Add(buildingId);
            return true;
        }

        private static int GetDemand(global::NeedsConfig needs, NeedType need)
        {
            switch (need)
            {
                case NeedType.Rooms: return needs.rooms;
                case NeedType.Food: return needs.food;
                case NeedType.Drinks: return needs.drinks;
                case NeedType.Relax: return needs.relax;
                case NeedType.OceanView: return needs.ocean_view;
                default: return 0;
            }
        }
        
        public void UpdateFastMode()
        {
            waveTimer = UtilConfig.getWaveTimer();
        }
        public WaveForecast GetNextWaveForecast()
        {
            if (CurrentState != GameState.AwaitingWave ||
                nextWaveIndex >= waves.Length)
            {
                return null;
            }

            global::WavesConfig wave = waves[nextWaveIndex];

            if (wave == null || wave.needs == null)
                return null;

            var availableNeeds = new HashSet<NeedType>();

            foreach (string buildingId in availableBuildingIds)
            {
                if (buildingNeeds.TryGetValue(buildingId, out NeedType need))
                    availableNeeds.Add(need);
            }

            NeedType[] displayOrder =
            {
                NeedType.Rooms,
                NeedType.Drinks,
                NeedType.Food,
                NeedType.Relax,
                NeedType.OceanView
            };

            var rows = new List<WaveForecastRow>();

            foreach (NeedType need in displayOrder)
            {
                if (!availableNeeds.Contains(need))
                    continue;

                int volume = GetDemand(wave.needs, need);

                if (volume > 0)
                    rows.Add(new WaveForecastRow(need, volume));
            }

            return new WaveForecast(wave.wave_id, rows);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ResortEmpire
{
    // Rozmiestnenie patri simulacii, nie povodnej konfiguracii ani scene.
    public sealed class WorkerAllocation
    {
        private readonly Dictionary<string, BuildingService> services = new(StringComparer.Ordinal);
        private readonly Dictionary<string, string> assignments = new(StringComparer.Ordinal);
        private readonly float unstaffedPerformanceMultiplier;

        // Zivy pohlad iba na citanie: kluc je dostupna budova, hodnota Worker ID alebo null.
        public IReadOnlyDictionary<string, string> Assignments { get; }
        public event Action<string, string, string> WorkerMoved;

        public WorkerAllocation(
            IReadOnlyDictionary<string, BuildingService> buildingServices,
            IEnumerable<global::WorkerInitialState> initialWorkers,
            float unstaffedPerformanceMultiplier)
        {
            if (buildingServices == null) throw new ArgumentNullException(nameof(buildingServices));
            if (initialWorkers == null) throw new ArgumentNullException(nameof(initialWorkers));
            if (float.IsNaN(unstaffedPerformanceMultiplier) || float.IsInfinity(unstaffedPerformanceMultiplier) ||
                unstaffedPerformanceMultiplier <= 0f || unstaffedPerformanceMultiplier >= 1f)
                throw new ArgumentOutOfRangeException(nameof(unstaffedPerformanceMultiplier));

            this.unstaffedPerformanceMultiplier = unstaffedPerformanceMultiplier;
            var uniqueServices = new HashSet<BuildingService>();
            foreach (var pair in buildingServices)
            {
                if (string.IsNullOrWhiteSpace(pair.Key) || pair.Value == null || !uniqueServices.Add(pair.Value))
                    throw new ArgumentException("Budova musi mat platne ID a vlastnu obsluhu.", nameof(buildingServices));
                services.Add(pair.Key, pair.Value);
                assignments.Add(pair.Key, null);
            }

            var workerIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var worker in initialWorkers)
            {
                if (worker == null || string.IsNullOrWhiteSpace(worker.id) || !workerIds.Add(worker.id) ||
                    string.IsNullOrWhiteSpace(worker.buildingId) || !assignments.ContainsKey(worker.buildingId) ||
                    assignments[worker.buildingId] != null)
                    throw new ArgumentException("Neplatne alebo duplicitne rozmiestnenie Workerov.", nameof(initialWorkers));
                assignments[worker.buildingId] = worker.id;
            }

            // V tejto kapitole je presne jedna dostupna budova bez Workera (FR-11).
            if (services.Count == 0 || workerIds.Count != services.Count - 1)
                throw new ArgumentException("Workerov musi byt o jedneho menej ako dostupnych budov.", nameof(initialWorkers));

            // Obsluhy zmenime az po overeni celeho vstupu, aby chyba nemenila ich vykon.
            foreach (var pair in services)
                pair.Value.SetPerformanceMultiplier(HasWorker(pair.Key) ? 1f : unstaffedPerformanceMultiplier);
            Assignments = new ReadOnlyDictionary<string, string>(assignments);
        }

        public bool HasWorker(string buildingId) =>
            buildingId != null && assignments.TryGetValue(buildingId, out var workerId) && workerId != null;

        public string GetWorkerId(string buildingId)
        {
            if (buildingId == null || !assignments.TryGetValue(buildingId, out var workerId))
                throw new ArgumentException("Budova nie je dostupna.", nameof(buildingId));
            return workerId;
        }

        public string GetWorkerBuildingId(string workerId)
        {
            if (workerId != null)
                foreach (var pair in assignments)
                    if (pair.Value == workerId)
                        return pair.Key;
            throw new ArgumentException("Neznamy Worker.", nameof(workerId));
        }

        public bool CanMoveWorker(string fromBuildingId, string toBuildingId) =>
            HasWorker(fromBuildingId) && toBuildingId != null && fromBuildingId != toBuildingId &&
            assignments.TryGetValue(toBuildingId, out var targetWorker) && targetWorker == null;

        public bool TryMoveWorker(string fromBuildingId, string toBuildingId)
        {
            if (!CanMoveWorker(fromBuildingId, toBuildingId))
                return false;

            string workerId = assignments[fromBuildingId];
            services[fromBuildingId].SetPerformanceMultiplier(unstaffedPerformanceMultiplier);
            services[toBuildingId].SetPerformanceMultiplier(1f);
            assignments[fromBuildingId] = null;
            assignments[toBuildingId] = workerId;

            // UI dostane udalost az ked su oba vykony aj obe priradenia aktualne.
            WorkerMoved?.Invoke(workerId, fromBuildingId, toBuildingId);
            return true;
        }
    }
}
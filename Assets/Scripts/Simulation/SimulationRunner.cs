using System;
using System.Collections.Generic;
using UnityEngine;

namespace ResortEmpire
{
    public class SimulationRunner : MonoBehaviour
    {
        private const float TickInterval = 0.1f;

        [SerializeField]
        private string[] initiallyAvailableBuildingIds = { "Reception", "Restaurant", "BeachBar" };
        [SerializeField] private int initialBuildingLevel = 1;

        private global::MainConfig config;
        private GameSimulation simulation;
        private GameCycle gameCycle;
        private float accumulatedTime;

        private readonly Dictionary<string, NeedType> buildingNeeds = new();
        private readonly HashSet<string> availableBuildingIds = new();

        // Fasáda smerom k UI a starým komponentom
        public GameState CurrentState => gameCycle?.CurrentState ?? GameState.AwaitingWave;
        public int ActiveWaveId => gameCycle?.ActiveWaveId ?? 0;

        public event Action<GameState> GameStateChanged;
        public event Action<NeedType, NeedState> NeedStateChanged;
        public event Action<int> WaveCompleted;
        
        public event Action Ticked;

        public NeedsPanelData GetNeedsPanelData()
        {
            if (simulation?.CurrentWave == null || CurrentState != GameState.WaveInProgress)
                return null;

            return NeedsPanelPresenter.Build(
                ActiveWaveId,
                simulation.CurrentWave.TotalDemand,
                simulation.ServedThisWave,
                simulation.NeedStates);
        }

        private void Awake()
        {
            global::UtilConfig.loadConfig();
            config = global::UtilConfig.getConfig();

            if (!HasRequiredConfig())
            {
                enabled = false;
                return;
            }

            try
            {
                simulation = new GameSimulation(config.needStateConfig.bottleneckThreshold);
                simulation.NeedStateChanged += ForwardNeedStateChanged;

                RegisterBuildingServices();
                SetInitialAvailableBuildings();

                gameCycle = new GameCycle(
                    simulation,
                    config.wave_settings.waves,
                    config.wave_settings.wave_timer,
                    buildingNeeds,
                    availableBuildingIds
                );

                gameCycle.GameStateChanged += state => GameStateChanged?.Invoke(state);
                gameCycle.WaveCompleted += waveId => WaveCompleted?.Invoke(waveId);
            }
            catch (Exception exception)
            {
                Debug.LogError($"Simulation sa nepodarilo inicializovať: {exception.Message}", this);
                enabled = false;
            }
        }

        private void Start()
        {
            GameStateChanged += state => Debug.Log($"Game state: {state}");
            NeedStateChanged += (need, state) => Debug.Log($"Need {need}: {state}");
            WaveCompleted += waveId => Debug.Log($"Wave {waveId} completed");
        }

        private bool HasRequiredConfig()
        {
            if (config == null || config.needStateConfig == null || config.buildings == null ||
                config.workers == null || config.wave_settings == null || config.wave_settings.waves == null ||
                config.wave_settings.waves.Length == 0)
            {
                Debug.LogError("Konfigurácii chýbajú budovy, worker-i alebo vlny.", this);
                return false;
            }
            return true;
        }

        private void RegisterBuildingServices()
        {
            foreach (global::BuildingsConfig building in config.buildings)
            {
                if (building == null || string.IsNullOrWhiteSpace(building.id))
                    throw new InvalidOperationException("Config contains an invalid building.");

                if (!TryParseNeed(building.need, out NeedType need))
                    throw new InvalidOperationException($"Unknown need '{building.need}' on building '{building.id}'.");

                global::LevelConfig level = Array.Find(building.levels, item => item != null && item.level == initialBuildingLevel);
                if (level == null)
                    throw new InvalidOperationException($"Building '{building.id}' misses level {initialBuildingLevel}.");

                float performance = global::UtilConfig.GetBuildingPerformanceMultiplier(config, building.id);
                var service = new BuildingService(level.capacity, building.service_time, performance);

                simulation.RegisterBuildingService(need, service);
                buildingNeeds.Add(building.id, need);
            }
        }

        private void SetInitialAvailableBuildings()
        {
            foreach (string buildingId in initiallyAvailableBuildingIds)
            {
                if (!buildingNeeds.ContainsKey(buildingId))
                    throw new InvalidOperationException($"Initial building '{buildingId}' not found in config.");

                availableBuildingIds.Add(buildingId);
            }
        }

        [ContextMenu("Start Next Wave")]
        public void StartNextWave()
        {
            if (gameCycle == null) return;

            try
            {
                gameCycle.StartNextWave();
                accumulatedTime = 0f;

                foreach (string buildingId in availableBuildingIds)
                {
                    if (buildingNeeds.TryGetValue(buildingId, out var need))
                        NeedStateChanged?.Invoke(need, simulation.GetNeedState(need));
                }
            }
            catch (Exception exception)
            {
                Debug.LogError($"Vlnu sa nepodarilo spustiť: {exception.Message}", this);
            }
        }

        private void Update()
        {
            if (simulation == null || simulation.IsPaused || gameCycle == null || CurrentState != GameState.WaveInProgress)
                return;

            accumulatedTime += Time.deltaTime;

            while (accumulatedTime >= TickInterval && CurrentState == GameState.WaveInProgress)
            {
                gameCycle.UpdateStep();
                accumulatedTime -= TickInterval;
                Ticked?.Invoke();
            }
        }

        private void ForwardNeedStateChanged(NeedType need, NeedState state) => NeedStateChanged?.Invoke(need, state);
        public void PauseSimulation() => simulation?.Pause();
        public void ResumeSimulation() => simulation?.Resume();

        private static bool TryParseNeed(string value, out NeedType need)
        {
            switch (value?.Trim().ToLowerInvariant())
            {
                case "rooms": need = NeedType.Rooms; return true;
                case "food": need = NeedType.Food; return true;
                case "drinks": need = NeedType.Drinks; return true;
                case "relax": need = NeedType.Relax; return true;
                case "ocean_view": need = NeedType.OceanView; return true;
                default: need = default; return false;
            }
        }

        [ContextMenu("Log Wave Diagnostics")]
        private void LogWaveDiagnostics()
        {
            if (simulation == null || gameCycle == null) return;
            var wave = simulation.CurrentWave;
            if (wave == null) return;

            int expectedTicks = Mathf.RoundToInt(config.wave_settings.wave_timer / TickInterval);
            Debug.Log($"Wave diagnostics: state={CurrentState}, paused={simulation.IsPaused}, ticks={simulation.TickCount}, wave={ActiveWaveId}, phase={wave.Phase}, elapsed={wave.ElapsedTicks}/{expectedTicks}, allNeedsSatisfied={simulation.AllNeedsSatisfied}", this);
        }
    }
}

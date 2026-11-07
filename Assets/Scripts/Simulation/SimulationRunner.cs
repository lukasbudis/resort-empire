using System;
using System.Collections.Generic;
using UnityEngine;

namespace ResortEmpire
{
    public enum GameState
    {
        AwaitingWave,
        WaveInProgress,
        AllWavesCompleted
    }

    public class SimulationRunner : MonoBehaviour
    {
        [ContextMenu("Log Wave Diagnostics")]
private void LogWaveDiagnostics() /*dočasná diagnostika*/
{
    if (simulation == null)
    {
        Debug.LogError("Simulation nie je inicializovaná.", this);
        return;
    }

    var wave = simulation.CurrentWave;

    if (wave == null)
    {
        Debug.Log("Nie je spustená žiadna vlna.", this);
        return;
    }

    int expectedTicks = Mathf.RoundToInt(
        config.wave_settings.wave_timer / TickInterval);

    Debug.Log(
        $"Wave diagnostics: state={CurrentState}, " +
        $"paused={simulation.IsPaused}, timeScale={Time.timeScale}, " +
        $"ticks={simulation.TickCount}, wave={activeWaveId}, " +
        $"phase={wave.Phase}, elapsed={wave.ElapsedTicks}/{expectedTicks}, " +
        $"allNeedsSatisfied={simulation.AllNeedsSatisfied}",
        this);
}
        private void Start() /*dočasná diagnostika*/
{
    GameStateChanged += state =>
        Debug.Log($"Game state: {state}");

    NeedStateChanged += (need, state) =>
        Debug.Log($"Need {need}: {state}");

    WaveCompleted += waveId =>
        Debug.Log($"Wave {waveId} completed");
}
        private const float TickInterval = 0.1f;

        [SerializeField]
        private string[] initiallyAvailableBuildingIds =
        {
            "Reception",
            "Restaurant",
            "BeachBar"
        };

        [SerializeField] private int initialBuildingLevel = 1;

        private global::MainConfig config;
        private GameSimulation simulation;
        private float accumulatedTime;
        private int nextWaveIndex;
        private int activeWaveId;

        private readonly Dictionary<string, NeedType> buildingNeeds = new();
        private readonly HashSet<string> availableBuildingIds = new();

        public GameState CurrentState { get; private set; } = GameState.AwaitingWave;

        public event Action<GameState> GameStateChanged;
        public event Action<NeedType, NeedState> NeedStateChanged;
        public event Action<int> WaveCompleted;

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
                simulation = new GameSimulation(
                    config.needStateConfig.bottleneckThreshold);

                simulation.NeedStateChanged += ForwardNeedStateChanged;

                RegisterBuildingServices();
                SetInitialAvailableBuildings();
            }
            catch (Exception exception)
            {
                Debug.LogError($"Simulation sa nepodarilo inicializovať: {exception.Message}", this);
                enabled = false;
            }
        }

        private bool HasRequiredConfig()
        {
            if (config == null ||
                config.needStateConfig == null ||
                config.buildings == null ||
                config.workers == null ||
                config.wave_settings == null ||
                config.wave_settings.waves == null ||
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
                    throw new InvalidOperationException("Config obsahuje neplatnú budovu.");

                if (!TryParseNeed(building.need, out NeedType need))
                    throw new InvalidOperationException(
                        $"Neznámy need '{building.need}' pri budove '{building.id}'.");

                if (building.levels == null)
                    throw new InvalidOperationException(
                        $"Budova '{building.id}' nemá definované levely.");

                global::LevelConfig level = Array.Find(
                    building.levels,
                    item => item != null && item.level == initialBuildingLevel);

                if (level == null)
                    throw new InvalidOperationException(
                        $"Pre budovu '{building.id}' chýba level {initialBuildingLevel}.");

                float performance = global::UtilConfig
                    .GetBuildingPerformanceMultiplier(config, building.id);

                var service = new BuildingService(
                    level.capacity,
                    building.service_time,
                    performance);

                simulation.RegisterBuildingService(need, service);
                buildingNeeds.Add(building.id, need);
            }
        }

        private void SetInitialAvailableBuildings()
        {
            foreach (string buildingId in initiallyAvailableBuildingIds)
            {
                if (!buildingNeeds.ContainsKey(buildingId))
                    throw new InvalidOperationException(
                        $"Počiatočná budova '{buildingId}' nie je v configu.");

                availableBuildingIds.Add(buildingId);
            }
        }

        [ContextMenu("Start Next Wave")]
        public void StartNextWave()
        {
            if (simulation == null || CurrentState != GameState.AwaitingWave)
                return;

            if (nextWaveIndex >= config.wave_settings.waves.Length)
            {
                SetGameState(GameState.AllWavesCompleted);
                return;
            }

            global::WavesConfig wave = config.wave_settings.waves[nextWaveIndex];

            if (wave == null || wave.needs == null)
            {
                Debug.LogError($"Vlna na indexe {nextWaveIndex} nemá definované needy.", this);
                return;
            }

            var availableNeeds = new HashSet<NeedType>();
            foreach (string buildingId in availableBuildingIds)
                availableNeeds.Add(buildingNeeds[buildingId]);

            var demand = new Dictionary<NeedType, int>();
            foreach (NeedType need in availableNeeds)
                demand[need] = GetDemand(wave.needs, need);

            try
            {
                simulation.StartWave(
                    demand,
                    availableNeeds,
                    config.wave_settings.wave_timer);

                activeWaveId = wave.wave_id;
                nextWaveIndex++;
                accumulatedTime = 0f;

                SetGameState(GameState.WaveInProgress);

                foreach (NeedType need in availableNeeds)
                    NeedStateChanged?.Invoke(need, simulation.GetNeedState(need));
            }
            catch (Exception exception)
            {
                Debug.LogError($"Vlnu sa nepodarilo spustiť: {exception.Message}", this);
            }
        }

        private void Update()
        {
            if (simulation == null ||
                simulation.IsPaused ||
                CurrentState != GameState.WaveInProgress)
                return;

            accumulatedTime += Time.deltaTime;

            while (accumulatedTime >= TickInterval &&
                   CurrentState == GameState.WaveInProgress)
            {
                simulation.Step();
                accumulatedTime -= TickInterval;

                if (simulation.AllNeedsSatisfied)
                    CompleteWave();
            }
        }

        private void CompleteWave()
        {
            int completedWaveId = activeWaveId;
            activeWaveId = 0;

            GameState nextState =
                nextWaveIndex < config.wave_settings.waves.Length
                    ? GameState.AwaitingWave
                    : GameState.AllWavesCompleted;

            SetGameState(nextState);
            WaveCompleted?.Invoke(completedWaveId);
        }

        private void ForwardNeedStateChanged(NeedType need, NeedState state)
        {
            NeedStateChanged?.Invoke(need, state);
        }

        private void SetGameState(GameState newState)
        {
            if (CurrentState == newState)
                return;

            CurrentState = newState;
            GameStateChanged?.Invoke(newState);
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

        private static bool TryParseNeed(string value, out NeedType need)
        {
            switch (value?.Trim().ToLowerInvariant())
            {
                case "rooms": need = NeedType.Rooms; return true;
                case "food": need = NeedType.Food; return true;
                case "drinks": need = NeedType.Drinks; return true;
                case "relax": need = NeedType.Relax; return true;
                case "ocean_view": need = NeedType.OceanView; return true;
                default:
                    need = default;
                    return false;
            }
        }

        public void PauseSimulation()
        {
            simulation?.Pause();
        }

        public void ResumeSimulation()
        {
            simulation?.Resume();
        }
    }
    
    
}

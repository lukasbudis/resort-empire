
using System;
using System.Collections.Generic;
using System.Linq;

public class GameSimulation
{
    private readonly Dictionary<NeedType, BuildingService> buildingServices = new();
    private readonly Dictionary<NeedType, NeedStateMachine> needStates = new();
    private readonly int bottleneckThreshold;

    public int TickCount { get; private set; } = 0;
    public bool IsPaused { get; private set; } = false;

    public WaveModel CurrentWave { get; private set; }

    public bool AllNeedsSatisfied =>
        CurrentWave != null &&
        CurrentWave.Phase == WavePhase.Draining &&
        needStates.Values.All(state => state.State == NeedState.Satisfied);

    public event Action<NeedType, NeedState> NeedStateChanged;

    // The threshold comes from config.json (needStateConfig.bottleneckThreshold).
    public GameSimulation(
        int bottleneckThreshold = NeedStateMachine.DefaultBottleneckThreshold)
    {
        if (bottleneckThreshold < 0)
            throw new ArgumentOutOfRangeException(nameof(bottleneckThreshold));

        this.bottleneckThreshold = bottleneckThreshold;
    }

    public void RegisterBuildingService(NeedType need, BuildingService service)
    {
        if (service == null)
            throw new ArgumentNullException(nameof(service));

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
        foreach (var need in CurrentWave.TotalDemand.Keys)
            needStates[need] = new NeedStateMachine(bottleneckThreshold);
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

        UpdateNeedStates();
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
}

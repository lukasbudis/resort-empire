
using System;
using System.Collections.Generic;

public enum WavePhase
{
    Incoming,
    Draining
}

public enum NeedType
{
    Rooms,
    Food,
    Drinks,
    Relax,
    OceanView
}

public class WaveModel
{
    private const float TickSeconds = 0.1f;

    private readonly int arrivalTicks;
    private readonly Dictionary<NeedType, int> totalDemand = new();
    private readonly Dictionary<NeedType, int> arrived = new();
    private readonly Dictionary<NeedType, int> newArrivals = new();

    public WavePhase Phase { get; private set; } = WavePhase.Incoming;
    public int ElapsedTicks { get; private set; }

    public IReadOnlyDictionary<NeedType, int> TotalDemand => totalDemand;
    public IReadOnlyDictionary<NeedType, int> Arrived => arrived;
    public IReadOnlyDictionary<NeedType, int> NewArrivals => newArrivals;
    public event Action<NeedType, int> NeedArrived;
    public event Action<WavePhase> PhaseChanged;

    public WaveModel(
        Dictionary<NeedType, int> demand,
        IEnumerable<NeedType> availableNeeds,
        float arrivalSeconds = 90f)
    {
        if (demand == null)
            throw new ArgumentNullException(nameof(demand));

        if (availableNeeds == null)
            throw new ArgumentNullException(nameof(availableNeeds));

        if (float.IsNaN(arrivalSeconds) ||
            float.IsInfinity(arrivalSeconds) ||
            arrivalSeconds <= 0f)
            throw new ArgumentException("Invalid arrival duration.");

        arrivalTicks = Math.Max(
            1,
            (int)Math.Round(arrivalSeconds / TickSeconds)
        );

        foreach (var pair in demand)
        {
            if (pair.Value < 0)
                throw new ArgumentException("Demand cannot be negative.");
        }

        foreach (var need in availableNeeds)
        {
            totalDemand[need] = demand.TryGetValue(need, out int count)
                ? count : 0;

            arrived[need] = 0;
            newArrivals[need] = 0;
        }
    }

    
    public void Step()
    {
        foreach (var need in totalDemand.Keys)
            newArrivals[need] = 0;

        if (Phase != WavePhase.Incoming)
            return;

        ElapsedTicks++;

        foreach (var pair in totalDemand)
        {
            int expected = (int)(
                (long)pair.Value * ElapsedTicks / arrivalTicks
            );

            int count = expected - arrived[pair.Key];

            newArrivals[pair.Key] = count;
            arrived[pair.Key] = expected;

            if (count > 0)
                NeedArrived?.Invoke(pair.Key, count);
        }

        if (ElapsedTicks >= arrivalTicks)
        {
            Phase = WavePhase.Draining;
            PhaseChanged?.Invoke(Phase);
        }
    }


    public int GetArrivedCount(NeedType need)
    {
        return arrived.TryGetValue(need, out int count) ? count : 0;
    }

    // FR-09: volume of the need in this wave ÷ arrival duration, in units per tick.
    public double GetArrivalRatePerTick(NeedType need)
    {
        return totalDemand.TryGetValue(need, out int count)
            ? count / (double)arrivalTicks
            : 0.0;
    }
}

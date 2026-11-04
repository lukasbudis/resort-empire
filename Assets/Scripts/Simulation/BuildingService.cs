
using System;
using System.Collections.Generic;

public class BuildingService
{
    private const float TickSeconds = 0.1f;

    private readonly Queue<int> waitingUnits = new();
    private readonly int[] activeUnitIds;
    private readonly int[] remainingTicks;
    private readonly int serviceTicks;
    private readonly float serviceSeconds;
    private readonly float performanceMultiplier;

    private int nextUnitId = 1;

    public int TotalArrived { get; private set; }
    public int TotalServed { get; private set; }

    public int Capacity => activeUnitIds.Length;
    public int WaitingCount => waitingUnits.Count;
    public int ActiveCount { get; private set; }

    public bool IsEmpty => WaitingCount == 0 && ActiveCount == 0;

    // FR-09: seats × performance ÷ current service time, units per second.
    public double ServiceRate => Capacity * (double)performanceMultiplier / serviceSeconds;

    public BuildingService(
        int capacity,
        float serviceSeconds,
        float performanceMultiplier = 1f)
    {
        if (capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacity));

        if (float.IsNaN(serviceSeconds) ||
            float.IsInfinity(serviceSeconds) ||
            serviceSeconds <= 0f)
            throw new ArgumentOutOfRangeException(nameof(serviceSeconds));

        if (float.IsNaN(performanceMultiplier) ||
            float.IsInfinity(performanceMultiplier) ||
            performanceMultiplier <= 0f)
            throw new ArgumentOutOfRangeException(nameof(performanceMultiplier));

        activeUnitIds = new int[capacity];
        remainingTicks = new int[capacity];
        this.serviceSeconds = serviceSeconds;
        this.performanceMultiplier = performanceMultiplier;

        serviceTicks = Math.Max(
            1,
            (int)Math.Ceiling(
                // Ignore float representation noise at exact tick boundaries.
                (double)serviceSeconds / (TickSeconds * (double)performanceMultiplier) - 1e-6
            )
        );
    }

    
    public void AddArrivals(int count)
    {
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count));

        for (int i = 0; i < count; i++)
        {
            waitingUnits.Enqueue(nextUnitId++);
        }

        TotalArrived += count;
        FillFreeSlots();
    }

    public void Step()
    {
        for (int i = 0; i < Capacity; i++)
        {
            if (activeUnitIds[i] == 0)
                continue;

            remainingTicks[i]--;

            if (remainingTicks[i] <= 0)
            {
                activeUnitIds[i] = 0;
                remainingTicks[i] = 0;

                ActiveCount--;
                TotalServed++;
            }
        }

        FillFreeSlots();
    }

    public int GetActiveUnitId(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= Capacity)
            throw new ArgumentOutOfRangeException(nameof(slotIndex));

        return activeUnitIds[slotIndex];
    }

    private void FillFreeSlots()
    {
        for (int i = 0; i < Capacity && waitingUnits.Count > 0; i++)
        {
            if (activeUnitIds[i] != 0)
                continue;

            activeUnitIds[i] = waitingUnits.Dequeue();
            remainingTicks[i] = serviceTicks;

            ActiveCount++;
        }
    }

}

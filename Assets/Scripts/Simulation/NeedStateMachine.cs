using System;

public enum NeedState
{
    Ok,
    Bottleneck,
    Recovering,
    Satisfied
}

// State of one need during a wave (FR-09).
public class NeedStateMachine
{
    public const int DefaultBottleneckThreshold = 4;

    private readonly int bottleneckThreshold;

    public NeedState State { get; private set; } = NeedState.Ok;

    public NeedStateMachine(int bottleneckThreshold = DefaultBottleneckThreshold)
    {
        if (bottleneckThreshold < 0)
            throw new ArgumentOutOfRangeException(nameof(bottleneckThreshold));

        this.bottleneckThreshold = bottleneckThreshold;
    }

    // Rates are units per second. Returns true when the state changed.
    public bool Update(
        bool arrivalClosed,
        int waitingCount,
        int servingCount,
        double serviceRate,
        double arrivalRate)
    {
        if (waitingCount < 0)
            throw new ArgumentOutOfRangeException(nameof(waitingCount));

        if (servingCount < 0)
            throw new ArgumentOutOfRangeException(nameof(servingCount));

        if (double.IsNaN(serviceRate) || double.IsInfinity(serviceRate) || serviceRate < 0)
            throw new ArgumentOutOfRangeException(nameof(serviceRate));

        if (double.IsNaN(arrivalRate) || double.IsInfinity(arrivalRate) || arrivalRate < 0)
            throw new ArgumentOutOfRangeException(nameof(arrivalRate));

        NeedState next = NextState(
            arrivalClosed, waitingCount, servingCount, serviceRate, arrivalRate);

        if (next == State)
            return false;

        State = next;
        return true;
    }

    private NeedState NextState(
        bool arrivalClosed,
        int waitingCount,
        int servingCount,
        double serviceRate,
        double arrivalRate)
    {
        if (State == NeedState.Satisfied)
            return NeedState.Satisfied;

        if (arrivalClosed && waitingCount == 0 && servingCount == 0)
            return NeedState.Satisfied;

        bool entersBottleneck =
            !arrivalClosed &&
            waitingCount > bottleneckThreshold &&
            serviceRate < arrivalRate;

        switch (State)
        {
            // Leaves only through Recovering, so a queue dipping under the
            // threshold does not make the warning blink (AC1).
            case NeedState.Bottleneck:
                return serviceRate > arrivalRate || arrivalClosed
                    ? NeedState.Recovering
                    : NeedState.Bottleneck;

            case NeedState.Recovering:
                if (entersBottleneck)
                    return NeedState.Bottleneck;

                return waitingCount <= bottleneckThreshold
                    ? NeedState.Ok
                    : NeedState.Recovering;

            default:
                return entersBottleneck ? NeedState.Bottleneck : NeedState.Ok;
        }
    }
}

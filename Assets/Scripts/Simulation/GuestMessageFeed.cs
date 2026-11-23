using System;

namespace ResortEmpire
{
    public enum GuestMessageKind
    {
        // Need went to BOTTLENECK (e.g. "Food takes a bit too long.").
        Complaint,

        // Need went to RECOVERING, the warning turns positive.
        Praise
    }

    public readonly struct GuestMessage : IEquatable<GuestMessage>
    {
        public NeedType Need { get; }
        public GuestMessageKind Kind { get; }

        public GuestMessage(NeedType need, GuestMessageKind kind)
        {
            Need = need;
            Kind = kind;
        }

        public bool Equals(GuestMessage other) => Need == other.Need && Kind == other.Kind;
        public override bool Equals(object obj) => obj is GuestMessage other && Equals(other);
        public override int GetHashCode() => ((int)Need * 397) ^ (int)Kind;
        public override string ToString() => $"{Kind} {Need}";
    }

    // FR-37: S2 briefly shows a guest message when a need enters BOTTLENECK
    // and a positive one when it enters RECOVERING. One message at a time,
    // the newest one wins and restarts the timer.
    public class GuestMessageFeed
    {
        public const float DefaultDisplaySeconds = 4f;

        private readonly float displaySeconds;

        public GuestMessage? Current { get; private set; }
        public float RemainingSeconds { get; private set; }

        // Raised when a message appears, is replaced, or disappears (null).
        public event Action<GuestMessage?> CurrentChanged;

        public GuestMessageFeed(float displaySeconds = DefaultDisplaySeconds)
        {
            if (float.IsNaN(displaySeconds) || float.IsInfinity(displaySeconds) || displaySeconds <= 0f)
                throw new ArgumentOutOfRangeException(nameof(displaySeconds));

            this.displaySeconds = displaySeconds;
        }

        // Returns true when the state change produced a message.
        // OK and SATISFIED produce none and leave the current message alone,
        // so a quick RECOVERING -> OK does not cut the positive message short.
        public bool OnNeedStateChanged(NeedType need, NeedState state)
        {
            GuestMessageKind kind;

            switch (state)
            {
                case NeedState.Bottleneck: kind = GuestMessageKind.Complaint; break;
                case NeedState.Recovering: kind = GuestMessageKind.Praise; break;
                default: return false;
            }

            Current = new GuestMessage(need, kind);
            RemainingSeconds = displaySeconds;
            CurrentChanged?.Invoke(Current);
            return true;
        }

        public void Advance(float deltaSeconds)
        {
            if (float.IsNaN(deltaSeconds) || deltaSeconds < 0f)
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds));

            if (Current == null)
                return;

            RemainingSeconds -= deltaSeconds;

            if (RemainingSeconds <= 0f)
                Clear();
        }

        public void Clear()
        {
            RemainingSeconds = 0f;

            if (Current == null)
                return;

            Current = null;
            CurrentChanged?.Invoke(null);
        }
    }
}

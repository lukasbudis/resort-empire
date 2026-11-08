using System;
using System.Collections.Generic;

namespace ResortEmpire
{
    // How one need row in S2 looks (FR-36, FR-37).
    public enum NeedRowStatus
    {
        InProgress,
        Bottleneck,
        Recovering,
        Done
    }

    public readonly struct NeedRowData
    {
        public NeedType Need { get; }
        public int Served { get; }
        public int Total { get; }
        public NeedRowStatus Status { get; }

        // Ocean View is the premium need (Bungalows, DICTIONARY).
        public bool IsPremium => Need == NeedType.OceanView;

        // 0..1 for the progress bar; a need without demand shows an empty bar.
        public float Progress => Total <= 0
            ? 0f
            : Math.Min(1f, Math.Max(0f, Served / (float)Total));

        public NeedRowData(NeedType need, int served, int total, NeedRowStatus status)
        {
            if (served < 0)
                throw new ArgumentOutOfRangeException(nameof(served));

            if (total < 0)
                throw new ArgumentOutOfRangeException(nameof(total));

            Need = need;
            Served = served;
            Total = total;
            Status = status;
        }
    }

    public sealed class NeedsPanelData
    {
        public int WaveId { get; }
        public IReadOnlyList<NeedRowData> Rows { get; }

        public NeedsPanelData(int waveId, IReadOnlyList<NeedRowData> rows)
        {
            WaveId = waveId;
            Rows = rows ?? throw new ArgumentNullException(nameof(rows));
        }
    }

    // Turns the state of the running wave into rows of the S2 panel (FR-36).
    public static class NeedsPanelPresenter
    {
        public const int MaxRows = 5;

        // Order on screen (Figma S2), independent of the NeedType enum order.
        private static readonly NeedType[] displayOrder =
        {
            NeedType.Rooms,
            NeedType.Drinks,
            NeedType.Food,
            NeedType.Relax,
            NeedType.OceanView
        };

        public static IReadOnlyList<NeedType> DisplayOrder => displayOrder;

        // demand = needs of the current wave (locked needs are not in it),
        // served = units served since the wave started, states = NeedState per need.
        public static NeedsPanelData Build(
            int waveId,
            IReadOnlyDictionary<NeedType, int> demand,
            IReadOnlyDictionary<NeedType, int> served,
            IReadOnlyDictionary<NeedType, NeedState> states)
        {
            if (demand == null)
                throw new ArgumentNullException(nameof(demand));

            if (served == null)
                throw new ArgumentNullException(nameof(served));

            if (states == null)
                throw new ArgumentNullException(nameof(states));

            var rows = new List<NeedRowData>(MaxRows);

            foreach (NeedType need in displayOrder)
            {
                if (!demand.TryGetValue(need, out int total))
                    continue;

                served.TryGetValue(need, out int servedCount);

                NeedState state = states.TryGetValue(need, out NeedState value)
                    ? value
                    : NeedState.Ok;

                rows.Add(new NeedRowData(need, servedCount, total, ToStatus(state)));
            }

            if (rows.Count != demand.Count)
                throw new ArgumentException("Demand obsahuje need, ktorý nie je v poradí zobrazenia.", nameof(demand));

            return new NeedsPanelData(waveId, rows);
        }

        public static NeedRowStatus ToStatus(NeedState state)
        {
            switch (state)
            {
                case NeedState.Bottleneck: return NeedRowStatus.Bottleneck;
                case NeedState.Recovering: return NeedRowStatus.Recovering;
                case NeedState.Satisfied: return NeedRowStatus.Done;
                default: return NeedRowStatus.InProgress;
            }
        }
    }
}

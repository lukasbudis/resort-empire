using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace ResortEmpire
{
    // What S4 lists under "What went well" and "Needs improvement" (FR-20, AC6).
    // S5 suggests an upgrade for the building of NeedsImprovement (FR-25).
    public sealed class WaveSummary
    {
        // Simulation steps each need of the wave spent in BOTTLENECK.
        public IReadOnlyDictionary<NeedType, int> BottleneckTicks { get; }

        // Needs that were never in BOTTLENECK, in display order. When every need was in
        // BOTTLENECK, it holds the one that spent the least time there instead; it is empty
        // when the wave had a single need, because that need already needs improvement.
        public IReadOnlyList<NeedType> WentWell { get; }

        // Need that spent the most time in BOTTLENECK; null when no need was in it.
        public NeedType? NeedsImprovement { get; }

        public bool HadBottleneck => NeedsImprovement.HasValue;

        // True when no need avoided BOTTLENECK. WentWell then names at most the least affected one.
        public bool AllNeedsHadBottleneck { get; }

        private WaveSummary(
            IReadOnlyDictionary<NeedType, int> bottleneckTicks,
            IReadOnlyList<NeedType> wentWell,
            NeedType? needsImprovement,
            bool allNeedsHadBottleneck)
        {
            BottleneckTicks = bottleneckTicks;
            WentWell = wentWell;
            NeedsImprovement = needsImprovement;
            AllNeedsHadBottleneck = allNeedsHadBottleneck;
        }

        // Ties go to the need that comes first in the display order of S2.
        public static WaveSummary FromBottleneckTicks(IReadOnlyDictionary<NeedType, int> bottleneckTicks)
        {
            if (bottleneckTicks == null)
                throw new ArgumentNullException(nameof(bottleneckTicks));

            var needs = new List<NeedType>(bottleneckTicks.Count);
            var ticks = new Dictionary<NeedType, int>(bottleneckTicks.Count);

            foreach (NeedType need in NeedsPanelPresenter.DisplayOrder)
            {
                if (!bottleneckTicks.TryGetValue(need, out int value))
                    continue;

                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(bottleneckTicks));

                needs.Add(need);
                ticks.Add(need, value);
            }

            if (needs.Count != bottleneckTicks.Count)
                throw new ArgumentException("A need is missing in the display order.", nameof(bottleneckTicks));

            var wentWell = new List<NeedType>();
            NeedType? needsImprovement = null;
            int mostTicks = 0;

            foreach (NeedType need in needs)
            {
                if (ticks[need] == 0)
                    wentWell.Add(need);
                else if (ticks[need] > mostTicks)
                {
                    needsImprovement = need;
                    mostTicks = ticks[need];
                }
            }

            bool allNeedsHadBottleneck = needs.Count > 0 && wentWell.Count == 0;

            if (allNeedsHadBottleneck)
            {
                NeedType? leastAffected = null;
                int fewestTicks = int.MaxValue;

                foreach (NeedType need in needs)
                {
                    // The same need cannot be both the best and the worst of the wave.
                    if (need == needsImprovement.Value || ticks[need] >= fewestTicks)
                        continue;

                    leastAffected = need;
                    fewestTicks = ticks[need];
                }

                if (leastAffected.HasValue)
                    wentWell.Add(leastAffected.Value);
            }

            return new WaveSummary(
                new ReadOnlyDictionary<NeedType, int>(ticks),
                wentWell.AsReadOnly(),
                needsImprovement,
                allNeedsHadBottleneck);
        }

        public static double CalculateUnitSatisfaction(double waitingSeconds, float lower_wait, float upper_wait, float unit_floor)
        {
            if (waitingSeconds <= lower_wait)
                return 100.0;

            if (waitingSeconds >= upper_wait)
                return unit_floor;

            double t = (waitingSeconds - lower_wait) / (upper_wait - lower_wait);
            return 100.0 - t * (100.0 - unit_floor);
        }

        public static int CalculateWaveSatisfaction(IReadOnlyDictionary<NeedType, IReadOnlyList<UnitWaitingTime>> waitingTimesByNeed)
        {
            if (waitingTimesByNeed == null)
                throw new ArgumentNullException(nameof(waitingTimesByNeed));

            double weightedSum = 0.0;
            double totalWeight = 0.0;

            float lower_wait = UtilConfig.getPatienceGrace();
            float upper_wait = UtilConfig.getPatienceFloorAfter();
            float unit_floor = UtilConfig.getWaveFloor(); 

            foreach (var pair in waitingTimesByNeed)
            {
                NeedType need = pair.Key;
                IReadOnlyList<UnitWaitingTime> units = pair.Value;

                if (units == null || units.Count == 0)
                    continue;

                double weight = need == NeedType.OceanView ? UtilConfig.getOceanViewSatisfactionWeight() : 1.0f;

                foreach (UnitWaitingTime unit in units)
                {
                    double unitScore = CalculateUnitSatisfaction(unit.WaitingSeconds, lower_wait, upper_wait, unit_floor);
                    weightedSum += unitScore * weight;
                    totalWeight += weight;
                }
            }

            if (totalWeight <= 0.0)
                return Mathf.RoundToInt(UtilConfig.getRatingThresholds());

            double average = weightedSum / totalWeight;
            double clamped = Math.Max(UtilConfig.getRatingThresholds(), average);

            return (int)Math.Round(clamped, MidpointRounding.AwayFromZero);
        }
    }
}

using System;
using System.Collections.Generic;

namespace ResortEmpire
{
    public static class WaveSatisfactionCalculator
    {
        public static int CalculatePercent(
            GameSimulation simulation,
            global::WaveSettingsConfig settings)
        {
            if (simulation == null)
                throw new ArgumentNullException(nameof(simulation));
            if (simulation.CurrentWave == null)
                throw new InvalidOperationException("Vlna ešte nebola spustená.");
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            if (settings.patience_grace < 0f ||
                settings.patience_floor_after <= settings.patience_grace ||
                settings.unit_floor < 0f ||
                settings.unit_floor >= settings.wave_floor ||
                settings.wave_floor >= 70f ||
                settings.ocean_view_satisfaction_weight <= 0f)
            {
                throw new ArgumentException("Neplatné nastavenia spokojnosti v konfigurácii.");
            }

            double weightedTotal = 0d;
            double totalWeight = 0d;

            foreach (KeyValuePair<NeedType, int> need in simulation.CurrentWave.TotalDemand)
            {
                if (need.Value <= 0)
                    continue;

                double needWeight =
                    need.Key == NeedType.OceanView
                        ? settings.ocean_view_satisfaction_weight
                        : 1.0;

                IReadOnlyList<UnitWaitingTime> waits =
                    simulation.GetWaitingTimesThisWave(need.Key);

                foreach (UnitWaitingTime sample in waits)
                {
                    weightedTotal += Contribution(sample.WaitingSeconds, settings) * needWeight;
                    totalWeight += needWeight;
                }
            }

            double average = totalWeight == 0d
                ? settings.wave_floor
                : weightedTotal / totalWeight;

            double score = Math.Max(settings.wave_floor, average);
            return (int)Math.Round(Math.Min(100d, score));
        }

        private static double Contribution(
            double waitingSeconds,
            global::WaveSettingsConfig settings)
        {
            if (waitingSeconds <= settings.patience_grace)
                return 100d;

            if (waitingSeconds >= settings.patience_floor_after)
                return settings.unit_floor;

            double progress =
                (waitingSeconds - settings.patience_grace) /
                (settings.patience_floor_after - settings.patience_grace);

            return 100d + (settings.unit_floor - 100d) * progress;
        }
    }
}

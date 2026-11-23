using UnityEngine;

namespace ResortEmpire
{
    public sealed class WaveCompleteResult
    {
        public int WaveId { get; }
        public int SatisfactionPercent { get; }
        public int Rating { get; }
        public int ServedUnits { get; }
        public int TotalUnits { get; }
        public int CoinsReward { get; }
        public WaveSummary Summary { get; }

        public WaveCompleteResult(
            int waveId,
            int satisfactionPercent,
            int rating,
            int servedUnits,
            int totalUnits,
            int coinsReward,
            WaveSummary summary)
        {
            WaveId = waveId;
            SatisfactionPercent = satisfactionPercent;
            Rating = rating;
            ServedUnits = servedUnits;
            TotalUnits = totalUnits;
            CoinsReward = coinsReward;
            Summary = summary;
        }
    }
}

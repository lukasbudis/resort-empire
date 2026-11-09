using System.Collections.Generic;

namespace ResortEmpire
{
    public readonly struct WaveForecastRow
    {
        public NeedType Need { get; }
        public int Volume { get; }

        public WaveForecastRow(NeedType need, int volume)
        {
            Need = need;
            Volume = volume;
        }
    }

    public sealed class WaveForecast
    {
        public int WaveId { get; }
        public IReadOnlyList<WaveForecastRow> Rows { get; }

        public WaveForecast(int waveId, IReadOnlyList<WaveForecastRow> rows)
        {
            WaveId = waveId;
            Rows = rows;
        }
    }
}
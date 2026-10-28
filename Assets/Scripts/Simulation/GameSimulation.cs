
using System.Collections.Generic;

public class GameSimulation
{
    public int TickCount { get; private set; } = 0;
    public bool IsPaused { get; private set; } = false;

    public WaveModel CurrentWave { get; private set; }

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
    }

    public void Step()
    {
        if (IsPaused)
        {
            return;
        }

        TickCount++;

        CurrentWave?.Step();
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

public class GameSimulation
{
    public int TickCount { get; private set; } = 0;

    public bool IsPaused { get; private set; } = false;

    public void Step()
    {
        if (IsPaused)
        {
            return;
        }

        TickCount++;
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

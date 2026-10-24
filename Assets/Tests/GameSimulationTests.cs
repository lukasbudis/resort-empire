
using NUnit.Framework;

public class GameSimulationTests
{
    [Test]
    public void Step_IncreasesTickCount()
    {
        GameSimulation simulation = new GameSimulation();

        simulation.Step();
        simulation.Step();
        simulation.Step();

        Assert.AreEqual(3, simulation.TickCount);
    }

    [Test]
    public void Pause_StopsSimulation()
    {
        GameSimulation simulation = new GameSimulation();

        simulation.Step();
        simulation.Pause();
        simulation.Step();

        Assert.AreEqual(1, simulation.TickCount);
        Assert.IsTrue(simulation.IsPaused);
    }

    [Test]
    public void Resume_ContinuesSimulation()
    {
        GameSimulation simulation = new GameSimulation();

        simulation.Step();
        simulation.Pause();
        simulation.Step();
        simulation.Resume();
        simulation.Step();

        Assert.AreEqual(2, simulation.TickCount);
        Assert.IsFalse(simulation.IsPaused);
    }
}

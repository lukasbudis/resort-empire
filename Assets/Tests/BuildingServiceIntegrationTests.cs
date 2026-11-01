
using System.Collections.Generic;
using NUnit.Framework;

public class BuildingServiceIntegrationTests
{
    [Test]
    public void MissingBuildingServiceReportsConfigurationError()
    {
        var simulation = new GameSimulation();
        simulation.StartWave(
            new Dictionary<NeedType, int> { { NeedType.Food, 1 } },
            new[] { NeedType.Food }, 0.1f);
        var error = Assert.Throws<System.InvalidOperationException>(() => simulation.Step());
        StringAssert.Contains("Food", error.Message);
    }
    [Test]
    public void WaveArrivalsAreFullyServedByBuildings()
    {
        var simulation = new GameSimulation();

        var rooms = new BuildingService(2, 0.3f);
        var food = new BuildingService(1, 0.2f);
        var drinks = new BuildingService(2, 0.4f);

        simulation.RegisterBuildingService(NeedType.Rooms, rooms);
        simulation.RegisterBuildingService(NeedType.Food, food);
        simulation.RegisterBuildingService(NeedType.Drinks, drinks);

        var demand = new Dictionary<NeedType, int>
        {
            { NeedType.Rooms, 7 },
            { NeedType.Food, 5 },
            { NeedType.Drinks, 4 }
        };

        simulation.StartWave(
            demand,
            new[] { NeedType.Rooms, NeedType.Food, NeedType.Drinks },
            1f
        );

        int ticks = 0;

        while (ticks < 200 &&
            (simulation.CurrentWave.Phase == WavePhase.Incoming ||
             !rooms.IsEmpty || !food.IsEmpty || !drinks.IsEmpty))
        {
            simulation.Step();
            ticks++;
        }

        Assert.AreEqual(WavePhase.Draining, simulation.CurrentWave.Phase);

        Assert.IsTrue(rooms.IsEmpty);
        Assert.IsTrue(food.IsEmpty);
        Assert.IsTrue(drinks.IsEmpty);

        Assert.AreEqual(7, rooms.TotalArrived);
        Assert.AreEqual(5, food.TotalArrived);
        Assert.AreEqual(4, drinks.TotalArrived);

        Assert.AreEqual(7, rooms.TotalServed);
        Assert.AreEqual(5, food.TotalServed);
        Assert.AreEqual(4, drinks.TotalServed);

        Assert.AreEqual(16,
            rooms.TotalServed + food.TotalServed + drinks.TotalServed);
    }

    
    [Test]
    public void PauseStopsBuildingServiceUntilResume()
    {
        var simulation = new GameSimulation();
        var food = new BuildingService(1, 0.5f);

        simulation.RegisterBuildingService(NeedType.Food, food);

        simulation.StartWave(
            new Dictionary<NeedType, int>
            {
                { NeedType.Food, 3 }
            },
            new[] { NeedType.Food },
            0.1f
        );

        simulation.Step();

        Assert.AreEqual(3, food.TotalArrived);
        Assert.AreEqual(1, food.ActiveCount);
        Assert.AreEqual(2, food.WaitingCount);

        int ticksBeforePause = simulation.TickCount;

        simulation.Pause();

        for (int i = 0; i < 20; i++)
            simulation.Step();

        Assert.AreEqual(ticksBeforePause, simulation.TickCount);
        Assert.AreEqual(0, food.TotalServed);
        Assert.AreEqual(2, food.WaitingCount);
        Assert.AreEqual(1, food.GetActiveUnitId(0));

        simulation.Resume();

        for (int i = 0; i < 20; i++)
            simulation.Step();

        Assert.AreEqual(3, food.TotalServed);
        Assert.IsTrue(food.IsEmpty);
    }

}


using System.Collections.Generic;
using NUnit.Framework;

public class WaveModelTests
{
    [Test]
    public void DemandArrivesEvenly()
    {
        var wave = new WaveModel(
            new Dictionary<NeedType, int>
            {
                { NeedType.Rooms, 10 }
            },
            new[] { NeedType.Rooms },
            10f
        );

        // Po 5 sekundach ma prist polovica.
        for (int i = 0; i < 50; i++)
            wave.Step();

        Assert.AreEqual(5, wave.GetArrivedCount(NeedType.Rooms));

        // Po 10 sekundach pridu vsetky jednotky.
        for (int i = 0; i < 50; i++)
            wave.Step();

        Assert.AreEqual(10, wave.GetArrivedCount(NeedType.Rooms));
    }

    [Test]
    public void ArrivalEndsAfterConfiguredDuration()
    {
        var wave = new WaveModel(
            new Dictionary<NeedType, int>
            {
                { NeedType.Food, 5 }
            },
            new[] { NeedType.Food },
            2f
        );

        for (int i = 0; i < 19; i++)
            wave.Step();

        Assert.AreEqual(WavePhase.Incoming, wave.Phase);

        wave.Step();

        Assert.AreEqual(WavePhase.Draining, wave.Phase);
        Assert.AreEqual(5, wave.GetArrivedCount(NeedType.Food));

        wave.Step();

        Assert.AreEqual(0, wave.NewArrivals[NeedType.Food]);
    }

    [Test]
    public void LockedBuildingNeedsNeverArrive()
    {
        var wave = new WaveModel(
            new Dictionary<NeedType, int>
            {
                { NeedType.Rooms, 10 },
                { NeedType.Relax, 20 },
                { NeedType.OceanView, 30 }
            },
            new[] { NeedType.Rooms },
            1f
        );

        for (int i = 0; i < 10; i++)
            wave.Step();

        Assert.AreEqual(10, wave.GetArrivedCount(NeedType.Rooms));
        Assert.AreEqual(0, wave.GetArrivedCount(NeedType.Relax));
        Assert.AreEqual(0, wave.GetArrivedCount(NeedType.OceanView));

        Assert.IsFalse(wave.TotalDemand.ContainsKey(NeedType.Relax));
        Assert.IsFalse(wave.TotalDemand.ContainsKey(NeedType.OceanView));
    }

    [Test]
    public void PauseStopsWaveProgress()
    {
        var simulation = new GameSimulation();
        simulation.RegisterBuildingService(NeedType.Drinks, new BuildingService(1, 0.1f));

        simulation.StartWave(
            new Dictionary<NeedType, int>
            {
                { NeedType.Drinks, 10 }
            },
            new[] { NeedType.Drinks },
            10f
        );

        for (int i = 0; i < 10; i++)
            simulation.Step();

        simulation.Pause();

        for (int i = 0; i < 20; i++)
            simulation.Step();

        Assert.AreEqual(10, simulation.CurrentWave.ElapsedTicks);

        simulation.Resume();
        simulation.Step();

        Assert.AreEqual(11, simulation.CurrentWave.ElapsedTicks);
    }


    [Test]
    public void EventsNotifyArrivalsAndPhaseChange()
    {
        var wave = new WaveModel(
            new Dictionary<NeedType, int>
            {
            { NeedType.Rooms, 4 }
            },
            new[] { NeedType.Rooms },
            1f
        );

        int arrivedFromEvents = 0;
        int phaseChangeCount = 0;

        wave.NeedArrived += (need, count) =>
        {
            if (need == NeedType.Rooms)
                arrivedFromEvents += count;
        };

        wave.PhaseChanged += phase =>
        {
            if (phase == WavePhase.Draining)
                phaseChangeCount++;
        };

        for (int i = 0; i < 10; i++)
            wave.Step();

        // Dobeh nesmie vytvorit dalsie udalosti.
        wave.Step();

        Assert.AreEqual(4, arrivedFromEvents);
        Assert.AreEqual(1, phaseChangeCount);
        Assert.AreEqual(WavePhase.Draining, wave.Phase);
    }

}

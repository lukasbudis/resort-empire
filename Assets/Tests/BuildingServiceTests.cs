
using NUnit.Framework;

public class BuildingServiceTests
{
    [Test]
    public void ArrivalsOccupyFreeSlots()
    {
        var service = new BuildingService(2, 1f);

        service.AddArrivals(5);

        Assert.AreEqual(5, service.TotalArrived);
        Assert.AreEqual(2, service.ActiveCount);
        Assert.AreEqual(3, service.WaitingCount);

        Assert.AreEqual(1, service.GetActiveUnitId(0));
        Assert.AreEqual(2, service.GetActiveUnitId(1));
    }

    [Test]
    public void WaitingUnitsFollowFifoOrder()
    {
        var service = new BuildingService(1, 0.1f);

        service.AddArrivals(3);

        Assert.AreEqual(1, service.GetActiveUnitId(0));

        service.Step();
        Assert.AreEqual(2, service.GetActiveUnitId(0));

        service.Step();
        Assert.AreEqual(3, service.GetActiveUnitId(0));

        service.Step();

        Assert.AreEqual(3, service.TotalServed);
        Assert.IsTrue(service.IsIdle);
    }

    [Test]
    public void ServiceRespectsConfiguredDuration()
    {
        var service = new BuildingService(1, 1f);

        service.AddArrivals(1);

        for (int i = 0; i < 9; i++)
            service.Step();

        Assert.AreEqual(0, service.TotalServed);

        service.Step();

        Assert.AreEqual(1, service.TotalServed);
        Assert.IsTrue(service.IsIdle);
    }

    [Test]
    public void AllArrivedUnitsAreEventuallyServed()
    {
        var service = new BuildingService(2, 0.5f);

        service.AddArrivals(20);

        for (int i = 0; i < 100; i++)
            service.Step();

        Assert.AreEqual(20, service.TotalArrived);
        Assert.AreEqual(20, service.TotalServed);
        Assert.AreEqual(0, service.WaitingCount);
        Assert.AreEqual(0, service.ActiveCount);
        Assert.IsTrue(service.IsIdle);
    }

    [Test]
    public void WorkerPerformanceChangesServiceSpeed()
    {
        var normal = new BuildingService(1, 1f, 1f);
        var slower = new BuildingService(1, 1f, 0.5f);

        normal.AddArrivals(1);
        slower.AddArrivals(1);

        for (int i = 0; i < 10; i++)
        {
            normal.Step();
            slower.Step();
        }

        Assert.AreEqual(1, normal.TotalServed);
        Assert.AreEqual(0, slower.TotalServed);

        for (int i = 0; i < 10; i++)
            slower.Step();

        Assert.AreEqual(1, slower.TotalServed);
    }
}

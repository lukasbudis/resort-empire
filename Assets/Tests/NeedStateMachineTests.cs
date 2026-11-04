using System;
using NUnit.Framework;

public class NeedStateMachineTests
{
    private const int Threshold = 4;
    private const double ArrivalRate = 2.0;
    private const double SlowService = 1.0;
    private const double FastService = 3.0;

    private static NeedStateMachine InBottleneck()
    {
        var machine = new NeedStateMachine(Threshold);
        machine.Update(false, 5, 1, SlowService, ArrivalRate);
        Assert.AreEqual(NeedState.Bottleneck, machine.State);
        return machine;
    }

    private static NeedStateMachine InRecovering()
    {
        var machine = InBottleneck();
        machine.Update(false, 6, 1, FastService, ArrivalRate);
        Assert.AreEqual(NeedState.Recovering, machine.State);
        return machine;
    }

    [Test]
    public void StartsOk()
    {
        Assert.AreEqual(NeedState.Ok, new NeedStateMachine().State);
    }

    [Test]
    public void OkToBottleneckWhenQueueAboveThresholdAndServiceSlower()
    {
        var machine = new NeedStateMachine(Threshold);

        Assert.IsTrue(machine.Update(false, 5, 1, SlowService, ArrivalRate));
        Assert.AreEqual(NeedState.Bottleneck, machine.State);
    }

    [TestCase(4, SlowService)]
    [TestCase(5, ArrivalRate)]
    [TestCase(5, FastService)]
    public void OkStaysOkWithoutAllBottleneckConditions(int waiting, double serviceRate)
    {
        var machine = new NeedStateMachine(Threshold);

        Assert.IsFalse(machine.Update(false, waiting, 1, serviceRate, ArrivalRate));
        Assert.AreEqual(NeedState.Ok, machine.State);
    }

    [Test]
    public void ClosedArrivalNeverStartsBottleneck()
    {
        var machine = new NeedStateMachine(Threshold);

        machine.Update(true, 10, 1, SlowService, ArrivalRate);

        Assert.AreEqual(NeedState.Ok, machine.State);
    }

    [Test]
    public void SeatedUnitsDoNotCountToQueue()
    {
        var machine = new NeedStateMachine(Threshold);

        machine.Update(false, 0, 20, SlowService, ArrivalRate);

        Assert.AreEqual(NeedState.Ok, machine.State);
    }

    [Test]
    public void ZeroThresholdReportsAnyWaitingUnit()
    {
        var machine = new NeedStateMachine(0);

        machine.Update(false, 1, 1, SlowService, ArrivalRate);

        Assert.AreEqual(NeedState.Bottleneck, machine.State);
    }

    [Test]
    public void BottleneckToRecoveringWhenServiceBecomesFaster()
    {
        // AC2: a Worker arrives and the service rate jumps above the arrival rate.
        var machine = InBottleneck();

        Assert.IsTrue(machine.Update(false, 8, 1, FastService, ArrivalRate));
        Assert.AreEqual(NeedState.Recovering, machine.State);
    }

    [Test]
    public void BottleneckToRecoveringWhenArrivalCloses()
    {
        var machine = InBottleneck();

        machine.Update(true, 8, 1, SlowService, ArrivalRate);

        Assert.AreEqual(NeedState.Recovering, machine.State);
    }

    [TestCase(4, SlowService)]
    [TestCase(0, SlowService)]
    [TestCase(6, ArrivalRate)]
    public void BottleneckStaysWhileServiceIsNotFaster(int waiting, double serviceRate)
    {
        // A queue dipping under the threshold must not switch the warning off.
        var machine = InBottleneck();

        Assert.IsFalse(machine.Update(false, waiting, 1, serviceRate, ArrivalRate));
        Assert.AreEqual(NeedState.Bottleneck, machine.State);
    }

    [TestCase(4)]
    [TestCase(0)]
    public void RecoveringToOkWhenQueueAtOrBelowThreshold(int waiting)
    {
        var machine = InRecovering();

        Assert.IsTrue(machine.Update(false, waiting, 1, FastService, ArrivalRate));
        Assert.AreEqual(NeedState.Ok, machine.State);
    }

    [Test]
    public void RecoveringStaysWhileQueueAboveThreshold()
    {
        var machine = InRecovering();

        Assert.IsFalse(machine.Update(false, 5, 1, FastService, ArrivalRate));
        Assert.AreEqual(NeedState.Recovering, machine.State);
    }

    [Test]
    public void RecoveringToBottleneckWhenServiceSlowsAgain()
    {
        // The Worker is moved away again while guests still arrive.
        var machine = InRecovering();

        machine.Update(false, 6, 1, SlowService, ArrivalRate);

        Assert.AreEqual(NeedState.Bottleneck, machine.State);
    }

    [Test]
    public void RecoveringAfterClosedArrivalNeverReturnsToBottleneck()
    {
        var machine = InBottleneck();
        machine.Update(true, 9, 1, SlowService, ArrivalRate);

        machine.Update(true, 9, 1, SlowService, ArrivalRate);

        Assert.AreEqual(NeedState.Recovering, machine.State);
    }

    [Test]
    public void OkToSatisfiedWhenArrivalClosedAndBuildingEmpty()
    {
        var machine = new NeedStateMachine(Threshold);

        Assert.IsTrue(machine.Update(true, 0, 0, FastService, ArrivalRate));
        Assert.AreEqual(NeedState.Satisfied, machine.State);
    }

    [Test]
    public void RecoveringToSatisfiedWhenArrivalClosedAndBuildingEmpty()
    {
        var machine = InRecovering();

        machine.Update(true, 0, 0, FastService, ArrivalRate);

        Assert.AreEqual(NeedState.Satisfied, machine.State);
    }

    [TestCase(false, 0, 0)]
    [TestCase(true, 1, 0)]
    [TestCase(true, 0, 1)]
    public void NotSatisfiedUntilArrivalClosedAndNobodyLeft(bool arrivalClosed, int waiting, int serving)
    {
        var machine = new NeedStateMachine(Threshold);

        machine.Update(arrivalClosed, waiting, serving, FastService, ArrivalRate);

        Assert.AreEqual(NeedState.Ok, machine.State);
    }

    [Test]
    public void SatisfiedIsFinal()
    {
        var machine = new NeedStateMachine(Threshold);
        machine.Update(true, 0, 0, FastService, ArrivalRate);

        Assert.IsFalse(machine.Update(false, 10, 1, SlowService, ArrivalRate));
        Assert.AreEqual(NeedState.Satisfied, machine.State);
    }

    [Test]
    public void InvalidInputsAreRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new NeedStateMachine(-1));

        var machine = new NeedStateMachine();

        Assert.Throws<ArgumentOutOfRangeException>(() => machine.Update(false, -1, 0, 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => machine.Update(false, 0, -1, 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => machine.Update(false, 0, 0, double.NaN, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => machine.Update(false, 0, 0, 1, -1));
        Assert.AreEqual(NeedState.Ok, machine.State);
    }
}

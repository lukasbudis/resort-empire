using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace ResortEmpire
{
    public class UnitWaitingTimeTests
    {
        private static void Step(BuildingService service, int ticks)
        {
            for (int i = 0; i < ticks; i++) service.Step();
        }

        private static void Step(GameSimulation simulation, int ticks)
        {
            for (int i = 0; i < ticks; i++) simulation.Step();
        }

        private static GameSimulation Create(float seconds, out BuildingService service)
        {
            var simulation = new GameSimulation();
            service = new BuildingService(1, seconds);
            simulation.RegisterBuildingService(NeedType.Food, service);
            return simulation;
        }

        private static void Start(GameSimulation simulation, int count, float duration)
        {
            simulation.StartWave(new Dictionary<NeedType, int> { { NeedType.Food, count } },
                new[] { NeedType.Food }, duration);
        }

        [Test]
        public void FreeSeatsHaveZeroWaitEvenWithLongService()
        {
            var service = new BuildingService(2, 10f);
            Step(service, 7);
            service.AddArrivals(2);
            Assert.AreEqual(2, service.WaitingTimes.Count);
            for (int i = 0; i < 2; i++)
            {
                var sample = service.WaitingTimes[i];
                Assert.AreEqual(i + 1, sample.UnitId);
                Assert.AreEqual(7, sample.ArrivalTick);
                Assert.AreEqual(7, sample.SeatedTick);
                Assert.AreEqual(0, sample.WaitingTicks);
                Assert.AreEqual(0, sample.WaitingSeconds);
            }
            Step(service, 100);
            Assert.AreEqual(2, service.TotalServed);
            Assert.AreEqual(0, service.WaitingTimes[1].WaitingTicks);
        }

        [Test]
        public void FullSeatRecordsFifoWaitOnlyWhenUnitIsSeated()
        {
            var service = new BuildingService(1, 1f);
            service.AddArrivals(3);
            Assert.AreEqual(1, service.WaitingTimes.Count);
            Step(service, 9);
            Assert.AreEqual(1, service.WaitingTimes.Count);
            service.Step();
            Assert.AreEqual(2, service.WaitingTimes.Count);
            Assert.AreEqual(10, service.WaitingTimes[1].WaitingTicks);
            Assert.AreEqual(1d, service.WaitingTimes[1].WaitingSeconds);
            Step(service, 10);
            Assert.AreEqual(3, service.WaitingTimes.Count);
            Assert.AreEqual(3, service.WaitingTimes[2].UnitId);
            Assert.AreEqual(20, service.WaitingTimes[2].WaitingTicks);
            Step(service, 10);
            Assert.AreEqual(3, service.TotalServed);
            Assert.AreEqual(3, service.WaitingTimes.Count);
        }

        [Test]
        public void StaggeredArrivalsUseTheirOwnArrivalTick()
        {
            var service = new BuildingService(1, 1f);
            service.AddArrivals(1);
            Step(service, 3);
            service.AddArrivals(1);
            Step(service, 3);
            service.AddArrivals(1);
            Step(service, 14);
            Assert.AreEqual(3, service.WaitingTimes[1].ArrivalTick);
            Assert.AreEqual(10, service.WaitingTimes[1].SeatedTick);
            Assert.AreEqual(7, service.WaitingTimes[1].WaitingTicks);
            Assert.AreEqual(6, service.WaitingTimes[2].ArrivalTick);
            Assert.AreEqual(20, service.WaitingTimes[2].SeatedTick);
            Assert.AreEqual(14, service.WaitingTimes[2].WaitingTicks);
        }

        [Test]
        public void MultipleSeatsRecordOnlyWaitingUnitsInArrivalOrder()
        {
            var service = new BuildingService(2, 0.5f);
            service.AddArrivals(5);
            Step(service, 10);
            CollectionAssert.AreEqual(new long[] { 0, 0, 5, 5, 10 },
                new List<UnitWaitingTime>(service.WaitingTimes).ConvertAll(sample => sample.WaitingTicks));
            for (int i = 0; i < 5; i++) Assert.AreEqual(i + 1, service.WaitingTimes[i].UnitId);
        }

        [TestCase(0.5f, 1f, 4, 8)]
        [TestCase(1f, 0.5f, 4, 12)]
        public void PerformanceChangesAffectQueueWaitButNeverExistingSamples(
            float initial, float updated, int before, int after)
        {
            var service = new BuildingService(1, 1f, initial);
            service.AddArrivals(2);
            Step(service, before);
            service.SetPerformanceMultiplier(updated);
            Step(service, after);
            Assert.AreEqual(0, service.WaitingTimes[0].WaitingTicks);
            Assert.AreEqual(before + after, service.WaitingTimes[1].WaitingTicks);
            Step(service, 50);
            Assert.AreEqual(before + after, service.WaitingTimes[1].WaitingTicks);
        }

        [Test]
        public void FastModeKeepsElapsedQueueTime()
        {
            var service = new BuildingService(1, 2f);
            service.AddArrivals(2);
            Step(service, 4);
            service.UpdateFastMode(1f);
            Step(service, 8);
            Assert.AreEqual(12, service.WaitingTimes[1].WaitingTicks);
            Assert.AreEqual(1.2d, service.WaitingTimes[1].WaitingSeconds, 1e-12);
        }

        [Test]
        public void InvalidAndZeroArrivalsCreateNoSamples()
        {
            var service = new BuildingService(1, 1f);
            Assert.Throws<ArgumentOutOfRangeException>(() => service.AddArrivals(-1));
            service.AddArrivals(0);
            Assert.AreEqual(0, service.WaitingTimes.Count);
            Assert.AreEqual(0, service.TotalArrived);
        }

        [Test]
        public void WaitingTimeViewCannotBeModified()
        {
            var service = new BuildingService(1, 1f);
            service.AddArrivals(1);
            Assert.Throws<NotSupportedException>(() =>
                ((IList<UnitWaitingTime>)service.WaitingTimes).Clear());
            Assert.AreEqual(1, service.WaitingTimes.Count);
        }

        [TestCase(0.1f, 0)]
        [TestCase(0.6f, 0)]
        [TestCase(1f, 4)]
        public void SimulationUsesActualArrivalStepWithoutExtraWaitTick(float serviceSeconds, int expectedWait)
        {
            var simulation = Create(serviceSeconds, out _);
            Start(simulation, 2, 1f);
            for (int i = 0; i < 100 && !simulation.AllNeedsSatisfied; i++) simulation.Step();
            var samples = simulation.GetWaitingTimesThisWave(NeedType.Food);
            Assert.AreEqual(2, samples.Count);
            Assert.AreEqual(5, samples[0].ArrivalTick);
            Assert.AreEqual(5, samples[0].SeatedTick);
            Assert.AreEqual(10, samples[1].ArrivalTick);
            Assert.AreEqual(expectedWait, samples[1].WaitingTicks);
            Assert.AreEqual(0, samples[0].WaitingTicks);
        }

        [Test]
        public void PauseAddsNoWaitingOrServiceTicks()
        {
            var simulation = Create(0.5f, out var service);
            Start(simulation, 2, 0.1f);
            simulation.Step();
            simulation.Pause();
            Step(simulation, 100);
            Assert.AreEqual(1, service.ElapsedTicks);
            Assert.AreEqual(1, service.WaitingTimes.Count);
            simulation.Resume();
            Step(simulation, 4);
            Assert.AreEqual(4, service.WaitingTimes[1].WaitingTicks);
            Assert.AreEqual(1, service.WaitingTimes[1].ArrivalTick);
            Assert.AreEqual(5, service.WaitingTimes[1].SeatedTick);
        }

        [Test]
        public void EachWaveHasItsOwnReadOnlySnapshot()
        {
            var simulation = Create(1f, out var service);
            Start(simulation, 3, 0.1f);
            for (int i = 0; i < 100 && !simulation.AllNeedsSatisfied; i++) simulation.Step();
            var first = simulation.GetWaitingTimesThisWave(NeedType.Food);
            Assert.AreEqual(3, first.Count);
            Assert.Throws<NotSupportedException>(() => ((IList<UnitWaitingTime>)first).Clear());
            Assert.AreEqual(9, first[1].WaitingTicks);
            Start(simulation, 1, 0.1f);
            Assert.AreEqual(0, simulation.GetWaitingTimesThisWave(NeedType.Food).Count);
            for (int i = 0; i < 100 && !simulation.AllNeedsSatisfied; i++) simulation.Step();
            var second = simulation.GetWaitingTimesThisWave(NeedType.Food);
            Assert.AreEqual(1, second.Count);
            Assert.AreEqual(4, second[0].UnitId);
            Assert.AreEqual(0, second[0].WaitingTicks);
            Assert.AreEqual(3, first.Count);
            Assert.AreEqual(4, service.WaitingTimes.Count);
        }

        [Test]
        public void WaitsAreGroupedByNeedAndAvailableAtWaveCompletion()
        {
            var simulation = Create(0.5f, out _);
            simulation.RegisterBuildingService(NeedType.Drinks, new BuildingService(2, 0.5f));
            var mapping = new Dictionary<string, NeedType>
            {
                { "Restaurant", NeedType.Food }, { "BeachBar", NeedType.Drinks }
            };
            var waves = new[] { new WavesConfig { wave_id = 1,
                needs = new NeedsConfig { food = 2, drinks = 2 } } };
            var cycle = new GameCycle(simulation, waves, 0.1f, mapping, new HashSet<string>(mapping.Keys));
            int completed = 0;
            cycle.WaveCompleted += waveId =>
            {
                completed++;
                Assert.AreEqual(2, simulation.GetWaitingTimesThisWave(NeedType.Food).Count);
                Assert.AreEqual(4, simulation.GetWaitingTimesThisWave(NeedType.Food)[1].WaitingTicks);
                Assert.AreEqual(0, simulation.GetWaitingTimesThisWave(NeedType.Drinks)[1].WaitingTicks);
            };
            cycle.StartNextWave();
            for (int i = 0; i < 100 && cycle.CurrentState == GameState.WaveInProgress; i++) cycle.UpdateStep();
            Assert.AreEqual(1, completed);
        }

        [Test]
        public void EmptyWaveHasNoSamplesAndInvalidQueriesAreRejected()
        {
            var simulation = new GameSimulation();
            Assert.Throws<InvalidOperationException>(() => simulation.GetWaitingTimesThisWave(NeedType.Food));
            Start(simulation, 0, 0.1f);
            simulation.Step();
            Assert.IsTrue(simulation.AllNeedsSatisfied);
            Assert.AreEqual(0, simulation.GetWaitingTimesThisWave(NeedType.Food).Count);
            Assert.Throws<ArgumentException>(() => simulation.GetWaitingTimesThisWave(NeedType.Relax));
        }
    }
}

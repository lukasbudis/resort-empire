using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace ResortEmpire
{
    public class NeedStateIntegrationTests
    {
        private static GameSimulation StartSingleNeedWave(
            NeedType need, BuildingService service, int demand, float arrivalSeconds,
            int bottleneckThreshold = 4)
        {
            var simulation = new GameSimulation(bottleneckThreshold);
            simulation.RegisterBuildingService(need, service);
            simulation.StartWave(
                new Dictionary<NeedType, int> { { need, demand } },
                new[] { need },
                arrivalSeconds);
            return simulation;
        }

        // Runs the wave to its end and returns every state the need entered.
        private static List<NeedState> RunToEnd(GameSimulation simulation, NeedType need)
        {
            var states = new List<NeedState>();
            simulation.NeedStateChanged += (changedNeed, state) =>
            {
                if (changedNeed == need)
                    states.Add(state);
            };

            int ticks = 0;
            while (ticks < 10000 && !simulation.AllNeedsSatisfied)
            {
                simulation.Step();
                ticks++;
            }

            Assert.IsTrue(simulation.AllNeedsSatisfied, "The wave did not finish.");
            return states;
        }

        [Test]
        public void SlowBuildingGoesThroughEachStateOnce()
        {
            // 20 units in 10 s = 2/s against one seat served in 1 s = 1/s.
            var food = new BuildingService(1, 1f);
            var simulation = StartSingleNeedWave(NeedType.Food, food, 20, 10f);

            var states = RunToEnd(simulation, NeedType.Food);

            CollectionAssert.AreEqual(
                new[] { NeedState.Bottleneck, NeedState.Recovering, NeedState.Ok, NeedState.Satisfied },
                states);
            Assert.AreEqual(20, food.TotalServed);
            Assert.AreEqual(NeedState.Satisfied, simulation.GetNeedState(NeedType.Food));
        }

        [Test]
        public void FastBuildingNeverReportsBottleneck()
        {
            // 10 units in 5 s = 2/s against three seats served in 0,5 s = 6/s.
            var drinks = new BuildingService(3, 0.5f);
            var simulation = StartSingleNeedWave(NeedType.Drinks, drinks, 10, 5f);

            var states = RunToEnd(simulation, NeedType.Drinks);

            CollectionAssert.AreEqual(new[] { NeedState.Satisfied }, states);
        }

        [Test]
        public void SeatedUnitsAloneDoNotCauseBottleneck()
        {
            // Ten seats take the whole wave at once: service is slow, but nobody waits.
            var rooms = new BuildingService(10, 20f);
            var simulation = StartSingleNeedWave(NeedType.Rooms, rooms, 10, 1f);

            var states = RunToEnd(simulation, NeedType.Rooms);

            CollectionAssert.AreEqual(new[] { NeedState.Satisfied }, states);
        }

        [Test]
        public void UnstaffedBuildingFallsIntoBottleneckWhereStaffedDoesNot()
        {
            // 16 units in 10 s = 1,6/s; two seats at 1 s: staffed 2/s, unstaffed 0,66/s.
            var staffed = StartSingleNeedWave(NeedType.Drinks, new BuildingService(2, 1f, 1f), 16, 10f);
            var unstaffed = StartSingleNeedWave(NeedType.Drinks, new BuildingService(2, 1f, 0.33f), 16, 10f);

            CollectionAssert.DoesNotContain(RunToEnd(staffed, NeedType.Drinks), NeedState.Bottleneck);
            CollectionAssert.Contains(RunToEnd(unstaffed, NeedType.Drinks), NeedState.Bottleneck);
        }

        // With a fixed Worker placement the rates do not change during the wave,
        // so every state may be entered at most once (no blinking back and forth).
        [TestCase(1, 1f, 1f, 20, 10f)]
        [TestCase(3, 0.5f, 1f, 10, 5f)]
        [TestCase(2, 1f, 0.33f, 16, 10f)]
        [TestCase(3, 12f, 1f, 22, 90f)]
        [TestCase(3, 12f, 0.33f, 16, 90f)]
        [TestCase(3, 12f, 0.33f, 30, 90f)]
        public void StateNeverFlipsBackWithFixedWorkers(
            int capacity, float serviceSeconds, float performance, int demand, float arrivalSeconds)
        {
            var service = new BuildingService(capacity, serviceSeconds, performance);
            var simulation = StartSingleNeedWave(NeedType.Food, service, demand, arrivalSeconds);

            var states = RunToEnd(simulation, NeedType.Food);

            CollectionAssert.AllItemsAreUnique(states);
            Assert.AreEqual(NeedState.Satisfied, states[states.Count - 1]);
            Assert.AreEqual(demand, service.TotalServed);
        }

        [Test]
        public void ThresholdDecidesWhenBottleneckStarts()
        {
            var lowThreshold = StartSingleNeedWave(NeedType.Food, new BuildingService(1, 1f), 20, 10f, 4);
            var highThreshold = StartSingleNeedWave(NeedType.Food, new BuildingService(1, 1f), 20, 10f, 100);

            CollectionAssert.Contains(RunToEnd(lowThreshold, NeedType.Food), NeedState.Bottleneck);
            CollectionAssert.DoesNotContain(RunToEnd(highThreshold, NeedType.Food), NeedState.Bottleneck);
        }

        // Slow wave: 40 units in 10 s against one seat served in 1 s. The queue grows
        // by at most one unit per tick, so the warning starts exactly when one unit
        // more than the threshold is waiting.
        private static int WaitingWhenBottleneckStarts(int bottleneckThreshold)
        {
            var food = new BuildingService(1, 1f);
            var simulation = StartSingleNeedWave(NeedType.Food, food, 40, 10f, bottleneckThreshold);

            while (simulation.TickCount < 1000)
            {
                simulation.Step();
                if (simulation.GetNeedState(NeedType.Food) == NeedState.Bottleneck)
                    return food.WaitingCount;
            }

            return -1;
        }

        [Test]
        public void ProjectConfigThresholdDrivesSimulation()
        {
            var config = UtilConfig.ParseConfig(Resources.Load<TextAsset>("config").text);

            Assert.AreEqual(
                config.needStateConfig.bottleneckThreshold + 1,
                WaitingWhenBottleneckStarts(config.needStateConfig.bottleneckThreshold));
        }

        [Test]
        public void ParsedThresholdDrivesSimulation()
        {
            var config = UtilConfig.ParseConfig(
                "{\"workerConfig\":{\"unstaffedPerformanceMultiplier\":0.33}," +
                "\"needStateConfig\":{\"bottleneckThreshold\":7},\"resourceConfig\":{}}");

            Assert.AreEqual(8, WaitingWhenBottleneckStarts(config.needStateConfig.bottleneckThreshold));
        }

        [Test]
        public void RoundedServiceTimeCountsAsSlowerService()
        {
            // 0,25 s rounds up to 3 ticks: on paper 4/s like the arrival, really 3,33/s.
            var drinks = new BuildingService(1, 0.25f);
            var simulation = StartSingleNeedWave(NeedType.Drinks, drinks, 40, 10f);

            CollectionAssert.Contains(RunToEnd(simulation, NeedType.Drinks), NeedState.Bottleneck);
        }

        [Test]
        public void HandlerStartingNextWaveGetsNoStaleEvents()
        {
            var simulation = new GameSimulation();
            simulation.StartWave(
                new Dictionary<NeedType, int> { { NeedType.Food, 0 }, { NeedType.Drinks, 0 } },
                new[] { NeedType.Food, NeedType.Drinks },
                0.1f);

            var events = new List<NeedType>();
            simulation.NeedStateChanged += (need, state) =>
            {
                events.Add(need);
                if (events.Count == 1)
                    simulation.StartWave(
                        new Dictionary<NeedType, int> { { NeedType.Food, 5 }, { NeedType.Drinks, 5 } },
                        new[] { NeedType.Food, NeedType.Drinks },
                        10f);
            };

            simulation.Step();

            Assert.AreEqual(1, events.Count);
            Assert.AreEqual(NeedState.Ok, simulation.GetNeedState(NeedType.Food));
            Assert.AreEqual(NeedState.Ok, simulation.GetNeedState(NeedType.Drinks));
        }

        [Test]
        public void EmptyWaveFinishesOnlyAfterArrivalCloses()
        {
            var simulation = new GameSimulation();
            simulation.StartWave(new Dictionary<NeedType, int>(), new NeedType[0], 1f);

            Assert.IsFalse(simulation.AllNeedsSatisfied);

            for (int i = 0; i < 10; i++)
                simulation.Step();

            Assert.IsTrue(simulation.AllNeedsSatisfied);
        }

        [Test]
        public void NewWaveStartsWithEveryNeedOk()
        {
            var food = new BuildingService(1, 1f);
            var simulation = StartSingleNeedWave(NeedType.Food, food, 20, 10f);
            RunToEnd(simulation, NeedType.Food);

            simulation.StartWave(
                new Dictionary<NeedType, int> { { NeedType.Food, 5 } },
                new[] { NeedType.Food },
                10f);

            Assert.AreEqual(NeedState.Ok, simulation.GetNeedState(NeedType.Food));
            Assert.IsFalse(simulation.AllNeedsSatisfied);
        }

        [Test]
        public void NeedOutsideCurrentWaveHasNoState()
        {
            var simulation = StartSingleNeedWave(NeedType.Food, new BuildingService(1, 1f), 5, 10f);

            Assert.Throws<System.ArgumentException>(() => simulation.GetNeedState(NeedType.Relax));
            Assert.IsFalse(new GameSimulation().AllNeedsSatisfied);
        }

        [Test]
        public void PauseFreezesNeedState()
        {
            var food = new BuildingService(1, 1f);
            var simulation = StartSingleNeedWave(NeedType.Food, food, 20, 10f);
            while (simulation.GetNeedState(NeedType.Food) != NeedState.Bottleneck)
                simulation.Step();

            simulation.Pause();
            for (int i = 0; i < 200; i++)
                simulation.Step();

            Assert.AreEqual(NeedState.Bottleneck, simulation.GetNeedState(NeedType.Food));
        }
    }
}
using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace ResortEmpire
{
    public class WaveSummaryTests
    {
        private const float ArrivalSeconds = 20f;
        private const int Demand = 20;

        private static WaveSummary Summary(params (NeedType need, int ticks)[] entries)
        {
            var ticks = new Dictionary<NeedType, int>();
            foreach (var entry in entries)
                ticks.Add(entry.need, entry.ticks);
            return WaveSummary.FromBottleneckTicks(ticks);
        }

        // One seat and 1 s of service: with multiplier 1 the building keeps up with 20 units
        // in 20 s, below 1 it falls behind and the need enters BOTTLENECK.
        private static BuildingService Building(float performanceMultiplier) =>
            new BuildingService(1, 1f, performanceMultiplier);

        private static GameSimulation Simulation(params (NeedType need, BuildingService service)[] buildings)
        {
            var simulation = new GameSimulation(bottleneckThreshold: 1);
            foreach (var building in buildings)
                simulation.RegisterBuildingService(building.need, building.service);
            return simulation;
        }

        private static void StartWave(GameSimulation simulation, params (NeedType need, int demand)[] needs)
        {
            var demand = new Dictionary<NeedType, int>();
            var available = new List<NeedType>();
            foreach (var entry in needs)
            {
                demand.Add(entry.need, entry.demand);
                available.Add(entry.need);
            }
            simulation.StartWave(demand, available, ArrivalSeconds);
        }

        private static void RunToEnd(GameSimulation simulation)
        {
            int ticks = 0;
            while (ticks++ < 10000 && !simulation.AllNeedsSatisfied)
                simulation.Step();
            Assert.IsTrue(simulation.AllNeedsSatisfied, "The wave did not finish.");
        }

        private static void StepUntil(GameSimulation simulation, NeedType need, NeedState state)
        {
            int ticks = 0;
            while (ticks++ < 10000 && simulation.GetNeedState(need) != state)
                simulation.Step();
            Assert.AreEqual(state, simulation.GetNeedState(need));
        }

        [Test]
        public void CommonCase_ListsNeedsWithoutBottleneckAndTheLongestOne()
        {
            WaveSummary summary = Summary(
                (NeedType.Food, 10), (NeedType.Rooms, 0), (NeedType.Drinks, 30), (NeedType.Relax, 0));

            CollectionAssert.AreEqual(new[] { NeedType.Rooms, NeedType.Relax }, summary.WentWell);
            Assert.AreEqual(NeedType.Drinks, summary.NeedsImprovement);
            Assert.IsTrue(summary.HadBottleneck);
            Assert.IsFalse(summary.AllNeedsHadBottleneck);
        }

        [Test]
        public void NoBottleneck_EveryNeedWentWellAndNothingNeedsImprovement()
        {
            WaveSummary summary = Summary((NeedType.Food, 0), (NeedType.Drinks, 0), (NeedType.Rooms, 0));

            // Display order of S2, not the order of the input.
            CollectionAssert.AreEqual(
                new[] { NeedType.Rooms, NeedType.Drinks, NeedType.Food }, summary.WentWell);
            Assert.IsNull(summary.NeedsImprovement);
            Assert.IsFalse(summary.HadBottleneck);
            Assert.IsFalse(summary.AllNeedsHadBottleneck);
        }

        [Test]
        public void AllNeedsInBottleneck_WentWellNamesTheShortestOne()
        {
            WaveSummary summary = Summary((NeedType.Rooms, 12), (NeedType.Drinks, 30), (NeedType.Food, 5));

            CollectionAssert.AreEqual(new[] { NeedType.Food }, summary.WentWell);
            Assert.AreEqual(NeedType.Drinks, summary.NeedsImprovement);
            Assert.IsTrue(summary.AllNeedsHadBottleneck);
        }

        [Test]
        public void TiedLongest_FirstNeedInDisplayOrderNeedsImprovement()
        {
            WaveSummary summary = Summary((NeedType.Food, 30), (NeedType.Drinks, 30), (NeedType.Rooms, 0));

            Assert.AreEqual(NeedType.Drinks, summary.NeedsImprovement);
            CollectionAssert.AreEqual(new[] { NeedType.Rooms }, summary.WentWell);
        }

        [Test]
        public void AllNeedsTied_BestAndWorstAreDifferentNeeds()
        {
            WaveSummary summary = Summary((NeedType.Food, 8), (NeedType.Rooms, 8), (NeedType.Drinks, 8));

            Assert.AreEqual(NeedType.Rooms, summary.NeedsImprovement);
            CollectionAssert.AreEqual(new[] { NeedType.Drinks }, summary.WentWell);
            Assert.IsTrue(summary.AllNeedsHadBottleneck);
        }

        [Test]
        public void SingleNeedInBottleneck_IsNotListedAsWentWell()
        {
            WaveSummary summary = Summary((NeedType.Food, 8));

            Assert.AreEqual(NeedType.Food, summary.NeedsImprovement);
            CollectionAssert.IsEmpty(summary.WentWell);
            Assert.IsTrue(summary.AllNeedsHadBottleneck);
        }

        [Test]
        public void EmptyWave_HasEmptySummary()
        {
            WaveSummary summary = Summary();

            CollectionAssert.IsEmpty(summary.WentWell);
            Assert.IsNull(summary.NeedsImprovement);
            Assert.IsFalse(summary.AllNeedsHadBottleneck);
        }

        [Test]
        public void InvalidInput_IsRejected()
        {
            Assert.Throws<ArgumentNullException>(() => WaveSummary.FromBottleneckTicks(null));
            Assert.Throws<ArgumentOutOfRangeException>(() => Summary((NeedType.Food, -1)));
        }

        [Test]
        public void Summary_DoesNotChangeWithItsSource()
        {
            var ticks = new Dictionary<NeedType, int> { { NeedType.Food, 5 }, { NeedType.Rooms, 0 } };
            WaveSummary summary = WaveSummary.FromBottleneckTicks(ticks);

            ticks[NeedType.Food] = 0;
            ticks[NeedType.Rooms] = 99;

            Assert.AreEqual(5, summary.BottleneckTicks[NeedType.Food]);
            Assert.AreEqual(NeedType.Food, summary.NeedsImprovement);
            Assert.Throws<NotSupportedException>(
                () => ((IDictionary<NeedType, int>)summary.BottleneckTicks)[NeedType.Food] = 1);
        }

        [Test]
        public void Simulation_CountsOnlyStepsThatEndInBottleneck()
        {
            GameSimulation simulation = Simulation(
                (NeedType.Food, Building(0.5f)), (NeedType.Rooms, Building(1f)));
            StartWave(simulation, (NeedType.Food, Demand), (NeedType.Rooms, Demand));

            int bottleneckSteps = 0;
            int recoveringSteps = 0;
            int ticks = 0;
            while (ticks++ < 10000 && !simulation.AllNeedsSatisfied)
            {
                simulation.Step();
                if (simulation.GetNeedState(NeedType.Food) == NeedState.Bottleneck) bottleneckSteps++;
                if (simulation.GetNeedState(NeedType.Food) == NeedState.Recovering) recoveringSteps++;
            }

            Assert.IsTrue(simulation.AllNeedsSatisfied, "The wave did not finish.");
            Assert.Greater(bottleneckSteps, 0);
            Assert.Greater(recoveringSteps, 0);
            Assert.AreEqual(bottleneckSteps, simulation.BottleneckTicksThisWave[NeedType.Food]);
            Assert.AreEqual(0, simulation.BottleneckTicksThisWave[NeedType.Rooms]);

            WaveSummary summary = simulation.GetWaveSummary();
            Assert.AreEqual(NeedType.Food, summary.NeedsImprovement);
            CollectionAssert.AreEqual(new[] { NeedType.Rooms }, summary.WentWell);
            Assert.AreEqual(bottleneckSteps, summary.BottleneckTicks[NeedType.Food]);
            Assert.IsFalse(summary.AllNeedsHadBottleneck);
        }

        [Test]
        public void Simulation_WaveWithoutBottleneck_HasNothingToImprove()
        {
            GameSimulation simulation = Simulation(
                (NeedType.Food, Building(1f)), (NeedType.Rooms, Building(1f)));
            StartWave(simulation, (NeedType.Food, Demand), (NeedType.Rooms, Demand));
            RunToEnd(simulation);

            WaveSummary summary = simulation.GetWaveSummary();
            Assert.IsFalse(summary.HadBottleneck);
            CollectionAssert.AreEqual(new[] { NeedType.Rooms, NeedType.Food }, summary.WentWell);
        }

        [Test]
        public void Simulation_EveryNeedInBottleneck_NamesLeastAffectedNeed()
        {
            GameSimulation simulation = Simulation(
                (NeedType.Food, Building(0.8f)), (NeedType.Rooms, Building(0.5f)));
            StartWave(simulation, (NeedType.Food, Demand), (NeedType.Rooms, Demand));
            RunToEnd(simulation);

            int foodTicks = simulation.BottleneckTicksThisWave[NeedType.Food];
            int roomsTicks = simulation.BottleneckTicksThisWave[NeedType.Rooms];
            Assert.Greater(foodTicks, 0);
            Assert.Greater(roomsTicks, foodTicks);

            WaveSummary summary = simulation.GetWaveSummary();
            Assert.IsTrue(summary.AllNeedsHadBottleneck);
            Assert.AreEqual(NeedType.Rooms, summary.NeedsImprovement);
            CollectionAssert.AreEqual(new[] { NeedType.Food }, summary.WentWell);
        }

        [Test]
        public void Simulation_PauseAddsNoBottleneckTime()
        {
            GameSimulation simulation = Simulation((NeedType.Food, Building(0.5f)));
            StartWave(simulation, (NeedType.Food, Demand));
            StepUntil(simulation, NeedType.Food, NeedState.Bottleneck);
            int before = simulation.BottleneckTicksThisWave[NeedType.Food];
            Assert.Greater(before, 0);

            simulation.Pause();
            for (int i = 0; i < 50; i++) simulation.Step();
            Assert.AreEqual(before, simulation.BottleneckTicksThisWave[NeedType.Food]);

            simulation.Resume();
            simulation.Step();
            Assert.AreEqual(NeedState.Bottleneck, simulation.GetNeedState(NeedType.Food));
            Assert.AreEqual(before + 1, simulation.BottleneckTicksThisWave[NeedType.Food]);
        }

        [Test]
        public void Simulation_NextWaveStartsFromZeroAndOldSummaryStays()
        {
            GameSimulation simulation = Simulation((NeedType.Food, Building(0.5f)));
            StartWave(simulation, (NeedType.Food, Demand));
            RunToEnd(simulation);
            WaveSummary first = simulation.GetWaveSummary();
            int firstTicks = first.BottleneckTicks[NeedType.Food];
            Assert.Greater(firstTicks, 0);

            StartWave(simulation, (NeedType.Food, 1));
            Assert.AreEqual(0, simulation.BottleneckTicksThisWave[NeedType.Food]);
            RunToEnd(simulation);

            Assert.IsFalse(simulation.GetWaveSummary().HadBottleneck);
            Assert.AreEqual(firstTicks, first.BottleneckTicks[NeedType.Food]);
            Assert.AreEqual(NeedType.Food, first.NeedsImprovement);
        }

        [Test]
        public void Simulation_CountIncludesTheStepWhenStateChangeIsRaised()
        {
            GameSimulation simulation = Simulation((NeedType.Food, Building(0.5f)));
            var seen = new List<(NeedState state, int ticks)>();
            simulation.NeedStateChanged += (need, state) =>
                seen.Add((state, simulation.BottleneckTicksThisWave[need]));
            StartWave(simulation, (NeedType.Food, Demand));
            RunToEnd(simulation);

            int entered = seen.FindIndex(change => change.state == NeedState.Bottleneck);
            int left = seen.FindLastIndex(change => change.state == NeedState.Recovering);
            Assert.GreaterOrEqual(entered, 0);
            Assert.Greater(left, entered);
            Assert.AreEqual(1, seen[entered].ticks);
            Assert.AreEqual(simulation.BottleneckTicksThisWave[NeedType.Food], seen[left].ticks);
        }

        [Test]
        public void Simulation_HandlerStartingNextWaveStartsFromZero()
        {
            GameSimulation simulation = Simulation((NeedType.Food, Building(0.5f)));
            bool restarted = false;
            simulation.NeedStateChanged += (need, state) =>
            {
                if (state != NeedState.Satisfied || restarted)
                    return;

                restarted = true;
                StartWave(simulation, (NeedType.Food, Demand));
            };
            StartWave(simulation, (NeedType.Food, Demand));

            int ticks = 0;
            while (ticks++ < 10000 && !restarted)
                simulation.Step();

            Assert.IsTrue(restarted);
            Assert.AreEqual(0, simulation.BottleneckTicksThisWave[NeedType.Food]);
            Assert.AreEqual(NeedState.Ok, simulation.GetNeedState(NeedType.Food));
        }

        [Test]
        public void Simulation_NeedWithoutDemandIsLeftOut()
        {
            GameSimulation simulation = Simulation(
                (NeedType.Food, Building(1f)), (NeedType.Rooms, Building(1f)));
            StartWave(simulation, (NeedType.Food, Demand), (NeedType.Rooms, 0));
            RunToEnd(simulation);

            WaveSummary summary = simulation.GetWaveSummary();
            Assert.IsFalse(summary.BottleneckTicks.ContainsKey(NeedType.Rooms));
            CollectionAssert.AreEqual(new[] { NeedType.Food }, summary.WentWell);
        }

        [Test]
        public void Simulation_SummaryBeforeFirstWaveThrows()
        {
            GameSimulation simulation = Simulation((NeedType.Food, Building(1f)));

            Assert.Throws<InvalidOperationException>(() => simulation.GetWaveSummary());
        }
    }
}

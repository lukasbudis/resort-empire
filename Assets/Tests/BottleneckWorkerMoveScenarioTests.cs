using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace ResortEmpire
{
    // AC2 and AC2b of the Build document without graphics (SCRUM-140): a need in BOTTLENECK,
    // the player moves a Worker through S3, the need goes RECOVERING -> OK and the source slows down.
    public class BottleneckWorkerMoveScenarioTests
    {
        // Wave 2 of the prototype as in config.json at the time of writing. The numbers are
        // copied on purpose, so balancing in config.json does not change what this scenario checks.
        private const int Capacity = 3;
        private const float ServiceSeconds = 12f;
        private const float UnstaffedPerformance = 0.33f;
        private const float ArrivalSeconds = 90f;
        private const int Threshold = 4;

        private static Dictionary<string, NeedType> BuildingNeeds() => new()
        {
            { "Reception", NeedType.Rooms }, { "Restaurant", NeedType.Food }, { "BeachBar", NeedType.Drinks }
        };

        // The Beach Bar starts without a Worker, as in the prototype.
        private static WorkerInitialState[] InitialWorkers() => new[]
        {
            new WorkerInitialState { id = "Worker1", buildingId = "Reception" },
            new WorkerInitialState { id = "Worker2", buildingId = "Restaurant" }
        };

        private static GameSimulation StartWaveTwo(out Dictionary<string, BuildingService> services)
        {
            var simulation = new GameSimulation(Threshold);
            services = new Dictionary<string, BuildingService>();
            foreach (var pair in BuildingNeeds())
            {
                services.Add(pair.Key, new BuildingService(Capacity, ServiceSeconds));
                simulation.RegisterBuildingService(pair.Value, services[pair.Key]);
            }

            simulation.InitializeWorkers(BuildingNeeds(), InitialWorkers(), UnstaffedPerformance);
            simulation.StartWave(new Dictionary<NeedType, int>
            {
                { NeedType.Rooms, 12 }, { NeedType.Food, 10 }, { NeedType.Drinks, 16 }
            }, new[] { NeedType.Rooms, NeedType.Food, NeedType.Drinks }, ArrivalSeconds);
            return simulation;
        }

        // Steps until the need enters BOTTLENECK; fails if guests stop arriving first.
        private static void RunUntilBottleneck(GameSimulation simulation, NeedType need)
        {
            while (simulation.GetNeedState(need) != NeedState.Bottleneck)
            {
                Assert.AreEqual(WavePhase.Incoming, simulation.CurrentWave.Phase,
                    $"{need} did not enter BOTTLENECK while guests were arriving.");
                simulation.Step();
            }
        }

        private static List<NeedState> RecordStates(GameSimulation simulation, NeedType need)
        {
            var states = new List<NeedState>();
            simulation.NeedStateChanged += (changedNeed, state) =>
            {
                if (changedNeed == need)
                    states.Add(state);
            };
            return states;
        }

        // What S3 shows for a building, built the same way as WorkerAllocationScreen.Render.
        private static WorkerAllocationScreenData OpenS3(GameSimulation simulation, string buildingId) =>
            WorkerAllocationPresenter.Build(
                buildingId, simulation.WorkerAssignments, BuildingNeeds(), simulation.CanMoveWorker);

        private static BuildingCardData Card(WorkerAllocationScreenData screen, string buildingId)
        {
            foreach (var card in screen.Cards)
            {
                if (card.BuildingId == buildingId)
                    return card;
            }

            Assert.Fail($"S3 for {screen.TargetBuildingId} has no card for {buildingId}.");
            return default;
        }

        [Test]
        public void MovingWorkerToBottleneckBuildingRecoversNeedAndLeavesSourceUnstaffed()
        {
            var simulation = StartWaveTwo(out var services);
            var beachBar = services["BeachBar"];
            var reception = services["Reception"];
            double drinksArrivalRate = simulation.CurrentWave.GetArrivalRatePerTick(NeedType.Drinks);
            double staffedRate = reception.ServiceRatePerTick;
            double unstaffedRate = beachBar.ServiceRatePerTick;

            // Given: Drinks is in BOTTLENECK because the unstaffed Beach Bar is slower than the arrivals.
            RunUntilBottleneck(simulation, NeedType.Drinks);
            Assert.Less(unstaffedRate, drinksArrivalRate);
            Assert.Greater(staffedRate, drinksArrivalRate);
            Assert.Greater(beachBar.WaitingCount, Threshold);

            // Tapping Drinks in S2 opens S3 for the Beach Bar, which offers the Reception Worker.
            string target = simulation.GetBuildingIdForNeed(NeedType.Drinks);
            Assert.AreEqual("BeachBar", target);
            var s3 = OpenS3(simulation, target);
            Assert.IsFalse(s3.TargetHasWorker);
            Assert.IsTrue(Card(s3, "Reception").CanSelect);

            var drinksStates = RecordStates(simulation, NeedType.Drinks);
            var moves = new List<string>();
            simulation.WorkerMoved += (worker, from, to) => moves.Add($"{worker}: {from} -> {to}");
            int tickOfMove = simulation.TickCount;
            int coinsBefore = ResourceValues.getCoins();
            int coinEvents = 0;
            Action<int> countCoinEvents = _ => coinEvents++;

            // When: the player picks the Reception card; the move is free and needs no confirmation.
            ResourceValues.OnCoinsChanged += countCoinEvents;
            try
            {
                Assert.IsTrue(simulation.TryMoveWorker("Reception", target));
            }
            finally
            {
                ResourceValues.OnCoinsChanged -= countCoinEvents;
            }

            CollectionAssert.AreEqual(new[] { "Worker1: Reception -> BeachBar" }, moves);
            Assert.AreEqual(coinsBefore, ResourceValues.getCoins(), "Moving a Worker must not cost Coins.");
            Assert.AreEqual(0, coinEvents, "Moving a Worker must not touch Coins.");

            // Then (AC2): the Beach Bar serves at full speed at once, without any simulation step.
            Assert.AreEqual(tickOfMove, simulation.TickCount);
            Assert.AreEqual(1f, beachBar.PerformanceMultiplier, "Beach Bar utilization must go 0.33 -> 1.0 in the move.");
            Assert.AreEqual(staffedRate, beachBar.ServiceRatePerTick, 1e-12);

            // Then (AC2b): Reception drops to the unstaffed speed at once and is shown without a Worker.
            Assert.AreEqual(UnstaffedPerformance, reception.PerformanceMultiplier, "Reception utilization must go 1.0 -> 0.33 in the move.");
            Assert.AreEqual(unstaffedRate, reception.ServiceRatePerTick, 1e-12);
            Assert.IsFalse(simulation.HasWorker("Reception"));
            Assert.IsNull(simulation.GetWorkerId("Reception"));
            Assert.IsFalse(OpenS3(simulation, "Reception").TargetHasWorker);
            Assert.IsTrue(OpenS3(simulation, target).TargetHasWorker);

            // AC2 leaves open whether the state changes in the move itself or in the next step (0.1 s).
            // Either way the warning is positive after that step. Guests are still arriving,
            // so the move caused it, not the end of the arrival.
            simulation.Step();
            Assert.AreEqual(WavePhase.Incoming, simulation.CurrentWave.Phase);
            Assert.AreEqual(NeedState.Recovering, simulation.GetNeedState(NeedType.Drinks));
            CollectionAssert.AreEqual(new[] { NeedState.Recovering }, drinksStates);

            // Drinks stays RECOVERING while the queue is above the threshold and reaches OK
            // once the queue drops to the threshold, still before the arrival closes.
            while (simulation.GetNeedState(NeedType.Drinks) == NeedState.Recovering)
            {
                Assert.Greater(beachBar.WaitingCount, Threshold, "RECOVERING ended only once the queue was small.");
                Assert.AreEqual(WavePhase.Incoming, simulation.CurrentWave.Phase,
                    "With the Worker the Beach Bar should catch up before guests stop arriving.");
                simulation.Step();
            }

            Assert.AreEqual(NeedState.Ok, simulation.GetNeedState(NeedType.Drinks));
            Assert.LessOrEqual(beachBar.WaitingCount, Threshold);
            Assert.AreEqual(WavePhase.Incoming, simulation.CurrentWave.Phase);

            // For the rest of the wave Drinks never falls back and Reception stays unstaffed.
            int steps = 0;
            while (!simulation.AllNeedsSatisfied)
            {
                Assert.Less(steps++, 10000, "The wave did not finish.");
                simulation.Step();
                Assert.AreNotEqual(NeedState.Bottleneck, simulation.GetNeedState(NeedType.Drinks));
                Assert.AreEqual(UnstaffedPerformance, reception.PerformanceMultiplier);
            }

            CollectionAssert.AreEqual(
                new[] { NeedState.Recovering, NeedState.Ok, NeedState.Satisfied }, drinksStates);
            Assert.AreEqual(1, moves.Count);
        }

        // Control for the scenario above: without the move the warning stays on
        // for the whole arrival, so RECOVERING there comes from the move.
        [Test]
        public void WithoutMoveBottleneckLastsUntilArrivalCloses()
        {
            var simulation = StartWaveTwo(out var services);
            RunUntilBottleneck(simulation, NeedType.Drinks);

            while (simulation.CurrentWave.Phase == WavePhase.Incoming)
            {
                Assert.AreEqual(NeedState.Bottleneck, simulation.GetNeedState(NeedType.Drinks),
                    "Without a Worker the Beach Bar must not catch up while guests arrive.");
                simulation.Step();
            }

            // Only the end of the arrival moves Drinks on (FR-09), with the queue still above the threshold.
            Assert.AreEqual(NeedState.Recovering, simulation.GetNeedState(NeedType.Drinks));
            Assert.Greater(services["BeachBar"].WaitingCount, Threshold);
            Assert.AreEqual(UnstaffedPerformance, services["BeachBar"].PerformanceMultiplier);
        }
    }
}

using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace ResortEmpire
{
    public class WorkerAllocationTests
    {
        private static Dictionary<string, NeedType> Mapping() => new()
        {
            { "Reception", NeedType.Rooms }, { "Restaurant", NeedType.Food }, { "BeachBar", NeedType.Drinks }
        };

        private static WorkerInitialState[] Workers() => new[]
        {
            new WorkerInitialState { id = "Worker1", buildingId = "Reception" },
            new WorkerInitialState { id = "Worker2", buildingId = "Restaurant" }
        };

        private static GameSimulation Create(out Dictionary<string, BuildingService> services)
        {
            services = new Dictionary<string, BuildingService>();
            var simulation = new GameSimulation();
            foreach (var pair in Mapping())
            {
                services.Add(pair.Key, new BuildingService(1, 1f));
                simulation.RegisterBuildingService(pair.Value, services[pair.Key]);
            }
            simulation.InitializeWorkers(Mapping(), Workers(), 0.33f);
            return simulation;
        }

        private static void StartWave(GameSimulation simulation, float seconds = 1f)
        {
            simulation.StartWave(new Dictionary<NeedType, int>
            {
                { NeedType.Rooms, 3 }, { NeedType.Food, 3 }, { NeedType.Drinks, 3 }
            }, new[] { NeedType.Rooms, NeedType.Food, NeedType.Drinks }, seconds);
        }

        [Test]
        public void PlacementPersistsThroughRealGameCycleAwaitingWave()
        {
            var simulation = Create(out var services);
            var waves = new[]
            {
                new WavesConfig { wave_id = 1, needs = new NeedsConfig { rooms = 1, food = 1, drinks = 1 } },
                new WavesConfig { wave_id = 2, needs = new NeedsConfig { rooms = 1, food = 1, drinks = 1 } }
            };
            var cycle = new GameCycle(simulation, waves, 0.1f, Mapping(),
                new HashSet<string>(Mapping().Keys));
            Assert.AreEqual(GameState.AwaitingWave, cycle.CurrentState);
            Assert.IsTrue(simulation.TryMoveWorker("Reception", "BeachBar"));
            cycle.StartNextWave();
            for (int i = 0; i < 100 && cycle.CurrentState == GameState.WaveInProgress; i++)
                cycle.UpdateStep();
            Assert.AreEqual(GameState.AwaitingWave, cycle.CurrentState);
            Assert.AreEqual("BeachBar", simulation.GetWorkerBuildingId("Worker1"));
            Assert.IsTrue(simulation.TryMoveWorker("Restaurant", "Reception"));
            cycle.StartNextWave();
            for (int i = 0; i < 100 && cycle.CurrentState == GameState.WaveInProgress; i++)
                cycle.UpdateStep();
            Assert.AreEqual(GameState.AllWavesCompleted, cycle.CurrentState);
            Assert.AreEqual("Reception", simulation.GetWorkerBuildingId("Worker2"));
            Assert.AreEqual(2, services["Restaurant"].TotalServed);
        }

        [Test]
        public void RunnerFacadeExposesMoveAndForwardsItsEvent()
        {
            bool oldFastMode = UtilConfig.getFastMode();
            var gameObject = new GameObject("SCRUM128RunnerTest");
            try
            {
                UtilConfig.setFastMode(false);
                var runner = gameObject.AddComponent<SimulationRunner>();
                // EditMode nemusi spustit Awake automaticky ako pri behu sceny.
                if (runner.WorkerAssignments == null)
                    typeof(SimulationRunner).GetMethod("Awake",
                        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                        .Invoke(runner, null);
                Assert.AreEqual("BeachBar", runner.GetBuildingIdForNeed(NeedType.Drinks));
                Assert.IsNull(runner.GetBuildingIdForNeed(NeedType.Relax));
                Assert.IsTrue(runner.HasWorker("Reception"));
                Assert.IsNull(runner.GetWorkerId("BeachBar"));
                Assert.IsFalse(runner.CanMoveWorker("Reception", "Pool"));
                int count = 0;
                runner.WorkerMoved += (worker, from, to) =>
                {
                    count++;
                    Assert.AreEqual("Worker1", worker);
                    Assert.AreEqual("Reception", from);
                    Assert.AreEqual("BeachBar", to);
                    Assert.AreEqual(worker, runner.WorkerAssignments[to]);
                };
                Assert.IsTrue(runner.TryMoveWorker("Reception", "BeachBar"));
                Assert.AreEqual(1, count);
                Assert.AreEqual("BeachBar", runner.GetWorkerBuildingId("Worker1"));
                Assert.IsFalse(runner.HasWorker("Reception"));
                runner.StartNextWave();
                UtilConfig.setFastMode(true);
                runner.UpdateFastMode();
                Assert.AreEqual("Worker1", runner.GetWorkerId("BeachBar"));
                Assert.IsFalse(runner.TryMoveWorker("Reception", "Restaurant"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
                UtilConfig.setFastMode(oldFastMode);
            }
        }
        [Test]
        public void InitialPlacementUsesConfigurationAndHasOneEmptyBuilding()
        {
            var simulation = Create(out var services);
            Assert.AreEqual(3, simulation.WorkerAssignments.Count);
            Assert.AreEqual("Worker1", simulation.GetWorkerId("Reception"));
            Assert.AreEqual("Restaurant", simulation.GetWorkerBuildingId("Worker2"));
            Assert.IsNull(simulation.GetWorkerId("BeachBar"));
            Assert.IsFalse(simulation.HasWorker("BeachBar"));
            Assert.AreEqual(1f, services["Reception"].PerformanceMultiplier);
            Assert.AreEqual(0.33f, services["BeachBar"].PerformanceMultiplier);
            Assert.AreEqual("BeachBar", simulation.GetBuildingIdForNeed(NeedType.Drinks));
            Assert.IsNull(simulation.GetBuildingIdForNeed(NeedType.Relax));
        }

        [Test]
        public void MoveUpdatesBothServicesBeforePublishingOneEvent()
        {
            var simulation = Create(out var services);
            int count = 0;
            simulation.WorkerMoved += (worker, from, to) =>
            {
                count++;
                Assert.AreEqual("Worker1", worker);
                Assert.AreEqual("Reception", from);
                Assert.AreEqual("BeachBar", to);
                Assert.IsNull(simulation.GetWorkerId(from));
                Assert.AreEqual(worker, simulation.GetWorkerId(to));
                Assert.AreEqual(0.33f, services[from].PerformanceMultiplier);
                Assert.AreEqual(1f, services[to].PerformanceMultiplier);
            };
            Assert.IsTrue(simulation.CanMoveWorker("Reception", "BeachBar"));
            Assert.IsTrue(simulation.TryMoveWorker("Reception", "BeachBar"));
            Assert.AreEqual(1, count);
            Assert.AreEqual(0, simulation.TickCount);
            Assert.AreEqual("BeachBar", simulation.GetWorkerBuildingId("Worker1"));
        }

        [TestCase("Reception", "Restaurant")]
        [TestCase("Reception", "Reception")]
        [TestCase("BeachBar", "Reception")]
        [TestCase("Unknown", "BeachBar")]
        [TestCase("Reception", "Pool")]
        [TestCase(null, "BeachBar")]
        [TestCase("Reception", null)]
        public void RejectedMoveDoesNotChangePlacementRatesOrEvents(string from, string to)
        {
            var simulation = Create(out var services);
            int count = 0;
            simulation.WorkerMoved += (_, __, ___) => count++;
            Assert.IsFalse(simulation.CanMoveWorker(from, to));
            Assert.IsFalse(simulation.TryMoveWorker(from, to));
            Assert.AreEqual("Worker1", simulation.GetWorkerId("Reception"));
            Assert.AreEqual("Worker2", simulation.GetWorkerId("Restaurant"));
            Assert.IsNull(simulation.GetWorkerId("BeachBar"));
            Assert.AreEqual(1f, services["Reception"].PerformanceMultiplier);
            Assert.AreEqual(0.33f, services["BeachBar"].PerformanceMultiplier);
            Assert.AreEqual(0, count);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void MoveWorksDuringArrivalAndDraining(bool draining)
        {
            var simulation = Create(out _);
            StartWave(simulation, 1f);
            for (int i = 0; i < (draining ? 10 : 5); i++) simulation.Step();
            Assert.AreEqual(draining ? WavePhase.Draining : WavePhase.Incoming, simulation.CurrentWave.Phase);
            Assert.IsTrue(simulation.TryMoveWorker("Reception", "BeachBar"));
            for (int i = 0; i < 500 && !simulation.AllNeedsSatisfied; i++) simulation.Step();
            Assert.IsTrue(simulation.AllNeedsSatisfied);
        }

        [Test]
        public void PlacementSurvivesCompletedWaveAndNextWave()
        {
            var simulation = Create(out var services);
            Assert.IsTrue(simulation.TryMoveWorker("Reception", "BeachBar"));
            for (int wave = 0; wave < 2; wave++)
            {
                StartWave(simulation);
                for (int i = 0; i < 500 && !simulation.AllNeedsSatisfied; i++) simulation.Step();
                Assert.IsTrue(simulation.AllNeedsSatisfied);
                Assert.AreEqual("BeachBar", simulation.GetWorkerBuildingId("Worker1"));
                Assert.AreEqual(0.33f, services["Reception"].PerformanceMultiplier);
                Assert.AreEqual(1f, services["BeachBar"].PerformanceMultiplier);
            }
            Assert.AreEqual(6, services["Reception"].TotalServed);
            Assert.AreEqual(6, services["BeachBar"].TotalServed);
        }

        [Test]
        public void MoveChangesAlreadySeatedUnitsOnBothBuildings()
        {
            var simulation = Create(out var services);
            var source = services["Reception"];
            var target = services["BeachBar"];
            source.AddArrivals(1);
            target.AddArrivals(1);
            for (int i = 0; i < 4; i++) simulation.Step();
            Assert.IsTrue(simulation.TryMoveWorker("Reception", "BeachBar"));
            for (int i = 0; i < 9; i++) simulation.Step();
            Assert.AreEqual(1, target.TotalServed);
            Assert.AreEqual(0, source.TotalServed);
            for (int i = 0; i < 9; i++) simulation.Step();
            Assert.AreEqual(0, source.TotalServed);
            simulation.Step();
            Assert.AreEqual(1, source.TotalServed);
        }

        [Test]
        public void PausedMoveChangesPlacementWithoutAdvancingTime()
        {
            var simulation = Create(out var services);
            services["BeachBar"].AddArrivals(1);
            simulation.Pause();
            Assert.IsTrue(simulation.TryMoveWorker("Reception", "BeachBar"));
            for (int i = 0; i < 100; i++) simulation.Step();
            Assert.AreEqual(0, simulation.TickCount);
            Assert.AreEqual(0, services["BeachBar"].TotalServed);
            simulation.Resume();
            for (int i = 0; i < 10; i++) simulation.Step();
            Assert.AreEqual(1, services["BeachBar"].TotalServed);
        }

        [Test]
        public void AssignmentViewIsReadOnlyAndInitialConfigIsNotMutated()
        {
            var simulation = new GameSimulation();
            var initial = Workers();
            foreach (var pair in Mapping()) simulation.RegisterBuildingService(pair.Value, new BuildingService(1, 1f));
            simulation.InitializeWorkers(Mapping(), initial, 0.33f);
            var view = simulation.WorkerAssignments;
            Assert.Throws<NotSupportedException>(() =>
                ((IDictionary<string, string>)view)["Reception"] = "Other");
            Assert.IsTrue(simulation.TryMoveWorker("Reception", "BeachBar"));
            Assert.AreEqual("Worker1", view["BeachBar"]);
            Assert.AreEqual("Reception", initial[0].buildingId);
            initial[0].buildingId = "Restaurant";
            Assert.AreEqual("BeachBar", simulation.GetWorkerBuildingId("Worker1"));
        }

        [Test]
        public void UnknownQueriesAndUseBeforeInitializationAreDefined()
        {
            var empty = new GameSimulation();
            Assert.IsNull(empty.WorkerAssignments);
            Assert.IsFalse(empty.CanMoveWorker("Reception", "BeachBar"));
            Assert.IsFalse(empty.TryMoveWorker("Reception", "BeachBar"));
            Assert.Throws<InvalidOperationException>(() => empty.GetWorkerId("Reception"));
            var simulation = Create(out _);
            Assert.IsFalse(simulation.HasWorker("Pool"));
            Assert.Throws<ArgumentException>(() => simulation.GetWorkerId("Pool"));
            Assert.Throws<ArgumentException>(() => simulation.GetWorkerBuildingId("Missing"));
            Assert.Throws<ArgumentException>(() => simulation.GetWorkerBuildingId(null));
            Assert.Throws<InvalidOperationException>(() => simulation.InitializeWorkers(Mapping(), Workers(), 0.33f));
            Assert.Throws<InvalidOperationException>(() => simulation.RegisterBuildingService(NeedType.Rooms, new BuildingService(1, 1f)));
        }

        [TestCase("duplicate-worker")]
        [TestCase("duplicate-building")]
        [TestCase("locked-building")]
        [TestCase("missing-worker")]
        [TestCase("extra-worker")]
        [TestCase("null-worker")]
        public void InvalidInitialPlacementIsRejectedBeforeChangingServices(string scenario)
        {
            var services = new Dictionary<string, BuildingService>
            {
                { "Reception", new BuildingService(1, 1f, 0.5f) },
                { "Restaurant", new BuildingService(1, 1f, 0.5f) },
                { "BeachBar", new BuildingService(1, 1f, 0.5f) }
            };
            var workers = new List<WorkerInitialState>(Workers());
            switch (scenario)
            {
                case "duplicate-worker": workers[1].id = workers[0].id; break;
                case "duplicate-building": workers[1].buildingId = workers[0].buildingId; break;
                case "locked-building": workers[1].buildingId = "Pool"; break;
                case "missing-worker": workers.RemoveAt(1); break;
                case "extra-worker": workers.Add(new WorkerInitialState { id = "Worker3", buildingId = "BeachBar" }); break;
                case "null-worker": workers[1] = null; break;
            }
            Assert.Throws<ArgumentException>(() => new WorkerAllocation(services, workers, 0.33f));
            foreach (var service in services.Values) Assert.AreEqual(0.5f, service.PerformanceMultiplier);
        }

        [TestCase(0f)]
        [TestCase(1f)]
        [TestCase(-0.5f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void InvalidUnstaffedPerformanceIsRejected(float value)
        {
            var services = new Dictionary<string, BuildingService> { { "Reception", new BuildingService(1, 1f) } };
            Assert.Throws<ArgumentOutOfRangeException>(() => new WorkerAllocation(services, Array.Empty<WorkerInitialState>(), value));
        }

        [Test]
        public void RealConfigMoveAndFastModeKeepAssignmentsPerformanceAndProgress()
        {
            bool oldFastMode = UtilConfig.getFastMode();
            try
            {
                UtilConfig.setFastMode(false);
                UtilConfig.loadConfig();
                var config = UtilConfig.getConfig();
                var simulation = new GameSimulation();
                var services = new Dictionary<string, BuildingService>();
                foreach (var pair in Mapping())
                {
                    var service = new BuildingService(1, UtilConfig.getServiceTimeById(pair.Key));
                    services.Add(pair.Key, service);
                    simulation.RegisterBuildingService(pair.Value, service);
                }
                simulation.InitializeWorkers(Mapping(), config.workers, config.workerConfig.unstaffedPerformanceMultiplier);
                Assert.IsTrue(simulation.TryMoveWorker("Reception", "BeachBar"));
                services["BeachBar"].AddArrivals(1);
                for (int i = 0; i < 60; i++) simulation.Step(); // Polovica obsluhy pri case 12 s.
                UtilConfig.setFastMode(true);
                simulation.UpdateFastMode();
                Assert.AreEqual(config.workerConfig.unstaffedPerformanceMultiplier, services["Reception"].PerformanceMultiplier);
                Assert.AreEqual(1f, services["BeachBar"].PerformanceMultiplier);
                Assert.AreEqual("BeachBar", simulation.GetWorkerBuildingId("Worker1"));
                Assert.Greater(services["BeachBar"].ServiceRatePerTick, 1.0 / 120);
                for (int i = 0; i < 5; i++) simulation.Step();
                Assert.AreEqual(0, services["BeachBar"].TotalServed);
                simulation.Step();
                Assert.AreEqual(1, services["BeachBar"].TotalServed);
                UtilConfig.setFastMode(false);
                simulation.UpdateFastMode();
                Assert.AreEqual(1.0 / 120, services["BeachBar"].ServiceRatePerTick, 1e-12);
                Assert.AreEqual(0.33f, services["Reception"].PerformanceMultiplier);
                Assert.IsTrue(simulation.TryMoveWorker("BeachBar", "Reception"));
                Assert.AreEqual(1f, services["Reception"].PerformanceMultiplier);
                Assert.AreEqual(0.33f, services["BeachBar"].PerformanceMultiplier);
            }
            finally { UtilConfig.setFastMode(oldFastMode); }
        }
    }
}

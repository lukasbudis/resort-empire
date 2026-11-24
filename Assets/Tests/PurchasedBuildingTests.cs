using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace ResortEmpire
{
    public class PurchasedBuildingTests
    {
        private GameSimulation simulation;
        private BuildingService pool;
        private GameCycle cycle;

        private void CreateSimulation(string receptionWorkerId = "Worker1")
        {
            simulation = new GameSimulation();
            var initial = new Dictionary<string, NeedType>
            {
                { "Reception", NeedType.Rooms }, { "Restaurant", NeedType.Food }, { "BeachBar", NeedType.Drinks }
            };
            foreach (var need in initial.Values)
                simulation.RegisterBuildingService(need, new BuildingService(3, 0.1f));
            pool = new BuildingService(3, 0.1f, 0.33f);
            simulation.RegisterBuildingService(NeedType.Relax, pool);
            simulation.RegisterBuildingService(NeedType.OceanView, new BuildingService(3, 0.1f));
            simulation.InitializeWorkers(initial, new[]
            {
                new WorkerInitialState { id = receptionWorkerId, buildingId = "Reception" },
                new WorkerInitialState { id = "Worker2", buildingId = "Restaurant" }
            }, 0.33f);
            var all = new Dictionary<string, NeedType>(initial)
            {
                { "Pool", NeedType.Relax }, { "Bungalows", NeedType.OceanView }
            };
            var waves = new[]
            {
                new WavesConfig { wave_id = 3, needs = new NeedsConfig { rooms = 1, relax = 2 } },
                new WavesConfig { wave_id = 4, needs = new NeedsConfig { rooms = 1, relax = 3, ocean_view = 2 } }
            };
            cycle = new GameCycle(simulation, waves, 0.1f, all, new HashSet<string>(initial.Keys));
        }

        [Test]
        public void PoolAddsOneWorkerToExistingAllocationAndKeepsBothTransfersAvailable()
        {
            CreateSimulation();
            var assignments = simulation.WorkerAssignments;
            Assert.IsTrue(cycle.TryAddPurchasedBuilding("Pool"));
            Assert.AreEqual(4, assignments.Count);
            Assert.AreEqual(3, assignments.Values.Count(id => id != null));
            Assert.AreEqual("Worker1", assignments["Reception"]);
            Assert.AreEqual("Worker2", assignments["Restaurant"]);
            Assert.IsNull(assignments["BeachBar"]);
            string worker = assignments["Pool"];
            Assert.AreEqual("Pool", simulation.GetWorkerBuildingId(worker));
            Assert.AreEqual("Pool", simulation.GetBuildingIdForNeed(NeedType.Relax));
            Assert.AreEqual(1f, pool.PerformanceMultiplier);

            Assert.IsTrue(simulation.TryMoveWorker("Pool", "BeachBar"));
            Assert.AreEqual(worker, assignments["BeachBar"]);
            Assert.AreEqual(0.33f, pool.PerformanceMultiplier);
            Assert.IsTrue(simulation.TryMoveWorker("Reception", "Pool"));
            Assert.AreEqual("Worker1", assignments["Pool"]);
            Assert.AreEqual(1f, pool.PerformanceMultiplier);

            Assert.IsTrue(cycle.TryAddPurchasedBuilding("Bungalows"));
            Assert.AreEqual("Bungalows", simulation.GetBuildingIdForNeed(NeedType.OceanView));
            Assert.AreEqual(4, assignments.Values.Where(id => id != null).Distinct().Count());
            Assert.IsNull(assignments["Reception"]);
        }

        [Test]
        public void PurchaseBetweenWavesUpdatesForecastAndNextWaveDemand()
        {
            CreateSimulation();
            Assert.IsFalse(cycle.GetNextWaveForecast().Rows.Any(row => row.Need == NeedType.Relax));
            cycle.StartNextWave();
            Assert.IsFalse(cycle.TryAddPurchasedBuilding("Pool"));
            Assert.IsNull(simulation.GetBuildingIdForNeed(NeedType.Relax));
            Assert.IsFalse(simulation.WorkerAssignments.ContainsKey("Pool"));
            Assert.IsFalse(simulation.CurrentWave.TotalDemand.ContainsKey(NeedType.Relax));
            CompleteCurrentWave();

            Assert.AreEqual(GameState.AwaitingWave, cycle.CurrentState);
            Assert.IsFalse(cycle.GetNextWaveForecast().Rows.Any(row => row.Need == NeedType.Relax));
            Assert.IsTrue(cycle.TryAddPurchasedBuilding("Pool"));
            var forecast = cycle.GetNextWaveForecast();
            Assert.AreEqual(4, forecast.WaveId);
            Assert.AreEqual(3, forecast.Rows.Single(row => row.Need == NeedType.Relax).Volume);
            Assert.IsFalse(forecast.Rows.Any(row => row.Need == NeedType.OceanView));

            cycle.StartNextWave();
            Assert.AreEqual(3, simulation.CurrentWave.TotalDemand[NeedType.Relax]);
            Assert.IsFalse(simulation.CurrentWave.TotalDemand.ContainsKey(NeedType.OceanView));
            CompleteCurrentWave();
            Assert.AreEqual(3, pool.TotalServed);
            Assert.AreEqual(GameState.AllWavesCompleted, cycle.CurrentState);
            Assert.IsFalse(cycle.TryAddPurchasedBuilding("Bungalows"));
            Assert.IsFalse(simulation.WorkerAssignments.ContainsKey("Bungalows"));
        }

        private void CompleteCurrentWave()
        {
            for (int i = 0; i < 100 && cycle.CurrentState == GameState.WaveInProgress; i++)
                cycle.UpdateStep();
            Assert.AreNotEqual(GameState.WaveInProgress, cycle.CurrentState);
        }

        [Test]
        public void RepeatedOrInvalidPurchasePreservesWorkerPlacement()
        {
            CreateSimulation();
            Assert.IsFalse(cycle.TryAddPurchasedBuilding(null));
            Assert.IsFalse(cycle.TryAddPurchasedBuilding("Unknown"));
            Assert.IsFalse(cycle.TryAddPurchasedBuilding("BeachBar"));
            Assert.IsTrue(cycle.TryAddPurchasedBuilding("Pool"));
            Assert.IsTrue(simulation.TryMoveWorker("Pool", "BeachBar"));
            string worker = simulation.GetWorkerId("BeachBar");
            Assert.IsFalse(cycle.TryAddPurchasedBuilding("Pool"));
            Assert.IsNull(simulation.GetWorkerId("Pool"));
            Assert.AreEqual(worker, simulation.GetWorkerId("BeachBar"));
            Assert.AreEqual(3, simulation.WorkerAssignments.Values.Count(id => id != null));
            Assert.AreEqual(0.33f, pool.PerformanceMultiplier);
        }

        [Test]
        public void PurchasePreservesExistingWorkerWhoseIdMatchesTheNextNumber()
        {
            CreateSimulation("Worker3");
            Assert.IsTrue(cycle.TryAddPurchasedBuilding("Pool"));

            var assignments = simulation.WorkerAssignments;
            Assert.AreEqual("Worker3", assignments["Reception"]);
            Assert.AreEqual("Worker2", assignments["Restaurant"]);
            Assert.IsNotNull(assignments["Pool"]);
            Assert.AreEqual(3, assignments.Values.Where(id => id != null).Distinct().Count());
            Assert.IsNull(assignments["BeachBar"]);
            Assert.AreEqual("Pool", simulation.GetWorkerBuildingId(assignments["Pool"]));
        }

        [TestCase("Pool", NeedType.Relax, "Bungalows")]
        [TestCase("Bungalows", NeedType.OceanView, "Pool")]
        public void RunnerActivatesConfiguredAreaAndRejectsOtherAreaDuringWave(
            string purchased, NeedType need, string other)
        {
            int originalCoins = ResourceValues.getCoins();
            var owner = new GameObject("PurchasedBuildingTest");
            try
            {
                var runner = owner.AddComponent<SimulationRunner>();
                if (runner.WorkerAssignments == null)
                    typeof(SimulationRunner).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)
                        .Invoke(runner, null);

                Assert.IsTrue(runner.TryAddPurchasedBuilding(purchased));
                Assert.AreEqual("Worker3", runner.GetWorkerId(purchased));
                Assert.AreEqual(purchased, runner.GetBuildingIdForNeed(need));
                runner.StartNextWave();
                Assert.AreEqual(GameState.WaveInProgress, runner.CurrentState);
                Assert.IsFalse(runner.TryAddPurchasedBuilding(other));
                Assert.IsFalse(runner.WorkerAssignments.ContainsKey(other));
            }
            finally
            {
                Object.DestroyImmediate(owner);
                ResourceValues.addCoins(originalCoins - ResourceValues.getCoins());
            }
        }
    }
}

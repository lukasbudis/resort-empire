using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace ResortEmpire
{
    public class BuildingUpgradeTests
    {
        private static GameCycle CreateCycle(BuildingService service)
        {
            var simulation = new GameSimulation();
            simulation.RegisterBuildingService(NeedType.Food, service);
            simulation.RegisterBuildingService(NeedType.Relax, new BuildingService(Levels(), 1, 1f));
            var wave = new WavesConfig { wave_id = 1, needs = new NeedsConfig { food = 1 } };
            return new GameCycle(simulation, new[] { wave, wave }, 0.1f,
                new Dictionary<string, NeedType> { { "Restaurant", NeedType.Food }, { "Pool", NeedType.Relax } },
                new HashSet<string> { "Restaurant" });
        }

        private static LevelConfig[] Levels() => new[]
        {
            new LevelConfig { level = 2, capacity = 4 },
            new LevelConfig { level = 1, capacity = 2 },
            new LevelConfig { level = 3, capacity = 8 },
            // FR-24 caps upgrades at level 3 even if additional data is supplied.
            new LevelConfig { level = 4, capacity = 10 }
        };

        [Test]
        public void NewSeatsFillImmediatelyInFifoOrderAndKeepExistingServiceProgress()
        {
            var service = new BuildingService(Levels(), 1, 1f);
            var cycle = CreateCycle(service);
            service.AddArrivals(6);
            for (int i = 0; i < 4; i++)
                service.Step();

            Assert.IsTrue(cycle.TryIncreaseBuildingCapacity("Restaurant"));
            Assert.AreEqual(4, service.Capacity);
            Assert.AreEqual(4, service.ActiveCount);
            Assert.AreEqual(2, service.WaitingCount);
            Assert.AreEqual(4, service.ElapsedTicks);
            for (int slot = 0; slot < 4; slot++)
                Assert.AreEqual(slot + 1, service.GetActiveUnitId(slot));
            Assert.AreEqual(4, service.WaitingTimes[2].WaitingTicks);
            Assert.AreEqual(4, service.WaitingTimes[3].WaitingTicks);

            for (int i = 0; i < 6; i++)
                service.Step();
            Assert.AreEqual(2, service.TotalServed);
            Assert.AreEqual(5, service.GetActiveUnitId(0));
            Assert.AreEqual(6, service.GetActiveUnitId(1));
            Assert.AreEqual(3, service.GetActiveUnitId(2));
            Assert.AreEqual(4, service.GetActiveUnitId(3));
            for (int i = 0; i < 10; i++)
                service.Step();
            Assert.AreEqual(6, service.TotalServed);
            Assert.IsTrue(service.IsEmpty);
        }

        [Test]
        public void UpgradesFollowConfiguredLevelsAndRequireAwaitingWave()
        {
            var service = new BuildingService(Levels(), 2, 0.1f);
            var cycle = CreateCycle(service);
            Assert.IsFalse(cycle.TryIncreaseBuildingCapacity("Pool"));
            Assert.IsFalse(cycle.TryIncreaseBuildingCapacity(null));
            Assert.IsFalse(cycle.TryIncreaseBuildingCapacity("Unknown"));
            cycle.StartNextWave();
            Assert.IsFalse(cycle.TryIncreaseBuildingCapacity("Restaurant"));
            Assert.AreEqual(2, service.Level);
            Assert.AreEqual(4, service.Capacity);

            for (int i = 0; i < 20 && cycle.CurrentState == GameState.WaveInProgress; i++)
                cycle.UpdateStep();
            Assert.AreEqual(GameState.AwaitingWave, cycle.CurrentState);
            Assert.IsTrue(cycle.TryIncreaseBuildingCapacity("Restaurant"));
            Assert.IsFalse(cycle.TryIncreaseBuildingCapacity("Restaurant"));
            Assert.AreEqual(3, service.Level);
            Assert.AreEqual(8, service.Capacity);
        }

        [Test]
        public void FinalWaveCompletionDoesNotReenableUpgrades()
        {
            var service = new BuildingService(Levels(), 1, 0.1f);
            var cycle = CreateCycle(service);
            for (int wave = 0; wave < 2; wave++)
            {
                cycle.StartNextWave();
                for (int i = 0; i < 20 && cycle.CurrentState == GameState.WaveInProgress; i++)
                    cycle.UpdateStep();
            }
            Assert.AreEqual(GameState.AllWavesCompleted, cycle.CurrentState);
            Assert.IsFalse(cycle.TryIncreaseBuildingCapacity("Restaurant"));
            Assert.AreEqual(1, service.Level);
            Assert.AreEqual(2, service.Capacity);
        }

        [Test]
        public void RunnerUsesConfiguredSeatsAndUpgradesOnlyTheSelectedBuilding()
        {
            int originalCoins = ResourceValues.getCoins();
            var owner = new GameObject("BuildingUpgradeTest");
            try
            {
                var runner = owner.AddComponent<SimulationRunner>();
                if (runner.WorkerAssignments == null)
                    typeof(SimulationRunner).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)
                        .Invoke(runner, null);
                AssertSeats(runner, "Reception", 1, 3);
                Assert.IsTrue(runner.TryIncreaseBuildingCapacity("Reception"));
                AssertSeats(runner, "Reception", 2, 5);
                Assert.IsTrue(runner.TryIncreaseBuildingCapacity("Reception"));
                Assert.IsFalse(runner.TryIncreaseBuildingCapacity("Reception"));
                AssertSeats(runner, "Reception", 3, 7);
                AssertSeats(runner, "Restaurant", 1, 3);
            }
            finally
            {
                Object.DestroyImmediate(owner);
                ResourceValues.addCoins(originalCoins - ResourceValues.getCoins());
            }
        }

        private static void AssertSeats(SimulationRunner runner, string id, int expectedLevel, int expectedCapacity)
        {
            Assert.IsTrue(runner.TryGetBuildingSeats(id, out int level, out int capacity));
            Assert.AreEqual(expectedLevel, level);
            Assert.AreEqual(expectedCapacity, capacity);
        }
    }
}

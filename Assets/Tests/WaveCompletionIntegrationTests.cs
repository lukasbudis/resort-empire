using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace ResortEmpire
{
    [TestFixture]
    public class WaveCompletionIntegrationTests
    {
        private static GameSimulation CreateFoodSimulation(out BuildingService food, int capacity = 1, float serviceTime = 0.5f, float performance = 1.0f)
        {
            var simulation = new GameSimulation();
            food = new BuildingService(capacity, serviceTime, performance);
            simulation.RegisterBuildingService(NeedType.Food, food);
            return simulation;
        }

        private static void StartFoodWave(GameSimulation simulation, int demand)
        {
            simulation.StartWave(
                new Dictionary<NeedType, int> { { NeedType.Food, demand } },
                new[] { NeedType.Food },
                1f);
        }

        private static void RunToEndWithStrictIntegrityChecks(GameSimulation simulation, BuildingService service)
        {
            int ticks = 0;
            var wave = simulation.CurrentWave;
            Assert.IsNotNull(wave);

            while (ticks < 30000 && !simulation.AllNeedsSatisfied)
            {
                bool isWaveActive = (wave.Phase == WavePhase.Incoming) || 
                                    (service.WaitingCount > 0) || 
                                    (service.ActiveCount > 0);

                if (isWaveActive)
                {
                    Assert.IsFalse(simulation.AllNeedsSatisfied);
                }

                simulation.Step();
                ticks++;

                int currentTotalInBuilding = service.TotalServed + service.WaitingCount + service.ActiveCount;
                Assert.AreEqual(service.TotalArrived, currentTotalInBuilding);
                Assert.AreEqual(wave.GetArrivedCount(NeedType.Food), service.TotalArrived);
            }

            Assert.IsTrue(simulation.AllNeedsSatisfied);
            Assert.AreEqual(0, service.WaitingCount);
            Assert.AreEqual(0, service.ActiveCount);
        }

        [TestCase(1, 0.5f, 1.0f, 5)]
        [TestCase(2, 0.2f, 1.0f, 12)]
        [TestCase(3, 1.0f, 1.5f, 8)]
        public void WaveArrivalsAreFullyServedByBuildingsWithDynamicDemand(int capacity, float serviceTime, float performance, int demand)
        {
            GameSimulation simulation = CreateFoodSimulation(out BuildingService food, capacity, serviceTime, performance);
            StartFoodWave(simulation, demand);

            int expectedDemand = simulation.CurrentWave.TotalDemand[NeedType.Food];

            RunToEndWithStrictIntegrityChecks(simulation, food);

            Assert.AreEqual(expectedDemand, simulation.ServedThisWave[NeedType.Food]);
        }

        [Test]
        public void WaveWithExtremeSlowServiceFinishesWithinReasonableLimit()
        {
            int demand = 30;
            float serviceTime = 12.0f;
            int capacity = 1;
            float performance = 0.33f;

            GameSimulation simulation = CreateFoodSimulation(out BuildingService food, capacity, serviceTime, performance);
            StartFoodWave(simulation, demand);

            int expectedDemand = simulation.CurrentWave.TotalDemand[NeedType.Food];
            Assert.AreEqual(30, expectedDemand);

            RunToEndWithStrictIntegrityChecks(simulation, food);

            Assert.AreEqual(expectedDemand, simulation.ServedThisWave[NeedType.Food]);
            Assert.AreEqual(expectedDemand, food.TotalArrived);
            Assert.AreEqual(expectedDemand, food.TotalServed);
        }
    }
}
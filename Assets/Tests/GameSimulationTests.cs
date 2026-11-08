
using System.Collections.Generic;
using NUnit.Framework;

namespace ResortEmpire
{
    public class GameSimulationTests
    {
        [Test]
        public void Step_IncreasesTickCount()
        {
            GameSimulation simulation = new GameSimulation();

            simulation.Step();
            simulation.Step();
            simulation.Step();

            Assert.AreEqual(3, simulation.TickCount);
        }

        [Test]
        public void Pause_StopsSimulation()
        {
            GameSimulation simulation = new GameSimulation();

            simulation.Step();
            simulation.Pause();
            simulation.Step();

            Assert.AreEqual(1, simulation.TickCount);
            Assert.IsTrue(simulation.IsPaused);
        }

        [Test]
        public void Resume_ContinuesSimulation()
        {
            GameSimulation simulation = new GameSimulation();

            simulation.Step();
            simulation.Pause();
            simulation.Step();
            simulation.Resume();
            simulation.Step();

            Assert.AreEqual(2, simulation.TickCount);
            Assert.IsFalse(simulation.IsPaused);
        }

        // One fast seat: 1 s arrival, 0.5 s service per unit.
        private static GameSimulation CreateFoodSimulation(out BuildingService food)
        {
            var simulation = new GameSimulation();
            food = new BuildingService(1, 0.5f);
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

        private static void RunToEnd(GameSimulation simulation)
        {
            int ticks = 0;
            while (ticks < 10000 && !simulation.AllNeedsSatisfied)
            {
                simulation.Step();
                ticks++;
            }

            Assert.IsTrue(simulation.AllNeedsSatisfied, "The wave did not finish.");
        }

        [Test]
        public void ServedThisWave_AtWaveStart_IsZero()
        {
            GameSimulation simulation = CreateFoodSimulation(out _);

            StartFoodWave(simulation, 5);

            Assert.AreEqual(0, simulation.ServedThisWave[NeedType.Food]);
        }

        [Test]
        public void ServedThisWave_AtWaveEnd_EqualsDemand()
        {
            GameSimulation simulation = CreateFoodSimulation(out _);
            StartFoodWave(simulation, 5);

            RunToEnd(simulation);

            Assert.AreEqual(5, simulation.ServedThisWave[NeedType.Food]);
        }

        [Test]
        public void ServedThisWave_SecondWave_CountsOnlyItsOwnUnits()
        {
            GameSimulation simulation = CreateFoodSimulation(out BuildingService food);
            StartFoodWave(simulation, 5);
            RunToEnd(simulation);

            StartFoodWave(simulation, 3);

            Assert.AreEqual(0, simulation.ServedThisWave[NeedType.Food]);

            RunToEnd(simulation);

            Assert.AreEqual(3, simulation.ServedThisWave[NeedType.Food]);
            Assert.AreEqual(8, food.TotalServed);
        }

        [Test]
        public void NeedStates_ContainsOnlyNeedsOfCurrentWave()
        {
            GameSimulation simulation = CreateFoodSimulation(out _);
            simulation.RegisterBuildingService(NeedType.Rooms, new BuildingService(1, 0.5f));

            StartFoodWave(simulation, 2);

            Assert.AreEqual(1, simulation.NeedStates.Count);
            Assert.AreEqual(NeedState.Ok, simulation.NeedStates[NeedType.Food]);
            Assert.IsFalse(simulation.ServedThisWave.ContainsKey(NeedType.Rooms));
        }

        [Test]
        public void NeedStates_FollowsStateMachine()
        {
            GameSimulation simulation = CreateFoodSimulation(out _);
            StartFoodWave(simulation, 2);

            RunToEnd(simulation);

            Assert.AreEqual(NeedState.Satisfied, simulation.NeedStates[NeedType.Food]);
            Assert.AreEqual(simulation.GetNeedState(NeedType.Food), simulation.NeedStates[NeedType.Food]);
        }

        [Test]
        public void NeedStates_NextWave_ResetsToOk()
        {
            GameSimulation simulation = CreateFoodSimulation(out _);
            StartFoodWave(simulation, 2);
            RunToEnd(simulation);

            StartFoodWave(simulation, 2);

            Assert.AreEqual(NeedState.Ok, simulation.NeedStates[NeedType.Food]);
        }
    }
}

using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace ResortEmpire
{
    public class WaveSatisfactionCalculatorTests
    {
        private static global::WaveSettingsConfig CreateValidSettings()
        {
            return new global::WaveSettingsConfig
            {
                patience_grace = 10f,
                patience_floor_after = 30f,
                unit_floor = 25f,
                wave_floor = 40f,
                ocean_view_satisfaction_weight = 1.5f
            };
        }

        private static void Step(BuildingService service, int ticks)
        {
            for (int i = 0; i < ticks; i++)
                service.Step();
        }


        private static GameSimulation CreateSingleWaitSimulation(
            NeedType need,
            double waitSeconds,
            int demandCount = 1)
        {
            var simulation = new GameSimulation();

            if (waitSeconds <= 0d)
            {
                var service = new BuildingService(1, 1f);
                simulation.RegisterBuildingService(need, service);
                simulation.StartWave(
                    new Dictionary<NeedType, int> { { need, demandCount } },
                    new[] { need },
                    90f);

                if (demandCount > 0)
                    service.AddArrivals(1);
            }
            else
            {
                int ticks = (int)Math.Round(waitSeconds / 0.1d);
                var service = new BuildingService(1, (float)waitSeconds);

                // Blokujúci zákazník pred spustením vlny, aby sa jeho 0s čakanie nezarátalo do aktuálnej vlny
                service.AddArrivals(1);

                simulation.RegisterBuildingService(need, service);
                simulation.StartWave(
                    new Dictionary<NeedType, int> { { need, demandCount } },
                    new[] { need },
                    90f);

                if (demandCount > 0)
                {
                    service.AddArrivals(1);
                    Step(service, ticks);
                }
            }

            return simulation;
        }


        [Test]
        public void CalculatePercent_NullSimulation_ThrowsArgumentNullException()
        {
            var settings = CreateValidSettings();
            Assert.Throws<ArgumentNullException>(() =>
                WaveSatisfactionCalculator.CalculatePercent(null, settings));
        }

        [Test]
        public void CalculatePercent_WaveNotStarted_ThrowsInvalidOperationException()
        {
            var simulation = new GameSimulation();
            var settings = CreateValidSettings();

            Assert.Throws<InvalidOperationException>(() =>
                WaveSatisfactionCalculator.CalculatePercent(simulation, settings));
        }

        [Test]
        public void CalculatePercent_NullSettings_ThrowsArgumentNullException()
        {
            var simulation = CreateSingleWaitSimulation(NeedType.Rooms, 0d);

            Assert.Throws<ArgumentNullException>(() =>
                WaveSatisfactionCalculator.CalculatePercent(simulation, null));
        }

        [TestCase(-1f, 30f, 25f, 40f, 1.5f)] // patience_grace < 0
        [TestCase(10f, 10f, 25f, 40f, 1.5f)] // patience_floor_after == patience_grace
        [TestCase(15f, 10f, 25f, 40f, 1.5f)] // patience_floor_after < patience_grace
        [TestCase(10f, 30f, -5f, 40f, 1.5f)] // unit_floor < 0
        [TestCase(10f, 30f, 40f, 40f, 1.5f)] // unit_floor == wave_floor
        [TestCase(10f, 30f, 45f, 40f, 1.5f)] // unit_floor > wave_floor
        [TestCase(10f, 30f, 25f, 70f, 1.5f)] // wave_floor == 70
        [TestCase(10f, 30f, 25f, 85f, 1.5f)] // wave_floor > 70
        [TestCase(10f, 30f, 25f, 40f, 0f)]   // ocean_view_satisfaction_weight == 0
        [TestCase(10f, 30f, 25f, 40f, -1f)]  // ocean_view_satisfaction_weight < 0
        public void CalculatePercent_InvalidSettings_ThrowsArgumentException(
            float patienceGrace,
            float patienceFloorAfter,
            float unitFloor,
            float waveFloor,
            float oceanViewWeight)
        {
            var simulation = CreateSingleWaitSimulation(NeedType.Rooms, 0d);
            var invalidSettings = new global::WaveSettingsConfig
            {
                patience_grace = patienceGrace,
                patience_floor_after = patienceFloorAfter,
                unit_floor = unitFloor,
                wave_floor = waveFloor,
                ocean_view_satisfaction_weight = oceanViewWeight
            };

            Assert.Throws<ArgumentException>(() =>
                WaveSatisfactionCalculator.CalculatePercent(simulation, invalidSettings));
        }


        [TestCase(0.0, 100)]   // Pod patience_grace -> 100 %
        [TestCase(10.0, 100)]  // Presne na patience_grace -> 100 %
        [TestCase(15.0, 81)]   // 25 % poklesu: 100 + (-75 * 0.25) = 81.25 -> 81
        [TestCase(20.0, 63)]   // 50 % poklesu: 100 + (-75 * 0.50) = 62.50 -> 63 (Midpoint ToEven = 62, pozor: v C# Math.Round(62.5) je 62!)
        [TestCase(30.0, 40)]   // Na patience_floor_after -> unit_floor (25), ale ohraničené na wave_floor (40)
        [TestCase(50.0, 40)]   // Nad patience_floor_after -> unit_floor (25), ohraničené na wave_floor (40)
        public void CalculatePercent_SingleUnitWaitTimes_ReturnsExpectedScore(
            double waitSeconds,
            int expectedScore)
        {
            // Poznámka: V C# štandardný Math.Round(62.5) používa Banker's rounding (zaokrúhlenie na párne -> 62)
            if (Math.Abs(waitSeconds - 20.0) < 1e-6)
                expectedScore = (int)Math.Round(62.5d);

            var settings = CreateValidSettings();
            var simulation = CreateSingleWaitSimulation(NeedType.Rooms, waitSeconds);

            int actual = WaveSatisfactionCalculator.CalculatePercent(simulation, settings);
            Assert.AreEqual(expectedScore, actual);
        }

        [Test]
        public void CalculatePercent_OceanViewHasHigherWeightThanStandardNeeds()
        {
            var settings = CreateValidSettings(); // ocean_view_satisfaction_weight = 1.5
            var simulation = new GameSimulation();

            var roomsService = new BuildingService(1, 1f);
            simulation.RegisterBuildingService(NeedType.Rooms, roomsService);

            var oceanService = new BuildingService(1, 30f);
            oceanService.AddArrivals(1); // blokujúci zákazník
            simulation.RegisterBuildingService(NeedType.OceanView, oceanService);

            simulation.StartWave(
                new Dictionary<NeedType, int>
                {
                    { NeedType.Rooms, 1 },
                    { NeedType.OceanView, 1 }
                },
                new[] { NeedType.Rooms, NeedType.OceanView },
                90f);

            // Rooms: 0s -> 100 * 1.0 = 100
            // OceanView: 30s -> 25 * 1.5 = 37.5
            // Výsledok: 137.5 / 2.5 = 55
            roomsService.AddArrivals(1);
            oceanService.AddArrivals(1);
            Step(oceanService, 300);

            int actual = WaveSatisfactionCalculator.CalculatePercent(simulation, settings);
            Assert.AreEqual(55, actual);
        }

        [Test]
        public void CalculatePercent_IgnoresNeedsWithZeroDemandEvenIfWaitsExist()
        {
            var settings = CreateValidSettings();
            var simulation = new GameSimulation();

            var roomsService = new BuildingService(1, 1f);
            simulation.RegisterBuildingService(NeedType.Rooms, roomsService);

            var foodService = new BuildingService(1, 30f);
            foodService.AddArrivals(1);
            simulation.RegisterBuildingService(NeedType.Food, foodService);

            simulation.StartWave(
                new Dictionary<NeedType, int>
                {
                    { NeedType.Rooms, 1 },
                    { NeedType.Food, 0 } // Dopyt 0 -> musí sa preskočiť
                },
                new[] { NeedType.Rooms, NeedType.Food },
                90f);

            roomsService.AddArrivals(1);
            foodService.AddArrivals(1);
            Step(foodService, 300);

            // Ak by sa Food zarátalo, skóre by kleslo pod 100. Keďže má demand 0, ostáva 100.
            int actual = WaveSatisfactionCalculator.CalculatePercent(simulation, settings);
            Assert.AreEqual(100, actual);
        }

        [Test]
        public void CalculatePercent_NoUnitsSeatedYet_ReturnsWaveFloor()
        {
            var settings = CreateValidSettings();
            var simulation = new GameSimulation();

            simulation.RegisterBuildingService(NeedType.Rooms, new BuildingService(1, 1f));
            simulation.StartWave(
                new Dictionary<NeedType, int> { { NeedType.Rooms, 5 } },
                new[] { NeedType.Rooms },
                90f);

            // totalWeight == 0 -> vráti settings.wave_floor (40)
            int actual = WaveSatisfactionCalculator.CalculatePercent(simulation, settings);
            Assert.AreEqual(40, actual);
        }

        [Test]
        public void CalculatePercent_IgnoresWaitingTimesFromPreviousWave()
        {
            var settings = CreateValidSettings();
            var simulation = new GameSimulation();

            var roomsService = new BuildingService(1, 30f);
            roomsService.AddArrivals(1); // blokujúci pre 1. vlnu
            simulation.RegisterBuildingService(NeedType.Rooms, roomsService);

            // 1. vlna: zákazník čaká 30s (zlá spokojnosť)
            simulation.StartWave(new Dictionary<NeedType, int> { { NeedType.Rooms, 1 } }, new[] { NeedType.Rooms }, 90f);
            roomsService.AddArrivals(1);
            Step(roomsService, 300); // usadí sa po 30s
            Step(roomsService, 300); // dobehne jeho obsluha, budova je prázdna
            // 2. vlna: zákazník čaká 0s (100% spokojnosť)
            simulation.StartWave(
            new Dictionary<NeedType, int> { { NeedType.Rooms, 1 } },
            new[] { NeedType.Rooms },
            90f);
            roomsService.AddArrivals(1);
            int actual = WaveSatisfactionCalculator.CalculatePercent(simulation, settings);
            Assert.AreEqual(100, actual);
        }
    }
}
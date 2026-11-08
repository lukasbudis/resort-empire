
using NUnit.Framework;

namespace ResortEmpire
{
    public class ConfigurationTests
    {
        [TestCase("0")]
        [TestCase("-0.1")]
        [TestCase("1")]
        [TestCase("2")]
        public void InvalidUnstaffedPerformanceIsRejected(string value)
        {
            string json = "{\"workerConfig\":{\"unstaffedPerformanceMultiplier\":" +
                value + "},\"resourceConfig\":{}}";
            Assert.Throws<System.ArgumentOutOfRangeException>(() => UtilConfig.ParseConfig(json));
        }

        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(float.NegativeInfinity)]
        public void NonFiniteUnstaffedPerformanceIsRejected(float value)
        {
            var config = new WorkerConfig { unstaffedPerformanceMultiplier = value };
            Assert.Throws<System.ArgumentOutOfRangeException>(() => config.Validate());
        }

        [Test]
        public void EmptyConfigIsRejected()
        {
            Assert.Throws<System.ArgumentException>(() =>
                UtilConfig.ParseConfig(""));
        }

        [TestCase(0.33f, 31)]
        [TestCase(0.5f, 20)]
        public void ConfiguredWorkerPlacementControlsServiceSpeed(float performance, int unstaffedTicks)
        {
            string json = UnityEngine.Resources.Load<UnityEngine.TextAsset>("config").text;
            var config = UtilConfig.ParseConfig(json);
            config.workerConfig.unstaffedPerformanceMultiplier = performance;
            config = UtilConfig.ParseConfig(UnityEngine.JsonUtility.ToJson(config));
            var staffed = new BuildingService(1, 1f, UtilConfig.GetBuildingPerformanceMultiplier(config, "Reception"));
            var unstaffed = new BuildingService(1, 1f, UtilConfig.GetBuildingPerformanceMultiplier(config, "BeachBar"));
            staffed.AddArrivals(1);
            unstaffed.AddArrivals(1);
            for (int i = 0; i < 9; i++)
            {
                staffed.Step();
                unstaffed.Step();
            }
            Assert.AreEqual(0, staffed.TotalServed);
            staffed.Step();
            unstaffed.Step();
            Assert.AreEqual(1, staffed.TotalServed);
            Assert.AreEqual(0, unstaffed.TotalServed);
            for (int i = 10; i < unstaffedTicks - 1; i++)
                unstaffed.Step();
            Assert.AreEqual(0, unstaffed.TotalServed);
            unstaffed.Step();
            Assert.AreEqual(1, unstaffed.TotalServed);
            Assert.IsTrue(unstaffed.IsEmpty);
        }

        [Test]
        public void ProjectConfigDefinesBottleneckThreshold()
        {
            var config = UtilConfig.ParseConfig(
                UnityEngine.Resources.Load<UnityEngine.TextAsset>("config").text);
            Assert.AreEqual(4, config.needStateConfig.bottleneckThreshold);
        }

        [TestCase("\"needStateConfig\":{\"bottleneckThreshold\":-1},")]
        [TestCase("\"needStateConfig\":{\"bottleneckTreshold\":9},")]
        [TestCase("")]
        public void MissingOrNegativeBottleneckThresholdIsRejected(string needStateSection)
        {
            string json = "{\"workerConfig\":{\"unstaffedPerformanceMultiplier\":0.33}," +
                needStateSection + "\"resourceConfig\":{}}";
            var error = Assert.Throws<System.ArgumentOutOfRangeException>(() => UtilConfig.ParseConfig(json));
            Assert.AreEqual("bottleneckThreshold", error.ParamName);
        }

        [Test]
        public void ProjectConfigUsesDefaultUnstaffedPerformance()
        {
            var config = UtilConfig.ParseConfig(
                UnityEngine.Resources.Load<UnityEngine.TextAsset>("config").text);
            Assert.AreEqual(0.33f, config.workerConfig.unstaffedPerformanceMultiplier);
            Assert.Throws<System.ArgumentException>(() =>
                UtilConfig.GetBuildingPerformanceMultiplier(config, "UnknownBuilding"));
        }
        [Test]
        public void LoadConfiguration()
        {
            UtilConfig.loadConfig();

            int coins = UtilConfig.getCoins();
            int energy = UtilConfig.getCurrentEnergy();
            int workersCount =  UtilConfig.getWorkersCount();
            var workers = UtilConfig.getWorkers();
            int buildingsCount =  UtilConfig.getBuildingCount();
            var buildings = UtilConfig.getBuildings();

            Assert.AreNotEqual(-1, coins);
            Assert.AreNotEqual(-1, energy);
            Assert.AreNotEqual(-1, workersCount);
            Assert.AreNotEqual(null, workers);
            Assert.AreNotEqual(-1, buildingsCount);
            Assert.AreNotEqual(null, buildings);
        }
        
        [Test]
        public void ConfigContainsExactlyEightWavesWithNonNegativeVolumes()
        {
            UtilConfig.loadConfig();

            int wave_count = UtilConfig.getWavesCount();
            Assert.AreEqual(8, wave_count, "Konfigurácia neobsahuje presne 8 vĺn!");

            for (int i = 0; i < wave_count; i++)
            {
                var wave = UtilConfig.getWaveByIndex(i);

                Assert.IsNotNull(wave, $"Vlna na indexe {i} je null!");

                int checkedRooms = UtilConfig.getNeedVolumeForWave(wave.wave_id, "rooms");
                int checkedDrinks = UtilConfig.getNeedVolumeForWave(wave.wave_id, "drinks");
                int checkedFood = UtilConfig.getNeedVolumeForWave(wave.wave_id, "food");
                int checkedRelax = UtilConfig.getNeedVolumeForWave(wave.wave_id, "relax");
                int checkedOceanView = UtilConfig.getNeedVolumeForWave(wave.wave_id, "ocean_view");

                Assert.GreaterOrEqual(checkedRooms, 0, $"Vlna ID {wave.wave_id} má záporný objem pre 'rooms'!");
                Assert.GreaterOrEqual(checkedDrinks, 0, $"Vlna ID {wave.wave_id} má záporný objem pre 'drinks'!");
                Assert.GreaterOrEqual(checkedFood, 0, $"Vlna ID {wave.wave_id} má záporný objem pre 'food'!");
                Assert.GreaterOrEqual(checkedRelax, 0, $"Vlna ID {wave.wave_id} má záporný objem pre 'relax'!");
                Assert.GreaterOrEqual(checkedOceanView, 0, $"Vlna ID {wave.wave_id} má záporný objem pre 'ocean_view'!");
            }
        }
    }
}

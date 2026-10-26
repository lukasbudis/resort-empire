
using NUnit.Framework;

public class ConfigurationTests
{
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
}

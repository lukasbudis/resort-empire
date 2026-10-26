
using NUnit.Framework;

public class ConfigurationTests
{
    [Test]
    public void LoadConfiguration()
    {
        UtilConfig.loadConfig();

        int coins = UtilConfig.getCoins();
        int energy = UtilConfig.getCurrentEnergy();
        int maxWorkers =  UtilConfig.getMaxWorkers();

        Assert.AreNotEqual(-1, coins);
        Assert.AreNotEqual(-1, energy);
        Assert.AreNotEqual(-1, maxWorkers);
    }
}

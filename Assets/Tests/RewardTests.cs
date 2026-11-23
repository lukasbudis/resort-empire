
using NUnit.Framework;
using UnityEngine;

namespace ResortEmpire
{
    public class RewardTests
    {

        [Test]
        public void GetRewardCoinsAllWaves(
            [Values(1, 2, 3, 4, 5, 6, 7, 8)] int waveId,
            [Values(0, 39, 40, 55, 70, 80, 90, 100)] int score)
        {
            int guests = UtilConfig.getGuestsInWave(waveId);
            int coinsPerUnit = UtilConfig.getCoinsPerUnit();
            float multiplier = UtilConfig.getWaveMultiplier(score);
            int expectedCoins = Mathf.RoundToInt(guests * coinsPerUnit * multiplier);

            int actualCoins = RewardUtil.getRewardCoins(waveId, score);

            Assert.AreEqual(expectedCoins, actualCoins);
        }

        [TestCase(0, 80)]
        [TestCase(-1, 80)]
        [TestCase(9, 80)]
        [TestCase(999, 80)]
        public void GetRewardCoinsInvalidWaveId(int invalidWaveId, int validScore)
        {
            int result = RewardUtil.getRewardCoins(invalidWaveId, validScore);
            Assert.AreEqual(0, result);
        }

        [TestCase(1, -1)]
        [TestCase(1, -50)]
        [TestCase(1, 101)]
        [TestCase(1, 200)]
        public void GetRewardCoinsInvalidSatisfactionScore(int validWaveId, int invalidScore)
        {
            int result = RewardUtil.getRewardCoins(validWaveId, invalidScore);
            Assert.AreEqual(0, result);
        }
    }
}


using UnityEngine;

public static class RewardUtil
{

    public static int getRewardCoins(int waveId, int satisfactionScore)
    {

        int guests = UtilConfig.getGuestsInWave(waveId);
        if ( guests == -1 )
        {
            return 0;
        }

        float multiplier = UtilConfig.getWaveMultiplier(satisfactionScore);
        if ( multiplier == -1f )
        {
            return 0;
        }

        int coinsPerUnit = UtilConfig.getCoinsPerUnit();
        if ( coinsPerUnit == -1)
        {
            return 0;
        }

        return Mathf.RoundToInt(guests * coinsPerUnit * multiplier);
    }

}
using System;
using UnityEngine;
public static class ResourceValues
{
    private static int currentCoins = 0;
    public static event Action<int> OnCoinsChanged;

    public static void loadFromConfig()
    {
        currentCoins = UtilConfig.getCoins();
        OnCoinsChanged?.Invoke(currentCoins);
    }

    public static int getCoins()
    {
        return currentCoins;
    }

    public static void addCoins(int coins)
    {
        if ( coins == 0 ){
            return;
        }
        currentCoins = currentCoins + coins;
        OnCoinsChanged?.Invoke(currentCoins);
    }

    public static bool canSubtractsCoins(int coins)
    {
        return currentCoins >= coins;
    }

    public static bool subtractsCoins(int coins)
    {
        if (canSubtractsCoins(coins))
        {
            currentCoins = currentCoins - coins;
            OnCoinsChanged?.Invoke(currentCoins);
            return true;
        }
        return false;
    }
}
using System;
using System.IO;
using UnityEngine;

public static class UtilConfig
{
    private static MainConfig data;

    public static void loadConfig(){
        try 
        {
            TextAsset configFile = Resources.Load<TextAsset>("config");
            string content = configFile.text;

            data = JsonUtility.FromJson<MainConfig>(content);

            if (data == null || data.resourceConfig == null) 
            {
                throw new Exception("JSON je neplatný alebo mu chýbajú kľúčové sekcie (workerConfig/resourceConfig).");
            }

            Debug.Log("JSON bol úspešne načítaný:\n" + data.ToString());
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Config Error] Neočakávaná chyba pri načítaní/validácii: {ex.Message}");
        }
    }

    public static int getCurrentEnergy()
    {
        if (data == null) {
            return -1;
        }
        return data.resourceConfig.currentEnergy;
    }

    public static int getMaxEnergy()
    {
        if (data == null) {
            return -1;
        }
        return data.resourceConfig.maxEnergy;
    }
    public static int getCoins()
    {
        if (data == null) {
            return -1;
        }
        return data.resourceConfig.coins;
    }
    public static int getMaxWorkers()
    {
        if (data == null) {
            return -1;
        }
        return data.workerConfig.maxWorkers;
    }
}

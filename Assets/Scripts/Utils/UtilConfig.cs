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

    public static BuildingInitialState[] getBuildings()
    {
        if (data == null) {
            return null;
        }
        return data.buildings;
    }

    public static BuildingInitialState getBuildingByIndex(int i)
    {
        if (data == null || i > data.buildings.Length)
        {
            return null;
        }
        return data.buildings[i];
    }

    public static int getBuildingCount()
    {
        if (data == null) {
            return -1;
        }
        return data.buildings.Length;
    }

    public static WorkerInitialState[] getWorkers()
    {
        if (data == null) {
            return null;
        }
        return data.workers;
    }

    public static WorkerInitialState getWorkersByIndex(int i)
    {
        if (data == null || i > data.workers.Length)
        {
            return null;
        }
        return data.workers[i];
    }

    public static int getWorkersCount()
    {
        if (data == null) {
            return -1;
        }
        return data.workers.Length;
    }

    public static MainConfig getConfig()
    {
    return data;
    }
}

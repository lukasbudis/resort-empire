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

            data = ParseConfig(content);

            Debug.Log("JSON bol úspešne načítaný:\n" + data.ToString());
        }
        catch (Exception ex)
        {
            data = null;
            Debug.LogError($"[Config Error] Neočakávaná chyba pri načítaní/validácii: {ex.Message}");
        }
    }

    // Uses initial worker placement; live worker transfers belong to Chapter 2.
    public static float GetBuildingPerformanceMultiplier(
        MainConfig config, string buildingId)
    {
        if (config == null)
            throw new ArgumentNullException(nameof(config));
        if (config.workerConfig == null || config.workers == null || config.buildings == null)
            throw new ArgumentException("Configuration requires workers, buildings and workerConfig.", nameof(config));
        config.workerConfig.Validate();
        if (string.IsNullOrWhiteSpace(buildingId) ||
            !Array.Exists(config.buildings, building => building != null && building.id == buildingId))
            throw new ArgumentException("Building is not present in configuration.", nameof(buildingId));

        bool hasWorker = Array.Exists(config.workers,
            worker => worker != null && worker.buildingId == buildingId);
        return hasWorker ? 1f : config.workerConfig.unstaffedPerformanceMultiplier;
    }
    public static MainConfig ParseConfig(string content)
    {
        var config = JsonUtility.FromJson<MainConfig>(content);
        if (config == null || config.resourceConfig == null || config.workerConfig == null)
            throw new ArgumentException("Configuration requires workerConfig and resourceConfig.");
        config.workerConfig.Validate();
        if (config.needStateConfig == null)
            throw new ArgumentException("Configuration requires needStateConfig.");
        config.needStateConfig.Validate();
        return config;
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

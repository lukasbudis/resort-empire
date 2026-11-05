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

    public static BuildingsConfig[] getBuildings()
    {
        if (data == null) {
            return null;
        }
        return data.buildings;
    }

    public static BuildingsConfig getBuildingByIndex(int i)
    {
        if (data == null || i >= data.buildings.Length || i < 0)
        {
            return null;
        }
        return data.buildings[i];
    }

    public static BuildingsConfig getBuildingsById(string id)
    {
        if (data == null)
        {
            return null;
        }

        foreach (var building in data.buildings)
        {
            if (id.Equals(building.id))
            {
                return building;
            }
        }
        return null;
    }

    public static LevelConfig getBuildingLevelData(string buildingId, int level)
    {
        var building = getBuildingsById(buildingId);
        if (building == null || building.levels == null) return null;

        foreach (var blevel in building.levels)
        {
            if (blevel.level == level)
            {
                return blevel;
            }
        }
        return null;
    }

    public static int getBuildingCount()
    {
        if (data == null) {
            return -1;
        }
        return data.buildings.Length;
    }

    public static WavesConfig getWaveById(int waveId)
    {
        if (data == null || data.wave_settings == null || data.wave_settings.waves == null) 
            return null;
            
        foreach (var wave in data.wave_settings.waves)
        {
            if (wave.wave_id == waveId)
            {
                return wave;
            }
        }
        return null;
    }

    public static int getNeedVolumeForWave(int waveId, string needName)
    {
        var wave = getWaveById(waveId);
        if (wave == null || wave.needs == null) return 0;
        
        switch (needName.ToLower())
        {
            case "rooms": return wave.needs.rooms;
            case "drinks": return wave.needs.drinks;
            case "food": return wave.needs.food;
            case "relax": return wave.needs.relax;
            case "ocean_view": return wave.needs.ocean_view;
            default: return 0; // Ak sa zadá neznáma potreba
        }
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
        if (data == null || i >= data.workers.Length || i < 0)
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

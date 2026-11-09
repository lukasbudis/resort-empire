using UnityEngine;
using System;

public class UtilClasses
{
    
}

[Serializable]
public class WorkerConfig
{
    public int maxWorkers;
    public float unstaffedPerformanceMultiplier = 0.33f;

    public void Validate()
    {
        if (float.IsNaN(unstaffedPerformanceMultiplier) ||
            float.IsInfinity(unstaffedPerformanceMultiplier) ||
            unstaffedPerformanceMultiplier <= 0f ||
            unstaffedPerformanceMultiplier >= 1f)
            throw new ArgumentOutOfRangeException(nameof(unstaffedPerformanceMultiplier),
                "Unstaffed performance must be greater than zero and less than one.");
    }

    public override string ToString()
    {
        return $"WorkerConfig [maxWorkers: {maxWorkers}]";
    }
}

[Serializable]
public class NeedStateConfig
{
    // Waiting units (seated ones excluded) above which a need can enter BOTTLENECK (FR-09).
    // JsonUtility keeps this value when the key is missing, so -1 makes it mandatory.
    public int bottleneckThreshold = -1;

    public void Validate()
    {
        if (bottleneckThreshold < 0)
            throw new ArgumentOutOfRangeException(nameof(bottleneckThreshold),
                "needStateConfig.bottleneckThreshold must be set and cannot be negative.");
    }

    public override string ToString()
    {
        return $"NeedStateConfig [bottleneckThreshold: {bottleneckThreshold}]";
    }
}

[Serializable]
public class ResourceConfig
{
    public int coins;
    public int currentEnergy;

    public override string ToString()
    {
        return $"ResourceConfig [coins: {coins}, currentEnergy: {currentEnergy}]";
    }
}

[Serializable]
public class LevelConfig
{
    public int level;
    public int capacity;
}

[Serializable]
public class BuildingsConfig
{
    public string id;
    public string need;
    public float service_time;
    public LevelConfig[] levels;
}

[Serializable]
public class WorkerInitialState
{
    public string id;
    public string buildingId;
}

[Serializable]
public class NeedsConfig
{
    public int rooms;
    public int drinks;
    public int food;
    public int relax;
    public int ocean_view;

}

[Serializable]
public class WavesConfig
{
    public int wave_id;
    public int guests;
    public NeedsConfig needs;

}

[Serializable]
public class WaveSettingsConfig
{
    public float wave_timer;
    public WavesConfig[] waves;

}

[Serializable]
public class MainConfig
{
    public WorkerConfig workerConfig;
    public NeedStateConfig needStateConfig;
    public ResourceConfig resourceConfig;
    public BuildingsConfig[] buildings;
    public WorkerInitialState[] workers;
    public WaveSettingsConfig wave_settings;

    public override string ToString()
    {
        return $"MainConfig:\n  {workerConfig}\n  {needStateConfig}\n  {resourceConfig}";
    }
}

[Serializable]
public class FastModeBuildings
{
    public string id;
    public float service_time;
}

[Serializable]
public class FastModeWaveSettings
{
    public float wave_timer;
}

[Serializable]
public class FastModeConfig
{
    public FastModeBuildings[] buildings;
    public FastModeWaveSettings wave_settings;
}

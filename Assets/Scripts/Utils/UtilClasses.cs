using UnityEngine;
using System;

public class UtilClasses
{
    
}

[Serializable]
public class WorkerConfig
{
    public int maxWorkers;

    public override string ToString()
    {
        return $"WorkerConfig [maxWorkers: {maxWorkers}]";
    }
}

[Serializable]
public class ResourceConfig
{
    public int coins;
    public int maxEnergy;
    public int currentEnergy;

    public override string ToString()
    {
        return $"ResourceConfig [coins: {coins}, maxEnergy: {maxEnergy}, currentEnergy: {currentEnergy}]";
    }
}


[Serializable]
public class MainConfig
{
    public WorkerConfig workerConfig;
    public ResourceConfig resourceConfig;

    public override string ToString()
    {
        return $"MainConfig:\n  {workerConfig}\n  {resourceConfig}";
    }
}

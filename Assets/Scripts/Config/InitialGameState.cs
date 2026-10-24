using System;

[Serializable]
public class InitialGameState
{
    public int coins;
    public int energy;
    public int resortValue;
    public BuildingInitialState[] buildings;
    public WorkerInitialState[] workers;
}

[Serializable]
public class BuildingInitialState
{
    public string id;
    public int level;
}

[Serializable]
public class WorkerInitialState
{
    public string id;
    public string buildingId;
}
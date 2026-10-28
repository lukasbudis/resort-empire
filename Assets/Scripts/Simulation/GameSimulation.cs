
using System;
using System.Collections.Generic;

public class GameSimulation
{
    private readonly Dictionary<NeedType, BuildingService> buildingServices = new();

    public int TickCount { get; private set; } = 0;
    public bool IsPaused { get; private set; } = false;

    public WaveModel CurrentWave { get; private set; }

    public void RegisterBuildingService(NeedType need, BuildingService service)
    {
        if (service == null)
            throw new ArgumentNullException(nameof(service));

        buildingServices[need] = service;
    }

    public void StartWave(
        Dictionary<NeedType, int> demand,
        IEnumerable<NeedType> availableNeeds,
        float arrivalSeconds = 90f)
    {
        CurrentWave = new WaveModel(
            demand,
            availableNeeds,
            arrivalSeconds
        );

        CurrentWave.NeedArrived += OnNeedArrived;
    }

    private void OnNeedArrived(NeedType need, int count)
    {
        if (buildingServices.TryGetValue(need, out var service))
        {
            service.AddArrivals(count);
        }
    }

    public void Step()
    {
        if (IsPaused)
        {
            return;
        }

        TickCount++;

        CurrentWave?.Step();

        foreach (var service in buildingServices.Values)
        {
            service.Step();
        }
    }

    public void Pause()
    {
        IsPaused = true;
    }

    public void Resume()
    {
        IsPaused = false;
    }
}

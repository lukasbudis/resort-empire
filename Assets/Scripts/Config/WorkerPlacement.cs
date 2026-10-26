using System;
using UnityEngine;

public class WorkerPlacement : MonoBehaviour
{
    [SerializeField] private BuildingWorkerPoint[] buildingPoints;
    [SerializeField] private WorkerActor[] workers;

    public void Apply(MainConfig config)
    {
        if (config == null || config.workers == null)
        {
            Debug.LogError("Konfigurácia neobsahuje zoznam workerov.", this);
            return;
        }

        foreach (WorkerActor worker in workers)
        {
            if (worker != null)
                worker.gameObject.SetActive(false);
        }

        foreach (WorkerInitialState workerState in config.workers)
        {
            WorkerActor worker = Array.Find(
                workers,
                item => item != null && item.workerId == workerState.id);

            BuildingWorkerPoint building = Array.Find(
                buildingPoints,
                item => item != null && item.buildingId == workerState.buildingId);

            if (worker == null)
            {
                Debug.LogError($"Worker '{workerState.id}' sa nenašiel v scéne.", this);
                continue;
            }

            if (building == null || building.workerPoint == null)
            {
                Debug.LogError(
                    $"Miesto pre budovu '{workerState.buildingId}' sa nenašlo.",
                    this);
                continue;
            }

            worker.transform.position = building.workerPoint.position;
            worker.gameObject.SetActive(true);
        }
    }
}
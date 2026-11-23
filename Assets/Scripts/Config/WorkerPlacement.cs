using System;
using ResortEmpire;
using UnityEngine;

public class WorkerPlacement : MonoBehaviour
{
    [SerializeField] private SimulationRunner runner;
    [SerializeField] private BuildingWorkerPoint[] buildingPoints;
    [SerializeField] private WorkerActor[] workers;

    private void OnEnable()
    {
        if (runner != null)
            runner.WorkerMoved += OnWorkerMoved;
    }

    private void OnDisable()
    {
        if (runner != null)
            runner.WorkerMoved -= OnWorkerMoved;
    }

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
            if (workerState != null)
                PlaceWorker(workerState.id, workerState.buildingId);
        }
    }

    private void OnWorkerMoved(string workerId, string fromBuildingId, string toBuildingId)
    {
        PlaceWorker(workerId, toBuildingId);
    }

    private void PlaceWorker(string workerId, string buildingId)
    {
        WorkerActor worker = Array.Find(
            workers,
            item => item != null && item.workerId == workerId);

        BuildingWorkerPoint building = Array.Find(
            buildingPoints,
            item => item != null && item.buildingId == buildingId);

        if (worker == null)
        {
            Debug.LogError($"Worker '{workerId}' sa nenašiel v scéne.", this);
            return;
        }

        if (building == null || building.workerPoint == null)
        {
            Debug.LogError($"Miesto pre budovu '{buildingId}' sa nenašlo.", this);
            return;
        }

        worker.transform.position = building.workerPoint.position;
        worker.gameObject.SetActive(true);
    }
}
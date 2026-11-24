using ResortEmpire;
using UnityEngine;
using UnityEngine.EventSystems;

// Map entry point: building actions before a wave, staff allocation during a wave.
// Needs a Collider2D on this object and a Physics 2D Raycaster on the camera.
[RequireComponent(typeof(Collider2D))]
public class BuildingTapTarget : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private string buildingId;
    [SerializeField] private SimulationRunner runner;
    [SerializeField] private WorkerAllocationScreen allocationScreen;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (runner == null || allocationScreen == null)
        {
            Debug.LogError("BuildingTapTarget nemá priradený runner alebo S3.", this);
            return;
        }

        if (allocationScreen.IsOpen)
            return;

        switch (runner.CurrentState)
        {
            case GameState.AwaitingWave:
                allocationScreen.OpenBuildingActions(buildingId);
                break;
            case GameState.WaveInProgress:
                allocationScreen.Open(buildingId);
                break;
        }
    }
}

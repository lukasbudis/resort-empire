using ResortEmpire;
using UnityEngine;
using UnityEngine.EventSystems;

// Building on the map in S1: a tap opens S3 for it before the wave (FR-35).
// Needs a Collider2D on this object and a Physics 2D Raycaster on the camera.
// During the wave S3 is opened from S2, so the map does not react then.
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

        if (runner.CurrentState != GameState.AwaitingWave || allocationScreen.IsOpen)
            return;

        allocationScreen.Open(buildingId);
    }
}

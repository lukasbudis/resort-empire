
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class IslandCameraFitter : MonoBehaviour
{
    [SerializeField]
    private SpriteRenderer islandBackground;

    private Camera islandCamera;

    private void Start()
    {
        islandCamera = GetComponent<Camera>();

        FitCamera();
    }

    private void FitCamera()
    {
        if (islandBackground == null)
        {
            Debug.LogError("Island Background nie je priradeny!");
            return;
        }

        Bounds bounds = islandBackground.bounds;

        float halfHeight = bounds.extents.y;
        float halfWidth = bounds.extents.x;

        float aspectRatio = islandCamera.aspect;

        float requiredSize = Mathf.Max(
            halfHeight,
            halfWidth / aspectRatio
        );

        islandCamera.orthographicSize = requiredSize + 0.05f;
    }
}

using UnityEngine;
using UnityEngine.EventSystems;

public class CameraZoomController : MonoBehaviour
{
    [Header("Zoom")]
    [SerializeField] private float zoomSpeed = 10f;
    [SerializeField] private float minZoom = 20f;
    [SerializeField] private float maxZoom = 120f;

    private Camera targetCamera;
    private CameraBoundsController boundsController;

    private void Awake()
    {
        targetCamera = GetComponent<Camera>();

        if (targetCamera == null)
            targetCamera = Camera.main;

        boundsController = GetComponent<CameraBoundsController>();
        if (boundsController == null)
        {
            boundsController = gameObject.AddComponent<CameraBoundsController>();
        }
    }

    private void Update()
    {
        if (targetCamera == null)
            return;

        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Approximately(scroll, 0f))
            return;

        if (IsPointerOverUI())
            return;

        if (targetCamera.orthographic)
        {
            float nextZoom = targetCamera.orthographicSize - scroll * zoomSpeed;
            targetCamera.orthographicSize = Mathf.Clamp(nextZoom, minZoom, maxZoom);
            ClampCameraView();
            return;
        }

        ZoomPerspectiveByMovingCamera(scroll);
    }

    private void ZoomPerspectiveByMovingCamera(float scroll)
    {
        Vector3 currentPosition = boundsController != null ? boundsController.TargetPosition : transform.position;
        float minHeight = Mathf.Min(minZoom, maxZoom);
        float maxHeight = Mathf.Max(minZoom, maxZoom);
        float targetHeight = Mathf.Clamp(currentPosition.y - scroll * zoomSpeed, minHeight, maxHeight);
        float heightDelta = targetHeight - currentPosition.y;

        if (Mathf.Approximately(heightDelta, 0f))
            return;

        Vector3 zoomDirection = transform.forward;
        if (Mathf.Abs(zoomDirection.y) < 0.0001f)
        {
            zoomDirection = heightDelta < 0f ? Vector3.down : Vector3.up;
        }

        Vector3 worldDelta = zoomDirection * (heightDelta / zoomDirection.y);
        if (boundsController != null)
        {
            boundsController.Move(worldDelta);
        }
        else
        {
            transform.position += worldDelta;
        }
    }

    private static bool IsPointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    private void ClampCameraView()
    {
        if (boundsController != null)
        {
            boundsController.ClampCurrentView();
        }
    }
}

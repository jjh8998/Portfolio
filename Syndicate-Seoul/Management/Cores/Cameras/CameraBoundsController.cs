using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public class CameraBoundsController : MonoBehaviour
{
    [Header("Bounds")]
    [SerializeField] private bool constrainToWaterPlane = true;
    [SerializeField] private string waterPlaneObjectName = "HexTerrainChunk_WaterPlane";
    [SerializeField] private Renderer boundsRenderer;
    [SerializeField] private float boundsPadding = 1f;

    [Header("Motion")]
    [SerializeField] private bool smoothMovement = true;
    [SerializeField] private float smoothTime = 0.16f;
    [SerializeField] private float maxSmoothSpeed = 1000f;

    private Camera targetCamera;
    private Vector3 targetPosition;
    private Vector3 smoothVelocity;
    private bool initialized;

    public Vector3 TargetPosition => initialized ? targetPosition : transform.position;

    private void Awake()
    {
        targetCamera = GetComponent<Camera>();
        ResetTargetToCurrentPosition();
    }

    private void OnEnable()
    {
        ResetTargetToCurrentPosition();
    }

    private void LateUpdate()
    {
        if (!initialized)
        {
            ResetTargetToCurrentPosition();
        }

        targetPosition = ClampPositionToWaterBounds(targetPosition);

        if (!smoothMovement || smoothTime <= 0f)
        {
            transform.position = targetPosition;
            smoothVelocity = Vector3.zero;
            return;
        }

        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPosition,
            ref smoothVelocity,
            smoothTime,
            maxSmoothSpeed,
            Time.deltaTime);
    }

    public void Move(Vector3 worldDelta)
    {
        if (!initialized)
        {
            ResetTargetToCurrentPosition();
        }

        targetPosition = ClampPositionToWaterBounds(targetPosition + worldDelta);
    }

    public void SetTargetPosition(Vector3 position, bool snapImmediately = false)
    {
        targetPosition = ClampPositionToWaterBounds(position);
        initialized = true;

        if (snapImmediately)
        {
            transform.position = targetPosition;
            smoothVelocity = Vector3.zero;
        }
    }

    public void ClampCurrentView()
    {
        SetTargetPosition(transform.position, true);
    }

    private void ResetTargetToCurrentPosition()
    {
        targetPosition = transform.position;
        smoothVelocity = Vector3.zero;
        initialized = true;
    }

    private Vector3 ClampPositionToWaterBounds(Vector3 candidate)
    {
        if (!constrainToWaterPlane || targetCamera == null)
        {
            return candidate;
        }

        if (!TryGetWaterBounds(out Bounds waterBounds))
        {
            return candidate;
        }

        if (!TryGetViewportFootprintOffsets(candidate, waterBounds.center.y, out Vector2 minOffset, out Vector2 maxOffset))
        {
            return candidate;
        }

        float minX = waterBounds.min.x + boundsPadding - minOffset.x;
        float maxX = waterBounds.max.x - boundsPadding - maxOffset.x;
        float minZ = waterBounds.min.z + boundsPadding - minOffset.y;
        float maxZ = waterBounds.max.z - boundsPadding - maxOffset.y;

        if (minX > maxX)
        {
            candidate.x = waterBounds.center.x - (minOffset.x + maxOffset.x) * 0.5f;
        }
        else
        {
            candidate.x = Mathf.Clamp(candidate.x, minX, maxX);
        }

        if (minZ > maxZ)
        {
            candidate.z = waterBounds.center.z - (minOffset.y + maxOffset.y) * 0.5f;
        }
        else
        {
            candidate.z = Mathf.Clamp(candidate.z, minZ, maxZ);
        }

        return candidate;
    }

    private bool TryGetWaterBounds(out Bounds bounds)
    {
        if (boundsRenderer == null)
        {
            GameObject waterPlane = GameObject.Find(waterPlaneObjectName);
            if (waterPlane != null)
            {
                boundsRenderer = waterPlane.GetComponent<Renderer>();
            }
        }

        if (boundsRenderer == null)
        {
            bounds = default;
            return false;
        }

        bounds = boundsRenderer.bounds;
        return bounds.size.x > 0.001f && bounds.size.z > 0.001f;
    }

    private bool TryGetViewportFootprintOffsets(Vector3 cameraPosition, float planeY, out Vector2 minOffset, out Vector2 maxOffset)
    {
        Vector3 originalPosition = transform.position;
        transform.position = cameraPosition;

        bool hasFootprint = TryProjectViewportPoint(new Vector3(0f, 0f, 0f), planeY, cameraPosition, out Vector2 first);
        minOffset = first;
        maxOffset = first;

        if (hasFootprint)
        {
            AccumulateViewportPoint(new Vector3(0f, 1f, 0f), planeY, cameraPosition, ref minOffset, ref maxOffset);
            AccumulateViewportPoint(new Vector3(1f, 0f, 0f), planeY, cameraPosition, ref minOffset, ref maxOffset);
            AccumulateViewportPoint(new Vector3(1f, 1f, 0f), planeY, cameraPosition, ref minOffset, ref maxOffset);
        }

        transform.position = originalPosition;
        return hasFootprint;
    }

    private void AccumulateViewportPoint(Vector3 viewportPoint, float planeY, Vector3 cameraPosition, ref Vector2 minOffset, ref Vector2 maxOffset)
    {
        if (!TryProjectViewportPoint(viewportPoint, planeY, cameraPosition, out Vector2 offset))
        {
            return;
        }

        minOffset = Vector2.Min(minOffset, offset);
        maxOffset = Vector2.Max(maxOffset, offset);
    }

    private bool TryProjectViewportPoint(Vector3 viewportPoint, float planeY, Vector3 cameraPosition, out Vector2 offset)
    {
        Ray ray = targetCamera.ViewportPointToRay(viewportPoint);
        Plane waterPlane = new Plane(Vector3.up, new Vector3(0f, planeY, 0f));
        if (!waterPlane.Raycast(ray, out float distance))
        {
            offset = default;
            return false;
        }

        Vector3 worldPoint = ray.GetPoint(distance);
        offset = new Vector2(worldPoint.x - cameraPosition.x, worldPoint.z - cameraPosition.z);
        return true;
    }
}

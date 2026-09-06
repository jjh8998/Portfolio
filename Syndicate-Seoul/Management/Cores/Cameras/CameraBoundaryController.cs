using UnityEngine;

public class CameraBoundaryController : MonoBehaviour
{
    [SerializeField] private bool useBoundary;
    [SerializeField] private Vector2 minBoundary;
    [SerializeField] private Vector2 maxBoundary;

    private void LateUpdate()
    {
        if (!useBoundary)
            return;

        Vector3 position = transform.position;
        position.x = Mathf.Clamp(position.x, minBoundary.x, maxBoundary.x);
        position.z = Mathf.Clamp(position.z, minBoundary.y, maxBoundary.y);
        transform.position = position;
    }
}

using UnityEngine;
using UnityEngine.EventSystems;

public class CameraDragController : MonoBehaviour
{
    [SerializeField] private float dragSpeed = 0.05f;

    private bool isDragging;
    private Vector3 lastMousePosition;
    private CameraBoundsController boundsController;

    private void Awake()
    {
        boundsController = GetComponent<CameraBoundsController>();
        if (boundsController == null)
        {
            boundsController = gameObject.AddComponent<CameraBoundsController>();
        }
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (IsPointerOverUI())
                return;

            isDragging = true;
            lastMousePosition = Input.mousePosition;
        }

        if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
        }

        if (!isDragging || !Input.GetMouseButton(0))
            return;

        Vector3 currentMousePosition = Input.mousePosition;
        Vector3 mouseDelta = currentMousePosition - lastMousePosition;
        Vector3 move = new Vector3(-mouseDelta.x, 0f, -mouseDelta.y) * dragSpeed;

        if (boundsController != null)
        {
            boundsController.Move(move);
        }
        else
        {
            transform.position += move;
        }

        lastMousePosition = currentMousePosition;
    }

    private bool IsPointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }
}

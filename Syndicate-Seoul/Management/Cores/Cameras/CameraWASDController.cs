using UnityEngine;

public class CameraWASDController : MonoBehaviour
{
    [Header("Move")]
    [SerializeField] float moveSpeed = 12f;
    [SerializeField] float boostMultiplier = 2f;

    private CameraBoundsController boundsController;

    private void Awake()
    {
        boundsController = GetComponent<CameraBoundsController>();
        if (boundsController == null)
        {
            boundsController = gameObject.AddComponent<CameraBoundsController>();
        }
    }

    void Update()
    {
        // 입력 (WASD)
        float x = 0f;
        float z = 0f;

        if (Input.GetKey(KeyCode.A)) x -= 1f;
        if (Input.GetKey(KeyCode.D)) x += 1f;
        if (Input.GetKey(KeyCode.S)) z -= 1f;
        if (Input.GetKey(KeyCode.W)) z += 1f;

        Vector3 input = new Vector3(x, 0f, z);
        if (input.sqrMagnitude < 0.0001f) return;

        input.Normalize();

        // 카메라 기준 방향(수평 평면)
        Vector3 forward = transform.forward;
        forward.y = 0f;
        forward.Normalize();

        Vector3 right = transform.right;
        right.y = 0f;
        right.Normalize();

        Vector3 dir = right * input.x + forward * input.z;

        float speed = moveSpeed;
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            speed *= boostMultiplier;

        Vector3 delta = dir * (speed * Time.deltaTime);
        if (boundsController != null)
        {
            boundsController.Move(delta);
        }
        else
        {
            transform.position += delta;
        }
    }
}

using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Positioning")]
    public Vector3 offset = new Vector3(0f, 2f, -4f);
    public Vector3 lockOnOffset = new Vector3(1f, 1.5f, -3f); // Offset when locked on

    [Header("Rotation")]
    public float mouseSensitivity = 2f;
    public float minPitch = -30f;
    public float maxPitch = 60f;

    [Header("Smoothing")]
    public float positionSmoothSpeed = 10f;
    public float lockOnRotationSpeed = 5f;

    [HideInInspector] public float yaw;
    [HideInInspector] public float pitch;

    private Transform lockOnTarget;
    private bool isLockedOn = false;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (target != null)
        {
            Vector3 angles = transform.eulerAngles;
            yaw = angles.y;
            pitch = angles.x;

            if (pitch > 180f) pitch -= 360f;
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        if (isLockedOn && lockOnTarget != null)
        {
            UpdateLockedCamera();
        }
        else
        {
            UpdateFreeCamera();
        }
    }

    void UpdateFreeCamera()
    {
        // Get mouse input (no Time.deltaTime for mouse!)
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        yaw += mouseX;
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 desiredPosition = target.position + rotation * offset;

        float smoothFactor = 1f - Mathf.Pow(0.5f, positionSmoothSpeed * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothFactor);

        transform.rotation = rotation;
    }

    void UpdateLockedCamera()
    {
        // Calculate direction from player to locked target
        Vector3 directionToTarget = (lockOnTarget.position - target.position).normalized;
        
        // Allow horizontal rotation around locked target
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * 0.5f;
        yaw += mouseX;
        
        // Calculate desired rotation (look at target with some manual control)
        Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
        float targetYaw = targetRotation.eulerAngles.y;
        
        // Blend between locked rotation and manual yaw
        float blendedYaw = Mathf.LerpAngle(yaw, targetYaw, lockOnRotationSpeed * Time.deltaTime);
        yaw = blendedYaw;
        
        Quaternion rotation = Quaternion.Euler(0f, yaw, 0f);
        Vector3 desiredPosition = target.position + rotation * lockOnOffset;

        float smoothFactor = 1f - Mathf.Pow(0.5f, positionSmoothSpeed * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothFactor);

        // Look at locked target
        transform.LookAt(lockOnTarget.position);
    }

    public void SetLockOnTarget(Transform target)
    {
        lockOnTarget = target;
        isLockedOn = (target != null);
    }
}

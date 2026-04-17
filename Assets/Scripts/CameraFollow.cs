using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Positioning")]
    public Vector3 offset = new Vector3(0f, 2f, -4f);
    public Vector3 lockOnOffset = new Vector3(1f, 0.8f, -3f);

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
        // Direction from player to enemy (horizontal only)
        Vector3 directionToEnemy = lockOnTarget.position - target.position;
        directionToEnemy.y = 0;
        
        float targetAngle = Mathf.Atan2(directionToEnemy.x, directionToEnemy.z) * Mathf.Rad2Deg;
        
        // Smoothly interpolate only the yaw — no tilt, no roll
        yaw = Mathf.LerpAngle(yaw, targetAngle, lockOnRotationSpeed * Time.deltaTime);
        float fixedPitch = 10f; // constant downward angle, zero tilt
        
        // Set rotation directly — pitch and roll are always locked
        transform.rotation = Quaternion.Euler(fixedPitch, yaw, 0f);
        
        // Position camera behind the player based on smoothed yaw
        Quaternion facingRotation = Quaternion.Euler(0, yaw, 0);
        Vector3 cameraOffset = facingRotation * lockOnOffset;
        Vector3 desiredPosition = target.position + cameraOffset;
        
        // Smooth camera position
        float smoothFactor = 1f - Mathf.Pow(0.5f, positionSmoothSpeed * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothFactor);
        
        pitch = fixedPitch;
    }

    public void SetLockOnTarget(Transform target)
    {
        lockOnTarget = target;
        isLockedOn = (target != null);
        
        if (isLockedOn && target != null)
        {
            // Initialize yaw to face the enemy when locking on
            Vector3 directionToEnemy = target.position - this.target.position;
            float targetAngle = Mathf.Atan2(directionToEnemy.x, directionToEnemy.z) * Mathf.Rad2Deg;
            yaw = targetAngle;
        }
    }
}

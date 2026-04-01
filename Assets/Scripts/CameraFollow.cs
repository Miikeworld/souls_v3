using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Positioning")]
    public Vector3 offset = new Vector3(0f, 2f, -4f);
    public Vector3 lockOnOffset = new Vector3(1f, 1.5f, -3f);

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
        // Calculate direction from player to enemy
        Vector3 directionToEnemy = lockOnTarget.position - target.position;
        directionToEnemy.y = 0; // Keep horizontal for direction
        
        // Calculate the angle player should face
        float targetAngle = Mathf.Atan2(directionToEnemy.x, directionToEnemy.z) * Mathf.Rad2Deg;
        Quaternion targetRotation = Quaternion.Euler(0, targetAngle, 0);
        
        // Smoothly rotate player to face enemy (optional - remove if you want manual control)
        // target.rotation = Quaternion.Slerp(target.rotation, targetRotation, 5f * Time.deltaTime);
        
        // Calculate camera position relative to player's intended facing direction
        Vector3 cameraOffset = targetRotation * lockOnOffset;
        Vector3 desiredPosition = target.position + cameraOffset;
        
        // Allow some manual orbit adjustment with mouse
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * 0.5f;
        yaw += mouseX;
        
        // Apply mouse rotation to the camera offset
        Quaternion mouseRotation = Quaternion.Euler(0, yaw - targetAngle, 0);
        cameraOffset = mouseRotation * lockOnOffset;
        desiredPosition = target.position + cameraOffset;
        
        // Smooth camera movement
        float smoothFactor = 1f - Mathf.Pow(0.5f, positionSmoothSpeed * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothFactor);
        
        // Camera should look at the enemy, not the player
        Vector3 lookAtPosition = lockOnTarget.position + Vector3.up * 1f; // Look at enemy's chest/head
        transform.LookAt(lookAtPosition);
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

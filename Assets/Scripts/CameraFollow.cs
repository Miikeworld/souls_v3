using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Positioning")]
    public Vector3 offset = new Vector3(0f, 2f, -4f);

    [Header("Rotation")]
    public float mouseSensitivity = 2f;  // Much lower value now
    public float minPitch = -30f;
    public float maxPitch = 60f;

    [Header("Smoothing")]
    public float positionSmoothSpeed = 10f;

    [HideInInspector] public float yaw;
    [HideInInspector] public float pitch;

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

        // Get RAW mouse input (no Unity smoothing) - DO NOT multiply by Time.deltaTime!
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // Update yaw and pitch directly
        yaw += mouseX;
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        // Calculate rotation - instant, no smoothing
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);

        // Calculate desired position
        Vector3 desiredPosition = target.position + rotation * offset;

        // Use exponential smoothing formula (proper way to use Lerp)
        float smoothFactor = 1f - Mathf.Pow(0.5f, positionSmoothSpeed * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothFactor);

        // Set rotation directly
        transform.rotation = rotation;
    }
}

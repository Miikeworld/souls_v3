using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform target;
    
    [Header("Positioning")]
    public Vector3 offset = new Vector3(0f, 2f, -4f);
    
    [Header("Rotation")]
    public float rotationSpeed = 100f;
    public float minPitch = -30f;
    public float maxPitch = 60f;
    
    [Header("Smoothing")]
    public float followSmooth = 8f;
    
    private float yaw;
    private float pitch;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void LateUpdate()
    {
        if (!target) return;

        // Mouse input
        float mouseX = Input.GetAxis("Mouse X") * rotationSpeed * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * rotationSpeed * Time.deltaTime;

        yaw += mouseX;
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        // Calculate camera position
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 desiredPos = target.position + rotation * offset;
        transform.position = Vector3.Lerp(transform.position, desiredPos, followSmooth * Time.deltaTime);
        
        // Look at player head
        transform.LookAt(target.position + Vector3.up * 1.5f);
    }
}

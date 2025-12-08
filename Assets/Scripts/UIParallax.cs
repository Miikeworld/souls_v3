using UnityEngine;

public class UIParallax : MonoBehaviour
{
    [Header("References")]
    public Transform cameraTransform;
    
    [Header("=== PARALLAX EFFECT ===")]
    public bool enableParallax = true;
    [Range(0f, 1f)]
    public float parallaxStrength = 0.1f;
    public bool reactToPosition = true;
    public bool reactToRotation = true;
    public float parallaxSmoothSpeed = 5f;
    
    [Header("=== FLOAT EFFECT ===")]
    public bool enableFloat = false;
    public float floatSpeed = 1f;
    public float floatAmountY = 3f;
    public float floatAmountX = 0f;
    
    [Header("=== BREATHING SCALE ===")]
    public bool enableBreathing = false;
    public float breathSpeed = 2f;
    public float breathAmount = 0.015f;
    
    [Header("=== TILT EFFECT ===")]
    public bool enableTilt = false;
    [Range(0f, 15f)]
    public float maxTiltAngle = 3f;
    public float tiltSpeed = 3f;
    
    // Private variables
    private RectTransform rectTransform;
    private Vector3 previousCameraPosition;
    private Vector3 previousCameraRotation;
    private Vector2 initialPosition;
    private Vector2 parallaxTargetPosition;
    private Vector3 initialScale;
    private float randomOffset;
    
    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        
        if (cameraTransform == null)
        {
            cameraTransform = Camera.main.transform;
        }
        
        if (cameraTransform != null)
        {
            previousCameraPosition = cameraTransform.position;
            previousCameraRotation = cameraTransform.eulerAngles;
        }
        
        initialPosition = rectTransform.anchoredPosition;
        parallaxTargetPosition = initialPosition;
        initialScale = rectTransform.localScale;
        
        // Random offset so elements don't sync perfectly
        randomOffset = Random.Range(0f, Mathf.PI * 2f);
    }
    
    void LateUpdate()
    {
        if (cameraTransform == null || rectTransform == null) return;
        
        // Apply effects
        UpdateParallax();
        UpdateFloat();
        UpdateBreathing();
        UpdateTilt();
    }
    
    void UpdateParallax()
    {
        if (!enableParallax) return;
        
        Vector2 parallaxOffset = Vector2.zero;
        
        // Position-based parallax (camera movement)
        if (reactToPosition)
        {
            Vector3 cameraDelta = cameraTransform.position - previousCameraPosition;
            
            // Apply parallax (opposite direction for depth effect)
            parallaxOffset.x += -cameraDelta.x * parallaxStrength * 100f;
            parallaxOffset.y += -cameraDelta.y * parallaxStrength * 100f;
        }
        
        // Rotation-based parallax (camera look)
        if (reactToRotation)
        {
            Vector3 rotationDelta = cameraTransform.eulerAngles - previousCameraRotation;
            
            // Normalize rotation delta (-180 to 180)
            if (rotationDelta.y > 180f) rotationDelta.y -= 360f;
            if (rotationDelta.y < -180f) rotationDelta.y += 360f;
            if (rotationDelta.x > 180f) rotationDelta.x -= 360f;
            if (rotationDelta.x < -180f) rotationDelta.x += 360f;
            
            // Apply rotation parallax
            parallaxOffset.x += rotationDelta.y * parallaxStrength * 2f;
            parallaxOffset.y += -rotationDelta.x * parallaxStrength * 2f;
        }
        
        // Calculate target position
        parallaxTargetPosition = initialPosition + parallaxOffset;
        
        // Smoothly move to target
        rectTransform.anchoredPosition = Vector2.Lerp(
            rectTransform.anchoredPosition,
            parallaxTargetPosition,
            parallaxSmoothSpeed * Time.deltaTime
        );
        
        // Update previous values
        previousCameraPosition = cameraTransform.position;
        previousCameraRotation = cameraTransform.eulerAngles;
    }
    
    void UpdateFloat()
    {
        if (!enableFloat) return;
        
        // Floating motion (smooth sine wave)
        float floatY = Mathf.Sin((Time.time * floatSpeed) + randomOffset) * floatAmountY;
        float floatX = Mathf.Sin((Time.time * floatSpeed * 0.5f) + randomOffset) * floatAmountX;
        
        // Add float offset to current position
        Vector2 floatOffset = new Vector2(floatX, floatY);
        rectTransform.anchoredPosition = (enableParallax ? rectTransform.anchoredPosition : initialPosition) + floatOffset;
    }
    
    void UpdateBreathing()
    {
        if (!enableBreathing) return;
        
        // Breathing scale effect
        float breath = Mathf.Sin((Time.time * breathSpeed) + randomOffset) * breathAmount;
        float scale = 1f + breath;
        rectTransform.localScale = initialScale * scale;
    }
    
    void UpdateTilt()
    {
        if (!enableTilt) return;
        
        // Get camera rotation
        float cameraYaw = cameraTransform.eulerAngles.y;
        
        // Normalize to -180 to 180
        if (cameraYaw > 180f) cameraYaw -= 360f;
        
        // Calculate tilt (opposite direction for depth)
        float targetTilt = -cameraYaw * (maxTiltAngle / 180f);
        
        // Smoothly apply tilt
        Vector3 currentRotation = rectTransform.localEulerAngles;
        if (currentRotation.z > 180f) currentRotation.z -= 360f;
        
        currentRotation.z = Mathf.LerpAngle(currentRotation.z, targetTilt, tiltSpeed * Time.deltaTime);
        rectTransform.localEulerAngles = currentRotation;
    }
    
    // Optional: Reset to initial state
    public void ResetPosition()
    {
        rectTransform.anchoredPosition = initialPosition;
        rectTransform.localScale = initialScale;
        rectTransform.localEulerAngles = Vector3.zero;
    }
}

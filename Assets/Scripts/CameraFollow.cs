using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Free Camera")]
    public Vector3 offset = new Vector3(0f, 2f, -4f);
    public float mouseSensitivity = 2f;
    public float minPitch = -20f;
    public float maxPitch = 60f;
    public float positionSmoothSpeed = 10f;

    [Header("Lock-On Camera")]
    public float lockOnDistance = 5f;
    public float lockOnHeight = 1.2f;
    public float lockOnPitch = 10f;
    public float lockOnYawSpeed = 3f;
    public float lockOnPositionDamp = 0.15f;
    public float lockOnLookDamp = 0.08f;

    [Header("Collision")]
    public float collisionRadius = 0.2f;
    public LayerMask collisionLayers = ~0;
    public LayerMask lockOnCollisionLayers; // Set this to Environment layers only

    [HideInInspector] public float yaw;
    [HideInInspector] public float pitch;

    private Transform lockOnTarget;
    private bool isLockedOn = false;

    // Camera shake
    private float shakeTimer = 0f;
    private float shakeMagnitude = 0f;
    private float shakeDecay = 1f;

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

    private float maxCameraDistance = 20f;

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

        // Safety clamp — never let camera drift too far from player
        ClampCameraDistance();

        ApplyShake();
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

        // Prevent clipping through walls
        transform.position = HandleCameraCollision(target.position + Vector3.up * 1f, transform.position);

        transform.rotation = rotation;
    }

    void UpdateLockedCamera()
    {
        if (lockOnTarget == null) return;

        // ── 1. Position: behind player relative to enemy ──
        // Camera behind player, based on direction to enemy
        Vector3 toEnemy = lockOnTarget.position - target.position;
        toEnemy.y = 0f;
        Vector3 desiredPos = transform.position;

        if (toEnemy.sqrMagnitude > 0.01f)
        {
            Vector3 toEnemyDir = toEnemy.normalized;
            Vector3 behindOffset = -toEnemyDir * lockOnDistance + Vector3.up * lockOnHeight;
            desiredPos = target.position + behindOffset;
        }

        // Smooth position follow (faster for tighter tracking)
        float smoothFactor = 1f - Mathf.Pow(0.5f, 50f * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, desiredPos, smoothFactor);

        // Simple collision check - use lockOn layers when locked on
        Vector3 pivot = target.position + Vector3.up * 1.2f;
        LayerMask layersToUse = isLockedOn && lockOnCollisionLayers != 0 ? lockOnCollisionLayers : collisionLayers;
        transform.position = HandleCameraCollisionWithLayers(pivot, transform.position, layersToUse);

        // ── 2. Rotation: look at enemy directly ──
        Vector3 enemyCenter = lockOnTarget.position + Vector3.up * 1.0f;
        Vector3 toLook = enemyCenter - transform.position;
        if (toLook.sqrMagnitude > 0.01f)
        {
            Quaternion desiredRot = Quaternion.LookRotation(toLook, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRot,
                1f - Mathf.Exp(-60f * Time.deltaTime));
        }

        // Sync pitch/yaw so free camera resumes smoothly
        pitch = transform.eulerAngles.x;
        if (pitch > 180f) pitch -= 360f;
        yaw = transform.eulerAngles.y;
    }

    void ClampCameraDistance()
    {
        Vector3 toCamera = transform.position - target.position;
        if (toCamera.magnitude > maxCameraDistance)
        {
            transform.position = target.position + toCamera.normalized * maxCameraDistance;
        }
    }

    Vector3 HandleCameraCollision(Vector3 from, Vector3 to)
    {
        return HandleCameraCollisionWithLayers(from, to, collisionLayers);
    }

    Vector3 HandleCameraCollisionWithLayers(Vector3 from, Vector3 to, LayerMask layers)
    {
        Vector3 direction = to - from;
        float distance = direction.magnitude;

        if (distance < 0.01f) return to;

        RaycastHit hit;
        if (Physics.SphereCast(from, collisionRadius, direction.normalized, out hit, distance, layers))
        {
            return hit.point + hit.normal * collisionRadius;
        }

        return to;
    }

    // ══════════════════════════════════════════════════════════════
    //  CAMERA SHAKE
    // ══════════════════════════════════════════════════════════════
    public void Shake(float magnitude = 0.15f, float duration = 0.2f)
    {
        shakeMagnitude = magnitude;
        shakeTimer = duration;
        shakeDecay = magnitude / duration;
    }

    void ApplyShake()
    {
        if (shakeTimer <= 0f) return;
        shakeTimer -= Time.deltaTime;
        float t = Mathf.Clamp01(shakeTimer / Mathf.Max(shakeMagnitude / shakeDecay, 0.01f));
        float currentMag = shakeMagnitude * t;
        // Apply shake as a temporary offset in screen space (not cumulative)
        Vector3 shakeOffset = Random.insideUnitSphere * currentMag;
        shakeOffset.z = 0f;
        transform.position += transform.right * shakeOffset.x + transform.up * shakeOffset.y;
    }

    public void SetLockOnTarget(Transform target)
    {
        lockOnTarget = target;
        isLockedOn = (target != null);

        if (isLockedOn && target != null)
        {
            Vector3 directionToEnemy = target.position - this.target.position;
            float targetAngle = Mathf.Atan2(directionToEnemy.x, directionToEnemy.z) * Mathf.Rad2Deg;
            yaw = targetAngle;
        }
    }
}

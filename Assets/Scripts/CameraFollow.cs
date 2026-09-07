using UnityEngine;
using UnityEngine.InputSystem;

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
    public LayerMask lockOnCollisionLayers; // env layers only when locked on

    [HideInInspector] public float yaw;
    [HideInInspector] public float pitch;

    private Transform lockOnTarget;
    private bool isLockedOn = false;

    // Camera shake
    private float shakeTimer = 0f;
    private float shakeMagnitude = 0f;
    private float shakeDecay = 1f;

    // new input action for mouse look
    private InputAction lookAction;

    void OnEnable()
    {
        // try to use the built in InputSystem_Actions "Player/Look" action
        // that asset is already loaded by the UI modules, so it works in builds
        InputActionAsset[] assets = Resources.FindObjectsOfTypeAll<InputActionAsset>();
        foreach (var a in assets)
        {
            if (a.name == "InputSystem_Actions")
            {
                lookAction = a.FindAction("Player/Look", true);
                if (lookAction != null)
                    break;
            }
        }

        // fall back to a direct mouse action
        if (lookAction == null)
        {
            lookAction = new InputAction("CameraLook", InputActionType.Value, binding: "<Mouse>/delta");
        }

        lookAction.Enable();
    }

    void OnDisable()
    {
        lookAction?.Disable();
    }

    void Start()
    {
        // auto find player if target not set
        if (target == null)
        {
            PlayerController pc = FindAnyObjectByType<PlayerController>();
            if (pc != null)
                target = pc.transform;
            else
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                    target = player.transform;
            }
        }

        // tag this cam as main so PlayerController can find it
        if (Camera.main == null && GetComponent<Camera>() != null)
            tag = "MainCamera";

        if (target != null)
        {
            Vector3 angles = transform.eulerAngles;
            yaw = angles.y;
            pitch = angles.x;

            if (pitch > 180f) pitch -= 360f;

            // lock + hide cursor for gameplay
            if (Cursor.visible)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }

    private float maxCameraDistance = 20f;

    void LateUpdate()
    {
        if (target == null)
        {
            PlayerController pc = FindAnyObjectByType<PlayerController>();
            if (pc != null) target = pc.transform;
        }

        // relock cursor when it gets unlocked
        if (target != null && Cursor.lockState != CursorLockMode.Locked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

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

    // empty OnGUI fixes a unity build bug where old mouse input gets stripped when "Both" is active
    void OnGUI() { }

    void UpdateFreeCamera()
    {
        // use an Input Action for mouse look, fallback to old axis
        float mouseX = 0f, mouseY = 0f;
        if (lookAction != null && lookAction.enabled)
        {
            Vector2 md = lookAction.ReadValue<Vector2>();
            mouseX = md.x * mouseSensitivity * 0.1f;
            mouseY = md.y * mouseSensitivity * 0.1f;
        }

        // if new input gave nothing, use old Input Manager
        if (Mathf.Abs(mouseX) < 0.001f && Mathf.Abs(mouseY) < 0.001f)
        {
            mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
        }

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

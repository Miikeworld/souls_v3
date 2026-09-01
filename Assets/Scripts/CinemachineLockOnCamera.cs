using UnityEngine;
using Unity.Cinemachine;
using Unity.Cinemachine.TargetTracking;
using System.Collections;

/// <summary>
/// Souls-like camera using Cinemachine 3.x (tested with 3.1.6).
/// Creates two virtual cameras at runtime:
///   1. FreeCam  — third-person orbit controlled by mouse
///   2. LockOnCam — sits behind player, looks at enemy
///
/// SETUP:
///   1. Create an empty GameObject "CameraManager" and add this script
///   2. Assign the player Transform
///   3. DISABLE or REMOVE the old CameraFollow script from the Main Camera
///   4. Wire up LockOnSystem.cinemachineCam to this object
///   5. This script auto-adds CinemachineBrain to the Main Camera
/// </summary>
public class CinemachineLockOnCamera : MonoBehaviour
{
    [Header("Target")]
    public Transform player;

    [Header("Free Camera")]
    public float freeDistance = 4f;
    public float freeHeight = 1.6f;

    [Header("Lock-On Camera")]
    public float lockOnDistance = 5f;
    public float lockOnHeight = 2f;
    public Vector3 lockOnShoulderOffset = new Vector3(0.6f, 0.3f, 0f);

    [Header("Camera Shake")]
    public float defaultShakeMagnitude = 0.15f;
    public float defaultShakeDuration = 0.2f;

    // Runtime references
    private CinemachineCamera freeCam;
    private CinemachineCamera lockOnCam;
    private Transform lockOnTarget;
    private bool isLockedOn;
    private float shakeTimer;
    private CameraFollow cachedCameraFollow;
    private CinemachineBrain cachedBrain;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        EnsureBrain();
        CreateFreeCam();
        CreateLockOnCam();

        SetPriority(freeCam, 10);
        SetPriority(lockOnCam, 0);
    }

    void Update()
    {
        if (shakeTimer > 0f)
            shakeTimer -= Time.deltaTime;

        // Failsafe: ensure camera is always controllable
        if (cachedBrain != null && cachedCameraFollow != null)
        {
            if (!cachedBrain.enabled && !cachedCameraFollow.enabled)
            {
                // Both disabled - re-enable CinemachineBrain as fallback
                cachedBrain.enabled = true;
                Debug.LogWarning("[CinemachineLockOnCamera] Both camera systems were disabled, re-enabled CinemachineBrain");
            }
        }

        // Ensure cursor stays locked (unlocked cursor prevents mouse camera control)
        if (Cursor.lockState != CursorLockMode.Locked)
            Cursor.lockState = CursorLockMode.Locked;
    }

    // ═══════════════════════════════════════════════════════════════
    //  SETUP
    // ═══════════════════════════════════════════════════════════════

    void EnsureBrain()
    {
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            Debug.LogError("[CinemachineLockOnCamera] No Main Camera found!");
            return;
        }

        cachedCameraFollow = mainCam.GetComponent<CameraFollow>();
        cachedBrain = mainCam.GetComponent<CinemachineBrain>();
        if (cachedBrain == null)
        {
            cachedBrain = mainCam.gameObject.AddComponent<CinemachineBrain>();
            Debug.Log("[CinemachineLockOnCamera] Added CinemachineBrain to Main Camera.");
        }

        cachedBrain.DefaultBlend = new CinemachineBlendDefinition(
            CinemachineBlendDefinition.Styles.EaseInOut, 0.4f);

        // Start with CameraFollow disabled, Cinemachine free cam active
        if (cachedCameraFollow != null) cachedCameraFollow.enabled = false;
        cachedBrain.enabled = true;
    }

    void CreateFreeCam()
    {
        GameObject go = new GameObject("CM_FreeCam");
        go.transform.SetParent(transform);

        freeCam = go.AddComponent<CinemachineCamera>();
        freeCam.Follow = player;
        freeCam.LookAt = player;

        // Orbital follow for mouse-controlled orbit
        var orbital = go.AddComponent<CinemachineOrbitalFollow>();
        orbital.TargetOffset = new Vector3(0f, freeHeight, 0f);
        orbital.OrbitStyle = CinemachineOrbitalFollow.OrbitStyles.Sphere;
        orbital.Radius = freeDistance;

        // Input axis controller for mouse orbit
        go.AddComponent<CinemachineInputAxisController>();

        // Rotation composer to look at player
        var composer = go.AddComponent<CinemachineRotationComposer>();
        composer.TargetOffset = new Vector3(0f, 1.2f, 0f);

        Debug.Log("[CinemachineLockOnCamera] Created FreeCam.");
    }

    void CreateLockOnCam()
    {
        GameObject go = new GameObject("CM_LockOnCam");
        go.transform.SetParent(transform);

        lockOnCam = go.AddComponent<CinemachineCamera>();
        lockOnCam.Follow = player;
        lockOnCam.LookAt = player;

        // Positional follow — stable orbit, does NOT chase player rotation
        var follow = go.AddComponent<CinemachineFollow>();
        follow.FollowOffset = new Vector3(0f, lockOnHeight, -lockOnDistance);
        follow.TrackerSettings = new TrackerSettings
        {
            BindingMode = BindingMode.WorldSpace,
            PositionDamping = new Vector3(0.8f, 0.5f, 0.8f),
            RotationDamping = Vector3.zero,
            QuaternionDamping = 0f
        };

        // Rotation composer to aim at the lock-on target
        var composer = go.AddComponent<CinemachineRotationComposer>();
        composer.TargetOffset = new Vector3(0f, 1.0f, 0f);
        composer.Damping = new Vector2(2f, 2f);

        Debug.Log("[CinemachineLockOnCamera] Created LockOnCam.");
    }

    // ═══════════════════════════════════════════════════════════════
    //  PRIORITY HELPER (CM3 uses PrioritySettings struct)
    // ═══════════════════════════════════════════════════════════════

    void SetPriority(CinemachineCamera cam, int value)
    {
        cam.Priority = new PrioritySettings { Enabled = true, Value = value };
    }

    // ═══════════════════════════════════════════════════════════════
    //  PUBLIC API — called by LockOnSystem
    // ═══════════════════════════════════════════════════════════════

    public void SetLockOnTarget(Transform target)
    {
        if (target == null)
        {
            ClearLockOn();
            return;
        }

        lockOnTarget = target;
        isLockedOn = true;

        // Disable Cinemachine, enable CameraFollow for lock-on
        if (cachedBrain != null) cachedBrain.enabled = false;
        if (cachedCameraFollow != null)
        {
            cachedCameraFollow.enabled = true;
            cachedCameraFollow.SetLockOnTarget(target);
        }
    }

    public void ClearLockOn()
    {
        isLockedOn = false;
        lockOnTarget = null;

        // Disable CameraFollow, re-enable Cinemachine for free cam
        if (cachedCameraFollow != null)
        {
            cachedCameraFollow.SetLockOnTarget(null);
            cachedCameraFollow.enabled = false;
        }
        if (cachedBrain != null) cachedBrain.enabled = true;
    }

    // ═══════════════════════════════════════════════════════════════
    //  CAMERA SHAKE
    // ═══════════════════════════════════════════════════════════════

    public void Shake(float magnitude = -1f, float duration = -1f)
    {
        if (magnitude < 0f) magnitude = defaultShakeMagnitude;
        if (duration < 0f) duration = defaultShakeDuration;

        // Route shake to CameraFollow when it's active (lock-on mode)
        if (cachedCameraFollow != null && cachedCameraFollow.enabled)
        {
            cachedCameraFollow.Shake(magnitude, duration);
            return;
        }
        StartCoroutine(DoShake(magnitude, duration));
    }

    IEnumerator DoShake(float magnitude, float duration)
    {
        Camera cam = Camera.main;
        if (cam == null) yield break;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float decay = 1f - (elapsed / duration);
            Vector3 offset = Random.insideUnitSphere * magnitude * decay;
            offset.z = 0f;
            cam.transform.localPosition += cam.transform.TransformDirection(offset) * Time.deltaTime;
            elapsed += Time.deltaTime;
            yield return null;
        }
    }
}

using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;
using Unity.Cinemachine.TargetTracking;
using System.Collections;
using System.Linq;

/// <summary>
/// Cinemachine 3.x free + lock-on camera. Creates FreeCam and LockOnCam at runtime.
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

        // Re-enable brain if both camera systems are disabled
        if (cachedBrain != null && cachedCameraFollow != null)
        {
            if (!cachedBrain.enabled && !cachedCameraFollow.enabled)
            {
                cachedBrain.enabled = true;
                Debug.LogWarning("[CinemachineLockOnCamera] Both camera systems were disabled, re-enabled CinemachineBrain");
            }
        }

        // Re-lock cursor when hidden (other systems may unlock it)
        if (!Cursor.visible && Cursor.lockState != CursorLockMode.Locked)
            Cursor.lockState = CursorLockMode.Locked;
    }


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

        // Disable CameraFollow, enable Cinemachine free cam
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

        // Mouse-controlled orbit
        var orbital = go.AddComponent<CinemachineOrbitalFollow>();
        orbital.TargetOffset = new Vector3(0f, freeHeight, 0f);
        orbital.OrbitStyle = CinemachineOrbitalFollow.OrbitStyles.Sphere;
        orbital.Radius = freeDistance;

        // Bind look and scroll inputs
        var axisController = go.AddComponent<CinemachineInputAxisController>();

        // Bind the Player/Look action
        InputActionReference lookRef = GetLookActionReference();
        if (lookRef != null)
        {
            axisController.SynchronizeControllers();
            foreach (var c in axisController.Controllers)
            {
                if (c == null || c.Input == null) continue;

                c.Input.CancelDeltaTime = true;

                if (c.Name == "Look Orbit X" || c.Name == "Look Orbit Y")
                {
                    c.Input.InputAction = lookRef;
                    c.Input.Gain = c.Name == "Look Orbit Y" ? -0.08f : 0.08f;
                }
                else if (c.Name == "Orbit Scale")
                {
                    c.Input.LegacyInput = "Mouse ScrollWheel";
                    c.Input.LegacyGain = 1f;
                }
            }
        }

        // Look at player
        var composer = go.AddComponent<CinemachineRotationComposer>();
        composer.TargetOffset = new Vector3(0f, 1.2f, 0f);

        Debug.Log("[CinemachineLockOnCamera] Created FreeCam.");
    }

    InputActionReference GetLookActionReference()
    {
        InputActionAsset asset = null;

        // Try project-wide actions first
        if (InputSystem.actions != null)
            asset = InputSystem.actions;

        // Then search preloaded asset
        if (asset == null)
        {
            var found = Resources.FindObjectsOfTypeAll<InputActionAsset>();
            asset = found.FirstOrDefault(a => a.name == "InputSystem_Actions");
        }

        if (asset == null) return null;

        var action = asset.FindAction("Player/Look", true);
        if (action == null) return null;

        return InputActionReference.Create(action);
    }

    void CreateLockOnCam()
    {
        GameObject go = new GameObject("CM_LockOnCam");
        go.transform.SetParent(transform);

        lockOnCam = go.AddComponent<CinemachineCamera>();
        lockOnCam.Follow = player;
        lockOnCam.LookAt = player;

        // World-space follow; does not inherit player rotation
        var follow = go.AddComponent<CinemachineFollow>();
        follow.FollowOffset = new Vector3(0f, lockOnHeight, -lockOnDistance);
        follow.TrackerSettings = new TrackerSettings
        {
            BindingMode = BindingMode.WorldSpace,
            PositionDamping = new Vector3(0.8f, 0.5f, 0.8f),
            RotationDamping = Vector3.zero,
            QuaternionDamping = 0f
        };

        // Aim at lock-on target
        var composer = go.AddComponent<CinemachineRotationComposer>();
        composer.TargetOffset = new Vector3(0f, 1.0f, 0f);
        composer.Damping = new Vector2(2f, 2f);

        Debug.Log("[CinemachineLockOnCamera] Created LockOnCam.");
    }

    // Set CM3 priority via PrioritySettings
    void SetPriority(CinemachineCamera cam, int value)
    {
        cam.Priority = new PrioritySettings { Enabled = true, Value = value };
    }

    // Lock-on API
    public void SetLockOnTarget(Transform target)
    {
        if (target == null)
        {
            ClearLockOn();
            return;
        }

        lockOnTarget = target;
        isLockedOn = true;

        // Use CameraFollow for lock-on
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

        // Use Cinemachine for free cam
        if (cachedCameraFollow != null)
        {
            cachedCameraFollow.SetLockOnTarget(null);
            cachedCameraFollow.enabled = false;
        }
        if (cachedBrain != null) cachedBrain.enabled = true;
    }


    public void Shake(float magnitude = -1f, float duration = -1f)
    {
        if (magnitude < 0f) magnitude = defaultShakeMagnitude;
        if (duration < 0f) duration = defaultShakeDuration;

        // Delegate shake to CameraFollow when active
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

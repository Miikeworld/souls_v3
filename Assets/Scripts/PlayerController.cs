using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 4f;
    public float sprintSpeed = 7f;
    public float rotationSmoothTime = 0.1f;

    [Header("Jumping")]
    public float jumpForce = 7f;
    public float gravity = -20f;

    [Header("Roll / Dodge")]
    public float rollSpeed = 10f;
    public float rollDuration = 0.4f;
    public float rollCooldown = 0.5f;
    public float tapThreshold = 0.2f; // Time threshold to detect tap vs hold

    private CharacterController controller;
    private Transform cam;

    private Vector3 velocity;
    private float turnSmoothVelocity;

    private bool isRolling = false;
    private float rollTimer = 0f;
    private float rollCooldownTimer = 0f;
    private Vector3 rollDirection;

    private float spaceHoldTime = 0f;
    private bool spaceWasPressed = false;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
            cam = mainCamera.transform;

        if (controller == null)
            Debug.LogError("CharacterController missing!");
        if (cam == null)
            Debug.LogError("Main Camera not found!");
    }

    void Update()
    {
        if (controller == null || cam == null) return;

        // Cooldown timer
        if (rollCooldownTimer > 0f)
            rollCooldownTimer -= Time.deltaTime;

        // Handle rolling state
        if (isRolling)
        {
            HandleRoll();
            ApplyGravity();
            return;
        }

        // Get input using direct keys
        float h = (Input.GetKey(KeyCode.D) ? 1f : 0f) + (Input.GetKey(KeyCode.A) ? -1f : 0f);
        float v = (Input.GetKey(KeyCode.W) ? 1f : 0f) + (Input.GetKey(KeyCode.S) ? -1f : 0f);

        Vector3 inputDir = new Vector3(h, 0f, v);

        // Detect tap vs hold for Space key
        bool isSprinting = false;
        
        if (Input.GetKey(KeyCode.Space))
        {
            spaceHoldTime += Time.deltaTime;
            spaceWasPressed = true;
            
            // If held longer than threshold, it's a sprint
            if (spaceHoldTime > tapThreshold)
            {
                isSprinting = true;
            }
        }
        
        if (Input.GetKeyUp(KeyCode.Space) && spaceWasPressed)
        {
            // If released quickly, it's a tap = roll
            if (spaceHoldTime <= tapThreshold && inputDir.magnitude > 0.1f && rollCooldownTimer <= 0f)
            {
                Vector3 camForward = cam.forward;
                camForward.y = 0f;
                camForward.Normalize();

                Vector3 camRight = cam.right;
                camRight.y = 0f;
                camRight.Normalize();

                Vector3 rollDir = (camForward * v + camRight * h).normalized;
                StartRoll(rollDir);
            }
            
            spaceHoldTime = 0f;
            spaceWasPressed = false;
        }

        // Handle movement and rotation
        if (inputDir.magnitude > 0.1f)
        {
            HandleMovement(inputDir, isSprinting);
        }

        // Handle jump with F key
        if (Input.GetKeyDown(KeyCode.F) && controller.isGrounded)
        {
            velocity.y = jumpForce;
        }

        // Apply gravity
        ApplyGravity();
    }

    void HandleMovement(Vector3 inputDir, bool sprint)
    {
        // Get camera's forward and right vectors (flattened on XZ plane)
        Vector3 camForward = cam.forward;
        camForward.y = 0f;
        camForward.Normalize();

        Vector3 camRight = cam.right;
        camRight.y = 0f;
        camRight.Normalize();

        // Calculate movement direction relative to camera
        Vector3 moveDir = (camForward * inputDir.z + camRight * inputDir.x).normalized;

        // Calculate target rotation
        if (moveDir.magnitude > 0.1f)
        {
            float targetAngle = Mathf.Atan2(moveDir.x, moveDir.z) * Mathf.Rad2Deg;

            // Smooth rotation toward target angle
            float smoothAngle = Mathf.SmoothDampAngle(
                transform.eulerAngles.y,
                targetAngle,
                ref turnSmoothVelocity,
                rotationSmoothTime
            );

            transform.rotation = Quaternion.Euler(0f, smoothAngle, 0f);

            // Move character
            float speed = sprint ? sprintSpeed : walkSpeed;
            controller.Move(moveDir * speed * Time.deltaTime);
        }
    }

    void ApplyGravity()
    {
        if (controller.isGrounded && velocity.y < 0f)
        {
            velocity.y = -2f;
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    void StartRoll(Vector3 direction)
    {
        isRolling = true;
        rollTimer = rollDuration;
        rollCooldownTimer = rollCooldown;
        rollDirection = direction;
    }

    void HandleRoll()
    {
        rollTimer -= Time.deltaTime;

        if (rollTimer <= 0f)
        {
            isRolling = false;
            return;
        }

        controller.Move(rollDirection * rollSpeed * Time.deltaTime);
    }
}

using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 6f;
    public float gravity = -9.81f;
    public float jumpForce = 5f;
    
    [Header("Rotation")]
    public float rotationSmoothTime = 0.1f;
    
    private CharacterController controller;
    private Camera mainCam;
    private Vector3 velocity;
    private float turnSmoothVelocity;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        mainCam = Camera.main;
        
        if (controller == null)
            Debug.LogError("CharacterController not found on " + gameObject.name);
        
        if (mainCam == null)
            Debug.LogError("Main Camera not found!");
    }

    void Update()
    {
        if (mainCam == null || controller == null) return;

        // Input - DEBUG VERSION
        float horizontal = Input.GetKey(KeyCode.D) ? 1f : Input.GetKey(KeyCode.A) ? -1f : 0f;
        float vertical = Input.GetKey(KeyCode.W) ? 1f : Input.GetKey(KeyCode.S) ? -1f : 0f;
        Vector3 direction = new Vector3(horizontal, 0f, vertical).normalized;

        Debug.Log($"Input: H={horizontal}, V={vertical}, Direction={direction}");

        // Movement
        if (direction.magnitude >= 0.1f)
        {
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + mainCam.transform.eulerAngles.y;
            float smoothedAngle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, rotationSmoothTime);
            transform.rotation = Quaternion.Euler(0f, smoothedAngle, 0f);

            Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            controller.Move(moveDir.normalized * moveSpeed * Time.deltaTime);
            
            Debug.Log("Moving: " + moveDir.normalized * moveSpeed);
        }

        // Gravity
        if (controller.isGrounded && velocity.y < 0)
            velocity.y = -2f;

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        // Jump
        if (Input.GetKeyDown(KeyCode.Space) && controller.isGrounded)
        {
            velocity.y = jumpForce;
            Debug.Log("Jumping!");
        }
    }
}

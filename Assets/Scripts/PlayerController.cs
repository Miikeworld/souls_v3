using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : Entity
{
    [Header("Movement")]
    public float walkSpeed = 5f;
    public float sprintSpeed = 8f;
    public float sprintStaminaCost = 20f;
    public float jumpForce = 5f;
    public float jumpStaminaCost = 15f;
    public float gravity = -20f;
    
    [Header("Roll/Dodge")]
    public float rollSpeed = 10f;
    public float rollDuration = 0.4f;
    public float rollCooldown = 0.5f;
    public float rollStaminaCost = 30f;
    
    [Header("Combat")]
    public float attackDamage = 20f;
    public float attackRange = 2f;
    public float attackStaminaCost = 10f;
    public float attackCooldown = 0.5f;
    
    [Header("Abilities")]
    public float fireballDamage = 30f;
    public float fireballManaCost = 25f;
    public GameObject fireballPrefab;
    public Transform castPoint;
    
    private CharacterController controller;
    private float attackTimer = 0f;
    
    // Roll state
    private bool isRolling = false;
    private float rollTimer = 0f;
    private float rollCooldownTimer = 0f;
    private Vector3 rollDirection = Vector3.zero;
    
    // Sprint state
    private float spaceHoldTime = 0f;
    private bool spaceWasPressed = false;
    private float tapThreshold = 0.2f;
    
    private Vector3 velocity = Vector3.zero;
    
    protected override void Start()
    {
        base.Start();
        
        controller = GetComponent<CharacterController>();
        
        // Create cast point if missing
        if (castPoint == null)
        {
            GameObject cp = new GameObject("CastPoint");
            cp.transform.parent = transform;
            cp.transform.localPosition = new Vector3(0f, 1.5f, 0.5f);
            castPoint = cp.transform;
        }
    }
    
    protected override void Update()
    {
        base.Update();
        
        if (isDead) return;
        
        attackTimer -= Time.deltaTime;
        
        if (rollCooldownTimer > 0)
        {
            rollCooldownTimer -= Time.deltaTime;
        }
        
        HandleInput();
        
        if (isRolling)
        {
            HandleRoll();
        }
        else
        {
            HandleMovement();
        }
        
        ApplyGravity();
    }
    
    void HandleInput()
    {
        // Attack (Left Click)
        if (Input.GetMouseButtonDown(0) && attackTimer <= 0f)
        {
            if (UseStamina(attackStaminaCost))
            {
                PerformMeleeAttack();
                attackTimer = attackCooldown;
            }
        }
        
        // Fireball (Q)
        if (Input.GetKeyDown(KeyCode.Q))
        {
            if (UseMana(fireballManaCost))
            {
                CastFireball();
            }
        }
        
        // Jump (F key)
        if (Input.GetKeyDown(KeyCode.F) && controller.isGrounded)
        {
            if (UseStamina(jumpStaminaCost))
            {
                velocity.y = jumpForce;
            }
        }
        
        // Space key handling
        if (Input.GetKey(KeyCode.Space))
        {
            spaceHoldTime += Time.deltaTime;
            spaceWasPressed = true;
        }
        
        if (Input.GetKeyUp(KeyCode.Space) && spaceWasPressed)
        {
            // Quick tap = roll
            if (spaceHoldTime <= tapThreshold && rollCooldownTimer <= 0f && controller.isGrounded)
            {
                float h = Input.GetAxis("Horizontal");
                float v = Input.GetAxis("Vertical");
                
                if (Mathf.Abs(h) > 0.1f || Mathf.Abs(v) > 0.1f)
                {
                    Camera cam = Camera.main;
                    if (cam != null)
                    {
                        Vector3 camForward = new Vector3(cam.transform.forward.x, 0, cam.transform.forward.z).normalized;
                        Vector3 camRight = new Vector3(cam.transform.right.x, 0, cam.transform.right.z).normalized;
                        
                        Vector3 rollDir = (camForward * v + camRight * h).normalized;
                        StartRoll(rollDir);
                    }
                }
            }
            
            spaceHoldTime = 0f;
            spaceWasPressed = false;
        }
    }
    
    void HandleMovement()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        
        if (Mathf.Abs(horizontal) > 0.1f || Mathf.Abs(vertical) > 0.1f)
        {
            Camera cam = Camera.main;
            if (cam == null) return;
            
            // Get camera directions
            Vector3 camForward = new Vector3(cam.transform.forward.x, 0, cam.transform.forward.z).normalized;
            Vector3 camRight = new Vector3(cam.transform.right.x, 0, cam.transform.right.z).normalized;
            
            // Calculate movement
            Vector3 moveDir = (camForward * vertical + camRight * horizontal).normalized;
            
            // Sprint check
            bool isSprinting = Input.GetKey(KeyCode.Space) && spaceHoldTime > tapThreshold;
            float currentSpeed = isSprinting ? sprintSpeed : walkSpeed;
            
            if (isSprinting)
            {
                if (!UseStamina(sprintStaminaCost * Time.deltaTime))
                {
                    currentSpeed = walkSpeed;
                }
            }
            
            // Rotate to face direction
            float targetAngle = Mathf.Atan2(moveDir.x, moveDir.z) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, Mathf.LerpAngle(transform.eulerAngles.y, targetAngle, 10f * Time.deltaTime), 0);
            
            // Move
            controller.Move(moveDir * currentSpeed * Time.deltaTime);
        }
    }
    
    void ApplyGravity()
    {
        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }
        
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
    
    void StartRoll(Vector3 direction)
    {
        if (!UseStamina(rollStaminaCost)) return;
        
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
    
    void PerformMeleeAttack()
    {
        Collider[] hitEnemies = Physics.OverlapSphere(transform.position + transform.forward, attackRange);
        
        foreach (Collider col in hitEnemies)
        {
            Entity enemy = col.GetComponent<Entity>();
            if (enemy != null && enemy != this)
            {
                enemy.TakeDamage(attackDamage, this);
            }
        }
    }
    
    void CastFireball()
    {
        if (fireballPrefab == null) return;
        
        GameObject fireball = Instantiate(fireballPrefab, castPoint.position, castPoint.rotation);
        
        Rigidbody fbRb = fireball.GetComponent<Rigidbody>();
        if (fbRb != null)
        {
            fbRb.linearVelocity = transform.forward * 15f;
        }
        
        Projectile proj = fireball.GetComponent<Projectile>();
        if (proj != null)
        {
            proj.damage = fireballDamage;
            proj.owner = this;
        }
        
        Destroy(fireball, 5f);
    }
    
    protected override void Die()
    {
        base.Die();
        enabled = false;
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + transform.forward, attackRange);
    }
}

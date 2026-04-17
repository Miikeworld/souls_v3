using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : Entity
{
    [Header("Movement")]
    public float walkSpeed = 5f;
    public float sprintSpeed = 8f;
    public float sprintStaminaCost = 20f;
    public float jumpForce = 8f;
    public float jumpStaminaCost = 15f;
    public float gravity = -20f;
    public float fallMultiplier = 2.5f;
    
    [Header("Animation")]
    public Animator animator;
    public float animationSpeedMultiplier = 1f;
    
    [Header("Weapon System")]
    public int currentWeaponType = 1; // 1=OneHanded, 2=TwoHanded, 3=Greatsword, 4=Rapier, 5=Unarmed
    
    [Header("Roll/Dodge")]
    public float rollSpeed = 10f;
    public float rollDuration = 0.8f;
    public float rollCooldown = 0.5f;
    public float rollStaminaCost = 30f;
    
    [Header("Combat")]
    public float attackDamage = 20f;
    public float attackRange = 2f;
    public float attackStaminaCost = 10f;
    public float attackCooldown = 1.2f;
    
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
    
    // Attack state
    private bool isAttacking = false;
    private int comboStep = 0;
    private float comboResetTimer = 0f;
    private float comboWindow = 0.8f;
    private float comboCooldown = 0f;
    private float comboCooldownDuration = 1.5f; // cooldown after full combo
    private float backstepDuration = 0.45f; // 13 frames at 30fps
    
    // Magical Knight combo animation names (root motion)
    private readonly string[] comboAnims = { "combo_01_1", "combo_01_2", "combo_01_3", "combo_01_4" };
    
    // Movement tracking
    private float currentMoveSpeed = 0f;
    
    // Jump/Fall state
    private bool isJumping = false;
    private bool isFalling = false;
    private bool wasGrounded = true;
    private float airTime = 0f;
    private float jumpAnimDuration = 0.8f; // approximate jump anim length
    
    // Sprint state
    private float spaceHoldTime = 0f;
    private bool spaceWasPressed = false;
    private float tapThreshold = 0.2f;
    
    private Vector3 velocity = Vector3.zero;
    
    protected override void Start()
    {
        base.Start();
        
        controller = GetComponent<CharacterController>();
        
        // Get animator if not assigned
        if (animator == null)
            animator = GetComponent<Animator>();
        
        // Enable root motion so animations move the character
        if (animator != null)
            animator.applyRootMotion = true;
        
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
        
        if (comboCooldown > 0)
            comboCooldown -= Time.deltaTime;
        
        if (rollCooldownTimer > 0)
        {
            rollCooldownTimer -= Time.deltaTime;
        }
        
        // Combo reset timer
        if (comboResetTimer > 0)
        {
            comboResetTimer -= Time.deltaTime;
            if (comboResetTimer <= 0)
                comboStep = 0;
        }
        
        HandleInput();
        
        if (isRolling)
        {
            HandleRoll();
        }
        else if (!isAttacking)
        {
            HandleMovement();
        }
        
        ApplyGravity();
        
        // Update animations
        UpdateAnimations();
    }
    
    void UpdateAnimations()
    {
        if (animator == null) return;
        
        // Smooth Speed parameter for natural idle↔movement blending
        float targetSpeed = currentMoveSpeed * animationSpeedMultiplier;
        float currentSpeed = animator.GetFloat("Speed");
        animator.SetFloat("Speed", Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * 10f));
        
        // Ground state
        animator.SetBool("IsGrounded", controller.isGrounded);
        
        // Landing detection
        if (controller.isGrounded && !wasGrounded)
        {
            isJumping = false;
            isFalling = false;
            airTime = 0f;
            currentMoveSpeed = 0f;
            animator.ResetTrigger("Jump");
            // Don't override roll or attack animations on landing
            if (!isRolling && !isAttacking)
                animator.CrossFade("Movement", 0.15f);
        }
        
        // Falling detection - when airborne past jump animation or walking off edge
        // Skip during roll/attack — root motion can briefly lift character off ground
        if (!controller.isGrounded && !isRolling && !isAttacking)
        {
            airTime += Time.deltaTime;
            if (!isFalling && (airTime > jumpAnimDuration || !isJumping))
            {
                isFalling = true;
                // Use jump anim as falling pose (looping)
                animator.CrossFade("jump", 0.2f);
            }
        }
        else if (controller.isGrounded)
        {
            airTime = 0f;
        }
        
        // Safety: clear stale Jump trigger while grounded
        if (controller.isGrounded)
        {
            animator.ResetTrigger("Jump");
        }
        wasGrounded = controller.isGrounded;
        animator.SetBool("IsJumping", isJumping);
        
        // Combat states
        animator.SetBool("IsAttacking", isAttacking);
        animator.SetBool("IsBlocking", Input.GetKey(KeyCode.Mouse1));
        
        // Weapon system
        animator.SetInteger("WeaponType", currentWeaponType);
        
        // Animator playback speed (faster when sprinting)
        bool sprintAnim = Input.GetKey(KeyCode.Space) && spaceHoldTime > tapThreshold && currentMoveSpeed >= 1.0f;
        animator.speed = sprintAnim ? 1.3f : 1f;
    }
    
    void HandleInput()
    {
        // Attack (Left Click)
        if (Input.GetMouseButtonDown(0) && attackTimer <= 0f && comboCooldown <= 0f)
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
                attackTimer = attackCooldown;
            }
        }
        
        // Use Potion (R key)
        if (Input.GetKeyDown(KeyCode.R))
        {
            UsePotion();
        }
        
        // Weapon Switch (1, 2, 3, 4, 5 keys)
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            currentWeaponType = 1; // One-Handed
            Debug.Log("Switched to One-Handed weapon");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            currentWeaponType = 2; // Two-Handed
            Debug.Log("Switched to Two-Handed weapon");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            currentWeaponType = 3; // Greatsword
            Debug.Log("Switched to Greatsword");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            currentWeaponType = 4; // Rapier
            Debug.Log("Switched to Rapier");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            currentWeaponType = 5; // Unarmed
            Debug.Log("Switched to Unarmed");
        }
        
        // Jump (F key) - only if grounded and not already jumping
        if (Input.GetKeyDown(KeyCode.F) && controller.isGrounded && !isJumping)
        {
            velocity.y = jumpForce;
            isJumping = true;
            if (animator != null)
            {
                animator.ResetTrigger("Jump");
                animator.SetTrigger("Jump");
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
                    // Directional roll
                    Camera cam = Camera.main;
                    if (cam != null)
                    {
                        Vector3 camForward = new Vector3(cam.transform.forward.x, 0, cam.transform.forward.z).normalized;
                        Vector3 camRight = new Vector3(cam.transform.right.x, 0, cam.transform.right.z).normalized;
                        Vector3 rollDir = (camForward * v + camRight * h).normalized;
                        StartRoll(rollDir);
                    }
                }
                else
                {
                    // No direction = backstep
                    StartBackstep();
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
            bool canSprint = isSprinting && UseStamina(sprintStaminaCost * Time.deltaTime);
            float actualSpeed = canSprint ? sprintSpeed : walkSpeed;
            
            // Rotate to face direction
            float targetAngle = Mathf.Atan2(moveDir.x, moveDir.z) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, Mathf.LerpAngle(transform.eulerAngles.y, targetAngle, 10f * Time.deltaTime), 0);
            
            // Move
            controller.Move(moveDir * actualSpeed * Time.deltaTime);
            
            // Track speed for animator - must match actual speed, not input
            // 0=idle, 0.5=jog, 1.0=fast run
            currentMoveSpeed = canSprint ? 1.0f : 0.5f;
        }
        else
        {
            currentMoveSpeed = 0f;
        }
    }
    
    void ApplyGravity()
    {
        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }
        
        // Apply stronger gravity when falling for snappier descent
        if (velocity.y < 0)
            velocity.y += gravity * fallMultiplier * Time.deltaTime;
        else
            velocity.y += gravity * Time.deltaTime;
        
        controller.Move(velocity * Time.deltaTime);
    }
    
    void StartRoll(Vector3 direction)
    {
        if (!UseStamina(rollStaminaCost)) return;
        
        // Cancel attack if rolling
        if (isAttacking)
        {
            isAttacking = false;
            CancelInvoke(nameof(ResetAttackState));
            CancelInvoke(nameof(DealMeleeDamage));
        }
        
        isRolling = true;
        rollTimer = rollDuration;
        rollCooldownTimer = rollCooldown;
        rollDirection = direction;
        
        // Play directional roll animation (root motion)
        if (animator != null)
            animator.CrossFade("roll_front", 0.05f);
    }
    
    void StartBackstep()
    {
        if (!UseStamina(rollStaminaCost * 0.5f)) return;
        
        // Cancel attack if backstepping
        if (isAttacking)
        {
            isAttacking = false;
            CancelInvoke(nameof(ResetAttackState));
            CancelInvoke(nameof(DealMeleeDamage));
        }
        
        isRolling = true; // reuse roll state to block movement
        rollTimer = backstepDuration;
        rollCooldownTimer = rollCooldown;
        rollDirection = -transform.forward;
        
        // Play backstep animation (root motion)
        if (animator != null)
            animator.CrossFade("move_step_back", 0.05f);
    }
    
    void HandleRoll()
    {
        rollTimer -= Time.deltaTime;
        
        if (rollTimer <= 0f)
        {
            isRolling = false;
            // Snap back to Movement immediately
            if (animator != null)
                animator.CrossFade("Movement", 0.1f);
            return;
        }
        
        // Root motion handles roll movement — no code-based move needed
    }
    
    void PerformMeleeAttack()
    {
        isAttacking = true;
        
        // After full combo, set cooldown and don't attack
        if (comboStep >= comboAnims.Length)
        {
            comboStep = 0;
            comboCooldown = comboCooldownDuration;
            isAttacking = false;
            return;
        }
        
        // Play the combo animation directly via CrossFade
        if (animator != null)
            animator.CrossFade(comboAnims[comboStep], 0.1f);
        
        // Deal damage slightly after swing starts
        Invoke(nameof(DealMeleeDamage), 0.2f);
        
        comboStep++;
        comboResetTimer = comboWindow;
        
        // Reset attack state after animation plays (must be longer than attackCooldown)
        CancelInvoke(nameof(ResetAttackState));
        Invoke(nameof(ResetAttackState), 1.1f);
    }
    
    void DealMeleeDamage()
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
    
    void ReturnToMovement()
    {
        if (animator != null && !isRolling && !isJumping && !isAttacking)
            animator.CrossFade("Movement", 0.15f);
    }
    
    void ResetAttackState()
    {
        isAttacking = false;
        // Only snap back to Movement if no new attack is queued
        if (animator != null && !isRolling && !isJumping)
            animator.CrossFade("Movement", 0.2f);
    }
    
    // Keep these for backwards compatibility if Sharp Accent anims are still referenced
    public void OpenDamageColliders() { }
    public void CloseDamageColliders() { }
    
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
        
        // Respawn player after delay
        StartCoroutine(RespawnCoroutine());
    }
    
    IEnumerator RespawnCoroutine()
    {
        yield return new WaitForSeconds(3f);
        
        // Respawn at last bonfire
        GameManager.Instance.RespawnPlayer(gameObject);
        
        // Respawn enemies
        GameManager.Instance.RespawnEnemies();
        
        // Re-enable player controller
        enabled = true;
    }
    
    void OnAnimatorMove()
    {
        if (animator == null) return;
        
        // During attacks and rolls, let root motion drive movement
        if (isAttacking || isRolling)
        {
            Vector3 rootMotion = animator.deltaPosition;
            
            rootMotion.y = velocity.y * Time.deltaTime; // keep gravity
            controller.Move(rootMotion);
        }
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + transform.forward, attackRange);
    }
}

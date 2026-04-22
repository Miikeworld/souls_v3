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
    
    [Header("Roll")]
    public float rollDuration = 1.0f;
    public float rollCooldown = 0.5f;
    public float rollStaminaCost = 30f;
    public float rollSpeed = 8f;
    public float rollIframeDuration = 0.6f;
    
    [Header("Combat")]
    public float attackDamage = 25f;
    public float attackRange = 2.5f;
    public float attackStaminaCost = 12f;
    public float attackCooldown = 1.2f;
    public float attackLungeSpeed = 3f;
    
    [Header("Weapon Hitbox (assign in Inspector)")]
    public WeaponHitbox weaponHitbox;
    
    [Header("Lock-On")]
    public LockOnSystem lockOnSystem;
    
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
    private float backstepDuration = 0.45f;
    private float iframeTimer = 0f;
    
    // Attack state
    private bool isAttacking = false;
    private int comboStep = 0;
    private float comboResetTimer = 0f;
    private float comboWindow = 0.8f;
    private float comboCooldown = 0f;
    private float comboCooldownDuration = 1.5f;
    
    // Katana 3-hit combo animation names
    private readonly string[] comboAnims = { "Attack_3Combo_1", "Attack_3Combo_2", "Attack_3Combo_3" };
    private readonly float[] comboDurations = { 1.27f, 1.17f, 2.43f }; // frames / 30fps
    private float currentAttackTimer = 0f;
    private bool comboQueued = false;
    
    // Healing state
    private bool isHealing = false;
    private float healTimer = 0f;
    private float healDuration = 2.67f;
    private float emptyPotionDuration = 2.17f;
    private float healMoveSpeedMultiplier = 0.4f;
    private int upperBodyLayerIndex = 1;
    
    // Hit reaction
    private bool isStaggered = false;
    private float staggerTimer = 0f;
    private float staggerDuration = 0.5f;
    
    // Movement tracking
    private float currentMoveSpeed = 0f;
    
    // Jump/Fall state
    private bool isJumping = false;
    private bool isFalling = false;
    private bool wasGrounded = true;
    private float airTime = 0f;
    private float jumpAnimDuration = 0.8f;
    
    // Sprint state
    private float spaceHoldTime = 0f;
    private bool spaceWasPressed = false;
    private float tapThreshold = 0.2f;
    
    private Vector3 velocity = Vector3.zero;
    
    // Check if locked on
    private bool IsLockedOn => lockOnSystem != null && lockOnSystem.currentTarget != null;
    
    // Public accessor for camera to check roll state
    public bool IsRolling => isRolling;
    
    protected override void Start()
    {
        base.Start();
        
        controller = GetComponent<CharacterController>();
        
        if (animator == null)
            animator = GetComponent<Animator>();
        
        if (animator != null)
            animator.applyRootMotion = true;
        
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
        
        if (comboCooldown > 0) comboCooldown -= Time.deltaTime;
        if (rollCooldownTimer > 0) rollCooldownTimer -= Time.deltaTime;
        
        // Combo reset timer
        if (comboResetTimer > 0)
        {
            comboResetTimer -= Time.deltaTime;
            if (comboResetTimer <= 0) comboStep = 0;
        }
        
        // Current attack duration countdown
        if (currentAttackTimer > 0)
        {
            currentAttackTimer -= Time.deltaTime;
            if (currentAttackTimer <= 0f)
            {
                // Current attack anim finished — check if next was queued
                if (comboQueued && comboStep < comboAnims.Length && comboCooldown <= 0f)
                {
                    comboQueued = false;
                    PerformMeleeAttack();
                }
                else
                {
                    // No queue or combo done — return to movement
                    isAttacking = false;
                    comboQueued = false;
                    if (animator != null) animator.CrossFade("Movement", 0.2f);
                }
            }
        }
        
        // Stagger timer
        if (isStaggered)
        {
            staggerTimer -= Time.deltaTime;
            if (staggerTimer <= 0f)
            {
                isStaggered = false;
                if (animator != null) animator.CrossFade("Movement", 0.2f);
            }
        }
        
        // Healing timer
        if (isHealing)
        {
            healTimer -= Time.deltaTime;
            if (healTimer <= 0f)
            {
                isHealing = false;
                if (animator != null)
                {
                    animator.SetLayerWeight(upperBodyLayerIndex, 0f);
                    animator.CrossFade("Movement", 0.2f, 0);
                }
            }
        }
        
        HandleInput();
        
        if (isRolling)
        {
            HandleRoll();
        }
        else if (!isAttacking && !isStaggered)
        {
            HandleMovement();
        }
        
        ApplyGravity();
        UpdateAnimations();
    }
    
    void UpdateAnimations()
    {
        if (animator == null) return;
        
        // Smooth Speed parameter for idle↔jog↔run blend
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
            if (!isRolling && !isAttacking)
                animator.CrossFade("Movement", 0.15f);
        }
        
        // Falling detection
        if (!controller.isGrounded && !isRolling && !isAttacking)
        {
            airTime += Time.deltaTime;
            if (!isFalling && (airTime > jumpAnimDuration || !isJumping))
            {
                isFalling = true;
                animator.CrossFade("jump", 0.2f);
            }
        }
        else if (controller.isGrounded)
        {
            airTime = 0f;
        }
        
        if (controller.isGrounded) animator.ResetTrigger("Jump");
        wasGrounded = controller.isGrounded;
        animator.SetBool("IsJumping", isJumping);
        
        // Combat states
        animator.SetBool("IsAttacking", isAttacking);
        animator.SetBool("IsBlocking", Input.GetKey(KeyCode.Mouse1));
        
        // Animator playback speed (faster when sprinting)
        bool sprintAnim = Input.GetKey(KeyCode.Space) && spaceHoldTime > tapThreshold && currentMoveSpeed >= 1.0f;
        animator.speed = sprintAnim ? 1.3f : 1f;
    }
    
    void HandleInput()
    {
        // Attack (Left Click)
        if (Input.GetMouseButtonDown(0) && !isRolling && !isStaggered)
        {
            if (isAttacking && currentAttackTimer > 0f)
            {
                // Queue next combo hit
                comboQueued = true;
            }
            else if (!isAttacking && comboCooldown <= 0f)
            {
                if (UseStamina(attackStaminaCost))
                    PerformMeleeAttack();
            }
        }
        
        // Fireball (Q)
        if (Input.GetKeyDown(KeyCode.Q) && !isAttacking && !isRolling && !isStaggered)
        {
            if (UseMana(fireballManaCost))
            {
                CastFireball();
                attackTimer = attackCooldown;
            }
        }
        
        // Use Potion (R key)
        if (Input.GetKeyDown(KeyCode.R) && !isHealing && !isRolling && !isAttacking && !isStaggered && controller.isGrounded)
        {
            if (currentPotions > 0 && currentHealth < maxHealth)
                StartHealing();
            else if (currentPotions <= 0)
                StartEmptyPotion();
        }
        
        // Jump (F key)
        if (Input.GetKeyDown(KeyCode.F) && controller.isGrounded && !isJumping && !isAttacking && !isRolling && !isStaggered)
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
            if (spaceHoldTime <= tapThreshold && rollCooldownTimer <= 0f && controller.isGrounded && !isStaggered)
            {
                float h = Input.GetAxis("Horizontal");
                float v = Input.GetAxis("Vertical");
                
                if (Mathf.Abs(h) > 0.1f || Mathf.Abs(v) > 0.1f)
                {
                    // Always camera-relative roll, even during lock-on
                    Camera cam = Camera.main;
                    Vector3 rollDir;
                    if (cam != null)
                    {
                        Vector3 camForward = new Vector3(cam.transform.forward.x, 0, cam.transform.forward.z).normalized;
                        Vector3 camRight = new Vector3(cam.transform.right.x, 0, cam.transform.right.z).normalized;
                        rollDir = (camForward * v + camRight * h).normalized;
                    }
                    else
                    {
                        rollDir = transform.forward;
                    }
                    // Face roll direction
                    transform.rotation = Quaternion.LookRotation(rollDir);
                    StartRoll(rollDir);
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
            // Sprint check
            bool isSprinting = Input.GetKey(KeyCode.Space) && spaceHoldTime > tapThreshold;
            bool canSprint = isSprinting && UseStamina(sprintStaminaCost * Time.deltaTime);
            float actualSpeed = canSprint ? sprintSpeed : walkSpeed;
            
            // Slow down while healing
            if (isHealing) actualSpeed *= healMoveSpeedMultiplier;
            
            // ── Always camera-relative movement ──
            Camera cam = Camera.main;
            if (cam == null) { currentMoveSpeed = 0f; return; }
            
            Vector3 camForward = new Vector3(cam.transform.forward.x, 0, cam.transform.forward.z).normalized;
            Vector3 camRight = new Vector3(cam.transform.right.x, 0, cam.transform.right.z).normalized;
            Vector3 moveDir = (camForward * vertical + camRight * horizontal).normalized;
            
            float targetAngle = Mathf.Atan2(moveDir.x, moveDir.z) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, Mathf.LerpAngle(transform.eulerAngles.y, targetAngle, 10f * Time.deltaTime), 0);
            
            // Move
            controller.Move(moveDir * actualSpeed * Time.deltaTime);
            
            // Track speed for animator: 0=idle, 0.5=jog, 1.0=run
            currentMoveSpeed = canSprint ? 1.0f : 0.5f;
            if (isHealing) currentMoveSpeed *= healMoveSpeedMultiplier;
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
            velocity.y = -8f; // strong downward force to keep grounded
        }
        
        if (velocity.y < 0)
            velocity.y += gravity * fallMultiplier * Time.deltaTime;
        else
            velocity.y += gravity * Time.deltaTime;
        
        controller.Move(velocity * Time.deltaTime);
    }
    
    // ===== HEALING =====
    
    void StartHealing()
    {
        isHealing = true;
        healTimer = healDuration;
        
        if (animator != null)
        {
            animator.SetLayerWeight(upperBodyLayerIndex, 1f);
            animator.Play("Potion_Drink", upperBodyLayerIndex, 0f);
            // Use sharp walking on lower body instead of Potion_Drink (which leans back)
            animator.CrossFade("HealWalk", 0.15f, 0);
        }
        
        Invoke(nameof(ApplyHeal), 1.2f);
    }
    
    void StartEmptyPotion()
    {
        isHealing = true;
        healTimer = emptyPotionDuration;
        
        if (animator != null)
        {
            animator.SetLayerWeight(upperBodyLayerIndex, 1f);
            animator.Play("Potion_Empty", upperBodyLayerIndex, 0f);
            animator.CrossFade("HealWalk", 0.15f, 0);
        }
    }
    
    void ApplyHeal()
    {
        UsePotion();
    }
    
    // ===== ROLL =====
    
    void StartRoll(Vector3 direction)
    {
        if (!UseStamina(rollStaminaCost)) return;
        
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
        iframeTimer = rollIframeDuration;
        
        if (animator != null)
            animator.CrossFade("roll_forward", 0.05f);
    }
    
    void StartBackstep()
    {
        if (!UseStamina(rollStaminaCost * 0.5f)) return;
        
        if (isAttacking)
        {
            isAttacking = false;
            CancelInvoke(nameof(ResetAttackState));
            CancelInvoke(nameof(DealMeleeDamage));
        }
        
        isRolling = true;
        rollTimer = backstepDuration;
        rollCooldownTimer = rollCooldown;
        rollDirection = -transform.forward;
        
        if (animator != null)
            animator.CrossFade("move_step_back", 0.05f);
    }
    
    void HandleRoll()
    {
        rollTimer -= Time.deltaTime;
        if (iframeTimer > 0f) iframeTimer -= Time.deltaTime;
        
        if (rollTimer <= 0f)
        {
            isRolling = false;
            iframeTimer = 0f;
            if (animator != null)
                animator.CrossFade("Movement", 0.1f);
            return;
        }
    }
    
    // ===== COMBO ATTACK =====
    
    void PerformMeleeAttack()
    {
        if (comboStep >= comboAnims.Length)
        {
            comboStep = 0;
            comboCooldown = comboCooldownDuration;
            isAttacking = false;
            comboQueued = false;
            if (animator != null) animator.CrossFade("Movement", 0.2f);
            return;
        }
        
        isAttacking = true;
        
        // Face enemy if locked on
        if (IsLockedOn)
        {
            Vector3 dir = lockOnSystem.currentTarget.position - transform.position;
            dir.y = 0;
            if (dir.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.LookRotation(dir.normalized);
        }
        
        if (animator != null)
            animator.CrossFade(comboAnims[comboStep], 0.1f);
        
        // Activate weapon hitbox via timer (in case animation events are not set up)
        float clipDur = comboDurations[comboStep];
        float hitStart = clipDur * 0.25f;
        float hitEnd = clipDur * 0.55f;
        if (weaponHitbox != null)
        {
            Invoke(nameof(ActivateHitbox), hitStart);
            Invoke(nameof(DeactivateHitbox), hitEnd);
        }
        else
        {
            Invoke(nameof(DealMeleeDamage), hitStart);
        }
        
        // Set timer for ~75% of anim so combo chains faster
        currentAttackTimer = clipDur * 0.75f;
        
        comboStep++;
        comboResetTimer = comboWindow + comboDurations[comboStep - 1];
    }
    
    // ===== HIT REACTION =====
    
    public override void TakeDamage(float damage, Entity attacker = null)
    {
        if (isDead) return;
        
        // Roll i-frames — invincible during the first part of the roll
        if (isRolling && iframeTimer > 0f) return;
        
        base.TakeDamage(damage, attacker);
        
        if (!isDead && !isRolling)
        {
            isStaggered = true;
            staggerTimer = staggerDuration;
            
            if (isAttacking)
            {
                isAttacking = false;
                CancelInvoke(nameof(ResetAttackState));
                CancelInvoke(nameof(DealMeleeDamage));
            }
            
            if (animator != null)
                animator.CrossFade("Damage_Front_Small_ver_A", 0.05f);

            // Camera shake on hit — scales continuously with health
            float healthPercent = currentHealth / maxHealth;
            float mag = Mathf.Lerp(0.25f, 0.05f, healthPercent);
            float dur = Mathf.Lerp(0.3f, 0.15f, healthPercent);

            // Try Cinemachine camera first, fall back to CameraFollow
            var cmCam = FindAnyObjectByType<CinemachineLockOnCamera>();
            if (cmCam != null)
            {
                cmCam.Shake(mag, dur);
            }
            else
            {
                CameraFollow cam = Camera.main?.GetComponent<CameraFollow>();
                if (cam != null) cam.Shake(mag, dur);
            }
        }
    }
    
    // ===== HELPERS =====
    
    void DealMeleeDamage()
    {
        // Fallback: only used if no WeaponHitbox is assigned
        if (weaponHitbox != null) return;
        Collider[] hitEnemies = Physics.OverlapSphere(transform.position + transform.forward, attackRange);
        foreach (Collider col in hitEnemies)
        {
            Entity enemy = col.GetComponent<Entity>();
            if (enemy != null && enemy != this)
                enemy.TakeDamage(attackDamage, this);
        }
    }
    
    void ResetAttackState()
    {
        isAttacking = false;
        comboQueued = false;
        currentAttackTimer = 0f;
        if (animator != null && !isRolling && !isJumping)
            animator.CrossFade("Movement", 0.2f);
    }
    
    /// <summary>Called by animation event — enables weapon hitbox.</summary>
    public void ActivateHitbox()
    {
        if (weaponHitbox != null)
        {
            weaponHitbox.owner = this;
            weaponHitbox.Activate(attackDamage);
        }
    }
    
    /// <summary>Called by animation event — disables weapon hitbox.</summary>
    public void DeactivateHitbox()
    {
        if (weaponHitbox != null)
            weaponHitbox.Deactivate();
    }
    
    public void OpenDamageColliders() { ActivateHitbox(); }
    public void CloseDamageColliders() { DeactivateHitbox(); }
    
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

        isAttacking = false;
        isRolling = false;
        isHealing = false;
        isStaggered = false;
        CancelInvoke(nameof(ResetAttackState));
        CancelInvoke(nameof(DealMeleeDamage));

        if (animator != null)
            animator.CrossFade("Damage_Die", 0.1f);

        // Disable character controller to allow falling to ground
        if (controller != null)
            controller.enabled = false;

        StartCoroutine(RespawnCoroutine());
    }
    
    IEnumerator RespawnCoroutine()
    {
        yield return new WaitForSeconds(3f);
        
        GameManager.Instance.RespawnPlayer(gameObject);
        GameManager.Instance.RespawnEnemies();
        
        enabled = true;
    }
    
    void OnAnimatorMove()
    {
        if (animator == null) return;
        
        if (isAttacking || isRolling || isStaggered)
        {
            Vector3 rootMotion = animator.deltaPosition;
            
            // If attack clip has little/no root motion, push player forward
            if (isAttacking && rootMotion.magnitude < 0.001f)
            {
                rootMotion += transform.forward * attackLungeSpeed * Time.deltaTime;
            }
            
            // Roll: drive movement with rollSpeed in the chosen direction
            if (isRolling && rollDirection.sqrMagnitude > 0.01f)
            {
                rootMotion = rollDirection * rollSpeed * Time.deltaTime;
            }
            
            rootMotion.y = velocity.y * Time.deltaTime;
            controller.Move(rootMotion);
        }
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + transform.forward, attackRange);
    }
}

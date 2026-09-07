using UnityEngine;
using System.Collections;
using System.Collections.Generic;

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
    
    [Header("Weapon / Hand")]
    [Tooltip("Optional: assign the right hand bone directly if auto-detection fails.")]
    public Transform rightHandBone;
    public WeaponHitbox weaponHitbox;
    public Vector3 weaponPositionOffset;
    public Vector3 weaponRotationOffset;
    [Tooltip("Multiplies the final world size of the weapon. 1 = prefab size, 0.5 = half size, 2 = double size.")]
    public float weaponScaleMultiplier = 0.7f;

    private Vector3 weaponOriginalScale = Vector3.one;
    private bool weaponScaleCaptured = false;
    private float lastAppliedWeaponScale = -1f;
    private Transform weaponHandBone;
    private Quaternion weaponBaseRotation = Quaternion.identity;
    
    [Header("Lock-On")]
    public LockOnSystem lockOnSystem;
    
    [Header("Abilities")]
    public float fireballDamage = 30f;
    public float fireballManaCost = 25f;
    public GameObject fireballPrefab;
    public Transform castPoint;

    [Header("Progression")]
    public int souls = 0;
    public int playerLevel = 1;
    public int vigor = 10;
    public int mind = 10;
    public int endurance = 10;
    public int strength = 10;

    /// <summary>Souls needed for the next level-up.</summary>
    public int NextLevelCost => 50 + (playerLevel - 1) * 25;

    public void AddSouls(int amount)
    {
        if (amount <= 0) return;
        souls += amount;
        Debug.Log($"[PlayerController] Gained {amount} souls (total: {souls}).");
    }
    
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
    private float comboWindow = 0.4f;
    private float comboCooldown = 0f;
    private float comboCooldownDuration = 0.75f;
    
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

    protected virtual void Awake()
    {
        // Prevent duplicate WeaponMount/WeaponAttacher on the model from fighting with this script.
        DisableChildWeaponMounting();
    }

    void DisableChildWeaponMounting()
    {
        WeaponMount[] mounts = GetComponentsInChildren<WeaponMount>(true);
        foreach (WeaponMount m in mounts)
        {
            if (m != null && m.enabled)
            {
                m.enabled = false;
                Debug.Log($"[PlayerController] Disabled existing {m.GetType().Name} on {m.name} to avoid duplicate weapon mounting.");
            }
        }

        WeaponAttacher[] attachers = GetComponentsInChildren<WeaponAttacher>(true);
        foreach (WeaponAttacher a in attachers)
        {
            if (a != null && a.enabled)
            {
                a.enabled = false;
                Debug.Log($"[PlayerController] Disabled existing {a.GetType().Name} on {a.name} to avoid duplicate weapon mounting.");
            }
        }
    }
    
    protected override void Start()
    {
        base.Start();
        
        controller = GetComponent<CharacterController>();
        
        if (animator == null)
            animator = GetComponent<Animator>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
        
        if (animator != null)
            animator.applyRootMotion = true;
        
        if (castPoint == null)
        {
            GameObject cp = new GameObject("CastPoint");
            cp.transform.parent = transform;
            cp.transform.localPosition = new Vector3(0f, 1.5f, 0.5f);
            castPoint = cp.transform;
        }

        // Make sure the quick item bar and souls UI exist.
        if (GetComponent<QuickItemBar>() == null)
            gameObject.AddComponent<QuickItemBar>();
        if (GetComponent<SoulsUI>() == null)
            gameObject.AddComponent<SoulsUI>();

        // Delay one frame so the Animator/avatar and any spawned children are fully ready.
        StartCoroutine(TryMountWeaponDelayed());
    }

    System.Collections.IEnumerator TryMountWeaponDelayed()
    {
        // Wait two frames so the Animator/avatar and any spawned children are fully ready.
        yield return null;
        yield return null;
        TryMountWeapon();
    }
    
    protected override void Update()
    {
        base.Update();
        
        if (isDead) return;

        // Lock the weapon to the hand and live-apply offset/scale changes.
        if (weaponHandBone != null && weaponHitbox != null)
            ApplyWeaponTransform();

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
        
        // Use selected quick item (R key)
        if (Input.GetKeyDown(KeyCode.R) && !isHealing && !isRolling && !isAttacking && !isStaggered && controller.isGrounded)
        {
            QuickItemBar itemBar = GetComponent<QuickItemBar>();
            if (itemBar != null && !itemBar.SelectedIsFlask)
            {
                // Use whatever consumable is selected in the quick bar
                itemBar.UseSelectedConsumable(this);
            }
            else
            {
                // Default flask behaviour (drink animation + heal)
                if (currentPotions > 0 && currentHealth < maxHealth)
                    StartHealing();
                else if (currentPotions <= 0)
                    StartEmptyPotion();
            }
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
                    // no direction = roll forward
                    StartRoll(transform.forward);
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
            
            // move relative to camera
            Camera cam = Camera.main;
            Vector3 camForward, camRight;
            if (cam != null)
            {
                camForward = new Vector3(cam.transform.forward.x, 0, cam.transform.forward.z).normalized;
                camRight = new Vector3(cam.transform.right.x, 0, cam.transform.right.z).normalized;
            }
            else
            {
                // fall back to player forward if no cam
                camForward = new Vector3(transform.forward.x, 0, transform.forward.z).normalized;
                camRight = new Vector3(transform.right.x, 0, transform.right.z).normalized;
            }
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
        // Every slash costs stamina, including combo follow-ups
        if (!UseStamina(attackStaminaCost))
        {
            Debug.Log("[PlayerController] Not enough stamina to attack.");
            comboQueued = false;
            return;
        }

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
        // Active frames: slightly generous so the visual blade swipe lines up
        // with the hit window, without being so long that it double-hits.
        float hitStartPct = (comboStep == 0) ? 0.30f :
                            (comboStep == 1) ? 0.35f : 0.40f;
        float hitEndPct   = (comboStep == 0) ? 0.55f :
                            (comboStep == 1) ? 0.58f : 0.72f;
        float hitStart = clipDur * hitStartPct;
        float hitEnd = clipDur * hitEndPct;
        if (weaponHitbox != null)
        {
            Invoke(nameof(ActivateHitbox), hitStart);
            Invoke(nameof(DeactivateHitbox), hitEnd);
        }
        else
        {
            Invoke(nameof(DealMeleeDamage), hitStart);
        }
        
        // End the active attack earlier so the player can chain the next swing sooner.
        // The heavy final slash gets a slightly larger window because the anim is longer.
        float endFactor = (comboStep == 2) ? 0.65f : 0.55f;
        currentAttackTimer = clipDur * endFactor;
        
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

        // Release lock-on so the camera doesn't stay glued to the boss after respawn
        if (lockOnSystem != null)
            lockOnSystem.ReleaseLockOn();

        StartCoroutine(RespawnCoroutine());
    }
    
    IEnumerator RespawnCoroutine()
    {
        yield return new WaitForSeconds(3f);

        GameManager.Instance.RespawnPlayer(gameObject);
        GameManager.Instance.RespawnEnemies();

        // Reset all states, re-mount the weapon, and re-enable the controller.
        ResetAfterDeath();

        enabled = true;
    }

    /// <summary>
    /// Called after the GameManager respawns the player.
    /// Clears stuck animation layers, re-enables the CharacterController,
    /// and re-mounts the sword so the player doesn’t end up half-animated
    /// or weaponless on respawn.
    /// </summary>
    public void ResetAfterDeath()
    {
        isDead = false;
        isAttacking = false;
        isRolling = false;
        isHealing = false;
        isStaggered = false;
        isJumping = false;
        isFalling = false;
        comboQueued = false;
        comboStep = 0;
        comboCooldown = 0f;
        currentAttackTimer = 0f;
        healTimer = 0f;
        staggerTimer = 0f;
        velocity = Vector3.zero;

        if (animator != null)
        {
            // Make sure the upper-body layer is not stuck playing a drinking/empty potion
            animator.SetLayerWeight(upperBodyLayerIndex, 0f);
            animator.SetBool("IsAttacking", false);
            animator.SetBool("IsBlocking", false);
            animator.SetBool("IsJumping", false);
            animator.CrossFade("Movement", 0.2f);
        }

        if (controller != null)
            controller.enabled = true;

        // Re-mount the weapon. If it was already in place this is a no-op.
        StartCoroutine(TryMountWeaponDelayed());
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

    // ══════════════════════════════════════════════════════════════
    //  WEAPON AUTO-MOUNT
    //  If weaponHitbox references a prefab asset (not a scene
    //  instance), instantiate it and attach it to the right hand.
    // ══════════════════════════════════════════════════════════════
    void TryMountWeapon()
    {
        Debug.Log($"[PlayerController] TryMountWeapon called. weaponHitbox={(weaponHitbox == null ? "null" : weaponHitbox.name)} activeInHierarchy={(weaponHitbox != null ? weaponHitbox.gameObject.activeInHierarchy : false)}");

        // 1. If a weapon is already parented under the player, mount it.
        WeaponHitbox existing = GetComponentInChildren<WeaponHitbox>(true);
        if (existing != null)
        {
            Debug.Log($"[PlayerController] Found existing WeaponHitbox on player: {existing.name}. Mounting.");
            weaponHitbox = existing;
            MoveWeaponToHandAndOwn(weaponHitbox);
            return;
        }

        // 2. If weaponHitbox is assigned, use it (scene instance or prefab asset).
        if (weaponHitbox == null)
        {
            // No assigned weapon and none on the player. Try to find a sword mesh in children and add a WeaponHitbox.
            Transform foundSword = FindSwordMeshInPlayer();
            if (foundSword != null)
            {
                Debug.Log($"[PlayerController] Found sword mesh {foundSword.name} on player; adding WeaponHitbox and mounting.");
                WeaponHitbox wh = foundSword.gameObject.AddComponent<WeaponHitbox>();
                weaponHitbox = wh;
                MoveWeaponToHandAndOwn(wh);
                return;
            }

            // Still nothing — look for a loose sword in the scene that isn’t an enemy weapon.
            foundSword = FindSwordMeshInScene();
            if (foundSword != null)
            {
                Debug.Log($"[PlayerController] Found loose sword {foundSword.name} in scene; adding WeaponHitbox and mounting.");
                WeaponHitbox wh = foundSword.gameObject.AddComponent<WeaponHitbox>();
                weaponHitbox = wh;
                MoveWeaponToHandAndOwn(wh);
                return;
            }

            Debug.LogWarning("[PlayerController] No weaponHitbox assigned, no WeaponHitbox on player, and no sword mesh found. Sword cannot be mounted.");
            return;
        }

        // 3. Active scene instance assigned but not yet under the hand — mount it.
        if (weaponHitbox.gameObject.activeInHierarchy)
        {
            Debug.Log($"[PlayerController] weaponHitbox is an active scene instance. Mounting.");
            MoveWeaponToHandAndOwn(weaponHitbox);
            return;
        }

        // 4. Otherwise weaponHitbox is a prefab asset — instantiate it.
        GameObject prefab = weaponHitbox.gameObject;
        Debug.Log($"[PlayerController] weaponHitbox is a prefab asset. Instantiating {prefab.name}.");

        Transform hand = FindRightHandBone();
        if (hand == null)
        {
            Debug.LogWarning("[PlayerController] No right hand bone found; cannot mount weapon. Add a WeaponAttacher/WeaponMount or assign a hand bone.");
            LogHandBones();
            return;
        }

        GameObject weaponInstance = Instantiate(prefab);

        WeaponHitbox newHitbox = weaponInstance.GetComponentInChildren<WeaponHitbox>(true);
        if (newHitbox == null)
        {
            // Prefab has no WeaponHitbox script — add one to the root so it can deal damage.
            Debug.Log($"[PlayerController] Prefab {prefab.name} has no WeaponHitbox; adding one to the instance root.");
            newHitbox = weaponInstance.AddComponent<WeaponHitbox>();
        }

        // If the WeaponHitbox is on a child, move it to the root so the whole sword is mounted.
        if (newHitbox.transform != weaponInstance.transform && newHitbox.transform.IsChildOf(weaponInstance.transform))
        {
            Debug.Log($"[PlayerController] WeaponHitbox is a child of the prefab; moving it to the root so the whole sword is mounted.");
            WeaponHitbox rootHitbox = weaponInstance.AddComponent<WeaponHitbox>();

            // Remove any colliders / rigidbodies the child WeaponHitbox may have added.
            foreach (Collider c in newHitbox.GetComponents<Collider>())
                Destroy(c);
            Rigidbody rb = newHitbox.GetComponent<Rigidbody>();
            if (rb != null) Destroy(rb);

            Destroy(newHitbox);
            newHitbox = rootHitbox;
        }

        if (newHitbox != null)
        {
            weaponHitbox = newHitbox;
            MoveWeaponToHandAndOwn(newHitbox);
            Debug.Log($"[PlayerController] Mounted {prefab.name} to {hand.name}.");
        }
    }

    void MoveWeaponToHandAndOwn(WeaponHitbox hitbox)
    {
        if (hitbox == null) return;

        hitbox.owner = this;

        Transform hand = FindRightHandBone();
        if (hand == null)
        {
            Debug.LogWarning("[PlayerController] Could not find right hand bone to position weapon.");
            LogHandBones();
            return;
        }

        // Mount the root of the weapon, not just the WeaponHitbox component, so the
        // entire sword (mesh + collider) ends up in the hand.
        Transform weaponRoot = (hitbox.transform.root != transform) ? hitbox.transform.root : hitbox.transform;

        // Only reparent if it's not already on the hand.
        if (weaponRoot.parent != hand)
        {
            if (!weaponScaleCaptured)
            {
                weaponOriginalScale = weaponRoot.localScale;
                weaponScaleCaptured = true;
            }

            weaponRoot.SetParent(hand, false);
            weaponRoot.localPosition = weaponPositionOffset;
            weaponRoot.localRotation = Quaternion.Euler(weaponRotationOffset);

            Debug.Log($"[PlayerController] Moved {weaponRoot.name} to {hand.name}.");
        }
        else if (!weaponScaleCaptured)
        {
            // Weapon was already on the hand — treat its current scale relative to hand
            // as the base. Reconstruct an approximate original from world size.
            weaponOriginalScale = weaponRoot.localScale;
            Vector3 hs = hand.lossyScale;
            weaponOriginalScale = new Vector3(
                weaponOriginalScale.x * hs.x, weaponOriginalScale.y * hs.y, weaponOriginalScale.z * hs.z);
            weaponScaleCaptured = true;
        }

        weaponHandBone = hand;
        ComputeWeaponBaseRotation(weaponRoot);
        ApplyWeaponTransform();

        // Debug info to help find the sword if it's not visible.
        MeshRenderer renderer = weaponRoot.GetComponentInChildren<MeshRenderer>();
        MeshFilter filter = weaponRoot.GetComponentInChildren<MeshFilter>();
        Debug.Log($"[PlayerController] Weapon '{weaponRoot.name}' parent: {weaponRoot.parent?.name}, " +
                  $"world pos: {weaponRoot.position}, local pos: {weaponRoot.localPosition}, " +
                  $"local rot: {weaponRoot.localRotation.eulerAngles}, lossy scale: {weaponRoot.lossyScale}, " +
                  $"renderer enabled: {(renderer != null ? renderer.enabled.ToString() : "NO MESHRENDERER")}, " +
                  $"mesh: {(filter != null && filter.sharedMesh != null ? filter.sharedMesh.name : "NO MESH")}");
    }

    /// <summary>
    /// Counter-scales the weapon against the hand bone's lossy scale
    /// (Synty models are authored at 1 unit = 1 cm, so the hand bone is ~0.01)
    /// and applies weaponScaleMultiplier. Re-runs whenever the multiplier changes.
    /// </summary>
    void ApplyWeaponTransform()
    {
        if (weaponHitbox == null || weaponHandBone == null || !weaponScaleCaptured) return;

        // Scale the root of the weapon so the mesh and collider scale together.
        Transform weaponRoot = (weaponHitbox.transform.root != transform) ? weaponHitbox.transform.root : weaponHitbox.transform;

        // Lock position and rotation relative to the hand. This also lets the user
        // tweak weaponPositionOffset / weaponRotationOffset in the inspector and see it live.
        weaponRoot.localPosition = weaponPositionOffset;
        weaponRoot.localRotation = weaponBaseRotation * Quaternion.Euler(weaponRotationOffset);

        Vector3 handScale = weaponHandBone.lossyScale;
        const float minScale = 0.0001f;
        weaponRoot.localScale = new Vector3(
            weaponOriginalScale.x * weaponScaleMultiplier / Mathf.Max(handScale.x, minScale),
            weaponOriginalScale.y * weaponScaleMultiplier / Mathf.Max(handScale.y, minScale),
            weaponOriginalScale.z * weaponScaleMultiplier / Mathf.Max(handScale.z, minScale));

        lastAppliedWeaponScale = weaponScaleMultiplier;
    }

    /// <summary>
    /// Guesses the base rotation so the sword's longest dimension (blade) points
    /// along the hand's back (-Z in hand local space). For most Synty/hero
    /// characters the idle hand points its local +Z behind the player, so this
    /// looks correct; use weaponRotationOffset to tweak if your rig differs.
    /// </summary>
    void ComputeWeaponBaseRotation(Transform weaponRoot)
    {
        weaponBaseRotation = Quaternion.identity;

        MeshFilter mf = weaponRoot.GetComponentInChildren<MeshFilter>();
        if (mf == null || mf.sharedMesh == null) return;

        Bounds b = mf.sharedMesh.bounds;
        Vector3 size = b.size;
        if (size.sqrMagnitude < 0.0001f) return;

        // Longest axis = blade axis
        int longAxis = 0;
        if (size.y > size.x) longAxis = 1;
        if (size.z > size[longAxis]) longAxis = 2;

        // The blade points from the handle toward the tip. The AABB center is
        // biased toward the longer end; use that as a starting direction.
        Vector3 bladeDir = Vector3.zero;
        bladeDir[longAxis] = Mathf.Sign(b.center[longAxis]);
        if (Mathf.Approximately(bladeDir[longAxis], 0f)) bladeDir[longAxis] = 1f;

        // Point the blade along the hand's -Z. For this rig, +Z was backward,
        // so -Z is the correct forward direction for the weapon.
        weaponBaseRotation = Quaternion.FromToRotation(bladeDir, Vector3.back);

        Debug.Log($"[PlayerController] Auto-aligned sword '{weaponRoot.name}' blade dir {bladeDir:F2} to hand back. " +
                  $"Base rotation euler: {weaponBaseRotation.eulerAngles:F2}. Use 'Weapon Rotation Offset' to tweak.");
    }

    Transform FindRightHandBone()
    {
        // Manual override — use this if auto-detection keeps picking the wrong bone.
        if (rightHandBone != null)
        {
            Debug.Log($"[PlayerController] Using manually assigned right hand bone: {rightHandBone.name}");
            return rightHandBone;
        }

        // Humanoid avatars
        if (animator != null && animator.isHuman)
        {
            Transform hand = animator.GetBoneTransform(HumanBodyBones.RightHand);
            if (hand != null)
            {
                Debug.Log($"[PlayerController] Humanoid right hand bone found: {hand.name}");
                return hand;
            }
        }

        // Generic / Synty / Mixamo / UE fallbacks — exact names first
        string[] handNames = {
            "Hand_R", "RightHand", "Right_Hand", "R_Hand", "hand_r", "hand.R",
            "mixamorig:RightHand", "Bip01 R Hand", "Right wrist", "right_hand",
            "J_Bip_R_Hand", "Wrist_R", "R_Wrist", "Right_Wrist", "RightPalm",
            "R_Palm", "Palm_R", "Right Finger", "R_Finger", "Right Hand",
            "R Hand", "RightHand_01", "Right_Hand_01"
        };

        foreach (string name in handNames)
        {
            Transform found = FindBoneRecursive(transform, name);
            if (found != null)
            {
                Debug.Log($"[PlayerController] Right hand bone found by name: {found.name}");
                return found;
            }
        }

        // Keyword fallbacks (must contain "right" and a hand-related word)
        string[][] keywordSets = {
            new[] { "right", "hand" },
            new[] { "right", "wrist" },
            new[] { "right", "palm" },
            new[] { "right", "finger" },
            new[] { "r", "hand" }       // for "R Hand", "R_Hand_Glove" etc.
        };

        foreach (string[] set in keywordSets)
        {
            Transform keyword = FindHandByKeywords(set);
            if (keyword != null)
            {
                Debug.Log($"[PlayerController] Right hand bone found by keyword ({string.Join(",", set)}): {keyword.name}");
                return keyword;
            }
        }

        Debug.LogWarning("[PlayerController] No right hand bone could be found. Assign one to the 'Right Hand Bone' field on PlayerController, or give the hand bone a standard name.");
        return null;
    }

    Transform FindHandByKeywords(params string[] keywords)
    {
        foreach (Transform t in GetComponentsInChildren<Transform>(true))
        {
            string lower = t.name.ToLower();
            bool all = true;
            foreach (string kw in keywords)
                if (!lower.Contains(kw)) { all = false; break; }
            if (all) return t;
        }
        return null;
    }

    Transform FindSwordMeshInPlayer()
    {
        string[] swordNames = { "sword", "katana", "blade", "wep", "weapon" };
        foreach (Transform t in GetComponentsInChildren<Transform>(true))
        {
            string lower = t.name.ToLower();
            foreach (string s in swordNames)
            {
                if (lower.Contains(s))
                {
                    MeshFilter mf = t.GetComponent<MeshFilter>();
                    MeshRenderer mr = t.GetComponent<MeshRenderer>();
                    if (mf != null || mr != null)
                        return t;
                }
            }
        }
        return null;
    }

    Transform FindSwordMeshInScene()
    {
        string[] swordNames = { "sword", "katana", "blade", "wep", "weapon" };
        Transform best = null;
        float bestDist = float.MaxValue;

        foreach (MeshRenderer mr in FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
        {
            if (mr == null) continue;
            Transform t = mr.transform;
            Transform root = t.root;

            // Don’t steal an enemy’s weapon.
            if (root.CompareTag("Enemy")) continue;

            string lower = t.name.ToLower();
            foreach (string s in swordNames)
            {
                if (lower.Contains(s))
                {
                    float d = Vector3.Distance(transform.position, t.position);
                    if (d < bestDist)
                    {
                        bestDist = d;
                        best = t;
                    }
                    break;
                }
            }
        }

        return best;
    }

    void LogHandBones()
    {
        // Print every child transform name that might be a hand, to help debug skeleton names.
        List<string> candidates = new List<string>();
        CollectBoneNames(transform, "hand", "right", "r", candidates);
        if (candidates.Count > 0)
        {
            Debug.Log("[PlayerController] Possible right hand bones found: " + string.Join(", ", candidates));
        }
        else
        {
            Debug.Log("[PlayerController] No hand-like bones found under player.");
        }
    }

    void CollectBoneNames(Transform parent, params object[] args)
    {
        // args: keywords and a List<string> result list at the end.
        List<string> result = args[args.Length - 1] as List<string>;
        string[] keywords = new string[args.Length - 1];
        for (int i = 0; i < args.Length - 1; i++)
            keywords[i] = args[i].ToString().ToLower();

        string lowerName = parent.name.ToLower();
        bool allMatch = true;
        foreach (string kw in keywords)
        {
            if (!lowerName.Contains(kw))
            {
                allMatch = false;
                break;
            }
        }

        if (allMatch)
            result.Add(parent.name);

        for (int i = 0; i < parent.childCount; i++)
            CollectBoneNames(parent.GetChild(i), args);
    }

    Transform FindBoneRecursive(Transform parent, string boneName)
    {
        if (parent.name.Equals(boneName, System.StringComparison.OrdinalIgnoreCase))
            return parent;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform found = FindBoneRecursive(parent.GetChild(i), boneName);
            if (found != null) return found;
        }
        return null;
    }
}

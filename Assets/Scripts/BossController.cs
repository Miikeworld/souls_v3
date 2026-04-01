using UnityEngine;
using System.Collections;

public class BossController : Entity
{
    [Header("Boss Settings")]
    public string bossName = "Ancient Guardian";
    public float detectionRange = 15f;
    public float attackRange = 3f;
    public float rotationSpeed = 5f;
    
    [Header("Movement")]
    public float moveSpeed = 3f;
    public float chargeSpeed = 8f;
    public float chargeDuration = 1.5f;
    public float stopDistance = 1f;
    
    [Header("Pathfinding")]
    public float obstacleCheckDistance = 2f;
    public float avoidanceAngle = 45f;
    public LayerMask obstacleLayer = -1; // All layers by default
    
    [Header("Combat")]
    public float meleeDamage = 25f;
    public float heavyDamage = 40f;
    public float attackCooldown = 2f;
    public float heavyAttackCooldown = 4f;
    
    [Header("Special Abilities")]
    public GameObject groundSlamPrefab;
    public GameObject projectilePrefab;
    public Transform projectileSpawnPoint;
    public float groundSlamCooldown = 8f;
    public float projectileCooldown = 6f;
    public int projectileCount = 3;
    public float projectileSpread = 30f;
    
    [Header("Phase System")]
    public float phase2HealthThreshold = 0.5f;    // 50% HP
    public float phase3HealthThreshold = 0.25f;   // 25% HP
    public float phase4HealthThreshold = 0f;       // 0% HP (revival)
    public bool isInPhase2 = false;
    public bool isInPhase3 = false;
    public bool isInPhase4 = false;
    public bool isRevived = false;
    
    [Header("Phase 4 - Second Life")]
    public float revivalHealthPercentage = 0.5f; // Revive with 50% health
    public float maxHealthPhase4 = 500f; // Second life health pool
    
    [Header("Visual Effects")]
    public GameObject[] phaseEffects;
    public AudioClip[] attackSounds;
    public AudioClip[] hurtSounds;
    public AudioClip deathSound;
    
    // Private variables
    private Transform player;
    private Animator animator;
    private AudioSource audioSource;
    private CharacterController controller;
    
    // Pathfinding
    private float pathUpdateTimer = 0f;
    
    // Timers and states
    private float attackTimer = 0f;
    private float heavyAttackTimer = 0f;
    private float groundSlamTimer = 0f;
    private float projectileTimer = 0f;
    private bool isAttacking = false;
        private bool isEnraged = false;
    
    // AI States
    private enum BossState { Idle, Pursuing, Attacking, SpecialAbility, Stunned, PhaseTransition }
    private BossState currentState = BossState.Idle;
    
    // Phase states
    private enum BossPhase { Phase1_Mage, Phase2_Blade, Phase3_Samurai, Phase4_LastResort }
    private BossPhase currentPhase = BossPhase.Phase1_Mage;
    
    protected override void Start()
    {
        base.Start();
        
        player = GameObject.FindGameObjectWithTag("Player").transform;
        animator = GetComponent<Animator>();
        controller = GetComponent<CharacterController>();
        
        // Add audio source if missing
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
        
        maxHealth = 1000f; // Total boss health
        currentHealth = maxHealth;
        InvokeResourceEvents();
    }
    
    protected override void Update()
    {
        base.Update();
        
        if (isDead) return;
        
        UpdateTimers();
        CheckPhaseTransitions();
        CheckForPlayer();
        
        switch (currentState)
        {
            case BossState.Idle:
                HandleIdle();
                break;
            case BossState.Pursuing:
                PursuePlayer();
                break;
            case BossState.Attacking:
                HandleAttack();
                break;
            case BossState.SpecialAbility:
                HandleSpecialAbility();
                break;
            case BossState.PhaseTransition:
                // Handled by phase transition coroutine
                break;
        }
        
        // Update animation
        if (animator != null)
        {
            animator.SetFloat("Speed", controller.velocity.magnitude);
            animator.SetBool("IsAttacking", isAttacking);
        }
    }
    
    private void UseSpecialAbility()
    {
        // This method is called when in SpecialAbility state
        // The actual ability execution is handled by coroutines
        // State will be set back to Pursuing in the coroutines
    }
    
    private void UpdateTimers()
    {
        if (attackTimer > 0) attackTimer -= Time.deltaTime;
        if (heavyAttackTimer > 0) heavyAttackTimer -= Time.deltaTime;
        if (groundSlamTimer > 0) groundSlamTimer -= Time.deltaTime;
        if (projectileTimer > 0) projectileTimer -= Time.deltaTime;
    }
    
    private void CheckPhaseTransitions()
    {
        float healthPercent = GetHealthPercent();
        
        // Phase 2 transition (50% HP)
        if (!isInPhase2 && healthPercent <= phase2HealthThreshold && currentPhase == BossPhase.Phase1_Mage)
        {
            StartCoroutine(TransitionToPhase2());
        }
        
        // Phase 3 transition (25% HP)
        if (!isInPhase3 && healthPercent <= phase3HealthThreshold && currentPhase == BossPhase.Phase2_Blade)
        {
            StartCoroutine(TransitionToPhase3());
        }
        
        // Phase 4 transition (0% HP - revival)
        if (!isInPhase4 && healthPercent <= phase4HealthThreshold && currentPhase == BossPhase.Phase3_Samurai && !isRevived)
        {
            StartCoroutine(TransitionToPhase4());
        }
    }
    
    float GetHealthPercent()
    {
        if (isRevived)
            return currentHealth / maxHealthPhase4;
        else
            return currentHealth / maxHealth;
    }
    
    // Phase Transition Methods
    IEnumerator TransitionToPhase2()
    {
        Debug.Log("Transitioning to Phase 2: Desperate Blade");
        currentState = BossState.PhaseTransition;
        isAttacking = true;
        
        // Play transition effects
        PlayPhaseTransitionEffect();
        if (audioSource != null && deathSound != null)
            audioSource.PlayOneShot(deathSound);
            
        yield return new WaitForSeconds(2f);
        
        // Update phase state
        currentPhase = BossPhase.Phase2_Blade;
        isInPhase2 = true;
        
        // Update visuals (you can swap models here)
        UpdateBossVisuals("Phase2_Blade");
        
        // Reset combat timers
        attackTimer = 0f;
        heavyAttackTimer = 0f;
        
        isAttacking = false;
        currentState = BossState.Pursuing;
        
        Debug.Log("Phase 2 transition complete");
    }
    
    IEnumerator TransitionToPhase3()
    {
        Debug.Log("Transitioning to Phase 3: Abyssal Samurai");
        currentState = BossState.PhaseTransition;
        isAttacking = true;
        
        // Play transition effects
        PlayPhaseTransitionEffect();
        if (audioSource != null && deathSound != null)
            audioSource.PlayOneShot(deathSound);
            
        yield return new WaitForSeconds(2f);
        
        // Update phase state
        currentPhase = BossPhase.Phase3_Samurai;
        isInPhase3 = true;
        
        // Update visuals (you can swap models here)
        UpdateBossVisuals("Phase3_Samurai");
        
        // Increase movement speed for samurai phase
        moveSpeed *= 1.5f;
        
        // Reset combat timers
        attackTimer = 0f;
        heavyAttackTimer = 0f;
        
        isAttacking = false;
        currentState = BossState.Pursuing;
        
        Debug.Log("Phase 3 transition complete");
    }
    
    IEnumerator TransitionToPhase4()
    {
        Debug.Log("Transitioning to Phase 4: Tyrant's Last Resort");
        currentState = BossState.PhaseTransition;
        isAttacking = true;
        
        // Play transition effects
        PlayPhaseTransitionEffect();
        if (audioSource != null && deathSound != null)
            audioSource.PlayOneShot(deathSound);
            
        yield return new WaitForSeconds(3f);
        
        // Revive with second life
        isRevived = true;
        currentPhase = BossPhase.Phase4_LastResort;
        isInPhase4 = true;
        
        // Set new health pool
        currentHealth = maxHealthPhase4 * revivalHealthPercentage;
        InvokeResourceEvents();
        
        // Update visuals (you can swap models here)
        UpdateBossVisuals("Phase4_LastResort");
        
        // Reduce movement speed for final phase
        moveSpeed *= 0.7f;
        
        // Reset combat timers
        attackTimer = 0f;
        heavyAttackTimer = 0f;
        
        isAttacking = false;
        currentState = BossState.Pursuing;
        
        Debug.Log("Phase 4 transition complete - Boss revived!");
    }
    
    private void EnterPhase2()
    {
        Debug.Log("Entering Phase 2: Desperate Blade");
        currentState = BossState.PhaseTransition;
        isAttacking = true;
        
        // Play transition effects
        PlayPhaseTransitionEffect();
        if (audioSource != null && deathSound != null)
            audioSource.PlayOneShot(deathSound);
            
        StartCoroutine(PhaseTransitionCoroutine(() => {
            currentPhase = BossPhase.Phase2_Blade;
            isInPhase2 = true;
            
            // Update visuals
            UpdateBossVisuals("Phase2_Blade");
            
            // Reset combat timers
            attackTimer = 0f;
            heavyAttackTimer = 0f;
            
            isAttacking = false;
            currentState = BossState.Pursuing;
            
            Debug.Log("Phase 2 transition complete");
        }, 2f));
    }
    
    private void EnterPhase3()
    {
        Debug.Log("Entering Phase 3: Abyssal Samurai");
        currentState = BossState.PhaseTransition;
        isAttacking = true;
        
        // Play transition effects
        PlayPhaseTransitionEffect();
        if (audioSource != null && deathSound != null)
            audioSource.PlayOneShot(deathSound);
            
        StartCoroutine(PhaseTransitionCoroutine(() => {
            currentPhase = BossPhase.Phase3_Samurai;
            isInPhase3 = true;
            
            // Update visuals
            UpdateBossVisuals("Phase3_Samurai");
            
            // Increase movement speed for samurai phase
            moveSpeed *= 1.5f;
            
            // Reset combat timers
            attackTimer = 0f;
            heavyAttackTimer = 0f;
            
            isAttacking = false;
            currentState = BossState.Pursuing;
            
            Debug.Log("Phase 3 transition complete");
        }, 2f));
    }
    
    IEnumerator PhaseTransitionCoroutine(System.Action onComplete, float delay)
    {
        yield return new WaitForSeconds(delay);
        onComplete?.Invoke();
    }
    
    private void PlayPhaseTransitionEffect()
    {
        // Spawn phase transition effect
        if (phaseEffects != null && phaseEffects.Length > 0)
        {
            int effectIndex = Mathf.Clamp((int)currentPhase, 0, phaseEffects.Length - 1);
            if (phaseEffects[effectIndex] != null)
            {
                Instantiate(phaseEffects[effectIndex], transform.position, Quaternion.identity);
            }
        }
    }
    
    void UpdateBossVisuals(string phaseName)
    {
        // You can implement model swapping here
        // For now, just log the phase change
        Debug.Log("Boss visuals updated to: " + phaseName);
        
        // Example: Swap models
        // foreach (Transform child in transform)
        // {
        //     if (child.name.Contains(phaseName))
        //         child.gameObject.SetActive(true);
        //     else
        //         child.gameObject.SetActive(false);
        // }
    }
    
    private void HandleIdle()
    {
        // Idle behavior
        if (animator != null)
            animator.SetFloat("Speed", 0f);
    }
    
    private void HandleAttack()
    {
        // Basic attack handling
        if (attackTimer <= 0 && !isAttacking)
        {
            StartCoroutine(MeleeAttack());
        }
    }
    
    private void HandleSpecialAbility()
    {
        // Special ability handling
        // This will call the appropriate phase-specific abilities
    }
    
    // Phase 1 Attack Methods (Grand Mage)
    void PerformFireballBarrage()
    {
        if (projectileTimer > 0) return;
        
        currentState = BossState.SpecialAbility;
        StartCoroutine(FireballBarrageAttack());
    }
    
    IEnumerator FireballBarrageAttack()
    {
        isAttacking = true;
        
        // Create multiple fireballs in spread
        for (int i = 0; i < projectileCount; i++)
        {
            float spreadAngle = -projectileSpread + (projectileSpread * 2f * i / (projectileCount - 1));
            Vector3 spreadDirection = Quaternion.Euler(0, spreadAngle, 0) * transform.forward;
            
            if (projectilePrefab != null && projectileSpawnPoint != null)
            {
                GameObject fireball = Instantiate(projectilePrefab, projectileSpawnPoint.position, Quaternion.LookRotation(spreadDirection));
                Projectile projectile = fireball.GetComponent<Projectile>();
                if (projectile != null)
                {
                    projectile.damage = meleeDamage;
                    projectile.owner = this;
                }
            }
            
            yield return new WaitForSeconds(0.1f);
        }
        
        projectileTimer = projectileCooldown;
        isAttacking = false;
        currentState = BossState.Pursuing;
    }
    
    void PerformAbyssalOrb()
    {
        if (projectileTimer > 0) return;
        
        currentState = BossState.SpecialAbility;
        StartCoroutine(AbyssalOrbAttack());
    }
    
    IEnumerator AbyssalOrbAttack()
    {
        isAttacking = true;
        
        // Charge up
        if (animator != null)
            animator.SetTrigger("Charge");
            
        yield return new WaitForSeconds(1f);
        
        // Fire slow tracking orb
        if (projectilePrefab != null && projectileSpawnPoint != null)
        {
            GameObject orb = Instantiate(projectilePrefab, projectileSpawnPoint.position, Quaternion.identity);
            Projectile projectile = orb.GetComponent<Projectile>();
            if (projectile != null)
            {
                projectile.damage = heavyDamage;
                projectile.owner = this;
                // Make it track player (you'd need to modify Projectile class for tracking)
            }
        }
        
        projectileTimer = projectileCooldown * 1.5f; // Longer cooldown for powerful attack
        isAttacking = false;
        currentState = BossState.Pursuing;
    }
    
    void PerformTeleportEvade()
    {
        if (player == null) return;
        
        float distance = Vector3.Distance(transform.position, player.position);
        if (distance > 5f) return; // Only teleport if player is close
        
        currentState = BossState.SpecialAbility;
        StartCoroutine(TeleportEvadeAttack());
    }
    
    IEnumerator TeleportEvadeAttack()
    {
        isAttacking = true;
        
        // Teleport effect
        PlayPhaseTransitionEffect();
        
        yield return new WaitForSeconds(0.3f);
        
        // Calculate teleport position (away from player)
        Vector3 teleportDirection = (transform.position - player.position).normalized;
        Vector3 teleportPosition = transform.position + teleportDirection * 8f;
        
        // Teleport
        transform.position = teleportPosition;
        
        yield return new WaitForSeconds(0.5f);
        
        isAttacking = false;
        currentState = BossState.Pursuing;
    }
    
    void PerformMagicShield()
    {
        currentState = BossState.SpecialAbility;
        StartCoroutine(MagicShieldDefense());
    }
    
    IEnumerator MagicShieldDefense()
    {
        isAttacking = true;
        
        // Activate shield (you'd need to implement shield visual)
        Debug.Log("Magic Shield activated - temporary invulnerability");
        
        // Make boss temporarily invulnerable
        // You could set a flag here and check it in TakeDamage
        
        yield return new WaitForSeconds(2f);
        
        Debug.Log("Magic Shield deactivated");
        isAttacking = false;
        currentState = BossState.Pursuing;
    }
    
    // Phase 2 Attack Methods (Desperate Blade)
    void PerformMagicSwordCombo()
    {
        if (attackTimer > 0) return;
        
        currentState = BossState.Attacking;
        StartCoroutine(MagicSwordComboAttack());
    }
    
    IEnumerator MagicSwordComboAttack()
    {
        isAttacking = true;
        attackTimer = attackCooldown;
        
        // 3-hit combo with magical blade
        for (int i = 0; i < 3; i++)
        {
            if (animator != null)
                animator.SetTrigger("Attack" + (i + 1));
                
            yield return new WaitForSeconds(0.3f);
            
            // Deal damage for each hit
            DealMeleeDamage(meleeDamage * (i == 1 ? 1.2f : 1f)); // Second hit is stronger
        }
        
        yield return new WaitForSeconds(0.5f);
        
        isAttacking = false;
        currentState = BossState.Pursuing;
    }
    
    void PerformArcaneWave()
    {
        if (heavyAttackTimer > 0) return;
        
        currentState = BossState.SpecialAbility;
        StartCoroutine(ArcaneWaveAttack());
    }
    
    IEnumerator ArcaneWaveAttack()
    {
        isAttacking = true;
        heavyAttackTimer = heavyAttackCooldown;
        
        // Charge up
        if (animator != null)
            animator.SetTrigger("Charge");
            
        yield return new WaitForSeconds(0.8f);
        
        // Create ground wave
        if (groundSlamPrefab != null)
        {
            Vector3 wavePosition = transform.position + transform.forward * 2f;
            GameObject wave = Instantiate(groundSlamPrefab, wavePosition, Quaternion.identity);
            
            // Wave moves forward (you'd need to implement wave movement script)
            Destroy(wave, 3f);
        }
        
        // Deal damage in arc
        Collider[] hitColliders = Physics.OverlapSphere(transform.position + transform.forward * 3f, 4f);
        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.CompareTag("Player"))
            {
                Entity playerEntity = hitCollider.GetComponent<Entity>();
                if (playerEntity != null)
                {
                    playerEntity.TakeDamage(meleeDamage * 1.5f, this);
                }
            }
        }
        
        yield return new WaitForSeconds(1f);
        
        isAttacking = false;
        currentState = BossState.Pursuing;
    }
    
    void PerformLeapingStrike()
    {
        if (player == null) return;
        
        float distance = Vector3.Distance(transform.position, player.position);
        if (distance > 8f) return; // Only leap if at medium range
        
        if (heavyAttackTimer > 0) return;
        
        currentState = BossState.SpecialAbility;
        StartCoroutine(LeapingStrikeAttack());
    }
    
    IEnumerator LeapingStrikeAttack()
    {
        isAttacking = true;
        heavyAttackTimer = heavyAttackCooldown;
        
        // Jump towards player
        Vector3 jumpTarget = player.position;
        Vector3 jumpDirection = (jumpTarget - transform.position).normalized;
        jumpDirection.y = 0;
        
        // Perform leap
        float leapDuration = 0.6f;
        float leapHeight = 3f;
        Vector3 startPos = transform.position;
        Vector3 endPos = jumpTarget;
        
        for (float t = 0; t < leapDuration; t += Time.deltaTime)
        {
            // Arc trajectory
            float height = Mathf.Sin(t / leapDuration * Mathf.PI) * leapHeight;
            Vector3 currentPos = Vector3.Lerp(startPos, endPos, t / leapDuration);
            currentPos.y = Mathf.Max(startPos.y, endPos.y) + height;
            
            transform.position = currentPos;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(jumpDirection), rotationSpeed * Time.deltaTime);
            
            yield return null;
        }
        
        // Overhead slam impact
        if (animator != null)
            animator.SetTrigger("Slam");
            
        // Create shockwave
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, 5f);
        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.CompareTag("Player"))
            {
                Entity playerEntity = hitCollider.GetComponent<Entity>();
                if (playerEntity != null)
                {
                    playerEntity.TakeDamage(heavyDamage * 1.8f, this); // High damage
                }
            }
        }
        
        yield return new WaitForSeconds(0.8f);
        
        isAttacking = false;
        currentState = BossState.Pursuing;
    }
    
    void PerformEnchantedParry()
    {
        // This would require player attack detection
        // For now, implement as a timed counter-attack
        if (heavyAttackTimer > 0) return;
        
        currentState = BossState.SpecialAbility;
        StartCoroutine(EnchantedParryCounter());
    }
    
    IEnumerator EnchantedParryCounter()
    {
        isAttacking = true;
        heavyAttackTimer = heavyAttackCooldown;
        
        // Parry stance (short window)
        Debug.Log("Enchanted Parry - counter window open");
        
        yield return new WaitForSeconds(0.3f);
        
        // If player attacks during this window, counter (you'd need player attack detection)
        // For now, just do a powerful counter-attack
        
        if (animator != null)
            animator.SetTrigger("Counter");
            
        yield return new WaitForSeconds(0.5f);
        
        // Deal massive counter damage
        DealMeleeDamage(heavyDamage * 2f); // Very high damage on successful parry
        
        yield return new WaitForSeconds(0.5f);
        
        isAttacking = false;
        currentState = BossState.Pursuing;
    }
    
    void DealMeleeDamage(float damage)
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position + transform.forward * attackRange, attackRange);
        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.CompareTag("Player"))
            {
                Entity playerEntity = hitCollider.GetComponent<Entity>();
                if (playerEntity != null)
                {
                    playerEntity.TakeDamage(damage, this);
                }
            }
        }
    }
    
    private void CheckForPlayer()
    {
        if (player == null) return;
        
        float distance = Vector3.Distance(transform.position, player.position);
        
        if (distance <= detectionRange)
        {
            currentState = BossState.Pursuing;
            if (animator != null)
                animator.SetTrigger("Alert");
        }
    }
    
    private void PursuePlayer()
    {
        if (player == null || isAttacking) return;
        
        float distance = Vector3.Distance(transform.position, player.position);
        
        // Move towards player with obstacle avoidance
        if (distance > stopDistance)
        {
            MoveTowardsPlayer();
        }
        else
        {
            // Look at player when close enough
            Vector3 direction = (player.position - transform.position).normalized;
            direction.y = 0;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), rotationSpeed * Time.deltaTime);
        }
    }
    
    void MoveTowardsPlayer()
    {
        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0f;
        
        // Apply obstacle avoidance
        direction = GetAvoidanceDirection(direction);
        
        if (direction != Vector3.zero)
        {
            // Look at movement direction
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), rotationSpeed * Time.deltaTime);
            
            // Move
            Vector3 moveDirection = direction * moveSpeed;
            controller.Move(moveDirection * Time.deltaTime);
        }
    }
    
    Vector3 GetAvoidanceDirection(Vector3 desiredDirection)
    {
        // Check for obstacles in desired direction
        Vector3 rayOrigin = transform.position + Vector3.up * 0.5f;
        Vector3 rayDirection = desiredDirection.normalized;
        
        if (Physics.Raycast(rayOrigin, rayDirection, obstacleCheckDistance, obstacleLayer))
        {
            // Obstacle detected, try to avoid it
            Vector3 leftDirection = Quaternion.Euler(0, -avoidanceAngle, 0) * rayDirection;
            Vector3 rightDirection = Quaternion.Euler(0, avoidanceAngle, 0) * rayDirection;
            
            // Check which side is clearer
            bool leftClear = !Physics.Raycast(rayOrigin, leftDirection, obstacleCheckDistance, obstacleLayer);
            bool rightClear = !Physics.Raycast(rayOrigin, rightDirection, obstacleCheckDistance, obstacleLayer);
            
            if (leftClear && !rightClear)
                return leftDirection;
            else if (rightClear && !leftClear)
                return rightDirection;
            else if (leftClear && rightClear)
                return (leftDirection + rightDirection).normalized;
            else
            {
                // Both sides blocked, try bigger avoidance
                Vector3 bigLeftDirection = Quaternion.Euler(0, -avoidanceAngle * 2, 0) * rayDirection;
                Vector3 bigRightDirection = Quaternion.Euler(0, avoidanceAngle * 2, 0) * rayDirection;
                
                if (!Physics.Raycast(rayOrigin, bigLeftDirection, obstacleCheckDistance, obstacleLayer))
                    return bigLeftDirection;
                else if (!Physics.Raycast(rayOrigin, bigRightDirection, obstacleCheckDistance, obstacleLayer))
                    return bigRightDirection;
            }
        }
        
        return desiredDirection;
    }
    
    private void HandleCombat()
    {
        if (player == null) return;
        
        float distance = Vector3.Distance(transform.position, player.position);
        
        // Look at player
        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), rotationSpeed * Time.deltaTime);
        
        // Phase-specific combat logic
        switch (currentPhase)
        {
            case BossPhase.Phase1_Mage:
                HandlePhase1Combat(distance);
                break;
            case BossPhase.Phase2_Blade:
                HandlePhase2Combat(distance);
                break;
            case BossPhase.Phase3_Samurai:
                HandlePhase3Combat(distance);
                break;
            case BossPhase.Phase4_LastResort:
                HandlePhase4Combat(distance);
                break;
        }
    }
    
    void HandlePhase1Combat(float distance)
    {
        // Grand Mage - keep distance, use ranged attacks
        if (distance > attackRange * 1.5f && projectileTimer <= 0)
        {
            // Use fireball barrage at range
            if (Random.value < 0.6f)
                PerformFireballBarrage();
            else
                PerformAbyssalOrb();
        }
        else if (distance <= 3f && Random.value < 0.2f)
        {
            // Teleport evade if player gets too close
            PerformTeleportEvade();
        }
        else if (Random.value < 0.1f)
        {
            // Magic shield defensively
            PerformMagicShield();
        }
    }
    
    void HandlePhase2Combat(float distance)
    {
        // Desperate Blade - hybrid melee-magic, more aggressive
        if (distance <= attackRange && !isAttacking)
        {
            if (Random.value < 0.7f)
                PerformMagicSwordCombo(); // Primary attack
            else if (Random.value < 0.4f)
                PerformArcaneWave(); // Mid-range attack
            else
                PerformLeapingStrike(); // Gap closer
        }
        else if (Random.value < 0.15f)
        {
            PerformEnchantedParry(); // Defensive counter
        }
    }
    
    // Phase 3 Attack Methods (Abyssal Samurai)
    void PerformShadowDashCombo()
    {
        if (attackTimer > 0) return;
        
        currentState = BossState.Attacking;
        StartCoroutine(ShadowDashComboAttack());
    }
    
    IEnumerator ShadowDashComboAttack()
    {
        isAttacking = true;
        attackTimer = attackCooldown * 0.7f; // Faster attacks in samurai phase
        
        // Series of rapid dashes with sword slashes
        for (int i = 0; i < 4; i++)
        {
            // Dash towards player
            Vector3 dashDirection = (player.position - transform.position).normalized;
            Vector3 dashTarget = transform.position + dashDirection * 2f;
            
            // Create shadow ink trail
            if (groundSlamPrefab != null)
            {
                GameObject ink = Instantiate(groundSlamPrefab, transform.position, Quaternion.identity);
                Destroy(ink, 2f);
            }
            
            // Fast dash
            float dashDuration = 0.15f;
            Vector3 startPos = transform.position;
            Vector3 endPos = dashTarget;
            
            for (float t = 0; t < dashDuration; t += Time.deltaTime)
            {
                transform.position = Vector3.Lerp(startPos, endPos, t / dashDuration);
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dashDirection), rotationSpeed * Time.deltaTime * 3f);
                yield return null;
            }
            
            // Quick sword slash
            if (animator != null)
                animator.SetTrigger("Attack");
                
            DealMeleeDamage(meleeDamage * 0.8f); // Moderate damage but fast
            
            yield return new WaitForSeconds(0.1f);
        }
        
        yield return new WaitForSeconds(0.3f);
        
        isAttacking = false;
        currentState = BossState.Pursuing;
    }
    
    void PerformMagicIaijutsu()
    {
        if (heavyAttackTimer > 0) return;
        
        currentState = BossState.SpecialAbility;
        StartCoroutine(MagicIaijutsuAttack());
    }
    
    IEnumerator MagicIaijutsuAttack()
    {
        isAttacking = true;
        heavyAttackTimer = heavyAttackCooldown * 0.8f;
        
        // Very fast sword draw with massive damage
        if (animator != null)
            animator.SetTrigger("Iaijutsu");
            
        yield return new WaitForSeconds(0.2f); // Very short wind-up
        
        // Instant massive damage
        DealMeleeDamage(heavyDamage * 2.5f); // Very high damage
        
        // Visual effect for iaijutsu
        if (groundSlamPrefab != null)
        {
            GameObject effect = Instantiate(groundSlamPrefab, transform.position + transform.forward * 2f, Quaternion.identity);
            Destroy(effect, 1f);
        }
        
        yield return new WaitForSeconds(0.4f);
        
        isAttacking = false;
        currentState = BossState.Pursuing;
    }
    
    void PerformAbyssalClone()
    {
        if (projectileTimer > 0) return;
        
        currentState = BossState.SpecialAbility;
        StartCoroutine(AbyssalCloneAttack());
    }
    
    IEnumerator AbyssalCloneAttack()
    {
        isAttacking = true;
        projectileTimer = projectileCooldown * 1.2f;
        
        // Summon 1-2 shadowy clones
        int cloneCount = Random.Range(1, 3);
        for (int i = 0; i < cloneCount; i++)
        {
            Vector3 spawnOffset = Quaternion.Euler(0, 120f * i, 0) * Vector3.forward * 2f;
            Vector3 clonePos = transform.position + spawnOffset;
            
            // Create clone (you'd need a separate clone script)
            if (groundSlamPrefab != null)
            {
                GameObject clone = Instantiate(groundSlamPrefab, clonePos, Quaternion.identity);
                
                // Make clone attack after delay
                StartCoroutine(CloneAttack(clone, player.position));
                Destroy(clone, 3f);
            }
        }
        
        yield return new WaitForSeconds(0.5f);
        
        isAttacking = false;
        currentState = BossState.Pursuing;
    }
    
    IEnumerator CloneAttack(GameObject clone, Vector3 targetPos)
    {
        yield return new WaitForSeconds(0.8f);
        
        // Clone attacks towards player
        Vector3 direction = (targetPos - clone.transform.position).normalized;
        clone.transform.rotation = Quaternion.LookRotation(direction);
        
        // Deal damage
        DealMeleeDamageFromPosition(clone.transform.position, meleeDamage * 0.6f);
    }
    
    void PerformShadowInkBurst()
    {
        if (player == null) return;
        
        float distance = Vector3.Distance(transform.position, player.position);
        if (distance > 4f) return; // Only use if player is close
        
        if (heavyAttackTimer > 0) return;
        
        currentState = BossState.SpecialAbility;
        StartCoroutine(ShadowInkBurstAttack());
    }
    
    IEnumerator ShadowInkBurstAttack()
    {
        isAttacking = true;
        heavyAttackTimer = heavyAttackCooldown;
        
        // Jump back and create ink burst
        Vector3 jumpBack = transform.position - transform.forward * 3f;
        
        // Create ink burst effect
        if (groundSlamPrefab != null)
        {
            for (int i = 0; i < 8; i++)
            {
                Vector3 inkPos = transform.position + Quaternion.Euler(0, 45f * i, 0) * Vector3.forward * 2f;
                GameObject ink = Instantiate(groundSlamPrefab, inkPos, Quaternion.identity);
                Destroy(ink, 1.5f);
            }
        }
        
        // Jump back
        Vector3 startPos = transform.position;
        for (float t = 0; t < 0.3f; t += Time.deltaTime)
        {
            transform.position = Vector3.Lerp(startPos, jumpBack, t / 0.3f);
            yield return null;
        }
        
        // Deal area damage
        DealMeleeDamage(heavyDamage * 0.7f); // Moderate area damage
        
        yield return new WaitForSeconds(0.5f);
        
        isAttacking = false;
        currentState = BossState.Pursuing;
    }
    
    void DealMeleeDamageFromPosition(Vector3 damageOrigin, float damage)
    {
        Collider[] hitColliders = Physics.OverlapSphere(damageOrigin, attackRange);
        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.CompareTag("Player"))
            {
                Entity playerEntity = hitCollider.GetComponent<Entity>();
                if (playerEntity != null)
                {
                    playerEntity.TakeDamage(damage, this);
                }
            }
        }
    }
    
    // Phase 3 and 4 combat handlers
    void HandlePhase3Combat(float distance)
    {
        // Abyssal Samurai - lightning fast, aggressive, shadow-based
        if (distance <= attackRange && !isAttacking)
        {
            if (Random.value < 0.6f)
                PerformShadowDashCombo(); // Primary fast attack
            else if (Random.value < 0.3f)
                PerformMagicIaijutsu(); // Punishing fast attack
            else if (Random.value < 0.15f)
                PerformAbyssalClone(); // Utility/pressure attack
            else
                PerformShadowInkBurst(); // Area control
        }
        else if (Random.value < 0.1f)
        {
            // More aggressive movement
            moveSpeed = moveSpeed * 1.2f; // Temporary speed boost
        }
    }
    
    // Phase 4 Attack Methods (Tyrant's Last Resort)
    void PerformCrushingOverheadSlam()
    {
        if (heavyAttackTimer > 0) return;
        
        currentState = BossState.SpecialAbility;
        StartCoroutine(CrushingOverheadSlamAttack());
    }
    
    IEnumerator CrushingOverheadSlamAttack()
    {
        isAttacking = true;
        heavyAttackTimer = heavyAttackCooldown * 1.5f; // Slower attacks in final phase
        
        // Very slow, highly telegraphed overhead swing
        if (animator != null)
            animator.SetTrigger("Charge"); // Long charge animation
            
        yield return new WaitForSeconds(1.5f); // Long wind-up time
        
        if (animator != null)
            animator.SetTrigger("Slam");
            
        // Deal extreme damage in large area
        Collider[] hitColliders = Physics.OverlapSphere(transform.position + transform.forward * 3f, 6f);
        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.CompareTag("Player"))
            {
                Entity playerEntity = hitCollider.GetComponent<Entity>();
                if (playerEntity != null)
                {
                    playerEntity.TakeDamage(heavyDamage * 3f, this); // Extreme damage
                }
            }
        }
        
        // Create large shockwave
        if (groundSlamPrefab != null)
        {
            GameObject shockwave = Instantiate(groundSlamPrefab, transform.position + transform.forward * 3f, Quaternion.identity);
            Destroy(shockwave, 2f);
        }
        
        yield return new WaitForSeconds(1f);
        
        isAttacking = false;
        currentState = BossState.Pursuing;
    }
    
    void PerformSweepingArc()
    {
        if (heavyAttackTimer > 0) return;
        
        currentState = BossState.Attacking;
        StartCoroutine(SweepingArcAttack());
    }
    
    IEnumerator SweepingArcAttack()
    {
        isAttacking = true;
        heavyAttackTimer = heavyAttackCooldown * 1.3f; // Slower attacks
        
        // Wide, slow horizontal swing
        if (animator != null)
            animator.SetTrigger("Sweep");
            
        yield return new WaitForSeconds(1f); // Slow wind-up
        
        // Deal high damage in wide arc
        Vector3 arcCenter = transform.position + transform.forward * 2f;
        Quaternion boxRotation = Quaternion.LookRotation(transform.forward);
        Vector3 boxSize = new Vector3(8f, 2f, 4f);
        Collider[] hitColliders = Physics.OverlapBox(arcCenter - Vector3.right * 4f, boxSize, boxRotation);
        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.CompareTag("Player"))
            {
                Entity playerEntity = hitCollider.GetComponent<Entity>();
                if (playerEntity != null)
                {
                    playerEntity.TakeDamage(heavyDamage * 2.2f, this); // High damage
                    // Could knock player down here
                }
            }
        }
        
        yield return new WaitForSeconds(0.8f);
        
        isAttacking = false;
        currentState = BossState.Pursuing;
    }
    
    void PerformAbyssalGroundPound()
    {
        if (heavyAttackTimer > 0) return;
        
        currentState = BossState.SpecialAbility;
        StartCoroutine(AbyssalGroundPoundAttack());
    }
    
    IEnumerator AbyssalGroundPoundAttack()
    {
        isAttacking = true;
        heavyAttackTimer = heavyAttackCooldown * 1.8f; // Very slow attacks
        
        // Raise heavy sword and slam into ground
        if (animator != null)
            animator.SetTrigger("GroundPound");
            
        yield return new WaitForSeconds(1.2f); // Very slow wind-up
        
        // Create multiple Shadow Ink geysers in pattern
        Vector3 poundPosition = transform.position + transform.forward * 2f;
        
        // Create 5 ink geysers in cross pattern
        for (int i = 0; i < 5; i++)
        {
            Vector3 geyserOffset;
            if (i == 0) geyserOffset = Vector3.forward * 3f + Vector3.right * 3f;
            else if (i == 1) geyserOffset = Vector3.forward * 3f - Vector3.right * 3f;
            else if (i == 2) geyserOffset = Vector3.forward * 6f;
            else if (i == 3) geyserOffset = Vector3.right * 6f;
            else geyserOffset = -Vector3.forward * 3f;
            
            Vector3 geyserPos = poundPosition + geyserOffset;
            
            if (groundSlamPrefab != null)
            {
                GameObject geyser = Instantiate(groundSlamPrefab, geyserPos, Quaternion.identity);
                Destroy(geyser, 2.5f);
            }
        }
        
        // Deal area damage
        Collider[] hitColliders = Physics.OverlapSphere(poundPosition, 8f);
        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.CompareTag("Player"))
            {
                Entity playerEntity = hitCollider.GetComponent<Entity>();
                if (playerEntity != null)
                {
                    playerEntity.TakeDamage(heavyDamage * 2.5f, this); // High damage
                }
            }
        }
        
        yield return new WaitForSeconds(1.5f);
        
        isAttacking = false;
        currentState = BossState.Pursuing;
    }
    
    void PerformDesperateRoar()
    {
        if (player == null) return;
        
        float distance = Vector3.Distance(transform.position, player.position);
        if (distance > 5f) return; // Only use if player is close
        
        if (heavyAttackTimer > 0) return;
        
        currentState = BossState.SpecialAbility;
        StartCoroutine(DesperateRoarAttack());
    }
    
    IEnumerator DesperateRoarAttack()
    {
        isAttacking = true;
        heavyAttackTimer = heavyAttackCooldown;
        
        // Briefly stun player
        if (animator != null)
            animator.SetTrigger("Roar");
            
        // Stun nearby player (you'd need to implement player stun system)
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, 6f);
        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.CompareTag("Player"))
            {
                // PlayerController playerController = hitCollider.GetComponent<PlayerController>();
                // if (playerController != null)
                // {
                //     playerController.Stun(1f); // Stun for 1 second
                // }
            }
        }
        
        yield return new WaitForSeconds(0.5f);
        
        // Creates opening for follow-up attack
        isAttacking = false;
        currentState = BossState.Pursuing;
    }
    
    void HandlePhase4Combat(float distance)
    {
        // Tyrant's Last Resort - slow but relentless
        if (distance <= attackRange && !isAttacking)
        {
            if (Random.value < 0.4f)
                PerformCrushingOverheadSlam(); // Primary heavy attack
            else if (Random.value < 0.3f)
                PerformSweepingArc(); // Wide area attack
            else if (Random.value < 0.2f)
                PerformAbyssalGroundPound(); // Area control attack
            else
                PerformDesperateRoar(); // Utility setup attack
        }
        // Does not actively chase with speed, relies on massive attack range
        // Movement is already slowed in phase transition
    }
    
    private IEnumerator MeleeAttack()
    {
        isAttacking = true;
        attackTimer = attackCooldown;
        
        if (animator != null)
            animator.SetTrigger("MeleeAttack");
            
        PlaySound(attackSounds);
        
        yield return new WaitForSeconds(0.5f); // Wind-up time
        
        // Deal damage in front of boss
        Collider[] hitColliders = Physics.OverlapSphere(transform.position + transform.forward * attackRange, attackRange);
        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.CompareTag("Player"))
            {
                Entity playerEntity = hitCollider.GetComponent<Entity>();
                if (playerEntity != null)
                {
                    playerEntity.TakeDamage(meleeDamage, this);
                }
            }
        }
        
        yield return new WaitForSeconds(0.5f); // Recovery time
        isAttacking = false;
    }
    
    private IEnumerator HeavyAttack()
    {
        isAttacking = true;
        heavyAttackTimer = heavyAttackCooldown;
        
        if (animator != null)
            animator.SetTrigger("HeavyAttack");
            
        PlaySound(attackSounds);
        
        yield return new WaitForSeconds(0.8f); // Longer wind-up
        
        // Deal heavy damage in larger area
        Collider[] hitColliders = Physics.OverlapSphere(transform.position + transform.forward * attackRange * 1.5f, attackRange * 1.5f);
        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.CompareTag("Player"))
            {
                Entity playerEntity = hitCollider.GetComponent<Entity>();
                if (playerEntity != null)
                {
                    playerEntity.TakeDamage(heavyDamage, this);
                }
            }
        }
        
        yield return new WaitForSeconds(1f); // Longer recovery
        isAttacking = false;
    }
    
    private IEnumerator GroundSlam()
    {
        isAttacking = true;
        groundSlamTimer = groundSlamCooldown;
        
        if (animator != null)
            animator.SetTrigger("GroundSlam");
            
        PlaySound(attackSounds);
        
        yield return new WaitForSeconds(1f);
        
        // Create ground slam effect
        if (groundSlamPrefab != null)
        {
            Instantiate(groundSlamPrefab, transform.position, Quaternion.identity);
        }
        
        // Deal damage in large AoE
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, 8f);
        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.CompareTag("Player"))
            {
                Entity playerEntity = hitCollider.GetComponent<Entity>();
                if (playerEntity != null)
                {
                    playerEntity.TakeDamage(heavyDamage * 0.8f, this);
                }
            }
        }
        
        yield return new WaitForSeconds(1f);
        isAttacking = false;
        currentState = BossState.Pursuing;
    }
    
    private IEnumerator ProjectileAttack()
    {
        isAttacking = true;
        projectileTimer = projectileCooldown;
        
        if (animator != null)
            animator.SetTrigger("Cast");
            
        PlaySound(attackSounds);
        
        yield return new WaitForSeconds(0.5f);
        
        // Fire multiple projectiles
        for (int i = 0; i < projectileCount; i++)
        {
            if (projectilePrefab != null && projectileSpawnPoint != null)
            {
                GameObject projectile = Instantiate(projectilePrefab, projectileSpawnPoint.position, projectileSpawnPoint.rotation);
                
                // Calculate spread
                float angle = (i - (projectileCount - 1) / 2f) * projectileSpread;
                Quaternion rotation = projectileSpawnPoint.rotation * Quaternion.Euler(0, angle, 0);
                projectile.transform.rotation = rotation;
                
                // Set projectile damage
                Projectile projectileScript = projectile.GetComponent<Projectile>();
                if (projectileScript != null)
                {
                    projectileScript.damage = 20f;
                }
            }
        }
        
        yield return new WaitForSeconds(0.5f);
        isAttacking = false;
        currentState = BossState.Pursuing;
    }
    
    private void HandleStunned()
    {
        // Stunned state logic
        if (attackTimer <= 0)
        {
            currentState = BossState.Pursuing;
        }
    }
    
    protected override void OnDamageTaken(float damage, Entity attacker)
    {
        // Play hurt sound
        PlaySound(hurtSounds);
        
        // Flash red or other visual feedback
        if (animator != null)
            animator.SetTrigger("Hurt");
            
        // Chance to get stunned in phase 3
        if (isInPhase3 && Random.value < 0.1f)
        {
            currentState = BossState.Stunned;
            attackTimer = 1f;
        }
    }
    
    protected override void Die()
    {
        base.Die();
        
        // Play death sound
        if (audioSource != null && deathSound != null)
        {
            audioSource.PlayOneShot(deathSound);
        }
        
        // Play death animation
        if (animator != null)
            animator.SetTrigger("Death");
            
        // Disable components
        if (controller != null)
            controller.enabled = false;
            
        // Disable this script after a delay
        StartCoroutine(DisableAfterDelay(3f));
        
        Debug.Log(bossName + " has been defeated!");
    }
    
    private IEnumerator DisableAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        gameObject.SetActive(false);
    }
    
    private void PlaySound(AudioClip[] clips)
    {
        if (audioSource != null && clips != null && clips.Length > 0)
        {
            AudioClip clip = clips[Random.Range(0, clips.Length)];
            if (clip != null)
                audioSource.PlayOneShot(clip);
        }
    }
    
    // Public methods for external triggers
    public void ForcePhase(int phase)
    {
        switch (phase)
        {
            case 2:
                if (!isInPhase2) EnterPhase2();
                break;
            case 3:
                if (!isInPhase3) EnterPhase3();
                break;
        }
    }
    
    // Debug visualization
    private void OnDrawGizmosSelected()
    {
        // Detection range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        
        // Attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        // Ground slam range
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, 8f);
    }
}

using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

public class BossController : Entity
{
    // ══════════════════════════════════════════════════════════════
    //  ENUMS
    // ══════════════════════════════════════════════════════════════
    public enum BossState { Idle, Chasing, Attacking, Evading, Dead }

    // ══════════════════════════════════════════════════════════════
    //  INSPECTOR
    // ══════════════════════════════════════════════════════════════
    [Header("Boss Settings")]
    public string bossName = "Arcane Tyrant";
    public float detectionRange = 25f;
    public float rotationSpeed = 8f;
    public float walkSpeed = 3.5f;
    public float runSpeed = 5.5f;

    [Header("Combat — Ranged")]
    public float attackRange = 15f;
    public float preferredRange = 8f;
    public float closeRange = 4f;
    public float attackCooldownMin = 0.8f;
    public float attackCooldownMax = 2.2f;

    [Header("Combat — Damage")]
    public float projectileDamage = 20f;
    public float spellDamage = 35f;
    public float comboDamage = 25f;
    public float skillDamage = 50f;
    public float damageRange = 4f;

    [Header("Combat — Teleport")]
    public float teleportDistance = 10f;
    public float teleportCooldown = 8f;

    [Header("Combat — Evade")]
    public float evadeChance = 0.25f;
    public float evadeCooldown = 5f;

    [Header("Projectiles")]
    public GameObject projectilePrefab;
    public Transform projectileSpawnPoint;
    public float projectileSpeed = 15f;

    [Header("AOE Effects")]
    public Material aoeRingMaterial; 
    public float aoeRingDuration = 1f;

    [Header("VFX / SFX")]
    public GameObject teleportVFX;
    public AudioClip[] attackSounds;
    public AudioClip[] hurtSounds;
    public AudioClip deathSound;

    [Header("Hitbox (for Animation Events)")]
    public WeaponHitbox weaponHitbox;

    // ══════════════════════════════════════════════════════════════
    //  RUNTIME STATE
    // ══════════════════════════════════════════════════════════════
    [HideInInspector] public BossState currentState = BossState.Idle;

    private Transform      player;
    private Animator       animator;
    private NavMeshAgent   agent;
    private AudioSource    audioSource;
    private LockOnSystem   playerLockOn;

    private bool  isAttacking = false;
    private bool  useRootMotion = false;
    private float attackCooldownTimer = 0f;
    private float teleportCooldownTimer = 0f;
    private float evadeCooldownTimer = 0f;
    private int   lastAttackIndex = -1;
    private int   consecutiveSameRange = 0;

    // Pending damage — set by coroutine, consumed by animation event
    private float pendingDamage = 0f;
    private float pendingDamageRange = 3f;
    private bool  pendingIsAOE = false;

    // ══════════════════════════════════════════════════════════════
    //  ATTACK TABLE — every Frank_Mage animation, categorized
    // ══════════════════════════════════════════════════════════════
    enum AttackType { Projectile, AOE, Combo, Teleport, Evade }

    struct AttackEntry
    {
        public string   name;
        public string[] clips;      // animation clip names (single or multi-hit combo)
        public float    duration;   // total time before EndAttack
        public float    dmgMult;    // multiplier on base damage
        public float    range;      // effective range
        public bool     isAOE;
        public AttackType type;

        public AttackEntry(string n, string[] c, float dur, float mult, float rng, bool aoe, AttackType t)
        { name=n; clips=c; duration=dur; dmgMult=mult; range=rng; isAOE=aoe; type=t; }
    }

    // Built once in Start
    private List<AttackEntry> rangedAttacks  = new List<AttackEntry>();
    private List<AttackEntry> comboAttacks   = new List<AttackEntry>();
    private List<AttackEntry> skillAttacks   = new List<AttackEntry>();
    private string[] evadeAnims;
    private string[] stepAnims;
    private string[] hitAnims;

    void BuildAttackTable()
    {
        // ── 6 single-cast ranged attacks ──
        for (int i = 1; i <= 6; i++)
            rangedAttacks.Add(new AttackEntry(
                $"Attack{i:D2}",
                new[] { $"Frank_RPG_Mage_Attack{i:D2}" },
                1.4f, 1f, attackRange, false, AttackType.Projectile));

        // ── 4 combo sets ──
        comboAttacks.Add(new AttackEntry("Combo01", new[] {
            "Frank_RPG_Mage_Combo01_1","Frank_RPG_Mage_Combo01_2","Frank_RPG_Mage_Combo01_3"
        }, 2.1f, 0.8f, damageRange, false, AttackType.Combo));

        comboAttacks.Add(new AttackEntry("Combo02", new[] {
            "Frank_RPG_Mage_Combo02_1","Frank_RPG_Mage_Combo02_2","Frank_RPG_Mage_Combo02_3"
        }, 2.1f, 0.9f, damageRange, false, AttackType.Combo));

        comboAttacks.Add(new AttackEntry("Combo03", new[] {
            "Frank_RPG_Mage_Combo03_1","Frank_RPG_Mage_Combo03_2","Frank_RPG_Mage_Combo03_3"
        }, 2.1f, 1f, damageRange, false, AttackType.Combo));

        comboAttacks.Add(new AttackEntry("Combo04", new[] {
            "Frank_RPG_Mage_Combo04_1","Frank_RPG_Mage_Combo04_2","Frank_RPG_Mage_Combo04_3","Frank_RPG_Mage_Combo04_4"
        }, 2.8f, 1.1f, damageRange, false, AttackType.Combo));

        // ── Skill attacks (skip Skill03=teleport, Skill04=teleport) ──
        for (int i = 1; i <= 7; i++)
        {
            if (i == 3 || i == 4) continue; // reserved for teleport
            bool aoe = (i == 1 || i == 5 || i == 7);
            float mult = (i <= 2) ? 1.5f : (i <= 5) ? 1.8f : 2f;
            float dur  = (i <= 2) ? 1.8f : (i <= 5) ? 2.2f : 2.5f;
            skillAttacks.Add(new AttackEntry(
                $"Skill{i:D2}",
                new[] { $"Frank_RPG_Mage_Skill{i:D2}" },
                dur, mult, aoe ? damageRange * 1.5f : attackRange, aoe,
                AttackType.AOE));
        }

        // ── Evades / Steps / Hits ──
        evadeAnims = new[] { "Frank_RPG_Mage_Evade_B","Frank_RPG_Mage_Evade_F","Frank_RPG_Mage_Evade_L","Frank_RPG_Mage_Evade_R" };
        stepAnims  = new[] { "Frank_RPG_Mage_Step_B","Frank_RPG_Mage_Step_F","Frank_RPG_Mage_Step_L","Frank_RPG_Mage_Step_R" };
        hitAnims   = new[] { "Frank_RPG_Mage_Hit01","Frank_RPG_Mage_Hit02","Frank_RPG_Mage_Hit03" };
    }

    // ══════════════════════════════════════════════════════════════
    //  LIFECYCLE
    // ══════════════════════════════════════════════════════════════
    protected override void Start()
    {
        base.Start();

        player      = GameObject.FindGameObjectWithTag("Player")?.transform;
        animator    = GetComponent<Animator>();
        agent       = GetComponent<NavMeshAgent>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        // Disable NavMeshAgent if no valid NavMesh exists in scene
        if (agent != null && !agent.isOnNavMesh)
        {
            agent.enabled = false;
            Debug.LogWarning("[BossController] No valid NavMesh found, NavMeshAgent disabled. Boss will use transform-based movement only.");
        }

        if (weaponHitbox != null) { weaponHitbox.owner = this; weaponHitbox.Deactivate(); }

        // Find lock-on system so we can break lock when teleporting
        PlayerController pc = FindObjectOfType<PlayerController>();
        if (pc != null) playerLockOn = pc.lockOnSystem;

        // Auto-create projectile spawn point if not assigned
        if (projectileSpawnPoint == null)
        {
            GameObject sp = new GameObject("ProjectileSpawn");
            sp.transform.SetParent(transform);
            sp.transform.localPosition = new Vector3(0f, 1.5f, 0.8f);
            projectileSpawnPoint = sp.transform;
        }

        // Configure NavMeshAgent movement speed
        if (agent != null)
        {
            agent.baseOffset = 0f;
            agent.speed = walkSpeed;
            agent.angularSpeed = 360f;
            agent.acceleration = 12f;
            agent.stoppingDistance = 0f;
        }
        GroundOnNavMesh();

        BuildAttackTable();
        currentState = BossState.Idle;
    }

    protected override void Update()
    {
        base.Update();
        if (isDead) return;

        if (attackCooldownTimer > 0f)  attackCooldownTimer  -= Time.deltaTime;
        if (teleportCooldownTimer > 0f) teleportCooldownTimer -= Time.deltaTime;
        if (evadeCooldownTimer > 0f)   evadeCooldownTimer   -= Time.deltaTime;

        switch (currentState)
        {
            case BossState.Idle:     HandleIdle();    break;
            case BossState.Chasing:  HandleChasing(); break;
            case BossState.Attacking: break;
            case BossState.Evading:   break;
            case BossState.Dead:      break;
        }

        UpdateAnimatorParams();
    }

    // ══════════════════════════════════════════════════════════════
    //  STATE HANDLERS
    // ══════════════════════════════════════════════════════════════
    void HandleIdle()
    {
        if (player == null) return;
        if (Vector3.Distance(transform.position, player.position) <= detectionRange)
            currentState = BossState.Chasing;
    }

    void HandleChasing()
    {
        if (player == null || isAttacking) return;
        float dist = Vector3.Distance(transform.position, player.position);
        FacePlayer();

        // Player too close → teleport away or evade
        if (dist < closeRange)
        {
            if (teleportCooldownTimer <= 0f && Random.value < 0.4f)
            { StartCoroutine(DoTeleport()); return; }
            if (evadeCooldownTimer <= 0f && Random.value < evadeChance)
            { StartCoroutine(DoEvade()); return; }
        }

        // In preferred range → can attack if cooldown ready
        if (dist <= preferredRange && dist >= closeRange && attackCooldownTimer <= 0f)
        {
            ChooseAttack(dist);
            return;
        }

        // Outside preferred range, but in attack range → ranged attack occasionally
        if (dist > preferredRange && dist <= attackRange && attackCooldownTimer <= 0f)
        {
            if (Random.value < 0.5f)
            {
                ChooseAttack(dist);
                return;
            }
        }

        // ── MOVEMENT — boss should always be visibly moving ──
        SetAgentEnabled(true);
        if (agent == null || !agent.enabled || !agent.isOnNavMesh) return;

        if (dist > preferredRange)
        {
            // Run toward player when far
            agent.speed = dist > attackRange ? runSpeed : walkSpeed;
            agent.SetDestination(player.position);
        }
        else if (dist >= closeRange && dist <= preferredRange)
        {
            // Circle/strafe around the player while waiting for cooldown
            agent.speed = walkSpeed * 0.8f;
            Vector3 toPlayer = (player.position - transform.position).normalized;
            Vector3 strafeDir = Vector3.Cross(Vector3.up, toPlayer);
            // Swap strafe direction every ~3 seconds
            if (Mathf.Sin(Time.time * 0.7f) > 0f) strafeDir = -strafeDir;
            Vector3 strafeTarget = transform.position + strafeDir * 4f + toPlayer * 0.5f;
            agent.SetDestination(strafeTarget);
        }
        else
        {
            // Too close — back away
            agent.speed = walkSpeed;
            Vector3 awayDir = (transform.position - player.position).normalized;
            agent.SetDestination(transform.position + awayDir * 4f);
        }
    }

    // ══════════════════════════════════════════════════════════════
    //  ATTACK CHOOSER — unpredictable selection
    // ══════════════════════════════════════════════════════════════
    void ChooseAttack(float dist)
    {
        // Weight categories based on distance
        float rangedW = Mathf.Clamp01((dist - closeRange) / (attackRange - closeRange));
        float comboW  = 1f - rangedW;
        float skillW  = 0.2f; // skills always have a chance
        float total = rangedW + comboW + skillW;

        float roll = Random.value * total;

        if (roll < rangedW)
        {
            int idx = PickRandom(rangedAttacks.Count);
            StartCoroutine(DoRangedAttack(rangedAttacks[idx]));
        }
        else if (roll < rangedW + comboW)
        {
            int idx = PickRandom(comboAttacks.Count);
            StartCoroutine(DoComboAttack(comboAttacks[idx]));
        }
        else
        {
            int idx = PickRandom(skillAttacks.Count);
            AttackEntry skill = skillAttacks[idx];
            if (skill.type == AttackType.Teleport)
                StartCoroutine(DoTeleport());
            else
                StartCoroutine(DoSkillAttack(skill));
        }
    }

    int PickRandom(int count)
    {
        // Avoid repeating the same attack twice in a row
        int idx = Random.Range(0, count);
        if (idx == lastAttackIndex && count > 1)
            idx = (idx + 1) % count;
        lastAttackIndex = idx;
        return idx;
    }

    // ══════════════════════════════════════════════════════════════
    //  ATTACK COROUTINES
    // ══════════════════════════════════════════════════════════════

    // ── Single ranged cast (Attack01–06) ──
    IEnumerator DoRangedAttack(AttackEntry atk)
    {
        BeginAttack();
        useRootMotion = true;
        float dmg = projectileDamage * atk.dmgMult;
        SetPendingDamage(dmg, atk.range, false);
        animator.Play(atk.clips[0], 0, 0f);
        PlayAttackSound();
        yield return new WaitForSeconds(atk.duration * 0.55f);
        // Fire a projectile if prefab exists, otherwise fall back to instant damage
        if (projectilePrefab != null && player != null)
            FireProjectile();
        else
            DealDamageInFront(dmg, atk.range);
        yield return new WaitForSeconds(atk.duration * 0.45f);
        EndAttack();
    }

    // ── Multi-hit combos (Combo01–04) ──
    IEnumerator DoComboAttack(AttackEntry atk)
    {
        BeginAttack();
        useRootMotion = true;
        float perClip = atk.duration / atk.clips.Length;
        foreach (string clip in atk.clips)
        {
            FacePlayer();
            float dmg = comboDamage * atk.dmgMult;
            SetPendingDamage(dmg, atk.range, atk.isAOE);
            animator.Play(clip, 0, 0f);
            PlayAttackSound();
            // Deal damage at the midpoint of each hit
            yield return new WaitForSeconds(perClip * 0.5f);
            // Fire projectile for visual feedback if prefab exists
            if (projectilePrefab != null && player != null && !atk.isAOE)
                FireProjectile();
            if (atk.isAOE) DealDamageAround(dmg, atk.range);
            else DealDamageInFront(dmg, atk.range);
            yield return new WaitForSeconds(perClip * 0.5f);
        }
        EndAttack();
    }

    // ── Big skill cast (Skill01–07) ──
    IEnumerator DoSkillAttack(AttackEntry atk)
    {
        BeginAttack();
        useRootMotion = true;
        float dmg = skillDamage * atk.dmgMult;
        SetPendingDamage(dmg, atk.range, atk.isAOE);
        animator.Play(atk.clips[0], 0, 0f);
        PlayAttackSound();
        yield return new WaitForSeconds(atk.duration * 0.5f);
        // Fire projectile for visual feedback if prefab exists
        if (projectilePrefab != null && player != null && !atk.isAOE)
            FireProjectile();
        if (atk.isAOE) DealDamageAround(dmg, atk.range);
        else DealDamageInFront(dmg, atk.range);
        // Big attack — shake the screen for impact
        ShakePlayerCamera(0.2f, 0.25f);
        yield return new WaitForSeconds(atk.duration * 0.5f);
        EndAttack();
    }

    // ── Teleport (Skill04) — warp away, stay grounded ──
    IEnumerator DoTeleport()
    {
        BeginAttack();
        if (teleportVFX != null) Instantiate(teleportVFX, transform.position, Quaternion.identity);

        // Don't break lock-on during teleport - user wants lock-on to persist
        // BreakLockOn();

        animator.Play("Frank_RPG_Mage_Skill03", 0, 0f);
        // Let the teleport windup animation play before warping
        yield return new WaitForSeconds(1.2f);

        Vector3 awayDir = (transform.position - player.position).normalized;
        awayDir.y = 0f;
        Vector3 target = transform.position + awayDir * teleportDistance;

        // Only use NavMesh if agent is valid and enabled
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            NavMeshHit hit;
            if (NavMesh.SamplePosition(target, out hit, teleportDistance, NavMesh.AllAreas))
                target = hit.position;
            else
                target.y = transform.position.y;

            agent.Warp(target);
            GroundOnNavMesh();
        }
        else
        {
            // Direct teleport without NavMesh
            transform.position = target;
        }

        if (teleportVFX != null) Instantiate(teleportVFX, transform.position, Quaternion.identity);
        yield return new WaitForSeconds(0.5f);

        teleportCooldownTimer = teleportCooldown;
        EndAttack(0.5f);
    }

    // ── Evade (directional dodge) ──
    IEnumerator DoEvade()
    {
        currentState = BossState.Evading;
        isAttacking = true;
        SetAgentEnabled(false);

        // Pick contextual evade: if player is in front, evade back; else random
        string anim;
        Vector3 toPlayer = (player.position - transform.position).normalized;
        float dot = Vector3.Dot(transform.forward, toPlayer);
        if (dot > 0.5f) anim = evadeAnims[0]; // Evade_B
        else anim = evadeAnims[Random.Range(0, evadeAnims.Length)];

        animator.CrossFade(anim, 0.1f);
        yield return new WaitForSeconds(0.8f);

        evadeCooldownTimer = evadeCooldown;
        isAttacking = false;
        SetAgentEnabled(true);
        currentState = BossState.Chasing;
    }

    // ══════════════════════════════════════════════════════════════
    //  ATTACK HELPERS
    // ══════════════════════════════════════════════════════════════
    void BeginAttack()
    {
        isAttacking = true;
        currentState = BossState.Attacking;
        SetAgentEnabled(false);
        FacePlayer();
    }

    void EndAttack(float cooldownOverride = -1f)
    {
        isAttacking = false;
        useRootMotion = false;
        attackCooldownTimer = cooldownOverride >= 0f
            ? cooldownOverride
            : Random.Range(attackCooldownMin, attackCooldownMax);
        SetAgentEnabled(true);
        currentState = BossState.Chasing;
    }

    void FacePlayer()
    {
        if (player == null) return;
        Vector3 dir = player.position - transform.position;
        dir.y = 0;
        if (dir.sqrMagnitude > 0.01f)
        {
            Quaternion target = Quaternion.LookRotation(dir.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, target, rotationSpeed * Time.deltaTime);
        }
    }

    void SetAgentEnabled(bool enabled)
    {
        if (agent == null || !agent.isOnNavMesh) return;
        agent.isStopped = !enabled;
        agent.updatePosition = enabled;
        agent.updateRotation = enabled;
    }

    // ══════════════════════════════════════════════════════════════
    //  DAMAGE DEALING
    // ══════════════════════════════════════════════════════════════
    void SetPendingDamage(float damage, float range, bool aoe)
    {
        pendingDamage = damage;
        pendingDamageRange = range;
        pendingIsAOE = aoe;
    }

    void DealDamageInFront(float damage, float range)
    {
        Vector3 center = transform.position + transform.forward * (range * 0.5f);
        foreach (Collider col in Physics.OverlapSphere(center, range))
        {
            if (col.CompareTag("Player"))
            {
                Entity e = col.GetComponent<Entity>();
                if (e != null) e.TakeDamage(damage, this);
            }
        }
    }

    void DealDamageAround(float damage, float radius)
    {
        // Spawn ring effect for visual feedback (safe now with safeguard in AOERingEffect)
        if (aoeRingMaterial != null)
        {
            AOERingEffect.Spawn(transform.position, aoeRingMaterial, radius, aoeRingDuration);
        }

        foreach (Collider col in Physics.OverlapSphere(transform.position, radius))
        {
            if (col.CompareTag("Player"))
            {
                Entity playerEntity = col.GetComponent<Entity>();
                if (playerEntity != null) playerEntity.TakeDamage(damage, this);
            }
        }
    }

    // ══════════════════════════════════════════════════════════════
    //  ANIMATION EVENT CALLBACKS
    //  Add these as Animation Events on your clips at the impact frame.
    // ══════════════════════════════════════════════════════════════
    public void ActivateHitbox()
    {
        if (weaponHitbox != null) weaponHitbox.Activate(pendingDamage);
    }

    public void DeactivateHitbox()
    {
        if (weaponHitbox != null) weaponHitbox.Deactivate();
    }

    public void DealPendingDamage()
    {
        if (pendingIsAOE)
            DealDamageAround(pendingDamage, pendingDamageRange);
        else
            DealDamageInFront(pendingDamage, pendingDamageRange);
    }

    public void FireProjectile()
    {
        if (projectilePrefab == null || player == null) return;
        Vector3 spawnPos = projectileSpawnPoint != null
            ? projectileSpawnPoint.position
            : transform.position + transform.forward * 1f + Vector3.up * 1.5f;
        Vector3 dir = (player.position + Vector3.up * 1f - spawnPos).normalized;
        GameObject proj = Instantiate(projectilePrefab, spawnPos, Quaternion.LookRotation(dir));

        // Give it velocity so it actually flies
        Rigidbody rb = proj.GetComponent<Rigidbody>();
        if (rb != null)
            rb.linearVelocity = dir * projectileSpeed;

        Projectile p = proj.GetComponent<Projectile>();
        if (p != null) { p.damage = pendingDamage; p.owner = this; }

        Destroy(proj, 10f);
    }

    public void PlayAttackSound() { PlaySound(attackSounds); }

    // ══════════════════════════════════════════════════════════════
    //  ANIMATOR
    // ══════════════════════════════════════════════════════════════
    void UpdateAnimatorParams()
    {
        if (animator == null) return;
        float speed = agent != null && agent.enabled ? agent.velocity.magnitude : 0f;
        animator.SetFloat("Speed", speed, 0.1f, Time.deltaTime);
    }

    void OnAnimatorMove()
    {
        if (animator == null) return;

        // Dead — strip ALL root motion, keep grounded
        if (currentState == BossState.Dead)
        {
            // Only apply horizontal root motion, lock Y to last grounded pos
            Vector3 delta = animator.deltaPosition;
            delta.y = 0f;
            transform.position += delta;
            return;
        }

        if (useRootMotion && isAttacking)
        {
            Vector3 delta = animator.deltaPosition;
            delta.y = 0f;
            transform.position += delta;
            transform.rotation *= animator.deltaRotation;
            if (agent != null && agent.enabled && agent.isOnNavMesh)
                transform.position = new Vector3(transform.position.x, agent.nextPosition.y, transform.position.z);
        }
        else if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            // Let NavMeshAgent drive ALL movement
            transform.position = agent.nextPosition;
        }
    }

    // ══════════════════════════════════════════════════════════════
    //  HEALTH OVERRIDES
    // ══════════════════════════════════════════════════════════════
    public override void TakeDamage(float damage, Entity attacker = null)
    {
        if (isDead) return;
        base.TakeDamage(damage, attacker);
    }

    protected override void OnDamageTaken(float damage, Entity attacker)
    {
        PlaySound(hurtSounds);
        // Play random hit reaction if not mid-attack
        if (animator != null && !isAttacking && hitAnims != null && hitAnims.Length > 0)
            animator.CrossFade(hitAnims[Random.Range(0, hitAnims.Length)], 0.1f);
    }

    protected override void Die()
    {
        currentState = BossState.Dead;
        isAttacking = false;
        useRootMotion = false;
        StopAllCoroutines();

        // Disable agent completely so it doesn't fight with position
        if (agent != null)
        {
            agent.isStopped = true;
            agent.updatePosition = false;
            agent.updateRotation = false;
            agent.enabled = false;
        }

        // Ground the boss firmly before playing death
        GroundOnNavMesh();

        // Random death anim
        string deathAnim = Random.value > 0.5f ? "Frank_RPG_Mage_Die" : "Frank_RPG_Mage_Die02";
        if (animator != null) animator.CrossFade(deathAnim, 0.15f);
        PlaySound(deathSound);

        base.Die();
        StartCoroutine(DisableAfterDelay(5f));
        Debug.Log($"[Boss] {bossName} defeated!");
    }

    IEnumerator DisableAfterDelay(float t)
    {
        yield return new WaitForSeconds(t);
        gameObject.SetActive(false);
    }

    // ══════════════════════════════════════════════════════════════
    //  UTILITY
    // ══════════════════════════════════════════════════════════════
    void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }

    void PlaySound(AudioClip[] clips)
    {
        if (audioSource == null || clips == null || clips.Length == 0) return;
        audioSource.PlayOneShot(clips[Random.Range(0, clips.Length)]);
    }

    void ShakePlayerCamera(float magnitude, float duration)
    {
        var cmCam = FindAnyObjectByType<CinemachineLockOnCamera>();
        if (cmCam != null) { cmCam.Shake(magnitude, duration); return; }
        CameraFollow cam = Camera.main?.GetComponent<CameraFollow>();
        if (cam != null) cam.Shake(magnitude, duration);
    }

    void GroundOnNavMesh()
    {
        NavMeshHit hit;
        if (NavMesh.SamplePosition(transform.position, out hit, 10f, NavMesh.AllAreas))
        {
            transform.position = hit.position;
            if (agent != null && agent.enabled)
                agent.Warp(hit.position);
        }
    }

    void BreakLockOn()
    {
        if (playerLockOn == null) return;
        // Only break if the player is locked onto THIS boss
        if (playerLockOn.currentTarget != null)
        {
            Transform lockTarget = playerLockOn.currentTarget;
            // Check if lock target is this boss or a child of this boss (LockOnPoint)
            if (lockTarget == transform || lockTarget.IsChildOf(transform))
                playerLockOn.ReleaseLockOn();
        }
    }

    // ══════════════════════════════════════════════════════════════
    //  DEBUG
    // ══════════════════════════════════════════════════════════════
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, preferredRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, closeRange);
    }
}

using UnityEngine;

public abstract class EnemySkeleton : Entity
{
    [Header("Movement")]
    public float moveSpeed = 3f;
    public float rotationSpeed = 5f;
    
    [Header("Detection")]
    public float detectionRange = 15f;
    public float attackRange = 2f;
    
    [Header("Patrol")]
    public Transform[] patrolPoints;
    public float patrolWaitTime = 2f;
    
    [Header("References")]
    public Transform player;
    public Transform lockOnPoint;
    public Animator animator;

    [Header("Defense")]
    public float armor = 0f;

    public override void TakeDamage(float damageAmount, Entity damageSource = null)
    {
        float reducedDamage = damageAmount - (armor * 0.5f);
        reducedDamage = Mathf.Max(reducedDamage, damageAmount * 0.2f);
        
        base.TakeDamage(reducedDamage, damageSource);
    }
    
    protected Rigidbody rb;
    protected enum State { Idle, Chase, Attack, Dead }
    protected State currentState = State.Idle;
    protected float attackCooldown = 0f;
    
    private int currentPatrolIndex = 0;
    private float patrolTimer = 0f;
    private bool isWaitingAtPatrol = false;
    
    protected override void Start()
    {
        base.Start();
        
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }
        
        // Get animator if not assigned
        if (animator == null)
            animator = GetComponent<Animator>();
        
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
        }
        
        if (lockOnPoint == null)
        {
            GameObject lockPoint = new GameObject("LockOnPoint");
            lockPoint.transform.parent = transform;
            lockPoint.transform.localPosition = new Vector3(0f, 2f, 0f);
            lockOnPoint = lockPoint.transform;
        }
    }
    
    protected override void Update()
    {
        base.Update();
        
        if (currentState == State.Dead) return;
        
        attackCooldown -= Time.deltaTime;
        UpdateState();
        UpdateAnimations();
    }
    
    protected virtual void UpdateAnimations()
    {
        if (animator == null) return;
        
        // Movement speed
        float speed = rb != null ? new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z).magnitude : 0f;
        animator.SetFloat("Speed", speed);
        
        // Combat states
        animator.SetBool("IsAttacking", currentState == State.Attack);
        animator.SetBool("IsAlerted", currentState == State.Chase);
        animator.SetBool("IsDead", currentState == State.Dead);
    }
    
    protected virtual void FixedUpdate()
    {
        if (currentState == State.Dead) return;
        
        switch (currentState)
        {
            case State.Idle:
                HandleIdle();
                break;
            case State.Chase:
                HandleChase();
                break;
            case State.Attack:
                HandleAttack();
                break;
        }
    }
    
    protected virtual void UpdateState()
    {
        if (player == null) return;
        
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        
        if (distanceToPlayer <= attackRange)
        {
            currentState = State.Attack;
        }
        else if (distanceToPlayer <= detectionRange)
        {
            currentState = State.Chase;
        }
        else
        {
            currentState = State.Idle;
        }
    }
    
    protected virtual void HandleIdle()
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            StopMoving();
            return;
        }
        
        if (!isWaitingAtPatrol)
        {
            Transform patrolTarget = patrolPoints[currentPatrolIndex];
            
            if (patrolTarget == null)
            {
                StopMoving();
                return;
            }
            
            float distanceToPatrolPoint = Vector3.Distance(transform.position, patrolTarget.position);
            
            if (distanceToPatrolPoint < 1f)
            {
                isWaitingAtPatrol = true;
                patrolTimer = patrolWaitTime;
                StopMoving();
            }
            else
            {
                MoveTowards(patrolTarget.position);
            }
        }
        else
        {
            StopMoving();
            patrolTimer -= Time.fixedDeltaTime;
            
            if (patrolTimer <= 0f)
            {
                isWaitingAtPatrol = false;
                currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
            }
        }
    }
    
    protected virtual void HandleChase()
    {
        if (player == null) return;
        MoveTowards(player.position);
    }
    
    protected virtual void HandleAttack()
    {
        StopMoving();
        LookAt(player.position);
        
        if (attackCooldown <= 0f)
        {
            PerformAttack();
        }
    }
    
    protected abstract void PerformAttack();
    
    protected virtual void MoveTowards(Vector3 targetPosition)
    {
        Vector3 direction = (targetPosition - transform.position).normalized;
        direction.y = 0f;
        
        if (direction != Vector3.zero)
        {
            LookAt(targetPosition);
            Vector3 velocity = direction * moveSpeed;
            velocity.y = rb.linearVelocity.y;
            rb.linearVelocity = velocity;
        }
    }
    
    protected virtual void StopMoving()
    {
        if (rb != null)
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
        }
    }
    
    protected virtual void LookAt(Vector3 targetPosition)
    {
        Vector3 direction = (targetPosition - transform.position).normalized;
        direction.y = 0f;
        
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime));
        }
    }
    
    protected override void Die()
    {
        base.Die();
        currentState = State.Dead;
        
        // Trigger death animation
        if (animator != null)
            animator.SetTrigger("Die");
        
        if (rb != null) rb.isKinematic = true;
        
        Destroy(gameObject, 2f);
    }
    
    protected virtual void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        if (patrolPoints != null && patrolPoints.Length > 1)
        {
            Gizmos.color = Color.blue;
            for (int i = 0; i < patrolPoints.Length; i++)
            {
                if (patrolPoints[i] == null) continue;
                
                int nextIndex = (i + 1) % patrolPoints.Length;
                if (patrolPoints[nextIndex] != null)
                {
                    Gizmos.DrawLine(patrolPoints[i].position, patrolPoints[nextIndex].position);
                    Gizmos.DrawSphere(patrolPoints[i].position, 0.3f);
                }
            }
        }
    }
}

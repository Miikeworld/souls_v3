using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class EnemyAI : MonoBehaviour
{
    [Header("Detection")]
    public float detectionRange = 15f;
    public float loseTargetRange = 18f;
    public float attackRange = 2f;
    public float exitAttackRange = 3f;
    
    [Header("Movement")]
    public float moveSpeed = 3f;
    public float rotationSpeed = 5f; // Increased from 3
    public float stopDistance = 1.5f;
    
    [Header("Patrol")]
    public Transform[] patrolPoints;
    public float patrolWaitTime = 2f;
    
    [Header("References")]
    public Transform player;
    public Transform lockOnPoint;
    
    [Header("Debug")]
    public bool showDebugLines = true;
    
    private Rigidbody rb;
    private int currentPatrolIndex = 0;
    private float patrolTimer = 0f;
    private bool isWaiting = false;
    
    private bool isChasing = false;
    private bool isAttacking = false;
    
    private enum State { Patrol, Chase, Attack }
    private State currentState = State.Patrol;
    
    private Vector3 targetPosition;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.constraints = RigidbodyConstraints.FreezeRotationX | 
                           RigidbodyConstraints.FreezeRotationZ;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.linearDamping = 0f; // No drag
        }
        else
        {
            Debug.LogError("Rigidbody missing on " + gameObject.name);
        }
        
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
            else
                Debug.LogWarning("Player not found! Make sure player has 'Player' tag.");
        }
        
        if (lockOnPoint == null)
        {
            GameObject lockPoint = new GameObject("LockOnPoint");
            lockPoint.transform.parent = transform;
            lockPoint.transform.localPosition = new Vector3(0f, 2f, 0f);
            lockOnPoint = lockPoint.transform;
        }
    }

    void Update()
    {
        if (player == null) return;
        
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        
        // State transitions
        if (distanceToPlayer <= attackRange)
        {
            isAttacking = true;
            isChasing = true;
            currentState = State.Attack;
        }
        else if (distanceToPlayer > exitAttackRange && isAttacking)
        {
            isAttacking = false;
            currentState = State.Chase;
        }
        else if (distanceToPlayer <= detectionRange)
        {
            isChasing = true;
            isAttacking = false;
            currentState = State.Chase;
        }
        else if (distanceToPlayer > loseTargetRange && isChasing)
        {
            isChasing = false;
            isAttacking = false;
            currentState = State.Patrol;
        }
        else if (!isChasing && !isAttacking)
        {
            currentState = State.Patrol;
        }
        
        // Debug
        if (showDebugLines)
        {
            Debug.DrawLine(transform.position, transform.position + transform.forward * 2f, Color.blue);
            if (currentState == State.Chase || currentState == State.Attack)
            {
                Debug.DrawLine(transform.position, player.position, Color.green);
            }
        }
    }

    void FixedUpdate()
    {
        switch (currentState)
        {
            case State.Patrol:
                HandlePatrol();
                break;
            case State.Chase:
                HandleChase();
                break;
            case State.Attack:
                HandleAttack();
                break;
        }
    }

    void HandlePatrol()
    {
        if (patrolPoints.Length == 0)
        {
            StopMoving();
            return;
        }
        
        if (!isWaiting)
        {
            Transform patrolTarget = patrolPoints[currentPatrolIndex];
            targetPosition = patrolTarget.position;
            
            float distanceToPatrolPoint = Vector3.Distance(transform.position, targetPosition);
            
            if (distanceToPatrolPoint < 1f)
            {
                isWaiting = true;
                patrolTimer = patrolWaitTime;
                StopMoving();
            }
            else
            {
                MoveTowardsTarget(targetPosition);
            }
        }
        else
        {
            StopMoving();
            patrolTimer -= Time.fixedDeltaTime;
            
            if (patrolTimer <= 0f)
            {
                isWaiting = false;
                currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
            }
        }
    }

    void HandleChase()
    {
        if (player == null) return;
        
        targetPosition = player.position;
        float distance = Vector3.Distance(transform.position, targetPosition);
        
        if (distance > stopDistance)
        {
            MoveTowardsTarget(targetPosition);
        }
        else
        {
            StopMoving();
        }
    }

    void HandleAttack()
    {
        if (player == null) return;
        
        targetPosition = player.position;
        StopMoving();
        
        // Only rotate to face player
        Vector3 direction = (targetPosition - transform.position).normalized;
        direction.y = 0f;
        
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime));
        }
        
        // TODO: Attack logic here
    }

    void MoveTowardsTarget(Vector3 target)
    {
        // Calculate direction to target
        Vector3 direction = (target - transform.position).normalized;
        direction.y = 0f; // Flatten on horizontal plane
        
        if (direction != Vector3.zero)
        {
            // Rotate towards target faster
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime));
            
            // Move in the direction we're facing
            Vector3 moveVelocity = direction * moveSpeed;
            moveVelocity.y = rb.linearVelocity.y; // Keep gravity
            
            rb.linearVelocity = moveVelocity;
        }
    }

    void StopMoving()
    {
        if (rb != null)
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
        }
    }

    void OnDrawGizmos()
    {
        if (showDebugLines && Application.isPlaying)
        {
            // Current state text
            UnityEditor.Handles.Label(transform.position + Vector3.up * 3f, "State: " + currentState.ToString());
        }
    }

    void OnDrawGizmosSelected()
    {
        // Detection range (yellow)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        
        // Lose target range (yellow transparent)
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, loseTargetRange);
        
        // Attack range (red)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        // Exit attack range (red transparent)
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, exitAttackRange);
        
        // Patrol path
        if (patrolPoints != null && patrolPoints.Length > 1)
        {
            Gizmos.color = Color.blue;
            for (int i = 0; i < patrolPoints.Length; i++)
            {
                int nextIndex = (i + 1) % patrolPoints.Length;
                if (patrolPoints[i] != null && patrolPoints[nextIndex] != null)
                {
                    Gizmos.DrawLine(patrolPoints[i].position, patrolPoints[nextIndex].position);
                    Gizmos.DrawSphere(patrolPoints[i].position, 0.3f);
                }
            }
        }
    }
}

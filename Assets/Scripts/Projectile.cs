using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float damage = 10f;
    public Entity owner;
    
    [Header("Homing")]
    public bool enableHoming = false;
    public Transform target;
    public float homingDuration = 2f;
    public float homingTurnSpeed = 5f;
    public float speed = 15f;
    
    private float homingTimer;
    private Rigidbody rb;
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        homingTimer = homingDuration;
    }
    
    void FixedUpdate()
    {
        if (enableHoming && target != null && homingTimer > 0f)
        {
            homingTimer -= Time.fixedDeltaTime;
            
            // Calculate direction to target
            Vector3 targetPos = target.position + Vector3.up * 1f;
            Vector3 currentDir = rb.linearVelocity.normalized;
            Vector3 targetDir = (targetPos - transform.position).normalized;
            
            // Rotate towards target
            Vector3 newDir = Vector3.RotateTowards(currentDir, targetDir, homingTurnSpeed * Time.fixedDeltaTime, 0f);
            
            // Apply new velocity with constant speed
            rb.linearVelocity = newDir * speed;
        }
        else if (rb != null)
        {
            // Stop homing, maintain current direction
            rb.linearVelocity = rb.linearVelocity.normalized * speed;
        }
    }
    
    void OnCollisionEnter(Collision collision)
    {
        Entity targetEntity = collision.gameObject.GetComponent<Entity>();
        
        if (targetEntity != null && targetEntity != owner)
        {
            targetEntity.TakeDamage(damage, owner);
        }
        
        Destroy(gameObject);
    }
}

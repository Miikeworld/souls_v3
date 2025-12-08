using UnityEngine;

public class TankSkeleton : EnemySkeleton
{
    [Header("Tank Settings")]
    public float tankDamage = 10f;
    public float shieldBashCooldown = 2.5f;
    public float shieldStaminaCost = 15f;
    public float bashRadius = 2f;
    
    [Header("Shield Settings")]
    public float maxShieldHealth = 50f;
    public float currentShieldHealth = 50f;
    public float shieldRegenRate = 5f;
    public float shieldRegenDelay = 3f;
    private float timeSinceShieldDamaged = 0f;
    
    private bool hasShield = true;
    
    protected override void Start()
    {
        base.Start();
        attackRange = 2f;
        
        maxHealth = 150f;
        currentHealth = maxHealth;
        maxStamina = 100f;
        currentStamina = maxStamina;
        armor = 15f;
        
        currentShieldHealth = maxShieldHealth;
    }
    
    protected override void Update()
    {
        base.Update();
        
        if (hasShield && currentShieldHealth < maxShieldHealth)
        {
            timeSinceShieldDamaged += Time.deltaTime;
            
            if (timeSinceShieldDamaged >= shieldRegenDelay)
            {
                currentShieldHealth += shieldRegenRate * Time.deltaTime;
                currentShieldHealth = Mathf.Clamp(currentShieldHealth, 0, maxShieldHealth);
            }
        }
    }
    
    public override void TakeDamage(float damageAmount, Entity damageSource = null)
    {
        if (hasShield && currentShieldHealth > 0)
        {
            float shieldDamage = damageAmount * 0.7f;
            float remainingDamage = damageAmount * 0.3f;
            
            currentShieldHealth -= shieldDamage;
            timeSinceShieldDamaged = 0f;
            
            if (currentShieldHealth <= 0)
            {
                currentShieldHealth = 0;
                hasShield = false;
                
                if (showDebug)
                {
                    Debug.Log("Tank Skeleton's shield BROKEN!");
                }
            }
            
            base.TakeDamage(remainingDamage, damageSource);
        }
        else
        {
            base.TakeDamage(damageAmount, damageSource);
        }
    }
    
    protected override void PerformAttack()
    {
        if (!UseStamina(shieldStaminaCost))
        {
            return;
        }
        
        attackCooldown = shieldBashCooldown;
        
        if (player == null) return;
        
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (distanceToPlayer <= bashRadius)
        {
            Entity playerEntity = player.GetComponent<Entity>();
            if (playerEntity != null)
            {
                playerEntity.TakeDamage(tankDamage, this);
                
                Vector3 knockbackDirection = (player.position - transform.position).normalized;
                Rigidbody playerRb = player.GetComponent<Rigidbody>();
                if (playerRb != null)
                {
                    playerRb.AddForce(knockbackDirection * 10f, ForceMode.Impulse);
                }
            }
            
            if (showDebug)
            {
                Debug.Log("Tank Skeleton shield bashed for " + tankDamage + " damage!");
            }
        }
    }
}

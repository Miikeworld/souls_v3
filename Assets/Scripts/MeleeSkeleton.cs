using UnityEngine;

public class MeleeSkeleton : EnemySkeleton
{
    [Header("Melee Settings")]
    public float meleeDamage = 15f;
    public float meleeAttackCooldown = 1.5f;
    public float meleeStaminaCost = 10f;
    public float swingRadius = 2f;
    
    protected override void Start()
    {
        base.Start();
        attackRange = 2.5f;
        
        maxStamina = 80f;
        currentStamina = maxStamina;
    }
    
    protected override void PerformAttack()
    {
        if (!UseStamina(meleeStaminaCost))
        {
            return;
        }
        
        attackCooldown = meleeAttackCooldown;
        
        if (player == null) return;
        
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (distanceToPlayer <= swingRadius)
        {
            Entity playerEntity = player.GetComponent<Entity>();
            if (playerEntity != null)
            {
                playerEntity.TakeDamage(meleeDamage, this);
            }
            
            if (showDebug)
            {
                Debug.Log("Melee Skeleton attacked for " + meleeDamage + " damage!");
            }
        }
    }
}

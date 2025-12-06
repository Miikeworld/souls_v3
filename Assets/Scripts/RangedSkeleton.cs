using UnityEngine;

public class RangedSkeleton : EnemySkeleton
{
    [Header("Ranged Settings")]
    public GameObject projectilePrefab;
    public Transform firePoint;
    public float projectileDamage = 10f;
    public float projectileSpeed = 15f;
    public float shootCooldown = 2f;
    public float arrowManaCost = 15f;
    
    protected override void Start()
    {
        base.Start();
        attackRange = 10f;
        detectionRange = 15f;
        
        maxMana = 150f;
        currentMana = maxMana;
        manaRegenRate = 15f;
        
        if (firePoint == null)
        {
            GameObject fp = new GameObject("FirePoint");
            fp.transform.parent = transform;
            fp.transform.localPosition = new Vector3(0f, 1.5f, 0.5f);
            firePoint = fp.transform;
        }
    }
    
    protected override void PerformAttack()
    {
        if (!UseMana(arrowManaCost))
        {
            return;
        }
        
        attackCooldown = shootCooldown;
        
        if (player == null || projectilePrefab == null) return;
        
        GameObject projectile = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
        
        Rigidbody projRb = projectile.GetComponent<Rigidbody>();
        if (projRb != null)
        {
            Vector3 direction = (player.position - firePoint.position).normalized;
            projRb.linearVelocity = direction * projectileSpeed;
        }
        
        Projectile projScript = projectile.GetComponent<Projectile>();
        if (projScript != null)
        {
            projScript.damage = projectileDamage;
            projScript.owner = this;
        }
        
        Destroy(projectile, 5f);
    }
}

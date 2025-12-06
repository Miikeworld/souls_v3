using UnityEngine;
using System.Collections.Generic;

public class HealerSkeleton : EnemySkeleton
{
    [Header("Healer Settings")]
    public float healerDamage = 8f;
    public float magicBoltCooldown = 3f;
    public float magicStaminaCost = 12f;
    public float boltRange = 15f;
    
    [Header("Healing Settings")]
    public float healAmount = 40f;
    public float healCooldown = 5f;
    public float healRange = 20f;
    public float healStaminaCost = 20f;
    
    [Header("Projectile")]
    public GameObject magicBoltPrefab;
    public float boltSpeed = 15f;
    
    private float timeSinceLastHeal = 0f;
    
    protected override void Start()
    {
        base.Start();
        attackRange = 15f;
        
        maxHealth = 50f;
        currentHealth = maxHealth;
        maxStamina = 120f;
        currentStamina = maxStamina;
        armor = 3f;
    }
    
    protected override void Update()
    {
        base.Update();
        
        timeSinceLastHeal += Time.deltaTime;
    }
    
    protected override void PerformAttack()
    {
        if (player == null) return;
        
        List<EnemySkeleton> nearbyAllies = FindNearbyAllies(healRange);
        
        foreach (EnemySkeleton ally in nearbyAllies)
        {
            if (ally.currentHealth < ally.maxHealth * 0.7f && 
                timeSinceLastHeal >= healCooldown &&
                UseStamina(healStaminaCost))
            {
                CastHeal(ally);
                return;
            }
        }
        
        if (UseStamina(magicStaminaCost))
        {
            CastMagicBolt();
        }
    }
    
    private void CastHeal(EnemySkeleton target)
    {
        float oldHealth = target.currentHealth;
        target.currentHealth += healAmount;
        target.currentHealth = Mathf.Clamp(target.currentHealth, 0, target.maxHealth);
        
        float actualHealed = target.currentHealth - oldHealth;
        
        timeSinceLastHeal = 0f;
        attackCooldown = healCooldown;
        
        if (showDebug)
        {
            Debug.Log("Healer Skeleton healed " + target.name + " for " + actualHealed + " HP!");
        }
    }
    
    private void CastMagicBolt()
    {
        attackCooldown = magicBoltCooldown;
        
        if (magicBoltPrefab == null || player == null) return;
        
        Vector3 spawnPos = transform.position + transform.forward * 1.5f + Vector3.up * 1.5f;
        GameObject bolt = Instantiate(magicBoltPrefab, spawnPos, Quaternion.identity);
        
        Rigidbody boltRb = bolt.GetComponent<Rigidbody>();
        if (boltRb != null)
        {
            Vector3 direction = (player.position - transform.position).normalized;
            boltRb.linearVelocity = direction * boltSpeed;
        }
        
        Projectile projectile = bolt.GetComponent<Projectile>();
        if (projectile != null)
        {
            projectile.damage = healerDamage;
            projectile.owner = this;
        }
        
        if (showDebug)
        {
            Debug.Log("Healer Skeleton cast magic bolt!");
        }
    }
    
    private List<EnemySkeleton> FindNearbyAllies(float range)
    {
        List<EnemySkeleton> allies = new List<EnemySkeleton>();
        
        EnemySkeleton[] allSkeletons = FindObjectsOfType<EnemySkeleton>();
        
        foreach (EnemySkeleton skeleton in allSkeletons)
        {
            if (skeleton == this) continue;
            
            float distance = Vector3.Distance(transform.position, skeleton.transform.position);
            if (distance <= range)
            {
                allies.Add(skeleton);
            }
        }
        
        return allies;
    }
}

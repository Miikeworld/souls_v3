using UnityEngine;
using System;

public abstract class Entity : MonoBehaviour
{
    [Header("Health")]
    public float maxHealth = 100f;
    public float currentHealth;
    public float healAmount = 30f; // Amount healed by bonfire
    public float healStaminaCost = 20f; // Stamina cost to heal
    
    [Header("Potions")]
    public int maxPotions = 10;
    public int currentPotions = 5;
    
    [Header("Mana")]
    public float maxMana = 100f;
    public float currentMana;
    public float manaRegenRate = 10f; // Mana per second
    
    [Header("Stamina")]
    public float maxStamina = 100f;
    public float currentStamina;
    public float staminaRegenRate = 15f; // Stamina per second
    public float staminaRegenDelay = 1f; // Delay after use
    
    [Header("Debug")]
    public bool showDebug = false;
    
    // Events for UI updates
    public event Action OnHealthChanged;
    public event Action OnManaChanged;
    public event Action OnStaminaChanged;
    public event Action OnPotionsChanged;
    public event Action OnDeath;
    
    public bool isDead = false;
    protected float healthRegenTimer = 0f;
    protected float staminaRegenTimer = 0f;
    
    protected virtual void Start()
    {
        // Initialize to max
        currentHealth = maxHealth;
        currentMana = maxMana;
        currentStamina = maxStamina;
        currentPotions = 5; // Start with 5 potions
        
        InvokeResourceEvents();
    }
    
    protected virtual void Update()
    {
        if (isDead) return;
        
        RegenerateResources();
    }
    
    protected virtual void RegenerateResources()
    {
        // Health regeneration (removed - only heal at bonfires)
        
        // Mana regeneration (constant)
        if (currentMana < maxMana)
        {
            ModifyMana(manaRegenRate * Time.deltaTime);
        }
        
        // Stamina regeneration (with delay)
        if (staminaRegenTimer > 0f)
        {
            staminaRegenTimer -= Time.deltaTime;
        }
        else if (currentStamina < maxStamina)
        {
            ModifyStamina(staminaRegenRate * Time.deltaTime);
        }
    }
    
    // Health methods
    public virtual void TakeDamage(float damage, Entity attacker = null)
    {
        if (isDead) return;
        
        currentHealth -= damage;
        currentHealth = Mathf.Max(currentHealth, 0f);
        
        OnHealthChanged?.Invoke();
        
        if (showDebug)
        {
            Debug.Log(gameObject.name + " took " + damage + " damage. HP: " + currentHealth);
        }
        
        if (currentHealth <= 0f)
        {
            Die();
        }
        else
        {
            OnDamageTaken(damage, attacker);
        }
    }
    
    public virtual void Heal(float amount)
    {
        if (isDead) return;
        
        ModifyHealth(amount);
        
        if (showDebug)
        {
            Debug.Log(gameObject.name + " healed for " + amount + ". HP: " + currentHealth);
        }
    }
    
    public virtual bool TryHeal()
    {
        if (isDead) return false;
        if (currentHealth >= maxHealth) return false;
        if (!UseStamina(healStaminaCost)) return false;
        
        float healAmount = this.healAmount;
        
        // Don't overheal
        float maxHeal = maxHealth - currentHealth;
        if (healAmount > maxHeal)
            healAmount = maxHeal;
        
        ModifyHealth(healAmount);
        
        if (showDebug)
        {
            Debug.Log(gameObject.name + " healed for " + healAmount + " HP using stamina. HP: " + currentHealth);
        }
        
        return true;
    }
    
    public virtual bool UsePotion()
    {
        if (isDead) return false;
        if (currentPotions <= 0) return false;
        if (currentHealth >= maxHealth) return false;
        
        currentPotions--;
        float healAmount = 50f; // Potions heal 50 HP
        
        // Don't overheal
        float maxHeal = maxHealth - currentHealth;
        if (healAmount > maxHeal)
            healAmount = maxHeal;
        
        ModifyHealth(healAmount);
        OnPotionsChanged?.Invoke();
        
        if (showDebug)
        {
            Debug.Log(gameObject.name + " used potion. Healed for " + healAmount + " HP. Potions remaining: " + currentPotions);
        }
        
        return true;
    }
    
    public virtual void RestorePotions()
    {
        currentPotions = maxPotions;
        OnPotionsChanged?.Invoke();
        
        if (showDebug)
        {
            Debug.Log(gameObject.name + " potions restored to max: " + currentPotions);
        }
    }
    
    protected virtual void ModifyHealth(float amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        OnHealthChanged?.Invoke();
    }
    
    // Mana methods
    public virtual bool UseMana(float amount)
    {
        if (currentMana >= amount)
        {
            currentMana -= amount;
            OnManaChanged?.Invoke();
            
            if (showDebug)
            {
                Debug.Log(gameObject.name + " used " + amount + " mana. Remaining: " + currentMana);
            }
            
            return true;
        }
        
        if (showDebug)
        {
            Debug.Log(gameObject.name + " not enough mana!");
        }
        
        return false;
    }
    
    protected virtual void ModifyMana(float amount)
    {
        currentMana += amount;
        currentMana = Mathf.Clamp(currentMana, 0f, maxMana);
        OnManaChanged?.Invoke();
    }
    
    // Stamina methods
    public virtual bool UseStamina(float amount)
    {
        if (currentStamina >= amount)
        {
            currentStamina -= amount;
            staminaRegenTimer = staminaRegenDelay;
            OnStaminaChanged?.Invoke();
            
            if (showDebug)
            {
                Debug.Log(gameObject.name + " used " + amount + " stamina. Remaining: " + currentStamina);
            }
            
            return true;
        }
        
        if (showDebug)
        {
            Debug.Log(gameObject.name + " not enough stamina!");
        }
        
        return false;
    }
    
    protected virtual void ModifyStamina(float amount)
    {
        currentStamina += amount;
        currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);
        OnStaminaChanged?.Invoke();
    }
    
    // Getters for percentages (useful for UI)
    public float GetHealthPercent() => currentHealth / maxHealth;
    public float GetManaPercent() => currentMana / maxMana;
    public float GetStaminaPercent() => currentStamina / maxStamina;
    
    // Abstract and virtual methods
    protected virtual void OnDamageTaken(float damage, Entity attacker) { }
    
    protected virtual void Die()
    {
        if (isDead) return;
        
        isDead = true;
        OnDeath?.Invoke();
        
        if (showDebug)
        {
            Debug.Log(gameObject.name + " died!");
        }
    }
    
    public void InvokeResourceEvents()
    {
        OnHealthChanged?.Invoke();
        OnManaChanged?.Invoke();
        OnStaminaChanged?.Invoke();
        OnPotionsChanged?.Invoke();
    }
}

using UnityEngine;
using System;

public abstract class Entity : MonoBehaviour
{
    [Header("Health")]
    public float maxHealth = 100f;
    public float currentHealth;
    public float healthRegenRate = 5f; // HP per second
    public float healthRegenDelay = 3f; // Delay after taking damage
    
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
    public event Action OnDeath;
    
    protected bool isDead = false;
    protected float healthRegenTimer = 0f;
    protected float staminaRegenTimer = 0f;
    
    protected virtual void Start()
    {
        // Initialize to max
        currentHealth = maxHealth;
        currentMana = maxMana;
        currentStamina = maxStamina;
        
        InvokeResourceEvents();
    }
    
    protected virtual void Update()
    {
        if (isDead) return;
        
        RegenerateResources();
    }
    
    protected virtual void RegenerateResources()
    {
        // Health regeneration (with delay)
        if (healthRegenTimer > 0f)
        {
            healthRegenTimer -= Time.deltaTime;
        }
        else if (currentHealth < maxHealth)
        {
            ModifyHealth(healthRegenRate * Time.deltaTime);
        }
        
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
        
        // Reset health regen timer
        healthRegenTimer = healthRegenDelay;
        
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
    
    protected void InvokeResourceEvents()
    {
        OnHealthChanged?.Invoke();
        OnManaChanged?.Invoke();
        OnStaminaChanged?.Invoke();
    }
}

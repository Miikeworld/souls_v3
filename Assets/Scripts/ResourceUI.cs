using UnityEngine;
using UnityEngine.UI;

public class ResourceUI : MonoBehaviour
{
    [Header("References")]
    public Entity targetEntity;
    
    [Header("Health Bar")]
    public Image healthBarFill;
    public Image healthBarBackground; // NEW: For damage flash effect
    public Text healthText;
    
    [Header("Mana Bar")]
    public Image manaBarFill;
    public Text manaText;
    
    [Header("Stamina Bar")]
    public Image staminaBarFill;
    public Text staminaText;
    
    [Header("Colors")]
    public Color healthColor = Color.red;
    public Color manaColor = Color.blue;
    public Color staminaColor = Color.green;
    public Color lowHealthColor = new Color(1f, 0.5f, 0f); // Orange when low
    
    [Header("Animation")]
    public float fillSpeed = 5f; // Smooth fill animation speed
    public bool useSmoothFill = true;
    
    // Target fill amounts
    private float targetHealthFill = 1f;
    private float targetManaFill = 1f;
    private float targetStaminaFill = 1f;
    
    void Start()
    {
        if (targetEntity == null)
        {
            targetEntity = FindObjectOfType<PlayerController>();
        }
        
        if (targetEntity != null)
        {
            targetEntity.OnHealthChanged += UpdateHealth;
            targetEntity.OnManaChanged += UpdateMana;
            targetEntity.OnStaminaChanged += UpdateStamina;
            
            UpdateHealth();
            UpdateMana();
            UpdateStamina();
        }
        
        if (healthBarFill != null) healthBarFill.color = healthColor;
        if (manaBarFill != null) manaBarFill.color = manaColor;
        if (staminaBarFill != null) staminaBarFill.color = staminaColor;
    }
    
    void Update()
    {
        if (!useSmoothFill) return;
        
        // Smoothly animate fill amounts
        if (healthBarFill != null)
        {
            healthBarFill.fillAmount = Mathf.Lerp(healthBarFill.fillAmount, targetHealthFill, fillSpeed * Time.deltaTime);
        }
        
        if (manaBarFill != null)
        {
            manaBarFill.fillAmount = Mathf.Lerp(manaBarFill.fillAmount, targetManaFill, fillSpeed * Time.deltaTime);
        }
        
        if (staminaBarFill != null)
        {
            staminaBarFill.fillAmount = Mathf.Lerp(staminaBarFill.fillAmount, targetStaminaFill, fillSpeed * Time.deltaTime);
        }
    }
    
    void OnDestroy()
    {
        if (targetEntity != null)
        {
            targetEntity.OnHealthChanged -= UpdateHealth;
            targetEntity.OnManaChanged -= UpdateMana;
            targetEntity.OnStaminaChanged -= UpdateStamina;
        }
    }
    
    void UpdateHealth()
    {
        if (targetEntity == null) return;
        
        targetHealthFill = targetEntity.GetHealthPercent();
        
        if (!useSmoothFill && healthBarFill != null)
        {
            healthBarFill.fillAmount = targetHealthFill;
        }
        
        // Change color when low health (below 30%)
        if (healthBarFill != null)
        {
            if (targetHealthFill < 0.3f)
            {
                healthBarFill.color = lowHealthColor;
            }
            else
            {
                healthBarFill.color = healthColor;
            }
        }
        
        if (healthText != null)
        {
            healthText.text = Mathf.Ceil(targetEntity.currentHealth) + " / " + targetEntity.maxHealth;
        }
    }
    
    void UpdateMana()
    {
        if (targetEntity == null) return;
        
        targetManaFill = targetEntity.GetManaPercent();
        
        if (!useSmoothFill && manaBarFill != null)
        {
            manaBarFill.fillAmount = targetManaFill;
        }
        
        if (manaText != null)
        {
            manaText.text = Mathf.Ceil(targetEntity.currentMana) + " / " + targetEntity.maxMana;
        }
    }
    
    void UpdateStamina()
    {
        if (targetEntity == null) return;
        
        targetStaminaFill = targetEntity.GetStaminaPercent();
        
        if (!useSmoothFill && staminaBarFill != null)
        {
            staminaBarFill.fillAmount = targetStaminaFill;
        }
        
        if (staminaText != null)
        {
            staminaText.text = Mathf.Ceil(targetEntity.currentStamina) + " / " + targetEntity.maxStamina;
        }
    }
}

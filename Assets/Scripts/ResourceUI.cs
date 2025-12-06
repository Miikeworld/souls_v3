using UnityEngine;
using UnityEngine.UI;

public class ResourceUI : MonoBehaviour
{
    [Header("References")]
    public Entity targetEntity;
    
    [Header("Health Bar")]
    public Image healthBarFill;
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
        
        if (healthBarFill != null)
        {
            healthBarFill.fillAmount = targetEntity.GetHealthPercent();
        }
        
        if (healthText != null)
        {
            healthText.text = Mathf.Ceil(targetEntity.currentHealth) + " / " + targetEntity.maxHealth;
        }
    }
    
    void UpdateMana()
    {
        if (targetEntity == null) return;
        
        if (manaBarFill != null)
        {
            manaBarFill.fillAmount = targetEntity.GetManaPercent();
        }
        
        if (manaText != null)
        {
            manaText.text = Mathf.Ceil(targetEntity.currentMana) + " / " + targetEntity.maxMana;
        }
    }
    
    void UpdateStamina()
    {
        if (targetEntity == null) return;
        
        if (staminaBarFill != null)
        {
            staminaBarFill.fillAmount = targetEntity.GetStaminaPercent();
        }
        
        if (staminaText != null)
        {
            staminaText.text = Mathf.Ceil(targetEntity.currentStamina) + " / " + targetEntity.maxStamina;
        }
    }
}

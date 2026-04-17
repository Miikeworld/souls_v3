using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Bonfire : MonoBehaviour
{
    [Header("Bonfire Settings")]
    public string bonfireName = "Unknown Bonfire";
    public float healRange = 3f;
    public bool isLit = false;
    public Transform respawnPoint;
    
    [Header("Visual Effects")]
    public ParticleSystem fireParticles;
    public Light bonfireLight;
    public GameObject bonfireUnlitModel;
    public GameObject bonfireLitModel;
    
    [Header("UI References")]
    public GameObject interactionPrompt;
    public TextMeshProUGUI bonfireNameText;
    public GameObject bonfirePanel;
    public TextMeshProUGUI bonfireTitleText;
    public Button restButton;
    public Button travelButton;
    public Button levelUpButton;
    public Button cancelButton;
    
    [Header("Player Stats Display")]
    public TextMeshProUGUI healthText;
    public TextMeshProUGUI staminaText;
    public TextMeshProUGUI manaText;
    public TextMeshProUGUI potionsText;
    public TextMeshProUGUI currencyText;
    
    private bool playerInRange = false;
    private Entity playerEntity;
    private PlayerController playerController;
    private bool isUIActive = false;
    
    // Auto-created prompt UI
    private static GameObject promptCanvas;
    private static TextMeshProUGUI promptText;
    private static GameObject promptPanel;
    private static Bonfire activePromptBonfire;
    
    void Start()
    {
        Debug.Log("Bonfire Start() called for: " + gameObject.name);
        
        // Ensure bonfire has required components
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogError("Bonfire missing Collider component!");
        }
        else if (!col.isTrigger)
        {
            Debug.LogError("Bonfire Collider must be set as Trigger!");
        }
        
        // Set initial visual state
        UpdateVisualState();
        
        // Auto-create interaction prompt if none exists
        CreatePromptUI();
        
        // Hide UI elements initially
        if (interactionPrompt != null)
            interactionPrompt.SetActive(false);
            
        if (bonfireNameText != null)
            bonfireNameText.gameObject.SetActive(false);
            
        if (bonfirePanel != null)
            bonfirePanel.SetActive(false);
            
        // Setup button listeners
        if (restButton != null)
            restButton.onClick.AddListener(RestAtBonfire);
            
        if (travelButton != null)
            travelButton.onClick.AddListener(OpenTravelMenu);
            
        if (levelUpButton != null)
            levelUpButton.onClick.AddListener(OpenLevelUpMenu);
            
        if (cancelButton != null)
            cancelButton.onClick.AddListener(CloseUI);
    }
    
    void Update()
    {
        // Distance-based detection (more reliable than trigger colliders)
        CheckPlayerDistance();
        
        // Toggle UI with B key when near bonfire
        if (Input.GetKeyDown(KeyCode.B))
        {
            if (isUIActive)
                CloseUI();
            else if (IsNearBonfire())
                OpenUI();
        }
        
        // Close UI with Escape
        if (Input.GetKeyDown(KeyCode.Escape) && isUIActive)
        {
            CloseUI();
        }
        
        // Check for player input when in range
        if (playerInRange && Input.GetKeyDown(KeyCode.E))
        {
            OpenBonfireMenu();
        }
    }
    
    void CheckPlayerDistance()
    {
        if (isUIActive) return;
        
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null) return;
        
        float distance = Vector3.Distance(transform.position, playerObj.transform.position);
        bool wasInRange = playerInRange;
        playerInRange = distance <= healRange;
        
        // Player just entered range
        if (playerInRange && !wasInRange)
        {
            playerEntity = playerObj.GetComponent<Entity>();
            ShowPrompt(this);
        }
        
        // Player just left range
        if (!playerInRange && wasInRange)
        {
            playerEntity = null;
            HidePrompt(this);
        }
    }
    
    void OpenBonfireMenu()
    {
        Debug.Log("OpenBonfireMenu called");
        
        // Hide the interaction prompt
        HidePrompt(this);
        
        // First, do the bonfire interaction (full heal, restore potions, set respawn)
        InteractWithBonfire();
        
        // Then open the UI menu
        OpenUI();
    }
    
    bool IsNearBonfire()
    {
        return playerInRange;
    }
    
    public void OpenUI()
    {
        if (bonfirePanel == null) return;
        
        // Get player references
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerEntity = player.GetComponent<Entity>();
            playerController = player.GetComponent<PlayerController>();
        }
        
        // Update player stats display
        UpdatePlayerStatsDisplay();
        
        // Show UI
        bonfirePanel.SetActive(true);
        isUIActive = true;
        
        // Disable player movement
        if (playerController != null)
            playerController.enabled = false;
            
        // Show cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        Debug.Log("Bonfire UI opened");
    }
    
    void CloseUI()
    {
        if (bonfirePanel == null) return;
        
        // Hide UI
        bonfirePanel.SetActive(false);
        isUIActive = false;
        
        // Re-enable player movement
        if (playerController != null)
            playerController.enabled = true;
            
        // Hide cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        Debug.Log("Bonfire UI closed");
    }
    
    void UpdatePlayerStatsDisplay()
    {
        if (playerEntity == null) return;
        
        // Update health
        if (healthText != null)
            healthText.text = $"Health: {Mathf.Floor(playerEntity.currentHealth)}/{playerEntity.maxHealth}";
            
        // Update stamina
        if (staminaText != null)
            staminaText.text = $"Stamina: {Mathf.Floor(playerEntity.currentStamina)}/{playerEntity.maxStamina}";
            
        // Update mana
        if (manaText != null)
            manaText.text = $"Mana: {Mathf.Floor(playerEntity.currentMana)}/{playerEntity.maxMana}";
            
        // Update potions
        if (potionsText != null)
            potionsText.text = $"Potions: {playerEntity.currentPotions}/{playerEntity.maxPotions}";
            
        // Update currency (placeholder for now)
        if (currencyText != null)
            currencyText.text = "Souls: 0"; // You can implement currency system later
    }
    
    void RestAtBonfire()
    {
        // Fully heal player
        if (playerEntity != null)
        {
            playerEntity.currentHealth = playerEntity.maxHealth;
            playerEntity.currentStamina = playerEntity.maxStamina;
            playerEntity.currentMana = playerEntity.maxMana;
            playerEntity.RestorePotions();
            playerEntity.InvokeResourceEvents();
            
            UpdatePlayerStatsDisplay();
            
            Debug.Log("Rested at bonfire - fully healed and potions restored");
        }
    }
    
    void OpenTravelMenu()
    {
        Debug.Log("Travel menu - implement fast travel between bonfires");
        // You can implement a list of discovered bonfires for fast travel
    }
    
    void OpenLevelUpMenu()
    {
        Debug.Log("Level up menu - implement character progression");
        // You can implement stat allocation, skill upgrades, etc.
    }
    
    static void CreatePromptUI()
    {
        if (promptCanvas != null) return; // already created
        
        // Create Canvas
        promptCanvas = new GameObject("BonfirePromptCanvas");
        Canvas canvas = promptCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        promptCanvas.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        promptCanvas.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
        promptCanvas.AddComponent<GraphicRaycaster>();
        DontDestroyOnLoad(promptCanvas);
        
        // Create background panel at bottom of screen
        promptPanel = new GameObject("PromptPanel");
        promptPanel.transform.SetParent(promptCanvas.transform, false);
        RectTransform panelRect = promptPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.3f, 0.02f);
        panelRect.anchorMax = new Vector2(0.7f, 0.1f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        Image panelImg = promptPanel.AddComponent<Image>();
        panelImg.color = new Color(0f, 0f, 0f, 0.7f);
        
        // Create text
        GameObject textObj = new GameObject("PromptText");
        textObj.transform.SetParent(promptPanel.transform, false);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(10, 0);
        textRect.offsetMax = new Vector2(-10, 0);
        promptText = textObj.AddComponent<TextMeshProUGUI>();
        promptText.alignment = TextAlignmentOptions.Center;
        promptText.fontSize = 24;
        promptText.color = Color.white;
        
        promptPanel.SetActive(false);
    }
    
    static void ShowPrompt(Bonfire bonfire)
    {
        activePromptBonfire = bonfire;
        if (promptText != null)
            promptText.text = $"<size=28>{bonfire.bonfireName}</size>\nPress <color=#FFD700>E</color> to interact";
        if (promptPanel != null)
            promptPanel.SetActive(true);
    }
    
    static void HidePrompt(Bonfire bonfire)
    {
        if (activePromptBonfire == bonfire)
        {
            if (promptPanel != null)
                promptPanel.SetActive(false);
            activePromptBonfire = null;
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
        Debug.Log("Bonfire: OnTriggerEnter with " + other.name + ", tag: " + other.tag);
        
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            playerEntity = other.GetComponent<Entity>();
            
            Debug.Log("Player entered bonfire range. Player entity: " + (playerEntity != null ? "found" : "not found"));
            
            // Show bottom-screen interaction prompt
            ShowPrompt(this);
            
            // Show legacy prompt if assigned
            if (interactionPrompt != null)
                interactionPrompt.SetActive(true);
                
            if (bonfireNameText != null)
            {
                bonfireNameText.text = bonfireName;
                bonfireNameText.gameObject.SetActive(true);
            }
        }
    }
    
    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            playerEntity = null;
            
            // Hide bottom-screen prompt
            HidePrompt(this);
            
            // Hide legacy UI elements
            if (interactionPrompt != null)
                interactionPrompt.SetActive(false);
                
            if (bonfireNameText != null)
                bonfireNameText.gameObject.SetActive(false);
        }
    }
    
    void InteractWithBonfire()
    {
        Debug.Log("InteractWithBonfire called. Player entity: " + (playerEntity != null ? "found" : "not found"));
        
        if (!isLit)
        {
            LightBonfire();
        }
        
        // Full heal and restore potions at bonfire
        if (playerEntity != null)
        {
            HealPlayerAtBonfire();
        }
        else
        {
            Debug.LogError("Player entity is null!");
        }
    }
    
    void LightBonfire()
    {
        isLit = true;
        UpdateVisualState();
        
        Debug.Log("Bonfire '" + bonfireName + "' has been lit!");
        
        // You could save bonfire state here for persistence
        // PlayerPrefs.SetInt("Bonfire_" + bonfireName, 1);
    }
    
    void HealPlayerAtBonfire()
    {
        // Full heal at bonfire (no stamina cost)
        playerEntity.currentHealth = playerEntity.maxHealth;
        playerEntity.currentStamina = playerEntity.maxStamina;
        playerEntity.currentMana = playerEntity.maxMana;
        
        // Restore potions to max
        playerEntity.RestorePotions();
        
        // Update UI
        playerEntity.InvokeResourceEvents();
        
        // Set respawn point
        GameManager.Instance.SetRespawnPoint(this);
        
        // Play healing effect/sound
        PlayHealEffect();
        
        Debug.Log("Fully healed at bonfire '" + bonfireName + "' and potions restored!");
    }
    
    void PlayHealEffect()
    {
        // Visual healing feedback
        Debug.Log("Healing effect played");
        
        // Temporary visual feedback with controlled particles
        if (fireParticles != null)
        {
            var emission = fireParticles.emission;
            var main = fireParticles.main;
            
            // Briefly boost particle rate for healing effect
            emission.rateOverTime = 25f; // Moderate boost
            main.startLifetime = 1.5f; // Slightly shorter for heal effect
            main.startSpeed = 1.5f; // Slightly faster for heal effect
            
            // Reset after duration
            Invoke(nameof(ResetParticleRate), 2f);
        }
    }
    
    void ResetParticleRate()
    {
        if (fireParticles != null)
        {
            var emission = fireParticles.emission;
            var main = fireParticles.main;
            
            // Return to normal fire settings
            emission.rateOverTime = 15f; // Normal rate
            main.startLifetime = 2f; // Normal lifetime
            main.startSpeed = 1f; // Normal speed
        }
    }
    
    void UpdateVisualState()
    {
        // Update particle system
        if (fireParticles != null)
        {
            if (isLit)
            {
                if (!fireParticles.isPlaying)
                    fireParticles.Play();
                
                // Ensure proper emission rate for normal fire
                var emission = fireParticles.emission;
                emission.rateOverTime = 15f; // Normal fire rate
                
                // Control particle lifetime to prevent lingering
                var main = fireParticles.main;
                main.startLifetime = 2f; // Shorter lifetime
                main.startSpeed = 1f; // Slower start speed
            }
            else
            {
                fireParticles.Stop();
            }
        }
        
        // Update light
        if (bonfireLight != null)
        {
            bonfireLight.enabled = isLit;
        }
        
        // Update models
        if (bonfireUnlitModel != null)
            bonfireUnlitModel.SetActive(!isLit);
            
        if (bonfireLitModel != null)
            bonfireLitModel.SetActive(isLit);
    }
    
    // Public methods for external systems
    public void SetLitState(bool lit)
    {
        isLit = lit;
        UpdateVisualState();
    }
    
    public bool IsLit()
    {
        return isLit;
    }
    
    // Draw gizmo for range visualization
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, healRange);
        
        // Draw trigger collider if present
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
        }
    }
    
    private void OnDrawGizmos()
    {
        if (playerInRange)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, healRange);
        }
    }
}

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
    
    // Prompt UI
    private static GameObject promptCanvas;
    private static TextMeshProUGUI promptText;
    private static GameObject promptPanel;
    private static Bonfire activePromptBonfire;

    // Lit banner
    private static GameObject bannerObj;
    private static TextMeshProUGUI bannerText;
    private static CanvasGroup bannerGroup;
    
    void Start()
    {
        Debug.Log("Bonfire Start() called for: " + gameObject.name);

        // Validate trigger collider
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogError("Bonfire missing Collider component!");
        }
        else if (!col.isTrigger)
        {
            Debug.LogError("Bonfire Collider must be set as Trigger!");
        }

        // Set visuals
        UpdateVisualState();

        // Create prompt UI
        CreatePromptUI();

        // Hide UI initially
        if (interactionPrompt != null)
            interactionPrompt.SetActive(false);

        if (bonfireNameText != null)
            bonfireNameText.gameObject.SetActive(false);
            
        if (bonfirePanel != null)
            bonfirePanel.SetActive(false);

        // Bind buttons
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
        // Detect by distance, not triggers
        CheckPlayerDistance();

        // Open/close UI with B
        if (Input.GetKeyDown(KeyCode.B))
        {
            if (isUIActive)
                CloseUI();
            else if (IsNearBonfire())
                OpenUI();
        }

        // Close with Escape
        if (Input.GetKeyDown(KeyCode.Escape) && isUIActive)
        {
            CloseUI();
        }

        // Interact with E
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

        // Player entered range
        if (playerInRange && !wasInRange)
        {
            playerEntity = playerObj.GetComponent<Entity>();
            ShowPrompt(this);
        }

        // Player left range
        if (!playerInRange && wasInRange)
        {
            playerEntity = null;
            HidePrompt(this);
        }
    }
    
    void OpenBonfireMenu()
    {
        Debug.Log("OpenBonfireMenu called");

        // Hide prompt
        HidePrompt(this);

        // Heal, restore potions, set respawn
        InteractWithBonfire();

        // Open menu
        OpenUI();
    }
    
    bool IsNearBonfire()
    {
        return playerInRange;
    }
    
    public void OpenUI()
    {
        // Cache player refs
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerEntity = playerObj.GetComponent<Entity>();
            playerController = playerObj.GetComponent<PlayerController>();
        }

        if (bonfirePanel != null)
        {
            // Use assigned panel if available
            UpdatePlayerStatsDisplay();
            bonfirePanel.SetActive(true);
        }
        else
        {
            // Otherwise use auto menu
            BonfireMenu.Show(this);
        }

        isUIActive = true;

        // Disable player controller
        if (playerController != null)
            playerController.enabled = false;

        // Unlock cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        Debug.Log("Bonfire UI opened");
    }
    
    public void CloseUI()
    {
        if (bonfirePanel != null)
            bonfirePanel.SetActive(false);

        BonfireMenu.Hide();
        isUIActive = false;

        // Re-enable player controller
        if (playerController != null)
            playerController.enabled = true;

        // Lock cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        Debug.Log("Bonfire UI closed");
    }
    
    void UpdatePlayerStatsDisplay()
    {
        if (playerEntity == null) return;

        if (healthText != null)
            healthText.text = $"Health: {Mathf.Floor(playerEntity.currentHealth)}/{playerEntity.maxHealth}";

        if (staminaText != null)
            staminaText.text = $"Stamina: {Mathf.Floor(playerEntity.currentStamina)}/{playerEntity.maxStamina}";

        if (manaText != null)
            manaText.text = $"Mana: {Mathf.Floor(playerEntity.currentMana)}/{playerEntity.maxMana}";

        if (potionsText != null)
            potionsText.text = $"Potions: {playerEntity.currentPotions}/{playerEntity.maxPotions}";

        if (currencyText != null)
            currencyText.text = "Souls: 0"; // Placeholder
    }
    
    public void RestAtBonfire()
    {
        // Full heal and restore potions
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
        // TODO: fast travel list
    }

    void OpenLevelUpMenu()
    {
        Debug.Log("Level up menu - implement character progression");
        // TODO: stat allocation
    }
    
    static void CreatePromptUI()
    {
        if (promptCanvas != null) return; // Skip if exists

        // Create canvas
        promptCanvas = new GameObject("BonfirePromptCanvas");
        Canvas canvas = promptCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        promptCanvas.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        promptCanvas.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
        promptCanvas.AddComponent<GraphicRaycaster>();
        DontDestroyOnLoad(promptCanvas);

        // Create prompt panel
        promptPanel = new GameObject("PromptPanel");
        promptPanel.transform.SetParent(promptCanvas.transform, false);
        RectTransform panelRect = promptPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.3f, 0.02f);
        panelRect.anchorMax = new Vector2(0.7f, 0.1f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        Image panelImg = promptPanel.AddComponent<Image>();
        panelImg.color = new Color(0f, 0f, 0f, 0.7f);

        // Create prompt text
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

            // Show prompt
            ShowPrompt(this);

            // Show legacy prompt
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

            // Hide prompt
            HidePrompt(this);

            // Hide legacy UI
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

        // Heal and restore potions
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

        // Auto-create fire and light if missing
        if (fireParticles == null)
            CreateAutoFireEffect();
        if (bonfireLight == null)
            CreateAutoLight();

        UpdateVisualState();

        // Show banner
        ShowLitBanner();
        
        Debug.Log("Bonfire '" + bonfireName + "' has been lit!");

        // TODO: save bonfire state
    }

    void CreateAutoFireEffect()
    {
        GameObject fireObj = new GameObject("AutoFireParticles");
        fireObj.transform.SetParent(transform, false);
        fireObj.transform.localPosition = Vector3.up * 0.3f;

        fireParticles = fireObj.AddComponent<ParticleSystem>();

        var main = fireParticles.main;
        main.startLifetime = 1.2f;
        main.startSpeed = 1.5f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.45f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.6f, 0.1f), new Color(1f, 0.25f, 0f));
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 80;

        var emission = fireParticles.emission;
        emission.rateOverTime = 15f;

        var shape = fireParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 12f;
        shape.radius = 0.25f;

        // Fade color/size over lifetime
        var colorOverLifetime = fireParticles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new[] {
                new GradientColorKey(new Color(1f, 0.85f, 0.3f), 0f),
                new GradientColorKey(new Color(1f, 0.4f, 0f), 0.5f),
                new GradientColorKey(new Color(0.4f, 0.05f, 0f), 1f)
            },
            new[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.8f, 0.6f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = grad;

        var sizeOverLifetime = fireParticles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f,
            new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0.2f)));

        // Use unlit particle material
        ParticleSystemRenderer psr = fireObj.GetComponent<ParticleSystemRenderer>();
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
        if (mat.shader == null || !mat.shader.isSupported)
            mat = new Material(Shader.Find("Particles/Standard Unlit"));
        psr.material = mat;

        Debug.Log("[Bonfire] Auto-created fire particle effect.");
    }

    void CreateAutoLight()
    {
        GameObject lightObj = new GameObject("AutoBonfireLight");
        lightObj.transform.SetParent(transform, false);
        lightObj.transform.localPosition = Vector3.up * 0.8f;

        bonfireLight = lightObj.AddComponent<Light>();
        bonfireLight.type = LightType.Point;
        bonfireLight.color = new Color(1f, 0.55f, 0.2f);
        bonfireLight.intensity = 2.5f;
        bonfireLight.range = 8f;

        Debug.Log("[Bonfire] Auto-created bonfire light.");
    }

    void ShowLitBanner()
    {
        EnsureBanner();
        if (bannerText != null)
            bannerText.text = "BONFIRE LIT";
        StartCoroutine(FadeBanner());
    }

    static void EnsureBanner()
    {
        if (bannerObj != null) return;

        GameObject canvasObj = new GameObject("BonfireBannerCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 150;
        canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObj.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
        DontDestroyOnLoad(canvasObj);

        bannerObj = new GameObject("BonfireLitBanner");
        bannerObj.transform.SetParent(canvasObj.transform, false);
        RectTransform rect = bannerObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0.42f);
        rect.anchorMax = new Vector2(1f, 0.58f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        // Dark background strip
        Image bg = bannerObj.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.6f);

        GameObject textObj = new GameObject("BannerText");
        textObj.transform.SetParent(bannerObj.transform, false);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        bannerText = textObj.AddComponent<TextMeshProUGUI>();
        bannerText.alignment = TextAlignmentOptions.Center;
        bannerText.fontSize = 64;
        bannerText.color = new Color(1f, 0.8f, 0.3f); // warm gold
        bannerText.characterSpacing = 12f;

        bannerGroup = bannerObj.AddComponent<CanvasGroup>();
        bannerGroup.alpha = 0f;
        bannerObj.SetActive(false);
    }

    System.Collections.IEnumerator FadeBanner()
    {
        if (bannerObj == null || bannerGroup == null) yield break;

        bannerObj.SetActive(true);

        // Fade in
        for (float t = 0f; t < 0.5f; t += Time.deltaTime)
        {
            bannerGroup.alpha = t / 0.5f;
            yield return null;
        }
        bannerGroup.alpha = 1f;

        // Hold
        yield return new WaitForSeconds(2f);

        // Fade out
        for (float t = 0f; t < 1f; t += Time.deltaTime)
        {
            bannerGroup.alpha = 1f - t;
            yield return null;
        }
        bannerGroup.alpha = 0f;
        bannerObj.SetActive(false);
    }
    
    void HealPlayerAtBonfire()
    {
        // Full heal
        playerEntity.currentHealth = playerEntity.maxHealth;
        playerEntity.currentStamina = playerEntity.maxStamina;
        playerEntity.currentMana = playerEntity.maxMana;

        // Restore potions
        playerEntity.RestorePotions();

        // Update UI
        playerEntity.InvokeResourceEvents();

        // Set respawn
        GameManager.Instance.SetRespawnPoint(this);

        // Play effect
        PlayHealEffect();
        
        Debug.Log("Fully healed at bonfire '" + bonfireName + "' and potions restored!");
    }
    
    void PlayHealEffect()
    {
        // Visual feedback
        Debug.Log("Healing effect played");

        // Boost fire particles
        if (fireParticles != null)
        {
            var emission = fireParticles.emission;
            var main = fireParticles.main;
            
            // Boost particle rate
            emission.rateOverTime = 25f;
            main.startLifetime = 1.5f; // Shorter lifetime
            main.startSpeed = 1.5f; // Slightly faster

            // Reset after 2s
            Invoke(nameof(ResetParticleRate), 2f);
        }
    }
    
    void ResetParticleRate()
    {
        if (fireParticles != null)
        {
            var emission = fireParticles.emission;
            var main = fireParticles.main;

            // Reset to normal
            emission.rateOverTime = 15f;
            main.startLifetime = 2f;
            main.startSpeed = 1f;
        }
    }
    
    void UpdateVisualState()
    {
        // Toggle fire particles
        if (fireParticles != null)
        {
            if (isLit)
            {
                if (!fireParticles.isPlaying)
                    fireParticles.Play();

                // Normal emission
                var emission = fireParticles.emission;
                emission.rateOverTime = 15f;

                // Normal lifetime
                var main = fireParticles.main;
                main.startLifetime = 2f;
                main.startSpeed = 1f;
            }
            else
            {
                fireParticles.Stop();
            }
        }

        // Toggle light
        if (bonfireLight != null)
        {
            bonfireLight.enabled = isLit;
        }

        // Toggle models
        if (bonfireUnlitModel != null)
            bonfireUnlitModel.SetActive(!isLit);
            
        if (bonfireLitModel != null)
            bonfireLitModel.SetActive(isLit);
    }
    
    // Public API
    public void SetLitState(bool lit)
    {
        isLit = lit;
        UpdateVisualState();
    }
    
    public bool IsLit()
    {
        return isLit;
    }
    
    // Draw range gizmo
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, healRange);

        // Draw trigger bounds
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

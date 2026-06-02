using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Souls-style boss health bar that appears at the bottom center of the screen.
/// Auto-creates its own UI at runtime — just add this component to ANY GameObject
/// and assign the boss reference (or it auto-finds the first BossController).
///
/// SETUP: Add this to a GameObject in your scene. No manual UI setup needed.
/// </summary>
public class BossHealthBarUI : MonoBehaviour
{
    [Header("Boss Reference")]
    [Tooltip("Leave empty to auto-find the first BossController in the scene.")]
    public BossController boss;

    [Header("Bar Settings")]
    public float barWidth = 600f;
    public float barHeight = 16f;
    public float yOffset = 60f;
    public float fillSpeed = 4f;
    public float showDistance = 30f;

    [Header("Colors")]
    public Color barColor = new Color(0.85f, 0.15f, 0.1f, 1f);
    public Color bgColor = new Color(0.12f, 0.12f, 0.12f, 0.85f);
    public Color damageTrailColor = new Color(0.9f, 0.6f, 0.1f, 0.8f);
    public Color nameColor = new Color(0.95f, 0.9f, 0.8f, 1f);

    // Runtime UI references
    private Canvas canvas;
    private GameObject barRoot;
    private Image bgImage;
    private Image trailImage;
    private Image fillImage;
    private Text nameText;

    private float targetFill = 1f;
    private float trailFill = 1f;
    private float lastKnownHealth = -1f;
    private bool isVisible = false;
    private Transform player;

    void Start()
    {
        PlayerController pc = FindAnyObjectByType<PlayerController>();
        if (pc != null) player = pc.transform;

        CreateUI();
        SetVisible(false);

        // Bind to the best boss (the assigned one if valid, else auto-pick)
        BindBoss(boss != null && boss.isActiveAndEnabled ? boss : FindBestBoss());
    }

    void OnDestroy()
    {
        if (boss != null)
            boss.OnHealthChanged -= OnBossHealthChanged;
    }

    /// <summary>Subscribe to a boss's health event, unsubscribing from the previous one.</summary>
    void BindBoss(BossController newBoss)
    {
        if (newBoss == boss) return;

        if (boss != null)
            boss.OnHealthChanged -= OnBossHealthChanged;

        boss = newBoss;
        lastKnownHealth = -1f; // force resync on next Update

        if (boss != null)
        {
            boss.OnHealthChanged += OnBossHealthChanged;
            if (nameText != null) nameText.text = boss.bossName;
        }
    }

    /// <summary>Pick the active, non-dead boss that is being damaged, or nearest the player.</summary>
    BossController FindBestBoss()
    {
        BossController[] all = FindObjectsByType<BossController>(FindObjectsSortMode.None);
        BossController best = null;
        float bestScore = float.MaxValue;

        foreach (BossController b in all)
        {
            if (b == null || !b.isActiveAndEnabled || b.isDead) continue;

            // Strongly prefer a boss that has taken damage (it's the one being fought)
            float damageBias = b.currentHealth < b.maxHealth ? 0f : 1000f;
            float dist = player != null ? Vector3.Distance(player.position, b.transform.position) : 0f;
            float score = damageBias + dist;

            if (score < bestScore)
            {
                bestScore = score;
                best = b;
            }
        }
        return best;
    }

    void Update()
    {
        // Rebuild UI if it was destroyed (e.g. CharacterLoader cleanup nuked the canvas)
        if (canvas == null || barRoot == null)
        {
            CreateUI();
            SetVisible(false);
        }

        // Re-acquire player if it wasn't ready at Start (e.g. scene loaded from
        // character creation spawns the player after this component's Start runs)
        if (player == null)
        {
            PlayerController pc = FindAnyObjectByType<PlayerController>();
            if (pc != null) player = pc.transform;
        }

        // Re-bind if the current boss is gone/inactive, or another boss is being damaged
        if (boss == null || !boss.isActiveAndEnabled || boss.isDead)
        {
            BindBoss(FindBestBoss());
            if (boss == null) return;
        }
        else if (boss.currentHealth >= boss.maxHealth)
        {
            // Currently bound boss is untouched — is another boss actually taking damage?
            BossController damaged = FindBestBoss();
            if (damaged != null && damaged != boss && damaged.currentHealth < damaged.maxHealth)
                BindBoss(damaged);
        }

        // Poll health every frame as fallback (in case event subscription failed)
        if (boss.currentHealth != lastKnownHealth)
        {
            lastKnownHealth = boss.currentHealth;
            targetFill = boss.GetHealthPercent();
        }

        // Show bar when: boss has taken damage OR boss is actively fighting
        bool shouldShow = !boss.isDead
            && player != null
            && Vector3.Distance(player.position, boss.transform.position) <= showDistance
            && (boss.currentState != BossController.BossState.Idle || boss.currentHealth < boss.maxHealth);

        if (shouldShow && !isVisible) SetVisible(true);
        if (!shouldShow && isVisible)
        {
            // Keep visible briefly after boss dies for dramatic effect
            if (boss.isDead)
                Invoke(nameof(HideBar), 2f);
            else
                SetVisible(false);
        }

        if (!isVisible) return;

        // Smooth fill
        if (fillImage != null)
            fillImage.fillAmount = Mathf.Lerp(fillImage.fillAmount, targetFill, fillSpeed * Time.deltaTime);

        // Trail (delayed damage indicator) — follows more slowly
        if (trailImage != null)
        {
            if (trailFill > targetFill)
                trailFill = Mathf.Lerp(trailFill, targetFill, fillSpeed * 0.5f * Time.deltaTime);
            else
                trailFill = targetFill;
            trailImage.fillAmount = trailFill;
        }
    }

    void OnBossHealthChanged()
    {
        if (boss == null) return;
        targetFill = boss.GetHealthPercent();
        if (!isVisible && !boss.isDead)
            SetVisible(true);
    }

    void HideBar() { SetVisible(false); }

    void SetVisible(bool visible)
    {
        isVisible = visible;
        if (barRoot != null) barRoot.SetActive(visible);
    }

    // ═══════════════════════════════════════════════════════════════
    //  AUTO-CREATE UI
    // ═══════════════════════════════════════════════════════════════
    void CreateUI()
    {
        // ALWAYS create our own dedicated canvas. Do NOT reuse an existing one —
        // CharacterLoader.DestroyTextForever() destroys canvases containing demo
        // text for 5s after scene load, which would take our bar down with it.
        GameObject canvasGO = new GameObject("BossBarCanvas");
        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        canvasGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGO.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();

        // Root container — anchored to bottom center
        barRoot = new GameObject("BossHealthBar");
        barRoot.transform.SetParent(canvas.transform, false);
        RectTransform rootRT = barRoot.AddComponent<RectTransform>();
        rootRT.anchorMin = new Vector2(0.5f, 0f);
        rootRT.anchorMax = new Vector2(0.5f, 0f);
        rootRT.pivot = new Vector2(0.5f, 0f);
        rootRT.anchoredPosition = new Vector2(0f, yOffset);
        rootRT.sizeDelta = new Vector2(barWidth, barHeight + 30f);

        // Boss name text
        GameObject nameGO = new GameObject("BossName");
        nameGO.transform.SetParent(barRoot.transform, false);
        nameText = nameGO.AddComponent<Text>();
        nameText.text = boss != null ? boss.bossName : "???";
        nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (nameText.font == null)
            nameText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        nameText.fontSize = 18;
        nameText.alignment = TextAnchor.MiddleCenter;
        nameText.color = nameColor;
        RectTransform nameRT = nameGO.GetComponent<RectTransform>();
        nameRT.anchorMin = new Vector2(0f, 1f);
        nameRT.anchorMax = new Vector2(1f, 1f);
        nameRT.pivot = new Vector2(0.5f, 0f);
        nameRT.anchoredPosition = new Vector2(0f, 2f);
        nameRT.sizeDelta = new Vector2(0f, 24f);

        // Background bar
        GameObject bgGO = CreateBarImage("BG", barRoot.transform, bgColor);
        bgImage = bgGO.GetComponent<Image>();
        bgImage.type = Image.Type.Sliced;
        RectTransform bgRT = bgGO.GetComponent<RectTransform>();
        bgRT.anchorMin = new Vector2(0f, 0f);
        bgRT.anchorMax = new Vector2(1f, 0f);
        bgRT.pivot = new Vector2(0.5f, 0f);
        bgRT.anchoredPosition = Vector2.zero;
        bgRT.sizeDelta = new Vector2(0f, barHeight);

        // Damage trail bar (orange, fades behind the red)
        GameObject trailGO = CreateBarImage("Trail", bgGO.transform, damageTrailColor);
        trailImage = trailGO.GetComponent<Image>();
        trailImage.type = Image.Type.Filled;
        trailImage.fillMethod = Image.FillMethod.Horizontal;
        trailImage.fillAmount = 1f;
        RectTransform trailRT = trailGO.GetComponent<RectTransform>();
        trailRT.anchorMin = Vector2.zero;
        trailRT.anchorMax = Vector2.one;
        trailRT.offsetMin = Vector2.one * 2f;
        trailRT.offsetMax = -Vector2.one * 2f;

        // Health fill bar (red)
        GameObject fillGO = CreateBarImage("Fill", bgGO.transform, barColor);
        fillImage = fillGO.GetComponent<Image>();
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillAmount = 1f;
        RectTransform fillRT = fillGO.GetComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = Vector2.one;
        fillRT.offsetMin = Vector2.one * 2f;
        fillRT.offsetMax = -Vector2.one * 2f;

        trailFill = 1f;
        targetFill = boss != null ? boss.GetHealthPercent() : 1f;
    }

    GameObject CreateBarImage(string name, Transform parent, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Image img = go.AddComponent<Image>();
        img.color = color;
        // A sprite is REQUIRED for Image.Type.Filled to clip visually
        img.sprite = GetWhiteSprite();
        return go;
    }

    private static Sprite _whiteSprite;
    static Sprite GetWhiteSprite()
    {
        if (_whiteSprite == null)
        {
            Texture2D tex = Texture2D.whiteTexture;
            _whiteSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        }
        return _whiteSprite;
    }

    Canvas FindExistingOverlayCanvas()
    {
        foreach (Canvas c in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (c.renderMode == RenderMode.ScreenSpaceOverlay && c.gameObject.activeInHierarchy)
                return c;
        }
        return null;
    }
}

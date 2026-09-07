using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Auto-created boss health bar that binds to the nearest live BossController.
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
    public float showDistance = 80f;

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
    private bool isVisible = false;
    private Transform player;
    private bool loggedNoBoss = false;

    void Start()
    {
        // Find the live boss instance in the scene
        if (boss == null || !boss.gameObject.scene.IsValid())
            boss = FindAnyObjectByType<BossController>();

        PlayerController pc = FindAnyObjectByType<PlayerController>();
        if (pc != null) player = pc.transform;

        CreateUI();
        SetVisible(false);

        if (boss != null)
            boss.OnHealthChanged += OnBossHealthChanged;
    }

    void OnDestroy()
    {
        if (boss != null)
            boss.OnHealthChanged -= OnBossHealthChanged;
    }

    void Update()
    {
        // Re-find player if spawned later
        if (player == null)
        {
            PlayerController pc = FindAnyObjectByType<PlayerController>();
            if (pc != null) player = pc.transform;
        }

        // Refind live boss if assigned one is invalid/dead
        if (boss == null || boss.isDead || !boss.gameObject.scene.IsValid())
            boss = FindBestBoss();

        if (boss == null)
        {
            if (isVisible) SetVisible(false);
            if (!loggedNoBoss)
            {
                Debug.Log("[BossHealthBarUI] No live BossController found in scene.");
                loggedNoBoss = true;
            }
            return;
        }
        else
        {
            loggedNoBoss = false;
        }

        // Keep fill current even when hidden
        targetFill = boss.GetHealthPercent();

        // Show when player is in range and boss is alive; hide briefly on death
        bool shouldShow = !boss.isDead
            && (player == null
            || Vector3.Distance(player.position, boss.transform.position) <= Mathf.Max(showDistance, 80f));

        if (shouldShow && !isVisible) SetVisible(true);
        if (!shouldShow && isVisible)
        {
            if (boss.isDead)
                Invoke(nameof(HideBar), 2f);
            else
                SetVisible(false);
        }

        if (!isVisible) return;

        // Smooth fill
        if (fillImage != null)
            fillImage.fillAmount = Mathf.Lerp(fillImage.fillAmount, targetFill, fillSpeed * Time.deltaTime);

        // Delayed damage trail
        if (trailImage != null)
        {
            if (trailFill > targetFill)
                trailFill = Mathf.Lerp(trailFill, targetFill, fillSpeed * 0.5f * Time.deltaTime);
            else
                trailFill = targetFill;
            trailImage.fillAmount = trailFill;
        }
    }

    BossController FindBestBoss()
    {
        BossController[] all = FindObjectsByType<BossController>(FindObjectsSortMode.None);
        BossController best = null;
        float bestDist = float.MaxValue;
        foreach (BossController b in all)
        {
            if (b.isDead) continue;
            if (player == null) { best = b; break; }
            float d = Vector3.Distance(player.position, b.transform.position);
            if (d < bestDist)
            {
                bestDist = d;
                best = b;
            }
        }
        return best;
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

    void CreateUI()
    {
        // Create overlay canvas if needed
        canvas = FindExistingOverlayCanvas();
        if (canvas == null)
        {
            GameObject canvasGO = new GameObject("BossBarCanvas");
            canvasGO.AddComponent<RectTransform>();
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            canvasGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGO.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
            canvasGO.AddComponent<GraphicRaycaster>();
        }

        // Bar root
        barRoot = new GameObject("BossHealthBar");
        barRoot.transform.SetParent(canvas.transform, false);
        RectTransform rootRT = barRoot.AddComponent<RectTransform>();
        rootRT.anchorMin = new Vector2(0.5f, 0f);
        rootRT.anchorMax = new Vector2(0.5f, 0f);
        rootRT.pivot = new Vector2(0.5f, 0f);
        rootRT.anchoredPosition = new Vector2(0f, yOffset);
        rootRT.sizeDelta = new Vector2(barWidth, barHeight + 30f);

        // Boss name
        GameObject nameGO = new GameObject("BossName");
        nameGO.transform.SetParent(barRoot.transform, false);
        nameGO.AddComponent<RectTransform>();
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

        // Background
        GameObject bgGO = CreateBarImage("BG", barRoot.transform, bgColor);
        bgImage = bgGO.GetComponent<Image>();
        bgImage.type = Image.Type.Simple;
        RectTransform bgRT = bgGO.GetComponent<RectTransform>();
        bgRT.anchorMin = new Vector2(0f, 0f);
        bgRT.anchorMax = new Vector2(1f, 0f);
        bgRT.pivot = new Vector2(0.5f, 0f);
        bgRT.anchoredPosition = Vector2.zero;
        bgRT.sizeDelta = new Vector2(0f, barHeight);

        // Damage trail
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

        // Health fill
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
        go.AddComponent<RectTransform>();
        Image img = go.AddComponent<Image>();
        img.color = color;
        img.sprite = WhiteSprite();
        return go;
    }

    Sprite WhiteSprite()
    {
        Texture2D tex = new Texture2D(2, 2);
        Color[] pixels = new Color[4] { Color.white, Color.white, Color.white, Color.white };
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f));
    }

    Canvas FindExistingOverlayCanvas()
    {
        // Only reuse a canvas created by this bar
        foreach (Canvas c in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (c.renderMode == RenderMode.ScreenSpaceOverlay &&
                c.gameObject.activeInHierarchy &&
                c.name == "BossBarCanvas")
                return c;
        }
        return null;
    }
}

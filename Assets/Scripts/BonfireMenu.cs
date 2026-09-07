using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Auto-generated Souls-style bonfire menu with a level-up panel.
/// Used automatically by Bonfire when no bonfirePanel is assigned in the inspector.
/// Rest / Level Up / Leave. Level Up spends souls to raise stats.
/// </summary>
public class BonfireMenu : MonoBehaviour
{
    private static BonfireMenu instance;

    private Bonfire activeBonfire;
    private PlayerController player;

    private GameObject canvasObj;
    private GameObject mainPanel;
    private GameObject levelPanel;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI levelText;
    private TextMeshProUGUI levelInfoText;
    private TextMeshProUGUI vigorLabel, mindLabel, enduranceLabel, strengthLabel;

    private static readonly Color PanelBg = new Color(0.05f, 0.045f, 0.04f, 0.93f);
    private static readonly Color Gold = new Color(0.85f, 0.72f, 0.42f);
    private static readonly Color TextDim = new Color(0.8f, 0.78f, 0.72f);
    private static readonly Color ButtonBg = new Color(0.13f, 0.12f, 0.1f, 0.95f);
    private static readonly Color ButtonHover = new Color(0.28f, 0.24f, 0.16f, 0.95f);

    public static bool IsOpen => instance != null && instance.canvasObj != null && instance.canvasObj.activeSelf;

    public static void Show(Bonfire bonfire)
    {
        if (instance == null)
        {
            GameObject go = new GameObject("BonfireMenu");
            instance = go.AddComponent<BonfireMenu>();
            DontDestroyOnLoad(go);
            instance.BuildUI();
        }

        instance.activeBonfire = bonfire;
        instance.player = FindAnyObjectByType<PlayerController>();
        instance.canvasObj.SetActive(true);
        instance.ShowMain();
    }

    public static void Hide()
    {
        if (instance != null && instance.canvasObj != null)
            instance.canvasObj.SetActive(false);
    }

    void ShowMain()
    {
        mainPanel.SetActive(true);
        levelPanel.SetActive(false);
        if (titleText != null && activeBonfire != null)
            titleText.text = activeBonfire.bonfireName;
        RefreshLevel();
    }

    void ShowLevelUp()
    {
        mainPanel.SetActive(false);
        levelPanel.SetActive(true);
        RefreshLevelPanel();
    }

    void RefreshLevel()
    {
        if (player == null || levelText == null) return;
        levelText.text = $"Level {player.playerLevel}";
    }

    void RefreshLevelPanel()
    {
        if (player == null) return;

        int cost = player.NextLevelCost;
        if (levelInfoText != null)
            levelInfoText.text = $"Level {player.playerLevel}     Souls <color=#D9B86A>{player.souls}</color>     Next level: {cost} souls";

        if (vigorLabel != null) vigorLabel.text = $"<b>Vigor</b>  {player.vigor}   <size=70%>(Max HP {player.maxHealth})</size>";
        if (mindLabel != null) mindLabel.text = $"<b>Mind</b>  {player.mind}   <size=70%>(Max FP {player.maxMana})</size>";
        if (enduranceLabel != null) enduranceLabel.text = $"<b>Endurance</b>  {player.endurance}   <size=70%>(Max Stamina {player.maxStamina})</size>";
        if (strengthLabel != null) strengthLabel.text = $"<b>Strength</b>  {player.strength}   <size=70%>(Attack {player.attackDamage})</size>";
    }

    void TryLevelUp(int stat)
    {
        if (player == null) return;

        int cost = player.NextLevelCost;
        if (player.souls < cost)
        {
            Debug.Log("[BonfireMenu] Not enough souls to level up.");
            return;
        }

        player.souls -= cost;
        player.playerLevel++;

        switch (stat)
        {
            case 0: // Vigor
                player.vigor++;
                player.maxHealth += 20f;
                player.currentHealth += 20f;
                break;
            case 1: // Mind
                player.mind++;
                player.maxMana += 10f;
                player.currentMana += 10f;
                break;
            case 2: // Endurance
                player.endurance++;
                player.maxStamina += 10f;
                player.currentStamina += 10f;
                break;
            case 3: // Strength
                player.strength++;
                player.attackDamage += 2f;
                break;
        }

        player.InvokeResourceEvents();
        RefreshLevelPanel();
    }

    void Leave()
    {
        if (activeBonfire != null)
            activeBonfire.CloseUI();
        else
            Hide();
    }

    // ══════════════════════════════════════════════════════════════
    //  UI CONSTRUCTION
    // ══════════════════════════════════════════════════════════════
    void BuildUI()
    {
        canvasObj = new GameObject("BonfireMenuCanvas");
        canvasObj.transform.SetParent(transform, false);
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 120;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<GraphicRaycaster>();

        // Dim the whole screen slightly behind the menu
        GameObject dim = new GameObject("Dim");
        dim.transform.SetParent(canvasObj.transform, false);
        Stretch(dim.AddComponent<RectTransform>(), Vector2.zero, Vector2.one);
        dim.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.45f);

        mainPanel = BuildPanel("MainPanel");
        levelPanel = BuildPanel("LevelPanel");

        BuildMainPanelContent();
        BuildLevelPanelContent();

        canvasObj.SetActive(false);
    }

    GameObject BuildPanel(string name)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(canvasObj.transform, false);
        RectTransform rect = panel.AddComponent<RectTransform>();
        // Full-screen panel
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
        Image bg = panel.AddComponent<Image>();
        bg.color = PanelBg;

        // Thin gold border line at the top
        GameObject line = new GameObject("GoldLine");
        line.transform.SetParent(panel.transform, false);
        RectTransform lineRect = line.AddComponent<RectTransform>();
        lineRect.anchorMin = new Vector2(0.05f, 1f);
        lineRect.anchorMax = new Vector2(0.95f, 1f);
        lineRect.pivot = new Vector2(0.5f, 1f);
        lineRect.anchoredPosition = new Vector2(0f, -58f);
        lineRect.sizeDelta = new Vector2(0f, 2f);
        line.AddComponent<Image>().color = Gold;

        return panel;
    }

    void BuildMainPanelContent()
    {
        titleText = MakeText(mainPanel.transform, "Title", "Bonfire", 54, Gold);
        Anchor(titleText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -10f), new Vector2(0f, 72f));

        levelText = MakeText(mainPanel.transform, "Level", "", 36, TextDim);
        Anchor(levelText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -85f), new Vector2(0f, 48f));

        float y = 60f;
        MakeButton(mainPanel.transform, "Level Up", y, ShowLevelUp);
        MakeButton(mainPanel.transform, "Leave", y - 120f, Leave);
    }

    void BuildLevelPanelContent()
    {
        TextMeshProUGUI lvTitle = MakeText(levelPanel.transform, "Title", "Level Up", 54, Gold);
        Anchor(lvTitle.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -10f), new Vector2(0f, 72f));

        levelInfoText = MakeText(levelPanel.transform, "Info", "", 28, TextDim);
        Anchor(levelInfoText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -92f), new Vector2(0f, 44f));

        float y = 40f;
        vigorLabel = MakeStatRow(levelPanel.transform, "Vigor", y, () => TryLevelUp(0));
        mindLabel = MakeStatRow(levelPanel.transform, "Mind", y - 100f, () => TryLevelUp(1));
        enduranceLabel = MakeStatRow(levelPanel.transform, "Endurance", y - 200f, () => TryLevelUp(2));
        strengthLabel = MakeStatRow(levelPanel.transform, "Strength", y - 300f, () => TryLevelUp(3));

        MakeButton(levelPanel.transform, "Back", y - 420f, ShowMain);
    }

    TextMeshProUGUI MakeStatRow(Transform parent, string statName, float y, UnityEngine.Events.UnityAction onPlus)
    {
        // Row container — centered, fixed width
        GameObject row = new GameObject(statName + "Row");
        row.transform.SetParent(parent, false);
        RectTransform rowRect = row.AddComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0.5f, 0.5f);
        rowRect.anchorMax = new Vector2(0.5f, 0.5f);
        rowRect.pivot = new Vector2(0.5f, 0.5f);
        rowRect.anchoredPosition = new Vector2(0f, y);
        rowRect.sizeDelta = new Vector2(680f, 64f);

        // Label
        TextMeshProUGUI label = MakeText(row.transform, "Label", statName, 22, TextDim);
        RectTransform labelRect = label.rectTransform;
        labelRect.anchorMin = new Vector2(0f, 0f);
        labelRect.anchorMax = new Vector2(0.78f, 1f);
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        label.alignment = TextAlignmentOptions.MidlineLeft;

        // Plus button
        GameObject btnObj = new GameObject("PlusButton");
        btnObj.transform.SetParent(row.transform, false);
        RectTransform btnRect = btnObj.AddComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.82f, 0.1f);
        btnRect.anchorMax = new Vector2(0.98f, 0.9f);
        btnRect.offsetMin = Vector2.zero;
        btnRect.offsetMax = Vector2.zero;

        Image btnImg = btnObj.AddComponent<Image>();
        btnImg.color = ButtonBg;
        Button btn = btnObj.AddComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.highlightedColor = ButtonHover;
        colors.pressedColor = Gold;
        btn.colors = colors;
        btn.onClick.AddListener(onPlus);

        TextMeshProUGUI plus = MakeText(btnObj.transform, "PlusText", "+", 34, Gold);
        Stretch(plus.rectTransform, Vector2.zero, Vector2.one);
        plus.alignment = TextAlignmentOptions.Center;

        return label;
    }

    void MakeButton(Transform parent, string label, float y, UnityEngine.Events.UnityAction onClick)
    {
        GameObject btnObj = new GameObject(label + "Button");
        btnObj.transform.SetParent(parent, false);
        RectTransform rect = btnObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, y);
        rect.sizeDelta = new Vector2(620f, 80f);

        Image img = btnObj.AddComponent<Image>();
        img.color = ButtonBg;
        Button btn = btnObj.AddComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.highlightedColor = ButtonHover;
        colors.pressedColor = Gold;
        btn.colors = colors;
        btn.onClick.AddListener(onClick);

        TextMeshProUGUI text = MakeText(btnObj.transform, "Text", label, 34, TextDim);
        Stretch(text.rectTransform, Vector2.zero, Vector2.one);
        text.alignment = TextAlignmentOptions.Center;
    }

    TextMeshProUGUI MakeText(Transform parent, string name, string content, int size, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.AddComponent<RectTransform>();
        TextMeshProUGUI text = obj.AddComponent<TextMeshProUGUI>();
        text.text = content;
        text.fontSize = size;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        return text;
    }

    static void Stretch(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    static void Anchor(RectTransform rect, Vector2 min, Vector2 max, Vector2 pos, Vector2 size)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;
    }
}

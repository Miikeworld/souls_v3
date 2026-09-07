using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class MainMenuUI : MonoBehaviour
{
    [Header("UI Colors")]
    public Color backgroundColor = new Color(0, 0, 0, 0.85f);
    public Color buttonColor = new Color(0.15f, 0.15f, 0.15f, 1f);
    public Color buttonHighlight = new Color(0.3f, 0.25f, 0.15f, 1f);
    public Color textColor = new Color(0.9f, 0.85f, 0.7f, 1f);

    [Header("Scenes")]
    public string newGameScene = "CharacterCreation";
    public string continueScene = "Fantasy";

    Canvas menuCanvas;
    GameObject settingsPanel;
    Sprite whiteSprite;

    Sprite GetWhiteSprite()
    {
        if (whiteSprite == null)
        {
            Texture2D tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            whiteSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect);
        }
        return whiteSprite;
    }

    void Start()
    {
        EnsureEventSystem();
        BuildMenu();
    }

    void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();

            // Use the new Input System module if available, otherwise fall back.
            var inputModule = es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            if (inputModule == null)
                es.AddComponent<StandaloneInputModule>();
        }
    }

    void BuildMenu()
    {
        // Canvas
        GameObject canvasGO = new GameObject("MainMenuCanvas");
        menuCanvas = canvasGO.AddComponent<Canvas>();
        menuCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        menuCanvas.sortingOrder = 100;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasGO.AddComponent<GraphicRaycaster>();

        RectTransform canvasRT = canvasGO.GetComponent<RectTransform>();
        canvasRT.anchorMin = Vector2.zero;
        canvasRT.anchorMax = Vector2.one;
        canvasRT.sizeDelta = Vector2.zero;

        // Background
        GameObject bg = CreatePanel("Background", canvasRT, Vector2.zero, Vector2.one, Vector2.zero, backgroundColor);

        // Title
        GameObject titleGO = new GameObject("Title");
        titleGO.transform.SetParent(canvasRT, false);
        Text titleText = titleGO.AddComponent<Text>();
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize = 72;
        titleText.color = textColor;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.text = "SOULS";

        RectTransform titleRT = titleGO.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0, 0.75f);
        titleRT.anchorMax = new Vector2(1, 0.95f);
        titleRT.sizeDelta = Vector2.zero;

        // Button container
        GameObject buttonContainer = CreatePanel("ButtonContainer", canvasRT, new Vector2(0.35f, 0.25f), new Vector2(0.65f, 0.65f), Vector2.zero, Color.clear);

        VerticalLayoutGroup vlg = buttonContainer.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 20;
        vlg.childControlHeight = false;
        vlg.childControlWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childAlignment = TextAnchor.MiddleCenter;

        ContentSizeFitter csf = buttonContainer.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        AddButton("New Game", buttonContainer.GetComponent<RectTransform>(), NewGame);
        AddButton("Continue", buttonContainer.GetComponent<RectTransform>(), ContinueGame);
        AddButton("Settings", buttonContainer.GetComponent<RectTransform>(), OpenSettings);
        AddButton("Exit", buttonContainer.GetComponent<RectTransform>(), ExitGame);

        // Settings panel (initially hidden)
        BuildSettingsPanel(canvasRT);
    }

    GameObject CreatePanel(string name, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 sizeDelta, Color color)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);

        Image img = panel.AddComponent<Image>();
        img.sprite = GetWhiteSprite();
        img.type = Image.Type.Simple;
        img.color = color;

        RectTransform rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.sizeDelta = sizeDelta;

        return panel;
    }

    void AddButton(string label, RectTransform parent, UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonGO = new GameObject(label + "Button");
        buttonGO.transform.SetParent(parent, false);

        Image img = buttonGO.AddComponent<Image>();
        img.sprite = GetWhiteSprite();
        img.color = buttonColor;
        img.type = Image.Type.Simple;

        Button btn = buttonGO.AddComponent<Button>();
        btn.targetGraphic = img;
        ColorBlock cb = btn.colors;
        cb.normalColor = buttonColor;
        cb.highlightedColor = buttonHighlight;
        cb.pressedColor = buttonHighlight * 0.8f;
        cb.disabledColor = Color.gray;
        cb.fadeDuration = 0.1f;
        btn.colors = cb;
        btn.onClick.AddListener(onClick);

        RectTransform btnRT = buttonGO.GetComponent<RectTransform>();
        btnRT.sizeDelta = new Vector2(0, 80);

        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(btnRT, false);
        Text txt = textGO.AddComponent<Text>();
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = 36;
        txt.color = textColor;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.text = label;

        RectTransform textRT = textGO.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.sizeDelta = Vector2.zero;
    }

    void BuildSettingsPanel(RectTransform canvasRT)
    {
        settingsPanel = CreatePanel("SettingsPanel", canvasRT, Vector2.zero, Vector2.one, Vector2.zero, backgroundColor);
        settingsPanel.SetActive(false);

        // Title
        GameObject titleGO = new GameObject("SettingsTitle");
        titleGO.transform.SetParent(settingsPanel.GetComponent<RectTransform>(), false);
        Text titleText = titleGO.AddComponent<Text>();
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize = 60;
        titleText.color = textColor;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.text = "Settings";

        RectTransform titleRT = titleGO.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0, 0.75f);
        titleRT.anchorMax = new Vector2(1, 0.9f);
        titleRT.sizeDelta = Vector2.zero;

        // Settings container
        GameObject settingsContainer = new GameObject("SettingsContainer");
        settingsContainer.transform.SetParent(settingsPanel.GetComponent<RectTransform>(), false);
        RectTransform containerRT = settingsContainer.AddComponent<RectTransform>();
        containerRT.anchorMin = new Vector2(0.25f, 0.35f);
        containerRT.anchorMax = new Vector2(0.75f, 0.65f);
        containerRT.sizeDelta = Vector2.zero;

        VerticalLayoutGroup vlg = settingsContainer.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 24;
        vlg.childControlHeight = false;
        vlg.childControlWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childAlignment = TextAnchor.MiddleCenter;

        AddFullscreenToggle(settingsContainer.transform);
        AddVolumeSlider(settingsContainer.transform);

        // Back button
        GameObject backGO = new GameObject("BackButton");
        backGO.transform.SetParent(settingsPanel.GetComponent<RectTransform>(), false);
        Image backImg = backGO.AddComponent<Image>();
        backImg.sprite = GetWhiteSprite();
        backImg.color = buttonColor;
        backImg.type = Image.Type.Simple;

        Button backBtn = backGO.AddComponent<Button>();
        backBtn.targetGraphic = backImg;
        ColorBlock cb = backBtn.colors;
        cb.normalColor = buttonColor;
        cb.highlightedColor = buttonHighlight;
        cb.pressedColor = buttonHighlight * 0.8f;
        cb.fadeDuration = 0.1f;
        backBtn.colors = cb;
        backBtn.onClick.AddListener(() => settingsPanel.SetActive(false));

        RectTransform backRT = backGO.GetComponent<RectTransform>();
        backRT.anchorMin = new Vector2(0.4f, 0.15f);
        backRT.anchorMax = new Vector2(0.6f, 0.25f);
        backRT.sizeDelta = Vector2.zero;

        GameObject backTextGO = new GameObject("Text");
        backTextGO.transform.SetParent(backRT, false);
        Text backText = backTextGO.AddComponent<Text>();
        backText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        backText.fontSize = 32;
        backText.color = textColor;
        backText.alignment = TextAnchor.MiddleCenter;
        backText.text = "Back";

        RectTransform backTextRT = backTextGO.GetComponent<RectTransform>();
        backTextRT.anchorMin = Vector2.zero;
        backTextRT.anchorMax = Vector2.one;
        backTextRT.sizeDelta = Vector2.zero;
    }

    void AddFullscreenToggle(Transform parent)
    {
        GameObject row = new GameObject("FullscreenRow");
        row.transform.SetParent(parent, false);
        RectTransform rowRT = row.AddComponent<RectTransform>();
        rowRT.sizeDelta = new Vector2(0, 60);

        HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 12;
        hlg.childControlWidth = false;
        hlg.childForceExpandWidth = false;
        hlg.childAlignment = TextAnchor.MiddleCenter;

        GameObject labelGO = new GameObject("Label");
        labelGO.transform.SetParent(rowRT, false);
        Text label = labelGO.AddComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = 28;
        label.color = textColor;
        label.alignment = TextAnchor.MiddleRight;
        label.text = "Fullscreen";
        RectTransform labelRT = labelGO.GetComponent<RectTransform>();
        labelRT.sizeDelta = new Vector2(160, 40);

        GameObject toggleGO = new GameObject("FullscreenToggle");
        toggleGO.transform.SetParent(rowRT, false);
        Toggle toggle = toggleGO.AddComponent<Toggle>();
        toggle.isOn = Screen.fullScreen;

        Image bgImg = toggleGO.AddComponent<Image>();
        bgImg.sprite = GetWhiteSprite();
        bgImg.color = buttonColor;
        bgImg.type = Image.Type.Simple;
        toggle.targetGraphic = bgImg;

        RectTransform toggleRT = toggleGO.GetComponent<RectTransform>();
        toggleRT.sizeDelta = new Vector2(40, 40);

        GameObject checkGO = new GameObject("Checkmark");
        checkGO.transform.SetParent(toggleRT, false);
        Image checkImg = checkGO.AddComponent<Image>();
        checkImg.sprite = GetWhiteSprite();
        checkImg.color = textColor;
        checkImg.type = Image.Type.Simple;
        toggle.graphic = checkImg;

        RectTransform checkRT = checkGO.GetComponent<RectTransform>();
        checkRT.anchorMin = Vector2.zero;
        checkRT.anchorMax = Vector2.one;
        checkRT.sizeDelta = Vector2.zero;

        toggle.onValueChanged.AddListener(isOn => Screen.fullScreen = isOn);
    }

    void AddVolumeSlider(Transform parent)
    {
        GameObject row = new GameObject("VolumeRow");
        row.transform.SetParent(parent, false);
        RectTransform rowRT = row.AddComponent<RectTransform>();
        rowRT.sizeDelta = new Vector2(0, 60);

        HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 12;
        hlg.childControlWidth = false;
        hlg.childForceExpandWidth = false;
        hlg.childAlignment = TextAnchor.MiddleCenter;

        GameObject labelGO = new GameObject("Label");
        labelGO.transform.SetParent(rowRT, false);
        Text label = labelGO.AddComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = 28;
        label.color = textColor;
        label.alignment = TextAnchor.MiddleRight;
        label.text = "Volume";
        RectTransform labelRT = labelGO.GetComponent<RectTransform>();
        labelRT.sizeDelta = new Vector2(160, 40);

        GameObject sliderGO = new GameObject("VolumeSlider");
        sliderGO.transform.SetParent(rowRT, false);
        Slider slider = sliderGO.AddComponent<Slider>();
        slider.minValue = 0;
        slider.maxValue = 1;
        slider.value = AudioListener.volume;

        RectTransform sliderRT = sliderGO.GetComponent<RectTransform>();
        sliderRT.sizeDelta = new Vector2(200, 40);

        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(sliderRT, false);
        Image bgImg = bg.AddComponent<Image>();
        bgImg.sprite = GetWhiteSprite();
        bgImg.color = buttonColor;
        bgImg.type = Image.Type.Sliced;
        RectTransform bgRT = bg.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.sizeDelta = Vector2.zero;
        slider.targetGraphic = bgImg;

        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderRT, false);
        RectTransform fillAreaRT = fillArea.AddComponent<RectTransform>();
        fillAreaRT.anchorMin = Vector2.zero;
        fillAreaRT.anchorMax = Vector2.one;
        fillAreaRT.sizeDelta = Vector2.zero;

        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillAreaRT, false);
        Image fillImg = fill.AddComponent<Image>();
        fillImg.sprite = GetWhiteSprite();
        fillImg.color = buttonHighlight;
        fillImg.type = Image.Type.Sliced;
        RectTransform fillRT = fill.GetComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = Vector2.one;
        fillRT.sizeDelta = Vector2.zero;

        slider.fillRect = fillRT;
        slider.direction = Slider.Direction.LeftToRight;

        slider.onValueChanged.AddListener(value => AudioListener.volume = value);
    }

    void NewGame()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.LoadCharacterCreation();
        else
            SceneManager.LoadScene(newGameScene);
    }

    void ContinueGame()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.LoadFantasy();
        else
            SceneManager.LoadScene(continueScene);
    }

    void OpenSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(true);
    }

    void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}

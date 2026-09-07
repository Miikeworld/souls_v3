using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class LoadingScreen : MonoBehaviour
{
    static LoadingScreen instance;

    [Header("Visuals")]
    public float minDisplayTime = 1.5f;
    public float finalHoldTime = 0.2f;

    [Header("Colors")]
    public Color titleColor = new Color(0.9f, 0.85f, 0.7f, 1f);
    public Color bgColor = new Color(0, 0, 0, 1f);
    public Color barBgColor = new Color(0.15f, 0.15f, 0.15f, 1f);

    [Header("Tips")]
    [TextArea(2, 4)]
    public string[] loadingTips = new string[]
    {
        "Stamina runs out fast — learn when to dodge and when to block.",
        "Bonfires restore your flasks, but they also bring the dead back to life.",
        "Boss patterns repeat. Watch, wait, and strike during the opening.",
        "Two-handing a weapon increases its damage but removes your shield.",
        "Rolling at the right moment grants brief invincibility.",
        "Backstabs deal massive damage — circle around foes for the opening.",
        "Bosses are weak after a heavy attack. Punish their recovery.",
        "Keep your weapon repaired. A blunt blade still cuts, but not well.",
        "The heaviest armor protects, but it also slows your rolls.",
        "Use the terrain. Pillars and walls can block a boss's reach."
    };

    GameObject screenRoot;
    Canvas canvas;
    Slider slider;
    Text infoText;
    Sprite whiteSprite;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        BuildUI();
        if (screenRoot != null)
            screenRoot.SetActive(false);
    }

    static LoadingScreen EnsureInstance()
    {
        if (instance == null)
        {
            GameObject go = new GameObject("LoadingScreen");
            instance = go.AddComponent<LoadingScreen>();
        }
        return instance;
    }

    public static void LoadScene(string sceneName, string displayInfo = "")
    {
        EnsureInstance().ShowAndLoad(sceneName, displayInfo);
    }

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

    void BuildUI()
    {
        // Canvas
        GameObject canvasGO = new GameObject("LoadingCanvas");
        canvasGO.transform.SetParent(transform, false);
        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;
        canvasGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGO.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();

        RectTransform canvasRT = canvasGO.GetComponent<RectTransform>();
        canvasRT.anchorMin = Vector2.zero;
        canvasRT.anchorMax = Vector2.one;
        canvasRT.sizeDelta = Vector2.zero;

        // Background
        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(canvasRT, false);
        Image bgImg = bg.AddComponent<Image>();
        bgImg.sprite = GetWhiteSprite();
        bgImg.color = bgColor;
        RectTransform bgRT = bg.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.sizeDelta = Vector2.zero;

        screenRoot = canvasGO;

        // Title
        GameObject titleGO = new GameObject("Title");
        titleGO.transform.SetParent(canvasRT, false);
        Text titleText = titleGO.AddComponent<Text>();
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.text = "LOADING";
        titleText.fontSize = 48;
        titleText.color = titleColor;
        titleText.alignment = TextAnchor.MiddleCenter;
        RectTransform titleRT = titleGO.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0.2f, 0.55f);
        titleRT.anchorMax = new Vector2(0.8f, 0.65f);
        titleRT.sizeDelta = Vector2.zero;

        // Slider
        GameObject sliderGO = new GameObject("LoadingBar");
        sliderGO.transform.SetParent(canvasRT, false);
        slider = sliderGO.AddComponent<Slider>();
        slider.value = 0;
        slider.interactable = false;

        RectTransform sliderRT = sliderGO.GetComponent<RectTransform>();
        sliderRT.anchorMin = new Vector2(0.25f, 0.45f);
        sliderRT.anchorMax = new Vector2(0.75f, 0.5f);
        sliderRT.sizeDelta = Vector2.zero;

        // Slider background
        GameObject sliderBg = new GameObject("Background");
        sliderBg.transform.SetParent(sliderRT, false);
        Image sliderBgImg = sliderBg.AddComponent<Image>();
        sliderBgImg.sprite = GetWhiteSprite();
        sliderBgImg.color = barBgColor;
        sliderBgImg.type = Image.Type.Simple;
        RectTransform sliderBgRT = sliderBg.GetComponent<RectTransform>();
        sliderBgRT.anchorMin = Vector2.zero;
        sliderBgRT.anchorMax = Vector2.one;
        sliderBgRT.sizeDelta = Vector2.zero;
        slider.targetGraphic = sliderBgImg;

        // Fill area
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
        fillImg.color = titleColor;
        fillImg.type = Image.Type.Simple;
        RectTransform fillRT = fill.GetComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = Vector2.one;
        fillRT.sizeDelta = Vector2.zero;

        slider.fillRect = fillRT;

        // Info text
        GameObject infoGO = new GameObject("Info");
        infoGO.transform.SetParent(canvasRT, false);
        infoText = infoGO.AddComponent<Text>();
        infoText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        infoText.text = "";
        infoText.fontSize = 24;
        infoText.color = titleColor;
        infoText.alignment = TextAnchor.MiddleCenter;
        RectTransform infoRT = infoGO.GetComponent<RectTransform>();
        infoRT.anchorMin = new Vector2(0.2f, 0.35f);
        infoRT.anchorMax = new Vector2(0.8f, 0.4f);
        infoRT.sizeDelta = Vector2.zero;
    }

    string GetRandomTip()
    {
        if (loadingTips == null || loadingTips.Length == 0) return "";
        return loadingTips[Random.Range(0, loadingTips.Length)];
    }

    void ShowAndLoad(string sceneName, string displayInfo = "")
    {
        if (screenRoot != null)
            screenRoot.SetActive(true);

        if (infoText != null)
            infoText.text = GetRandomTip();

        StopAllCoroutines();
        StartCoroutine(LoadRoutine(sceneName));
    }

    IEnumerator LoadRoutine(string sceneName)
    {
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        if (op == null)
        {
            Debug.LogError("Failed to load scene: " + sceneName);
            yield break;
        }

        op.allowSceneActivation = false;
        float timer = 0f;

        while (timer < minDisplayTime || op.progress < 0.9f)
        {
            timer += Time.unscaledDeltaTime;
            float fakeProgress = Mathf.Clamp01(timer / minDisplayTime);
            float realProgress = Mathf.Clamp01(op.progress / 0.9f);
            if (slider != null)
                slider.value = Mathf.Min(fakeProgress, realProgress);
            yield return null;
        }

        if (slider != null)
            slider.value = 1f;

        yield return new WaitForSecondsRealtime(finalHoldTime);

        op.allowSceneActivation = true;

        // Wait until the new scene has loaded before hiding the screen.
        while (!op.isDone)
            yield return null;

        if (screenRoot != null)
            screenRoot.SetActive(false);
    }
}

using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Displays the player's current Souls in the bottom-right corner.
/// Auto-adds itself to the player; no manual UI setup needed.
/// </summary>
public class SoulsUI : MonoBehaviour
{
    private PlayerController player;
    private int lastSouls = -1;

    private static GameObject canvasObj;
    private static GameObject panel;
    private static TextMeshProUGUI soulsText;

    void Start()
    {
        player = GetComponent<PlayerController>();
        if (player == null)
        {
            player = FindAnyObjectByType<PlayerController>();
            if (player != null)
                transform.SetParent(player.transform);
        }
        EnsureUI();
        Refresh();
    }

    void Update()
    {
        if (player == null) return;
        if (player.souls != lastSouls)
            Refresh();
    }

    void Refresh()
    {
        lastSouls = player != null ? player.souls : 0;
        if (soulsText != null)
            soulsText.text = $"<color=#D9B86A>SOULS</color> {lastSouls:N0}";
    }

    void EnsureUI()
    {
        if (canvasObj != null) return;

        // Share a single bottom HUD canvas with the QuickItemBar if it already exists.
        GameObject existing = GameObject.Find("PlayerBottomHUD");
        if (existing != null)
        {
            canvasObj = existing;
            panel = canvasObj.transform.Find("SoulsPanel")?.gameObject;
            if (panel != null)
            {
                soulsText = panel.GetComponentInChildren<TextMeshProUGUI>();
                return;
            }
        }

        canvasObj = new GameObject("PlayerBottomHUD");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 60;
        canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObj.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
        DontDestroyOnLoad(canvasObj);

        panel = new GameObject("SoulsPanel");
        panel.transform.SetParent(canvasObj.transform, false);
        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.76f, 0.015f);
        rect.anchorMax = new Vector2(0.985f, 0.075f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image bg = panel.AddComponent<Image>();
        bg.color = new Color(0.02f, 0.02f, 0.02f, 0.65f);

        GameObject textObj = new GameObject("SoulsText");
        textObj.transform.SetParent(panel.transform, false);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(10, 0);
        textRect.offsetMax = new Vector2(-10, 0);

        soulsText = textObj.AddComponent<TextMeshProUGUI>();
        soulsText.alignment = TextAlignmentOptions.MidlineRight;
        soulsText.fontSize = 18;
        soulsText.color = Color.white;
    }
}

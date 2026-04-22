using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Creates an urgent feel when the player's health is low:
///   - Red vignette overlay that pulses
///   - Heartbeat sound that speeds up as health drops
///   - Slight chromatic-style red tint
///
/// SETUP:
///   1. Add this script to the Player
///   2. It auto-creates a UI overlay Canvas at runtime — no manual setup needed
///   3. Optionally assign a heartbeat AudioClip in the inspector
/// </summary>
public class LowHealthEffect : MonoBehaviour
{
    [Header("Thresholds")]
    [Tooltip("Health % below which the effect starts (0.3 = 30%)")]
    public float healthThreshold = 0.3f;

    [Header("Vignette")]
    public Color vignetteColor = new Color(0.6f, 0f, 0f, 0.35f);
    public float pulseSpeed = 2f;
    public float pulseMin = 0.15f;
    public float pulseMax = 0.4f;

    [Header("Heartbeat (optional)")]
    public AudioClip heartbeatClip;
    [Range(0f, 1f)] public float heartbeatVolume = 0.4f;

    private Entity entity;
    private Image vignetteImage;
    private AudioSource heartbeatSource;
    private float nextBeatTime = 0f;
    private Canvas canvas;

    void Start()
    {
        entity = GetComponent<Entity>();
        if (entity == null)
        {
            enabled = false;
            return;
        }

        CreateVignetteUI();
        SetupHeartbeatAudio();
    }

    void Update()
    {
        if (entity == null || entity.isDead)
        {
            if (vignetteImage != null) vignetteImage.enabled = false;
            return;
        }

        float healthPercent = entity.currentHealth / entity.maxHealth;
        bool isLow = healthPercent <= healthThreshold && healthPercent > 0f;

        // ── Vignette pulse ──
        if (vignetteImage != null)
        {
            vignetteImage.enabled = isLow;
            if (isLow)
            {
                // Intensity increases as health drops
                float urgency = 1f - (healthPercent / healthThreshold);
                float alpha = Mathf.Lerp(pulseMin, pulseMax, urgency);

                // Pulse
                float pulse = Mathf.Sin(Time.time * pulseSpeed * (1f + urgency)) * 0.5f + 0.5f;
                alpha *= Mathf.Lerp(0.6f, 1f, pulse);

                Color c = vignetteColor;
                c.a = alpha;
                vignetteImage.color = c;
            }
        }

        // ── Heartbeat ──
        if (isLow && heartbeatClip != null && heartbeatSource != null)
        {
            float urgency = 1f - (healthPercent / healthThreshold);
            float beatInterval = Mathf.Lerp(1.0f, 0.4f, urgency);

            if (Time.time >= nextBeatTime)
            {
                heartbeatSource.pitch = Mathf.Lerp(0.9f, 1.3f, urgency);
                heartbeatSource.PlayOneShot(heartbeatClip, heartbeatVolume);
                nextBeatTime = Time.time + beatInterval;
            }
        }
    }

    void CreateVignetteUI()
    {
        // Create a full-screen overlay canvas
        GameObject canvasGO = new GameObject("LowHealthVignette");
        canvasGO.transform.SetParent(transform);
        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;

        // No CanvasScaler needed for a simple overlay
        canvasGO.AddComponent<CanvasScaler>();

        // Create the vignette image
        GameObject imgGO = new GameObject("VignetteImage");
        imgGO.transform.SetParent(canvasGO.transform, false);
        vignetteImage = imgGO.AddComponent<Image>();

        // Stretch to fill screen
        RectTransform rt = vignetteImage.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        // Use a radial gradient-like effect via sprite = null (solid color for now)
        vignetteImage.color = vignetteColor;
        vignetteImage.raycastTarget = false;
        vignetteImage.enabled = false;

        // Make it a soft vignette by using a gradient material or just alpha
        // For a simple approach, we create a texture with a radial gradient
        vignetteImage.sprite = CreateVignetteSprite();
    }

    Sprite CreateVignetteSprite()
    {
        int size = 256;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float maxDist = size / 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center) / maxDist;
                // Smooth vignette: transparent in center, opaque at edges
                float alpha = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((dist - 0.3f) / 0.7f));
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    void SetupHeartbeatAudio()
    {
        if (heartbeatClip == null) return;
        heartbeatSource = gameObject.AddComponent<AudioSource>();
        heartbeatSource.playOnAwake = false;
        heartbeatSource.spatialBlend = 0f; // 2D sound
    }
}

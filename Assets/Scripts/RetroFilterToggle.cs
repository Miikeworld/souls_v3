using UnityEngine;
using UnityEngine.Experimental.Rendering;

public class RetroFilterToggle : MonoBehaviour
{
    [Header("References")]
    public Camera mainCamera;
    public RenderTexture retroRenderTexture;
    public Canvas retroCanvas;
    
    [Header("Settings")]
    public KeyCode toggleKey = KeyCode.P; // Press P to toggle
    public int retroWidth = 640;
    public int retroHeight = 360;
    
    private bool isRetroMode = true;

    void Start()
    {
        // Recreate the RenderTexture with HDR so bloom/glow effects work
        EnsureHDRRenderTexture();
        SetRetroFilter(true);
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            isRetroMode = !isRetroMode;
            SetRetroFilter(isRetroMode);
        }
    }

    void EnsureHDRRenderTexture()
    {
        if (retroRenderTexture == null) return;

        // Check if already HDR
        if (retroRenderTexture.graphicsFormat == GraphicsFormat.R16G16B16A16_SFloat)
            return;

        // Release old texture and recreate as HDR
        int w = retroRenderTexture.width > 0 ? retroRenderTexture.width : retroWidth;
        int h = retroRenderTexture.height > 0 ? retroRenderTexture.height : retroHeight;
        int aa = retroRenderTexture.antiAliasing;

        retroRenderTexture.Release();
        retroRenderTexture.graphicsFormat = GraphicsFormat.R16G16B16A16_SFloat;
        retroRenderTexture.width = w;
        retroRenderTexture.height = h;
        retroRenderTexture.antiAliasing = aa;
        retroRenderTexture.Create();

        Debug.Log($"[RetroFilter] Upgraded RenderTexture to HDR ({w}x{h}) — bloom will now work.");
    }

    void SetRetroFilter(bool enabled)
    {
        if (enabled)
        {
            mainCamera.targetTexture = retroRenderTexture;
            retroCanvas.enabled = true;
        }
        else
        {
            mainCamera.targetTexture = null;
            retroCanvas.enabled = false;
        }
    }

    // Optional: Call this from UI buttons or other scripts
    public void ToggleFilter()
    {
        isRetroMode = !isRetroMode;
        SetRetroFilter(isRetroMode);
    }
}

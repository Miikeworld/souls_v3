using UnityEngine;

public class RetroFilterToggle : MonoBehaviour
{
    [Header("References")]
    public Camera mainCamera;
    public RenderTexture retroRenderTexture;
    public Canvas retroCanvas;
    
    [Header("Settings")]
    public KeyCode toggleKey = KeyCode.P; // Press P to toggle
    
    private bool isRetroMode = true;

    void Start()
    {
        // Start with retro filter enabled
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

    void SetRetroFilter(bool enabled)
    {
        if (enabled)
        {
            // Enable retro filter
            mainCamera.targetTexture = retroRenderTexture;
            retroCanvas.enabled = true;
        }
        else
        {
            // Disable retro filter (normal rendering)
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

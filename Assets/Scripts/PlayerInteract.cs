using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class PlayerInteract : MonoBehaviour
{
    [Header("Interaction Range")]
    [Tooltip("How far the player can reach with E.")]
    public float interactionRange = 2.5f;

    [Tooltip("Which layers can be interacted with. Default is everything.")]
    public LayerMask interactableLayers = ~0;

    [Header("Input")]
    public KeyCode interactKey = KeyCode.E;

    [Header("Prompt UI (optional — will auto-create if left empty)")]
    public TextMeshProUGUI promptTMP;
    public Text promptLegacy;

    private IInteractable currentInteractable;

    // Auto-created fallback prompt
    private static GameObject promptCanvas;
    private static GameObject promptPanel;
    private static TextMeshProUGUI promptText;

    void Update()
    {
        FindBestInteractable();
        UpdatePrompt();

        if (currentInteractable != null && Input.GetKeyDown(interactKey))
        {
            currentInteractable.Interact(transform);
        }
    }

    void FindBestInteractable()
    {
        currentInteractable = null;
        float bestDistance = float.MaxValue;

        Vector3 checkPoint = transform.position + Vector3.up * 0.5f;
        Collider[] hits = Physics.OverlapSphere(checkPoint, interactionRange, interactableLayers);

        foreach (Collider hit in hits)
        {
            if (hit.transform == transform) continue;

            IInteractable interactable = hit.GetComponentInParent<IInteractable>();
            if (interactable == null) continue;

            float distance = Vector3.Distance(transform.position, hit.transform.position);
            if (distance < bestDistance && interactable.CanInteract(transform))
            {
                bestDistance = distance;
                currentInteractable = interactable;
            }
        }
    }

    void UpdatePrompt()
    {
        string prompt = currentInteractable != null
            ? $"<color=#FFD700>{interactKey}</color> {currentInteractable.GetPrompt()}"
            : string.Empty;

        if (promptTMP != null)
        {
            promptTMP.text = prompt;
        }
        else if (promptLegacy != null)
        {
            promptLegacy.text = prompt;
        }
        else
        {
            EnsureAutoPrompt();
            if (promptText != null)
                promptText.text = prompt;
            if (promptPanel != null)
                promptPanel.SetActive(currentInteractable != null);
        }
    }

    void EnsureAutoPrompt()
    {
        if (promptCanvas != null) return;

        promptCanvas = new GameObject("InteractionPromptCanvas");
        Canvas canvas = promptCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        promptCanvas.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        promptCanvas.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
        promptCanvas.AddComponent<GraphicRaycaster>();
        DontDestroyOnLoad(promptCanvas);

        promptPanel = new GameObject("PromptPanel");
        promptPanel.transform.SetParent(promptCanvas.transform, false);
        RectTransform panelRect = promptPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.3f, 0.12f);
        panelRect.anchorMax = new Vector2(0.7f, 0.2f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        Image panelImg = promptPanel.AddComponent<Image>();
        panelImg.color = new Color(0f, 0f, 0f, 0.7f);

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

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.5f, interactionRange);
    }
}

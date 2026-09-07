using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Quick item bar: scroll to cycle, R to use. Slot 0 is the healing flask.
/// </summary>
public class QuickItemBar : MonoBehaviour
{
    [Header("Selection")]
    [Tooltip("Name shown for the built-in healing flask slot.")]
    public string flaskName = "Healing Potion";

    [Header("UI (auto-created if left empty)")]
    public TextMeshProUGUI itemLabel;

    private int selectedIndex = 0;
    private PlayerController player;

    // Auto-created UI
    private static GameObject barCanvas;
    private static GameObject barPanel;
    private static TextMeshProUGUI barText;

    /// <summary>True when the built-in healing flask slot is selected.</summary>
    public bool SelectedIsFlask => selectedIndex == 0;

    void Start()
    {
        player = GetComponent<PlayerController>();
        EnsureUI();
        RefreshUI();
    }

    private float highlightTimer = 0f;

    void Update()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            int slotCount = GetSlotCount();
            if (slotCount > 0)
            {
                selectedIndex += scroll > 0 ? -1 : 1;
                if (selectedIndex < 0) selectedIndex = slotCount - 1;
                if (selectedIndex >= slotCount) selectedIndex = 0;
                highlightTimer = 1f; // Flash label on switch
            }
        }

        // Clamp selected index
        int count = GetSlotCount();
        if (selectedIndex >= count) selectedIndex = 0;

        if (highlightTimer > 0f) highlightTimer -= Time.deltaTime;

        RefreshUI();
    }

    int GetSlotCount()
    {
        // Slot 0 is the flask; rest are consumables
        return 1 + GetConsumables().Count;
    }

    List<ItemStack> GetConsumables()
    {
        List<ItemStack> result = new List<ItemStack>();
        if (Inventory.Instance != null)
        {
            foreach (ItemStack stack in Inventory.Instance.items)
            {
                if (stack.item != null && stack.item.isConsumable && stack.count > 0)
                    result.Add(stack);
            }
        }
        return result;
    }

    /// <summary>Returns the currently selected consumable stack, or null if the flask is selected.</summary>
    public ItemStack GetSelectedConsumable()
    {
        if (SelectedIsFlask) return null;
        List<ItemStack> consumables = GetConsumables();
        int idx = selectedIndex - 1;
        if (idx < 0 || idx >= consumables.Count) return null;
        return consumables[idx];
    }

    /// <summary>Uses the selected consumable and removes it from inventory.</summary>
    public bool UseSelectedConsumable(Entity target)
    {
        ItemStack stack = GetSelectedConsumable();
        if (stack == null || target == null) return false;

        ItemData item = stack.item;

        bool didSomething = false;

        if (item.healthRestore > 0f && target.currentHealth < target.maxHealth)
        {
            target.currentHealth = Mathf.Min(target.maxHealth, target.currentHealth + item.healthRestore);
            didSomething = true;
        }
        if (item.manaRestore > 0f && target.currentMana < target.maxMana)
        {
            target.currentMana = Mathf.Min(target.maxMana, target.currentMana + item.manaRestore);
            didSomething = true;
        }
        if (item.staminaRestore > 0f && target.currentStamina < target.maxStamina)
        {
            target.currentStamina = Mathf.Min(target.maxStamina, target.currentStamina + item.staminaRestore);
            didSomething = true;
        }

        if (didSomething)
        {
            Inventory.Instance.Remove(item, 1);
            target.InvokeResourceEvents();
            Debug.Log($"[QuickItemBar] Used {item.itemName}.");
            RefreshUI();
        }

        return didSomething;
    }

    void RefreshUI()
    {
        string content;
        if (SelectedIsFlask)
        {
            int potions = player != null ? player.currentPotions : 0;
            content = $"{flaskName} <color=#FFFFFF>x{potions}</color>";
        }
        else
        {
            ItemStack stack = GetSelectedConsumable();
            content = stack != null ? $"{stack.item.itemName} <color=#FFFFFF>x{stack.count}</color>" : flaskName;
        }

        // Append slot position
        int slotCount = GetSlotCount();
        if (slotCount > 1)
            content += $" <size=70%>({selectedIndex + 1}/{slotCount})</size>";

        string label = $"<color=#D9B86A>ITEM</color> {content}";

        Color labelColor = highlightTimer > 0f
            ? Color.Lerp(Color.white, new Color(1f, 0.85f, 0.3f), highlightTimer)
            : Color.white;

        if (itemLabel != null)
        {
            itemLabel.text = label;
            itemLabel.color = labelColor;
        }
        else if (barText != null)
        {
            barText.text = label;
            barText.color = labelColor;
        }
    }

    void EnsureUI()
    {
        if (itemLabel != null || barCanvas != null) return;

        // Reuse bottom HUD canvas if it exists
        GameObject existing = GameObject.Find("PlayerBottomHUD");
        if (existing != null)
        {
            barCanvas = existing;
            barPanel = barCanvas.transform.Find("QuickItemPanel")?.gameObject;
            if (barPanel != null)
            {
                barText = barPanel.GetComponentInChildren<TextMeshProUGUI>();
                return;
            }
        }

        barCanvas = new GameObject("PlayerBottomHUD");
        Canvas canvas = barCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 60;
        barCanvas.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        barCanvas.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
        DontDestroyOnLoad(barCanvas);

        // Create bottom-left quick item panel
        barPanel = new GameObject("QuickItemPanel");
        barPanel.transform.SetParent(barCanvas.transform, false);
        RectTransform rect = barPanel.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.015f, 0.015f);
        rect.anchorMax = new Vector2(0.24f, 0.075f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        Image bg = barPanel.AddComponent<Image>();
        bg.color = new Color(0.02f, 0.02f, 0.02f, 0.65f);

        GameObject textObj = new GameObject("QuickItemText");
        textObj.transform.SetParent(barPanel.transform, false);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(10, 0);
        textRect.offsetMax = new Vector2(-10, 0);
        barText = textObj.AddComponent<TextMeshProUGUI>();
        barText.alignment = TextAlignmentOptions.MidlineLeft;
        barText.fontSize = 18;
        barText.color = Color.white;
    }
}

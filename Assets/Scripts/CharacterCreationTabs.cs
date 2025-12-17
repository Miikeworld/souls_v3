using UnityEngine;
using UnityEngine.UI;

public class CharacterCreationTabs : MonoBehaviour
{
    [Header("Tab Buttons")]
    public Button basicTab;
    public Button faceTab;
    public Button bodyTab;
    public Button equipmentTab;
    public Button colorsTab;
    
    [Header("Tab Panels")]
    public GameObject basicPanel;
    public GameObject facePanel;
    public GameObject bodyPanel;
    public GameObject equipmentPanel;
    public GameObject colorsPanel;
    
    [Header("Tab Button Colors (Optional)")]
    public Color activeTabColor = new Color(0.3f, 0.6f, 1f);
    public Color inactiveTabColor = Color.white;
    
    private Button currentActiveButton;
    
    void Start()
    {
        // Setup tab button listeners
        if (basicTab) basicTab.onClick.AddListener(() => ShowTab("Basic", basicTab));
        if (faceTab) faceTab.onClick.AddListener(() => ShowTab("Face", faceTab));
        if (bodyTab) bodyTab.onClick.AddListener(() => ShowTab("Body", bodyTab));
        if (equipmentTab) equipmentTab.onClick.AddListener(() => ShowTab("Equipment", equipmentTab));
        if (colorsTab) colorsTab.onClick.AddListener(() => ShowTab("Colors", colorsTab));
        
        // Show first tab by default
        ShowTab("Basic", basicTab);
    }
    
    public void ShowTab(string tabName, Button tabButton)
    {
        // Hide all panels
        if (basicPanel) basicPanel.SetActive(false);
        if (facePanel) facePanel.SetActive(false);
        if (bodyPanel) bodyPanel.SetActive(false);
        if (equipmentPanel) equipmentPanel.SetActive(false);
        if (colorsPanel) colorsPanel.SetActive(false);
        
        // Reset all tab button colors
        ResetTabColors();
        
        // Highlight active tab
        if (tabButton != null)
        {
            ColorBlock colors = tabButton.colors;
            colors.normalColor = activeTabColor;
            tabButton.colors = colors;
            currentActiveButton = tabButton;
        }
        
        // Show selected panel
        switch (tabName)
        {
            case "Basic":
                if (basicPanel) basicPanel.SetActive(true);
                break;
            case "Face":
                if (facePanel) facePanel.SetActive(true);
                break;
            case "Body":
                if (bodyPanel) bodyPanel.SetActive(true);
                break;
            case "Equipment":
                if (equipmentPanel) equipmentPanel.SetActive(true);
                break;
            case "Colors":
                if (colorsPanel) colorsPanel.SetActive(true);
                break;
        }
    }
    
    void ResetTabColors()
    {
        Button[] allTabs = { basicTab, faceTab, bodyTab, equipmentTab, colorsTab };
        
        foreach (Button tab in allTabs)
        {
            if (tab != null)
            {
                ColorBlock colors = tab.colors;
                colors.normalColor = inactiveTabColor;
                tab.colors = colors;
            }
        }
    }
}

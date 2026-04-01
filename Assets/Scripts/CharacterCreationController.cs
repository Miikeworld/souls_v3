using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using PsychoticLab;
using System.Collections.Generic;

public class CharacterCreationController : MonoBehaviour
{
    [Header("References")]
    public CharacterRandomizer characterRandomizer;
    
    [Header("Main Buttons")]
    public Button randomizeButton;
    public Button confirmButton;
    public Button genderButton;
    public Button raceButton;
    
    [Header("Body Part Buttons")]
    public BodyPartButtons[] bodyPartButtons;
    
    [Header("Color Buttons")]
    public ColorButtons[] colorButtons;
    
    [Header("TextMeshPro Labels")]
    public TMP_Text genderText;
    public TMP_Text raceText;
    public BodyPartText[] bodyPartTexts;
    public ColorText[] colorTexts;
    
    [Header("Settings")]
    public string gameSceneName = "Fantasy";
    
    // Current selections
    private Gender currentGender = Gender.Male;
    private Race currentRace = Race.Human;
    private SkinColor currentSkinColor = SkinColor.White;
    
    // Store all indices in a dictionary
    private Dictionary<string, int> partIndices = new Dictionary<string, int>();
    private Dictionary<string, int> colorIndices = new Dictionary<string, int>();
    
    void Start()
    {
        if (characterRandomizer != null)
        {
            characterRandomizer.repeatOnPlay = false;
        }
        
        InitializeIndices();
        SetupButtons();
        UpdateCharacter();
        UpdateUI();
    }
    
    void InitializeIndices()
    {
        // Initialize all body part indices
        partIndices["Head"] = 0;
        partIndices["Hair"] = 1;
        partIndices["FacialHair"] = 0;
        partIndices["Torso"] = 1;
        partIndices["ArmUpper"] = 0;
        partIndices["ArmLower"] = 0;
        partIndices["Hand"] = 0;
        partIndices["Hips"] = 1;
        partIndices["Legs"] = 0;
        partIndices["Helmet"] = 0;
        partIndices["HipAttachment"] = 0;
        partIndices["Back"] = 0;
        partIndices["Shoulder"] = 0;
        partIndices["Elbow"] = 0;
        partIndices["Knee"] = 0;
        
        // Initialize color indices
        colorIndices["HairColor"] = 0;
        colorIndices["PrimaryColor"] = 0;
        colorIndices["SecondaryColor"] = 0;
        colorIndices["MetalColor"] = 0;
    }
    
    void SetupButtons()
    {
        // Main buttons
        if (randomizeButton) randomizeButton.onClick.AddListener(RandomizeCharacter);
        if (confirmButton) confirmButton.onClick.AddListener(ConfirmCharacter);
        if (genderButton) genderButton.onClick.AddListener(ToggleGender);
        if (raceButton) raceButton.onClick.AddListener(ToggleRace);
        
        // Body part buttons (loop through array)
        foreach (BodyPartButtons bpb in bodyPartButtons)
        {
            if (bpb.nextButton != null)
            {
                string partName = bpb.partName;
                bpb.nextButton.onClick.AddListener(() => ChangePart(partName, 1));
            }
            
            if (bpb.prevButton != null)
            {
                string partName = bpb.partName;
                bpb.prevButton.onClick.AddListener(() => ChangePart(partName, -1));
            }
        }
        
        // Color buttons (loop through array)
        foreach (ColorButtons cb in colorButtons)
        {
            if (cb.nextButton != null)
            {
                string colorName = cb.colorName;
                cb.nextButton.onClick.AddListener(() => ChangeColor(colorName, 1));
            }
            
            if (cb.prevButton != null)
            {
                string colorName = cb.colorName;
                cb.prevButton.onClick.AddListener(() => ChangeColor(colorName, -1));
            }
        }
    }
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            RandomizeCharacter();
        }
        
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            ConfirmCharacter();
        }
    }
    
    void RandomizeCharacter()
    {
        if (characterRandomizer != null)
        {
            // Use our own randomization to ensure indices are saved
            RandomizeAllParts();
            UpdateCharacter();
            UpdateUI();
            
            Debug.Log("Character randomized and ready to save!");
        }
    }
    
    void RandomizeAllParts()
    {
        // Randomize gender and race
        currentGender = (Gender)Random.Range(0, 2);
        currentRace = (Race)Random.Range(0, 2);
        currentSkinColor = (currentRace == Race.Elf) ? SkinColor.Elf : (SkinColor)Random.Range(0, 3);
        
        // Randomize all parts EXCEPT Helmet (we'll do that separately)
        foreach (var key in new List<string>(partIndices.Keys))
        {
            if (key == "Helmet") continue; // Skip helmet for now
            
            int maxCount = GetPartMaxCount(key);
            int minValue = (key == "Torso" || key == "Hips") ? 1 : 0;
            
            if (maxCount > 0)
            {
                partIndices[key] = Random.Range(minValue, maxCount);
            }
        }
        
        // Randomize helmet separately (including 0 for no helmet)
        int helmetMax = GetPartMaxCount("Helmet");
        partIndices["Helmet"] = Random.Range(0, helmetMax); // 0 = no helmet
        
        // Randomize colors
        colorIndices["HairColor"] = Random.Range(0, GetHairColorArray().Length);
        colorIndices["PrimaryColor"] = Random.Range(0, characterRandomizer.primary.Length);
        colorIndices["SecondaryColor"] = Random.Range(0, characterRandomizer.secondary.Length);
        colorIndices["MetalColor"] = Random.Range(0, characterRandomizer.metalPrimary.Length);
    }
    
    void ToggleGender()
    {
        currentGender = (currentGender == Gender.Male) ? Gender.Female : Gender.Male;
        partIndices["Head"] = 0;
        partIndices["FacialHair"] = 0;
        partIndices["Torso"] = 1;
        UpdateCharacter();
        UpdateUI();
    }
    
    void ToggleRace()
    {
        currentRace = (currentRace == Race.Human) ? Race.Elf : Race.Human;
        currentSkinColor = (currentRace == Race.Elf) ? SkinColor.Elf : SkinColor.White;
        
        // Reset hair color when race changes
        colorIndices["HairColor"] = 0;
        
        UpdateCharacter();
        UpdateColors();
        UpdateUI();
    }
    
    // Universal part changer
    void ChangePart(string partName, int direction)
    {
        int maxCount = GetPartMaxCount(partName);
        int minValue = (partName == "Torso" || partName == "Hips") ? 1 : 0;
        
        int newValue = partIndices[partName] + direction;
        
        // Check limits - stop at boundaries
        if (newValue >= minValue && newValue < maxCount)
        {
            partIndices[partName] = newValue;
            UpdateCharacter();
            UpdateUI();
        }
    }
    
    // Universal color changer
    void ChangeColor(string colorName, int direction)
    {
        if (colorName == "SkinColor")
        {
            if (currentRace == Race.Elf) return; // Can't change elf skin
            
            int skinIndex = (int)currentSkinColor;
            int newSkinValue = skinIndex + direction;
            
            if (newSkinValue >= 0 && newSkinValue <= 2) // White, Brown, Black
            {
                currentSkinColor = (SkinColor)newSkinValue;
                
                // RESET HAIR COLOR INDEX when skin changes (different array lengths!)
                Color[] newHairColors = GetHairColorArray();
                
                // If current hair index is too high for new skin, reset to 0
                if (colorIndices["HairColor"] >= newHairColors.Length)
                {
                    colorIndices["HairColor"] = 0;
                }
                
                UpdateColors();
                UpdateUI();
            }
            return;
        }
        
        // Special handling for HairColor to respect current skin's array length
        if (colorName == "HairColor")
        {
            Color[] hairColors = GetHairColorArray();
            int currentIndex = colorIndices["HairColor"];
            int newHairValue = currentIndex + direction;
            
            // Check limits for current skin's hair color array
            if (newHairValue >= 0 && newHairValue < hairColors.Length)
            {
                colorIndices["HairColor"] = newHairValue;
                UpdateColors();
                UpdateUI();
            }
            return;
        }
        
        // Regular color handling for Primary, Secondary, Metal
        int maxCount = GetColorMaxCount(colorName);
        int newValue = colorIndices[colorName] + direction;
        
        // Check limits - stop at boundaries
        if (newValue >= 0 && newValue < maxCount)
        {
            colorIndices[colorName] = newValue;
            UpdateColors();
            UpdateUI();
        }
    }
    
    int GetPartMaxCount(string partName)
    {
        CharacterObjectGroups parts = GetCurrentParts();
        
        switch (partName)
        {
            case "Head": return parts.headAllElements.Count;
            case "Hair": return characterRandomizer.allGender.all_Hair.Count;
            case "FacialHair": return parts.facialHair.Count;
            case "Torso": return parts.torso.Count;
            case "ArmUpper": return parts.arm_Upper_Right.Count;
            case "ArmLower": return parts.arm_Lower_Right.Count;
            case "Hand": return parts.hand_Right.Count;
            case "Hips": return parts.hips.Count;
            case "Legs": return parts.leg_Right.Count;
            case "Helmet": 
                // Combined count: 0=none + all helmet types
                return 1 + characterRandomizer.allGender.headCoverings_Base_Hair.Count + 
                       characterRandomizer.allGender.headCoverings_No_Hair.Count +
                       characterRandomizer.allGender.headCoverings_No_FacialHair.Count;
            case "HipAttachment": return characterRandomizer.allGender.hips_Attachment.Count;
            case "Back": return characterRandomizer.allGender.back_Attachment.Count;
            case "Shoulder": return characterRandomizer.allGender.shoulder_Attachment_Right.Count;
            case "Elbow": return characterRandomizer.allGender.elbow_Attachment_Right.Count;
            case "Knee": return characterRandomizer.allGender.knee_Attachement_Right.Count;
            default: return 0;
        }
    }
    
    int GetColorMaxCount(string colorName)
    {
        switch (colorName)
        {
            case "HairColor": return GetHairColorArray().Length;
            case "PrimaryColor": return characterRandomizer.primary.Length;
            case "SecondaryColor": return characterRandomizer.secondary.Length;
            case "MetalColor": return characterRandomizer.metalPrimary.Length;
            default: return 0;
        }
    }
    
    CharacterObjectGroups GetCurrentParts()
    {
        return (currentGender == Gender.Male) ? characterRandomizer.male : characterRandomizer.female;
    }
    
    Color[] GetHairColorArray()
    {
        switch (currentSkinColor)
        {
            case SkinColor.White: return characterRandomizer.whiteHair;
            case SkinColor.Brown: return characterRandomizer.brownHair;
            case SkinColor.Black: return characterRandomizer.blackHair;
            case SkinColor.Elf: return characterRandomizer.elfHair;
            default: return characterRandomizer.whiteHair;
        }
    }
    
    Color[] GetSkinColorArray()
    {
        switch (currentSkinColor)
        {
            case SkinColor.White: return characterRandomizer.whiteSkin;
            case SkinColor.Brown: return characterRandomizer.brownSkin;
            case SkinColor.Black: return characterRandomizer.blackSkin;
            case SkinColor.Elf: return characterRandomizer.elfSkin;
            default: return characterRandomizer.whiteSkin;
        }
    }
    
    void UpdateColors()
    {
        if (characterRandomizer.mat == null) return;
        
        // Skin
        Color[] skinColors = GetSkinColorArray();
        if (skinColors.Length > 0)
            characterRandomizer.mat.SetColor("_Color_Skin", skinColors[0]);
        
        // Hair - PROPERLY CLAMPED for current skin
        Color[] hairColors = GetHairColorArray();
        if (hairColors.Length > 0)
        {
            int clampedIndex = Mathf.Clamp(colorIndices["HairColor"], 0, hairColors.Length - 1);
            colorIndices["HairColor"] = clampedIndex;
            characterRandomizer.mat.SetColor("_Color_Hair", hairColors[clampedIndex]);
        }
        
        // Primary
        if (characterRandomizer.primary.Length > 0)
        {
            int index = Mathf.Clamp(colorIndices["PrimaryColor"], 0, characterRandomizer.primary.Length - 1);
            characterRandomizer.mat.SetColor("_Color_Primary", characterRandomizer.primary[index]);
        }
        
        // Secondary
        if (characterRandomizer.secondary.Length > 0)
        {
            int index = Mathf.Clamp(colorIndices["SecondaryColor"], 0, characterRandomizer.secondary.Length - 1);
            characterRandomizer.mat.SetColor("_Color_Secondary", characterRandomizer.secondary[index]);
        }
        
        // Metal
        if (characterRandomizer.metalPrimary.Length > 0)
        {
            int index = Mathf.Clamp(colorIndices["MetalColor"], 0, characterRandomizer.metalPrimary.Length - 1);
            characterRandomizer.mat.SetColor("_Color_Metal_Primary", characterRandomizer.metalPrimary[index]);
        }
    }
    
    void UpdateCharacter()
    {
        // Clear existing
        if (characterRandomizer.enabledObjects.Count > 0)
        {
            foreach (GameObject obj in characterRandomizer.enabledObjects)
            {
                if (obj != null) obj.SetActive(false);
            }
            characterRandomizer.enabledObjects.Clear();
        }
        
        CharacterObjectGroups parts = GetCurrentParts();
        
        // Determine helmet type and what to hide
        int helmetIndex = partIndices["Helmet"];
        bool hideHair = false;
        bool hideFacialHair = false;
        
        // Activate parts using dictionary
        ActivatePartByName("Head", parts.headAllElements);
        ActivatePartByName("Eyebrows", parts.eyebrow, 0);
        
        // Process helmet (combined system) - ONLY IF INDEX > 0
        if (helmetIndex > 0)
        {
            int hairHelmets = characterRandomizer.allGender.headCoverings_Base_Hair.Count;
            int fullHelmets = characterRandomizer.allGender.headCoverings_No_Hair.Count;
            int facialHairHelmets = characterRandomizer.allGender.headCoverings_No_FacialHair.Count;
            
            if (helmetIndex <= hairHelmets)
            {
                // Type 1: Helmets that show hair (hats, hoods, bandanas)
                int index = helmetIndex - 1;
                if (index >= 0 && index < hairHelmets)
                {
                    ActivatePart(characterRandomizer.allGender.headCoverings_Base_Hair[index]);
                }
                hideHair = false;
                hideFacialHair = false;
            }
            else if (helmetIndex <= hairHelmets + fullHelmets)
            {
                // Type 2: Full face helmets (hide hair)
                int index = helmetIndex - hairHelmets - 1;
                if (index >= 0 && index < fullHelmets)
                {
                    ActivatePart(characterRandomizer.allGender.headCoverings_No_Hair[index]);
                }
                hideHair = true;
                hideFacialHair = true;
            }
            else
            {
                // Type 3: Helmets that hide facial hair only
                int index = helmetIndex - hairHelmets - fullHelmets - 1;
                if (index >= 0 && index < facialHairHelmets)
                {
                    ActivatePart(characterRandomizer.allGender.headCoverings_No_FacialHair[index]);
                }
                hideHair = false;
                hideFacialHair = true;
            }
        }
        // If helmetIndex == 0, no helmet is activated at all
        
        // Only show facial hair if male and not hidden by helmet
        if (currentGender == Gender.Male && !hideFacialHair)
            ActivatePartByName("FacialHair", parts.facialHair);
        
        ActivatePartByName("Torso", parts.torso);
        ActivatePartByName("ArmUpper", parts.arm_Upper_Right);
        ActivatePartByName("ArmUpper", parts.arm_Upper_Left);
        ActivatePartByName("ArmLower", parts.arm_Lower_Right);
        ActivatePartByName("ArmLower", parts.arm_Lower_Left);
        ActivatePartByName("Hand", parts.hand_Right);
        ActivatePartByName("Hand", parts.hand_Left);
        ActivatePartByName("Hips", parts.hips);
        ActivatePartByName("Legs", parts.leg_Right);
        ActivatePartByName("Legs", parts.leg_Left);
        
        // All gender parts
        // Only show hair if not hidden by helmet
        if (!hideHair)
            ActivatePartByName("Hair", characterRandomizer.allGender.all_Hair);
        
        ActivatePartByName("HipAttachment", characterRandomizer.allGender.hips_Attachment);
        ActivatePartByName("Back", characterRandomizer.allGender.back_Attachment);
        ActivatePartByName("Shoulder", characterRandomizer.allGender.shoulder_Attachment_Right);
        ActivatePartByName("Shoulder", characterRandomizer.allGender.shoulder_Attachment_Left);
        ActivatePartByName("Elbow", characterRandomizer.allGender.elbow_Attachment_Right);
        ActivatePartByName("Elbow", characterRandomizer.allGender.elbow_Attachment_Left);
        ActivatePartByName("Knee", characterRandomizer.allGender.knee_Attachement_Right);
        ActivatePartByName("Knee", characterRandomizer.allGender.knee_Attachement_Left);
        
        // Elf ears
        if (currentRace == Race.Elf && characterRandomizer.allGender.elf_Ear.Count > 0)
        {
            ActivatePart(characterRandomizer.allGender.elf_Ear[0]);
        }
        
        UpdateColors();
    }
    
    void ActivatePartByName(string partName, List<GameObject> partList, int? overrideIndex = null)
    {
        if (partList.Count == 0) return;
        
        int index = overrideIndex ?? partIndices.GetValueOrDefault(partName, 0);
        index = Mathf.Clamp(index, 0, partList.Count - 1);
        
        ActivatePart(partList[index]);
    }
    
    void ActivatePart(GameObject part)
    {
        if (part != null)
        {
            part.SetActive(true);
            characterRandomizer.enabledObjects.Add(part);
        }
    }
    
    void UpdateUI()
    {
        if (genderText) genderText.text = currentGender.ToString();
        if (raceText) raceText.text = currentRace.ToString();
        
        // Update body part texts (loop)
        foreach (BodyPartText bpt in bodyPartTexts)
        {
            if (bpt.text != null && partIndices.ContainsKey(bpt.partName))
            {
                int count = GetPartMaxCount(bpt.partName);
                
                // Special display for helmet
                if (bpt.partName == "Helmet")
                {
                    int helmetIndex = partIndices["Helmet"];
                    if (helmetIndex == 0)
                    {
                        bpt.text.text = "None";
                    }
                    else
                    {
                        bpt.text.text = helmetIndex + "/" + (count - 1);
                    }
                }
                else
                {
                    bpt.text.text = (partIndices[bpt.partName] + 1) + "/" + count;
                }
            }
        }
        
        // Update color texts (loop)
        foreach (ColorText ct in colorTexts)
        {
            if (ct.text == null) continue;
            
            if (ct.colorName == "SkinColor")
            {
                ct.text.text = currentSkinColor.ToString();
            }
            else if (colorIndices.ContainsKey(ct.colorName))
            {
                int currentIndex = colorIndices[ct.colorName];
                
                // Special handling for hair color to show correct count per skin
                if (ct.colorName == "HairColor")
                {
                    Color[] hairColors = GetHairColorArray();
                    ct.text.text = (currentIndex + 1) + "/" + hairColors.Length;
                }
                else
                {
                    int count = GetColorMaxCount(ct.colorName);
                    ct.text.text = (currentIndex + 1) + "/" + count;
                }
            }
        }
    }
    
    void ConfirmCharacter()
    {
        // Save gender and race
        PlayerPrefs.SetString("CharacterGender", currentGender.ToString());
        PlayerPrefs.SetString("CharacterRace", currentRace.ToString());
        PlayerPrefs.SetString("CharacterSkinColor", currentSkinColor.ToString());
        
        // Save all part indices (loop)
        foreach (var kvp in partIndices)
        {
            PlayerPrefs.SetInt("Character" + kvp.Key, kvp.Value);
        }
        
        // Save all color indices (loop)
        foreach (var kvp in colorIndices)
        {
            PlayerPrefs.SetInt("Character" + kvp.Key, kvp.Value);
        }
        
        PlayerPrefs.Save();
        
        // DESTROY ALL CANVASES BEFORE LOADING NEW SCENE
        Canvas[] allCanvases = FindObjectsOfType<Canvas>();
        foreach (Canvas canvas in allCanvases)
        {
            Destroy(canvas.gameObject);
        }
        
        Debug.Log("Character saved");
        
        // Notify GameManager that character was created
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnCharacterCreated();
        }
        
        SceneManager.LoadScene(gameSceneName);
    }
}

// Serializable classes for Inspector arrays
[System.Serializable]
public class BodyPartButtons
{
    public string partName;
    public Button nextButton;
    public Button prevButton;
}

[System.Serializable]
public class BodyPartText
{
    public string partName;
    public TMP_Text text;
}

[System.Serializable]
public class ColorButtons
{
    public string colorName;
    public Button nextButton;
    public Button prevButton;
}

[System.Serializable]
public class ColorText
{
    public string colorName;
    public TMP_Text text;
}

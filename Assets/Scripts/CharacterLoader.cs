using UnityEngine;
using PsychoticLab;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;


public class CharacterLoader : MonoBehaviour
{
    public CharacterRandomizer characterRandomizer;
    
void Start()
{
    LoadCharacter();
    StartCoroutine(DestroyTextForever());
}

System.Collections.IEnumerator DestroyTextForever()
{
    // Check every 0.1 seconds for 5 seconds
    for (int i = 0; i < 50; i++)
    {
        yield return new WaitForSeconds(0.1f);
        
        // Destroy ALL canvases and text in entire scene
        Canvas[] canvases = FindObjectsOfType<Canvas>();
        foreach (Canvas canvas in canvases)
        {
            // Check if it has that annoying text
            TMPro.TMP_Text[] texts = canvas.GetComponentsInChildren<TMPro.TMP_Text>(true);
            foreach (TMPro.TMP_Text text in texts)
            {
                if (text.text.Contains("Mouse") || text.text.Contains("WASD") || text.text.Contains("Rotate"))
                {
                    Debug.Log("💀 FOUND AND DESTROYING: " + canvas.gameObject.name);
                    Destroy(canvas.gameObject);
                    break;
                }
            }
            
            // Also check legacy Text
            UnityEngine.UI.Text[] legacyTexts = canvas.GetComponentsInChildren<UnityEngine.UI.Text>(true);
            foreach (UnityEngine.UI.Text text in legacyTexts)
            {
                if (text.text.Contains("Mouse") || text.text.Contains("WASD") || text.text.Contains("Rotate"))
                {
                    Debug.Log("💀 FOUND AND DESTROYING: " + canvas.gameObject.name);
                    Destroy(canvas.gameObject);
                    break;
                }
            }
        }
    }
}

    
    void LoadCharacter()
    {
        if (characterRandomizer == null)
        {
            Debug.LogError("CharacterRandomizer is not assigned!");
            return;
        }

        // Load saved data
        string genderStr = PlayerPrefs.GetString("CharacterGender", "Male");
        string raceStr = PlayerPrefs.GetString("CharacterRace", "Human");
        string skinColorStr = PlayerPrefs.GetString("CharacterSkinColor", "White");

        bool isMale = (genderStr == "Male");
        bool isElf = (raceStr == "Elf");

        // Load part indices
        int headIndex = PlayerPrefs.GetInt("CharacterHead", 0);
        int hairIndex = PlayerPrefs.GetInt("CharacterHair", 1);
        int facialHairIndex = PlayerPrefs.GetInt("CharacterFacialHair", 0);
        int torsoIndex = PlayerPrefs.GetInt("CharacterTorso", 1);
        int armUpperIndex = PlayerPrefs.GetInt("CharacterArmUpper", 0);
        int armLowerIndex = PlayerPrefs.GetInt("CharacterArmLower", 0);
        int handIndex = PlayerPrefs.GetInt("CharacterHand", 0);
        int hipsIndex = PlayerPrefs.GetInt("CharacterHips", 1);
        int legsIndex = PlayerPrefs.GetInt("CharacterLegs", 0);
        int helmetIndex = PlayerPrefs.GetInt("CharacterHelmet", 0);
        int hipAttachmentIndex = PlayerPrefs.GetInt("CharacterHipAttachment", 0);
        int backIndex = PlayerPrefs.GetInt("CharacterBack", 0);
        int shoulderIndex = PlayerPrefs.GetInt("CharacterShoulder", 0);
        int elbowIndex = PlayerPrefs.GetInt("CharacterElbow", 0);
        int kneeIndex = PlayerPrefs.GetInt("CharacterKnee", 0);

        // Load color indices
        int hairColorIndex = PlayerPrefs.GetInt("CharacterHairColor", 0);
        int primaryColorIndex = PlayerPrefs.GetInt("CharacterPrimaryColor", 0);
        int secondaryColorIndex = PlayerPrefs.GetInt("CharacterSecondaryColor", 0);
        int metalColorIndex = PlayerPrefs.GetInt("CharacterMetalColor", 0);

        // Clear existing objects
        if (characterRandomizer.enabledObjects.Count > 0)
        {
            foreach (GameObject obj in characterRandomizer.enabledObjects)
            {
                if (obj != null) obj.SetActive(false);
            }
            characterRandomizer.enabledObjects.Clear();
        }

        // Get correct parts based on gender
        CharacterObjectGroups parts = isMale ? characterRandomizer.male : characterRandomizer.female;

        // Determine helmet type and visibility
        bool hideHair = false;
        bool hideFacialHair = false;

        // Activate body parts
        ActivatePart(parts.headAllElements, headIndex);
        ActivatePart(parts.eyebrow, 0);

        // Handle helmet system
        if (helmetIndex > 0)
        {
            int hairHelmets = characterRandomizer.allGender.headCoverings_Base_Hair.Count;
            int fullHelmets = characterRandomizer.allGender.headCoverings_No_Hair.Count;

            if (helmetIndex <= hairHelmets)
            {
                ActivatePart(characterRandomizer.allGender.headCoverings_Base_Hair, helmetIndex - 1);
                hideHair = false;
                hideFacialHair = false;
            }
            else if (helmetIndex <= hairHelmets + fullHelmets)
            {
                ActivatePart(characterRandomizer.allGender.headCoverings_No_Hair, helmetIndex - hairHelmets - 1);
                hideHair = true;
                hideFacialHair = true;
            }
            else
            {
                int index = helmetIndex - hairHelmets - fullHelmets - 1;
                ActivatePart(characterRandomizer.allGender.headCoverings_No_FacialHair, index);
                hideHair = false;
                hideFacialHair = true;
            }
        }

        // Facial hair (male only, if not hidden)
        if (isMale && !hideFacialHair)
        {
            ActivatePart(parts.facialHair, facialHairIndex);
        }

        // Body parts
        ActivatePart(parts.torso, torsoIndex);
        ActivatePart(parts.arm_Upper_Right, armUpperIndex);
        ActivatePart(parts.arm_Upper_Left, armUpperIndex);
        ActivatePart(parts.arm_Lower_Right, armLowerIndex);
        ActivatePart(parts.arm_Lower_Left, armLowerIndex);
        ActivatePart(parts.hand_Right, handIndex);
        ActivatePart(parts.hand_Left, handIndex);
        ActivatePart(parts.hips, hipsIndex);
        ActivatePart(parts.leg_Right, legsIndex);
        ActivatePart(parts.leg_Left, legsIndex);

        // All gender parts
        if (!hideHair)
        {
            ActivatePart(characterRandomizer.allGender.all_Hair, hairIndex);
        }

        ActivatePart(characterRandomizer.allGender.hips_Attachment, hipAttachmentIndex);
        ActivatePart(characterRandomizer.allGender.back_Attachment, backIndex);
        ActivatePart(characterRandomizer.allGender.shoulder_Attachment_Right, shoulderIndex);
        ActivatePart(characterRandomizer.allGender.shoulder_Attachment_Left, shoulderIndex);
        ActivatePart(characterRandomizer.allGender.elbow_Attachment_Right, elbowIndex);
        ActivatePart(characterRandomizer.allGender.elbow_Attachment_Left, elbowIndex);
        ActivatePart(characterRandomizer.allGender.knee_Attachement_Right, kneeIndex);
        ActivatePart(characterRandomizer.allGender.knee_Attachement_Left, kneeIndex);

        // Elf ears
        if (isElf && characterRandomizer.allGender.elf_Ear.Count > 0)
        {
            ActivatePart(characterRandomizer.allGender.elf_Ear, 0);
        }

        // Apply colors
        ApplyColors(skinColorStr, hairColorIndex, primaryColorIndex, secondaryColorIndex, metalColorIndex);

        // ========== REMOVE SYNTY DEMO TEXT ==========
        TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);
        foreach (TMP_Text text in texts)
        {
            if (text.text.Contains("camera") || text.text.Contains("mouse") || text.text.Contains("WASD"))
            {
                Debug.Log("Removing demo text: " + text.gameObject.name);
                Destroy(text.gameObject);
            }
        }
        
        Text[] legacyTexts = GetComponentsInChildren<Text>(true);
        foreach (Text text in legacyTexts)
        {
            if (text.text.Contains("camera") || text.text.Contains("mouse") || text.text.Contains("WASD"))
            {
                Debug.Log("Removing demo text: " + text.gameObject.name);
                Destroy(text.gameObject);
            }
        }
        
        Canvas[] canvases = GetComponentsInChildren<Canvas>(true);
        foreach (Canvas canvas in canvases)
        {
            Debug.Log("Removing canvas: " + canvas.gameObject.name);
            Destroy(canvas.gameObject);
        }
        
        Debug.Log("✅ Character loaded and cleaned!");
    }
    
    void ActivatePart(List<GameObject> partList, int index)
    {
        if (partList == null || partList.Count == 0) return;
        
        index = Mathf.Clamp(index, 0, partList.Count - 1);
        GameObject part = partList[index];
        
        if (part != null)
        {
            part.SetActive(true);
            characterRandomizer.enabledObjects.Add(part);
        }
    }
    
    void ApplyColors(string skinColorStr, int hairColorIndex, int primaryColorIndex, int secondaryColorIndex, int metalColorIndex)
    {
        if (characterRandomizer.mat == null) return;
        
        // Get color arrays based on skin color
        Color[] skinColors = GetSkinColorArray(skinColorStr);
        Color[] hairColors = GetHairColorArray(skinColorStr);
        
        // Apply skin color
        if (skinColors.Length > 0)
        {
            characterRandomizer.mat.SetColor("_Color_Skin", skinColors[0]);
        }
        
        // Apply hair color
        if (hairColors.Length > 0)
        {
            int clampedHairIndex = Mathf.Clamp(hairColorIndex, 0, hairColors.Length - 1);
            characterRandomizer.mat.SetColor("_Color_Hair", hairColors[clampedHairIndex]);
        }
        
        // Apply primary color
        if (characterRandomizer.primary.Length > 0)
        {
            int clampedIndex = Mathf.Clamp(primaryColorIndex, 0, characterRandomizer.primary.Length - 1);
            characterRandomizer.mat.SetColor("_Color_Primary", characterRandomizer.primary[clampedIndex]);
        }
        
        // Apply secondary color
        if (characterRandomizer.secondary.Length > 0)
        {
            int clampedIndex = Mathf.Clamp(secondaryColorIndex, 0, characterRandomizer.secondary.Length - 1);
            characterRandomizer.mat.SetColor("_Color_Secondary", characterRandomizer.secondary[clampedIndex]);
        }
        
        // Apply metal color
        if (characterRandomizer.metalPrimary.Length > 0)
        {
            int clampedIndex = Mathf.Clamp(metalColorIndex, 0, characterRandomizer.metalPrimary.Length - 1);
            characterRandomizer.mat.SetColor("_Color_Metal_Primary", characterRandomizer.metalPrimary[clampedIndex]);
        }
    }
    
    Color[] GetSkinColorArray(string skinColorStr)
    {
        if (skinColorStr == "Brown") return characterRandomizer.brownSkin;
        if (skinColorStr == "Black") return characterRandomizer.blackSkin;
        if (skinColorStr == "Elf") return characterRandomizer.elfSkin;
        return characterRandomizer.whiteSkin;
    }
    
    Color[] GetHairColorArray(string skinColorStr)
    {
        if (skinColorStr == "Brown") return characterRandomizer.brownHair;
        if (skinColorStr == "Black") return characterRandomizer.blackHair;
        if (skinColorStr == "Elf") return characterRandomizer.elfHair;
        return characterRandomizer.whiteHair;
    }
}

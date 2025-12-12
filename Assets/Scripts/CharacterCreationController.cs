using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro; // Add this for TextMeshPro
using PsychoticLab;
using System.Collections.Generic;

public class CharacterCreationController : MonoBehaviour
{
    [Header("References")]
    public CharacterRandomizer characterRandomizer;
    
    [Header("UI Buttons")]
    public Button randomizeButton;
    public Button confirmButton;
    public Button genderButton;
    public Button raceButton;
    
    // Head
    public Button nextHeadButton;
    public Button prevHeadButton;
    
    // Hair
    public Button nextHairButton;
    public Button prevHairButton;
    
    // Facial Hair (male only)
    public Button nextFacialHairButton;
    public Button prevFacialHairButton;
    
    // Torso
    public Button nextTorsoButton;
    public Button prevTorsoButton;
    
    // Arms Upper
    public Button nextArmUpperButton;
    public Button prevArmUpperButton;
    
    // Arms Lower
    public Button nextArmLowerButton;
    public Button prevArmLowerButton;
    
    // Hands
    public Button nextHandButton;
    public Button prevHandButton;
    
    // Hips
    public Button nextHipsButton;
    public Button prevHipsButton;
    
    // Legs
    public Button nextLegsButton;
    public Button prevLegsButton;
    
    // Chest Attachment (armor)
    public Button nextChestArmorButton;
    public Button prevChestArmorButton;
    
    // Back Attachment
    public Button nextBackButton;
    public Button prevBackButton;
    
    // Shoulder Attachments
    public Button nextShoulderButton;
    public Button prevShoulderButton;
    
    // Elbow Attachments
    public Button nextElbowButton;
    public Button prevElbowButton;
    
    // Hip Attachment
    public Button nextHipArmorButton;
    public Button prevHipArmorButton;
    
    // Knee Attachments
    public Button nextKneeButton;
    public Button prevKneeButton;
    
    [Header("TextMeshPro Labels")]
    public TMP_Text genderText;
    public TMP_Text raceText;
    public TMP_Text headText;
    public TMP_Text hairText;
    public TMP_Text facialHairText;
    public TMP_Text torsoText;
    public TMP_Text armUpperText;
    public TMP_Text armLowerText;
    public TMP_Text handText;
    public TMP_Text hipsText;
    public TMP_Text legsText;
    public TMP_Text chestArmorText;
    public TMP_Text backText;
    public TMP_Text shoulderText;
    public TMP_Text elbowText;
    public TMP_Text hipArmorText;
    public TMP_Text kneeText;
    
    [Header("Settings")]
    public string gameSceneName = "GameScene";
    
    // Current selections
    private Gender currentGender = Gender.Male;
    private Race currentRace = Race.Human;
    private int currentHeadIndex = 0;
    private int currentHairIndex = 1;
    private int currentFacialHairIndex = 0;
    private int currentTorsoIndex = 1;
    private int currentArmUpperIndex = 0;
    private int currentArmLowerIndex = 0;
    private int currentHandIndex = 0;
    private int currentHipsIndex = 1;
    private int currentLegsIndex = 0;
    private int currentChestArmorIndex = 0;
    private int currentBackIndex = 0;
    private int currentShoulderIndex = 0;
    private int currentElbowIndex = 0;
    private int currentHipArmorIndex = 0;
    private int currentKneeIndex = 0;
    
    void Start()
    {
        // Disable auto-randomize
        if (characterRandomizer != null)
        {
            characterRandomizer.repeatOnPlay = false;
        }
        
        // Setup button listeners
        if (randomizeButton) randomizeButton.onClick.AddListener(RandomizeCharacter);
        if (confirmButton) confirmButton.onClick.AddListener(ConfirmCharacter);
        if (genderButton) genderButton.onClick.AddListener(ToggleGender);
        if (raceButton) raceButton.onClick.AddListener(ToggleRace);
        
        if (nextHeadButton) nextHeadButton.onClick.AddListener(NextHead);
        if (prevHeadButton) prevHeadButton.onClick.AddListener(PreviousHead);
        
        if (nextHairButton) nextHairButton.onClick.AddListener(NextHair);
        if (prevHairButton) prevHairButton.onClick.AddListener(PreviousHair);
        
        if (nextFacialHairButton) nextFacialHairButton.onClick.AddListener(NextFacialHair);
        if (prevFacialHairButton) prevFacialHairButton.onClick.AddListener(PreviousFacialHair);
        
        if (nextTorsoButton) nextTorsoButton.onClick.AddListener(NextTorso);
        if (prevTorsoButton) prevTorsoButton.onClick.AddListener(PreviousTorso);
        
        if (nextArmUpperButton) nextArmUpperButton.onClick.AddListener(NextArmUpper);
        if (prevArmUpperButton) prevArmUpperButton.onClick.AddListener(PreviousArmUpper);
        
        if (nextArmLowerButton) nextArmLowerButton.onClick.AddListener(NextArmLower);
        if (prevArmLowerButton) prevArmLowerButton.onClick.AddListener(PreviousArmLower);
        
        if (nextHandButton) nextHandButton.onClick.AddListener(NextHand);
        if (prevHandButton) prevHandButton.onClick.AddListener(PreviousHand);
        
        if (nextHipsButton) nextHipsButton.onClick.AddListener(NextHips);
        if (prevHipsButton) prevHipsButton.onClick.AddListener(PreviousHips);
        
        if (nextLegsButton) nextLegsButton.onClick.AddListener(NextLegs);
        if (prevLegsButton) prevLegsButton.onClick.AddListener(PreviousLegs);
        
        if (nextChestArmorButton) nextChestArmorButton.onClick.AddListener(NextChestArmor);
        if (prevChestArmorButton) prevChestArmorButton.onClick.AddListener(PreviousChestArmor);
        
        if (nextBackButton) nextBackButton.onClick.AddListener(NextBack);
        if (prevBackButton) prevBackButton.onClick.AddListener(PreviousBack);
        
        if (nextShoulderButton) nextShoulderButton.onClick.AddListener(NextShoulder);
        if (prevShoulderButton) prevShoulderButton.onClick.AddListener(PreviousShoulder);
        
        if (nextElbowButton) nextElbowButton.onClick.AddListener(NextElbow);
        if (prevElbowButton) prevElbowButton.onClick.AddListener(PreviousElbow);
        
        if (nextHipArmorButton) nextHipArmorButton.onClick.AddListener(NextHipArmor);
        if (prevHipArmorButton) prevHipArmorButton.onClick.AddListener(PreviousHipArmor);
        
        if (nextKneeButton) nextKneeButton.onClick.AddListener(NextKnee);
        if (prevKneeButton) prevKneeButton.onClick.AddListener(PreviousKnee);
        
        UpdateCharacter();
        UpdateUI();
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
            characterRandomizer.SendMessage("Randomize");
        }
    }
    
    void ToggleGender()
    {
        currentGender = (currentGender == Gender.Male) ? Gender.Female : Gender.Male;
        currentHeadIndex = 0;
        currentFacialHairIndex = 0;
        currentTorsoIndex = 1;
        UpdateCharacter();
        UpdateUI();
    }
    
    void ToggleRace()
    {
        currentRace = (currentRace == Race.Human) ? Race.Elf : Race.Human;
        UpdateCharacter();
        UpdateUI();
    }
    
    void NextHead() { currentHeadIndex++; UpdateCharacter(); UpdateUI(); }
    void PreviousHead() { currentHeadIndex--; if (currentHeadIndex < 0) currentHeadIndex = 0; UpdateCharacter(); UpdateUI(); }
    
    void NextHair() { currentHairIndex++; UpdateCharacter(); UpdateUI(); }
    void PreviousHair() { currentHairIndex--; if (currentHairIndex < 0) currentHairIndex = 0; UpdateCharacter(); UpdateUI(); }
    
    void NextFacialHair() { currentFacialHairIndex++; UpdateCharacter(); UpdateUI(); }
    void PreviousFacialHair() { currentFacialHairIndex--; if (currentFacialHairIndex < 0) currentFacialHairIndex = 0; UpdateCharacter(); UpdateUI(); }
    
    void NextTorso() { currentTorsoIndex++; UpdateCharacter(); UpdateUI(); }
    void PreviousTorso() { currentTorsoIndex--; if (currentTorsoIndex < 1) currentTorsoIndex = 1; UpdateCharacter(); UpdateUI(); }
    
    void NextArmUpper() { currentArmUpperIndex++; UpdateCharacter(); UpdateUI(); }
    void PreviousArmUpper() { currentArmUpperIndex--; if (currentArmUpperIndex < 0) currentArmUpperIndex = 0; UpdateCharacter(); UpdateUI(); }
    
    void NextArmLower() { currentArmLowerIndex++; UpdateCharacter(); UpdateUI(); }
    void PreviousArmLower() { currentArmLowerIndex--; if (currentArmLowerIndex < 0) currentArmLowerIndex = 0; UpdateCharacter(); UpdateUI(); }
    
    void NextHand() { currentHandIndex++; UpdateCharacter(); UpdateUI(); }
    void PreviousHand() { currentHandIndex--; if (currentHandIndex < 0) currentHandIndex = 0; UpdateCharacter(); UpdateUI(); }
    
    void NextHips() { currentHipsIndex++; UpdateCharacter(); UpdateUI(); }
    void PreviousHips() { currentHipsIndex--; if (currentHipsIndex < 1) currentHipsIndex = 1; UpdateCharacter(); UpdateUI(); }
    
    void NextLegs() { currentLegsIndex++; UpdateCharacter(); UpdateUI(); }
    void PreviousLegs() { currentLegsIndex--; if (currentLegsIndex < 0) currentLegsIndex = 0; UpdateCharacter(); UpdateUI(); }
    
    void NextChestArmor() { currentChestArmorIndex++; UpdateCharacter(); UpdateUI(); }
    void PreviousChestArmor() { currentChestArmorIndex--; if (currentChestArmorIndex < 0) currentChestArmorIndex = 0; UpdateCharacter(); UpdateUI(); }
    
    void NextBack() { currentBackIndex++; UpdateCharacter(); UpdateUI(); }
    void PreviousBack() { currentBackIndex--; if (currentBackIndex < 0) currentBackIndex = 0; UpdateCharacter(); UpdateUI(); }
    
    void NextShoulder() { currentShoulderIndex++; UpdateCharacter(); UpdateUI(); }
    void PreviousShoulder() { currentShoulderIndex--; if (currentShoulderIndex < 0) currentShoulderIndex = 0; UpdateCharacter(); UpdateUI(); }
    
    void NextElbow() { currentElbowIndex++; UpdateCharacter(); UpdateUI(); }
    void PreviousElbow() { currentElbowIndex--; if (currentElbowIndex < 0) currentElbowIndex = 0; UpdateCharacter(); UpdateUI(); }
    
    void NextHipArmor() { currentHipArmorIndex++; UpdateCharacter(); UpdateUI(); }
    void PreviousHipArmor() { currentHipArmorIndex--; if (currentHipArmorIndex < 0) currentHipArmorIndex = 0; UpdateCharacter(); UpdateUI(); }
    
    void NextKnee() { currentKneeIndex++; UpdateCharacter(); UpdateUI(); }
    void PreviousKnee() { currentKneeIndex--; if (currentKneeIndex < 0) currentKneeIndex = 0; UpdateCharacter(); UpdateUI(); }
    
    void UpdateCharacter()
    {
        if (characterRandomizer.enabledObjects.Count > 0)
        {
            foreach (GameObject obj in characterRandomizer.enabledObjects)
            {
                if (obj != null) obj.SetActive(false);
            }
            characterRandomizer.enabledObjects.Clear();
        }
        
        CharacterObjectGroups parts = (currentGender == Gender.Male) ? 
            characterRandomizer.male : characterRandomizer.female;
        
        // HEAD
        if (parts.headAllElements.Count > 0)
        {
            int index = Mathf.Clamp(currentHeadIndex, 0, parts.headAllElements.Count - 1);
            ActivatePart(parts.headAllElements[index]);
        }
        
        // EYEBROWS
        if (parts.eyebrow.Count > 0)
        {
            ActivatePart(parts.eyebrow[0]);
        }
        
        // FACIAL HAIR
        if (currentGender == Gender.Male && parts.facialHair.Count > 0)
        {
            int index = Mathf.Clamp(currentFacialHairIndex, 0, parts.facialHair.Count - 1);
            ActivatePart(parts.facialHair[index]);
        }
        
        // TORSO
        if (parts.torso.Count > 0)
        {
            int index = Mathf.Clamp(currentTorsoIndex, 0, parts.torso.Count - 1);
            ActivatePart(parts.torso[index]);
        }
        
        // ARMS UPPER
        if (parts.arm_Upper_Right.Count > 0)
        {
            int index = Mathf.Clamp(currentArmUpperIndex, 0, parts.arm_Upper_Right.Count - 1);
            ActivatePart(parts.arm_Upper_Right[index]);
            if (parts.arm_Upper_Left.Count > index)
                ActivatePart(parts.arm_Upper_Left[index]);
        }
        
        // ARMS LOWER
        if (parts.arm_Lower_Right.Count > 0)
        {
            int index = Mathf.Clamp(currentArmLowerIndex, 0, parts.arm_Lower_Right.Count - 1);
            ActivatePart(parts.arm_Lower_Right[index]);
            if (parts.arm_Lower_Left.Count > index)
                ActivatePart(parts.arm_Lower_Left[index]);
        }
        
        // HANDS
        if (parts.hand_Right.Count > 0)
        {
            int index = Mathf.Clamp(currentHandIndex, 0, parts.hand_Right.Count - 1);
            ActivatePart(parts.hand_Right[index]);
            if (parts.hand_Left.Count > index)
                ActivatePart(parts.hand_Left[index]);
        }
        
        // HIPS
        if (parts.hips.Count > 0)
        {
            int index = Mathf.Clamp(currentHipsIndex, 0, parts.hips.Count - 1);
            ActivatePart(parts.hips[index]);
        }
        
        // LEGS
        if (parts.leg_Right.Count > 0)
        {
            int index = Mathf.Clamp(currentLegsIndex, 0, parts.leg_Right.Count - 1);
            ActivatePart(parts.leg_Right[index]);
            if (parts.leg_Left.Count > index)
                ActivatePart(parts.leg_Left[index]);
        }
        
        // HAIR
        if (characterRandomizer.allGender.all_Hair.Count > 0)
        {
            int index = Mathf.Clamp(currentHairIndex, 0, characterRandomizer.allGender.all_Hair.Count - 1);
            ActivatePart(characterRandomizer.allGender.all_Hair[index]);
        }
        
        // CHEST ARMOR
        if (characterRandomizer.allGender.chest_Attachment.Count > 0)
        {
            int index = Mathf.Clamp(currentChestArmorIndex, 0, characterRandomizer.allGender.chest_Attachment.Count - 1);
            ActivatePart(characterRandomizer.allGender.chest_Attachment[index]);
        }
        
        // BACK
        if (characterRandomizer.allGender.back_Attachment.Count > 0)
        {
            int index = Mathf.Clamp(currentBackIndex, 0, characterRandomizer.allGender.back_Attachment.Count - 1);
            ActivatePart(characterRandomizer.allGender.back_Attachment[index]);
        }
        
        // SHOULDERS
        if (characterRandomizer.allGender.shoulder_Attachment_Right.Count > 0)
        {
            int index = Mathf.Clamp(currentShoulderIndex, 0, characterRandomizer.allGender.shoulder_Attachment_Right.Count - 1);
            ActivatePart(characterRandomizer.allGender.shoulder_Attachment_Right[index]);
            if (characterRandomizer.allGender.shoulder_Attachment_Left.Count > index)
                ActivatePart(characterRandomizer.allGender.shoulder_Attachment_Left[index]);
        }
        
        // ELBOWS
        if (characterRandomizer.allGender.elbow_Attachment_Right.Count > 0)
        {
            int index = Mathf.Clamp(currentElbowIndex, 0, characterRandomizer.allGender.elbow_Attachment_Right.Count - 1);
            ActivatePart(characterRandomizer.allGender.elbow_Attachment_Right[index]);
            if (characterRandomizer.allGender.elbow_Attachment_Left.Count > index)
                ActivatePart(characterRandomizer.allGender.elbow_Attachment_Left[index]);
        }
        
        // HIP ARMOR
        if (characterRandomizer.allGender.hips_Attachment.Count > 0)
        {
            int index = Mathf.Clamp(currentHipArmorIndex, 0, characterRandomizer.allGender.hips_Attachment.Count - 1);
            ActivatePart(characterRandomizer.allGender.hips_Attachment[index]);
        }
        
        // KNEES
        if (characterRandomizer.allGender.knee_Attachement_Right.Count > 0)
        {
            int index = Mathf.Clamp(currentKneeIndex, 0, characterRandomizer.allGender.knee_Attachement_Right.Count - 1);
            ActivatePart(characterRandomizer.allGender.knee_Attachement_Right[index]);
            if (characterRandomizer.allGender.knee_Attachement_Left.Count > index)
                ActivatePart(characterRandomizer.allGender.knee_Attachement_Left[index]);
        }
        
        // ELF EARS
        if (currentRace == Race.Elf && characterRandomizer.allGender.elf_Ear.Count > 0)
        {
            ActivatePart(characterRandomizer.allGender.elf_Ear[0]);
        }
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
        CharacterObjectGroups parts = (currentGender == Gender.Male) ? 
            characterRandomizer.male : characterRandomizer.female;
        
        if (genderText) genderText.text = currentGender.ToString();
        if (raceText) raceText.text = currentRace.ToString();
        if (headText) headText.text = (currentHeadIndex + 1) + "/" + parts.headAllElements.Count;
        if (hairText) hairText.text = (currentHairIndex + 1) + "/" + characterRandomizer.allGender.all_Hair.Count;
        if (facialHairText) facialHairText.text = (currentFacialHairIndex + 1) + "/" + parts.facialHair.Count;
        if (torsoText) torsoText.text = (currentTorsoIndex + 1) + "/" + parts.torso.Count;
        if (armUpperText) armUpperText.text = (currentArmUpperIndex + 1) + "/" + parts.arm_Upper_Right.Count;
        if (armLowerText) armLowerText.text = (currentArmLowerIndex + 1) + "/" + parts.arm_Lower_Right.Count;
        if (handText) handText.text = (currentHandIndex + 1) + "/" + parts.hand_Right.Count;
        if (hipsText) hipsText.text = (currentHipsIndex + 1) + "/" + parts.hips.Count;
        if (legsText) legsText.text = (currentLegsIndex + 1) + "/" + parts.leg_Right.Count;
        if (chestArmorText) chestArmorText.text = (currentChestArmorIndex + 1) + "/" + characterRandomizer.allGender.chest_Attachment.Count;
        if (backText) backText.text = (currentBackIndex + 1) + "/" + characterRandomizer.allGender.back_Attachment.Count;
        if (shoulderText) shoulderText.text = (currentShoulderIndex + 1) + "/" + characterRandomizer.allGender.shoulder_Attachment_Right.Count;
        if (elbowText) elbowText.text = (currentElbowIndex + 1) + "/" + characterRandomizer.allGender.elbow_Attachment_Right.Count;
        if (hipArmorText) hipArmorText.text = (currentHipArmorIndex + 1) + "/" + characterRandomizer.allGender.hips_Attachment.Count;
        if (kneeText) kneeText.text = (currentKneeIndex + 1) + "/" + characterRandomizer.allGender.knee_Attachement_Right.Count;
    }
    
    void ConfirmCharacter()
    {
        PlayerPrefs.SetString("CharacterGender", currentGender.ToString());
        PlayerPrefs.SetString("CharacterRace", currentRace.ToString());
        PlayerPrefs.SetInt("CharacterHead", currentHeadIndex);
        PlayerPrefs.SetInt("CharacterHair", currentHairIndex);
        PlayerPrefs.SetInt("CharacterFacialHair", currentFacialHairIndex);
        PlayerPrefs.SetInt("CharacterTorso", currentTorsoIndex);
        PlayerPrefs.SetInt("CharacterArmUpper", currentArmUpperIndex);
        PlayerPrefs.SetInt("CharacterArmLower", currentArmLowerIndex);
        PlayerPrefs.SetInt("CharacterHand", currentHandIndex);
        PlayerPrefs.SetInt("CharacterHips", currentHipsIndex);
        PlayerPrefs.SetInt("CharacterLegs", currentLegsIndex);
        PlayerPrefs.SetInt("CharacterChestArmor", currentChestArmorIndex);
        PlayerPrefs.SetInt("CharacterBack", currentBackIndex);
        PlayerPrefs.SetInt("CharacterShoulder", currentShoulderIndex);
        PlayerPrefs.SetInt("CharacterElbow", currentElbowIndex);
        PlayerPrefs.SetInt("CharacterHipArmor", currentHipArmorIndex);
        PlayerPrefs.SetInt("CharacterKnee", currentKneeIndex);
        PlayerPrefs.Save();
        
        Debug.Log("Character saved! Loading game...");
        SceneManager.LoadScene(gameSceneName);
    }
}

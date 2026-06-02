using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Hides the Frank_Mage body mesh and overlays your custom model on top.
/// The Frank_Mage skeleton still drives all animations + FX (Generic mode),
/// but visually the boss looks like your model.
///
/// SETUP:
///   1. Place Frank_RPG_Mage_Unity as the boss (Generic rig, with BossController)
///   2. Add this script to the same GameObject
///   3. Drag your DarkLord prefab into the "customModelPrefab" slot
///   4. The script hides the Frank_Mage body and parents DarkLord parts
///      to the matching Frank_Mage bones.
/// </summary>
public class BossSkinSwap : MonoBehaviour
{
    [Header("Custom Model")]
    [Tooltip("Drag your DarkLord prefab here.")]
    public GameObject customModelPrefab;

    [Header("Options")]
    [Tooltip("Hide the Frank_Mage body mesh but keep FX meshes visible.")]
    public bool hideFrankBody = true;

    [Tooltip("Scale applied to the custom model (adjust if size differs).")]
    public float modelScale = 1f;

    [Tooltip("Y offset to align feet (negative = lower).")]
    public float heightOffset = 0f;

    // Mapping from DarkLord (PolygonDarkFantasy) bone names → Frank_Mage bone names
    static readonly Dictionary<string, string> BONE_MAP = new Dictionary<string, string>()
    {
        // Core
        { "Hips",          "pelvis" },
        { "Spine_01",      "spine_01" },
        { "Spine_02",      "spine_02" },
        { "Spine_03",      "spine_02" },   // Frank has no upper chest, map to chest
        { "Neck",          "neck_01" },
        { "Head",          "head" },
        // Left arm
        { "Clavicle_L",   "clavicle_l" },
        { "Shoulder_L",   "upperarm_l" },
        { "Elbow_L",      "lowerarm_l" },
        { "Hand_L",       "hand_l" },
        // Right arm
        { "Clavicle_R",   "clavicle_r" },
        { "Shoulder_R",   "upperarm_r" },
        { "Elbow_R",      "lowerarm_r" },
        { "Hand_R",       "hand_r" },
        // Left leg
        { "UpperLeg_L",   "thigh_l" },
        { "LowerLeg_L",   "calf_l" },
        { "Ankle_L",      "foot_l" },
        { "Ball_L",       "ball_l" },
        // Right leg
        { "UpperLeg_R",   "thigh_r" },
        { "LowerLeg_R",   "calf_r" },
        { "Ankle_R",      "foot_r" },
        { "Ball_R",       "ball_r" },
        // Fingers (left)
        { "Thumb_01",     "thumb_01_l" },
        { "Thumb_02",     "thumb_02_l" },
        { "IndexFinger_01", "index_01_l" },
        { "IndexFinger_02", "index_02_l" },
        { "IndexFinger_03", "index_03_l" },
        { "Finger_01",    "middle_01_l" },
        { "Finger_02",    "middle_02_l" },
        { "Finger_03",    "middle_03_l" },
        // Fingers (right — Synty appends " 1")
        { "Thumb_01 1",     "thumb_01_r" },
        { "Thumb_02 1",     "thumb_02_r" },
        { "IndexFinger_01 1", "index_01_r" },
        { "IndexFinger_02 1", "index_02_r" },
        { "IndexFinger_03 1", "index_03_r" },
        { "Finger_01 1",    "middle_01_r" },
        { "Finger_02 1",    "middle_02_r" },
        { "Finger_03 1",    "middle_03_r" },
        // Eyes
        { "Eyes",          "head" },
    };

    private Dictionary<string, Transform> frankBones = new Dictionary<string, Transform>();

    void Start()
    {
        // Cache all Frank_Mage bones
        CacheBones(transform, frankBones);

        // Hide Frank body mesh (but keep FX meshes)
        if (hideFrankBody)
            HideFrankBody();

        // Spawn and bind custom model
        if (customModelPrefab != null)
            SpawnCustomModel();
        else
            Debug.LogWarning("[BossSkinSwap] No custom model prefab assigned. Boss will be invisible.");
    }

    void HideFrankBody()
    {
        // The Frank_Mage body mesh is typically named "Frank_Mesh_Unity".
        // Hide ONLY body meshes — keep ALL effect meshes (projectiles, rings,
        // lasers, spells) so the boss's attack VFX still appear.
        foreach (var renderer in GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            string n = renderer.gameObject.name;
            if (IsFXRenderer(n)) continue;
            if (n.Contains("Frank") || n.Contains("Mesh"))
            {
                renderer.enabled = false;
                Debug.Log($"[BossSkinSwap] Hidden: {n}");
            }
        }

        // Also hide any regular MeshRenderers that are the body (not FX)
        foreach (var renderer in GetComponentsInChildren<MeshRenderer>())
        {
            string n = renderer.gameObject.name;
            if (IsFXRenderer(n)) continue;
            // This is likely a body part — hide it
            if (n.Contains("Frank") || n.Contains("Mesh"))
            {
                renderer.enabled = false;
                Debug.Log($"[BossSkinSwap] Hidden mesh: {n}");
            }
        }
    }

    /// <summary>True if the renderer is an attack/effect visual that must stay visible.</summary>
    static bool IsFXRenderer(string name)
    {
        string n = name.ToLower();
        return n.Contains("fx") || n.Contains("projectile") || n.Contains("ring")
            || n.Contains("laser") || n.Contains("spell") || n.Contains("magic")
            || n.Contains("cast") || n.Contains("vfx") || n.Contains("effect")
            || n.Contains("glow") || n.Contains("trail") || n.Contains("aura")
            || n.Contains("beam") || n.Contains("orb") || n.Contains("aoe")
            // weapons/staves are not body parts either
            || n.Contains("weapon") || n.Contains("staff");
    }

    void SpawnCustomModel()
    {
        GameObject model = Instantiate(customModelPrefab, transform);
        model.transform.localPosition = new Vector3(0f, heightOffset, 0f);
        model.transform.localRotation = Quaternion.identity;
        model.transform.localScale = Vector3.one * modelScale;
        model.name = "CustomSkin";

        // Disable any Animator on the custom model (Frank_Mage drives animation)
        Animator modelAnimator = model.GetComponent<Animator>();
        if (modelAnimator != null)
        {
            modelAnimator.enabled = false;
            Debug.Log("[BossSkinSwap] Disabled Animator on custom model (Frank_Mage skeleton drives all).");
        }

        // Rebind all SkinnedMeshRenderers to Frank_Mage skeleton
        foreach (var smr in model.GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            RebindSkin(smr);
        }
    }

    void RebindSkin(SkinnedMeshRenderer smr)
    {
        Transform[] originalBones = smr.bones;
        Transform[] newBones = new Transform[originalBones.Length];
        int mapped = 0;

        for (int i = 0; i < originalBones.Length; i++)
        {
            if (originalBones[i] == null) continue;

            string customBoneName = originalBones[i].name;
            string frankBoneName = null;

            // Try direct mapping
            if (BONE_MAP.TryGetValue(customBoneName, out frankBoneName))
            {
                if (frankBones.TryGetValue(frankBoneName, out Transform frankBone))
                {
                    newBones[i] = frankBone;
                    mapped++;
                    continue;
                }
            }

            // Try fuzzy match (case-insensitive contains)
            string lower = customBoneName.ToLower();
            foreach (var kvp in frankBones)
            {
                if (kvp.Key.ToLower().Contains(lower) || lower.Contains(kvp.Key.ToLower()))
                {
                    newBones[i] = kvp.Value;
                    mapped++;
                    break;
                }
            }

            // If still not found, try keeping original (will show a warning)
            if (newBones[i] == null)
            {
                Debug.LogWarning($"[BossSkinSwap] Could not map bone '{customBoneName}' — mesh may deform incorrectly.");
                newBones[i] = originalBones[i];
            }
        }

        smr.bones = newBones;

        // Remap rootBone too
        if (smr.rootBone != null)
        {
            string rootName = smr.rootBone.name;
            if (BONE_MAP.TryGetValue(rootName, out string mappedRoot) && frankBones.ContainsKey(mappedRoot))
                smr.rootBone = frankBones[mappedRoot];
            else if (frankBones.ContainsKey("pelvis"))
                smr.rootBone = frankBones["pelvis"];
        }

        Debug.Log($"[BossSkinSwap] Rebound '{smr.gameObject.name}': {mapped}/{originalBones.Length} bones mapped.");
    }

    void CacheBones(Transform root, Dictionary<string, Transform> dict)
    {
        dict[root.name] = root;
        for (int i = 0; i < root.childCount; i++)
            CacheBones(root.GetChild(i), dict);
    }
}

using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

/// <summary>
/// Editor tool: builds a complete Boss_Mage Animator Controller from the Frank_Mage
/// Generic animation clips. Includes a locomotion blend tree + every attack/skill/combo state.
///
/// Usage: Unity menu → Tools → Build Boss Mage Animator
/// </summary>
public class BossAnimatorBuilder
{
    // Path to the Root_Motion animation FBXes
    static readonly string ANIM_ROOT = "Assets/Assets(Graphics)/animations/Frank_Mage/Assets/Animations/FBX Animation/Root_Motion";

    [MenuItem("Tools/Build Boss Mage Animator")]
    public static void Build()
    {
        // Ensure the output folder exists
        if (!AssetDatabase.IsValidFolder("Assets/Animations"))
            AssetDatabase.CreateFolder("Assets", "Animations");

        string savePath = "Assets/Animations/Boss_Mage.controller";

        // Delete old controller if it exists
        if (AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(savePath) != null)
            AssetDatabase.DeleteAsset(savePath);

        // Create controller
        var controller = AnimatorController.CreateAnimatorControllerAtPath(savePath);
        if (controller == null)
        {
            Debug.LogError("[BossAnimatorBuilder] Failed to create controller. Check the Console for errors.");
            return;
        }

        // Add Speed parameter for locomotion blend tree
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);

        var rootStateMachine = controller.layers[0].stateMachine;

        // ── Locomotion blend tree (Idle / Walk / Run) ──
        BlendTree blendTree;
        var locomotionState = controller.CreateBlendTreeInController("Locomotion", out blendTree, 0);
        blendTree.blendParameter = "Speed";
        blendTree.blendType = BlendTreeType.Simple1D;

        AnimationClip idleClip = LoadClip("Frank_RPG_Mage_Idle");
        AnimationClip walkClip = LoadClip("Frank_RPG_Mage_Walk");
        AnimationClip runClip  = LoadClip("Frank_RPG_Mage_Run01");

        if (idleClip != null) blendTree.AddChild(idleClip, 0f);
        if (walkClip != null) blendTree.AddChild(walkClip, 2f);
        if (runClip  != null) blendTree.AddChild(runClip, 5f);

        rootStateMachine.defaultState = locomotionState;

        // ── Attack states (Attack01-06) ──
        for (int i = 1; i <= 6; i++)
            AddState(rootStateMachine, $"Frank_RPG_Mage_Attack{i:D2}");

        // ── Combo individual hits ──
        for (int c = 1; c <= 3; c++)
            for (int h = 1; h <= 3; h++)
                AddState(rootStateMachine, $"Frank_RPG_Mage_Combo{c:D2}_{h}");

        // Combo04 has 4 hits
        for (int h = 1; h <= 4; h++)
            AddState(rootStateMachine, $"Frank_RPG_Mage_Combo04_{h}");

        // ── Combo All versions ──
        for (int c = 1; c <= 4; c++)
            AddState(rootStateMachine, $"Frank_RPG_Mage_Combo{c:D2}_All");

        // ── Skill states (Skill01-07) ──
        for (int i = 1; i <= 7; i++)
            AddState(rootStateMachine, $"Frank_RPG_Mage_Skill{i:D2}");

        // ── Evades ──
        AddState(rootStateMachine, "Frank_RPG_Mage_Evade_B");
        AddState(rootStateMachine, "Frank_RPG_Mage_Evade_F");
        AddState(rootStateMachine, "Frank_RPG_Mage_Evade_L");
        AddState(rootStateMachine, "Frank_RPG_Mage_Evade_R");

        // ── Steps ──
        AddState(rootStateMachine, "Frank_RPG_Mage_Step_B");
        AddState(rootStateMachine, "Frank_RPG_Mage_Step_F");
        AddState(rootStateMachine, "Frank_RPG_Mage_Step_L");
        AddState(rootStateMachine, "Frank_RPG_Mage_Step_R");

        // ── Hit reactions ──
        AddState(rootStateMachine, "Frank_RPG_Mage_Hit01");
        AddState(rootStateMachine, "Frank_RPG_Mage_Hit02");
        AddState(rootStateMachine, "Frank_RPG_Mage_Hit03");

        // ── Knockback / Knockdown / Getup ──
        AddState(rootStateMachine, "Frank_RPG_Mage_Hit_Knockback");
        AddState(rootStateMachine, "Frank_RPG_Mage_Hit_Knockdown");
        AddState(rootStateMachine, "Frank_RPG_Mage_Hit_Knockdown_Loop");
        AddState(rootStateMachine, "Frank_RPG_Mage_Getup01");
        AddState(rootStateMachine, "Frank_RPG_Mage_Getup02");

        // ── Block / Guard ──
        AddState(rootStateMachine, "Frank_RPG_Mage_Block");
        AddState(rootStateMachine, "Frank_RPG_Mage_Guard");

        // ── Jump ──
        AddState(rootStateMachine, "Frank_RPG_Mage_Jump_01");
        AddState(rootStateMachine, "Frank_RPG_Mage_Jump_02");
        AddState(rootStateMachine, "Frank_RPG_Mage_Jump_ZeroHeight");

        // ── Equip / Unequip ──
        AddState(rootStateMachine, "Frank_RPG_Mage_Equip");
        AddState(rootStateMachine, "Frank_RPG_Mage_Unequip");
        AddState(rootStateMachine, "Frank_RPG_Mage_Unequip_Idle");
        AddState(rootStateMachine, "Frank_RPG_Mage_Unequip_Run");
        AddState(rootStateMachine, "Frank_RPG_Mage_Unequip_Run_Faster");

        // ── Extra locomotion ──
        AddState(rootStateMachine, "Frank_RPG_Mage_Run02");
        AddState(rootStateMachine, "Frank_RPG_Mage_Walk_Faster");

        // ── Deaths ──
        AddState(rootStateMachine, "Frank_RPG_Mage_Die");
        AddState(rootStateMachine, "Frank_RPG_Mage_Die02");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[BossAnimatorBuilder] Created controller at {savePath} with all Frank_Mage states.");
        Selection.activeObject = controller;
        EditorGUIUtility.PingObject(controller);
    }

    static void AddState(AnimatorStateMachine sm, string clipName)
    {
        AnimationClip clip = LoadClip(clipName);
        if (clip == null)
        {
            Debug.LogWarning($"[BossAnimatorBuilder] Clip not found, skipping state: {clipName}");
            return;
        }
        var state = sm.AddState(clipName);
        state.motion = clip;
    }

    static AnimationClip LoadClip(string clipName)
    {
        // The FBX file name matches the clip name
        string fbxPath = $"{ANIM_ROOT}/{clipName}.FBX";
        
        // Try loading — the clip inside is typically named "Take 001" for Generic
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
        if (assets == null || assets.Length == 0)
        {
            // Try lowercase extension
            fbxPath = $"{ANIM_ROOT}/{clipName}.fbx";
            assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
        }

        if (assets != null)
        {
            foreach (Object asset in assets)
            {
                if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                    return clip;
            }
        }

        return null;
    }
}

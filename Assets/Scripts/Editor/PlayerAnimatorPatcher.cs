using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

/// <summary>
/// Adds a "HealWalk" state to the Player Animator Controller using the Sharp walking clip.
/// Run from: Tools → Patch Player Animator (Add HealWalk)
/// </summary>
public class PlayerAnimatorPatcher
{
    [MenuItem("Tools/Patch Player Animator (Add HealWalk)")]
    public static void Patch()
    {
        string controllerPath = "Assets/Scripts/PlayerAnimatorController.controller";
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null)
        {
            Debug.LogError($"[PlayerAnimatorPatcher] Controller not found at {controllerPath}");
            return;
        }

        // Load the sharp walking clip
        string walkClipPath = "Assets/Assets(Graphics)/animations/sharp/Animations/Locomotion/walking.anim";
        AnimationClip walkClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(walkClipPath);
        if (walkClip == null)
        {
            Debug.LogError($"[PlayerAnimatorPatcher] Walking clip not found at {walkClipPath}");
            return;
        }

        // Check if state already exists in the base layer
        var rootSM = controller.layers[0].stateMachine;
        foreach (var state in rootSM.states)
        {
            if (state.state.name == "HealWalk")
            {
                Debug.Log("[PlayerAnimatorPatcher] HealWalk state already exists. Updating clip.");
                state.state.motion = walkClip;
                state.state.speed = 1f;
                EditorUtility.SetDirty(controller);
                AssetDatabase.SaveAssets();
                return;
            }
        }

        // Add HealWalk state
        var healWalkState = rootSM.AddState("HealWalk");
        healWalkState.motion = walkClip;
        healWalkState.speed = 1f;

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[PlayerAnimatorPatcher] Added 'HealWalk' state to base layer of PlayerAnimatorController.");
    }
}

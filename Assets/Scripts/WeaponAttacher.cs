using UnityEngine;

/// <summary>
/// Automatically finds the hand bone on ANY character model and attaches a weapon prefab.
/// Works regardless of the skeleton naming convention (Mixamo, UE Mannequin, Synty, etc.)
///
/// SETUP:
///   1. Add this to the character (player or enemy)
///   2. Assign weaponPrefab in the inspector
///   3. Set hand to Left or Right
///   4. Adjust positionOffset and rotationOffset to fine-tune grip
///   5. Hit Play — weapon appears in the correct hand
///
/// WEAPON PREFAB REQUIREMENTS:
///   - The prefab needs a Collider (BoxCollider, CapsuleCollider, etc.)
///   - Add a WeaponHitbox component if you want it to deal damage
///   - The collider should be set to Trigger (isTrigger = true)
/// </summary>
public class WeaponAttacher : MonoBehaviour
{
    public enum Hand { Right, Left }

    [Header("Weapon")]
    public GameObject weaponPrefab;
    public Hand hand = Hand.Right;

    [Header("Manual Hand Bone (fallback if auto-detect fails)")]
    public Transform manualHandBone;

    [Header("Offset (fine-tune in Play mode, copy values back)")]
    public Vector3 positionOffset = Vector3.zero;
    public Vector3 rotationOffset = Vector3.zero;

    [Header("Debug")]
    public bool showDebugLog = true;

    private Transform handBone;
    private GameObject weaponInstance;

    // Common right hand bone names across different skeletons
    static readonly string[] RIGHT_HAND_NAMES = {
        "Hand_R", "hand_r", "RightHand", "Right_Hand", "R_Hand",
        "mixamorig:RightHand", "Bip01 R Hand", "R Hand", "hand.R",
        "RightHandIndex1", "Wrist_R", "J_Bip_R_Hand",
        "Right wrist", "right_hand", "def_hand_R"
    };

    static readonly string[] LEFT_HAND_NAMES = {
        "Hand_L", "hand_l", "LeftHand", "Left_Hand", "L_Hand",
        "mixamorig:LeftHand", "Bip01 L Hand", "L Hand", "hand.L",
        "LeftHandIndex1", "Wrist_L", "J_Bip_L_Hand",
        "Left wrist", "left_hand", "def_hand_L"
    };

    void Start()
    {
        if (weaponPrefab == null)
        {
            Debug.LogWarning("[WeaponAttacher] No weapon prefab assigned. Please assign a weapon prefab in the Inspector.");
            return;
        }

        // Try auto-detect first
        handBone = FindHandBone();

        // Fallback to manual assignment if auto-detect fails
        if (handBone == null && manualHandBone != null)
        {
            handBone = manualHandBone;
            Debug.Log($"[WeaponAttacher] Using manually assigned hand bone: '{handBone.name}'");
        }

        if (handBone == null)
        {
            Debug.LogWarning($"[WeaponAttacher] Could not find {hand} hand bone on {gameObject.name}. " +
                $"Listing all bones for debugging:");
            PrintAllBones(transform, 0);
            return;
        }

        if (showDebugLog)
            Debug.Log($"[WeaponAttacher] Found {hand} hand bone: '{handBone.name}' on {gameObject.name}");

        AttachWeapon();
    }

    Transform FindHandBone()
    {
        string[] names = hand == Hand.Right ? RIGHT_HAND_NAMES : LEFT_HAND_NAMES;

        // Try exact matches first
        foreach (string boneName in names)
        {
            Transform bone = FindBoneRecursive(transform, boneName, false);
            if (bone != null)
            {
                Debug.Log($"[WeaponAttacher] Found exact match: '{boneName}'");
                return bone;
            }
        }

        // Try partial/contains match as fallback
        string[] keywords = hand == Hand.Right
            ? new[] { "right", "hand_r", "r_hand" }
            : new[] { "left", "hand_l", "l_hand" };

        Transform[] allChildren = GetComponentsInChildren<Transform>();
        foreach (Transform child in allChildren)
        {
            string lower = child.name.ToLower();
            foreach (string keyword in keywords)
            {
                if (lower.Contains("hand") && lower.Contains(keyword.Split('_')[0]))
                {
                    Debug.Log($"[WeaponAttacher] Found partial match: '{child.name}' for keyword '{keyword}'");
                    return child;
                }
            }
        }

        Debug.LogWarning($"[WeaponAttacher] No hand bone found. Character model may use non-standard bone names.");
        return null;
    }

    Transform FindBoneRecursive(Transform parent, string boneName, bool ignoreCase)
    {
        System.StringComparison comp = ignoreCase
            ? System.StringComparison.OrdinalIgnoreCase
            : System.StringComparison.Ordinal;

        if (parent.name.Equals(boneName, comp))
            return parent;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform found = FindBoneRecursive(parent.GetChild(i), boneName, ignoreCase);
            if (found != null) return found;
        }
        return null;
    }

    void AttachWeapon()
    {
        if (weaponPrefab == null)
        {
            Debug.LogWarning("[WeaponAttacher] No weapon prefab assigned.");
            return;
        }

        weaponInstance = Instantiate(weaponPrefab, handBone);
        weaponInstance.transform.localPosition = positionOffset;
        weaponInstance.transform.localRotation = Quaternion.Euler(rotationOffset);

        Debug.Log($"[WeaponAttacher] Weapon instantiated. Parent: '{weaponInstance.transform.parent.name}', " +
            $"WorldPos: {weaponInstance.transform.position}, LocalPos: {weaponInstance.transform.localPosition}, " +
            $"Scale: {weaponInstance.transform.lossyScale}");

        // Auto-setup WeaponHitbox owner if present
        WeaponHitbox hitbox = weaponInstance.GetComponent<WeaponHitbox>();
        if (hitbox == null)
            hitbox = weaponInstance.GetComponentInChildren<WeaponHitbox>();

        if (hitbox != null)
        {
            Entity ownerEntity = GetComponent<Entity>();
            if (ownerEntity != null)
            {
                hitbox.owner = ownerEntity;
                hitbox.Deactivate();
                Debug.Log($"[WeaponAttacher] WeaponHitbox found and configured");
            }

            // Auto-assign to PlayerController — always use the live instance, not any prefab reference
            PlayerController pc = GetComponent<PlayerController>();
            if (pc != null)
            {
                pc.weaponHitbox = hitbox;
                Debug.Log("[WeaponAttacher] Assigned live WeaponHitbox to PlayerController");
            }
        }
        else
        {
            Debug.LogWarning("[WeaponAttacher] No WeaponHitbox found on weapon prefab");
        }

        // Check if weapon has a renderer
        Renderer[] rends = weaponInstance.GetComponentsInChildren<Renderer>(true);
        if (rends.Length == 0)
        {
            Debug.LogWarning("[WeaponAttacher] No Renderer found on weapon prefab - weapon will be invisible!");
        }
        else
        {
            foreach (Renderer r in rends)
            {
                r.enabled = true; // Force enable in case it was disabled
                Debug.Log($"[WeaponAttacher] Renderer '{r.gameObject.name}' enabled, bounds: {r.bounds.size}");
            }
        }

        if (showDebugLog)
            Debug.Log($"[WeaponAttacher] Attached '{weaponPrefab.name}' to '{handBone.name}'");
    }

    void PrintAllBones(Transform parent, int depth)
    {
        Debug.Log($"{"".PadLeft(depth * 2)}> {parent.name}");
        for (int i = 0; i < parent.childCount; i++)
            PrintAllBones(parent.GetChild(i), depth + 1);
    }

    /// <summary>
    /// Call this at runtime to change the weapon.
    /// </summary>
    public void SwapWeapon(GameObject newWeaponPrefab)
    {
        if (weaponInstance != null)
            Destroy(weaponInstance);

        weaponPrefab = newWeaponPrefab;
        if (handBone != null)
            AttachWeapon();
    }

    /// <summary>
    /// Returns the current hand bone transform (useful for VFX spawning).
    /// </summary>
    public Transform GetHandBone() => handBone;
}

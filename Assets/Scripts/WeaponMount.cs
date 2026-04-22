using UnityEngine;

/// <summary>
/// Attach this to the player root. It finds the hand bone at runtime
/// and parents the weapon to it so it follows all animations.
/// Works with Synty's modular character skeleton (bone name: Hand_R).
/// </summary>
public class WeaponMount : MonoBehaviour
{
    [Header("Weapon")]
    [Tooltip("The weapon GameObject (with mesh + WeaponHitbox) to attach to the hand.")]
    public GameObject weaponPrefab;

    [Tooltip("If the weapon already exists in the scene, drag it here instead of using a prefab.")]
    public Transform existingWeapon;

    [Header("Hand Bone")]
    [Tooltip("Name of the right-hand bone in the skeleton hierarchy.")]
    public string handBoneName = "Hand_R";

    [Header("Offset (adjust until weapon looks right in hand)")]
    public Vector3 positionOffset = Vector3.zero;
    public Vector3 rotationOffset = Vector3.zero;

    [Header("Result (read-only)")]
    [SerializeField] private Transform mountedWeapon;

    void Start()
    {
        // Delay by one frame so CharacterLoader / CharacterRandomizer finishes first
        StartCoroutine(AttachAfterFrame());
    }

    System.Collections.IEnumerator AttachAfterFrame()
    {
        yield return null;
        AttachWeapon();
    }

    public void AttachWeapon()
    {
        // Find the hand bone recursively in all children
        Transform handBone = FindBoneRecursive(transform, handBoneName);

        if (handBone == null)
        {
            Debug.LogError($"[WeaponMount] Could not find bone '{handBoneName}' under {gameObject.name}. " +
                           "Check the bone name in your character's skeleton hierarchy.");
            return;
        }

        Transform weapon = null;

        if (existingWeapon != null)
        {
            // Re-parent existing scene weapon to the hand bone
            weapon = existingWeapon;
        }
        else if (weaponPrefab != null)
        {
            // Instantiate a new weapon from prefab
            GameObject go = Instantiate(weaponPrefab);
            weapon = go.transform;
        }
        else
        {
            Debug.LogWarning("[WeaponMount] No weapon prefab or existing weapon assigned.");
            return;
        }

        // Parent to hand bone
        weapon.SetParent(handBone, false);
        weapon.localPosition = positionOffset;
        weapon.localRotation = Quaternion.Euler(rotationOffset);

        mountedWeapon = weapon;

        // Auto-assign WeaponHitbox on PlayerController if present
        PlayerController pc = GetComponent<PlayerController>();
        WeaponHitbox hitbox = weapon.GetComponentInChildren<WeaponHitbox>();
        if (pc != null && hitbox != null && pc.weaponHitbox == null)
        {
            pc.weaponHitbox = hitbox;
            hitbox.owner = pc;
        }

        Debug.Log($"[WeaponMount] Weapon attached to bone '{handBoneName}' on {gameObject.name}");
    }

    Transform FindBoneRecursive(Transform parent, string boneName)
    {
        if (parent.name == boneName)
            return parent;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform found = FindBoneRecursive(parent.GetChild(i), boneName);
            if (found != null) return found;
        }

        return null;
    }
}

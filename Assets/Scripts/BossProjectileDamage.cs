using UnityEngine;

/// <summary>
/// Attach to the boss root. Finds the FX_Projectile bones in the Frank_Mage
/// skeleton and adds trigger colliders so they deal damage when they hit the player.
/// The projectile visuals are already animated by the Generic clips — this just
/// makes them hurt.
/// </summary>
public class BossProjectileDamage : MonoBehaviour
{
    [Header("Settings")]
    public float projectileDamage = 20f;
    public float colliderRadius = 0.5f;
    public string playerTag = "Player";

    [Header("FX bone names to search for")]
    public string[] fxBoneNames = {
        "FX_Projectile", "FX_Projectile1",
        "FX_Projectile_Mesh", "FX_Projectile_Mesh1",
        "FX_Laser", "FX_Ring_Mesh"
    };

    private Entity owner;

    void Start()
    {
        owner = GetComponent<Entity>();
        StartCoroutine(SetupAfterFrame());
    }

    System.Collections.IEnumerator SetupAfterFrame()
    {
        yield return null;

        int found = 0;
        foreach (string boneName in fxBoneNames)
        {
            Transform bone = FindBoneRecursive(transform, boneName);
            if (bone != null)
            {
                SetupFXCollider(bone);
                found++;
            }
        }

        Debug.Log($"[BossProjectileDamage] Set up {found} FX hitboxes.");
    }

    void SetupFXCollider(Transform fxBone)
    {
        // Add a trigger collider if it doesn't have one
        SphereCollider col = fxBone.GetComponent<SphereCollider>();
        if (col == null)
        {
            col = fxBone.gameObject.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = colliderRadius;
        }

        // Add a Rigidbody so OnTriggerEnter works (kinematic so physics doesn't move it)
        Rigidbody rb = fxBone.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = fxBone.gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        // Add the damage dealer script
        FXHitbox hitbox = fxBone.GetComponent<FXHitbox>();
        if (hitbox == null)
        {
            hitbox = fxBone.gameObject.AddComponent<FXHitbox>();
            hitbox.damage = projectileDamage;
            hitbox.owner = owner;
            hitbox.playerTag = playerTag;
        }
    }

    Transform FindBoneRecursive(Transform parent, string boneName)
    {
        if (parent.name == boneName) return parent;
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform found = FindBoneRecursive(parent.GetChild(i), boneName);
            if (found != null) return found;
        }
        return null;
    }
}

/// <summary>
/// Deals damage when an animated FX mesh overlaps the player.
/// Only damages once per activation (resets when the FX goes inactive/active).
/// </summary>
public class FXHitbox : MonoBehaviour
{
    public float damage = 20f;
    public Entity owner;
    public string playerTag = "Player";
    public float hitCooldown = 1f;

    private float lastHitTime = -999f;
    private bool wasActive = false;

    void Update()
    {
        // Reset hit tracking when the FX cycles (goes inactive then active again)
        bool isVisible = IsVisible();
        if (!wasActive && isVisible)
            lastHitTime = -999f; // new activation, allow hitting again
        wasActive = isVisible;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!IsVisible()) return;
        if (Time.time - lastHitTime < hitCooldown) return;
        if (!other.CompareTag(playerTag)) return;

        Entity target = other.GetComponent<Entity>();
        if (target != null && target != owner)
        {
            target.TakeDamage(damage, owner);
            lastHitTime = Time.time;
            Debug.Log($"[FXHitbox] {gameObject.name} hit {other.name} for {damage} damage!");
        }
    }

    bool IsVisible()
    {
        // FX meshes are "active" when their scale is non-zero
        // (the animation scales them up when firing and down when not)
        return transform.localScale.sqrMagnitude > 0.01f
            && gameObject.activeInHierarchy;
    }
}

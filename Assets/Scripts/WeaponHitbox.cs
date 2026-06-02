using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Attach to a weapon GameObject that has a Collider (used for size reference).
/// The owning Entity enables/disables this via animation events.
/// Deals damage ONCE per swing to each target hit.
/// Uses active Physics.OverlapBox instead of OnTriggerEnter for reliability
/// (kinematic rigidbodies moved via bone parenting don't fire triggers reliably).
/// </summary>
[RequireComponent(typeof(Collider))]
public class WeaponHitbox : MonoBehaviour
{
    [HideInInspector] public float damage = 25f;
    [HideInInspector] public Entity owner;

    private HashSet<Entity> alreadyHit = new HashSet<Entity>();
    private BoxCollider hitboxCollider;
    private bool isActive = false;

    void Awake()
    {
        hitboxCollider = GetComponent<BoxCollider>();
        if (hitboxCollider == null)
        {
            hitboxCollider = gameObject.AddComponent<BoxCollider>();
            Debug.LogWarning("[WeaponHitbox] No BoxCollider found — auto-added one. Adjust its size in the Inspector.");
        }
        // Disable the collider — we use manual overlap, not trigger events
        hitboxCollider.isTrigger = true;
        hitboxCollider.enabled = false;
    }

    /// <summary>Call from animation event or script to start detecting hits.</summary>
    public void Activate(float swingDamage)
    {
        if (hitboxCollider == null)
        {
            Debug.LogWarning("[WeaponHitbox] No collider found. Weapon hitbox will not work.");
            return;
        }
        damage = swingDamage;
        alreadyHit.Clear();
        isActive = true;
    }

    /// <summary>Call from animation event or script to stop detecting hits.</summary>
    public void Deactivate()
    {
        isActive = false;
        alreadyHit.Clear();
    }

    void FixedUpdate()
    {
        if (!isActive || hitboxCollider == null) return;

        // Calculate world-space box from the collider's bounds
        Vector3 worldCenter = transform.TransformPoint(hitboxCollider.center);
        Vector3 halfExtents = Vector3.Scale(hitboxCollider.size * 0.5f, transform.lossyScale);

        // Enforce minimum half extents — thin sword colliders miss too easily
        halfExtents.x = Mathf.Max(halfExtents.x, 0.3f);
        halfExtents.y = Mathf.Max(halfExtents.y, 0.4f);
        halfExtents.z = Mathf.Max(halfExtents.z, 0.3f);

        Collider[] hits = Physics.OverlapBox(worldCenter, halfExtents, transform.rotation, Physics.AllLayers, QueryTriggerInteraction.Ignore);

        foreach (Collider col in hits)
        {
            if (col.gameObject == gameObject) continue; // skip self
            if (col.transform.IsChildOf(owner?.transform)) continue; // skip owner hierarchy

            Entity target = col.GetComponent<Entity>();
            if (target == null) target = col.GetComponentInParent<Entity>();

            if (target != null && target != owner && !alreadyHit.Contains(target))
            {
                alreadyHit.Add(target);
                target.TakeDamage(damage, owner);
                Debug.Log($"[WeaponHitbox] HIT {target.gameObject.name} for {damage} damage");
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        BoxCollider col = GetComponent<BoxCollider>();
        if (col == null) return;
        Gizmos.color = isActive ? Color.red : Color.yellow;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(col.center, col.size);
    }
}

using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Attach to a weapon GameObject that has a Collider set to IsTrigger.
/// The owning Entity enables/disables this via animation events.
/// Deals damage ONCE per swing to each target hit.
/// </summary>
[RequireComponent(typeof(Collider))]
public class WeaponHitbox : MonoBehaviour
{
    [HideInInspector] public float damage = 25f;
    [HideInInspector] public Entity owner;

    private HashSet<Entity> alreadyHit = new HashSet<Entity>();
    private Collider hitboxCollider;

    void Awake()
    {
        hitboxCollider = GetComponent<Collider>();
        hitboxCollider.isTrigger = true;
        hitboxCollider.enabled = false;
    }

    /// <summary>Call from animation event or script to start detecting hits.</summary>
    public void Activate(float swingDamage)
    {
        damage = swingDamage;
        alreadyHit.Clear();
        hitboxCollider.enabled = true;
    }

    /// <summary>Call from animation event or script to stop detecting hits.</summary>
    public void Deactivate()
    {
        hitboxCollider.enabled = false;
        alreadyHit.Clear();
    }

    void OnTriggerEnter(Collider other)
    {
        if (!hitboxCollider.enabled) return;

        Entity target = other.GetComponent<Entity>();
        if (target == null) target = other.GetComponentInParent<Entity>();

        if (target != null && target != owner && !alreadyHit.Contains(target))
        {
            alreadyHit.Add(target);
            target.TakeDamage(damage, owner);
        }
    }
}

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

        // Safety: if the object somehow has no collider (or only a CharacterController),
        // add a small trigger box so the hitbox can actually work.
        if (hitboxCollider == null || hitboxCollider is CharacterController)
        {
            if (hitboxCollider != null && hitboxCollider is CharacterController)
            {
                Debug.LogWarning($"[WeaponHitbox] {gameObject.name} had a CharacterController as its Collider. Adding a dedicated trigger collider instead.");
            }

            BoxCollider box = gameObject.AddComponent<BoxCollider>();
            FitColliderToMesh(box);
            hitboxCollider = box;
        }
        else if (hitboxCollider is BoxCollider existingBox)
        {
            // If a box collider exists but is still tiny/default, fit it to the mesh.
            if (existingBox.size.sqrMagnitude < 0.05f)
                FitColliderToMesh(existingBox);
        }
        else if (hitboxCollider is MeshCollider mesh)
        {
            // Non-convex MeshColliders cannot be triggers. Try to convex it, otherwise
            // replace it with a box trigger that covers the mesh.
            if (!mesh.convex)
            {
                try
                {
                    mesh.convex = true;
                }
                catch
                {
                    mesh.convex = false;
                }
            }

            if (!mesh.convex)
            {
                Debug.LogWarning($"[WeaponHitbox] {gameObject.name} has a non-convex MeshCollider; replacing with a fitted BoxCollider trigger.");
                mesh.enabled = false;
                BoxCollider newBox = gameObject.AddComponent<BoxCollider>();
                FitColliderToMesh(newBox);
                hitboxCollider = newBox;
            }
        }

        hitboxCollider.isTrigger = true;
        hitboxCollider.enabled = false;

        // A trigger collider on its own will not reliably fire OnTriggerEnter
        // unless it has a Rigidbody. Add one and make it kinematic so it
        // follows the hand without being affected by physics.
        if (GetComponent<Rigidbody>() == null)
        {
            Rigidbody rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            // Interpolation on a fast-moving hand can make the sword lag behind and look floaty.
            rb.interpolation = RigidbodyInterpolation.None;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        }
    }

    void FitColliderToMesh(BoxCollider box)
    {
        MeshFilter mf = GetComponent<MeshFilter>();
        if (mf == null) mf = GetComponentInChildren<MeshFilter>();
        if (mf == null || mf.sharedMesh == null) return;

        // Fit the trigger box to the mesh's local axis-aligned bounds.
        // This makes the weapon hitbox match the actual blade/mesh shape.
        Bounds b = mf.sharedMesh.bounds;
        if (mf.transform == transform)
        {
            box.center = b.center;
            box.size = b.size;
        }
        else
        {
            Vector3 localCenter = transform.InverseTransformPoint(mf.transform.TransformPoint(b.center));
            Vector3 localExtents = transform.InverseTransformVector(mf.transform.TransformVector(b.extents));
            box.center = localCenter;
            box.size = localExtents * 2f;
        }

        // Pad the width/depth so fast swings don't tunnel through enemies.
        // A katana is thin, but gameplay hit detection needs a little forgiveness.
        Vector3 padded = box.size;
        padded.x = Mathf.Max(padded.x, 0.15f);
        padded.z = Mathf.Max(padded.z, 0.15f);
        box.size = padded;

        // Make sure the collider isn't degenerate.
        if (box.size.sqrMagnitude < 0.05f)
            box.size = Vector3.one * 0.1f;

    }

    /// <summary>Call from animation event or script to start detecting hits.</summary>
    public void Activate(float swingDamage)
    {
        if (!gameObject.activeInHierarchy)
        {
            Debug.LogWarning($"[WeaponHitbox] {gameObject.name} is not active in the scene. Cannot activate it (probably a prefab asset or disabled).");
            return;
        }

        if (hitboxCollider == null)
            hitboxCollider = GetComponent<Collider>();

        if (hitboxCollider == null)
        {
            Debug.LogWarning($"[WeaponHitbox] {gameObject.name} has no Collider; cannot activate.");
            return;
        }

        damage = swingDamage;
        alreadyHit.Clear();
        hitboxCollider.enabled = true;
    }

    /// <summary>Call from animation event or script to stop detecting hits.</summary>
    public void Deactivate()
    {
        if (hitboxCollider == null)
            hitboxCollider = GetComponent<Collider>();

        if (hitboxCollider != null)
            hitboxCollider.enabled = false;

        alreadyHit.Clear();
    }

    void OnTriggerEnter(Collider other)
    {
        TryHit(other);
    }

    void OnTriggerStay(Collider other)
    {
        // Catch hits that slip through between FixedUpdate steps on fast swings.
        TryHit(other);
    }

    void TryHit(Collider other)
    {
        if (hitboxCollider == null || !hitboxCollider.enabled) return;

        Entity target = other.GetComponent<Entity>();
        if (target == null) target = other.GetComponentInParent<Entity>();

        if (target != null && target != owner && !alreadyHit.Contains(target))
        {
            alreadyHit.Add(target);
            target.TakeDamage(damage, owner);
        }
    }
}

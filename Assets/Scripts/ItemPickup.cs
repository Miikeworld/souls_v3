using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ItemPickup : MonoBehaviour, IInteractable
{
    [Header("Item")]
    public ItemData item;
    public int quantity = 1;

    [Header("Prompt")]
    public string promptPrefix = "Pick up";

    [Header("Elden Ring-style floating effect")]
    public bool animatePickup = true;
    public float rotationSpeed = 90f;
    public float bobSpeed = 2f;
    public float bobHeight = 0.1f;
    public Transform modelToAnimate;

    [Header("Effects")]
    public GameObject pickupEffect;
    public AudioClip pickupSound;

    private Vector3 startPosition;
    private Collider pickupCollider;

    private void Awake()
    {
        // Make sure the item has a trigger collider for interaction.
        // Concave MeshColliders cannot be triggers, so we add a SphereCollider instead.
        pickupCollider = GetComponent<Collider>();

        if (pickupCollider is MeshCollider mesh && !mesh.convex)
        {
            Debug.LogWarning($"[ItemPickup] {gameObject.name} has a concave MeshCollider; adding a SphereCollider for interaction and disabling the MeshCollider.");
            mesh.enabled = false;

            SphereCollider sc = gameObject.AddComponent<SphereCollider>();
            sc.isTrigger = true;
            sc.radius = 0.5f;
            pickupCollider = sc;
        }
        else if (pickupCollider == null || pickupCollider is CharacterController)
        {
            if (pickupCollider is CharacterController)
            {
                Debug.LogWarning($"[ItemPickup] {gameObject.name} had a CharacterController as its Collider; adding a dedicated trigger instead.");
            }

            SphereCollider sc = gameObject.AddComponent<SphereCollider>();
            sc.isTrigger = true;
            sc.radius = 0.5f;
            pickupCollider = sc;
        }
        else if (!pickupCollider.isTrigger)
        {
            pickupCollider.isTrigger = true;
        }

        if (modelToAnimate == null)
            modelToAnimate = transform;

        startPosition = modelToAnimate.position;
    }

    private void Update()
    {
        if (!animatePickup) return;

        modelToAnimate.Rotate(0f, rotationSpeed * Time.deltaTime, 0f);

        if (bobHeight > 0f && bobSpeed > 0f)
        {
            float offset = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            modelToAnimate.position = startPosition + Vector3.up * offset;
        }
    }

    public string GetPrompt()
    {
        if (item == null) return "Pick up ???";
        return $"{promptPrefix} {item.itemName} x{quantity}";
    }

    public bool CanInteract(Transform interactor)
    {
        return item != null;
    }

    public void Interact(Transform interactor)
    {
        if (item == null) return;

        if (Inventory.Instance.Add(item, quantity))
        {
            if (pickupSound != null)
                AudioSource.PlayClipAtPoint(pickupSound, transform.position);

            if (pickupEffect != null)
                Instantiate(pickupEffect, transform.position, Quaternion.identity);

            Debug.Log($"Picked up {quantity}x {item.itemName}");

            Destroy(gameObject);
        }
    }
}

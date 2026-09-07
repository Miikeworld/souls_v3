using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Door : MonoBehaviour, IInteractable
{
    [Header("Teleport")]
    [Tooltip("Where the player will be teleported to.")]
    public Transform destination;

    [Tooltip("If >= 0, the player will be rotated to this Y angle after teleporting.")]
    public float fixedRotationY = -1f;

    [Header("Prompt")]
    public string prompt = "Open";

    [Header("Effects")]
    public GameObject openEffect;
    public AudioClip openSound;

    [Header("Gizmo")]
    public Color gizmoColor = Color.cyan;

    public string GetPrompt()
    {
        return prompt;
    }

    public bool CanInteract(Transform interactor)
    {
        return destination != null;
    }

    public void Interact(Transform interactor)
    {
        if (destination == null) return;

        CharacterController controller = interactor.GetComponent<CharacterController>();

        // Disable CharacterController before teleporting or it won't move
        if (controller != null)
            controller.enabled = false;

        interactor.position = destination.position;

        if (fixedRotationY >= 0f)
            interactor.rotation = Quaternion.Euler(0f, fixedRotationY, 0f);

        if (controller != null)
            controller.enabled = true;

        if (openSound != null)
            AudioSource.PlayClipAtPoint(openSound, interactor.position);

        if (openEffect != null)
            Instantiate(openEffect, destination.position, Quaternion.identity);

        Debug.Log($"Teleported {interactor.name} to {destination.name}");
    }

    private void OnDrawGizmos()
    {
        if (destination == null) return;

        Gizmos.color = gizmoColor;
        Gizmos.DrawLine(transform.position, destination.position);
        Gizmos.DrawWireSphere(transform.position, 0.5f);
        Gizmos.DrawWireSphere(destination.position, 0.5f);
    }
}

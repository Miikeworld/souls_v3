using UnityEngine;

public interface IInteractable
{
    /// <summary>Returns true if the interactor can use this right now.</summary>
    bool CanInteract(Transform interactor);

    /// <summary>Perform the interaction.</summary>
    void Interact(Transform interactor);

    /// <summary>Short prompt shown to the player, e.g. "Open" or "Pick up Gold".</summary>
    string GetPrompt();
}

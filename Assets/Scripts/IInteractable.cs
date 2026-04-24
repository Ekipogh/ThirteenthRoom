using UnityEngine;

public interface IInteractable
{
    void Interact(PlayerInteractor playerInteractor);
    string GetInteractionPrompt();
}

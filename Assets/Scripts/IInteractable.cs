using UnityEngine;

public interface IInteractable
{
    void Interact(PlayerInteractor playerInteractor);
    string GetInteractionPrompt(PlayerInteractor playerInteractor);
}

public interface IHoldInteractable
{
    string GetInteractionPrompt(PlayerInteractor playerInteractor);
    void BeginHoldInteract(PlayerInteractor playerInteractor);
    void HoldInteract(PlayerInteractor playerInteractor, float deltaTime);
    void EndHoldInteract(PlayerInteractor playerInteractor);
}

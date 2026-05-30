using UnityEngine;

public class MainDoorsInteractable : MonoBehaviour, IInteractable
{
    public string GetInteractionPrompt(PlayerInteractor playerInteractor)
    {
        return "The main doors are locked. You need to find another way out.";
    }

    public void Interact(PlayerInteractor playerInteractor)
    {
        return;
    }
}

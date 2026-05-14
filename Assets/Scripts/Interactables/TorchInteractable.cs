using UnityEngine;

public class TorchInteractable : MonoBehaviour, IInteractable
{
    public string GetInteractionPrompt(PlayerInteractor playerInteractor)
    {
        return "Press E to pick up the torch";
    }

    public void Interact(PlayerInteractor playerInteractor)
    {
        if (playerInteractor.TryGetComponent<PlayerController>(out var player))
        {
            player.EnableTorch();
            Destroy(gameObject);
        }
    }
}

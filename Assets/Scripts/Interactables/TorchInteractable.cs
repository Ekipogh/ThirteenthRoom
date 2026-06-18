using UnityEngine;

public class TorchInteractable : ItemPickup
{
    public override void Interact(PlayerInteractor playerInteractor)
    {
        if (playerInteractor.TryGetComponent<PlayerController>(out var player))
        {
            player.EnableTorch();
            Destroy(gameObject);
        }
    }
}

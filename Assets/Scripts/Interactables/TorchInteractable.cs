using UnityEngine;

public class TorchInteractable : ItemPickup
{
    public override void Interact(PlayerInteractor playerInteractor)
    {
        if (PickupSound != null)
        {
            playerInteractor.GetComponent<PlayerAudioManager>().PlayPickupSound(PickupSound);
        }
        if (playerInteractor.TryGetComponent<PlayerController>(out var player))
        {
            player.EnableTorch();
            Destroy(gameObject);
        }
    }
}

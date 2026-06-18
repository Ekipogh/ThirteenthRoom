using UnityEngine;

public class ItemPickup : MonoBehaviour, IInteractable
{
    [Header("Item")]
    [SerializeField] ItemDefinition itemDefinition;
    public AudioClip PickupSound;

    [Header("Scoring")]
    [SerializeField] ScoreManager scoreManager;

    public string GetInteractionPrompt(PlayerInteractor playerInteractor)
    {
        return $"Press E to pick up {itemDefinition.DisplayName}";
    }

    public virtual void Interact(PlayerInteractor playerInteractor)
    {
        if (scoreManager != null)
        {
            scoreManager.AddScore(itemDefinition.ScoreOnPickup);
        }
        if (PickupSound != null)
        {
            playerInteractor.GetComponent<PlayerAudioManager>().PlayPickupSound(PickupSound);
        }
        if (this is ITargetable targetable)
        {
            playerInteractor.Inventory.AddInstanceItem(itemDefinition, targetable.TargetID);
        }
        else
        {
            playerInteractor.Inventory.AddItem(itemDefinition);
        }
        Destroy(gameObject);
    }
}

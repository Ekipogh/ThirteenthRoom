using UnityEngine;

public class ItemPickup : MonoBehaviour, IInteractable, IScoreReceiver
{
    [Header("Item")]
    [SerializeField] ItemDefinition itemDefinition;
    [SerializeField, Min(1)] int quantity = 1;
    public AudioClip PickupSound;

    [Header("Scoring")]
    [SerializeField] ScoreManager scoreManager;

    public void BindScoreManager(ScoreManager manager) => scoreManager = manager;

    public virtual string GetInteractionPrompt(PlayerInteractor playerInteractor)
    {
        return $"Press E to pick up {itemDefinition.DisplayName}";
    }

    protected virtual string InventoryDetail => null;

    public virtual void Interact(PlayerInteractor playerInteractor)
    {
        if (itemDefinition == null || playerInteractor == null || playerInteractor.Inventory == null) return;
        if (scoreManager != null)
        {
            scoreManager.AddScore(itemDefinition.ScoreOnPickup);
        }
        if (PickupSound != null)
        {
            playerInteractor.GetComponent<PlayerAudioManager>()?.PlayPickupSound(PickupSound);
        }
        if (this is ITargetable targetable)
        {
            playerInteractor.Inventory.AddInstanceItem(itemDefinition, targetable.TargetID, InventoryDetail);
        }
        else
        {
            playerInteractor.Inventory.AddItems(itemDefinition, Mathf.Max(1, quantity));
        }
        Destroy(gameObject);
    }
}

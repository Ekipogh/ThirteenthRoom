using UnityEngine;

class ItemPickup : MonoBehaviour, IInteractable
{
    [Header("Item")]
    [SerializeField] ItemDefinition itemDefinition;
    public AudioClip PickupSound;
    public bool DestroyOrDisable = true;

    [Header("Scoring")]
    [SerializeField] ScoreManager scoreManager;

    public string GetInteractionPrompt(PlayerInteractor playerInteractor)
    {
        return $"Press E to pick up {itemDefinition.DisplayName}";
    }

    public void Interact(PlayerInteractor playerInteractor)
    {
        if (scoreManager != null)
        {
            scoreManager.AddScore(itemDefinition.ScoreOnPickup);
        }
        if (PickupSound != null)
        {
            playerInteractor.GetComponent<PlayerAudioManager>().PlayPickupSound(PickupSound);
        }
        playerInteractor.Inventory.AddItem(itemDefinition);
        if (DestroyOrDisable)
        {
            Destroy(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

}

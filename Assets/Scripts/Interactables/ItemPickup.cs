using UnityEngine;

class ItemPickup : MonoBehaviour, IInteractable
{
    [SerializeField] ItemDefinition itemDefinition;
    [SerializeField] ScoreManager scoreManager;
    public AudioClip PickupSound;
    public bool DestroyOrDisable = true;

    public string GetInteractionPrompt(PlayerInteractor playerInteractor)
    {
        return $"Press E to pick up {itemDefinition._displayName}";
    }

    public void Interact(PlayerInteractor playerInteractor)
    {
        if (scoreManager != null)
        {
            scoreManager.AddScore(itemDefinition._scoreOnPickup);
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
using UnityEngine;
using System.Collections;

class ItemPickup : MonoBehaviour, IInteractable
{
    [SerializeField] string itemName;
    [SerializeField] float scoreReward = 5f;
    [SerializeField] ScoreManager scoreManager;
    public AudioClip PickupSound;
    public bool DestroyOrDisable = true;

    public string GetInteractionPrompt(PlayerInteractor playerInteractor)
    {
        return $"Press E to pick up {itemName}";
    }

    public void Interact(PlayerInteractor playerInteractor)
    {
        if (scoreManager != null)
        {
            scoreManager.AddScore(scoreReward);
        }
        if (PickupSound != null)
        {
            playerInteractor.GetComponent<PlayerAudioManager>().PlayPickupSound(PickupSound);
        }
        playerInteractor.Inventory.AddItem(itemName);
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
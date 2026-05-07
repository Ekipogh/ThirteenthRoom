using UnityEngine;
using System.Collections;

class ItemPickup : MonoBehaviour, IInteractable
{
    [SerializeField] string itemName;
    [SerializeField] float scoreReward = 5f;
    [SerializeField] ScoreManger scoreManager;
    public AudioClip PickupSound;

    public string GetInteractionPrompt()
    {
        return $"pick up {itemName}";
    }

    public void Interact(PlayerInteractor playerInteractor)
    {
        scoreManager.AddScore(scoreReward);
        if (PickupSound != null)
        {
            playerInteractor.GetComponent<PlayerAudioManager>().PlayPickupSound(PickupSound);
        }
        Destroy(gameObject);
    }

}
using UnityEngine;

class ItemPickup : MonoBehaviour, IInteractable
{
    [SerializeField] string itemName;
    [SerializeField] float scoreReward = 5f;
    [SerializeField] ScoreManger scoreManager;

    public string GetInteractionPrompt()
    {
        return $"pick up {itemName}";
    }

    public void Interact(PlayerInteractor playerInteractor)
    {
        scoreManager.AddScore(scoreReward);
        Destroy(gameObject);
    }
}
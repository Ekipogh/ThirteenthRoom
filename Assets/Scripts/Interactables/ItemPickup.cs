using UnityEngine;

class ItemPickup : MonoBehaviour, IInteractable
{
    [SerializeField] string itemName;

    public string GetInteractionPrompt()
    {
        return $"pick up {itemName}";
    }

    public void Interact(PlayerInteractor playerInteractor)
    {
        Debug.Log($"Picked up {itemName}!");
        Destroy(gameObject);
    }
}
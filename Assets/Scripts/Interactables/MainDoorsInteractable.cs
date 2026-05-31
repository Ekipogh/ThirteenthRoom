using UnityEngine;

public class MainDoorsInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private AudioSource lockedSound;
    public string GetInteractionPrompt(PlayerInteractor playerInteractor)
    {
        return "The main doors are locked. You need to find another way out.";
    }

    public void Interact(PlayerInteractor playerInteractor)
    {
        if (lockedSound != null)
        {
            lockedSound.Play();
        }
    }
}

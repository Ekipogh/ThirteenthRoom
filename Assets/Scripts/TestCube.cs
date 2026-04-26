using UnityEngine;

public class TestCube : MonoBehaviour, IInteractable
{
    public string GetInteractionPrompt()
    {
        return "Press E to interact with the cube.";
    }

    public void Interact(PlayerInteractor playerInteractor)
    {
        Debug.Log("Cube interacted with!");
    }
}

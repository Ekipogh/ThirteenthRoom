using UnityEngine;

[RequireComponent(typeof(Collider))]
public class SafeInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] Transform safeDoor;
    [SerializeField] float openAngle = -90f;
    [SerializeField] float openSpeed = 120f;
    bool _isOpening;
    Collider _collider;

    void Awake()
    {
        _collider = GetComponent<Collider>();
    }

    public string GetInteractionPrompt(PlayerInteractor playerInteractor)
    {
        bool hasKey = playerInteractor.Inventory.HasItem("Safe Key");
        return hasKey ? "Open the safe" : "The safe is locked";
    }

    public void Interact(PlayerInteractor playerInteractor)
    {
        bool hasKey = playerInteractor.Inventory.HasItem("Safe Key");
        if (hasKey)
        {
            // Implement safe opening logic here
            if (safeDoor != null)
            {
                _isOpening = true;
            }
        }
    }

    void Update()
    {
        if (_isOpening && safeDoor != null)
        {
            Vector3 angles = safeDoor.localEulerAngles;
            angles.y = Mathf.MoveTowardsAngle(angles.y, openAngle, openSpeed * Time.deltaTime);
            safeDoor.localEulerAngles = angles;

            if (Mathf.Abs(Mathf.DeltaAngle(angles.y, openAngle)) < 0.1f)
            {
                _isOpening = false;
                _collider.enabled = false; // Disable collider to prevent further interaction
            }
        }
    }
}

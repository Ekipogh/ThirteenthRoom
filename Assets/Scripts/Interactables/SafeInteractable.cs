using UnityEngine;

[RequireComponent(typeof(Collider))]
public class SafeInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] Transform safeDoor;
    [SerializeField] float openAngle = -90f;
    [SerializeField] float openSpeed = 120f;
    [SerializeField] AudioClip openSound;
    bool _isOpening;
    bool _isOpened;
    float _closedDoorLocalY;
    float _targetDoorLocalY;
    Collider _collider;
    [SerializeField] ItemDefinition safeKeyItem;

    void Awake()
    {
        _collider = GetComponent<Collider>();
        if (safeDoor != null)
        {
            _closedDoorLocalY = safeDoor.localEulerAngles.y;
            _targetDoorLocalY = _closedDoorLocalY;
        }
    }

    public string GetInteractionPrompt(PlayerInteractor playerInteractor)
    {
        bool hasKey = playerInteractor.Inventory.HasItem(safeKeyItem);
        return hasKey ? "Open the safe" : "The safe is locked";
    }

    public void Interact(PlayerInteractor playerInteractor)
    {
        bool hasKey = playerInteractor.Inventory.HasItem(safeKeyItem);
        if (!hasKey || safeDoor == null || _isOpening || _isOpened)
        {
            return;
        }

        _targetDoorLocalY = _closedDoorLocalY + openAngle;
        _isOpening = true;
        if (openSound != null)
        {
            AudioSource.PlayClipAtPoint(openSound, transform.position);
        }
    }

    void Update()
    {
        if (_isOpening && safeDoor != null)
        {
            Vector3 angles = safeDoor.localEulerAngles;
            angles.y = Mathf.MoveTowardsAngle(angles.y, _targetDoorLocalY, openSpeed * Time.deltaTime);
            safeDoor.localEulerAngles = angles;

            if (Mathf.Abs(Mathf.DeltaAngle(angles.y, _targetDoorLocalY)) < 0.1f)
            {
                _isOpening = false;
                _isOpened = true;
                _collider.enabled = false; // Disable collider to prevent further interaction
            }
        }
    }
}

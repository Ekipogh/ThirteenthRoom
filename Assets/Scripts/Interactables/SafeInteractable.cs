using UnityEngine;

[RequireComponent(typeof(Collider))]
public class SafeInteractable : MonoBehaviour, IInteractable
{
    [Header("Door")]
    [SerializeField] Transform safeDoor;
    [SerializeField] float openAngle = -90f;
    [SerializeField] float openSpeed = 120f;

    [Header("Required Item")]
    [SerializeField] SpawnItemDefinition safeKeyItem;
    private string requiredItemId;

    [Header("Audio")]
    [SerializeField] AudioClip openSound;

    bool _isOpening;
    bool _isOpened;
    float _closedDoorLocalY;
    float _targetDoorLocalY;
    Collider _collider;

    void Awake()
    {
        _collider = GetComponent<Collider>();
        if (safeDoor != null)
        {
            _closedDoorLocalY = safeDoor.localEulerAngles.y;
            _targetDoorLocalY = _closedDoorLocalY;
        }
        RequestKey();
    }

    private void RequestKey()
    {
        if (safeKeyItem == null)
        {
            Debug.LogWarning("SafeInteractable is missing a safe key item definition.");
            return;
        }

        Room room = GetComponentInParent<Room>();
        if (!room)
        {
            Debug.LogWarning("SafeInteractable is not inside a Room. Cannot request key item.");
            return;
        }
        if (room != null)
        {
            var randomSuffix = RandomString.GenerateRandomString(6);
            requiredItemId = room.name + "_SafeKey_" + randomSuffix;
            safeKeyItem = Instantiate(safeKeyItem);
            safeKeyItem.targetID = requiredItemId;
            Debug.Log($"SafeInteractable in room '{room.name}' is requesting key item with ID: {requiredItemId}");
            room.RequestItem(safeKeyItem);
        }
    }

    public string GetInteractionPrompt(PlayerInteractor playerInteractor)
    {
        bool hasKey = playerInteractor.Inventory.HasInstanceItem(safeKeyItem.ItemDefinition, requiredItemId);
        return hasKey ? "Open the safe" : "The safe is locked";
    }

    public void Interact(PlayerInteractor playerInteractor)
    {
        bool hasKey = playerInteractor.Inventory.HasInstanceItem(safeKeyItem.ItemDefinition, requiredItemId);
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

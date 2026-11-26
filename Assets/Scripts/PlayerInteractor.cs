using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteractor : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] float interactionRange = 3f;
    [SerializeField] LayerMask interactableLayerMask = ~0; // Default to everything
    [SerializeField] Transform headTransform;
    [SerializeField] PlayerInput playerInput;
    [SerializeField] string interactActionName = "Interact";

    [Header("UI")]
    [SerializeField] TMPro.TextMeshProUGUI interactionPrompt;

    IInteractable _currentInteractableTarget;
    IHoldInteractable _currentHoldTarget;
    IHoldInteractable _activeHoldTarget;
    bool _isInteractPressed;
    InputAction _interactAction;

    private PlayerInventory _playerInventory;
    public PlayerInventory Inventory => _playerInventory;
    bool _inventoryInputBlocked;
    PlayerItemController _items;
    bool InputBlocked => _inventoryInputBlocked || (_items != null && !_items.GameplayActive);
    public void SetInventoryInputBlocked(bool blocked)
    {
        _inventoryInputBlocked = blocked;
        if (!blocked && !InputBlocked) return;
        _isInteractPressed = false;
        EndHeldInteraction();
        _currentInteractableTarget = null;
        _currentHoldTarget = null;
        if (interactionPrompt != null) interactionPrompt.gameObject.SetActive(false);
    }

    void Awake()
    {
        if (headTransform == null)
        {
            headTransform = transform.Find("HeadJoint");
        }

        if (playerInput == null)
        {
            playerInput = GetComponent<PlayerInput>();
        }

        if (playerInput != null && playerInput.actions != null)
        {
            _interactAction = playerInput.actions.FindAction(interactActionName, throwIfNotFound: false);
        }

        _playerInventory = new PlayerInventory();
        _items = GetComponent<PlayerItemController>();
    }

    void Update()
    {
        if (InputBlocked) { SetInventoryInputBlocked(_inventoryInputBlocked); return; }
        CheckForInteractables();
        RefreshInteractButtonState();
        UpdateHeldInteraction();
    }

    void OnInteract(InputValue value)
    {
        if (InputBlocked) { _isInteractPressed = false; EndHeldInteraction(); return; }
        _isInteractPressed = value.isPressed;

        if (!_isInteractPressed)
        {
            EndHeldInteraction();
            return;
        }

        // Input callbacks can run before Update; validate visibility at the moment of use.
        CheckForInteractables();

        if (_currentHoldTarget != null)
        {
            BeginHeldInteraction(_currentHoldTarget);
            return;
        }

        _currentInteractableTarget?.Interact(this);

        // One-shot interactions should not depend on a release callback from the input action.
        _isInteractPressed = false;
    }

    void RefreshInteractButtonState()
    {
        if (_interactAction == null)
        {
            return;
        }

        if (_isInteractPressed && !_interactAction.IsPressed())
        {
            _isInteractPressed = false;
            EndHeldInteraction();
        }
    }

    void UpdateHeldInteraction()
    {
        if (!_isInteractPressed || _activeHoldTarget == null)
        {
            return;
        }

        if (_currentHoldTarget != _activeHoldTarget)
        {
            EndHeldInteraction();
            _isInteractPressed = false;
            return;
        }

        _activeHoldTarget.HoldInteract(this, Time.deltaTime);
    }

    void BeginHeldInteraction(IHoldInteractable holdTarget)
    {
        if (_activeHoldTarget == holdTarget)
        {
            return;
        }

        EndHeldInteraction();
        _activeHoldTarget = holdTarget;
        _activeHoldTarget.BeginHoldInteract(this);
    }

    void EndHeldInteraction()
    {
        if (_activeHoldTarget == null)
        {
            return;
        }

        _activeHoldTarget.EndHoldInteract(this);
        _activeHoldTarget = null;
    }

    void CheckForInteractables()
    {
        Ray ray = new(headTransform.position, headTransform.forward);
        Debug.DrawRay(ray.origin, ray.direction * interactionRange, Color.green);
        if (Physics.Raycast(ray, out RaycastHit hit, interactionRange, interactableLayerMask, QueryTriggerInteraction.Collide) &&
            !IsInteractionObstructed(ray, hit))
        {
            if (GetInteractionOwner(hit.collider) is IHoldInteractable holdInteractable)
            {
                _currentHoldTarget = holdInteractable;
                _currentInteractableTarget = null;
                interactionPrompt.text = FormatInteractionPrompt(holdInteractable.GetInteractionPrompt(this));
                interactionPrompt.gameObject.SetActive(true);
                return;
            }

            if (GetInteractionOwner(hit.collider) is IInteractable interactable)
            {
                _currentHoldTarget = null;
                _currentInteractableTarget = interactable;
                interactionPrompt.text = FormatInteractionPrompt(interactable.GetInteractionPrompt(this));
                interactionPrompt.gameObject.SetActive(true);
                return;
            }
        }

        _currentHoldTarget = null;
        _currentInteractableTarget = null;
        interactionPrompt.gameObject.SetActive(false);
    }

    static bool IsInteractionObstructed(Ray ray, RaycastHit targetHit)
    {
        var targetOwner = GetInteractionOwner(targetHit.collider);
        foreach (RaycastHit obstruction in Physics.RaycastAll(ray, targetHit.distance,
                     ~0, QueryTriggerInteraction.Ignore))
        {
            var collider = obstruction.collider;
            var owner = GetInteractionOwner(collider);
            if (collider == targetHit.collider || (targetOwner != null && owner == targetOwner))
                continue;

            // Safes deliberately disable their interaction collider when opened.
            // Their physical hull can still enclose the visible contents.
            if (owner is SafeInteractable safe)
            {
                if (safe.GetComponent<Collider>().enabled)
                    return true;
                continue;
            }

            if (owner is DoorInteractable || owner is MainDoorsInteractable ||
                collider.GetComponentInParent<InteractionBlocker>() != null)
                return true;
        }

        return false;
    }
    static Component GetInteractionOwner(Collider collider)
    {
        var holdTarget = collider.GetComponentInParent<IHoldInteractable>();
        if (holdTarget is Component holdComponent)
            return holdComponent;

        return collider.GetComponentInParent<IInteractable>() as Component;
    }
    static string FormatInteractionPrompt(string interactionAction)
    {
        if (string.IsNullOrWhiteSpace(interactionAction))
        {
            return "Press E to interact.";
        }

        string normalizedAction = interactionAction.Trim().TrimEnd('.');
        return $"{normalizedAction}.";
    }
}

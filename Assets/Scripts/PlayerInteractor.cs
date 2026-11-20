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
    }

    void Update()
    {
        CheckForInteractables();
        RefreshInteractButtonState();
        UpdateHeldInteraction();
    }

    void OnInteract(InputValue value)
    {
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
        if (Physics.Raycast(ray, out RaycastHit hit, interactionRange, interactableLayerMask) &&
            !IsInteractionObstructed(ray, hit))
        {
            if (hit.collider.TryGetComponent<IHoldInteractable>(out var holdInteractable))
            {
                _currentHoldTarget = holdInteractable;
                _currentInteractableTarget = null;
                interactionPrompt.text = FormatInteractionPrompt(holdInteractable.GetInteractionPrompt(this));
                interactionPrompt.gameObject.SetActive(true);
                return;
            }

            if (hit.collider.TryGetComponent<IInteractable>(out var interactable))
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
        // A door's interaction trigger and solid child mesh belong to the same
        // object. Ignore its own geometry, but still check every separate blocker.
        foreach (RaycastHit obstruction in Physics.RaycastAll(ray, targetHit.distance,
                     ~0, QueryTriggerInteraction.Ignore))
        {
            if (obstruction.collider == targetHit.collider)
                continue;

            if (targetOwner != null && GetInteractionOwner(obstruction.collider) == targetOwner)
                continue;

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

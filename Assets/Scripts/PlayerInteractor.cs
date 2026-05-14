using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteractor : MonoBehaviour
{
    [SerializeField] float interactionRange = 3f;
    [SerializeField] LayerMask interactableLayerMask = ~0; // Default to everything
    [SerializeField] Transform headTransform;
    IInteractable _currentInteractableTarget;
    IHoldInteractable _currentHoldTarget;
    IHoldInteractable _activeHoldTarget;
    bool _isInteractPressed;

    [SerializeField] TMPro.TextMeshProUGUI interactionPrompt;

    private PlayerInventory _playerInventory;
    public PlayerInventory Inventory => _playerInventory;

    void Awake()
    {
        if (headTransform == null)
        {
            headTransform = transform.Find("HeadJoint");
        }
        _playerInventory = new PlayerInventory();
    }

    void Update()
    {
        CheckForInteractables();
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

        if (_currentHoldTarget != null)
        {
            BeginHeldInteraction(_currentHoldTarget);
            return;
        }

        _currentInteractableTarget?.Interact(this);

        // One-shot interactions should not depend on a release callback from the input action.
        _isInteractPressed = false;
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
        if (Physics.Raycast(ray, out RaycastHit hit, interactionRange, interactableLayerMask))
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

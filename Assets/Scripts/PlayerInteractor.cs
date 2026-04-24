using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteractor : MonoBehaviour
{
    [SerializeField] float interactionRange = 3f;
    [SerializeField] LayerMask interactableLayerMask;
    [SerializeField] Transform headTransform;
    IInteractable _currentTarget;

    void Awake()
    {
        if (headTransform == null)
        {
            headTransform = transform.Find("HeadJoint");
        }
    }

    void Update()
    {
        CheckForInteractables();
    }

    void OnInteract(InputValue value)
    {
        if (value.isPressed && _currentTarget != null)
        {
            _currentTarget.Interact(this);
        }
    }

    void CheckForInteractables()
    {
        Ray ray = new(headTransform.position, headTransform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, interactionRange, interactableLayerMask))
        {
            if (hit.collider.TryGetComponent<IInteractable>(out var interactable))
            {
                _currentTarget = interactable;
                // Optionally show interaction prompt here using _currentTarget.GetInteractionPrompt()
                return;
            }
        }
        _currentTarget = null;
        // Optionally hide interaction prompt here
    }
}

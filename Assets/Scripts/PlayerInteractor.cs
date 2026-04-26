using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteractor : MonoBehaviour
{
    [SerializeField] float interactionRange = 3f;
    [SerializeField] LayerMask interactableLayerMask = ~0; // Default to everything
    [SerializeField] Transform headTransform;
    IInteractable _currentTarget;

    [SerializeField] TMPro.TextMeshProUGUI interactionPrompt;

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
                interactionPrompt.text = interactable.GetInteractionPrompt();
                interactionPrompt.gameObject.SetActive(true);
                return;
            }
        }
        _currentTarget = null;
        interactionPrompt.gameObject.SetActive(false);
    }
}

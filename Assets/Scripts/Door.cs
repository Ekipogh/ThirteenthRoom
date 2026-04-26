using UnityEngine;

public class Door : MonoBehaviour, IInteractable
{
    bool _isOpening = false;
    bool _isOpen = false;
    float _openingTime = 1f; // in seconds
    Vector3 _playerLastPosition;
    public string GetInteractionPrompt()
    {
        return _isOpen ? "Press E to close the door." : "Press E to open the door.";
    }

    public void Interact(PlayerInteractor playerInteractor)
    {
        _playerLastPosition = playerInteractor.transform.position;
        _isOpening = ! _isOpening;
    }

    void Update()
    {
        if (_isOpening)
        {
            transform.Rotate(Vector3.up, 90f / _openingTime * Time.deltaTime * (_isOpen ? -1 : 1));
            if (Mathf.Abs(transform.localEulerAngles.y) > 90f)
            {
                Vector3 angles = transform.localEulerAngles;
                angles.y = _isOpen ? 0 : 90f;
                transform.localEulerAngles = angles;
                _isOpen = !_isOpen;
                _isOpening = false;
            }
        }
    }
}

using UnityEngine;

public class DoorInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] float openingTime = 1f;
    const float OpenAngle = 90f;
    const float AngleEpsilon = 0.1f;

    bool _isMoving;
    float _closedLocalY;
    float _targetRelativeAngle;
    Vector3 _doorLocalCenter;

    public string GetInteractionPrompt()
    {
        return IsOpen() ? "close the door" : "open the door";
    }

    public void Interact(PlayerInteractor playerInteractor)
    {
        if (_isMoving)
        {
            _targetRelativeAngle = Mathf.Approximately(_targetRelativeAngle, 0f)
                ? ComputeOpenTarget(playerInteractor.transform.position)
                : 0f;
            return;
        }

        if (IsOpen())
        {
            _targetRelativeAngle = 0f;
        }
        else
        {
            _targetRelativeAngle = ComputeOpenTarget(playerInteractor.transform.position);
        }

        _isMoving = true;
    }

    void Awake()
    {
        _closedLocalY = transform.localEulerAngles.y;
        _doorLocalCenter = CalculateDoorLocalCenter();
    }

    void Update()
    {
        if (_isMoving)
        {
            float currentRelative = GetCurrentRelativeAngle();
            float speed = OpenAngle / Mathf.Max(openingTime, 0.01f);
            float nextRelative = Mathf.MoveTowards(currentRelative, _targetRelativeAngle, speed * Time.deltaTime);

            Vector3 angles = transform.localEulerAngles;
            angles.y = _closedLocalY + nextRelative;
            transform.localEulerAngles = angles;

            if (Mathf.Abs(nextRelative - _targetRelativeAngle) <= AngleEpsilon)
            {
                angles.y = _closedLocalY + _targetRelativeAngle;
                transform.localEulerAngles = angles;
                _isMoving = false;
            }
        }
    }

    float GetCurrentRelativeAngle()
    {
        return Mathf.DeltaAngle(_closedLocalY, transform.localEulerAngles.y);
    }

    bool IsOpen()
    {
        return Mathf.Abs(GetCurrentRelativeAngle()) > OpenAngle * 0.5f;
    }

    float ComputeOpenTarget(Vector3 playerPosition)
    {
        Vector3 centerWorld = transform.TransformPoint(_doorLocalCenter);
        Vector3 toPlayer = playerPosition - centerWorld;
        Vector3 hingeToCenter = centerWorld - transform.position;

        // Choose the rotation sign whose initial motion at door center moves away from the player.
        Vector3 plusVelocity = Vector3.Cross(transform.up, hingeToCenter);
        float scorePlus = Vector3.Dot(plusVelocity, toPlayer);
        if (Mathf.Approximately(scorePlus, 0f))
        {
            return _doorLocalCenter.x >= 0f ? OpenAngle : -OpenAngle;
        }

        return scorePlus < 0f ? OpenAngle : -OpenAngle;
    }

    Vector3 CalculateDoorLocalCenter()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            return Vector3.right * 0.5f;
        }

        bool hasBounds = false;
        Bounds localBounds = new(Vector3.zero, Vector3.zero);

        foreach (Renderer renderer in renderers)
        {
            Bounds worldBounds = renderer.bounds;
            Vector3 min = worldBounds.min;
            Vector3 max = worldBounds.max;

            Vector3[] corners =
            {
                new(min.x, min.y, min.z),
                new(min.x, min.y, max.z),
                new(min.x, max.y, min.z),
                new(min.x, max.y, max.z),
                new(max.x, min.y, min.z),
                new(max.x, min.y, max.z),
                new(max.x, max.y, min.z),
                new(max.x, max.y, max.z)
            };

            foreach (Vector3 corner in corners)
            {
                Vector3 localPoint = transform.InverseTransformPoint(corner);
                if (!hasBounds)
                {
                    localBounds = new Bounds(localPoint, Vector3.zero);
                    hasBounds = true;
                }
                else
                {
                    localBounds.Encapsulate(localPoint);
                }
            }
        }

        return hasBounds ? localBounds.center : Vector3.right * 0.5f;
    }
}

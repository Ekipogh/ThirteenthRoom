using UnityEngine;

/// <summary>Purely visual motion around an authored, local-space pose.</summary>
[DisallowMultipleComponent]
public sealed class FloatingPropVisual : MonoBehaviour
{
    [SerializeField, Min(0f)] float amplitude = 0.25f;
    [SerializeField, Min(0.1f)] float period = 6f;
    [SerializeField] float rotationSpeed = 6f;

    Vector3 restPosition;
    Quaternion restRotation;
    float startTime;
    bool captured;

    void OnEnable()
    {
        if (!captured)
        {
            restPosition = transform.localPosition;
            restRotation = transform.localRotation;
            captured = true;
        }
        startTime = Time.time;
    }

    void LateUpdate()
    {
        float elapsed = Time.time - startTime;
        transform.localPosition = restPosition + Vector3.up *
            (Mathf.Sin(elapsed * Mathf.PI * 2f / Mathf.Max(0.1f, period)) * amplitude);
        transform.localRotation = restRotation * Quaternion.AngleAxis(elapsed * rotationSpeed, Vector3.up);
    }

    void OnDisable()
    {
        if (!captured) return;
        transform.localPosition = restPosition;
        transform.localRotation = restRotation;
    }
}

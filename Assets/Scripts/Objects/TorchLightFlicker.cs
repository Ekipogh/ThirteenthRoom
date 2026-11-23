using UnityEngine;

/// <summary>Bounded, smooth firelight variation; independent of electrical power and switches.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Light))]
public sealed class TorchLightFlicker : MonoBehaviour
{
    [SerializeField, Range(0f, 0.4f)] float intensityVariation = 0.18f;
    [SerializeField, Min(0.1f)] float flickerSpeed = 4f;
    [SerializeField] float phaseOffset;

    Light _light;
    float _baseIntensity;

    void Awake()
    {
        _light = GetComponent<Light>();
        _baseIntensity = _light.intensity;
    }

    void Update()
    {
        // Two smooth frequencies avoid a repeating sine pulse or harsh random flashes.
        float time = Time.time * flickerSpeed;
        float slow = Mathf.PerlinNoise(phaseOffset, time);
        float flutter = Mathf.PerlinNoise(phaseOffset + 37.1f, time * 3.1f);
        float variation = (slow * 0.7f + flutter * 0.3f) * 2f - 1f;
        _light.intensity = _baseIntensity * (1f + variation * intensityVariation);
    }

    void OnDisable()
    {
        if (_light != null) _light.intensity = _baseIntensity;
    }
}

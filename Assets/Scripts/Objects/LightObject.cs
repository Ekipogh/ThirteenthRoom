using UnityEngine;

public class LightObject : MonoBehaviour
{
    static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    [Header("Objects")]
    [SerializeField] GameObject lights;
    [SerializeField] GameObject model;

    [Header("Emission")]
    [SerializeField] Color onEmissionColor = Color.white;
    [SerializeField] Color offEmissionColor = Color.black;

    [Header("State")]
    public bool IsOn = true;
    public bool IsPowered = true;

    Renderer _modelRenderer;
    MaterialPropertyBlock _propertyBlock;

    void Awake()
    {
        if (model != null && !model.TryGetComponent(out _modelRenderer))
        {
            _modelRenderer = model.GetComponentInChildren<Renderer>(true);
        }

        ApplyLightState();
    }

    public void ToggleLight(bool isOn)
    {
        IsOn = isOn;

        ApplyLightState();
    }

    public void SetPower(bool isPowered)
    {
        IsPowered = isPowered;

        ApplyLightState();
    }

    void ApplyLightState()
    {
        bool lightIsOn = IsPowered && IsOn;

        if (lights != null && lights.activeSelf != lightIsOn)
        {
            lights.SetActive(lightIsOn);
        }

        if (_modelRenderer != null)
        {
            _propertyBlock ??= new MaterialPropertyBlock();
            _modelRenderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(EmissionColorId, lightIsOn ? onEmissionColor : offEmissionColor);
            _modelRenderer.SetPropertyBlock(_propertyBlock);
        }
    }
}

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

    Renderer[] _modelRenderers;
    MaterialPropertyBlock _propertyBlock;

    void Awake()
    {
        _modelRenderers = model != null ? model.GetComponentsInChildren<Renderer>(true) : System.Array.Empty<Renderer>();

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

        foreach (Renderer modelRenderer in _modelRenderers ?? System.Array.Empty<Renderer>())
        {
            _propertyBlock ??= new MaterialPropertyBlock();
            modelRenderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(EmissionColorId, lightIsOn ? onEmissionColor : offEmissionColor);
            modelRenderer.SetPropertyBlock(_propertyBlock);
        }
    }
}

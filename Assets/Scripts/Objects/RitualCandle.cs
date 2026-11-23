using System.Collections.Generic;
using UnityEngine;

public class RitualCandle : MonoBehaviour
{
    [Header("Visuals")]
    [SerializeField] List<GameObject> pointLights;
    [SerializeField] List<GameObject> flameParticles;

    static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
    MaterialPropertyBlock _emissionBlock;

    void Awake()
    {
        SetEmission(pointLights != null && pointLights.Exists(light => light != null && light.activeSelf));
    }

    void SetEmission(bool lit)
    {
        _emissionBlock ??= new MaterialPropertyBlock();
        foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
        {
            if (renderer is ParticleSystemRenderer) continue;
            renderer.GetPropertyBlock(_emissionBlock);
            _emissionBlock.SetColor(EmissionColorId, lit ? Color.white : Color.black);
            renderer.SetPropertyBlock(_emissionBlock);
        }
    }

    public void LightCandle()
    {
        SetEmission(true);
        foreach (GameObject light in pointLights)
        {
            light.SetActive(true);
        }

        foreach (GameObject flame in flameParticles)
        {
            flame.SetActive(true);
        }
    }

    public void ExtinguishCandle()
    {
        SetEmission(false);
        foreach (GameObject light in pointLights)
        {
            light.SetActive(false);
        }

        foreach (GameObject flame in flameParticles)
        {
            flame.SetActive(false);
        }
    }
}

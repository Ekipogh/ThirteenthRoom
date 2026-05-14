using System.Collections.Generic;
using UnityEngine;

public class RitualCandle : MonoBehaviour
{
    [SerializeField] List<GameObject> pointLights;
    [SerializeField] List<GameObject> flameParticles;

    public void LightCandle()
    {
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

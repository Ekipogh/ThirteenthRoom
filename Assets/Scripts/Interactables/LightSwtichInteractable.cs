using UnityEngine;

public class LightSwtichInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] GameObject CeilingPointLight;
    [SerializeField] GameObject CeilingLightModel;
    [SerializeField] Transform LightSwitchTransform;
    [SerializeField] AudioSource OnSound;
    [SerializeField] AudioSource OffSound;

    bool isActive = false;
    const float _switchOffAngle = -60f;

    void Awake()
    {
        SwitchLight(isActive);
    }
    public string GetInteractionPrompt(PlayerInteractor playerInteractor)
    {
        return "Press E to toggle the light switch";
    }

    public void Interact(PlayerInteractor playerInteractor)
    {
        if (CeilingPointLight != null)
        {
            isActive = CeilingPointLight.activeSelf;
            CeilingPointLight.SetActive(!isActive);
            PlaySound(isActive);
        }
        // Turn off emmision of ceiling light when the switch is off
        SwitchLight(!isActive);
    }

    void PlaySound(bool isTurningOff)
    {
        AudioSource sourceToPlay = isTurningOff ? OffSound : OnSound;
        if (sourceToPlay != null)
        {
            sourceToPlay.PlayOneShot(sourceToPlay.clip);
        }
    }

    void SwitchLight(bool turnOn)
    {
        if (CeilingPointLight != null)
        {
            CeilingPointLight.SetActive(turnOn);
        }
        if (CeilingLightModel != null)
        {
            var emission = CeilingLightModel.GetComponent<Renderer>().material.GetColor("_EmissionColor");
            if (turnOn)
            {
                emission = Color.white; // Restore emission (you can adjust the color as needed)
            }
            else
            {
                emission = Color.black; // Turn off emission
            }
            CeilingLightModel.GetComponent<Renderer>().material.SetColor("_EmissionColor", emission);
        }
        // Rotate the light switch model to indicate on/off state
        if (LightSwitchTransform != null)
        {
            float targetAngle = turnOn ? 0f : _switchOffAngle;
            Vector3 angles = LightSwitchTransform.localEulerAngles;
            angles.x = targetAngle;
            LightSwitchTransform.localEulerAngles = angles;
        }
    }
}

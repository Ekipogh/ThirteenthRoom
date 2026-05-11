using UnityEngine;

public class LightSwitchInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] GameObject CeilingPointLight;
    [SerializeField] GameObject CeilingLightModel;
    [SerializeField] Transform LightSwitchTransform;
    [SerializeField] AudioSource OnSound;
    [SerializeField] AudioSource OffSound;

    bool isActive = false;
    const float _switchOffAngle = -60f;

    public bool IsPowered = true; // Indicates whether the switch is powered and can be interacted with

    void Awake()
    {
        ApplyLightState();
    }
    public string GetInteractionPrompt(PlayerInteractor playerInteractor)
    {
        return "Press E to toggle the light switch";
    }

    public void Interact(PlayerInteractor playerInteractor)
    {
        PlaySound(isActive);
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

    public void SwitchLight(bool turnOn)
    {
        isActive = turnOn;
        ApplyLightState();
    }

    public void SetPower(bool powered)
    {
        IsPowered = powered;
        ApplyLightState();
    }

    void ApplyLightState()
    {
        if (CeilingPointLight != null)
        {
            CeilingPointLight.SetActive(IsPowered && isActive);
        }
        if (CeilingLightModel != null)
        {
            var emission = CeilingLightModel.GetComponent<Renderer>().material.GetColor("_EmissionColor");
            if (IsPowered && isActive)
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
            float targetAngle = isActive ? 0f : _switchOffAngle;
            Vector3 angles = LightSwitchTransform.localEulerAngles;
            angles.x = targetAngle;
            LightSwitchTransform.localEulerAngles = angles;
        }
    }
}

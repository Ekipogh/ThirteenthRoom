using UnityEngine;

public class LightSwitchInteractable : MonoBehaviour, IInteractable
{
    [Header("Lights")]
    [SerializeField] LightObject[] LightObjects;

    [Header("Switch")]
    [SerializeField] Transform LightSwitchTransform;

    [Header("Audio")]
    [SerializeField] AudioSource OnSound;
    [SerializeField] AudioSource OffSound;

    bool isActive = false;
    const float _switchOffAngle = -60f;

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
        if (sourceToPlay != null && sourceToPlay.clip != null)
        {
            sourceToPlay.PlayOneShot(sourceToPlay.clip);
        }
    }

    public void SwitchLight(bool turnOn)
    {
        isActive = turnOn;
        ApplyLightState();
    }

    void ApplyLightState()
    {
        if (LightObjects != null)
        {
            foreach (LightObject lightObject in LightObjects)
            {
                if (lightObject != null)
                {
                    lightObject.ToggleLight(isActive);
                }
            }
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

using UnityEngine;

public class DoorAudioManager : MonoBehaviour
{
    [SerializeField] AudioSource DoorOpenAudioSource;
    [SerializeField] AudioSource DoorCloseAudioSource;

    public void PlayDoorOpenSound()
    {
        if (DoorOpenAudioSource != null)
        {
            DoorOpenAudioSource.Play();
        }
    }

    public void PlayDoorCloseSound()
    {
        if (DoorCloseAudioSource != null)
        {
            DoorCloseAudioSource.Play();
        }
    }
}

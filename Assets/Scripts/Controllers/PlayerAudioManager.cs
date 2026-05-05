using UnityEngine;

public class PlayerAudioManager : MonoBehaviour
{
    [SerializeField] AudioSource NormalBreathingAudioSource;
    [SerializeField] AudioSource SprintingBreathingAudioSource;
    [SerializeField] AudioSource GaspAudioSource;

    public void SetSprinting(bool isSprinting)
    {
        if (isSprinting)
        {
            NormalBreathingAudioSource.Pause();
            if (SprintingBreathingAudioSource.isPlaying == false)
            {
                SprintingBreathingAudioSource.Play();
            }
        }
        else
        {
            SprintingBreathingAudioSource.Pause();
            if (NormalBreathingAudioSource.isPlaying == false)
            {
                NormalBreathingAudioSource.Play();
            }
        }
    }

    public void PlayGasp()
    {
        GaspAudioSource.PlayOneShot(GaspAudioSource.clip);
    }
}

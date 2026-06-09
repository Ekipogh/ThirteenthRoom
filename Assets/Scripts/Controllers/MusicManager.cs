using UnityEngine;

public class MusicManager : MonoBehaviour
{
    [Header("Audio Source")]
    [SerializeField] AudioSource musicSource;

    [Header("Clips")]
    [SerializeField] AudioClip normalMusic;
    [SerializeField] AudioClip intenseMusic;

    private void Start()
    {
        PlayNormalMusic();
    }

    public void PlayNormalMusic()
    {
        PlayMusic(normalMusic);
    }

    public void PlayIntenseMusic()
    {
        PlayMusic(intenseMusic);
    }

    private void PlayMusic(AudioClip clip)
    {
        if (musicSource == null || clip == null)
        {
            return;
        }

        if (musicSource.clip == clip && musicSource.isPlaying)
        {
            return;
        }

        musicSource.clip = clip;
        musicSource.Play();
    }
}

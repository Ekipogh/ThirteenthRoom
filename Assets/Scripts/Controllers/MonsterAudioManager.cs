using System.Collections.Generic;
using UnityEngine;

public class MonsterAudioManager : MonoBehaviour
{
    [Header("Footsteps")]
    [SerializeField] GameObject FootStepSourceObject;
    [SerializeField] float footstepDistanceThreshold = 2.5f;
    [SerializeField] GameObject AlertSoundsObject;

    List<AudioSource> _footstepAudioSources;
    List<AudioSource> _alertSoundSources;
    float _distanceTraveled;

    void Awake()
    {
        _footstepAudioSources = new List<AudioSource>();
        if (FootStepSourceObject != null)
        {
            _footstepAudioSources.AddRange(FootStepSourceObject.GetComponentsInChildren<AudioSource>());
        }
        _alertSoundSources = new List<AudioSource>();
        if (AlertSoundsObject != null)
        {
            _alertSoundSources.AddRange(AlertSoundsObject.GetComponentsInChildren<AudioSource>());
        }
    }

    public void PlayFootstepForDistance(float distance)
    {
        if (distance <= 0f)
        {
            return;
        }

        _distanceTraveled += distance;
        if (_distanceTraveled < footstepDistanceThreshold)
        {
            return;
        }

        _distanceTraveled = 0f;
        PlayRandomFootstepSound();
    }

    public void ResetFootstepDistance()
    {
        _distanceTraveled = 0f;
    }

    void PlayRandomFootstepSound()
    {
        if (_footstepAudioSources.Count == 0)
        {
            return;
        }

        int index = Random.Range(0, _footstepAudioSources.Count);
        AudioSource footstepSource = _footstepAudioSources[index];
        if (footstepSource.clip != null)
        {
            footstepSource.PlayOneShot(footstepSource.clip);
        }
    }

    public void PlayAlertSound()
    {
        if (_alertSoundSources.Count == 0)
        {
            return;
        }
        int index = Random.Range(0, _alertSoundSources.Count);
        AudioSource alertSource = _alertSoundSources[index];
        if (alertSource.clip != null)
        {
            alertSource.PlayOneShot(alertSource.clip);
        }
    }
}

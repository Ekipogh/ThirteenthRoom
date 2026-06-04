using System.Collections.Generic;
using UnityEngine;

public class MonsterAudioManager : MonoBehaviour
{
    [SerializeField] GameObject FootStepSourceObject;
    [SerializeField] float footstepDistanceThreshold = 2.5f;

    List<AudioSource> _footstepAudioSources;
    float _distanceTraveled;

    void Awake()
    {
        _footstepAudioSources = new List<AudioSource>();
        if (FootStepSourceObject != null)
        {
            _footstepAudioSources.AddRange(FootStepSourceObject.GetComponentsInChildren<AudioSource>());
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
}

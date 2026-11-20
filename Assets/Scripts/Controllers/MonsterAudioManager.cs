using System.Collections.Generic;
using UnityEngine;

public class MonsterAudioManager : MonoBehaviour
{
    [Header("Footsteps")]
    [SerializeField] GameObject FootStepSourceObject;
    [SerializeField] float footstepDistanceThreshold = 2.5f;
    [Header("Vocals")]
    [SerializeField] GameObject AlertSoundsObject;
    [SerializeField] AudioSource ChaseAudioSource;

    AudioSource _activeAlert;
    bool _alertPaused, _chaseStarted, _chasePaused;

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
        if (_activeAlert != null && (_activeAlert.isPlaying || _alertPaused)) return;
        if (_alertSoundSources.Count == 0)
        {
            return;
        }
        int index = Random.Range(0, _alertSoundSources.Count);
        AudioSource alertSource = _alertSoundSources[index];
        if (alertSource.clip != null)
        {
            _activeAlert = alertSource;
            _alertPaused = false;
            alertSource.loop = false;
            alertSource.Play();
        }
    }

    public void UpdateVocalState(MonsterState state, GameState gameplay)
    {
        if (gameplay == GameState.GameOver || state == MonsterState.Roaming)
        {
            StopVocals();
            return;
        }
        if (gameplay == GameState.Paused)
        {
            if (_activeAlert != null && _activeAlert.isPlaying)
            {
                _activeAlert.Pause();
                _alertPaused = true;
            }
            PauseChase();
            return;
        }
        if (_alertPaused && _activeAlert != null)
        {
            _activeAlert.UnPause();
            _alertPaused = false;
        }
        if (_activeAlert != null && !_activeAlert.isPlaying) _activeAlert = null;
        if (state != MonsterState.Chasing || _activeAlert != null)
        {
            PauseChase();
            return;
        }
        if (ChaseAudioSource == null || ChaseAudioSource.clip == null) return;
        if (_chasePaused)
        {
            ChaseAudioSource.UnPause();
            _chasePaused = false;
        }
        else if (!_chaseStarted)
        {
            ChaseAudioSource.loop = true;
            ChaseAudioSource.Play();
            _chaseStarted = true;
        }
    }

    void PauseChase()
    {
        if (!_chaseStarted || _chasePaused || ChaseAudioSource == null) return;
        ChaseAudioSource.Pause();
        _chasePaused = true;
    }

    public void StopVocals()
    {
        if (_activeAlert != null) _activeAlert.Stop();
        if (ChaseAudioSource != null && _chaseStarted) ChaseAudioSource.Stop();
        _activeAlert = null;
        _alertPaused = _chaseStarted = _chasePaused = false;
    }

    void OnDisable() => StopVocals();
}

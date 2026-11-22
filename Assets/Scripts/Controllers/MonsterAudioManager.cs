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
    [SerializeField] AudioSource BreathingAudioSource;

    [SerializeField, Min(0.01f)] float VocalFadeDuration = 0.35f;

    float _chaseVolume, _breathingVolume;
    float _chaseTarget, _breathingTarget;
    bool _vocalsSuspended, _stopChaseWhenSilent;

    AudioSource _activeAlert;
    bool _alertPaused, _chaseStarted, _chasePaused;
    bool _breathingPaused;

    List<AudioSource> _footstepAudioSources;
    List<AudioSource> _alertSoundSources;
    float _distanceTraveled;

    void Awake()
    {
        _chaseVolume = ChaseAudioSource != null ? ChaseAudioSource.volume : 1f;
        _breathingVolume = BreathingAudioSource != null ? BreathingAudioSource.volume : 1f;
        _breathingTarget = _breathingVolume;
        if (ChaseAudioSource != null) ChaseAudioSource.volume = 0f;
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
        _vocalsSuspended = gameplay == GameState.Paused || gameplay == GameState.GameOver;
        if (gameplay == GameState.GameOver)
        {
            StopVocals();
            PauseBreathing();
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
            PauseBreathing();
            return;
        }
        _stopChaseWhenSilent = state == MonsterState.Roaming || state == MonsterState.Investigating;
        if (_stopChaseWhenSilent && _activeAlert != null)
        {
            _activeAlert.Stop();
            _activeAlert = null;
            _alertPaused = false;
        }
        if (_alertPaused && _activeAlert != null)
        {
            _activeAlert.UnPause();
            _alertPaused = false;
        }
        if (_activeAlert != null && !_activeAlert.isPlaying) _activeAlert = null;
        bool chasing = state == MonsterState.Chasing;
        _breathingTarget = chasing ? 0f : _breathingVolume;
        _chaseTarget = chasing && _activeAlert == null ? _chaseVolume : 0f;
        if (_breathingTarget > 0f && _breathingPaused && BreathingAudioSource != null)
        {
            BreathingAudioSource.UnPause();
            _breathingPaused = false;
        }
        if (ChaseAudioSource == null || ChaseAudioSource.clip == null) return;
        if (_chasePaused && (_chaseTarget > 0f || ChaseAudioSource.volume > 0f))
        {
            ChaseAudioSource.UnPause();
            _chasePaused = false;
        }
        else if (!_chaseStarted && _chaseTarget > 0f)
        {
            ChaseAudioSource.volume = 0f;
            ChaseAudioSource.loop = true;
            ChaseAudioSource.Play();
            _chaseStarted = true;
        }
    }

    void Update() => FadeVocals(Time.deltaTime);

    void FadeVocals(float deltaTime)
    {
        if (_vocalsSuspended) return;
        float duration = Mathf.Max(0.01f, VocalFadeDuration);
        if (BreathingAudioSource != null)
        {
            BreathingAudioSource.volume = Mathf.MoveTowards(BreathingAudioSource.volume,
                _breathingTarget, _breathingVolume * deltaTime / duration);
            if (_breathingTarget == 0f && BreathingAudioSource.volume == 0f) PauseBreathing();
        }
        if (ChaseAudioSource != null && _chaseStarted)
        {
            ChaseAudioSource.volume = Mathf.MoveTowards(ChaseAudioSource.volume,
                _chaseTarget, _chaseVolume * deltaTime / duration);
            if (_chaseTarget == 0f && ChaseAudioSource.volume == 0f)
            {
                if (_stopChaseWhenSilent)
                {
                    ChaseAudioSource.Stop();
                    _chaseStarted = _chasePaused = false;
                }
                else PauseChase();
            }
        }
    }

    void PauseBreathing()
    {
        if (BreathingAudioSource == null || !BreathingAudioSource.isPlaying) return;
        BreathingAudioSource.Pause();
        _breathingPaused = true;
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
        _chaseTarget = 0f;
        if (ChaseAudioSource != null) ChaseAudioSource.volume = 0f;
        _activeAlert = null;
        _alertPaused = _chaseStarted = _chasePaused = false;
    }

    void OnDisable() => StopVocals();
}

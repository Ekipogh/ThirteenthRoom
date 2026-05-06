using System.Collections.Generic;
using UnityEngine;

public class PlayerAudioManager : MonoBehaviour
{
    [SerializeField] AudioSource NormalBreathingAudioSource;
    [SerializeField] AudioSource SprintingBreathingAudioSource;
    [SerializeField] AudioSource GaspAudioSource;
    [SerializeField, Min(0.01f)] float BreathingFadeDuration = 0.35f;
    [SerializeField] GameObject FootStepSourceObject;
    List<AudioSource> _footstepAudioSources;

    bool _isSprinting;
    bool _isMonsterEncounterActive;
    bool _hasBreathingState;
    float _normalBreathingVolume = 1f;
    float _sprintingBreathingVolume = 1f;
    float _normalBreathingTargetVolume;
    float _sprintingBreathingTargetVolume;

    void Awake()
    {
        _normalBreathingVolume = GetConfiguredVolume(NormalBreathingAudioSource);
        _sprintingBreathingVolume = GetConfiguredVolume(SprintingBreathingAudioSource);

        SetVolumeIfPresent(NormalBreathingAudioSource, 0f);
        SetVolumeIfPresent(SprintingBreathingAudioSource, 0f);

        _footstepAudioSources = new List<AudioSource>();
        if (FootStepSourceObject != null)
        {
            _footstepAudioSources.AddRange(FootStepSourceObject.GetComponentsInChildren<AudioSource>());
        }
    }

    void Update()
    {
        FadeBreathingAudio(NormalBreathingAudioSource, _normalBreathingTargetVolume);
        FadeBreathingAudio(SprintingBreathingAudioSource, _sprintingBreathingTargetVolume);
    }

    public void SetSprinting(bool isSprinting)
    {
        if (_hasBreathingState && _isSprinting == isSprinting)
        {
            return;
        }

        _isSprinting = isSprinting;
        _hasBreathingState = true;

        if (_isMonsterEncounterActive)
        {
            return;
        }

        PlayBreathingForCurrentState();
    }

    public void SetMonsterEncounterActive(bool isActive)
    {
        if (_isMonsterEncounterActive == isActive)
        {
            return;
        }

        _isMonsterEncounterActive = isActive;

        if (_isMonsterEncounterActive)
        {
            FadeOutBreathing();
            PlayGasp();
            return;
        }

        PlayBreathingForCurrentState();
    }

    void PlayBreathingForCurrentState()
    {
        if (_isSprinting)
        {
            PlayIfNeeded(SprintingBreathingAudioSource);
            _normalBreathingTargetVolume = 0f;
            _sprintingBreathingTargetVolume = _sprintingBreathingVolume;
        }
        else
        {
            PlayIfNeeded(NormalBreathingAudioSource);
            _normalBreathingTargetVolume = _normalBreathingVolume;
            _sprintingBreathingTargetVolume = 0f;
        }
    }

    void FadeOutBreathing()
    {
        _normalBreathingTargetVolume = 0f;
        _sprintingBreathingTargetVolume = 0f;
    }

    void FadeBreathingAudio(AudioSource audioSource, float targetVolume)
    {
        if (audioSource == null)
        {
            return;
        }

        if (targetVolume > 0f)
        {
            PlayIfNeeded(audioSource);
        }

        audioSource.volume = Mathf.MoveTowards(
            audioSource.volume,
            targetVolume,
            Time.deltaTime / BreathingFadeDuration);

        if (Mathf.Approximately(audioSource.volume, 0f) && Mathf.Approximately(targetVolume, 0f))
        {
            PauseIfPresent(audioSource);
        }
    }

    float GetConfiguredVolume(AudioSource audioSource)
    {
        return audioSource != null ? audioSource.volume : 1f;
    }

    void SetVolumeIfPresent(AudioSource audioSource, float volume)
    {
        if (audioSource != null)
        {
            audioSource.volume = volume;
        }
    }

    void PauseIfPresent(AudioSource audioSource)
    {
        if (audioSource != null)
        {
            audioSource.Pause();
        }
    }

    void PlayIfNeeded(AudioSource audioSource)
    {
        if (audioSource != null && audioSource.isPlaying == false)
        {
            audioSource.Play();
        }
    }

    public void PlayGasp()
    {
        if (GaspAudioSource != null && GaspAudioSource.clip != null)
        {
            GaspAudioSource.PlayOneShot(GaspAudioSource.clip);
        }
    }

    public void PlayRandomFootstep()
    {
        if (_footstepAudioSources == null || _footstepAudioSources.Count == 0)
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
using System.Collections.Generic;
using UnityEngine;

public class PlayerAudioManager : MonoBehaviour
{
    [Header("Breathing Sources")]
    [SerializeField] AudioSource NormalBreathingAudioSource;
    [SerializeField] AudioSource SprintingBreathingAudioSource;
    [SerializeField] AudioSource RecoveringBreathingAudioSource;

    [Header("One-Shot Sources")]
    [SerializeField] AudioSource GaspAudioSource;
    [SerializeField] AudioSource PickupAudioSource;

    [Header("Breathing Settings")]
    [SerializeField, Min(0.01f)] float BreathingFadeDuration = 0.35f;
    [SerializeField, Min(0.01f)] float RecoveringBreathingLowStaminaVolume = 1.0f;
    [SerializeField, Min(0.01f)] float RecoveringBreathingHighStaminaVolume = 0.0f;

    [Header("Footsteps")]
    [SerializeField] GameObject FootStepSourceObject;
    [SerializeField] GameObject SprintFootStepSourceObject;

    List<AudioSource> _footstepAudioSources;
    List<AudioSource> _sprintingAudioSources;

    bool _isSprinting;
    bool _isRecovering;
    bool _isMonsterEncounterActive;
    bool _hasBreathingState;
    float _normalBreathingVolume = 1f;
    float _sprintingBreathingVolume = 1f;
    float _recoveringBreathingVolume = 1f;
    float _normalBreathingTargetVolume;
    float _sprintingBreathingTargetVolume;
    float _recoveringBreathingTargetVolume;

    void Awake()
    {
        _normalBreathingVolume = GetConfiguredVolume(NormalBreathingAudioSource);
        _sprintingBreathingVolume = GetConfiguredVolume(SprintingBreathingAudioSource);
        _recoveringBreathingVolume = GetConfiguredVolume(RecoveringBreathingAudioSource);
        SetVolumeIfPresent(NormalBreathingAudioSource, 0f);
        SetVolumeIfPresent(SprintingBreathingAudioSource, 0f);
        SetVolumeIfPresent(RecoveringBreathingAudioSource, 0f);

        _footstepAudioSources = new List<AudioSource>();
        _sprintingAudioSources = new List<AudioSource>();
        if (FootStepSourceObject != null)
        {
            _footstepAudioSources.AddRange(FootStepSourceObject.GetComponentsInChildren<AudioSource>());
        }
        if (SprintFootStepSourceObject != null)
        {
            _sprintingAudioSources.AddRange(SprintFootStepSourceObject.GetComponentsInChildren<AudioSource>());
        }
    }

    void Update()
    {
        FadeBreathingAudio(NormalBreathingAudioSource, _normalBreathingTargetVolume);
        FadeBreathingAudio(SprintingBreathingAudioSource, _sprintingBreathingTargetVolume);
        FadeBreathingAudio(RecoveringBreathingAudioSource, _recoveringBreathingTargetVolume);
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

    public void SetRecovering(bool isRecovering, float staminaPercent)
    {
        if (isRecovering)
        {
            SetRecoveringBreathingPitch(staminaPercent);
        }

        if (_isRecovering == isRecovering)
        {
            return;
        }

        _isRecovering = isRecovering;

        if (_isMonsterEncounterActive)
        {
            return;
        }

        PlayBreathingForCurrentState();
    }

    void SetRecoveringBreathingPitch(float staminaPercent)
    {
        if (RecoveringBreathingAudioSource == null)
        {
            return;
        }

        float normalizedStamina = Mathf.Clamp01(staminaPercent);
        RecoveringBreathingAudioSource.volume = Mathf.Lerp(
            RecoveringBreathingLowStaminaVolume,
            RecoveringBreathingHighStaminaVolume,
            normalizedStamina
        );
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
            _recoveringBreathingTargetVolume = 0f;
            _sprintingBreathingTargetVolume = _sprintingBreathingVolume;
        }
        else if (_isRecovering)
        {
            PlayIfNeeded(RecoveringBreathingAudioSource);
            _normalBreathingTargetVolume = 0f;
            _sprintingBreathingTargetVolume = 0f;
            _recoveringBreathingTargetVolume = _recoveringBreathingVolume;
        }
        else
        {
            PlayIfNeeded(NormalBreathingAudioSource);
            _normalBreathingTargetVolume = _normalBreathingVolume;
            _sprintingBreathingTargetVolume = 0f;
            _recoveringBreathingTargetVolume = 0f;
        }
    }

    void FadeOutBreathing()
    {
        _normalBreathingTargetVolume = 0f;
        _sprintingBreathingTargetVolume = 0f;
        _recoveringBreathingTargetVolume = 0f;
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

    public void PlayRandomFootstep(bool isSprinting)
    {
        List<AudioSource> audioSources = isSprinting ? _sprintingAudioSources : _footstepAudioSources;
        if (audioSources == null || audioSources.Count == 0)
        {
            return;
        }

        int index = Random.Range(0, audioSources.Count);
        AudioSource footstepSource = audioSources[index];
        if (footstepSource.clip != null)
        {
            footstepSource.PlayOneShot(footstepSource.clip);
        }
    }

    public void PlayPickupSound(AudioClip clip)
    {
        if (clip != null && PickupAudioSource != null)
        {
            PickupAudioSource.PlayOneShot(clip);
        }
    }
}

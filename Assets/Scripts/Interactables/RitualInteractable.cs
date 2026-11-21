using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class RitualInteractable : MonoBehaviour, IHoldInteractable
{
    public event System.Action<RitualInteractable, bool> StateChanged;
    public bool IsComplete => _isActive;
    GameManager _aggressionOwner;

    public void BindAggressionOwner(GameManager owner)
    {
        if (_aggressionOwner == owner) return;
        if (_aggressionOwner != null) _aggressionOwner.UnregisterRitual(this);
        _aggressionOwner = owner;
        if (_aggressionOwner != null) _aggressionOwner.RegisterRitual(this);
    }
    void OnDestroy()
    {
        if (_aggressionOwner != null) _aggressionOwner.UnregisterRitual(this);
    }
    bool GameplayActive => _aggressionOwner == null || _aggressionOwner.currentState == GameState.Playing;

    [Header("Ritual Objects")]
    [SerializeField] List<RitualCandle> candles;

    [Header("Audio")]
    [SerializeField] AudioSource ritualAudioSource;

    [Header("Scoring")]
    [SerializeField] ScoreManager scoreManager;

    private readonly float interactionTime = 10f; // Time required to complete the ritual
    private float currentInteractionTime = 0f;
    private bool _isActive = false; // false = ritual needs to be completed, true = ritual is complete
    private bool _isPerformingRitual = false;

    private readonly float _deactivateChance = 0.1f;
    private float _deactivateTimer = 0f;
    private readonly float _deactivateInterval = 5f; // Check every 5 seconds

    private readonly int _scorePerRitual = 100;

    void Update()
    {
        if (!GameplayActive) return;
        if (_isActive)
        {
            _deactivateTimer += Time.deltaTime;
            if (_deactivateTimer >= _deactivateInterval)
            {
                _deactivateTimer = 0f;
                if (Random.value < _deactivateChance)
                {
                    DeactivateRitual();
                }
            }
        }
    }

    void Awake()
    {
        if (ritualAudioSource == null)
        {
            ritualAudioSource = GetComponent<AudioSource>();
        }
    }

    private void DeactivateRitual()
    {
        if (!_isActive) return;
        _isActive = false;
        StateChanged?.Invoke(this, false);
        if (candles == null)
        {
            return;
        }

        foreach (RitualCandle candle in candles)
        {
            if (candle != null)
            {
                candle.ExtinguishCandle();
            }
        }
        ShuffleCandles(); // Candles will re-light in a different order when the ritual is performed again
        currentInteractionTime = 0f;
        _deactivateTimer = 0f;
    }

    private void CompleteRitual()
    {
        if (_isActive) return;
        _isActive = true;
        StateChanged?.Invoke(this, true);
        currentInteractionTime = 0f;
        StopRitualMusic();
    }

    public void BeginHoldInteract(PlayerInteractor interactor)
    {
        if (!GameplayActive) return;
        if (!_isActive)
        {
            StartRitualMusic();
        }
    }

    public void HoldInteract(PlayerInteractor interactor, float deltaTime)
    {
        if (!GameplayActive || _isActive)
        {
            return;
        }

        currentInteractionTime += deltaTime;
        UpdateLights();
        if (currentInteractionTime >= interactionTime)
        {
            CompleteRitual();
            if (scoreManager != null)
            {
                scoreManager.AddScore(_scorePerRitual);
            }
        }
    }

    public void EndHoldInteract(PlayerInteractor interactor)
    {
        StopRitualMusic();

        // Releasing before completion resets progress.
        if (!_isActive)
        {
            currentInteractionTime = 0f;
            UpdateLights();
        }
    }

    private void StartRitualMusic()
    {
        if (_isPerformingRitual)
        {
            return;
        }

        if (ritualAudioSource == null)
        {
            return;
        }

        ritualAudioSource.Play();
        _isPerformingRitual = true;
    }

    private void StopRitualMusic()
    {
        if (!_isPerformingRitual || ritualAudioSource == null)
        {
            return;
        }

        ritualAudioSource.Stop();
        _isPerformingRitual = false;
    }

    public string GetInteractionPrompt(PlayerInteractor interactor)
    {
        return _isActive ? "Ritual is complete" : "Hold E to complete the ritual";
    }

    private void UpdateLights()
    {
        if (candles == null || candles.Count == 0)
        {
            return;
        }

        float progress = currentInteractionTime / interactionTime;
        int candlesToLight = Mathf.FloorToInt(progress * candles.Count);
        for (int i = 0; i < candles.Count; i++)
        {
            if (candles[i] == null)
            {
                continue;
            }

            if (i < candlesToLight)
            {
                candles[i].LightCandle();
            }
            else
            {
                candles[i].ExtinguishCandle();
            }
        }
    }

    void ShuffleCandles()
    {
        if (candles == null || candles.Count <= 1)
        {
            return;
        }

        for (int i = 0; i < candles.Count; i++)
        {
            int randomIndex = Random.Range(0, candles.Count);
            RitualCandle temp = candles[i];
            candles[i] = candles[randomIndex];
            candles[randomIndex] = temp;
        }
    }
}

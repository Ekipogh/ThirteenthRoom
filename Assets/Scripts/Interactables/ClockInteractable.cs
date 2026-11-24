using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Clock))]
public class ClockInteractable : MonoBehaviour, IInteractable, IScoreReceiver
{
    [SerializeField] private ScoreManager scoreManager;

    public void BindScoreManager(ScoreManager manager) => scoreManager = manager;

    private float _timerDuration = 60f;
    private float _currentTime = 0f;

    [Range(0f, 1f)]
    [SerializeField] private float _breakingChance = 0.3f;

    private bool _isBroken = false;
    [SerializeField] private float _fixingScore = 10f;

    [SerializeField] AudioSource _breakSound;

    [SerializeField] Clock _clock;

    public void Interact(PlayerInteractor playerInteractor)
    {
        if (_isBroken)
        {
            FixClock();
        }
    }

    public string GetInteractionPrompt(PlayerInteractor playerInteractor)
    {
        if (_isBroken)
        {
            return "Fix the clock";
        }
        return "An antique grandfather clock";
    }

    void Update()
    {
        if (_isBroken)
        {
            return;
        }
        _currentTime += Time.deltaTime;
        if (_currentTime > _timerDuration)
        {
            if (Random.value < _breakingChance)
            {
                BreakClock();
            }
            _currentTime = 0;
        }
    }

    private void BreakClock()
    {
        _isBroken = true;
        _clock.IsBroken = true;
        if (_breakSound != null)
        {
            _breakSound.Play();
        }
    }

    private void FixClock()
    {
        _isBroken = false;
        _clock.IsBroken = false;
        _currentTime = 0f;
        if (scoreManager != null)
        {
            scoreManager.AddScore(_fixingScore);
        }
    }
}
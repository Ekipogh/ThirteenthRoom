using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;

public class FuseBox : MonoBehaviour, IInteractable
{
    [SerializeField] List<Room> rooms;
    [SerializeField] Transform particleEffectPoint;
    [SerializeField] List<Transform> fuses;
    [SerializeField] List<Transform> fuseItems;
    [SerializeField] ScoreManager scoreManager;
    [SerializeField] Transform boxSwitch;
    [SerializeField] AudioSource fuseInsertSound;
    [SerializeField] AudioSource powerOnSound;
    [SerializeField] AudioSource FizzleSound;

    float _switchOffAngle = 60f;

    public float ScoreReward = 20f;
    readonly float _timerDuration = 20f;
    float _currentTimer = 0f;
    readonly float _fuseBlowChance = 0.5f;
    bool _isActive = true;

    int _fuseActiveCount = 0;

    void Start()
    {
        _currentTimer = _timerDuration;
        _fuseActiveCount = fuses.Count;
    }

    public void ActivateFuseBox(bool activate)
    {
        _isActive = activate;
        particleEffectPoint.gameObject.SetActive(!_isActive);
        MakeFusesVisible(_isActive);
        if (boxSwitch != null)
        {
            float targetAngle = activate ? 0f : _switchOffAngle;
            boxSwitch.localRotation = Quaternion.Euler(targetAngle, 0f, 0f);
        }

        foreach (var room in rooms)
        {
            LightSwitchInteractable lightSwitch = room.GetLightSwitch();
            if (lightSwitch != null)
            {
                lightSwitch.SetPower(activate);
            }
        }

        if (!activate && FizzleSound != null)
        {
            FizzleSound.PlayOneShot(FizzleSound.clip);
        }
    }

    void MakeFusesVisible(bool visible)
    {
        foreach (var fuse in fuses)
        {
            fuse.gameObject.SetActive(visible);
        }
        _fuseActiveCount = visible ? fuses.Count : 0;
        foreach (var item in fuseItems)
        {
            item.gameObject.SetActive(!visible);
        }
    }

    void Update()
    {
        if (_currentTimer > 0 && _isActive)
        {
            _currentTimer -= Time.deltaTime;
            if (_currentTimer <= 0)
            {
                _currentTimer = _timerDuration;
                if (Random.value < _fuseBlowChance)
                {
                    ActivateFuseBox(false);
                }
            }
        }
    }

    public string GetInteractionPrompt(PlayerInteractor playerInteractor)
    {
        string prompt = "";
        if (!_isActive)
        {
            int fusesNeeded = fuses.Count - _fuseActiveCount;
            prompt = fusesNeeded > 0 ? $"Press E to insert a fuse ({fusesNeeded} needed)" : "Press E to restore power";
        }
        return prompt;
    }

    public void Interact(PlayerInteractor playerInteractor)
    {
        if (!_isActive)
        {
            int fusesNeeded = fuses.Count - _fuseActiveCount;
            if (fusesNeeded <= 0)
            {
                ActivateFuseBox(true);
                scoreManager.AddScore(ScoreReward);
                if (powerOnSound != null)
                {
                    powerOnSound.PlayOneShot(powerOnSound.clip);
                }
                return;
            }

            if (playerInteractor.Inventory.RemoveItem("Fuse"))
            {
                _fuseActiveCount++;
                fuses[_fuseActiveCount - 1].gameObject.SetActive(true);
                if (fuseInsertSound != null)
                {
                    fuseInsertSound.PlayOneShot(fuseInsertSound.clip);
                }
            }
        }
    }
}

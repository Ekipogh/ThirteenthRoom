using System.Collections.Generic;
using UnityEngine;

public class FuseBox : MonoBehaviour
{
    [SerializeField] List<Room> rooms;
    [SerializeField] Transform particleEffectPoint;
    [SerializeField] List<Transform> fuses;
    [SerializeField] List<Transform> fuseItems;
    readonly float _timerDuration = 20f;
    float _currentTimer = 0f;
    readonly float _fuseBlowChance = 0.5f;
    bool _isActive = true;

    void Start()
    {
        _currentTimer = _timerDuration;
    }

    public void ActivateFuseBox(bool activate)
    {
        foreach (var room in rooms)
        {
            var lightSwitch = room.GetLightSwitch();
            if (lightSwitch != null)
            {
                if (lightSwitch.TryGetComponent<LightSwitchInteractable>(out var interactable))
                {
                    interactable.SwitchLight(activate);
                    interactable.IsPowered = activate; // Set the powered state of the switch
                    _isActive = activate;
                    particleEffectPoint.gameObject.SetActive(!_isActive);
                    MakeFusesVisible(_isActive);
                }
            }
        }
    }

    void MakeFusesVisible(bool visible)
    {
        foreach (var fuse in fuses)
        {
            fuse.gameObject.SetActive(visible);
        }
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
}

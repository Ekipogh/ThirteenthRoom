using System.Collections.Generic;
using UnityEngine;

public class FuseBox : MonoBehaviour, IInteractable
{
    [Header("Controlled Rooms")]
    [SerializeField] List<Room> rooms;

    [Header("Visuals")]
    [SerializeField] Transform particleEffectPoint;
    [SerializeField] List<Transform> fuses;
    [SerializeField] List<Transform> fuseItems;
    [SerializeField] Transform boxSwitch;

    [Header("Audio")]
    [SerializeField] AudioSource fuseInsertSound;
    [SerializeField] AudioSource powerOnSound;
    [SerializeField] AudioSource FizzleSound;

    [Header("Required Item")]
    [SerializeField] ItemDefinition fuseItem;

    [Header("Scoring")]
    public float ScoreReward = 20f;

    [SerializeField] ScoreManager scoreManager;

    readonly float _timerDuration = 5 * 60f;
    readonly float _fuseBlowChance = 0.3f;
    float _switchOffAngle = 60f;
    float _currentTimer = 0f;
    bool _isActive = true;

    int _fuseActiveCount = 0;

    void Start()
    {
        _currentTimer = _timerDuration;
        _fuseActiveCount = fuses != null ? fuses.Count : 0;
    }

    public void SetControlledRooms(IEnumerable<Room> controlledRooms)
    {
        rooms = new List<Room>();
        if (controlledRooms != null)
        {
            foreach (Room room in controlledRooms)
            {
                if (room != null && !rooms.Contains(room))
                {
                    rooms.Add(room);
                }
            }
        }

        ApplyPowerToRooms(_isActive);
    }

    public void ActivateFuseBox(bool activate)
    {
        _isActive = activate;
        if (particleEffectPoint != null)
        {
            particleEffectPoint.gameObject.SetActive(!_isActive);
        }

        MakeFusesVisible(_isActive);
        if (boxSwitch != null)
        {
            float targetAngle = activate ? 0f : _switchOffAngle;
            boxSwitch.localRotation = Quaternion.Euler(targetAngle, 0f, 0f);
        }

        if (rooms != null)
        {
            foreach (Room room in rooms)
            {
                if (room == null)
                {
                    continue;
                }

                ApplyPowerToRoom(room, activate);
            }
        }

        if (!activate && FizzleSound != null)
        {
            FizzleSound.PlayOneShot(FizzleSound.clip);
        }
    }

    void ApplyPowerToRooms(bool activate)
    {
        if (rooms == null)
        {
            return;
        }

        foreach (Room room in rooms)
        {
            if (room != null)
            {
                ApplyPowerToRoom(room, activate);
            }
        }
    }

    void ApplyPowerToRoom(Room room, bool activate)
    {
        List<LightObject> lightObjects = room.GetLightObjects();
        if (lightObjects == null)
        {
            return;
        }

        foreach (LightObject lightObject in lightObjects)
        {
            if (lightObject != null)
            {
                lightObject.SetPower(activate);
            }
        }
    }

    void MakeFusesVisible(bool visible)
    {
        if (fuses != null)
        {
            foreach (var fuse in fuses)
            {
                if (fuse != null)
                {
                    fuse.gameObject.SetActive(visible);
                }
            }
        }

        _fuseActiveCount = visible && fuses != null ? fuses.Count : 0;
        if (fuseItems != null)
        {
            foreach (var item in fuseItems)
            {
                if (item != null)
                {
                    item.gameObject.SetActive(!visible);
                }
            }
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
            int fuseCount = fuses != null ? fuses.Count : 0;
            int fusesNeeded = fuseCount - _fuseActiveCount;
            prompt = fusesNeeded > 0 ? $"Press E to insert a fuse ({fusesNeeded} needed)" : "Press E to restore power";
        }
        return prompt;
    }

    public void Interact(PlayerInteractor playerInteractor)
    {
        if (!_isActive)
        {
            int fuseCount = fuses != null ? fuses.Count : 0;
            int fusesNeeded = fuseCount - _fuseActiveCount;
            if (fusesNeeded <= 0)
            {
                ActivateFuseBox(true);
                if (scoreManager != null)
                {
                    scoreManager.AddScore(ScoreReward);
                }

                if (powerOnSound != null)
                {
                    powerOnSound.PlayOneShot(powerOnSound.clip);
                }
                return;
            }

            if (playerInteractor.Inventory.RemoveItem(fuseItem))
            {
                _fuseActiveCount++;
                if (fuses != null && _fuseActiveCount - 1 >= 0 && _fuseActiveCount - 1 < fuses.Count && fuses[_fuseActiveCount - 1] != null)
                {
                    fuses[_fuseActiveCount - 1].gameObject.SetActive(true);
                }

                if (fuseInsertSound != null)
                {
                    fuseInsertSound.PlayOneShot(fuseInsertSound.clip);
                }
            }
        }
    }
}

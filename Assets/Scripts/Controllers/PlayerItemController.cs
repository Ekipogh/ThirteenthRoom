using System;
using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-50)]
[RequireComponent(typeof(PlayerInteractor), typeof(PlayerController))]
public class PlayerItemController : MonoBehaviour
{
    PlayerInteractor _interactor;
    PlayerController _player;
    GameManager _manager;
    float _nextShot;
    CursorLockMode _previousLock;
    bool _previousCursorVisible;
    public ActiveItemSlots Slots { get; private set; }
    public PlayerInventory Inventory => _interactor.Inventory;
    public bool IsInventoryOpen { get; private set; }
    public bool GameplayActive => _manager == null || _manager.currentState == GameState.Playing;
    public event Action ViewChanged;
    public string Feedback { get; private set; }
    float _feedbackUntil;

    void Awake()
    {
        _interactor = GetComponent<PlayerInteractor>();
        _player = GetComponent<PlayerController>();
    }
    void Start()
    {
        _manager = FindFirstObjectByType<GameManager>();
        Slots = new ActiveItemSlots(Inventory);
        Slots.Changed += Notify;
        Inventory.Changed += Notify;
        ViewChanged?.Invoke();
    }
    void Notify() => ViewChanged?.Invoke();
    public void SetInventoryOpen(bool open)
    {
        if (open && (!GameplayActive || Slots == null)) return;
        if (open == IsInventoryOpen) return;
        IsInventoryOpen = open;
        if (open)
        {
            _previousLock = Cursor.lockState;
            _previousCursorVisible = Cursor.visible;
            _player.SetInventoryInputBlocked(true);
            _interactor.SetInventoryInputBlocked(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            _player.SetInventoryInputBlocked(false);
            _interactor.SetInventoryInputBlocked(false);
            Cursor.lockState = GameplayActive ? _previousLock : CursorLockMode.None;
            Cursor.visible = GameplayActive ? _previousCursorVisible : true;
        }
        Notify();
    }
    void Update()
    {
        if (Slots == null) return;
        if (!GameplayActive) { SetInventoryOpen(false); return; }
        if (!string.IsNullOrEmpty(Feedback) && Time.unscaledTime >= _feedbackUntil) { Feedback = ""; Notify(); }
        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.iKey.wasPressedThisFrame || keyboard.tabKey.wasPressedThisFrame) { SetInventoryOpen(!IsInventoryOpen); return; }
            if (IsInventoryOpen && keyboard.escapeKey.wasPressedThisFrame) { SetInventoryOpen(false); return; }
            for (int i = 0; i < ActiveItemSlots.Count; i++)
                if (keyboard[(Key)((int)Key.Digit1 + i)].wasPressedThisFrame) Slots.Select(i);
        }
        if (IsInventoryOpen || Cursor.lockState != CursorLockMode.Locked) return;
        var mouse = Mouse.current;
        if (mouse != null && mouse.scroll.ReadValue().y != 0f) Slots.Cycle(mouse.scroll.ReadValue().y > 0f ? -1 : 1);
        if (keyboard != null && keyboard.rKey.wasPressedThisFrame) TryReload();
        if (mouse != null && mouse.leftButton.wasPressedThisFrame) TryUseSelected();
    }
    public bool TryReload()
    {
        if (!CanUse(out var entry) || entry.Definition.UseEffect != ItemUseEffect.Gun) return false;
        int loaded = Inventory.Reload(entry.Definition);
        ShowFeedback(loaded > 0 ? "Reloaded" : Inventory.LoadedRounds(entry.Definition) >= entry.Definition.MagazineCapacity ? "Already loaded" : "No ammunition");
        return loaded > 0;
    }
    bool CanUse(out InventoryEntry entry)
    {
        entry = Slots?.Selected ?? default;
        return isActiveAndEnabled && GameplayActive && !IsInventoryOpen && Slots?.Selected != null && Inventory.Quantity(entry) > 0 && !entry.IsInstance;
    }
    public bool TryUseSelected()
    {
        if (!CanUse(out var entry)) return false;
        var item = entry.Definition;
        if (item.UseEffect == ItemUseEffect.StaminaPotion)
        {
            if (!_player.RestoreStamina(item.StaminaRestored)) { ShowFeedback("Stamina is full"); return false; }
            Inventory.RemoveItem(item);
            ShowFeedback("Stamina restored");
            return true;
        }
        if (item.UseEffect != ItemUseEffect.Gun || Time.time < _nextShot) return false;
        var camera = _player.PlayerCamera;
        if (camera == null) return false;
        if (!Inventory.ConsumeLoadedRound(item)) { ShowFeedback("Empty - press R to reload"); return false; }
        _nextShot = Time.time + Mathf.Max(0f, item.ShotCooldown);
        if (item.ShotAgitation != null) _manager?.ReportAgitation(transform.position, item.ShotAgitation);
        var ray = new Ray(camera.transform.position, camera.transform.forward);
        var hits = Physics.RaycastAll(ray, Mathf.Max(0f, item.ShotRange), ~0, QueryTriggerInteraction.Collide);
        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (var hit in hits)
        {
            if (hit.collider.transform.IsChildOf(transform)) continue;
            var monster = hit.collider.GetComponentInParent<MonsterController>();
            // Monster hitboxes are triggers so they do not change navigation or player collision.
            if (hit.collider.isTrigger && monster == null) continue;
            if (monster != null) monster.ApplyStun(item.StunSeconds);
            ShowFeedback(monster != null ? "Monster stunned" : "Shot fired");
            return true;
        }
        ShowFeedback("Shot fired");
        return true;
    }
    void ShowFeedback(string message) { Feedback = message; _feedbackUntil = Time.unscaledTime + 1.8f; Notify(); }
    void OnDisable() { if (IsInventoryOpen) SetInventoryOpen(false); }
    void OnDestroy()
    {
        if (Slots != null) { Slots.Changed -= Notify; Slots.Dispose(); }
        if (_interactor != null && _interactor.Inventory != null) Inventory.Changed -= Notify;
    }
}

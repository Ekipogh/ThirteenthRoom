using System;

public sealed class ActiveItemSlots : IDisposable
{
    public const int Count = 9;
    readonly PlayerInventory _inventory;
    readonly InventoryEntry?[] _slots = new InventoryEntry?[Count];
    public event Action Changed;
    public int SelectedIndex { get; private set; }
    public InventoryEntry? Selected => _slots[SelectedIndex];
    public InventoryEntry? this[int index] => _slots[index];

    public ActiveItemSlots(PlayerInventory inventory) { _inventory = inventory; _inventory.Changed += Prune; }
    public void Select(int index)
    {
        if (index < 0 || index >= Count || index == SelectedIndex) return;
        SelectedIndex = index;
        Changed?.Invoke();
    }
    public void Cycle(int direction) => Select((SelectedIndex + direction % Count + Count) % Count);
    public bool Assign(int index, InventoryEntry entry)
    {
        if (index < 0 || index >= Count || _inventory.Quantity(entry) <= 0) return false;
        for (int i = 0; i < Count; i++) if (_slots[i].HasValue && _slots[i].Value.Equals(entry)) _slots[i] = null;
        _slots[index] = entry;
        Changed?.Invoke();
        return true;
    }
    public void Clear(int index)
    {
        if (index < 0 || index >= Count) return;
        _slots[index] = null;
        Changed?.Invoke();
    }
    void Prune()
    {
        for (int i = 0; i < Count; i++)
            if (_slots[i].HasValue && _inventory.Quantity(_slots[i].Value) == 0) _slots[i] = null;
        Changed?.Invoke();
    }
    public void Dispose() => _inventory.Changed -= Prune;
}

using System;
using System.Collections.Generic;

public readonly struct InventoryEntry : IEquatable<InventoryEntry>
{
    public ItemDefinition Definition { get; }
    public string TargetID { get; }
    public bool IsInstance { get; }
    public InventoryEntry(ItemDefinition definition) { Definition = definition; TargetID = null; IsInstance = false; }
    public InventoryEntry(ItemDefinition definition, string targetID) { Definition = definition; TargetID = targetID; IsInstance = true; }
    public bool Equals(InventoryEntry other) => Definition == other.Definition && IsInstance == other.IsInstance && TargetID == other.TargetID;
    public override bool Equals(object obj) => obj is InventoryEntry other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Definition, TargetID, IsInstance);
}

public class PlayerInventory
{
    readonly Dictionary<ItemDefinition, int> _items = new();
    readonly List<InstanceItem> _instanceItems = new();
    readonly Dictionary<ItemDefinition, int> _loadedRounds = new();
    public event Action Changed;

    // Enumerate a snapshot: callers cannot mutate storage or invalidate it while consuming items.
    public IReadOnlyList<InventoryEntry> Entries
    {
        get
        {
            var entries = new List<InventoryEntry>();
            foreach (var pair in _items) entries.Add(new InventoryEntry(pair.Key));
            foreach (var item in _instanceItems)
            {
                var entry = new InventoryEntry(item.Definition, item.TargetID);
                if (!entries.Contains(entry)) entries.Add(entry);
            }
            return entries.AsReadOnly();
        }
    }

    public int Quantity(ItemDefinition item) => item != null && _items.TryGetValue(item, out int count) ? count : 0;
    public int Quantity(InventoryEntry entry)
    {
        if (!entry.IsInstance) return Quantity(entry.Definition);
        int count = 0;
        foreach (var item in _instanceItems)
            if (item.Definition == entry.Definition && item.TargetID == entry.TargetID) count++;
        return count;
    }
    public void AddItem(ItemDefinition item) => AddItems(item, 1);
    public void AddItems(ItemDefinition item, int quantity)
    {
        if (item == null || quantity <= 0) return;
        _items[item] = Quantity(item) + quantity;
        Changed?.Invoke();
    }
    public void AddInstanceItem(ItemDefinition itemDefinition, string targetID, string inventoryDetail = null)
    {
        if (itemDefinition == null) return;
        _instanceItems.Add(new InstanceItem(itemDefinition, targetID, inventoryDetail));
        Changed?.Invoke();
    }
    public string GetInventoryDetail(InventoryEntry entry)
    {
        if (entry.IsInstance)
        {
            var item = _instanceItems.Find(x => x.Definition == entry.Definition && x.TargetID == entry.TargetID);
            if (!string.IsNullOrWhiteSpace(item?.InventoryDetail)) return item.InventoryDetail;
        }
        return entry.Definition != null ? entry.Definition.InventoryDetail : null;
    }
    public bool HasItem(ItemDefinition item) => Quantity(item) > 0;
    public bool HasInstanceItem(ItemDefinition item, string targetID) => item != null && Quantity(new InventoryEntry(item, targetID)) > 0;
    public bool RemoveItem(ItemDefinition item) => RemoveItems(item, 1);
    public bool RemoveItems(ItemDefinition item, int quantity)
    {
        if (quantity <= 0 || Quantity(item) < quantity) return false;
        int remaining = _items[item] - quantity;
        if (remaining == 0) { _items.Remove(item); _loadedRounds.Remove(item); }
        else _items[item] = remaining;
        Changed?.Invoke();
        return true;
    }
    public bool RemoveInstanceItem(ItemDefinition item, string targetID, string inventoryDetail = null)
    {
        int index = _instanceItems.FindIndex(x => x.Definition == item && x.TargetID == targetID);
        if (item == null || index < 0) return false;
        _instanceItems.RemoveAt(index);
        Changed?.Invoke();
        return true;
    }
    public int LoadedRounds(ItemDefinition gun) => gun != null && _loadedRounds.TryGetValue(gun, out int count) ? count : 0;
    public int Reload(ItemDefinition gun)
    {
        if (!HasItem(gun) || gun.UseEffect != ItemUseEffect.Gun || gun.Ammunition == null) return 0;
        int transferred = Math.Min(Math.Max(0, gun.MagazineCapacity - LoadedRounds(gun)), Quantity(gun.Ammunition));
        if (transferred <= 0) return 0;
        _loadedRounds[gun] = LoadedRounds(gun) + transferred;
        RemoveItems(gun.Ammunition, transferred);
        return transferred;
    }
    public bool ConsumeLoadedRound(ItemDefinition gun)
    {
        if (!HasItem(gun) || LoadedRounds(gun) <= 0) return false;
        _loadedRounds[gun]--;
        Changed?.Invoke();
        return true;
    }
}

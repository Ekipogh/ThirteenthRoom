using System.Collections.Generic;

public class PlayerInventory
{
    List<string> _items;
    public PlayerInventory()
    {
        _items = new List<string>();
    }

    public void AddItem(string item)
    {
        _items.Add(item);
    }

    public bool HasItem(string item)
    {
        return _items.Contains(item);
    }

    public void RemoveItem(string item)
    {
        _items.Remove(item);
    }
}
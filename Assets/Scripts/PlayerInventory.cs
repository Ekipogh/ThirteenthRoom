using System.Collections.Generic;

public class PlayerInventory
{
    Dictionary<ItemDefinition, int> _items;
    public PlayerInventory()
    {
        _items = new Dictionary<ItemDefinition, int>();
    }

    public void AddItem(ItemDefinition item)
    {
        if (_items.ContainsKey(item))
        {
            _items[item]++;
        }
        else
        {
            _items[item] = 1;
        }
    }

    public bool HasItem(ItemDefinition item)
    {
        return _items.ContainsKey(item);
    }

    public bool RemoveItem(ItemDefinition item)
    {
        if (_items.ContainsKey(item))
        {
            _items[item]--;
            if (_items[item] <= 0)
            {
                _items.Remove(item);
            }
            return true;
        }
        return false;
    }
}
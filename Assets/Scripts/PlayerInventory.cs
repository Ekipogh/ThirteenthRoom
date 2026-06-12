using System.Collections.Generic;

public class PlayerInventory
{
    Dictionary<ItemDefinition, int> _items;
    List<InstanceItem> _instanceItems;

    public PlayerInventory()
    {
        _items = new Dictionary<ItemDefinition, int>();
        _instanceItems = new List<InstanceItem>();
    }

    public void AddItem(ItemDefinition item)
    {
        if (item == null)
        {
            return;
        }

        if (_items.ContainsKey(item))
        {
            _items[item]++;
        }
        else
        {
            _items[item] = 1;
        }
    }

    public void AddInstanceItem(ItemDefinition itemDefinition, string targetID)
    {
        if (itemDefinition != null)
        {
            _instanceItems ??= new List<InstanceItem>();
            _instanceItems.Add(new InstanceItem(itemDefinition, targetID));
        }
    }

    public bool HasItem(ItemDefinition item)
    {
        if (item == null)
        {
            return false;
        }

        return _items.ContainsKey(item);
    }

    public bool HasInstanceItem(ItemDefinition instanceItem, string requiredItemId)
    {
        if (instanceItem == null)
        {
            return false;
        }

        for (int i = 0; i < _instanceItems.Count; i++)
        {
            if (_instanceItems[i].Definition == instanceItem && _instanceItems[i].TargetID == requiredItemId)
            {
                return true;
            }
        }
        return false;
    }

    public bool RemoveItem(ItemDefinition item)
    {
        if (item == null)
        {
            return false;
        }

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

    public bool RemoveInstanceItem(ItemDefinition instanceItem, string requiredItemId)
    {
        if (instanceItem == null || _instanceItems == null)
        {
            return false;
        }

        for (int i = 0; i < _instanceItems.Count; i++)
        {
            if (_instanceItems[i].Definition == instanceItem && _instanceItems[i].TargetID == requiredItemId)
            {
                _instanceItems.RemoveAt(i);
                return true;
            }
        }
        return false;
    }
}
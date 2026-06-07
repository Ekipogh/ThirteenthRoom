using UnityEngine;

[CreateAssetMenu(fileName = "ItemDefinition", menuName = "Scriptable Objects/ItemDefinition")]
public class ItemDefinition : ScriptableObject
{
    public string _itemId;
    public string _displayName;
    public Sprite _itemIcon;
    public string _category;
    public bool _isUnique;
    public bool _isConsumable;
    public int _scoreOnPickup;
}

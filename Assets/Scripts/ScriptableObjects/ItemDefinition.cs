using UnityEngine;

[CreateAssetMenu(fileName = "ItemDefinition", menuName = "Scriptable Objects/ItemDefinition")]
public class ItemDefinition : ScriptableObject
{
    public string ItemId;
    public string DisplayName;
    public Sprite ItemIcon;
    public ItemCategory Category;
    public bool IsUnique;
    public bool IsConsumable;
    public int ScoreOnPickup;
}

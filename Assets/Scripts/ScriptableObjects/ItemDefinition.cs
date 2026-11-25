using UnityEngine;

public enum ItemUseEffect { None, StaminaPotion, Gun }

[CreateAssetMenu(fileName = "ItemDefinition", menuName = "Scriptable Objects/ItemDefinition")]
public class ItemDefinition : ScriptableObject
{
    public string ItemId;
    public string DisplayName;
    public string InventoryDetail;
    public Sprite ItemIcon;
    public ItemCategory Category;
    public bool IsUnique;
    public bool IsConsumable;
    public int ScoreOnPickup;

    [Header("Use")]
    public ItemUseEffect UseEffect;
    [Min(0f)] public float StaminaRestored = 50f;
    public ItemDefinition Ammunition;
    [Min(1)] public int MagazineCapacity = 6;
    [Min(0f)] public float ShotRange = 25f;
    [Min(0f)] public float ShotCooldown = 0.5f;
    [Min(0f)] public float StunSeconds = 3f;
    public AgitationProfile ShotAgitation;
}

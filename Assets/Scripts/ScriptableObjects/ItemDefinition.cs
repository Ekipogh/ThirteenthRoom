using UnityEngine;
using UnityEngine.Serialization;

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
    public AudioClip ReloadSound;

    [Header("Use Feedback")]
    [Tooltip("Optional sound played after a successful item use.")]
    [FormerlySerializedAs("ShotSound")] public AudioClip UseSound;
    [Tooltip("Optional noise reported to nearby monsters after a successful item use. Independent of the audible sound.")]
    [FormerlySerializedAs("ShotAgitation")] public AgitationProfile UseAgitation;
}

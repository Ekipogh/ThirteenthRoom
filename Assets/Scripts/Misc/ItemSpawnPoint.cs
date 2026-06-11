using UnityEngine;

public class ItemSpawnPoint : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] ItemCategory _itemCategory;
    [SerializeField] bool _available = true;

    public void MarkAsOccupied()
    {
        _available = false;
    }

    public bool CanSpawnItem(SpawnItemDefinition itemDefinition)
    {
        return _available && itemDefinition.ItemDefinition.Category == _itemCategory;
    }
}

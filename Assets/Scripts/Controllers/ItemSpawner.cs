using UnityEngine;
using System.Collections.Generic;

public class ItemSpawner : MonoBehaviour
{
    [SerializeField] Transform itemParent;

    public void SpawnInitialItems(MansionModel mansion)
    {
        if (mansion == null)
        {
            Debug.LogWarning("Cannot spawn initial items because mansion is null.");
            return;
        }

        List<SpawnItemDefinition> itemsToSpawn = CollectRequestedItems(mansion);
        List<ItemSpawnPoint> availableSpawnPoints = CollectSpawnPoints(mansion);

        foreach (SpawnItemDefinition itemDefinition in itemsToSpawn)
        {
            if (!IsSpawnableDefinition(itemDefinition))
            {
                Debug.LogWarning($"Skipping invalid item definition '{GetItemLabel(itemDefinition)}'.");
                continue;
            }

            if (TrySelectSpawnPoint(itemDefinition, availableSpawnPoints, out ItemSpawnPoint chosenSpawnPoint))
            {
                SpawnItem(itemDefinition, chosenSpawnPoint);
                availableSpawnPoints.Remove(chosenSpawnPoint);
                continue;
            }

            Debug.LogWarning($"No valid spawn points found for item: {GetItemLabel(itemDefinition)}");
        }
    }

    private static List<SpawnItemDefinition> CollectRequestedItems(MansionModel mansion)
    {
        List<SpawnItemDefinition> itemsToSpawn = new();
        if (mansion.InitialSpawnItemDefinitions != null)
        {
            itemsToSpawn.AddRange(mansion.InitialSpawnItemDefinitions);
        }
        foreach (Room room in mansion.Rooms)
        {
            if (room == null)
            {
                continue;
            }

            foreach (SpawnItemDefinition itemDefinition in room.RequestedItems)
            {
                itemsToSpawn.Add(itemDefinition);
            }
        }

        return itemsToSpawn;
    }

    private static List<ItemSpawnPoint> CollectSpawnPoints(MansionModel mansion)
    {
        List<ItemSpawnPoint> spawnPoints = new();
        foreach (Room room in mansion.Rooms)
        {
            if (room == null)
            {
                continue;
            }

            ItemSpawnPoint[] roomSpawnPoints = room.GetItemSpawnPoints();
            spawnPoints.AddRange(roomSpawnPoints);
        }

        return spawnPoints;
    }

    private static bool TrySelectSpawnPoint(SpawnItemDefinition itemDefinition, List<ItemSpawnPoint> availableSpawnPoints, out ItemSpawnPoint chosenSpawnPoint)
    {
        List<ItemSpawnPoint> validSpawnPoints = availableSpawnPoints.FindAll(sp => sp != null && sp.CanSpawnItem(itemDefinition));
        if (validSpawnPoints.Count == 0)
        {
            chosenSpawnPoint = null;
            return false;
        }

        chosenSpawnPoint = validSpawnPoints[Random.Range(0, validSpawnPoints.Count)];
        return true;
    }

    private static bool IsSpawnableDefinition(SpawnItemDefinition itemDefinition)
    {
        return itemDefinition != null && itemDefinition.ItemDefinition != null && itemDefinition.ItemPrefab != null;
    }

    private static string GetItemLabel(SpawnItemDefinition itemDefinition)
    {
        if (itemDefinition == null)
        {
            return "<null definition>";
        }

        if (itemDefinition.ItemPrefab != null)
        {
            return itemDefinition.ItemPrefab.name;
        }

        if (itemDefinition.ItemDefinition != null)
        {
            return itemDefinition.ItemDefinition.name;
        }

        return itemDefinition.name;
    }

    private void SpawnItem(SpawnItemDefinition spawnDefinition, ItemSpawnPoint spawnPoint)
    {
        if (spawnDefinition == null || spawnPoint == null)
        {
            Debug.LogWarning("Cannot spawn item because definition or spawn point is missing.");
            return;
        }

        var instance = Instantiate(spawnDefinition.ItemPrefab, spawnPoint.transform.position, Quaternion.identity, itemParent);
        if (instance.TryGetComponent<ITargetable>(out var targetable))        {
            targetable.TargetID = spawnDefinition.targetID;
        }
        spawnPoint.MarkAsOccupied();
    }
}

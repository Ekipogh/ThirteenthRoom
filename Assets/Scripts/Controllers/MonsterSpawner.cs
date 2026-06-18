using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MonsterSpawner : MonoBehaviour
{
    [Header("Definitions")]
    [SerializeField] MonsterDefinition[] MonsterDefinitions;

    [Header("Scene References")]
    [SerializeField] RoomTracker RoomTracker;
    [SerializeField] PlayerAudioManager PlayerAudioManager;
    [SerializeField] GameManager GameManager;
    [SerializeField] Transform Floors;

    public void SpawnMonster(MansionModel mansion)
    {
        if (MonsterDefinitions.Length == 0)
        {
            Debug.LogWarning("No monster definitions assigned to the MonsterSpawner.");
            return;
        }
        var randomMonsterDef = MonsterDefinitions[Random.Range(0, MonsterDefinitions.Length)];
        var spawnableRooms = GetSpawnableRooms(randomMonsterDef.SpawnTags);
        if (spawnableRooms.Count == 0)
        {
            Debug.LogWarning("No spawnable rooms found for the monster with tags: " + string.Join(", ", randomMonsterDef.SpawnTags));
            return;
        }
        var randomRoom = spawnableRooms[Random.Range(0, spawnableRooms.Count)];
        // Instantiate the monster prefab in the selected room
        var monsterInstance = Instantiate(randomMonsterDef.MonsterPrefab, randomRoom.transform.position, Quaternion.identity);
        MonsterController monsterController = monsterInstance.GetComponent<MonsterController>();
        if (monsterController != null)
        {
            monsterController.StartingRoom = randomRoom;
            monsterController.Mansion = mansion;
            monsterController.RoomTracker = RoomTracker;
            monsterController.GameManager = GameManager;
            monsterController.PlayerAudioManager = PlayerAudioManager;
        }
        else
        {
            Debug.LogWarning("The monster prefab does not have a MonsterController component.");
        }
    }

    private List<Room> GetSpawnableRooms(string[] spawnTags)
    {
        var spawnableRooms = new List<Room>();
        foreach (Transform floor in Floors)
        {
            foreach (Transform roomTransform in floor)
            {
                var room = roomTransform.GetComponent<Room>();
                if (room != null && room.SpawnTags != null && spawnTags.Any(tag => room.SpawnTags.Contains(tag)))
                {
                    spawnableRooms.Add(room);
                }
            }
        }
        return spawnableRooms;
    }
}

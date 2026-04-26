using UnityEngine;

public class MonsterController : MonoBehaviour
{
    [SerializeField] Room StartingRoom;
    [SerializeField] RoomTracker RoomTracker;
    [SerializeField] float moveInterval = 5f;

    Room _currentRoom;

    void Start()
    {
        _currentRoom = StartingRoom;
        MoveVisualToRoom();
        StartCoroutine(Roam());
    }

    void ChooseNextRoom()
    {
        if (_currentRoom == null || _currentRoom.ConnectedRooms.Count == 0)
        {
            return;
        }
        int index = Random.Range(0, _currentRoom.ConnectedRooms.Count);
        _currentRoom = _currentRoom.ConnectedRooms[index];
        Debug.Log($"Monster moved to room: {_currentRoom.name}");
    }

    void MoveVisualToRoom()
    {
        if (_currentRoom.MonsterPoint != null)
        {
            transform.position = _currentRoom.MonsterPoint.position;
        }
    }

    void CheckIfInSameRoomAsPlayer()
    {
        if (RoomTracker.CurrentRoom == _currentRoom)
        {
            Debug.Log("The monster is in the same room as the player!");
        }
    }

    System.Collections.IEnumerator Roam()
    {
        while (true)
        {
            yield return new WaitForSeconds(moveInterval);
            ChooseNextRoom();
            MoveVisualToRoom();
            CheckIfInSameRoomAsPlayer();
        }
    }

    void Update()
    {
        CheckIfInSameRoomAsPlayer();
    }
}

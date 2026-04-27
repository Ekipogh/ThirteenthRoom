using UnityEngine;

public class MonsterController : MonoBehaviour
{
    [SerializeField] Room StartingRoom;
    [SerializeField] RoomTracker RoomTracker;
    [SerializeField] float moveInterval = 5f;

    [SerializeField] GameManager GameManager;

    GameState _currentGameState;

    Room _currentRoom;

    void Start()
    {
        _currentRoom = StartingRoom;
        _currentGameState = GameManager != null ? GameManager.currentState : GameState.Playing;
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
        if (_currentRoom != null && _currentRoom.MonsterPoint != null)
        {
            transform.position = _currentRoom.MonsterPoint.position;
        }
    }

    void CheckIfInSameRoomAsPlayer()
    {
        if (RoomTracker == null || _currentRoom == null || _currentGameState != GameState.Playing)
        {
            return;
        }

        if (RoomTracker.CurrentRoom == _currentRoom)
        {
            if (GameManager != null)
            {
                GameManager.OnPlayerDeath();
            }
        }
    }

    System.Collections.IEnumerator Roam()
    {
        while (_currentGameState == GameState.Playing)
        {
            yield return new WaitForSeconds(moveInterval);

            UpdateGameState();
            if (_currentGameState != GameState.Playing)
            {
                yield break;
            }

            ChooseNextRoom();
            MoveVisualToRoom();
        }
    }

    void UpdateGameState()
    {
        if (GameManager != null)
        {
            _currentGameState = GameManager.currentState;
        }
    }

    void Update()
    {
        UpdateGameState();
        CheckIfInSameRoomAsPlayer();
    }
}

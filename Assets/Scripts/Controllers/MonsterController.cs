using UnityEngine;

public enum MonsterState
{
    Roaming,
    EscapeWindow,
    Attacking
}

public class MonsterController : MonoBehaviour
{
    [SerializeField] Room StartingRoom;
    [SerializeField] RoomTracker RoomTracker;
    [SerializeField] float moveInterval = 5f;
    [SerializeField] float escapeWindowDuration = 2f;

    [SerializeField] GameManager GameManager;

    GameState _currentGameState;

    Room _currentRoom;
    MonsterState _currentState;
    Coroutine _roamCoroutine;
    Coroutine _escapeCoroutine;

    public Room CurrentRoom => _currentRoom;
    public MonsterState CurrentState => _currentState;

    void Start()
    {
        _currentRoom = StartingRoom;
        _currentState = MonsterState.Roaming;
        _currentGameState = GameManager != null ? GameManager.currentState : GameState.Playing;
        MoveVisualToRoom();
        _roamCoroutine = StartCoroutine(Roam());
        CheckIfInSameRoomAsPlayer();
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

        if (RoomTracker.CurrentRoom == _currentRoom && _currentState == MonsterState.Roaming)
        {
            StartAttackCountdown();
            return;
        }

        if (RoomTracker.CurrentRoom != _currentRoom && _currentState == MonsterState.EscapeWindow)
        {
            ResumeRoamingAfterEscape();
        }
    }

    void StartAttackCountdown()
    {
        _currentState = MonsterState.EscapeWindow;
        Debug.Log("Monster spotted player. Escape window started.");
        StopRoaming();
        StartEscapeWindow();
    }

    void ResumeRoamingAfterEscape()
    {
        _currentState = MonsterState.Roaming;
        Debug.Log("Player escaped room. Monster resumed roaming.");
        StopEscapeWindow();
        StartRoaming();
    }

    void StartRoaming()
    {
        if (_roamCoroutine == null && _currentState == MonsterState.Roaming)
        {
            _roamCoroutine = StartCoroutine(Roam());
        }
    }

    void StopRoaming()
    {
        if (_roamCoroutine != null)
        {
            StopCoroutine(_roamCoroutine);
            _roamCoroutine = null;
        }
    }

    void StartEscapeWindow()
    {
        if (_escapeCoroutine == null && _currentState == MonsterState.EscapeWindow)
        {
            _escapeCoroutine = StartCoroutine(HandleEscapeWindow());
        }
    }

    void StopEscapeWindow()
    {
        if (_escapeCoroutine != null)
        {
            StopCoroutine(_escapeCoroutine);
            _escapeCoroutine = null;
        }
    }

    System.Collections.IEnumerator Roam()
    {
        while (_currentGameState == GameState.Playing && _currentState == MonsterState.Roaming)
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

        _roamCoroutine = null;
    }

    System.Collections.IEnumerator HandleEscapeWindow()
    {
        yield return new WaitForSeconds(escapeWindowDuration);

        UpdateGameState();
        if (_currentGameState != GameState.Playing)
        {
            _escapeCoroutine = null;
            yield break;
        }

        if (RoomTracker != null && RoomTracker.CurrentRoom == _currentRoom && _currentState == MonsterState.EscapeWindow)
        {
            _currentState = MonsterState.Attacking;
            Debug.Log("Player failed to escape. Monster attacks.");
            if (GameManager != null)
            {
                GameManager.OnPlayerDeath();
            }
        }
        else if (_currentState == MonsterState.EscapeWindow)
        {
            _currentState = MonsterState.Roaming;
            StartRoaming();
        }

        _escapeCoroutine = null;
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
        GameState previousGameState = _currentGameState;
        UpdateGameState();

        if (_currentGameState != GameState.Playing)
        {
            StopRoaming();
            StopEscapeWindow();
            return;
        }

        if (previousGameState != GameState.Playing)
        {
            if (_currentState == MonsterState.Roaming)
            {
                StartRoaming();
            }
            else if (_currentState == MonsterState.EscapeWindow)
            {
                StartEscapeWindow();
            }
        }

        CheckIfInSameRoomAsPlayer();
    }
}

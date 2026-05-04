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
    [SerializeField] float escapeWindowDuration = 5f;
    [SerializeField] float travelTime = 2f;

    [SerializeField] GameManager GameManager;

    GameState _currentGameState;

    Room _currentRoom;
    MonsterState _currentState;
    Coroutine _roamCoroutine;
    Coroutine _escapeCoroutine;
    Renderer[] _renderers;
    Collider[] _colliders;
    bool _isTraveling;

    public Room CurrentRoom => _currentRoom;
    public MonsterState CurrentState => _currentState;

    void Awake()
    {
        _renderers = GetComponentsInChildren<Renderer>(true);
        _colliders = GetComponentsInChildren<Collider>(true);
    }

    void Start()
    {
        _currentRoom = StartingRoom;
        _currentState = MonsterState.Roaming;
        _currentGameState = GameManager != null ? GameManager.currentState : GameState.Playing;
        MoveVisualToRoom();
        SetMonsterVisible(true);
        _roamCoroutine = StartCoroutine(Roam());
        CheckIfInSameRoomAsPlayer();
    }

    bool TryGetNextRoom(out Room nextRoom)
    {
        nextRoom = null;
        if (_currentRoom == null || _currentRoom.ConnectedRooms.Count == 0)
        {
            return false;
        }

        int index = Random.Range(0, _currentRoom.ConnectedRooms.Count);
        nextRoom = _currentRoom.ConnectedRooms[index];
        return true;
    }

    void MoveVisualToRoom()
    {
        if (_currentRoom != null && _currentRoom.MonsterPoint != null)
        {
            transform.position = _currentRoom.MonsterPoint.position;
            float randomYRotation = Random.Range(0f, 360f);
            transform.rotation = Quaternion.Euler(0f, randomYRotation, 0f);
        }
    }

    void CheckIfInSameRoomAsPlayer()
    {
        if (RoomTracker == null || _currentRoom == null || _currentGameState != GameState.Playing || _isTraveling)
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
        LookAtPlayer();
        StartEscapeWindow();
    }

    void LookAtPlayer()
    {
        if (RoomTracker == null) return;
        Vector3 direction = RoomTracker.transform.position - transform.position;
        direction.y = 0f;
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
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

        if (_isTraveling)
        {
            _isTraveling = false;
            SetMonsterVisible(true);
        }
    }

    void SetMonsterVisible(bool isVisible)
    {
        if (_renderers != null)
        {
            foreach (Renderer rendererComponent in _renderers)
            {
                if (rendererComponent != null)
                {
                    rendererComponent.enabled = isVisible;
                }
            }
        }

        if (_colliders != null)
        {
            foreach (Collider colliderComponent in _colliders)
            {
                if (colliderComponent != null)
                {
                    colliderComponent.enabled = isVisible;
                }
            }
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

            yield return TravelToNextRoom();
        }

        _roamCoroutine = null;
    }

    System.Collections.IEnumerator TravelToNextRoom()
    {
        if (!TryGetNextRoom(out Room nextRoom))
        {
            yield break;
        }

        _isTraveling = true;
        SetMonsterVisible(false);

        float effectiveTravelTime = Mathf.Max(0f, travelTime);
        if (effectiveTravelTime > 0f)
        {
            yield return new WaitForSeconds(effectiveTravelTime);
        }

        UpdateGameState();
        if (_currentGameState != GameState.Playing || _currentState != MonsterState.Roaming)
        {
            _isTraveling = false;
            SetMonsterVisible(true);
            yield break;
        }

        _currentRoom = nextRoom;
        MoveVisualToRoom();
        SetMonsterVisible(true);
        _isTraveling = false;
        Debug.Log($"Monster moved to room: {_currentRoom.name}");
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

        if (_currentState == MonsterState.EscapeWindow || _currentState == MonsterState.Attacking)
        {
            LookAtPlayer();
        }
    }
}

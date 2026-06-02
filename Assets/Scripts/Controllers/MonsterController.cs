using UnityEngine;

public enum MonsterState
{
    Roaming,
    EscapeWindow,
    Attacking
}

public class MonsterController : MonoBehaviour
{
    public Room StartingRoom { get; set; }
    public MansionModel Mansion { get; set; }
    public RoomTracker RoomTracker { get; set; }
    [SerializeField] float moveInterval = 5f;
    [SerializeField] float escapeWindowDuration = 5f;
    [SerializeField] float travelSpeed = 1.5f;
    [SerializeField] float doorframeWaitTime = 1f;
    [SerializeField] string doorframeEmoteTrigger;

    public GameManager GameManager { get; set; }
    public PlayerAudioManager PlayerAudioManager { get; set; }
    [SerializeField] MonsterAudioManager MonsterAudioManager;

    GameState _currentGameState;

    Room _currentRoom;
    MonsterState _currentState;
    Coroutine _roamCoroutine;
    Coroutine _escapeCoroutine;
    Renderer[] _renderers;
    Collider[] _colliders;
    Animator _animator;
    bool _isTraveling;

    public Room CurrentRoom => _currentRoom;
    public MonsterState CurrentState => _currentState;

    float _lightSwitchFlipChance = 0.3f;

    void Awake()
    {
        _renderers = GetComponentsInChildren<Renderer>(true);
        _colliders = GetComponentsInChildren<Collider>(true);
        _animator = GetComponentInChildren<Animator>();
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

    bool TryGetNextConnection(out RoomConnection connection)
    {
        connection = null;
        if (_currentRoom == null)
        {
            return false;
        }

        if (Mansion != null)
        {
            connection = Mansion.GetRandomConnection(_currentRoom);
            return connection != null;
        }

        if (_currentRoom.ConnectedRooms.Count == 0)
        {
            return false;
        }

        int index = Random.Range(0, _currentRoom.ConnectedRooms.Count);
        Room nextRoom = _currentRoom.ConnectedRooms[index];
        connection = new RoomConnection(
            _currentRoom,
            nextRoom,
            RoomDirection.North,
            (_currentRoom.transform.position + nextRoom.transform.position) / 2f);
        return connection.To != null;
    }

    void MoveVisualToRoom()
    {
        if (_currentRoom != null)
        {
            transform.position = GetMonsterPoint(_currentRoom);
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
        SetMonsterEncounterAudioActive(true);
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
        SetMonsterEncounterAudioActive(false);
        StopEscapeWindow();
        StartRoaming();
    }

    void SetMonsterEncounterAudioActive(bool isActive)
    {
        if (PlayerAudioManager != null)
        {
            PlayerAudioManager.SetMonsterEncounterActive(isActive);
        }
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
        if (!TryGetNextConnection(out RoomConnection connection))
        {
            yield break;
        }

        _isTraveling = true;
        MonsterAudioManager?.ResetFootstepDistance();

        yield return MoveToPosition(connection.GetApproachPoint(_currentRoom), travelSpeed);

        UpdateGameState();
        if (_currentGameState != GameState.Playing || _currentState != MonsterState.Roaming)
        {
            _isTraveling = false;
            yield break;
        }

        yield return WaitAtDoorframe();

        UpdateGameState();
        if (_currentGameState != GameState.Playing || _currentState != MonsterState.Roaming)
        {
            _isTraveling = false;
            yield break;
        }

        _currentRoom = connection.To;
        yield return MoveToPosition(GetMonsterPoint(_currentRoom), travelSpeed);

        UpdateGameState();
        if (_currentGameState != GameState.Playing || _currentState != MonsterState.Roaming)
        {
            _isTraveling = false;
            yield break;
        }

        FlipTheSwitch();
        _isTraveling = false;
    }

    System.Collections.IEnumerator WaitAtDoorframe()
    {
        PlayDoorframeEmote();

        float effectiveWaitTime = Mathf.Max(0f, doorframeWaitTime);
        float elapsed = 0f;
        while (elapsed < effectiveWaitTime)
        {
            UpdateGameState();
            if (_currentGameState != GameState.Playing || _currentState != MonsterState.Roaming)
            {
                yield break;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    void PlayDoorframeEmote()
    {
        if (_animator == null || string.IsNullOrWhiteSpace(doorframeEmoteTrigger))
        {
            return;
        }

        _animator.SetTrigger(doorframeEmoteTrigger);
    }

    System.Collections.IEnumerator MoveToPosition(Vector3 targetPosition, float speed)
    {
        Vector3 startPosition = transform.position;
        float distance = Vector3.Distance(startPosition, targetPosition);
        float effectiveSpeed = Mathf.Max(0f, speed);

        if (effectiveSpeed <= 0f || distance <= 0f)
        {
            transform.position = targetPosition;
            FaceTravelDirection(targetPosition - startPosition);
            yield break;
        }

        float duration = distance / effectiveSpeed;
        float elapsed = 0f;
        FaceTravelDirection(targetPosition - startPosition);
        Vector3 previousPosition = transform.position;

        while (elapsed < duration)
        {
            UpdateGameState();
            if (_currentGameState != GameState.Playing || _currentState != MonsterState.Roaming)
            {
                yield break;
            }

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            PlayFootstepForMovement(previousPosition, transform.position);
            previousPosition = transform.position;
            yield return null;
        }

        transform.position = targetPosition;
        PlayFootstepForMovement(previousPosition, transform.position);
    }

    void PlayFootstepForMovement(Vector3 previousPosition, Vector3 currentPosition)
    {
        if (MonsterAudioManager == null)
        {
            return;
        }

        previousPosition.y = 0f;
        currentPosition.y = 0f;
        MonsterAudioManager.PlayFootstepForDistance(Vector3.Distance(previousPosition, currentPosition));
    }

    Vector3 GetMonsterPoint(Room room)
    {
        if (room == null)
        {
            return transform.position;
        }

        return room.MonsterPoint != null
            ? room.MonsterPoint.transform.position
            : room.transform.position;
    }

    void FaceTravelDirection(Vector3 direction)
    {
        direction.y = 0f;
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
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
            SetMonsterEncounterAudioActive(false);
            if (GameManager != null)
            {
                GameManager.OnPlayerDeath();
            }
        }
        else if (_currentState == MonsterState.EscapeWindow)
        {
            _currentState = MonsterState.Roaming;
            SetMonsterEncounterAudioActive(false);
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
            if (_currentState == MonsterState.EscapeWindow)
            {
                SetMonsterEncounterAudioActive(false);
            }
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

    void FlipTheSwitch()
    {
        if (Random.value < _lightSwitchFlipChance)
        {
            LightSwitchInteractable lightSwitch = _currentRoom.GetLightSwitch();
            if (lightSwitch != null)            {
                lightSwitch.SwitchLight(false);
            }
        }
    }
}

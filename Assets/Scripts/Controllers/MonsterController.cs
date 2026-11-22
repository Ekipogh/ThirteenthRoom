using UnityEngine;
using UnityEngine.AI;

public enum MonsterState { Roaming, Chasing, Searching, Attacking, Investigating }

[RequireComponent(typeof(NavMeshAgent))]
public class MonsterController : MonoBehaviour
{
    public MonsterDefinition Definition { get; set; }
    readonly MonsterAggressionProfile _fallbackProfile = new MonsterAggressionProfile();
    readonly MonsterAggression _fallbackAggression = new MonsterAggression();
    MonsterAggressionProfile Profile => Definition != null && Definition.Aggression != null ? Definition.Aggression : _fallbackProfile;
    MonsterAggression Aggression => GameManager != null ? GameManager.Aggression : _fallbackAggression;
    public float CurrentAggression => Aggression.GetLevel(Profile);
    public bool IsMaximumHunt => Aggression.IsMaximumHunt(Profile);
    float TrackingDuration => Mathf.Lerp(Profile.TrackingSeconds.x, Profile.TrackingSeconds.y, CurrentAggression / 99f);
    float SearchDuration => Mathf.Lerp(Profile.SearchSeconds.x, Profile.SearchSeconds.y, CurrentAggression / 99f);
    float _concealedFor, _distantConcealedFor;
    bool _wasMaximumHunt;

    public Room StartingRoom { get; set; }
    public MansionModel Mansion { get; set; }
    public RoomTracker RoomTracker { get; set; }
    GameManager _gameManager;
    public GameManager GameManager
    {
        get => _gameManager;
        set
        {
            if (_gameManager == value) return;
            if (_gameManager != null) _gameManager.UnregisterMonster(this);
            ClearInvestigation();
            _gameManager = value;
            if (_gameManager != null && isActiveAndEnabled) _gameManager.RegisterMonster(this);
        }
    }
    Vector3 _agitationOrigin;
    float _agitationRemaining, _agitationStrength;
    bool _agitationReached;
    public Vector3 InvestigationOrigin => _agitationOrigin;
    public float InvestigationSecondsRemaining => _agitationRemaining;

    void OnEnable()
    {
        if (GameManager != null) GameManager.RegisterMonster(this);
    }

    public void ReceiveAgitation(Vector3 origin, AgitationProfile profile)
    {
        if (!isActiveAndEnabled || !Playing || profile == null || !profile.IsValid || IsMaximumHunt ||
            CurrentState == MonsterState.Chasing || CurrentState == MonsterState.Attacking ||
            !AgitationProfile.Finite(origin.x) || !AgitationProfile.Finite(origin.y) || !AgitationProfile.Finite(origin.z) ||
            Vector3.Distance(transform.position, origin) > profile.HearingRadius) return;
        if (CurrentState == MonsterState.Investigating && _agitationRemaining > 0f && profile.Strength < _agitationStrength) return;
        if (_agent == null || !_agent.isOnNavMesh) return;
        ChangeState(MonsterState.Investigating);
        _agitationOrigin = origin;
        _agitationRemaining = profile.InvestigationSeconds;
        _agitationStrength = profile.Strength;
        _agitationReached = false;
        _waitingDoor = null;
        _nextDestination = 0f;
        _agent.ResetPath();
        _agent.isStopped = false;
    }

    public void ClearInvestigation()
    {
        _agitationRemaining = _agitationStrength = 0f;
        _agitationOrigin = default;
        _agitationReached = false;
        if (CurrentState == MonsterState.Investigating) ChangeState(MonsterState.Roaming);
    }

    void AdvanceInvestigation(float deltaTime)
    {
        if (!Playing || CurrentState != MonsterState.Investigating) return;
        _agitationRemaining = Mathf.Max(0f, _agitationRemaining - Mathf.Max(0f, deltaTime));
        if (_agitationRemaining <= 0f) ClearInvestigation();
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (!Application.isPlaying || !isActiveAndEnabled ||
            CurrentState != MonsterState.Investigating || _agitationRemaining <= 0f) return;

        Color previousColor = Gizmos.color;
        Gizmos.color = new Color(1f, 0.55f, 0.1f);
        Gizmos.DrawWireSphere(_agitationOrigin, 0.75f);
        Gizmos.DrawLine(_agitationOrigin - Vector3.right, _agitationOrigin + Vector3.right);
        Gizmos.DrawLine(_agitationOrigin - Vector3.forward, _agitationOrigin + Vector3.forward);
        Gizmos.DrawLine(_agitationOrigin, _agitationOrigin + Vector3.up * 2f);
        Gizmos.DrawLine(transform.position, _agitationOrigin);
        Gizmos.color = previousColor;
        UnityEditor.Handles.Label(_agitationOrigin + Vector3.up * 2.2f,
            $"Agitation | strength {_agitationStrength:0.##} | {_agitationRemaining:0.0}s remaining");
    }
#endif
    void Investigate()
    {
        if (!_agitationReached)
        {
            if (Vector3.Distance(transform.position, _agitationOrigin) < 1f)
            {
                _agitationReached = true;
                _agent.ResetPath();
            }
            else NavigateTo(_agitationOrigin, MansionNavigation.FindRoom(Mansion, _agitationOrigin));
        }
        else SearchNearby(_agitationOrigin);
    }

    public PlayerAudioManager PlayerAudioManager { get; set; }

    [Header("Roaming")]
    [SerializeField] float moveInterval = 5f;
    [SerializeField] float travelSpeed = 1.5f;
    [SerializeField] float doorframeWaitTime = 1f;
    [SerializeField] string doorframeEmoteTrigger;
    [Header("Perception")]
    [SerializeField] float viewRange = 25f;
    [SerializeField, Range(0f, 360f)] float fieldOfView = 160f;
    [SerializeField, Min(0f)] float closeAwarenessRange = 8f;
    [SerializeField] float eyeHeight = 4.8f;
    [SerializeField] LayerMask sightMask = ~0;
    [Header("Pursuit")]
    [SerializeField] float chaseSpeed = 3f;
    [SerializeField] float destinationRefresh = 0.2f;
    [Header("Attack")]
    [SerializeField] float attackReach = 1.5f;
    [SerializeField] float attackImpactDelay = 0.5f;
    [SerializeField] float attackDuration = 1f;
    [Header("Audio")]
    [SerializeField] MonsterAudioManager MonsterAudioManager;
    [SerializeField, Min(0f)] float alertCooldown = 15f;
    float _alertCooldownRemaining;

    NavMeshAgent _agent;
    Animator _animator;
    Room _currentRoom;
    Room _roamRoom;
    Vector3 _roamTarget, _lastSeen, _previousPosition, _progressPosition;
    float _nextDestination, _roamAfter, _searchUntil, _searchTravelUntil, _attackStarted, _nextProgressCheck;
    bool _impactApplied, _hasRoamTarget, _searchReached;
    DoorInteractable _waitingDoor;
    float _doorWaitUntil;
    public Room CurrentRoom => _currentRoom;
    public MonsterState CurrentState { get; private set; }
    public bool IsGameplayActive => Playing;
    public float MoveSpeed => _agent != null && _agent.isOnNavMesh ? _agent.velocity.magnitude : 0f;

    void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _animator = GetComponentInChildren<Animator>();
        if (_animator != null) _animator.applyRootMotion = false;
        foreach (Rigidbody body in GetComponentsInChildren<Rigidbody>()) body.isKinematic = true;
    }

    void Start()
    {
        _currentRoom = StartingRoom;
        _previousPosition = _progressPosition = transform.position;
        CurrentState = MonsterState.Roaming;
        _roamAfter = Time.time + moveInterval;
        if (!_agent.isOnNavMesh)
        {
            Debug.LogError("Monster was spawned outside the mansion NavMesh; navigation disabled.", this);
            enabled = false;
        }
    }

    bool Playing => GameManager == null || GameManager.currentState == GameState.Playing;
    Vector3 PlayerPosition => RoomTracker.transform.position;

    public bool CanSeePlayer(bool requireFieldOfView = true)
    {
        if (RoomTracker == null) return false;
        Vector3 eye = transform.position + Vector3.up * eyeHeight;
        Bounds bounds = PlayerBounds();
        Vector3 direction = bounds.center - eye;
        if (direction.sqrMagnitude > viewRange * viewRange) return false;
        Vector3 horizontal = Vector3.ProjectOnPlane(direction, Vector3.up);
        if (requireFieldOfView && Vector3.Angle(transform.forward, horizontal) > fieldOfView * 0.5f) return false;
        Vector3 side = Vector3.Cross(Vector3.up, horizontal.normalized);
        float radius = Mathf.Min(bounds.extents.x, bounds.extents.z) * 0.8f;
        return ClearSight(eye, bounds.center + Vector3.up * bounds.extents.y * 0.8f)
            || ClearSight(eye, bounds.center)
            || ClearSight(eye, bounds.center + side * radius)
            || ClearSight(eye, bounds.center - side * radius);
    }

    Bounds PlayerBounds()
    {
        CharacterController body = RoomTracker.GetComponent<CharacterController>();
        return body != null ? body.bounds : new Bounds(PlayerPosition + Vector3.up * eyeHeight * 0.5f,
            new Vector3(1f, eyeHeight, 1f));
    }

    bool ClearSight(Vector3 eye, Vector3 target)
    {
        Vector3 direction = target - eye;
        foreach (RaycastHit hit in Physics.RaycastAll(eye, direction.normalized, direction.magnitude, sightMask, QueryTriggerInteraction.Ignore))
        {
            if (hit.transform.IsChildOf(transform) || hit.transform == transform) continue;
            if (hit.transform == RoomTracker.transform || hit.transform.IsChildOf(RoomTracker.transform)) continue;
            return false;
        }
        return true;
    }

    public bool CanDetectPlayer()
    {
        if (RoomTracker == null) return false;
        // Nearby players can be noticed from any direction. Once chasing,
        // turning along a path must not make the monster forget a visible target.
        bool nearby = (PlayerPosition - transform.position).sqrMagnitude <= closeAwarenessRange * closeAwarenessRange;
        return CanSeePlayer(!nearby && CurrentState != MonsterState.Chasing);
    }

    public bool CanAttackPlayer()
    {
        if (RoomTracker == null || Mathf.Abs(PlayerPosition.y - transform.position.y) > 1.25f) return false;
        Vector3 delta = Vector3.ProjectOnPlane(PlayerPosition - transform.position, Vector3.up);
        CharacterController body = RoomTracker.GetComponent<CharacterController>();
        float reach = attackReach + (body != null ? body.radius * RoomTracker.transform.lossyScale.x : 0f);
        if (delta.sqrMagnitude > reach * reach) return false;
        Bounds bounds = PlayerBounds();
        Vector3 eye = transform.position + Vector3.up * eyeHeight;
        return ClearSight(eye, bounds.center) || ClearSight(eye, bounds.center + Vector3.up * bounds.extents.y * 0.8f);
    }

    void ChangeState(MonsterState state)
    {
        if (CurrentState == state) return;
        if (state == MonsterState.Chasing && (CurrentState == MonsterState.Roaming || CurrentState == MonsterState.Investigating) &&
            _alertCooldownRemaining <= 0f)
        {
            MonsterAudioManager?.PlayAlertSound();
            _alertCooldownRemaining = Mathf.Max(0f, alertCooldown);
        }
        if (state != MonsterState.Investigating)
        {
            _agitationRemaining = _agitationStrength = 0f;
            _agitationOrigin = default;
            _agitationReached = false;
        }
        CurrentState = state;
        UpdateVocalAudio();
        _nextDestination = 0f;
        _waitingDoor = null;
        if (_agent != null && _agent.isOnNavMesh) { _agent.ResetPath(); _agent.isStopped = false; }
        PlayerAudioManager?.SetMonsterEncounterActive(state == MonsterState.Chasing || state == MonsterState.Searching || state == MonsterState.Attacking);
        if (state == MonsterState.Searching)
        {
            _searchUntil = float.PositiveInfinity;
            _searchTravelUntil = Time.time + Mathf.Max(10f, SearchDuration * 5f);
            _searchReached = false;
        }
        if (state == MonsterState.Roaming)
        {
            _hasRoamTarget = false;
            _roamAfter = Time.time + moveInterval;
        }
        if (state == MonsterState.Attacking)
        {
            _attackStarted = Time.time;
            _impactApplied = false;
            _agent.isStopped = true;
        }
    }

    void Update()
    {
        if (Playing && GameManager == null) _fallbackAggression.Advance(Time.deltaTime);
        if (Playing) _alertCooldownRemaining = Mathf.Max(0f, _alertCooldownRemaining - Time.deltaTime);
        UpdateVocalAudio();
        if (_agent == null || !_agent.isOnNavMesh) return;
        if (!Playing)
        {
            _agent.isStopped = true;
            PlayerAudioManager?.SetMonsterEncounterActive(false);
            _previousPosition = transform.position;
            _attackStarted += Time.deltaTime;
            _searchUntil += Time.deltaTime;
            _searchTravelUntil += Time.deltaTime;
            _doorWaitUntil += Time.deltaTime;
            _roamAfter += Time.deltaTime;
            return;
        }
        _agent.isStopped = false;
        PlayerAudioManager?.SetMonsterEncounterActive(CurrentState == MonsterState.Chasing || CurrentState == MonsterState.Searching || CurrentState == MonsterState.Attacking);
        _currentRoom = MansionNavigation.FindRoom(Mansion, transform.position) ?? _currentRoom;
        MonsterAudioManager?.PlayFootstepForDistance(Vector3.ProjectOnPlane(transform.position - _previousPosition, Vector3.up).magnitude);
        _previousPosition = transform.position;

        if (CurrentState == MonsterState.Attacking)
        {
            _agent.isStopped = true;
            FacePlayer();
            if (!_impactApplied && Time.time - _attackStarted >= attackImpactDelay)
            {
                _impactApplied = true;
                if (CanAttackPlayer()) GameManager?.OnPlayerDeath();
            }
            if (Time.time - _attackStarted >= Mathf.Max(attackImpactDelay, attackDuration))
                ChangeState(MonsterState.Chasing);
            return;
        }

        UpdatePursuit(Time.deltaTime);
        AdvanceInvestigation(Time.deltaTime);

        if (CurrentState == MonsterState.Chasing && CanAttackPlayer())
        {
            ChangeState(MonsterState.Attacking);
            return;
        }
        _agent.speed = CurrentState == MonsterState.Chasing ? chaseSpeed : travelSpeed;
        if (HandleDoorWait()) return;
        if (Time.time < _nextDestination) return;
        _nextDestination = Time.time + Mathf.Max(0.05f, destinationRefresh);

        if (CurrentState == MonsterState.Roaming) Roam();
        else if (CurrentState == MonsterState.Chasing) NavigateTo(_lastSeen, MansionNavigation.FindRoom(Mansion, _lastSeen));
        else if (CurrentState == MonsterState.Investigating) Investigate();
        else Search();
        CheckProgress();
    }

    void UpdatePursuit(float deltaTime)
    {
        if (RoomTracker == null) return;
        bool maximum = IsMaximumHunt;
        if (_wasMaximumHunt && !maximum) _concealedFor = _distantConcealedFor = 0f;
        _wasMaximumHunt = maximum;
        bool visible = CanDetectPlayer();
        if (maximum || visible)
        {
            _concealedFor = _distantConcealedFor = 0f;
            _lastSeen = PlayerPosition;
            ChangeState(MonsterState.Chasing);
        }
        else if (CurrentState == MonsterState.Chasing)
        {
            _concealedFor += Mathf.Max(0f, deltaTime);
            bool distant = (PlayerPosition - transform.position).sqrMagnitude > Profile.EscapeDistance * Profile.EscapeDistance;
            _distantConcealedFor = distant ? _distantConcealedFor + Mathf.Max(0f, deltaTime) : 0f;
            if (_concealedFor >= TrackingDuration || (distant && _distantConcealedFor >= Profile.DistantConcealmentSeconds))
                ChangeState(MonsterState.Searching);
            else _lastSeen = PlayerPosition;
        }
    }

    bool HandleDoorWait()
    {
        if (_waitingDoor == null) return false;
        if (!_waitingDoor.IsPassable)
        {
            _waitingDoor.OpenFrom(transform.position);
            _agent.isStopped = true;
            if (Time.time > _doorWaitUntil + 5f)
            {
                _waitingDoor = null;
                _hasRoamTarget = false;
                _roamAfter = Time.time + moveInterval;
            }
            return true;
        }
        if (Time.time < _doorWaitUntil) { _agent.isStopped = true; return true; }
        _waitingDoor = null;
        return false;
    }

    bool NavigateTo(Vector3 target, Room targetRoom)
    {
        RoomConnection connection = MansionNavigation.NextConnection(Mansion, _currentRoom, targetRoom);
        if (connection != null && connection.Door != null && !connection.Door.IsPassable)
        {
            Vector3 inward = Vector3.ProjectOnPlane(_currentRoom.transform.position - connection.DoorwayPoint, Vector3.up).normalized;
            Vector3 approach = connection.DoorwayPoint + inward * 2f;
            if (Vector3.Distance(transform.position, approach) <= 2f)
            {
                _waitingDoor = connection.Door;
                _waitingDoor.OpenFrom(transform.position);
                _doorWaitUntil = Time.time + Mathf.Max(0f, doorframeWaitTime);
                _agent.ResetPath();
                _agent.isStopped = true;
                if (_animator != null && !string.IsNullOrWhiteSpace(doorframeEmoteTrigger)) _animator.SetTrigger(doorframeEmoteTrigger);
                return true;
            }
            return SetDestination(approach);
        }
        // Advance through the graph one room at a time, so a closed door farther
        // along a pursuit never makes the entire route appear unreachable.
        if (connection != null)
        {
            Vector3 inward = Vector3.ProjectOnPlane(connection.To.transform.position - connection.DoorwayPoint, Vector3.up).normalized;
            target = connection.DirectionFromFromToTo == RoomDirection.Up || connection.DirectionFromFromToTo == RoomDirection.Down
                ? GetRoomPoint(connection.To) : connection.DoorwayPoint + inward * 3f;
        }
        return SetDestination(target);
    }

    bool SetDestination(Vector3 target)
    {
        if (!MansionNavigation.TrySample(target, out Vector3 sampled, 3f)) { _agent.ResetPath(); return false; }
        var path = new NavMeshPath();
        if (!_agent.CalculatePath(sampled, path) || path.status != NavMeshPathStatus.PathComplete) { _agent.ResetPath(); return false; }
        _agent.stoppingDistance = 0.25f;
        return _agent.SetPath(path);
    }

    static Vector3 GetRoomPoint(Room room) => MansionNavigation.TryRoomPoint(room, out Vector3 point) ? point : room.transform.position;

    bool TryRoamPoint(Room room, out Vector3 point)
    {
        Vector3 origin = GetRoomPoint(room);
        for (int i = 0; i < 12; i++)
        {
            Vector2 offset = Random.insideUnitCircle * 8f;
            if (MansionNavigation.TrySample(origin + new Vector3(offset.x, 0f, offset.y), out point)
                && MansionNavigation.FindRoom(Mansion, point) == room) return true;
        }
        return MansionNavigation.TrySample(origin, out point, 4f);
    }

    void Roam()
    {
        if (Time.time < _roamAfter || _currentRoom == null) return;
        if (!_hasRoamTarget)
        {
            var connections = Mansion?.GetConnections(_currentRoom);
            _roamRoom = connections != null && connections.Count > 0 && Random.value < 0.75f
                ? connections[Random.Range(0, connections.Count)].To : _currentRoom;
            _hasRoamTarget = TryRoamPoint(_roamRoom, out _roamTarget);
            if (!_hasRoamTarget) { _roamAfter = Time.time + moveInterval; return; }
        }
        if (_currentRoom == _roamRoom && Vector3.Distance(transform.position, _roamTarget) < 0.8f)
        {
            _hasRoamTarget = false;
            _agent.ResetPath();
            _roamAfter = Time.time + moveInterval;
            if (Random.value < 0.3f) _currentRoom.GetLightSwitch()?.SwitchLight(false);
        }
        else if (!NavigateTo(_roamTarget, _roamRoom))
        {
            _hasRoamTarget = false;
            _roamAfter = Time.time + 1f;
        }
    }

    void Search()
    {
        if (Time.time >= _searchUntil || (!_searchReached && Time.time >= _searchTravelUntil)) { ChangeState(MonsterState.Roaming); return; }
        if (!_searchReached)
        {
            if (Vector3.Distance(transform.position, _lastSeen) < 1f)
            {
                _searchReached = true;
                _searchUntil = Time.time + SearchDuration;
                _agent.ResetPath();
            }
            else NavigateTo(_lastSeen, MansionNavigation.FindRoom(Mansion, _lastSeen));
        }
        else SearchNearby(_lastSeen);
    }

    void SearchNearby(Vector3 origin)
    {
        if (!_agent.hasPath || _agent.remainingDistance < 0.5f)
        {
            Vector2 offset = Random.insideUnitCircle * 3f;
            SetDestination(origin + new Vector3(offset.x, 0f, offset.y));
        }
    }

    void CheckProgress()
    {
        if (Time.time < _nextProgressCheck) return;
        _nextProgressCheck = Time.time + 3f;
        if (_agent.hasPath && Vector3.Distance(_progressPosition, transform.position) < 0.1f)
        {
            _agent.ResetPath();
            _hasRoamTarget = false;
            _roamAfter = Time.time + 1f;
        }
        _progressPosition = transform.position;
    }

    void FacePlayer()
    {
        if (RoomTracker == null) return;
        Vector3 direction = Vector3.ProjectOnPlane(PlayerPosition - transform.position, Vector3.up);
        if (direction.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(direction);
    }

    void OnDisable()
    {
        if (GameManager != null) GameManager.UnregisterMonster(this);
        ClearInvestigation();
        MonsterAudioManager?.StopVocals();
        if (_agent != null && _agent.isOnNavMesh) { _agent.isStopped = true; _agent.ResetPath(); }
        PlayerAudioManager?.SetMonsterEncounterActive(false);
    }

    void UpdateVocalAudio()
    {
        MonsterAudioManager?.UpdateVocalState(CurrentState,
            GameManager != null ? GameManager.currentState : GameState.Playing);
    }
}

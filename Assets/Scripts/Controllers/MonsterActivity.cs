using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class MonsterActivityProfile
{
    public Vector2 PatrolSpeed = new Vector2(3f, 5f);
    public Vector2 InvestigationSpeed = new Vector2(4f, 6f);
    public Vector2 ChaseSpeed = new Vector2(6f, 8.5f);
    public Vector2 PatrolPauseSeconds = new Vector2(2f, 0.25f);
    [Tooltip("Multiplier at aggression 0 and 100. Starting aggression 20 hears sprinting within 30 units.")]
    public Vector2 HearingRadiusMultiplier = new Vector2(0.1f, 1.6f);
    public Vector2 NoiseStrengthMultiplier = new Vector2(0.125f, 2f);
    public Vector2 InvestigationDurationMultiplier = new Vector2(0.125f, 2f);
    [Min(0f)] public float NoiseBonusPerStrength = 10f;
    [Min(0f)] public float MaximumNoiseBonus = 30f;
    [Min(0f)] public float NoiseDecayPerSecond = 2f;
    [Min(1)] public int DestinationsBeforeFloorChange = 3;
    [Min(0f)] public float FailedDestinationSeconds = 15f;

    // Resolve each event from aggression, never from its own agitation bonus.
    // Shared action profiles remain unchanged; heard strength/duration are snapshots.
    public float HearingRadius(float baseRadius, float aggression) => Mathf.Max(0f, baseRadius * Mathf.Lerp(HearingRadiusMultiplier.x, HearingRadiusMultiplier.y, aggression / 100f));
    public float NoiseStrength(float baseStrength, float aggression) => Mathf.Max(0.01f, baseStrength * Mathf.Lerp(NoiseStrengthMultiplier.x, NoiseStrengthMultiplier.y, aggression / 100f));
    public float InvestigationDuration(float baseSeconds, float aggression) => Mathf.Max(0.01f, baseSeconds * Mathf.Lerp(InvestigationDurationMultiplier.x, InvestigationDurationMultiplier.y, aggression / 100f));

    public float Speed(MonsterState state, float agitation)
    {
        Vector2 range = state == MonsterState.Chasing ? ChaseSpeed
            : state == MonsterState.Investigating || state == MonsterState.Searching ? InvestigationSpeed : PatrolSpeed;
        return Mathf.Max(0f, Mathf.Lerp(range.x, range.y, agitation / 100f));
    }
    public float Pause(float agitation) => Mathf.Max(0f, Mathf.Lerp(PatrolPauseSeconds.x, PatrolPauseSeconds.y, agitation / 100f));
}

/// <summary>Per-monster exploration memory. Never reads the player's location.</summary>
public sealed class MonsterPatrolMemory
{
    readonly Dictionary<Room, long> _roomVisits = new Dictionary<Room, long>();
    readonly Dictionary<int, long> _floorVisits = new Dictionary<int, long>();
    readonly Dictionary<Room, float> _failedUntil = new Dictionary<Room, float>();
    long _visitSequence;
    Room _lastRoom;
    int _completedFloor, _completedOnFloor;

    static int Floor(Room room) => Mathf.RoundToInt(room.transform.position.y / Room.RoomSize.y);
    public void Visit(Room room)
    {
        if (room == null || room == _lastRoom) return;
        _lastRoom = room;
        _roomVisits[room] = ++_visitSequence;
        _floorVisits[Floor(room)] = _visitSequence;
    }

    public void Complete(Room room)
    {
        if (room == null) return;
        int floor = Floor(room);
        _completedOnFloor = _completedOnFloor > 0 && floor == _completedFloor ? _completedOnFloor + 1 : 1;
        _completedFloor = floor;
    }

    public void Fail(Room room, float now, float seconds)
    {
        if (room != null) _failedUntil[room] = now + Mathf.Max(0f, seconds);
    }

    public Room Choose(MansionModel mansion, Room current, float now, int floorChangeAfter)
    {
        if (mansion == null || current == null) return null;
        var reachable = new HashSet<Room> { current };
        var queue = new Queue<Room>();
        var candidates = new List<Room>();
        queue.Enqueue(current);
        while (queue.Count > 0)
        {
            Room room = queue.Dequeue();
            foreach (RoomConnection connection in mansion.GetConnections(room))
            {
                Room next = connection.To;
                if (next == null || !reachable.Add(next)) continue;
                queue.Enqueue(next);
                if (!_failedUntil.TryGetValue(next, out float until) || now >= until) candidates.Add(next);
            }
        }

        int preferredFloor = Floor(current);
        bool forceFloor = false;
        if (_completedOnFloor >= Mathf.Max(1, floorChangeAfter) && _completedFloor == preferredFloor)
        {
            long oldestFloor = long.MaxValue;
            var floors = new List<int>();
            foreach (Room room in candidates)
            {
                int floor = Floor(room);
                if (floor == Floor(current) || floors.Contains(floor)) continue;
                long visit = _floorVisits.TryGetValue(floor, out long seen) ? seen : 0;
                if (visit < oldestFloor) { floors.Clear(); oldestFloor = visit; }
                if (visit == oldestFloor) floors.Add(floor);
            }
            if (floors.Count > 0) { preferredFloor = floors[UnityEngine.Random.Range(0, floors.Count)]; forceFloor = true; }
        }

        long oldestRoom = long.MaxValue;
        var choices = new List<Room>();
        foreach (Room room in candidates)
        {
            if (forceFloor && Floor(room) != preferredFloor) continue;
            long visit = _roomVisits.TryGetValue(room, out long seen) ? seen : 0;
            if (visit < oldestRoom) { choices.Clear(); oldestRoom = visit; }
            if (visit == oldestRoom) choices.Add(room);
        }
        return choices.Count > 0 ? choices[UnityEngine.Random.Range(0, choices.Count)] : null;
    }

    public void Reset()
    {
        _roomVisits.Clear(); _floorVisits.Clear(); _failedUntil.Clear();
        _lastRoom = null; _visitSequence = 0; _completedOnFloor = 0;
    }
}

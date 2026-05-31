using System.Collections.Generic;
using UnityEngine;

public class RoomConnection
{
    public Room From { get; }
    public Room To { get; }
    public RoomDirection DirectionFromFromToTo { get; }
    public Vector3 DoorwayPoint { get; }
    public DoorInteractable Door { get; }
    public RoomConnection Reverse { get; private set; }

    public RoomConnection(Room from, Room to, RoomDirection directionFromFromToTo, Vector3 doorwayPoint, DoorInteractable door = null)
    {
        From = from;
        To = to;
        DirectionFromFromToTo = directionFromFromToTo;
        DoorwayPoint = doorwayPoint;
        Door = door;
    }

    public void SetReverse(RoomConnection reverse)
    {
        Reverse = reverse;
    }

    public Room GetOther(Room room)
    {
        if (room == From) return To;
        if (room == To) return From;
        return null;
    }

    public bool Contains(Room room)
    {
        return room == From || room == To;
    }

    public bool TryGetOther(Room room, out Room other)
    {
        other = GetOther(room);
        return other != null;
    }

    public Vector3 GetApproachPoint(Room room)
    {
        if (room == From || room == To)
        {
            return DoorwayPoint;
        }

        return Vector3.zero;
    }
}

public class MansionModel
{
    readonly List<Room> _rooms;
    readonly List<RoomConnection> _connections;
    readonly Dictionary<Room, List<RoomConnection>> _connectionsByRoom = new();

    public IReadOnlyList<Room> Rooms => _rooms;
    public IReadOnlyList<RoomConnection> Connections => _connections;

    public MansionModel(IEnumerable<Room> rooms, IEnumerable<RoomConnection> connections)
    {
        _rooms = new List<Room>(rooms);
        _connections = new List<RoomConnection>(connections);

        foreach (Room room in _rooms)
        {
            if (room != null && !_connectionsByRoom.ContainsKey(room))
            {
                _connectionsByRoom.Add(room, new List<RoomConnection>());
            }
        }

        foreach (RoomConnection connection in _connections)
        {
            if (connection?.From == null)
            {
                continue;
            }

            if (!_connectionsByRoom.TryGetValue(connection.From, out List<RoomConnection> roomConnections))
            {
                roomConnections = new List<RoomConnection>();
                _connectionsByRoom.Add(connection.From, roomConnections);
            }

            roomConnections.Add(connection);
        }
    }

    public IReadOnlyList<RoomConnection> GetConnections(Room room)
    {
        if (room == null || !_connectionsByRoom.TryGetValue(room, out List<RoomConnection> connections))
        {
            return System.Array.Empty<RoomConnection>();
        }

        return connections;
    }

    public RoomConnection GetRandomConnection(Room room)
    {
        IReadOnlyList<RoomConnection> connections = GetConnections(room);
        if (connections.Count == 0)
        {
            return null;
        }

        return connections[Random.Range(0, connections.Count)];
    }

    public bool TryGetConnection(Room from, Room to, out RoomConnection connection)
    {
        connection = null;
        IReadOnlyList<RoomConnection> connections = GetConnections(from);
        for (int i = 0; i < connections.Count; i++)
        {
            if (connections[i].To == to)
            {
                connection = connections[i];
                return true;
            }
        }

        return false;
    }
}

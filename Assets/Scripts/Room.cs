using System.Collections.Generic;
using UnityEngine;

// Base: door to the west
// Corner: door to the west and south
// Corridor: door to the west and east
// Junction: door to the west, east, and south
// Intersection: door to all four sides

public enum RoomDirection
{
    North,
    South,
    East,
    West,
    Up,
    Down
}

public enum RoomType
{
    Base,
    Corner,
    Corridor,
    Junction,
    Intersection,
    Dynamic
}

public class Room : MonoBehaviour
{
    public string RoomId;
    public SpawnPoint PlayerSpawnPoint;
    public SpawnPoint MonsterPoint;
    // Connected Rooms
    [SerializeField] protected Room North;
    [SerializeField] protected Room South;
    [SerializeField] protected Room East;
    [SerializeField] protected Room West;
    [SerializeField] protected Room Up;
    [SerializeField] protected Room Down;

    [SerializeField] protected RoomType roomType;

    public List<Room> ConnectedRooms
    {
        get
        {
            List<Room> connectedRooms = new();
            if (North != null) connectedRooms.Add(North);
            if (South != null) connectedRooms.Add(South);
            if (East != null) connectedRooms.Add(East);
            if (West != null) connectedRooms.Add(West);
            if (Up != null) connectedRooms.Add(Up);
            if (Down != null) connectedRooms.Add(Down);
            return connectedRooms;
        }
    }
    [SerializeField] GameObject lightObjectsParent;

    protected virtual void Awake()
    {
        if (!lightObjectsParent)
        {
            // root.Objects.Lights
            var lightsParent = transform.Find("Objects/Lights");
            if (lightsParent != null)
            {
                lightObjectsParent = lightsParent.gameObject;
            }
            else
            {
                Debug.LogWarning($"Room '{name}' is missing a reference to lightObjectsParent and could not find a child at 'Objects/Lights'.");
            }
        }
    }

    public LightSwitchInteractable GetLightSwitch()
    {
        return transform.Find("LightSwitch")?.GetComponent<LightSwitchInteractable>();
    }

    public List<LightObject> GetLightObjects()
    {
        List<LightObject> lightObjects = new();
        if (lightObjectsParent != null)
        {
            lightObjects.AddRange(lightObjectsParent.GetComponentsInChildren<LightObject>(true));
        }
        return lightObjects;
    }

    public bool HasConnectedRoomInDirection(RoomDirection direction)
    {
        RoomDirection rotatedDirection = GetRotatedDirection(direction);
        Room connectedRoom = GetConnectedRoom(rotatedDirection);

        if (connectedRoom == null)
        {
            return false;
        }

        if (rotatedDirection == RoomDirection.Up || rotatedDirection == RoomDirection.Down)
        {
            return true;
        }

        return connectedRoom.HasDoorFacingWorldDirection(GetOppositeDirection(rotatedDirection));
    }

    private Room GetConnectedRoom(RoomDirection direction)
    {
        return direction switch
        {
            RoomDirection.North => North,
            RoomDirection.South => South,
            RoomDirection.East => East,
            RoomDirection.West => West,
            RoomDirection.Up => Up,
            RoomDirection.Down => Down,
            _ => null
        };
    }

    private bool HasDoorFacingWorldDirection(RoomDirection worldDirection)
    {
        RoomDirection localDirection = GetLocalDirection(worldDirection);

        return roomType switch
        {
            RoomType.Base => localDirection == RoomDirection.West,
            RoomType.Corner => localDirection == RoomDirection.West || localDirection == RoomDirection.South,
            RoomType.Corridor => localDirection == RoomDirection.West || localDirection == RoomDirection.East,
            RoomType.Junction => localDirection == RoomDirection.West || localDirection == RoomDirection.East || localDirection == RoomDirection.South,
            RoomType.Intersection => localDirection == RoomDirection.North || localDirection == RoomDirection.South || localDirection == RoomDirection.East || localDirection == RoomDirection.West,
            RoomType.Dynamic => localDirection == RoomDirection.North || localDirection == RoomDirection.South || localDirection == RoomDirection.East || localDirection == RoomDirection.West,
            _ => false
        };
    }

    private RoomDirection GetRotatedDirection(RoomDirection direction)
    {
        Vector3 localDirection = DirectionToVector(direction);
        if (localDirection == Vector3.zero)
        {
            return direction;
        }

        Vector3 worldDirection = transform.TransformDirection(localDirection);

        return VectorToDirection(worldDirection, direction);
    }

    private RoomDirection GetLocalDirection(RoomDirection worldDirection)
    {
        Vector3 direction = DirectionToVector(worldDirection);
        if (direction == Vector3.zero)
        {
            return worldDirection;
        }

        Vector3 localDirection = transform.InverseTransformDirection(direction);

        return VectorToDirection(localDirection, worldDirection);
    }

    private static RoomDirection VectorToDirection(Vector3 direction, RoomDirection fallbackDirection)
    {
        if (direction == Vector3.zero)
        {
            return fallbackDirection;
        }

        if (Mathf.Abs(direction.y) > Mathf.Abs(direction.x) &&
            Mathf.Abs(direction.y) > Mathf.Abs(direction.z))
        {
            return direction.y > 0f ? RoomDirection.Up : RoomDirection.Down;
        }

        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.z))
        {
            return direction.x > 0f ? RoomDirection.East : RoomDirection.West;
        }

        return direction.z > 0f ? RoomDirection.North : RoomDirection.South;
    }

    private static RoomDirection GetOppositeDirection(RoomDirection direction)
    {
        return direction switch
        {
            RoomDirection.North => RoomDirection.South,
            RoomDirection.South => RoomDirection.North,
            RoomDirection.East => RoomDirection.West,
            RoomDirection.West => RoomDirection.East,
            RoomDirection.Up => RoomDirection.Down,
            RoomDirection.Down => RoomDirection.Up,
            _ => direction
        };
    }

    private static Vector3 DirectionToVector(RoomDirection direction)
    {
        return direction switch
        {
            RoomDirection.North => Vector3.forward,
            RoomDirection.South => Vector3.back,
            RoomDirection.East => Vector3.right,
            RoomDirection.West => Vector3.left,
            RoomDirection.Up => Vector3.up,
            RoomDirection.Down => Vector3.down,
            _ => Vector3.zero
        };
    }

    void OnDrawGizmos()
    {
        // Draw arrow to the north
        Gizmos.color = Color.red;
        Vector3 northDirection = transform.TransformDirection(Vector3.forward);
        Gizmos.DrawLine(transform.position, transform.position + northDirection);
        // Draw arrow to the south
        Gizmos.color = Color.green;
        Vector3 southDirection = transform.TransformDirection(Vector3.back);
        Gizmos.DrawLine(transform.position, transform.position + southDirection);
        // Draw arrow to the east
        Gizmos.color = Color.blue;
        Vector3 eastDirection = transform.TransformDirection(Vector3.right);
        Gizmos.DrawLine(transform.position, transform.position + eastDirection);
        // Draw arrow to the west
        Gizmos.color = Color.yellow;
        Vector3 westDirection = transform.TransformDirection(Vector3.left);
        Gizmos.DrawLine(transform.position, transform.position + westDirection);
    }
}

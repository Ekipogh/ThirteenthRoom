using System;
using System.Collections.Generic;
using UnityEngine;

// Base: door to the south
// Corner: door to the south and east
// Corridor: door to the north and south
// Junction: door to the west, south, and east
// Intersection: door to all four sides

[Serializable]
public class ItemNeed
{
    public SpawnItemDefinition Item;
    public int Quantity;
}

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
    public static readonly Vector3 RoomSize = new(37.5f, 13.5f, 37.5f);

    [Header("Identity")]
    public string RoomId;

    [Header("Spawn Points")]
    public PointNode PlayerSpawnPoint;
    public PointNode MonsterPoint;
    public string[] SpawnTags;

    [Header("Door Points")]
    [SerializeField] protected PointNode northDoorPoint;
    [SerializeField] protected PointNode southDoorPoint;
    [SerializeField] protected PointNode westDoorPoint;
    [SerializeField] protected PointNode eastDoorPoint;
    [SerializeField] protected PointNode upDoorPoint;
    [SerializeField] protected PointNode downDoorPoint;

    [Header("Connected Rooms")]
    // Connected Rooms
    [SerializeField] protected Room North;
    [SerializeField] protected Room South;
    [SerializeField] protected Room East;
    [SerializeField] protected Room West;
    [SerializeField] protected Room Up;
    [SerializeField] protected Room Down;

    [Header("Room Settings")]
    [SerializeField] protected RoomType roomType;

    [Header("Child Containers")]
    [SerializeField] GameObject itemSpawnPointsParent;
    [SerializeField] GameObject lightObjectsParent;

    [Header("Items")]
    private List<SpawnItemDefinition> _requestedItems = new();
    public IReadOnlyList<SpawnItemDefinition> RequestedItems => _requestedItems.AsReadOnly();
    // Items to request on room generation, with quantities
    [SerializeField] private List<ItemNeed> itemNeeds = new();

    public RoomType RoomType => roomType;

    public void SetRoomType(RoomType type)
    {
        roomType = type;
    }

    public void SetNorth(Room room)
    {
        North = room;
    }

    public void SetSouth(Room room)
    {
        South = room;
    }

    public void SetEast(Room room)
    {
        East = room;
    }

    public void SetWest(Room room)
    {
        West = room;
    }

    public void SetUp(Room room)
    {
        Up = room;
    }

    public void SetDown(Room room)
    {
        Down = room;
    }

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
        if (!itemSpawnPointsParent)
        {
            // root.ItemSpawnPoints
            var itemSpawnPoints = transform.Find("ItemSpawnPoints");
            if (itemSpawnPoints != null)
            {
                itemSpawnPointsParent = itemSpawnPoints.gameObject;
            }
            else
            {
                Debug.LogWarning($"Room '{name}' is missing a reference to itemSpawnPointsParent and could not find a child at 'ItemSpawnPoints'.");
            }
        }
        if (itemNeeds != null && itemNeeds.Count > 0)
        {
            foreach (var itemNeed in itemNeeds)
            {
                for (int i = 0; i < itemNeed.Quantity; i++)
                {
                    _requestedItems.Add(itemNeed.Item);
                }
            }
        }
    }

    protected virtual void Start()
    {
        // Ensure the room has a unique ID for saving/loading purposes
        if (string.IsNullOrEmpty(RoomId))
        {
            RoomId = name + "_" + GetInstanceID();
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

    public bool HasDoorFacingDirection(RoomDirection worldDirection)
    {
        return HasDoorFacingWorldDirection(worldDirection);
    }

    protected Room GetConnectedRoom(RoomDirection direction)
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
            RoomType.Base => localDirection == RoomDirection.South,
            RoomType.Corner => localDirection == RoomDirection.South || localDirection == RoomDirection.East,
            RoomType.Corridor => localDirection == RoomDirection.North || localDirection == RoomDirection.South,
            RoomType.Junction => localDirection == RoomDirection.West || localDirection == RoomDirection.South || localDirection == RoomDirection.East,
            RoomType.Intersection => localDirection == RoomDirection.North || localDirection == RoomDirection.South || localDirection == RoomDirection.East || localDirection == RoomDirection.West,
            RoomType.Dynamic => localDirection == RoomDirection.North || localDirection == RoomDirection.South || localDirection == RoomDirection.East || localDirection == RoomDirection.West,
            _ => false
        };
    }

    protected RoomDirection GetRotatedDirection(RoomDirection direction)
    {
        Vector3 localDirection = DirectionToVector(direction);
        if (localDirection == Vector3.zero)
        {
            return direction;
        }

        Vector3 worldDirection = transform.TransformDirection(localDirection);

        return VectorToDirection(worldDirection, direction);
    }

    protected RoomDirection GetLocalDirection(RoomDirection worldDirection)
    {
        Vector3 direction = DirectionToVector(worldDirection);
        if (direction == Vector3.zero)
        {
            return worldDirection;
        }

        Vector3 localDirection = transform.InverseTransformDirection(direction);

        return VectorToDirection(localDirection, worldDirection);
    }

    protected static RoomDirection VectorToDirection(Vector3 direction, RoomDirection fallbackDirection)
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

        return direction.z > 0f ? RoomDirection.South : RoomDirection.North;
    }

    protected static RoomDirection GetOppositeDirection(RoomDirection direction)
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

    protected static Vector3 DirectionToVector(RoomDirection direction)
    {
        return direction switch
        {
            RoomDirection.North => Vector3.back,
            RoomDirection.South => Vector3.forward,
            RoomDirection.East => Vector3.right,
            RoomDirection.West => Vector3.left,
            RoomDirection.Up => Vector3.up,
            RoomDirection.Down => Vector3.down,
            _ => Vector3.zero
        };
    }

    public int GetDoorCount()
    {
        return roomType switch
        {
            RoomType.Base => 1,
            RoomType.Corner => 2,
            RoomType.Corridor => 2,
            RoomType.Junction => 3,
            RoomType.Intersection => 4,
            RoomType.Dynamic => ConnectedRooms.Count,
            _ => 0
        };
    }

    public Vector3 GetDoorPosition(RoomDirection direction)
    {
        return TryGetDoorPosition(direction, out Vector3 position)
            ? position
            : Vector3.zero;
    }

    public bool TryGetDoorPosition(RoomDirection worldDirection, out Vector3 position)
    {
        PointNode doorPoint = GetDoorPoint(GetLocalDirection(worldDirection));
        if (doorPoint == null)
        {
            position = Vector3.zero;
            return false;
        }

        position = doorPoint.transform.position;
        return true;
    }

    private PointNode GetDoorPoint(RoomDirection localDirection)
    {
        return localDirection switch
        {
            RoomDirection.North => northDoorPoint,
            RoomDirection.South => southDoorPoint,
            RoomDirection.East => eastDoorPoint,
            RoomDirection.West => westDoorPoint,
            RoomDirection.Up => upDoorPoint,
            RoomDirection.Down => downDoorPoint,
            _ => null
        };
    }

    public ItemSpawnPoint[] GetItemSpawnPoints()
    {
        if (itemSpawnPointsParent == null)
        {
            Debug.LogWarning($"Room '{name}' is missing a reference to itemSpawnPointsParent.");
            return Array.Empty<ItemSpawnPoint>();
        }

        return itemSpawnPointsParent.GetComponentsInChildren<ItemSpawnPoint>();
    }
}

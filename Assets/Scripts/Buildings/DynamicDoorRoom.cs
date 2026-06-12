using UnityEngine;

public class DynamicDoorRoom : Room
{
    [Header("North Opening")]
    public GameObject NorthWall;
    public GameObject NorthDoor;

    [Header("South Opening")]
    public GameObject SouthWall;
    public GameObject SouthDoor;

    [Header("East Opening")]
    public GameObject EastWall;
    public GameObject EastDoor;

    [Header("West Opening")]
    public GameObject WestWall;
    public GameObject WestDoor;

    protected override void Awake()
    {
        base.Awake();
        roomType = RoomType.Dynamic;
    }

    protected override void Start()
    {
        base.Start();
        UpdateDynamicDoors();
    }

    public void UpdateDynamicDoors()
    {
        SetOpening(RoomDirection.North, NorthWall, NorthDoor);
        SetOpening(RoomDirection.South, SouthWall, SouthDoor);
        SetOpening(RoomDirection.East, EastWall, EastDoor);
        SetOpening(RoomDirection.West, WestWall, WestDoor);
    }

    protected virtual void SetOpening(RoomDirection direction, GameObject wall, GameObject door)
    {
        bool hasOpening = HasAdjacentRoomDoorFacingThisRoom(direction);

        if (wall != null)
        {
            wall.SetActive(!hasOpening);
        }
        else
        {
            Debug.LogWarning($"{GetType().Name} '{name}' is missing a wall reference for {direction}.");
        }

        if (door != null)
        {
            door.SetActive(hasOpening);
        }
        else if (hasOpening)
        {
            Debug.LogWarning($"{GetType().Name} '{name}' has an opening for {direction}, but is missing a door reference.");
        }
    }

    private bool HasAdjacentRoomDoorFacingThisRoom(RoomDirection localDirection)
    {
        RoomDirection worldDirection = GetRotatedDirection(localDirection);
        Room adjacentRoom = GetAdjacentRoomInWorldDirection(worldDirection);

        if (adjacentRoom == null)
        {
            return false;
        }

        RoomDirection directionFacingThisRoom = GetOppositeDirection(worldDirection);
        return adjacentRoom.HasDoorFacingDirection(directionFacingThisRoom);
    }

    private Room GetAdjacentRoomInWorldDirection(RoomDirection worldDirection)
    {
        Room connectedRoom = GetConnectedRoom(worldDirection);
        return connectedRoom;
    }

    private static Vector3 GetRoomOffset(RoomDirection direction)
    {
        return direction switch
        {
            RoomDirection.North => new Vector3(0f, 0f, RoomSize.z),
            RoomDirection.South => new Vector3(0f, 0f, -RoomSize.z),
            RoomDirection.East => new Vector3(RoomSize.x, 0f, 0f),
            RoomDirection.West => new Vector3(-RoomSize.x, 0f, 0f),
            RoomDirection.Up => new Vector3(0f, RoomSize.y, 0f),
            RoomDirection.Down => new Vector3(0f, -RoomSize.y, 0f),
            _ => Vector3.zero
        };
    }
}

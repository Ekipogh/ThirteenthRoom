using UnityEngine;

public class Landing : Room
{
    public GameObject NorthWall;
    public GameObject NorthDoor;
    public GameObject SouthWall;
    public GameObject SouthDoor;
    public GameObject EastWall;
    public GameObject EastDoor;
    public GameObject WestWall;
    public GameObject WestDoor;

    protected override void Awake()
    {
        base.Awake();

        SetOpening(RoomDirection.North, NorthWall, NorthDoor);
        SetOpening(RoomDirection.South, SouthWall, SouthDoor);
        SetOpening(RoomDirection.East, EastWall, EastDoor);
        SetOpening(RoomDirection.West, WestWall, WestDoor);
    }

    void SetOpening(RoomDirection direction, GameObject wall, GameObject door)
    {
        bool hasConnection = HasConnectedRoomInDirection(direction);

        if (wall != null)
        {
            wall.SetActive(!hasConnection);
        }
        else
        {
            Debug.LogWarning($"Landing '{name}' is missing a wall reference for {direction}.");
        }

        if (door != null)
        {
            door.SetActive(hasConnection);
        }
        else
        {
            Debug.LogWarning($"Landing '{name}' is missing a door reference for {direction}.");
        }
    }
}

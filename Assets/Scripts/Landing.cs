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

    [SerializeField] GameObject leftStaircase;
    [SerializeField] GameObject leftStairsCeiling;
    [SerializeField] GameObject rightStaircase;
    [SerializeField] GameObject rightStairsCeiling;
    [SerializeField] GameObject floorsParent;

    [SerializeField] private int floor = 0;

    protected override void Awake()
    {
        base.Awake();

        SetOpening(RoomDirection.North, NorthWall, NorthDoor);
        SetOpening(RoomDirection.South, SouthWall, SouthDoor);
        SetOpening(RoomDirection.East, EastWall, EastDoor);
        SetOpening(RoomDirection.West, WestWall, WestDoor);
        ManageStaircaseVisibility();
    }

    private void ManageStaircaseVisibility()
    {
        if (floor > 0)
        {
            floorsParent.SetActive(false);
        }
        if (Up == null)
        {
            if (leftStaircase != null) leftStaircase.SetActive(false);
            if (leftStairsCeiling != null) leftStairsCeiling.SetActive(true);
            if (rightStaircase != null) rightStaircase.SetActive(false);
            if (rightStairsCeiling != null) rightStairsCeiling.SetActive(true);
            return;
        }
        if (floor % 2 == 0)
        {
            if (leftStaircase != null) leftStaircase.SetActive(true);
            if (leftStairsCeiling != null) leftStairsCeiling.SetActive(true);
            if (rightStaircase != null) rightStaircase.SetActive(false);
            if (rightStairsCeiling != null) rightStairsCeiling.SetActive(false);
        }
        else
        {
            if (leftStaircase != null) leftStaircase.SetActive(false);
            if (leftStairsCeiling != null) leftStairsCeiling.SetActive(false);
            if (rightStaircase != null) rightStaircase.SetActive(true);
            if (rightStairsCeiling != null) rightStairsCeiling.SetActive(true);
        }
    }


    void SetOpening(RoomDirection direction, GameObject wall, GameObject door)
    {
        bool hasOpening = HasConnectedRoomInDirection(direction);

        if (wall != null)
        {
            wall.SetActive(!hasOpening);
        }
        else
        {
            Debug.LogWarning($"Landing '{name}' is missing a wall reference for {direction}.");
        }

        if (door != null)
        {
            door.SetActive(hasOpening);
        }
        else if (hasOpening)
        {
            Debug.LogWarning($"Landing '{name}' has an opening for {direction}, but is missing a door reference.");
        }
    }
}

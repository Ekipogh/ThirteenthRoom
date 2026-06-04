using System.Collections.Generic;
using UnityEngine;

public class MansionGenerator : MonoBehaviour
{
    [SerializeField] PlayerController playerController;

    [SerializeField] GameObject entrancePrefab;
    [SerializeField] GameObject stairsPrefab;

    // Unique rooms
    [SerializeField] GameObject[] uniqueRooms;
    // Generic rooms
    [SerializeField] GameObject[] genericBaseRoomPrefabs;
    [SerializeField] GameObject[] genericCorridorPrefabs;
    [SerializeField] GameObject[] genericCornerPrefabs;
    [SerializeField] GameObject[] genericJuntionPrefabs;
    [SerializeField] GameObject[] genericIntersectionPrefabs;

    [SerializeField] GameObject doorPrefab;
    [SerializeField] GameObject doorParent;
    [SerializeField] GameObject floorParent;

    GameObject[] floorParents;

    public int width = 5; // Number of rooms horizontally
    public int height = 5; // Number of rooms vertically
    public int floorCount = 2;

    private const int MaxLayoutAttempts = 50;
    private const int DoorNorth = 1 << 0;
    private const int DoorEast = 1 << 1;
    private const int DoorSouth = 1 << 2;
    private const int DoorWest = 1 << 3;

    private Room[,,] placedRooms;
    private readonly List<RoomConnection> roomConnections = new();
    private readonly Dictionary<DoorKey, DoorInteractable> doorsByConnection = new();

    public MansionModel CurrentMansion { get; private set; }

    public void GenerateMansion()
    {
        CurrentMansion = null;
        roomConnections.Clear();
        doorsByConnection.Clear();
        CreateFloorParentObjects();
        var entranceRoom = InitializeEntrance();
        Cell[][,] mansionFloorLayouts = null;
        List<RoomPlacement> uniquePlacements = null;

        for (int attempt = 0; attempt < MaxLayoutAttempts; attempt++)
        {
            mansionFloorLayouts = GenerateLayouts();
            uniquePlacements = CreateUniqueRoomPlacements(mansionFloorLayouts);

            if (uniquePlacements != null)
            {
                break;
            }
        }
        PlaceDoors(mansionFloorLayouts);
        if (uniquePlacements == null || mansionFloorLayouts == null)
        {
            Debug.LogError($"Failed to place all unique mansion rooms after {MaxLayoutAttempts} layout attempts.");
            return;
        }

        placedRooms = new Room[floorCount, width, height];
        InitializeStairs(mansionFloorLayouts, entranceRoom);
        InitializeRooms(mansionFloorLayouts, uniquePlacements);
        ConnectPlacedRooms(mansionFloorLayouts, entranceRoom);
        CurrentMansion = new MansionModel(GetGeneratedRooms(entranceRoom), roomConnections);
        BindGeneratedFuseBoxes();
        RefreshDynamicRooms();
    }

    private Cell[][,] GenerateLayouts()
    {
        Cell[][,] layouts = new Cell[floorCount][,];
        for (int i = 0; i < floorCount; i++)
        {
            layouts[i] = GenerateMansionLayout();
        }
        return layouts;
    }

    private void PlaceDoors(Cell[][,] mansionFloorLayouts)
    {
        const float doorOffset = 1.5f;
        for (int floor = 0; floor < floorCount; floor++)
        {
            Cell[,] mansionLayout = mansionFloorLayouts[floor];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Cell cell = mansionLayout[x, y];
                    Vector3 roomPosition = GetRoomPosition(floor, x, y);

                    // North
                    if (cell.Doors[0] && !cell.DoorsPlaced[0])
                    {
                        DoorInteractable door = PlaceDoor(roomPosition + new Vector3(0f + doorOffset, 0f, -Room.RoomSize.z / 2f), Quaternion.identity, "Door_North" + $"_Floor{floor}_{x}_{y}");
                        RegisterDoor(floor, x, y, RoomDirection.North, door);
                        cell.DoorsPlaced[0] = true;
                        if (y > 0)
                        {
                            mansionLayout[x, y - 1].DoorsPlaced[2] = true;
                        }
                    }
                    //  East
                    if (cell.Doors[1] && !cell.DoorsPlaced[1])
                    {
                        DoorInteractable door = PlaceDoor(roomPosition + new Vector3(-Room.RoomSize.x / 2f, 0f, 0f - doorOffset), Quaternion.Euler(0f, 90f, 0f), "Door_East" + $"_Floor{floor}_{x}_{y}");
                        RegisterDoor(floor, x, y, RoomDirection.East, door);
                        cell.DoorsPlaced[1] = true;
                        if (x + 1 < width)
                        {
                            mansionLayout[x + 1, y].DoorsPlaced[3] = true;
                        }
                    }
                    // South
                    if (cell.Doors[2] && !cell.DoorsPlaced[2])
                    {
                        DoorInteractable door = PlaceDoor(roomPosition + new Vector3(0f + doorOffset, 0f, Room.RoomSize.z / 2f), Quaternion.identity, "Door_South" + $"_Floor{floor}_{x}_{y}");
                        RegisterDoor(floor, x, y, RoomDirection.South, door);
                        cell.DoorsPlaced[2] = true;
                        if (y + 1 < height)
                        {
                            mansionLayout[x, y + 1].DoorsPlaced[0] = true;
                        }
                    }
                    // West
                    if (cell.Doors[3] && !cell.DoorsPlaced[3])
                    {
                        DoorInteractable door = PlaceDoor(roomPosition + new Vector3(Room.RoomSize.x / 2f, 0f, 0f - doorOffset), Quaternion.Euler(0f, 90f, 0f), "Door_West" + $"_Floor{floor}_{x}_{y}");
                        RegisterDoor(floor, x, y, RoomDirection.West, door);
                        cell.DoorsPlaced[3] = true;
                        if (x > 0)
                        {
                            mansionLayout[x - 1, y].DoorsPlaced[1] = true;
                        }
                    }
                }
            }
        }
    }

    private DoorInteractable PlaceDoor(Vector3 position, Quaternion rotation, string doorName = "Door")
    {
        var door = Instantiate(doorPrefab, position, rotation, doorParent.transform);
        door.name = doorName;
        return door.GetComponentInChildren<DoorInteractable>();
    }

    private void InitializeRooms(Cell[][,] mansionFloorLayouts, List<RoomPlacement> uniquePlacements)
    {
        foreach (RoomPlacement placement in uniquePlacements)
        {
            PlaceRoom(placement, true);
            mansionFloorLayouts[placement.Floor][placement.X, placement.Y].IsPlaced = true;
        }

        for (int floor = 0; floor < floorCount; floor++)
        {
            Cell[,] mansionLayout = mansionFloorLayouts[floor];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Cell cell = mansionLayout[x, y];
                    if (cell.IsPlaced)
                    {
                        continue;
                    }

                    if (!TryCreateGenericPlacement(floor, x, y, cell, out RoomPlacement placement))
                    {
                        Debug.LogError($"No generic room prefab can fit cell ({x}, {y}) on floor {floor} with door mask {GetRequiredWorldDoorMask(cell, x, y)}.");
                        continue;
                    }

                    PlaceRoom(placement, false);
                    cell.IsPlaced = true;
                }
            }
        }
    }

    private void InitializeStairs(Cell[][,] mansionFloorLayouts, Room entranceRoom)
    {
        if (floorCount > 1)
        {
            var (x, y) = (width / 2, height - 1);
            Room previousStairs = null;
            for (int floor = 0; floor < floorCount; floor++)
            {
                var stairs = Instantiate(stairsPrefab, GetRoomPosition(floor, x, y), Quaternion.identity);
                SetRoomIdAndParent(stairs, $"Stairs_Floor{floor}", floor);
                Room stairsRoom = stairs.GetComponent<Room>();

                if (floor == 0)
                {
                    entranceRoom.SetNorth(stairsRoom);
                    stairsRoom.SetSouth(entranceRoom);
                    CreateRoomConnectionPair(entranceRoom, stairsRoom, RoomDirection.North);
                }
                else
                {
                    previousStairs.SetUp(stairsRoom);
                    stairsRoom.SetDown(previousStairs);
                    CreateRoomConnectionPair(previousStairs, stairsRoom, RoomDirection.Up);
                }

                stairs.GetComponent<Landing>().SetFloor(floor);
                placedRooms[floor, x, y] = stairsRoom;
                previousStairs = stairsRoom;
                mansionFloorLayouts[floor][x, y].IsPlaced = true;
            }
        }
    }

    private Room InitializeEntrance()
    {
        var (x, y) = (width / 2, height); // entrance is placed just outside the bottom center of the mansion
        var entrance = Instantiate(entrancePrefab, GetRoomPosition(0, x, y), Quaternion.identity);
        SetRoomIdAndParent(entrance, "Entrance", 0);
        Room entranceRoom = entrance.GetComponent<Room>();
        playerController.SetStartingRoom(entranceRoom);
        return entranceRoom;
    }

    private void SetRoomIdAndParent(GameObject room, string roomId, int floor)
    {
        Room roomComponent = room.GetComponent<Room>();
        if (roomComponent == null)
        {
            Debug.LogError($"Generated room '{room.name}' is missing a Room component.");
            return;
        }

        roomComponent.RoomId = roomId;
        room.transform.name = roomId;
        room.transform.SetParent(floorParents[floor].transform);
    }

    private Cell[,] GenerateMansionLayout()
    {
        MazeGenerator mazeGenerator = new(width, height);
        Cell[,] mansionLayout = mazeGenerator.GenerateMaze();
        return mansionLayout;
    }

    private List<RoomPlacement> CreateUniqueRoomPlacements(Cell[][,] mansionFloorLayouts)
    {
        List<RoomPlacement> placements = new(uniqueRooms?.Length ?? 0);
        List<CellLocation> availableCells = GetAvailableCells();

        Shuffle(availableCells);

        foreach (GameObject uniqueRoom in uniqueRooms ?? System.Array.Empty<GameObject>())
        {
            if (uniqueRoom == null)
            {
                Debug.LogWarning("MansionGenerator has a null unique room prefab reference.");
                continue;
            }

            if (!uniqueRoom.TryGetComponent<Room>(out Room uniqueRoomComponent))
            {
                Debug.LogWarning($"Unique room prefab '{uniqueRoom.name}' is missing a Room component.");
                continue;
            }

            int uniqueDoorCount = uniqueRoomComponent.GetDoorCount();

            int selectedCellIndex = -1;
            Quaternion selectedRotation = Quaternion.identity;

            for (int i = 0; i < availableCells.Count; i++)
            {
                CellLocation location = availableCells[i];
                Cell cell = mansionFloorLayouts[location.Floor][location.X, location.Y];

                // check number of doors
                if (cell.DoorCount() != uniqueDoorCount)
                {
                    continue;
                }

                int requiredMask = GetRequiredWorldDoorMask(cell, location.X, location.Y);
                if (TryGetMatchingRotation(uniqueRoom, requiredMask, out selectedRotation))
                {
                    selectedCellIndex = i;
                    break;
                }
            }

            if (selectedCellIndex < 0)
            {
                return null;
            }

            CellLocation selectedCell = availableCells[selectedCellIndex];
            availableCells.RemoveAt(selectedCellIndex);
            placements.Add(new RoomPlacement(uniqueRoom, selectedCell.Floor, selectedCell.X, selectedCell.Y, selectedRotation));
        }

        return placements;
    }

    private List<CellLocation> GetAvailableCells()
    {
        List<CellLocation> cells = new(floorCount * width * height);
        for (int floor = 0; floor < floorCount; floor++)
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (IsReservedForStairs(x, y))
                    {
                        continue;
                    }

                    cells.Add(new CellLocation(floor, x, y));
                }
            }
        }

        return cells;
    }

    private bool TryCreateGenericPlacement(int floor, int x, int y, Cell cell, out RoomPlacement placement)
    {
        int doorMask = GetRequiredWorldDoorMask(cell, x, y);
        List<GameObject> prefabOptions = new(GetGenericPrefabsForDoorMask(doorMask));
        Shuffle(prefabOptions);
        RoomType genericRoomType = GetGenericRoomType(doorMask);
        int genericLocalDoorMask = GetDoorMask(genericRoomType);

        foreach (GameObject prefab in prefabOptions)
        {
            if (prefab == null)
            {
                continue;
            }

            if (TryGetMatchingRotation(prefab, genericLocalDoorMask, doorMask, out Quaternion rotation))
            {
                placement = new RoomPlacement(prefab, floor, x, y, rotation, genericRoomType);
                return true;
            }
        }

        placement = default;
        return false;
    }

    private GameObject[] GetGenericPrefabsForDoorMask(int doorMask)
    {
        int doorCount = CountDoors(doorMask);

        return doorCount switch
        {
            1 => genericBaseRoomPrefabs ?? System.Array.Empty<GameObject>(),
            2 => IsOppositeDoorPair(doorMask) ? (genericCorridorPrefabs ?? System.Array.Empty<GameObject>()) : (genericCornerPrefabs ?? System.Array.Empty<GameObject>()),
            3 => genericJuntionPrefabs ?? System.Array.Empty<GameObject>(),
            4 => genericIntersectionPrefabs ?? System.Array.Empty<GameObject>(),
            _ => System.Array.Empty<GameObject>()
        };
    }

    private void PlaceRoom(RoomPlacement placement, bool isUnique)
    {
        Vector3 position = GetRoomPosition(placement.Floor, placement.X, placement.Y);
        GameObject roomObject = Instantiate(placement.Prefab, position, placement.Rotation);
        string prefix = isUnique ? "UniqueRoom" : "Room";
        SetRoomIdAndParent(roomObject, $"{prefix}_Floor{placement.Floor}_{placement.X}_{placement.Y}", placement.Floor);
        Room room = roomObject.GetComponent<Room>();
        if (room == null)
        {
            Debug.LogError($"Placed prefab '{placement.Prefab.name}' is missing a Room component.");
            return;
        }

        if (placement.HasRoomTypeOverride)
        {
            room.SetRoomType(placement.RoomTypeOverride);
        }
        placedRooms[placement.Floor, placement.X, placement.Y] = room;
    }

    private void ConnectPlacedRooms(Cell[][,] mansionFloorLayouts, Room entranceRoom)
    {
        for (int floor = 0; floor < floorCount; floor++)
        {
            Cell[,] mansionLayout = mansionFloorLayouts[floor];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Room room = placedRooms[floor, x, y];
                    if (room == null)
                    {
                        continue;
                    }

                    Cell cell = mansionLayout[x, y];

                    if (cell.Doors[0] && y > 0)
                    {
                        room.SetNorth(placedRooms[floor, x, y - 1]);
                        CreateRoomConnectionPair(room, placedRooms[floor, x, y - 1], RoomDirection.North, GetDoor(floor, x, y, RoomDirection.North));
                    }
                    if (cell.Doors[1] && x < width - 1)
                    {
                        room.SetEast(placedRooms[floor, x + 1, y]);
                        CreateRoomConnectionPair(room, placedRooms[floor, x + 1, y], RoomDirection.East, GetDoor(floor, x, y, RoomDirection.East));
                    }
                    if (cell.Doors[2] && y < height - 1)
                    {
                        room.SetSouth(placedRooms[floor, x, y + 1]);
                        CreateRoomConnectionPair(room, placedRooms[floor, x, y + 1], RoomDirection.South, GetDoor(floor, x, y, RoomDirection.South));
                    }
                    if (cell.Doors[3] && x > 0)
                    {
                        room.SetWest(placedRooms[floor, x - 1, y]);
                        CreateRoomConnectionPair(room, placedRooms[floor, x - 1, y], RoomDirection.West, GetDoor(floor, x, y, RoomDirection.West));
                    }
                }
            }
        }

        if (floorCount == 1)
        {
            Room entranceConnection = placedRooms[0, width / 2, height - 1];
            if (entranceConnection != null)
            {
                entranceRoom.SetNorth(entranceConnection);
                entranceConnection.SetSouth(entranceRoom);
                CreateRoomConnectionPair(entranceRoom, entranceConnection, RoomDirection.North);
            }
        }
    }

    private void CreateRoomConnectionPair(Room roomA, Room roomB, RoomDirection directionFromAtoB, DoorInteractable door = null)
    {
        if (roomA == null || roomB == null)
        {
            return;
        }

        RoomConnection forward = GetOrCreateConnection(roomA, roomB, directionFromAtoB, door);
        RoomConnection reverse = GetOrCreateConnection(roomB, roomA, GetOppositeDirection(directionFromAtoB), door);
        forward.SetReverse(reverse);
        reverse.SetReverse(forward);
    }

    private RoomConnection GetOrCreateConnection(Room from, Room to, RoomDirection directionFromFromToTo, DoorInteractable door)
    {
        foreach (RoomConnection connection in roomConnections)
        {
            if (connection.From == from && connection.To == to)
            {
                return connection;
            }
        }

        Vector3 doorwayPoint = GetDoorwayPoint(from, to, directionFromFromToTo, door);
        RoomConnection createdConnection = new(from, to, directionFromFromToTo, doorwayPoint, door);
        roomConnections.Add(createdConnection);
        return createdConnection;
    }

    private static Vector3 GetDoorwayPoint(Room from, Room to, RoomDirection directionFromFromToTo, DoorInteractable door)
    {
        if (from.TryGetDoorPosition(directionFromFromToTo, out Vector3 doorwayPoint))
        {
            return doorwayPoint;
        }

        if (door != null)
        {
            return door.transform.position;
        }

        return (from.transform.position + to.transform.position) / 2f;
    }

    private void RefreshDynamicRooms()
    {
        foreach (Room room in placedRooms)
        {
            if (room is Landing landing)
            {
                landing.UpdateDoorsAndStairs();
            }
            else if (room is DynamicDoorRoom dynamicDoorRoom)
            {
                dynamicDoorRoom.UpdateDynamicDoors();
            }
        }
    }

    private void BindGeneratedFuseBoxes()
    {
        List<Room> generatedRooms = GetGeneratedRooms();

        foreach (Room room in generatedRooms)
        {
            FuseBox[] fuseBoxes = room.GetComponentsInChildren<FuseBox>(true);
            foreach (FuseBox fuseBox in fuseBoxes)
            {
                if (fuseBox != null)
                {
                    fuseBox.SetControlledRooms(generatedRooms);
                }
            }
        }
    }

    private List<Room> GetGeneratedRooms(Room extraRoom = null)
    {
        List<Room> generatedRooms = new(floorCount * width * height);
        if (extraRoom != null)
        {
            generatedRooms.Add(extraRoom);
        }

        if (placedRooms == null)
        {
            return generatedRooms;
        }

        foreach (Room room in placedRooms)
        {
            if (room != null)
            {
                generatedRooms.Add(room);
            }
        }

        return generatedRooms;
    }

    private void RegisterDoor(int floor, int x, int y, RoomDirection direction, DoorInteractable door)
    {
        if (door == null)
        {
            return;
        }

        doorsByConnection[new DoorKey(floor, x, y, direction)] = door;
        if (TryGetAdjacentCell(x, y, direction, out int adjacentX, out int adjacentY))
        {
            doorsByConnection[new DoorKey(floor, adjacentX, adjacentY, GetOppositeDirection(direction))] = door;
        }
    }

    private DoorInteractable GetDoor(int floor, int x, int y, RoomDirection direction)
    {
        doorsByConnection.TryGetValue(new DoorKey(floor, x, y, direction), out DoorInteractable door);
        return door;
    }

    private static bool TryGetAdjacentCell(int x, int y, RoomDirection direction, out int adjacentX, out int adjacentY)
    {
        adjacentX = x;
        adjacentY = y;

        switch (direction)
        {
            case RoomDirection.North:
                adjacentY = y - 1;
                return true;
            case RoomDirection.South:
                adjacentY = y + 1;
                return true;
            case RoomDirection.East:
                adjacentX = x + 1;
                return true;
            case RoomDirection.West:
                adjacentX = x - 1;
                return true;
            default:
                return false;
        }
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

    private bool IsReservedForStairs(int x, int y)
    {
        return floorCount > 1 && x == width / 2 && y == height - 1;
    }

    private static bool TryGetMatchingRotation(GameObject prefab, int targetDoorMask, out Quaternion rotation)
    {
        if (!prefab.TryGetComponent<Room>(out var room))
        {
            rotation = Quaternion.identity;
            return false;
        }

        return TryGetMatchingRotation(prefab, GetDoorMask(room.RoomType), targetDoorMask, out rotation);
    }

    private static bool TryGetMatchingRotation(GameObject prefab, int localDoorMask, int targetDoorMask, out Quaternion rotation)
    {
        if (prefab.GetComponent<Room>() == null)
        {
            rotation = Quaternion.identity;
            return false;
        }

        for (int turns = 0; turns < 4; turns++)
        {
            int rotatedMask = RotateDoorMask(localDoorMask, turns);
            if (rotatedMask == targetDoorMask)
            {
                rotation = Quaternion.Euler(0f, turns * 90f, 0f);
                return true;
            }
        }

        rotation = Quaternion.identity;
        return false;
    }

    private static int GetDoorMask(Cell cell)
    {
        int mask = 0;
        if (cell.Doors[0]) mask |= DoorNorth;
        if (cell.Doors[1]) mask |= DoorEast;
        if (cell.Doors[2]) mask |= DoorSouth;
        if (cell.Doors[3]) mask |= DoorWest;
        return mask;
    }

    private int GetRequiredWorldDoorMask(Cell cell, int x, int y)
    {
        int mask = GetDoorMask(cell);
        if (floorCount == 1 && x == width / 2 && y == height - 1)
        {
            mask |= DoorSouth;
        }

        return mask;
    }

    private Vector3 GetRoomPosition(int floor, int x, int y)
    {
        return new Vector3((width - 1 - x) * Room.RoomSize.x, floor * Room.RoomSize.y, y * Room.RoomSize.z);
    }

    private static int GetDoorMask(RoomType roomType)
    {
        return roomType switch
        {
            RoomType.Base => DoorSouth,
            RoomType.Corner => DoorSouth | DoorEast,
            RoomType.Corridor => DoorNorth | DoorSouth,
            RoomType.Junction => DoorWest | DoorSouth | DoorEast,
            RoomType.Intersection => DoorNorth | DoorEast | DoorSouth | DoorWest,
            RoomType.Dynamic => DoorNorth | DoorEast | DoorSouth | DoorWest,
            _ => 0
        };
    }

    private static RoomType GetGenericRoomType(int targetDoorMask)
    {
        int doorCount = CountDoors(targetDoorMask);
        return doorCount switch
        {
            1 => RoomType.Base,
            2 => IsOppositeDoorPair(targetDoorMask) ? RoomType.Corridor : RoomType.Corner,
            3 => RoomType.Junction,
            4 => RoomType.Intersection,
            _ => RoomType.Dynamic
        };
    }

    private static int RotateDoorMask(int mask, int clockwiseTurns)
    {
        int rotatedMask = mask;
        for (int i = 0; i < clockwiseTurns; i++)
        {
            int nextMask = 0;
            if ((rotatedMask & DoorNorth) != 0) nextMask |= DoorEast;
            if ((rotatedMask & DoorEast) != 0) nextMask |= DoorSouth;
            if ((rotatedMask & DoorSouth) != 0) nextMask |= DoorWest;
            if ((rotatedMask & DoorWest) != 0) nextMask |= DoorNorth;
            rotatedMask = nextMask;
        }

        return rotatedMask;
    }

    private static int CountDoors(int doorMask)
    {
        int count = 0;
        if ((doorMask & DoorNorth) != 0) count++;
        if ((doorMask & DoorEast) != 0) count++;
        if ((doorMask & DoorSouth) != 0) count++;
        if ((doorMask & DoorWest) != 0) count++;
        return count;
    }

    private static bool IsOppositeDoorPair(int doorMask)
    {
        return doorMask == (DoorNorth | DoorSouth) || doorMask == (DoorEast | DoorWest);
    }

    private static void Shuffle<T>(IList<T> items)
    {
        for (int i = items.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (items[i], items[j]) = (items[j], items[i]);
        }
    }

    private readonly struct CellLocation
    {
        public readonly int Floor;
        public readonly int X;
        public readonly int Y;

        public CellLocation(int floor, int x, int y)
        {
            Floor = floor;
            X = x;
            Y = y;
        }
    }

    private readonly struct RoomPlacement
    {
        public readonly GameObject Prefab;
        public readonly int Floor;
        public readonly int X;
        public readonly int Y;
        public readonly Quaternion Rotation;
        public readonly bool HasRoomTypeOverride;
        public readonly RoomType RoomTypeOverride;

        public RoomPlacement(GameObject prefab, int floor, int x, int y, Quaternion rotation)
        {
            Prefab = prefab;
            Floor = floor;
            X = x;
            Y = y;
            Rotation = rotation;
            HasRoomTypeOverride = false;
            RoomTypeOverride = RoomType.Dynamic;
        }

        public RoomPlacement(GameObject prefab, int floor, int x, int y, Quaternion rotation, RoomType roomTypeOverride)
        {
            Prefab = prefab;
            Floor = floor;
            X = x;
            Y = y;
            Rotation = rotation;
            HasRoomTypeOverride = true;
            RoomTypeOverride = roomTypeOverride;
        }
    }

    private readonly struct DoorKey
    {
        readonly int floor;
        readonly int x;
        readonly int y;
        readonly RoomDirection direction;

        public DoorKey(int floor, int x, int y, RoomDirection direction)
        {
            this.floor = floor;
            this.x = x;
            this.y = y;
            this.direction = direction;
        }

        public override bool Equals(object obj)
        {
            return obj is DoorKey other &&
                floor == other.floor &&
                x == other.x &&
                y == other.y &&
                direction == other.direction;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + floor;
                hash = hash * 31 + x;
                hash = hash * 31 + y;
                hash = hash * 31 + (int)direction;
                return hash;
            }
        }
    }

    void CreateFloorParentObjects()
    {
        floorParents = new GameObject[floorCount];
        for (int i = 0; i < floorCount; i++)
        {
            floorParents[i] = new GameObject($"Floor_{i}");
            floorParents[i].transform.SetParent(floorParent.transform);
        }
    }
}

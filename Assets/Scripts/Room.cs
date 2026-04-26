using System.Collections.Generic;
using UnityEngine;

public class Room : MonoBehaviour
{
    public GameObject NorthWall;
    public GameObject NorthDoor;
    public GameObject SouthWall;
    public GameObject SouthDoor;
    public GameObject EastWall;
    public GameObject EastDoor;
    public GameObject WestWall;
    public GameObject WestDoor;

    public string RoomId;
    public Transform PlayerSpawnPoint;
    public Transform MonsterPoint;
    public List<Room> ConnectedRooms = new();
}

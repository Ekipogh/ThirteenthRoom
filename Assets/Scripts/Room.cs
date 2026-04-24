using System.Collections.Generic;
using UnityEngine;

public class Room : MonoBehaviour
{
    public GameObject northWall;
    public GameObject northDoor;
    public GameObject southWall;
    public GameObject southDoor;
    public GameObject eastWall;
    public GameObject eastDoor;
    public GameObject westWall;
    public GameObject westDoor;

    public string roomId;
    public Transform playerSpawnPoint;
    public Transform monsterPoint;
    public List<Room> connectedRooms = new();
}

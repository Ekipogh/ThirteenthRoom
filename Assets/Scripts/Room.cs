using System.Collections.Generic;
using UnityEngine;

public enum RoomDirection
{
    North,
    South,
    East,
    West
}

public class Room : MonoBehaviour
{
    public string RoomId;
    public Transform PlayerSpawnPoint;
    public Transform MonsterPoint;
    // Connected Rooms
    [SerializeField] Room North;
    [SerializeField] Room South;
    [SerializeField] Room East;
    [SerializeField] Room West;
    public List<Room> ConnectedRooms
    {
        get
        {
            List<Room> connectedRooms = new();
            if (North != null) connectedRooms.Add(North);
            if (South != null) connectedRooms.Add(South);
            if (East != null) connectedRooms.Add(East);
            if (West != null) connectedRooms.Add(West);
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
        return direction switch
        {
            RoomDirection.North => North != null,
            RoomDirection.South => South != null,
            RoomDirection.East => East != null,
            RoomDirection.West => West != null,
            _ => false
        };
    }
}

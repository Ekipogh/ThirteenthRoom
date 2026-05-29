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
    public List<Room> ConnectedRooms = new();
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

    public bool TryGetDirectionTo(Room other, out RoomDirection direction)
    {
        direction = RoomDirection.North;
        if (other == null || other == this)
        {
            return false;
        }

        Vector3 offset = other.transform.position - transform.position;
        offset.y = 0f;
        if (offset == Vector3.zero)
        {
            return false;
        }

        if (Mathf.Abs(offset.z) >= Mathf.Abs(offset.x))
        {
            direction = offset.z >= 0f ? RoomDirection.North : RoomDirection.South;
        }
        else
        {
            direction = offset.x >= 0f ? RoomDirection.East : RoomDirection.West;
        }

        return true;
    }

    public bool HasConnectedRoomInDirection(RoomDirection direction)
    {
        foreach (Room connectedRoom in ConnectedRooms)
        {
            if (TryGetDirectionTo(connectedRoom, out RoomDirection connectedDirection) && connectedDirection == direction)
            {
                return true;
            }
        }

        return false;
    }
}

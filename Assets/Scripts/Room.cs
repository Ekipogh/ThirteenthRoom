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
    [SerializeField] GameObject lightObjectsParent;

    void Awake()
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
}

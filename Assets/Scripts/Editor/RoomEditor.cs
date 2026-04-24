using UnityEngine;
using UnityEditor;
using Codice.Client.BaseCommands.WkStatus.Printers;

[CustomEditor(typeof(Room))]
public class RoomEditor : Editor
{
    Room room;

    void OnEnable()
    {
        room = (Room)target;
    }

    void ToggleWall(GameObject wall, GameObject door)
    {
        bool isActive = wall.activeSelf;
        wall.SetActive(!isActive);
        door.SetActive(isActive);
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GUILayout.Space(10);

        if (room.northWall != null && room.northDoor != null)
        {
            if (GUILayout.Button("Toggle North"))
            {
                ToggleWall(room.northWall, room.northDoor);
            }
        }
        if (room.southWall != null && room.southDoor != null)
        {
            if (GUILayout.Button("Toggle South"))
            {
                ToggleWall(room.southWall, room.southDoor);
            }
        }
        if (room.eastWall != null && room.eastDoor != null)
        {
            if (GUILayout.Button("Toggle East"))
            {
                {
                    ToggleWall(room.eastWall, room.eastDoor);
                }
            }
        }
        if (room.westWall != null && room.westDoor != null)
        {
            if (GUILayout.Button("Toggle West"))
            {
                ToggleWall(room.westWall, room.westDoor);
            }
        }
    }
}

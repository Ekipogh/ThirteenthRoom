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

        if (room.NorthWall != null && room.NorthDoor != null)
        {
            if (GUILayout.Button("Toggle North"))
            {
                ToggleWall(room.NorthWall, room.NorthDoor);
            }
        }
        if (room.SouthWall != null && room.SouthDoor != null)
        {
            if (GUILayout.Button("Toggle South"))
            {
                ToggleWall(room.SouthWall, room.SouthDoor);
            }
        }
        if (room.EastWall != null && room.EastDoor != null)
        {
            if (GUILayout.Button("Toggle East"))
            {
                {
                    ToggleWall(room.EastWall, room.EastDoor);
                }
            }
        }
        if (room.WestWall != null && room.WestDoor != null)
        {
            if (GUILayout.Button("Toggle West"))
            {
                ToggleWall(room.WestWall, room.WestDoor);
            }
        }
    }
}

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

        if (GUILayout.Button("Toggle North"))
        {
            ToggleWall(room.northWall, room.northDoor);
        }
        if (GUILayout.Button("Toggle South"))
        {
            ToggleWall(room.southWall, room.southDoor);
        }
        if (GUILayout.Button("Toggle East"))
        {
            ToggleWall(room.eastWall, room.eastDoor);
        }
        if (GUILayout.Button("Toggle West"))
        {
            ToggleWall(room.westWall, room.westDoor);
        }
    }
}

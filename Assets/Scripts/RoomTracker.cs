using UnityEngine;

public class RoomTracker : MonoBehaviour
{
    public Room CurrentRoom { get; private set; }

    public void Initialize(Room startingRoom)
    {
        if (startingRoom == null)
        {
            Debug.LogWarning("RoomTracker.Initialize: startingRoom is null.");
            return;
        }
        CurrentRoom = startingRoom;
        Debug.Log($"RoomTracker initialized to room: {startingRoom.name}");
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<Room>(out var room))
        {
            CurrentRoom = room;
            Debug.Log($"Entered room: {room.name}");
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

/// <summary>A player-only shortcut, bound once for each generated mansion.</summary>
[DisallowMultipleComponent]
public sealed class EscapeLadderInteractable : MonoBehaviour, IInteractable
{
    Room destination;
    MansionModel mansion;
    PlayerController player;
    bool navigationReady;
    bool arrivalAvailable;
    float nextPromptCheck;
    Vector3 arrival;

    public Room Destination => destination;

    public void ClearDestination()
    {
        destination = null;
        mansion = null;
        player = null;
        navigationReady = false;
        arrivalAvailable = false;
        nextPromptCheck = 0f;
    }

    public void BindDestination(MansionModel model, Room entrance, PlayerController controller, bool ready)
    {
        ClearDestination();
        mansion = model;
        player = controller;
        navigationReady = ready;
        if (!ready || model == null || controller == null) return;
        Room source = GetComponentInParent<Room>();
        if (source == null) return;

        var candidates = new List<Room>();
        foreach (Room room in model.Rooms)
        {
            if (room == null || !room.gameObject.activeInHierarchy || room == source || room == entrance ||
                room is Landing || room.RoomType == RoomType.Dynamic || candidates.Contains(room)) continue;
            if (TryFindArrival(room, out _)) candidates.Add(room);
        }
        if (candidates.Count == 0) return;
        destination = candidates[Random.Range(0, candidates.Count)];
        arrivalAvailable = TryFindArrival(destination, out arrival);
    }

    bool TryFindArrival(Room room, out Vector3 point)
    {
        point = default;
        if (room == null || player == null || mansion == null || !navigationReady ||
            !room.gameObject.activeInHierarchy) return false;
        bool belongs = false;
        foreach (Room member in mansion.Rooms) if (member == room) { belongs = true; break; }
        if (!belongs) return false;

        Vector3 preferred = room.PlayerSpawnPoint != null ? room.PlayerSpawnPoint.transform.position : room.transform.position;
        Quaternion facing = room.PlayerSpawnPoint != null ? room.PlayerSpawnPoint.transform.rotation : room.transform.rotation;
        bool Sample(Vector3 desired, out Vector3 sampled)
        {
            if (!MansionNavigation.TrySample(desired, out sampled, 1f)) return false;
            return MansionNavigation.FindRoom(mansion, sampled) == room && player.CanTeleportTo(room, sampled, facing);
        }
        if (Sample(preferred, out point)) return true;
        for (int radius = 2; radius <= 6; radius += 2)
            for (int x = -radius; x <= radius; x += 2)
                for (int z = -radius; z <= radius; z += 2)
                    if ((Mathf.Abs(x) == radius || Mathf.Abs(z) == radius) &&
                        Sample(preferred + room.transform.TransformDirection(new Vector3(x, 0f, z)), out point)) return true;
        for (int x = -12; x <= 12; x += 4)
            for (int z = -12; z <= 12; z += 4)
                if (Sample(room.transform.TransformPoint(new Vector3(x, 0f, z)), out point)) return true;
        return false;
    }

    public string GetInteractionPrompt(PlayerInteractor interactor)
    {
        if (!isActiveAndEnabled || !navigationReady || mansion == null)
            return "The escape route is not ready";
        if (destination == null) return "No safe escape route is available";
        if (Time.unscaledTime >= nextPromptCheck)
        {
            arrivalAvailable = TryFindArrival(destination, out arrival);
            nextPromptCheck = Time.unscaledTime + 0.25f;
        }
        return arrivalAvailable ? "Press E to climb the escape ladder" : "The escape route is blocked";
    }

    public void Interact(PlayerInteractor interactor)
    {
        if (!isActiveAndEnabled || interactor == null || player == null ||
            interactor.GetComponentInParent<PlayerController>() != player ||
            !TryFindArrival(destination, out Vector3 point)) return;
        Quaternion facing = destination.PlayerSpawnPoint != null
            ? destination.PlayerSpawnPoint.transform.rotation : destination.transform.rotation;
        player.TryTeleportTo(destination, point, facing);
    }
}

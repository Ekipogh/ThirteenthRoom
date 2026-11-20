using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

/// <summary>Owns the navigation data for one generated mansion.</summary>
public sealed class MansionNavigation : MonoBehaviour
{
    NavMeshSurface _surface;
    NavMeshData _ownedData;
    public bool IsReady { get; private set; }

    public bool Build(MansionModel mansion, Transform geometryRoot)
    {
        IsReady = false;
        if (_surface != null) _surface.RemoveData();
        if (_ownedData != null) Destroy(_ownedData);
        if (mansion == null || geometryRoot == null) return false;
        _surface = geometryRoot.GetComponent<NavMeshSurface>();
        if (_surface == null) _surface = geometryRoot.gameObject.AddComponent<NavMeshSurface>();
        _surface.agentTypeID = 0;
        _surface.collectObjects = CollectObjects.Children;
        _surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        _surface.overrideVoxelSize = true;
        _surface.voxelSize = 0.1f;
        foreach (Room room in mansion.Rooms)
        {
            foreach (Collider collider in room.GetComponentsInChildren<Collider>())
            {
                // Polygon mansion props are solid obstacles, never walkable
                // platforms. Rugs remain part of the floor.
                if (collider.isTrigger || !collider.name.StartsWith("SM_Prop_") || collider.name.Contains("Rug")) continue;
                var modifier = collider.GetComponent<NavMeshModifier>();
                if (modifier == null) modifier = collider.gameObject.AddComponent<NavMeshModifier>();
                modifier.overrideArea = true;
                modifier.area = 1; // Not Walkable
            }
            foreach (DoorInteractable door in room.GetComponentsInChildren<DoorInteractable>(true))
                door.ConfigureNavigation();
        }
        foreach (RoomConnection connection in mansion.Connections)
            if (connection.Door != null) connection.Door.ConfigureNavigation();
        Physics.SyncTransforms();
        _surface.BuildNavMesh();
        _ownedData = _surface.navMeshData;
        IsReady = _surface.navMeshData != null && NavMesh.CalculateTriangulation().vertices.Length > 0;
        if (!IsReady) Debug.LogError("Mansion navigation bake produced no walkable geometry.", this);
        return IsReady;
    }

    public static bool TrySample(Vector3 point, out Vector3 result, float radius = 2f)
    {
        if (NavMesh.SamplePosition(point, out NavMeshHit hit, radius, NavMesh.AllAreas)
            && Mathf.Abs(hit.position.y - point.y) <= 1.5f)
        {
            result = hit.position;
            return true;
        }
        result = default;
        return false;
    }

    public static Room FindRoom(MansionModel mansion, Vector3 position)
    {
        if (mansion == null) return null;
        Room closest = null;
        float best = float.PositiveInfinity;
        foreach (Room room in mansion.Rooms)
        {
            if (room == null) continue;
            Vector3 delta = position - room.transform.position;
            if (delta.y < -1f || delta.y >= Room.RoomSize.y - 1f) continue;
            float distance = delta.x * delta.x + delta.z * delta.z;
            if (distance < best) { best = distance; closest = room; }
        }
        return closest;
    }

    public static bool TryRoomPoint(Room room, out Vector3 point)
    {
        Vector3 preferred = room.MonsterPoint != null ? room.MonsterPoint.transform.position : room.transform.position;
        preferred.y = room.transform.position.y;
        if (TrySample(preferred, out point, 4f)) return true;
        // Furnishing changes can occupy the authored marker. Search the room's
        // floor, never its tables or a different storey.
        for (int x = -12; x <= 12; x += 4)
            for (int z = -12; z <= 12; z += 4)
                if (TrySample(room.transform.position + new Vector3(x, 0f, z), out point, 2f)) return true;
        point = default;
        return false;
    }

    public static RoomConnection NextConnection(MansionModel mansion, Room from, Room to)
    {
        if (mansion == null || from == null || to == null || from == to) return null;
        var queue = new Queue<Room>();
        var first = new Dictionary<Room, RoomConnection> { [from] = null };
        queue.Enqueue(from);
        while (queue.Count > 0)
        {
            Room room = queue.Dequeue();
            foreach (RoomConnection connection in mansion.GetConnections(room))
            {
                if (connection.To == null || first.ContainsKey(connection.To)) continue;
                first[connection.To] = room == from ? connection : first[room];
                if (connection.To == to) return first[connection.To];
                queue.Enqueue(connection.To);
            }
        }
        return null;
    }

    void OnDestroy()
    {
        IsReady = false;
        if (_surface != null) _surface.RemoveData();
        if (_ownedData != null) Destroy(_ownedData);
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Plans room choices and globally matches renewable object requirements before instantiation.</summary>
public static class ObjectDependencyPlanner
{
    public readonly struct Assignment
    {
        public readonly int RoomIndex;
        public readonly int PointIndex;
        public readonly SpawnObjectDefinition Definition;
        public Assignment(int room, int point, SpawnObjectDefinition definition)
        { RoomIndex = room; PointIndex = point; Definition = definition; }
    }

    public static bool TryPlan(IReadOnlyList<Room[]> options, out int[] choices,
        out List<Assignment> assignments, out string error, int searchBudget = 4096)
    {
        choices = new int[options.Count];
        assignments = null;
        error = null;
        foreach (var rooms in options)
        {
            if (rooms == null || rooms.Length == 0) { error = "A room cell has no compatible prefab."; return false; }
            foreach (var room in rooms)
            {
                if (room == null) { error = "Missing Room component in candidate prefab."; return false; }
                foreach (var point in Points(room))
                    if (point.ValidationError != null) { error = $"{room.name}/{point.name}: {point.ValidationError}"; return false; }
                foreach (var need in room.ObjectRequirements)
                    if (need == null || need.Object == null || !need.Object.IsValid || need.Quantity < 1)
                    { error = $"Room '{room.name}' has an invalid object requirement."; return false; }
            }
        }
        var visited = new HashSet<string>();
        bool success = Search(options, choices, visited, ref searchBudget, out assignments, out error);
        if (!success && searchBudget <= 0) error += " Object planning search budget exhausted.";
        return success;
    }

    // Inactive points are not providers: they may be hidden behind a dynamic wall or disabled furniture.
    public static ObjectSpawnPoint[] Points(Room room)
    {
        var points = new List<ObjectSpawnPoint>();
        foreach (var point in room.GetComponentsInChildren<ObjectSpawnPoint>(true))
        {
            bool active = point.enabled;
            for (Transform t = point.transform; t != room.transform && active; t = t.parent)
                active &= t.gameObject.activeSelf;
            if (active) points.Add(point);
        }
        return points.ToArray();
    }

    static bool Search(IReadOnlyList<Room[]> options, int[] choices, HashSet<string> visited,
        ref int budget, out List<Assignment> assignments, out string error)
    {
        assignments = null;
        error = "No compatible object assignment.";
        if (budget-- <= 0 || !visited.Add(string.Join(",", choices))) return false;
        var requirements = new Dictionary<SpawnObjectDefinition, int>();
        var points = new List<ObjectSpawnPoint>();
        var locations = new List<(int room, int point)>();
        for (int r = 0; r < options.Count; r++)
        {
            Room room = options[r][choices[r]];
            foreach (var need in room.ObjectRequirements)
                requirements[need.Object] = Math.Max(requirements.TryGetValue(need.Object, out int count) ? count : 0, need.Quantity);
            var roomPoints = Points(room);
            for (int p = 0; p < roomPoints.Length; p++) { points.Add(roomPoints[p]); locations.Add((r, p)); }
        }
        var demands = new List<SpawnObjectDefinition>();
        SpawnObjectDefinition missing = null;
        foreach (var pair in requirements)
        {
            // Cap expansion for bad authoring rather than allocating an unbounded list.
            int possible = 0;
            foreach (var roomOptions in options)
            {
                int best = 0;
                foreach (var room in roomOptions)
                {
                    int count = 0;
                    foreach (var point in Points(room)) if (point.Accepts(pair.Key)) count++;
                    best = Math.Max(best, count);
                }
                possible += best;
            }
            if (pair.Value > possible)
            { missing = pair.Key; break; }
            for (int n = 0; n < pair.Value; n++) demands.Add(pair.Key);
        }
        int[] owners = new int[points.Count];
        Array.Fill(owners, -1);
        for (int d = 0; missing == null && d < demands.Count; d++)
            if (!Match(d, demands, points, owners, new bool[points.Count])) { missing = demands[d]; break; }
        if (missing == null)
        {
            assignments = new List<Assignment>();
            for (int p = 0; p < owners.Length; p++)
                if (owners[p] >= 0) assignments.Add(new Assignment(locations[p].room, locations[p].point, demands[owners[p]]));
            return true;
        }
        error = $"Object '{missing.name}' requires {requirements[missing]} compatible points; selected rooms lack free compatible capacity.";
        string failure = error;
        // Change provider rooms only when needed, preserving the randomized first choice elsewhere.
        // Same-capacity alternatives matter when other objects compete for flexible points.
        for (int r = 0; r < options.Count && budget > 0; r++)
        {
            int previous = choices[r];
            for (int c = 0; c < options[r].Length && budget > 0; c++)
            {
                if (c == previous) continue;
                bool provides = false;
                foreach (var point in Points(options[r][c])) if (point.Accepts(missing)) { provides = true; break; }
                int oldNeed = 0, newNeed = 0;
                foreach (var need in options[r][previous].ObjectRequirements)
                    if (need.Object == missing) oldNeed = Math.Max(oldNeed, need.Quantity);
                foreach (var need in options[r][c].ObjectRequirements)
                    if (need.Object == missing) newNeed = Math.Max(newNeed, need.Quantity);
                if (!provides && newNeed >= oldNeed) continue;
                choices[r] = c;
                if (Search(options, choices, visited, ref budget, out assignments, out error)) return true;
            }
            choices[r] = previous;
        }
        error = failure;
        return false;
    }

    static bool Match(int demand, List<SpawnObjectDefinition> demands, List<ObjectSpawnPoint> points,
        int[] owners, bool[] seen)
    {
        for (int p = 0; p < points.Count; p++)
        {
            if (seen[p] || !points[p].Accepts(demands[demand])) continue;
            seen[p] = true;
            if (owners[p] < 0 || Match(owners[p], demands, points, owners, seen))
            { owners[p] = demand; return true; }
        }
        return false;
    }
}

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

/// <summary>Executable Unity checks. Run RunFocusedChecks in Play Mode through script eval.</summary>
public static class MansionNavigationChecks
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Set(object instance, string name, object value) => instance.GetType().GetField(name, Private).SetValue(instance, value);
    static object Call(object instance, string name, params object[] arguments) => instance.GetType().GetMethod(name, Private).Invoke(instance, arguments);
    static void Assert(bool condition, string message) { if (!condition) throw new Exception("Navigation check failed: " + message); }

    public static string RunFocusedChecks()
    {
        Assert(Application.isPlaying, "Run focused checks in Play Mode");
        var root = new GameObject("Navigation test fixture");
        NavMeshSurface surface = null;
        int checks = 0;
        try
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.SetParent(root.transform);
            floor.transform.position = new Vector3(1000f, -0.1f, 1000f);
            floor.transform.localScale = new Vector3(40f, 0.2f, 40f);
            surface = root.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            Physics.SyncTransforms();
            surface.BuildNavMesh();

            var monsterObject = new GameObject("Test monster");
            monsterObject.transform.SetParent(root.transform);
            monsterObject.transform.position = new Vector3(1000f, 0f, 1000f);
            var monster = monsterObject.AddComponent<MonsterController>();
            monster.enabled = false;
            var agent = monster.GetComponent<NavMeshAgent>();
            agent.Warp(monsterObject.transform.position);
            var player = new GameObject("Test player");
            player.transform.SetParent(root.transform);
            player.transform.position = monsterObject.transform.position + Vector3.forward * 5f;
            var body = player.AddComponent<CharacterController>();
            body.height = 5f; body.radius = 1f; body.center = Vector3.up * 2.5f;
            monster.RoomTracker = player.AddComponent<RoomTracker>();
            var managerObject = new GameObject("Test game manager");
            managerObject.transform.SetParent(root.transform);
            var manager = managerObject.AddComponent<GameManager>();
            manager.enabled = false;
            monster.GameManager = manager;
            Physics.SyncTransforms();
            Assert(monster.CanSeePlayer(), "Unobstructed player must be visible"); checks++;
            player.transform.position = monsterObject.transform.position + new Vector3(18f, 0f, 8f);
            Physics.SyncTransforms();
            Assert(monster.CanDetectPlayer(), "Player entering the wider peripheral view must be detected"); checks++;
            player.transform.position = monsterObject.transform.position + Vector3.forward * 22f;
            Physics.SyncTransforms();
            Assert(monster.CanDetectPlayer(), "Player across a large room must be detected within sight range"); checks++;
            player.transform.position = monsterObject.transform.position - Vector3.forward * 5f;
            Physics.SyncTransforms();
            Assert(!monster.CanSeePlayer(), "Player behind the monster is outside visual field of view"); checks++;
            Assert(monster.CanDetectPlayer(), "Nearby player behind the monster must trigger awareness"); checks++;
            Call(monster, "Update");
            Assert(monster.CurrentState == MonsterState.Chasing, "Approaching from behind must start pursuit"); checks++;
            player.transform.position = monsterObject.transform.position - Vector3.forward * 12f;
            Physics.SyncTransforms();
            Assert(monster.CanDetectPlayer(), "Visible target behind a chasing monster must remain tracked"); checks++;
            Call(monster, "ChangeState", MonsterState.Roaming);
            Assert(!monster.CanDetectPlayer(), "Distant rear targets must still require visual detection before chasing"); checks++;
            player.transform.position = monsterObject.transform.position + Vector3.forward * 5f;
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.SetParent(root.transform);
            wall.transform.position = monsterObject.transform.position + new Vector3(0f,3f,3f);
            wall.transform.localScale = new Vector3(5f,6f,0.3f);
            Physics.SyncTransforms();
            Assert(!monster.CanSeePlayer(), "Wall must block detection"); checks++;
            Assert(!monster.CanDetectPlayer(), "Close awareness must not detect through walls or closed doors"); checks++;
            wall.SetActive(false);
            Physics.SyncTransforms();
            Call(monster, "Update");
            Assert(monster.CurrentState == MonsterState.Chasing, "Visible player must start chase"); checks++;
            wall.SetActive(true); Physics.SyncTransforms();
            Call(monster, "Update");
            Assert(monster.CurrentState == MonsterState.Chasing, "Brief occlusion must preserve chase"); checks++;
            Set(monster, "_concealedFor", 9f);
            Call(monster, "Update");
            Assert(monster.CurrentState == MonsterState.Searching, "Sustained concealment must start search"); checks++;
            Set(monster, "_searchUntil", Time.time - 1f);
            Set(monster, "_nextDestination", 0f);
            Call(monster, "Update");
            Assert(monster.CurrentState == MonsterState.Roaming, "Expired search must resume roaming"); checks++;
            Assert(!(bool)Call(monster, "SetDestination", new Vector3(10000f,0f,10000f)), "Unreachable destination must fail");
            Assert(!agent.hasPath, "Failed destination must not retain a stale path"); checks++;
            wall.SetActive(false);
            player.transform.position = monsterObject.transform.position + Vector3.forward * 2f;
            Physics.SyncTransforms();
            Assert(monster.CanAttackPlayer(), "Player within reach must be attackable"); checks++;
            player.transform.position += Vector3.up * 13.75f; Physics.SyncTransforms();
            Assert(!monster.CanAttackPlayer(), "Player on another storey must not be attackable"); checks++;
            player.transform.position -= Vector3.up * 13.75f;
            wall.transform.position = monsterObject.transform.position + new Vector3(0f,3f,1f);
            wall.SetActive(true); Physics.SyncTransforms();
            Assert(!monster.CanAttackPlayer(), "Wall must prevent close-range damage"); checks++;
            wall.SetActive(false); Physics.SyncTransforms();
            Call(monster, "ChangeState", MonsterState.Attacking);
            player.transform.position += Vector3.forward * 8f; Physics.SyncTransforms();
            Set(monster, "_attackStarted", Time.time - 0.6f);
            Call(monster, "Update");
            Assert(manager.currentState == GameState.Playing, "Moving out of reach before impact must avoid death"); checks++;
            Call(monster, "ChangeState", MonsterState.Roaming);
            player.transform.position -= Vector3.forward * 8f; Physics.SyncTransforms();
            Call(monster, "ChangeState", MonsterState.Attacking);
            Set(monster, "_attackStarted", Time.time - 0.6f);
            Call(monster, "Update");
            Assert(manager.currentState == GameState.GameOver, "Attack impact must trigger existing death handler");
            Assert((bool)monster.GetType().GetField("_impactApplied", Private).GetValue(monster), "Impact must be marked consumed");
            Call(monster, "Update");
            Assert(agent.isStopped, "Game over must stop navigation"); checks++;
            manager.currentState = GameState.Playing;
            Call(monster, "Update");
            Assert(manager.currentState == GameState.Playing, "Consumed impact must not notify death again"); checks++;
            manager.currentState = GameState.Paused;
            Call(monster, "Update");
            Assert(agent.isStopped, "Pause must stop navigation"); checks++;

            var doorObject = new GameObject("Test door");
            doorObject.transform.SetParent(root.transform);
            doorObject.transform.position = new Vector3(1010f,0f,1000f);
            var door = doorObject.AddComponent<DoorInteractable>();
            door.OpenFrom(new Vector3(1009f,0f,1000f));
            float target = (float)typeof(DoorInteractable).GetField("_targetRelativeAngle", Private).GetValue(door);
            door.OpenFrom(new Vector3(1011f,0f,1000f));
            Assert(target == (float)typeof(DoorInteractable).GetField("_targetRelativeAngle", Private).GetValue(door), "Repeated monster open must not reverse moving door"); checks++;
            door.transform.localRotation = Quaternion.Euler(0f,target,0f);
            Set(door, "_isMoving", false);
            Assert(door.IsPassable, "Fully open door must be passable"); checks++;
            return checks + " focused navigation checks passed";
        }
        finally
        {
            if (surface != null) surface.RemoveData();
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    public static string AuditGeneratedMansion()
    {
        var generator = UnityEngine.Object.FindFirstObjectByType<MansionGenerator>();
        Assert(generator != null && generator.NavigationReady, "Generated navigation must be ready");
        int paths = 0;
        var failures = new List<string>();
        foreach (var c in generator.CurrentMansion.Connections)
        {
            var path = new NavMeshPath();
            if (!MansionNavigation.TryRoomPoint(c.From, out var from) || !MansionNavigation.TryRoomPoint(c.To, out var to)
                || !NavMesh.CalculatePath(from, to, NavMesh.AllAreas, path) || path.status != NavMeshPathStatus.PathComplete)
                failures.Add(c.From.name + " -> " + c.To.name);
            else paths++;
        }
        Assert(failures.Count == 0, string.Join("; ", failures));
        return paths + " complete generated connection paths passed";
    }

    public static string GenerateSeed(int seed, int floors = 2)
    {
        Assert(Application.isPlaying, "Generate seed only in Play Mode");
        var generator = UnityEngine.Object.FindFirstObjectByType<MansionGenerator>();
        foreach (var monster in UnityEngine.Object.FindObjectsByType<MonsterController>(FindObjectsSortMode.None))
            UnityEngine.Object.DestroyImmediate(monster.gameObject);
        foreach (string field in new[] { "floorParent", "doorParent" })
        {
            var parent = (GameObject)typeof(MansionGenerator).GetField(field, Private).GetValue(generator);
            while (parent.transform.childCount > 0) UnityEngine.Object.DestroyImmediate(parent.transform.GetChild(0).gameObject);
        }
        UnityEngine.Random.InitState(seed);
        generator.floorCount = floors;
        var mansion = generator.GenerateMansion();
        UnityEngine.Object.FindFirstObjectByType<MonsterSpawner>().SpawnMonster(mansion);
        foreach (var door in UnityEngine.Object.FindObjectsByType<DoorInteractable>(FindObjectsSortMode.None))
            door.OpenFrom(door.transform.position);
        return "Generated seed " + seed + "; wait for doors to finish before auditing";
    }
}
#endif

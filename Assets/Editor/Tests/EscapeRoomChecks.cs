#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

public static class EscapeRoomChecks
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Set(object target, string field, object value) => target.GetType().GetField(field, Private).SetValue(target, value);
    static object Get(object target, string field) => target.GetType().GetField(field, Private).GetValue(target);

    public static string RunFocusedChecks()
    {
        if (!Application.isPlaying) throw new Exception("Run in Play Mode");
        var fixture = new GameObject("Escape room checks");
        NavMeshSurface surface = null;
        ItemDefinition item = null;
        int checks = 0;
        void Check(bool condition, string message) { if (!condition) throw new Exception(message); checks++; }
        try
        {
            Room MakeRoom(string name, Vector3 position, bool landing = false)
            {
                var go = new GameObject(name); go.transform.SetParent(fixture.transform); go.transform.position = position;
                var objects = new GameObject("Objects"); objects.transform.SetParent(go.transform, false);
                new GameObject("Lights").transform.SetParent(objects.transform, false);
                new GameObject("ItemSpawnPoints").transform.SetParent(go.transform, false);
                Room room = landing ? go.AddComponent<Landing>() : go.AddComponent<Room>(); room.enabled = false;
                var floor = new GameObject("Floor"); floor.transform.SetParent(go.transform, false);
                var tile = GameObject.CreatePrimitive(PrimitiveType.Cube); tile.transform.SetParent(floor.transform, false);
                tile.transform.localPosition = new Vector3(0, -.1f, 0); tile.transform.localScale = new Vector3(30, .2f, 30);
                var spawn = new GameObject("PlayerSpawnPoint"); spawn.transform.SetParent(go.transform, false);
                spawn.transform.localPosition = new Vector3(0, 0, 10); spawn.transform.localRotation = Quaternion.Euler(0, 90, 0);
                room.PlayerSpawnPoint = spawn.AddComponent<PointNode>();
                return room;
            }
            Room source = MakeRoom("Source", new Vector3(2000, 0, 2000));
            Room target = MakeRoom("Target", new Vector3(2100, 15, 2000));
            Room entrance = MakeRoom("Entrance", new Vector3(2200, 0, 2000));
            Room landingRoom = MakeRoom("Landing", new Vector3(2300, 0, 2000), true);
            surface = fixture.AddComponent<NavMeshSurface>(); surface.collectObjects = CollectObjects.Children;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            Physics.SyncTransforms(); surface.BuildNavMesh();

            var body = new GameObject("Player"); body.transform.SetParent(fixture.transform); body.transform.position = new Vector3(1950, 0, 2000);
            var cc = body.AddComponent<CharacterController>(); cc.height = 2; cc.radius = .4f; cc.center = Vector3.up;
            var player = body.AddComponent<PlayerController>(); player.enabled = false;
            var tracker = body.AddComponent<RoomTracker>(); tracker.Initialize(source); Set(player, "RoomTracker", tracker);
            var head = new GameObject("Head"); head.transform.SetParent(body.transform, false); player.HeadJoint = head.transform;
            var interactor = body.AddComponent<PlayerInteractor>(); interactor.enabled = false;
            var torch = new GameObject("Torch"); torch.transform.SetParent(body.transform, false); Set(player, "TorchLight", torch); player.EnableTorch();
            Set(player, "_stamina", 42f); Set(player, "_velocity", new Vector3(0, -99, 0)); Set(player, "_distanceTraveled", 10f);
            item = ScriptableObject.CreateInstance<ItemDefinition>(); interactor.Inventory.AddItem(item);
            var ladderObject = new GameObject("Ladder"); ladderObject.transform.SetParent(source.transform, false);
            var ladder = ladderObject.AddComponent<EscapeLadderInteractable>();
            var model = new MansionModel(new[] { source, target, entrance, landingRoom }, Array.Empty<RoomConnection>(), Array.Empty<SpawnItemDefinition>());
            ladder.BindDestination(model, entrance, player, false);
            Check(ladder.Destination == null && ladder.GetInteractionPrompt(interactor).Contains("not ready"), "Navigation readiness must gate binding");
            ladder.BindDestination(model, entrance, player, true);
            Check(ladder.Destination == target, "Only the other ordinary room is eligible, including on another floor");
            ladder.Interact(interactor);
            Check(tracker.CurrentRoom == target && Vector3.Distance(body.transform.position, target.PlayerSpawnPoint.transform.position) < 1.1f, "Teleport must update position and room immediately");
            Check(cc.enabled && player.Stamina == 42 && torch.activeSelf && interactor.Inventory.HasItem(item), "Player resources and controller state must survive teleport");
            Check((Vector3)Get(player, "_velocity") == Vector3.zero && (float)Get(player, "_distanceTraveled") == 0f && Mathf.Abs(Mathf.DeltaAngle(body.transform.eulerAngles.y, 90)) < .1f, "Teleport must clear movement accumulation and apply arrival orientation");
            Check(player.TryTeleportTo(source, source.PlayerSpawnPoint.transform.position, Quaternion.identity), "Player must still be movable after teleport");
            ladder.Interact(interactor);
            Check(ladder.Destination == target && tracker.CurrentRoom == target, "Repeated use must retain destination");

            player.TryTeleportTo(source, source.PlayerSpawnPoint.transform.position, Quaternion.identity);
            var spawnBlocker = GameObject.CreatePrimitive(PrimitiveType.Cube); spawnBlocker.transform.SetParent(target.transform, false);
            spawnBlocker.transform.position = target.PlayerSpawnPoint.transform.position + Vector3.up * 1.5f;
            spawnBlocker.transform.localScale = new Vector3(3, 3, 3); Physics.SyncTransforms();
            ladder.Interact(interactor);
            Check(tracker.CurrentRoom == target && Vector3.Distance(body.transform.position, target.PlayerSpawnPoint.transform.position) > 1.5f,
                "An occupied preferred spawn must use a clear point in the same destination room");
            UnityEngine.Object.DestroyImmediate(spawnBlocker); Physics.SyncTransforms();
            player.TryTeleportTo(source, source.PlayerSpawnPoint.transform.position, Quaternion.identity);
            Vector3 before = body.transform.position;
            var blocker = GameObject.CreatePrimitive(PrimitiveType.Cube); blocker.transform.SetParent(target.transform, false);
            blocker.transform.localPosition = Vector3.up * 3; blocker.transform.localScale = new Vector3(36, 6, 36); Physics.SyncTransforms();
            ladder.Interact(interactor);
            Check(body.transform.position == before && ladder.Destination == target, "A blocked room must refuse travel without selecting another room");
            ladder.BindDestination(model, entrance, player, true);
            Check(ladder.Destination == null, "No safe candidate must disable travel");
            UnityEngine.Object.DestroyImmediate(blocker); Physics.SyncTransforms();
            ladder.BindDestination(model, entrance, player, true);
            Check(ladder.Destination == target, "Rebinding must discard the previous binding state");
            var invalid = new MansionModel(new[] { source, entrance, landingRoom }, Array.Empty<RoomConnection>(), Array.Empty<SpawnItemDefinition>());
            ladder.BindDestination(invalid, entrance, player, true);
            Check(ladder.Destination == null, "Self, entrance and landing must never become a destination");
            Check(!player.TryTeleportTo(target, target.transform.position + Vector3.up * 7, Quaternion.identity) && body.transform.position == before, "Unsupported arrivals must fail without moving the player");
            return checks + " escape room checks passed";
        }
        finally
        {
            if (surface != null)
            {
                var data = surface.navMeshData; surface.RemoveData();
                if (data != null) UnityEngine.Object.DestroyImmediate(data);
            }
            UnityEngine.Object.DestroyImmediate(fixture);
            if (item != null) UnityEngine.Object.DestroyImmediate(item);
        }
    }
}
#endif

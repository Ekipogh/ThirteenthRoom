#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class ObjectDependencyChecks
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Assert(bool condition, string message) { if (!condition) throw new Exception("Object dependency check failed: " + message); }
    static void Needs(Room room, params ObjectRequirement[] needs) => typeof(Room).GetField("objectRequirements", Private).SetValue(room, new List<ObjectRequirement>(needs));
    static ObjectRequirement Need(SpawnObjectDefinition definition, int count = 1) => new ObjectRequirement { Object = definition, Quantity = count };

    public static string RunFocusedChecks()
    {
        var root = new GameObject("Object dependency fixture");
        root.SetActive(false);
        var assets = new List<SpawnObjectDefinition>();
        int checks = 0;
        try
        {
            SpawnObjectDefinition Definition(string name, ObjectSize size)
            {
                var definition = ScriptableObject.CreateInstance<SpawnObjectDefinition>();
                definition.name = name; definition.RequiredSize = size;
                definition.Prefab = new GameObject(name + " prefab");
                definition.Prefab.transform.SetParent(root.transform, false);
                assets.Add(definition); return definition;
            }
            Room MakeRoom(string name)
            {
                var go = new GameObject(name); go.transform.SetParent(root.transform, false);
                return go.AddComponent<Room>();
            }
            ObjectSpawnPoint Point(Room room, params SpawnObjectDefinition[] allowed)
            {
                var go = new GameObject("Object point"); go.transform.SetParent(room.transform, false);
                var point = go.AddComponent<ObjectSpawnPoint>();
                point.MaximumSize = ObjectSize.Large; point.AllowedObjects = allowed; return point;
            }
            var a = Definition("Plant", ObjectSize.Small);
            var b = Definition("Furniture", ObjectSize.Medium);
            var large = Definition("Large", ObjectSize.Large);
            var office = MakeRoom("Office");
            var point = Point(office, a, b, large);
            foreach (ObjectSize size in Enum.GetValues(typeof(ObjectSize)))
            {
                point.MaximumSize = size;
                foreach (var definition in assets)
                { Assert(point.Accepts(definition) == (definition.RequiredSize <= size), "Size tier comparison"); checks++; }
            }
            var unlisted = Definition("Unlisted", ObjectSize.Small);
            Assert(!point.Accepts(unlisted), "Only allowed definitions accepted"); checks++;
            point.DefaultObject = unlisted;
            Assert(point.ValidationError != null, "Invalid default rejected"); checks++;
            point.DefaultObject = a;
            Assert(point.ValidationError == null, "Valid default accepted"); checks++;
            point.transform.localPosition = new Vector3(2, 3, 4);
            point.transform.localRotation = Quaternion.Euler(0, 90, 0);
            a.Prefab.transform.localScale = new Vector3(2, 3, 4);
            var spawned = point.Spawn(a);
            Assert(spawned.transform.parent == point.transform && spawned.transform.localPosition == Vector3.zero && spawned.transform.localRotation == Quaternion.identity && spawned.transform.localScale == a.Prefab.transform.localScale, "Spawn preserves point pose and prefab scale"); checks++;
            Assert(!point.CanSpawn(a), "Occupied points reject second object"); checks++;
            UnityEngine.Object.DestroyImmediate(spawned);
            var lab = MakeRoom("Laboratory"); Needs(lab, Need(a));
            var lab2 = MakeRoom("Second laboratory"); Needs(lab2, Need(a));
            var empty = MakeRoom("Empty room");
            var provider = MakeRoom("Office with plant point"); Point(provider, a);
            Assert(ObjectDependencyPlanner.TryPlan(new[]{new[]{lab},new[]{lab2},new[]{empty,provider}}, out var choices, out var assignments, out var error), error); checks++;
            Assert(choices[2] == 1 && assignments.Count == 1, "Office supplies shared source without a Garden"); checks++;
            Needs(lab2, Need(a, 2));
            Assert(!ObjectDependencyPlanner.TryPlan(new[]{new[]{lab},new[]{lab2},new[]{provider}}, out _, out _, out error) && error.Contains("Plant"), "Quantities use maximum and failure identifies object"); checks++;
            var flexible = MakeRoom("Flexible provider"); Point(flexible, a, b); Point(flexible, a);
            Needs(lab, Need(a), Need(b));
            Assert(ObjectDependencyPlanner.TryPlan(new[]{new[]{lab},new[]{flexible}}, out _, out assignments, out error) && assignments.Count == 2, "Global matching reassigns flexible point"); checks++;
            Needs(lab, Need(a)); Needs(provider, Need(b));
            var furnitureRoom = MakeRoom("Furniture provider"); Point(furnitureRoom, b);
            Assert(ObjectDependencyPlanner.TryPlan(new[]{new[]{lab},new[]{empty,provider},new[]{empty,furnitureRoom}}, out choices, out assignments, out error) && choices[1] == 1 && choices[2] == 1 && assignments.Count == 2, "Provider-introduced requirements resolved"); checks++;
            Needs(provider, Need(unlisted));
            Assert(ObjectDependencyPlanner.TryPlan(new[]{new[]{provider,empty}}, out choices, out _, out error) && choices[0] == 1, "Optional impossible provider replaced"); checks++;
            Assert(!ObjectDependencyPlanner.TryPlan(new[]{new[]{lab},new[]{empty,office}}, out _, out _, out _, 0), "Search budget enforced"); checks++;
            point.gameObject.SetActive(false);
            Assert(ObjectDependencyPlanner.Points(office).Length == 0, "Inactive points excluded"); checks++;
            foreach (var path in new[]{"Assets/Prefabs/Building/I_GardenRoom.prefab", "Assets/Prefabs/Building/J_GardenRoom.prefab"})
            {
                var garden = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Assert(garden.GetComponentsInChildren<PlantInteractable>(true).Length == 0, "No embedded Garden plants"); checks++;
                var points = ObjectDependencyPlanner.Points(garden.GetComponent<Room>());
                Assert(points.Length == 4 && Array.TrueForAll(points, p => p.DefaultObject != null && p.Accepts(p.DefaultObject)), "Four valid default plant points"); checks++;
            }
            return $"Passed {checks} focused object-dependency checks.";
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
            foreach (var asset in assets) UnityEngine.Object.DestroyImmediate(asset);
        }
    }

    public static string RunSeededChecks(int seedsPerFloorCount = 40)
    {
        var preview = UnityEditor.SceneManagement.EditorSceneManager.OpenPreviewScene("Assets/Scenes/GameScene.unity");
        MansionGenerator source = null;
        foreach (var root in preview.GetRootGameObjects())
        {
            source = root.GetComponentInChildren<MansionGenerator>(true);
            if (source != null) break;
        }
        if (source == null)
        {
            UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(preview);
            throw new Exception("GameScene has no MansionGenerator.");
        }
        var generatorObject = new GameObject("Seeded planner fixture");
        generatorObject.SetActive(false);
        var generator = generatorObject.AddComponent<MansionGenerator>();
        foreach (var field in typeof(MansionGenerator).GetFields(Private | BindingFlags.Public))
            if (!field.IsInitOnly) field.SetValue(generator, field.GetValue(source));
        var randomState = UnityEngine.Random.state;
        int plans = 0;
        try
        {
            for (int floors = 1; floors <= 3; floors++)
                for (int seed = 0; seed < seedsPerFloorCount; seed++)
                {
                    generator.floorCount = floors;
                    UnityEngine.Random.InitState(seed);
                    bool success = false;
                    string failure = null;
                    for (int attempt = 0; attempt < 50 && !success; attempt++)
                    {
                        var layouts = typeof(MansionGenerator).GetMethod("GenerateLayouts", Private).Invoke(generator, null);
                        var unique = typeof(MansionGenerator).GetMethod("CreateUniqueRoomPlacements", Private).Invoke(generator, new[]{layouts});
                        if (unique == null) continue;
                        object[] args = { layouts, unique, null, null, null };
                        success = (bool)typeof(MansionGenerator).GetMethod("TryPlanRooms", Private).Invoke(generator, args);
                        failure = args[4] as string;
                        if (success)
                        {
                            var selectedRooms = new List<Room> { (Room)((GameObject)typeof(MansionGenerator).GetField("entrancePrefab", Private).GetValue(generator)).GetComponent<Room>() };
                            if (floors > 1)
                                for (int floor = 0; floor < floors; floor++) selectedRooms.Add(((GameObject)typeof(MansionGenerator).GetField("stairsPrefab", Private).GetValue(generator)).GetComponent<Room>());
                            foreach (var placement in (System.Collections.IEnumerable)args[2])
                                selectedRooms.Add(((GameObject)placement.GetType().GetField("Prefab").GetValue(placement)).GetComponent<Room>());
                            Assert(selectedRooms.Count == 1 + floors * generator.width * generator.height, "Every cell planned");
                            var spawnedCounts = new Dictionary<SpawnObjectDefinition, int>();
                            var used = new HashSet<(int room, int point)>();
                            foreach (var assignment in (List<ObjectDependencyPlanner.Assignment>)args[3])
                            {
                                Assert(used.Add((assignment.RoomIndex, assignment.PointIndex)), "No point assigned twice");
                                Assert(ObjectDependencyPlanner.Points(selectedRooms[assignment.RoomIndex])[assignment.PointIndex].Accepts(assignment.Definition), "Assignment fits selected prefab point");
                                spawnedCounts[assignment.Definition] = spawnedCounts.TryGetValue(assignment.Definition, out int n) ? n + 1 : 1;
                            }
                            foreach (var room in selectedRooms)
                                foreach (var need in room.ObjectRequirements)
                                    Assert(spawnedCounts.TryGetValue(need.Object, out int n) && n >= need.Quantity, "Every selected room requirement satisfied");
                        }
                    }
                    Assert(success, $"Seed {seed}, floors {floors}: {failure}");
                    plans++;
                }
            return $"Passed {plans} seeded mansion plans across one, two, and three floors.";
        }
        finally
        {
            UnityEngine.Random.state = randomState;
            UnityEngine.Object.DestroyImmediate(generatorObject);
            UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(preview);
        }
    }
}
#endif

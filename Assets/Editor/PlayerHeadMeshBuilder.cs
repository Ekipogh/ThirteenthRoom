using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Builds persistent camera-independent head/body meshes without modifying the imported model.</summary>
public static class PlayerHeadMeshBuilder
{
    public const string PrefabPath = "Assets/Prefabs/Player.prefab";
    public const string MeshFolder = "Assets/Generated/PlayerMeshes";
    public const string HeadLayer = "PlayerHead";
    const string ModelGuid = "5572a64bd7a852043ad459aa87aa5d10";
    public const float HeadWeightThreshold = 0.001f;

    public static bool[] HeadVertices(Mesh source, Transform[] bones, Transform head)
    {
        var headBones = new HashSet<int>();
        for (int i = 0; i < bones.Length; i++)
            if (bones[i] != null && (bones[i] == head || bones[i].IsChildOf(head))) headBones.Add(i);
        if (headBones.Count == 0) throw new InvalidOperationException("Renderer has no head bones: " + source.name);
        var result = new bool[source.vertexCount];
        var counts = source.GetBonesPerVertex();
        var weights = source.GetAllBoneWeights();
        try
        {
            int offset = 0;
            for (int vertex = 0; vertex < result.Length; vertex++)
            {
                float total = 0;
                for (int j = 0; j < counts[vertex]; j++)
                {
                    var weight = weights[offset++];
                    if (headBones.Contains(weight.boneIndex)) total += weight.weight;
                }
                result[vertex] = total > HeadWeightThreshold;
            }
        }
        finally { counts.Dispose(); weights.Dispose(); }
        return result;
    }

    public static Mesh Partition(Mesh source, bool[] headVertices, bool keepHead)
    {
        var mesh = UnityEngine.Object.Instantiate(source);
        mesh.name = source.name + (keepHead ? "_Head" : "_Body");
        for (int submesh = 0; submesh < source.subMeshCount; submesh++)
        {
            if (source.GetTopology(submesh) != MeshTopology.Triangles)
                throw new InvalidOperationException("Expected triangular character mesh.");
            var triangles = source.GetTriangles(submesh);
            var kept = new List<int>();
            for (int i = 0; i < triangles.Length; i += 3)
            {
                bool isHead = headVertices[triangles[i]] || headVertices[triangles[i + 1]] || headVertices[triangles[i + 2]];
                if (isHead != keepHead) continue;
                kept.Add(triangles[i]); kept.Add(triangles[i + 1]); kept.Add(triangles[i + 2]);
            }
            mesh.SetTriangles(kept, submesh, false);
        }
        mesh.bounds = source.bounds;
        return mesh;
    }

    static Mesh SaveMesh(Mesh mesh)
    {
        string path = MeshFolder + "/" + mesh.name + ".asset";
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing == null) { AssetDatabase.CreateAsset(mesh, path); return mesh; }
        EditorUtility.CopySerialized(mesh, existing);
        UnityEngine.Object.DestroyImmediate(mesh);
        EditorUtility.SetDirty(existing);
        return existing;
    }

    static SkinnedMeshRenderer AddHead(SkinnedMeshRenderer body, Mesh mesh, string name, int layer, ShadowCastingMode shadows)
    {
        var child = new GameObject(name);
        child.transform.SetParent(body.transform, false);
        child.layer = layer;
        var renderer = child.AddComponent<SkinnedMeshRenderer>();
        EditorUtility.CopySerialized(body, renderer);
        renderer.sharedMesh = mesh;
        renderer.shadowCastingMode = shadows;
        renderer.updateWhenOffscreen = true;
        return renderer;
    }

    [MenuItem("Tools/Player/Rebuild Head Visibility Meshes")]
    public static void Rebuild()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode before rebuilding player assets.");
        var tags = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var layers = tags.FindProperty("layers");
        int layer = LayerMask.NameToLayer(HeadLayer);
        if (layer < 0)
        {
            for (int i = 8; i < layers.arraySize; i++)
                if (string.IsNullOrEmpty(layers.GetArrayElementAtIndex(i).stringValue)) { layer = i; break; }
            if (layer < 0) throw new InvalidOperationException("No free layer for PlayerHead.");
            layers.GetArrayElementAtIndex(layer).stringValue = HeadLayer;
            tags.ApplyModifiedPropertiesWithoutUndo();
        }
        if (!AssetDatabase.IsValidFolder("Assets/Generated")) AssetDatabase.CreateFolder("Assets", "Generated");
        if (!AssetDatabase.IsValidFolder(MeshFolder)) AssetDatabase.CreateFolder("Assets/Generated", "PlayerMeshes");
        var sources = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(ModelGuid)).OfType<Mesh>().ToDictionary(m => m.name);
        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            var head = root.GetComponentsInChildren<Transform>(true).Single(t => t.name == "Head");
            head.localScale = Vector3.one;
            foreach (var renderer in root.GetComponentsInChildren<SkinnedMeshRenderer>(true).Where(r => sources.ContainsKey(r.name)).ToArray())
            {
                foreach (Transform child in renderer.transform.Cast<Transform>().ToArray())
                    if (child.name == "Head Visible" || child.name == "Head Shadow") UnityEngine.Object.DestroyImmediate(child.gameObject);
                var source = sources[renderer.name];
                var mask = HeadVertices(source, renderer.bones, head);
                var bodyMesh = SaveMesh(Partition(source, mask, false));
                var headMesh = SaveMesh(Partition(source, mask, true));
                renderer.sharedMesh = bodyMesh;
                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.updateWhenOffscreen = true;
                AddHead(renderer, headMesh, "Head Visible", layer, ShadowCastingMode.Off);
                AddHead(renderer, headMesh, "Head Shadow", renderer.gameObject.layer, ShadowCastingMode.ShadowsOnly);
            }
            var camera = root.GetComponentsInChildren<Camera>(true).Single(c => c.CompareTag("MainCamera"));
            camera.cullingMask &= ~(1 << layer);
            // Shadows and reflection cameras must keep receiving animation when the owner cannot see the mesh.
            foreach (var animator in root.GetComponentsInChildren<Animator>(true)) animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        AssetDatabase.SaveAssets();
    }
}

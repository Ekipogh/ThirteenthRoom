using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class PlayerHeadVisibilityTests
{
    static Type Builder => Type.GetType("PlayerHeadMeshBuilder, Assembly-CSharp-Editor", true);
    static object Call(string name, params object[] args) => Builder.GetMethod(name).Invoke(null, args);

    [Test]
    public void HeadClassificationSumsDescendantInfluenceAndPreservesUnweightedBody()
    {
        var root = new GameObject("Skeleton");
        var head = new GameObject("Head"); head.transform.SetParent(root.transform);
        var jaw = new GameObject("Jaw"); jaw.transform.SetParent(head.transform);
        var mesh = new Mesh();
        try
        {
            mesh.vertices = new Vector3[4];
            mesh.boneWeights = new[] {
                new BoneWeight { boneIndex0 = 0, weight0 = 1 },
                new BoneWeight { boneIndex0 = 1, weight0 = 0.0006f, boneIndex1 = 2, weight1 = 0.0006f, boneIndex2 = 0, weight2 = 0.9988f },
                new BoneWeight { boneIndex0 = 2, weight0 = 1 },
                new BoneWeight { boneIndex0 = 1, weight0 = 0.0005f, boneIndex1 = 0, weight1 = 0.9995f }
            };
            var mask = (bool[])Call("HeadVertices", mesh, new[] { root.transform, head.transform, jaw.transform }, head.transform);
            CollectionAssert.AreEqual(new[] { false, true, true, false }, mask);
        }
        finally { UnityEngine.Object.DestroyImmediate(mesh); UnityEngine.Object.DestroyImmediate(root); }
    }

    [Test]
    public void PartitionsKeepBoundaryTrianglesWholeAndRetainMeshAttributes()
    {
        var mesh = new Mesh(); Mesh body = null, head = null;
        try
        {
            mesh.vertices = new[] { Vector3.zero, Vector3.right, Vector3.up, Vector3.forward };
            mesh.uv = new[] { Vector2.zero, Vector2.right, Vector2.up, Vector2.one };
            mesh.bindposes = new[] { Matrix4x4.identity };
            mesh.boneWeights = Enumerable.Repeat(new BoneWeight { boneIndex0 = 0, weight0 = 1 }, 4).ToArray();
            mesh.subMeshCount = 2;
            mesh.SetTriangles(new[] { 0, 1, 2 }, 0);
            mesh.SetTriangles(new[] { 1, 2, 3 }, 1);
            mesh.AddBlendShapeFrame("Test", 100, new Vector3[4], new Vector3[4], new Vector3[4]);
            var mask = new[] { true, false, false, false };
            body = (Mesh)Call("Partition", mesh, mask, false);
            head = (Mesh)Call("Partition", mesh, mask, true);
            CollectionAssert.AreEqual(new[] { 0, 1, 2 }, head.GetTriangles(0));
            Assert.That(body.GetTriangles(0), Is.Empty);
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, body.GetTriangles(1));
            Assert.That(head.GetTriangles(1), Is.Empty);
            foreach (var part in new[] { body, head })
            {
                CollectionAssert.AreEqual(mesh.vertices, part.vertices);
                CollectionAssert.AreEqual(mesh.uv, part.uv);
                CollectionAssert.AreEqual(mesh.boneWeights, part.boneWeights);
                CollectionAssert.AreEqual(mesh.bindposes, part.bindposes);
                Assert.That(part.bounds, Is.EqualTo(mesh.bounds));
                Assert.That(part.blendShapeCount, Is.EqualTo(1));
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(mesh);
            if (body != null) UnityEngine.Object.DestroyImmediate(body);
            if (head != null) UnityEngine.Object.DestroyImmediate(head);
        }
    }

    [Test]
    public void SavedPlayerHasCompletePartitionsSharedSkeletonAndCameraIndependentShadows()
    {
        var root = PrefabUtility.LoadPrefabContents("Assets/Prefabs/Player.prefab");
        try
        {
            var head = root.GetComponentsInChildren<Transform>(true).Single(t => t.name == "Head");
            Assert.That(head.localScale, Is.EqualTo(Vector3.one));
            int layer = LayerMask.NameToLayer("PlayerHead");
            Assert.That(layer, Is.GreaterThanOrEqualTo(8));
            var camera = root.GetComponentsInChildren<Camera>(true).Single(c => c.CompareTag("MainCamera"));
            Assert.That(camera.cullingMask & (1 << layer), Is.Zero);
            var originals = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath("5572a64bd7a852043ad459aa87aa5d10")).OfType<Mesh>().ToDictionary(m => m.name);
            var bodies = root.GetComponentsInChildren<SkinnedMeshRenderer>(true).Where(r => originals.ContainsKey(r.name)).ToArray();
            Assert.That(bodies.Length, Is.EqualTo(10));
            foreach (var body in bodies)
            {
                var visible = body.transform.Find("Head Visible").GetComponent<SkinnedMeshRenderer>();
                var shadow = body.transform.Find("Head Shadow").GetComponent<SkinnedMeshRenderer>();
                Assert.That(visible.sharedMesh, Is.SameAs(shadow.sharedMesh));
                Assert.That(visible.gameObject.layer, Is.EqualTo(layer));
                Assert.That(shadow.gameObject.layer, Is.EqualTo(body.gameObject.layer));
                Assert.That(camera.cullingMask & (1 << shadow.gameObject.layer), Is.Not.Zero);
                Assert.That(body.shadowCastingMode, Is.EqualTo(ShadowCastingMode.On));
                Assert.That(visible.shadowCastingMode, Is.EqualTo(ShadowCastingMode.Off));
                Assert.That(shadow.shadowCastingMode, Is.EqualTo(ShadowCastingMode.ShadowsOnly));
                foreach (var part in new[] { visible, shadow })
                {
                    CollectionAssert.AreEqual(body.bones, part.bones);
                    CollectionAssert.AreEqual(body.sharedMaterials, part.sharedMaterials);
                    Assert.That(part.rootBone, Is.SameAs(body.rootBone));
                    Assert.That(part.localBounds, Is.EqualTo(body.localBounds));
                    Assert.That(part.transform.localPosition, Is.EqualTo(Vector3.zero));
                    Assert.That(part.transform.localRotation, Is.EqualTo(Quaternion.identity));
                    Assert.That(part.transform.localScale, Is.EqualTo(Vector3.one));
                    Assert.That(part.updateWhenOffscreen, Is.True);
                }
                var source = originals[body.name];
                var mask = (bool[])Call("HeadVertices", source, body.bones, head);
                foreach (bool keepHead in new[] { false, true })
                {
                    var expected = (Mesh)Call("Partition", source, mask, keepHead);
                    try
                    {
                        var actual = keepHead ? visible.sharedMesh : body.sharedMesh;
                        Assert.That(actual.triangles.Length, Is.GreaterThan(0));
                        for (int s = 0; s < source.subMeshCount; s++) CollectionAssert.AreEqual(expected.GetTriangles(s), actual.GetTriangles(s), body.name);
                        CollectionAssert.AreEqual(source.boneWeights, actual.boneWeights);
                        CollectionAssert.AreEqual(source.bindposes, actual.bindposes);
                    }
                    finally { UnityEngine.Object.DestroyImmediate(expected); }
                }
                Assert.That(body.sharedMesh.triangles.Length + visible.sharedMesh.triangles.Length, Is.EqualTo(source.triangles.Length));
            }
            foreach (var animator in root.GetComponentsInChildren<Animator>(true))
            {
                Assert.That(animator.cullingMode, Is.EqualTo(AnimatorCullingMode.AlwaysAnimate));
                foreach (var clip in animator.runtimeAnimatorController.animationClips.Distinct())
                    foreach (float fraction in new[] { 0f, 0.25f, 0.5f, 0.75f, 1f })
                    {
                        clip.SampleAnimation(animator.gameObject, clip.length * fraction);
                        Assert.That(head.localScale, Is.EqualTo(Vector3.one), clip.name);
                    }
            }
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }
}

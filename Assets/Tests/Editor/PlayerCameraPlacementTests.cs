using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class PlayerCameraPlacementTests
{
    [Test]
    public void IdleWalkingAndStoppingKeepEyeInFrontOfNeckThroughPitchAndBob()
    {
        var root = new GameObject("Camera placement regression fixture");
        root.SetActive(false);
        try
        {
            var type = Type.GetType("PlayerController, Assembly-CSharp", true);
            var controller = root.AddComponent(type);
            var pivot = new GameObject("HeadJoint").transform;
            pivot.SetParent(root.transform, false);
            type.GetField("HeadJoint").SetValue(controller, pivot);
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            type.GetField("_headHeight", flags).SetValue(controller, 5f);
            var maintain = type.GetMethod("MaintainHeadForwardOffset", flags);
            var bob = type.GetMethod("HeadBob", flags);
            foreach (var input in new[] { Vector2.zero, Vector2.up, Vector2.down, Vector2.zero })
            foreach (float pitch in new[] { -70f, 0f, 70f })
            foreach (float height in new[] { 4.9f, 5f, 5.1f })
            {
                type.GetField("_moveInput", flags).SetValue(controller, input);
                pivot.localPosition = new Vector3(0, height, 0);
                pivot.localRotation = Quaternion.Euler(pitch, 0, 0);
                maintain.Invoke(controller, null);
                bob.Invoke(controller, null);
                Assert.That(pivot.localPosition.z, Is.EqualTo(0.9f).Within(0.0001f), "Idle or stopping must not return the camera into the torso.");
                Assert.That(Quaternion.Angle(pivot.localRotation, Quaternion.Euler(pitch, 0, 0)), Is.LessThan(0.001f));
            }
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    [Test]
    public void SavedEyePositionStartsForwardAndStaysWithinCollisionCapsule()
    {
        var root = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab");
        var type = Type.GetType("PlayerController, Assembly-CSharp", true);
        var controller = root.GetComponent(type);
        var pivot = (Transform)type.GetField("HeadJoint").GetValue(controller);
        var camera = (Camera)type.GetField("PlayerCamera").GetValue(controller);
        Assert.That(pivot.localPosition.z, Is.EqualTo(0.9f).Within(0.0001f));
        Assert.That(camera.transform.localPosition, Is.EqualTo(Vector3.zero), "Pitch must not orbit an additional camera offset into the torso.");
        Assert.That(pivot.localPosition.z, Is.LessThan(root.GetComponent<CharacterController>().radius));
        Assert.That(camera.nearClipPlane, Is.EqualTo(0.05f).Within(0.0001f));
        var corners = new Vector3[4];
        camera.CalculateFrustumCorners(new Rect(0, 0, 1, 1), camera.nearClipPlane, Camera.MonoOrStereoscopicEye.Mono, corners);
        foreach (float pitch in new[] { -70f, 0f, 70f })
        foreach (var corner in corners)
        {
            var point = pivot.localPosition + Quaternion.Euler(pitch, 0, 0) * corner;
            Assert.That(new Vector2(point.x, point.z).magnitude, Is.LessThan(root.GetComponent<CharacterController>().radius), "Near plane must remain inside the capsule to avoid seeing through walls.");
        }
    }
}


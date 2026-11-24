#if UNITY_EDITOR
using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class EscapeRoomBehaviorTests
{
    [UnityTest]
    public IEnumerator LadderSelectionTravelAndSafety()
    {
        yield return null;
        var checks = Type.GetType("EscapeRoomChecks, Assembly-CSharp-Editor", true);
        Assert.That(checks.GetMethod("RunFocusedChecks").Invoke(null, null), Is.EqualTo("13 escape room checks passed"));
    }

    [UnityTest]
    public IEnumerator FloatingVisualMovesAndRestoresAuthoredPose()
    {
        var go = new GameObject("Floating visual test");
        var rest = new Vector3(4, 6, 2); var rotation = Quaternion.Euler(15, 30, 20);
        go.transform.SetPositionAndRotation(rest, rotation);
        var visual = (Behaviour)go.AddComponent(Type.GetType("FloatingPropVisual, Assembly-CSharp", true));
        try
        {
            yield return new WaitForSeconds(.2f);
            Assert.That(go.transform.localPosition.y, Is.Not.EqualTo(rest.y).Within(.001f));
            Assert.That(Mathf.Abs(go.transform.localPosition.y - rest.y), Is.LessThanOrEqualTo(.251f));
            Assert.That(Quaternion.Angle(go.transform.localRotation, rotation), Is.GreaterThan(.1f));
            visual.enabled = false;
            Assert.That(go.transform.localPosition, Is.EqualTo(rest));
            Assert.That(Quaternion.Angle(go.transform.localRotation, rotation), Is.LessThan(.01f));
            visual.enabled = true;
            yield return new WaitForSeconds(.15f);
            visual.enabled = false;
            Assert.That(go.transform.localPosition, Is.EqualTo(rest));
        }
        finally { UnityEngine.Object.DestroyImmediate(go); }
    }
}
#endif

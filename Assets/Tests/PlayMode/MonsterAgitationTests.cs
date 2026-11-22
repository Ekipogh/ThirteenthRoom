#if UNITY_EDITOR
using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;
public class MonsterAgitationTests
{
    static void Run(string method) => Assert.That((string)Type.GetType("MonsterAgitationChecks, Assembly-CSharp-Editor",true).GetMethod(method).Invoke(null,null), Does.Contain("passed"));
    [UnityTest] public IEnumerator HearingAndTimers() { yield return null; Run("HearingAndTimers"); }
    [UnityTest] public IEnumerator PriorityAndLifecycle() { yield return null; Run("PriorityAndLifecycle"); }
    [UnityTest] public IEnumerator ActionIntegration() { yield return null; Run("ActionIntegration"); }
    [UnityTest] public IEnumerator WalkingIntegration() { yield return null; Run("WalkingIntegration"); }
    [Test] public void AssetConfiguration() => Run("AssetConfiguration");
}
#endif

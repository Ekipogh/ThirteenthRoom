#if UNITY_EDITOR
using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;

public class MonsterPursuitTests
{
    static string Run(string method) => (string)Type.GetType("MonsterPursuitChecks, Assembly-CSharp-Editor", true).GetMethod(method).Invoke(null, null);
    [Test] public void AggressionCurveAndEvents() => Assert.That(Run("RunAggressionChecks"), Does.Contain("passed"));
    [UnityTest] public IEnumerator PursuitAndRitualIntegration()
    {
        yield return null;
        Assert.That(Run("RunPursuitChecks"), Does.Contain("passed"));
    }
}
#endif

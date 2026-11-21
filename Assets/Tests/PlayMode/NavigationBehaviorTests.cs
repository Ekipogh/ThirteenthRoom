#if UNITY_EDITOR
using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;

public class NavigationBehaviorTests
{
    [UnityTest]
    public IEnumerator MonsterPhysicsAndNavigationRegressionChecks()
    {
        yield return null;
        // Gameplay uses Unity's predefined assemblies. Resolve the editor check
        // runner without forcing an assembly-definition migration of the game.
        Type checks = Type.GetType("MansionNavigationChecks, Assembly-CSharp-Editor", true);
        object result = checks.GetMethod("RunFocusedChecks").Invoke(null, null);
        Assert.That(result, Is.EqualTo("24 focused navigation checks passed"));
    }
}
#endif

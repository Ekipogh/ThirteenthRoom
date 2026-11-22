#if UNITY_EDITOR
using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;
public class MonsterCombatTests
{
    static void Run(string method) => Assert.That((string)Type.GetType("MonsterCombatChecks, Assembly-CSharp-Editor",true).GetMethod(method).Invoke(null,null),Does.Contain("passed"));
    [UnityTest] public IEnumerator AnimationPace() { yield return null; Run("AnimationPace"); }
    [UnityTest] public IEnumerator AttackReachAndEscape() { yield return null; Run("AttackReachAndEscape"); }
    [UnityTest] public IEnumerator ObstructionAndPause() { yield return null; Run("ObstructionAndPause"); }
}
#endif

#if UNITY_EDITOR
using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;
public class InventoryTests
{
    static void Run(string method)
    {
        try { Assert.That((string)Type.GetType("InventoryChecks, Assembly-CSharp-Editor", true).GetMethod(method).Invoke(null, null), Does.Contain("passed")); }
        catch (System.Reflection.TargetInvocationException e) { throw e.InnerException; }
    }
    [UnityTest] public IEnumerator StacksAndShortcuts() { yield return null; Run("StacksAndShortcuts"); }
    [UnityTest] public IEnumerator PotionAndInputBlocking() { yield return null; Run("PotionAndInputBlocking"); }
    [UnityTest] public IEnumerator GunAndStun() { yield return null; Run("GunAndStun"); }
    [UnityTest] public IEnumerator UIAndPickup() { yield return null; Run("UIAndPickup"); }
    [UnityTest] public IEnumerator KeyboardAndWheel() { yield return null; Run("KeyboardAndWheel"); }
}
#endif

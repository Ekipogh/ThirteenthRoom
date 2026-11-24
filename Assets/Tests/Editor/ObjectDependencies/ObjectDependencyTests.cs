#if UNITY_EDITOR
using System;
using NUnit.Framework;

public class ObjectDependencyTests
{
    static object Run(string method) => Type.GetType("ObjectDependencyChecks, Assembly-CSharp-Editor", true)
        .GetMethod(method).Invoke(null, method == "RunSeededChecks" ? new object[]{40} : null);

    [Test] public void SizesMatchingProvidersDefaultsAndContent() => Assert.That(Run("RunFocusedChecks"), Is.Not.Null);
    [Test] public void SeededMansionsAcrossFloorCounts() => Assert.That(Run("RunSeededChecks"), Is.Not.Null);
}
#endif

#if UNITY_EDITOR
using System;
using NUnit.Framework;
public class MonsterActivityTests
{
    static void Run(string method) => Assert.That((string)Type.GetType("MonsterActivityChecks, Assembly-CSharp-Editor",true).GetMethod(method).Invoke(null,null), Does.Contain("passed"));
    [Test] public void AgitationAndLifecycle() => Run("AgitationAndLifecycle");
    [Test] public void AggressionAmplifiesNoise() => Run("AggressionAmplifiesNoise");
    [Test] public void MovementAndPauses() => Run("MovementAndPauses");
    [Test] public void PatrolGraphCoverage() => Run("PatrolGraphCoverage");
    [Test] public void FloorCoverageAndVisits() => Run("FloorCoverageAndVisits");
    [Test] public void DestinationRetentionAndRecovery() => Run("DestinationRetentionAndRecovery");
    [Test] public void AssetConfiguration() => Run("AssetConfiguration");
}
#endif

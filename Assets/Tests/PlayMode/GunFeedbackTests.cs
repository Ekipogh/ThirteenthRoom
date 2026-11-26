#if UNITY_EDITOR
using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;

public class GunFeedbackTests
{
    static IEnumerator Run(string name)
    {
        var enumerator = (IEnumerator)Type.GetType("GunFeedbackChecks, Assembly-CSharp-Editor", true).GetMethod(name).Invoke(null, null);
        while (true)
        {
            bool more;
            try { more = enumerator.MoveNext(); }
            catch (Exception e) { (enumerator as IDisposable)?.Dispose(); Assert.Fail(e.ToString()); yield break; }
            if (!more) break;
            yield return enumerator.Current;
        }
    }
    [UnityTest] public IEnumerator WeaponActionsAndCleanup() => Run(nameof(WeaponActionsAndCleanup));
    [UnityTest] public IEnumerator OverlappingShotTails() => Run(nameof(OverlappingShotTails));
    [UnityTest] public IEnumerator AgitationIntegration() => Run(nameof(AgitationIntegration));
    [UnityTest] public IEnumerator HitReactionLifecycle() => Run(nameof(HitReactionLifecycle));
}
#endif

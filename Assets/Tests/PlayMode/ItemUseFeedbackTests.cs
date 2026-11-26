#if UNITY_EDITOR
using System;
using System.Collections;
using UnityEngine.TestTools;

public class ItemUseFeedbackTests
{
    static IEnumerator Run(string name)
    {
        var enumerator = (IEnumerator)Type.GetType("GunFeedbackChecks, Assembly-CSharp-Editor", true).GetMethod(name).Invoke(null, null);
        try
        {
            while (enumerator.MoveNext()) yield return enumerator.Current;
        }
        finally { (enumerator as IDisposable)?.Dispose(); }
    }
    [UnityTest] public IEnumerator PotionUseFeedback() => Run(nameof(PotionUseFeedback));
    [UnityTest] public IEnumerator OptionalItemUseFeedback() => Run(nameof(OptionalItemUseFeedback));
}
#endif

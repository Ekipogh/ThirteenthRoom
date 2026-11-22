using UnityEngine;

[CreateAssetMenu(menuName = "Thirteenth Room/Agitation Profile")]
public class AgitationProfile : ScriptableObject
{
    [Min(0.01f)] public float Strength = 1f;
    [Min(0f)] public float HearingRadius = 75f;
    [Min(0.01f)] public float InvestigationSeconds = 5f;

    public bool IsValid => Finite(Strength) && Strength > 0f && Finite(HearingRadius) &&
        HearingRadius >= 0f && Finite(InvestigationSeconds) && InvestigationSeconds > 0f;
    public static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
}

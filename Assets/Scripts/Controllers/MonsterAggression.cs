using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class MonsterAggressionProfile
{
    [Range(0f, 99f)] public float BaseLevel = 20f;
    [Tooltip("Additive aggression over elapsed Playing seconds.")]
    public AnimationCurve TimeIncrease = AnimationCurve.Linear(0f, 0f, 1200f, 80f);
    [Min(1f)] public float EndgameSeconds = 1200f;
    public Vector2 TrackingSeconds = new Vector2(3f, 8f);
    public Vector2 SearchSeconds = new Vector2(8f, 20f);
    [Min(0f)] public float EscapeDistance = 50f;
    [Min(0f)] public float DistantConcealmentSeconds = 2f;
    [Min(0f)] public float FailedRitualIncrease = 5f;
    [Min(0f)] public float RestorationReduction = 10f;
    [Min(0f)] public float RestorationSeconds = 30f;
}

/// <summary>Run-scoped state, advanced exclusively by Playing time. Profiles remain immutable.</summary>
public sealed class MonsterAggression
{
    struct Modifier { public float Amount, Until; }
    readonly Dictionary<string, Modifier> _modifiers = new Dictionary<string, Modifier>();
    readonly HashSet<string> _maximumHunts = new HashSet<string>();
    readonly HashSet<object> _failedRituals = new HashSet<object>();
    readonly HashSet<MonsterAggressionProfile> _endgameProfiles = new HashSet<MonsterAggressionProfile>();
    float _lastRestoration = float.NegativeInfinity;
    public float ElapsedSeconds { get; private set; }
    public float DifficultyOffset { get; set; }

    public void Advance(float seconds)
    {
        if (float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds <= 0f) return;
        ElapsedSeconds += seconds;
    }

    public bool IsMaximumHunt(MonsterAggressionProfile profile)
    {
        if (ElapsedSeconds >= Mathf.Max(1f, profile.EndgameSeconds)) _endgameProfiles.Add(profile);
        return _maximumHunts.Count > 0 || _endgameProfiles.Contains(profile);
    }

    public float GetLevel(MonsterAggressionProfile profile)
    {
        if (IsMaximumHunt(profile)) return 100f;
        float increase = profile.TimeIncrease != null ? profile.TimeIncrease.Evaluate(ElapsedSeconds) : 0f;
        float level = profile.BaseLevel + increase + DifficultyOffset + _failedRituals.Count * profile.FailedRitualIncrease;
        foreach (var modifier in _modifiers.Values)
            if (ElapsedSeconds < modifier.Until) level += modifier.Amount;
        if (ElapsedSeconds - _lastRestoration < profile.RestorationSeconds) level -= profile.RestorationReduction;
        return Mathf.Clamp(level, 0f, 99f);
    }

    /// <summary>Replace a named modifier. Nonpositive duration means until explicitly removed.</summary>
    public void SetModifier(string id, float amount, float durationSeconds = 0f)
    {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("A modifier needs a stable id.", nameof(id));
        if (float.IsNaN(amount) || float.IsInfinity(amount)) throw new ArgumentOutOfRangeException(nameof(amount));
        _modifiers[id] = new Modifier { Amount = amount, Until = durationSeconds > 0f ? ElapsedSeconds + durationSeconds : float.PositiveInfinity };
    }
    public void RemoveModifier(string id) { if (id != null) _modifiers.Remove(id); }
    public void SetMaximumHunt(string id, bool active)
    {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("A hunt needs a stable id.", nameof(id));
        if (active) _maximumHunts.Add(id); else _maximumHunts.Remove(id);
    }
    public void SetRitualFailed(object ritual, bool failed)
    {
        if (ritual == null) return;
        if (failed) _failedRituals.Add(ritual);
        else if (_failedRituals.Remove(ritual)) _lastRestoration = ElapsedSeconds;
    }
    public void UnregisterRitual(object ritual) { if (ritual != null) _failedRituals.Remove(ritual); }
    public void Reset()
    {
        ElapsedSeconds = 0f;
        _lastRestoration = float.NegativeInfinity;
        _modifiers.Clear(); _maximumHunts.Clear(); _failedRituals.Clear(); _endgameProfiles.Clear();
    }
}

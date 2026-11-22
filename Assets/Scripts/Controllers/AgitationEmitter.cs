using UnityEngine;

// Explicit gameplay signals, independent of whether an AudioSource is present or audible.
public class AgitationEmitter : MonoBehaviour
{
    [SerializeField] GameManager owner;
    [SerializeField] AgitationProfile profile;
    [SerializeField, Min(0.01f)] float repeatSeconds = 1f;
    float _untilNext;
    bool _continuous;

    public GameManager Owner => owner;
    public AgitationProfile Profile => profile;
    public bool CanEmit => isActiveAndEnabled && owner != null && owner.currentState == GameState.Playing;

    public void BindOwner(GameManager manager)
    {
        if (owner != manager) StopContinuous();
        owner = manager;
    }

    // No arguments, so one-shot actions can invoke this through a UnityEvent.
    public void Emit()
    {
        if (CanEmit) owner.ReportAgitation(transform.position, profile);
    }

    // Call only from the action's active gameplay loop, never from audio playback.
    public void TickContinuous(bool active, float deltaTime)
    {
        if (!active) { StopContinuous(); return; }
        if (!CanEmit || !AgitationProfile.Finite(deltaTime) || deltaTime < 0f) return;
        if (!_continuous)
        {
            _continuous = true;
            _untilNext = Mathf.Max(0.01f, repeatSeconds);
            Emit();
            return;
        }
        _untilNext -= deltaTime;
        if (_untilNext > 0f) return;
        Emit();
        // At most one pulse per frame; never replay missed pulses after a stall.
        _untilNext = Mathf.Max(0.01f, repeatSeconds);
    }

    public void StopContinuous() { _continuous = false; _untilNext = 0f; }
    void OnDisable() => StopContinuous();
}

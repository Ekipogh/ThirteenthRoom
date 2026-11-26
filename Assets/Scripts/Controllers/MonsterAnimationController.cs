using UnityEngine;

[RequireComponent(typeof(MonsterController))]
public class MonsterAnimationController : CharacterAnimationController
{
    [Header("Monster")]
    [SerializeField] MonsterController monsterController;

    [Header("Movement")]
    [SerializeField] float speedSmoothingTime = 0.15f;
    [SerializeField] float startWalkingSpeed = 0.15f;
    [SerializeField] float stopWalkingSpeed = 0.05f;
    [SerializeField, Min(0f)] float locomotionPlaybackSpeed = 0.15f;
    [SerializeField] Vector2 locomotionPlaybackRange = new Vector2(0.5f, 1.1f);

    readonly int _isAttackingHash = Animator.StringToHash("Attack");
    readonly int _stunnedHash = Animator.StringToHash("Stunned");
    readonly int _hitHash = Animator.StringToHash("ShotHit");
    readonly int _stunnedIdleHash = Animator.StringToHash("Base Layer.Stunned Idle");
    MonsterState _currentState;
    Gait _currentGait;
    Vector3 _lastPosition;
    float _smoothedSpeed;
    float _speedSmoothVelocity;
    bool _hasLastPosition;

    protected override void Awake()
    {
        base.Awake();
        if (monsterController == null) monsterController = GetComponent<MonsterController>();
    }
    void OnEnable()
    {
        monsterController.StunApplied += PlayHitReaction;
        Animator.ResetTrigger(_hitHash);
        Animator.SetBool(_stunnedHash, monsterController.IsStunned);
        // Re-enabling a disabled controller must not replay an old hit request.
        if (monsterController.IsStunned) Animator.Play(_stunnedIdleHash, 0, 0f);
    }
    void PlayHitReaction()
    {
        if (!isActiveAndEnabled || !monsterController.IsGameplayActive) return;
        Animator.SetBool(_isAttackingHash, false);
        Animator.SetBool(_stunnedHash, true);
        Animator.ResetTrigger(_hitHash);
        Animator.SetTrigger(_hitHash);
        Animator.speed = 1f;
    }
    void UpdateAnimator()
    {
        if (!monsterController.IsGameplayActive)
        {
            Animator.speed = 0f;
            if (monsterController.GameManager != null && monsterController.GameManager.currentState == GameState.GameOver)
            {
                Animator.ResetTrigger(_hitHash);
                Animator.SetBool(_stunnedHash, false);
                Animator.SetBool(_isAttackingHash, false);
            }
            return;
        }
        Animator.SetBool(_stunnedHash, monsterController.IsStunned);
        if (monsterController.IsStunned)
        {
            _lastPosition = transform.position; _hasLastPosition = true;
            _smoothedSpeed = _speedSmoothVelocity = 0f; _currentGait = Gait.Idle;
            UpdateLocomotion(0f, Gait.Idle);
            Animator.SetBool(_isAttackingHash, false);
            Animator.speed = 1f;
            return;
        }
        float speed = GetMoveSpeed();
        _currentGait = GetSmoothedGait(speed);
        UpdateLocomotion(speed, _currentGait);
        bool isAttacking = _currentState == MonsterState.Attacking;
        // Only locomotion uses the heavy monster's bounded playback rate.
        Animator.speed = _currentGait == Gait.Idle || isAttacking ? 1f
            : Mathf.Clamp(speed * locomotionPlaybackSpeed, locomotionPlaybackRange.x, locomotionPlaybackRange.y);
        Animator.SetBool(_isAttackingHash, isAttacking);
    }
    float GetMoveSpeed()
    {
        if (!_hasLastPosition) { _lastPosition = transform.position; _hasLastPosition = true; return 0f; }
        Vector3 delta = transform.position - _lastPosition; delta.y = 0f; _lastPosition = transform.position;
        if (Time.deltaTime <= 0f) return 0f;
        float speed = delta.magnitude / Time.deltaTime;
        _smoothedSpeed = Mathf.SmoothDamp(_smoothedSpeed, speed, ref _speedSmoothVelocity, speedSmoothingTime);
        return _smoothedSpeed;
    }
    void OnDisable()
    {
        if (monsterController != null) monsterController.StunApplied -= PlayHitReaction;
        if (Animator == null) return;
        Animator.ResetTrigger(_hitHash);
        Animator.SetBool(_stunnedHash, false);
        Animator.speed = 1f;
    }
    Gait GetSmoothedGait(float speed)
    {
        if (_currentGait == Gait.Idle && speed >= startWalkingSpeed) return Gait.Walk;
        if (_currentGait != Gait.Idle && speed <= stopWalkingSpeed) return Gait.Idle;
        return _currentGait;
    }
    void Update() { _currentState = monsterController.CurrentState; UpdateAnimator(); }
}

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

    MonsterState _currentState;
    Gait _currentGait;
    Vector3 _lastPosition;
    float _smoothedSpeed;
    float _speedSmoothVelocity;
    bool _hasLastPosition;

    protected override void Awake()
    {
        base.Awake();

        if (monsterController == null)
        {
            monsterController = GetComponent<MonsterController>();
        }
    }

    void UpdateAnimator()
    {
        if (monsterController.IsStunned) { Animator.SetBool(_isAttackingHash, false); Animator.speed = 0f; return; }
        float speed = GetMoveSpeed();
        _currentGait = GetSmoothedGait(speed);
        UpdateLocomotion(speed, _currentGait);

        bool isAttacking = monsterController.IsGameplayActive && _currentState == MonsterState.Attacking;
        // Navigation speed must not turn the heavy Monster's gait into a sprinting cartoon.
        // Keep idle/attack timing independent of the bounded locomotion rate.
        Animator.speed = _currentGait == Gait.Idle || isAttacking ? 1f
            : Mathf.Clamp(speed * locomotionPlaybackSpeed, locomotionPlaybackRange.x, locomotionPlaybackRange.y);
        Animator.SetBool(_isAttackingHash, isAttacking);
    }

    float GetMoveSpeed()
    {
        if (!_hasLastPosition)
        {
            _lastPosition = transform.position;
            _hasLastPosition = true;
            return 0f;
        }

        Vector3 delta = transform.position - _lastPosition;
        delta.y = 0f;
        _lastPosition = transform.position;

        if (Time.deltaTime <= 0f)
        {
            return 0f;
        }

        float speed = delta.magnitude / Time.deltaTime;
        _smoothedSpeed = Mathf.SmoothDamp(_smoothedSpeed, speed, ref _speedSmoothVelocity, speedSmoothingTime);
        return _smoothedSpeed;
    }

    void OnDisable()
    {
        if (Animator != null)
        {
            Animator.speed = 1f;
        }
    }

    Gait GetSmoothedGait(float speed)
    {
        if (_currentGait == Gait.Idle && speed >= startWalkingSpeed)
        {
            return Gait.Walk;
        }

        if (_currentGait != Gait.Idle && speed <= stopWalkingSpeed)
        {
            return Gait.Idle;
        }

        return _currentGait;
    }

    void Update()
    {
        _currentState = monsterController.CurrentState;
        UpdateAnimator();
    }
}

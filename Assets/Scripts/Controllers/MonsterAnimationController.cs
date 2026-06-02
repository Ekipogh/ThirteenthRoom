using UnityEngine;

[RequireComponent(typeof(MonsterController))]
public class MonsterAnimationController : CharacterAnimationController
{
    [SerializeField] MonsterController monsterController;
    [SerializeField] float speedSmoothingTime = 0.15f;
    [SerializeField] float startWalkingSpeed = 0.15f;
    [SerializeField] float stopWalkingSpeed = 0.05f;

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
        float speed = GetMoveSpeed();
        _currentGait = GetSmoothedGait(speed);
        UpdateLocomotion(speed, _currentGait);
        Animator.SetBool(_isAttackingHash, _currentState == MonsterState.EscapeWindow || _currentState == MonsterState.Attacking);
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
        Debug.Log($"Calculated speed: {speed}, Smoothed speed: {_smoothedSpeed}");
        return _smoothedSpeed;
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

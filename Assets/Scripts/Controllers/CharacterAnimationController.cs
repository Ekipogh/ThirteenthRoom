using UnityEngine;

public abstract class CharacterAnimationController : MonoBehaviour
{
    protected enum Gait
    {
        Idle = 0,
        Walk = 1,
        Run = 2
    }

    [Header("Animation")]
    [SerializeField] Animator _animator;

    readonly int _currentGaitHash = Animator.StringToHash("CurrentGait");
    readonly int _isGroundedHash = Animator.StringToHash("IsGrounded");
    readonly int _isWalkingHash = Animator.StringToHash("IsWalking");
    readonly int _moveSpeedHash = Animator.StringToHash("MoveSpeed");
    readonly int _movementInputHeldHash = Animator.StringToHash("MovementInputHeld");

    protected Animator Animator => _animator;

    protected virtual void Awake()
    {
        if (_animator == null)
        {
            _animator = GetComponentInChildren<Animator>();
        }

        _animator.SetBool(_isGroundedHash, true);
        _animator.SetInteger(_currentGaitHash, (int)Gait.Idle);
    }

    protected void UpdateLocomotion(float speed, Gait gait)
    {
        _animator.SetBool(_movementInputHeldHash, gait != Gait.Idle);
        _animator.SetInteger(_currentGaitHash, (int)gait);
        _animator.SetBool(_isWalkingHash, gait == Gait.Walk || gait == Gait.Run);
        _animator.SetFloat(_moveSpeedHash, speed);
    }
}

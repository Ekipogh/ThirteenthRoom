using UnityEngine;

[RequireComponent(typeof(PlayerController))]
public class PlayerAnimationController : MonoBehaviour
{
    enum Gait
    {
        Idle = 0,
        Walk = 1,
        Run = 2
    }
    [SerializeField] Animator _animator;
    [SerializeField] PlayerController playerController;

    readonly int _currentGaitHash = Animator.StringToHash("CurrentGait");
    readonly int _isGroundedHash = Animator.StringToHash("IsGrounded");
    readonly int _isWalkingHash = Animator.StringToHash("IsWalking");
    private readonly int _moveSpeedHash = Animator.StringToHash("MoveSpeed");
    private readonly int _movementInputHeldHash = Animator.StringToHash("MovementInputHeld");

    Gait _currentGait;

    void Awake()
    {
        if (_animator == null)
        {
            _animator = GetComponentInChildren<Animator>();
        }
        if (playerController == null)
        {
            playerController = GetComponent<PlayerController>();
        }
        _animator.SetBool(_isGroundedHash, true);
        _animator.SetInteger(_currentGaitHash, (int)Gait.Idle);
    }

    void UpdateAnimator()
    {
        float speed = playerController.Speed();
        Debug.Log($"Player speed: {speed}");
        _currentGait = (Gait)playerController.CurrentGait;
        _animator.SetBool(_movementInputHeldHash, speed > 0.1f);
        _animator.SetInteger(_currentGaitHash, (int)_currentGait);
        _animator.SetBool(_isWalkingHash, _currentGait == Gait.Walk || _currentGait == Gait.Run);
        _animator.SetFloat(_moveSpeedHash, speed);
    }

    void Update()
    {
        UpdateAnimator();
    }
}

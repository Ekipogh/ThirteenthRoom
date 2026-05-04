using UnityEngine;

[RequireComponent(typeof(MonsterController))]
public class MonsterAnimationController : MonoBehaviour
{
    [SerializeField] Animator _animator;
    [SerializeField] MonsterController monsterController;

    readonly int _isAttackingHash = Animator.StringToHash("Attack");
    readonly int _isGroundedHash = Animator.StringToHash("IsGrounded");

    MonsterState _currentState;

    void Awake()
    {
        if (_animator == null)
        {
            _animator = GetComponentInChildren<Animator>();
        }
        if (monsterController == null)
        {
            monsterController = GetComponent<MonsterController>();
        }
        _animator.SetBool(_isGroundedHash, true);
    }

    void UpdateAnimator()
    {
        _animator.SetBool(_isAttackingHash, monsterController.CurrentState == MonsterState.EscapeWindow || monsterController.CurrentState == MonsterState.Attacking);
    }

    void Update()
    {
        _currentState = monsterController.CurrentState;
        UpdateAnimator();
    }
}

using UnityEngine;

[RequireComponent(typeof(PlayerController))]
public class PlayerAnimationController : CharacterAnimationController
{
    [Header("Player")]
    [SerializeField] PlayerController playerController;

    protected override void Awake()
    {
        base.Awake();

        if (playerController == null)
        {
            playerController = GetComponent<PlayerController>();
        }
    }

    void UpdateAnimator()
    {
        float speed = playerController.Speed();
        UpdateLocomotion(speed, (Gait)playerController.CurrentGait);
    }

    void Update()
    {
        UpdateAnimator();
    }
}

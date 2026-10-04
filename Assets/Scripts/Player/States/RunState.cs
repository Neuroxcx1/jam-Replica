using UnityEngine;

public class RunState : PlayerState
{
    [SerializeField] ParticleSystem walkDust;

    public override void Enter()
    {
        if (walkDust != null) walkDust.Play();
    }

    public override void Exit()
    {
        if (walkDust != null) walkDust.Stop();
    }

    public override void UpdateState(float delta)
    {
        if (player.CanJump()) TransitionTo("jump");
        else if (!player.IsGrounded()) TransitionTo("fall");
        else if (player.MoveInput == 0) TransitionTo("idle");
    }

    public override void PhysicsUpdate(float delta)
    {
        player.Move(player.MoveInput);
    }
}

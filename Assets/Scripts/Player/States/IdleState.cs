public class IdleState : PlayerState
{
    public override void Enter()
    {
        PlayAnimation();
    }

    public override void UpdateState(float delta)
    {
        if (player.CanJump()) TransitionTo("jump");
        else if (!player.IsGrounded()) TransitionTo("fall");
        else if (player.MoveInput != 0) TransitionTo("run");
    }

    public override void PhysicsUpdate(float delta)
    {
        player.Move(0);
    }
}

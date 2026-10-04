using UnityEngine;

public class FallState : PlayerState
{
    [SerializeField] float gravityMultiplier = 1.7f;
    [SerializeField] float maxFallSpeed = 20f;
    [SerializeField] GameObject landDust;
    [SerializeField] float landDustSpeed = 6f;

    float normalGravity;
    float fastestFall;

    public override void Enter()
    {
        normalGravity = player.Rb.gravityScale;
        player.Rb.gravityScale = normalGravity * gravityMultiplier;
        fastestFall = 0f;
    }

    public override void Exit()
    {
        player.Rb.gravityScale = normalGravity;
    }

    public override void UpdateState(float delta)
    {
        // aqui entran el coyote time y el buffer: CanJump() mira los dos timers
        if (player.CanJump()) TransitionTo("jump");
        else if (player.IsGrounded())
        {
            // polvo al aterrizar, solo si venias cayendo rapido
            if (landDust != null && fastestFall < -landDustSpeed)
                Instantiate(landDust, player.Feet, Quaternion.identity);
            TransitionTo(player.MoveInput != 0 ? "run" : "idle");
        }
    }

    public override void PhysicsUpdate(float delta)
    {
        player.Move(player.MoveInput);
        fastestFall = Mathf.Min(fastestFall, player.Rb.linearVelocity.y);

        if (player.Rb.linearVelocity.y < -maxFallSpeed)
            player.Rb.linearVelocity = new Vector2(player.Rb.linearVelocity.x, -maxFallSpeed);
    }
}

using UnityEngine;

public class FallState : PlayerState
{
    [SerializeField] float gravityMultiplier = 1.8f;
    [SerializeField] float maxFallSpeed = 22f;
    // justo despues del punto mas alto (con el salto pulsado) cae mas despacio, igual que en JumpState
    [SerializeField] float apexSpeed = 2f;
    [SerializeField] float apexGravity = 0.5f;
    [SerializeField] GameObject landDust;
    [SerializeField] float landDustSpeed = 6f;

    float fastestFall;

    public override void Enter()
    {
        fastestFall = 0f;
    }

    public override void Exit()
    {
        player.Rb.gravityScale = player.BaseGravity;
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

        // cae mas rapido de lo que sube: el salto se siente con peso y no flota
        float vy = player.Rb.linearVelocity.y;
        bool apex = player.JumpHeld && vy > -apexSpeed;
        player.Rb.gravityScale = player.BaseGravity * (apex ? apexGravity : gravityMultiplier);

        fastestFall = Mathf.Min(fastestFall, vy);
        if (vy < -maxFallSpeed)
            player.Rb.linearVelocity = new Vector2(player.Rb.linearVelocity.x, -maxFallSpeed);
    }
}

using UnityEngine;

public class JumpState : PlayerState
{
    [SerializeField] float jumpForce = 13f;
    [SerializeField] float jumpCutMultiplier = 0.5f;
    // cerca del punto mas alto (con el salto pulsado) la gravedad baja: da un instante para apuntar el aterrizaje
    [SerializeField] float apexSpeed = 2f;
    [SerializeField] float apexGravity = 0.5f;
    [SerializeField] GameObject jumpDust;
    [SerializeField] float shakePixels = 1f;

    public override void Enter()
    {
        // temblor leve en todos los saltos; el polvo solo si sales del suelo (no en el de coyote)
        CameraFollow.Shake(shakePixels, 0.1f);
        if (jumpDust != null && player.IsGrounded())
            Instantiate(jumpDust, player.Feet, Quaternion.identity);

        player.ClearJumpTimers();
        player.Rb.linearVelocity = new Vector2(player.Rb.linearVelocity.x, jumpForce);
    }

    public override void Exit()
    {
        player.Rb.gravityScale = player.BaseGravity;
    }

    public override void UpdateState(float delta)
    {
        Vector2 velocity = player.Rb.linearVelocity;

        // si sueltas el boton antes de tiempo el salto es mas corto
        if (player.JumpReleased && velocity.y > 0)
            player.Rb.linearVelocity = new Vector2(velocity.x, velocity.y * jumpCutMultiplier);

        if (velocity.y <= 0) TransitionTo("fall");
    }

    public override void PhysicsUpdate(float delta)
    {
        player.Move(player.MoveInput);

        bool apex = player.JumpHeld && player.Rb.linearVelocity.y < apexSpeed;
        player.Rb.gravityScale = player.BaseGravity * (apex ? apexGravity : 1f);
    }
}

using UnityEngine;

public class DeadState : PlayerState
{
    [SerializeField] float respawnDelay = 0.55f;
    [SerializeField] SoulOrb soulOrb;
    [SerializeField] GameObject respawnEffect;

    float timer;

    public override void Enter()
    {
        timer = respawnDelay;

        // la bolita de luz vuela al checkpoint mientras esperas
        if (soulOrb != null)
            Instantiate(soulOrb).Fly(player.transform.position, player.Checkpoint, respawnDelay);

        player.SetAlive(false);
    }

    public override void UpdateState(float delta)
    {
        timer -= delta;
        if (timer <= 0)
        {
            player.Respawn();
            if (respawnEffect != null) Instantiate(respawnEffect, player.transform.position, Quaternion.identity);
            TransitionTo("idle");
        }
    }
}

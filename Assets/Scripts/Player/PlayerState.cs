using UnityEngine;

public abstract class PlayerState : State
{
    // la animacion de este estado: se pone al entrar (como el AnimationPlayer en Godot)
    [Header("Animacion")]
    [SerializeField] Sprite[] frames;
    [SerializeField] float fps = 12f;
    [SerializeField] bool loop = true;

    protected Player player;

    protected virtual void Awake()
    {
        player = GetComponentInParent<Player>();
    }

    protected void PlayAnimation() => player.Animate(frames, fps, loop);
}

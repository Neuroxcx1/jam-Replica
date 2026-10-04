using UnityEngine;

public abstract class PlayerState : State
{
    protected Player player;

    protected virtual void Awake()
    {
        player = GetComponentInParent<Player>();
    }
}

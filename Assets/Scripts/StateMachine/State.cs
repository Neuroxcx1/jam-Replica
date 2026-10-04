using System;
using UnityEngine;

public abstract class State : MonoBehaviour
{
    public event Action<State, string> Transitioned;

    public virtual void Enter() { }
    public virtual void Exit() { }
    public virtual void UpdateState(float delta) { }
    public virtual void PhysicsUpdate(float delta) { }

    protected void TransitionTo(string stateName)
    {
        Transitioned?.Invoke(this, stateName);
    }
}

using System.Collections.Generic;
using UnityEngine;

public class StateMachine : MonoBehaviour
{
    [SerializeField] State initialState;

    public State CurrentState { get; private set; }

    readonly Dictionary<string, State> states = new Dictionary<string, State>();

    void Awake()
    {
        foreach (State state in GetComponentsInChildren<State>())
        {
            states[state.name.ToLower()] = state;
            state.Transitioned += OnChildTransition;
        }
    }

    void Start()
    {
        if (initialState != null)
        {
            CurrentState = initialState;
            CurrentState.Enter();
        }
    }

    void Update()
    {
        if (CurrentState != null) CurrentState.UpdateState(Time.deltaTime);
    }

    void FixedUpdate()
    {
        if (CurrentState != null) CurrentState.PhysicsUpdate(Time.fixedDeltaTime);
    }

    public void ChangeState(string stateName)
    {
        if (!states.TryGetValue(stateName.ToLower(), out State newState))
        {
            Debug.LogWarning($"No existe el estado '{stateName}'");
            return;
        }

        if (CurrentState != null) CurrentState.Exit();
        CurrentState = newState;
        CurrentState.Enter();
    }

    void OnChildTransition(State state, string newStateName)
    {
        if (state != CurrentState) return;
        ChangeState(newStateName);
    }
}

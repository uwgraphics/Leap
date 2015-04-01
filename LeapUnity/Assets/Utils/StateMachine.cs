using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Handler for individual state updates. 
/// </summary>
public delegate void StateHandler();

/// <summary>
/// Handler for state transitions. 
/// </summary>
public delegate void StateTransitionHandler();

/// <summary>
/// Class implementing simple finite state machine logic.
/// </summary>
public class StateMachine
{
    private class StateTransition
    {
        public int srcState;
        public int trgState;

        public StateTransition(int srcState, int trgState)
        {
            this.srcState = srcState;
            this.trgState = trgState;
        }

        public override bool Equals(object obj)
        {
            StateTransition rhs = (StateTransition)obj;

            if (srcState == rhs.srcState &&
               trgState == rhs.trgState)
                return true;

            return false;
        }

        public override int GetHashCode()
        {
            return srcState.GetHashCode() + trgState.GetHashCode();
        }
    }

    private int state = -1;
    private List<StateHandler> stateHandlers;
    private List<StateHandler> stateHandlersLate;
    private Dictionary<StateTransition, StateTransitionHandler> transHandlers;
    private Dictionary<string, int> stateIndexes;

    /// <summary>
    /// Constructor.
    /// </summary>
    public StateMachine()
    {
        stateHandlers = new List<StateHandler>();
        stateHandlersLate = new List<StateHandler>();
        transHandlers = new Dictionary<StateTransition, StateTransitionHandler>();
        stateIndexes = new Dictionary<string, int>();
    }

    /// <summary>
    /// Current FSM state. 
    /// </summary>
    public int State
    {
        get
        {
            return state;
        }
    }

    /// <summary>
    /// Registers a new state with this FSM.
    /// </summary>
    /// <param name="stateName">
    /// State name. <see cref="System.String"/>
    /// </param>
    /// <param name="handler">
    /// State update handler. <see cref="StateHandler"/>
    /// </param>
    public void RegisterState(string stateName, StateHandler handler)
    {
        RegisterState(stateName, handler, null);
    }

    /// <summary>
    /// Registers a new state with this FSM.
    /// </summary>
    /// <param name="stateName">
    /// State name. <see cref="System.String"/>
    /// </param>
    /// <param name="handler">
    /// State update handler. <see cref="StateHandler"/>
    /// </param>
    /// <param name="lateHandler">
    /// State late update handler. <see cref="StateHandler"/>
    /// </param>
    public void RegisterState(string stateName, StateHandler handler,
                              StateHandler lateHandler)
    {
        if (state < 0)
            state = 0;

        stateHandlers.Add(handler);
        stateHandlersLate.Add(lateHandler);
        stateIndexes.Add(stateName, stateIndexes.Count);
    }

    /// <summary>
    /// Removes all states and state transitions from the FSM.
    /// </summary>
    public void Reset()
    {
        stateHandlers.Clear();
        transHandlers.Clear();
        stateIndexes.Clear();
    }

    /// <summary>
    /// Checks whether a state exists in the FSM.
    /// </summary>
    /// <param name="stateName">
    /// State name. <see cref="System.String"/>
    /// </param>
    /// <returns>
    /// true if the state exists, false otherwise. <see cref="System.Boolean"/>
    /// </returns>
    public bool HasState(string stateName)
    {
        return stateIndexes.ContainsKey(stateName);
    }

    /// <summary>
    /// Gets the numeric ID of the specified state. 
    /// </summary>
    /// <param name="stateName">
    /// State name. <see cref="System.String"/>
    /// </param>
    /// <returns>
    /// State ID. <see cref="System.Int32"/>
    /// </returns>
    public int GetStateId(string stateName)
    {
        if (stateIndexes.ContainsKey(stateName))
            return stateIndexes[stateName];

        return -1;
    }

    /// <summary>
    /// Finds the name of the specified state. 
    /// </summary>
    /// <param name="stateId">
    /// State ID. <see cref="System.Int32"/>
    /// </param>
    /// <returns>
    /// State name. <see cref="System.String"/>
    /// </returns>
    public string FindStateName(int stateId)
    {
        foreach (KeyValuePair<string, int> nameid in stateIndexes)
        {
            if (nameid.Value == stateId)
                return nameid.Key;
        }

        return "";
    }

    /// <summary>
    /// Registers a new transition between states. 
    /// </summary>
    /// <param name="srcState">
    /// Source state. <see cref="System.String"/>
    /// </param>
    /// <param name="trgState">
    /// Target state. <see cref="System.String"/>
    /// </param>
    /// <param name="handler">
    /// State transition handler. <see cref="StateTransitionHandler"/>
    /// </param>
    public void RegisterStateTransition(string srcState, string trgState, StateTransitionHandler handler)
    {
        if (!HasState(srcState) || !HasState(trgState))
        {
            Debug.LogWarning("Attempting to register transition between non-existent states: " +
                             srcState + " - " + trgState);
            return;
        }

        transHandlers[new StateTransition(stateIndexes[srcState], stateIndexes[trgState])] = handler;
    }

    /// <summary>
    /// Unregisters a transition between states. 
    /// </summary>
    /// <param name="srcState">
    /// Source state. <see cref="System.String"/>
    /// </param>
    /// <param name="trgState">
    /// Target state. <see cref="System.String"/>
    /// </param>
    public void UnregisterStateTransition(string srcState, string trgState)
    {
        if (!HasState(srcState) || !HasState(trgState))
        {
            Debug.LogWarning("Attempting to register transition between non-existent states: " +
                             srcState + " - " + trgState);
            return;
        }

        StateTransition trans = new StateTransition(stateIndexes[srcState], stateIndexes[trgState]);

        if (transHandlers.ContainsKey(trans))
            transHandlers.Remove(trans);
    }

    /// <summary>
    /// Initiates a transition to another state. 
    /// </summary>
    /// <param name="nextState">
    /// Target state. <see cref="System.String"/>
    /// </param>
    public void GoToState(string nextState)
    {
        if (!HasState(nextState))
        {
            Debug.LogWarning("Attempting transition to a non-existent state: " + nextState);
            return;
        }

        GoToState(stateIndexes[nextState]);
    }

    /// <summary>
    /// Initiates a transition to another state. 
    /// </summary>
    /// <param name="nextState">
    /// Target state. <see cref="System.String"/>
    /// </param>
    public void GoToState(int nextState)
    {
        StateTransition trans = new StateTransition(state, nextState);

        if (transHandlers.ContainsKey(trans))
        {
            if (transHandlers[trans] != null)
                transHandlers[trans]();

            state = nextState;
        }
        else
        {
            Debug.LogError(string.Format("Invalid state transition from {0} to {1}",
                                          FindStateName(state), FindStateName(nextState)));
        }
    }

    /// <summary>
    /// Performs an update of the FSM.
    /// </summary>
    public void Update()
    {
        if (stateHandlers[state] != null)
            stateHandlers[state]();
    }

    /// <summary>
    /// Performs a late update of the FSM.
    /// </summary>
    public void LateUpdate()
    {
        if (stateHandlersLate[state] != null)
            stateHandlersLate[state]();
    }

    // Set the current state of the FSM.
    public void _SetState(int stateId)
    {
        state = stateId;
    }
}

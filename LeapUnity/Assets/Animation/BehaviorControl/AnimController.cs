using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

public delegate void StateChangeEvtH(AnimController sender, int srcState, int trgState);

/// <summary>
/// Snapshot of the runtime state of an animation controller.
/// </summary>
public interface IAnimControllerState
{
}

/// <summary>
/// Base class for agent
/// animation controllers.
/// </summary>
[RequireComponent(typeof(AnimControllerTree))]
public abstract class AnimController : MonoBehaviour
{
    /// <summary>
    /// Controller state transition definition.
    /// </summary>
    [Serializable]
    public class StateTransitionDef
    {
        public string nextState;
        public string transitionHandler;
    }

    /// <summary>
    /// Controller state definition.
    /// </summary>
    [Serializable]
    public class StateDef
    {
        public string name = "Default";
        public string updateHandler;
        public string lateUpdateHandler;
        public StateTransitionDef[] nextStates = new StateTransitionDef[0];
    }

    /// <summary>
    /// Elapsed time since last update.
    /// </summary>
    public virtual float DeltaTime
    {
        get { return gameObject.GetComponent<AnimControllerTree>().DeltaTime; }
    }

    /// <summary>
    /// Enabled/disable this controller and its subtree.
    /// </summary>
    public bool isEnabled = true;

    /// <summary>
    /// Blend weight with which the controller's animation is applied.
    /// </summary>
    public float weight = 1f;

    /// <summary>
    /// Animation controller states.
    /// </summary>
    public StateDef[] states = new StateDef[1];

    /// <summary>
    /// Children of this animation controller.
    /// </summary>
    public AnimController[] childControllers;

    /// <summary>
    /// Event triggered by FSM state transition. 
    /// </summary>
    public event StateChangeEvtH StateChange;

    /// <summary>
    /// Joints puppeteered by the controller.
    /// </summary>
    public DirectableJoint[] puppetJoints;

    /// <summary>
    /// If true, joint puppeteering is enabled.
    /// </summary>
    public bool puppetEnabled = true;

    /// <summary>
    /// If true, state changes in the controller will be logged.
    /// </summary>
    public bool logStateChange = false;

    protected StateMachine _fsm; // Animation controller's internal state machine
    protected AnimController _parentController = null;
    protected MorphController _morphController = null;
    protected ModelController _modelController = null;

    /// <summary>
    /// Animation controller name. 
    /// </summary>
    public virtual string Name
    {
        get
        {
            return this.GetType().Name;
        }
    }

    /// <summary>
    /// Current state of the animation controller (as name string).
    /// </summary>
    public virtual string State
    {
        get
        {
            return states[_fsm.State].name;
        }
    }

    /// <summary>
    /// Current state of the animation controller (as numeric ID).
    /// </summary>
    public virtual int StateId
    {
        get
        {
            return _fsm.State;
        }
    }

    /// <summary>
    /// Parent of this animation controller. 
    /// </summary>
    public virtual AnimController Parent
    {
        get
        {
            return _parentController;
        }
    }

    /// <summary>
    /// Agent model controller. 
    /// </summary>
    public virtual ModelController ModelController
    {
        get
        {
            return _modelController;
        }
    }

    /// <summary>
    /// Agent morph animation controller.
    /// </summary>
    public virtual MorphController MorphController
    {
        get
        {
            return _morphController;
        }
    }

    /// <summary>
    /// Initiates a transition to another state. 
    /// </summary>
    /// <param name="nextState">
    /// Target state. <see cref="System.String"/>
    /// </param>
    public virtual void GoToState(string nextState)
    {
        GoToState(_fsm.GetStateId(nextState));
    }

    /// <summary>
    /// Initiates a transition to another state. 
    /// </summary>
    /// <param name="nextState">
    /// Target state. <see cref="int"/>
    /// </param>
    public virtual void GoToState(int nextState)
    {
        if (nextState < 0 || nextState >= states.Length)
        {
            Debug.LogWarning("Attempting transition to a non-existent state: " + nextState);

            return;
        }

        // First, notify listeners that controller state is changing
        int old_state = StateId;
        OnStateChange(old_state, nextState);
        // Then, do the actual state change
        _StateTransition(nextState);
        _fsm.GoToState(nextState);
    }

    /// <summary>
    /// Adds a child controller to this controller. 
    /// </summary>
    /// <param name="ctrl">
    /// Child controller. <see cref="AnimController"/>
    /// </param>
    public virtual void AddChild(AnimController ctrl)
    {
        if (ctrl == null)
        {
            Debug.LogWarning("Attempting to add null animation controller as child of " + name);

            return;
        }

        if (ctrl._parentController != null)
        {
            // Controller already child of another controller, detach...
            ctrl._parentController.RemoveChild(ctrl);
        }
        ctrl._parentController = this;

        List<AnimController> children = new List<AnimController>(childControllers);
        if (children.Contains(ctrl))
            // Controller already added as child
            return;
        children.Add(ctrl);
        childControllers = children.ToArray();
    }

    /// <summary>
    /// Removes a child controller from this controller. 
    /// </summary>
    /// <param name="ctrl">
    /// Child controller. <see cref="AnimController"/>
    /// </param>
    public virtual void RemoveChild(AnimController ctrl)
    {
        if (ctrl == null)
        {
            Debug.LogWarning("Attempting to remove null animation controller from " + name);

            return;
        }

        List<AnimController> children = new List<AnimController>(childControllers);
        children.Remove(ctrl);
        ctrl._parentController = null;
        childControllers = children.ToArray();
    }

    /// <summary>
    /// Finds a child controller by name 
    /// </summary>
    /// <param name="childName">
    /// Child controller name. <see cref="System.String"/>
    /// </param>
    /// <returns>
    /// Child controller. <see cref="AnimController"/>
    /// </returns>
    public virtual AnimController FindChild(string childName)
    {
        foreach (AnimController child in childControllers)
        {
            if (child.name == childName)
            {
                return child;
            }
        }

        return null;
    }

    /// <summary>
    /// Get a snapshot of the current runtime state of the animation controller.
    /// </summary>
    /// <returns>Controller state</returns>
    public virtual IAnimControllerState GetRuntimeState()
    {
        return null;
    }

    /// <summary>
    /// Set the current runtime state of the animation controller from a snapshot.
    /// </summary>
    /// <param name="state">Controller state</param>
    public virtual void SetRuntimeState(IAnimControllerState state)
    {
    }

    /// <summary>
    /// Initializes the whole subtree of controllers. 
    /// </summary>
    public virtual void _InitTree()
    {
        // Make sure all empty arrays are constructed:
        if (states == null)
            states = new StateDef[1];
        foreach (StateDef sd in states)
        {

            if (sd.nextStates == null)
                sd.nextStates = new StateTransitionDef[0];
        }

        // Get essential components
        _modelController = GetComponent<ModelController>();
        if (_modelController == null)
        {
            enabled = false;
            return;
        }
        _morphController = GetComponent<MorphController>();

        // Perform initialization
        Debug.Log(string.Format("Initializing {0} on agent {1}", this.Name, gameObject.name));
        _Init();
        _InitFSM();

        foreach (AnimController ctrl in childControllers)
        {
            if (ctrl == null)
                continue;

            ctrl._InitTree();
        }
    }

    /// <summary>
    /// Updates the whole subtree of controllers. 
    /// </summary>
    public virtual void _UpdateTree()
    {
        if (!isEnabled)
        {
            // Controller disabled, can't do anything
            enabled = false;
            return;
        }
        else
        {
            enabled = true;
        }

        _Update();
        _fsm.Update();

        foreach (AnimController ctrl in childControllers)
        {
            if (ctrl == null)
                continue;

            ctrl._UpdateTree();
        }
    }

    /// <summary>
    /// Updates the whole subtree of controllers. 
    /// </summary>
    public virtual void _LateUpdateTree()
    {
        if (!enabled)
            // Controller disabled, can't do anything
            return;

        _LateUpdate();
        _fsm.LateUpdate();

        foreach (AnimController ctrl in childControllers)
        {
            if (ctrl == null)
                continue;

            ctrl._LateUpdateTree();
        }
    }

    // Get the underlying FSM of the current controller
    public virtual StateMachine _GetFSM()
    {
        return _fsm;
    }

    /// <summary>
    /// Initializes this controller. 
    /// </summary>
    protected virtual void _Init()
    {
        // Initialize puppeteered joints
        foreach (DirectableJoint joint in puppetJoints)
            joint.Init(gameObject);
    }

    /// <summary>
    /// Updates this controller. 
    /// </summary>
    protected virtual void _Update()
    {
        if (puppetEnabled)
            _RestorePreviousPose();
    }

    /// <summary>
    /// Updates this controller. 
    /// </summary>
    protected virtual void _LateUpdate()
    {
        if (puppetEnabled)
            _RestorePreviousPose();
    }

    /// <summary>
    /// Initializes the controller's state machine. 
    /// </summary>
    protected virtual void _InitFSM()
    {
        _fsm = new StateMachine();

        // Initialize the FSM - states
        Type ctrl_type = this.GetType();
        foreach (StateDef sd in states)
        {
            StateHandler handler = null, handlerLate = null;

            // Get update handler method (if it exists)
            if (sd.updateHandler == null)
                sd.updateHandler = "";
            MethodInfo mi = ctrl_type.GetMethod(sd.updateHandler, BindingFlags.Instance |
                                                BindingFlags.Public | BindingFlags.NonPublic);
            if (mi != null)
            {
                handler = (StateHandler)Delegate.CreateDelegate(typeof(StateHandler), this, mi);
            }

            // Get late update handler method (if it exists)
            if (sd.lateUpdateHandler == null)
                sd.lateUpdateHandler = "";
            mi = ctrl_type.GetMethod(sd.lateUpdateHandler, BindingFlags.Instance |
                                                BindingFlags.Public | BindingFlags.NonPublic);
            if (mi != null)
            {
                handlerLate = (StateHandler)Delegate.CreateDelegate(typeof(StateHandler), this, mi);
            }

            // Register new state
            _fsm.RegisterState(sd.name, handler, handlerLate);
        }

        // Initialize the FSM - state transitions
        foreach (StateDef sd in states)
        {
            foreach (StateTransitionDef std in sd.nextStates)
            {
                StateTransitionHandler handler = null;

                // Get update handler method (if it exists)
                if (std.transitionHandler == null)
                    std.transitionHandler = "";
                MethodInfo mi = ctrl_type.GetMethod(std.transitionHandler, BindingFlags.Instance |
                                                BindingFlags.Public | BindingFlags.NonPublic);
                if (mi != null)
                {
                    handler = (StateTransitionHandler)Delegate.CreateDelegate(typeof(StateTransitionHandler), this, mi);
                }

                // Register new state transition
                _fsm.RegisterStateTransition(sd.name, std.nextState, handler);
            }
        }
    }

    /// <summary>
    /// Default state transition handler. 
    /// </summary>
    /// <param name="nextState">
    /// Target state ID. <see cref="System.Int32"/>
    /// </param>
    protected virtual void _StateTransition(int nextState)
    {
        if (logStateChange)
            Debug.Log(string.Format("State change from {0} to {1}", State, _fsm.FindStateName(nextState)));
    }

    protected virtual void OnStateChange(int srcState, int trgState)
    {
        if (StateChange != null)
            StateChange(this, srcState, trgState);
    }

    /// <summary>
    /// Helper method for initializing the array of state definitions.
    /// </summary>
    protected void _InitStateDefs<ST>()
        where ST : struct, IComparable, IFormattable, IConvertible
    {
        string[] names = System.Enum.GetNames(typeof(ST));

        // Create state array
        states = new StateDef[names.Length];

        // Create each state
        for (int name_i = 0; name_i < names.Length; ++name_i)
        {
            states[name_i] = new AnimController.StateDef();
            states[name_i].name = names[name_i];
        }
    }

    /// <summary>
    /// Helper method for initializing an array of state transitions.
    /// </summary>
    /// <param name="srcState">
    /// Source state ID. <see cref="StateDef"/>
    /// </param>
    /// <param name="numTrans">
    /// Number of transitions starting in the source state.
    /// </param>
    protected void _InitStateTransDefs(int srcState, int numTrans)
    {
        states[srcState].nextStates = new StateTransitionDef[numTrans];

        for (int tri = 0; tri < numTrans; ++tri)
            states[srcState].nextStates[tri] = new AnimController.StateTransitionDef();
    }

    // Restores the previous frame pose of joints that are directly puppeteered
    // by this controller.
    protected virtual void _RestorePreviousPose()
    {
        foreach (DirectableJoint joint in puppetJoints)
            joint.bone.localRotation = ModelController.GetPrevRotation(joint.bone);
    }

    /// <summary>
    /// Editor method that creates initial state definitions
    /// for this controller.
    /// </summary>
    public virtual void _CreateStates()
    {
    }
}

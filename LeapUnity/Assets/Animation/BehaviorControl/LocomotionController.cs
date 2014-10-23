using UnityEngine;
using System.Collections;

public enum LocomotionState
{
	Stationary,
	Walking
};

/// <summary>
/// Animation controller for locomotion and
/// all other forms of full-, upper- and lower-body motion.
/// </summary>
public class LocomotionController : AnimController
{
	protected override void _Init()
	{
	}
	
	protected virtual void Update_Stationary()
	{
	}
	
	protected virtual void Update_Walking()
	{
	}
	
	protected virtual void Transition_StationaryWalking()
	{
	}
	
	protected virtual void Transition_WalkingStationary()
	{
	}
	
	public override void _CreateStates()
	{
		// Initialize states
		_InitStateDefs<LocomotionState>();
		_InitStateTransDefs( (int)LocomotionState.Stationary, 1 );
		_InitStateTransDefs( (int)LocomotionState.Walking, 1 );
		states[(int)LocomotionState.Stationary].updateHandler = "Update_Stationary";
		states[(int)LocomotionState.Stationary].nextStates[0].nextState = "Stationary";
		states[(int)LocomotionState.Stationary].nextStates[0].transitionHandler = "Transition_StationaryWalking";
		states[(int)LocomotionState.Walking].updateHandler = "Update_Walking";
		states[(int)LocomotionState.Walking].nextStates[0].nextState = "Walking";
		states[(int)LocomotionState.Walking].nextStates[0].transitionHandler = "Transition_WalkingStationary";
	}
}


using UnityEngine;
using System.Collections;

public enum GestureState
{
	NoGesture,
	Gesturing
}

/// <summary>
/// Animation controller for upper body gestures. 
/// </summary>
public class GestureController : AnimController
{
	/// <summary>
	/// Gesture animation to play. 
	/// </summary>
	public string gestureAnimation = "";
	
	/// <summary>
	/// Set to true to perform the gesture. 
	/// </summary>
	public bool doGesture = false;
	
	/// <summary>
	/// Executes an upper-body gesture. 
	/// </summary>
	/// <param name="gestureName">
	/// Gesture animation name <see cref="System.String"/>
	/// </param>
	public virtual void DoGesture( string gestureName )
	{
		doGesture = true;
		gestureAnimation = gestureName;
	}

	protected override void _Init()
	{
	}
	
	protected virtual void Update_NoGesture()
	{
		if(doGesture)
		{
			animation[gestureAnimation].enabled = true;
			animation[gestureAnimation].layer = 1;
			animation[gestureAnimation].weight = 1f;
			animation[gestureAnimation].wrapMode = WrapMode.Once;
			
			doGesture = false;
			GoToState((int)GestureState.Gesturing);
		}
	}
	
	protected virtual void Update_Gesturing()
	{
		if( animation[gestureAnimation].enabled == false )
			GoToState((int)GestureState.NoGesture);
	}
	
	protected virtual void Transition_NoGestureGesturing()
	{
		doGesture = false;
	}
	
	protected virtual void Transition_GesturingNoGesture()
	{
	}
	
	protected virtual void Transition_Gesturing()
	{
	}
	
	public override void UEdCreateStates()
	{
		// Initialize states
		_InitStateDefs<GestureState>();
		_InitStateTransDefs( (int)GestureState.NoGesture, 1 );
		_InitStateTransDefs( (int)GestureState.Gesturing, 1 );
		states[(int)GestureState.NoGesture].updateHandler = "Update_NoGesture";
		states[(int)GestureState.NoGesture].nextStates[0].nextState = "Gesturing";
		states[(int)GestureState.NoGesture].nextStates[0].transitionHandler = "Transition_NoGestureGesturing";
		states[(int)GestureState.Gesturing].updateHandler = "Update_Gesturing";
		states[(int)GestureState.Gesturing].nextStates[0].nextState = "NoGesture";
		states[(int)GestureState.Gesturing].nextStates[0].transitionHandler = "Transition_GesturingNoGesture";
	}
}

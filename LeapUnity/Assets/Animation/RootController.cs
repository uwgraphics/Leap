using UnityEngine;

public enum RootState
{
	Animated = 0,
	Static
};

/// <summary>
/// Root animation controller for the virtual agent.
/// </summary>
public class RootController : AnimController
{
	protected override void _Init()
	{
	}
	
	protected virtual void Update_Animated()
	{
	}
	
	protected virtual void Update_Static()
	{
	}
	
	protected virtual void Transition_AnimatedStatic()
	{
		// TODO: disable all animation
	}
	
	protected virtual void Transition_StaticAnimated()
	{
		// TODO: enable all animation
	}
	
	public override void UEdCreateStates()
	{
		// Initialize states
		_InitStateDefs<RootState>();
		_InitStateTransDefs( (int)RootState.Animated, 1 );
		_InitStateTransDefs( (int)RootState.Static, 1 );
		states[(int)RootState.Animated].updateHandler = "Update_Animated";
		states[(int)RootState.Animated].nextStates[0].nextState = "Static";
		states[(int)RootState.Animated].nextStates[0].transitionHandler = "Transition_AnimatedStatic";
		states[(int)RootState.Static].updateHandler = "Update_Static";
		states[(int)RootState.Static].nextStates[0].nextState = "Animated";
		states[(int)RootState.Static].nextStates[0].transitionHandler = "Transition_StaticAnimated";
	}
}

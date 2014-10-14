using UnityEngine;
using System.Collections;

public enum BodyIdleState
{
	Idle,
	Engaged
};

/// <summary>
/// Animation controller for idle body motion. 
/// </summary>
public class BodyIdleController : AnimController
{
	/// <summary>
	/// Posture shift animation. 
	/// </summary>
	public string postureAnimation = "NeutralPose";
	
	/// <summary>
	/// Set to true to perform a posture shift.
	/// </summary>
	public bool changePosture = false;
	
	/// <summary>
	/// How long the posture change should take.
	/// </summary>
	public float postureChangeTime = 0f;
	
	/// <summary>
	/// Blend weight of the new posture.
	/// </summary>
	public float postureWeight = 1f;

	/// <summary>
	/// If true, subtle, random motion generated using Perlin noise
	/// will be applied to the body.
	/// </summary>
	public bool randomMotionEnabled = true;
	
	/// <summary>
	/// For generating random head motion. 
	/// </summary>
	public PerlinMotionGenerator randomMotionGen = new PerlinMotionGenerator();
	
	/// <summary>
	/// If true, random motion generated using Perlin noise
	/// is applied to the head, similar to the kind of head motion
	/// humans exibit while speaking.
	/// </summary>
	public bool speechMotionEnabled = false;
	
	protected string curPostureAnim = "";
	
	/// <summary>
	/// Changes to a different posture.
	/// </summary>
	/// <param name="animName">
	/// Posture shift animation name <see cref="System.String"/>
	/// </param>
	public virtual void ChangePosture( string animName )
	{
		changePosture = true;
		postureAnimation = animName;
	}

	protected override void _Init()
	{
		changePosture = true;
		
		// Initialize random motion generators
		randomMotionGen.Init(gameObject);
	}
	
	protected virtual void Update_Idle()
	{	
		if(changePosture)
		{
			if( curPostureAnim != "" )
				animation[curPostureAnim].enabled = false;
			
			animation[postureAnimation].time = 0;
			animation[postureAnimation].enabled = true;
			animation[postureAnimation].layer = 0;
			animation[postureAnimation].speed = animation[postureAnimation].length/(postureChangeTime+0.00001f);
			animation[postureAnimation].weight = postureWeight;
			animation[postureAnimation].wrapMode = WrapMode.ClampForever;
			
			curPostureAnim = postureAnimation;
			changePosture = false;
		}
	}
	
	protected virtual void LateUpdate_Idle()
	{	
		if(randomMotionEnabled)
		{
			if(!randomMotionGen.Running)
				randomMotionGen.Start();
			
			// Update and apply random body motion
			randomMotionGen.Update();
			randomMotionGen.LateApply();
		}
		else
		{
			randomMotionGen.Stop();
		}
		
		/*if( randomMotionEnabled && speechMotionEnabled )
			// Start performing more pronounced random body motion
			GoToState((int)BodyIdleState.Engaged);*/
	}
	
	protected virtual void Update_Engaged()
	{
		if(randomMotionEnabled)
			puppetEnabled = true;
		
		if(changePosture)
		{
			if( curPostureAnim != "" )
				animation[curPostureAnim].enabled = false;
			
			animation[postureAnimation].enabled = true;
			animation[postureAnimation].layer = 0;
			animation[postureAnimation].speed = animation[postureAnimation].length/(postureChangeTime+0.00001f);
			animation[postureAnimation].weight = postureWeight;
			animation[postureAnimation].wrapMode = WrapMode.ClampForever;
			
			curPostureAnim = postureAnimation;
			changePosture = false;
		}
	}
	
	protected virtual void LateUpdate_Engaged()
	{	
		if(randomMotionEnabled)
		{
			// Update and apply random body motion
			if(!randomMotionGen.Running)
				randomMotionGen.Start();
			randomMotionGen.Update();
			randomMotionGen.LateApply();
		}
		
		if( !speechMotionEnabled )
			// Go back to more subtle random head motion
			GoToState((int)BodyIdleState.Idle);
			return;
	}
	
	protected virtual void Transition_IdleEngaged()
	{
		randomMotionGen.GoToPreset2();
	}
	
	protected virtual void Transition_EngagedIdle()
	{
		randomMotionGen.GoToPreset1();
	}

	public override void UEdCreateStates()
	{
		// Initialize states
		_InitStateDefs<BodyIdleState>();
		_InitStateTransDefs( (int)BodyIdleState.Idle, 1 );
		_InitStateTransDefs( (int)BodyIdleState.Engaged, 1 );
		states[(int)BodyIdleState.Idle].updateHandler = "Update_Idle";
		states[(int)BodyIdleState.Idle].lateUpdateHandler = "LateUpdate_Idle";
		states[(int)BodyIdleState.Idle].nextStates[0].nextState = "Engaged";
		states[(int)BodyIdleState.Idle].nextStates[0].transitionHandler = "Transition_IdleEngaged";
		states[(int)BodyIdleState.Engaged].updateHandler = "Update_Engaged";
		states[(int)BodyIdleState.Engaged].nextStates[0].nextState = "Idle";
		states[(int)BodyIdleState.Engaged].nextStates[0].transitionHandler = "Transition_EngagedIdle";
	}
}


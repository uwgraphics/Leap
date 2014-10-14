using UnityEngine;
using System.Collections;

public class StoryScenario : Scenario
{
	public enum ConditionType
	{
		NoGaze,
		HumanlikeGaze,
		StylizedGaze
	}
	
	public ConditionType condition;
	public string agentName;
	
	protected PhotoMosaic phMosaic;
	
	///<see cref="Scenario.GazeAt"/>
	public override int GazeAt( string agentName, string targetName )
	{
		if( targetName.StartsWith("PPStory") )
		{
			_AlignPhotoTarget(targetName);
			targetName = "GTPhoto";
		}
		
		if( condition == ConditionType.NoGaze )
			return targetName != "GTEyeContact" ? -1 : base.GazeAt(agentName,targetName,1f,0f);
		
		return base.GazeAt( agentName, targetName );
	}
	
	/// <see cref="Scenario.GazeAt"/>
	public override int GazeAt( string agentName, string targetName,
	                           float headAlign, float headLatency )
	{
		if( targetName.StartsWith("PPStory") )
		{
			_AlignPhotoTarget(targetName);
			targetName = "GTPhoto";
		}
		
		if( condition == ConditionType.NoGaze )
			return targetName != "GTEyeContact" ? -1 : base.GazeAt(agentName,targetName,1f,0f);
		
		return base.GazeAt( agentName, targetName, headAlign, headLatency );
	}
	
	/// <see cref="Scenario.GazeAt"/>
	public override int GazeAt( string agentName, string targetName,
	                           float headAlign, float headLatency,
	                           float bodyAlign, float bodyLatency, float eyeAlign )
	{
		if( targetName.StartsWith("PPStory") )
		{
			_AlignPhotoTarget(targetName);
			targetName = "GTPhoto";
		}
		
		if( condition == ConditionType.NoGaze )
			return targetName != "GTEyeContact" ? -1 : base.GazeAt(agentName,targetName,1f,0f);
		
		return base.GazeAt( agentName, targetName, headAlign, headLatency,
		                   bodyAlign, bodyLatency, eyeAlign );
	}
	
	/// <summary>
	/// Sets the gaze shift path curving parameters. 
	/// </summary>
	/// <param name="agentName">
	/// Virtual agent name.
	/// </param>
	/// <param name="lEyeArcs">
	/// Left eye path curving.
	/// </param>
	/// <param name="rEyeArcs">
	/// Right eye path curving.
	/// </param>
	/// <param name="headArcs">
	/// Head path curving/
	/// </param>
	public virtual void SetGazePathArcs( string agentName, float lEyeArcs,
	                               float rEyeArcs, float headArcs )
	{
		// TODO: remove this method
	}
	
	/// <summary>
	/// Hide the scene by deactivating lights and unlit objects.
	/// </summary>
	public virtual void HideScene()
	{
		SetObjectActive("SunUpLeft",false);
		SetObjectActive("SunLeft",false);
		SetObjectActive("SunDownRight",false);
		SetObjectActive("BgPanel",false);
	}
	
	/// <summary>
	/// Show the scene by activating lights and unlit objects. 
	/// </summary>
	public virtual void ShowScene()
	{
		SetObjectActive("SunUpLeft",true);
		SetObjectActive("SunLeft",true);
		SetObjectActive("SunDownRight",true);
		SetObjectActive("BgPanel",true);
	}
	
	/// <see cref="Scenario._Init()"/>
	protected override void _Init()
	{
		// Initialize important variables
		//phMosaic = GameObject.FindGameObjectWithTag("PhotoMosaic").GetComponent<PhotoMosaic>();
	}
	
	/// <see cref="Scenario._Run()"/>
	protected override IEnumerator _Run()
	{
		// Configure gaze based on condition
		GazeController gctrl = agents[agentName].GetComponent<GazeController>();
		ExpressionController exprCtrl = agents[agentName].GetComponent<ExpressionController>();
		BlinkController bctrl = agents[agentName].GetComponent<BlinkController>();
		gctrl.stylizeGaze = condition == ConditionType.StylizedGaze;
		bctrl.gazeEvokedBlinks = false;
		
		// Hide scene
		//HideScene();
		
		int curgaze = -1;
		
		// Smile!
		int curexpr = ChangeExpression(agentName,"ExpressionSmileClosed",1f,0f);
		yield return StartCoroutine( WaitUntilFinished(curexpr) );
		exprCtrl.FixExpression();
		
		// Initialize gaze
		yield return new WaitForSeconds(0.3f);
		//
		//Time.timeScale = 0.2f;
		//
		curgaze = GazeAtCamera(agentName,1f,0f);
		yield return StartCoroutine( WaitUntilFinished(curgaze) );
	
		// Reveal scene
		ShowScene();
	}
	
	/// <see cref="Scenario._Finish()"/>
	protected override void _Finish()
	{
	}
	
	protected virtual void _AlignPhotoTarget( string targetName )
	{
		// Position the gaze target
		GameObject photo = phMosaic[targetName];
		Vector3 curpos = phMosaic.transform.localPosition;
		Vector3 curscal = phMosaic.transform.localScale;
		Vector3 view = ( cameras["Main Camera"].transform.position -
		                photo.transform.position ).normalized;
		gazeTargets["GTPhoto"].transform.position =
			photo.transform.position + 0.906f*view;
	}
}

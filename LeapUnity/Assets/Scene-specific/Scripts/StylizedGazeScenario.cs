using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class StylizedGazeScenario : Scenario
{
	public enum ConditionType
	{
		NoGaze,
		HumanlikeGaze,
		StylizedGaze
	}
	
	public ConditionType condition;
	
	protected string agentName;
	protected PhotoMosaic phMosaic;
	
	///<see cref="Scenario.GazeAt"/>
	public override int GazeAt( string agentName, string targetName )
	{
		if( condition == ConditionType.NoGaze )
			return -1;
		
		return base.GazeAt(agentName,targetName);
	}
	
	/// <see cref="Scenario.GazeAt"/>
	public override int GazeAt( string agentName, string targetName,
	                           float headAlign, float headLatency )
	{
		if( condition == ConditionType.NoGaze )
			return -1;
		
		return base.GazeAt (agentName, targetName, headAlign, headLatency);
	}
	
	/// <summary>
	/// Gaze at specified photo in the mosaic. 
	/// </summary>
	/// <param name="agentName">
	/// Virtual agent name.
	/// </param>
	/// <param name="targetName">
	/// Target photo name.
	/// </param>
	/// <param name="headAlign">
	/// Head alignment.
	/// </param>
	/// <param name="headLatency">
	/// Head latency.
	/// </param>
	/// <returns>
	/// Gaze action ID.
	/// </returns>
	public virtual int GazeAtPhoto( string agentName, string targetName,
	                       float headAlign, float headLatency )
	{
		// Compute mosaic normal
		Vector3 norm = phMosaic.transform.localRotation*(new Vector3(0,1,0));
		norm.Normalize();
		
		// Position the gaze target
		GameObject photo = phMosaic[targetName];
		Vector3 curpos = phMosaic.transform.localPosition;
		Vector3 curscal = phMosaic.transform.localScale;
		PhotoMosaic.KeyFrame kf = phMosaic.FindKeyFrame(targetName);
		phMosaic.transform.localPosition = kf.position;
		phMosaic.transform.localScale = kf.scale;
		gazeTargets["GTPhoto"].transform.position =
			photo.transform.position + 0.7356f*norm;
		phMosaic.transform.localPosition = curpos;
		phMosaic.transform.localScale = curscal;
		
		return GazeAt(agentName,"GTPhoto",headAlign,headLatency);
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
	
	/// <see cref="Scenario._Init()"/>
	protected override void _Init()
	{
		// Set frame rate and resolution
		// TODO: won't be needed when all conditions are finalized
		Application.targetFrameRate = 30;
		Screen.SetResolution( 1280, 720, false );
		
		// Initialize important variables
		agentName = GameObject.FindGameObjectWithTag("Agent").name;
		phMosaic = GameObject.FindGameObjectWithTag("PhotoMosaic").GetComponent<PhotoMosaic>();
	}
	
	/// <see cref="Scenario._Run()"/>
	protected override IEnumerator _Run()
	{
		int curgaze = -1;
		
		// Hide scene
		SetObjectActive("SunDownRight",false);
		SetObjectActive("SunLeft",false);
		SetObjectActive("SunUpLeft",false);
		phMosaic.gameObject.SetActiveRecursively(false);
		phMosaic.MoveTo("Start",0);
		
		// Initialize gaze
		yield return new WaitForSeconds(0.6f);
		curgaze = base.GazeAt(agentName,"GTEyeContact",0.5f,0f);
		yield return StartCoroutine( WaitUntilFinished(curgaze) );
		curgaze = base.GazeAt(agentName,"GTEyeContact",0f,0f);
		yield return StartCoroutine( WaitUntilFinished(curgaze) );
		
		// Configure gaze based on condition
		GazeController gctrl = agents[agentName].GetComponent<GazeController>();
		gctrl.stylizeGaze = condition == ConditionType.StylizedGaze;
		
		// Reveal scene
		phMosaic.gameObject.SetActiveRecursively(true);
		SetObjectActive("SunDownRight",true);
		SetObjectActive("SunLeft",true);
		SetObjectActive("SunUpLeft",true);
	}
	
	/// <see cref="Scenario._Finish()"/>
	protected override void _Finish()
	{
	}
};

using UnityEngine;
using System.Collections;

public enum ConditionType
{
	Audio,
	Affiliative,
	Referential,
	Both
};

public abstract class LectureScenario : Scenario
{
	protected GameObject agent; // Agent in the scene
	
	protected bool gazeShiftFinished = false;
	protected bool speechFinished = false;
	
	protected GazeController gazeCtrl;
	protected SpeechController speechCtrl;
	protected ExpressionController expressionCtrl;
	
	public ConditionType condition = ConditionType.Affiliative;
	
	protected float mapAlign = 0f;
	protected float cameraAlign = 1f;

	protected override void _Init()
	{
		// TODO: remove this
		// Set frame rate and resolution
		Application.targetFrameRate = 30;
		Screen.SetResolution( 1280, 800, false );
		//
		if( agent == null )
		{
			agent = GameObject.FindGameObjectWithTag("Player");
		}
		
		if( agent == null )
		{
			// No agent in the scene
			return;
		}
		
		gazeCtrl = agent.GetComponent<GazeController>();
		speechCtrl = agent.GetComponent<SpeechController>();
		expressionCtrl = agent.GetComponent<ExpressionController>();
		
		// Register for gaze events
		gazeCtrl.StateChange += new StateChangeEvtH(GazeController_StateChange);
		// Register for speech events
		speechCtrl.StateChange += new StateChangeEvtH(SpeechController_StateChange);
		
		//Initialize head alignment parameters based on the condition
		if (condition == ConditionType.Affiliative)
		{
			mapAlign = 0f;
			cameraAlign = 1f;
		}
		else if (condition == ConditionType.Both)
		{
			mapAlign = 1f;
			cameraAlign = 1f;
		}
		else if (condition == ConditionType.Referential)
		{
			mapAlign = 1f;
			cameraAlign = 0f;
		}
	}
	
	protected IEnumerator WaitForGazeShiftFinished()
	{
		while( !gazeShiftFinished )
		{
			yield return 0;
		}
		
		gazeShiftFinished = false;
	}
	
	protected IEnumerator WaitForSpeechFinished()
	{
		while( !speechFinished )
		{
			yield return 0;
		}
		
		speechFinished = false;
	}
	
	protected virtual void GazeController_StateChange( AnimController sender, int srcState, int trgState )
	{
		if( srcState == (int)GazeState.NoGaze &&
		   trgState == (int)GazeState.Shifting )
		{
			// Gaze shift starting
			//gazeCtrl.GazeAt(gazeTarget);
		}
		/*else if( srcState == (int)GazeState.Shifting &&
		   trgState == (int)GazeState.FixedOnTarget )
		{
			gazeShiftFinished = true;
		}*/
	}
	
	protected virtual void SpeechController_StateChange( AnimController sender, int srcState, int trgState )
	{
		if( srcState == (int)SpeechState.Speaking &&
		   trgState == (int)SpeechState.NoSpeech )
		{
			// Speech finished, begin next speech
			speechFinished = true;
		}
	}
}

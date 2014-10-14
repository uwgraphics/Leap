using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class IvyStylizedGazeScenario : StylizedGazeScenario
{
	/// <see cref="Scenario._Run()"/>
	protected override IEnumerator _Run()
	{
		int curspeak = -1;
		int curgaze = -1;
		
		yield return StartCoroutine(base._Run());
		
		// The agent should smile the whole time?
		this[agentName].GetComponent<ExpressionController>().magnitude = 0.2f;
		this[agentName].GetComponent<ExpressionController>().InstaChangeExpression("ExpressionSmileOpen");
		this[agentName].GetComponent<ExpressionController>().FixExpression();
		
		// Intro
		yield return new WaitForSeconds(2f);
		curspeak = Speak(agentName,"dubrovnik1_01");
		yield return new WaitForSeconds(0.2f);
		ChangeExpression(agentName,"ModifierBrowUp",0.5f,0.2f);
		yield return new WaitForSeconds(0.2f);
		HeadNod(agentName,1,FaceGestureSpeed.Normal,4f,0f);
		yield return new WaitForSeconds(0.1f);
		ChangeExpression(agentName,"ModifierBrowUp",0f,0.2f);
		yield return new WaitForSeconds(1.6f);
		
		// Gaze at panorama and back
		SetGazePathArcs(agentName,12f,12f,86f);
		curgaze = GazeAtPhoto(agentName,"PPPanorama1",0.3f,0f);
		yield return new WaitForSeconds(0.4f);
		phMosaic.MoveTo("PPPanorama1",0.8f);
		yield return StartCoroutine( WaitUntilFinished(curgaze) );
		yield return new WaitForSeconds(0.5f);
		curgaze = GazeAt(agentName,"GTEyeContact",0.8f,0f);
		yield return new WaitForSeconds(0.5f);
		ChangeExpression(agentName,"ModifierBrowUp",1f,0.3f);
		yield return new WaitForSeconds(0.5f);
		ChangeExpression(agentName,"ModifierBrowUp",0f,0.3f);
		yield return new WaitForSeconds(0.1f);
		
		// Finish intro
		yield return StartCoroutine( WaitUntilFinished(curspeak) );
		yield return new WaitForSeconds(1.5f);
		
		// About Old Town
		phMosaic.MoveTo("PPOldTown2",0.8f);
		yield return new WaitForSeconds(1f);
		curspeak = Speak(agentName,"dubrovnik1_02");
		
		// Gaze at Old Town and back
		yield return new WaitForSeconds(0.2f);
		ChangeExpression(agentName,"ModifierBrowUp",1f,0.3f);
		yield return new WaitForSeconds(0.5f);
		ChangeExpression(agentName,"ModifierBrowUp",0f,0.3f);
		yield return new WaitForSeconds(0.8f);
		SetGazePathArcs(agentName,11f,11f,79f);
		curgaze = GazeAtPhoto(agentName,"PPOldTown2",0.3f,0f);
		yield return StartCoroutine( WaitUntilFinished(curgaze) );
		yield return new WaitForSeconds(0.6f);
		curgaze = GazeAt(agentName,"GTEyeContact",0.85f,0f);
		
		// About Old Town (cont'd)
		yield return StartCoroutine( WaitUntilFinished(curspeak) );
		yield return new WaitForSeconds(0.6f);

		// About Pile gate
		yield return new WaitForSeconds(0.6f);
		curspeak = Speak(agentName,"dubrovnik1_03");
		
		// Gaze at Pile gate and back
		SetGazePathArcs(agentName,14f,14f,100f);
		curgaze = GazeAtPhoto(agentName,"PPPile1",0.3f,0f);
		yield return new WaitForSeconds(0.4f);
		phMosaic.MoveTo("PPPile1",0.8f);
		
		// About Pile Gate (cont'd)
		yield return StartCoroutine( WaitUntilFinished(curspeak) );
		
		// About Stradun
		yield return new WaitForSeconds(0.5f);
		curspeak = Speak(agentName,"dubrovnik1_04");
		
		// Gaze at Stradun and back
		yield return new WaitForSeconds(0.2f);
		ChangeExpression(agentName,"ModifierBrowUp",1f,0.3f);
		yield return new WaitForSeconds(0.4f);
		ChangeExpression(agentName,"ModifierBrowUp",0f,0.3f);
		SetGazePathArcs(agentName,9f,9f,64f);
		curgaze = GazeAtPhoto(agentName,"PPStradun2",0.7f,0f);
		yield return new WaitForSeconds(0.4f);
		phMosaic.MoveTo("PPStradun2",0.8f);
		yield return new WaitForSeconds(1.8f);
		curgaze = GazeAt(agentName,"GTEyeContact",0.8f,0f);
		
		// About Stradun (cont'd)
		yield return StartCoroutine( WaitUntilFinished(curspeak) );
		yield return new WaitForSeconds(0.8f);
		
		// About Stradun #2
		yield return new WaitForSeconds(0.8f);
		curspeak = Speak(agentName,"dubrovnik1_05");
		yield return new WaitForSeconds(1f);
		ChangeExpression(agentName,"ModifierBrowUp",1f,0.3f);
		yield return new WaitForSeconds(0.5f);
		ChangeExpression(agentName,"ModifierBrowUp",0f,0.3f);
		yield return StartCoroutine( WaitUntilFinished(curspeak) );
		
		// About palace
		yield return new WaitForSeconds(0.6f);
		curspeak = Speak(agentName,"dubrovnik1_06");
		
		// Gaze at palace and back
		yield return new WaitForSeconds(0.25f);
		SetGazePathArcs(agentName,12f,12f,86f);
		curgaze = GazeAtPhoto(agentName,"PPRectors4",0.4f,0f);
		ChangeExpression(agentName,"ModifierBrowUp",0.7f,0.3f);
		yield return new WaitForSeconds(0.4f);
		phMosaic.MoveTo("PPRectors4",0.8f);
		yield return new WaitForSeconds(0.3f);
		ChangeExpression(agentName,"ModifierBrowUp",0f,0.6f);
		yield return new WaitForSeconds(1.3f);
		
		// About palace (cont'd)
		yield return StartCoroutine( WaitUntilFinished(curspeak) );
		
		// About church
		yield return new WaitForSeconds(0.6f);
		curspeak = Speak(agentName,"dubrovnik1_07");
		
		// Gaze at church and back
		yield return new WaitForSeconds(0.1f);
		SetGazePathArcs(agentName,6f,6f,43f);
		curgaze = GazeAtPhoto(agentName,"PPStBlaise1",0.6f,0f);
		yield return new WaitForSeconds(0.4f);
		phMosaic.MoveTo("PPStBlaise1",0.8f);
		yield return new WaitForSeconds(0.9f);
		curgaze = GazeAt(agentName,"GTEyeContact",0.3f,0f);
		
		// About church (cont'd)
		yield return StartCoroutine( WaitUntilFinished(curspeak) );
		
		// About monastery
		yield return new WaitForSeconds(1.8f);
		curspeak = Speak(agentName,"dubrovnik1_08");
		
		// Gaze at monastery and back
		yield return new WaitForSeconds(0.3f);
		ChangeExpression(agentName,"ModifierBrowUp",0.7f,0.3f);
		SetGazePathArcs(agentName,11f,11f,79f);
		curgaze = GazeAtPhoto(agentName,"PPFrMonastery1",0.5f,0f);
		yield return new WaitForSeconds(0.4f);
		phMosaic.MoveTo("PPFrMonastery1",0.8f);
		yield return new WaitForSeconds(0.1f);
		ChangeExpression(agentName,"ModifierBrowUp",0f,0.3f);
		yield return StartCoroutine( WaitUntilFinished(curgaze) );
		
		// About monastery (cont'd)
		yield return StartCoroutine( WaitUntilFinished(curspeak) );
		
		// About cloister
		yield return new WaitForSeconds(0.4f);
		curspeak = Speak(agentName,"dubrovnik1_09");
		
		// Gaze at cloister and back
		yield return new WaitForSeconds(0.1f);
		curgaze = GazeAtPhoto(agentName,"PPFrMonastery2",0.8f,0f);
		yield return new WaitForSeconds(0.4f);
		phMosaic.MoveTo("PPFrMonastery2",0.8f);
		yield return new WaitForSeconds(1.6f);
		ChangeExpression(agentName,"ModifierBrowUp",1f,0.3f);
		yield return new WaitForSeconds(0.3f);
		curgaze = GazeAt(agentName,"GTEyeContact",0.7f,0f);
		yield return new WaitForSeconds(0.2f);
		ChangeExpression(agentName,"ModifierBrowUp",0f,0.3f);
		
		// About cloister (cont'd)
		yield return StartCoroutine( WaitUntilFinished(curspeak) );
		
		// About city walls
		yield return new WaitForSeconds(1.8f);
		curspeak = Speak(agentName,"dubrovnik1_10");
		
		// Gaze at city walls and back
		ChangeExpression(agentName,"ModifierBrowUp",0.6f,0.3f);
		yield return new WaitForSeconds(0.5f);
		ChangeExpression(agentName,"ModifierBrowUp",0f,0.3f);
		yield return new WaitForSeconds(1.3f);
		SetGazePathArcs(agentName,12f,12f,86f);
		curgaze = GazeAtPhoto(agentName,"PPCityWalls3",0.6f,0f);
		yield return new WaitForSeconds(0.4f);
		phMosaic.MoveTo("PPCityWalls3",0.8f);
		
		// About city walls (cont'd)
		yield return StartCoroutine( WaitUntilFinished(curspeak) );
		
		// About city walls #2
		SetGazePathArcs(agentName,9f,9f,64f);
		curgaze = GazeAt(agentName,"GTEyeContact",0.2f,0f);
		yield return new WaitForSeconds(0.3f);
		curspeak = Speak(agentName,"dubrovnik1_11");
		
		// Gaze at city walls and back
		yield return new WaitForSeconds(1.4f);
		ChangeExpression(agentName,"ModifierBrowUp",0.6f,0.2f);
		yield return new WaitForSeconds(0.4f);
		ChangeExpression(agentName,"ModifierBrowUp",0f,0.2f);
		curgaze = GazeAt(agentName,"GTEyeContact",0.8f,0f);
		
		// About city walls #2 (cont'd)
		yield return StartCoroutine( WaitUntilFinished(curspeak) );
		yield return new WaitForSeconds(0.8f);

		// About restaurant
		yield return new WaitForSeconds(0.8f);
		curspeak = Speak(agentName,"dubrovnik1_12");
		yield return new WaitForSeconds(3.5f);
		ChangeExpression(agentName,"ModifierBrowUp",1f,0.3f);
		yield return new WaitForSeconds(0.5f);
		ChangeExpression(agentName,"ModifierBrowUp",0f,0.3f);
		
		// About restaurant (cont'd)
		yield return StartCoroutine( WaitUntilFinished(curspeak) );
		
		// About restaurant #2
		yield return new WaitForSeconds(0.3f);
		curspeak = Speak(agentName,"dubrovnik1_13");

		// Gaze at restaurant and back
		yield return new WaitForSeconds(0.3f);
		SetGazePathArcs(agentName,14f,14f,100f);
		curgaze = GazeAtPhoto(agentName,"PPNautika3",0.4f,0f);
		yield return new WaitForSeconds(0.4f);
		phMosaic.MoveTo("PPNautika3",0.8f);
		yield return new WaitForSeconds(1f);
		ChangeExpression(agentName,"ModifierBrowUp",0.8f,0.25f);
		yield return new WaitForSeconds(0.45f);
		ChangeExpression(agentName,"ModifierBrowUp",0f,0.25f);
		yield return new WaitForSeconds(0.8f);
		curgaze = GazeAt(agentName,"GTEyeContact",0.8f,0f);
		
		// About a restaurant #2 (cont'd)
		yield return StartCoroutine( WaitUntilFinished(curspeak) );
		yield return new WaitForSeconds(2f);
		
		// Conclusion
		phMosaic.MoveTo("PPNightfall",0.8f);
		yield return new WaitForSeconds(0.8f);
		curspeak = Speak(agentName,"dubrovnik1_14");
		yield return new WaitForSeconds(1.2f);
		HeadNod(agentName,1,FaceGestureSpeed.Normal,4f,0f);
		ChangeExpression(agentName,"ModifierBrowUp",0.8f,0.25f);
		yield return new WaitForSeconds(0.45f);
		ChangeExpression(agentName,"ModifierBrowUp",0f,0.25f);
		yield return StartCoroutine( WaitUntilFinished(curspeak) );
		yield return new WaitForSeconds(1f);
	}
};

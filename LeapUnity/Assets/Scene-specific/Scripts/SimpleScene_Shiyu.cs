using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

public class SimpleScene_Shiyu : Scenario
{
	
	private string agentName = "Lily";
	
	/// <see cref="Scenario._Init()"/>
	protected override void _Init()
	{
	}
	
	/// <see cref="Scenario._Run()"/>
	protected override IEnumerator _Run()
	{
		int curspeak = -1;
		int curgaze = -1;
		
		GazeAversionController gactrl = agents[agentName].GetComponent<GazeAversionController>();
		GazeController gazectrl = agents[agentName].GetComponent<GazeController>();
		SpeechController speechCtrl = agents[agentName].GetComponent<SpeechController>();
		
		// Initialize gaze
		yield return new WaitForSeconds(0.6f);
		curgaze = GazeAt(agentName, gactrl.mutualGazeObject, 0.8f, 0f);
		yield return StartCoroutine( WaitUntilFinished(curgaze) );
		curgaze = GazeAt(agentName, gactrl.mutualGazeObject, 0.8f, 0f);
		yield return StartCoroutine( WaitUntilFinished(curgaze) );
		curgaze = GazeAt(agentName, gactrl.mutualGazeObject, 1.0f, 0f);
		yield return StartCoroutine( WaitUntilFinished(curgaze) );
		
		
		int curexpr = ChangeExpression(agentName,"VisemeAh",0.5f,0.1f);
		yield return StartCoroutine( WaitUntilFinished(curexpr) );
		this[agentName].GetComponent<ExpressionController>().FixExpression();
		
		//Question 1
		/*curspeak = Speak (agentName, "disclosure_1");
		yield return StartCoroutine( WaitUntilFinished(curspeak) );
		//Listen to response
		
		yield return new WaitForSeconds(1f);
		yield return StartCoroutine(agentSmileAndEyebrow(2.0));
		yield return new WaitForSeconds(2f);
		HeadNod(agentName,1,FaceGestureSpeed.Slow,3f,0f);*/
	}
	
	IEnumerator agentSmileAndEyebrow(double smileTime) {
		int curexpr = ChangeExpression(agentName,"ExpressionSmileOpen",0.5f,0.1f);
		yield return StartCoroutine( WaitUntilFinished(curexpr) );
		this[agentName].GetComponent<ExpressionController>().FixExpression();
		ChangeExpression(agentName,"ModifierBrowUp",0.6f,0.3f);
		yield return new WaitForSeconds((float)smileTime / 2.0f);
		curexpr = ChangeExpression(agentName,"ModifierBrowUp",0f,0.3f);
		yield return StartCoroutine( WaitUntilFinished(curexpr) );
		this[agentName].GetComponent<ExpressionController>().UnfixExpression("ExpressionSmileOpen");
		yield return new WaitForSeconds((float)smileTime / 2.0f);
		curexpr = ChangeExpression(agentName,"ExpressionSmileOpen",0f,0.5f);
		yield return StartCoroutine( WaitUntilFinished(curexpr) );
	}
	
	/// <see cref="Scenario._Finish()"/>
	protected override void _Finish()
	{
	}
};
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class JasminGazeAversion : Scenario
{
	/// <see cref="Scenario._Init()"/>
	protected override void _Init()
	{
	}
	
	/// <see cref="Scenario._Run()"/>
	protected override IEnumerator _Run()
	{
		int curspeak = -1;
		int curgaze = -1;
		int curlisten = -1;
		int curexpr = -1;
		
		GazeAversionController gactrl = agents["Jasmin"].GetComponent<GazeAversionController>();
		ListenController listenCtrl = agents["Jasmin"].GetComponent<ListenController>();
		// Initialize gaze
		curexpr = ChangeExpression("Jasmin","ModifierEyeSquintRight",0.3f,0.2f);
		yield return StartCoroutine( WaitUntilFinished(curexpr) );
		this["Jasmin"].GetComponent<ExpressionController>().FixExpression();
		curexpr = ChangeExpression("Jasmin","ModifierEyeSquintLeft",0.26f,0.2f);
		yield return StartCoroutine( WaitUntilFinished(curexpr) );
		this["Jasmin"].GetComponent<ExpressionController>().FixExpression();
		
		yield return new WaitForSeconds(0.6f);
		curgaze = GazeAtCamera("Jasmin",0.8f,0f);
		yield return StartCoroutine( WaitUntilFinished(curgaze) );
		curgaze = GazeAtCamera("Jasmin",0.8f,0f);
		yield return StartCoroutine( WaitUntilFinished(curgaze) );
		/*curgaze = GazeAtMutual("Jasmin", GameObject.FindGameObjectsWithTag("MutualGaze")[0], 0.8f, 0f);
		curgazeIvy = GazeAtMutual("Lily", GameObject.FindGameObjectsWithTag("MutualGaze2")[0], 0.8f, 0f);
		yield return StartCoroutine( WaitUntilFinished(curgaze) );
		yield return StartCoroutine( WaitUntilFinished(curgazeIvy) );
		curgaze = GazeAtMutual("Jasmin", GameObject.FindGameObjectsWithTag("MutualGaze")[0], 0.8f, 0f);
		curgazeIvy = GazeAtMutual("Lily", GameObject.FindGameObjectsWithTag("MutualGaze2")[0], 0.8f, 0f);
		yield return StartCoroutine( WaitUntilFinished(curgaze) );
		yield return StartCoroutine( WaitUntilFinished(curgazeIvy) );*/
		
		yield return new WaitForSeconds(3f);
		
		//INTRODUCTION
		gactrl.resetTime();
		curspeak = Speak("Jasmin", "jasmin_1", SpeechType.Other, false);
		curexpr = ChangeExpression("Jasmin","ExpressionSmileOpen",0.5f,0.1f);
		yield return StartCoroutine( WaitUntilFinished(curexpr) );
		this["Jasmin"].GetComponent<ExpressionController>().FixExpression();
		ChangeExpression("Jasmin","ModifierBrowUp",0.6f,0.3f);
		yield return new WaitForSeconds(0.5f);
		curexpr = ChangeExpression("Jasmin","ModifierBrowUp",0f,0.3f);
		yield return StartCoroutine( WaitUntilFinished(curexpr) );
		yield return StartCoroutine( WaitUntilFinished(curspeak) );
		yield return new WaitForSeconds(1f);
		
		curspeak = Speak("Jasmin", "jasmin_2", SpeechType.Other, false);
		yield return new WaitForSeconds(2f);
		ChangeExpression("Jasmin","ModifierBrowUp",0.6f,0.3f);
		yield return new WaitForSeconds(0.5f);
		curexpr = ChangeExpression("Jasmin","ModifierBrowUp",0f,0.3f);
		yield return StartCoroutine( WaitUntilFinished(curexpr) );
		yield return StartCoroutine( WaitUntilFinished(curspeak) );
		yield return new WaitForSeconds(1f);
		
		curspeak = Speak("Jasmin", "jasmin_3", SpeechType.Other, false);
		yield return new WaitForSeconds(2f);
		ChangeExpression("Jasmin","ModifierBrowUp",0.6f,0.3f);
		yield return new WaitForSeconds(0.5f);
		curexpr = ChangeExpression("Jasmin","ModifierBrowUp",0f,0.3f);
		yield return StartCoroutine( WaitUntilFinished(curexpr) );
		yield return StartCoroutine( WaitUntilFinished(curspeak) );
		yield return new WaitForSeconds(1f);
		
		curspeak = Speak("Jasmin", "jasmin_4", SpeechType.Other, false);
		yield return new WaitForSeconds(2f);
		ChangeExpression("Jasmin","ModifierBrowUp",0.6f,0.3f);
		yield return new WaitForSeconds(0.5f);
		curexpr = ChangeExpression("Jasmin","ModifierBrowUp",0f,0.3f);
		yield return StartCoroutine( WaitUntilFinished(curexpr) );
		yield return StartCoroutine( WaitUntilFinished(curspeak) );
		yield return new WaitForSeconds(1f);
		
		curspeak = Speak("Jasmin", "jasmin_5", SpeechType.Other, false);
		yield return new WaitForSeconds(2f);
		ChangeExpression("Jasmin","ModifierBrowUp",0.6f,0.3f);
		yield return new WaitForSeconds(0.5f);
		curexpr = ChangeExpression("Jasmin","ModifierBrowUp",0f,0.3f);
		yield return StartCoroutine( WaitUntilFinished(curexpr) );
		this["Jasmin"].GetComponent<ExpressionController>().UnfixExpression("ExpressionSmileOpen");
		yield return StartCoroutine( WaitUntilFinished(curspeak) );
		curexpr = ChangeExpression("Jasmin","ExpressionSmileOpen",0f,0.5f);
		yield return StartCoroutine( WaitUntilFinished(curexpr) );
		
		//ANSWERING QUESTIONS
		while (listenCtrl.utterancesLeft() > 0) {
			curlisten = Listen("Jasmin");
			yield return StartCoroutine( WaitUntilFinished(curlisten) );
			curspeak = Speak("Jasmin", listenCtrl.response, SpeechType.Answer, true);
			curexpr = ChangeExpression("Jasmin","ExpressionSmileOpen",0.5f,0.2f);
			yield return StartCoroutine( WaitUntilFinished(curexpr) );
			this["Jasmin"].GetComponent<ExpressionController>().FixExpression();
			yield return new WaitForSeconds(3f);
			ChangeExpression("Jasmin","ModifierBrowUp",0.6f,0.3f);
			yield return new WaitForSeconds(0.5f);
			curexpr = ChangeExpression("Jasmin","ModifierBrowUp",0f,0.3f);
			yield return StartCoroutine( WaitUntilFinished(curexpr) );
			this["Jasmin"].GetComponent<ExpressionController>().UnfixExpression("ExpressionSmileOpen");
			yield return StartCoroutine( WaitUntilFinished(curspeak) );
			curexpr = ChangeExpression("Jasmin","ExpressionSmileOpen",0f,0.5f);
			yield return StartCoroutine( WaitUntilFinished(curexpr) );
		}
		
		//TRANSITION
		curspeak = Speak("Jasmin", "jasmin_transition", SpeechType.Other, false);
		curexpr = ChangeExpression("Jasmin","ExpressionSmileOpen",0.5f,0.2f);
		yield return StartCoroutine( WaitUntilFinished(curexpr) );
		this["Jasmin"].GetComponent<ExpressionController>().FixExpression();
		yield return new WaitForSeconds(3f);
		ChangeExpression("Jasmin","ModifierBrowUp",0.6f,0.3f);
		yield return new WaitForSeconds(0.5f);
		curexpr = ChangeExpression("Jasmin","ModifierBrowUp",0f,0.3f);
		yield return StartCoroutine( WaitUntilFinished(curexpr) );
		this["Jasmin"].GetComponent<ExpressionController>().UnfixExpression("ExpressionSmileOpen");
		yield return StartCoroutine( WaitUntilFinished(curspeak) );
		curexpr = ChangeExpression("Jasmin","ExpressionSmileOpen",0f,0.5f);
		yield return StartCoroutine( WaitUntilFinished(curexpr) );
		
		//ASKING QUESTIONS
		//listenCtrl.openListeningMode = true;
		curspeak = Speak("Jasmin", "jasmin_disc1", SpeechType.Question, false);
		curexpr = ChangeExpression("Jasmin","ExpressionSmileOpen",0.5f,0.1f);
		yield return StartCoroutine( WaitUntilFinished(curexpr) );
		this["Jasmin"].GetComponent<ExpressionController>().FixExpression();
		ChangeExpression("Jasmin","ModifierBrowUp",0.6f,0.3f);
		yield return new WaitForSeconds(0.5f);
		curexpr = ChangeExpression("Jasmin","ModifierBrowUp",0f,0.3f);
		yield return StartCoroutine( WaitUntilFinished(curexpr) );
		this["Jasmin"].GetComponent<ExpressionController>().UnfixExpression("ExpressionSmileOpen");
		yield return StartCoroutine( WaitUntilFinished(curspeak) );
		curexpr = ChangeExpression("Jasmin","ExpressionSmileOpen",0f,0.5f);
		yield return StartCoroutine( WaitUntilFinished(curexpr) );
		
		curlisten = Listen("Jasmin");
		yield return StartCoroutine( WaitUntilFinished(curlisten) );
		
		curspeak = Speak("Jasmin", "jasmin_disc2", SpeechType.Question, true);
		curexpr = ChangeExpression("Jasmin","ExpressionSmileOpen",0.5f,0.2f);
		yield return StartCoroutine( WaitUntilFinished(curexpr) );
		this["Jasmin"].GetComponent<ExpressionController>().FixExpression();
		yield return new WaitForSeconds(3f);
		ChangeExpression("Jasmin","ModifierBrowUp",0.6f,0.3f);
		yield return new WaitForSeconds(0.5f);
		curexpr = ChangeExpression("Jasmin","ModifierBrowUp",0f,0.3f);
		yield return StartCoroutine( WaitUntilFinished(curexpr) );
		this["Jasmin"].GetComponent<ExpressionController>().UnfixExpression("ExpressionSmileOpen");
		yield return StartCoroutine( WaitUntilFinished(curspeak) );
		curexpr = ChangeExpression("Jasmin","ExpressionSmileOpen",0f,0.5f);
		yield return StartCoroutine( WaitUntilFinished(curexpr) );
		
		curlisten = Listen("Jasmin");
		yield return StartCoroutine( WaitUntilFinished(curlisten) );
		
		curspeak = Speak("Jasmin", "jasmin_disc3", SpeechType.Question, true);
		curexpr = ChangeExpression("Jasmin","ExpressionSmileOpen",0.5f,0.2f);
		yield return StartCoroutine( WaitUntilFinished(curexpr) );
		this["Jasmin"].GetComponent<ExpressionController>().FixExpression();
		yield return new WaitForSeconds(3f);
		ChangeExpression("Jasmin","ModifierBrowUp",0.6f,0.3f);
		yield return new WaitForSeconds(0.5f);
		curexpr = ChangeExpression("Jasmin","ModifierBrowUp",0f,0.3f);
		yield return StartCoroutine( WaitUntilFinished(curexpr) );
		this["Jasmin"].GetComponent<ExpressionController>().UnfixExpression("ExpressionSmileOpen");
		yield return StartCoroutine( WaitUntilFinished(curspeak) );
		curexpr = ChangeExpression("Jasmin","ExpressionSmileOpen",0f,0.5f);
		yield return StartCoroutine( WaitUntilFinished(curexpr) );
		
		curlisten = Listen("Jasmin");
		yield return StartCoroutine( WaitUntilFinished(curlisten) );
		
		curspeak = Speak("Jasmin", "jasmin_disc4", SpeechType.Question, true);
		curexpr = ChangeExpression("Jasmin","ExpressionSmileOpen",0.5f,0.2f);
		yield return StartCoroutine( WaitUntilFinished(curexpr) );
		this["Jasmin"].GetComponent<ExpressionController>().FixExpression();
		yield return new WaitForSeconds(3f);
		ChangeExpression("Jasmin","ModifierBrowUp",0.6f,0.3f);
		yield return new WaitForSeconds(0.5f);
		curexpr = ChangeExpression("Jasmin","ModifierBrowUp",0f,0.3f);
		yield return StartCoroutine( WaitUntilFinished(curexpr) );
		this["Jasmin"].GetComponent<ExpressionController>().UnfixExpression("ExpressionSmileOpen");
		yield return StartCoroutine( WaitUntilFinished(curspeak) );
		curexpr = ChangeExpression("Jasmin","ExpressionSmileOpen",0f,0.5f);
		yield return StartCoroutine( WaitUntilFinished(curexpr) );
		
		curlisten = Listen("Jasmin");
		yield return StartCoroutine( WaitUntilFinished(curlisten) );
		
		curspeak = Speak("Jasmin", "jasmin_disc5", SpeechType.Question, true);
		curexpr = ChangeExpression("Jasmin","ExpressionSmileOpen",0.5f,0.2f);
		yield return StartCoroutine( WaitUntilFinished(curexpr) );
		this["Jasmin"].GetComponent<ExpressionController>().FixExpression();
		yield return new WaitForSeconds(0.5f);
		ChangeExpression("Jasmin","ModifierBrowUp",0.6f,0.3f);
		yield return new WaitForSeconds(0.5f);
		curexpr = ChangeExpression("Jasmin","ModifierBrowUp",0f,0.3f);
		yield return StartCoroutine( WaitUntilFinished(curexpr) );
		this["Jasmin"].GetComponent<ExpressionController>().UnfixExpression("ExpressionSmileOpen");
		yield return StartCoroutine( WaitUntilFinished(curspeak) );
		curexpr = ChangeExpression("Jasmin","ExpressionSmileOpen",0f,0.5f);
		yield return StartCoroutine( WaitUntilFinished(curexpr) );
		
		curlisten = Listen("Jasmin");
		yield return StartCoroutine( WaitUntilFinished(curlisten) );
		
		//OUTRO
		curspeak = Speak("Jasmin", "jasmin_end1", SpeechType.Other, false);
		curexpr = ChangeExpression("Jasmin","ExpressionSmileOpen",0.5f,0.1f);
		yield return StartCoroutine( WaitUntilFinished(curexpr) );
		this["Jasmin"].GetComponent<ExpressionController>().FixExpression();
		ChangeExpression("Jasmin","ModifierBrowUp",0.6f,0.3f);
		yield return new WaitForSeconds(0.5f);
		curexpr = ChangeExpression("Jasmin","ModifierBrowUp",0f,0.3f);
		yield return StartCoroutine( WaitUntilFinished(curexpr) );
		this["Jasmin"].GetComponent<ExpressionController>().UnfixExpression("ExpressionSmileOpen");
		yield return StartCoroutine( WaitUntilFinished(curspeak) );
		curspeak = Speak("Jasmin", "jasmin_end2", SpeechType.Other, false);
		yield return StartCoroutine( WaitUntilFinished(curspeak) );
		curexpr = ChangeExpression("Jasmin","ExpressionSmileOpen",0f,0.5f);
		yield return StartCoroutine( WaitUntilFinished(curexpr) );
		
		
		
		/*
		curspeak = Speak("Jasmin", "hmo", SpeechType.Answer, true);
		curexpr = ChangeExpression("Jasmin","ExpressionSmileOpen",0.5f,0.2f);
		yield return StartCoroutine( WaitUntilFinished(curexpr) );
		this["Jasmin"].GetComponent<ExpressionController>().FixExpression();
		yield return new WaitForSeconds(3f);
		ChangeExpression("Jasmin","ModifierBrowUp",0.6f,0.3f);
		yield return new WaitForSeconds(0.5f);
		ChangeExpression("Jasmin","ModifierBrowUp",0f,0.3f);
		yield return StartCoroutine( WaitUntilFinished(curspeak) );
		this["Jasmin"].GetComponent<ExpressionController>().UnfixExpression("ExpressionSmileOpen");
		ChangeExpression("Jasmin","ExpressionSmileOpen",0f,0.5f);
		
		yield return new WaitForSeconds(5f);
		
		curspeak = Speak("Jasmin", "healthinsuranceclaim", SpeechType.Answer, true);
		curexpr = ChangeExpression("Jasmin","ExpressionSmileOpen",0.5f,0.2f);
		yield return StartCoroutine( WaitUntilFinished(curexpr) );
		this["Jasmin"].GetComponent<ExpressionController>().FixExpression();
		yield return new WaitForSeconds(3f);
		ChangeExpression("Jasmin","ModifierBrowUp",0.6f,0.3f);
		yield return new WaitForSeconds(0.5f);
		ChangeExpression("Jasmin","ModifierBrowUp",0f,0.3f);
		yield return StartCoroutine( WaitUntilFinished(curspeak) );
		this["Jasmin"].GetComponent<ExpressionController>().UnfixExpression("ExpressionSmileOpen");
		ChangeExpression("Jasmin","ExpressionSmileOpen",0f,0.5f);
		
		yield return new WaitForSeconds(5f);
		
		curspeak = Speak("Jasmin", "feeforservice", SpeechType.Answer, true);
		curexpr = ChangeExpression("Jasmin","ExpressionSmileOpen",0.5f,0.2f);
		yield return StartCoroutine( WaitUntilFinished(curexpr) );
		this["Jasmin"].GetComponent<ExpressionController>().FixExpression();
		yield return new WaitForSeconds(3f);
		ChangeExpression("Jasmin","ModifierBrowUp",0.6f,0.3f);
		yield return new WaitForSeconds(0.5f);
		ChangeExpression("Jasmin","ModifierBrowUp",0f,0.3f);
		yield return StartCoroutine( WaitUntilFinished(curspeak) );
		this["Jasmin"].GetComponent<ExpressionController>().UnfixExpression("ExpressionSmileOpen");
		ChangeExpression("Jasmin","ExpressionSmileOpen",0f,0.5f);
		
		yield return new WaitForSeconds(5f);
		
		curspeak = Speak("Jasmin", "coinsurance", SpeechType.Answer, true);
		curexpr = ChangeExpression("Jasmin","ExpressionSmileOpen",0.5f,0.2f);
		yield return StartCoroutine( WaitUntilFinished(curexpr) );
		this["Jasmin"].GetComponent<ExpressionController>().FixExpression();
		yield return new WaitForSeconds(3f);
		ChangeExpression("Jasmin","ModifierBrowUp",0.6f,0.3f);
		yield return new WaitForSeconds(0.5f);
		ChangeExpression("Jasmin","ModifierBrowUp",0f,0.3f);
		yield return StartCoroutine( WaitUntilFinished(curspeak) );
		this["Jasmin"].GetComponent<ExpressionController>().UnfixExpression("ExpressionSmileOpen");
		ChangeExpression("Jasmin","ExpressionSmileOpen",0f,0.5f);
		
		yield return new WaitForSeconds(5f);
		
		curspeak = Speak("Jasmin", "outofpocket", SpeechType.Answer, true);
		curexpr = ChangeExpression("Jasmin","ExpressionSmileOpen",0.5f,0.2f);
		yield return StartCoroutine( WaitUntilFinished(curexpr) );
		this["Jasmin"].GetComponent<ExpressionController>().FixExpression();
		yield return new WaitForSeconds(3f);
		ChangeExpression("Jasmin","ModifierBrowUp",0.6f,0.3f);
		yield return new WaitForSeconds(0.5f);
		ChangeExpression("Jasmin","ModifierBrowUp",0f,0.3f);
		yield return StartCoroutine( WaitUntilFinished(curspeak) );
		this["Jasmin"].GetComponent<ExpressionController>().UnfixExpression("ExpressionSmileOpen");
		ChangeExpression("Jasmin","ExpressionSmileOpen",0f,0.5f);
		
		yield return new WaitForSeconds(5f);
		
		curspeak = Speak("Jasmin", "coverage", SpeechType.Answer, true);
		curexpr = ChangeExpression("Jasmin","ExpressionSmileOpen",0.5f,0.2f);
		yield return StartCoroutine( WaitUntilFinished(curexpr) );
		this["Jasmin"].GetComponent<ExpressionController>().FixExpression();
		yield return new WaitForSeconds(3f);
		ChangeExpression("Jasmin","ModifierBrowUp",0.6f,0.3f);
		yield return new WaitForSeconds(0.5f);
		ChangeExpression("Jasmin","ModifierBrowUp",0f,0.3f);
		yield return StartCoroutine( WaitUntilFinished(curspeak) );
		this["Jasmin"].GetComponent<ExpressionController>().UnfixExpression("ExpressionSmileOpen");
		ChangeExpression("Jasmin","ExpressionSmileOpen",0f,0.5f);*/
		
		//=========================================================
		
		/*curspeak = Speak("Jasmin", "prototype_1", SpeechType.Question);
		curexpr = ChangeExpression("Jasmin","ExpressionSmileOpen",0.8f,0.2f);
		yield return StartCoroutine( WaitUntilFinished(curexpr) );
		this["Jasmin"].GetComponent<ExpressionController>().FixExpression();
		ChangeExpression("Jasmin","ModifierBrowUp",0.6f,0.3f);
		yield return new WaitForSeconds(0.5f);
		ChangeExpression("Jasmin","ModifierBrowUp",0f,0.3f);
		yield return StartCoroutine( WaitUntilFinished(curspeak) );
		this["Jasmin"].GetComponent<ExpressionController>().UnfixExpression("ExpressionSmileOpen");
		ChangeExpression("Jasmin","ExpressionSmileOpen",0f,0.5f);
		
		yield return new WaitForSeconds(2f);
		
		curspeak = Speak("Jasmin", "prototype_2", SpeechType.Question, true);
		yield return new WaitForSeconds(0.1f);
		yield return new WaitForSeconds((float)gactrl.cogStartTime - 0.1f);
		curexpr = ChangeExpression("Jasmin","ExpressionSmileOpen",0.6f,1.5f);
		yield return StartCoroutine( WaitUntilFinished(curspeak) );
		ChangeExpression("Jasmin","ExpressionSmileOpen",0f,0.5f);
		
		yield return new WaitForSeconds(4f);
		HeadNod("Jasmin",1,FaceGestureSpeed.Normal,3f,0f);
		yield return new WaitForSeconds(4f);
		
		curspeak = Speak("Jasmin", "prototype_3", SpeechType.Answer, true);
		yield return new WaitForSeconds(4f);
		ChangeExpression("Jasmin","ModifierBrowUp",0.6f,0.3f);
		yield return new WaitForSeconds(0.5f);
		ChangeExpression("Jasmin","ModifierBrowUp",0f,0.3f);
		yield return new WaitForSeconds(2f);
		ChangeExpression("Jasmin","ModifierBrowUp",0.6f,0.3f);
		yield return new WaitForSeconds(0.5f);
		ChangeExpression("Jasmin","ModifierBrowUp",0f,0.3f);
		yield return StartCoroutine( WaitUntilFinished(curspeak) );
		
		yield return new WaitForSeconds(4f);
		
		curspeak = Speak("Jasmin", "prototype_4", SpeechType.Answer, true);
		yield return new WaitForSeconds(2f);
		ChangeExpression("Jasmin","ModifierBrowUp",0.8f,0.3f);
		yield return new WaitForSeconds(0.5f);
		ChangeExpression("Jasmin","ModifierBrowUp",0f,0.3f);
		yield return new WaitForSeconds(2f);
		ChangeExpression("Jasmin","ModifierBrowUp",0.6f,0.3f);
		yield return new WaitForSeconds(0.5f);
		ChangeExpression("Jasmin","ModifierBrowUp",0f,0.3f);
		HeadShake("Jasmin",1,FaceGestureSpeed.Normal,6f,1f);
		yield return new WaitForSeconds(1f);
		ChangeExpression("Jasmin","ModifierBrowUp",0.6f,0.3f);
		yield return new WaitForSeconds(0.5f);
		ChangeExpression("Jasmin","ModifierBrowUp",0f,0.3f);
		yield return new WaitForSeconds(1f);
		HeadShake("Jasmin",1,FaceGestureSpeed.Normal,6f,1f);
		yield return StartCoroutine( WaitUntilFinished(curspeak) );
		HeadNod("Jasmin",2,FaceGestureSpeed.Normal,3f,0f);
		
		yield return new WaitForSeconds(2.9f);*/
	}
	
	/// <see cref="Scenario._Finish()"/>
	protected override void _Finish()
	{
	}
};
using UnityEngine;
using System.Collections;

public class JasminStoryScenario : StoryScenario
{
	/// <see cref="Scenario._Run()"/>
	protected override IEnumerator _Run()
	{
		ExpressionController exprCtrl = agents[agentName].GetComponent<ExpressionController>();
		BlinkController blinkCtrl = agents[agentName].GetComponent<BlinkController>();
		int curspeak = -1;
		int curgaze = -1;
		int curexpr = -1;
		float pitch = agents[agentName].audio.pitch;
		
		yield return StartCoroutine(base._Run());
		
		curexpr = ChangeExpression(agentName,"ExpressionSmileClosed",1f,0f);
		yield return StartCoroutine( WaitUntilFinished(curexpr) );
		exprCtrl.FixExpression();
		
		//phMosaic.gameObject.SetActiveRecursively(false);
		Time.timeScale = 0.2f;
		
		yield return new WaitForSeconds(Time.timeScale);
		
		// PP -> PPC
		SetGazePathArcs(agentName,0f,0f,0f);
		//curgaze = GazeAt(agentName,"GTTestL",1f,0f,0f,0f,0.4f);
        // TODO: bring this back when you bring back stylized gaze
		//curgaze = GazeAtCamera(agentName,0f,0f,0f,0f,1f);
		//blinkCtrl.Blink(0.65f,0.8f,1f);
		yield return StartCoroutine( WaitUntilFinished(curgaze) );
		yield return new WaitForSeconds(Time.timeScale);
		
		/*// Gaze at horse
		SetGazePathArcs(agentName,6f,6f,4f);
		curgaze = GazeAt(agentName,"PPStoryHorse",0f,0f,0.2f,60f,0.4f);
		ChangeExpression(agentName,"ExpressionSmileClosed",1f,0.38f);
		yield return new WaitForSeconds(0.07f);
		PostureShift(agentName,"PostureGazeRight",0.48f,1f);
		yield return StartCoroutine( WaitUntilFinished(curgaze) );
		//
		yield return new WaitForSeconds(0.3f);
		// Gaze from horse to snake
		PostureShift(agentName,"PostureGazeRightNeutral",0.48f,1f);
		yield return new WaitForSeconds(0.12f);
		SetGazePathArcs(agentName,4f,4f,4f);
		curgaze = GazeAt(agentName,"PPStorySnake",0f,0f,0.1f,60f,0.9f);
		ChangeExpression(agentName,"ExpressionFear",0.5f,0.38f);
		yield return StartCoroutine( WaitUntilFinished(curgaze) );
		//
		yield return new WaitForSeconds(0.3f);
		// Gaze back from snake
		SetGazePathArcs(agentName,4f,4f,4f);
		curgaze = base.GazeAtCamera(agentName,0.8f,0f,0.2f,45f,1f);
		ChangeExpression(agentName,"ExpressionSmileClosed",1f,0.38f);
		yield return StartCoroutine( WaitUntilFinished(curgaze) );
		//*/
		
		yield return 0;
		
		/*
		// Begin intro
		ChangeExpression(agentName,"ModifierBrowUp",0.7f,0.2f);
		yield return new WaitForSeconds(0.1f);
		curspeak = Speak(agentName,"Story1");
		yield return new WaitForSeconds(0.2f);
		ChangeExpression(agentName,"ModifierBrowUp",0f,0.2f);
		yield return new WaitForSeconds(0.4f);
		HeadNod(agentName,1,FaceGestureSpeed.Normal,4f,0f);
		yield return StartCoroutine( WaitUntilFinished(curspeak) );
		yield return new WaitForSeconds(0.8f);
		curspeak = Speak(agentName,"Story2");
		yield return new WaitForSeconds(1.9f);
		ChangeExpression(agentName,"ModifierBrowUp",0.7f,0.3f);
		yield return new WaitForSeconds(0.5f);
		ChangeExpression(agentName,"ModifierBrowUp",0f,0.3f);
		yield return StartCoroutine( WaitUntilFinished(curspeak) );
		yield return new WaitForSeconds(0.4f);
		curspeak = Speak(agentName,"Story3");
		
		// Gaze at rabbit
		SetGazePathArcs(agentName,6f,6f,6f);
		curgaze = GazeAt(agentName,"PPStoryRabbit",0f,0f,0.1f,40f,0.3f);
		exprCtrl.UnfixExpression("ExpressionSmileClosed");
		ChangeExpression(agentName,"ExpressionSmileOpen",1f,0.38f);
		yield return StartCoroutine( WaitUntilFinished(curgaze) );
		//
		yield return new WaitForSeconds(0.3f);
		// Gaze back from rabbit
		SetGazePathArcs(agentName,4f,4f,4f);
		curgaze = base.GazeAtCamera(agentName,0.4f,0f,0.2f,50f,1f);
		ChangeExpression(agentName,"ExpressionSmileClosed",1f,0.38f);
		yield return StartCoroutine( WaitUntilFinished(curgaze) );
		//
		
		// Finish intro
		yield return StartCoroutine( WaitUntilFinished(curspeak) );
		yield return new WaitForSeconds(0.4f);
		curspeak = Speak(agentName,"Story4");
		yield return new WaitForSeconds(1f);
		exprCtrl.FixExpression();
		ChangeExpression(agentName,"ModifierBrowUp",0.7f,0.3f);
		yield return new WaitForSeconds(0.5f);
		ChangeExpression(agentName,"ModifierBrowUp",0f,0.3f);
		yield return StartCoroutine( WaitUntilFinished(curspeak) );
		yield return new WaitForSeconds(0.8f);
		curspeak = Speak(agentName,"Story5");
		yield return StartCoroutine( WaitUntilFinished(curspeak) );
		yield return new WaitForSeconds(0.4f);
		curspeak = Speak(agentName,"Story6");
		yield return StartCoroutine( WaitUntilFinished(curspeak) );
		yield return new WaitForSeconds(0.8f);
		
		// Jade emperor
		curspeak = Speak(agentName,"Story7");
		ChangeExpression(agentName,"ModifierBrowUp",0.7f,0.4f);
		yield return new WaitForSeconds(0.7f/pitch);
		ChangeExpression(agentName,"ModifierBrowUp",0f,0.6f);
		
		// Gaze at rabbit
		SetGazePathArcs(agentName,6f,6f,6f);
		curgaze = GazeAt(agentName,"PPStoryRabbit",0f,0f,0.1f,40f,0.2f);
		exprCtrl.UnfixExpression("ExpressionSmileClosed");
		ChangeExpression(agentName,"ExpressionSmileOpen",1f,0.38f);
		yield return StartCoroutine( WaitUntilFinished(curgaze) );
		//
		yield return new WaitForSeconds(0.3f);
		// Gaze back from rabbit
		SetGazePathArcs(agentName,4f,4f,4f);
		curgaze = base.GazeAtCamera(agentName,0.4f,0f,0.2f,50f,1f);
		ChangeExpression(agentName,"ExpressionSmileClosed",1f,0.38f);
		yield return StartCoroutine( WaitUntilFinished(curgaze) );
		//
		
		// Continue Jade Emperor
		yield return StartCoroutine( WaitUntilFinished(curspeak) );
		yield return new WaitForSeconds(0.4f);
		curspeak = Speak(agentName,"Story8");
		yield return new WaitForSeconds(2.6f);
		exprCtrl.FixExpression();
		ChangeExpression(agentName,"ModifierBrowUp",0.7f,0.3f);
		yield return new WaitForSeconds(0.4f);
		ChangeExpression(agentName,"ModifierBrowUp",0f,0.3f);
		yield return StartCoroutine( WaitUntilFinished(curspeak) );
		yield return new WaitForSeconds(0.4f);
		curspeak = Speak(agentName,"Story9");
		
		// Gaze at rabbit
		SetGazePathArcs(agentName,6f,6f,6f);
		curgaze = GazeAt(agentName,"PPStoryRabbit",0f,0f,0.1f,40f,0.2f);
		exprCtrl.UnfixExpression("ExpressionSmileClosed");
		ChangeExpression(agentName,"ExpressionSmileOpen",1f,0.38f);
		yield return StartCoroutine( WaitUntilFinished(curgaze) );
		//
		yield return new WaitForSeconds(0.3f);
		// Gaze back from rabbit
		SetGazePathArcs(agentName,4f,4f,4f);
		curgaze = base.GazeAtCamera(agentName,0.4f,0f,0.2f,50f,1f);
		ChangeExpression(agentName,"ExpressionSmileClosed",1f,0.38f);
		yield return StartCoroutine( WaitUntilFinished(curgaze) );
		//
		
		// Finish Jade Emperor
		yield return StartCoroutine( WaitUntilFinished(curspeak) );
		yield return new WaitForSeconds(0.4f);
		yield return new WaitForSeconds(0.4f);
		*/
		
		
		
		
		
		
		
		
		
		/*
		
		
		yield return new WaitForSeconds(1f);
		
		// Gaze at rat
		SetGazePathArcs(agentName,8f,8f,8f);
		curgaze = GazeAt(agentName,"PPStoryRat",0f,0f,0.1f,60f,0.4f);
		ChangeExpression(agentName,"ExpressionDisgust",1f,0.38f);
		yield return new WaitForSeconds(0.07f);
		PostureShift(agentName,"PostureGazeRight",0.48f,1f);
		yield return StartCoroutine( WaitUntilFinished(curgaze) );
		//
		yield return new WaitForSeconds(0.3f);
		// Gaze back from rat
		PostureShift(agentName,"PostureGazeRightNeutral",0.48f,1f);
		yield return new WaitForSeconds(0.07f);
		SetGazePathArcs(agentName,8f,8f,8f);
		curgaze = base.GazeAtCamera(agentName,0f,0f,0.1f,60f,1f);
		ChangeExpression(agentName,"ExpressionSmileClosed",1f,0.38f);
		yield return StartCoroutine( WaitUntilFinished(curgaze) );
		//
		
		yield return new WaitForSeconds(1f);
		
		// Gaze at ox
		SetGazePathArcs(agentName,8f,8f,6f);
		curgaze = GazeAt(agentName,"PPStoryOx",0f,0f,0f,0f,0.5f);
		ChangeExpression(agentName,"ExpressionSmileClosed",1f,0.38f);
		yield return StartCoroutine( WaitUntilFinished(curgaze) );
		//
		yield return new WaitForSeconds(0.3f);
		// Gaze back from ox
		SetGazePathArcs(agentName,8f,8f,4f);
		curgaze = base.GazeAtCamera(agentName,0.4f,0f,0f,0f,1f);
		ChangeExpression(agentName,"ExpressionSmileOpen",1f,0.38f);
		yield return StartCoroutine( WaitUntilFinished(curgaze) );
		//
		
		yield return new WaitForSeconds(1f);
		
		// Gaze at tiger
		SetGazePathArcs(agentName,12f,12f,6f);
		curgaze = GazeAt(agentName,"PPStoryTiger",0f,0f,0f,0f,0.5f);
		ChangeExpression(agentName,"ExpressionSmileClosed",1f,0.38f);
		yield return StartCoroutine( WaitUntilFinished(curgaze) );
		//
		yield return new WaitForSeconds(0.3f);
		// Gaze back from tiger
		SetGazePathArcs(agentName,4f,4f,4f);
		curgaze = base.GazeAtCamera(agentName,0.2f,0f,0.2f,45f,1f);
		ChangeExpression(agentName,"ExpressionSmileOpen",1f,0.38f);
		yield return StartCoroutine( WaitUntilFinished(curgaze) );
		//
		
		yield return new WaitForSeconds(1f);
		
		// Gaze at rabbit
		SetGazePathArcs(agentName,6f,6f,6f);
		curgaze = GazeAt(agentName,"PPStoryRabbit",0f,0f,0.4f,40f,0.4f);
		ChangeExpression(agentName,"ExpressionSmileClosed",1f,0.38f);
		yield return StartCoroutine( WaitUntilFinished(curgaze) );
		//
		yield return new WaitForSeconds(0.3f);
		// Gaze back from rabbit
		SetGazePathArcs(agentName,4f,4f,4f);
		curgaze = base.GazeAtCamera(agentName,0f,0f,0.2f,50f,1f);
		ChangeExpression(agentName,"ExpressionSmileOpen",1f,0.38f);
		yield return StartCoroutine( WaitUntilFinished(curgaze) );
		//
		
		yield return new WaitForSeconds(0.3f);
		
		// Gaze fully at camera
		curgaze = base.GazeAtCamera(agentName,1f,0f,0f,0f,1f);
		//
		
		yield return new WaitForSeconds(1f);
		
		// Gaze at dragon
		SetGazePathArcs(agentName,8f,8f,8f);
		curgaze = GazeAt(agentName,"PPStoryDragon",0f,0f,0.1f,60f,0.4f);
		ChangeExpression(agentName,"ExpressionSmileClosed",1f,0.38f);
		yield return StartCoroutine( WaitUntilFinished(curgaze) );
		//
		yield return new WaitForSeconds(0.3f);
		// Gaze back from dragon
		SetGazePathArcs(agentName,18f,18f,8f);
		curgaze = base.GazeAtCamera(agentName,0.5f,0f,0f,0f,1f);
		ChangeExpression(agentName,"ExpressionSmileOpen",1f,0.38f);
		yield return StartCoroutine( WaitUntilFinished(curgaze) );
		//
		
		yield return new WaitForSeconds(0.3f);
		
		// Gaze fully at camera
		curgaze = base.GazeAtCamera(agentName,1f,0f,0f,0f,1f);
		//
		
		yield return new WaitForSeconds(1f);
		
		// Gaze at horse
		SetGazePathArcs(agentName,6f,6f,4f);
		curgaze = GazeAt(agentName,"PPStoryHorse",0f,0f,0.2f,60f,0.4f);
		ChangeExpression(agentName,"ExpressionSmileClosed",1f,0.38f);
		yield return new WaitForSeconds(0.07f);
		PostureShift(agentName,"PostureGazeRight",0.48f,1f);
		yield return StartCoroutine( WaitUntilFinished(curgaze) );
		//
		yield return new WaitForSeconds(0.3f);
		// Gaze from horse to snake
		PostureShift(agentName,"PostureGazeRightNeutral",0.48f,1f);
		yield return new WaitForSeconds(0.12f);
		SetGazePathArcs(agentName,4f,4f,4f);
		curgaze = GazeAt(agentName,"PPStorySnake",0f,0f,0.1f,60f,0.9f);
		ChangeExpression(agentName,"ExpressionFear",0.5f,0.38f);
		yield return StartCoroutine( WaitUntilFinished(curgaze) );
		//
		yield return new WaitForSeconds(0.3f);
		// Gaze back from snake
		SetGazePathArcs(agentName,4f,4f,4f);
		curgaze = base.GazeAtCamera(agentName,0.8f,0f,0.2f,45f,1f);
		ChangeExpression(agentName,"ExpressionSmileClosed",1f,0.38f);
		yield return StartCoroutine( WaitUntilFinished(curgaze) );
		//
		
		yield return new WaitForSeconds(1f);
		
		// Gaze at horse
		SetGazePathArcs(agentName,6f,6f,4f);
		curgaze = GazeAt(agentName,"PPStoryHorse",0f,0f,0.2f,60f,0.4f);
		ChangeExpression(agentName,"ExpressionSmileOpen",1f,0.38f);
		yield return StartCoroutine( WaitUntilFinished(curgaze) );
		//
		yield return new WaitForSeconds(0.3f);
		// Gaze back from horse
		SetGazePathArcs(agentName,4f,4f,4f);
		curgaze = base.GazeAtCamera(agentName,0f,0f,0f,0f,1f);
		ChangeExpression(agentName,"ExpressionSmileOpen",1f,0.38f);
		yield return StartCoroutine( WaitUntilFinished(curgaze) );
		//
		
		yield return new WaitForSeconds(1f);
		
		// Gaze at goat
		SetGazePathArcs(agentName,8f,8f,8f);
		curgaze = GazeAt(agentName,"PPStoryGoat",0f,0f,0.2f,60f,0.4f);
		ChangeExpression(agentName,"ExpressionSmileClosed",1f,0.38f);
		yield return StartCoroutine( WaitUntilFinished(curgaze) );
		//
		yield return new WaitForSeconds(0.3f);
		//
		// Gaze at monkey
		SetGazePathArcs(agentName,8f,8f,8f);
		curgaze = GazeAt(agentName,"PPStoryMonkey",0.3f,0f,0.2f,60f,0.4f);
		ChangeExpression(agentName,"ExpressionSmileClosed",1f,0.38f);
		yield return StartCoroutine( WaitUntilFinished(curgaze) );
		//
		yield return new WaitForSeconds(0.3f);
		// Gaze at rooster
		SetGazePathArcs(agentName,8f,8f,8f);
		curgaze = GazeAt(agentName,"PPStoryRooster",0.6f,0f,0.2f,60f,0.4f);
		ChangeExpression(agentName,"ExpressionSmileClosed",1f,0.38f);
		yield return StartCoroutine( WaitUntilFinished(curgaze) );
		//
		yield return new WaitForSeconds(0.3f);
		//
		// Gaze back from rooster
		SetGazePathArcs(agentName,4f,4f,4f);
		curgaze = base.GazeAtCamera(agentName,0f,0f,0f,0f,1f);
		ChangeExpression(agentName,"ExpressionSmileOpen",1f,0.38f);
		yield return StartCoroutine( WaitUntilFinished(curgaze) );
		//
		
		yield return new WaitForSeconds(0.3f);
		
		// Gaze fully at camera
		curgaze = base.GazeAtCamera(agentName,1f,0f,0f,0f,1f);
		//
		
		yield return new WaitForSeconds(1f);
		
		// Gaze at dog
		SetGazePathArcs(agentName,4f,4f,4f);
		curgaze = GazeAt(agentName,"PPStoryDog",0f,0f,0.2f,60f,0.4f);
		ChangeExpression(agentName,"ExpressionSmileOpen",1f,0.38f);
		yield return StartCoroutine( WaitUntilFinished(curgaze) );
		//
		yield return new WaitForSeconds(0.3f);
		// Gaze back from dog
		SetGazePathArcs(agentName,4f,4f,4f);
		curgaze = base.GazeAtCamera(agentName,0.4f,0f,0.2f,60f,1f);
		ChangeExpression(agentName,"ExpressionSmileClosed",1f,0.38f);
		yield return StartCoroutine( WaitUntilFinished(curgaze) );
		//
		
		yield return new WaitForSeconds(1f);
		
		// Gaze at pig
		SetGazePathArcs(agentName,6f,6f,4f);
		curgaze = GazeAt(agentName,"PPStoryPig",0f,0f,0.2f,60f,0.4f);
		ChangeExpression(agentName,"ExpressionSmileClosed",1f,0.38f);
		yield return new WaitForSeconds(0.07f);
		PostureShift(agentName,"PostureGazeRight",0.48f,1f);
		yield return StartCoroutine( WaitUntilFinished(curgaze) );
		//
		yield return new WaitForSeconds(0.3f);
		// Gaze back from pig
		PostureShift(agentName,"PostureGazeRightNeutral",0.48f,1f);
		yield return new WaitForSeconds(0.07f);
		SetGazePathArcs(agentName,4f,4f,4f);
		curgaze = base.GazeAtCamera(agentName,0.6f,0f,0.2f,45f,1f);
		ChangeExpression(agentName,"ExpressionSmileOpen",1f,0.38f);
		yield return StartCoroutine( WaitUntilFinished(curgaze) );
		//
		
		yield return new WaitForSeconds(0.3f);
		
		// Gaze fully at camera
		curgaze = base.GazeAtCamera(agentName,0.7f,0f,0.3f,20f,1f);
		//
		
		//yield return new WaitForSeconds(0.12f);
		//yield return new WaitForSeconds(1.3f);
		//
		/*
		
		
		
		
		 
		// Rat and Ox
		curspeak = Speak(agentName,"Story10");
		// Gaze at photo
		yield return new WaitForSeconds(1.1f/pitch);
		SetGazePathArcs(agentName,8f,8f,40f);
		curgaze = GazeAtPhoto(agentName,"PPStoryRat",0.3f,0f);
		//
		// Gaze at viewer
		yield return StartCoroutine( WaitUntilFinished(curgaze) );
		yield return new WaitForSeconds(0.4f);
		curgaze = GazeAt(agentName,"GTEyeContact",0.6f,0f);
		//
		yield return new WaitForSeconds(1.3f/pitch);
		ChangeExpression(agentName,"ModifierBrowUp",0.7f,0.3f);
		yield return new WaitForSeconds(0.5f/pitch);
		ChangeExpression(agentName,"ModifierBrowUp",0f,0.3f);
		// Gaze at photo
		yield return new WaitForSeconds(3.3f/pitch);
		SetGazePathArcs(agentName,6f,6f,50f);
		curgaze = GazeAtPhoto(agentName,"PPStoryOx",0.3f,0f);
		//
		// Gaze at viewer
		yield return StartCoroutine( WaitUntilFinished(curgaze) );
		yield return new WaitForSeconds(0.4f);
		curgaze = GazeAt(agentName,"GTEyeContact",0.75f,0f);
		//
		yield return StartCoroutine( WaitUntilFinished(curspeak) );
		yield return new WaitForSeconds(0.4f);
		curspeak = Speak(agentName,"Story11");
		yield return new WaitForSeconds(2.4f);
		HeadNod(agentName,1,FaceGestureSpeed.Normal,4f,0f);
		yield return StartCoroutine( WaitUntilFinished(curspeak) );
		yield return new WaitForSeconds(0.4f);
		curspeak = Speak(agentName,"Story12");
		// Gaze at photo
		yield return new WaitForSeconds(2.1f/pitch);
		SetGazePathArcs(agentName,12f,12f,60f);
		curgaze = GazeAtPhoto(agentName,"PPStoryRat",0.3f,0f);
		//
		// Gaze at viewer
		yield return StartCoroutine( WaitUntilFinished(curgaze) );
		yield return new WaitForSeconds(0.4f);
		curgaze = GazeAt(agentName,"GTEyeContact",0.7f,0f);
		//
		yield return StartCoroutine( WaitUntilFinished(curspeak) );
		yield return new WaitForSeconds(0.4f);
		ChangeExpression(agentName,"ModifierBrowUp",0.7f,0.3f);
		yield return new WaitForSeconds(0.2f);
		curspeak = Speak(agentName,"Story13");
		yield return new WaitForSeconds(0.3f);
		ChangeExpression(agentName,"ModifierBrowUp",0f,0.3f);
		yield return StartCoroutine( WaitUntilFinished(curspeak) );
		yield return new WaitForSeconds(0.4f);
		curspeak = Speak(agentName,"Story14");
		// Gaze at photo
		yield return new WaitForSeconds(1.6f/pitch);
		SetGazePathArcs(agentName,10f,10f,50f);
		curgaze = GazeAtPhoto(agentName,"PPStoryOx",0.3f,0f);
		//
		// Gaze at viewer
		yield return StartCoroutine( WaitUntilFinished(curgaze) );
		yield return new WaitForSeconds(0.4f);
		curgaze = GazeAt(agentName,"GTEyeContact",0.9f,0f);
		//
		yield return StartCoroutine( WaitUntilFinished(curspeak) );
		yield return new WaitForSeconds(0.8f);
		
		// Tiger
		curspeak = Speak(agentName,"Story15");
		// Gaze at photo
		yield return new WaitForSeconds(1.4f/pitch);
		SetGazePathArcs(agentName,8f,8f,45f);
		curgaze = GazeAtPhoto(agentName,"PPStoryTiger",0.3f,0f);
		//
		// Gaze at viewer
		yield return StartCoroutine( WaitUntilFinished(curgaze) );
		yield return new WaitForSeconds(0.4f);
		curgaze = GazeAt(agentName,"GTEyeContact",0.9f,0f);
		//
		yield return StartCoroutine( WaitUntilFinished(curspeak) );
		yield return new WaitForSeconds(0.8f);
		
		// Rabbit
		curspeak = Speak(agentName,"Story16");
		// Gaze at photo
		yield return new WaitForSeconds(2.5f/pitch);
		SetGazePathArcs(agentName,12f,12f,70f);
		curgaze = GazeAtPhoto(agentName,"PPStoryTiger",0.3f,0f);
		//
		// Gaze at viewer
		yield return StartCoroutine( WaitUntilFinished(curgaze) );
		yield return new WaitForSeconds(0.4f);
		curgaze = GazeAt(agentName,"GTEyeContact",0.9f,0f);
		//
		yield return StartCoroutine( WaitUntilFinished(curspeak) );
		yield return new WaitForSeconds(0.4f);
		curspeak = Speak(agentName,"Story17");
		yield return new WaitForSeconds(1.4f);
		ChangeExpression(agentName,"ModifierBrowUp",0.7f,0.4f);
		yield return new WaitForSeconds(0.6f);
		ChangeExpression(agentName,"ModifierBrowUp",0f,0.4f);
		yield return StartCoroutine( WaitUntilFinished(curspeak) );
		yield return new WaitForSeconds(0.8f);
		
		// Dragon
		curspeak = Speak(agentName,"Story18");
		// Gaze at photo
		yield return new WaitForSeconds(1.2f/pitch);
		curgaze = GazeAtPhoto(agentName,"PPStoryDragon",0.3f,0f);
		//
		// Gaze at viewer
		yield return StartCoroutine( WaitUntilFinished(curgaze) );
		yield return new WaitForSeconds(0.4f);
		curgaze = GazeAt(agentName,"GTEyeContact",0.7f,0f);
		//
		yield return StartCoroutine( WaitUntilFinished(curspeak) );
		yield return new WaitForSeconds(0.4f);
		curspeak = Speak(agentName,"Story19");
		yield return new WaitForSeconds(2f);
		ChangeExpression(agentName,"ModifierBrowUp",0.7f,0.4f);
		yield return new WaitForSeconds(0.6f);
		ChangeExpression(agentName,"ModifierBrowUp",0f,0.4f);
		yield return StartCoroutine( WaitUntilFinished(curspeak) );
		yield return new WaitForSeconds(0.2f);
		// Gaze at photo
		curgaze = GazeAtPhoto(agentName,"PPStoryDragon",0.3f,0f);
		yield return new WaitForSeconds(0.2f);
		//
		curspeak = Speak(agentName,"Story20");
		// Gaze at viewer
		yield return new WaitForSeconds(0.8f);
		curgaze = GazeAt(agentName,"GTEyeContact",0.8f,0f);
		//
		yield return StartCoroutine( WaitUntilFinished(curspeak) );
		yield return new WaitForSeconds(0.4f);
		
		// Horse and Snake
		curspeak = Speak(agentName,"Story21");
		// Gaze at photo
		yield return new WaitForSeconds(0.8f/pitch);
		SetGazePathArcs(agentName,12f,12f,60f);
		curgaze = GazeAtPhoto(agentName,"PPStoryHorse",0.3f,0f);
		//
		yield return StartCoroutine( WaitUntilFinished(curspeak) );
		yield return new WaitForSeconds(0.3f);
		curspeak = Speak(agentName,"Story22");
		// Gaze at photo
		yield return new WaitForSeconds(1.3f/pitch);
		curgaze = GazeAtPhoto(agentName,"PPStorySnake",0.3f,0f);
		//
		// Gaze at viewer
		yield return StartCoroutine( WaitUntilFinished(curgaze) );
		yield return new WaitForSeconds(0.4f);
		curgaze = GazeAt(agentName,"GTEyeContact",0.7f,0f);
		//
		yield return StartCoroutine( WaitUntilFinished(curspeak) );
		yield return new WaitForSeconds(0.05f);
		curspeak = Speak(agentName,"Story23");
		yield return new WaitForSeconds(1.8f);
		ChangeExpression(agentName,"ModifierBrowUp",0.7f,0.3f);
		yield return new WaitForSeconds(0.3f);
		ChangeExpression(agentName,"ModifierBrowUp",0f,0.3f);
		// Gaze at photo
		yield return new WaitForSeconds(3f/pitch);
		SetGazePathArcs(agentName,7f,7f,40f);
		curgaze = GazeAtPhoto(agentName,"PPStoryHorse",0.3f,0f);
		//
		// Gaze at viewer
		yield return StartCoroutine( WaitUntilFinished(curgaze) );
		yield return new WaitForSeconds(0.4f);
		curgaze = GazeAt(agentName,"GTEyeContact",0.9f,0f);
		//
		yield return StartCoroutine( WaitUntilFinished(curspeak) );
		curspeak = Speak(agentName,"Story24");
		yield return StartCoroutine( WaitUntilFinished(curspeak) );
		yield return new WaitForSeconds(0.4f);
		//phMosaic.MoveTo("KF3",0.8f);
		yield return new WaitForSeconds(0.4f);
		
		// Goat, Monkey and Rooster
		curspeak = Speak(agentName,"Story25");
		// Gaze at photo
		yield return new WaitForSeconds(2.5f/pitch);
		SetGazePathArcs(agentName,16f,16f,70f);
		curgaze = GazeAtPhoto(agentName,"PPStoryGoat",0.3f,0f);
		yield return StartCoroutine( WaitUntilFinished(curgaze) );
		curgaze = GazeAtPhoto(agentName,"PPStoryMonkey",0f,0f);
		yield return StartCoroutine( WaitUntilFinished(curgaze) );
		curgaze = GazeAtPhoto(agentName,"PPStoryRooster",0f,0f);
		//
		// Gaze at viewer
		yield return StartCoroutine( WaitUntilFinished(curgaze) );
		yield return new WaitForSeconds(0.4f);
		curgaze = GazeAt(agentName,"GTEyeContact",0.75f,0f);
		//
		yield return StartCoroutine( WaitUntilFinished(curspeak) );
		yield return new WaitForSeconds(0.4f);
		curspeak = Speak(agentName,"Story26");
		yield return new WaitForSeconds(1.2f);
		ChangeExpression(agentName,"ModifierBrowUp",0.7f,0.3f);
		yield return new WaitForSeconds(0.5f);
		ChangeExpression(agentName,"ModifierBrowUp",0f,0.3f);
		yield return new WaitForSeconds(8.7f);
		ChangeExpression(agentName,"ModifierBrowUp",0.7f,0.3f);
		yield return new WaitForSeconds(0.4f);
		ChangeExpression(agentName,"ModifierBrowUp",0f,0.3f);
		yield return StartCoroutine( WaitUntilFinished(curspeak) );
		yield return new WaitForSeconds(0.4f);
		curspeak = Speak(agentName,"Story27");
		// Gaze at photo
		yield return new WaitForSeconds(4.1f/pitch);
		SetGazePathArcs(agentName,8f,8f,50f);
		curgaze = GazeAtPhoto(agentName,"PPStoryGoat",0.3f,0f);
		yield return new WaitForSeconds(1.9f/pitch);
		curgaze = GazeAtPhoto(agentName,"PPStoryMonkey",0.3f,0f);
		yield return new WaitForSeconds(1.6f/pitch);
		curgaze = GazeAtPhoto(agentName,"PPStoryRooster",0.3f,0f);
		//
		// Gaze at viewer
		yield return StartCoroutine( WaitUntilFinished(curgaze) );
		yield return new WaitForSeconds(0.4f);
		curgaze = GazeAt(agentName,"GTEyeContact",0.85f,0f);
		//
		yield return StartCoroutine( WaitUntilFinished(curspeak) );
		yield return new WaitForSeconds(0.8f);

		// Dog
		curspeak = Speak(agentName,"Story28");
		// Gaze at photo
		yield return new WaitForSeconds(0.7f/pitch);
		SetGazePathArcs(agentName,14f,14f,65f);
		curgaze = GazeAtPhoto(agentName,"PPStoryDog",0.3f,0f);
		//
		yield return StartCoroutine( WaitUntilFinished(curspeak) );
		yield return new WaitForSeconds(0.4f);
		curspeak = Speak(agentName,"Story29");
		// Gaze at viewer
		yield return new WaitForSeconds(0.2f);
		curgaze = GazeAt(agentName,"GTEyeContact",0.85f,0f);
		//
		yield return new WaitForSeconds(6f);
		ChangeExpression(agentName,"ModifierBrowUp",0.7f,0.2f);
		yield return new WaitForSeconds(0.3f);
		ChangeExpression(agentName,"ModifierBrowUp",0f,0.2f);
		yield return StartCoroutine( WaitUntilFinished(curspeak) );
		yield return new WaitForSeconds(0.4f);
		
		// Pig
		curspeak = Speak(agentName,"Story30");
		// Gaze at photo
		yield return new WaitForSeconds(2.0f/pitch);
		SetGazePathArcs(agentName,6f,6f,40f);
		curgaze = GazeAtPhoto(agentName,"PPStoryPig",0.3f,0f);
		//
		// Gaze at viewer
		yield return StartCoroutine( WaitUntilFinished(curgaze) );
		yield return new WaitForSeconds(0.4f);
		curgaze = GazeAt(agentName,"GTEyeContact",0.9f,0f);
		//
		yield return StartCoroutine( WaitUntilFinished(curspeak) );
		yield return new WaitForSeconds(0.4f);
		curspeak = Speak(agentName,"Story31");
		yield return new WaitForSeconds(2.2f);
		ChangeExpression(agentName,"ModifierBrowUp",0.7f,0.4f);
		yield return new WaitForSeconds(0.5f);
		ChangeExpression(agentName,"ModifierBrowUp",0f,0.4f);
		yield return StartCoroutine( WaitUntilFinished(curspeak) );
		yield return new WaitForSeconds(0.8f);
		
		// End
		curspeak = Speak(agentName,"Story32");
		ChangeExpression(agentName,"ModifierBrowUp",0.7f,0.25f);
		yield return new WaitForSeconds(0.2f);
		ChangeExpression(agentName,"ModifierBrowUp",0f,0.25f);
		// Gaze at photo
		SetGazePathArcs(agentName,16f,16f,70f);
		curgaze = GazeAtPhoto(agentName,"PPStoryRabbit",0.15f,0f);
		//
		// Gaze at viewer
		yield return StartCoroutine( WaitUntilFinished(curgaze) );
		yield return new WaitForSeconds(0.4f);
		curgaze = GazeAt(agentName,"GTEyeContact",1f,0f);
		//
		yield return StartCoroutine( WaitUntilFinished(curspeak) );
		yield return new WaitForSeconds(0.4f);
		curspeak = Speak(agentName,"Story37");
		yield return StartCoroutine( WaitUntilFinished(curspeak) );
		yield return new WaitForSeconds(0.2f);
		this[agentName].GetComponent<ExpressionController>().UnfixExpression("ExpressionSmileOpen");
		ChangeExpression(agentName,"ExpressionSmileOpen",0.8f,0.4f);
		yield return new WaitForSeconds(0.3f);
		curspeak = Speak(agentName,"Story38");
		HeadNod(agentName,1,FaceGestureSpeed.Normal,4f,0f);
		yield return new WaitForSeconds(0.4f);
		ChangeExpression(agentName,"ExpressionSmileOpen",0.3f,0.3f);
		yield return StartCoroutine( WaitUntilFinished(curspeak) );
		yield return new WaitForSeconds(0.4f);*/
	}
}

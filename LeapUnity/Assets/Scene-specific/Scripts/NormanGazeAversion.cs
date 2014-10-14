using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class NormanGazeAversion : Scenario
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
		
		GazeAversionController gactrl = agents["Norman"].GetComponent<GazeAversionController>();
		GazeController gazectrl = agents["Norman"].GetComponent<GazeController>();
		
		// Initialize gaze
		yield return new WaitForSeconds(0.6f);
		curgaze = GazeAtCamera("Norman",0.8f,0f);
		yield return StartCoroutine( WaitUntilFinished(curgaze) );
		curgaze = GazeAtCamera("Norman",0.8f,0f);
		yield return StartCoroutine( WaitUntilFinished(curgaze) );
		
		yield return new WaitForSeconds(2f);
		
		//INTRODUCTION
		gactrl.resetTime();
		
		curspeak = Speak("Norman", "question1", SpeechType.Question, true);
		yield return StartCoroutine( WaitUntilFinished(curspeak) );
		
		/*curgaze = GazeAt ("Norman", "GazeLeft");
		yield return StartCoroutine(WaitUntilFinished(curgaze));
		curgaze = GazeAt ("Norman", "GazeRight");
		yield return StartCoroutine(WaitUntilFinished(curgaze));
		GazeAt ("Norman", "GazeUpLeft");
		*/
	}
	
	/// <see cref="Scenario._Finish()"/>
	protected override void _Finish()
	{
	}
};
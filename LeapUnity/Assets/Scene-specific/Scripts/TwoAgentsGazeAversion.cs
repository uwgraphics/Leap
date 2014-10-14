using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class TwoAgentsGazeAversion : Scenario
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
		
		int curspeakOther = -1;
		int curgazeOther = -1;
		
		GazeAversionController gactrl = agents["Norman"].GetComponent<GazeAversionController>();
		GazeAversionController gactrlNorman2 = agents["Norman2"].GetComponent<GazeAversionController>();
		
		// Initialize gaze
		yield return new WaitForSeconds(0.6f);
		curgaze = GazeAt("Norman", gactrl.mutualGazeObject, 0.8f, 0f);
		curgazeOther = GazeAt("Norman2", gactrlNorman2.mutualGazeObject, 0.8f, 0f);
		yield return StartCoroutine( WaitUntilFinished(curgaze) );
		yield return StartCoroutine( WaitUntilFinished(curgazeOther) );
		curgaze = GazeAt("Norman", gactrl.mutualGazeObject, 0.8f, 0f);
		curgazeOther = GazeAt("Norman2", gactrlNorman2.mutualGazeObject, 0.8f, 0f);
		yield return StartCoroutine( WaitUntilFinished(curgaze) );
		yield return StartCoroutine( WaitUntilFinished(curgazeOther) );
		curgaze = GazeAt("Norman", gactrl.mutualGazeObject, 1.0f, 0f);
		curgazeOther = GazeAt("Norman2", gactrlNorman2.mutualGazeObject, 1.0f, 0f);
		yield return StartCoroutine( WaitUntilFinished(curgaze) );
		yield return StartCoroutine( WaitUntilFinished(curgazeOther) );
		
		yield return new WaitForSeconds(3f);
		
		//INTRODUCTION
		gactrl.resetTime();
		gactrlNorman2.resetTime();
		curspeak = Speak("Norman", "question1", SpeechType.Question, false);
		yield return StartCoroutine( WaitUntilFinished(curspeak) );
		
		curspeakOther = Speak ("Norman2", "answer1", SpeechType.Answer, true);
		yield return StartCoroutine( WaitUntilFinished(curspeakOther) );
		
		curspeak = Speak("Norman", "question2", SpeechType.Question, true);
		yield return StartCoroutine( WaitUntilFinished(curspeak) );
		
		curspeakOther = Speak ("Norman2", "answer2", SpeechType.Answer, true);
		yield return StartCoroutine( WaitUntilFinished(curspeakOther) );
		
		curspeak = Speak("Norman", "question3", SpeechType.Question, false);
		yield return StartCoroutine( WaitUntilFinished(curspeak) );
		
		curspeakOther = Speak ("Norman2", "answer3", SpeechType.Answer, true);
		yield return StartCoroutine( WaitUntilFinished(curspeakOther) );
		
		curspeak = Speak("Norman", "question4", SpeechType.Question, true);
		yield return StartCoroutine( WaitUntilFinished(curspeak) );
		
		curspeakOther = Speak ("Norman2", "answer4", SpeechType.Answer, false);
		yield return StartCoroutine( WaitUntilFinished(curspeakOther) );
		
		curspeak = Speak("Norman", "question5", SpeechType.Question, false);
		yield return StartCoroutine( WaitUntilFinished(curspeak) );
		
		curspeakOther = Speak ("Norman2", "answer5", SpeechType.Answer, true);
		yield return StartCoroutine( WaitUntilFinished(curspeakOther) );
	}
	
	/// <see cref="Scenario._Finish()"/>
	protected override void _Finish()
	{
	}
};
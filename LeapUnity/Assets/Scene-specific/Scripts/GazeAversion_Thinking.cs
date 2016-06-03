using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System;

public class GazeAversion_Thinking : Scenario
{
	
	//Keeping track of the pause
	private float timeElapsed = 0f;
	private float timeToWait = 7f;
	public bool captureVideo = false;
	VideoCapture vidcap;
	
	/// <see cref="Scenario._Init()"/>
    protected override void _Init()
	{
		vidcap = GetComponent<VideoCapture>();
		vidcap.enabled = false;
	}
	
	private void resetTime() {
		timeElapsed = 0f;	
	}
	
	/// <see cref="Scenario._Run()"/>
	protected override IEnumerator _Run()
	{
		int curspeak = -1;
		int curgaze = -1;
		int curlisten = -1;
		string agentName = "Norman";
		
		GazeAversionController gactrl = agents[agentName].GetComponent<GazeAversionController>();
		GazeController gazectrl = agents[agentName].GetComponent<GazeController>();
		ListenController listenCtrl = agents[agentName].GetComponent<ListenController>();
		SpeechController speechCtrl = agents[agentName].GetComponent<SpeechController>();
		
		string timestamp = DateTime.Now.ToString("yyyy_MM_dd_HH-mm-ss_") + "Thinking_Norman_" + gactrl.condition.ToString();
		
		GameObject[] lights = GameObject.FindGameObjectsWithTag("Spotlight");
		float[] lightIntensities = new float[lights.Length];
		for (int i = 0; i < lights.Length; ++i) {
			lightIntensities[i] = lights[i].GetComponent<Light>().intensity;
			lights[i].GetComponent<Light>().intensity = 0f;	
		}
		
		// Initialize gaze
		yield return new WaitForSeconds(0.6f);
		if (gactrl.condition != GazeAversionCondition.BadModel) {
			curgaze = GazeAt(agentName, gactrl.mutualGazeObject, 0.8f);
			yield return StartCoroutine( WaitUntilFinished(curgaze) );
			curgaze = GazeAt(agentName, gactrl.mutualGazeObject, 0.8f);
			yield return StartCoroutine( WaitUntilFinished(curgaze) );
			curgaze = GazeAt(agentName, gactrl.mutualGazeObject, 1.0f);
			yield return StartCoroutine( WaitUntilFinished(curgaze) );
		}
		else {
			curgaze = GazeAt(agentName, GameObject.Find ("GazeLeft"), 0.8f);
			yield return StartCoroutine( WaitUntilFinished(curgaze) );
			curgaze = GazeAt(agentName, GameObject.Find ("GazeLeft"), 0.8f);
			yield return StartCoroutine( WaitUntilFinished(curgaze) );
			curgaze = GazeAt(agentName, GameObject.Find ("GazeLeft"), 1.0f);
			yield return StartCoroutine( WaitUntilFinished(curgaze) );
		}
		
		GameObject.Find ("Shield").SetActive(false);
		
		for (int i = 0; i < lights.Length; ++i) {
			lights[i].GetComponent<Light>().intensity = lightIntensities[i];	
		}
		
		//SCREENSHOT CODE
		
		if (captureVideo) {
			vidcap.enabled = true;
			vidcap.Start();
		}
			
		yield return new WaitForSeconds(6f);
		gactrl.triggerManualGazeAversion(6.0f, GazeAversionTarget.Up);
		yield return new WaitForSeconds(6f);
		curspeak = Speak(agentName, "thinking_1", SpeechType.Answer, false);
		curgaze = GazeAt(agentName, gactrl.mutualGazeObject, 1.0f);
		yield return StartCoroutine( WaitUntilFinished(curgaze) );
		yield return StartCoroutine( WaitUntilFinished(curspeak) );
		vidcap.enabled = false;
		
		///Mouse click starts the scenario
		/*while (!ListenController.mouseClicked) {
			yield return 0;	
		}
		listenCtrl.StartLogTiming();
		gactrl.StartLogTiming();
		speechCtrl.StartLogTiming();
		ListenController.mouseClicked = false;
		gactrl.resetTime();
		
		while (listenCtrl.utterancesLeft() > 0) {
			//Listen for question
			curlisten = Listen(agentName);
			yield return StartCoroutine( WaitUntilFinished(curlisten) );
			
			//Save response and pause (gaze away)
			string response = listenCtrl.response;
			
			//Listen during this pause
			curlisten = OpenListen (agentName);
			while (listenCtrl.StateId == (int)ListenState.NotListening) {
				yield return 0;
			}
			gactrl.triggerManualGazeAversion(9.0f, GazeAversionTarget.None);
			resetTime();
			
			while( listenCtrl.StateId != (int)ListenState.OpenHearingSpeech && timeElapsed <= timeToWait)
			{
				yield return 0;
			}
			yield return new WaitForSeconds(2.0f);
			//Save time elapsed
			CancelAction(curlisten);
			curgaze = GazeAt(agentName, gactrl.mutualGazeObject, 1.0f, 0f);
			curspeak = Speak(agentName, response, SpeechType.Answer, false);
			yield return StartCoroutine(WaitUntilFinished(curspeak));
		}
		
		using (StreamWriter outfile = new StreamWriter(Application.dataPath + @"\" + timestamp + ".txt")) {
			outfile.Write (sb.ToString());
		}
		
		foreach (GameObject g in lights) {
			g.light.intensity = 0f;	
		}
		GameObject.Find ("FinalInstructions").guiText.enabled = true;*/
		
	}
	
	void Update() {
		timeElapsed += Time.deltaTime;	
	}
	
	/// <see cref="Scenario._Finish()"/>
	protected override void _Finish()
	{
	}
};
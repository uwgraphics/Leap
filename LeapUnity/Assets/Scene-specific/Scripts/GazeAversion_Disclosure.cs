using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MathNet.Numerics;
using MathNet.Numerics.Distributions;
using MathNet.Numerics.RandomSources;

public class GazeAversion_Disclosure : Scenario
{
	
	private string agentName = "Lily";
	private ListenController listenCtrl;
	
	//Random distributions for smiling and nodding while listening
	private MersenneTwisterRandomSource randNumGen = null; //random number generator for seeding the distributions below
	private NormalDistribution nextSmileDistribution = null;
	private NormalDistribution smileLengthDistribution = null;
	private NormalDistribution nextNodDistribution = null;
	private double nextSmileCounter = 0;
	private double nextNodCounter = 0;
	private double nextSmileTime = 0;
	private double nextNodTime = 0;
	public bool captureVideo = false;
	VideoCapture vidcap;
	
	/// <see cref="Scenario._Init()"/>
    protected override void _Init()
	{
		randNumGen = new MersenneTwisterRandomSource();
		nextSmileDistribution = new NormalDistribution(randNumGen);
		nextSmileDistribution.SetDistributionParameters(20.0, 5.0);
		smileLengthDistribution = new NormalDistribution(randNumGen);
		smileLengthDistribution.SetDistributionParameters(5.0, 1.0);
		nextNodDistribution = new NormalDistribution(randNumGen);
		nextNodDistribution.SetDistributionParameters(10.0, 3.0);
		
		nextSmileTime = nextSmileDistribution.NextDouble();
		nextNodTime = nextNodDistribution.NextDouble();
		
		vidcap = GetComponent<VideoCapture>();
		vidcap.enabled = false;
	}
	
	/// <see cref="Scenario._Run()"/>
	protected override IEnumerator _Run()
	{
		int curspeak = -1;
		int curgaze = -1;
		int curlisten = -1;
		
		GazeAversionController gactrl = agents[agentName].GetComponent<GazeAversionController>();
		GazeController gazectrl = agents[agentName].GetComponent<GazeController>();
		listenCtrl = agents[agentName].GetComponent<ListenController>();
		SpeechController speechCtrl = agents[agentName].GetComponent<SpeechController>();
		
		string timestamp = DateTime.Now.ToString("yyyy_MM_dd_HH-mm-ss_") + "Disclosure_Lily_" + gactrl.condition.ToString();
		
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
		
		for (int i = 0; i < lights.Length; ++i) {
			lights[i].GetComponent<Light>().intensity = lightIntensities[i];	
		}
		
		GameObject.Find ("Shield").SetActive(false);
		
		//SCREENSHOT CODE
		if (captureVideo) {
			vidcap.enabled = true;
			vidcap.Start();
		}
			
		yield return new WaitForSeconds(1f);
		curspeak = Speak(agentName, "disclosure_1", SpeechType.Question, true);
		yield return StartCoroutine(WaitUntilFinished(curspeak));
		yield return new WaitForSeconds(15f);
		vidcap.enabled = false;
		
		//Mouse click starts the scenario
		/*while (!ListenController.mouseClicked) {
			yield return 0;	
		}
		listenCtrl.StartLogTiming();
		gactrl.StartLogTiming();
		speechCtrl.StartLogTiming();
		ListenController.mouseClicked = false;
		gactrl.resetTime();
		
		//Question 1
		curspeak = Speak(agentName, "disclosure_1", SpeechType.Question, true);
		yield return StartCoroutine( WaitUntilFinished(curspeak) );
		//Listen to response
		curlisten = OpenListen(agentName);
		ListenController.mouseClicked = false;
		while (!ListenController.mouseClicked) {
			yield return 0;
		}
		CancelAction(curlisten);
		
		//Question 2
		curspeak = Speak(agentName, "disclosure_2", SpeechType.Question, false);
		yield return StartCoroutine( WaitUntilFinished(curspeak) );
		//Listen to response
		curlisten = OpenListen(agentName);
		ListenController.mouseClicked = false;
		while (!ListenController.mouseClicked) {
			yield return 0;
		}
		CancelAction(curlisten);
		
		//Question 3
		curspeak = Speak(agentName, "disclosure_3", SpeechType.Question, true);
		yield return StartCoroutine( WaitUntilFinished(curspeak) );
		//Listen to response
		curlisten = OpenListen(agentName);
		ListenController.mouseClicked = false;
		while (!ListenController.mouseClicked) {
			yield return 0;
		}
		CancelAction(curlisten);
		
		//Question 4
		curspeak = Speak(agentName, "disclosure_4", SpeechType.Question, true);
		yield return StartCoroutine( WaitUntilFinished(curspeak) );
		//Listen to response
		curlisten = OpenListen(agentName);
		ListenController.mouseClicked = false;
		while (!ListenController.mouseClicked) {
			yield return 0;
		}
		CancelAction(curlisten);
		
		//Question 5
		curspeak = Speak(agentName, "disclosure_5", SpeechType.Question, true);
		yield return StartCoroutine( WaitUntilFinished(curspeak) );
		//Listen to response
		curlisten = OpenListen(agentName);
		ListenController.mouseClicked = false;
		while (!ListenController.mouseClicked) {
			yield return 0;
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
		nextSmileCounter += Time.deltaTime;
		if (nextSmileCounter >= nextSmileTime) {
			nextSmileCounter = 0;
			nextSmileTime = nextSmileDistribution.NextDouble();
			StartCoroutine(agentSmileAndEyebrow(smileLengthDistribution.NextDouble()));
		}
		
		if (listenCtrl.StateId != (int)ListenState.NotListening) {
			nextNodCounter += Time.deltaTime;
			if (nextNodCounter >= nextNodTime) {
				nextNodCounter = 0f;
				nextNodTime = nextNodDistribution.NextDouble();
				HeadNod(agentName,1,FaceGestureSpeed.Slow,3f,0f);
			}
		}
	}
	
	IEnumerator agentSmileAndEyebrow(double smileTime) {
		int curexpr = ChangeExpression(agentName,"ExpressionSmileOpen",0.5f,0.1f);
		yield return StartCoroutine( WaitUntilFinished(curexpr) );
		ChangeExpression(agentName,"ModifierBrowUp",0.6f,0.3f);
		yield return new WaitForSeconds((float)smileTime / 2.0f);
		curexpr = ChangeExpression(agentName,"ModifierBrowUp",0f,0.3f);
		yield return StartCoroutine( WaitUntilFinished(curexpr) );
		yield return new WaitForSeconds((float)smileTime / 2.0f);
		curexpr = ChangeExpression(agentName,"ExpressionSmileOpen",0f,0.5f);
		yield return StartCoroutine( WaitUntilFinished(curexpr) );
	}
	
	/// <see cref="Scenario._Finish()"/>
	protected override void _Finish()
	{
	}
};
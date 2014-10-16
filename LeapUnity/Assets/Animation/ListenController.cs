using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Text;

public enum ListenState
{
	NotListening,
	Listening,
	HearingAudio,
	HearingSpeech,
	OpenListening,
	OpenHearingAudio,
	OpenHearingSpeech
};

public class ListenController : AnimController
{
	private String[] acceptableUtterances;
	private String[] responses;
	private bool[] completed;
	private static bool speechRejected = false;
	private static bool speechRecognized = false;
	private static bool speechHypothesized = false;
	private static bool audioSpike = false;
	private static String recognizedSpeech;
	private static double recognizedConfidence;
	public TextAsset speechInputs;
	public TextAsset speechResponses;
	public bool startListening = false;
	public bool startOpenListening = false;
	public bool stopListening = false;
	private double confidenceThreshold = 0.3;
	[HideInInspector]
	public String response;
	public static bool mouseClicked = false;
	
	private GameObject audioGui = null;
	private GameObject readyText = null;
	public float audioTimeOut = 0.2f;
	private float audioTime = 0f;
	public string KinectArgument = null;
	
	protected override void _Init()
	{	
		if (isEnabled) {
			//Set up the kinect speech application
			Process speechProcess = new Process();
			speechProcess.StartInfo.FileName = "C:\\Users\\Sean\\Documents\\Kinect\\Speech\\Bin\\Debug\\Speech.exe";
			if (KinectArgument != null) {
				speechProcess.StartInfo.Arguments = " " + KinectArgument;	
			}
			speechProcess.StartInfo.UseShellExecute = false;
			speechProcess.StartInfo.RedirectStandardOutput = true;
			speechProcess.OutputDataReceived += new DataReceivedEventHandler(SpeechOutputHandler);
			speechProcess.StartInfo.RedirectStandardInput = true;
			try
			{
				speechProcess.Start();
			}
			catch(Exception)
			{
				return;
			}
			speechProcess.BeginOutputReadLine();
			
			//Parse the file of "acceptable utterances"
			if (speechInputs != null && speechResponses != null) {
				char[] newLines = {'\n'};
				acceptableUtterances = speechInputs.text.Split(newLines);
				responses = speechResponses.text.Split(newLines);
				completed = new bool[responses.Length];
				for (int i = 0; i < completed.Length; ++i) {
					completed[i] = false;	
				}
			}
			
			audioGui = GameObject.Find ("Audio_Gui_Red");
			readyText = GameObject.Find ("ReadyText");
		}
	}
	
	//This handler receives output from the kinect speech application and parses it
	private static void SpeechOutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
    {
		speechRejected = false;
		speechRecognized = false;
		speechHypothesized = false;
		audioSpike = false;
		mouseClicked = false;
		
        // Collect the speech output.
        if (!String.IsNullOrEmpty(outLine.Data))
        {
			//Kinect is ready
			if (outLine.Data.Equals("Recognizing speech. Press ENTER to stop")) {
				UnityEngine.Debug.Log("Recognizing Speech!");
			}
			//Recognized speech
			Match match = Regex.Match(outLine.Data, @"Speech Recognized\:\s*([\s\S]*)\s*Confidence\:\s*([0-9]\.[0-9]*)", RegexOptions.IgnoreCase);
			if (match.Success)
			{
				recognizedSpeech = match.Groups[1].ToString();
				recognizedConfidence = Double.Parse(match.Groups[2].ToString());
				speechRecognized = true;
				UnityEngine.Debug.Log("Recognized Speech: " + recognizedSpeech + " || Confidence: " + recognizedConfidence);
				return;
			}
			//Hypothesized speech
			match = Regex.Match(outLine.Data, @"Speech Hypothesized\:\s*([\s\S]*)", RegexOptions.IgnoreCase);
			if (match.Success) {
				speechHypothesized = true;
				UnityEngine.Debug.Log("Hypothesized Speech: " + match.Groups[1]);
				return;
			}
			//Rejected speech
			match = Regex.Match(outLine.Data, @"Speech Rejected", RegexOptions.IgnoreCase);
			if (match.Success) {
				UnityEngine.Debug.Log("Speech was rejected");
				speechRejected = true;
				return;
			}
			//A spike in audio
			match = Regex.Match(outLine.Data, @"Loudness\:\s*([0-9]*\.[0-9]*)", RegexOptions.IgnoreCase);
			if (match.Success) {
				audioSpike = true;
				UnityEngine.Debug.Log("Audio Spike! " + match.Groups[1]);
			}
			//Device warming up
			match = Regex.Match(outLine.Data, @"Device will be ready for speech recognition in ([0-9]) second\(s\)\.\s*", RegexOptions.IgnoreCase);
			if (match.Success) {
				UnityEngine.Debug.Log("Preparing device..." + match.Groups[1]);
				return;
			}
			//Mouse click event
			match = Regex.Match(outLine.Data, @"Click", RegexOptions.IgnoreCase);
			if (match.Success) {
				UnityEngine.Debug.Log("Mouse clicked");
				mouseClicked = true;
				return;
			}
        }
    }
	
	//Match what has just been said to the list of acceptable utterances
	private String matchSpeech()
	{
		for (int i = 0; i < acceptableUtterances.Length; ++i) {
			String possibleUtterance = acceptableUtterances[i];
			if (String.Equals(possibleUtterance.Trim(), ListenController.recognizedSpeech.Trim())) {
				if (ListenController.recognizedConfidence >= confidenceThreshold) {
					completed[i] = true;
					return responses[i].Trim();
				}
				else {
					return null;	
				}
			}
		}
		return null;
	}
	
	protected override void _Update()
	{
		//Keep confidence threshold between 0 and 1
		if (confidenceThreshold <= 0)
			confidenceThreshold = 0;
		if (confidenceThreshold >= 1.0)
			confidenceThreshold = 1.0;
	}
	
	//Listening
	protected virtual void Update_Listening()
	{	
		readyText.guiText.enabled = true;
		audioGui.guiTexture.enabled = false;
		
		if (stopListening)
			GoToState((int)ListenState.NotListening);
		else if (ListenController.audioSpike)
			GoToState((int)ListenState.HearingAudio);
		else if (ListenController.speechHypothesized)
			GoToState((int)ListenState.HearingSpeech);
		//readyText.guiText.text = "Listening";
	}
	
	//Hearing Audio
	protected virtual void Update_HearingAudio()
	{
		readyText.guiText.enabled = true;
		audioGui.guiTexture.enabled = true;
		
		if (stopListening)
			GoToState((int)ListenState.NotListening);
		else if (ListenController.speechHypothesized)
			GoToState((int)ListenState.HearingSpeech);
		
		audioTime += Time.deltaTime;
		if (audioTime >= audioTimeOut) {
			GoToState((int)ListenState.Listening);
		}
		//readyText.guiText.text = "Hearing Audio";
	}
	
	//Hearing Speech
	protected virtual void Update_HearingSpeech()
	{
		readyText.guiText.enabled = false;
		audioGui.guiTexture.enabled = true;
		
		if (ListenController.speechRecognized) {
			response = matchSpeech();
			if (response != null)
				GoToState((int)ListenState.NotListening);
			else
				GoToState((int)ListenState.Listening);
		}
		else if (ListenController.speechRejected) {
			GoToState((int)ListenState.Listening);
		}
		//readyText.guiText.text = "Hearing Speech";
	}
	
	//Not Listening
	protected virtual void Update_NotListening()
	{
		if( readyText == null || audioGui == null )
			return;
		
		readyText.guiText.enabled = false;
		audioGui.guiTexture.enabled = false;
		
		if (startListening) {
			GoToState((int)ListenState.Listening);
		}
		else if (startOpenListening) {
			GoToState((int)ListenState.OpenListening);	
		}
		//readyText.guiText.text = "Not Listening";
	}
	
	//Open Listening
	protected virtual void Update_OpenListening()
	{
		readyText.guiText.enabled = false;
		audioGui.guiTexture.enabled = false;
		
		if (stopListening) {
			GoToState((int)ListenState.NotListening);
		}
		else if (ListenController.audioSpike) {
			GoToState((int)ListenState.OpenHearingAudio);	
		}
		else if (ListenController.speechHypothesized) {
			GoToState((int)ListenState.OpenHearingSpeech);
		}
		//readyText.guiText.text = "Open Listening";
	}
	
	//Open Hearing Audio
	protected virtual void Update_OpenHearingAudio()
	{
		readyText.guiText.enabled = false;
		audioGui.guiTexture.enabled = true;
		
		if (stopListening) {
			GoToState((int)ListenState.NotListening);
		}
		else if (ListenController.speechHypothesized) {
			GoToState((int)ListenState.OpenHearingSpeech);
		}
		
		audioTime += Time.deltaTime;
		if (audioTime >= audioTimeOut) {
			GoToState((int)ListenState.OpenListening);
		}
		//readyText.guiText.text = "Open Hearing Audio";
	}
	
	//Open Hearing Speech
	protected virtual void Update_OpenHearingSpeech()
	{
		readyText.guiText.enabled = false;
		
		if (ListenController.speechHypothesized) {
			audioGui.guiTexture.enabled = true;
		}
		else if (ListenController.speechRejected || ListenController.speechRecognized) {
			audioGui.guiTexture.enabled = false;
		}
		
		if (stopListening) {
			GoToState((int)ListenState.NotListening);
		}
		//readyText.guiText.text = "Open Hearing Speech";
	}
	
	//Listening -> Not Listening
	protected virtual void Transition_ListeningNotListening()
	{
		resetFlags();
	}
	
	//Not Listening -> Listening
	protected virtual void Transition_NotListeningListening()
	{
		resetFlags();
	}
	
	//Listening -> Hearing Audio
	protected virtual void Transition_ListeningHearingAudio()
	{
		resetFlags();
	}
	
	//Listening -> Hearing Speech
	protected virtual void Transition_ListeningHearingSpeech()
	{
		resetFlags();
	}
	
	//Hearing Audio -> Hearing Speech
	protected virtual void Transition_HearingAudioHearingSpeech()
	{
		resetFlags();
	}
	
	//Hearing Audio -> Not Listening
	protected virtual void Transition_HearingAudioNotListening()
	{
		resetFlags();
	}
	
	//Hearing Speech -> Listening
	protected virtual void Transition_HearingSpeechListening()
	{
		resetFlags();
	}
	
	//Hearing Speech -> Not Listening
	protected virtual void Transition_HearingSpeechNotListening()
	{
		resetFlags();
	}
	
	//Not Listening -> Open Listening
	protected virtual void Transition_NotListeningOpenListening()
	{
		resetFlags();
	}
	
	//Open Listening -> Not Listening
	protected virtual void Transition_OpenListeningNotListening()
	{
		resetFlags();
	}
	
	//Open Listening -> Open Hearing Audio
	protected virtual void Transition_OpenListeningOpenHearingAudio()
	{
		resetFlags();
	}
	
	//Open Hearing Audio -> Not Listening
	protected virtual void Transition_OpenHearingAudioNotListening()
	{
		resetFlags();
	}
	
	protected virtual void Transition_OpenListeningOpenHearingSpeech()
	{
		resetFlags();
	}
	
	protected virtual void Transition_OpenHearingAudioOpenHearingSpeech()
	{
		resetFlags();
	}
	
	protected virtual void Transition_OpenHearingSpeechNotListening()
	{
		resetFlags();
	}
	
	protected virtual void Transition_OpenHearingAudioOpenListening()
	{
		resetFlags();	
	}
	
	protected virtual void Transition_HearingAudioListening()
	{
		resetFlags();
	}
	
	//Reset all flags for triggering transitions
	public void resetFlags()
	{
		startListening = false;
		startOpenListening = false;
		stopListening = false;
		ListenController.audioSpike = false;
		ListenController.speechHypothesized = false;
		ListenController.speechRecognized = false;
		ListenController.speechRejected = false;
		ListenController.mouseClicked = false;
		audioTime = 0f;
	}
	
	
	//How many utterances have not been heard yet?
	public int utterancesLeft()
	{
		int numLeft = 0;
		foreach (bool comp in completed) {
			if (!comp)
				numLeft++;
		}
		return numLeft;
	}
	
	
	public override void _CreateStates()
	{
		// Initialize states
		_InitStateDefs<ListenState>();
		_InitStateTransDefs( (int)ListenState.NotListening, 2 );
		_InitStateTransDefs( (int)ListenState.Listening, 3 );
		_InitStateTransDefs( (int)ListenState.OpenListening, 3 );
		_InitStateTransDefs( (int)ListenState.OpenHearingAudio, 3 );
		_InitStateTransDefs( (int)ListenState.HearingAudio, 3 );
		_InitStateTransDefs( (int)ListenState.HearingSpeech, 2 );
		_InitStateTransDefs( (int)ListenState.OpenHearingSpeech, 1 );
		//NotListening
		states[(int)ListenState.NotListening].updateHandler = "Update_NotListening";
		states[(int)ListenState.NotListening].nextStates[0].nextState = "Listening";
		states[(int)ListenState.NotListening].nextStates[0].transitionHandler = "Transition_NotListeningListening";
		states[(int)ListenState.NotListening].nextStates[1].nextState = "OpenListening";
		states[(int)ListenState.NotListening].nextStates[1].transitionHandler = "Transition_NotListeningOpenListening";
		//Listening
		states[(int)ListenState.Listening].updateHandler = "Update_Listening";
		states[(int)ListenState.Listening].nextStates[0].nextState = "NotListening";
		states[(int)ListenState.Listening].nextStates[0].transitionHandler = "Transition_ListeningNotListening";
		states[(int)ListenState.Listening].nextStates[1].nextState = "HearingAudio";
		states[(int)ListenState.Listening].nextStates[1].transitionHandler = "Transition_ListeningHearingAudio";
		states[(int)ListenState.Listening].nextStates[2].nextState = "HearingSpeech";
		states[(int)ListenState.Listening].nextStates[2].transitionHandler = "Transition_ListeningHearingSpeech";
		//OpenListening
		states[(int)ListenState.OpenListening].updateHandler = "Update_OpenListening";
		states[(int)ListenState.OpenListening].nextStates[0].nextState = "OpenHearingAudio";
		states[(int)ListenState.OpenListening].nextStates[0].transitionHandler = "Transition_OpenListeningOpenHearingAudio";
		states[(int)ListenState.OpenListening].nextStates[1].nextState = "NotListening";
		states[(int)ListenState.OpenListening].nextStates[1].transitionHandler = "Transition_OpenListeningNotListening";
		states[(int)ListenState.OpenListening].nextStates[2].nextState = "OpenHearingSpeech";
		states[(int)ListenState.OpenListening].nextStates[2].transitionHandler = "Transition_OpenListeningOpenHearingSpeech";
		//OpenHearingAudio
		states[(int)ListenState.OpenHearingAudio].updateHandler = "Update_OpenHearingAudio";
		states[(int)ListenState.OpenHearingAudio].nextStates[0].nextState = "NotListening";
		states[(int)ListenState.OpenHearingAudio].nextStates[0].transitionHandler = "Transition_OpenHearingAudioNotListening";
		states[(int)ListenState.OpenHearingAudio].nextStates[1].nextState = "OpenHearingSpeech";
		states[(int)ListenState.OpenHearingAudio].nextStates[1].transitionHandler = "Transition_OpenHearingAudioOpenHearingSpeech";
		states[(int)ListenState.OpenHearingAudio].nextStates[2].nextState = "OpenListening";
		states[(int)ListenState.OpenHearingAudio].nextStates[2].transitionHandler = "Transition_OpenHearingAudioOpenListening";
		//HearingAudio
		states[(int)ListenState.HearingAudio].updateHandler = "Update_HearingAudio";
		states[(int)ListenState.HearingAudio].nextStates[0].nextState = "HearingSpeech";
		states[(int)ListenState.HearingAudio].nextStates[0].transitionHandler = "Transition_HearingAudioHearingSpeech";
		states[(int)ListenState.HearingAudio].nextStates[1].nextState = "NotListening";
		states[(int)ListenState.HearingAudio].nextStates[1].transitionHandler = "Transition_HearingAudioNotListening";
		states[(int)ListenState.HearingAudio].nextStates[2].nextState = "Listening";
		states[(int)ListenState.HearingAudio].nextStates[2].transitionHandler = "Transition_HearingAudioListening";
		//HearingSpeech
		states[(int)ListenState.HearingSpeech].updateHandler = "Update_HearingSpeech";
		states[(int)ListenState.HearingSpeech].nextStates[0].nextState = "Listening";
		states[(int)ListenState.HearingSpeech].nextStates[0].transitionHandler = "Transition_HearingSpeechListening";
		states[(int)ListenState.HearingSpeech].nextStates[1].nextState = "NotListening";
		states[(int)ListenState.HearingSpeech].nextStates[1].transitionHandler = "Transition_HearingSpeechNotListening";
		//OpenHearingSpeech
		states[(int)ListenState.OpenHearingSpeech].updateHandler = "Update_OpenHearingSpeech";
		states[(int)ListenState.OpenHearingSpeech].nextStates[0].nextState = "NotListening";
		states[(int)ListenState.OpenHearingSpeech].nextStates[0].transitionHandler = "Transition_OpenHearingSpeechNotListening";
	}
};
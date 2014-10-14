using UnityEngine;
using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;

public enum SimpleListenState
{
	NotListening,
	Listening,
	SpeechDetected
}

public class SimpleListenController : AnimController
{
	public string speechHostName = "localhost";
	public int speechPortNum = 1408;
	public string[] phrases = new string[0];
	public bool listen = false;
	public bool stopListen = false;
	
	protected TcpClient speechClient = null;
	protected NetworkStream speechStream = null;
	protected StreamReader speechStrIn = null;
	protected StreamWriter speechStrOut = null;
	protected bool speechRecognized = false;
	protected string speechRecognizedPhrase = "";
	protected bool speechRejected = false;
	protected Process speechProc = null;
	
	public virtual bool SpeechDetected
	{
		get
		{
			return StateId == (int)SimpleListenState.SpeechDetected;
		}
	}
	
	public virtual bool SpeechRecognized
	{
		get
		{
			return speechRecognized;
		}
	}
	
	public virtual string RecognizedPhrase
	{
		get
		{
			return speechRecognizedPhrase;
		}
	}
	
	public virtual bool SpeechRejected
	{
		get
		{
			return speechRejected;
		}
	}
	
	public virtual void LaunchServer()
	{
		speechProc = new Process();
		speechProc.StartInfo.FileName = ".\\KinectServer\\KinectServer.exe";
		speechProc.StartInfo.UseShellExecute = false;
		speechProc.StartInfo.WindowStyle = ProcessWindowStyle.Minimized;
		try
		{
			speechProc.Start();
		}
		catch(Exception ex)
		{
			UnityEngine.Debug.LogError("Failed to launch the speech recognition server: " + ex.Message);
		}
	}
	
	public virtual bool ConnectToServer()
	{
		try
		{
			speechClient = new TcpClient(speechHostName, speechPortNum);
			speechStream = speechClient.GetStream();
			speechStrIn = new StreamReader(speechStream);
			speechStrOut = new StreamWriter(speechStream);
			speechStrOut.AutoFlush = true;
			
			foreach( string phrase in phrases )
				speechStrOut.WriteLine("addPhrase " + phrase);
			speechStrOut.WriteLine("initSpeechRecognizer");
			
			return true;
		}
		catch(Exception ex)
		{
			UnityEngine.Debug.LogError( string.Format(
				"Failed to connect to speech server {0}:{1}: {2}",
				speechHostName, speechPortNum, ex.Message )
				);
			
			return false;
		}
	}
	
	public virtual void Listen()
	{
		listen = true;
	}
	
	public virtual void StopListening()
	{
		stopListen = true;
	}

	protected override void _Init()
	{
	}
	
	protected virtual void Update_NotListening()
	{
		if(listen)
			GoToState((int)SimpleListenState.Listening);
	}
	
	protected virtual void Update_Listening()
	{
		string cmd = speechStream.DataAvailable ? speechStrIn.ReadLine() : "";
		if( cmd == "speechDetected" )
			GoToState((int)SimpleListenState.SpeechDetected);
		else if(stopListen)
			GoToState((int)SimpleListenState.NotListening);
	}
	
	protected virtual void Update_SpeechDetected()
	{
		string cmd = speechStream.DataAvailable ? speechStrIn.ReadLine() : "";
		if( cmd.StartsWith("speechRecognized") )
		{
			speechRecognized = true;
			speechRejected = false;
			speechRecognizedPhrase = cmd.Substring("speechRecognized".Length + 1);
			
			GoToState((int)SimpleListenState.Listening);
		}
		else if( cmd.StartsWith("speechRejected") )
		{
			speechRejected = true;
			speechRecognized = false;
			speechRecognizedPhrase = "";
			
			GoToState((int)SimpleListenState.Listening);
		}
		else if(stopListen)
			GoToState((int)SimpleListenState.NotListening);
	}
	
	protected virtual void Transition_NotListeningListening()
	{
		listen = false;
		speechStrOut.WriteLine("listenToSpeech");
	}
	
	protected virtual void Transition_ListeningSpeechDetected()
	{
		speechRecognized = false;
		speechRejected = false;
		speechRecognizedPhrase = "";
	}
	
	protected virtual void Transition_ListeningNotListening()
	{
		stopListen = false;
		speechStrOut.WriteLine("stopListenToSpeech");
	}
	
	protected virtual void Transition_SpeechDetectedListening()
	{
	}
	
	protected virtual void Transition_SpeechDetectedNotListening()
	{
		stopListen = false;
		speechStrOut.WriteLine("stopListenToSpeech");
	}
	
	void OnDisable()
	{
		if( speechClient != null && speechClient.Connected &&
			speechStream != null && speechStream.CanWrite)
		{
			speechStrOut.WriteLine("stopListenToSpeech");
			speechStrOut.WriteLine("disconnect");
		}
		if( speechProc != null )
			speechProc.Kill();
	}
	
	public override void UEdCreateStates()
	{
		// Initialize states
		_InitStateDefs<SimpleListenState>();
		_InitStateTransDefs( (int)SimpleListenState.NotListening, 1 );
		_InitStateTransDefs( (int)SimpleListenState.Listening, 2 );
		_InitStateTransDefs( (int)SimpleListenState.SpeechDetected, 2 );
		// NotListening state
		states[(int)SimpleListenState.NotListening].updateHandler = "Update_NotListening";
		states[(int)SimpleListenState.NotListening].nextStates[0].nextState = "Listening";
		states[(int)SimpleListenState.NotListening].nextStates[0].transitionHandler = "Transition_NotListeningListening";
		// Listening state
		states[(int)SimpleListenState.Listening].updateHandler = "Update_Listening";
		states[(int)SimpleListenState.Listening].nextStates[0].nextState = "NotListening";
		states[(int)SimpleListenState.Listening].nextStates[0].transitionHandler = "Transition_ListeningNotListening";
		states[(int)SimpleListenState.Listening].nextStates[1].nextState = "SpeechDetected";
		states[(int)SimpleListenState.Listening].nextStates[1].transitionHandler = "Transition_ListeningSpeechDetected";
		// SpeechDetected state
		states[(int)SimpleListenState.SpeechDetected].updateHandler = "Update_SpeechDetected";
		states[(int)SimpleListenState.SpeechDetected].nextStates[0].nextState = "Listening";
		states[(int)SimpleListenState.SpeechDetected].nextStates[0].transitionHandler = "Transition_SpeechDetectedListening";
		states[(int)SimpleListenState.SpeechDetected].nextStates[1].nextState = "NotListening";
		states[(int)SimpleListenState.SpeechDetected].nextStates[1].transitionHandler = "Transition_SpeechDetectedNotListening";
	}
}


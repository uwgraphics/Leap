using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
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

/// <summary>
/// Speech listening result.
/// </summary>
public struct ListenResult
{
    private float detectTime;
    private float recognizeTime;
    private string recognizedPhrase;

    /// <summary>
    /// Time when speech was detected.
    /// </summary>
    public float DetectTime
    {
        get { return detectTime; }
    }

    /// <summary>
    /// Time when speech was recognized or rejected.
    /// </summary>
    public float RecognizeTime
    {
        get { return recognizeTime; }
    }

    /// <summary>
    /// true if speech was recognized, false otherwise.
    /// </summary>
    public bool Recognized
    {
        get { return recognizedPhrase != ""; }
    }

    /// <summary>
    /// Recognized phrase (empty string if speech was not recognized).
    /// </summary>
    public string RecognizedPhrase
    {
        get { return recognizedPhrase; }
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="detectTime">Time when speech was detected</param>
    /// <param name="recognizeTime">Time when speech was recognized or rejected</param>
    /// <param name="recognizedPhrase">Recognized phrase (empty string if speech was not recognized)</param>
    public ListenResult(float detectTime, float recognizeTime, string recognizedPhrase)
    {
        this.detectTime = detectTime;
        this.recognizeTime = recognizeTime;
        this.recognizedPhrase = recognizedPhrase;
    }
}

/// <summary>
/// Simple speech listening controller.
/// </summary>
public class SimpleListenController : AnimController
{
    public bool startProcess = true;
    public string speechHostName = "localhost";
    public int speechPortNum = 1408;
    public string[] phrases = new string[0];
    public bool listen = false;
    public bool stopListen = false;

    protected TcpClient speechClient = null;
    protected NetworkStream speechStream = null;
    protected StreamReader speechStrIn = null;
    protected StreamWriter speechStrOut = null;
    protected Process speechProc = null;
    protected float speechDetectTime = 0f;
    protected List<ListenResult> results = new List<ListenResult>();

    /// <summary>
    /// Speech listening results.
    /// </summary>
    public virtual List<ListenResult> Results
    {
        get
        {
            return results;
        }
    }

    /// <summary>
    /// Start speech recognition server process.
    /// </summary>
    public virtual void StartServer()
    {
        if (!startProcess)
            return;

        speechProc = new Process();
        speechProc.StartInfo.FileName = File.Exists("SpeechServer.exe") ? "SpeechServer.exe" :
            "..\\Tools\\SpeechServer\\bin\\Release\\SpeechServer.exe";
        speechProc.StartInfo.UseShellExecute = false;
        speechProc.StartInfo.WindowStyle = ProcessWindowStyle.Minimized;
        try
        {
            speechProc.Start();
            UnityEngine.Debug.Log("SpeechServer started");
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError("Failed to launch the speech recognition server: " + ex.Message);
        }
    }

    /// <summary>
    /// Connect to speech recognition server process.
    /// </summary>
    /// <returns></returns>
    public virtual bool ConnectToServer()
    {
        try
        {
            speechClient = new TcpClient(speechHostName, speechPortNum);
            speechStream = speechClient.GetStream();
            speechStrIn = new StreamReader(speechStream);
            speechStrOut = new StreamWriter(speechStream);
            speechStrOut.AutoFlush = true;

            foreach (string phrase in phrases)
                speechStrOut.WriteLine("addPhrase " + phrase);
            speechStrOut.WriteLine("initSpeechRecognizer");

            UnityEngine.Debug.Log("Connected to SpeechServer");

            return true;
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError(string.Format(
                "Failed to connect to speech server {0}:{1}: {2}",
                speechHostName, speechPortNum, ex.Message)
                );

            return false;
        }
    }

    /// <summary>
    /// Listen for speech utterances.
    /// </summary>
    public virtual void Listen()
    {
        listen = true;
    }

    /// <summary>
    /// Stop listening for speech utterances.
    /// </summary>
    public virtual void StopListening()
    {
        stopListen = true;
    }

    public override void Start()
    {
        base.Start();

        StartServer();
        ConnectToServer();
    }

    protected virtual void Update_NotListening()
    {
        if (listen && speechStream != null)
            GoToState((int)SimpleListenState.Listening);
    }

    protected virtual void Update_Listening()
    {
        string cmd = speechStream.DataAvailable ? speechStrIn.ReadLine() : "";
        if (cmd == "speechDetected")
        {
            UnityEngine.Debug.Log("SpeechServer: speech detected");
            GoToState((int)SimpleListenState.SpeechDetected);
        }
        else if (stopListen)
            GoToState((int)SimpleListenState.NotListening);
    }

    protected virtual void Update_SpeechDetected()
    {
        string cmd = speechStream.DataAvailable ? speechStrIn.ReadLine() : "";
        if (cmd.StartsWith("speechRecognized"))
        {
            string phrase = cmd.Substring("speechRecognized".Length + 1);
            results.Add(new ListenResult(speechDetectTime, Time.timeSinceLevelLoad, phrase));
            UnityEngine.Debug.Log("SpeechServer: speech recognized; phrase = " + phrase);

            GoToState((int)SimpleListenState.Listening);
        }
        else if (cmd.StartsWith("speechRejected") || cmd == "")
        {
            results.Add(new ListenResult(speechDetectTime, Time.timeSinceLevelLoad, ""));
            UnityEngine.Debug.Log("SpeechServer: speech rejected");

            GoToState((int)SimpleListenState.Listening);
        }
        else if (stopListen)
            GoToState((int)SimpleListenState.NotListening);
    }

    protected virtual void Transition_NotListeningListening()
    {
        listen = false;
        speechStrOut.WriteLine("listenToSpeech");
        results.Clear();
    }

    protected virtual void Transition_ListeningSpeechDetected()
    {
        speechDetectTime = Time.timeSinceLevelLoad;
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
        if (speechClient != null && speechClient.Connected &&
            speechStream != null && speechStream.CanWrite)
        {
            speechStrOut.WriteLine("stopListenToSpeech");
            speechStrOut.WriteLine("disconnect");
        }
        if (speechProc != null)
            speechProc.Kill();
    }

    public override void _CreateStates()
    {
        // Initialize states
        _InitStateDefs<SimpleListenState>();
        _InitStateTransDefs((int)SimpleListenState.NotListening, 1);
        _InitStateTransDefs((int)SimpleListenState.Listening, 2);
        _InitStateTransDefs((int)SimpleListenState.SpeechDetected, 2);
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


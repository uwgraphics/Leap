/************************************************************************************
Filename    :   OVRLipSyncContext.cs
Content     :   Interface to Oculus Lip-Sync engine
Created     :   August 6th, 2015
Copyright   :   Copyright 2015 Oculus VR, Inc. All Rights reserved.

Licensed under the Oculus VR Rift SDK License Version 3.1 (the "License"); 
you may not use the Oculus VR Rift SDK except in compliance with the License, 
which is provided at the time of installation or download, or which 
otherwise accompanies this software in either electronic or hard copy form.

You may obtain a copy of the License at

http://www.oculusvr.com/licenses/LICENSE-3.1 

Unless required by applicable law or agreed to in writing, the Oculus VR SDK 
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
************************************************************************************/
using UnityEngine;
using System;
using System.Runtime.InteropServices;


[RequireComponent(typeof(AudioSource))]

//-------------------------------------------------------------------------------------
// ***** OVRPhonemeContext
//
/// <summary>
/// OVRPhonemeContext interfaces into the Oculus phoneme recognizer. 
/// This component should be added into the scene once for each Audio Source. 
///
/// </summary>
public class OVRLipSyncContext : MonoBehaviour
{
    // * * * * * * * * * * * * *
    // Public members
    public AudioSource audioSource = null;
    public float gain = 1.0f;
    public bool audioMute = true;
    public KeyCode loopback = KeyCode.L;
    public KeyCode debugVisemes = KeyCode.D;
    public bool showVisemes = false;

    public OVRLipSync.ovrLipSyncContextProvider provider = OVRLipSync.ovrLipSyncContextProvider.Main;
    public bool delayCompensate = false;

    // * * * * * * * * * * * * *
    // Private members
    private OVRLipSync.ovrLipSyncFrame frame = new OVRLipSync.ovrLipSyncFrame(0);
    private uint context = 0;	// 0 is no context

    // Holds the state of previous frame
    private OVRLipSync.ovrLipSyncFrame debugFrame = new OVRLipSync.ovrLipSyncFrame(0);
    private float debugFrameTimer = 0.0f;
    private float debugFrameTimeoutValue = 0.1f;	// sec.

    // * * * * * * * * * * * * *
    // Static members

    // * * * * * * * * * * * * *
    // MonoBehaviour overrides

    /// <summary>
    /// Awake this instance.
    /// </summary>
    void Awake()
    {
        // Cache the audio source we are going to be using to pump data to the SR
        if (!audioSource) audioSource = GetComponent<AudioSource>();
        if (!audioSource) return;
    }

    /// <summary>
    /// Start this instance.
    /// Note: make sure to always have a Start function for classes that have editor scripts.
    /// </summary>
    void Start()
    {
        // Create the context that we will feed into the audio buffer
        lock (this)
        {
            if (context == 0)
            {
                if (OVRLipSync.CreateContext(ref context, provider) != OVRLipSync.ovrLipSyncSuccess)
                {
                    Debug.Log("OVRPhonemeContext.Start ERROR: Could not create Phoneme context.");
                    return;
                }
            }
        }

        //		OVRLipSyncDebugConsole.Clear ();
        //		OVRLipSyncDebugConsole.Log ("Welcome to the viseme demo! Use 'Left Arrow' and 'Right Arrow' to adjust input gain. Press 'L' to hear mic input.");

        // Add a listener to the OVRMessenger for touch events
        OVRMessenger.AddListener<OVRTouchpad.TouchEvent>("Touchpad", LocalTouchEventCallback);
    }

    /// <summary>
    /// Run processes that need to be updated in our game thread
    /// </summary>
    void Update()
    {
        // Turn loopback on/off
        if (Input.GetKeyDown(loopback))
        {
            audioMute = !audioMute;

            OVRLipSyncDebugConsole.Clear();
            OVRLipSyncDebugConsole.ClearTimeout(1.5f);

            if (audioMute)
                OVRLipSyncDebugConsole.Log("LOOPBACK MODE: ENABLED");
            else
                OVRLipSyncDebugConsole.Log("LOOPBACK MODE: DISABLED");
        }
        else if (Input.GetKeyDown(debugVisemes))
        {
            showVisemes = !showVisemes;

            if (showVisemes)
                Debug.Log("DEBUG SHOW VISEMES: ENABLED");
            else
            {
                OVRLipSyncDebugConsole.Clear();
                Debug.Log("DEBUG SHOW VISEMES: DISABLED");
            }
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            gain -= 1.0f;
            if (gain < 1.0f) gain = 1.0f;

            string g = "LINEAR GAIN: ";
            g += gain;
            OVRLipSyncDebugConsole.Clear();
            OVRLipSyncDebugConsole.Log(g);
            OVRLipSyncDebugConsole.ClearTimeout(1.5f);
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            gain += 1.0f;
            if (gain > 15.0f)
                gain = 15.0f;

            string g = "LINEAR GAIN: ";
            g += gain;
            OVRLipSyncDebugConsole.Clear();
            OVRLipSyncDebugConsole.Log(g);
            OVRLipSyncDebugConsole.ClearTimeout(1.5f);
        }

        DebugShowVisemes();
    }

    /// <summary>
    /// Raises the destroy event.
    /// </summary>
    void OnDestroy()
    {
        // Create the context that we will feed into the audio buffer
        lock (this)
        {
            if (context != 0)
            {
                if (OVRLipSync.DestroyContext(context) != OVRLipSync.ovrLipSyncSuccess)
                {
                    Debug.Log("OVRPhonemeContext.OnDestroy ERROR: Could not delete Phoneme context.");
                }
            }
        }
    }

    /// <summary>
    /// Raises the audio filter read event.
    /// </summary>
    /// <param name="data">Data.</param>
    /// <param name="channels">Channels.</param>
    void OnAudioFilterRead(float[] data, int channels)
    {
        // Do not spatialize if we are not initialized, or if there is no
        // audio source attached to game object
        if ((OVRLipSync.IsInitialized() != OVRLipSync.ovrLipSyncSuccess) || audioSource == null)
            return;

        // increase the gain of the input to get a better signal input
        for (int i = 0; i < data.Length; ++i)
            data[i] = data[i] * gain;

        // Send data into Phoneme context for processing (if context is not 0)
        lock (this)
        {
            if (context != 0)
            {
                OVRLipSync.ovrLipSyncFlag flags = 0;

                // Set flags to feed into process
                if (delayCompensate == true)
                    flags |= OVRLipSync.ovrLipSyncFlag.DelayCompensateAudio;

                OVRLipSync.ProcessFrameInterleaved(context, data, flags, ref frame);
            }
        }

        // Turn off output (so that we don't get feedback from mics too close to speakers)
        if (audioMute == true)
        {
            for (int i = 0; i < data.Length; ++i)
                data[i] = data[i] * 0.0f;
        }
    }

    // * * * * * * * * * * * * *
    // Public Functions

    /// <summary>
    /// Gets the current phoneme frame (lock and copy current frame to caller frame)
    /// </summary>
    /// <returns>error code</returns>
    /// <param name="inFrame">In frame.</param>
    public int GetCurrentPhonemeFrame(ref OVRLipSync.ovrLipSyncFrame inFrame)
    {
        if (OVRLipSync.IsInitialized() != OVRLipSync.ovrLipSyncSuccess)
            return (int)OVRLipSync.ovrLipSyncError.Unknown;

        lock (this)
        {
            inFrame.frameNumber = frame.frameNumber;
            inFrame.frameDelay = frame.frameDelay;
            for (int i = 0; i < inFrame.Visemes.Length; i++)
            {
                inFrame.Visemes[i] = frame.Visemes[i];
            }
        }

        return OVRLipSync.ovrLipSyncSuccess;
    }

    /// <summary>
    /// Debugs the show visemes.
    /// </summary>
    void DebugShowVisemes()
    {
        if (showVisemes == false)
            return;

        debugFrameTimer -= Time.deltaTime;

        if (debugFrameTimer < 0.0f)
        {
            debugFrameTimer += debugFrameTimeoutValue;
            debugFrame.CopyInput(ref frame);
        }

        string seq = "";
        for (int i = 0; i < debugFrame.Visemes.Length; i++)
        {
            if (i < 10)
                seq += "0";

            seq += i;
            seq += ":";

            int count = (int)(50.0f * debugFrame.Visemes[i]);
            for (int c = 0; c < count; c++)
                seq += "*";

            //seq += (int)(debugFrame.Visemes[i] * 100.0f); 

            seq += "\n";
        }

        OVRLipSyncDebugConsole.Clear();
        OVRLipSyncDebugConsole.Log(seq);
    }

    /// <summary>
    /// Resets the context.
    /// </summary>
    /// <returns>error code</returns>
    public int ResetContext()
    {
        if (OVRLipSync.IsInitialized() != OVRLipSync.ovrLipSyncSuccess)
            return (int)OVRLipSync.ovrLipSyncError.Unknown;

        return OVRLipSync.ResetContext(context);
    }

    /// <summary>
    /// Sends the signal.
    /// </summary>
    /// <returns>error code</returns>
    /// <param name="signal">Signal.</param>
    /// <param name="arg1">Arg1.</param>
    /// <param name="arg2">Arg2.</param>
    public int SendSignal(OVRLipSync.ovrLipSyncSignals signal, int arg1, int arg2)
    {
        if (OVRLipSync.IsInitialized() != OVRLipSync.ovrLipSyncSuccess)
            return (int)OVRLipSync.ovrLipSyncError.Unknown;

        return OVRLipSync.SendSignal(context, signal, arg1, arg2);
    }

    // LocalTouchEventCallback
    void LocalTouchEventCallback(OVRTouchpad.TouchEvent touchEvent)
    {
        string g = "LINEAR GAIN: ";

        switch (touchEvent)
        {
            case (OVRTouchpad.TouchEvent.SingleTap):
                audioMute = !audioMute;

                OVRLipSyncDebugConsole.Clear();
                OVRLipSyncDebugConsole.ClearTimeout(1.5f);

                if (audioMute)
                    OVRLipSyncDebugConsole.Log("LOOPBACK MODE: ENABLED");
                else
                    OVRLipSyncDebugConsole.Log("LOOPBACK MODE: DISABLED");

                break;

            case (OVRTouchpad.TouchEvent.Up):
                gain += 1.0f;
                if (gain > 15.0f)
                    gain = 15.0f;

                g += gain;
                OVRLipSyncDebugConsole.Clear();
                OVRLipSyncDebugConsole.Log(g);
                OVRLipSyncDebugConsole.ClearTimeout(1.5f);
                break;

            case (OVRTouchpad.TouchEvent.Down):
                gain -= 1.0f;
                if (gain < 1.0f) gain = 1.0f;

                g += gain;
                OVRLipSyncDebugConsole.Clear();
                OVRLipSyncDebugConsole.Log(g);
                OVRLipSyncDebugConsole.ClearTimeout(1.5f);

                break;
        }
    }
}
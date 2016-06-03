/************************************************************************************
Filename    :   OVRLipSyncContextMorphTarget.cs
Content     :   This bridges the viseme output to the morph targets
Created     :   August 7th, 2015
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
using System.Collections;

public class OVRLipSyncContextMorphTarget : MonoBehaviour 
{	
	// PUBLIC

	// Manually assign the skinned mesh renderer to this script
	public SkinnedMeshRenderer skinnedMeshRenderer = null;
	
	// data that will be queried and passed between phoneme and renderer each frame
	private OVRLipSync.ovrLipSyncFrame frame = new OVRLipSync.ovrLipSyncFrame(0);

	// Set the blendshape index to go to (-1 means there is not one assigned)
	public int [] VisemeToBlendTargets = new int[(int)OVRLipSync.ovrLipSyncViseme.Count];

	// enable/disable sending signals to viseme engine
	public bool enableVisemeSignals = false;

	// button presses (1 through 0)that will send a signal to the lip-sync engine to trigger a viseme
	public int [] KeySendVisemeSignal = new int[10];

	// smoothing amount
	public int SmoothAmount = 100; 

	// PRIVATE

	// Look for a lip-sync Context (should be set at the same level as this component)
	private OVRLipSyncContext lipsyncContext = null;

	// Look for a sequence component (should be set at the same level as this component)
	private OVRLipSyncContextSequencer sequencer = null;
	
	//------------
	// Test values
	private const string testSeq1Name = "TestSequence1";
	private const string textSeq1Info = "Hello world. This is a test.";


	/// <summary>
	/// Start this instance.
	/// </summary>
	void Start () 
	{
		// morph target needs to be set manually; possibly other components will need the same
		if(skinnedMeshRenderer == null)
		{
			Debug.Log("LipSyncContextMorphTarget.Start WARNING: Please set required public components!");
			return;
		}

		// make sure there is a phoneme context assigned to this object
		lipsyncContext = GetComponent<OVRLipSyncContext>();
		if(lipsyncContext == null)
		{
			Debug.Log("LipSyncContextMorphTarget.Start WARNING: No phoneme context component set to object");
		}

		// Can we record and play sequences?
		sequencer = GetComponent<OVRLipSyncContextSequencer>();

		if(sequencer == null)
		{
			Debug.Log("LipSyncContextMorphTarget.Start: No sequencer set. Ability to record and playback keystrokes disabled.");
		}

		// Send smoothing amount to context
		lipsyncContext.SendSignal(OVRLipSync.ovrLipSyncSignals.VisemeSmoothing, SmoothAmount, 0);
	}
	
	/// <summary>
	/// Update this instance.
	/// </summary>
	void Update () 
	{
		if((lipsyncContext != null) && (skinnedMeshRenderer != null))
		{
			// trap inputs and send signals to phoneme engine for testing purposes

			// get the current viseme frame
			if(lipsyncContext.GetCurrentPhonemeFrame(ref frame) == OVRLipSync.ovrLipSyncSuccess)
			{
				SetVisemeToMorphTarget();
			}

			// Record and playback sequences
			ControlSequencer();

			// TEST visemes by capturing key inputs and sending a signal
			SendSignals();
		}
	}
	
	/// <summary>
	/// Sends the signals.
	/// </summary>
	void SendSignals()
	{
		if(enableVisemeSignals == false)
			return;

		// Send test signals here to move mouth with keys like a puppet
		// Only send key presses if sequencer is not playing
		if(SendVisemeSignalsPlaySequence() == true)
		{
			if(sequencer != null)
			{
				OVRLipSyncDebugConsole.Clear();
				OVRLipSyncDebugConsole.Log ("Playing recorded sequence.");
				string timer = "" + sequencer.GetCurrentTimer();
				OVRLipSyncDebugConsole.Log (timer);
			}
		}
		else
		{
			SendVisemeSignalsKeys();
		}
	}
	
	/// <summary>
	/// Sets the viseme to morph target.
	/// </summary>
	void SetVisemeToMorphTarget()
	{
		for (int i = 0; i < VisemeToBlendTargets.Length; i++)
		{
			if(VisemeToBlendTargets[i] != -1)
			{
				// Viseme blend weights are in range of 0->1.0, we need to make range 100
				skinnedMeshRenderer.SetBlendShapeWeight(VisemeToBlendTargets[i], frame.Visemes[i] * 100.0f);
			}
		}
	}
	
	/// <summary>
	/// Sends the viseme signals.
	/// </summary>
	void SendVisemeSignalsKeys()
	{
		// Capture buttons 1 through 0 (1 = 0, 0 = 10 :) )
		SendVisemeSignal(KeyCode.Alpha1, 0, 100); 
		SendVisemeSignal(KeyCode.Alpha2, 1, 100); 
		SendVisemeSignal(KeyCode.Alpha3, 2, 100); 
		SendVisemeSignal(KeyCode.Alpha4, 3, 100); 
		SendVisemeSignal(KeyCode.Alpha5, 4, 100); 
		SendVisemeSignal(KeyCode.Alpha6, 5, 100); 
		SendVisemeSignal(KeyCode.Alpha7, 6, 100); 
		SendVisemeSignal(KeyCode.Alpha8, 7, 100); 
		SendVisemeSignal(KeyCode.Alpha9, 8, 100); 
		SendVisemeSignal(KeyCode.Alpha0, 9, 100); 
		SendVisemeSignal(KeyCode.Q,     10, 100); 
		SendVisemeSignal(KeyCode.W,     11, 100); 
		SendVisemeSignal(KeyCode.E,     12, 100); 
		SendVisemeSignal(KeyCode.R,     13, 100); 
		SendVisemeSignal(KeyCode.T,     14, 100); 
	}
	
	/// <summary>
	/// Sends the viseme signal.
	/// </summary>
	/// <param name="key">Key.</param>
	/// <param name="viseme">Viseme.</param>
	/// <param name="arg1">Arg1.</param>
	void SendVisemeSignal(KeyCode key, int viseme, int arg1)
	{
		int result = 0;

		if (Input.GetKeyDown(key))
		{
			result = lipsyncContext.SendSignal(OVRLipSync.ovrLipSyncSignals.VisemeAmount, KeySendVisemeSignal[viseme], arg1);		
			RecordVisemeSignalSequenceEntry(OVRLipSync.ovrLipSyncSignals.VisemeAmount, KeySendVisemeSignal[viseme], arg1, 0);
		}
		if (Input.GetKeyUp(key))
		{
			result = lipsyncContext.SendSignal(OVRLipSync.ovrLipSyncSignals.VisemeAmount, KeySendVisemeSignal[viseme], 0);
			RecordVisemeSignalSequenceEntry(OVRLipSync.ovrLipSyncSignals.VisemeAmount, KeySendVisemeSignal[viseme], 0, 0);
		}

		if (result != OVRLipSync.ovrLipSyncSuccess)
		{
			Debug.Log("LipSyncContextMorphTarget.SendVisemeSignal WARNING: Possible bad range on arguments.");	
		}
	}

	/// <summary>
	/// Records the viseme signal.
	/// </summary>
	/// <param name="key">Key.</param>
	/// <param name="viseme">Viseme.</param>
	/// <param name="arg1">Arg1.</param>
	void RecordVisemeSignalSequenceEntry(OVRLipSync.ovrLipSyncSignals signal, int viseme, int arg1, int arg2)
	{
		// If record is on, add this to the list
		if((sequencer != null) && sequencer.IsRecording())
		{
			OVRLipSyncContextSequencer.SequenceEntry e = new OVRLipSyncContextSequencer.SequenceEntry();
			e.action = (int)signal;
			e.data1 = viseme;
			e.data2 = arg1;
			e.data3 = arg2;

			sequencer.AddEntryToRecording(ref e);
		}
	}

	/// <summary>
	/// Plaies the viseme signal.
	/// </summary>
	/// <returns><c>true</c>, if viseme signal was played, <c>false</c> otherwise.</returns>
	bool SendVisemeSignalsPlaySequence()
	{
		if(sequencer == null)
			return false;
		
		ArrayList entries = new ArrayList();	// We will add SequenceEntries here

		bool result = sequencer.UpdatePlayback(ref entries);

		for (int i = 0; i < entries.Count; i++)
		{
			OVRLipSyncContextSequencer.SequenceEntry e = (OVRLipSyncContextSequencer.SequenceEntry)entries[i];
			lipsyncContext.SendSignal((OVRLipSync.ovrLipSyncSignals)e.action, 
			                          e.data1, e.data2); // e.data3 reserved
		}

		return result;
	}

	/// <summary>
	/// Controls the sequencer.
	/// </summary>
	void ControlSequencer()
	{
		string sequenceName = testSeq1Name;
		string sequenceInfo = textSeq1Info;

		if(sequencer == null)
			return;

		if(sequencer.IsRecording() == false)
		{
			if (Input.GetKeyDown(KeyCode.Z))
			{
				sequencer.StopPlayback();

				if(sequencer.StartRecording(sequenceName, sequenceInfo) == false)
				{
					Debug.Log("LipSyncContextMorphTarget.ControlSequencer WARNING: Cannot start recording: " + sequenceName);	
				}
			}
			else if(Input.GetKeyDown(KeyCode.P))
			{
				sequencer.StopPlayback();

				if(sequencer.StartPlayback(testSeq1Name) == false)
				{
					Debug.Log("LipSyncContextMorphTarget.ControlSequencer WARNING: Cannot play sequence: " + sequenceName);	
				}
			}
			else if(Input.GetKeyDown(KeyCode.S))
			{
				sequencer.StopPlayback();

				if(sequencer.SaveSequence(testSeq1Name) == false)
				{
					Debug.Log("LipSyncContextMorphTarget.ControlSequencer WARNING: Cannot save sequence: " + sequenceName);	
				}
				else
				{
					OVRLipSyncDebugConsole.Clear();
					OVRLipSyncDebugConsole.Log ("Saving sequence " + sequenceName);
					OVRLipSyncDebugConsole.Log ("Press 'P' to play..");
				}
			}
			else if(Input.GetKeyDown(KeyCode.A))
			{
				sequencer.StopPlayback();

				if(sequencer.LoadSequence(testSeq1Name) == false)
				{
					Debug.Log("LipSyncContextMorphTarget.ControlSequencer WARNING: Cannot load sequence: " + sequenceName);	
				}
				else
				{
					OVRLipSyncDebugConsole.Clear();
					OVRLipSyncDebugConsole.Log ("Loading sequence " + sequenceName);
					OVRLipSyncDebugConsole.Log ("Press 'P' to play..");
				}
			}
		}
		else
		{
			// We are recording, update recorder
			OVRLipSyncDebugConsole.Clear();
			OVRLipSyncDebugConsole.Log ("Recording sequence " + sequenceName + ". Press 'X' to stop recording..");
			string tmr = "" + sequencer.GetCurrentTimer();
			OVRLipSyncDebugConsole.Log (tmr);

			if (Input.GetKeyDown(KeyCode.X))
			{
				OVRLipSyncDebugConsole.Clear();
				OVRLipSyncDebugConsole.Log ("Stopped recording sequence " + sequenceName);
				OVRLipSyncDebugConsole.Log ("Press 'P' to play..");
				
				if(sequencer.StopRecording() == false)
				{
					Debug.Log("LipSyncContextMorphTarget.ControlSequencer WARNING: Sequence not recording at this time.");	
				}
			}
		}
	}
}

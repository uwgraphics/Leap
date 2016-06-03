/************************************************************************************
Filename    :   OVRLipSyncContextSequencer.cs
Content     :   This component records and plays back OVRLipSyncContext signals
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

public class OVRLipSyncContextSequencer : MonoBehaviour
{
	/// Holds const values used by sequencer
	public struct ovrLipSyncContextSequencerValues 
	{
		public const int NullEntry = -1;		 	
		public const int CurrentSequenceNotSet = -1;
	};

	/// SequenceEntry - single entry in sequence. Added in order of time
	public struct SequenceEntry
	{
		// Init function
		public SequenceEntry(int init)
		{
			timestamp = 0.0f;
			action    = ovrLipSyncContextSequencerValues.NullEntry;
			data1     = 0;
			data2     = 0;
			data3     = 0;
		}

		// Clone
		public SequenceEntry Clone()
		{
			SequenceEntry r = new SequenceEntry(0);
			r.timestamp = timestamp;
			r.action = action;
			r.data1 = data1;
			r.data2 = data2;
			r.data3 = data3;

			return r;
		}

		// vars
		public float timestamp;

		// action + data
		public int   action; 
		public int   data1;
		public int   data2;
		public int   data3;
	};

	// Sequence - holds ordered entries for playback
	public struct Sequence
	{
		// Init function
		public Sequence(int init)
		{
			name = "INIT";
			info = "INIT";
			numEntries = 0;
			entries = new ArrayList();	// We will add SequenceEntries here
		}

		// Clone
		public Sequence Clone()
		{
			Sequence r = new Sequence(0);

			r.name = name;
			r.info = info;
			r.numEntries = numEntries;

			for (int i = 0; i < entries.Count; i++)
			{
				SequenceEntry e = (SequenceEntry)entries[i];
				r.entries.Add (e.Clone ());
			}
			return r;
		}

		// Clear
		void Clear()
		{
			name = "INIT";
			info = "INIT";
			numEntries = 0;
			entries.Clear();
		}

		// vars
		public string    name;
		public string    info;
		public int   	 numEntries;
		public ArrayList entries;
	};

	// Hold all sequences
	private ArrayList sequences = new ArrayList(0);

	// The current sequence playing/recording
	private int currentSequence = ovrLipSyncContextSequencerValues.CurrentSequenceNotSet;

	// Keep track of which entry the current sequence is at
	private int currentSequenceEntry =  0;

	private Sequence recordingSequence;

	// Captures the initial start time for recording
	private float startTime   = 0.0f;

	private bool  recording   = false;

	/// <summary>
	/// Gets the current timer.
	/// </summary>
	/// <returns>The current timer.</returns>
	public float GetCurrentTimer()
	{
		return Time.time - startTime;
	}

	/// <summary>
	/// Loads a sequence block. Name provided by caller.
	/// </summary>
	/// <returns><c>true</c>, if sequence block was loaded, <c>false</c> otherwise.</returns>
	/// <param name="blockName">Block name.</param>
	public bool LoadSequence(string sequenceName)
	{
		// look for loaded sequence already in array
		for (int i = 0; i < sequences.Count; i++)
		{
			Sequence s = (Sequence)sequences[i];
			if(s.name == sequenceName)
			{
				currentSequence = i;
				return true;
			}
		}

		// We will load up a stored sequence if it has been previously recorded
		if(PlayerPrefs.GetString (sequenceName, "") == "")
		{
			Debug.Log ("OVRLipSyncContextSequencer WARNING: Cannot load sequence " + sequenceName);
			return false;
		}

		// Create a new sequence
		Sequence seq = new Sequence(0);
		seq.name = sequenceName;

		// Use sequenceKey to collect sequence data
		string sequenceKey = "";

		// INFO (Used for string to show on-screen)
		sequenceKey = sequenceName;
		sequenceKey += "_INFO";

		seq.info = PlayerPrefs.GetString (sequenceKey, "NO INFO");

		// Number of entries
		sequenceKey = sequenceName;
		sequenceKey += "_NE";
		seq.numEntries = PlayerPrefs.GetInt (sequenceKey, 0);

		// Get entries
		for (int i = 0; i < seq.numEntries; i++)
		{
			SequenceEntry e = new SequenceEntry(0);

			// Name of entry
			sequenceKey = sequenceName;
			sequenceKey += "_E_";
			sequenceKey += i;

			// for each field in entry
			string sequenceKeyField = "";

			sequenceKeyField = sequenceKey;
			sequenceKeyField += "_TIMESTAMP";
			e.timestamp = PlayerPrefs.GetFloat(sequenceKeyField, 0.0f); 

			sequenceKeyField = sequenceKey;
			sequenceKeyField += "_ACTION";
			e.action = PlayerPrefs.GetInt (sequenceKeyField, -1); // NULL action if does not exist

			sequenceKeyField = sequenceKey;
			sequenceKeyField += "_DATA1";
			e.data1 = PlayerPrefs.GetInt (sequenceKeyField, 0);

			sequenceKeyField = sequenceKey;
			sequenceKeyField += "_DATA2";
			e.data2 = PlayerPrefs.GetInt (sequenceKeyField, 0);

			sequenceKeyField = sequenceKey;
			sequenceKeyField += "_DATA3";
			e.data3 = PlayerPrefs.GetInt (sequenceKeyField, 0);
		
			// Add entry
			seq.entries.Add (e);
		}

		// SANITY CHECK: Make sure that number of entries is the same as what was
		// added 
		if(seq.numEntries != seq.entries.Count)
		{
			Debug.Log ("OVRLipSyncContextSequencer WARNING: " + sequenceName + " might be corrupted.");
			return false;
		}

		// Add to sequence list
		sequences.Add (seq);

		return true;
	}

	/// <summary>
	/// Saves the sequence.
	/// </summary>
	/// <param name="SequenceName">Sequence name.</param>
	public bool SaveSequence(string sequenceName)
	{
		int sIndex = ovrLipSyncContextSequencerValues.NullEntry;

		// A sequence will need to be in the sequence list to save out
		// look for loaded sequence already in array
		for (int i = 0; i < sequences.Count; i++)
		{
			Sequence s = (Sequence)sequences[i];
			if(s.name == sequenceName)
			{
				sIndex = i;
				break;
			}
		}

		if(sIndex == ovrLipSyncContextSequencerValues.NullEntry)
		{
			Debug.Log ("OVRPhonemeContextSequencer WARNING: " + sequenceName + " does not exist, cannot save.");
			return false;
		}

		Sequence saveSeq = (Sequence)sequences[sIndex];

		// We can save out each entry in this sequence

		// Use sequenceKey to collect sequence data
		string sequenceKey = sequenceName;
		PlayerPrefs.SetString(sequenceKey, saveSeq.name);

		sequenceKey = sequenceName + "_INFO";
		PlayerPrefs.SetString(sequenceKey, saveSeq.info);

		sequenceKey = sequenceName + "_NE";
		PlayerPrefs.SetInt (sequenceKey, saveSeq.numEntries);

		for (int i = 0; i < saveSeq.entries.Count; i++)
		{
			SequenceEntry e = (SequenceEntry)saveSeq.entries[i];
			sequenceKey = sequenceName;
			sequenceKey += "_E_";
			sequenceKey += i;

			// for each field in entry
			string sequenceKeyField = "";
			
			sequenceKeyField = sequenceKey;
			sequenceKeyField += "_TIMESTAMP";
			PlayerPrefs.SetFloat (sequenceKeyField, e.timestamp);

			sequenceKeyField = sequenceKey;
			sequenceKeyField += "_ACTION";
			PlayerPrefs.SetInt (sequenceKeyField, e.action);

			sequenceKeyField = sequenceKey;
			sequenceKeyField += "_DATA1";
			PlayerPrefs.SetInt (sequenceKeyField, e.data1);

			sequenceKeyField = sequenceKey;
			sequenceKeyField += "_DATA2";
			PlayerPrefs.SetInt (sequenceKeyField, e.data2);

			sequenceKeyField = sequenceKey;
			sequenceKeyField += "_DATA3";
			PlayerPrefs.SetInt (sequenceKeyField, e.data3);
		}

		return true;
	}

	/// <summary>
	/// Determines whether this instance is recording.
	/// </summary>
	/// <returns><c>true</c> if this instance is recording; otherwise, <c>false</c>.</returns>
	public bool IsRecording()
	{
		return recording;
	}

	/// <summary>
	/// Starts recording a sequence.
	/// </summary>
	/// <returns><c>true</c>, if recording was started, <c>false</c> otherwise.</returns>
	/// <param name="sequence">Sequence.</param>
	/// <param name="info">Info.</param>
	public bool StartRecording(string sequenceName, string sequenceInfo)
	{
		// Stop from playing
		currentSequence = ovrLipSyncContextSequencerValues.CurrentSequenceNotSet;

		recording = true;

		recordingSequence = new Sequence(0);
		recordingSequence.name = sequenceName;
		recordingSequence.info = sequenceInfo;

		startTime = Time.time;

		return true;
	}

	/// <summary>
	/// Adds the entry to recording.
	/// </summary>
	/// <returns><c>true</c>, if entry to recording was added, <c>false</c> otherwise.</returns>
	/// <param name="entry">Entry.</param>
	public bool AddEntryToRecording(ref SequenceEntry entry)
	{
		if(recording == false)
			return false;

		SequenceEntry newEntry = entry.Clone();

		// set the timestamp
		newEntry.timestamp = Time.time - startTime;

		recordingSequence.entries.Add(newEntry);
		recordingSequence.numEntries++;

		return true;
	}
	
	/// <summary>
	/// Stops the recording.
	/// </summary>
	/// <returns><c>true</c>, if recording was stoped, <c>false</c> otherwise.</returns>
	public bool StopRecording()
	{
		// We weren't recording to begin with
		if(recording == false)
			return false;

		// Inject a NULL entry so that we can ensure that the sequence time is maintained
		SequenceEntry e = new SequenceEntry(0);
		e.timestamp = Time.time - startTime;
		recordingSequence.entries.Add (e);

		// Go through the list of sequences to see if it exists, and replace
		// look for loaded sequence already in array
		for (int i = 0; i < sequences.Count; i++)
		{
			Sequence s = (Sequence)sequences[i];
			if(s.name == recordingSequence.name)
			{
				sequences.RemoveAt(i);
			}
		}

		// Add to the list
		sequences.Add (recordingSequence.Clone());

		recording = false;

		return true;
	}

	/// <summary>
	/// Determines whether this instance is playing.
	/// </summary>
	/// <returns><c>true</c> if this instance is playing; otherwise, <c>false</c>.</returns>
	public bool IsPlaying()
	{
		if(currentSequence == ovrLipSyncContextSequencerValues.CurrentSequenceNotSet)
			return false;

		return true;
	}

	/// <summary>
	/// Starts the playback.
	/// </summary>
	/// <returns><c>true</c>, if playback was started, <c>false</c> otherwise.</returns>
	/// <param name="sequenceName">Sequence name.</param>
	public bool StartPlayback(string sequenceName)
	{
		// We are recording, which is higher pri then playback. So bail
		if(recording == true)
		{
			Debug.Log ("OVRLipSyncContextSequencer WARNING: Currently recording a sequence.");
			return false;
		}

		// Find the sequence; if it doesn't exist bail..
		for (int i = 0; i < sequences.Count; i++)
		{
			Sequence s = (Sequence)sequences[i];
			if(s.name == sequenceName)
			{
				currentSequence = i;
				currentSequenceEntry = 0;
				startTime = Time.time;
			
				return true;
			}
		}

		Debug.Log ("OVRLipSyncContextSequencer WARNING: " + sequenceName + " does not exist.");

		return false;
	}
	
	/// <summary>
	/// Stops the playback.
	/// </summary>
	/// <returns><c>true</c>, if playback was stoped, <c>false</c> otherwise.</returns>
	public bool StopPlayback()
	{
		if(currentSequence == ovrLipSyncContextSequencerValues.CurrentSequenceNotSet)
		{
			return false;
		}

		currentSequence = ovrLipSyncContextSequencerValues.CurrentSequenceNotSet;
		currentSequenceEntry = 0;

		return true;
	}

	/// <summary>
	/// Updates the playback.
	/// </summary>
	/// <returns><c>true</c>, if playback was updated, <c>false</c> otherwise.</returns>
	/// <param name="entries">Entries.</param>
	public bool UpdatePlayback(ref ArrayList entries)
	{
		// We are not playing any more
		if(IsPlaying() != true)
			return false;

		Sequence seq = (Sequence)sequences[currentSequence];

		// Make sure to stop playing current sequence when we have hit the end
		if(currentSequenceEntry >= seq.entries.Count)
		{
			currentSequence = ovrLipSyncContextSequencerValues.CurrentSequenceNotSet;
			currentSequenceEntry = 0;
			return false;
		}

		float currentTime = Time.time - startTime;

		SequenceEntry e = (SequenceEntry)seq.entries[currentSequenceEntry];
		float entryTime = e.timestamp;

		while(currentTime > entryTime)
		{
			// Do not add a null action, this is to inject 'NOP' values into the
			// stream
			if(e.action != ovrLipSyncContextSequencerValues.NullEntry)
				entries.Add (e.Clone());

			currentSequenceEntry++;

			if(currentSequenceEntry >= seq.entries.Count)
			{
				currentSequence = -1;
				return false;
			}

			// Keep filling entries with valid entries
			e = (SequenceEntry)seq.entries[currentSequenceEntry];
			entryTime = e.timestamp;
		}

		return true;
	}
}

/************************************************************************************
Filename    :   OVRLipSyncContextTextureFlip.cs
Content     :   This bridges the phoneme/viseme output to texture flip targets
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

public class OVRLipSyncContextTextureFlip : MonoBehaviour 
{	
	// PUBLIC

	// Manually assign the material
	public Material material = null;

	// Set the textures for each viseme. We should follow the viseme order as specified
	// by the Phoneme list
	public Texture[] Textures = new Texture[(int)OVRLipSync.ovrLipSyncViseme.Count];

	// The larger the number, the more the previous frame will influence the output 
	// Works like a low-pass filter
	public float smoothing = 0.0f;

	// PRIVATE

	// Look for a Phoneme Context (should be set at the same level as this component)
	private OVRLipSyncContext phonemeContext = null;

	// data that will be queried and passed between phoneme and renderer each frame
	private OVRLipSync.ovrLipSyncFrame frame = new OVRLipSync.ovrLipSyncFrame(0);
	
	// Capture the old viseme frame (we will write back into this one) 
	private OVRLipSync.ovrLipSyncFrame oldFrame = new OVRLipSync.ovrLipSyncFrame(0);

	/// <summary>
	/// Start this instance.
	/// </summary>
	void Start () 
	{
		// morph target needs to be set manually; possibly other components will need the same
		if(material == null)
		{
			Debug.Log("LipSyncContextTextureFlip.Start WARNING: Please set required public components!");
			return;
		}

		// make sure there is a phoneme context assigned to this object
		phonemeContext = GetComponent<OVRLipSyncContext>();
		if(phonemeContext == null)
		{
			Debug.Log("LipSyncContextTextureFlip.Start WARNING: No phoneme context component set to object");
		}
	}
	
	/// <summary>
	/// Update this instance.
	/// </summary>
	void Update () 
	{
		if((phonemeContext != null) && (material != null))
		{
			// trap inputs and send signals to phoneme engine for testing purposes

			// get the current viseme frame
			if(phonemeContext.GetCurrentPhonemeFrame(ref frame) == OVRLipSync.ovrLipSyncSuccess)
			{
				// Go through the current and old
				for (int i = 0; i < frame.Visemes.Length; i++)
				{
					oldFrame.Visemes[i] = 
					oldFrame.Visemes[i] * smoothing + 
					frame.Visemes[i] * (1.0f - smoothing); 
				}

				SetVisemeToTexture();
			}
		}
	}

	/// <summary>
	/// Sets the viseme to texture.
	/// </summary>
	void SetVisemeToTexture()
	{

		// This setting will run through all the Visemes, find the
		// one with the greatest amplitude and set it to max value.
		// all other visemes will be set to zero.
		int   gV = -1;
		float gA = 0.0f;
			
		for (int i = 0; i < oldFrame.Visemes.Length; i++)
		{
			if(oldFrame.Visemes[i] > gA)
			{
				gV = i;
				gA = oldFrame.Visemes[i];
			}
		}
			
		if ((gV != -1) && (gV < Textures.Length))
		{
			Texture t = Textures[gV];

			if(t != null)
				material.SetTexture("_MainTex", t);
		}
	}	
}

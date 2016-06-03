/************************************************************************************

Filename    :   OSPManagerEditor.cs
Content     :   OSP manager interface. 
				This script adds editor functionality to OSPManager script.
Created     :   November 19, 2014
Authors     :   Peter Giokaris

Copyright   :   Copyright 2014 Oculus VR, Inc. All Rights reserved.

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
#define CUSTOM_LAYOUT

using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

[CustomEditor(typeof(OSPManager))]

public class OSPManagerEditor : Editor
{
	// target component
	private OSPManager m_Component;

	// highlight color
	Color HColor = Color.yellow;

	// OnEnable
	void OnEnable()
	{
		m_Component = (OSPManager)target;
	}

	// OnDestroy
	void OnDestroy()
	{
	}

	// OnInspectorGUI
	public override void OnInspectorGUI()
	{
		GUI.color = Color.white;
		
		Undo.RecordObject(m_Component, "OSPManager");

		{
#if CUSTOM_LAYOUT
			Separator();
			m_Component.Bypass    			 = EditorGUILayout.Toggle("Bypass",   m_Component.Bypass);

			Separator();
			GUI.color = HColor;
			Label ("Global Scale (Range: 0.00001 - 10000.0f)");
			GUI.color = Color.white;
			m_Component.GlobalScale = EditorGUILayout.FloatField(" ", m_Component.GlobalScale);

			Separator();
			GUI.color = HColor;
			Label ("Gain (Range: -/+24 Decibels)");
			GUI.color = Color.white;
			m_Component.Gain   = EditorGUILayout.FloatField(" ",      m_Component.Gain);

			Separator();
			GUI.color = HColor;
			Label ("EARLY REFLECTIONS");
			GUI.color = Color.white;
			Label ("");
			m_Component.EnableReflections = EditorGUILayout.Toggle("Enable", m_Component.EnableReflections);
			m_Component.EnableReverb = EditorGUILayout.Toggle("Reverb On", m_Component.EnableReverb);
			Label ("");
			GUI.color = HColor;
			Label("Room Dimensions (Range: 1 - 200 meters)");
			GUI.color = Color.white;
			m_Component.Dimensions = EditorGUILayout.Vector3Field("",  m_Component.Dimensions);
			GUI.color = HColor;
			Label("Reflection Values (Range: 0 - 0.97)");
			GUI.color = Color.white;
			m_Component.RK01 = EditorGUILayout.Vector2Field("Left/Right",     m_Component.RK01);
			m_Component.RK23 = EditorGUILayout.Vector2Field("Up/Down",        m_Component.RK23);
			m_Component.RK45 = EditorGUILayout.Vector2Field("Behind/Front",       m_Component.RK45);

			Label ("");
			GUI.color = HColor;

			Label("Falloff Range (0 - 1000000 meters)");
			GUI.color = Color.white;
			m_Component.FalloffNear  = EditorGUILayout.FloatField("Near", m_Component.FalloffNear);
			m_Component.FalloffFar   = EditorGUILayout.FloatField("Far", m_Component.FalloffFar);

			Separator();
			GUI.color = HColor;
			Label ("INVERSE SQUARE ATTENUATION");
			GUI.color = Color.white;
			m_Component.UseInverseSquare = EditorGUILayout.Toggle("Enable", m_Component.UseInverseSquare);

			Separator();

/*
			// Reference GUI Layout fields
			m_Component.VerticalFOV         = EditorGUILayout.FloatField("Vertical FOV", m_Component.VerticalFOV);
			m_Component.NeckPosition 		= EditorGUILayout.Vector3Field("Neck Position", m_Component.NeckPosition);
			m_Component.UsePlayerEyeHeight  = EditorGUILayout.Toggle ("Use Player Eye Height", m_Component.UsePlayerEeHeight);
			m_Component.FollowOrientation   = EditorGUILayout.ObjectField("Follow Orientation", 
																		m_Component.FollowOrientation,
																		typeof(Transform), true) as Transform;
			m_Component.BackgroundColor 	= EditorGUILayout.ColorField("Background Color", m_Component.BackgroundColor);
			OVREditorGUIUtility.Separator();
*/

#else			 
			DrawDefaultInspector ();
#endif
		}

		if (GUI.changed)
		{
			EditorUtility.SetDirty(m_Component);
		}
	}	

	// Utilities, move out of here (or copy over to other editor script)

	// Separator
	void Separator()
	{
		GUI.color = new Color(1, 1, 1, 0.25f);
		GUILayout.Box("", "HorizontalSlider", GUILayout.Height(16));
		GUI.color = Color.white;
	}

	// Label
	void Label(string label)
	{
		EditorGUILayout.LabelField (label);
	}

}


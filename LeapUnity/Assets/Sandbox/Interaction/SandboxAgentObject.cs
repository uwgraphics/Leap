using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Sandbox agent game object. This class serves as "glue"
/// between sandbox interface and agent game object logic.
/// </summary>
public class SandboxAgentObject : SandboxObject
{
	// TODO: need a smarter way to execute actions, and add new ones;
	// support that in a generic way in AnimController class
	
	/// <summary>
	/// Default action to perform when none is specified.
	/// </summary>
	public string defaultAction = "Gaze";
	
	/// <summary>
	/// Gets the list of actions this agent can perform.
	/// </summary>
	/// <returns>
	/// List of actions <see cref="System.String[]"/>
	/// </returns>
	public virtual string[] GetActionList()
	{
		List<string> actions = new List<string>();
		
		if( gameObject.GetComponent<GazeController>() != null )
		{
			actions.Add("Gaze");
			actions.Add("GazeAtCamera");
		}
		if( gameObject.GetComponent<SpeechController>() != null )
		{
			SpeechController spctrl = gameObject.GetComponent<SpeechController>();
			foreach( AudioClip clip in spctrl.speechClips )
				actions.Add( "Speak#" + clip.name );
		}
		
		return actions.ToArray();
	}
	
	/// <summary>
	/// Have the agent perform a specific action.
	/// </summary>
	/// <param name="action">
	/// Action name <see cref="System.String"/>
	/// </param>
	/// <param name="target">
	/// Action target <see cref="GameObject"/>
	/// </param>
	/// <returns>
	/// false if action cannot be performed, true otherwise <see cref="System.Boolean"/>
	/// </returns>
	public virtual bool doAction( string action, GameObject target )
	{
		if( action == "" )
			action = defaultAction;
		
		if( action == "Gaze" && gameObject.GetComponent<GazeController>() != null &&
		   target != null )
		{
			gameObject.GetComponent<GazeController>().GazeAt(target);
			
			return true;
		}
		else if( action == "GazeAtCamera" && gameObject.GetComponent<GazeController>() != null &&
		        GameObject.FindWithTag("MainCamera") != null && target != null )
		{
			gameObject.GetComponent<GazeController>().GazeAt( GameObject.FindWithTag("MainCamera") );
		}
		else if( action.StartsWith("Speak#") && gameObject.GetComponent<SpeechController>() != null )
		{
			SpeechController spctrl = gameObject.GetComponent<SpeechController>();
			string clip_name = action.Substring("Speak#".Length);
			bool has_clip = false;
			
			foreach( AudioClip clip in spctrl.speechClips )
			{
				if( clip.name == clip_name )
				{
					has_clip = true;
					break;
				}
			}
			
			if(has_clip)
			{
				spctrl.Speak(clip_name);
				
				return true;
			}
		}
		
		return false;
	}
	
}

using UnityEngine;
using System.Collections;

/// <summary>
/// Class for drawing "eye lasers", which indicate direction
/// of the agent's gaze for debugging purposes.
/// </summary>
[RequireComponent (typeof(GazeController))]
public class EyeLaserGizmo : MonoBehaviour
{
	
	/// <summary>
	/// If true, eye lasers are shown.
	/// </summary>
	public bool show = true;
	
	/// <summary>
	/// Color of the head laser. 
	/// </summary>
	public Color headLaserColor = Color.green;
	
	/// <summary>
	/// Color of the eye lasers.
	/// </summary>
	public Color eyeLaserColor = Color.red;
	
	/// <summary>
	/// Color of lasers for other joints.
	/// </summary>
	public Color otherLaserColor = Color.blue;

	void OnDrawGizmos()
	{
		if( !show )
			return;
		
		GazeController gctrl = GetComponent<GazeController>();
		if( gctrl == null ||
		   gctrl.LEye == null )
			return;
		
		// Draw head laser
		if( gctrl.eyes.Length >= 2 )
		{
			Vector3 head_pos = ( gctrl.LEye.bone.position + gctrl.REye.bone.position )/2.0f;
			Vector3 head_dir = gctrl.Head.FaceDirection;
			Gizmos.color = headLaserColor;
			Gizmos.DrawRay( head_pos, head_dir );
		}
		
		// Draw lasers for other gaze joints
		for (int i = 0; i < gctrl.gazeJoints.Length; ++i)
		{
			GazeJoint joint = gctrl.gazeJoints[i];
			Vector3 pos = joint.bone.position;
			Vector3 dir = joint.FaceDirection;
			if(joint.IsEye)
				Gizmos.color = eyeLaserColor;
			else
				Gizmos.color = otherLaserColor;
			Gizmos.DrawRay( pos, dir );
		}
	}
	
}


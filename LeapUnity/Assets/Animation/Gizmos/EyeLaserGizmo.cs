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
	/// If true, single eye laser is shown from the centroid of the eyes.
	/// </summary>
	public bool showGazeLaser = true;
    
    /// <summary>
    /// If true, individual eye lasers are shown from each eye.
    /// </summary>
    public bool showEyeLasers = false;

    /// <summary>
    /// If true, eye laser is shown from the head.
    /// </summary>
    public bool showHeadLaser = false;

    /// <summary>
    /// If true, eye laser is shown from the torso.
    /// </summary>
    public bool showTorsoLaser = false;

	public Color gazeLaserColor = Color.magenta;
    public Color eyeLaserColor = Color.red;
    public Color headLaserColor = Color.blue;
    public Color torsoLaserColor = Color.blue;
	
	private void OnDrawGizmos()
	{
		GazeController gctrl = GetComponent<GazeController>();
		if( gctrl == null)
			return;

        Vector3 eyeCentroid = 0.5f*(gctrl.LEye.bone.position + gctrl.REye.bone.position);
        Vector3 gazeDir = (0.5f*(gctrl.LEye.Direction + gctrl.REye.Direction)).normalized;
        float gazetargetDist = gctrl.GazeTarget != null ?
            Vector3.Distance(eyeCentroid, gctrl.GazeTarget.transform.position) : 3f;

        if (showGazeLaser)
        {
            Gizmos.color = gazeLaserColor;
            Gizmos.DrawRay(eyeCentroid, gazeDir.normalized * gazetargetDist);
        }

        if (showEyeLasers)
        {
            Gizmos.color = eyeLaserColor;
            Gizmos.DrawRay(gctrl.LEye.bone.position, gctrl.LEye.Direction * gazetargetDist);
            Gizmos.DrawRay(gctrl.REye.bone.position, gctrl.REye.Direction * gazetargetDist);
        }

        if (showHeadLaser)
        {
            Gizmos.color = headLaserColor;
            Gizmos.DrawRay(gctrl.Head.bone.position, gctrl.Head.Direction * gazetargetDist);
        }

        if (showTorsoLaser && gctrl.Torso != null)
        {
            Gizmos.color = torsoLaserColor;
            Gizmos.DrawRay(gctrl.Torso.bone.position, gctrl.Torso.Direction * gazetargetDist);
        }
	}
	
}


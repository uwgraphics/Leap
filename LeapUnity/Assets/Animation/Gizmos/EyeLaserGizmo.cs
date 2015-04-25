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
    /// If true, eye laser is shown from the head.
    /// </summary>
    public bool showHeadLaser = false;

    /// <summary>
    /// If true, eye laser is shown from the torso.
    /// </summary>
    public bool showTorsoLaser = false;

    /// <summary>
    /// if true, eye laser is shown for every gaze joint.
    /// </summary>
    public bool showDetailedLasers = false;

	public Color gazeLaserColor = Color.magenta;
    public Color eyeLaserColor = Color.red;
    public Color headLaserColor = Color.blue;
    public Color torsoLaserColor = Color.blue;

	private void OnDrawGizmos()
	{
		GazeController gazeController = GetComponent<GazeController>();
		if( gazeController == null)
			return;

        Vector3 eyeCentroid = 0.5f*(gazeController.LEye.bone.position + gazeController.REye.bone.position);
        Vector3 gazeDir = (0.5f*(gazeController.LEye.Direction + gazeController.REye.Direction)).normalized;
        float gazetargetDist = gazeController.CurrentGazeTarget != null ?
            Vector3.Distance(eyeCentroid, gazeController.CurrentGazeTarget.transform.position) : 3f;

        if (showGazeLaser)
        {
            Gizmos.color = gazeLaserColor;
            Gizmos.DrawRay(eyeCentroid, gazeDir.normalized * gazetargetDist);
        }

        foreach (GazeJoint gazeJoint in gazeController.gazeJoints)
        {
            if (gazeJoint.IsEye && showDetailedLasers)
            {
                Gizmos.color = eyeLaserColor;
            }
            else if (gazeJoint.type == GazeJointType.Head && showHeadLaser &&
                (showDetailedLasers || gazeJoint == gazeController.Head))
            {
                Gizmos.color = headLaserColor;
            }
            else if (gazeJoint.type == GazeJointType.Torso && showTorsoLaser &&
                (showDetailedLasers || gazeJoint == gazeController.Torso))
            {
                Gizmos.color = torsoLaserColor;
            }
            else
            {
                continue;
            }

            Gizmos.DrawRay(gazeJoint.bone.position, gazeJoint.Direction * gazetargetDist);
        }
	}
}


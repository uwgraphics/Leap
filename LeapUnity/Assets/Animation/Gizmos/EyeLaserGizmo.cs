using UnityEngine;
using System.Collections;

public enum EyeLaserBodyPart
{
    Eyes,
    Head,
    Torso,
    All,
    None
}

/// <summary>
/// Class for drawing "eye lasers", which indicate direction
/// of the agent's gaze for debugging purposes.
/// </summary>
[RequireComponent (typeof(GazeController))]
public class EyeLaserGizmo : MonoBehaviour
{
    /// <summary>
    /// Specifies for which body parts lasers will be rendered.
    /// </summary>
    public EyeLaserBodyPart showLasersForBodyParts;

    /// <summary>
    /// If true, a laser is shown for every gaze joint.
    /// </summary>
    public bool showGazeJointLasers = false;

    /// <summary>
    /// If true, source and target gaze directions are also indicated with lasers.
    /// </summary>
    public bool showGazeShiftLasers = false;

	public Color eyeLaserColor = Color.red;
    public Color headLaserColor = Color.blue;
    public Color torsoLaserColor = Color.magenta;

	private void OnDrawGizmos()
	{
		GazeController gazeController = GetComponent<GazeController>();
		if( gazeController == null)
			return;

        if (showLasersForBodyParts == EyeLaserBodyPart.Eyes || showLasersForBodyParts == EyeLaserBodyPart.All)
        {
            _DrawLasers(gazeController.lEye, eyeLaserColor);
            _DrawLasers(gazeController.rEye, eyeLaserColor);
        }

        if (showLasersForBodyParts == EyeLaserBodyPart.Head || showLasersForBodyParts == EyeLaserBodyPart.All)
        {
            _DrawLasers(gazeController.head, headLaserColor);
        }

        if (showLasersForBodyParts == EyeLaserBodyPart.Torso || showLasersForBodyParts == EyeLaserBodyPart.All)
        {
            _DrawLasers(gazeController.torso, torsoLaserColor);
        }
	}

    private void _DrawLasers(GazeBodyPart gazeBodyPart, Color laserColor)
    {
        GazeController gazeController = GetComponent<GazeController>();
        Vector3 eyeCenter = gazeController.EyeCenter;
        float gazeTargetDist = gazeController.CurrentGazeTarget != null ?
            Vector3.Distance(eyeCenter, gazeController.CurrentGazeTarget.transform.position) : 3f;

        Gizmos.color = laserColor;
        Gizmos.DrawRay(gazeBodyPart.Top.position, gazeBodyPart.Direction * gazeTargetDist);

        if (showGazeJointLasers)
        {
            foreach (var gazeJoint in gazeBodyPart.gazeJoints)
            {
                Gizmos.DrawRay(gazeJoint.position, gazeJoint.forward * gazeTargetDist);
            }
        }

        if (showGazeShiftLasers)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(gazeBodyPart.Position, gazeBodyPart._SourceDirection * gazeTargetDist);
            Gizmos.color = Color.green;
            Gizmos.DrawRay(gazeBodyPart.Position, gazeBodyPart._TargetDirectionAlign * gazeTargetDist);
            Gizmos.color = Color.black;
            Gizmos.DrawRay(gazeBodyPart.Position, gazeBodyPart._TargetDirection * gazeTargetDist);
        }
    }
}


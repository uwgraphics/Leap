using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Class for drawing gizmos for animation editing.
/// </summary>
public class AnimationEditGizmos : MonoBehaviour
{
    /// <summary>
    /// If true, gaze targets will be visually indicated in the scene view.
    /// </summary>
    public bool showGazeTargets = true;

    /// <summary>
    /// If true, active end-effector constraints will be visually indicated in the scene view.
    /// </summary>
    public bool showEndEffectorConstraints = false;

    /// <summary>
    /// End-effectors for which constraints are shown.
    /// </summary>
    public string[] endEffectorTypes = new string[0];

    private Dictionary<Transform, Vector3> _endEffectorConstraints = new Dictionary<Transform, Vector3>();

    // Editor objects use this method to specify end-effector constraints for visualization
    public void _SetEndEffectorConstraint(Transform endEffector)
    {
        _endEffectorConstraints[endEffector] = endEffector.position;
    }

    // Editor objects use this method to unspecify end-effector constraints for visualization
    public void _UnsetEndEffectorConstraint(Transform endEffector)
    {
        _endEffectorConstraints.Remove(endEffector);
    }

    private void OnDrawGizmos()
    {
        if (showGazeTargets)
        {
            _DrawGazeTargets();
        }

        if (showEndEffectorConstraints)
        {
            _DrawEndEffectorConstraints();
        }
    }

    private void _DrawGazeTargets()
    {
        var model = ModelUtils.GetSelectedModel();
        var gazeController = model != null ? model.GetComponent<GazeController>() : null;

        GameObject[] gazeTargets = GameObject.FindGameObjectsWithTag("GazeTarget");
        foreach (var gazeTarget in gazeTargets)
        {
            string icon = "GazeTargetGizmo.png";
            if (gazeController != null)
            {
                if (gazeController._CurrentGazeTarget == gazeTarget)
                    icon = "GazeTargetCurrentGizmo.png";
            }

            Gizmos.DrawIcon(gazeTarget.transform.position, icon, true);
        }

        if (gazeController != null && gazeController.FixGazeTarget != null)
        {
            Gizmos.DrawIcon(gazeController.FixGazeTargetPosition, "GazeTargetFixGizmo.png", true);
        }
    }

    private void _DrawEndEffectorConstraints()
    {
        var model = ModelUtils.GetSelectedModel();
        if (model == null)
            return;
        Transform[] bones = ModelUtils.GetAllBones(model);

        foreach (var bone in bones)
        {
            if (endEffectorTypes.Contains(bone.tag) && _endEffectorConstraints.ContainsKey(bone))
            {
                // Show end-effector constraint
                Vector3 wPos = _endEffectorConstraints[bone];
                Gizmos.DrawIcon(wPos, "EndEffectorConstraintGizmo.png");
            }
        }
    }
}

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
    public bool showEndEffectorGoals = false;

    private Dictionary<Transform, IKGoal> _endEffectorGoals = new Dictionary<Transform, IKGoal>();

    /// <summary>
    /// Set end-effector IK goals so that gizmos can be rendered.
    /// </summary>
    /// <param name="goals">End-effector IK goals</param>
    public void _SetEndEffectorGoals(IKGoal[] goals)
    {
        for (int goalIndex = 0; goalIndex < goals.Length; ++goalIndex)
        {
            _endEffectorGoals[goals[goalIndex].endEffector] = goals[goalIndex];
        }
    }

    /// <summary>
    /// Clear end-effector IK goals so that gizmos are no longer rendered.
    /// </summary>
    public void _ClearEndEffectorGoals()
    {
        _endEffectorGoals.Clear();
    }

    private void OnDrawGizmos()
    {
        if (showGazeTargets)
        {
            _DrawGazeTargets();
        }

        if (showEndEffectorGoals)
        {
            _DrawEndEffectorGoals();
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
                if (gazeController.CurrentGazeTarget == gazeTarget)
                    icon = "GazeTargetCurrentGizmo.png";
            }

            Gizmos.DrawIcon(gazeTarget.transform.position, icon, true);
        }

        if (gazeController != null && gazeController.FixGazeTarget != null)
        {
            Gizmos.DrawIcon(gazeController.FixGazeTarget.transform.position, "GazeTargetFixGizmo.png", true);
        }
    }

    private void _DrawEndEffectorGoals()
    {
        foreach (KeyValuePair<Transform, IKGoal> kvp in _endEffectorGoals)
        {
            Gizmos.DrawIcon(kvp.Value.position, "EndEffectorConstraintGizmo.png");
            // TODO: should also indicate goal orientation and weight
        }
    }
}

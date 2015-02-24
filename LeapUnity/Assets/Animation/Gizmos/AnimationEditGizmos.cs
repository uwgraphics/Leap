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

        IKSolver[] solvers = model.GetComponents<IKSolver>();
        HashSet<Transform> endEffectorsShown = new HashSet<Transform>();
        foreach (var solver in solvers)
        {
            if (!solver.enabled)
                continue;

            foreach (var goal in solver.Goals)
            {
                if (!endEffectorsShown.Contains(goal.endEffector))
                {
                    // Show end-effector constraint
                    Gizmos.DrawIcon(goal.position, "EndEffectorConstraintGizmo.png");
                    endEffectorsShown.Add(goal.endEffector);
                    // TODO: if goal weight < 0, we shouldn't render the gizmo
                    // however, for some idiotic reason OnDrawGizmos() gets called before
                    // LeapAnimationEditor.Update(), so IK goals aren't updated yet
                }
            }
        }
    }
}

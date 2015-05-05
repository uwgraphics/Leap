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
    /// If true, sequence of gaze shifts will be visually indicated.
    /// </summary>
    public bool showGazeSequence = true;

    /// <summary>
    /// If true, gaze targets will be visually indicated in the scene view.
    /// </summary>
    public bool showGazeTargets = true;

    /// <summary>
    /// If true, active end-effector constraints will be visually indicated in the scene view.
    /// </summary>
    public bool showEndEffectorGoals = false;

    private List<Vector3> _gazeTargetSequence;
    private int _currentGazeIndex = -1;
    private Dictionary<Transform, IKGoal> _endEffectorGoals = new Dictionary<Transform, IKGoal>();

    // Set gaze target sequence so that gaze shifts can be visually indicated
    public void _SetGazeSequence(Vector3[] gazeTargetSequence, int currentGazeIndex = -1)
    {
        _ClearGazeSequence();
        _gazeTargetSequence.AddRange(gazeTargetSequence);
        _currentGazeIndex = currentGazeIndex;
    }

    // Clear gaze target sequence so that gaze shifts are no longer visually indicated
    public void _ClearGazeSequence()
    {
        _gazeTargetSequence.Clear();
        _currentGazeIndex = -1;
    }

    // Set end-effector IK goals so that gizmos can be rendered
    public void _SetEndEffectorGoals(IKGoal[] goals)
    {
        for (int goalIndex = 0; goalIndex < goals.Length; ++goalIndex)
        {
            _endEffectorGoals[goals[goalIndex].endEffector] = goals[goalIndex];
        }
    }

    // Clear end-effector IK goals so that gizmos are no longer rendered
    public void _ClearEndEffectorGoals()
    {
        _endEffectorGoals.Clear();
    }

    private void OnDrawGizmos()
    {
        if (showGazeSequence)
        {
            _DrawGazeSequence();
        }

        if (showGazeTargets)
        {
            _DrawGazeTargets();
        }

        if (showEndEffectorGoals)
        {
            _DrawEndEffectorGoals();
        }
    }

    private void _DrawGazeSequence()
    {
        for (int gazeIndex = 1; gazeIndex < _gazeTargetSequence.Count; ++gazeIndex)
        {
            Gizmos.color = gazeIndex == _currentGazeIndex ? new Color(0.8f, 0f, 0f) : Color.black;
            Gizmos.DrawLine(_gazeTargetSequence[gazeIndex - 1], _gazeTargetSequence[gazeIndex]);
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

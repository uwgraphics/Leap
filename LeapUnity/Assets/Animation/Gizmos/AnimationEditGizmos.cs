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

    private List<GameObject> _gazeTargets = new List<GameObject>();
    private int _currentGazeTargetIndex = -1;
    private bool _currentIsFixated = false;
    private List<Vector3> _gazeTargetSequence = new List<Vector3>();
    private int _currentGazeIndex = -1;
    private Dictionary<Transform, IKGoal> _endEffectorGoals = new Dictionary<Transform, IKGoal>();

    // Set gaze targets in the scene that a character can look at
    public void _SetGazeTargets(GameObject[] gazeTargets, int currentGazeTargetIndex = -1, bool currentIsFixated = false)
    {
        _ClearGazeTargets();
        _gazeTargets.AddRange(gazeTargets);
        _currentGazeTargetIndex = currentGazeTargetIndex;
        _currentIsFixated = currentIsFixated;
    }

    // Clear gaze target sequence so that gaze shifts are no longer visually indicated
    public void _ClearGazeTargets()
    {
        _gazeTargets.Clear();
        _currentGazeTargetIndex = -1;
    }

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

    // Notify when mouse has been clicked in the scene view and detect which gaze target
    // has been selected (if any)
    public GameObject _OnSelectGazeTarget(Camera camera, Vector3 mousePosition)
    {
        foreach (var gazeTarget in _gazeTargets)
        {
            Vector3 gazeTargetPosition = camera.WorldToScreenPoint(gazeTarget.transform.position);
            gazeTargetPosition = new Vector3(gazeTargetPosition.x, camera.pixelHeight - gazeTargetPosition.y, 0f);

            if (Mathf.Abs(mousePosition.x - gazeTargetPosition.x) <= 8 &&
                Mathf.Abs(mousePosition.y - gazeTargetPosition.y) <= 8)
            {
                return gazeTarget;
            }
        }

        return null;
    }

    // Clear end-effector IK goals so that gizmos are no longer rendered
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

        if (showGazeSequence)
        {
            _DrawGazeSequence();
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
            Vector3 p1 = _gazeTargetSequence[gazeIndex - 1];
            Vector3 p2 = _gazeTargetSequence[gazeIndex];

            // Draw gaze shift line
            Gizmos.color = gazeIndex == _currentGazeIndex ? new Color(0.8f, 0f, 0f) : Color.black;
            Gizmos.DrawLine(p1, p2);

            // Draw gaze shift direction arrow
            Matrix4x4 curMat = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(p2,
                Quaternion.FromToRotation(Vector3.forward, (p1 - p2).normalized),
                Vector3.one);
            Gizmos.DrawFrustum(new Vector3(0f, 0f, 0f), 45f, 0.2f, 0f, 1f);
            Gizmos.matrix = curMat;
        }
    }

    private void _DrawGazeTargets()
    {
        for (int gazeTargetIndex = 0; gazeTargetIndex < _gazeTargets.Count; ++gazeTargetIndex)
        {
            string icon = "GazeTargetGizmo.png";
            if (gazeTargetIndex == _currentGazeTargetIndex)
            {
                icon = _currentIsFixated ? "GazeTargetFixGizmo.png" : "GazeTargetCurrentGizmo.png";
            }

            Gizmos.DrawIcon(_gazeTargets[gazeTargetIndex].transform.position, icon, false);
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

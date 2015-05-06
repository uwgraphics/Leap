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
    public struct EyeGazeInstanceDesc
    {
        public Vector3 targetPosition;
        public float headAlign;
        public float torsoAlign;
        
        public EyeGazeInstanceDesc(Vector3 targetPosition, float headAlign, float torsoAlign)
        {
            this.targetPosition = targetPosition;
            this.headAlign = headAlign;
            this.torsoAlign = torsoAlign;
        }
    }

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
    private List<EyeGazeInstanceDesc> _gazeSequence = new List<EyeGazeInstanceDesc>();
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

    // Set gaze sequence so that gaze shifts can be visually indicated and edited
    public void _SetGazeSequence(EyeGazeInstanceDesc[] gazeSequence, int currentGazeIndex = -1)
    {
        _ClearGazeSequence();
        _gazeSequence.AddRange(gazeSequence);
        _currentGazeIndex = currentGazeIndex;
    }

    // Clear gaze target sequence so that gaze shifts are no longer visually indicated
    public void _ClearGazeSequence()
    {
        _gazeSequence.Clear();
        _currentGazeIndex = -1;
    }

    /// <summary>
    /// Get the line connecting the previous and next gaze target in the current gaze shift.
    /// </summary>
    /// <param name="targetPos1">Previous gaze target position</param>
    /// <param name="targetPos2">Next gaze target position</param>
    public void _GetCurrentGazeTargetLine(out Vector3 targetPos1, out Vector3 targetPos2)
    {
        if (_currentGazeIndex <= 0)
            throw new System.Exception("There is no previous gaze target");

        targetPos1 = _gazeSequence[_currentGazeIndex - 1].targetPosition;
        targetPos2 = _gazeSequence[_currentGazeIndex].targetPosition;
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
    public GameObject _OnSelectGazeTarget(Vector3 mousePosition)
    {
        foreach (var gazeTarget in _gazeTargets)
        {
            Vector3 gazeTargetPosition = Camera.current.WorldToScreenPoint(gazeTarget.transform.position);
            gazeTargetPosition = new Vector3(gazeTargetPosition.x, Camera.current.pixelHeight - gazeTargetPosition.y, 0f);

            if (Mathf.Abs(mousePosition.x - gazeTargetPosition.x) <= 8 &&
                Mathf.Abs(mousePosition.y - gazeTargetPosition.y) <= 8)
            {
                return gazeTarget;
            }
        }

        return null;
    }

    // Notify when mouse has been clicked in the scene view and detect if
    // head alignment marker is being manipulated
    public bool _OnChangeGazeHeadAlign(Vector3 mousePosition)
    {
        if (_currentGazeIndex <= 0)
            return false;

        Vector3 p1 = _gazeSequence[_currentGazeIndex - 1].targetPosition;
        Vector3 p2 = _gazeSequence[_currentGazeIndex].targetPosition;
        Vector3 headAlignMarkerPos = p1 + (p2 - p1) * _gazeSequence[_currentGazeIndex].headAlign;
        headAlignMarkerPos = Camera.current.WorldToScreenPoint(headAlignMarkerPos);
        headAlignMarkerPos = new Vector3(headAlignMarkerPos.x, Camera.current.pixelHeight - headAlignMarkerPos.y, 0f);

        if (Mathf.Abs(mousePosition.x - headAlignMarkerPos.x) <= 8 &&
            Mathf.Abs(mousePosition.y - headAlignMarkerPos.y) <= 8)
        {
            Debug.LogWarning("Clicked on head align marker");
            return true;
        }

        return false;
    }

    // Notify when mouse has been clicked in the scene view and detect if
    // torso alignment marker is being manipulated
    public bool _OnChangeGazeTorsoAlign(Vector3 mousePosition)
    {
        if (_currentGazeIndex <= 0)
            return false;

        Vector3 p1 = _gazeSequence[_currentGazeIndex - 1].targetPosition;
        Vector3 p2 = _gazeSequence[_currentGazeIndex].targetPosition;
        Vector3 torsoAlignMarkerPos = p1 + (p2 - p1) * _gazeSequence[_currentGazeIndex].torsoAlign;
        torsoAlignMarkerPos = Camera.current.WorldToScreenPoint(torsoAlignMarkerPos);
        torsoAlignMarkerPos = new Vector3(torsoAlignMarkerPos.x, Camera.current.pixelHeight - torsoAlignMarkerPos.y, 0f);

        if (Mathf.Abs(mousePosition.x - torsoAlignMarkerPos.x) <= 8 &&
            Mathf.Abs(mousePosition.y - torsoAlignMarkerPos.y) <= 8)
        {
            return true;
        }

        return false;
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
        for (int gazeIndex = 1; gazeIndex < _gazeSequence.Count; ++gazeIndex)
        {
            Vector3 p1 = _gazeSequence[gazeIndex - 1].targetPosition;
            Vector3 p2 = _gazeSequence[gazeIndex].targetPosition;

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

            if (gazeIndex == _currentGazeIndex)
            {
                // Draw head alignment marker
                float headAlign = Mathf.Clamp01(_gazeSequence[gazeIndex].headAlign);
                Vector3 headAlignMarkerPos = p1 + (p2 - p1) * headAlign;
                Gizmos.DrawIcon(headAlignMarkerPos, "GazeHeadAlignGizmo.png");

                // Draw torso alignment marker
                float torsoAlign = Mathf.Clamp01(_gazeSequence[gazeIndex].torsoAlign);
                Vector3 torsoAlignMarkerPos = p1 + (p2 - p1) * torsoAlign;
                Gizmos.DrawIcon(torsoAlignMarkerPos, "GazeTorsoAlignGizmo.png");
            }
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

using UnityEngine;
using UnityEditor;
using System.Collections;

/// <summary>
/// Class for drawing gizmos for animation editing.
/// </summary>
public class AnimationEditGizmos : MonoBehaviour
{
    /// <summary>
    /// If true, gaze targets will be visually indicated in scene view.
    /// </summary>
    public bool showGazeTargets = true;

    public Color gazeTargetColor = Color.red;

    private void OnDrawGizmos()
    {
        GameObject[] gazeTargets = GameObject.FindGameObjectsWithTag("GazeTarget");
        foreach (var gazeTarget in gazeTargets)
        {
            string icon = "GazeTargetGizmo.png";
            // TODO: use different icon for current gaze target

            Gizmos.DrawIcon(gazeTarget.transform.position, icon, true);
        }
    }
}

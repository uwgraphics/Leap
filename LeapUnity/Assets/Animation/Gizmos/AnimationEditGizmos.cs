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

    private void OnDrawGizmos()
    {
        if (showGazeTargets)
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

            if (gazeController != null)
            {
                Gizmos.DrawIcon(gazeController.FixGazeTargetPosition, "GazeTargetFixGizmo.png", true);
            }
        }
    }
}

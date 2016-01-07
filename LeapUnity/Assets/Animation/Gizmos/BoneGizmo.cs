using UnityEngine;
using System.Collections;
using System.Linq;

/// <summary>
/// Class for drawing bone visualization gizmos.
/// </summary>
public class BoneGizmo : MonoBehaviour
{
    public Transform bone = null;

    private void OnDrawGizmos()
    {
        // Update gizmo position and scale
        Vector3 gizmoPosition = 0.5f * bone.localPosition;
        Quaternion gizmoRotation = Quaternion.FromToRotation(new Vector3(0f, 1f, 0f), bone.localPosition.normalized);
        Vector3 gizmoScale = new Vector3(LEAPCore.boneGizmoScale, bone.localPosition.magnitude, LEAPCore.boneGizmoScale);
        transform.localPosition = gizmoPosition;
        transform.localRotation = gizmoRotation;
        transform.localScale = gizmoScale;
    }
}


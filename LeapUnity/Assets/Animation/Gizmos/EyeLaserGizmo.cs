using UnityEngine;
using System.Collections;

/// <summary>
/// Class for drawing "eye lasers", which indicate direction
/// of the agent's gaze for debugging purposes.
/// </summary>
[RequireComponent (typeof(GazeController))]
public class EyeLaserGizmo : MonoBehaviour
{
    /// <summary>
	/// If true, single eye laser is shown from the centroid of the eyes.
	/// </summary>
	public bool showGazeLaser = true;
    
    /// <summary>
    /// If true, individual eye lasers are shown from each eye.
    /// </summary>
    public bool showEyeLasers = false;

    /// <summary>
    /// If true, eye laser is shown from the head.
    /// </summary>
    public bool showHeadLaser = false;

    /// <summary>
    /// If true, eye laser is shown from the torso.
    /// </summary>
    public bool showTorsoLaser = false;

	public Color gazeLaserColor = Color.magenta;
    public Color eyeLaserColor = Color.red;
    public Color headLaserColor = Color.blue;
    public Color torsoLaserColor = Color.blue;

    // TODO: remove this
    /*public Vector3 lWristPosW0 = Vector3.zero;
    public Vector3 lWristPosW1 = Vector3.zero;
    public Vector3 lWristPosW2 = Vector3.zero;
    public Vector3 ndLWristPosW2x = Vector3.zero;
    public Vector3 ndLWristPosW2y = Vector3.zero;
    public Vector3 ndLWristPosW2z = Vector3.zero;
    public Vector3 adLWristPosW2x = Vector3.zero;
    public Vector3 adLWristPosW2y = Vector3.zero;
    public Vector3 adLWristPosW2z = Vector3.zero;

    private Vector3 prevLForearmRot = Vector3.zero;*/
    //
	private void OnDrawGizmos()
	{
        // TODO: remove this
        /*GameObject model = ModelUtils.GetSelectedModel();
        if (model == null)
            return;
        Transform root = ModelUtils.FindRootBone(model);
        Transform lWrist = ModelUtils.FindBoneWithTag(root, "LWrist");
        Transform lForearm = lWrist.parent;
        Transform lElbow = lForearm.parent;
        Transform lUpperArm = lElbow.parent;
        float scale = 0.199f;

        // 1. Get Unity-computed wrist position
        lWristPosW0 = lWrist.position;

        // 2. Get wrist position computed using quaternions
        lWristPosW1 = lUpperArm.rotation * (scale * (
            lElbow.localRotation * lForearm.localRotation * lWrist.localPosition +
            lElbow.localRotation * lForearm.localPosition +
            lElbow.localPosition
            ))
            + lUpperArm.position;

        // Get previous value of wrist position
        Vector3 prevLWristPosW2 = lWristPosW2;

        // 3. Get wrist position computed using rotation vectors
        Vector3 lUpperArmRotW = QuaternionUtil.Log(lUpperArm.rotation);
        Vector3 lForearmRot = QuaternionUtil.Log(lForearm.localRotation);
        Vector3 lElbowRot = QuaternionUtil.Log(lElbow.localRotation);
        lWristPosW2 = lUpperArm.rotation * (scale * (
            QuaternionUtil.Exp(lForearmRot + lElbowRot) * lWrist.localPosition +
            QuaternionUtil.Exp(lElbowRot) * lForearm.localPosition +
            lElbow.localPosition
            ))
            + lUpperArm.position;

        // Numerically estimate 1st-order derivative of wrist position
        Vector3 dLForearmRot = lForearmRot - prevLForearmRot;
        Vector3 dLWristPosW2 = lWristPosW2 - prevLWristPosW2;
        ndLWristPosW2x = dLForearmRot.x > 0f ?
            new Vector3(dLWristPosW2.x / dLForearmRot.x,
                dLWristPosW2.y / dLForearmRot.x,
                dLWristPosW2.z / dLForearmRot.x) : Vector3.zero;
        ndLWristPosW2y = dLForearmRot.y > 0f ?
            new Vector3(dLWristPosW2.x / dLForearmRot.y,
                dLWristPosW2.y / dLForearmRot.y,
                dLWristPosW2.z / dLForearmRot.y) : Vector3.zero;
        ndLWristPosW2z = dLForearmRot.z > 0f ?
            new Vector3(dLWristPosW2.x / dLForearmRot.z,
                dLWristPosW2.y / dLForearmRot.z,
                dLWristPosW2.z / dLForearmRot.z) : Vector3.zero;

        // Analytically compute 1st-order derivative of wrist position
        adLWristPosW2x = lUpperArm.rotation * (scale * (
            QuaternionUtil.DExp(lForearmRot + lElbowRot, 0) * lWrist.localPosition
            ));
        adLWristPosW2y = lUpperArm.rotation * (scale * (
            QuaternionUtil.DExp(lForearmRot + lElbowRot, 1) * lWrist.localPosition
            ));
        adLWristPosW2z = lUpperArm.rotation * (scale * (
            QuaternionUtil.DExp(lForearmRot + lElbowRot, 2) * lWrist.localPosition
            ));

        // Store previous values of rotations/positions
        prevLForearmRot = lForearmRot;

        // Test DExp
        Quaternion q1 = Quaternion.Euler(new Vector3(30f, 30f, 30f));
        Quaternion q2 = Quaternion.Euler(new Vector3(31f, 32f, 33f));
        Vector3 v1 = QuaternionUtil.Log(q1);
        Vector3 v2 = QuaternionUtil.Log(q2);
        Quaternion dq = Quaternion.Inverse(q1) * q2;
        Vector3 dv = v2 - v1;

        Quaternion ndqx = dv.x > 0.005f ? QuaternionUtil.Mul(dq, 1f / dv.x) : new Quaternion(0f, 0f, 0f, 0f);
        Quaternion ndqy = dv.y > 0.005f ? QuaternionUtil.Mul(dq, 1f / dv.y) : new Quaternion(0f, 0f, 0f, 0f);
        Quaternion ndqz = dv.z > 0.005f ? QuaternionUtil.Mul(dq, 1f / dv.z) : new Quaternion(0f, 0f, 0f, 0f);

        Quaternion adqx = QuaternionUtil.DExp(v2, 0);
        Quaternion adqy = QuaternionUtil.DExp(v2, 1);
        Quaternion adqz = QuaternionUtil.DExp(v2, 2);

        Quaternion nq3 = q2 * QuaternionUtil.Mul(ndqx, dv.x) * QuaternionUtil.Mul(ndqx, dv.y) * QuaternionUtil.Mul(ndqx, dv.z);
        Quaternion aq3 = q2 * QuaternionUtil.Mul(adqx, dv.x) * QuaternionUtil.Mul(adqx, dv.y) * QuaternionUtil.Mul(adqx, dv.z);
        //

        Debug.LogWarning(string.Format("nd: {0}, {1}, {2} \n ad = {3}, {4}, {5}",
            ndLWristPosW2x, ndLWristPosW2y, ndLWristPosW2z, adLWristPosW2x, adLWristPosW2y, adLWristPosW2z));*/
        //
		GazeController gctrl = GetComponent<GazeController>();
		if( gctrl == null)
			return;

        Vector3 eyeCentroid = 0.5f*(gctrl.LEye.bone.position + gctrl.REye.bone.position);
        Vector3 gazeDir = (0.5f*(gctrl.LEye.Direction + gctrl.REye.Direction)).normalized;
        float gazetargetDist = gctrl.CurrentGazeTarget != null ?
            Vector3.Distance(eyeCentroid, gctrl.CurrentGazeTarget.transform.position) : 3f;

        if (showGazeLaser)
        {
            Gizmos.color = gazeLaserColor;
            Gizmos.DrawRay(eyeCentroid, gazeDir.normalized * gazetargetDist);
        }

        if (showEyeLasers)
        {
            Gizmos.color = eyeLaserColor;
            Gizmos.DrawRay(gctrl.LEye.bone.position, gctrl.LEye.Direction * gazetargetDist);
            Gizmos.DrawRay(gctrl.REye.bone.position, gctrl.REye.Direction * gazetargetDist);
        }

        if (showHeadLaser)
        {
            Gizmos.color = headLaserColor;
            Gizmos.DrawRay(gctrl.Head.bone.position, gctrl.Head.Direction * gazetargetDist);
        }

        if (showTorsoLaser && gctrl.Torso != null)
        {
            Gizmos.color = torsoLaserColor;
            Gizmos.DrawRay(gctrl.Torso.bone.position, gctrl.Torso.Direction * gazetargetDist);
        }
	}
}


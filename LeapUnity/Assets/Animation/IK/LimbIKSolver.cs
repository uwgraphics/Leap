using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public enum LimbType
{
    LeftArm,
    RightArm,
    LeftLeg,
    RightLeg
}

/// <summary>
/// Simple, analytical IK solver for 3-joint limbs.
/// </summary>
/// <remarks>Terminology used for joint names in the solver code and comments
/// is based on arm joints, but the solver will work just fine for legs
/// and other limbs.</remarks>
public class LimbIKSolver : IKSolver
{
    /// <summary>
    /// Limb type handled by the current IK solver.
    /// </summary>
    public LimbType limbType = LimbType.LeftArm;
    
    protected Transform shoulder, elbow, wrist;
    protected Vector3 elbowAxis = new Vector3(0f, 1f, 0f);

    /// <summary>
    /// <see cref="IKSolver.Init"/>
    /// </summary>
    public override void Init()
    {
        base.Init();

        shoulder = ModelUtils.FindBoneWithTag(Model.Root, _GetTagForJoint(limbType, 0));
        elbow = ModelUtils.FindBoneWithTag(Model.Root, _GetTagForJoint(limbType, 1));
        wrist = ModelUtils.FindBoneWithTag(Model.Root, _GetTagForJoint(limbType, 2));

        goals = new IKGoal[1];
        goals[0] = new IKGoal(wrist, Vector3.zero, 0f);
    }

    /// <summary>
    /// <see cref="IKSolver.Solve"/>
    /// </summary>
    public override void Solve()
    {
        IKGoal goal = goals.FirstOrDefault(g => g.endEffector == wrist);
        if (goal == null || goal.weight <= 0f)
            // No goal defined for the current limb's end-effector
            return;

        // Store the unadapted pose
	    Quaternion lcl_sq0 = shoulder.localRotation;
        Quaternion sq0 = shoulder.rotation;
        Quaternion lcl_eq0 = elbow.localRotation;
        //Quaternion wq0 = wrist.localRotation;

	    // Flex elbow to make goal achievable
	    Vector3 cur_n = wrist.position - shoulder.position;
	    Vector3 goal_n = goal.position - shoulder.position;
	    float goal_l = goal_n.magnitude; // distance from shoulder to goal
	    Vector3 cur_es = shoulder.position - elbow.position;
	    Vector3 cur_ew = wrist.position - elbow.position;
	    float les = cur_es.magnitude;
	    float lew = cur_ew.magnitude;
	    cur_es.Normalize();
	    cur_ew.Normalize();
	    // If elbow is completely extended or collapsed, we reuse previous rot. axis
	    if (Mathf.Abs(Mathf.Abs(Vector3.Dot(cur_es, cur_ew)) - 1f) > 0.0001f)
		    elbowAxis = Vector3.Cross(cur_es, cur_ew).normalized;
	    float eth = Vector3.Angle(cur_es, cur_ew);
        float goal_coseth = (les * les + lew * lew - goal_l * goal_l) / (2f * les * lew);
        float goal_eth = Mathf.Rad2Deg * Mathf.Acos(Mathf.Clamp(goal_coseth, -1f, 1f));
	    Vector3 lcl_eax =  Quaternion.Inverse(shoulder.rotation) * elbowAxis;
	    elbow.localRotation = Quaternion.AngleAxis(goal_eth-eth, lcl_eax) * elbow.localRotation;

	    // Rotate shoulder to align wrist with goal
        cur_n = (wrist.position - shoulder.position).normalized;
	    goal_n.Normalize();
	    Quaternion spqi = Quaternion.Inverse(shoulder.parent.rotation);
	    Vector3 lcl_curn = spqi * cur_n;
	    Vector3 lcl_goaln = spqi * goal_n;
	    shoulder.localRotation = Quaternion.FromToRotation(lcl_curn, lcl_goaln) * shoulder.localRotation;
	    Quaternion sq = shoulder.rotation;

	    // Compute shoulder swivel angle
	    Quaternion sqref = sq;
	    float a = Quaternion.Dot(sq0, sqref);
	    float b = sqref.w * Vector3.Dot(goal_n, new Vector3(sq0.x, sq0.y, sq0.z)) - 
		    sq0.w * Vector3.Dot(goal_n, new Vector3(sqref.x, sqref.y, sqref.z)) + 
		    Vector3.Dot(new Vector3(sq0.x, sq0.y, sq0.z), Vector3.Cross(goal_n, new Vector3(sqref.x, sqref.y, sqref.z)));
	    float alpha = Mathf.Rad2Deg * Mathf.Atan2(a, b);
	    Quaternion sq1 = Quaternion.AngleAxis(-2f * alpha + Mathf.PI * Mathf.Rad2Deg, goal_n) * sqref;
        Quaternion sq2 = Quaternion.AngleAxis(-2f * alpha - Mathf.PI * Mathf.Rad2Deg, goal_n) * sqref;
		if( Quaternion.Dot(sq0, sq1) > Quaternion.Dot(sq0, sq2))
			sq = sq1;
		else
			sq = sq2;
        shoulder.localRotation = Quaternion.Inverse(shoulder.parent.rotation) * sq;

	    // TODO: fix wrist/ankle

	    // Blend based on goal weight
	    shoulder.localRotation = Quaternion.Slerp(lcl_sq0, shoulder.localRotation, goal.weight);
	    elbow.localRotation = Quaternion.Slerp(lcl_eq0, elbow.localRotation, goal.weight);
    }

    protected virtual string _GetTagForJoint(LimbType limbType, int jointIndex)
    {
        string sidePrefix, jointName;

        // Determine side prefix
        if (limbType == LimbType.LeftArm || limbType == LimbType.LeftLeg)
            sidePrefix = "L";
        else // if (limbType == LimbType.RightArm || limbType == LimbType.RightLeg)
            sidePrefix = "R";

        // Determine joint name
        if (limbType == LimbType.LeftArm || limbType == LimbType.RightArm)
        {
            switch (jointIndex)
            {
                case 0:
                    jointName = "Shoulder";
                    break;
                case 1:
                    jointName = "Elbow";
                    break;
                case 2:
                    jointName = "Wrist";
                    break;
                default:
                    jointName = "Shoulder";
                    break;
            }
        }
        else // if (limbType == LimbType.LeftLeg || limbType == LimbType.RightLeg)
        {
            switch (jointIndex)
            {
                case 0:
                    jointName = "Hip";
                    break;
                case 1:
                    jointName = "Knee";
                    break;
                case 2:
                    jointName = "Ankle";
                    break;
                default:
                    jointName = "Hip";
                    break;
            }
        }

        return sidePrefix + jointName;
    }
}

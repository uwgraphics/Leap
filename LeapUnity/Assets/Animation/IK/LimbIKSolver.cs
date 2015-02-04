﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Simple, analytical IK solver for 3-joint limbs.
/// </summary>
/// <remarks>Terminology used for joint names in the solver code and comments
/// is based on arm joints, but the solver will work just fine for legs
/// and other limbs.</remarks>
public class LimbIKSolver : IKSolver
{
    protected bool _isLeg = false;
    protected Transform _shoulder, _elbow, _wrist;
    protected Vector3 _elbowAxis = new Vector3(0f, 1f, 0f);

    /// <summary>
    /// <see cref="IKSolver.Init"/>
    /// </summary>
    public override void Init()
    {
        if (endEffectors.Length != 1)
        {
            Debug.LogError("LimbIKSolver incorrectly configured, must specify exactly one end-effector tag");
            return;
        }

        base.Init();

        _isLeg = endEffectors[0] == "LAnkle" || endEffectors[0] == "RAnkle";
        _shoulder = ModelUtils.FindBoneWithTag(Model.Root, GetJointTagForLimb(endEffectors[0], 2));
        _elbow = ModelUtils.FindBoneWithTag(Model.Root, GetJointTagForLimb(endEffectors[0], 1));
        _wrist = ModelUtils.FindBoneWithTag(Model.Root, endEffectors[0]);
    }

    /// <summary>
    /// <see cref="IKSolver.Solve"/>
    /// </summary>
    public override void Solve()
    {
        IKGoal goal = goals.FirstOrDefault(g => g.endEffector == _wrist);
        if (goal == null || goal.weight <= 0f)
            // No goal defined for the current limb's end-effector
            return;

        // Store the unadapted pose
	    Quaternion lcl_sq0 = _shoulder.localRotation;
        Quaternion sq0 = _shoulder.rotation;
        Quaternion lcl_eq0 = _elbow.localRotation;
        Quaternion wq0 = goal.rotation;

	    // Determine if goal is within reach
	    Vector3 cur_n = _wrist.position - _shoulder.position;
	    Vector3 goal_n = goal.position - _shoulder.position;
	    float goal_l = goal_n.magnitude; // distance from shoulder to goal
        Vector3 cur_es = _shoulder.position - _elbow.position;
        Vector3 cur_ew = _wrist.position - _elbow.position;
        float les = cur_es.magnitude;
        float lew = cur_ew.magnitude;
        cur_es.Normalize();
        cur_ew.Normalize();
        // Don't allow the arm to extend completley
        // TODO: body solver should take care of this
        /*if (goal_l >= 0.95f * (les + lew))
        {
            goal_l = 0.95f * (les + lew);
            goal_n = goal_n.normalized * goal_l;

            //Debug.LogWarning(string.Format("Limb {0} cannot reach the goal", endEffectors[0]));
        }*/
        //

        // If elbow is completely extended or collapsed, we reuse previous rot. axis
	    if (Mathf.Abs(Mathf.Abs(Vector3.Dot(cur_es, cur_ew)) - 1f) > 0.0001f)
		    _elbowAxis = Vector3.Cross(cur_es, cur_ew).normalized;
        if (_isLeg)
            _elbowAxis = -_elbowAxis;

        // Flex elbow to make goal achievable
	    float eth = Vector3.Angle(cur_es, cur_ew);
        float goal_coseth = (les * les + lew * lew - goal_l * goal_l) / (2f * les * lew);
        float goal_eth = Mathf.Rad2Deg * Mathf.Acos(Mathf.Clamp(goal_coseth, -1f, 1f));
	    Vector3 lcl_eax =  Quaternion.Inverse(_shoulder.rotation) * _elbowAxis;
	    _elbow.localRotation = Quaternion.AngleAxis(goal_eth-eth, lcl_eax) * _elbow.localRotation;

	    // Rotate shoulder to align wrist with goal
        cur_n = (_wrist.position - _shoulder.position).normalized;
	    goal_n.Normalize();
	    Quaternion spqi = Quaternion.Inverse(_shoulder.parent.rotation);
	    Vector3 lcl_curn = spqi * cur_n;
	    Vector3 lcl_goaln = spqi * goal_n;
	    _shoulder.localRotation = Quaternion.FromToRotation(lcl_curn, lcl_goaln) * _shoulder.localRotation;
	    Quaternion sq = _shoulder.rotation;

	    // Compute shoulder swivel angle
	    Quaternion sqref = sq;
	    float a = Quaternion.Dot(sq0, sqref);
	    float b = sqref.w * Vector3.Dot(goal_n, new Vector3(sq0.x, sq0.y, sq0.z)) - 
		    sq0.w * Vector3.Dot(goal_n, new Vector3(sqref.x, sqref.y, sqref.z)) + 
		    Vector3.Dot(new Vector3(sq0.x, sq0.y, sq0.z), Vector3.Cross(goal_n, new Vector3(sqref.x, sqref.y, sqref.z)));
	    float alpha = Mathf.Rad2Deg * Mathf.Atan2(a, b);
	    Quaternion sq1 = Quaternion.AngleAxis(-2f * alpha + Mathf.PI * Mathf.Rad2Deg, goal_n) * sqref;
        Quaternion sq2 = Quaternion.AngleAxis(-2f * alpha - Mathf.PI * Mathf.Rad2Deg, goal_n) * sqref;
		if (Quaternion.Dot(sq0, sq1) > Quaternion.Dot(sq0, sq2))
			sq = sq1;
		else
			sq = sq2;
        _shoulder.localRotation = Quaternion.Inverse(_shoulder.parent.rotation) * sq;

	    // Blend based on goal weight
	    _shoulder.localRotation = Quaternion.Slerp(lcl_sq0, _shoulder.localRotation, goal.weight);
	    _elbow.localRotation = Quaternion.Slerp(lcl_eq0, _elbow.localRotation, goal.weight);
        if (goal.preserveAbsoluteRotation)
        {
            _wrist.rotation = wq0;
        }
    }

    /// <summary>
    /// Get tag on the joint that is part of the specified end-effector's limb.
    /// </summary>
    /// <param name="endEffector">End effector tag</param>
    /// <param name="jointIndex">Joint index within the limb's chain (0 corresponds to the end-effector)</param>
    /// <returns></returns>
    public static string GetJointTagForLimb(string endEffector, int jointIndex)
    {
        if (endEffector == "LWrist")
        {
            switch (jointIndex)
            {
                case 0:
                    return endEffector;
                case 1:
                    return "LElbow";
                case 2:
                    return "LShoulder";
                default:
                    return endEffector;
            }
        }
        else if (endEffector == "RWrist")
        {
            switch (jointIndex)
            {
                case 0:
                    return endEffector;
                case 1:
                    return "RElbow";
                case 2:
                    return "RShoulder";
                default:
                    return endEffector;
            }
        }
        else if (endEffector == "LAnkle")
        {
            switch (jointIndex)
            {
                case 0:
                    return endEffector;
                case 1:
                    return "LKnee";
                case 2:
                    return "LHip";
                default:
                    return endEffector;
            }
        }
        else // if (endEffector == "RAnkle")
        {
            switch (jointIndex)
            {
                case 0:
                    return endEffector;
                case 1:
                    return "RKnee";
                case 2:
                    return "RHip";
                default:
                    return endEffector;
            }
        }
    }
}

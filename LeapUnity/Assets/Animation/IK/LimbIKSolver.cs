using UnityEngine;
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
    public bool solveSwivelAngle = true;

    protected bool _isLeg = false;
    protected Transform _shoulder, _elbow, _wrist;
    protected Vector3 _elbowAxis = new Vector3(0f, 1f, 0f);

    /// <summary>
    /// <see cref="IKSolver.Start"/>
    /// </summary>
    public override void Start()
    {
        if (endEffectors.Length != 1)
        {
            Debug.LogError("LimbIKSolver incorrectly configured, must specify exactly one end-effector tag");
            return;
        }

        base.Start();

        _isLeg = endEffectors[0] == "LAnkleBone" || endEffectors[0] == "RAnkleBone";
        _shoulder = ModelUtil.FindBoneWithTag(Model.Root, GetJointTagForLimb(endEffectors[0], 2));
        _elbow = ModelUtil.FindBoneWithTag(Model.Root, GetJointTagForLimb(endEffectors[0], 1));
        _wrist = ModelUtil.FindBoneWithTag(Model.Root, endEffectors[0]);
    }

    /// <summary>
    /// <see cref="IKSolver._Solve"/>
    /// </summary>
    protected override void _Solve()
    {
        foreach (var goal in _goals)
        {
            if (goal.weight <= 0f)
                continue;

            // Store the unadapted pose
            Quaternion lqs0 = _shoulder.localRotation;
            Quaternion qs0 = _shoulder.rotation;
            Quaternion lqe0 = _elbow.localRotation;
            Quaternion q0 = goal.rotation;

            // Determine if goal is within reach
            Vector3 v0 = _wrist.position - _shoulder.position;
            Vector3 vGoal = goal.position - _shoulder.position;
            float dGoal = vGoal.magnitude; // distance from shoulder to goal
            Vector3 es = _shoulder.position - _elbow.position;
            Vector3 ew = _wrist.position - _elbow.position;
            float des = es.magnitude;
            float dew = ew.magnitude;
            es.Normalize();
            ew.Normalize();
            // Don't allow the arm to extend completely
            if (dGoal >= LEAPCore.maxLimbExtension * (des + dew))
            {
                dGoal = LEAPCore.maxLimbExtension * (des + dew);
                vGoal = vGoal.normalized * dGoal;

                //Debug.LogWarning(string.Format("Limb {0} cannot reach the goal", endEffectors[0]));
            }

            // If elbow is completely extended or collapsed, we reuse previous rot. axis
            if (Mathf.Abs(Mathf.Abs(Vector3.Dot(es, ew)) - 1f) > 0.0001f)
                _elbowAxis = Vector3.Cross(es, ew).normalized;

            // Flex elbow to make the goal achievable
            float eth = Vector3.Angle(es, ew);
            float cosEthGoal = (des * des + dew * dew - dGoal * dGoal) / (2f * des * dew);
            float ethGoal = Mathf.Rad2Deg * Mathf.Acos(Mathf.Clamp(cosEthGoal, -1f, 1f));
            Vector3 lElbowAxis = Quaternion.Inverse(_shoulder.rotation) * _elbowAxis;
            _elbow.localRotation = Quaternion.AngleAxis(ethGoal - eth, lElbowAxis) * _elbow.localRotation;

            // Rotate shoulder to align wrist with goal
            v0 = _wrist.position - _shoulder.position;
            Vector3 lv0 = _shoulder.InverseTransformDirection(v0).normalized;
            Vector3 lvGoal = _shoulder.InverseTransformDirection(vGoal).normalized;
            Quaternion lqGoal = Quaternion.FromToRotation(lv0, lvGoal);
            //_shoulder.localRotation = lqGoal * _shoulder.localRotation;
            _shoulder.Rotate(Vector3.Cross(v0.normalized, vGoal.normalized).normalized,
                Vector3.Angle(v0.normalized, vGoal.normalized), Space.World);
            Quaternion qs = _shoulder.rotation;

            if (solveSwivelAngle)
            {
                // Compute shoulder swivel angle
                Quaternion qsr = qs;
                float a = Quaternion.Dot(qs0, qsr);
                float b = qsr.w * Vector3.Dot(vGoal, new Vector3(qs0.x, qs0.y, qs0.z)) -
                    qs0.w * Vector3.Dot(vGoal, new Vector3(qsr.x, qsr.y, qsr.z)) +
                    Vector3.Dot(new Vector3(qs0.x, qs0.y, qs0.z), Vector3.Cross(vGoal, new Vector3(qsr.x, qsr.y, qsr.z)));
                float alpha = Mathf.Rad2Deg * Mathf.Atan2(a, b);
                Quaternion qs1 = Quaternion.AngleAxis(-2f * alpha + Mathf.PI * Mathf.Rad2Deg, vGoal) * qsr;
                Quaternion qs2 = Quaternion.AngleAxis(-2f * alpha - Mathf.PI * Mathf.Rad2Deg, vGoal) * qsr;
                if (Quaternion.Dot(qs0, qs1) > Quaternion.Dot(qs0, qs2))
                    qs = qs1;
                else
                    qs = qs2;
                _shoulder.localRotation = Quaternion.Inverse(_shoulder.parent.rotation) * qs;
            }

            // Blend based on goal weight
            _shoulder.localRotation = Quaternion.Slerp(lqs0, _shoulder.localRotation, goal.weight);
            _elbow.localRotation = Quaternion.Slerp(lqe0, _elbow.localRotation, goal.weight);
            if (goal.preserveAbsoluteRotation)
                _wrist.rotation = Quaternion.Slerp(_wrist.rotation, q0, goal.weight);
        }
    }

    /// <summary>
    /// Get tag on the joint that is part of the specified end-effector's limb.
    /// </summary>
    /// <param name="endEffectorTag">End effector tag</param>
    /// <param name="jointIndex">Joint index within the limb's chain (0 corresponds to the end-effector itself)</param>
    /// <returns></returns>
    public static string GetJointTagForLimb(string endEffectorTag, int jointIndex)
    {
        if (endEffectorTag == "LWristBone")
        {
            switch (jointIndex)
            {
                case 0:
                    return endEffectorTag;
                case 1:
                    return "LElbowBone";
                case 2:
                    return "LShoulderBone";
                default:
                    return endEffectorTag;
            }
        }
        else if (endEffectorTag == "RWristBone")
        {
            switch (jointIndex)
            {
                case 0:
                    return endEffectorTag;
                case 1:
                    return "RElbowBone";
                case 2:
                    return "RShoulderBone";
                default:
                    return endEffectorTag;
            }
        }
        else if (endEffectorTag == "LAnkleBone")
        {
            switch (jointIndex)
            {
                case 0:
                    return endEffectorTag;
                case 1:
                    return "LKneeBone";
                case 2:
                    return "LHipBone";
                default:
                    return endEffectorTag;
            }
        }
        else // if (endEffector == "RAnkleBone")
        {
            switch (jointIndex)
            {
                case 0:
                    return endEffectorTag;
                case 1:
                    return "RKneeBone";
                case 2:
                    return "RHipBone";
                default:
                    return endEffectorTag;
            }
        }
    }
}

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

/// <summary>
/// Numerical IK solver for the whole body.
/// </summary>
public class BodyIKSolver : IKSolver
{
    /// <summary>
    /// If true, the solver will only solve for upper body pose,
    /// not affecting the pelvis or legs.
    /// </summary>
    public bool upperBodyOnly = false;

    /// <summary>
    /// If true, solver performance characteristics will be logged.
    /// </summary>
    public bool logPerformance = false;

    /// <summary>
    /// Specifies how important it is to preserve the pose from the original motion.
    /// </summary>
    public float basePoseWeight = 1f;

    /// <summary>
    /// Specifies how important the root position is relative to joint orientations.
    /// </summary>
    public float rootPositionWeight = 1f;

    // Joints the IK solver will manipulate to achieve the goals
    protected List<Transform> _bodyJoints = new List<Transform>();
    protected Dictionary<Transform, int> _bodyJointIndexes = new Dictionary<Transform, int>();
    protected Transform _root = null;
    protected Transform _lShoulder = null;
    protected Transform _rShoulder = null;
    protected Transform _lHip = null;
    protected Transform _rHip = null;

    // Solver data structures:
    protected double[] _x = null;
    protected double[] _s = null;
    protected alglib.minlbfgsstate _state = null;
    protected alglib.minlbfgsreport _rep = null;
    protected double[] _xb = null;

    // Solver results, timing & profiling
    protected Stopwatch _timer = new Stopwatch();
    protected float _goalTermFinal, _basePoseTermFinal;

    /// <summary>
    /// <see cref="IKSolver.Init"/>
    /// </summary>
    public override void Init()
    {
        base.Init();

        _CreateSolver();
    }

    /// <summary>
    /// <see cref="IKSolver.Solve"/>
    /// </summary>
    public override void Solve()
    {
        if (!goals.Any(g => g.weight > 0f))
        {
            // No IK goals set
            return;
        }

        _RunSolver();

        if (logPerformance)
        {
            UnityEngine.Debug.LogWarning(string.Format("Body IK solve: {0} ms, {1} iterations. {2} obj. evals",
                _timer.ElapsedMilliseconds, _rep.iterationscount, _rep.nfev));
            //
            UnityEngine.Debug.LogWarning(string.Format(
                "Objective function: goalTerm = {0}, basePoseTerm = {1}", _goalTermFinal, _basePoseTermFinal));
            //
        }
    }

    protected virtual void _CreateSolver()
    {
        // Get body joints
        _bodyJoints.Clear();
        _root = ModelUtils.FindRootBone(gameObject);
        _lShoulder = ModelUtils.FindBoneWithTag(_root, "LShoulder");
        _rShoulder = ModelUtils.FindBoneWithTag(_root, "RShoulder");
        var lShoulderBoneChain = ModelUtils.GetBoneChain(_root, _lShoulder.parent);
        var rShoulderBoneChain = ModelUtils.GetBoneChain(_root, _rShoulder.parent);
        IEnumerable<Transform> bodyJoints = lShoulderBoneChain.Union(rShoulderBoneChain);
        if (!upperBodyOnly)
        {
            _lHip = ModelUtils.FindBoneWithTag(_root, "LHip");
            _rHip = ModelUtils.FindBoneWithTag(_root, "RHip");
                var lHipBoneChain = ModelUtils.GetBoneChain(_root, _lHip.parent);
            var rHipBoneChain = ModelUtils.GetBoneChain(_root, _rHip.parent);
            bodyJoints = bodyJoints.Union(lHipBoneChain).Union(rHipBoneChain);
        }

        if (!upperBodyOnly)
        {
            // Add root to list of body joints
            _bodyJoints.Add(_root);
            _bodyJointIndexes[_root] = 0;
        }

        // Add all body joints to list
        foreach (var joint in bodyJoints)
        {
            if (joint == _root)
                continue;

            _bodyJoints.Add(joint);
            _bodyJointIndexes[joint] = _bodyJoints.Count - 1;
        }

        // Create solver data structures
        _x = upperBodyOnly ?
            new double[_bodyJoints.Count * 3] :
            new double[_bodyJoints.Count * 3 + 3];
        _xb = new double[_x.Length];
        _s = new double[_x.Length];
        
        // Set scaling coefficients
        for (int xi = 0; xi < _x.Length; ++xi)
        {
            if (upperBodyOnly && xi < 3)
                _s[xi] = 1.0;
            else
                _s[xi] = 1.0;
        }

        // Initialize solver
        alglib.minlbfgscreatef(Math.Min(5, _x.Length), _x, 0.05, out _state);
        alglib.minlbfgssetscale(_state, _s);
        alglib.minlbfgssetcond(_state, 0.05, 0, 0, 10);
        alglib.minlbfgssetprecdefault(_state);
        alglib.minlbfgsoptimize(_state, _ObjFunc, null, null); // dry run
        alglib.minlbfgsresults(_state, out _x, out _rep);
    }

    protected virtual void _RunSolver()
    {
        // Initialize and run solver
        _GetSolverPose(_x);
        alglib.minlbfgsrestartfrom(_state, _x);
        _timer.Reset();
        _timer.Start();
        alglib.minlbfgsoptimize(_state, _ObjFunc, null, null);
        _timer.Stop();
        alglib.minlbfgsresults(_state, out _x, out _rep);

        // Apply solution as the model pose
        _ApplySolverPose(_x);
    }

    protected void _GetSolverPose(double[] x)
    {
        if (!upperBodyOnly)
        {
            // Get root position
            Vector3 p = _bodyJoints[0].position;
            x[0] = p.x;
            x[1] = p.y;
            x[2] = p.z;
        }

        // Get joint orientations
        for (int jointIndex = 0; jointIndex < _bodyJoints.Count; ++jointIndex)
        {
            Vector3 v = QuaternionUtil.Log(_bodyJoints[jointIndex].localRotation);
            _SetBodyJointRotation(x, jointIndex, v);
        }
    }

    protected void _ApplySolverPose(double[] x)
    {
        if (!upperBodyOnly)
        {
            // Get root position
            Vector3 p = new Vector3((float)x[0], (float)x[1], (float)x[2]);
            _bodyJoints[0].position = p;
        }

        // Get joint orientations
        for (int jointIndex = 0; jointIndex < _bodyJoints.Count; ++jointIndex)
        {
            Vector3 v = _GetBodyJointRotation(x, jointIndex);
            _bodyJoints[jointIndex].localRotation = QuaternionUtil.Exp(v);
        }
    }

    protected Vector3 _GetRootPosition(double[] x)
    {
        Vector3 p = new Vector3((float)x[0], (float)x[1], (float)x[2]);
        return p;
    }

    protected void _SetRootPosition(double[] x, Vector3 p)
    {
        x[0] = p.x;
        x[1] = p.y;
        x[2] = p.z;
    }

    protected Vector3 _GetBodyJointRotation(double[] x, int jointIndex)
    {
        int rotStartIndex = upperBodyOnly ? 0 : 3;

        Vector3 v = new Vector3((float)x[rotStartIndex + 3 * jointIndex],
            (float)x[rotStartIndex + 3 * jointIndex + 1],
            (float)x[rotStartIndex + 3 * jointIndex + 2]);

        return v;
    }

    protected void _SetBodyJointRotation(double[] x, int jointIndex, Vector3 v)
    {
        int rotStartIndex = upperBodyOnly ? 0 : 3;

        x[rotStartIndex + 3 * jointIndex] = v.x;
        x[rotStartIndex + 3 * jointIndex + 1] = v.y;
        x[rotStartIndex + 3 * jointIndex + 2] = v.z;
    }

    protected void _ObjFunc(double[] x, ref double func, object obj)
    {
        float goalTerm = 0f;
        float basePoseTerm = 0f;

        // Get base pose
        _GetSolverPose(_xb);

        // Apply current pose
        _ApplySolverPose(x);

        // Compute goal term
        IKGoal goal;
        Transform wrist, shoulder, elbow;
        float limbLength = 0f, goalDistance = 0f, curGoalTerm = 0f;
        for (int goalIndex = 0; goalIndex < goals.Length; ++goalIndex)
        {
            goal = goals[goalIndex];

            if (goal.weight <= 0.005f)
                continue;

            // Compute relaxed limb length
            wrist = goal.endEffector;
            shoulder = ModelUtils.FindBoneWithTag(_root,
                LimbIKSolver.GetJointTagForLimb(goal.endEffector.tag, 2));
            elbow = ModelUtils.FindBoneWithTag(_root,
                LimbIKSolver.GetJointTagForLimb(goal.endEffector.tag, 1));
            limbLength = (shoulder.position - elbow.position).magnitude +
                (wrist.position - elbow.position).magnitude;
            limbLength /= goal.weight;

            // Compute distance beween limb root and goal
            goalDistance = (shoulder.position - goal.position).magnitude;

            if (goalDistance > limbLength)
            {
                // Goal is out of reach, compute goal term
                curGoalTerm = goalDistance - limbLength;
                curGoalTerm *= curGoalTerm;
                goalTerm += curGoalTerm;
            }
        }

        // Compute base pose term
        Vector3 vb, v;
        Quaternion qb, q, dq;
        float curRotTerm = 0f;
        for (int bodyJointIndex = 0; bodyJointIndex < _bodyJoints.Count; ++bodyJointIndex)
        {
            // Compute body joint orientation term
            vb = _GetBodyJointRotation(_xb, bodyJointIndex);
            qb = QuaternionUtil.Exp(vb);
            v = _GetBodyJointRotation(x, bodyJointIndex);
            q = QuaternionUtil.Exp(v);
            dq = Quaternion.Inverse(q) * qb;
            curRotTerm = QuaternionUtil.Log(dq).sqrMagnitude;
            //float curRotTerm = (vb - v).sqrMagnitude;
            basePoseTerm += curRotTerm;
        }
        if (!upperBodyOnly)
        {
            // Also add root position term
            Vector3 p0b = _GetRootPosition(_xb);
            Vector3 p0 = _GetRootPosition(x);
            float rootPosTerm = (p0 - p0b).sqrMagnitude;
            basePoseTerm += (rootPositionWeight * rootPosTerm);
        }

        // Reapply base pose
        _ApplySolverPose(_xb);

        // Compute total objective value
        func = goalTerm + basePoseWeight * basePoseTerm;

        // Log objective values
        _goalTermFinal = goalTerm;
        _basePoseTermFinal = basePoseTerm * basePoseWeight;
    }
}

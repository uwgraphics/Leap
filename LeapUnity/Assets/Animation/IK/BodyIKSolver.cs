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

    // Joints the IK solver will manipulate to achieve the goals
    protected List<Transform> _bodyJoints = new List<Transform>();

    // Solver data structures:
    protected double[] _x = null;
    protected alglib.minlbfgsstate _state = null;
    protected alglib.minlbfgsreport _rep = null;

    // Solver timing & profiling
    protected Stopwatch _timer = new Stopwatch();

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
        _RunSolver();

        if (logPerformance)
        {
            UnityEngine.Debug.LogWarning(string.Format("Body IK solve: {0} ms", _timer.ElapsedMilliseconds));
        }
    }

    protected virtual void _CreateSolver()
    {
        // Get body joints
        var rootBone = ModelUtils.FindRootBone(gameObject);
        var lShoulderBoneChain = ModelUtils.GetBoneChain(rootBone, ModelUtils.FindBoneWithTag(rootBone, "LShoulder").parent);
        var rShoulderBoneChain = ModelUtils.GetBoneChain(rootBone, ModelUtils.FindBoneWithTag(rootBone, "RShoulder").parent);
        IEnumerable<Transform> bodyJoints = lShoulderBoneChain.Union(rShoulderBoneChain);
        if (!upperBodyOnly)
        {
            var lHipBoneChain = ModelUtils.GetBoneChain(rootBone, ModelUtils.FindBoneWithTag(rootBone, "LHip").parent);
            var rHipBoneChain = ModelUtils.GetBoneChain(rootBone, ModelUtils.FindBoneWithTag(rootBone, "RHip").parent);
            bodyJoints = bodyJoints.Union(lHipBoneChain).Union(rHipBoneChain);
        }
        foreach (var joint in bodyJoints)
        {
            if (joint == rootBone && upperBodyOnly)
                continue;

            _bodyJoints.Add(joint);
        }

        // Create solver data structures
        _x = upperBodyOnly ?
            new double[_bodyJoints.Count * 3] :
            new double[_bodyJoints.Count * 3 + 3];
        alglib.minlbfgscreatef(Math.Min(5, _x.Length), _x, 0.005, out _state);
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

    protected virtual void _GetSolverPose(double[] x)
    {
        // TODO: initialize x from joint positions/rotations
    }

    protected virtual void _ApplySolverPose(double[] x)
    {
        // TODO: get joint positions/rotations from x and apply
    }

    protected static void _ObjFunc(double[] x, ref double func, object obj)
    {
        func = 0.0;
    }
}

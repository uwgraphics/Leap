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

    /// <summary>
    /// Specifies how important it is to preserve accurate gaze direction towards the gaze target.
    /// </summary>
    public float gazeDirectionWeight = 1f;

    /// <summary>
    /// Specifies how important it is to preserve gaze velocity towards the gaze target.
    /// </summary>
    public float gazeVelocityWeight = 1f;

    // If true, the solver will only solve for upper body pose, not affecting the pelvis or legs.
    protected bool _upperBodyOnly = false;

    // Gaze animation stuff:
    protected GazeController _gazeController = null;

    // Joints the IK solver will manipulate to achieve the goals:
    protected List<Transform> _bodyJoints = new List<Transform>();
    protected Dictionary<Transform, int> _bodyJointIndexes = new Dictionary<Transform, int>();
    protected Dictionary<Transform, int> _endEffectorIndexes = new Dictionary<Transform, int>();
    protected Transform _root = null;
    protected List<Transform> _limbShoulderJoints = new List<Transform>();
    protected List<Transform> _limbElbowJoints = new List<Transform>();
    protected List<Transform> _limbWristJoints = new List<Transform>();
    protected int _firstGazeJointIndex = 0;
    protected Dictionary<GazeJoint, int> _gazeJointIndexes = new Dictionary<GazeJoint, int>();

    // Solver data structures:
    protected double[] _x = null;
    protected double[] _s = null;
    protected alglib.minlbfgsstate _state = null;
    protected alglib.minlbfgsreport _rep = null;
    protected double[] _xb = null;
    protected double[] _xp = null;

    // Solver results, timing & profiling
    protected Stopwatch _timer = new Stopwatch();
    protected float _goalTermFinal, _basePoseTermFinal, _gazeDirectionTermFinal, _gazeVelocityTermFinal;

    /// <summary>
    /// Set current character pose as the base pose for the IK solver.
    /// </summary>
    /// <remarks>Base pose should be set before solving for the final pose</remarks>
    public virtual void InitBasePose()
    {
        _GetSolverPose(_xb);
    }

    /// <summary>
    /// Initialize current set of parameters from the gaze controller.
    /// </summary>
    public virtual void InitGazeParams()
    {
        // TODO
    }

    /// <summary>
    /// <see cref="IKSolver.Init"/>
    /// </summary>
    public override void Init()
    {
        base.Init();

        _upperBodyOnly = !endEffectors.Any(ee => ee == "LAnkle" || ee == "RAnkle");
        _CreateSolver();
    }

    /// <summary>
    /// <see cref="IKSolver._Solve"/>
    /// </summary>
    protected override void _Solve()
    {
        _RunSolver();

        if (logPerformance)
        {
            //
            /*for (int goalIndex = 0; goalIndex < _goals.Count; ++goalIndex)
            {
                IKGoal goal = _goals[goalIndex];
                UnityEngine.Debug.LogWarning(string.Format("Body IK goal: endEffector = {0}, position = {1}, weight = {2}",
                    goal.endEffector.tag, goal.position, goal.weight));
            }*/
            //
            UnityEngine.Debug.LogWarning(string.Format("Body IK solve: {0} ms, {1} iterations, {2} obj. evals",
                _timer.ElapsedMilliseconds, _rep.iterationscount, _rep.nfev));
            //
            UnityEngine.Debug.LogWarning(string.Format(
                "Objective function: goalTerm = {0}, basePoseTerm = {1}, gazeDirectionTerm = {2}, gazeVelocityTerm = {3}",
                _goalTermFinal, _basePoseTermFinal, _gazeDirectionTermFinal, _gazeVelocityTermFinal));
            //
        }
    }

    protected virtual void _CreateSolver()
    {
        _bodyJoints.Clear();
        _limbWristJoints.Clear();
        _limbElbowJoints.Clear();
        _limbShoulderJoints.Clear();

        // Get root joint
        _root = ModelUtils.FindRootBone(gameObject);
        if (!_upperBodyOnly)
        {
            // Add root to list of body joints
            _bodyJoints.Add(_root);
            _bodyJointIndexes[_root] = 0;
        }

        // Get all distinct body joints
        IEnumerable<Transform> bodyJoints = null;
        for (int endEffectorIndex = 0; endEffectorIndex < endEffectors.Length; ++endEffectorIndex)
        {
            string endEffectorTag = endEffectors[endEffectorIndex];

            var wrist = ModelUtils.FindBoneWithTag(_root, endEffectorTag);
            _endEffectorIndexes[wrist] = endEffectorIndex;
            _limbWristJoints.Add(wrist);
            string elbowTag = LimbIKSolver.GetJointTagForLimb(endEffectorTag, 1);
            _limbElbowJoints.Add(ModelUtils.FindBoneWithTag(_root, elbowTag));
            string shoulderTag = LimbIKSolver.GetJointTagForLimb(endEffectorTag, 2);
            _limbShoulderJoints.Add(ModelUtils.FindBoneWithTag(_root, shoulderTag));

            var shoulderBoneChain = ModelUtils.GetBoneChain(_root, _limbShoulderJoints[_limbShoulderJoints.Count - 1].parent);
            bodyJoints = bodyJoints == null ? shoulderBoneChain : bodyJoints.Union(shoulderBoneChain);
        }

        // Add body joints to the list
        foreach (var joint in bodyJoints)
        {
            if (joint == _root)
                continue;

            _bodyJoints.Add(joint);
            _bodyJointIndexes[joint] = _bodyJoints.Count - 1;
        }

        if (LEAPCore.useGazeIK)
        {
            // Get gaze controller and joints
            _gazeController = gameObject.GetComponent<GazeController>();
            if (_gazeController != null)
            {
                _firstGazeJointIndex = _bodyJoints.Count;

                foreach (var gazeJoint in _gazeController.gazeJoints)
                {
                    if (gazeJoint.type != GazeJointType.Torso)
                    {
                        _bodyJoints.Add(gazeJoint.bone);
                        _bodyJointIndexes[gazeJoint.bone] = _bodyJoints.Count - 1;
                    }
                    
                    _gazeJointIndexes[gazeJoint] = _bodyJoints.IndexOf(gazeJoint.bone);
                }
            }
        }
        
        // Create solver data structures
        _x = _upperBodyOnly ?
            new double[_bodyJoints.Count * 3] :
            new double[_bodyJoints.Count * 3 + 3];
        _xb = new double[_x.Length];
        _xp = new double[_x.Length];
        _s = new double[_x.Length];
        
        // Set scaling coefficients
        for (int xi = 0; xi < _x.Length; ++xi)
        {
            if (_upperBodyOnly && xi < 3)
                _s[xi] = 1.0;
            else
                _s[xi] = 1.0;
        }

        // Initialize solver
        alglib.minlbfgscreatef(Math.Min(5, _x.Length), _x, 0.05, out _state);
        alglib.minlbfgssetscale(_state, _s);
        alglib.minlbfgssetcond(_state, 0.05, 0, 0, 10);
        alglib.minlbfgssetprecdefault(_state);
        if (LEAPCore.useGazeIK)
            alglib.minlbfgsoptimize(_state, _ObjFunc2, null, null); // dry run
        else
            alglib.minlbfgsoptimize(_state, _ObjFunc1, null, null); // dry run
        alglib.minlbfgsresults(_state, out _x, out _rep);
    }

    protected virtual void _RunSolver()
    {
        if (LEAPCore.useGazeIK)
        {
            // Set model to base pose - that's our initial solution
            _ApplySolverPose(_xb);
        }
        else
        {
            // Output from the gaze controller *is* the base pose
            _GetSolverPose(_xb);
        }
        
        // Run solver
        _GetSolverPose(_x);
        alglib.minlbfgsrestartfrom(_state, _x);
        _timer.Reset();
        _timer.Start();
        if (LEAPCore.useGazeIK)
            alglib.minlbfgsoptimize(_state, _ObjFunc2, null, null);
        else
            alglib.minlbfgsoptimize(_state, _ObjFunc1, null, null);
        _timer.Stop();
        alglib.minlbfgsresults(_state, out _x, out _rep);

        // Apply solution as the new model pose
        _ApplySolverPose(_x);

        // Store the current pose as the previous pose for the next frame
        _GetSolverPose(_xp);
    }

    protected void _GetSolverPose(double[] x)
    {
        if (!_upperBodyOnly)
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
        if (!_upperBodyOnly)
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
        int rotStartIndex = _upperBodyOnly ? 0 : 3;

        Vector3 v = new Vector3((float)x[rotStartIndex + 3 * jointIndex],
            (float)x[rotStartIndex + 3 * jointIndex + 1],
            (float)x[rotStartIndex + 3 * jointIndex + 2]);

        return v;
    }

    protected void _SetBodyJointRotation(double[] x, int jointIndex, Vector3 v)
    {
        int rotStartIndex = _upperBodyOnly ? 0 : 3;

        x[rotStartIndex + 3 * jointIndex] = v.x;
        x[rotStartIndex + 3 * jointIndex + 1] = v.y;
        x[rotStartIndex + 3 * jointIndex + 2] = v.z;
    }

    protected void _ObjFunc1(double[] x, ref double func, object obj)
    {
        float goalTerm = 0f;
        float basePoseTerm = 0f;

        // Apply current pose
        _ApplySolverPose(x);

        // Compute goal term
        IKGoal goal;
        Transform wrist, shoulder, elbow;
        float limbLength = 0f, goalDistance = 0f, curGoalTerm = 0f;
        for (int goalIndex = 0; goalIndex < _goals.Count; ++goalIndex)
        {
            goal = _goals[goalIndex];

            if (goal.weight <= 0.005f)
                continue;

            // Compute relaxed limb length
            int endEffectorIndex = _endEffectorIndexes[goal.endEffector];
            wrist = goal.endEffector;
            shoulder = _limbShoulderJoints[endEffectorIndex];
            elbow = _limbElbowJoints[endEffectorIndex];
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
            var bodyJoint = _bodyJoints[bodyJointIndex];

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
        if (!_upperBodyOnly)
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

    protected void _ObjFunc2(double[] x, ref double func, object obj)
    {
        float goalTerm = 0f,
            basePoseTerm = 0f,
            gazeDirectionTerm = 0f,
            gazeVelocityTerm = 0f;

        // Apply current pose
        _ApplySolverPose(x);

        // Compute goal term
        IKGoal goal;
        Transform wrist, shoulder, elbow;
        float limbLength = 0f, goalDistance = 0f, curGoalTerm = 0f;
        for (int goalIndex = 0; goalIndex < _goals.Count; ++goalIndex)
        {
            goal = _goals[goalIndex];

            if (goal.weight <= 0.005f)
                continue;

            // Compute relaxed limb length
            int endEffectorIndex = _endEffectorIndexes[goal.endEffector];
            wrist = goal.endEffector;
            shoulder = _limbShoulderJoints[endEffectorIndex];
            elbow = _limbElbowJoints[endEffectorIndex];
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
        for (int bodyJointIndex = 0; bodyJointIndex < _firstGazeJointIndex; ++bodyJointIndex)
        {
            var bodyJoint = _bodyJoints[bodyJointIndex];

            // Penalize difference between base and current rotation of the joint
            vb = _GetBodyJointRotation(_xb, bodyJointIndex);
            qb = QuaternionUtil.Exp(vb);
            v = _GetBodyJointRotation(x, bodyJointIndex);
            q = QuaternionUtil.Exp(v);
            dq = Quaternion.Inverse(q) * qb;
            curRotTerm = QuaternionUtil.Log(dq).sqrMagnitude;
            //float curRotTerm = (vb - v).sqrMagnitude;
            basePoseTerm += curRotTerm;
        }
        if (!_upperBodyOnly)
        {
            // Also add root position term
            Vector3 p0b = _GetRootPosition(_xb);
            Vector3 p0 = _GetRootPosition(x);
            float rootPosTerm = (p0 - p0b).sqrMagnitude;
            basePoseTerm += (rootPositionWeight * rootPosTerm);
        }

        // Compute gaze direction and velocity terms
        Quaternion qs, qt, qp, qr;
        float r = 0f;
        float dt = _gazeController.DeltaTime;
        Vector3 vp;
        float rv = 0f;
        float curGazeDirectionTerm = 0f, curGazeVelocityTerm = 0f;
        for (int gazeJointIndex = _gazeController.LastGazeJointIndex; gazeJointIndex >= 0; --gazeJointIndex)
        {
            if (_gazeController.CurrentGazeTarget == null)
                continue;

            var gazeJoint = _gazeController.gazeJoints[gazeJointIndex];
            int bodyJointIndex = _gazeJointIndexes[gazeJoint];
            var bodyJoint = _bodyJoints[bodyJointIndex];

            // TODO: try directly penalizing rotational difference from gaze output rotation
            // w/ or w/o expressive displacement applied - might be OK

            // Set source and target rotations and rotation progress from the gaze model
            if (gazeJoint.isVOR)
            {
                
                qs = gazeJoint.fixSrcRot;
                //qt = gazeJoint.fixTrgRotAlign;
                qt = gazeJoint._ComputeTargetRotation(_gazeController.EffGazeTargetPosition);
                r = qs != gazeJoint.fixTrgRot ? GazeJoint.DistanceToRotate(qs, gazeJoint.fixTrgRotAlign) /
                    GazeJoint.DistanceToRotate(qs, gazeJoint.fixTrgRot) : 0f;
                qt = Quaternion.Slerp(qs, qt, r);
                qt = Quaternion.Euler(qt.eulerAngles.x, qt.eulerAngles.y, 0);
                r = gazeJoint.fixRotParamAlign;
            }
            else
            {
                qs = gazeJoint.srcRot;
                //qt = gazeJoint.trgRotAlign;
                qt = gazeJoint._ComputeTargetRotation(_gazeController.EffGazeTargetPosition);
                r = qs != gazeJoint.trgRot ? GazeJoint.DistanceToRotate(qs, gazeJoint.trgRotAlign) /
                    GazeJoint.DistanceToRotate(qs, gazeJoint.trgRot) : 0f;
                qt = Quaternion.Slerp(qs, qt, r);
                qt = Quaternion.Euler(qt.eulerAngles.x, qt.eulerAngles.y, 0);
                r = gazeJoint.rotParamAlign;
            }

            // Compute gaze shift shortest-arc rotation
            qr = Quaternion.Slerp(qs, qt, r);

            // Project current rotation onto that arc
            v = _GetBodyJointRotation(x, bodyJointIndex);
            q = QuaternionUtil.Exp(v);
            //qp = ModelUtils.ProjectRotationOntoArc(gazeJoint.bone, qs, qt, q);

            // Penalize difference between shortest-arc gaze shift rotation and projected current rotation
            //dq = Quaternion.Inverse(qr) * qp;
            dq = Quaternion.Inverse(qr) * q;
            curGazeDirectionTerm = QuaternionUtil.Log(dq).sqrMagnitude;
            gazeDirectionTerm += curGazeDirectionTerm;

            // Compute gaze joint velocity
            /*vp = _GetBodyJointRotation(_xp, bodyJointIndex);
            rv = (v - vp).magnitude / dt;

            // Penalize difference between gaze model velocity and actual velocity
            curGazeVelocityTerm = rv - gazeJoint.curVelocity;
            curGazeVelocityTerm *= curGazeVelocityTerm;
            gazeVelocityTerm += curGazeVelocityTerm;*/
        }

        // Reapply base pose
        _ApplySolverPose(_xb);

        // Compute total objective value
        func = goalTerm + basePoseWeight * basePoseTerm + gazeDirectionWeight * gazeDirectionTerm
            + gazeVelocityWeight * gazeVelocityTerm;

        // Log objective values
        _goalTermFinal = goalTerm;
        _basePoseTermFinal = basePoseTerm * basePoseWeight;
        _gazeDirectionTermFinal = gazeDirectionTerm * gazeDirectionWeight;
        _gazeVelocityTermFinal = gazeVelocityTerm * gazeVelocityWeight;
    }
}

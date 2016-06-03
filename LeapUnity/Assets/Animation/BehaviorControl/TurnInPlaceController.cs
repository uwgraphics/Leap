using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public enum TurnInPlaceState
{
    Static = 0,
    MoveLeftFoot,
    MoveRightFoot
};

/// <summary>
/// Animation controller for turning the character in place.
/// </summary>
public class TurnInPlaceController : AnimController
{
    /// <summary>
    /// Turn angle.
    /// </summary>
    public float turnAngle = 0f;

    /// <summary>
    /// Time in which the turn should be performed (in seconds).
    /// </summary>
    public float turnTimeLength = 1f;

    /// <summary>
    /// If true, the character will perform the turn.
    /// </summary>
    public bool doTurn = false;

    /// <summary>
    /// How much each foot will lift off the ground while turning.
    /// </summary>
    public float footLiftHeight = 0.025f;

    /// <summary>
    /// true if the current turn is a left turn, false otherwise.
    /// </summary>
    public virtual bool IsLeftTurn
    {
        get { return _curTurnAngle < 0f; }
    }

    // Turn-in-place state:
    protected float _curTurnAngle = 0f;
    protected float _curTurnTimeLength = 0f;
    protected float _lFootTurnAngle = 0f;
    protected float _rFootTurnAngle = 0f;
    protected Vector3 _feetCenter;
    protected Vector3 _lFootBasePos, _rFootBasePos;
    protected Quaternion _lFootBaseRot, _rFootBaseRot;
    protected Vector3 _lFootPos, _rFootPos;
    protected Quaternion _lFootRot, _rFootRot;
    protected float _curTurnTime = 0f;

    // Foot joints and IK solvers:
    protected Transform _root, _lFoot, _rFoot;
    protected LimbIKSolver _lFootSolver, _rFootSolver;

    /// <summary>
    /// Initialize base feet pose from the current pose.
    /// </summary>
    public virtual void InitBaseFeetPose()
    {
        if (!enabled)
            return;

        _lFootBasePos = _lFoot.position;
        _lFootBaseRot = _lFoot.rotation;
        _rFootBasePos = _rFoot.position;
        _rFootBaseRot = _rFoot.rotation;
        //_feetCenter = new Vector3(_root.position.x, 0.5f * (_lFoot.position.y + _rFoot.position.y), _root.position.z);
        _feetCenter = 0.5f * (_lFoot.position + _rFoot.position);
    }

    /// <summary>
    /// Perform a turn in place.
    /// </summary>
    /// <param name="angle">Turn angle in degrees; positive for right turn, negative for left turn</param>
    /// <param name="timeLength">Turn time</param>
    public virtual void Turn(float angle, float timeLength = 1f)
    {
        turnAngle = angle;
        turnTimeLength = timeLength;
        doTurn = true;

        Debug.Log(string.Format("Turning in place by {0} degrees in {1} seconds", turnAngle, turnTimeLength));
    }

    public override void Start()
    {
        base.Start();

        // Get feet
        _root = ModelUtil.FindRootBone(gameObject);
        _lFoot = ModelUtil.FindBoneWithTag(_root, "LAnkleBone");
        _rFoot = ModelUtil.FindBoneWithTag(_root, "RAnkleBone");
        if (_lFoot == null || _rFoot == null)
        {
            Debug.LogError("Foot joints not defined on agent " + name);
            enabled = false;
            return;
        }

        // Get IK solvers
        var ikSolvers = gameObject.GetComponents<LimbIKSolver>();
        if (ikSolvers == null)
        {
            Debug.LogError("Foot IK solvers missing on agent " + name);
            enabled = false;
            return;
        }
        _lFootSolver = ikSolvers.FirstOrDefault(s => s.endEffectors.Length == 1 && s.endEffectors[0] == "LAnkleBone");
        _rFootSolver = ikSolvers.FirstOrDefault(s => s.endEffectors.Length == 1 && s.endEffectors[0] == "RAnkleBone");
        if (_lFootSolver == null || _rFootSolver == null)
        {
            Debug.LogError("Left or right foot IK solver missing on agent " + name);
            return;
        }
        _lFootSolver.ClearGoals();
        _rFootSolver.ClearGoals();
        _lFootSolver.AddGoal(new IKGoal(_lFoot, _lFoot.position, _lFoot.rotation, 1f, true));
        _rFootSolver.AddGoal(new IKGoal(_rFoot, _rFoot.position, _rFoot.rotation, 1f, true));

        // TODO:
        // - Get limb IK solvers for left and right foot
        // - Hook this onto the gaze controller
        // - Specify that it executes after the gaze controller
        // - To execute a turn:
        //   - Compute turn rotation center
        //   - Get source left and right foot positions/orientations and root position/orientation
        //   - Compute final left and right foot positions/orientations and root position/orientation
        //   - Perform interpolation of foot positions (along half-ellipses) and orientations (slerp)
        //   - Perform interpolation of root position (linear) and orientation (slerp)
        // - Must keep track of current root translation/rotation, as well as foot translations/rotations
    }

    public override void LateUpdate()
    {
        base.LateUpdate();
    }

    protected virtual void LateUpdate_Static()
    {
        _UpdateStaticFeetPose();
        _UpdateFeetIK();

        if (doTurn)
        {
            _curTurnAngle = turnAngle;
            _curTurnTimeLength = turnTimeLength;
            _curTurnTime = 0f;
            doTurn = false;

            if (IsLeftTurn)
                GoToState((int)TurnInPlaceState.MoveLeftFoot);
            else
                GoToState((int)TurnInPlaceState.MoveRightFoot);
        }
    }

    protected virtual void LateUpdate_MoveLeftFoot()
    {
        _curTurnTime += DeltaTime;
        _UpdateLFootPose();
        _UpdateStaticRFootPose();
        _UpdateFeetIK();

        if (_curTurnTime >= _curTurnTimeLength / 2f)
        {
            // Left foot has finished moving
            if (IsLeftTurn)
                GoToState((int)TurnInPlaceState.MoveRightFoot);
            else
                GoToState((int)TurnInPlaceState.Static);
        }
    }

    protected virtual void LateUpdate_MoveRightFoot()
    {
        _curTurnTime += DeltaTime;
        _UpdateRFootPose();
        _UpdateStaticLFootPose();
        _UpdateFeetIK();

        if (_curTurnTime >= _curTurnTimeLength / 2f)
        {
            // Right foot has finished moving
            if (IsLeftTurn)
                GoToState((int)TurnInPlaceState.Static);
            else
                GoToState((int)TurnInPlaceState.MoveLeftFoot);
        }
    }

    protected virtual void Transition_StaticMoveLeftFoot()
    {
    }

    protected virtual void Transition_StaticMoveRightFoot()
    {
    }

    protected virtual void Transition_MoveLeftRightFoot()
    {
        _curTurnTime = 0f;
        _lFootTurnAngle += _curTurnAngle;
    }

    protected virtual void Transition_MoveLeftFootStatic()
    {
        _lFootTurnAngle += _curTurnAngle;
    }

    protected virtual void Transition_MoveRightLeftFoot()
    {
        _curTurnTime = 0f;
        _rFootTurnAngle += _curTurnAngle;
    }

    protected virtual void Transition_MoveRightFootStatic()
    {
        _rFootTurnAngle += _curTurnAngle;
    }

    protected virtual void _UpdateStaticFeetPose()
    {
        _UpdateStaticLFootPose();
        _UpdateStaticRFootPose();
    }

    // Update current left foot pose
    protected virtual void _UpdateStaticLFootPose()
    {
        _ComputeFootPose(_lFootTurnAngle, _lFootBasePos, _lFootBaseRot, out _lFootPos, out _lFootRot);
    }

    // Update current right foot pose
    protected virtual void _UpdateStaticRFootPose()
    {
        _ComputeFootPose(_rFootTurnAngle, _rFootBasePos, _rFootBaseRot, out _rFootPos, out _rFootRot);
    }

    protected virtual void _UpdateLFootPose()
    {
        _InterpolateFootPose(_lFootTurnAngle, _lFootBasePos, _lFootBaseRot, out _lFootPos, out _lFootRot);
    }

    protected virtual void _UpdateRFootPose()
    {
        _InterpolateFootPose(_rFootTurnAngle, _rFootBasePos, _rFootBaseRot, out _rFootPos, out _rFootRot);
    }

    protected virtual void _ComputeFootPose(float turnAngle, Vector3 basePos, Quaternion baseRot,
        out Vector3 pos, out Quaternion rot)
    {
        Quaternion qt = Quaternion.AngleAxis(turnAngle, Vector3.up);
        rot = qt * baseRot;
        Vector3 vr = basePos - _feetCenter;
        pos = _feetCenter + qt * vr;
    }

    protected virtual void _InterpolateFootPose(float turnAngle, Vector3 basePos, Quaternion baseRot,
        out Vector3 pos, out Quaternion rot)
    {
        // Compute source and target pose
        Vector3 srcPos, trgPos;
        Quaternion srcRot, trgRot;
        _ComputeFootPose(turnAngle, basePos, baseRot, out srcPos, out srcRot);
        _ComputeFootPose(turnAngle + _curTurnAngle, basePos, baseRot, out trgPos, out trgRot);
        float baseHeight = 0.5f * (srcPos.y + trgPos.y);

        // Compute movement progress
        float t = _curTurnTimeLength > 0f ? Mathf.Clamp01(2f * _curTurnTime / _curTurnTimeLength) : 1f;

        // Interpolate foot pose
        pos = Vector3.Lerp(srcPos, trgPos, t);
        rot = Quaternion.Slerp(srcRot, trgRot, t);
        if (t <= 0.5f)
            //pos.y = baseHeight + Mathf.Sqrt(2f * t) * footLiftHeight;
            pos.y = baseHeight + 2f * t * footLiftHeight;
        else
            //pos.y = baseHeight + Mathf.Sqrt(2f * (1f - t)) * footLiftHeight;
            pos.y = baseHeight + 2f * (1f - t) * footLiftHeight;
    }

    protected virtual void _UpdateFeetIK()
    {
        _lFootSolver.UpdateGoal(0, _lFootPos, _lFootRot, 1f);
        _rFootSolver.UpdateGoal(0, _rFootPos, _rFootRot, 1f);
    }

    public override void _CreateStates()
    {
        // Initialize states
        _InitStateDefs<TurnInPlaceState>();
        _InitStateTransDefs((int)TurnInPlaceState.Static, 2);
        _InitStateTransDefs((int)TurnInPlaceState.MoveLeftFoot, 2);
        _InitStateTransDefs((int)TurnInPlaceState.MoveRightFoot, 2);
        states[(int)TurnInPlaceState.Static].lateUpdateHandler = "LateUpdate_Static";
        states[(int)TurnInPlaceState.Static].nextStates[0].nextState = "MoveLeftFoot";
        states[(int)TurnInPlaceState.Static].nextStates[0].transitionHandler = "Transition_StaticMoveLeftFoot";
        states[(int)TurnInPlaceState.Static].nextStates[1].nextState = "MoveRightFoot";
        states[(int)TurnInPlaceState.Static].nextStates[1].transitionHandler = "Transition_StaticMoveRightFoot";
        states[(int)TurnInPlaceState.MoveLeftFoot].lateUpdateHandler = "LateUpdate_MoveLeftFoot";
        states[(int)TurnInPlaceState.MoveLeftFoot].nextStates[0].nextState = "MoveRightFoot";
        states[(int)TurnInPlaceState.MoveLeftFoot].nextStates[0].transitionHandler = "Transition_MoveLeftRightFoot";
        states[(int)TurnInPlaceState.MoveLeftFoot].nextStates[1].nextState = "Static";
        states[(int)TurnInPlaceState.MoveLeftFoot].nextStates[1].transitionHandler = "Transition_MoveLeftFootStatic";
        states[(int)TurnInPlaceState.MoveRightFoot].lateUpdateHandler = "LateUpdate_MoveRightFoot";
        states[(int)TurnInPlaceState.MoveRightFoot].nextStates[0].nextState = "MoveLeftFoot";
        states[(int)TurnInPlaceState.MoveRightFoot].nextStates[0].transitionHandler = "Transition_MoveRightLeftFoot";
        states[(int)TurnInPlaceState.MoveRightFoot].nextStates[1].nextState = "Static";
        states[(int)TurnInPlaceState.MoveRightFoot].nextStates[1].transitionHandler = "Transition_MoveRightFootStatic";
    }
}

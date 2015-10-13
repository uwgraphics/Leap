using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public enum GazeState
{
    NoGaze,
    Shifting
};

public enum GazeBodyPartType
{
    LEye,
    REye,
    Head,
    Torso
};

/// <summary>
/// Snapshot of the runtime state of a gaze controller.
/// </summary>
public struct GazeControllerState : IAnimControllerState
{
    // Runtime state of a gaze body part
    public struct GazeBodyPartState
    {
        public int gazeBodyPartType;
        public float align;
        public float velocity;
        public float inOMR, outOMR, upOMR, downOMR;
        public float postureWeight;
        public float curAlign;
        public float maxVelocity;
        public float curVelocity;
        public float latency;
        public float adjInOMR, adjOutOMR, adjUpOMR, adjDownOMR;
        public float curInOMR, curOutOMR, curUpOMR, curDownOMR;
        public Quaternion[] baseRots;
        public Quaternion[] srcRots;
        public Vector3 srcDir;
        public Vector3 trgDir;
        public Vector3 trgDirAlign;
        public float rotParam;
        public Vector3 curDir;
        public bool isFix;
        public Quaternion[] fixSrcRots;
        public Vector3 fixSrcDir;
        public Vector3 fixTrgDir;
        public Vector3 fixTrgDirAlign;
    }

    public int stateId;
    public GameObject gazeTarget;
    public bool doGazeShift;
    public bool stopGazeShift;
    public bool fixGaze;
    public bool useTorso;
    public float pelvisAlign;
    public float predictability;
    public Vector3 movingTargetPosOff;
    public bool removeRoll;
    public float weight;
    public float fixWeight;
    public GameObject curGazeTarget;
    public GameObject fixGazeTarget;
    public float amplitude;
    public Vector3 curMovingTargetPosOff;
    public bool curUseTorso;
    public bool reenableRandomHeadMotion;
    public bool reenableRandomSpeechMotion;

    public GazeBodyPartState lEyeState;
    public GazeBodyPartState rEyeState;
    public GazeBodyPartState headState;
    public GazeBodyPartState torsoState;
}

/// <summary>
/// Gaze animation controller. 
/// </summary>
public class GazeController : AnimController
{
    /// <summary>
    /// Left eye definition.
    /// </summary>
    public GazeBodyPart lEye;

    /// <summary>
    /// Right eye definition.
    /// </summary>
    public GazeBodyPart rEye;

    /// <summary>
    /// Head definition.
    /// </summary>
    public GazeBodyPart head;

    /// <summary>
    /// Torso definition.
    /// </summary>
    public GazeBodyPart torso;

    /// <summary>
    /// Next gaze target.
    /// </summary>
    public GameObject gazeTarget = null;

    /// <summary>
    /// If true, a gaze shift will begin on next frame.
    /// </summary>
    public bool doGazeShift = false;

    /// <summary>
    /// If true, a gaze shift will be interrupted on next frame.
    /// </summary>
    public bool stopGazeShift = false;

    /// <summary>
    /// If true, VOR will be active even in the state of idle gaze.
    /// </summary>
    public bool fixGaze = true;

    /// <summary>
    /// If true and torso is defined, the gaze controller will recruit
    /// the torso when performing the gaze shift; otherwise it will only
    /// move the eyes and head.
    /// </summary>
    public bool useTorso = true;

    /// <summary>
    /// How much the pelvis contributes to torso rotation toward the gaze target.
    /// </summary>
    public float pelvisAlign = 0.5f;

    /// <summary>
    /// Predictability of the gaze target (0-1). 
    /// </summary>
    public float predictability = 1f;

    /// <summary>
    /// If true, roll component of gaze joint rotations will be removed
    /// during animation.
    /// </summary>
    public bool removeRoll = true;

    /// <summary>
    /// If true, stylization principles will be applied to the gaze animation.
    /// </summary>
    public bool stylizeGaze = false;

    /// <summary>
    /// Current gaze target.
    /// </summary>
    public virtual GameObject CurrentGazeTarget
    {
        get;
        protected set;
    }

    /// <summary>
    /// Current position of the gaze target.
    /// </summary>
    public virtual Vector3 CurrentGazeTargetPosition
    {
        get { return CurrentGazeTarget.transform.position; }
    }

    /// <summary>
    /// Current position of the gaze target.
    /// </summary>
    public virtual Vector3 MovingGazeTargetPosition
    {
        get { return CurrentGazeTarget.transform.position + curMovingTargetPosOff; }
    }

    /// <summary>
    /// Current gaze target for fixation.
    /// </summary>
    public virtual GameObject FixGazeTarget
    {
        get;
        protected set;
    }

    /// <summary>
    /// Current gaze target position for fixation.
    /// </summary>
    public virtual Vector3 FixGazeTargetPosition
    {
        get { return FixGazeTarget.transform.position; }
    }

    /// <summary>
    /// Helper gaze target for looking at a specified location in the world.
    /// </summary>
    public virtual GameObject HelperTarget
    {
        get;
        protected set;
    }

    /// <summary>
    /// Helper gaze target for looking straight ahead.
    /// </summary>
    public virtual GameObject AheadHelperTarget
    {
        get;
        protected set;
    }

    /// <summary>
    /// Helper gaze target for fixations.
    /// </summary>
    public virtual GameObject FixHelperTarget
    {
        get;
        protected set;
    }

    /// <summary>
    /// If true, the agent is gazing ahead rather than fixating a specific target.
    /// </summary>
    public virtual bool IsGazingAhead
    {
        get { return AheadHelperTarget == CurrentGazeTarget || CurrentGazeTarget == null; }
    }

    /// <summary>
    /// Centroid of the eyes.
    /// </summary>
    public virtual Vector3 EyeCenter
    {
        get { return 0.5f * (lEye.Position + rEye.Position); }
    }

    /// <summary>
    /// Averaged gaze direction of the eyes.
    /// </summary>
    public virtual Vector3 EyeDirection
    {
        get { return (0.5f * (lEye.Direction + rEye.Direction)).normalized; }
    }

    /// <summary>
    /// Gaze shift amplitude.
    /// </summary>
    public float Amplitude
    {
        get;
        protected set;
    }

    /// <summary>
    /// Measure of how cross-eyed the character is, as angle between
    /// eye direction vectors.
    /// </summary>
    public virtual float CrossEyedness
    {
        get
        {
            float ce = Vector3.Angle(lEye.Direction, rEye.Direction);
            if (ce > 0.00001f)
            {
                // Is the character cross-eyed or eye-divergent?
                float lt, rt;
                GeomUtil.ClosestPointsOn2Lines(
                    lEye.Position, lEye.Position + lEye.Direction,
                    rEye.Position, rEye.Position + rEye.Direction,
                    out lt, out rt);
                if (rt <= 0)
                    ce = 0;
            }

            return ce;
        }
    }

    /// <summary>
    /// Measure of how divergent the character's eyes are, as angle between
    /// eye direction vectors.
    /// </summary>
    public virtual float EyeDivergence
    {
        get
        {
            float ed = Vector3.Angle(lEye.Direction, rEye.Direction);
            if (ed > 0.00001f)
            {
                // Is the character cross-eyed or eye-divergent?
                float lt, rt;
                GeomUtil.ClosestPointsOn2Lines(
                    lEye.Position, lEye.Position + lEye.Direction,
                    rEye.Position, rEye.Position + rEye.Direction,
                    out lt, out rt);
                if (rt > 0)
                    ed = 0;
            }

            return ed;
        }
    }

    /// <summary>
    /// Relative position of the gaze target in the future
    /// (used in computing gaze shift parameters for rel. moving targets).
    /// </summary>
    public Vector3 _MovingTargetPositionOffset
    {
        get;
        set;
    }

    // Current gaze shift settings:
    protected Vector3 curMovingTargetPosOff = Vector3.zero; // Rel. position offset of the current target in near future
    protected bool curUseTorso = true;

    // Other anim. controllers:
    protected FaceController faceController = null;
    protected bool reenableRandomHeadMotion = false;
    protected bool reenableRandomSpeechMotion = false;

    /// <summary>
    /// Constructor.
    /// </summary>
    public GazeController()
    {
        lEye = new GazeBodyPart(GazeBodyPartType.LEye, this);
        rEye = new GazeBodyPart(GazeBodyPartType.REye, this);
        head = new GazeBodyPart(GazeBodyPartType.Head, this);
        torso = new GazeBodyPart(GazeBodyPartType.Torso, this);
    }

    /// <summary>
    /// <see cref="AnimController.Start"/>
    /// </summary>
    public override void Start()
    {
        base.Start();

        // Initialize all gaze body parts
        _InitGazeBodyParts();

        // Find/create helper gaze targets
        HelperTarget = _GetHelperTarget(gameObject.name + "GazeHelper");
        AheadHelperTarget = _GetHelperTarget(gameObject.name + "GazeAheadHelper");
        FixHelperTarget = _GetHelperTarget(gameObject.name + "FixGazeHelper");

        // Initialize gaze targets
        CurrentGazeTarget = null;
        FixGazeTarget = null;

        // Get other relevant anim. controllers
        faceController = gameObject.GetComponent<FaceController>();
    }

    /// <summary>
    /// Gaze at specified target object.
    /// </summary>
    /// <param name="gazeTarget">
    /// Gaze target tag.
    /// </param>
    public virtual void GazeAt(string gazeTarget)
    {
        GazeAt(GameObject.FindGameObjectWithTag(gazeTarget));
    }

    /// <summary>
    /// Gaze at specified target object.
    /// </summary>
    /// <param name="gazeTarget">
    /// Gaze target. <see cref="GameObject"/>
    /// </param>
    public virtual void GazeAt(GameObject gazeTarget)
    {
        if (gazeTarget == null)
        {
            GazeAhead();
            return;
        }

        this.gazeTarget = gazeTarget;
        doGazeShift = true;
        _MovingTargetPositionOffset = Vector3.zero;

        Debug.Log("GazeAt " + gazeTarget.gameObject);
    }

    /// <summary>
    /// Gaze at specified point in world space.
    /// </summary>
    /// <param name="gazeTargetWPos">
    /// Gaze target position in world psace.
    /// </param>
    public virtual void GazeAt(Vector3 gazeTargetWPos)
    {
        if (FixGazeTarget == HelperTarget)
        {
            // Agent is currently fixating the helper target at a different position,
            // so we need to replace it with a fixation helper
            FixGazeTarget = FixHelperTarget;
            FixHelperTarget.transform.position = HelperTarget.transform.position;
        }

        HelperTarget.transform.position = gazeTargetWPos;
        GazeAt(HelperTarget);
    }

    /// <summary>
    /// Direct the agent's gaze to a "neutral position", i.e.
    /// whatever is in front of it. 
    /// </summary>
    public virtual void GazeAhead()
    {
        if (FixGazeTarget == AheadHelperTarget)
        {
            // Agent is currently fixating the ahead helper target in a different direction,
            // so we need to replace it with a fixation helper
            FixGazeTarget = FixHelperTarget;
            FixHelperTarget.transform.position = AheadHelperTarget.transform.position;
        }

        // Position the helper gaze target in front of the agent
        Vector3 bodyPos = ModelController.BodyPosition;
        Vector3 bodyDir = ModelController.BodyDirection;
        float height = ModelController.GetInitWorldPosition(head.Top).y;
        Vector3 pos = (new Vector3(bodyPos.x, height, bodyPos.z)) + height * bodyDir;
        AheadHelperTarget.transform.position = pos;

        GazeAt(AheadHelperTarget);
    }

    /// <summary>
    /// Interrupt ongoing gaze shift.
    /// </summary>
    public virtual void StopGaze()
    {
        stopGazeShift = true;

        Debug.LogWarning("Stopping gaze shift toward target " + CurrentGazeTarget.name);
    }

    protected virtual void LateUpdate_NoGaze()
    {
        _InitBaseRotations();
        _UpdateSourceDirections();

        if (fixGaze)
        {
            if (FixGazeTarget == null)
            {
                // No target set yet for fixation, set one now
                _InitSourceRotations();
                _UpdateSourceDirections();
                _InitFix();
            }

            // Fixate gaze onto the current target
            _ApplyFixSourceRotations();
            _ApplyFix();
        }

        if (doGazeShift && gazeTarget != null)
        {
            // Interrupt whatever the face is doing
            _StopHead();

            GoToState((int)GazeState.Shifting);
            return;
        }
    }

    protected virtual void LateUpdate_Shifting()
    {
        _InitBaseRotations();
        _UpdateSourceDirections();
        // TODO: this is a bit of a hacky solution to ensure the eyes fully align with the target
        // when min. head rotation is insufficient
        _InitHeadTargetDirection();
        //
        _ApplySourceRotations();

        // Advance torso gaze shift
        torso._UpdateTargetDirection();
        bool torsoAligned = _AdvanceGazeShift(torso, deltaTime);
        torso._ApplyGazeShift();

        // Advance head gaze shift
        head._UpdateTargetDirection();
        bool headAligned =  _AdvanceGazeShift(head, deltaTime);
        head._ApplyGazeShift();

        // Advance eye gaze shifts
        lEye._UpdateTargetDirection();
        rEye._UpdateTargetDirection();
        bool lEyeAligned = _AdvanceGazeShift(lEye, deltaTime);
        bool rEyeAligned = _AdvanceGazeShift(rEye, deltaTime);
        lEye._ApplyGazeShift();
        rEye._ApplyGazeShift();

        if (lEyeAligned && rEyeAligned && headAligned && torsoAligned ||
            stopGazeShift)
        {
            // Gaze shift finished, begin gaze fixation
            GoToState((int)GazeState.NoGaze);
            return;
        }
    }

    protected virtual void Transition_NoGazeShifting()
    {
        doGazeShift = false;

        if (!fixGaze && FixGazeTarget == null)
        {
            // This is the first-ever gaze shift, so there is no target set for VOR
            // during latency period, so set one now
            _InitSourceRotations();
            _UpdateSourceDirections();
            _InitFix();
        }

        // Initialize new gaze shift
        _InitGazeShift();
    }

    protected virtual void Transition_ShiftingNoGaze()
    {
        stopGazeShift = false;
        _InitFix();
        _RestartHead();
    }

    // Initialize gaze body parts
    protected virtual void _InitGazeBodyParts()
    {
        lEye._Init();
        rEye._Init();
        head._Init();
        if (torso.Defined)
            torso._Init();
    }

    // Store gaze joint rotations before gaze is applied
    protected virtual void _InitBaseRotations()
    {
        lEye._InitBaseRotations();
        rEye._InitBaseRotations();
        head._InitBaseRotations();
        if (torso.Defined)
            torso._InitBaseRotations();
    }

    // Initialize source rotations of all the gaze joints from their current rotations
    protected virtual void _InitSourceRotations()
    {
        lEye._InitSourceRotations();
        rEye._InitSourceRotations();
        head._InitSourceRotations();
        if (torso.Defined)
            torso._InitSourceRotations();
    }

    // Update source directions of all gaze body parts to account for movement
    // since gaze shift start
    protected virtual void _UpdateSourceDirections()
    {
        _ApplySourceRotations();

        // Update source gaze directions
        lEye._SourceDirection = lEye.Direction;
        rEye._SourceDirection = rEye.Direction;
        head._SourceDirection = head.Direction;
        if (torso.Defined)
            torso._SourceDirection = torso.Direction;

        _ApplyFixSourceRotations();

        // Update source gaze directions
        lEye._FixSourceDirection = lEye.Direction;
        rEye._FixSourceDirection = rEye.Direction;
        head._FixSourceDirection = head.Direction;
        if (torso.Defined)
            torso._FixSourceDirection = torso.Direction;

        // Reapply current posture
        _ApplyBaseRotations();
    }

    // Apply source posture (at the start of the gaze shift)
    protected virtual void _ApplySourceRotations()
    {
        lEye._ApplySourceRotations();
        rEye._ApplySourceRotations();
        head._ApplySourceRotations();
        if (torso.Defined)
            torso._ApplySourceRotations();
    }

    // Apply source posture (at the start of the previous gaze shift)
    protected virtual void _ApplyFixSourceRotations()
    {
        lEye._ApplyFixSourceRotations();
        rEye._ApplyFixSourceRotations();
        head._ApplyFixSourceRotations();
        if (torso.Defined)
            torso._ApplyFixSourceRotations();
    }

    // Apply current body posture (before gaze is applied)
    protected virtual void _ApplyBaseRotations()
    {
        lEye._ApplyBaseRotations();
        rEye._ApplyBaseRotations();
        head._ApplyBaseRotations();
        if (torso.Defined)
            torso._ApplyBaseRotations();
    }

    // Initialize fixation of the current gaze target
    protected virtual void _InitFix()
    {
        // Set fixation target
        if (IsGazingAhead)
        {
            // No current gaze target, fixate nothing
            FixGazeTarget = null;
        }
        else
        {
            // Fixate current gaze target
            FixGazeTarget = CurrentGazeTarget;
        }

        // Initialize fixation for each body part
        if (torso.Defined)
            torso._InitFix();
        head._InitFix();
        lEye._InitFix();
        rEye._InitFix();
    }

    // Apply fixation of the current gaze target
    protected virtual void _ApplyFix()
    {
        if (torso.Defined)
            torso._ApplyFix();
        head._ApplyFix();
        lEye._ApplyFix();
        rEye._ApplyFix();
    }

    // Initialize new gaze shift
    protected virtual void _InitGazeShift()
    {
        // Initialize gaze shift parameters
        CurrentGazeTarget = gazeTarget;
        curMovingTargetPosOff = _MovingTargetPositionOffset;
        curUseTorso = useTorso;
        if (!curUseTorso)
            torso.align = 0f;

        // Initialize per-body part gaze shift parameters
        if (torso.Defined)
            torso._InitGazeShift();
        head._InitGazeShift();
        lEye._InitGazeShift();
        rEye._InitGazeShift();

        // Initialize kinematic properties of the gaze shift
        _InitAmplitude();
        _InitOMR();
        _InitTargetDirections();
        _InitLatencies();
        _InitMaxVelocities();
    }

    // Compute overall amplitude of the gaze shift towards current target
    protected virtual void _InitAmplitude()
    {
        Vector3 trgPos = CurrentGazeTargetPosition + curMovingTargetPosOff;
        Vector3 trgDir = (trgPos - EyeCenter).normalized;
        Amplitude = Vector3.Angle(EyeDirection, trgDir);
    }

    // Initialize motor ranges of the eyes
    protected virtual void _InitOMR()
    {
        lEye._InitOMR();
        rEye._InitOMR();
    }

    // Initialize target gaze directions of all body parts
    protected virtual void _InitTargetDirections()
    {
        _InitTorsoTargetDirection();
        _InitEyeTargetDirections();
        _InitHeadTargetDirection();
    }

    // Initialize target gaze directions for the torso
    protected virtual void _InitTorsoTargetDirection()
    {
        if (!torso.Defined)
            return;

        // Compute source and target directions
        Vector3 srcDir = torso._SourceDirection;
        torso._InitTargetDirection();
        Vector3 trgDir = torso._TargetDirection;
        Vector3 trgDirAlign = srcDir;

        // Compute aligning target direction
        float minDistRot = curUseTorso ? _ComputeMinTorsoAmplitude() : 0f;
        float fullDistRot = Vector3.Angle(srcDir, trgDir);
        float align = fullDistRot > 0f ?
            Mathf.Clamp01((minDistRot + (fullDistRot - minDistRot) * torso._Align)/fullDistRot) : 1f;
        Quaternion rotAlign = Quaternion.Slerp(Quaternion.identity, Quaternion.FromToRotation(srcDir, trgDir), align);
            trgDirAlign = rotAlign * srcDir;
        torso._TargetDirectionAlign = trgDirAlign;
    }

    // Compute minimal torso rotational amplitude from overall gaze shift amplitude
    protected virtual float _ComputeMinTorsoAmplitude()
    {
        if (Amplitude >= 40f)
            return 0.43f * Mathf.Exp(0.029f * Amplitude) + 0.186f;
        else if (Amplitude >= 20f)
            return 0.078f * Amplitude - 1.558f;

        return 0f;
    }

    // Initialize target gaze directions for the eyes
    protected virtual void _InitEyeTargetDirections()
    {
        lEye._InitTargetDirection();
        lEye._InitOMRTargetDirection();
        rEye._InitTargetDirection();
        rEye._InitOMRTargetDirection();
    }

    // Initialize target gaze direction of the head
    protected virtual void _InitHeadTargetDirection()
    {
        head._InitTargetDirection();

        if (torso.Defined)
            // Orient the torso toward the target
            torso.RotateTowards(torso._TargetDirectionAlign);

        // Compute the maximum rotational difference between OMR-constrained eye target rotation
        // and eye target rotation required to align the eyes with the target
        Quaternion lEyeRotDiff = Quaternion.identity, rEyeRotDiff = Quaternion.identity;
        lEyeRotDiff = lEye._GetOMRTargetRotationDiff();
        rEyeRotDiff = rEye._GetOMRTargetRotationDiff();
        Quaternion maxRotDiff = QuaternionUtil.Angle(lEyeRotDiff) > QuaternionUtil.Angle(rEyeRotDiff) ?
            lEyeRotDiff : rEyeRotDiff;

        // Compute minimal target direction for the head
        Quaternion headCurRot = head.Top.localRotation;
        head.Top.localRotation *= maxRotDiff;
        Vector3 headTrgDirMin = head.Direction;
        head.Top.localRotation = headCurRot;

        if (torso.Defined)
            // Restore original torso orientation
            torso._ApplySourceRotations();

        // Compute aligning target direction for the head
        Vector3 headTrgDir = head._TargetDirection;
        Vector3 headSrcDir = head._SourceDirection;
        headTrgDirMin = GeomUtil.ProjectVectorOntoPlane(headTrgDirMin, Vector3.Cross(headSrcDir, headTrgDir)).normalized;
        float headDistRotMin = Vector3.Angle(headSrcDir, headTrgDirMin);
        float headDistRotFull = Vector3.Angle(headSrcDir, headTrgDir);
        float headAlign = headDistRotFull > 0f ?
            Mathf.Clamp01((headDistRotMin + (headDistRotFull - headDistRotMin) * head._Align) / headDistRotFull) : 1f;
        Quaternion rotAlign = Quaternion.Slerp(Quaternion.identity,
            Quaternion.FromToRotation(headSrcDir, headTrgDir), headAlign);
        Vector3 headTrgDirAlign = rotAlign * head._SourceDirection;
        head._TargetDirectionAlign = headTrgDirAlign;
    }
    
    // Initialize latency times of all gaze body parts
    protected virtual void _InitLatencies()
    {
        float headLatency = _ComputeHeadLatency();
        float torsoLatency = torso.Defined ? _ComputeTorsoLatency() : 0f;
        float minLatency = Mathf.Min(headLatency, torsoLatency);
        head._Latency = minLatency >= 0f ? headLatency : headLatency + Mathf.Abs(minLatency);
        if (torso.Defined)
            torso._Latency = minLatency >= 0f ? torsoLatency : torsoLatency + Mathf.Abs(minLatency);
    }

    // Compute head latency from gaze shift amplitude and target predictability
    protected virtual float _ComputeHeadLatency()
    {
        float pred = Mathf.Clamp01(predictability);
        // TODO: take into account amplitude
        float latency = 50f - 20f * pred;
        return latency / 1000f;
    }

    // Compute torso latency from gaze shift amplitude and target predictability
    protected virtual float _ComputeTorsoLatency()
    {
        float pred = Mathf.Clamp01(predictability);
        float latency = -0.25f * Amplitude * pred + 0.5f * Amplitude - 57.5f * pred + 105f;
        return latency / 1000f;
    }

    // Initialize maximum velocities of all gaze body parts
    protected virtual void _InitMaxVelocities()
    {
        // Set head and torso max. velocities
        if (torso.Defined)
            torso._MaxVelocity = _ComputeTorsoMaxVelocity();
        head._MaxVelocity = _ComputeHeadMaxVelocity();

        // Set eye max. velocities
        float eyeMaxVelocity = _ComputeEyeMaxVelocity();
        lEye._MaxVelocity = eyeMaxVelocity;
        rEye._MaxVelocity = eyeMaxVelocity;
    }

    // Compute max. gaze shift velocity of the torso
    protected virtual float _ComputeTorsoMaxVelocity()
    {
        float torsoDistRotAlign = _ComputeDistRotAlignForMovingTarget(torso);
        float maxVelocity = (4f / 3f) * (torso.velocity / 15f) * torsoDistRotAlign +
            torso.velocity / 0.5f;

        return maxVelocity;
    }

    // Compute max. gaze shift velocity of the head
    protected virtual float _ComputeHeadMaxVelocity()
    {
        float headDistRotAlign = _ComputeDistRotAlignForMovingTarget(head);
        float maxVelocity = (4f / 3f) * (head.velocity / 50f) * headDistRotAlign +
            head.velocity / 2.5f;

        return maxVelocity;
    }

    // Compute max. gaze shift velocity of the eyes
    protected virtual float _ComputeEyeMaxVelocity()
    {
        // Find the shortest eye rotation distance
        float lEyeA = _ComputeEyeDistRotForMovingTarget(lEye);
        float rEyeA = _ComputeEyeDistRotForMovingTarget(rEye);
        float Amin = Mathf.Min(lEyeA, rEyeA);
        float velocity = (lEye.velocity + rEye.velocity) / 2f;
        float maxVelocity = 4f * (velocity / 150f) * Amin + velocity / 6f;

        return maxVelocity;
    }

    // Compute the gaze body part's distance to rotate adjusted for projected target movement
    protected virtual float _ComputeDistRotAlignForMovingTarget(GazeBodyPart bodyPart)
    {
        float adjDistRotAlign = Vector3.Angle(bodyPart._SourceDirection, bodyPart._TargetDirectionAlign);
        if (curMovingTargetPosOff != Vector3.zero)
        {
            // Adjust rotational distance based on future target offset
            float distRot = Vector3.Angle(bodyPart._SourceDirection, bodyPart.GetTargetDirection(CurrentGazeTargetPosition));
            float adjDistRot = Vector3.Angle(bodyPart._SourceDirection,
                bodyPart.GetTargetDirection(CurrentGazeTargetPosition + curMovingTargetPosOff));
            adjDistRotAlign = distRot > 0f ? adjDistRotAlign / distRot * adjDistRot : 0f;
        }
        
        return adjDistRotAlign;
    }

    // Compute the eye's distance to rotate adjusted for projected target movement
    protected virtual float _ComputeEyeDistRotForMovingTarget(GazeBodyPart eye)
    {
        float adjDistRotOMR = Vector3.Angle(eye._SourceDirection, eye._TargetDirectionAlign);

        if (curMovingTargetPosOff != Vector3.zero)
        {
            // Adjust rotational distance based on future target offset
            Vector3 adjTrgDirOMR = eye.GetOMRTargetDirection(CurrentGazeTargetPosition + curMovingTargetPosOff);
            adjDistRotOMR = Vector3.Angle(eye._SourceDirection, adjTrgDirOMR);
        }

        return adjDistRotOMR;
    }

    // Advance the gaze shift movement of the specified body part
    protected virtual bool _AdvanceGazeShift(GazeBodyPart gazeBodyPart, float deltaTime)
    {
        bool aligned = true;
        float dt = 0f;
        for (float t = 0; t < DeltaTime; )
        {
            // Compute delta time
            t += LEAPCore.eulerTimeStep;
            dt = (t <= DeltaTime) ? LEAPCore.eulerTimeStep :
                DeltaTime - t + LEAPCore.eulerTimeStep;

            aligned = !gazeBodyPart.Defined || gazeBodyPart._AdvanceGazeShift(dt);
        }

        return aligned;
    }

    // Stop any ongoing head movements
    protected virtual void _StopHead()
    {
        if (faceController == null)
            return;

        faceController.stopGesture = true;
        reenableRandomHeadMotion = faceController.randomMotionEnabled;
        reenableRandomSpeechMotion = faceController.speechMotionEnabled;
        faceController.speechMotionEnabled = false;
        faceController.randomMotionEnabled = false;
    }

    // Restart any head movements that were previously active
    protected virtual void _RestartHead()
    {
        if (faceController == null)
            return;

        faceController.randomMotionEnabled = reenableRandomHeadMotion;
        faceController.speechMotionEnabled = reenableRandomSpeechMotion;
    }

    // Find/create helper gaze target with the specified name
    protected virtual GameObject _GetHelperTarget(string helperTargetName)
    {
        var helperTarget = GameObject.Find(helperTargetName);
        if (helperTarget == null)
        {
            helperTarget = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            helperTarget.name = helperTargetName;
            helperTarget.tag = "GazeTarget";
            helperTarget.renderer.enabled = false;
            helperTarget.transform.localScale = new Vector3(0.02f, 0.02f, 0.02f);
        }

        return helperTarget;
    }

    /// <summary>
    /// <see cref="AnimController.GetRuntimeState"/>
    /// </summary>
    /// <returns></returns>
    public override IAnimControllerState GetRuntimeState()
    {
        GazeControllerState state = new GazeControllerState();
        if (!enabled)
            return state;
        
        state.stateId = StateId;
        state.gazeTarget = gazeTarget;
        state.doGazeShift = doGazeShift;
        state.stopGazeShift = stopGazeShift;
        state.fixGaze = fixGaze;
        state.useTorso = useTorso;
        state.pelvisAlign = pelvisAlign;
        state.predictability = predictability;
        state.movingTargetPosOff = _MovingTargetPositionOffset;
        state.removeRoll = removeRoll;
        state.weight = weight;
        state.curGazeTarget = CurrentGazeTarget;
        state.fixGazeTarget = FixGazeTarget;
        state.amplitude = Amplitude;
        state.curMovingTargetPosOff = curMovingTargetPosOff;
        state.curUseTorso = curUseTorso;
        state.reenableRandomHeadMotion = reenableRandomHeadMotion;
        state.reenableRandomSpeechMotion = reenableRandomSpeechMotion;

        state.lEyeState = lEye._GetRuntimeState();
        state.rEyeState = rEye._GetRuntimeState();
        state.headState = head._GetRuntimeState();
        state.torsoState = torso._GetRuntimeState();

        return state;
    }

    /// <summary>
    /// <see cref="AnimController.SetRuntimeState"/>
    /// </summary>
    /// <returns></returns>
    public override void SetRuntimeState(IAnimControllerState state)
    {
        if (!enabled)
            return;
        
        GazeControllerState gazeControllerState = (GazeControllerState)state;
        _GetFSM()._SetState(gazeControllerState.stateId);
        gazeTarget = gazeControllerState.gazeTarget;
        doGazeShift = gazeControllerState.doGazeShift;
        stopGazeShift = gazeControllerState.stopGazeShift;
        fixGaze = gazeControllerState.fixGaze;
        useTorso = gazeControllerState.useTorso;
        pelvisAlign = gazeControllerState.pelvisAlign;
        predictability = gazeControllerState.predictability;
        _MovingTargetPositionOffset = gazeControllerState.movingTargetPosOff;
        removeRoll = gazeControllerState.removeRoll;
        weight = gazeControllerState.weight;
        CurrentGazeTarget = gazeControllerState.curGazeTarget;
        FixGazeTarget = gazeControllerState.fixGazeTarget;
        Amplitude = gazeControllerState.amplitude;
        curMovingTargetPosOff = gazeControllerState.curMovingTargetPosOff;
        reenableRandomHeadMotion = gazeControllerState.reenableRandomHeadMotion;
        reenableRandomSpeechMotion = gazeControllerState.reenableRandomSpeechMotion;

        lEye._SetRuntimeState(gazeControllerState.lEyeState);
        rEye._SetRuntimeState(gazeControllerState.rEyeState);
        head._SetRuntimeState(gazeControllerState.headState);
        torso._SetRuntimeState(gazeControllerState.torsoState);
    }

    /// <summary>
    /// Get zero gaze controller state (before any gaze shifts have been performed).
    /// </summary>
    /// <returns>Initial runtime state</returns>
    public virtual GazeControllerState GetZeroRuntimeState()
    {
        GazeControllerState state = (GazeControllerState)GetRuntimeState();

        state.stateId = (int)GazeState.NoGaze;
        state.gazeTarget = null;
        state.doGazeShift = false;
        state.stopGazeShift = false;
        state.fixGaze = false;
        state.useTorso = true;
        state.pelvisAlign = 0.5f;
        state.predictability = 1f;
        state.movingTargetPosOff = Vector3.zero;
        state.removeRoll = true;
        state.weight = 1f;
        state.fixWeight = 1f;
        state.curGazeTarget = null;
        state.fixGazeTarget = null;
        state.amplitude = 0f;
        state.curMovingTargetPosOff = Vector3.zero;
        state.curUseTorso = true;
        state.reenableRandomHeadMotion = false;
        state.reenableRandomSpeechMotion = false;

        state.lEyeState = lEye._GetInitRuntimeState();
        state.rEyeState = rEye._GetInitRuntimeState();
        state.headState = head._GetInitRuntimeState();
        state.torsoState = torso._GetInitRuntimeState();

        return state;
    }

    /// <summary>
    /// Get initial gaze controller state for the current gaze shift parameters.
    /// </summary>
    /// <returns>Initial runtime state</returns>
    public virtual GazeControllerState GetInitRuntimeState()
    {
        IAnimControllerState curState = GetRuntimeState();

        _InitBaseRotations();
        _InitGazeShift();
        GazeControllerState state = (GazeControllerState)GetRuntimeState();

        SetRuntimeState(curState);

        return state;
    }

    // Create default FSM states for this controller
    public override void _CreateStates()
    {
        // Initialize states
        _InitStateDefs<GazeState>();
        _InitStateTransDefs((int)GazeState.NoGaze, 1);
        _InitStateTransDefs((int)GazeState.Shifting, 1);
        states[(int)GazeState.NoGaze].lateUpdateHandler = "LateUpdate_NoGaze";
        states[(int)GazeState.NoGaze].nextStates[0].nextState = "Shifting";
        states[(int)GazeState.NoGaze].nextStates[0].transitionHandler = "Transition_NoGazeShifting";
        states[(int)GazeState.Shifting].lateUpdateHandler = "LateUpdate_Shifting";
        states[(int)GazeState.Shifting].nextStates[0].nextState = "NoGaze";
        states[(int)GazeState.Shifting].nextStates[0].transitionHandler = "Transition_ShiftingNoGaze";

        // Get all bones needed for gaze actuation
        var lEyeBone = ModelUtils.FindBoneWithTag(gameObject.transform, "LEyeBone");
        var rEyeBone = ModelUtils.FindBoneWithTag(gameObject.transform, "REyeBone");
        var headBones = ModelUtils.GetAllBonesWithTag(gameObject, "HeadBone");
        var torsoBones = ModelUtils.GetAllBonesWithTag(gameObject, "TorsoBone");

        // Add default eye joints
        lEye.gazeJoints = new Transform[1];
        lEye.gazeJoints[0] = lEyeBone;
        lEye.velocity = 150f;
        rEye.gazeJoints = new Transform[1];
        rEye.gazeJoints[0] = rEyeBone;
        rEye.velocity = 150f;

        // Add default head joints
        head.gazeJoints = headBones;
        head.velocity = 70f;

        // Add default torso joints
        torso.gazeJoints = torsoBones;
        torso.velocity = 40f; // TODO: make sure this is correct
    }
}

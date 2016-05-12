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
        public float curAlign;
        public float maxVelocity;
        public float curVelocity;
        public float latency;
        public float adjInOMR, adjOutOMR, adjUpOMR, adjDownOMR;
        public float curInOMR, curOutOMR, curUpOMR, curDownOMR;
        public Vector3 baseDir;
        public Quaternion[] baseRots;
        public Vector3 srcDir0;
        public Vector3 srcDir;
        public Vector3 trgDir;
        public Vector3 trgDirAlign;
        public float rotParam;
        public bool useLongArc;
        public Vector3 curDir;
        public bool isFix;
        public Vector3 fixSrcDir0;
        public Vector3 fixSrcDir;
        public Vector3 fixTrgDir;
        public Vector3 fixTrgDirAlign;
        public bool fixUseLongArc;
        public float weight;
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
    public Quaternion rootRot;
    public Quaternion fixRootRot;
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
    /// Root transform for gaze movements.
    /// </summary>
    public Transform Root
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
        get { return CurrentGazeTarget.transform.position + _curMovingTargetPosOff; }
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
    public virtual float Amplitude
    {
        get;
        protected set;
    }

    /// <summary>
    /// Minimal torso rotational amplitude from overall gaze shift amplitude.
    /// </summary>
    public virtual float MinTorsoAmplitude
    {
        get
        {
            if (Amplitude >= 40f)
                return 0.43f * Mathf.Exp(0.029f * Amplitude) + 0.186f;
            else if (Amplitude >= 20f)
                return 0.078f * Amplitude - 1.558f;

            return 0f;
        }
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
                GeometryUtil.ClosestPointsOn2Lines(
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
                GeometryUtil.ClosestPointsOn2Lines(
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

    /// <summary>
    /// Orientation of the root at the start of the current gaze shift.
    /// </summary>
    public Quaternion _RootRotation
    {
        get { return _rootRot; }
    }

    /// <summary>
    /// Orientation of the root at the start of the previous gaze shift.
    /// </summary>
    public Quaternion _FixRootRotation
    {
        get { return _fixRootRot; }
    }

    // Current gaze shift settings:
    protected Vector3 _curMovingTargetPosOff = Vector3.zero; // Rel. position offset of the current target in near future
    protected bool _curUseTorso = true;

    // Current gaze shift state:
    protected Quaternion _rootRot;
    protected Quaternion _fixRootRot;

    // Other anim. controllers:
    protected FaceController _faceController = null;
    protected bool _reenableRandomHeadMotion = false;
    protected bool _reenableRandomSpeechMotion = false;

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
        _faceController = gameObject.GetComponent<FaceController>();
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
        GazeAt(gazeTarget, Vector3.zero);
    }

    /// <summary>
    /// Gaze at specified target object.
    /// </summary>
    /// <param name="gazeTarget">
    /// Gaze target. <see cref="GameObject"/>
    /// </param>
    /// <param name="movingTargetPosOff">Anticipated positional offset of the gaze target
    /// relative to the agent over the course of the gaze shift</param>
    public virtual void GazeAt(GameObject gazeTarget, Vector3 movingTargetPosOff)
    {
        if (gazeTarget == null)
        {
            GazeAhead();
            return;
        }

        this.gazeTarget = gazeTarget;
        doGazeShift = true;
        _MovingTargetPositionOffset = movingTargetPosOff;

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
        GazeAt(gazeTargetWPos, Vector3.zero);
    }

    /// <summary>
    /// Gaze at specified point in world space.
    /// </summary>
    /// <param name="gazeTargetWPos">
    /// Gaze target position in world psace.
    /// </param>
    /// <param name="movingTargetPosOff">Anticipated positional offset of the gaze target
    /// relative to the agent over the course of the gaze shift</param>
    public virtual void GazeAt(Vector3 gazeTargetWPos, Vector3 movingTargetPosOff)
    {
        if (FixGazeTarget == HelperTarget)
        {
            // Agent is currently fixating the helper target at a different position,
            // so we need to replace it with a fixation helper
            FixGazeTarget = FixHelperTarget;
            FixHelperTarget.transform.position = HelperTarget.transform.position;
        }

        HelperTarget.transform.position = gazeTargetWPos;
        GazeAt(HelperTarget, movingTargetPosOff);
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

        if (doGazeShift && gazeTarget != null)
        {
            // Disallow head gestures
            _StopHead();

            GoToState((int)GazeState.Shifting);
            return;
        }

        if (fixGaze && FixGazeTarget != null)
        {
            // Fixate gaze onto the current target
            _ApplyFix();
        }
        else
        {
            FixGazeTarget = null;
        }
    }

    // TODO: should also update fixating target directions for joints that are in latency phase, but not doing that right now
    protected virtual void LateUpdate_Shifting()
    {
        _InitBaseRotations();
        _UpdateSourceDirections();
        _UpdateLatencyFixTargetDirections();
        // TODO: this is a bit of a hacky solution to ensure the eyes fully align with the target
        // when min. head rotation is insufficient
        _InitHeadTargetDirection();
        //
        _ApplySourceDirections();

        bool torsoAligned = true;
        if (torso.Defined)
        {
            // Advance torso gaze shift
            torso._UpdateTargetDirection();
            torsoAligned = torso._AdvanceGazeShift(deltaTime);
            torso._ApplyGazeShift();
        }

        // Advance head gaze shift
        head._UpdateTargetDirection();
        bool headAligned = head._AdvanceGazeShift(deltaTime);
        head._ApplyGazeShift();

        // Advance eye gaze shifts
        lEye._UpdateTargetDirection();
        rEye._UpdateTargetDirection();
        bool lEyeAligned = lEye._AdvanceGazeShift(deltaTime);
        bool rEyeAligned = rEye._AdvanceGazeShift(deltaTime);
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

        if (FixGazeTarget == null)
        {
            // This is the first gaze shift after a period of unconstrained gaze,
            // so set an initial fixation target for the latency phase
            _InitFix();
        }

        // Initialize new gaze shift
        _InitGazeShift();

        // Fixate onto the preceding target
        _ApplyBaseRotations();
        _ApplyFix();
    }

    protected virtual void Transition_ShiftingNoGaze()
    {
        stopGazeShift = false;
        _InitFix();

        // Allow head gestures to continue
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

        // Initialize the gaze root transform
        if (torso.Defined)
        {
            Root = torso.gazeJoints[torso.gazeJoints.Length - 1];
            Root = Root.tag == "RootBone" ? Root : Root.parent;
        }
        else
        {
            Root = head.gazeJoints[head.gazeJoints.Length - 1].parent;
        }
    }

    // Initialize fixation of the current gaze target
    protected virtual void _InitFix()
    {
        _InitFixRootRotation();

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
        _UpdateFixSourceDirections();
        _ApplyFixSourceDirections();

        if (torso.Defined)
        {
            // Apply torso gaze fixation
            torso._UpdateFixTargetDirection();
            torso._ApplyFix();
        }

        // Apply head gaze fixation
        head._UpdateFixTargetDirection();
        head._ApplyFix();

        // Apply eye gaze fixations
        lEye._UpdateFixTargetDirection();
        rEye._UpdateFixTargetDirection();
        lEye._ApplyFix();
        rEye._ApplyFix();
    }

    // Initialize new gaze shift
    protected virtual void _InitGazeShift()
    {
        // Initialize gaze shift parameters
        CurrentGazeTarget = gazeTarget;
        _curMovingTargetPosOff = _MovingTargetPositionOffset;
        _curUseTorso = useTorso;
        if (!_curUseTorso)
            torso.align = 0f;
        _InitRootRotation();

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
        _InitLongArcGazeShift();
        _InitLatencies();
        _InitMaxVelocities();
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

    // Apply current body posture (before gaze is applied)
    protected virtual void _ApplyBaseRotations()
    {
        lEye._ApplyBaseRotations();
        rEye._ApplyBaseRotations();
        head._ApplyBaseRotations();
        if (torso.Defined)
            torso._ApplyBaseRotations();
    }

    // Initialize rigid transformation of the root at preceding gaze shift start
    protected virtual void _InitFixRootRotation()
    {
        _fixRootRot = _rootRot;
    }

    // Update gaze fixation source directions of all body parts to account for root movement
    protected virtual void _UpdateFixSourceDirections()
    {
        Quaternion fixRootRot1 = Root.rotation;
        Quaternion dq = Quaternion.Inverse(_fixRootRot) * fixRootRot1;
        dq.eulerAngles = new Vector3(0f, dq.eulerAngles.y, 0f);
        torso._FixSourceDirection = dq * torso._FixSourceDirectionOriginal;
        head._FixSourceDirection = dq * head._FixSourceDirectionOriginal;
        lEye._FixSourceDirection = dq * lEye._FixSourceDirectionOriginal;
        rEye._FixSourceDirection = dq * rEye._FixSourceDirectionOriginal;
    }

    // Apply source body posture at the start of the preceding gaze shift
    protected virtual void _ApplyFixSourceDirections()
    {
        torso.RotateTowards(torso._FixSourceDirection);
        head.RotateTowards(head._FixSourceDirection);
        lEye.RotateTowards(lEye._FixSourceDirection); 
        rEye.RotateTowards(rEye._FixSourceDirection);
    }

    // Initialize rigid transformation of the root at current gaze shift start
    protected virtual void _InitRootRotation()
    {
        _rootRot = Root.rotation;
    }

    // Update gaze shift source directions of all body parts to account for root movement
    protected virtual void _UpdateSourceDirections()
    {
        Quaternion rootRot1 = Root.rotation;
        Quaternion dq = Quaternion.Inverse(_rootRot) * rootRot1;
        dq.eulerAngles = new Vector3(0f, dq.eulerAngles.y, 0f);
        torso._SourceDirection = dq * torso._SourceDirectionOriginal;
        head._SourceDirection = dq * head._SourceDirectionOriginal;
        lEye._SourceDirection = dq * lEye._SourceDirectionOriginal;
        rEye._SourceDirection = dq * rEye._SourceDirectionOriginal;
    }

    // Apply source body posture (at the current gaze shift start)
    protected virtual void _ApplySourceDirections()
    {
        if (torso.Defined)
            torso.RotateTowards(torso._SourceDirection);
        head.RotateTowards(head._SourceDirection);
        lEye.RotateTowards(lEye._SourceDirection);
        rEye.RotateTowards(rEye._SourceDirection);
    }

    // Update body parts' target directions for the gaze fixation during latency phase
    protected virtual void _UpdateLatencyFixTargetDirections()
    {
        _ApplyFixSourceDirections();

        // Update body parts' gaze fixation target directions during the latency phase
        _UpdateLatencyFixTargetDirections(torso);
        _UpdateLatencyFixTargetDirections(head);
        _UpdateLatencyFixTargetDirections(lEye);
        _UpdateLatencyFixTargetDirections(rEye);

        _ApplyBaseRotations();
        
        // Update body parts' gaze shift source directions to account
        // for root movement during the latency phase
        torso._UpdateSourceDirectionOnLatency();
        head._UpdateSourceDirectionOnLatency();
        lEye._UpdateSourceDirectionOnLatency();
        rEye._UpdateSourceDirectionOnLatency();
    }

    // Update specified body part's target direction for the gaze fixation during latency phase
    protected virtual void _UpdateLatencyFixTargetDirections(GazeBodyPart gazeBodyPart)
    {
        if (!gazeBodyPart.Defined || gazeBodyPart._Latency <= 0f)
            return;

        gazeBodyPart._UpdateFixTargetDirection();
    }

    // Compute overall amplitude of the gaze shift toward the next target
    protected virtual void _InitAmplitude()
    {
        Vector3 trgPos = CurrentGazeTargetPosition + _curMovingTargetPosOff;
        Vector3 trgDir = (trgPos - EyeCenter).normalized;
        Vector3 srcDir = (0.5f * (lEye._SourceDirectionOriginal + rEye._SourceDirectionOriginal)).normalized;
        Amplitude = Vector3.Angle(srcDir, trgDir);
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

    // Should gaze shift be performed along the longer rotational arc?
    protected virtual void _InitLongArcGazeShift()
    {
        torso._InitLongArcGazeShift();
        head._InitLongArcGazeShift();
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
        float minDistRot = _curUseTorso ? MinTorsoAmplitude : 0f;
        float fullDistRot = Vector3.Angle(srcDir, trgDir);
        float align = fullDistRot > 0f ?
            Mathf.Clamp01((minDistRot + (fullDistRot - minDistRot) * torso._Align)/fullDistRot) : 1f;
        Quaternion rotAlign = Quaternion.FromToRotation(srcDir, trgDir);
        rotAlign = Quaternion.Slerp(Quaternion.identity, rotAlign, align);
        trgDirAlign = rotAlign * srcDir;
        torso._TargetDirectionAlign = trgDirAlign;
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
            torso._ApplyBaseRotations();

        // Compute aligning target direction for the head
        Vector3 headTrgDir = head._TargetDirection;
        Vector3 headSrcDir = head._SourceDirection;
        headTrgDirMin = GeometryUtil.ProjectVectorOntoPlane(headTrgDirMin, Vector3.Cross(headSrcDir, headTrgDir)).normalized;
        float headDistRotMin = Vector3.Angle(headSrcDir, headTrgDirMin);
        float headDistRotFull = Vector3.Angle(headSrcDir, headTrgDir);
        float headAlign = headDistRotFull > 0f ?
            Mathf.Clamp01((headDistRotMin + (headDistRotFull - headDistRotMin) * head._Align) / headDistRotFull) : 1f;
        Quaternion rotAlign = Quaternion.FromToRotation(headSrcDir, headTrgDir);
        rotAlign = Quaternion.Slerp(Quaternion.identity, rotAlign, headAlign);
        Vector3 headTrgDirAlign = rotAlign * head._SourceDirection;
        head._TargetDirectionAlign = headTrgDirAlign;
    }

    // Initialize target gaze directions for the eyes
    protected virtual void _InitEyeTargetDirections()
    {
        lEye._InitTargetDirection();
        lEye._InitOMRTargetDirection();
        rEye._InitTargetDirection();
        rEye._InitOMRTargetDirection();
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
        float maxVelocity = 420f * (velocity / 200f) * (1f - Mathf.Exp(-Amin / 14f)); // from Baloh et al. 1975

        return maxVelocity;
    }

    // Compute the gaze body part's distance to rotate adjusted for projected target movement
    protected virtual float _ComputeDistRotAlignForMovingTarget(GazeBodyPart bodyPart)
    {
        float adjDistRotAlign = Vector3.Angle(bodyPart._SourceDirection, bodyPart._TargetDirectionAlign);
        if (_curMovingTargetPosOff != Vector3.zero)
        {
            // Adjust rotational distance based on future target offset
            float distRot = Vector3.Angle(bodyPart._SourceDirection, bodyPart.GetTargetDirection(CurrentGazeTargetPosition));
            float adjDistRot = Vector3.Angle(bodyPart._SourceDirection,
                bodyPart.GetTargetDirection(CurrentGazeTargetPosition + _curMovingTargetPosOff));
            adjDistRotAlign = distRot > 0f ? adjDistRotAlign / distRot * adjDistRot : 0f;
        }
        
        return adjDistRotAlign;
    }

    // Compute the eye's distance to rotate adjusted for projected target movement
    protected virtual float _ComputeEyeDistRotForMovingTarget(GazeBodyPart eye)
    {
        float adjDistRotOMR = Vector3.Angle(eye._SourceDirection, eye._TargetDirectionAlign);

        if (_curMovingTargetPosOff != Vector3.zero)
        {
            // Adjust rotational distance based on future target offset
            Vector3 adjTrgDirOMR = eye.GetOMRTargetDirection(CurrentGazeTargetPosition + _curMovingTargetPosOff);
            adjDistRotOMR = Vector3.Angle(eye._SourceDirection, adjTrgDirOMR);
        }

        return adjDistRotOMR;
    }

    // Stop any ongoing head movements
    protected virtual void _StopHead()
    {
        if (_faceController == null)
            return;

        _faceController.stopGesture = true;
        _reenableRandomHeadMotion = _faceController.randomMotionEnabled;
        _reenableRandomSpeechMotion = _faceController.speechMotionEnabled;
        _faceController.speechMotionEnabled = false;
        _faceController.randomMotionEnabled = false;
    }

    // Restart any head movements that were previously active
    protected virtual void _RestartHead()
    {
        if (_faceController == null)
            return;

        _faceController.randomMotionEnabled = _reenableRandomHeadMotion;
        _faceController.speechMotionEnabled = _reenableRandomSpeechMotion;
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
        state.curMovingTargetPosOff = _curMovingTargetPosOff;
        state.rootRot = _rootRot;
        state.fixRootRot = _fixRootRot;
        state.curUseTorso = _curUseTorso;
        state.reenableRandomHeadMotion = _reenableRandomHeadMotion;
        state.reenableRandomSpeechMotion = _reenableRandomSpeechMotion;

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
        _curMovingTargetPosOff = gazeControllerState.curMovingTargetPosOff;
        _rootRot = gazeControllerState.rootRot;
        _fixRootRot = gazeControllerState.fixRootRot;
        _reenableRandomHeadMotion = gazeControllerState.reenableRandomHeadMotion;
        _reenableRandomSpeechMotion = gazeControllerState.reenableRandomSpeechMotion;

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

        state.lEyeState = lEye._GetZeroRuntimeState();
        state.rEyeState = rEye._GetZeroRuntimeState();
        state.headState = head._GetZeroRuntimeState();
        state.torsoState = torso._GetZeroRuntimeState();

        return state;
    }

    /// <summary>
    /// Get initial gaze controller state for the current gaze shift parameters.
    /// </summary>
    /// <returns>Initial runtime state</returns>
    public virtual GazeControllerState GetInitRuntimeState()
    {
        IAnimControllerState curState = GetRuntimeState();

        doGazeShift = true;
        LateUpdate_NoGaze();
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
        var lEyeBone = ModelUtil.FindBoneWithTag(gameObject.transform, "LEyeBone");
        var rEyeBone = ModelUtil.FindBoneWithTag(gameObject.transform, "REyeBone");
        var headBones = ModelUtil.GetAllBonesWithTag(gameObject, "HeadBone");
        var torsoBones = ModelUtil.GetAllBonesWithTag(gameObject, "TorsoBone");

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

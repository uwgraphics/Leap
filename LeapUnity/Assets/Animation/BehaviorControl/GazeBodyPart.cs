using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Class representing a body part in gaze shifts and fixations.
/// It can be one of the following: left eye, right eye, head, torso.
/// </summary>
[Serializable]
public class GazeBodyPart
{
    /// <summary>
    /// Joint chain that comprises the body part.
    /// </summary>
    public Transform[] gazeJoints = new Transform[0];

    /// <summary>
    /// How much the body part should align with the gaze target.
    /// </summary>
    public float align = 1f;

    /// <summary>
    /// How fast the joint moves (in deg/s).
    /// </summary>
    /// <remarks>Should be 360 for the eyes, 100 for the head, and 12 for the torso.</remarks>
    public float velocity = 360f;

    /// <summary>
    /// Ocular motor range (eyes only).
    /// </summary>
    public float inOMR = 45f, outOMR = 45f, upOMR = 45f, downOMR = 45f;

    /// <summary>
    /// Weight with which vertical posture of the body part from the base animation
    /// is preserved when applying gaze.
    /// </summary>
    public float postureWeight = 0f;

    /// <summary>
    /// Gaze body part type.
    /// </summary>
    public GazeBodyPartType GazeBodyPartType
    {
        get { return gazeBodyPartType; }
    }

    /// <summary>
    /// Gaze controller that owns this body part.
    /// </summary>
    public GazeController GazeController
    {
        get { return gazeController; }
    }

    /// <summary>
    /// true if the body part has gaze joints defined, false otherwise.
    /// </summary>
    public bool Defined
    {
        get { return gazeJoints != null && gazeJoints.Length > 0; }
    }

    /// <summary>
    /// true if this body part is an eye, false otherwise.
    /// </summary>
    public bool IsEye
    {
        get
        {
            return GazeBodyPartType == GazeBodyPartType.LEye ||
                GazeBodyPartType == GazeBodyPartType.REye;
        }
    }

    /// <summary>
    /// Top gaze joint of the body part.
    /// </summary>
    public Transform Top
    {
        get { return gazeJoints[0]; }
    }

    /// <summary>
    /// Pitch angle of the body part's top gaze joint.
    /// </summary>
    public float Pitch
    {
        get { return Top.localEulerAngles.x; }
        set { Top.localEulerAngles = new Vector3(value, Top.localEulerAngles.y, Top.localEulerAngles.z); }
    }

    /// <summary>
    /// Yaw angle of the body part's top gaze joint.
    /// </summary>
    public float Yaw
    {
        get { return Top.localEulerAngles.y; }
        set { Top.localEulerAngles = new Vector3(Top.localEulerAngles.x, value, Top.localEulerAngles.z); }
    }

    /// <summary>
    /// Roll angle of the body part's top gaze joint.
    /// </summary>
    public float Roll
    {
        get { return Top.localEulerAngles.z; }
        set { Top.localEulerAngles = new Vector3(Top.localEulerAngles.x, Top.localEulerAngles.y, value); }
    }

    /// <summary>
    /// Current position of the body part.
    /// </summary>
    public Vector3 Position
    {
        get { return gazeJoints[0].position; }
    }

    /// <summary>
    /// Current facing direction of the body part.
    /// </summary>
    public Vector3 Direction
    {
        get { return gazeJoints[0].forward; }
    }

    /// <summary>
    /// Align parameter value for the current gaze shift.
    /// </summary>
    public float _Align
    {
        get { return curAlign; }
    }

    /// <summary>
    /// Current gaze shift source direction.
    /// </summary>
    public Vector3 _SourceDirection
    {
        get { return srcDir; }
        set { srcDir = value; }
    }

    /// <summary>
    /// Aligning target direction for the current gaze shift.
    /// </summary>
    public Vector3 _TargetDirectionAlign
    {
        get { return trgDirAlign; }
        set { trgDirAlign = value; }
    }

    // How far the body part must rotate to align with the target.
    public float _Amplitude
    {
        get { return Vector3.Angle(srcDir, trgDirAlign); }
    }

    /// <summary>
    /// Fully aligning target direction for the current gaze shift.
    /// </summary>
    public Vector3 _TargetDirection
    {
        get { return trgDir; }
    }

    /// <summary>
    /// Gaze latency of the body part.
    /// </summary>
    public float _Latency
    {
        get { return latency; }
        set { latency = value; }
    }

    /// <summary>
    /// Max. gaze shift velocity of the body part.
    /// </summary>
    public float _MaxVelocity
    {
        get { return maxVelocity; }
        set { maxVelocity = value; }
    }

    /// <summary>
    /// Current gaze shift source direction.
    /// </summary>
    public Vector3 _FixSourceDirection
    {
        get { return fixSrcDir; }
        set { fixSrcDir = value; }
    }

    /// <summary>
    /// Current gaze shift source direction.
    /// </summary>
    public Vector3 _FixTargetDirectionAlign
    {
        get { return fixTrgDirAlign; }
    }

    /// <summary>
    /// Current gaze shift source direction.
    /// </summary>
    public Vector3 _FixTargetDirection
    {
        get { return fixTrgDir; }
    }

    /// <summary>
    /// true if the body part is fixating a gaze target, false otherwise.
    /// </summary>
    public bool _IsFix
    {
        get { return isFix; }
    }

    private GazeBodyPartType gazeBodyPartType;
    private GazeController gazeController;
    private bool usePelvis = false;

    // Current gaze shift state:
    private float curAlign = 1f;
    private float maxVelocity = 0f;
    private float curVelocity = 0f;
    private float latency = 0f;
    private float adjInOMR = 0f, adjOutOMR = 0f, adjUpOMR = 0f, adjDownOMR = 0f;
    private float curInOMR = 0f, curOutOMR = 0f, curUpOMR = 0f, curDownOMR = 0f;
    private Quaternion[] baseRots;
    private Quaternion[] srcRots;
    private Vector3 srcDir = Vector3.zero;
    private Vector3 trgDir = Vector3.zero;
    private Vector3 trgDirAlign = Vector3.zero;
    private float rotParam = 0f;
    private Vector3 curDir = Vector3.zero;
    private bool isFix = false;
    private Quaternion[] fixSrcRots;
    private Vector3 fixSrcDir = Vector3.zero;
    private Vector3 fixTrgDir = Vector3.zero;
    private Vector3 fixTrgDirAlign = Vector3.zero;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="gazeBodyPartType">Gaze body part type (left eye, right eye, head, or torso)</param>
    /// <param name="gazeController">Gaze controller</param>
    public GazeBodyPart(GazeBodyPartType gazeBodyPartType, GazeController gazeController)
    {
        this.gazeBodyPartType = gazeBodyPartType;
        this.gazeController = gazeController;
    }

    /// <summary>
    /// Rotate the body part's gaze joints in the specified direction.
    /// </summary>
    /// <param name="direction">Gaze direction</param>
    public void RotateTowards(Vector3 direction)
    {
        int nj = gazeJoints.Length;
        if (nj <= 1)
        {
            gazeJoints[0].forward = direction;
            if (IsEye)
                gazeJoints[0].localRotation = Quaternion.Euler(gazeJoints[0].localEulerAngles.x,
                    gazeJoints[0].localEulerAngles.y, 0f);
            return;
        }

        // Gaze directions and joint rotations
        Vector3 trgDir, trgDirAlign, srcDir, baseDir;
        Quaternion trgRot, trgRotAlign, trgRotHAlign;

        // Initialize joint indices and contributions
        int jin = 0;
        int ji1 = jin + nj - 1;
        float cprev = 0f, c, c1;
        int jic;
        bool isTorso = usePelvis && gazeBodyPartType == GazeBodyPartType.Torso;

        // Apply rotational contribution of each joint in the chain
        for (int ji = ji1; ji >= jin; --ji)
        {
            var curJoint = gazeJoints[ji];

            // Compute joint contribution
            if (isTorso && ji == ji1)
            {
                c1 = gazeController.pelvisAlign;
            }
            else
            {
                jic = ji - jin + 1;
                c = isTorso ? ((float)((nj - jic) * (nj - jic + 1))) / (nj * (nj - 1)) :
                    ((float)((nj - jic + 1) * (nj - jic + 2))) / (nj * (nj + 1));
                c1 = (c - cprev) / (1f - cprev);
                cprev = c;
            }

            // Get current joint's source and target gaze directions
            curJoint.localRotation = Quaternion.identity;
            srcDir = curJoint.InverseTransformDirection(curJoint.forward);
            trgDir = curJoint.InverseTransformDirection(direction);

            // Compute current joint's contribution to the overall rotation
            trgRot = Quaternion.FromToRotation(srcDir, trgDir);
            trgRotAlign = Quaternion.Slerp(Quaternion.identity, trgRot, c1);
            if (gazeController.removeRoll)
            {
                curJoint.localRotation = trgRotAlign;
                trgRotAlign = Quaternion.Euler(curJoint.rotation.eulerAngles.x, curJoint.rotation.eulerAngles.y, 0f);
                curJoint.rotation = trgRotAlign;
                trgRotAlign = curJoint.localRotation;
                //trgRotAlign = Quaternion.Euler(trgRotAlign.eulerAngles.x, trgRotAlign.eulerAngles.y, 0f);
            }

            // Get current joint's target gaze direction in the horizontal plane
            curJoint.localRotation = trgRotAlign;
            trgDirAlign = curJoint.forward;
            trgDirAlign = new Vector3(trgDirAlign.x, 0f, trgDirAlign.z);
            curJoint.localRotation = Quaternion.identity;
            trgDirAlign = curJoint.InverseTransformDirection(trgDirAlign);

            // Get current joint's base gaze direction in horizontal plane
            curJoint.localRotation = baseRots[ji];
            baseDir = curJoint.forward;
            baseDir = new Vector3(baseDir.x, 0f, baseDir.z);
            curJoint.localRotation = Quaternion.identity;
            baseDir = curJoint.InverseTransformDirection(baseDir);

            // Align base gaze direction with the target gaze direction in the horizontal plane
            trgRotHAlign = Quaternion.FromToRotation(baseDir, trgDirAlign);
            trgRotHAlign = baseRots[ji] * trgRotHAlign;

            // Blend rotations
            Quaternion rot = Quaternion.Slerp(trgRotAlign, trgRotHAlign, postureWeight);
            rot = Quaternion.Slerp(baseRots[ji], rot, gazeController.weight);
            curJoint.localRotation = rot;
        }
    }

    /// <summary>
    /// Compute gaze direction which would fully align the body part with
    /// the specified target location.
    /// </summary>
    /// <param name="targetPosition">Gaze target position</param>
    /// <returns>Gaze target direction</returns>
    public Vector3 GetTargetDirection(Vector3 targetPosition)
    {
        // Store current rotation of the top joint
        Quaternion curRot = Top.localRotation;

        Quaternion trgRot = ModelUtils.LookAtRotation(Top, targetPosition);
        if (!IsEye)
        {
            // Compute target rotation adjusted for offset betwen the eyes and current body part
            Top.localRotation = trgRot;
            Vector3 llr = Top.InverseTransformDirection((gazeController.lEye.Position -
                gazeController.rEye.Position).normalized);
            Vector3 lvt1 = Top.InverseTransformDirection(Direction);
            Vector3 lup = Vector3.Cross(llr, lvt1);
            Plane hpl = new Plane(lup, Top.localPosition);
            float h = hpl.GetDistanceToPoint(Top.InverseTransformPoint(gazeController.EyeCenter));
            Vector3 ldir1 = (Top.InverseTransformPoint(targetPosition) - (Top.localPosition + h * lup)).normalized;
            Quaternion ldq = Quaternion.FromToRotation(lvt1, ldir1);
            trgRot *= ldq; // final joint rotation
        }
        Top.localRotation = trgRot;
        Vector3 trgDir = Top.forward;

        // Restore original rotation of the top joint
        Top.localRotation = curRot;

        return trgDir;
    }

    /// <summary>
    /// Compute gaze direction which would align the body part with the specified target location,
    /// while constraining its movement by OMR.
    /// </summary>
    /// <param name="targetPosition">Gaze target position</param>
    /// <returns>Gaze target direction</returns>
    public Vector3 GetOMRTargetDirection(Vector3 targetPosition)
    {
        // Get fully aligning gaze direction
        Vector3 trgDir = GetTargetDirection(targetPosition);
        if (!IsEye)
            return trgDir;

        // Get OMR-constrained gaze direction
        Quaternion curRot = Top.localRotation;
        RotateTowards(trgDir);
        ClampOMR(srcRots[0]);
        trgDir = Direction;
        Top.localRotation = curRot;

        return trgDir;
    }

    /// <summary>
    /// true if the current eye orientation violates OMR limits, otherwise false.
    /// </summary>
    public bool CheckOMR()
    {
        float yaw = gazeBodyPartType == GazeBodyPartType.REye ? Yaw : -Yaw;
        float pitch = Pitch;
        float y2 = yaw * yaw;
        float p2 = pitch * pitch;

        float res = 0f;
        if (yaw >= 0f && pitch >= 0f)
            res = y2 / (curInOMR * curInOMR) + p2 / (curDownOMR * curDownOMR);
        else if (yaw <= 0f && pitch >= 0f)
            res = y2 / (curOutOMR * curOutOMR) + p2 / (curDownOMR * curDownOMR);
        else if (yaw <= 0f && pitch <= 0f)
            res = y2 / (curOutOMR * curOutOMR) + p2 / (curUpOMR * curUpOMR);
        else if (yaw >= 0f && pitch <= 0f)
            res = y2 / (curInOMR * curInOMR) + p2 / (curUpOMR * curUpOMR);

        return res >= 1f;
    }

    /// <summary>
    /// Clamp current eye orientation to OMR limits.
    /// </summary>
    public void ClampOMR()
    {
        ClampOMR(Quaternion.identity);
    }

    /// <summary>
    /// Clamp current eye orientation to OMR limits relative to
    /// gaze shift source orientation.
    /// </summary>
    public void ClampOMRToSource()
    {
        ClampOMR(srcRots[0]);
    }

    /// <summary>
    /// Clamp current eye orientation to OMR limits relative to
    /// gaze shift source orientation.
    /// </summary>
    public void ClampOMRToFixSource()
    {
        ClampOMR(fixSrcRots[0]);
    }

    /// <summary>
    /// Clamp current eye orientation to OMR limits.
    /// </summary>
    /// <param name="origin">Origin eye orientation to which current eye orientation should be clamped</param>
    public void ClampOMR(Quaternion origin)
    {
        // TODO: use a smarter/more efficient method to compute the clamped orientation
        Quaternion trgRot = Top.localRotation;
        Top.localRotation = origin;
        bool srcOMRReached = CheckOMR();
        for (float t = 0f; t <= 1f; )
        {
            // Update joint rotation
            Quaternion prevRot = Top.localRotation;
            Top.localRotation = Quaternion.Slerp(origin, trgRot, t);

            // Has the joint violated OMR limits?
            if (CheckOMR())
            {
                if (!srcOMRReached)
                {
                    // Yes, previous rotation is as far as we can go
                    Top.localRotation = prevRot;
                    return;
                }
            }
            else
            {
                if (srcOMRReached)
                {
                    // We were outside OMR range at the start, but now we are back in valid range
                    srcOMRReached = false;
                }
            }

            // Advance joint rotation
            t += 0.01f;
        }
    }

    // Initialize gaze body part
    public void _Init()
    {
        baseRots = new Quaternion[gazeJoints.Length];
        srcRots = new Quaternion[gazeJoints.Length];
        fixSrcRots = new Quaternion[gazeJoints.Length];
        usePelvis = GazeBodyPartType == global::GazeBodyPartType.Torso &&
            gazeJoints.Length > 0 &&
            gazeJoints[gazeJoints.Length - 1].tag == "RootBone";
    }

    // Initialize base rotations of the gaze joints (before gaze is applied)
    public void _InitBaseRotations()
    {
        for (int gji = 0; gji < gazeJoints.Length; ++gji)
            baseRots[gji] = gazeJoints[gji].localRotation;
    }

    // Apply base rotations to the gaze joints (from before gaze was applied)
    public void _ApplyBaseRotations()
    {
        for (int gji = 0; gji < gazeJoints.Length; ++gji)
            gazeJoints[gji].localRotation = baseRots[gji];
    }

    // Initialize gaze fixation for this body part
    public void _InitFix()
    {
        isFix = true;
        if (gazeController.FixGazeTarget != null)
        {
            fixSrcDir = srcDir;
            fixTrgDir = trgDir;
            fixTrgDirAlign = trgDirAlign;
        }
        else
        {
            fixSrcDir = fixTrgDir = fixTrgDirAlign = srcDir;
        }

        // Also copy over source joint rotations
        srcRots.CopyTo(fixSrcRots, 0);
    }

    // Apply gaze fixation for this body part
    public void _ApplyFix()
    {
        _UpdateFixTargetDirection();

        // Apply the new body posture
        RotateTowards(fixTrgDirAlign);
        if (GazeBodyPartType == GazeBodyPartType.Torso)
            _SolveBodyIK();
        
        if (IsEye)
        {
            // Fixation must not violate OMR
            if (CheckOMR())
                ClampOMRToFixSource();
        }
    }

    // Stop gaze  fixation for this body part
    public void _StopFix()
    {
        isFix = false;
    }

    // Initialize source rotations of the gaze joints from their current rotations
    public void _InitSourceRotations()
    {
        for (int gji = 0; gji < gazeJoints.Length; ++gji)
            srcRots[gji] = gazeJoints[gji].localRotation;
    }

    // Initialize gaze shift parameters
    public void _InitGazeShift()
    {
        align = Mathf.Clamp01(align);
        curAlign = align;
        maxVelocity = 0f;
        curVelocity = 0f;
        latency = 0f;
        curInOMR = adjInOMR = inOMR;
        curOutOMR = adjOutOMR = outOMR;
        curUpOMR = adjUpOMR = upOMR;
        curDownOMR = adjDownOMR = downOMR;
        _InitSourceRotations();
        srcDir = fixTrgDirAlign;
        trgDir = srcDir;
        trgDirAlign = srcDir;
        rotParam = 0f;
        curDir = srcDir;
    }

    // Initialize fully aligning target direction for the current gaze target
    public void _InitTargetDirection()
    {
        trgDir = GetTargetDirection(gazeController.CurrentGazeTargetPosition);
    }

    // Initialize OMR-constrained target direction for the current gaze target
    public void _InitOMRTargetDirection()
    {
        trgDirAlign = GetOMRTargetDirection(gazeController.CurrentGazeTargetPosition);
        trgDirAlign = srcDir != trgDir ?
            GeomUtil.ProjectVectorOntoPlane(trgDirAlign, Vector3.Cross(srcDir, trgDir)) : trgDir;
    }

    // Initialize motor range of the eye
    public void _InitOMR()
    {
        if (!IsEye)
            return;

        // Compute mean initial eye position (IEP)
        float lEyePitch, lEyeYaw, rEyePitch, rEyeYaw;
        gazeController.lEye._GetIEP(out lEyePitch, out lEyeYaw);
        gazeController.rEye._GetIEP(out rEyePitch, out rEyeYaw);
        float pitch = (lEyePitch + rEyePitch) / 2f;
        float yaw = (lEyeYaw + rEyeYaw) / 2f;

        // Adjust OMR by IEP
        float pitchAdj = 1f / 360f * pitch + 0.75f;
        float yawAdj = 1f / 360f * yaw + 0.75f;
        curInOMR = adjInOMR = inOMR * yawAdj;
        curOutOMR = adjOutOMR = outOMR * yawAdj;
        curUpOMR = adjUpOMR = upOMR * pitchAdj;
        curDownOMR = adjDownOMR = downOMR * pitchAdj;
    }

    // Update gaze direction that will align the body part with the target
    public void _UpdateTargetDirection()
    {
        float prevDistRotAlign = Vector3.Angle(srcDir, trgDirAlign);
        float prevDistRot = Vector3.Angle(srcDir, trgDir);
        trgDir = GetTargetDirection(gazeController.CurrentGazeTargetPosition);

        if (!IsEye)
        {
            float prevAlign = prevDistRot > 0.0001f ? prevDistRotAlign / prevDistRot : 1f;
            Quaternion rotAlign = Quaternion.Slerp(Quaternion.identity, Quaternion.FromToRotation(srcDir, trgDir), prevAlign);
            trgDirAlign = rotAlign * srcDir;
        }
        else
        {
            trgDirAlign = GetOMRTargetDirection(gazeController.CurrentGazeTargetPosition);
            trgDirAlign = srcDir != trgDir ?
                GeomUtil.ProjectVectorOntoPlane(trgDirAlign, Vector3.Cross(srcDir, trgDir)) :
                trgDir;
        }

        // Renormalize gaze shift progress
        float distRotAlign = Vector3.Angle(srcDir, trgDirAlign);
        rotParam = Mathf.Max(Mathf.Clamp01(rotParam * (distRotAlign > 0.00001f ? prevDistRotAlign / distRotAlign : 1f)),
            rotParam);
    }

    // Rotate the body part's gaze joints toward the current target
    public bool _AdvanceGazeShift(float deltaTime)
    {
        if (!Defined)
            return true;
        
        float dt = 0f;
        for (float t = 0; t < deltaTime; )
        {
            // Compute delta time
            t += LEAPCore.eulerTimeStep;
            dt = (t <= deltaTime) ? LEAPCore.eulerTimeStep :
                deltaTime - t + LEAPCore.eulerTimeStep;

            if (latency > 0f)
            {
                // Body part not ready to move yet, just maintain fixation
                _ApplyFix();
                _InitSourceRotations();

                // Decrement latency and stop fixation if done
                latency -= dt;
                if (latency <= 0f)
                    _StopFix();

                return false;
            }

            // Update body part velocity and (if eye) OMR
            _UpdateVelocity();
            if (IsEye)
                _UpdateOMR();

            // Rotate the body part toward the target
            float distRotDiff = dt * curVelocity;
            float distRotAlign = Vector3.Angle(_SourceDirection, _TargetDirectionAlign);
            rotParam = rotParam < 1f ? Mathf.Clamp01(rotParam + distRotDiff / distRotAlign) : 1f;
            Quaternion rot = Quaternion.Slerp(Quaternion.identity, Quaternion.FromToRotation(srcDir, trgDirAlign), rotParam);
            curDir = rot * srcDir;

            if (rotParam >= 1f)
                // Gaze shift finished
                return true;
        }

        return false;
    }

    // Apply current gaze shift posture
    public void _ApplyGazeShift()
    {
        RotateTowards(curDir);
        if (GazeBodyPartType == GazeBodyPartType.Torso)
            _SolveBodyIK();

        if (IsEye && CheckOMR())
        {
            // If OMR has been reached, clamp the rotation
            ClampOMRToSource(); // TODO: keep an eye out for discontinuities in orientation (LOL)
        }
    }

    // Apply source rotations (before gaze shift start) to the gaze joints
    public void _ApplySourceRotations()
    {
        for (int gji = 0; gji < gazeJoints.Length; ++gji)
            gazeJoints[gji].localRotation = srcRots[gji];
    }

    // Apply source rotations (before gaze shift start) from the previous gaze shift
    // to the gaze joints
    public void _ApplyFixSourceRotations()
    {
        for (int gji = 0; gji < gazeJoints.Length; ++gji)
            gazeJoints[gji].localRotation = fixSrcRots[gji];
    }

    // Get current OMR
    public void _GetOMR(out float inOMR, out float outOMR, out float upOMR, out float downOMR)
    {
        inOMR = curInOMR;
        outOMR = curOutOMR;
        upOMR = curUpOMR;
        downOMR = curDownOMR;
    }

    // Get a snapshot of the current state of the gaze body part
    public GazeControllerState.GazeBodyPartState _GetRuntimeState()
    {
        GazeControllerState.GazeBodyPartState state = new GazeControllerState.GazeBodyPartState();
        state.gazeBodyPartType = (int)GazeBodyPartType;
        state.align = align;
        state.velocity = velocity;
        state.inOMR = inOMR;
        state.outOMR = outOMR;
        state.upOMR = upOMR;
        state.downOMR = downOMR;
        state.postureWeight = postureWeight;
        state.curAlign = curAlign;
        state.maxVelocity = maxVelocity;
        state.curVelocity = curVelocity;
        state.latency = latency;
        state.adjInOMR = adjInOMR;
        state.adjOutOMR = adjOutOMR;
        state.adjUpOMR = adjUpOMR;
        state.adjDownOMR = adjDownOMR;
        state.baseRots = (Quaternion[])baseRots.Clone();
        state.srcRots = (Quaternion[])srcRots.Clone();
        state.srcDir = srcDir;
        state.trgDir = trgDir;
        state.trgDirAlign = trgDirAlign;
        state.rotParam = rotParam;
        state.curDir = curDir;
        state.isFix = isFix;
        state.fixSrcRots = (Quaternion[])fixSrcRots.Clone();
        state.fixSrcDir = fixSrcDir;
        state.fixTrgDir = fixTrgDir;
        state.fixTrgDirAlign = fixTrgDirAlign;

        return state;
    }

    // Compute the difference between OMR-constrained eye target rotation and the target rotation
    // needed to align the eye with the target
    public Quaternion _GetOMRTargetRotationDiff()
    {
        Quaternion trgRot = ModelUtils.LookAtRotation(Top, gazeController.CurrentGazeTargetPosition);
        Quaternion curRot = Top.localRotation;
        Top.localRotation = trgRot;
        ClampOMRToSource();
        Quaternion trgRotOMR = Top.localRotation;
        Top.localRotation = curRot;
        
        return Quaternion.Inverse(trgRotOMR) * trgRot;
    }

    // Set the current state of the gaze body part from the snapshot
    public void _SetRuntimeState(GazeControllerState.GazeBodyPartState state)
    {
        gazeBodyPartType = (GazeBodyPartType)state.gazeBodyPartType;
        align = state.align;
        velocity = state.velocity;
        inOMR = state.inOMR;
        outOMR = state.outOMR;
        upOMR = state.upOMR;
        downOMR = state.downOMR;
        postureWeight = state.postureWeight;
        curAlign = state.curAlign;
        maxVelocity = state.maxVelocity;
        curVelocity = state.curVelocity;
        latency = state.latency;
        adjInOMR = state.adjInOMR;
        adjOutOMR = state.adjOutOMR;
        adjUpOMR = state.adjUpOMR;
        adjDownOMR = state.adjDownOMR;
        baseRots = (Quaternion[])state.baseRots.Clone();
        srcRots = (Quaternion[])state.srcRots.Clone();
        srcDir = state.srcDir;
        trgDir = state.trgDir;
        trgDirAlign = state.trgDirAlign;
        rotParam = state.rotParam;
        curDir = state.curDir;
        isFix = state.isFix;
        fixSrcRots = (Quaternion[])state.fixSrcRots.Clone();
        fixSrcDir = state.fixSrcDir;
        fixTrgDir = state.fixTrgDir;
        fixTrgDirAlign = state.fixTrgDirAlign;
    }

    // Get an initial state snapshot for the gaze body part
    public GazeControllerState.GazeBodyPartState _GetInitRuntimeState()
    {
        GazeControllerState.GazeBodyPartState state = new GazeControllerState.GazeBodyPartState();
        state.gazeBodyPartType = (int)GazeBodyPartType;
        state.align = 1f;
        state.postureWeight = IsEye ? 0f : 1f;
        state.curAlign = 1f;
        state.maxVelocity = 0f;
        state.curVelocity = 0f;
        state.latency = 0f;
        state.adjInOMR = 0f;
        state.adjOutOMR = 0f;
        state.adjUpOMR = 0f;
        state.adjDownOMR = 0f;
        state.baseRots = new Quaternion[gazeJoints.Length];
        state.srcRots = new Quaternion[gazeJoints.Length];
        state.srcDir = Vector3.zero;
        state.trgDir = Vector3.zero;
        state.trgDirAlign = Vector3.zero;
        state.rotParam = 0f;
        state.curDir = Vector3.zero;
        state.isFix = false;
        state.fixSrcRots = new Quaternion[gazeJoints.Length];
        state.fixSrcDir = Vector3.zero;
        state.fixTrgDir = Vector3.zero;
        state.fixTrgDirAlign = Vector3.zero;

        return state;
    }

    // Compute IEP of the specified eye (pitch and yaw given a contralateral target)
    private void _GetIEP(out float pitch, out float yaw)
    {
        // Get source and target eye orientations
        Quaternion srcRot = Top.localRotation;
        Top.localRotation = ModelUtils.LookAtRotation(Top, gazeController.MovingGazeTargetPosition);
        float trgPitch = Pitch;
        float trgYaw = Yaw;
        Top.localRotation = srcRot;

        // Compute IEP
        pitch = ((Pitch > 0f && trgPitch < 0f) || (Pitch < 0f && trgPitch > 0f)) ?
            Mathf.Abs(Pitch) : 0f;
        yaw = ((Yaw > 0f && trgYaw < 0f) || (Yaw < 0f && trgYaw > 0f)) ?
            Mathf.Abs(Yaw) : 0f;
    }

    // Update body part velocity based on gaze shift progress
    private void _UpdateVelocity()
    {
        if (IsEye)
        {
            // Compute gaze shift progress based on the eye that has farthest to move
            var lEye = gazeController.lEye;
            var rEye = gazeController.rEye;
            float lEyeDistRot = Vector3.Angle(lEye._SourceDirection, lEye._TargetDirectionAlign);
            float rEyeDistRot = Vector3.Angle(rEye._SourceDirection, rEye._TargetDirectionAlign);
            float p = Mathf.Clamp01(lEyeDistRot > rEyeDistRot ? lEye.rotParam : rEye.rotParam);
            float p2 = p * p;

            // Update eye velocity
            curVelocity = p < 0.5f ?
                curVelocity = (p + 0.5f) * maxVelocity :
                (8f * p2 * p - 18f * p2 + 12f * p - 1.5f) * maxVelocity;
        }
        else
        {
            // Get gaze shift progress
            float p = rotParam;
            float p2 = p * p;

            // Update body part velocity
            curVelocity = p < 0.5f ?
                (p * 0.75f / 0.5f + 0.25f) * maxVelocity :
                (12f * p2 * p - 27f * p2 + 18f * p - 2.75f) * maxVelocity;
        }
    }

    // Update eye motor range based on gaze shift progress
    private void _UpdateOMR()
    {
        if (!IsEye)
            return;

        float headVelocity = gazeController.head.curVelocity;
        curUpOMR = adjUpOMR * (-1f / 600f * headVelocity + 1f);
        curDownOMR = adjDownOMR * (-1f / 600f * headVelocity + 1f);
        curInOMR = adjInOMR * (-1f / 600f * headVelocity + 1f);
        curOutOMR = adjOutOMR * (-1f / 600f * headVelocity + 1f);
    }

    // Update gaze direction that will align the body part with the target during fixation
    private void _UpdateFixTargetDirection()
    {
        if (gazeController.FixGazeTarget == null)
            return;
        
        float prevDistRotAlign = Vector3.Angle(fixSrcDir, fixTrgDirAlign);
        float prevDistRot = Vector3.Angle(fixSrcDir, fixTrgDir);
        fixTrgDir = GetTargetDirection(gazeController.FixGazeTargetPosition);

        if (!IsEye)
        {
            float prevAlign = prevDistRot > 0.0001f ? prevDistRotAlign / prevDistRot : 1f;
            Quaternion rotAlign = Quaternion.Slerp(Quaternion.identity, Quaternion.FromToRotation(fixSrcDir, fixTrgDir), prevAlign);
            fixTrgDirAlign = rotAlign * fixSrcDir;
        }
        else
        {
            fixTrgDirAlign = GetOMRTargetDirection(gazeController.FixGazeTargetPosition);
            fixTrgDirAlign = fixSrcDir != fixTrgDir ?
                GeomUtil.ProjectVectorOntoPlane(fixTrgDirAlign, Vector3.Cross(fixSrcDir, fixTrgDir)) :
                fixTrgDir;
        }
    }

    // Solve for body posture using an IK solver
    private void _SolveBodyIK()
    {
        if (!LEAPCore.useGazeIK)
            return;

        var bodySolver = gazeController.gameObject.GetComponent<BodyIKSolver>();
        if (bodySolver != null && bodySolver.enabled)
        {
            bodySolver.InitGazePose();
            bodySolver.Solve();
        }
    }
}

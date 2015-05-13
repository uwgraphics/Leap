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

public enum GazeJointType
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
    // Runtime state of a gaze joint
    public struct GazeJointState
    {
        public GazeJointType gazeJointType;
        public float velocity;
        public float upMR, downMR, inMR, outMR;
        public float align, latency;
        public Quaternion rot, srcRot, trgRot, trgRotAlign, trgRotMR;
        public float distRotAlign, distRotMR, rotParamAlign, rotParamMR;
        public float maxVelocity, curVelocity, latencyTime;
        public bool mrReached, trgReached;
        public float adjUpMR, adjDownMR, adjInMR, adjOutMR, curUpMR, curDownMR, curInMR, curOutMR;
        public float curAlign;
        public bool isVOR;
        public Quaternion fixSrcRot, fixTrgRot, fixTrgRotAlign;
        public float fixRotParamAlign;
        public Quaternion baseRot, expressiveRot, fixExpressiveRot;
    }

    public int stateId;
    public GameObject gazeTarget;
    public bool doGazeShift;
    public bool stopGazeShift;
    public bool fixGaze;
    public bool useTorso;
    public float predictability;
    public Vector3 movingTargetPositionOffset;
    public bool stylizeGaze;
    public float quickness;
    public float eyeSize;
    public float eyeTorque;
    public float eyeAlign;
    public bool enableED;
    public bool enableAEM;
    public bool enableEAH;
    public float maxCrossEyedness;
    public bool removeRoll;
    public float amplitude;
    public GameObject currentGazeTarget;
    public Vector3 effGazeTargetPosition;
    public GameObject fixGazeTarget;
    public float weight;
    public float fixWeight;
    public GazeJointState[] gazeJointStates;
}

/// <summary>
/// Gaze animation controller. 
/// </summary>
public class GazeController : AnimController
{
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
    /// Chain of joints that are directed by the gaze controller.
    /// </summary>
    public GazeJoint[] gazeJoints = new GazeJoint[0];

    /// <summary>
    /// If true and torso joints are defined, the gaze controller will recruit
    /// them when performing the gaze shift; otherwise it will only
    /// move the eyes and head.
    /// </summary>
    public bool useTorso = true;

    /// <summary>
    /// Predictability of the gaze target (0-1). 
    /// </summary>
    public float predictability = 1f;

    /// <summary>
    /// Relative position of the gaze target in the future
    /// (used in computing gaze shift parameters for rel. moving targets).
    /// </summary>
    public Vector3 movingTargetPositionOffset;

    /// <summary>
    /// If true, cartoon animation principles will be applied to 
    /// gaze shifts.
    /// </summary>
    public bool stylizeGaze = false;

    /// <summary>
    /// How quick the character is relative to a normal human. 
    /// </summary>
    public float quickness = 1f;

    /// <summary>
    /// Character's eye size (as factor of normal human eyes). 
    /// </summary>
    public float eyeSize = 1f;

    /// <summary>
    /// Strength of eye muscles relative to normal human eye muscles.
    /// </summary>
    public float eyeTorque = 1f;

    /// <summary>
    /// When gaze is stylized, this parameter controls how
    /// much the eyes will align with the gaze target.
    /// </summary>
    public float eyeAlign = 1f;

    /// <summary>
    /// Enable eye divergence technique for stylized gaze.
    /// </summary>
    public bool enableED = true;

    /// <summary>
    /// Enable asymmetric eye movements technique for stylized gaze.
    /// </summary>
    public bool enableAEM = true;

    /// <summary>
    /// Enable eyes-ahead technique for stylized gaze.
    /// </summary>
    public bool enableEAH = true;

    /// <summary>
    /// How cross-eyed the character may become during gaze shifts. 
    /// </summary>
    public float maxCrossEyedness = 2f;

    /// <summary>
    /// If true, roll component of gaze joint rotations will be removed
    /// during animation.
    /// </summary>
    public bool removeRoll = true;

    /// <summary>
    /// Weight with which gaze is applied for the current fixation.
    /// </summary>
    public float fixWeight = 1f;

    /// <summary>
    /// Weight with which vertical head posture from the base animation
    /// is preserved when applying gaze.
    /// </summary>
    public float headPostureWeight = 0f;

    /// <summary>
    /// Weight with which vertical torso posture from the base animation
    /// is preserved when applying gaze.
    /// </summary>
    public float torsoPostureWeight = 1f;

    /// <summary>
    /// Weight with which expressive gaze is applied.
    /// </summary>
    public float expressiveWeight = 0f;

    // Shorthand for getting the eye joints
    [HideInInspector]
    public GazeJoint[] eyes = new GazeJoint[0];
    // Shorthand for getting the neck joints
    [HideInInspector]
    public GazeJoint[] head = new GazeJoint[0];
    // Shorthand for getting the torso joints
    [HideInInspector]
    public GazeJoint[] torso = new GazeJoint[0];

    protected int lEyeIndex = -1;
    protected int rEyeIndex = -1;
    protected int headIndex = -1;
    protected int torsoIndex = -1;

    protected GameObject curGazeTarget = null; // Current gaze target
    protected Vector3 curMovingTargetPosOff = Vector3.zero; // Rel. position offset of the current target in near future
    protected bool curUseTorso = true;
    protected float adjEyeAlign = 1f;
    protected float maxCrEyedView = 0f; // Maximum cross-eyedness allowed, adjusted by view angle
    protected Vector3 effGazeTargetPos; // Effective gaze target position

    protected GameObject helperTarget; // Allows gazing at arbitrary point in space
    protected GameObject aheadHelperTarget; // Allows the character to gaze ahead
    protected GameObject fixHelperTarget; // Allows the character to fixate a point in space
    protected GameObject cam = null;
    protected BlinkController blinkCtrl = null;
    protected FaceController faceCtrl = null;
    protected bool reenableRandomHeadMotion = false;
    protected bool reenableRandomSpeechMotion = false;

    protected Quaternion[] curRots;

    /// <summary>
    /// Shorthand for getting the left eye.
    /// </summary>
    public virtual GazeJoint LEye
    {
        get
        {
            if (lEyeIndex < 0)
                lEyeIndex = _FindGazeJointIndex("LEyeBone");
            
            return lEyeIndex >= 0 ? gazeJoints[lEyeIndex] : null;
        }
    }

    /// <summary>
    /// Shorthand for getting the right eye.
    /// </summary>
    public virtual GazeJoint REye
    {
        get
        {
            if (rEyeIndex < 0)
                rEyeIndex = _FindGazeJointIndex("REyeBone");

            return rEyeIndex >= 0 ? gazeJoints[rEyeIndex] : null;
        }
    }

    /// <summary>
    /// Shorthand for getting the head.
    /// </summary>
    public virtual GazeJoint Head
    {
        get
        {
            if (headIndex > 0 && gazeJoints.Length <= headIndex)
                headIndex = -1;

            if (headIndex < 0)
                headIndex = _FindGazeJointIndex("HeadBone");

            return headIndex >= 0 ? gazeJoints[headIndex] : null;
        }
    }

    /// <summary>
    /// Shorthand for getting the torso.
    /// </summary>
    public virtual GazeJoint Torso
    {
        get
        {
            if (torsoIndex > 0 && gazeJoints.Length <= torsoIndex)
                torsoIndex = -1;

            if (torsoIndex < 0)
                torsoIndex = _FindGazeJointIndex("TorsoBone");

            return torsoIndex >= 0 ? gazeJoints[torsoIndex] : null;
        }
    }

    /// <summary>
    /// Current gaze shift/fixation target.
    /// </summary>
    public virtual GameObject CurrentGazeTarget
    {
        get { return curGazeTarget; }
        protected set { curGazeTarget = value; }
    }

    /// <summary>
    /// Effective position of the gaze target,
    /// with view-based eye alignment and cross-eyedness
    /// correction taken into account.
    /// </summary>
    public virtual Vector3 EffGazeTargetPosition
    {
        get
        {
            return stylizeGaze ? effGazeTargetPos : curGazeTarget.transform.position;
        }
    }

    /// <summary>
    /// Gaze target for VOR.
    /// </summary>
    public virtual GameObject FixGazeTarget
    {
        get;
        protected set;
    }

    /// <summary>
    /// Helper gaze target for looking straight ahead.
    /// </summary>
    public virtual GameObject AheadHelperTarget
    {
        get { return aheadHelperTarget; }
    }

    /// <summary>
    /// Helper gaze target for fixation.
    /// </summary>
    public virtual GameObject FixHelperTarget
    {
        get { return fixHelperTarget; }
    }

    /// <summary>
    /// If true, the agent is gazing ahead rather than fixating a specific target.
    /// </summary>
    public virtual bool IsGazingAhead
    {
        get { return aheadHelperTarget == curGazeTarget || curGazeTarget == null; }
    }

    /// <summary>
    /// Centroid of the eyes.
    /// </summary>
    public virtual Vector3 EyeCenter
    {
        get { return 0.5f * (LEye.bone.position + REye.bone.position); }
    }

    /// <summary>
    /// Averaged gaze direction of the eyes.
    /// </summary>
    public virtual Vector3 EyeDirection
    {
        get { return (0.5f * (LEye.Direction + REye.Direction)).normalized; }
    }

    /// <summary>
    /// Gaze shift amplitude.
    /// </summary>
    public float Amplitude
    {
        get;
        set;
    }

    /// <summary>
    /// Measure of how cross-eyed the character is, as angle between
    /// eye direction vectors.
    /// </summary>
    public virtual float CrossEyedness
    {
        get
        {
            if (LEye == null || REye == null)
                return 0;

            float ce = Vector3.Angle(LEye.Direction, REye.Direction);
            if (ce > 0.00001f)
            {
                // Is the character cross-eyed or eye-divergent?
                float lt, rt;
                GeomUtil.ClosestPointsOn2Lines(
                    LEye.bone.position, LEye.bone.position + LEye.Direction,
                    REye.bone.position, REye.bone.position + REye.Direction,
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
            if (LEye == null || REye == null)
                return 0;

            float ed = Vector3.Angle(LEye.Direction, REye.Direction);
            if (ed > 0.00001f)
            {
                // Is the character cross-eyed or eye-divergent?
                float lt, rt;
                GeomUtil.ClosestPointsOn2Lines(
                    LEye.bone.position, LEye.bone.position + LEye.Direction,
                    REye.bone.position, REye.bone.position + REye.Direction,
                    out lt, out rt);
                if (rt > 0)
                    ed = 0;
                //return 1f;
            }

            return ed;
        }
    }

    /// <summary>
    /// Get the index of the last gaze joint recruited by the gaze controller
    /// for the current gaze shift.
    /// </summary>
    public virtual int LastGazeJointIndex
    {
        get { return curUseTorso ? gazeJoints.Length - 1: torsoIndex - 1; }
    }

    /// <summary>
    /// Get the number of joints of the specified type.
    /// </summary>
    /// <returns>
    /// Number of gaze joints of type.
    /// </returns>
    /// <param name='type'>
    /// Gaze joint type.
    /// </param>
    public virtual int GetNumGazeJointsInChain(GazeJointType type)
    {
        int num_joints = 0;
        foreach (GazeJoint joint in gazeJoints)
            if (joint.type == type)
                ++num_joints;

        return num_joints;
    }

    /// <summary>
    /// Gets the last gaze joint in the chain of joints
    /// of the specified type.
    /// </summary>
    /// <returns>
    /// The last gaze joint in chain.
    /// </returns>
    /// <param name='type'>
    /// Gaze joint type.
    /// </param>
    public virtual GazeJoint GetLastGazeJointInChain(GazeJointType type)
    {
        foreach (GazeJoint joint in gazeJoints)
            if (joint.type == type)
                return joint;

        return null;
    }

    /// <summary>
    /// Finds the index of the specified gaze joint.
    /// </summary>
    /// <returns>
    /// Gaze joint index.
    /// </returns>
    /// <param name='joint'>
    /// Gaze joint.
    /// </param>
    public virtual int FindGazeJointIndex(GazeJoint joint)
    {
        for (int ji = 0; ji < gazeJoints.Length; ++ji)
            if (gazeJoints[ji] == joint)
                return ji;

        return -1;
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
        movingTargetPositionOffset = Vector3.zero;

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
        if (FixGazeTarget == helperTarget)
        {
            // Agent is currently fixating the helper target at a different position,
            // so we need to replace it with a fixation helper
            FixGazeTarget = fixHelperTarget;
            FixHelperTarget.transform.position = helperTarget.transform.position;
        }

        helperTarget.transform.position = gazeTargetWPos;
        GazeAt(helperTarget);
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
            FixGazeTarget = fixHelperTarget;
            FixHelperTarget.transform.position = AheadHelperTarget.transform.position;
        }

        // Position the helper gaze target in front of the agent
        Vector3 bodyPos = ModelController.BodyPosition;
        Vector3 bodyDir = ModelController.BodyDirection;
        float height = ModelController.GetInitWorldPosition(Head.bone).y;
        Vector3 pos = (new Vector3(bodyPos.x, height, bodyPos.z)) + height * bodyDir;
        aheadHelperTarget.transform.position = pos;

        GazeAt(aheadHelperTarget);
    }

    /// <summary>
    /// Interrupt ongoing gaze shift.
    /// </summary>
    public virtual void StopGaze()
    {
        stopGazeShift = true;

        Debug.LogWarning("Stopping gaze shift towards target " + curGazeTarget.name);
    }

    // Initialize fixation of the current gaze target
    public virtual void _InitVOR()
    {
        // Set fixation target
        if (!IsGazingAhead)
        {
            // VOR towards current gaze target
            FixGazeTarget = curGazeTarget;
        }
        else
        {
            // No current gaze target, VOR towards nothing
            FixGazeTarget = null;
        }

        // Set fixation weight
        fixWeight = weight;

        // Initialize target rotations of joints for the fixation target
        for (int ji = LastGazeJointIndex; ji >= 0; --ji)
        {
            GazeJoint joint = gazeJoints[ji];
            joint._InitVOR();
        }
    }

    // Orient the gaze joints to the source pose (agent gazing
    // at the current gaze target)
    public virtual void _ApplySourcePose()
    {
        for (int ji = LastGazeJointIndex; ji >= 0; --ji)
        {
            curRots[ji] = gazeJoints[ji].bone.localRotation;
            gazeJoints[ji].bone.localRotation = gazeJoints[ji].srcRot;
        }
    }

    // Orient the gaze joints to the target pose (agent gazing
    // at the current gaze target)
    public virtual void _ApplyTargetPose()
    {
        for (int ji = LastGazeJointIndex; ji >= 0; --ji)
        {
            GazeJoint joint = gazeJoints[ji];
            curRots[ji] = joint.bone.localRotation;
            Quaternion trgrot = joint._ComputeTargetRotation(EffGazeTargetPosition);

            if (ji == torsoIndex)
            {
                // Torso joint
                joint.bone.localRotation = joint.trgRotAlign;
                _ApplyRotation(joint);
                _SolveBodyIK();
            }
            else if (headIndex > 0 && ji >= headIndex)
            {
                // Head joint
                joint.bone.localRotation = joint.srcRot;
            }
            else if (joint.IsEye)
            {
                // Eye
                joint.bone.localRotation = trgrot;
            }
        }
    }

    // Orient the gaze joints to the original pose
    public virtual void _ReapplyCurrentPose()
    {
        for (int ji = LastGazeJointIndex; ji >= 0; --ji)
            gazeJoints[ji].bone.localRotation = curRots[ji];
    }

    // Remove roll component from rotations of all the gaze joints
    public virtual void _RemoveRoll()
    {
        for (int gji = 0; gji <= LastGazeJointIndex; ++gji)
        {
            var gazeJoint = gazeJoints[gji];
            gazeJoint._RemoveRoll();
        }
    }

    // Apply rotation of the specified gaze joint such that it is distributed
    // across all the joints of its chain
    public virtual void _ApplyRotation(GazeJoint joint)
    {
        var last = GetLastGazeJointInChain(joint.type);
        if (joint != last)
            throw new Exception("Last gaze joint in a body part's chain must be specified when applying joint rotations");

        int nj = GetNumGazeJointsInChain(joint.type);
        if (nj <= 1)
            // No need to redistribute rotation when there is only 1 joint in the chain
            return;

        // Gaze directions and joint rotations
        Vector3 trgDirW = joint.Direction;
        Vector3 trgDir, trgDirAlign, srcDir, baseDir;
        Quaternion trgRot, trgRotAlign, trgRotHAlign;

        // Initialize joint indices and contributions
        int jin = FindGazeJointIndex(joint);
        int ji1 = jin + nj - 1;
        float cprev = 0f, c, c1;
        int jic;

        // Compute vertical posture weight
        float weight = 1f;
        switch (joint.type)
        {
            case GazeJointType.Head:
                weight = headPostureWeight;
                break;
            case GazeJointType.Torso:
                weight = torsoPostureWeight;
                break;
            default:
                break;
        }

        for (int ji = ji1; ji >= jin; --ji)
        {
            var curJoint = gazeJoints[ji];

            jic = ji - jin + 1;
            c = ((float)((nj - jic + 1) * (nj - jic + 2))) / (nj * (nj + 1));
            c1 = (c - cprev) / (1f - cprev);
            cprev = c;

            // Get current joint's source and target gaze directions
            curJoint.bone.localRotation = Quaternion.identity;
            srcDir = curJoint.bone.InverseTransformDirection(curJoint.bone.forward);
            trgDir = curJoint.bone.InverseTransformDirection(trgDirW);

            // Compute current joint's contribution to the overall rotation
            trgRot = Quaternion.FromToRotation(srcDir, trgDir);
            trgRotAlign = Quaternion.Slerp(Quaternion.identity, trgRot, c1);

            // Get current joint's base gaze direction in horizontal plane
            curJoint.bone.localRotation = curJoint.baseRot;
            baseDir = curJoint.bone.forward;
            baseDir = new Vector3(baseDir.x, 0f, baseDir.z);

            // Get current joint's source and target gaze directions, projected into horizontal plane
            curJoint.bone.localRotation = Quaternion.identity;
            srcDir = curJoint.bone.forward;
            srcDir = new Vector3(srcDir.x, 0f, srcDir.z);
            trgDir = new Vector3(trgDirW.x, 0f, trgDirW.z);

            // Compute current joint's contribution to the overall rotation, projected into horizontal plane
            trgRot = Quaternion.FromToRotation(srcDir, trgDir);
            trgRotHAlign = Quaternion.Slerp(Quaternion.identity, trgRot, c1);
            trgDirAlign = trgRotHAlign * srcDir;
            trgRotHAlign = Quaternion.FromToRotation(baseDir, trgDirAlign);
            trgRotHAlign = curJoint.baseRot * trgRotHAlign;

            // Blend between fully and horizontally aligning rotations
            curJoint.bone.localRotation = Quaternion.Slerp(trgRotAlign, trgRotHAlign, weight);
        }
    }

    // Initialize gaze parameters at the start of a gaze shift
    public virtual void _InitGazeParams()
    {
        curGazeTarget = gazeTarget;
        curMovingTargetPosOff = movingTargetPositionOffset;
        curUseTorso = useTorso;

        // Eyes geometry parameters
        eyeSize = Mathf.Clamp(eyeSize, 1f, 5.8f); // eyes can't be larger than the head...
        eyeTorque = eyeTorque < 1f ? 1f : eyeTorque;

        // Initialize per-joint parameters
        for (int gji = 0; gji <= LastGazeJointIndex; ++gji)
        {
            GazeJoint joint = gazeJoints[gji];
            joint._InitGazeParams();
        }
        curRots = new Quaternion[gazeJoints.Length];

        // Initialize effective gaze target
        effGazeTargetPos = curGazeTarget.transform.position;
        Amplitude = 0f;
        // TODO: Compute gaze shift parameters based on high-level parameters
    }

    // Initialize target pose for the gaze shift towards the current gaze target
    public virtual void _InitTargetRotations()
    {
        if (stylizeGaze)
            _InitTargetRotationsEffective();
        else
            _InitTargetRotationsBase();
    }

    // Initialize gaze joint latencies at the start of the gaze shift
    public virtual void _InitLatencies()
    {
        // Compute head joint latencies
        Head.latency = _ComputeHeadLatency();
        for (int gji = torsoIndex - 1;
            headIndex > 0 && gji >= headIndex; --gji)
        {
            GazeJoint joint = gazeJoints[gji];
            joint.latency = Head.latency;
        }

        if (Torso != null)
        {
            // Compute torso joint latencies
            Torso.latency = _ComputeTorsoLatency();
            for (int gji = LastGazeJointIndex;
                torsoIndex > 0 && curUseTorso && gji >= torsoIndex; --gji)
            {
                GazeJoint joint = gazeJoints[gji];
                joint.latency = Torso.latency;
            }
        }

        // What is the earliest latency time?
        float minLatency = gazeJoints.Min(j => j.latency);

        // Initialize latency times
        for (int i = 0; i <= LastGazeJointIndex; ++i)
        {
            GazeJoint joint = gazeJoints[i];
            GazeJoint last = GetLastGazeJointInChain(joint.type);
            joint.latencyTime += (minLatency >= 0f ? last.latency : last.latency + Mathf.Abs(minLatency));
            joint.latencyTime /= 1000f;
        }
    }

    // Calculate peak velocities in the gaze shift
    public virtual void _CalculateMaxVelocities()
    {
        if (Torso != null && curUseTorso)
        {
            // For the upper body joints
            float torsoDistRotAlign = _ComputeDistRotAlignForMovingTarget(Torso);
            for (int gji = torsoIndex; gji < gazeJoints.Length; ++gji)
            {
                GazeJoint joint = gazeJoints[gji];
                if (joint == Torso)
                {
                    joint.maxVelocity = (4f / 3f) * (joint.velocity / 15f) * torsoDistRotAlign +
                        joint.velocity / 0.5f;
                }
                else
                {
                    joint.maxVelocity = Torso.maxVelocity;
                }
            }
        }

        // For the head joints
        float headDistRotAlign = _ComputeDistRotAlignForMovingTarget(Head);
        for (int gji = headIndex; torsoIndex > -1 ? gji < torsoIndex : gji <= LastGazeJointIndex; ++gji)
        {
            GazeJoint joint = gazeJoints[gji];

            if (joint == Head)
            {
                joint.maxVelocity = (4f / 3f) * (joint.velocity / 50f) * headDistRotAlign +
                    joint.velocity / 2.5f;
            }
            else
            {
                joint.maxVelocity = Head.maxVelocity;
            }
        }

        // How opposing are the eye and head rotations? (rare)
        float[] opc = new float[eyes.Length];
        for (int ei = 0; ei < eyes.Length; ++ei)
            opc[ei] = 0f;
        if (Head != null)
        {
            Quaternion hrot = Quaternion.Inverse(Head.srcRot) * Head.trgRotAlign;
            if (hrot != Quaternion.identity)
            {
                Quaternion curhrot = Head.bone.localRotation;

                Vector3 vh1 = Head.Direction;
                Head.bone.localRotation = Head.trgRotAlign;
                Vector3 vh2 = Head.Direction;
                Vector3 vh = vh2 - vh1;
                Head.bone.localRotation = curhrot;

                for (int ei = 0; ei < eyes.Length; ++ei)
                {
                    GazeJoint eye = eyes[ei];
                    Quaternion curerot = eye.bone.localRotation;

                    Vector3 ve1 = eye.Direction;
                    eye.bone.localRotation = eye._ComputeTargetRotation(EffGazeTargetPosition);
                    Quaternion erot = Quaternion.Inverse(eye.srcRot) * eye.bone.localRotation;
                    if (erot == Quaternion.identity)
                        break;
                    Vector3 ve2 = eye.Direction;
                    Vector3 ve = ve2 - ve1;

                    opc[ei] = Vector3.Angle(ve, vh) / 180f;
                    opc[ei] = Mathf.Clamp01(opc[ei]);
                    //opc[ei] = opc[ei] < 1f/3f ? 0f : (2f/3f)*(opc[ei]-1/3f);

                    eye.bone.localRotation = curerot;
                }

                Head.bone.localRotation = curhrot;
            }
        }

        // For the eyes
        if (stylizeGaze)
        {
            // Find the longest eye rotation distance
            float Amax = -float.MaxValue;
            foreach (GazeJoint eye in eyes)
                Amax = Mathf.Max(eye.distRotMR, Amax);

            for (int ei = 0; ei < eyes.Length; ++ei)
            {
                GazeJoint eye = eyes[ei];

                eye.maxVelocity = eye.velocity;
                // Slow down the eyes in proportion to how large they are
                float es3 = Mathf.Pow(eyeSize, 3f);
                //eye.maxVelocity = eye.velocity*eyeTorque/es3;
                eye.maxVelocity = eye.velocity / es3;

                // Compute range of eye torques
                float etu = es3;
                float etl = eyeTorque;

                if (Head != null && enableEAH)
                {
                    // The less the head contributes to the gaze shift,
                    // the faster the eyes should be
                    float ha = Head.distRotAlign / GazeJoint.DistanceToRotate(Head.srcRot, Head.trgRot);
                    float et = (1f - ha) * etu + ha * etl;
                    eye.maxVelocity *= et;
                }

                // Speed up the eyes if they need to rotate far
                eye.maxVelocity *= ((2f / 75f) * Amax + 1f / 6f);

                // Slow down trailing eyes
                if (enableAEM)
                    eye.maxVelocity *= (Amax > 0.00001f ? eye.distRotMR / Amax : 1f);

                if (Head != null)
                    // Compensate for opposing eye and head rotation
                    eye.maxVelocity += 3f * opc[ei] * Head.maxVelocity;
            }
        }
        else
        {
            // Find the shortest eye rotation distance
            float Amin = float.MaxValue;
            foreach (GazeJoint eye in eyes)
            {
                float adjDistRotMR = eye.distRotMR;

                if (curMovingTargetPosOff != Vector3.zero)
                {
                    // Adjust rotational distance based on future target offset
                    Quaternion curTrgRot = eye.trgRot;
                    Quaternion curTrgRotMR = eye.trgRotMR;
                    float curDistRotMR = eye.distRotMR;
                    eye.trgRot = eye._ComputeTargetRotation(EffGazeTargetPosition + curMovingTargetPosOff);
                    eye._InitTargetRotationMR();
                    adjDistRotMR = eye.distRotMR;

                    // Restore current target rotations
                    eye.trgRot = curTrgRot;
                    eye.trgRotMR = curTrgRotMR;
                    eye.distRotMR = curDistRotMR;
                }

                // Update shortest eye rotation distance
                Amin = Mathf.Min(adjDistRotMR, Amin);
            }

            foreach (GazeJoint eye in eyes)
            {
                // TODO: This is a quick hack that approximates eye velocity
                // based on about how far the saccade will be
                eye.maxVelocity = 4f * (eye.velocity / 150f) * Amin + eye.velocity / 6f;
            }
        }

        if (stylizeGaze)
        {
            // Compute gaze quickness based on how long the gaze shift is
            float q = quickness;
            float rdt = Mathf.Clamp01(Head != null ? Head.distRotAlign / 90f : 0f);
            q = (1f + 0.2f * rdt) * q;

            // Speed up or slow down joints depending on quickness
            for (int i = 0; i <= LastGazeJointIndex; ++i)
            {
                GazeJoint joint = gazeJoints[i];
                if (stylizeGaze)
                    joint.maxVelocity *= q;
            }
        }
    }

    // Compute overall amplitude of the gaze shift towards current target
    public virtual float _ComputeGazeShiftAmplitude(bool adjustForMovingTarget = false)
    {
        Vector3 srcdir = new Vector3(0, 0, 0);
        Vector3 ec = new Vector3(0, 0, 0);
        foreach (GazeJoint eye in eyes)
        {
            srcdir += eye.Direction;
            ec += eye.bone.position;
        }
        srcdir /= (float)eyes.Length;
        ec /= (float)eyes.Length;
        Vector3 trgPos = adjustForMovingTarget ? EffGazeTargetPosition + curMovingTargetPosOff : EffGazeTargetPosition;
        Vector3 trgdir = (trgPos - ec).normalized;
        
        return Vector3.Angle(srcdir, trgdir);
    }

    // Compute torso rotational amplitude from overall gaze shift amplitude
    public virtual float _ComputeMinTorsoDistanceToRotate()
    {
        if (Amplitude >= 40f)
            return 0.43f * Mathf.Exp(0.029f * Amplitude) + 0.186f;
        else if (Amplitude >= 20f)
            return 0.078f * Amplitude - 1.558f;

        return 0;
    }

    // Compute head latency from gaze shift amplitude and target predictability
    public virtual float _ComputeHeadLatency()
    {
        float pred = Mathf.Clamp01(predictability);
        // TODO: take into account amplitude
        return 50f - 20f * pred;
    }

    // Compute torso latency from gaze shift amplitude and target predictability
    public virtual float _ComputeTorsoLatency()
    {
        float pred = Mathf.Clamp01(predictability);
        float amplitude = _ComputeGazeShiftAmplitude(true);
        return -0.25f * amplitude * pred + 0.5f * amplitude - 57.5f * pred + 105f;
    }

    protected override void _Init()
    {
        // Get joint chain indices
        torsoIndex = _FindGazeJointIndex("TorsoBone");
        headIndex = _FindGazeJointIndex("HeadBone");
        lEyeIndex = _FindGazeJointIndex("LEyeBone");
        rEyeIndex = _FindGazeJointIndex("REyeBone");

        // Initialize every gaze joint
        List<GazeJoint> eye_list = new List<GazeJoint>();
        List<GazeJoint> head_list = new List<GazeJoint>();
        List<GazeJoint> torso_list = new List<GazeJoint>();
        for (int i = 0; i <= LastGazeJointIndex; ++i)
        {
            GazeJoint joint = gazeJoints[i];
            joint.Init(gameObject);

            if (joint.type == GazeJointType.LEye || joint.type == GazeJointType.REye)
                eye_list.Add(joint);
            else if (joint.type == GazeJointType.Head)
                head_list.Add(joint);
            else // if( joint.type == GazeJointType.Torso )
                torso_list.Add(joint);
        }
        eyes = eye_list.ToArray();
        head = head_list.ToArray();
        torso = head_list.ToArray();

        // Find/create helper gaze target
        string helperTargetName = gameObject.name + "GazeHelper";
        helperTarget = GameObject.Find(helperTargetName);
        if (helperTarget == null)
        {
            helperTarget = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            helperTarget.name = helperTargetName;
            helperTarget.tag = "GazeTarget";
            helperTarget.renderer.enabled = false;
            helperTarget.transform.localScale = new Vector3(0.02f, 0.02f, 0.02f);
        }

        // Find/create helper gaze target for looking ahead
        string aheadHelperTargetName = gameObject.name + "GazeAheadHelper";
        aheadHelperTarget = GameObject.Find(aheadHelperTargetName);
        if (aheadHelperTarget == null)
        {
            aheadHelperTarget = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            aheadHelperTarget.name = aheadHelperTargetName;
            aheadHelperTarget.tag = "GazeTarget";
            aheadHelperTarget.renderer.enabled = false;
            aheadHelperTarget.transform.localScale = new Vector3(0.02f, 0.02f, 0.02f);
        }

        // Find/create helper gaze target for fixation
        string fixHelperTargetName = gameObject.name + "FixGazeHelper";
        fixHelperTarget = GameObject.Find(fixHelperTargetName);
        if (fixHelperTarget == null)
        {
            fixHelperTarget = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            fixHelperTarget.name = fixHelperTargetName;
            fixHelperTarget.tag = "GazeTarget";
            fixHelperTarget.renderer.enabled = false;
            fixHelperTarget.transform.localScale = new Vector3(0.02f, 0.02f, 0.02f);
        }

        // Initialize gaze targets
        CurrentGazeTarget = null;
        FixGazeTarget = null;

        // Get other relevant anim. controllers
        blinkCtrl = gameObject.GetComponent<BlinkController>();
        faceCtrl = gameObject.GetComponent<FaceController>();

        // Get main camera
        cam = GameObject.FindGameObjectWithTag("EyeContactHelper");
        if (cam == null)
            cam = GameObject.FindGameObjectWithTag("MainCamera");
    }

    protected override void _Update()
    {
    }

    protected virtual void LateUpdate_NoGaze()
    {
        _InitBaseRotations();

        if (doGazeShift && gazeTarget != null)
        {
            // Interrupt whatever the face is doing
            _StopFace();

            GoToState((int)GazeState.Shifting);
        }
        else
        {
            if (fixGaze)
            {
                // Fixate gaze onto the current target
                _ApplyVOR();
                // TODO: hack - if we don't apply gaze at least twice in a frame,
                // there is a discontinuity in torso rotations
                _ApplyVOR();
                //
            }
        }
    }

    protected virtual void LateUpdate_Shifting()
    {
        _InitBaseRotations();

        // Interrupt whatever the face is doing
        _StopFace();

        // Advance gaze shift
        bool gazeShiftFinished = false;
        float dt = 0;
        for (float t = 0; t < DeltaTime; )
        {
            // Compute delta time
            t += LEAPCore.eulerTimeStep;
            dt = (t <= DeltaTime) ? LEAPCore.eulerTimeStep :
                DeltaTime - t + LEAPCore.eulerTimeStep;

            gazeShiftFinished = _AdvanceGazeShift(dt);
        }

        if (gazeShiftFinished)
            // Gaze shift complete, start fixating the target
            GoToState((int)GazeState.NoGaze);

        if (stopGazeShift)
        {
            // Interrupt ongoing gaze shift
            GoToState((int)GazeState.NoGaze);
            return;
        }
    }

    protected virtual void Transition_NoGazeShifting()
    {
        doGazeShift = false;

        if (FixGazeTarget == null)
            // This is the first gaze shift, so there is no target set for VOR
            // during latency period - set it now
            _InitVOR();

        if (fixGaze)
        {
            // Fixate gaze onto the current target
            _ApplyVOR();
            // TODO: hack - if we don't apply gaze at least twice in a frame,
            // there is discontinuity in torso rotations
            _ApplyVOR();
            //
        }

        // Initialize new gaze shift
        _InitGazeParams(); // initial rotations, alignments, latencies...
        _ViewAlignTarget(); // if eyes don't need to align fully, then how much?
        _ViewAdjustOMR(); // correct asymmetric OMR if needed
        _InitTargetRotations(); // compute initial estimate of target pose
        _RemoveCrossEyedness(); // move out effective gaze target to eliminate cross-eyedness
        if (stylizeGaze) _InitTargetRotations(); // compute actual target pose
        _InitLatencies();
        _CalculateMaxVelocities();
    }

    protected virtual void Transition_ShiftingNoGaze()
    {
        stopGazeShift = false;
        _InitVOR();
        _RestartFace();
    }

    // Update the rotations of all gaze joints based on gaze shift progress
    protected virtual bool _AdvanceGazeShift(float deltaTime)
    {
        int bodyAligned = 0; // Number of fully aligned body joints
        int eyesAligned = 0; // Number of fully aligned eye joints
        int eyesBlocked = 0; // Number of eye joints blocked by OMR limits

        // Rotate each joint
        for (int ji = LastGazeJointIndex; ji >= 0; --ji)
        {
            GazeJoint joint = gazeJoints[ji];
            GazeJoint last = GetLastGazeJointInChain(joint.type);

            if (last.latencyTime > 0)
            {
                // Joint not ready to move yet, just VOR
                joint._ApplyVOR();
                joint._UpdateRotationsOnVOR();

                // Solve for final body posture
                if (joint == Torso)
                {
                    _ApplyRotation(joint);
                    _SolveBodyIK();
                }
                else if (joint == Head)
                {
                    _ApplyRotation(joint);
                }

                // Update latency time
                if (joint == last)
                    last.latencyTime -= deltaTime;

                continue;
            }
            else
            {
                if (joint.isVOR)
                {
                    joint._StopVOR();
                }

                // Update target rotation of the current joint to account for movements of
                // the preceding joints (or the whole body)
                joint._UpdateTargetRotation();
            }

            // Compute joint velocity
            if (torsoIndex > 0 && ji == gazeJoints.Length - 1)
            {
                Torso._RecalculateVelocity();
            }
            else if (torsoIndex > 0 && ji == torsoIndex - 1 ||
                ji == gazeJoints.Length - 1)
            {
                Head._RecalculateVelocity();
            }
            else if (ji == headIndex - 1)
            {
                _RecalculateEyeVelocities();
            }

            if (joint.IsEye)
            {
                // OMR changes as the eyes move
                joint._UpdateOMR();
            }
            
            // Update joint rotations
            joint._AdvanceRotation(deltaTime);

            // Has the joint reached its target?
            if (!joint.IsEye)
            {
                if (joint.trgReached)
                    ++bodyAligned;
            }
            else
            {
                if (joint.trgReached)
                    ++eyesAligned;
                else if (joint.mrReached)
                    ++eyesBlocked;
            }

            // Solve for final body posture
            if (joint == Torso)
            {
                _ApplyRotation(joint);
                _SolveBodyIK();
            }
            else if (joint == Head)
            {
                _ApplyRotation(joint);
            }
        }

        if (stylizeGaze && eyesAligned >= eyes.Length)
        {
            // When all eyes have aligned, only then allow VOR
            foreach (GazeJoint eye in eyes)
                eye.stopOvershoot = true;
        }

        // Is the gaze shift finished?
        if ((eyesAligned + eyesBlocked >= eyes.Length
            || stylizeGaze && eyesAligned > 0) && // TODO: This causes a minor error in target pose,
            // but could it become a problem in some circumstances?
           bodyAligned == LastGazeJointIndex + 1 - eyes.Length)
        {
            return true;
        }

        return false;
    }

    // Apply fixation of the current gaze target
    public virtual void _ApplyVOR()
    {
        // Rotate each joint
        for (int ji = LastGazeJointIndex; ji >= 0; --ji)
        {
            GazeJoint joint = gazeJoints[ji];
            joint._ApplyVOR();

            // Solve for final body posture
            if (joint == Torso)
            {
                _ApplyRotation(joint);
                _SolveBodyIK();
            }
            else if (joint == Head)
            {
                _ApplyRotation(joint);
            }
        }
    }

    // Find index of gaze joint with specified bone tag
    protected virtual int _FindGazeJointIndex(string boneTag)
    {
        for (int gji = 0; gji < gazeJoints.Length; ++gji)
            if (gazeJoints[gji].bone.tag == boneTag)
                return gji;

        return -1;
    }

    // Adjust effective gaze target to maintain more alignment
    // with the camera
    protected virtual void _ViewAlignTarget()
    {
        if (!stylizeGaze)
            return;

        // TODO: if vat < vas, and they are on the same side of the camera,
        // then eyeAlign = 1

        // Compute view angles
        float vas, vat;
        _ComputeViewAngles(out vas, out vat);

        // Compute eye alignment based on view angles
        float hava = eyeSize * 3.2f; // high-accuracy view angle
        float eap = Mathf.Clamp01(vat / (3f * hava));
        adjEyeAlign = 1f - (1f - eyeAlign) * eap;
        //adjEyeAlign = eyeAlign + (1f-eyeAlign)*Mathf.Exp( 1f/3f*(hava-vat) );

        if (adjEyeAlign >= 0.99999f)
            return;

        float maxt = -float.MaxValue;
        foreach (GazeJoint eye in eyes)
        {
            Quaternion currot = eye.bone.localRotation;

            // Find eye directions for target rotations
            Quaternion srcrot = eye._ComputeTargetRotation(cam.transform.position);
            //Quaternion srcrot = eye.bone.localRotation;
            Quaternion trgrot = eye._ComputeTargetRotation(curGazeTarget.transform.position);
            eye.bone.localRotation = Quaternion.Slerp(srcrot, trgrot, adjEyeAlign);

            // Closest points on view line
            Vector3 e1 = eye.bone.position;
            Vector3 e2 = e1 + eye.Direction;
            Vector3 v1 = curGazeTarget.transform.position;
            Vector3 v2 = cam.transform.position;
            Vector3 v = v2 - v1;
            float s, t;
            GeomUtil.ClosestPointsOn2Lines(e1, e2, v1, v2, out s, out t);
            Vector3 vpt = v1 + v * t;
            if (t > maxt)
                effGazeTargetPos = vpt;
            maxt = t;

            eye.bone.localRotation = currot;
        }
    }

    // Restrict asymmetric OMR based on target viewpoint
    protected virtual void _ViewAdjustOMR()
    {
        // Compute view angles
        float vas, vat;
        _ComputeViewAngles(out vas, out vat);

        foreach (GazeJoint eye in eyes)
        {
            // Restrict OMR based on view angle
            float hava = eyeSize * 3.2f; // high-accuracy view angle
            if (vat < hava)
                eye.adjOutMR = eye.adjInMR;
            else if (vat >= hava && vat <= 3f * hava)
            {
                float eap = Mathf.Clamp01((vat - hava) / (3f * hava));
                eye.adjOutMR = eye.adjInMR + eap * (eye.adjOutMR - eye.adjInMR);
            }
        }
    }

    // Adjust effective gaze target to remove cross-eyedness
    protected virtual void _RemoveCrossEyedness()
    {
        if (!stylizeGaze || eyes.Length != 2)
            // Can only handle 2-eyed characters for now...
            return;

        // Compute view angles
        float vas, vat;
        _ComputeViewAngles(out vas, out vat);

        // Compute cross-eyedness allowed
        float hava = eyeSize * 3.2f; // high-accuracy view angle
        float eap = Mathf.Clamp01(vat / (3f * hava));
        maxCrEyedView = (1f - eap) * 110f + eap * maxCrossEyedness;

        // Move gaze joints to target pose
        _ApplyTargetPose();

        // How much cross-eyedness is allowed?
        float maxce = maxCrEyedView;
        maxce = maxce <= 0.00001f ? 0.00001f : maxce; // so the calculation doesn't break

        // Does cross-eyedness occur?
        if (CrossEyedness > maxce)
        {
            // Move effective gaze target so that cross-eyedness does not occur
            Vector3 hpt = 0.5f * (LEye.bone.position + REye.bone.position);
            Vector3 lrv = LEye.bone.position - REye.bone.position;
            float lrvmag = lrv.magnitude;
            if (lrvmag <= 0.00001f)
                // This character cannot become cross-eyed
                return;
            Vector3 tlv = effGazeTargetPos - LEye.bone.position;
            Vector3 trv = effGazeTargetPos - REye.bone.position;
            float h = Vector3.Cross(trv, tlv).magnitude / lrvmag;
            float d = Vector3.Distance(hpt, effGazeTargetPos);
            if (d <= 0.00001f) d = 0.00001f;
            float alpha = Mathf.Rad2Deg * Mathf.Asin(d <= h ? 1f : h / d);
            float drh = Vector3.Distance(REye.bone.position, hpt);
            float dnew = drh * Mathf.Sin(Mathf.Deg2Rad * (alpha + maxce / 2f)) /
                Mathf.Sin(Mathf.Deg2Rad * maxce / 2f);
            Vector3 thvn = d > 0.00001f ? (effGazeTargetPos - hpt).normalized :
                (LEye.Direction + REye.Direction).normalized;
            effGazeTargetPos = hpt + dnew * thvn;
        }

        // Restore original pose
        _ReapplyCurrentPose();
    }

    // Compute view direction angles
    protected virtual void _ComputeViewAngles(out float avSrc, out float avTrg)
    {
        // Compute view direction angles
        Vector3 wp_eyes = new Vector3();
        Vector3 wd_src = new Vector3();
        foreach (GazeJoint eye in eyes)
        {
            wp_eyes += eye.bone.position;
            wd_src += eye.Direction;
        }
        wp_eyes *= (1f / eyes.Length); // eye centroid position
        wd_src *= (1f / eyes.Length); // mean gaze direction
        wd_src.Normalize();
        Vector3 wd_trg = (effGazeTargetPos - wp_eyes).normalized;
        Vector3 wd_cam = (cam.transform.position - wp_eyes).normalized;
        avTrg = Vector3.Angle(wd_trg, wd_cam);
        avSrc = Vector3.Angle(wd_src, wd_cam);
    }

    // Store current gaze joint rotations as base rotations
    protected virtual void _InitBaseRotations()
    {
        foreach (var gazeJoint in gazeJoints)
            gazeJoint.baseRot = gazeJoint.bone.localRotation;
    }

    // Compute target pose of the eyes (at the end of the gaze shift)
    protected virtual void _InitEyeTargetRotations()
    {
        if (stylizeGaze)
            _InitEyeTargetRotationsEffective();
        else
            _InitEyeTargetRotationsBase();
    }

    // Compute target pose using effective gaze target positions
    protected virtual void _InitTargetRotationsEffective()
    {
        // Set effective gaze target, to determine correct target rotations
        Vector3 trgpos = curGazeTarget.transform.position;
        curGazeTarget.transform.position = effGazeTargetPos;
        _InitTargetRotationsBase();
        curGazeTarget.transform.position = trgpos;
    }

    // Compute target pose of the eyes using effective gaze target positions
    protected virtual void _InitEyeTargetRotationsEffective()
    {
        // Set effective gaze target, to determine correct target rotations
        Vector3 trgpos = curGazeTarget.transform.position;
        curGazeTarget.transform.position = effGazeTargetPos;
        _InitEyeTargetRotationsBase();
        curGazeTarget.transform.position = trgpos;
    }

    // Compute target pose without any stylization principles
    protected virtual void _InitTargetRotationsBase()
    {
        // 1. Compute target rotations for torso joints
        Amplitude = _ComputeGazeShiftAmplitude();
        _InitTorsoTargetRotations();

        // 2. Compute target rotations for the eyes
        foreach (GazeJoint eye in eyes)
            eye._InitTargetRotation();
        _AdjustOMRByIEP(); // OMR is different depending on initial eye orientation
        _OMRConstrainEyeTargetRotations(); // Adjust target rotations based on joint motor limits

        // 3. Compute target rotations for head joints
        _InitMinHeadTargetRotations(); // If eyes can't reach the target due to OMR, head needs to align more
        Head.trgRotAlign = Quaternion.Slerp(Head.trgRotAlign, Head.trgRot, Head.curAlign);
        Head.distRotAlign = GazeJoint.DistanceToRotate(Head.srcRot, Head.trgRotAlign);
    }

    // Compute target pose of the eyes without any stylization principles
    protected virtual void _InitEyeTargetRotationsBase()
    {
        _InitTargetRotationsBase();
        // TODO: implement this
    }

    // Compute base target pose of the torso joints
    protected virtual void _InitTorsoTargetRotations()
    {
        if (torsoIndex < 0 || !curUseTorso)
            return;

        float minDistRot = _ComputeMinTorsoDistanceToRotate();
        for (int gji = LastGazeJointIndex; gji >= torsoIndex; --gji)
        {
            GazeJoint joint = gazeJoints[gji];

            joint.trgRot = joint._ComputeTargetRotation(EffGazeTargetPosition);
            if (gji == torsoIndex)
            {
                float distRot = GazeJoint.DistanceToRotate(joint.srcRot, joint.trgRot);
                float rotParamMin = joint.srcRot != joint.trgRot ? minDistRot / distRot : 0f;
                joint.trgRotAlign = Quaternion.Slerp(joint.srcRot, joint.trgRot, rotParamMin);
                joint.trgRotAlign = Quaternion.Slerp(joint.trgRotAlign, joint.trgRot, joint.curAlign);
                joint.distRotAlign = GazeJoint.DistanceToRotate(joint.srcRot, joint.trgRotAlign);
            }
            else
            {
                joint.trgRotAlign = joint.trgRot;
                joint.distRotAlign = GazeJoint.DistanceToRotate(joint.srcRot, joint.trgRotAlign);
            }
        }
    }

    // OMR depends on both initial and target eye poses
    protected virtual void _AdjustOMRByIEP()
    {
        // Calculate contralateral-ness (a 0 value means the eyes are central or toward the target)
        float eyeContraPitch = 0f;
        float eyeContraYaw = 0f;
        foreach (GazeJoint eye in eyes)
        {
            float p = eye.Pitch - eye.InitPitch;
            float y = eye.Yaw - eye.InitYaw;
            eye.bone.localRotation = curMovingTargetPosOff != Vector3.zero ?
                eye._ComputeTargetRotation(EffGazeTargetPosition + curMovingTargetPosOff) :
                eye.trgRot;
            float trgp = eye.Pitch - eye.InitPitch;
            float trgy = eye.Yaw - eye.InitYaw;
            eye.bone.localRotation = eye.srcRot;

            float contraPitch = 0f;
            float contraYaw = 0f;
            if ((p > 0f && trgp < 0f) || (p < 0f && trgp > 0f))
                contraPitch = Mathf.Abs(p);
            if ((y > 0f && trgy < 0f) || (y < 0f && trgy > 0f))
                contraYaw = Mathf.Abs(y);

            eyeContraPitch += contraPitch;
            eyeContraYaw += contraYaw;
        }
        eyeContraPitch /= (float)eyes.Length;
        eyeContraYaw /= (float)eyes.Length;

        // Adjust OMR (effective oculomotor range is greater when the eyes start out more contralateral)
        foreach (GazeJoint eye in eyes)
        {
            eye.adjUpMR *= (1f / 360f * eyeContraPitch + 0.75f);
            eye.adjDownMR *= (1f / 360f * eyeContraPitch + 0.75f);
            eye.adjInMR *= (1f / 360f * eyeContraYaw + 0.75f);
            eye.adjOutMR *= (1f / 360f * eyeContraYaw + 0.75f);
            eye._InitCurMR();
        }
    }

    // Compute target pose as constrained by OMR
    protected virtual void _OMRConstrainEyeTargetRotations()
    {
        foreach (GazeJoint eye in eyes)
            eye._InitTargetRotationMR();
    }

    // Adjust head target rotations so eyes can achieve alignment
    protected virtual void _InitMinHeadTargetRotations()
    {
        // Initialize head target rotations
        for (int gji = (torsoIndex > -1 ? torsoIndex - 1 : LastGazeJointIndex); gji >= headIndex; --gji)
        {
            GazeJoint joint = gazeJoints[gji];
            joint.trgRot = joint._ComputeTargetRotation(EffGazeTargetPosition);
            joint.trgRotAlign = joint.trgRot;
            joint.distRotAlign = GazeJoint.DistanceToRotate(joint.srcRot, joint.trgRot);
            if (gji == headIndex)
            {
                joint.trgRotAlign = joint.srcRot;
            }
        }

        _ApplyTargetPose();

        // Find the maximum difference between the target orientation and the eyes' orientation at max. OMR
        float maxdl = -float.MaxValue;
        Quaternion maxdq = Quaternion.identity;
        foreach (GazeJoint eye in eyes)
        {
            float dl = GazeJoint.DistanceToRotate(eye.srcRot, eye.trgRot) - eye.distRotAlign;
            if (dl > maxdl)
            {
                maxdl = dl;
                maxdq = eye.bone.localRotation;
                eye.ClampMR();
                maxdq = Quaternion.Inverse(eye.bone.localRotation) * maxdq;
            }
        }

        // Compute new target rotation for head joint
        Quaternion trgrot = Head.bone.localRotation * maxdq;
        Head.trgRotAlign = trgrot;
        Head.distRotAlign = DirectableJoint.DistanceToRotate(Head.srcRot, trgrot);

        _ReapplyCurrentPose();
    }

    // Compute aligning rotational distance for specified joint, adjusted for
    // future position of the moving gaze target
    protected virtual float _ComputeDistRotAlignForMovingTarget(GazeJoint joint)
    {
        float adjDistRotAlign = joint.distRotAlign;
        if (curMovingTargetPosOff != Vector3.zero)
        {
            // Adjust rotational distance based on future target offset
            float distRot = GazeJoint.DistanceToRotate(joint.srcRot, joint.trgRot);
            Quaternion adjTrgRot = joint._ComputeTargetRotation(EffGazeTargetPosition + curMovingTargetPosOff);
            adjDistRotAlign = distRot > 0f ?
                joint.distRotAlign / distRot * GazeJoint.DistanceToRotate(joint.srcRot, adjTrgRot) :
                0f;
        }
        
        return adjDistRotAlign;
    }

    // Recalculate eye velocities as gaze shift progresses
    protected virtual void _RecalculateEyeVelocities()
    {
        foreach (GazeJoint eye in eyes)
            eye._RecalculateVelocity();
    }

    // Solve for body posture using an IK solver
    protected virtual void _SolveBodyIK()
    {
        if (!LEAPCore.useGazeIK)
            return;

        var bodySolver = gameObject.GetComponent<BodyIKSolver>();
        if (bodySolver != null && bodySolver.enabled)
        {
            bodySolver.InitGazePose();
            bodySolver.Solve();
        }
    }

    /// <summary>
    /// Stop any ongoing facial movements.
    /// </summary>
    protected virtual void _StopFace()
    {
        if (faceCtrl == null)
            return;

        faceCtrl.stopGesture = true;
        reenableRandomHeadMotion = faceCtrl.randomMotionEnabled;
        reenableRandomSpeechMotion = faceCtrl.speechMotionEnabled;
        faceCtrl.speechMotionEnabled = false;
        faceCtrl.randomMotionEnabled = false;
    }

    /// <summary>
    /// Restart any facial movements that were previously active.
    /// </summary>
    protected virtual void _RestartFace()
    {
        if (faceCtrl == null)
            return;

        faceCtrl.randomMotionEnabled = reenableRandomHeadMotion;
        //Check to see if the character is still actually speaking first?...
        faceCtrl.speechMotionEnabled = reenableRandomSpeechMotion;
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
        state.predictability = predictability;
        state.movingTargetPositionOffset = movingTargetPositionOffset;
        state.stylizeGaze = stylizeGaze;
        state.quickness = quickness;
        state.eyeSize = eyeSize;
        state.eyeTorque = eyeTorque;
        state.eyeAlign = eyeAlign;
        state.enableED = enableED;
        state.enableAEM = enableAEM;
        state.enableEAH = enableEAH;
        state.maxCrossEyedness = maxCrossEyedness;
        state.removeRoll = removeRoll;
        state.amplitude = Amplitude;
        state.currentGazeTarget = CurrentGazeTarget;
        state.fixGazeTarget = FixGazeTarget;
        state.effGazeTargetPosition = effGazeTargetPos;
        state.weight = weight;
        state.fixWeight = fixWeight;

        state.gazeJointStates = new GazeControllerState.GazeJointState[gazeJoints.Length];
        for (int jointIndex = 0; jointIndex < gazeJoints.Length; ++jointIndex)
        {
            state.gazeJointStates[jointIndex] = gazeJoints[jointIndex]._GetRuntimeState();
        }

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
        predictability = gazeControllerState.predictability;
        movingTargetPositionOffset = gazeControllerState.movingTargetPositionOffset;
        stylizeGaze = gazeControllerState.stylizeGaze;
        quickness = gazeControllerState.quickness;
        eyeSize = gazeControllerState.eyeSize;
        eyeTorque = gazeControllerState.eyeTorque;
        eyeAlign = gazeControllerState.eyeAlign;
        enableED = gazeControllerState.enableED;
        enableAEM = gazeControllerState.enableAEM;
        enableEAH = gazeControllerState.enableEAH;
        maxCrossEyedness = gazeControllerState.maxCrossEyedness;
        removeRoll = gazeControllerState.removeRoll;
        Amplitude = gazeControllerState.amplitude;
        CurrentGazeTarget = gazeControllerState.currentGazeTarget;
        FixGazeTarget = gazeControllerState.fixGazeTarget;
        effGazeTargetPos = gazeControllerState.effGazeTargetPosition;
        weight = gazeControllerState.weight;
        fixWeight = gazeControllerState.fixWeight;

        if (gazeControllerState.gazeJointStates != null && gazeControllerState.gazeJointStates.Length == gazeJoints.Length)
        {
            for (int jointIndex = 0; jointIndex < gazeJoints.Length; ++jointIndex)
                gazeJoints[jointIndex]._SetRuntimeState(gazeControllerState.gazeJointStates[jointIndex]);
        }
    }

    /// <summary>
    /// Get initial/zero state for the gaze controller
    /// </summary>
    /// <returns>Initial runtime state</returns>
    public virtual GazeControllerState GetInitRuntimeState()
    {
        GazeControllerState state = (GazeControllerState)GetRuntimeState();

        state.stateId = (int)GazeState.NoGaze;
        state.gazeTarget = null;
        state.doGazeShift = false;
        state.stopGazeShift = false;
        state.fixGaze = false;
        state.useTorso = true;
        state.predictability = 1f;
        state.movingTargetPositionOffset = Vector3.zero;
        state.amplitude = 0f;
        state.currentGazeTarget = null;
        state.fixGazeTarget = null;
        state.weight = state.fixWeight = 1f;

        for (int gazeJointIndex = 0; gazeJointIndex < gazeJoints.Length; ++gazeJointIndex)
        {
            GazeControllerState.GazeJointState gazeJointState = state.gazeJointStates[gazeJointIndex];

            gazeJointState.align = 1f;
            gazeJointState.latency = 100f;
            gazeJointState.rot = Quaternion.identity;
            gazeJointState.srcRot = Quaternion.identity;
            gazeJointState.trgRot = Quaternion.identity;
            gazeJointState.trgRotAlign = Quaternion.identity;
            gazeJointState.trgRotMR = Quaternion.identity;
            gazeJointState.distRotAlign = 0f;
            gazeJointState.distRotMR = 0f;
            gazeJointState.rotParamAlign = 0f;
            gazeJointState.rotParamMR = 0f;
            gazeJointState.maxVelocity = 0f;
            gazeJointState.curVelocity = 0f;
            gazeJointState.latencyTime = 0f;
            gazeJointState.mrReached = false;
            gazeJointState.trgReached = false;
            gazeJointState.adjUpMR = gazeJointState.adjDownMR = gazeJointState.adjInMR = gazeJointState.adjOutMR =
                gazeJointState.curUpMR = gazeJointState.curDownMR = gazeJointState.curInMR = gazeJointState.curOutMR = 0f;
            gazeJointState.curAlign = 1f;
            gazeJointState.isVOR = false;
            gazeJointState.fixSrcRot = Quaternion.identity;
            gazeJointState.fixTrgRotAlign = Quaternion.identity;
            gazeJointState.expressiveRot = Quaternion.identity;
            gazeJointState.fixExpressiveRot = Quaternion.identity;

            state.gazeJointStates[gazeJointIndex] = gazeJointState;
        }

        return state;
    }

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
        var lEyeGazeHelper = ModelUtils.FindBoneWithTag(gameObject.transform, "LEyeGazeHelper");
        var rEyeBone = ModelUtils.FindBoneWithTag(gameObject.transform, "REyeBone");
        var rEyeGazeHelper = ModelUtils.FindBoneWithTag(gameObject.transform, "REyeGazeHelper");
        var headBones = ModelUtils.GetAllBonesWithTag(gameObject, "HeadBone");
        var headGazeHelpers = ModelUtils.GetAllBonesWithTag(gameObject, "HeadGazeHelper");
        var torsoBones = ModelUtils.GetAllBonesWithTag(gameObject, "TorsoBone");
        var torsoGazeHelpers = ModelUtils.GetAllBonesWithTag(gameObject, "TorsoGazeHelper");

        // Add default eye joints
        gazeJoints = new GazeJoint[2 + headBones.Length + torsoBones.Length];
        gazeJoints[0] = new GazeJoint();
        gazeJoints[0].type = GazeJointType.LEye;
        gazeJoints[0].bone = lEyeBone;
        gazeJoints[0].helper = lEyeGazeHelper;
        gazeJoints[1] = new GazeJoint();
        gazeJoints[1].type = GazeJointType.REye;
        gazeJoints[1].bone = rEyeBone;
        gazeJoints[1].helper = rEyeGazeHelper;
        gazeJoints[0].upMR = gazeJoints[0].downMR =
            gazeJoints[1].upMR = gazeJoints[1].downMR = 35f;
        gazeJoints[0].inMR = gazeJoints[0].outMR =
            gazeJoints[1].inMR = gazeJoints[1].outMR = 45f;
        gazeJoints[0].velocity = gazeJoints[1].velocity = 170f;

        // Add default head joints
        for (int headBoneIndex = headBones.Length-1; headBoneIndex >= 0; --headBoneIndex)
        {
            var headJoint = new GazeJoint();
            headJoint.type = GazeJointType.Head;
            headJoint.bone = headBones[headBoneIndex];
            headJoint.helper = headGazeHelpers.FirstOrDefault(helper => helper.parent == headBones[headBoneIndex]);
            headJoint.upMR = headJoint.downMR = 90f;
            headJoint.inMR = headJoint.outMR = 180f;
            headJoint.velocity = 75f;

            gazeJoints[2 + headBones.Length - headBoneIndex - 1] = headJoint;
        }

        // Add default torso joints
        for (int torsoBoneIndex = torsoBones.Length - 1; torsoBoneIndex >= 0; --torsoBoneIndex)
        {
            var torsoJoint = new GazeJoint();
            torsoJoint.type = GazeJointType.Torso;
            torsoJoint.bone = torsoBones[torsoBoneIndex];
            torsoJoint.helper = torsoGazeHelpers.FirstOrDefault(helper => helper.parent == torsoBones[torsoBoneIndex]);
            torsoJoint.upMR = torsoJoint.downMR = 90f;
            torsoJoint.inMR = torsoJoint.outMR = 180f;
            torsoJoint.velocity = 40f;

            gazeJoints[2 + headBones.Length + torsoBones.Length - torsoBoneIndex - 1] = torsoJoint;
        }
    }
}

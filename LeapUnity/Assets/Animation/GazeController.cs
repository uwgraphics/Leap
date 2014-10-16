using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

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
    public bool holdGaze = true;

    /// <summary>
    /// Chain of joints that are directed by the gaze controller.
    /// </summary>
    public GazeJoint[] gazeJoints = new GazeJoint[0];

    /// <summary>
    /// Predictability of the gaze target (0-1). 
    /// </summary>
    public float predictability = 1f;

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
    /// If true, gaze blinks will be staged to disguise visual artifacts.
    /// </summary>
    public bool stageGazeBlinks = true;

    /// <summary>
    /// If true, some torso motion will be automatically introduced into
    /// large gaze shifts.
    /// </summary>
    public bool enableAutoTorso = true;

    // Shorthand for getting the eye joints
    [HideInInspector]
    public GazeJoint[] eyes = new GazeJoint[0];
    // Shorthand for getting the neck joints
    [HideInInspector]
    public GazeJoint[] headNeck = new GazeJoint[0];
    // Shorthand for getting the torso joints
    [HideInInspector]
    public GazeJoint[] torso = new GazeJoint[0];

    protected int lEyeIndex = -1;
    protected int rEyeIndex = -1;
    protected int headIndex = -1;
    protected int torsoIndex = -1;

    protected GameObject curGazeTarget = null; // Current gaze target
    protected float curGazeHoldTime = 0;
    protected float latencyStart = 0f;
    protected float adjEyeAlign = 1f;
    protected float maxCrEyedView = 0f; // Maximum cross-eyedness allowed, adjusted by view angle
    protected Vector3 effGazeTrgPos; // Effective gaze target position
    protected float distRot = 0f; // Gaze shift amplitude

    protected GameObject helperTarget; // Allows gazing at arbitrary point in space
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
            return torsoIndex >= 0 ? gazeJoints[torsoIndex] : null;
        }
    }

    /// <summary>
    /// Current gaze target. 
    /// </summary>
    public virtual GameObject GazeTarget
    {
        get
        {
            return curGazeTarget;
        }
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
            return stylizeGaze ? effGazeTrgPos : curGazeTarget.transform.position;
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
            if (LEye == null || REye == null)
                return 0;

            float ce = Vector3.Angle(LEye.FaceDirection, REye.FaceDirection);
            if (ce > 0.00001f)
            {
                // Is the character cross-eyed or eye-divergent?
                float lt, rt;
                GeomUtil.ClosestPoints2Lines(
                    LEye.bone.position, LEye.bone.position + LEye.FaceDirection,
                    REye.bone.position, REye.bone.position + REye.FaceDirection,
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

            float ed = Vector3.Angle(LEye.FaceDirection, REye.FaceDirection);
            if (ed > 0.00001f)
            {
                // Is the character cross-eyed or eye-divergent?
                float lt, rt;
                GeomUtil.ClosestPoints2Lines(
                    LEye.bone.position, LEye.bone.position + LEye.FaceDirection,
                    REye.bone.position, REye.bone.position + REye.FaceDirection,
                    out lt, out rt);
                if (rt > 0)
                    ed = 0;
                //return 1f;
            }

            return ed;
        }
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
        this.gazeTarget = gazeTarget;
        doGazeShift = true;
    }

    /// <summary>
    /// Gaze at specified point in world space.
    /// </summary>
    /// <param name="gazeTargetWPos">
    /// Gaze target position in world psace.
    /// </param>
    public virtual void GazeAt(Vector3 gazeTargetWPos)
    {
        helperTarget.transform.position = gazeTargetWPos;
        GazeAt(helperTarget);
    }

    /// <summary>
    /// Direct the agent's gaze to a "neutral position", i.e.
    /// whatever is in front of it. 
    /// </summary>
    public virtual void GazeAtFront()
    {
        // TODO: this is broken, fix it
        // Position the helper gaze target in front of the agent
        Vector3 head_iwpos = ModelController.GetInitWorldPosition(Head.bone);
        helperTarget.transform.localPosition = new Vector3(0f, head_iwpos.y, 1000f);

        Head.align = 1f;
        GazeAt(helperTarget);
    }

    /// <summary>
    /// Interrupt ongoing gaze shift.
    /// </summary>
    public virtual void StopGaze()
    {
        stopGazeShift = true;
    }

    /// <summary>
    /// Initiates fixation of the current gaze target. 
    /// </summary>
    public void InitVOR()
    {
        foreach (GazeJoint joint in gazeJoints)
            joint._InitVOR();
    }

    /// <summary>
    /// Applies fixation of the current gaze target.
    /// </summary>
    public void ApplyVOR()
    {
        foreach (GazeJoint joint in gazeJoints)
            joint._ApplyVOR();
    }

    /// <summary>
    /// Applies fixation of the current gaze target, but only
    /// for the eyes.
    /// </summary>
    public void ApplyVORForEyes()
    {
        foreach (GazeJoint eye in eyes)
            eye._ApplyVOR();
    }

    /// <summary>
    /// Orient the gaze joints to the source pose (agent gazing
    /// at the current gaze target).
    /// </summary>
    public virtual void ApplySourcePose()
    {
        for (int ji = gazeJoints.Length - 1; ji >= 0; --ji)
        {
            curRots[ji] = gazeJoints[ji].bone.localRotation;
            gazeJoints[ji].bone.localRotation = gazeJoints[ji].srcRot;
        }
    }

    /// <summary>
    /// Orient the gaze joints to the target pose (agent gazing
    /// at the current gaze target).
    /// </summary>
    public virtual void ApplyTargetPose()
    {
        for (int ji = gazeJoints.Length - 1; ji >= 0; --ji)
        {
            GazeJoint joint = gazeJoints[ji];
            curRots[ji] = joint.bone.localRotation;
            Quaternion trgrot = joint._ComputeTargetRotation(EffGazeTargetPosition);

            if (torsoIndex > 0 && ji >= torsoIndex)
            {
                // Torso joint
                // TODO: compute min. target rotation using D-Dt curve,
                // and then use that instead of srcRot
                float pdr = Torso.distRotAlign / GetNumGazeJointsInChain(GazeJointType.Torso);
                float fdr = GazeJoint.DistanceToRotate(joint.srcRot, trgrot);
                joint.bone.localRotation = joint.srcRot == trgrot ? trgrot :
                    Quaternion.Slerp(joint.srcRot, trgrot, pdr / fdr);
            }
            else if (headIndex > 0 && ji >= headIndex)
                // Head joint
                joint.bone.localRotation = joint.srcRot;
            else if (joint.IsEye)
                // Eye
                joint.bone.localRotation = trgrot;
        }
    }

    /// <summary>
    /// Orient the gaze joints to the original pose.
    /// </summary>
    public virtual void ReapplyCurrentPose()
    {
        for (int ji = gazeJoints.Length - 1; ji >= 0; --ji)
            gazeJoints[ji].bone.localRotation = curRots[ji];
    }

    protected override void _Init()
    {
        // Create shorthands for some commonly used gaze joints
        lEyeIndex = _FindGazeJointIndex("LEyeBone");
        rEyeIndex = _FindGazeJointIndex("REyeBone");
        headIndex = _FindGazeJointIndex("HeadBone");
        torsoIndex = _FindGazeJointIndex("TorsoBone");

        // Initialize every gaze joint
        List<GazeJoint> eye_list = new List<GazeJoint>();
        List<GazeJoint> headneck_list = new List<GazeJoint>();
        List<GazeJoint> torso_list = new List<GazeJoint>();
        foreach (GazeJoint joint in gazeJoints)
        {
            joint.Init(gameObject);

            if (joint.type == GazeJointType.LEye || joint.type == GazeJointType.REye)
                eye_list.Add(joint);
            else if (joint.type == GazeJointType.Head)
                headneck_list.Add(joint);
            else // if( joint.type == GazeJointType.Torso )
                torso_list.Add(joint);
        }
        eyes = eye_list.ToArray();
        headNeck = headneck_list.ToArray();
        torso = headneck_list.ToArray();

        // Initialize VOR
        InitVOR();

        // Create helper gaze target
        helperTarget = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        helperTarget.name = "GTHelper";
        helperTarget.renderer.enabled = false;
        helperTarget.transform.localScale = new Vector3(0.02f, 0.02f, 0.02f);
        helperTarget.transform.parent = transform;

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
        stopGazeShift = false;

        if (holdGaze)
            // Eyes should remain fixed on their target no matter what
            ApplyVORForEyes();

        if (doGazeShift && gazeTarget != null)
        {
            // Interrupt whatever the face is doing
            faceCtrl.stopGesture = true;
            reenableRandomHeadMotion = faceCtrl.randomMotionEnabled;
            reenableRandomSpeechMotion = faceCtrl.speechMotionEnabled;
            faceCtrl.speechMotionEnabled = false;
            faceCtrl.randomMotionEnabled = false;

            GoToState((int)GazeState.Shifting);
        }
    }

    protected virtual void LateUpdate_Shifting()
    {
        if (stopGazeShift)
        {
            // Interrupt ongoing gaze shift
            GoToState((int)GazeState.NoGaze);
            return;
        }

        // Interrupt whatever the face is doing
        faceCtrl.stopGesture = true;
        faceCtrl.speechMotionEnabled = false;
        faceCtrl.randomMotionEnabled = false;

        // Advance gaze shift
        float dt = 0;
        for (float t = 0; t < Time.deltaTime; )
        {
            t += LEAPCore.eulerTimeStep;
            dt = (t <= Time.deltaTime) ? LEAPCore.eulerTimeStep :
                Time.deltaTime - t + LEAPCore.eulerTimeStep;
            if (_AdvanceGazeShift(dt))
            {
                GoToState((int)GazeState.NoGaze);
                break;
            }
        }
    }

    protected virtual void Transition_NoGazeShifting()
    {
        doGazeShift = false;
        curGazeTarget = gazeTarget;
        curGazeHoldTime = 0;

        // Initialize new gaze shift
        InitVOR(); // eyes may need to VOR at the start, if they move last
        _InitGazeParams(); // initial rotations, alignments, latencies...
        _ViewAlignTarget(); // if eyes don't need to align fully, then how much?
        _ViewAdjustOMR(); // correct asymmetric OMR if needed
        //
        //GameObject.Find("GTAlignCheck").transform.position = effGazeTrgPos;
        //
        _InitTargetRotations(); // compute initial estimate of target pose
        _RemoveCrossEyedness(); // move out effective gaze target to eliminate cross-eyedness
        if (stylizeGaze) _InitTargetRotations(); // compute actual target pose
        _InitLatencies();
        _CalculateMaxVelocities();
        _InitGazeBlink();
    }

    protected virtual void Transition_ShiftingNoGaze()
    {
        InitVOR();
        faceCtrl.randomMotionEnabled = reenableRandomHeadMotion;
        //Check to see if the character is still actually speaking first?...
        faceCtrl.speechMotionEnabled = reenableRandomSpeechMotion;
    }

    protected virtual bool _AdvanceGazeShift(float deltaTime)
    {
        int body_aligned = 0; // Number of fully aligned body joints
        int eyes_aligned = 0; // Number of fully aligned eye joints
        int eyes_blocked = 0; // Number of eye joints blocked by OMR limits

        // Rotate each joint
        for (int ji = gazeJoints.Length - 1; ji >= 0; --ji)
        {
            GazeJoint joint = gazeJoints[ji];

            if (joint.latencyTime > 0)
            {
                // Joint not ready to move yet
                if (joint.IsEye)
                    joint._ApplyVOR();
                joint.latencyTime -= deltaTime;
                continue;
            }

            // Compute joint velocity
            if (torsoIndex > 0 && ji == gazeJoints.Length - 1)
                // First torso joint
                Torso._RecalculateVelocity();
            else if (torsoIndex > 0 && ji == torsoIndex - 1 ||
                ji == gazeJoints.Length - 1)
                // First head joint
                Head._RecalculateVelocity();
            else if (ji == headIndex - 1)
                // First eye
                _RecalculateEyeVelocities();

            // Update joint rotations
            if (joint.IsEye)
                joint._UpdateOMR();
            joint._AdvanceRotation(deltaTime);
            if (!joint.IsEye)
                _UpdateTargetRotations(ji - 1);
            // TODO: update descendant target rotations here

            // Has the joint reached its target?
            if (!joint.IsEye)
            {
                if (joint.trgReached)
                    ++body_aligned;
            }
            else
            {
                if (joint.trgReached)
                    ++eyes_aligned;
                else if (joint.mrReached)
                    ++eyes_blocked;
            }
        }

        if (stylizeGaze && eyes_aligned >= eyes.Length)
        {
            // When all eyes have aligned, only then allow VOR
            foreach (GazeJoint eye in eyes)
                eye.stopOvershoot = true;
        }

        // Is the gaze shift finished?
        if ((eyes_aligned + eyes_blocked >= eyes.Length
            || stylizeGaze && eyes_aligned > 0) && // TODO: This causes a minor error in target pose,
            // but could it become a problem in some circumstances?
           body_aligned == gazeJoints.Length - eyes.Length)
        {
            /*foreach( GazeJoint eye in eyes )
                eye.trgReached = true;*/
            return true;
        }

        return false;
    }

    // Find index of gaze joint with specified bone tag
    protected virtual int _FindGazeJointIndex(string boneTag)
    {
        for (int gji = 0; gji < gazeJoints.Length; ++gji)
            if (gazeJoints[gji].bone.tag == boneTag)
                return gji;

        return -1;
    }

    protected virtual void _InitGazeParams()
    {
        // Eyes geometry parameters
        eyeSize = Mathf.Clamp(eyeSize, 1f, 5.8f); // eyes can't be larger than the head...
        eyeTorque = eyeTorque < 1f ? 1f : eyeTorque;

        // Initialize per-joint parameters
        for (int gji = 0; gji < gazeJoints.Length; ++gji)
        {
            GazeJoint joint = gazeJoints[gji];
            joint._InitGazeParams();
        }
        curRots = new Quaternion[gazeJoints.Length];

        // Initialize effective gaze target
        effGazeTrgPos = curGazeTarget.transform.position;
        distRot = 0f;
        // TODO: Compute gaze shift parameters based on high-level parameters
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
            Vector3 e2 = e1 + eye.FaceDirection;
            Vector3 v1 = curGazeTarget.transform.position;
            Vector3 v2 = cam.transform.position;
            Vector3 v = v2 - v1;
            float s, t;
            GeomUtil.ClosestPoints2Lines(e1, e2, v1, v2, out s, out t);
            Vector3 vpt = v1 + v * t;
            if (t > maxt)
                effGazeTrgPos = vpt;
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
        ApplyTargetPose();

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
            Vector3 tlv = effGazeTrgPos - LEye.bone.position;
            Vector3 trv = effGazeTrgPos - REye.bone.position;
            float h = Vector3.Cross(trv, tlv).magnitude / lrvmag;
            float d = Vector3.Distance(hpt, effGazeTrgPos);
            if (d <= 0.00001f) d = 0.00001f;
            float alpha = Mathf.Rad2Deg * Mathf.Asin(d <= h ? 1f : h / d);
            float drh = Vector3.Distance(REye.bone.position, hpt);
            float dnew = drh * Mathf.Sin(Mathf.Deg2Rad * (alpha + maxce / 2f)) /
                Mathf.Sin(Mathf.Deg2Rad * maxce / 2f);
            Vector3 thvn = d > 0.00001f ? (effGazeTrgPos - hpt).normalized :
                (LEye.FaceDirection + REye.FaceDirection).normalized;
            effGazeTrgPos = hpt + dnew * thvn;
        }

        // Restore original pose
        ReapplyCurrentPose();
    }

    protected virtual void _ComputeViewAngles(out float avSrc, out float avTrg)
    {
        // Compute view direction angles
        Vector3 wp_eyes = new Vector3();
        Vector3 wd_src = new Vector3();
        foreach (GazeJoint eye in eyes)
        {
            wp_eyes += eye.bone.position;
            wd_src += eye.FaceDirection;
        }
        wp_eyes *= (1f / eyes.Length); // eye centroid position
        wd_src *= (1f / eyes.Length); // mean gaze direction
        wd_src.Normalize();
        Vector3 wd_trg = (effGazeTrgPos - wp_eyes).normalized;
        Vector3 wd_cam = (cam.transform.position - wp_eyes).normalized;
        avTrg = Vector3.Angle(wd_trg, wd_cam);
        avSrc = Vector3.Angle(wd_src, wd_cam);
    }

    // Compute target pose (at the end of the gaze shift)
    protected virtual void _InitTargetRotations()
    {
        if (stylizeGaze)
            _InitTargetRotationsEffective();
        //_CalculateVelocityScales();
        else
            _InitTargetRotationsBase();
    }

    // Compute target pose of the eyes (at the end of the gaze shift)
    protected virtual void _InitEyeTargetRotations()
    {
        if (stylizeGaze)
            _InitEyeTargetRotationsEffective();
        //_CalculateVelocityScales();
        else
            _InitEyeTargetRotationsBase();
    }

    // Compute target pose using effective gaze target positions
    protected virtual void _InitTargetRotationsEffective()
    {
        // Set effective gaze target, to determine correct target rotations
        Vector3 trgpos = curGazeTarget.transform.position;
        curGazeTarget.transform.position = effGazeTrgPos;
        _InitTargetRotationsBase();
        curGazeTarget.transform.position = trgpos;
    }

    // Compute target pose of the eyes using effective gaze target positions
    protected virtual void _InitEyeTargetRotationsEffective()
    {
        // Set effective gaze target, to determine correct target rotations
        Vector3 trgpos = curGazeTarget.transform.position;
        curGazeTarget.transform.position = effGazeTrgPos;
        _InitEyeTargetRotationsBase();
        curGazeTarget.transform.position = trgpos;
    }

    // Compute target pose without any stylization principles
    protected virtual void _InitTargetRotationsBase()
    {
        // Compute gaze shift amplitude
        Vector3 srcdir = new Vector3(0, 0, 0);
        Vector3 ec = new Vector3(0, 0, 0);
        foreach (GazeJoint eye in eyes)
        {
            srcdir += eye.FaceDirection;
            ec += eye.bone.position;
        }
        srcdir /= (float)eyes.Length;
        ec /= (float)eyes.Length;
        Vector3 trgdir = (EffGazeTargetPosition - ec).normalized;
        distRot = Vector3.Angle(srcdir, trgdir);

        // 1. Compute target rotations for torso joints
        for (int gji = gazeJoints.Length - 1; gji >= headIndex; --gji)
        {
            GazeJoint joint = gazeJoints[gji];
            joint.trgRot = joint._ComputeTargetRotation(EffGazeTargetPosition);
            joint.trgRotAlign = joint.trgRot;
            joint.distRotAlign = GazeJoint.DistanceToRotate(joint.srcRot, joint.trgRot);
            if (gji == torsoIndex)
            {
                // Only the last torso joint needs exact trgRotAlign value,
                // because it drives the rest of the torso joints
                float dr = _ComputeTorsoRotDistance(distRot);
                float fdr = GazeJoint.DistanceToRotate(joint.srcRot, joint.trgRot);
                float amin = joint.srcRot != joint.trgRot ? dr / fdr : 0f;
                joint.trgRotAlign = Quaternion.Slerp(joint.srcRot, joint.trgRot, amin);
                joint.trgRotAlign = Quaternion.Slerp(joint.trgRotAlign, joint.trgRot, joint.align);
                joint.distRotAlign = GazeJoint.DistanceToRotate(joint.srcRot, joint.trgRotAlign);
                //
                Debug.Log("Trunk rotation amplitude " + joint.distRotAlign);
                //
            }
            else if (gji == headIndex)
            {
                // Same with the last head joint
                joint.trgRotAlign = joint.srcRot;
            }
        }

        // 2. Compute target rotations for the eyes
        foreach (GazeJoint eye in eyes)
            eye._InitTargetRotation();
        _AdjustOMRByIEP(); // OMR is different depending on initial eye orientation
        _OMRConstrainEyeTargetRotations(); // Adjust target rotations based on joint motor limits

        // 3. Compute target rotations for head joints
        _InitMinHeadTargetRotations(); // If eyes can't reach the target due to OMR, head needs to align more
        Head.trgRotAlign = Quaternion.Slerp(Head.trgRotAlign, Head.trgRot, Head.align);
        Head.distRotAlign = GazeJoint.DistanceToRotate(Head.srcRot, Head.trgRotAlign);
    }

    // Compute target pose of the eyes without any stylization principles
    protected virtual void _InitEyeTargetRotationsBase()
    {
        _InitTargetRotationsBase();
        // TODO: implement this
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
            eye.bone.localRotation = eye.trgRot;
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

    // Adjust target rotations so eyes can achieve alignment
    protected virtual void _InitMinHeadTargetRotations()
    {
        ApplyTargetPose();

        // Find the maximum difference between the target orientation and the eyes' orientation at max. OMR
        float maxdl = -float.MaxValue;
        Quaternion maxdq = Quaternion.identity;
        //GazeJoint maxeye = null;
        foreach (GazeJoint eye in eyes)
        {
            float dl = GazeJoint.DistanceToRotate(eye.srcRot, eye.trgRot) - eye.distRotAlign;
            if (dl > maxdl)
            {
                maxdl = dl;
                maxdq = eye.bone.localRotation;
                eye.ClampMRToSource();
                maxdq = Quaternion.Inverse(eye.bone.localRotation) * maxdq;
                //maxeye = eye;
            }
        }

        // Compute new target rotation for head joint
        Quaternion trgrot = Head.bone.localRotation * maxdq;
        Head.trgRotAlign = trgrot;
        Head.distRotAlign = DirectableJoint.DistanceToRotate(Head.srcRot, trgrot);

        // TODO: alignment for torso
        // torso should automatically start contributing for gaze shifts of more than 40 degrees
        // see the monkey paper for more detail

        ReapplyCurrentPose();
    }

    // Update target rotations of descendant joints
    protected virtual void _UpdateTargetRotations(int jointIndex)
    {
        // First for body joints
        for (int ji = jointIndex; ji >= 0; --ji)
        {
            GazeJoint joint = gazeJoints[ji];
            joint._UpdateTargetRotation();
        }
    }

    protected virtual void _InitLatencies()
    {
        // Compute torso joint latencies
        for (int gji = gazeJoints.Length - 1;
            torsoIndex > 0 && gji >= torsoIndex; --gji)
        {
            GazeJoint joint = gazeJoints[gji];
            joint.latency = _ComputeTorsoLatency(distRot, predictability);
        }

        // Initialize per-joint parameters
        float st0 = 0;
        float stmin = float.MaxValue;
        for (int i = 0; i < gazeJoints.Length; ++i)
        {
            GazeJoint joint = gazeJoints[i];
            st0 += joint.latency / 1000f;
            joint.latencyTime = st0;
            stmin = st0 < stmin ? st0 : stmin;
        }

        // Offset all latency times if needed
        if (stmin < 0)
        {
            stmin = Mathf.Abs(stmin);
            foreach (GazeJoint joint in gazeJoints)
                joint.latencyTime += stmin;
        }
    }

    protected virtual void _CalculateMaxVelocities()
    {
        // Estimate max. OMR
        // TODO: Should it be adjusted or original OMR?
        float OMR = -float.MaxValue;
        foreach (GazeJoint eye in eyes)
            OMR = Mathf.Max(OMR,
                            Mathf.Max(Mathf.Max(eye.curUpMR, eye.curDownMR),
                                      Mathf.Max(eye.curInMR, eye.curOutMR)));

        // For the upper body joints
        for (int gji = gazeJoints.Length - 1; gji >= eyes.Length; --gji)
        {
            GazeJoint joint = gazeJoints[gji];
            if (torsoIndex > 0 && gji >= torsoIndex)
                joint.maxVelocity = (4f / 3f) * (joint.velocity / 15f) * joint.distRotAlign +
                    joint.velocity / 0.5f;
            else if (gji >= headIndex)
                joint.maxVelocity = (4f / 3f) * (joint.velocity / 50f) * joint.distRotAlign +
                    joint.velocity / 2.5f;
        }
        /*for( int ji = 0; ji <= headIndex; ++ji )
        {
            GazeJoint joint = gazeJoints[ji];
            //if( joint.type == GazeJointType.Head )
            if( joint.IsEye )
                continue;
			
			
            //else if( joint.type == GazeJointType.Torso )
                joint.maxVelocity = 3.17f*Mathf.Exp(
                    0.0186f*(1f-joint.align)*distRot + 2.232f*joint.align
                    ) + 10.04f;
                //joint.maxVelocity = (1f-distRot/120f)*30f + distRot/120f*50f;
        }*/

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

                Vector3 vh1 = Head.FaceDirection;
                Head.bone.localRotation = Head.trgRotAlign;
                Vector3 vh2 = Head.FaceDirection;
                Vector3 vh = vh2 - vh1;
                Head.bone.localRotation = curhrot;

                for (int ei = 0; ei < eyes.Length; ++ei)
                {
                    GazeJoint eye = eyes[ei];
                    Quaternion curerot = eye.bone.localRotation;

                    Vector3 ve1 = eye.FaceDirection;
                    eye.bone.localRotation = eye._ComputeTargetRotation(EffGazeTargetPosition);
                    Quaternion erot = Quaternion.Inverse(eye.srcRot) * eye.bone.localRotation;
                    if (erot == Quaternion.identity)
                        break;
                    Vector3 ve2 = eye.FaceDirection;
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
                    /*eye.maxVelocity *= etl;
                    float et = Mathf.Clamp01( (Head.distRotAlign/Head.maxVelocity)*
                        (eye.distRotMR/eye.maxVelocity) );
                    eye.maxVelocity *= ( (1f-et)*etu/etl + et );*/
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
                Amin = Mathf.Min(eye.distRotMR, Amin);
            Amin = Mathf.Min(Amin, OMR);

            foreach (GazeJoint eye in eyes)
                // TODO: This is a quick hack that approximates eye velocity
                // based on about how far the saccade will be
                eye.maxVelocity = 4f * (eye.velocity / 150f) * Amin + eye.velocity / 6f;
        }

        if (stylizeGaze)
        {
            // Compute gaze quickness based on how long the gaze shift is
            float q = quickness;
            float rdt = Mathf.Clamp01(Head != null ? Head.distRotAlign / 90f : 0f);
            q = (1f + 0.2f * rdt) * q;

            // Speed up or slow down joints depending on quickness
            foreach (GazeJoint joint in gazeJoints)
                if (stylizeGaze)
                    joint.maxVelocity *= q;
        }
    }

    // Evoke a scheduled gaze blink to cover up a VOR artifact, if needed
    protected virtual void _InitGazeBlink()
    {
        if (!stylizeGaze || Head == null ||
           blinkCtrl == null || !stageGazeBlinks)
            return;

        // Compute view angles
        float vas, vat;
        _ComputeViewAngles(out vas, out vat);

        // TODO: probability of it being a staged blink,
        // should depend on 1) target view angle and 2) amount of VOR

        // Compute gaze blink probability
        float hrotd = UnityEngine.Quaternion.Angle(Head.SourceRotation, Head.TargetRotation);
        //float pb = 0.4f*hrotd/30f - 0.067f;
        float pb = 0.4f * (blinkCtrl.blinkRate / 0.2f) * hrotd / 30f - 0.067f;

        // Estimate blink start time
        float gst = _SimulateGazeShift(false);
        float tb = float.MaxValue;
        foreach (GazeJoint eye in eyes)
            if (eye.estTime < tb)
                tb = eye.estTime;
        if (tb > gst - 0.35f * blinkCtrl.blinkLength)
            // Gaze-evoked blink only makes sense if there is a long enough convergence phase
            return;
        tb -= 0.5f * blinkCtrl.blinkLength;

        // Generate blink
        blinkCtrl.Blink(tb, 1f, pb);
    }

    protected virtual void _RecalculateEyeVelocities()
    {
        foreach (GazeJoint eye in eyes)
            eye._RecalculateVelocity();
    }

    // Compute torso rotational amplitude from overall gaze shift amplitude
    protected virtual float _ComputeTorsoRotDistance(float distRot)
    {
        if (!enableAutoTorso)
            return 0;

        float dreff = (1f - Torso.align) * distRot + 120f * Torso.align;
        if (dreff >= 40f)
            return 0.43f * Mathf.Exp(0.029f * dreff) + 0.186f;
        else if (dreff >= 20f)
            return 0.078f * dreff - 1.558f;

        return 0;
    }

    // Compute torso latency from gaze shift amplitude and target predictability
    protected virtual float _ComputeTorsoLatency(float distRot, float pred)
    {
        pred = Mathf.Clamp01(pred);
        return -0.25f * distRot * pred + 0.5f * distRot - 57.5f * pred + 105f;
    }

    // Simulate the current gaze shift and measure time in various phases
    protected virtual float _SimulateGazeShift(bool persistState)
    {
        if (!persistState)
            _StoreState();
        foreach (GazeJoint joint in gazeJoints)
        {
            joint.estTime = 0;
            joint.estTimeMR = 0;
        }

        BitArray mr_reached = new BitArray(gazeJoints.Length, false);
        BitArray trg_reached = new BitArray(gazeJoints.Length, false);
        bool finished = false;
        float tt = 0f; // total time taken to complete the gaze shift
        float fdt = 1f / 30f;
        do
        {
            // Advance gaze shift
            float dt = 0;
            for (float t = 0; t < fdt; )
            {
                t += LEAPCore.eulerTimeStep;
                dt = (t <= fdt) ? LEAPCore.eulerTimeStep :
                    fdt - t + LEAPCore.eulerTimeStep;

                tt += dt;
                finished = _AdvanceGazeShift(dt);

                // Check how far along each joint is
                for (int ji = 0; ji < gazeJoints.Length && !persistState; ++ji)
                {
                    GazeJoint joint = gazeJoints[ji];
                    if (joint.mrReached && !mr_reached[ji])
                    {
                        joint.estTimeMR = tt;
                        mr_reached[ji] = true;
                    }

                    if (joint.trgReached && !trg_reached[ji])
                    {
                        joint.estTime = tt;
                        trg_reached[ji] = true;
                    }
                }
            }
        }
        while (!finished);

        if (!persistState)
            _RestoreState();

        return tt;
    }

    // Create a snapshot of the current gaze shift state
    protected virtual void _StoreState()
    {
        foreach (GazeJoint joint in gazeJoints)
            joint._StoreState();

    }

    // Restore the gaze shift state from a snapshot
    protected virtual void _RestoreState()
    {
        foreach (GazeJoint joint in gazeJoints)
            joint._RestoreState();
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
    }


}

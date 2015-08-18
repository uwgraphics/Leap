using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// This class extends the functionality of the general body join
/// controller, so it can be directed by the gaze controller.
/// </summary>
[Serializable]
public sealed class GazeJoint : DirectableJoint
{
    /// <summary>
    /// Type of the gaze joint.
    /// </summary>
    /// <remarks>There can be more than one left or right eye.
    /// Any joint that isn't an eye is a body joint (head, neck etc.)</remarks>
    public GazeJointType type = GazeJointType.LEye;

    /// <summary>
    /// Peak velocity of the joint.
    /// </summary>
    public float velocity = 50f; // 150 for the eyes

    /// <summary>
    /// Joint's upward motor range (in degs). 
    /// </summary>
    public float upMR = 90f; // 40 for the eyes

    /// <summary>
    /// Joint's downward motor range (in degs). 
    /// </summary>
    public float downMR = 90f;

    /// <summary>
    /// Joint's inward motor range (in degs).
    /// </summary>
    /// <remarks>"Inward" direction is right for all joints
    /// but the right eye (in which case it is left).</remarks>
    public float inMR = 180f; // 55 for the eyes

    /// <summary>
    /// Joint's outward motor range (in degs). 
    /// </summary>
    /// <remarks>"Outward" direction is left for all joints
    /// but the left eye (in which case it is right).</remarks>
    public float outMR = 180f;

    /// <summary>
    /// How much the gaze joint aligns with the eyes when gazing at a target (0-1).
    /// </summary>
    public float align = 1f;

    /// <summary>
    /// When the gaze joint will start moving relative to its immediate child
    /// in a gaze shift (in ms).
    /// </summary>
    public float latency = 100f;

    // Current joint velocity
    [HideInInspector]
    public float curVelocity = 50f;

    // Peak joint velocity
    [HideInInspector]
    public float maxVelocity = 50f;

    // MR adjusted by initial conditions
    [HideInInspector]
    public float adjUpMR;
    [HideInInspector]
    public float adjDownMR;
    [HideInInspector]
    public float adjInMR;
    [HideInInspector]
    public float adjOutMR;

    // Current MR (computed from adjusted MR,
    // based on current velocity)
    [HideInInspector]
    public float curUpMR;
    [HideInInspector]
    public float curDownMR;
    [HideInInspector]
    public float curInMR;
    [HideInInspector]
    public float curOutMR;

    // Latency timer
    [HideInInspector]
    public float latencyTime = 0f;

    // Joint alignment in the current gaze shift
    [HideInInspector]
    public float curAlign = 1f;

    // Horizontal joint alignment in the current gaze shift
    [HideInInspector]
    public float curHAlign = 1f;

    // Vertical joint alignment in the current gaze shift
    [HideInInspector]
    public float curVAlign = 1f;

    // Roll in the current gaze shift
    [HideInInspector]
    public float curRoll = 0f;

    // Source rotation (joint rotation at the start of gaze shift)
    [HideInInspector]
    public Quaternion srcRot;

    // Target rotation (joint rotation when it is fully aligned with the target)
    [HideInInspector]
    public Quaternion trgRot;

    // Target rotation constrained by alignment
    // (joint rotation that will be reached at the end of the gaze shift,
    // assuming no ancestor joints are rotating as well)
    [HideInInspector]
    public Quaternion trgRotAlign;

    // Target rotation constrained by joint motor limits
    // (joint rotation where the joint will get "stuck" due to motor limits)
    [HideInInspector]
    public Quaternion trgRotMR;

    // How much this eye has overshot the target
    // due to divergence.
    [HideInInspector]
    public Quaternion overshootRot;

    // If true, eye should stop overshooting and
    // start VOR-ing.
    [HideInInspector]
    public bool stopOvershoot;

    // Rotation progress (0-1, 0 corresponds to srcRot,
    // and 1 corresponds to trgRotAlign)
    [HideInInspector]
    public float rotParamAlign = 0;

    // Rotation progress (0-1, 0 corresponds to srcRot,
    // and 1 corresponds to trgRotMR)
    [HideInInspector]
    public float rotParamMR = 0;

    // Distance between srcRot and trgRotAlign,
    // a.k.a. angular distance the joint will rotate
    [HideInInspector]
    public float distRotAlign = 0f;

    // Distance between srcRot and trgRotMR,
    // a.k.a. angular distance the joint will rotate
    [HideInInspector]
    public float distRotMR = 0f;

    // true if OMR extents reached
    [HideInInspector]
    public bool mrReached = false;

    // true if target rotation reached
    [HideInInspector]
    public bool trgReached = false;

    // true if joint is in VOR mode
    [HideInInspector]
    public bool isVOR = false;

    // Source rotation during VOR
    [HideInInspector]
    public Quaternion fixSrcRot;

    // Target rotation during VOR
    [HideInInspector]
    public Quaternion fixTrgRot;

    // Alignment-constrained target rotation during VOR
    [HideInInspector]
    public Quaternion fixTrgRotAlign;

    // Rotation progress (0-1, 0 corresponds to srcRot,
    // and 1 corresponds to fixTrgRotAlign)
    [HideInInspector]
    public float fixRotParamAlign;

    // Rotation of the gaze joint before gaze is applied
    [HideInInspector]
    public Quaternion baseRot = Quaternion.identity;

    private MorphController morphCtrl;
    private GazeController gazeCtrl;

    public bool IsEye
    {
        get
        {
            return type == GazeJointType.LEye ||
                type == GazeJointType.REye;
        }
    }

    /// <summary>
    /// true if the joint is currently rotating, false
    /// if it is stationary.
    /// </summary>
    public bool IsRotating
    {
        get
        {
            return (IsEye && !mrReached || !IsEye) && rotParamAlign < 1f &&
                latencyTime <= 0f;
        }
    }

    /// <summary>
    /// Axis about which the gaze joint is rotating over the course
    /// of the gaze shift
    /// </summary>
    public Vector3 RotationAxis
    {
        get
        {
            Quaternion currot = bone.localRotation;

            Quaternion qs = isVOR ? fixSrcRot : srcRot;
            Quaternion qt = isVOR ? fixTrgRotAlign : trgRotAlign;

            Vector3 axis = Vector3.up.normalized;
            if (qs != qt)
            {
                bone.localRotation = qs;
                Vector3 vs = Direction.normalized;
                bone.localRotation = qt;
                Vector3 vt = Direction.normalized;
                axis = Vector3.Cross(vs, vt).normalized;
            }

            bone.localRotation = currot;

            return axis;
        }
    }

    /// <summary>
    /// How much this gaze joint contributes to the overall rotation of
    /// its body part.
    /// </summary>
    public float RotationContribution
    {
        get
        {
            var last = gazeCtrl.GetLastGazeJointInChain(type);
            int ji = gazeCtrl.FindGazeJointIndex(this) - gazeCtrl.FindGazeJointIndex(last);
            int nj = gazeCtrl.GetNumGazeJointsInChain(type);
            float c = 2f * (nj - ji) / (nj * (nj + 1f));

            return c;
        }
    }

    /// <summary>
    /// Gaze controller that owns the current gaze joint.
    /// </summary>
    public GazeController GazeController
    {
        get { return gazeCtrl; }
    }

    /// <summary>
    /// Initializes the gaze joint.
    /// </summary>
    /// <param name="agent">
    /// Virtual agent.
    /// </param>
    public override void Init(GameObject agent)
    {
        base.Init(agent);

        gazeCtrl = agent.GetComponent<GazeController>();
        morphCtrl = agent.GetComponent<MorphController>();

        // Initialize rotations
        srcRot = bone.localRotation;
        trgRot = srcRot;

        // Initialize MRs
        _ResetAdjMR();
        _InitCurMR();
    }

    /// <summary>
    /// true if MR limits are broken, otherwise false.
    /// </summary>
    public bool CheckMR()
    {
        float yaw = this.bone == mdlCtrl.REye ? (Yaw - InitYaw) : (-Yaw + InitYaw);
        float pitch = Pitch - InitPitch;

        float y2 = yaw*yaw;
        float p2 = pitch*pitch;
		
        float res = 0f;
        if( yaw >= 0f && pitch >= 0f )
            res = y2/(curInMR*curInMR) + p2/(curDownMR*curDownMR);
        else if( yaw <= 0f && pitch >= 0f )
            res = y2/(curOutMR*curOutMR) + p2/(curDownMR*curDownMR);
        else if( yaw <= 0f && pitch <= 0f )
            res = y2/(curOutMR*curOutMR) + p2/(curUpMR*curUpMR);
        else if( yaw >= 0f && pitch <= 0f )
            res = y2/(curInMR*curInMR) + p2/(curUpMR*curUpMR);
			
        return res >= 1f;

        /*bool res = false;
        if (yaw >= 0f && pitch >= 0f)
            res = yaw <= curInMR && pitch <= curDownMR;
        else if (yaw <= 0f && pitch >= 0f)
            res = -yaw <= curOutMR && pitch <= curDownMR;
        else if (yaw <= 0f && pitch <= 0f)
            res = -yaw <= curOutMR && -pitch <= curUpMR;
        else if (yaw >= 0f && pitch <= 0f)
            res = yaw <= curInMR && -pitch <= curUpMR;

        return !res;*/
    }

    /// <summary>
    /// Clamp joint orientation to MR limits.
    /// </summary>
    public void ClampMR()
    {
        // TODO: use a smarter/more efficient method here to determine OMR bound rotation
        // (e.g. adaptive sampling)
        Quaternion trgrot = bone.localRotation;
        bone.localRotation = srcRot;
        bool srcMRReached = CheckMR();
        for (float t = 0f; t <= 1f; )
        {
            // Update joint rotation
            Quaternion prevrot = bone.localRotation;
            bone.localRotation = Quaternion.Slerp(srcRot, trgrot, t);

            // Has the joint violated MR limits?
            if (CheckMR())
            {
                if (!srcMRReached)
                {
                    // Yes, previous rotation is as far as we can go
                    bone.localRotation = prevrot;
                    return;
                }
            }
            else
            {
                if (srcMRReached)
                {
                    // We were outside OMR range at the start, but now we are back in valid range
                    srcMRReached = false;
                }
            }

            // Advance joint rotation
            t += 0.01f;
        }
    }

    // OMR changes depending on head velocity
    public void _UpdateOMR()
    {
        if (gazeCtrl.Head != null && IsEye)
        {
            // TODO: because OMR changes during the gaze shift, target rotations
            // need to be continually updated,
            // yet currently it's too inefficient to update them
            // on every fixed step. Recomputations due to updateOMR() should be
            // made more efficient, so they can be done
            // at the end of every fixed step.
            curUpMR = adjUpMR * (-1f / 600f * gazeCtrl.Head.curVelocity + 1f);
            curDownMR = adjDownMR * (-1f / 600f * gazeCtrl.Head.curVelocity + 1f);
            curInMR = adjInMR * (-1f / 600f * gazeCtrl.Head.curVelocity + 1f);
            curOutMR = adjOutMR * (-1f / 600f * gazeCtrl.Head.curVelocity + 1f);
            /*curUpMR = upMR;
            curDownMR = downMR;
            curInMR = inMR;
            curOutMR = outMR;*/
        }
    }

    // Initialize VOR movement
    public void _InitVOR()
    {
        isVOR = true;

        fixSrcRot = srcRot;
        if (gazeCtrl.FixGazeTarget != null)
        {
            fixTrgRot = trgRot;
            fixTrgRotAlign = IsEye ? trgRot : trgRotAlign;
            fixRotParamAlign = rotParamAlign;
        }
        else
        {
            fixTrgRot = fixTrgRotAlign = bone.localRotation;
            fixRotParamAlign = 1f;
        }
    }

    // Stop VOR movement
    public void _StopVOR()
    {
        isVOR = false;
    }

    // Apply VOR movement
    public void _ApplyVOR()
    {
        // Update VOR rotation
        _UpdateVORTargetRotation();

        // Apply VOR rotation
        Quaternion rot = Quaternion.Slerp(fixSrcRot, fixTrgRotAlign, fixRotParamAlign);
        _ApplyRotation(rot);
        if (IsEye)
        {
            // VOR must not break OMR limits
            if (mrReached = CheckMR())
            {
                Quaternion curSrcRot = srcRot;
                srcRot = fixSrcRot;
                ClampMR();
                srcRot = curSrcRot;
            }
        }
    }

    // Update rotations during VOR before gaze shift start, to account for VOR movement
    public void _UpdateRotationsOnVOR()
    {
        // Update new source and target rotations
        float prevDistRot = DistanceToRotate(srcRot, trgRot);
        srcRot = bone.localRotation;
        trgRot = _ComputeTargetRotation(gazeCtrl.EffGazeTargetPosition);
        float distRot = DistanceToRotate(srcRot, trgRot);

        if (!IsEye)
        {
            // Update aligning target rotations
            float fr = prevDistRot > 0.0001f ? distRot / prevDistRot : 1f;
            distRotAlign *= fr;
            float ar = distRot > 0.0001f ? distRotAlign / distRot : 1f;
            trgRotAlign = Quaternion.Slerp(srcRot, trgRot, ar);
        }
        else
        {
            // Update OMR-constrained target rotations
            trgRotAlign = trgRot;
            _InitTargetRotationMR();
        }
    }

    // Initialize parameters at the start of a gaze shift
    public void _InitGazeParams()
    {
        // Initialize MR
        mrReached = false;
        trgReached = false;
        _ResetAdjMR();
        _InitCurMR();

        // Init. joint alignment and latency
        align = IsEye ? 1f : Mathf.Clamp01(align);
        curAlign = align;
        latencyTime = 0;

        // Initialize rotations
        srcRot = bone.localRotation;
        rotParamAlign = 0;
        rotParamMR = 0;
        _InitTargetRotation();
        if (gazeCtrl.stylizeGaze)
        {
            overshootRot = Quaternion.identity;
            stopOvershoot = false;
        }
    }

    // Initialize target rotations for the gaze shift
    public void _InitTargetRotation()
    {
        trgRot = _ComputeTargetRotation(gazeCtrl.EffGazeTargetPosition);
        trgRotMR = trgRotAlign = trgRot;
        distRotMR = distRotAlign = DistanceToRotate(srcRot, trgRotAlign);
    }

    // Initialize OMR-constrained target rotations for the gaze shift
    public void _InitTargetRotationMR()
    {
        Quaternion currot = bone.localRotation;

        // Compute target rotation constrained by MR
        bone.localRotation = srcRot;
        if (CheckMR())
        {
            // The joint shouldn't start outside of valid MR, so relax it
            // TODO: this issue should be fixed in some smarter way...

            float pitch = Pitch - InitPitch;
            float yaw = Yaw - InitYaw;

            if (pitch >= curDownMR)
            {
                adjDownMR = pitch + 1f;
                curDownMR = pitch + 1f;
            }
            else if (-pitch >= curUpMR)
            {
                adjUpMR = -pitch + 1f;
                curUpMR = -pitch + 1f;
            }
            else if (type == GazeJointType.LEye &&
                    yaw >= curOutMR ||
                    type == GazeJointType.REye &&
                    -yaw >= curOutMR)
            {
                adjOutMR = Mathf.Abs(yaw) + 1f;
                curOutMR = Mathf.Abs(yaw) + 1f;
            }
            else if (type == GazeJointType.LEye &&
                    -yaw >= curInMR ||
                    type == GazeJointType.REye &&
                    yaw >= curInMR)
            {
                adjInMR = Mathf.Abs(yaw) + 1f;
                curInMR = Mathf.Abs(yaw) + 1f;
            }
        }
        bone.localRotation = trgRot;
        ClampMR(); // TODO: ignore MR for non-eye joints?
        trgRotMR = bone.localRotation;
        distRotMR = DistanceToRotate(srcRot, trgRotMR);

        bone.localRotation = currot;
    }

    // Update target rotations during gaze shift to account for movement of the parent joints
    public void _UpdateTargetRotation()
    {
        float prevRotParamMR = rotParamMR;
        float prevRotParamAlign = rotParamAlign;
        float prevDistRotAlign = distRotAlign;
        float prevDistRotMR = distRotMR;
        float prevDistRot = DistanceToRotate(srcRot, trgRot);
        trgRot = _ComputeTargetRotation(gazeCtrl.EffGazeTargetPosition);
        if (!IsEye)
        {
            float prevAlign = prevDistRot > 0.0001f ? prevDistRotAlign / prevDistRot : 1f;
            trgRotAlign = Quaternion.Slerp(srcRot, trgRot, prevAlign);
        }
        else
        {
            trgRotAlign = trgRot;
            // Compute new target rotation constrained by OMR
            // TODO: this is quite inefficient in frame loop,
            // should be recomputed in some other way?
            _InitTargetRotationMR();
        }
        distRotAlign = DistanceToRotate(srcRot, trgRotAlign);

        // Renormalize rotation progress
        rotParamAlign *= (distRotAlign > 0.00001f ? prevDistRotAlign / distRotAlign : 1f);
        rotParamAlign = Mathf.Max(Mathf.Clamp01(rotParamAlign), prevRotParamAlign);
        if (IsEye)
        {
            rotParamMR *= (distRotMR > 0.00001f ? prevDistRotMR / distRotMR : 1f);
            rotParamMR = Mathf.Max(Mathf.Clamp01(rotParamMR), prevRotParamMR);
        }
    }

    // Update target rotations during VOR to account for movement of the parent joints
    public void _UpdateVORTargetRotation()
    {
        float prevFixDistRotAlign = DistanceToRotate(fixSrcRot, fixTrgRotAlign);
        float prevFixDistRot = DistanceToRotate(fixSrcRot, fixTrgRot);
        if (gazeCtrl.FixGazeTarget != null)
        {
            fixTrgRot = _ComputeTargetRotation(gazeCtrl.FixGazeTarget.transform.position);
            if (!IsEye)
            {
                float prevFixRotParamAlign = prevFixDistRot > 0.0001f ? prevFixDistRotAlign / prevFixDistRot : 1f;
                fixTrgRotAlign = Quaternion.Slerp(fixSrcRot, fixTrgRot, prevFixRotParamAlign);
            }
            else
            {
                fixTrgRotAlign = fixTrgRot;
            }
        }

        if (IsEye)
        {
            fixTrgRotAlign = fixTrgRot = gazeCtrl.FixGazeTarget != null ?
                _ComputeTargetRotation(gazeCtrl.FixGazeTarget.transform.position) : bone.localRotation;
        }
    }

    // Rotate the joint towards the target over the course of the gaze shift
    public bool _AdvanceRotation(float deltaTime)
    {
        GazeJoint last = gazeCtrl.GetLastGazeJointInChain(type);
        bool isLast = last == this;

        if (trgReached || // target reached in previous step, just VOR
            last.trgReached ||
            maxVelocity < 0.00001f || // joint has zero velocity
            distRotAlign < 0.00001f) // joint doesn't need to rotate at all
        {
            // TODO: rotParam* values for the eyes fall away from 1 for some reason
            if (IsEye)
                rotParamAlign = rotParamMR = 1f;

            if (gazeCtrl.stylizeGaze && gazeCtrl.enableED &&
                IsEye && outMR > inMR + 0.00001f)
            {
                // We allow some target overshoot for divergent eyes (with asym. OMR)
                if (!stopOvershoot)
                    overshootRot = Quaternion.Inverse(trgRotAlign) * bone.localRotation;
                else
                    _ApplyRotation(trgRotAlign * overshootRot);
            }
            else
            {
                _ApplyRotation(srcRot == trgRotAlign ? srcRot :
                    Quaternion.Slerp(srcRot, trgRotAlign, rotParamAlign));
            }

            if (IsEye && (mrReached = CheckMR()))
            {
                // If MR has been reached, clamp the rotation
                ClampMR();
            }

            trgReached = true;
            return trgReached;
        }

        // Compute velocity contribution for this joint
        int ji = gazeCtrl.FindGazeJointIndex(this) - gazeCtrl.FindGazeJointIndex(last);
        int nj = gazeCtrl.GetNumGazeJointsInChain(type);
        float v = 2f * (nj - ji) / (nj * (nj + 1f)) * last.curVelocity;

        // Update and apply new rotation
        float ddrot = deltaTime * v;
        rotParamAlign = Mathf.Clamp01(rotParamAlign + ddrot / distRotAlign);
        rotParamMR = Mathf.Clamp01(rotParamMR + ddrot / distRotMR);
        _ApplyRotation(srcRot == trgRotAlign ? srcRot :
            Quaternion.Slerp(srcRot, trgRotAlign, rotParamAlign));

        if (IsEye && (mrReached = CheckMR()))
        {
            // If MR has been reached, clamp the rotation
            ClampMR();
            rotParamAlign = distRotAlign > 0f ? DistanceToRotate(srcRot, bone.localRotation) / distRotAlign : 0f;
            rotParamMR = distRotMR > 0f ? DistanceToRotate(srcRot, bone.localRotation) / distRotMR : 0f;
        }
        // TODO: this is a hack to prevent OMR violation
        if (Mathf.Abs(Pitch) > curDownMR)
            Pitch = Mathf.Sign(Pitch) * curDownMR;
        //

        // Is the gaze shift finished?
        trgReached = rotParamAlign >= 1f;
        if (trgReached && isLast)
        {
            // Stop all the other joints in the chain
            foreach (GazeJoint joint in gazeCtrl.gazeJoints)
            {
                if (joint.type == type)
                    joint.trgReached = true;
            }
        }

        return trgReached;
    }

    // Apply new rotation to the joint
    public void _ApplyRotation(Quaternion q)
    {
        // Eliminate roll component from the joint's rotation
        bone.localRotation = q;
       if (IsEye || gazeCtrl.removeRoll)
            _RemoveRoll();
        q = bone.localRotation;

        // Blend with base joint rotation
        bone.localRotation = Quaternion.Slerp(baseRot, q, isVOR ? gazeCtrl.fixWeight : gazeCtrl.weight);
    }

    // Reset adjusted OMR
    public void _ResetAdjMR()
    {
        adjUpMR = upMR;
        adjDownMR = downMR;
        adjInMR = inMR;
        adjOutMR = outMR;
    }

    // Initialize OMR for a new gaze shift
    public void _InitCurMR()
    {
        curUpMR = adjUpMR;
        curDownMR = adjDownMR;
        curInMR = adjInMR;
        curOutMR = adjOutMR;
    }

    // Recalculate joint velocity based on gaze shift progress
    public void _RecalculateVelocity()
    {
        if (IsEye)
        {
            // Which eye has the farthest to rotate?
            float A = -float.MaxValue;
            GazeJoint eyeA = null;
            foreach (GazeJoint eye in gazeCtrl.eyes)
            {
                if (eye.distRotMR > A)
                {
                    A = eye.distRotMR;
                    eyeA = eye;
                }
            }

            // Which eye has rotated the farthest?
            float soFar = eyeA.rotParamMR * eyeA.distRotMR;

            // Overall gaze shift progress for the eyes
            float erp = A > 0f ? soFar / A : 1f;
            erp = Mathf.Clamp01(erp);
            float erp2 = erp * erp;
            // Adjust eye velocities based on progress
            if (erp < 0.5f)
                curVelocity = (erp + 0.5f) * maxVelocity;
            else
                curVelocity = (8f * erp2 * erp - 18f * erp2 + 12f * erp - 1.5f) * maxVelocity;
        }
        else
        {
            float rp = rotParamAlign;
            float rpmv = rp * maxVelocity;
            float rp2 = rp * rp;

            if (rp < 0.5)
                curVelocity = rpmv * 0.75f / 0.5f + 0.25f * maxVelocity;
            else
                curVelocity = (12f * rp2 * rp - 27f * rp2 + 18f * rp - 2.75f) * maxVelocity;
        }
    }

    // Remove roll component from the joint rotation
    public void _RemoveRoll()
    {
        // TODO: What if z isn't the roll axis?
        bone.localRotation = Quaternion.Euler(bone.localRotation.eulerAngles.x,
                                              bone.localRotation.eulerAngles.y, 0);
    }

    // Compute target rotation that aligns the joint with the specified world-space target position
    public Quaternion _ComputeTargetRotation(Vector3 wTargetPos)
    {
        Quaternion currot = bone.localRotation;
        Quaternion trgrot;

        // Compute target rotation of the joint
        trgrot = ModelUtils.LookAtRotation(bone, wTargetPos);

        // Strip roll out of the target rotation
        // TODO: should this be conditional upon gazeCtrl.removeRoll?
        /*trgrot *= COBRot;
        Quaternion li_rot = mdlCtrl.GetInitRotation(bone) * COBRot;
        trgrot = Quaternion.Inverse(li_rot) * trgrot;
        trgrot = li_rot * trgrot;
        trgrot *= Quaternion.Inverse(COBRot);*/

        if (!IsEye)
        {
            // Adjust pitch downward (otherwise chain would overshoot the target)
            bone.localRotation = trgrot;
            Vector3 l_lre = bone.InverseTransformDirection((gazeCtrl.LEye.bone.position -
                gazeCtrl.REye.bone.position).normalized);
            Vector3 l_trg1 = bone.InverseTransformDirection(Direction);
            Vector3 l_up = Vector3.Cross(l_lre, l_trg1);
            Plane hpl = new Plane(l_up, bone.localPosition);
            float h = hpl.GetDistanceToPoint(bone.InverseTransformPoint(
                0.5f * (gazeCtrl.LEye.bone.position +
                gazeCtrl.REye.bone.position)));
            Vector3 l_dir1 = (bone.InverseTransformPoint(wTargetPos) - (bone.localPosition + h * l_up)).normalized;
            Quaternion l_drot = Quaternion.FromToRotation(l_trg1, l_dir1);
            trgrot *= l_drot; // final joint rotation

            bone.localRotation = currot; // restore original rotation
        }

        //trgrot = Quaternion.Euler(trgrot.eulerAngles.x, trgrot.eulerAngles.y, 0f);

        return trgrot;
    }

    // Get snapshot of the gaze joint's state
    public GazeControllerState.GazeJointState _GetRuntimeState()
    {
        GazeControllerState.GazeJointState state = new GazeControllerState.GazeJointState();
        
        state.gazeJointType = type;
        state.velocity = velocity;
        state.upMR = upMR;
        state.downMR = downMR;
        state.inMR = inMR;
        state.outMR = outMR;
        state.align = align;
        state.latency = latency;
        state.rot = bone.localRotation;
        state.srcRot = srcRot;
        state.trgRot = trgRot;
        state.trgRotAlign = trgRotAlign;
        state.trgRotMR = trgRotMR;
        state.distRotAlign = distRotAlign;
        state.distRotMR = distRotMR;
        state.rotParamAlign = rotParamAlign;
        state.rotParamMR = rotParamMR;
        state.maxVelocity = maxVelocity;
        state.curVelocity = curVelocity;
        state.latency = latency;
        state.latencyTime = latencyTime;
        state.mrReached = mrReached;
        state.trgReached = trgReached;
        state.adjUpMR = adjUpMR;
        state.adjDownMR = adjDownMR;
        state.adjInMR = adjInMR;
        state.adjOutMR = adjOutMR;
        state.curUpMR = curUpMR;
        state.curDownMR = curDownMR;
        state.curInMR = curInMR;
        state.curOutMR = curOutMR;
        state.curAlign = curAlign;
        state.isVOR = isVOR;
        state.fixSrcRot = fixSrcRot;
        state.fixTrgRotAlign = fixTrgRotAlign;
        state.fixRotParamAlign = fixRotParamAlign;
        state.baseRot = baseRot;

        return state;
    }

    // Set gaze joint's state from a snapshot
    public void _SetRuntimeState(GazeControllerState.GazeJointState state)
    {
        type = state.gazeJointType;
        velocity = state.velocity;
        upMR = state.upMR;
        downMR = state.downMR;
        inMR = state.inMR;
        outMR = state.outMR;
        align = state.align;
        latency = state.latency;
        bone.localRotation = state.rot;
        srcRot = state.srcRot;
        trgRot = state.trgRot;
        trgRotAlign = state.trgRotAlign;
        trgRotMR = state.trgRotMR;
        distRotAlign = state.distRotAlign;
        distRotMR = state.distRotMR;
        rotParamAlign = state.rotParamAlign;
        rotParamMR = state.rotParamMR;
        maxVelocity = state.maxVelocity;
        curVelocity = state.curVelocity;
        latency = state.latency;
        latencyTime = state.latencyTime;
        mrReached = state.mrReached;
        trgReached = state.trgReached;
        adjUpMR = state.adjUpMR;
        adjDownMR = state.adjDownMR;
        adjInMR = state.adjInMR;
        adjOutMR = state.adjOutMR;
        curUpMR = state.curUpMR;
        curDownMR = state.curDownMR;
        curInMR = state.curInMR;
        curOutMR = state.curOutMR;
        curAlign = state.curAlign;
        isVOR = state.isVOR;
        baseRot = state.baseRot;
        fixSrcRot = state.fixSrcRot;
        fixTrgRotAlign = state.fixTrgRotAlign;
        fixRotParamAlign = state.fixRotParamAlign;
    }
}

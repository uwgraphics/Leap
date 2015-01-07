using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// This class extends the functionality of the general body join
/// controller, so it can be directed by the gaze controller.
/// </summary>
[Serializable]
public class GazeJoint : DirectableJoint
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
    public float latency = 0f;

    // Current joint velocity
    [HideInInspector]
    public float curVelocity = 50f;

    // Peak joint velocity
    [HideInInspector]
    public float maxVelocity = 50f;

    /// <summary>
    /// Weight with which this joint's rotation is applied to the underlying body motion
    /// </summary>
    public float weight = 1f;

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

    // Source rotation during VOR
    [HideInInspector]
    public Quaternion fixSrcRot;

    // Target rotation during VOR
    [HideInInspector]
    public Quaternion fixTrgRot;

    // Alignment-constrained target rotation during VOR
    [HideInInspector]
    public Quaternion fixTrgRotAlign;

    // Distance between source rotation and aligning target rotation during VOR
    [HideInInspector]
    public float fixDistRotAlign;

    // Rotation progress during VOR
    [HideInInspector]
    public float fixRotParamAlign;

    // Joint weight in the current gaze shift
    [HideInInspector]
    public float curWeight = 1f;

    private MorphController morphCtrl;
    private GazeController gazeCtrl;

    public virtual bool IsEye
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
    public virtual bool IsRotating
    {
        get
        {
            return !mrReached && rotParamAlign < 1f &&
                latencyTime <= 0f;
        }
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
    public virtual bool CheckMR()
    {
        float yaw = this.bone == mdlCtrl.REye ? (Yaw - InitYaw) : (-Yaw + InitYaw);
        float pitch = Pitch - InitPitch;

        // TODO: elliptic OMR doesn't work as well as I expected
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
    public virtual void ClampMR()
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

    public void _InitVOR()
    {
        fixSrcRot = srcRot;
        fixTrgRot = _ComputeTargetRotation(gazeCtrl.FixGazeTargetPosition);

        if (IsEye)
        {
            fixRotParamAlign = rotParamAlign;
            fixTrgRotAlign = fixTrgRot;
        }
        else
        {
            Quaternion curRot = Quaternion.Slerp(srcRot, trgRotAlign, rotParamAlign);
            float fixDistRot = DistanceToRotate(fixSrcRot, fixTrgRot);
            float fixDistRotAlign = DistanceToRotate(fixSrcRot, curRot);
            fixRotParamAlign = fixDistRot > 0.00001f ? fixDistRotAlign / fixDistRot : 0f;
            fixTrgRotAlign = fixSrcRot != fixTrgRot ? Quaternion.Slerp(fixSrcRot, fixTrgRot, fixRotParamAlign) : fixSrcRot;
            // TODO: there might be a small discontinuity here, keep an eye out for it
        }
    }

    public void _ApplyVOR()
    {
        _UpdateVORTargetRotation();

        _ApplyRotation(fixTrgRotAlign);
        if (IsEye)
        {
            // VOR fixation must not break OMR limits
            if (mrReached = CheckMR())
            {
                Quaternion curSrcRot = srcRot;
                srcRot = fixSrcRot;
                ClampMR();
                srcRot = curSrcRot;
            }
        }
    }

    public void _UpdateVORTargetRotation()
    {
        if (IsEye)
        {
            fixTrgRotAlign = fixTrgRot = _ComputeTargetRotation(gazeCtrl.FixGazeTargetPosition);
        }
        else
        {
            fixTrgRot = _ComputeTargetRotation(gazeCtrl.FixGazeTargetPosition);
            fixTrgRotAlign = fixSrcRot != fixTrgRot ? Quaternion.Slerp(fixSrcRot, fixTrgRot, fixRotParamAlign) : fixSrcRot;
        }
    }

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
        curWeight = weight;

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

    public void _InitTargetRotation()
    {
        trgRot = _ComputeTargetRotation(gazeCtrl.EffGazeTargetPosition);
        trgRotMR = trgRotAlign = trgRot;
        distRotMR = distRotAlign = DistanceToRotate(srcRot, trgRotAlign);
    }

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

    public void _UpdateTargetRotation()
    {
        // How much will the joint rotate?
        float prev_dra = distRotAlign;
        float prev_drmr = distRotMR;
        float prev_fdr = DistanceToRotate(srcRot, trgRot);
        trgRot = _ComputeTargetRotation(gazeCtrl.EffGazeTargetPosition);
        if (!IsEye)
        {
            float arp = prev_fdr > 0.00001f ? prev_dra / prev_fdr : 1f;
            trgRotAlign = Quaternion.Slerp(srcRot, trgRot, arp);
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
        rotParamAlign *= (distRotAlign > 0.00001f ? prev_dra / distRotAlign : 1f);
        rotParamAlign = Mathf.Clamp01(rotParamAlign);
        if (IsEye)
        {
            rotParamMR *= (distRotMR > 0.00001f ? prev_drmr / distRotMR : 1f);
            rotParamMR = Mathf.Clamp01(rotParamMR);
        }
    }

    public bool _AdvanceRotation(float deltaTime)
    {
        GazeJoint last = gazeCtrl.GetLastGazeJointInChain(type);
        bool isLast = last == this;

        // Compute renormalized rotation from previous step
        Quaternion currot = srcRot == trgRotAlign ? srcRot :
            Quaternion.Slerp(srcRot, trgRotAlign, rotParamAlign);

        if (trgReached || // target reached in previous step, just VOR
            last.trgReached ||
            maxVelocity < 0.00001f || // joint has zero velocity
            distRotAlign < 0.00001f) // joint doesn't need to rotate at all
        {
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
                _ApplyRotation(currot);
            }

            if (IsEye && (mrReached = CheckMR()))
            {
                // If MR has been reached, clamp the rotation
                bone.localRotation = currot;
                ClampMR();
            }

            trgReached = true;
            // TODO: some snapping occurs if not for this hack
            /*if(IsEye)
                rotParamAlign = rotParamMR = 1f;*/

            return trgReached;
        }

        // Compute velocity contribution for this joint
        int ji = gazeCtrl.FindGazeJointIndex(this) - gazeCtrl.FindGazeJointIndex(last);
        int nj = gazeCtrl.GetNumGazeJointsInChain(type);
        float v = 2f * (nj - ji) / (nj * (nj + 1f)) * last.curVelocity;

        // Update and apply new rotation
        float rpa = rotParamAlign;
        float rpmr = rotParamMR;
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

    public void _ApplyRotation(Quaternion q)
    {
        var last = gazeCtrl.GetLastGazeJointInChain(type);
        bone.localRotation = Quaternion.Slerp(bone.localRotation, q, last.curWeight);
        if (IsEye || !IsEye && gazeCtrl.removeRoll)
            _RemoveRoll(); // gaze rotations should have no roll component
    }

    public void _ResetAdjMR()
    {
        adjUpMR = upMR;
        adjDownMR = downMR;
        adjInMR = inMR;
        adjOutMR = outMR;
    }

    public void _InitCurMR()
    {
        curUpMR = adjUpMR;
        curDownMR = adjDownMR;
        curInMR = adjInMR;
        curOutMR = adjOutMR;
    }

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

            //if( type == GazeJointType.Head )
            {
                if (rp < 0.5)
                    curVelocity = rpmv * 0.75f / 0.5f + 0.25f * maxVelocity;
                else
                    curVelocity = (12f * rp2 * rp - 27f * rp2 + 18f * rp - 2.75f) * maxVelocity;
            }
            /*else
            {
                if (rp < 0.5)
                    curVelocity = rpmv + 0.5f*maxVelocity;
                else
                    curVelocity = ( 8f*rp2*rp - 18f*rp2 + 12f*rp - 1.5f )*maxVelocity;
            }*/
        }
    }

    public void _RemoveRoll()
    {
        // TODO: What if z isn't the roll axis?
        bone.localRotation = Quaternion.Euler(bone.localRotation.eulerAngles.x,
                                              bone.localRotation.eulerAngles.y, 0);
    }

    public Quaternion _ComputeTargetRotation(Vector3 wTargetPos)
    {
        Quaternion cur_rot = bone.localRotation;
        Quaternion trgrot;

        // Compute target rotation of the joint
        Vector3 l_dir0 = bone.InverseTransformDirection(Direction);
        Vector3 l_dir = bone.InverseTransformDirection((wTargetPos - bone.position).normalized);
        if (l_dir0 == l_dir)
        {
            // No need to adjust target rotation
            return bone.localRotation;
        }

        trgrot = bone.localRotation * (QuaternionUtil.Equal(l_dir0, l_dir) ?
            Quaternion.identity : Quaternion.FromToRotation(l_dir0, l_dir));

        // Strip roll out of the target rotation
        trgrot *= COBRot;
        Quaternion li_rot = mdlCtrl.GetInitRotation(bone) * COBRot;
        trgrot = Quaternion.Inverse(li_rot) * trgrot;
        trgrot = li_rot * trgrot;
        trgrot *= Quaternion.Inverse(COBRot);

        if (!IsEye)
        {
            // Adjust pitch (otherwise chain would overshoot the target)
            bone.localRotation = trgrot;
            Vector3 l_lre = bone.InverseTransformDirection((gazeCtrl.ModelController.LEye.position -
                gazeCtrl.ModelController.REye.position).normalized);
            Vector3 l_trg1 = bone.InverseTransformDirection(Direction);
            Vector3 l_up = Vector3.Cross(l_lre, l_trg1);
            Plane hpl = new Plane(l_up, bone.localPosition);
            float h = hpl.GetDistanceToPoint(bone.InverseTransformPoint(
                0.5f * (gazeCtrl.ModelController.LEye.position +
                gazeCtrl.ModelController.REye.position)));
            Vector3 l_dir1 = (bone.InverseTransformPoint(wTargetPos) - (bone.localPosition + h * l_up)).normalized;
            Quaternion l_drot = Quaternion.FromToRotation(l_trg1, l_dir1);
            trgrot *= l_drot; // final joint rotation

            bone.localRotation = cur_rot; // restore original rotation
        }

        return trgrot;
    }

    // Write gaze joint state to log
    public void _LogState()
    {
        if (this != gazeCtrl.GetLastGazeJointInChain(type))
            return;

        /*Debug.Log(string.Format("{0}: curVelocity = {1} [maxVelocity = {2}], latencyTime = {3}, cur*OMR = ({4}, {5}, {6}, {7}), " +
            "curAlign = {8}, srcRot = ({9}, {10}, {11}), trgRot = ({12}, {13}, {14}), trgRotAlign = ({15}, {16}, {17}), trgRotMR = ({18}, {19}, {20}), " +
            "distRotAlign = {21}, distRotMR = {22}, rotParamAlign = {23}, rotParamMR = {24}, mrReached = {25}, trgReached = {26}, " +
            "fixSrcRot = ({27}, {28}, {29}), fixTrgRot = ({30}, {31}, {32}), fixDistRotAlign = {33}, fixRotParamAlign = {34}", 
            type.ToString(), curVelocity, maxVelocity, latencyTime, curUpMR, curDownMR, curInMR, curOutMR,
            curAlign, srcRot.eulerAngles.x, srcRot.eulerAngles.y, srcRot.eulerAngles.z,
            trgRot.eulerAngles.x, trgRot.eulerAngles.y, trgRot.eulerAngles.z,
            trgRotAlign.eulerAngles.x, trgRotAlign.eulerAngles.y, trgRotAlign.eulerAngles.z,
            trgRotMR.eulerAngles.x, trgRotMR.eulerAngles.y, trgRotMR.eulerAngles.z,
            distRotAlign, distRotMR, rotParamAlign, rotParamMR, mrReached, trgReached,
            fixSrcRot.eulerAngles.x, fixSrcRot.eulerAngles.y, fixSrcRot.eulerAngles.z,
            fixTrgRot.eulerAngles.x, fixTrgRot.eulerAngles.y, fixTrgRot.eulerAngles.z,
            fixTrgRotAlign.eulerAngles.x, fixTrgRotAlign.eulerAngles.y, fixTrgRotAlign.eulerAngles.z,
            fixDistRotAlign, fixRotParamAlign));*/
    }
}

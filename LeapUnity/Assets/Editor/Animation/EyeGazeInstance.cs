using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Eye gaze animation instance that specified a gaze shift to
/// a specific target, followed by a gaze fixation on that target.
/// </summary>
public class EyeGazeInstance : AnimationControllerInstance
{
    /// <summary>
    /// Gaze animation controller.
    /// </summary>
    public virtual GazeController GazeController
    {
        get { return _controller as GazeController; }
    }

    /// <summary>
    /// Gaze target.
    /// </summary>
    public virtual GameObject Target
    {
        get;
        set;
    }

    /// <summary>
    /// How much the head should align with the target.
    /// </summary>
    public virtual float HeadAlign
    {
        get;
        set;
    }

    /// <summary>
    /// How much the upper body should align with the target.
    /// </summary>
    public virtual float TorsoAlign
    {
        get;
        set;
    }

    protected bool _gazeShiftStarted = false;

    // Runtime state of an eye gaze controller
    protected struct _GazeControllerState
    {
        public float amplitude;
        public GameObject currentGazeTarget;
        public _GazeJointState[] gazeJointStates;

        public _GazeControllerState(GazeController controller)
        {
            amplitude = controller.Amplitude;
            currentGazeTarget = controller._CurrentGazeTarget;
            gazeJointStates = new _GazeJointState[controller.gazeJoints.Length];
            for (int jointIndex = 0; jointIndex < controller.gazeJoints.Length; ++jointIndex)
                gazeJointStates[jointIndex] = new _GazeJointState(controller.gazeJoints[jointIndex]);
        }

        public void Apply(GazeController controller)
        {
            controller.Amplitude = amplitude;
            controller._CurrentGazeTarget = currentGazeTarget;
            for (int jointIndex = 0; jointIndex < controller.gazeJoints.Length; ++jointIndex)
                gazeJointStates[jointIndex].Apply(controller.gazeJoints[jointIndex]);
        }
    }

    // Runtime state of an eye gaze joint
    protected struct _GazeJointState
    {
        public Quaternion srcRot, trgRot, trgRotAlign, trgRotMR;
        public float distRotAlign, distRotMR, rotParamAlign, rotParamMR;
        public float maxVelocity, curVelocity, latency, latencyTime;
        public bool mrReached, trgReached;
        public float adjUpMR, adjDownMR, adjInMR, adjOutMR, curUpMR, curDownMR, curInMR, curOutMR;
        public float curAlign, curWeight;
        public Quaternion fixSrcRot, fixTrgRot, fixTrgRotAlign;
        public float fixDistRotAlign, fixRotParamAlign;

        public _GazeJointState(GazeJoint joint)
        {
            srcRot = joint.srcRot;
            trgRot = joint.trgRot;
            trgRotAlign = joint.trgRotAlign;
            trgRotMR = joint.trgRotMR;
            distRotAlign = joint.distRotAlign;
            distRotMR = joint.distRotMR;
            rotParamAlign = joint.rotParamAlign;
            rotParamMR = joint.rotParamMR;
            maxVelocity = joint.maxVelocity;
            curVelocity = joint.curVelocity;
            latency = joint.latency;
            latencyTime = joint.latencyTime;
            mrReached = joint.mrReached;
            trgReached = joint.trgReached;
            adjUpMR = joint.adjUpMR;
            adjDownMR = joint.adjDownMR;
            adjInMR = joint.adjInMR;
            adjOutMR = joint.adjOutMR;
            curUpMR = joint.curUpMR;
            curDownMR = joint.curDownMR;
            curInMR = joint.curInMR;
            curOutMR = joint.curOutMR;
            curAlign = joint.curAlign;
            curWeight = joint.curWeight;
            fixSrcRot = joint.fixSrcRot;
            fixTrgRot = joint.fixTrgRot;
            fixTrgRotAlign = joint.fixTrgRotAlign;
            fixDistRotAlign = joint.fixDistRotAlign;
            fixRotParamAlign = joint.fixRotParamAlign;
        }

        public void Apply(GazeJoint joint)
        {
            joint.srcRot = srcRot;
            joint.trgRot = trgRot;
            joint.trgRotAlign = trgRotAlign;
            joint.trgRotMR = trgRotMR;
            joint.distRotAlign = distRotAlign;
            joint.distRotMR = distRotMR;
            joint.rotParamAlign = rotParamAlign;
            joint.rotParamMR = rotParamMR;
            joint.maxVelocity = maxVelocity;
            joint.curVelocity = curVelocity;
            joint.latency = latency;
            joint.latencyTime = latencyTime;
            joint.mrReached = mrReached;
            joint.trgReached = trgReached;
            joint.adjUpMR = adjUpMR;
            joint.adjDownMR = adjDownMR;
            joint.adjInMR = adjInMR;
            joint.adjOutMR = adjOutMR;
            joint.curUpMR = curUpMR;
            joint.curDownMR = curDownMR;
            joint.curInMR = curInMR;
            joint.curOutMR = curOutMR;
            joint.curAlign = curAlign;
            joint.curWeight = curWeight;
            joint.fixSrcRot = fixSrcRot;
            joint.fixTrgRot = fixTrgRot;
            joint.fixTrgRotAlign = fixTrgRotAlign;
            joint.fixDistRotAlign = fixDistRotAlign;
            joint.fixRotParamAlign = fixRotParamAlign;
        }
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="model">Character model</param>
    /// <param name="animationClipName">Animation clip name</param>
    /// <param name="frameLength">Duration of the fixation on the target</param>
    /// <param name="target">Gaze target (null will generate a random gaze aversion)</param>
    /// <param name="headAlign">Head alignment</param>
    /// <param name="torsoAlign">Torso alignment</param>
    public EyeGazeInstance(GameObject model, string animationClipName,
        int frameLength = 30, GameObject target = null, float headAlign = 0f, float torsoAlign = 0f)
        : base(model, animationClipName, typeof(GazeController), frameLength)
    {
        Target = target;
        HeadAlign = headAlign;
        TorsoAlign = torsoAlign;

        // Bake only animation curves for eyes, head, and torso
        BakeMask.SetAll(false);
        foreach (var gazeJoint in GazeController.gazeJoints)
        {
            int boneIndex = ModelUtils.FindBoneIndex(model, gazeJoint.bone);
            int curveIndex = 3 + boneIndex * 4;
            BakeMask.Set(curveIndex++, true);
            BakeMask.Set(curveIndex++, true);
            BakeMask.Set(curveIndex++, true);
            BakeMask.Set(curveIndex, true);
        }
    }

    /// <summary>
    /// <see cref="AnimationInstance.Start"/>
    /// </summary>
    public override void Start()
    {
        base.Start();

        // Register handler for gaze controller state changes
        GazeController.StateChange += new StateChangeEvtH(GazeController_StateChange);

        // Initiate gaze shift to target
        if (GazeController.Head != null)
        {
            GazeController.Head.align = HeadAlign;
            //GazeController.Head.align = 0.3f;
            GazeController.Head.weight = 1f;
        }
        if (GazeController.Torso != null)
        {
            GazeController.Torso.align = TorsoAlign;
            //GazeController.Torso.align = 0.3f;
            GazeController.Torso.weight = 1f;
        }
        GazeController.GazeAt(Target);
        GazeController.movingTargetPositionOffset = _ComputeMovingTargetPositionOffset();
    }

    /// <summary>
    /// <see cref="AnimationInstance.Apply"/>
    /// </summary>
    public override void Apply(int frame, AnimationLayerMode layerMode)
    {
        if (IsBaking)
        {
            if (GazeController.StateId == (int)GazeState.Shifting)
            {
                if (!GazeController.fixGaze)
                {
                    // Make sure gaze remains fixated after gaze shift completion
                    GazeController.fixGaze = true;
                }
            }

            if (Target == null)
            {
                // This is gaze shift ahead, blend it out
                float t = Mathf.Clamp01(((float)frame) / FrameLength);
                float t2 = t * t;
                foreach (var gazeJoint in GazeController.gazeJoints)
                {
                    gazeJoint.curWeight = gazeJoint.weight * (1f + 2f * t2 * t - 3f * t2);
                }
            }
        }

        base.Apply(frame, layerMode);
    }

    /// <summary>
    /// <see cref="AnimationInstance.Finish"/>
    /// </summary>
    public override void Finish()
    {
        if (Target == null)
        {
            // Next gaze shift will have no previous target for VOR
            GazeController.InitVORNoTarget();
        }

        // Unregister handler for gaze controller state changes
        GazeController.StateChange -= GazeController_StateChange;

        base.Finish();
    }

    // Compute gaze shift parameters to account for anticipated body movement
    protected virtual Vector3 _ComputeMovingTargetPositionOffset()
    {
        // Store current model pose
        AnimationTimeline.Instance.StoreModelPose(Model.gameObject.name, AnimationClip.name + "Pose");

        // Store current gaze controller state
        var curState = new _GazeControllerState(GazeController);

        // Get base position at the current frame
        Vector3 currentBasePos = GazeController.gazeJoints[GazeController.gazeJoints.Length - 1].bone.position;

        // Apply the base animation at a time in near future
        var baseAnimationInstance = AnimationTimeline.Instance.GetLayer("BaseAnimation").Animations[0].Animation;
        int curFrame = AnimationTimeline.Instance.CurrentFrame;
        //float lookAheadTime = 0;
        float lookAheadTime = _ComputeEstGazeShiftTimeLength();
        int endFrame = curFrame + Mathf.RoundToInt(((float)LEAPCore.editFrameRate) * lookAheadTime); // look ahead to the estimated end of the current gaze shift
        baseAnimationInstance.Apply(endFrame, AnimationLayerMode.Override);

        // Get future base position at the current frame
        Vector3 futureBasePos = GazeController.gazeJoints[GazeController.gazeJoints.Length - 1].bone.position;
        
        // Reapply current model pose
        AnimationTimeline.Instance.ApplyModelPose(Model.gameObject.name, AnimationClip.name + "Pose");
        AnimationTimeline.Instance.RemoveModelPose(AnimationClip.name + "Pose");

        return currentBasePos - futureBasePos;
    }

    // Estimate the time duration of the current gaze shift
    protected virtual float _ComputeEstGazeShiftTimeLength()
    {
        float eyeRotTime = 0f;
        foreach (var eye in GazeController.eyes)
        {
            float edr = GazeJoint.DistanceToRotate(eye.bone.localRotation, eye._ComputeTargetRotation(Target.transform.position));
            eyeRotTime = Mathf.Max(eyeRotTime, eye.velocity > 0f ? edr / eye.velocity : 0f);
        }

        float hdr = GazeJoint.DistanceToRotate(GazeController.Head.bone.localRotation,
            GazeController.Head._ComputeTargetRotation(Target.transform.position));
        float OMR = Mathf.Min(GazeController.LEye.inMR + GazeController.LEye.outMR,
            GazeController.LEye.upMR + GazeController.LEye.downMR);
        float hdrmin = Mathf.Clamp(hdr - OMR, 0f, float.MaxValue);
        float hdra = (1f - HeadAlign) * hdrmin + HeadAlign * hdr;
        float headRotTime = GazeController.Head.velocity > 0f ? hdra / GazeController.Head.velocity : 0f;

        return Mathf.Max(headRotTime, eyeRotTime);
    }

    // Handler for gaze controller state changes
    protected virtual void GazeController_StateChange(AnimController sender, int  srcState, int trgState)
    {
    }
}

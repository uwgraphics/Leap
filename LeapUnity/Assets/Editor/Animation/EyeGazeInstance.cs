using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Snapshot of the runtime state of an eye gaze controller.
/// </summary>
public struct EyeGazeControllerState : IAnimationControllerState
{
    // Runtime state of an eye gaze joint
    public struct EyeGazeJointState
    {
        public Quaternion srcRot, trgRot, trgRotAlign, trgRotMR;
        public float distRotAlign, distRotMR, rotParamAlign, rotParamMR;
        public float maxVelocity, curVelocity, latency, latencyTime;
        public bool mrReached, trgReached;
        public float adjUpMR, adjDownMR, adjInMR, adjOutMR, curUpMR, curDownMR, curInMR, curOutMR;
        public float curAlign, curGazeWeight, curBaseWeight;
        public bool isVOR;
        public Quaternion fixSrcRot, fixTrgRot, fixTrgRotAlign;
        public float fixDistRotAlign, fixRotParamAlign;
        public Quaternion baseRot;

        public void Get(GazeJoint joint)
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
            curGazeWeight = joint.curGazeWeight;
            curBaseWeight = joint.curBaseWeight;
            isVOR = joint.isVOR;
            fixSrcRot = joint.fixSrcRot;
            fixTrgRot = joint.fixTrgRot;
            fixTrgRotAlign = joint.fixTrgRotAlign;
            fixDistRotAlign = joint.fixDistRotAlign;
            fixRotParamAlign = joint.fixRotParamAlign;
            baseRot = joint.baseRot;
        }

        public void Set(GazeJoint joint)
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
            joint.curGazeWeight = curGazeWeight;
            joint.curBaseWeight = curBaseWeight;
            joint.isVOR = isVOR;
            joint.fixSrcRot = fixSrcRot;
            joint.fixTrgRot = fixTrgRot;
            joint.fixTrgRotAlign = fixTrgRotAlign;
            joint.fixDistRotAlign = fixDistRotAlign;
            joint.fixRotParamAlign = fixRotParamAlign;
            joint.baseRot = baseRot;
        }
    }

    public float amplitude;
    public GameObject currentGazeTarget;
    public EyeGazeJointState[] eyeGazeJointStates;

    public void Get(AnimController controller)
    {
        var gazeController = controller as GazeController;
        amplitude = gazeController.Amplitude;
        currentGazeTarget = gazeController._CurrentGazeTarget;
        eyeGazeJointStates = new EyeGazeJointState[gazeController.gazeJoints.Length];
        for (int jointIndex = 0; jointIndex < gazeController.gazeJoints.Length; ++jointIndex)
        {
            eyeGazeJointStates[jointIndex] = new EyeGazeJointState();
            eyeGazeJointStates[jointIndex].Get(gazeController.gazeJoints[jointIndex]);
        }
    }

    public void Set(AnimController controller)
    {
        var gazeController = controller as GazeController;
        gazeController.Amplitude = amplitude;
        gazeController._CurrentGazeTarget = currentGazeTarget;

        if (eyeGazeJointStates != null && eyeGazeJointStates.Length == gazeController.gazeJoints.Length)
        {
            for (int jointIndex = 0; jointIndex < gazeController.gazeJoints.Length; ++jointIndex)
                eyeGazeJointStates[jointIndex].Set(gazeController.gazeJoints[jointIndex]);
        }
    }
}

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

    /// <summary>
    /// Time (relative to the start of the gaze instance) when the gaze shift
    /// is expected to finish and the fixation start.
    /// </summary>
    public virtual float FixationStartTime
    {
        get;
        protected set;
    }

    /// <summary>
    /// Frame (relative to the start of the gaze instance) when the gaze shift
    /// is expected to finish and the fixation start.
    /// </summary>
    public virtual float FixationStartFrame
    {
        get { return (int)(FixationStartTime * LEAPCore.editFrameRate + 0.5f); }
    }

    /// <summary>
    /// If true, this eye gaze instance is an edit. If false, this eye gaze instance
    /// represents an unmodified gaze shift from the original animation.
    /// </summary>
    public virtual bool IsEdit
    {
        get;
        protected set;
    }

    protected bool _gazeShiftStarted = false;

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
        int frameLength = 30, GameObject target = null, float headAlign = 0f, float torsoAlign = 0f,
        int fixationStartFrame = -1, bool isEdit = true)
        : base(model, animationClipName, typeof(GazeController), frameLength)
    {
        Target = target;
        HeadAlign = headAlign;
        TorsoAlign = torsoAlign;
        SetFixationStartFrame(fixationStartFrame);
        IsEdit = isEdit;

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
    /// Set the frame (relative to the start of the gaze instance) when
    /// the gaze shift is expected to end and the fixation start.
    /// </summary>
    /// <param name="frame"></param>
    public virtual void SetFixationStartFrame(int frame)
    {
        FixationStartTime = ((float)frame) / LEAPCore.editFrameRate;
    }

    /// <summary>
    /// <see cref="AnimationInstance.Start"/>
    /// </summary>
    public override void Start()
    {
        base.Start();

        // Register handler for gaze controller state changes
        GazeController.StateChange += new StateChangeEvtH(GazeController_StateChange);

        //if (Target != null)
        {
            // Initiate gaze shift to target
            if (GazeController.Head != null)
            {
                GazeController.Head.align = Mathf.Clamp01(HeadAlign);
                GazeController.Head.baseWeight = 1f;
            }
            if (GazeController.Torso != null)
            {
                GazeController.Torso.align = Mathf.Clamp01(TorsoAlign);
                GazeController.Torso.baseWeight = 1f;
            }
            GazeController.GazeAt(Target);
            GazeController.movingTargetPositionOffset = _ComputeMovingTargetPositionOffset();
        }
        //else
        {
            // Gaze back in the direction encoded in the original motion
            // TODO: compute the target position and alignment values for the gaze shift
        }
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
                // This is a gaze shift ahead, blend it out
                float t = Mathf.Clamp01(((float)frame) / FrameLength);
                float t2 = t * t;
                foreach (var gazeJoint in GazeController.gazeJoints)
                {
                    gazeJoint.curBaseWeight = gazeJoint.baseWeight * (1f + 2f * t2 * t - 3f * t2);
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
        // How far ahead do we need to look to anticipate the target?
        float lookAheadTime = EyeGazeEditor.ComputeEstGazeShiftTimeLength(this);

        // Store current model pose
        AnimationTimeline.Instance.StoreModelPose(Model.gameObject.name, AnimationClip.name + "Pose");

        // Get base position at the current frame
        Vector3 currentBasePos = GazeController.gazeJoints[GazeController.gazeJoints.Length - 1].bone.position;

        // Apply the base animation at a time in near future
        var baseAnimationInstance =
            AnimationTimeline.Instance.GetLayer("BaseAnimation").Animations.FirstOrDefault(inst => inst.Animation.Model.gameObject == Model.gameObject);
        int curFrame = AnimationTimeline.Instance.CurrentFrame;
        int endFrame = curFrame + Mathf.RoundToInt(((float)LEAPCore.editFrameRate) * lookAheadTime); // look ahead to the estimated end of the current gaze shift
        baseAnimationInstance.Animation.Apply(endFrame, AnimationLayerMode.Override);

        // Get future base position at the current frame
        Vector3 futureBasePos = GazeController.gazeJoints[GazeController.gazeJoints.Length - 1].bone.position;
        
        // Reapply current model pose
        //baseAnimationInstance.Animation.Apply(AnimationTimeline.Instance.CurrentFrame - baseAnimationInstance.StartFrame, AnimationLayerMode.Override);
        AnimationTimeline.Instance.ApplyModelPose(Model.gameObject.name, AnimationClip.name + "Pose");
        AnimationTimeline.Instance.RemoveModelPose(Model.gameObject.name, AnimationClip.name + "Pose");

        return currentBasePos - futureBasePos;
    }

    // Handler for gaze controller state changes
    protected virtual void GazeController_StateChange(AnimController sender, int  srcState, int trgState)
    {
    }

    /// <summary>
    /// <see cref="AnimationControllerState._CreateControllerState"/>
    /// </summary>
    protected override IAnimationControllerState _CreateControllerState()
    {
        return new EyeGazeControllerState();
    }
}

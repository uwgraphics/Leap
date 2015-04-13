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
        public float fixDistRotAlign, fixRotParamAlign;
        public Quaternion expressiveRot, fixExpressiveRot;

        public void Get(GazeJoint joint)
        {
            gazeJointType = joint.type;
            velocity = joint.velocity;
            upMR = joint.upMR;
            downMR = joint.downMR;
            inMR = joint.inMR;
            outMR = joint.outMR;
            align = joint.align;
            latency = joint.latency;
            rot = joint.bone.localRotation;
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
            isVOR = joint.isVOR;
            fixSrcRot = joint.fixSrcRot;
            fixTrgRot = joint.fixTrgRot;
            fixTrgRotAlign = joint.fixTrgRotAlign;
            fixDistRotAlign = joint.fixDistRotAlign;
            fixRotParamAlign = joint.fixRotParamAlign;
            expressiveRot = joint.expressiveRot;
            fixExpressiveRot = joint.fixExpressiveRot;
        }

        public void Set(GazeJoint joint)
        {
            joint.type = gazeJointType;
            joint.velocity = velocity;
            joint.upMR = upMR;
            joint.downMR = downMR;
            joint.inMR = inMR;
            joint.outMR = outMR;
            joint.align = align;
            latency = joint.latency;
            joint.bone.localRotation = rot;
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
            joint.isVOR = isVOR;
            joint.fixSrcRot = fixSrcRot;
            joint.fixTrgRot = fixTrgRot;
            joint.fixTrgRotAlign = fixTrgRotAlign;
            joint.fixDistRotAlign = fixDistRotAlign;
            joint.fixRotParamAlign = fixRotParamAlign;
            joint.expressiveRot = expressiveRot;
            joint.fixExpressiveRot = fixExpressiveRot;
        }
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
    public EyeGazeJointState[] eyeGazeJointStates;

    public void Get(AnimController controller)
    {
        var gazeController = controller as GazeController;
        stateId = gazeController.StateId;
        gazeTarget = gazeController.gazeTarget;
        doGazeShift = gazeController.doGazeShift;
        stopGazeShift = gazeController.stopGazeShift;
        fixGaze = gazeController.fixGaze;
        useTorso = gazeController.useTorso;
        predictability = gazeController.predictability;
        movingTargetPositionOffset = gazeController.movingTargetPositionOffset;
        stylizeGaze = gazeController.stylizeGaze;
        quickness = gazeController.quickness;
        eyeSize = gazeController.eyeSize;
        eyeTorque = gazeController.eyeTorque;
        eyeAlign = gazeController.eyeAlign;
        enableED = gazeController.enableED;
        enableAEM = gazeController.enableAEM;
        enableEAH = gazeController.enableEAH;
        maxCrossEyedness = gazeController.maxCrossEyedness;
        removeRoll = gazeController.removeRoll;
        amplitude = gazeController.Amplitude;
        currentGazeTarget = gazeController.CurrentGazeTarget;
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
        gazeController._GetFSM()._SetState(stateId);
        gazeController.gazeTarget = gazeTarget;
        gazeController.doGazeShift = doGazeShift;
        gazeController.stopGazeShift = stopGazeShift;
        gazeController.fixGaze = fixGaze;
        gazeController.useTorso = useTorso;
        gazeController.predictability = predictability;
        gazeController.movingTargetPositionOffset = movingTargetPositionOffset;
        gazeController.stylizeGaze = stylizeGaze;
        gazeController.quickness = quickness;
        gazeController.eyeSize = eyeSize;
        gazeController.eyeTorque = eyeTorque;
        gazeController.eyeAlign = eyeAlign;
        gazeController.enableED = enableED;
        gazeController.enableAEM = enableAEM;
        gazeController.enableEAH = enableEAH;
        gazeController.maxCrossEyedness = maxCrossEyedness;
        gazeController.removeRoll = removeRoll;
        gazeController.Amplitude = amplitude;
        gazeController._SetCurrentGazeTarget(currentGazeTarget);

        if (eyeGazeJointStates != null && eyeGazeJointStates.Length == gazeController.gazeJoints.Length)
        {
            for (int jointIndex = 0; jointIndex < gazeController.gazeJoints.Length; ++jointIndex)
                eyeGazeJointStates[jointIndex].Set(gazeController.gazeJoints[jointIndex]);
        }
    }

    /// <summary>
    /// Get initial/zero state for the specified gaze controller
    /// </summary>
    /// <param name="gazeController"></param>
    public static EyeGazeControllerState GetInitState(GazeController gazeController)
    {
        EyeGazeControllerState state = new EyeGazeControllerState();
        state.Get(gazeController);

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

        for (int gazeJointIndex = 0; gazeJointIndex < gazeController.gazeJoints.Length; ++gazeJointIndex)
        {
            EyeGazeJointState gazeJointState = state.eyeGazeJointStates[gazeJointIndex];

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
            gazeJointState.fixTrgRot = Quaternion.identity;
            gazeJointState.fixTrgRotAlign = Quaternion.identity;
            gazeJointState.fixDistRotAlign = 0f;
            gazeJointState.fixRotParamAlign = 0f;
            gazeJointState.expressiveRot = Quaternion.identity;
            gazeJointState.fixExpressiveRot = Quaternion.identity;

            state.eyeGazeJointStates[gazeJointIndex] = gazeJointState;
        }

        return state;
    }
}

/// <summary>
/// Struct that specifies an expressive component of an eye gaze animation,
/// consisting of an animation clip with an expressive displacement map
/// and rotational amplitude of the gaze shift.
/// </summary>
public struct ExpressiveEyeGazeAnimation
{
    public AnimationClip clip;
    public Vector3 gazeShiftRotation;

    public ExpressiveEyeGazeAnimation(AnimationClip clip, Vector3 gazeShiftRotation)
    {
        this.clip = clip;
        this.gazeShiftRotation = gazeShiftRotation;
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
    public virtual int FixationStartFrame
    {
        get { return (int)(FixationStartTime * LEAPCore.editFrameRate + 0.5f); }
    }

    /// <summary>
    /// If true, the gaze controller will recruit the body when shifting the gaze
    /// towards the target; otherwise, only the eyes and head will move.
    /// </summary>
    public virtual bool TurnBody
    {
        get;
        set;
    }

    /// <summary>
    /// If false, this eye gaze instance is an edit; otherwise,
    /// represents a gaze shift from the base animation.
    /// </summary>
    public virtual bool IsBase
    {
        get;
        protected set;
    }

    /// <summary>
    /// Position towards which the character should gaze in order to gaze straight ahead.
    /// </summary>
    /// <remarks>This property is only used is gaze instances where Target is set to null</remarks>
    public virtual Vector3 AheadTargetPosition
    {
        get;
        set;
    }

    /// <summary>
    /// Expressive gaze animations for the current gaze instance.
    /// </summary>
    public virtual ExpressiveEyeGazeAnimation[] ExpressiveGazeAnimations
    {
        get;
        protected set;
    }

    /// <summary>
    /// Weight with which expressive gaze animations are applied to the current
    /// gaze instance.
    /// </summary>
    public virtual float ExpressiveGazeWeight
    {
        get;
        set;
    }

    protected bool _gazeShiftStarted = false;
    protected int _curFixationStartFrame = -1;
    protected int _lastAppliedFrame = -1;

    protected int _baseStartFrame = 0;
    protected int _baseFixationStartFrame = 0;
    protected int _baseFrameLength = 0;

    protected Vector3[] _expressiveGazeAnimationRotations = null;
    protected float[] _expressiveGazeAnimationWeights = null;
    protected Quaternion[] _baseGazeRotations = null;
    

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="model">Character model</param>
    /// <param name="animationClipName">Animation clip name</param>
    /// <param name="frameLength">Duration of the fixation on the target</param>
    /// <param name="target">Gaze target (null will generate a random gaze aversion)</param>
    /// <param name="headAlign">Head alignment</param>
    /// <param name="torsoAlign">Torso alignment</param>
    /// <param name="fixationStartFrame">Frame (relative to the start of the gaze instance) when the gaze shift
    /// is expected to finish and the fixation start; only relevant when the gaze instance is not a novel edit</param>
    /// <param name="turnBody">If true, the gaze controller will recruit the body when shifting the gaze
    /// towards the target; otherwise, only the eyes and head will move</param>
    /// <param name="isBase">If false, this eye gaze instance is an edit; otherwise, it
    /// represents a gaze shift from the base animation.</param>
    /// <param name="baseStartFrame">Start frame of the eye gaze instance starts in the base animation;
    /// only relevant when the gaze instance is not a novel edit</param>
    public EyeGazeInstance(GameObject model, string animationClipName,
        int frameLength = 30, GameObject target = null, float headAlign = 0f, float torsoAlign = 0f,
        int fixationStartFrame = -1, bool turnBody = true, bool isBase = false, int baseStartFrame = 0)
        : base(model, animationClipName, typeof(GazeController), frameLength)
    {
        Target = target;
        HeadAlign = headAlign;
        TorsoAlign = torsoAlign;
        SetFixationStartFrame(fixationStartFrame);
        TurnBody = turnBody;
        IsBase = isBase;
        ExpressiveGazeAnimations = null;
        ExpressiveGazeWeight = 1f;

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

        if (isBase)
        {
            // Initialize base gaze shift timings
            _baseStartFrame = baseStartFrame;
            _baseFixationStartFrame = fixationStartFrame;
            _baseFrameLength = frameLength;
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
    /// Set expressive gaze animation clips for this eye gaze instance.
    /// </summary>
    /// <param name="clips">Expressive gaze animations (one for each gaze joint)</param>
    public virtual void SetExpressiveGazeAnimations(ExpressiveEyeGazeAnimation[] expressiveGazeAnimations)
    {
        int numClips = GazeController.gazeJoints.Length - GazeController.eyes.Length;
        if (expressiveGazeAnimations.Length != numClips)
        {
            Debug.LogError("Number of expressive gaze animation clips does not match the number of gaze joints");
            return;
        }

        ExpressiveGazeAnimations = expressiveGazeAnimations;
        _expressiveGazeAnimationRotations = new Vector3[ExpressiveGazeAnimations.Length];
        _expressiveGazeAnimationWeights = new float[ExpressiveGazeAnimations.Length];
        _baseGazeRotations = new Quaternion[ExpressiveGazeAnimations.Length];
    }

    /// <summary>
    /// <see cref="AnimationInstance.Start"/>
    /// </summary>
    public override void Start()
    {
        base.Start();

        // Register handler for gaze controller state changes
        GazeController.StateChange += new StateChangeEvtH(GazeController_StateChange);

        // Initialize state
        _curFixationStartFrame = -1;

        // Initiate gaze shift to target
        GazeController.Head.align = Mathf.Clamp01(HeadAlign);
        if (GazeController.Torso != null)
        {
            GazeController.Torso.align = Mathf.Clamp01(TorsoAlign);
        }
        GazeController.useTorso = TurnBody;
        if (Target != null)
            GazeController.GazeAt(Target);
        else
            GazeController.GazeAt(AheadTargetPosition);
        _InitGazeParameters();
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

                if (frame >= FrameLength - 1)
                {
                    // We have reached the end of the current gaze instance, make sure
                    // the gaze shift terminates on this update
                    GazeController.StopGaze();
                }
            }

            if (Target == null)
            {
                // This is a gaze shift ahead, blend it out
                int numFrames = Mathf.Min(FrameLength, Mathf.RoundToInt(LEAPCore.editFrameRate * LEAPCore.maxEyeGazeGapLength));
                float t = numFrames > 1 ? Mathf.Clamp01(((float)frame) / (numFrames - 1)) : 0f;
                t = Mathf.Clamp01(t);
                float t2 = t * t;
                Weight = 1f + 2f * t2 * t - 3f * t2;
            }
        }

        _ApplyExpressive(frame);
        base.Apply(frame, layerMode);

        /*if (IsBaking || frame != _lastAppliedFrame)
            _LogGazeControllerState();*/

        _lastAppliedFrame = frame;
    }

    /// <summary>
    /// <see cref="AnimationInstance.Finish"/>
    /// </summary>
    public override void Finish()
    {
        // Unregister handler for gaze controller state changes
        GazeController.StateChange -= GazeController_StateChange;

        base.Finish();
    }

    // Compute and apply expressive rotational displacements to gaze joints
    protected virtual void _ApplyExpressive(int frame)
    {
        // Reset expressive rotations
        for (int gazeJointIndex = 0; gazeJointIndex <= GazeController.LastGazeJointIndex; ++gazeJointIndex)
        {
            var gazeJoint = GazeController.gazeJoints[gazeJointIndex];
            gazeJoint.expressiveRot = Quaternion.identity;
            if (!GazeController.doGazeShift && (GazeController.StateId != (int)GazeState.Shifting || !gazeJoint.isVOR))
                // Gaze joints that are still VOR-ing towards previous gaze target do not get their expressive rotations reset,
                // but all others do
                gazeJoint.fixExpressiveRot = Quaternion.identity;
        }

        if (ExpressiveGazeAnimations == null)
        {
            // Expressive gaze animation not defined for this gaze instance
            return;
        }

        // Store gaze joint rotations from the base animation
        for (int gazeJointIndex = GazeController.eyes.Length; gazeJointIndex <= GazeController.LastGazeJointIndex; ++gazeJointIndex)
        {
            var gazeJoint = GazeController.gazeJoints[gazeJointIndex];
            _baseGazeRotations[gazeJointIndex - GazeController.eyes.Length] = gazeJoint.bone.localRotation;
        }

        // Add the expressive gaze animation
        if (GazeController.StateId == (int)GazeState.Shifting || GazeController.doGazeShift)
        {
            // Apply expressive gaze during the gaze shift

            // Compute the timing and weight of the expressive gaze animation clip for each gaze joint
            for (int gazeJointIndex = GazeController.eyes.Length; gazeJointIndex <= GazeController.LastGazeJointIndex; ++gazeJointIndex)
            {
                var gazeJoint = GazeController.gazeJoints[gazeJointIndex];
                var exprGazeAnim = ExpressiveGazeAnimations[gazeJointIndex - GazeController.eyes.Length];

                // Compute time and weight at which to apply the expressive gaze animation
                float rotParamAlign = !GazeController.doGazeShift ? gazeJoint.rotParamAlign : 0f;
                float exprTime = (((float)_baseFixationStartFrame) / LEAPCore.editFrameRate) * rotParamAlign;
                float exprWeight = Mathf.Clamp01(ExpressiveGazeWeight *
                    _expressiveGazeAnimationWeights[gazeJointIndex - GazeController.eyes.Length]);

                Animation[exprGazeAnim.clip.name].time = exprTime;
                Animation[exprGazeAnim.clip.name].weight = exprWeight;
                Animation[exprGazeAnim.clip.name].blendMode = AnimationBlendMode.Additive;
                Animation[exprGazeAnim.clip.name].enabled = true;
            }

            // Apply expressive gaze animations
            Animation.Sample();

            // Disable expressive gaze animations and extract rotational displacements
            for (int gazeJointIndex = GazeController.eyes.Length; gazeJointIndex <= GazeController.LastGazeJointIndex; ++gazeJointIndex)
            {
                var gazeJoint = GazeController.gazeJoints[gazeJointIndex];
                var exprGazeAnim = ExpressiveGazeAnimations[gazeJointIndex - GazeController.eyes.Length];
                Animation[exprGazeAnim.clip.name].enabled = false;

                Quaternion exprRot = Quaternion.Inverse(_baseGazeRotations[gazeJointIndex - GazeController.eyes.Length]) * gazeJoint.bone.localRotation;
                gazeJoint.expressiveRot = exprRot;
            }
        }
        else
        {
            // Apply the expressive animation during gaze fixation

            if (_curFixationStartFrame < 0)
                // We just started the gaze fixation, so remember the current frame
                _curFixationStartFrame = frame;

            // Compute time at which to apply the expressive gaze animations
            int fixationFrameLength = this.FrameLength - _curFixationStartFrame - 1;
            int exprFrame = fixationFrameLength > 0f ?
                Mathf.RoundToInt((float)_baseFixationStartFrame + (((float)(frame - _curFixationStartFrame)) / fixationFrameLength) *
                (_baseFrameLength - _baseFixationStartFrame - 1)) :
                _baseFixationStartFrame;
            float exprTime = ((float)exprFrame) / LEAPCore.editFrameRate;
            /*float exprWeight = _baseFrameLength - 1 - _baseFixationStartFrame > 0 ?
                ((float)(FrameLength - 1 - _curFixationStartFrame)) / (_baseFrameLength - 1 - _baseFixationStartFrame) :
                0f;*/

            // Compute the weight of the expressive gaze animation clip for each gaze joint
            for (int gazeJointIndex = GazeController.eyes.Length; gazeJointIndex <= GazeController.LastGazeJointIndex; ++gazeJointIndex)
            {
                var exprGazeAnim = ExpressiveGazeAnimations[gazeJointIndex - GazeController.eyes.Length];

                float exprWeight = Mathf.Clamp01(ExpressiveGazeWeight *
                    _expressiveGazeAnimationWeights[gazeJointIndex - GazeController.eyes.Length]);

                Animation[exprGazeAnim.clip.name].time = exprTime;
                Animation[exprGazeAnim.clip.name].weight = exprWeight;
                Animation[exprGazeAnim.clip.name].blendMode = AnimationBlendMode.Additive;
                Animation[exprGazeAnim.clip.name].enabled = true;
            }

            // Apply expressive gaze animations
            Animation.Sample();

            // Disable expressive gaze animations and extract rotational displacements
            for (int gazeJointIndex = GazeController.eyes.Length; gazeJointIndex <= GazeController.LastGazeJointIndex; ++gazeJointIndex)
            {
                var gazeJoint = GazeController.gazeJoints[gazeJointIndex];
                var exprGazeAnim = ExpressiveGazeAnimations[gazeJointIndex - GazeController.eyes.Length];
                Animation[exprGazeAnim.clip.name].enabled = false;

                Quaternion exprRot = Quaternion.Inverse(_baseGazeRotations[gazeJointIndex - GazeController.eyes.Length]) * gazeJoint.bone.localRotation;
                gazeJoint.fixExpressiveRot = exprRot;
            }
        }

        // Reapply gaze joint rotations from the base animation
        for (int gazeJointIndex = GazeController.eyes.Length; gazeJointIndex <= GazeController.LastGazeJointIndex; ++gazeJointIndex)
        {
            var gazeJoint = GazeController.gazeJoints[gazeJointIndex];
            gazeJoint.bone.localRotation = _baseGazeRotations[gazeJointIndex - GazeController.eyes.Length];
        }
    }

    // Compute gaze shift parameters to account for anticipated body movement
    protected virtual void _InitGazeParameters()
    {
        // How far ahead do we need to look to anticipate the target?
        EyeGazeControllerState state = EyeGazeEditor.GetInitControllerForEyeGazeInstance(this);
        var baseAnimationInstance = AnimationTimeline.Instance.GetLayer("BaseAnimation").Animations.FirstOrDefault(
            inst => inst.Animation.Model == Model);
        GazeController.movingTargetPositionOffset = EyeGazeEditor.ComputeMovingTargetPositionOffset(
            AnimationTimeline.Instance, baseAnimationInstance.InstanceId, this, AnimationTimeline.Instance.CurrentFrame);
        // TODO: base animation should be specified as a parameter of the eye gaze instance!

        if (ExpressiveGazeAnimations != null)
        {
            // Initialize expressive gaze
            _InitExpressiveGazeRotations(state);
            _InitExpressiveGazeWeights();
        }
    }

    // Set rotation magnitude and direction for each gaze joint,
    // which will be used to scale the expressive gaze displacement
    protected virtual void _InitExpressiveGazeRotations(EyeGazeControllerState state)
    {
        for (int gazeJointIndex = GazeController.eyes.Length; gazeJointIndex < GazeController.gazeJoints.Length; ++gazeJointIndex)
        {
            Quaternion qs = state.eyeGazeJointStates[gazeJointIndex].srcRot;
            Quaternion qf = state.eyeGazeJointStates[gazeJointIndex].trgRotAlign;
            Vector3 rot = QuaternionUtil.Log(Quaternion.Inverse(qs) * qf);

            _expressiveGazeAnimationRotations[gazeJointIndex - GazeController.eyes.Length] = rot;
        }
    }

    // Compute blend weights for expressive gaze animations
    protected virtual void _InitExpressiveGazeWeights()
    {
        float totalDistRot = ExpressiveGazeAnimations.Sum(anim => anim.gazeShiftRotation.magnitude) * Mathf.Rad2Deg;

        for (int gazeJointIndex = GazeController.eyes.Length; gazeJointIndex <= GazeController.LastGazeJointIndex; ++gazeJointIndex)
        {
            var gazeJoint = GazeController.gazeJoints[gazeJointIndex];
            Vector3 exprGazeRot = _expressiveGazeAnimationRotations[gazeJointIndex - GazeController.eyes.Length];
            var exprGazeAnim = ExpressiveGazeAnimations[gazeJointIndex - GazeController.eyes.Length];

            float distRotWeight = (exprGazeRot.magnitude * Mathf.Rad2Deg) / totalDistRot;
            /*float phi, phib;
            Vector3 v, vb;
            QuaternionUtil.Exp(exprGazeAnim.gazeShiftRotation).ToAngleAxis(out phib, out vb);
            QuaternionUtil.Exp(exprGazeRot).ToAngleAxis(out phi, out v);
            float rotDirWeight = Mathf.Abs(Mathf.Cos(Vector3.Angle(vb.normalized, v.normalized) * Mathf.Deg2Rad));*/
            //
            Quaternion rot = gazeJoint.bone.localRotation;
            gazeJoint.bone.localRotation = gazeJoint.srcRot;
            Vector3 vs = gazeJoint.Direction.normalized;
            gazeJoint.bone.localRotation = gazeJoint.srcRot * QuaternionUtil.Exp(exprGazeRot);
            Vector3 vf = gazeJoint.Direction.normalized;
            Vector3 v = (vf - vs).normalized;
            gazeJoint.bone.localRotation = gazeJoint.srcRot * QuaternionUtil.Exp(exprGazeAnim.gazeShiftRotation);
            vf = gazeJoint.Direction.normalized;
            Vector3 vb = (vf - vs).normalized;
            float angle = Vector3.Angle(v, vb);
            float rotDirWeight = Mathf.Abs(Mathf.Cos(angle * Mathf.Deg2Rad));
            gazeJoint.bone.localRotation = rot;
            //

            _expressiveGazeAnimationWeights[gazeJointIndex - GazeController.eyes.Length] =
                Mathf.Clamp01(distRotWeight * rotDirWeight);
        }
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

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
        get { return _headAlign; }
        set { _headAlign = Mathf.Clamp01(value); }
    }

    /// <summary>
    /// How much the upper body should align with the target.
    /// </summary>
    public virtual float TorsoAlign
    {
        get { return _torsoAlign; }
        set { _torsoAlign = Mathf.Clamp01(value); }
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
        get { return Mathf.RoundToInt(FixationStartTime * LEAPCore.editFrameRate); }
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

    protected float _headAlign, _torsoAlign;

    protected bool _gazeShiftStarted = false;
    protected int _curFixationStartFrame = -1;
    protected int _lastAppliedFrame = -1;

    protected int _baseStartFrame = 0;
    protected int _baseFixationStartFrame = 0;
    protected int _baseFrameLength = 0;

    protected Vector3[] _expressiveGazeAnimationRotations = null;
    protected float[] _expressiveGazeAnimationWeights = null;
    protected Quaternion[] _baseGazeRotations = null;

    protected List<IAnimControllerState> _bakedGazeControllerStates;
    protected List<IAnimControllerState> _bakedGazeControllerStatesIK;
    

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
        // Also bake blend shape weights for eye blinks
        for (int curveIndex = 3 + ModelController.NumberOfBones * 4; curveIndex < BakeMask.Length; ++curveIndex)
        {
            BakeMask.Set(curveIndex, true);
            // TODO: bake only eyeblink blend shapes, not all the blend shapes
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
    /// <see cref="AnimationControllerInstance.SetFrameLength"/>
    /// </summary>
    public override void SetFrameLength(int frameLength)
    {
        base.SetFrameLength(frameLength);

        if (FixationStartFrame >= FrameLength)
            SetFixationStartFrame(FrameLength - 1);
    }

    /// <summary>
    /// Set the frame (relative to the start of the gaze instance) when
    /// the gaze shift is expected to end and the fixation start.
    /// </summary>
    /// <param name="frame"></param>
    public virtual void SetFixationStartFrame(int frame)
    {
        FixationStartTime = ((float)frame) / LEAPCore.editFrameRate;
        if (FixationStartTime >= TimeLength)
            FixationStartTime = TimeLength;
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

        if (IsBaking)
        {
            // Initialize list of baked gaze controller states
            if (LEAPCore.useGazeIK)
            {
                _bakedGazeControllerStatesIK = new List<IAnimControllerState>(FrameLength);
                for (int frameIndex = 0; frameIndex < FrameLength; ++frameIndex)
                    _bakedGazeControllerStatesIK.Add(Controller.GetRuntimeState());
            }
            else
            {
                _bakedGazeControllerStates = new List<IAnimControllerState>(FrameLength);
                for (int frameIndex = 0; frameIndex < FrameLength; ++frameIndex)
                    _bakedGazeControllerStates.Add(Controller.GetRuntimeState());
            }
        }

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

            // Compute gaze weight
            float gazeWeight = 1f;
            if (Target == null)
            {
                // This is a gaze shift ahead, blend it out
                int numFrames = Mathf.Min(FrameLength, Mathf.RoundToInt(LEAPCore.editFrameRate * LEAPCore.gazeAheadBlendTime));
                float t = numFrames > 1 ? Mathf.Clamp01(((float)frame) / (numFrames - 1)) : 0f;
                float t2 = t * t;
                gazeWeight = 1f + 2f * t2 * t - 3f * t2;
            }
            
            // Apply gaze weight
            if (GazeController.StateId == (int)GazeState.Shifting)
                GazeController.weight = gazeWeight;
            else if (frame > 0)
                // TODO: careful here - we only compute fixation weight for the fixation in the *current* gaze instance
                // and not the still-ongoing fixation from the previous gaze instance
                GazeController.fixWeight = gazeWeight;

            _ApplyExpressive(frame);
        }

        base._Apply(frame, layerMode);
        // TODO: this is a hack to prevent OMR violation
        foreach (var eye in GazeController.eyes)
        {
            if (eye.bone.localEulerAngles.x > eye.curDownMR)
            {
                eye.bone.localEulerAngles = new Vector3(
                    eye.curDownMR,
                    eye.bone.localEulerAngles.y,
                    eye.bone.localEulerAngles.z);
            }
            else if (-eye.bone.localEulerAngles.x > eye.curUpMR)
            {
                eye.bone.localEulerAngles = new Vector3(
                    -eye.curUpMR,
                    eye.bone.localEulerAngles.y,
                    eye.bone.localEulerAngles.z);
            }
        }
        //
        _lastAppliedFrame = frame;
        _ApplyBake(frame, layerMode);

        List<IAnimControllerState> bakedControllerStates = LEAPCore.useGazeIK ?
            _bakedGazeControllerStatesIK : _bakedGazeControllerStates;
        if (IsBaking)
        {
            // Bake the current controller state
            bakedControllerStates[frame] = Controller.GetRuntimeState();
        }
        else
        {
            if (bakedControllerStates != null && frame < bakedControllerStates.Count)
            {
                // Apply the baked controller state
                Controller.SetRuntimeState(bakedControllerStates[frame]);

                // Also apply gaze joint rotations
                for (int gazeJointIndex = 0; gazeJointIndex < GazeController.gazeJoints.Length; ++gazeJointIndex)
                {
                    GazeController.gazeJoints[gazeJointIndex].bone.localRotation =
                        ((GazeControllerState)bakedControllerStates[frame]).gazeJointStates[gazeJointIndex].rot;
                }
            }
        }
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
            GazeController.expressiveWeight = 0f;
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
        GazeControllerState state = EyeGazeEditor.GetInitControllerForEyeGazeInstance(this);
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
    protected virtual void _InitExpressiveGazeRotations(GazeControllerState state)
    {
        for (int gazeJointIndex = GazeController.eyes.Length; gazeJointIndex < GazeController.gazeJoints.Length; ++gazeJointIndex)
        {
            Quaternion qs = state.gazeJointStates[gazeJointIndex].srcRot;
            Quaternion qf = state.gazeJointStates[gazeJointIndex].trgRotAlign;
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
}

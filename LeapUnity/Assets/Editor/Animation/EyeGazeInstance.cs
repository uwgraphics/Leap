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
    /// <see cref="AnimationInstance.FrameLength"/>
    /// </summary>
    public override int FrameLength
    {
        get
        {
            return base.FrameLength;
        }
        set
        {
            base.FrameLength = value;

            if (FixationStartFrame >= value)
                _SetFixationStartFrame(value - 1);
        }
    }

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
    /// Position towards which the character should gaze in order to gaze straight ahead.
    /// </summary>
    /// <remarks>This property is only used is gaze instances where Target is set to null</remarks>
    public virtual Vector3 AheadTargetPosition
    {
        get;
        set;
    }

    /// <summary>
    /// Base animation clip.
    /// </summary>
    public virtual AnimationClip BaseAnimationClip
    {
        get { return _baseInstance.AnimationClip; }
    }

    /// <summary>
    /// Gaze target animation clip.
    /// </summary>
    public virtual AnimationClip TargetAnimationClip
    {
        get
        {
            return _targetInstance != null ? _targetInstance.AnimationClip : null;
        }
    }

    protected float _headAlign, _torsoAlign;
    protected AnimationClipInstance _baseInstance, _targetInstance;

    // TODO: not happy about having elements of runtime state in this class
    protected bool _isActive = false;
    protected bool _blendIn, _blendOut, _zeroWeight;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="name">Animation instance name</param>
    /// <param name="model">Character model</param>
    /// <param name="frameLength">Duration of the fixation on the target</param>
    /// <param name="target">Gaze target (null will look ahead)</param>
    /// <param name="headAlign">Head alignment</param>
    /// <param name="torsoAlign">Torso alignment</param>
    /// <param name="fixationStartFrame">Frame (relative to the start of the gaze instance) when the gaze shift
    /// is expected to finish and the fixation start; only relevant when the gaze instance is not a novel edit</param>
    /// <param name="turnBody">If true, the gaze controller will recruit the body when shifting the gaze
    /// towards the target; otherwise, only the eyes and head will move</param>
    /// <param name="isBase">If false, this eye gaze instance is an edit; otherwise, it
    /// represents a gaze shift from the base animation.</param>
    /// <param name="baseStartFrame">Start frame of the eye gaze instance in the base animation;
    /// only relevant when the gaze instance is not a novel edit</param>
    public EyeGazeInstance(string name, GameObject model, int frameLength = 30, int fixationStartFrame = -1,
        GameObject target = null, float headAlign = 0f, float torsoAlign = 0f, bool turnBody = true,
        AnimationClip baseAnimationClip = null, AnimationClip targetAnimationClip = null)
        : base(name, model, typeof(GazeController), frameLength)
    {
        _SetFixationStartFrame(fixationStartFrame);
        Target = target;
        HeadAlign = headAlign;
        TorsoAlign = torsoAlign;
        TurnBody = turnBody;

        _baseInstance = baseAnimationClip != null ?
            new AnimationClipInstance(baseAnimationClip.name, Model, false, false, false) : null;
        _targetInstance = targetAnimationClip != null && Target != null ?
            new AnimationClipInstance(targetAnimationClip.name, Target, false, false, false) : null;
    }

    /// <summary>
    /// Set the frame (relative to the start of the gaze instance) when
    /// the gaze shift is expected to end and the fixation start.
    /// </summary>
    /// <param name="frame"></param>
    public virtual void _SetFixationStartFrame(int frame)
    {
        FixationStartTime = ((float)frame) / LEAPCore.editFrameRate;
        if (FixationStartTime >= TimeLength)
            FixationStartTime = TimeLength;
    }

    /// <summary>
    /// Set gaze target animation clip.
    /// </summary>
    /// <param name="targetAnimationClip">Gaze target animation clip</param>
    public virtual void _SetTargetAnimationClip(AnimationClip targetAnimationClip)
    {
        _targetInstance = Target != null && targetAnimationClip != null ?
                new AnimationClipInstance(targetAnimationClip.name, Target, false, false, false) : null;
    }

    /// <summary>
    /// <see cref="AnimationControllerInstance._ApplyController"/>
    /// </summary>
    protected override void _ApplyController(TimeSet times)
    {
        // Get current frame on the gaze track
        int headIndex = ModelController.GetBoneIndex(GazeController.head.Top);
        int frame = LEAPCore.ToFrame(times.boneTimes[headIndex]);

        if (frame == 0 && !_isActive)
        {
            // Start current gaze instance
            _Start(times);
        }
        
        if (_isActive && frame < FrameLength)
        {
            if (_blendIn || _blendOut)
            {
                // This is a gaze shift ahead, blend it out
                int numFrames = Mathf.Min(FrameLength, Mathf.RoundToInt(LEAPCore.editFrameRate * LEAPCore.gazeAheadBlendTime));
                float t = numFrames > 1 ? Mathf.Clamp01(((float)frame) / (numFrames - 1)) : 0f;
                float t2 = t * t;
                float weight = -2f * t2 * t + 3f * t2;
                GazeController.weight = _blendIn ? weight : 1f - weight;
            }
            else
            {
                GazeController.weight = _zeroWeight ? 0f : 1f;
            }
        }
        
        if (frame >= FrameLength - 1 && _isActive)
        {
            // We have reached the end of the current gaze instance
            _Finish();
        }
    }

    // Start the current gaze instance
    protected virtual void _Start(TimeSet times)
    {
        _isActive = true;

        // Register handler for gaze controller state changes
        GazeController.StateChange += new StateChangeEvtH(GazeController_StateChange);

        if (!GazeController.fixGaze && (GazeController.FixGazeTarget == null ||
            GazeController.FixGazeTarget == GazeController.HelperTarget ||
            GazeController.FixGazeTarget == GazeController.FixHelperTarget))
        {
            if (Target != null)
            {
                // This gaze instance is preceded by a period of unconstrained gaze,
                // so enable gaze fixation and start blending in
                GazeController.fixGaze = true;
                _blendIn = true;
            }
            else
                _zeroWeight = true;
        }
        else if (Target == null)
            // This gaze instance is followed by a period of unconstrained gaze,
            // so start blending out
            _blendOut = true;

        // Initiate gaze shift to the target
        GazeController.head.align = Mathf.Clamp01(HeadAlign);
        if (GazeController.torso != null)
            GazeController.torso.align = Mathf.Clamp01(TorsoAlign);
        GazeController.useTorso = TurnBody;
        var movingTargetPosOff = _baseInstance != null ?
            EyeGazeEditor.ComputeMovingGazeTargetPositionOffset(this, times, _baseInstance, _targetInstance) :
            Vector3.zero;
        if (Target != null)
            GazeController.GazeAt(Target, movingTargetPosOff);
        else
            GazeController.GazeAt(AheadTargetPosition, movingTargetPosOff);

        // Reset an active gaze aversion
        var eyesAliveController = Model.GetComponent<EyesAliveController>();
        if (eyesAliveController != null)
            eyesAliveController.resetGazeAversion = true;
    }

    // Finish the current gaze instance
    protected virtual void _Finish()
    {
        _isActive = false;

        if (Target == null)
        {
            // This gaze instance is followed by a period of unconstrained gaze,
            // so disable gaze fixation
            GazeController.fixGaze = false;
        }

        if (GazeController.StateId == (int)GazeState.Shifting)
        {
            // We have reached the end of the gaze instance, make sure
            // the gaze shift terminates on this update
            GazeController.StopGaze();
        }

        // Disable blending
        _blendIn = _blendOut = false;
        _zeroWeight = false;

        // Unregister handler for gaze controller state changes
        GazeController.StateChange -= GazeController_StateChange;
    }

    // Handler for gaze controller state changes
    protected virtual void GazeController_StateChange(AnimController sender, int  srcState, int trgState)
    {
    }
}

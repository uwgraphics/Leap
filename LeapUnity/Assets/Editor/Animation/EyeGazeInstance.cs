﻿using System;
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

    protected float _headAlign, _torsoAlign;
    protected int _baseStartFrame = 0;
    protected int _baseFixationStartFrame = 0;
    protected int _baseFrameLength = 0;

    // TODO: not happy about having elements of runtime state in this class
    protected bool _isActive = false;
    protected bool _blendIn, _blendOut;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="name">Animation instance name</param>
    /// <param name="model">Character model</param>
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
    /// <param name="baseStartFrame">Start frame of the eye gaze instance in the base animation;
    /// only relevant when the gaze instance is not a novel edit</param>
    public EyeGazeInstance(string name, GameObject model,
        int frameLength = 30, GameObject target = null, float headAlign = 0f, float torsoAlign = 0f,
        int fixationStartFrame = -1, bool turnBody = true, bool isBase = false, int baseStartFrame = 0)
        : base(name, model, typeof(GazeController), frameLength)
    {
        Target = target;
        HeadAlign = headAlign;
        TorsoAlign = torsoAlign;
        _SetFixationStartFrame(fixationStartFrame);
        TurnBody = turnBody;
        IsBase = isBase;

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
    public virtual void _SetFixationStartFrame(int frame)
    {
        FixationStartTime = ((float)frame) / LEAPCore.editFrameRate;
        if (FixationStartTime >= TimeLength)
            FixationStartTime = TimeLength;
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
            _Start();
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
                GazeController.weight = 1f;
            }
        }
        
        if (frame >= FrameLength - 1 && _isActive)
        {
            // We have reached the end of the current gaze instance
            _Finish();
        }
    }

    // Start the current gaze instance
    protected virtual void _Start()
    {
        _isActive = true;

        // Register handler for gaze controller state changes
        GazeController.StateChange += new StateChangeEvtH(GazeController_StateChange);

        if (!GazeController.fixGaze && GazeController.FixGazeTarget == null)
        {
            // This gaze instance is preceded by a period of unconstrained gaze,
            // so enable gaze fixation and start blending in
            GazeController.fixGaze = true;
            _blendIn = true;
        }
        else if (Target == null)
        {
            // This gaze instance is followed by a period of unconstrained gaze,
            // so start blending out
            _blendOut = true;
        }

        // Initiate gaze shift to the target
        GazeController.head.align = Mathf.Clamp01(HeadAlign);
        if (GazeController.torso != null)
        {
            GazeController.torso.align = Mathf.Clamp01(TorsoAlign);
        }
        GazeController.useTorso = TurnBody;
        if (Target != null)
            GazeController.GazeAt(Target);
        else
            GazeController.GazeAt(AheadTargetPosition);
        _InitGazeParameters();
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

        // Unregister handler for gaze controller state changes
        GazeController.StateChange -= GazeController_StateChange;
    }

    // Compute gaze shift parameters to account for anticipated body movement
    protected virtual void _InitGazeParameters()
    {
        // How far ahead do we need to look to anticipate the target?
        var timeline = AnimationManager.Instance.Timeline;
        var baseAnimationLayer = timeline.GetLayer(LEAPCore.baseAnimationLayerName);
        var baseAnimationInstance = baseAnimationLayer.Animations.FirstOrDefault(
            inst => inst.Animation.Model == Model);
        GazeController._MovingTargetPositionOffset = EyeGazeEditor.ComputeMovingTargetPositionOffset(
            AnimationManager.Instance.Timeline, baseAnimationInstance.InstanceId, this,
            AnimationManager.Instance.Timeline.CurrentFrame,
            Target == null ? AheadTargetPosition : Target.transform.position);
        // TODO: this should be computed in some smarter, e.g., base animation could be specified as a parameter of the eye gaze instance
    }

    // Handler for gaze controller state changes
    protected virtual void GazeController_StateChange(AnimController sender, int  srcState, int trgState)
    {
    }
}

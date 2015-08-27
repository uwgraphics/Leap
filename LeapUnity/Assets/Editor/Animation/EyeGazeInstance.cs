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
    /// <see cref="AnimationInstance.Start"/>
    /// </summary>
    public override void Start()
    {
        base.Start();

        // Register handler for gaze controller state changes
        GazeController.StateChange += new StateChangeEvtH(GazeController_StateChange);

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

        base.Apply(frame, layerMode);
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

    // Compute gaze shift parameters to account for anticipated body movement
    protected virtual void _InitGazeParameters()
    {
        // How far ahead do we need to look to anticipate the target?
        var baseAnimationInstance = AnimationManager.Instance.Timeline.GetLayer(LEAPCore.baseAnimationLayerName).Animations.FirstOrDefault(
            inst => inst.Animation.Model == Model);
        GazeController.movingTargetPositionOffset = EyeGazeEditor.ComputeMovingTargetPositionOffset(
            AnimationManager.Instance.Timeline, baseAnimationInstance.InstanceId, this,
            AnimationManager.Instance.Timeline.CurrentFrame,
            Target == null ? AheadTargetPosition : Target.transform.position);
        //
        Debug.LogWarning(string.Format("Frame {0}: Moving target position offset is {1}",
            AnimationManager.Instance.Timeline.CurrentFrame, GazeController.movingTargetPositionOffset));
        //
        // TODO: base animation should be specified as a parameter of the eye gaze instance!
    }

    // Handler for gaze controller state changes
    protected virtual void GazeController_StateChange(AnimController sender, int  srcState, int trgState)
    {
    }
}

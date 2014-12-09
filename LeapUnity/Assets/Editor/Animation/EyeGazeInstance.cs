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
    /// <see cref="AnimationInstance.StartBake"/>
    /// </summary>
    public override void StartBake()
    {
        base.StartBake();

        // Initiate gaze shift to target
        GazeController.holdGaze = false;
        if (GazeController.Head != null)
            GazeController.Head.align = HeadAlign;
        if (GazeController.Torso != null)
            GazeController.Torso.align = TorsoAlign;
        GazeController.GazeAt(Target);
    }

    /// <summary>
    /// <see cref="AnimationInstance.Apply"/>
    /// </summary>
    public override void Apply(int frame, AnimationLayerMode layerMode)
    {
        if (IsBaking && GazeController.StateId == (int)GazeState.Shifting)
            GazeController.holdGaze = true;

        base.Apply(frame, layerMode);
    }

    /// <summary>
    /// <see cref="AnimationInstance.FinishBake"/>
    /// </summary>
    public override void FinishBake()
    {
        base.FinishBake();

        // TODO: stop fixating gaze at the target
        // (maybe gaze straight ahead again?)
    }
}

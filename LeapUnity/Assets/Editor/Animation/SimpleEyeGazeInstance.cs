﻿using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Animation instance for simple eye saccade synthesis from body motion.
/// </summary>
public class SimpleEyeGazeInstance : AnimationInstance
{
    /// <summary>
    /// Body animation for which eye saccades should be synthesized.
    /// </summary>
    public virtual AnimationInstance BodyAnimation
    {
        get;
        set;
    }
    
    /// <summary>
    /// <see cref="AnimationInstance.TimeLength"/>
    /// </summary>
    public override float TimeLength
    {
        get { return BodyAnimation.TimeLength; }
        set { throw new InvalidOperationException(); }
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="name">Animation clip name</param>
    /// <param name="model">Character model</param>
    public SimpleEyeGazeInstance(string name, GameObject model, AnimationInstance bodyAnimation)
        : base(name, model)
    {
        BodyAnimation = bodyAnimation;
        if (ModelController == null)
        {
            throw new Exception("Cannot create SimpleEyeGazeSynthesisInstance on a character model without a ModelController");
        }
    }

    /// <summary>
    /// <see cref="AnimationInstance.Apply"/>
    /// </summary>
    public override void Apply(TimeSet times, AnimationLayerMode layerMode)
    {
        int headIndex = ModelController.GetBoneIndex(ModelController.LEye.parent);
        _ApplyEye(LEAPCore.ToFrame(times.boneTimes[headIndex]), layerMode, ModelController.LEye);
        _ApplyEye(LEAPCore.ToFrame(times.boneTimes[headIndex]), layerMode, ModelController.REye);
    }

    protected virtual void _ApplyEye(int frame, AnimationLayerMode layerMode, Transform eye)
    {
        // Get current head pose
        float headYaw = ModelController.LEye.parent.localEulerAngles.y;
        float headPitch = ModelController.LEye.parent.localEulerAngles.x;

        // Compute new eye pose
        float eyeYaw = headYaw / 65f * 45f;
        float eyePitch = headPitch / 65f * 45f;

        // Rotate the eye
        eye.localEulerAngles = new Vector3(eyePitch, eyeYaw, 0f);
    }
}

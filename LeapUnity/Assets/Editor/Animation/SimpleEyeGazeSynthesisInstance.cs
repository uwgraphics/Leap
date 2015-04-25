using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Animation instance for simple eye saccade synthesis from body motion.
/// </summary>
public class SimpleEyeGazeSynthesisInstance : AnimationInstance
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
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="model">Character model</param>
    /// <param name="animationClipName">Animation clip name</param>
    public SimpleEyeGazeSynthesisInstance(GameObject model, string animationClipName, AnimationInstance bodyAnimation)
        : base(model, animationClipName)
    {
        BodyAnimation = bodyAnimation;
        if (ModelController == null)
        {
            throw new Exception("Cannot create SimpleEyeGazeSynthesisInstance on a character model without a ModelController");
        }
    }

    /// <summary>
    /// <see cref="AnimationInstance._Apply"/>
    /// </summary>
    protected override void _Apply(int frame, AnimationLayerMode layerMode)
    {
        _ApplyEye(frame, layerMode, ModelController.LEye);
        _ApplyEye(frame, layerMode, ModelController.REye);
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

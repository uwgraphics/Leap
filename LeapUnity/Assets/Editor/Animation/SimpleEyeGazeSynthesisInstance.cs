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

    protected ModelController _modelController;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="model">Character model</param>
    /// <param name="animationClipName">Animation clip name</param>
    public SimpleEyeGazeSynthesisInstance(GameObject model, string animationClipName, AnimationInstance bodyAnimation)
        : base(model, animationClipName)
    {
        BodyAnimation = bodyAnimation;
        _modelController = model.GetComponent<ModelController>();
        if (_modelController == null)
        {
            throw new Exception("Cannot create SimpleEyeGazeSynthesisInstance on a character model without a ModelController");
        }
    }

    /// <summary>
    /// Apply animation instance to the character model at specified frame.
    /// </summary>
    /// <param name="frame">Frame index</param>
    /// <param name="layerMode">Animation layering mode</param>
    public override void Apply(int frame, AnimationLayerMode layerMode)
    {
        _ApplyEye(frame, layerMode, _modelController.LEye);
        _ApplyEye(frame, layerMode, _modelController.REye);
    }

    public virtual void _ApplyEye(int frame, AnimationLayerMode layerMode, Transform eye)
    {
        // Get current head pose
        float headYaw = _modelController.Head.localEulerAngles.y;
        float headPitch = _modelController.Head.localEulerAngles.x;

        // Compute new eye pose
        float eyeYaw = headYaw / 65f * 45f;
        float eyePitch = headPitch / 65f * 45f;

        // Rotate the eye
        eye.localEulerAngles = new Vector3(eyePitch, eyeYaw, 0f);
    }
}

using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Animation instance corresponding to a dynamic animation controller
/// on the character model.
/// </summary>
public abstract class AnimationControllerInstance : AnimationInstance
{
    /// <summary>
    /// Animation controller activity duration.
    /// </summary>
    public override float TimeLength
    {
        get { return _timeLength; }
        set { _timeLength = value < 0f ? 0f : value; }
    }

    /// <summary>
    /// Animation controller.
    /// </summary>
    public virtual AnimController Controller
    {
        get { return _controller; }
    }

    protected AnimController _controller = null;
    protected float _timeLength = 1f;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="name">Animation instance name</param>
    /// <param name="model">Character model</param>
    /// <param name="controllerType">AnimController type</param>
    /// <param name="frameLength">Animation controller activity duration.</param>
    public AnimationControllerInstance(string name, GameObject model, Type controllerType,
        int frameLength) : base(name, model)
    {
        _controller = model.GetComponent(controllerType) as AnimController;
        FrameLength = frameLength;
    }

    /// <summary>
    /// Apply animation instance to the character model at the specified times.
    /// </summary>
    /// <param name="times">Time indexes</param>
    /// <param name="layerMode">Animation layering mode</param>
    public override void Apply(TimeSet times, AnimationLayerMode layerMode)
    {
        if (layerMode == AnimationLayerMode.Additive)
        {
            throw new Exception("Additive layering not supported for AnimationControllerInstance");
        }

        if (!Controller.enabled)
            return;

        _ApplyController(times);
    }

    // Apply the animation controller at the specified times
    protected abstract void _ApplyController(TimeSet times);
}

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Layer type defines how animations are applied
/// to the character. Supported modes are:
/// - Additive - animation is multiplied by a weight value and added to the current character pose
/// - Override - animation overwrites the current character pose
/// </summary>
public enum AnimationLayerMode
{
    Additive,
    Override
}

/// <summary>
/// Animation instance. This can be either an existing animation clip
/// or a procedural animation that can be baked into an animation clip.
/// </summary>
public abstract class AnimationInstance
{
    /// <summary>
    /// Animation instance name.
    /// </summary>
    public virtual string Name
    {
        get;
        protected set;
    }

    /// <summary>
    /// Character model controller to which this animation instance is applied.
    /// </summary>
    public virtual GameObject Model
    {
        get;
        protected set;
    }

    /// <summary>
    /// Shorthand for getting the character model controller.
    /// </summary>
    public virtual ModelController ModelController
    {
        get { return Model.GetComponent<ModelController>(); }
    }

    /// <summary>
    /// Weight at which the animation instance is applied to the character.
    /// This is only used in additive layers.
    /// </summary>
    public virtual float Weight
    {
        get;
        set;
    }

    /// <summary>
    /// Length of this instance in frames.
    /// </summary>
    public virtual int FrameLength
    {
        get { return Mathf.RoundToInt(TimeLength * LEAPCore.editFrameRate); }
        set { TimeLength = ((float)value) / LEAPCore.editFrameRate; }
    }

    /// <summary>
    /// Length of this instance in seconds.
    /// </summary>
    public abstract float TimeLength
    {
        get;
        set;
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    public AnimationInstance()
    {
        Model = null;
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="name">Animation instance name</param>
    /// <param name="model">Character model</param>
    public AnimationInstance(string name, GameObject model)
    {
        Name = name;
        Model = model;
        if (Model == null)
        {
            throw new Exception("Must specify character model");
        }

        // Set default animation weight
        Weight = 1f;
    }

    /// <summary>
    /// Start the application of the current animation instance.
    /// </summary>
    /// <remarks>AnimationTimeline calls this function during playback, on the first frame of the current instance.</remarks>
    public virtual void Start()
    {
    }

    /// <summary>
    /// Apply animation instance to the character model at specified frame.
    /// </summary>
    /// <param name="frame">Frame index</param>
    /// <param name="layerMode">Animation layering mode</param>
    public abstract void Apply(int frame, AnimationLayerMode layerMode);

    /// <summary>
    /// Finish the application of the current animation instance.
    /// </summary>
    /// <remarks>AnimationTimeline calls this function during playback, after the last frame of the current instance.</remarks>
    public virtual void Finish()
    {
    }

    /// <summary>
    /// Get frame index at specified time index of an animation.
    /// </summary>
    /// <param name="time">Time index</param>
    /// <returns>Frame index</returns>
    public static int GetFrameAtTime(float time)
    {
        return Mathf.RoundToInt(time * LEAPCore.editFrameRate);
    }
}

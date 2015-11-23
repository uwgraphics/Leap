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
/// Animation track types.
/// </summary>
public enum AnimationTrackType
{
    Gaze,
    Posture,
    LArmGesture,
    RArmGesture,
    Locomotion
}

/// <summary>
/// Set of frame indexes signifying a specific time point in a multi-track animation.
/// </summary>
public struct FrameSet
{
    private int[] _frames;

    /// <summary>
    /// Indexer for getting/setting frame indexes in the frame set.
    /// </summary>
    public int this[AnimationTrackType trackType]
    {
        get { return _frames[(int)trackType]; }
        set { _frames[(int)trackType] = value; }
    }

    /// <summary>
    /// Number of animation tracks.
    /// </summary>
    public int NumberOfTracks
    {
        get { return _frames.Length; }
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="frame">Frame indexes on all tracks will be initialized to this frame index</param>
    public FrameSet(int frame)
    {
        _frames = new int[Enum.GetValues(typeof(AnimationTrackType)).Length];
        for (int trackIndex = 0; trackIndex < _frames.Length; ++trackIndex)
            _frames[trackIndex] = frame;
    }

    /// <summary>
    /// Copy constructor.
    /// </summary>
    /// <param name="other">Original frame set</param>
    public FrameSet(FrameSet other) : this(0)
    {
        for (int trackIndex = 0; trackIndex < _frames.Length; ++trackIndex)
            _frames[trackIndex] = other.GetFrame((AnimationTrackType)trackIndex);
    }

    /// <summary>
    /// Get frame index at the specified track.
    /// </summary>
    /// <param name="trackIndex">Animation track index</param>
    /// <returns>Frame index</returns>
    public int GetFrame(int trackIndex)
    {
        return _frames[trackIndex];
    }

    /// <summary>
    /// Get frame index at the specified track.
    /// </summary>
    /// <param name="trackType">Animation track</param>
    /// <returns>Frame index</returns>
    public int GetFrame(AnimationTrackType trackType)
    {
        return _frames[(int)trackType];
    }

    /// <summary>
    /// Set frame index at the specified track.
    /// </summary>
    /// <param name="trackType">Animation track</param>
    /// <param name="frame">Frame index</param>
    public void SetFrame(AnimationTrackType trackType, int frame)
    {
        _frames[(int)trackType] = frame;
    }

    /// <summary>
    /// Get frame set as string.
    /// </summary>
    /// <returns>Frame set as string</returns>
    public override string ToString()
    {
        string str = "";
        for (int trackIndex = 0; trackIndex < _frames.Length; ++trackIndex)
        {
            str += _frames[trackIndex].ToString();
            if (trackIndex < _frames.Length - 1)
                str += ",";
        }

        return str;
    }

    /// <summary>
    /// true if any frame index in the frame set lies within the specified frame index interval,
    /// false otherwise.
    /// </summary>
    /// <param name="s">Start frame index</param>
    /// <param name="e">End frame index</param>
    /// <param name="f">Frame set</param>
    /// <returns>true is frame set lies within the interval, false otherwise</returns>
    public static bool IsBetween(int s, int e, FrameSet f)
    {
        for (int trackIndex = 0; trackIndex < f._frames.Length; ++trackIndex)
            if (f._frames[trackIndex] >= s && f._frames[trackIndex] <= e)
                return true;

        return false;
    }

    /// <summary>
    /// Add a frame offset to the frame indexes in the frame set.
    /// </summary>
    public static FrameSet operator +(FrameSet f, int df)
    {
        FrameSet f1 = new FrameSet(f);
        for (int trackIndex = 0; trackIndex < f1._frames.Length; ++trackIndex)
            f1._frames[trackIndex] += df;

        return f1;
    }

    /// <summary>
    /// Add a frame offset to the frame indexes in the frame set.
    /// </summary>
    public static FrameSet operator +(int df, FrameSet f)
    {
        return f + df;
    }

    /// <summary>
    /// Subtract a frame offset from the frame indexes in the frame set.
    /// </summary>
    public static FrameSet operator -(FrameSet f, int df)
    {
        return f + (-df);
    }
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
    /// Apply animation instance to the character model at specified frame.
    /// </summary>
    /// <param name="frame">Frame index</param>
    /// <param name="layerMode">Animation layering mode</param>
    public void Apply(int frame, AnimationLayerMode layerMode)
    {
        Apply(new FrameSet(frame), layerMode);
    }

    /// <summary>
    /// Apply animation instance to the character model at specified frame.
    /// </summary>
    /// <param name="frame">Frame index</param>
    /// <param name="layerMode">Animation layering mode</param>
    public abstract void Apply(FrameSet frame, AnimationLayerMode layerMode);
    // TODO: handle cases when frame index for a particular track is out of range

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

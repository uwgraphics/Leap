using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Set of time indexes signifying a specific time point in a multi-track animation.
/// </summary>
public struct TimeSet
{
    /// <summary>
    /// Time of the root position in the animation clip.
    /// </summary>
    public float rootTime;

    /// <summary>
    /// Times of all bone rotations in the animation clip.
    /// </summary>
    public float[] boneTimes;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="model">Character model</param>
    public TimeSet(GameObject model)
    {
        rootTime = 0f;
        boneTimes = new float[ModelUtils.GetAllBones(model).Length];
    }

    /// <summary>
    /// Copy constructor.
    /// </summary>
    /// <param name="other">Original time set</param>
    public TimeSet(TimeSet other)
    {
        rootTime = other.rootTime;
        boneTimes = (float[])other.boneTimes.Clone();
    }

    /// <summary>
    /// true if any time index in the time set lies within the specified time index interval,
    /// false otherwise.
    /// </summary>
    /// <param name="s">Start time index</param>
    /// <param name="e">End time index</param>
    /// <param name="f">Time set</param>
    /// <returns>true is time set lies within the interval, false otherwise</returns>
    public static bool IsBetween(float s, float e, TimeSet f)
    {
        if (f.rootTime >= s && f.rootTime <= e)
            return true;

        for (int trackIndex = 0; trackIndex < f.boneTimes.Length; ++trackIndex)
            if (f.boneTimes[trackIndex] >= s && f.boneTimes[trackIndex] <= e)
                return true;

        return false;
    }

    /// <summary>
    /// Add a time offset to the frame indexes in the time set.
    /// </summary>
    public static TimeSet operator +(TimeSet f, float df)
    {
        TimeSet f1 = new TimeSet(f);
        f1.rootTime += df;
        for (int trackIndex = 0; trackIndex < f1.boneTimes.Length; ++trackIndex)
            f1.boneTimes[trackIndex] += df;

        return f1;
    }

    /// <summary>
    /// Add a time offset to the time indexes in the time set.
    /// </summary>
    public static TimeSet operator +(float df, TimeSet f)
    {
        return f + df;
    }

    /// <summary>
    /// Subtract a time offset from the time indexes in the time set.
    /// </summary>
    public static TimeSet operator -(TimeSet f, float df)
    {
        return f + (-df);
    }
}

/// <summary>
/// Set of time indexes determining the temporal location of
/// a key pose in an animation.
/// </summary>
public struct KeyTimeSet
{
    /// <summary>
    /// Key pose time.
    /// </summary>
    public float keyTime;

    /// <summary>
    /// Key pose times for individual bone tracks.
    /// </summary>
    public TimeSet trackKeyTimes;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="model">Character model</param>
    public KeyTimeSet(GameObject model)
    {
        keyTime = 0f;
        trackKeyTimes = new TimeSet(model);
    }
}

/// <summary>
/// Set of time indexes signifying a specific time point in a multi-track animation.
/// </summary>
public struct TrackTimeSet
{
    private float[] _times;

    /// <summary>
    /// Indexer for getting/setting time indexes in the time set.
    /// </summary>
    public float this[AnimationTrackType trackType]
    {
        get { return _times[(int)trackType]; }
        set { _times[(int)trackType] = value; }
    }

    /// <summary>
    /// Number of animation tracks.
    /// </summary>
    public int NumberOfTracks
    {
        get { return _times.Length; }
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="frame">Time indexes on all tracks will be initialized to this time index</param>
    public TrackTimeSet(float time)
    {
        _times = new float[Enum.GetValues(typeof(AnimationTrackType)).Length];
        for (int trackIndex = 0; trackIndex < _times.Length; ++trackIndex)
            _times[trackIndex] = time;
    }

    /// <summary>
    /// Copy constructor.
    /// </summary>
    /// <param name="other">Original time set</param>
    public TrackTimeSet(TrackTimeSet other)
        : this(0)
    {
        for (int trackIndex = 0; trackIndex < _times.Length; ++trackIndex)
            _times[trackIndex] = other.GetTime((AnimationTrackType)trackIndex);
    }

    /// <summary>
    /// Get time index at the specified track.
    /// </summary>
    /// <param name="trackIndex">Animation track index</param>
    /// <returns>Time index</returns>
    public float GetTime(int trackIndex)
    {
        return _times[trackIndex];
    }

    /// <summary>
    /// Get time index at the specified track.
    /// </summary>
    /// <param name="trackType">Animation track</param>
    /// <returns>Time index</returns>
    public float GetTime(AnimationTrackType trackType)
    {
        return _times[(int)trackType];
    }

    /// <summary>
    /// Set time index at the specified track.
    /// </summary>
    /// <param name="trackType">Animation track</param>
    /// <param name="frame">Time index</param>
    public void SetTime(AnimationTrackType trackType, float time)
    {
        _times[(int)trackType] = time;
    }

    /// <summary>
    /// Get time set as string.
    /// </summary>
    /// <returns>Time set as string</returns>
    public override string ToString()
    {
        string str = "";
        for (int trackIndex = 0; trackIndex < _times.Length; ++trackIndex)
        {
            str += Mathf.RoundToInt(LEAPCore.editFrameRate * _times[trackIndex]).ToString();
            if (trackIndex < _times.Length - 1)
                str += ",";
        }

        return str;
    }

    /// <summary>
    /// true if any time index in the time set lies within the specified time index interval,
    /// false otherwise.
    /// </summary>
    /// <param name="s">Start time index</param>
    /// <param name="e">End time index</param>
    /// <param name="f">Time set</param>
    /// <returns>true is time set lies within the interval, false otherwise</returns>
    public static bool IsBetween(float s, float e, TrackTimeSet f)
    {
        for (int trackIndex = 0; trackIndex < f._times.Length; ++trackIndex)
            if (f._times[trackIndex] >= s && f._times[trackIndex] <= e)
                return true;

        return false;
    }

    /// <summary>
    /// Add a time offset to the frame indexes in the time set.
    /// </summary>
    public static TrackTimeSet operator +(TrackTimeSet f, float df)
    {
        TrackTimeSet f1 = new TrackTimeSet(f);
        for (int trackIndex = 0; trackIndex < f1._times.Length; ++trackIndex)
            f1._times[trackIndex] += df;

        return f1;
    }

    /// <summary>
    /// Add a time offset to the time indexes in the time set.
    /// </summary>
    public static TrackTimeSet operator +(float df, TrackTimeSet f)
    {
        return f + df;
    }

    /// <summary>
    /// Subtract a time offset from the time indexes in the time set.
    /// </summary>
    public static TrackTimeSet operator -(TrackTimeSet f, float df)
    {
        return f + (-df);
    }
}

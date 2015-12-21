using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
    /// <param name="time">Time to which all time indexes should be initialized</param>
    public TimeSet(GameObject model, float time = 0f)
    {
        rootTime = time;
        var modelController = model.GetComponent<ModelController>();
        boneTimes = modelController != null ?
            Enumerable.Repeat<float>(time, modelController.NumberOfBones).ToArray() : new float[0];
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
    /// true iff all times in time set t1 are strictly greater than time t2.
    /// </summary>
    public static bool operator >(TimeSet t1, float t2)
    {
        if (t1.rootTime <= t2)
            return false;

        if (t1.boneTimes.Any(t => t <= t2))
            return false;

        return true;
    }

    /// <summary>
    /// true iff all times in time set t1 are strictly lesser than time t2.
    /// </summary>
    public static bool operator <(TimeSet t1, float t2)
    {
        if (t1.rootTime >= t2)
            return false;

        if (t1.boneTimes.Any(t => t >= t2))
            return false;

        return true;
    }

    /// <summary>
    /// true iff all times in time set t1 are greater than or equal to time t2.
    /// </summary>
    public static bool operator >=(TimeSet t1, float t2)
    {
        return !(t1 < t2);
    }

    /// <summary>
    /// true iff all times in time set t1 are lesser than or equal to time t2.
    /// </summary>
    public static bool operator <=(TimeSet t1, float t2)
    {
        return !(t1 > t2);
    }

    /// <summary>
    /// true iff all times in time set t1 are strictly greater than corresponding times in time set t2.
    /// </summary>
    public static bool operator >(TimeSet t1, TimeSet t2)
    {
        if (t1.rootTime <= t2.rootTime)
            return false;

        for (int boneIndex = 0; boneIndex < t1.boneTimes.Length; ++boneIndex)
            if (t1.boneTimes[boneIndex] <= t1.boneTimes[boneIndex])
                return false;

        return true;
    }

    /// <summary>
    /// true iff all times in time set t1 are strictly lesser than corresponding times in time set t2.
    /// </summary>
    public static bool operator <(TimeSet t1, TimeSet t2)
    {
        if (t1.rootTime >= t2.rootTime)
            return false;

        for (int boneIndex = 0; boneIndex < t1.boneTimes.Length; ++boneIndex)
            if (t1.boneTimes[boneIndex] >= t1.boneTimes[boneIndex])
                return false;

        return true;
    }

    /// <summary>
    /// true iff all times in time set t1 are greater than or equal to corresponding times in time set t2.
    /// </summary>
    public static bool operator >=(TimeSet t1, TimeSet t2)
    {
        return !(t1 < t2);
    }

    /// <summary>
    /// true iff all times in time set t1 are lesser than or equal to corresponding times in time set t2.
    /// </summary>
    public static bool operator <=(TimeSet t1, TimeSet t2)
    {
        return !(t1 > t2);
    }

    /// <summary>
    /// Add a time offset to the time indexes in the time set.
    /// </summary>
    public static TimeSet operator +(TimeSet t, float dt)
    {
        TimeSet t1 = new TimeSet(t);
        t1.rootTime += dt;
        for (int trackIndex = 0; trackIndex < t1.boneTimes.Length; ++trackIndex)
            t1.boneTimes[trackIndex] += dt;

        return t1;
    }

    /// <summary>
    /// Add a time offset to the time indexes in the time set.
    /// </summary>
    public static TimeSet operator +(float dt, TimeSet t)
    {
        return t + dt;
    }

    /// <summary>
    /// Subtract a time offset from the time indexes in the time set.
    /// </summary>
    public static TimeSet operator -(TimeSet t, float dt)
    {
        return t + (-dt);
    }

    /// <summary>
    /// Add a time offset to the time indexes in the time set.
    /// </summary>
    public static TimeSet operator +(TimeSet t, TimeSet dt)
    {
        TimeSet t1 = new TimeSet(t);
        t1.rootTime += dt.rootTime;
        for (int trackIndex = 0; trackIndex < t1.boneTimes.Length; ++trackIndex)
            t1.boneTimes[trackIndex] += dt.boneTimes[trackIndex];

        return t1;
    }

    /// <summary>
    /// Subtract a time offset from the time indexes in the time set.
    /// </summary>
    public static TimeSet operator -(TimeSet t, TimeSet dt)
    {
        TimeSet t1 = new TimeSet(t);
        t1.rootTime -= dt.rootTime;
        for (int trackIndex = 0; trackIndex < t1.boneTimes.Length; ++trackIndex)
            t1.boneTimes[trackIndex] -= dt.boneTimes[trackIndex];

        return t1;
    }

    /// <summary>
    /// Multiply all time indexes with the specified scalar value.
    /// </summary>
    public static TimeSet operator *(TimeSet t, float m)
    {
        TimeSet t1 = new TimeSet(t);
        t1.rootTime *= m;
        for (int trackIndex = 0; trackIndex < t1.boneTimes.Length; ++trackIndex)
            t1.boneTimes[trackIndex] *= m;

        return t1;
    }

    /// <summary>
    /// Multiply all time indexes with the specified scalar value.
    /// </summary>
    public static TimeSet operator *(float m, TimeSet t)
    {
        TimeSet t1 = new TimeSet(t);
        t1.rootTime *= m;
        for (int trackIndex = 0; trackIndex < t1.boneTimes.Length; ++trackIndex)
            t1.boneTimes[trackIndex] *= m;

        return t1;
    }

    /// <summary>
    /// Divide all time indexes by the specified scalar value.
    /// </summary>
    public static TimeSet operator /(TimeSet t, float m)
    {
        return t * (1f / m);
    }

    /// <summary>
    /// Divide all time indexes by the specified scalar value.
    /// </summary>
    public static TimeSet operator /(float m, TimeSet t)
    {
        return t * (1f / m);
    }

    /// <summary>
    /// Get frame indexes in the time set as a string.
    /// </summary>
    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.Append("(");
        sb.Append(LEAPCore.ToFrame(rootTime));
        sb.Append(", ");
        for (int boneIndex = 0; boneIndex < boneTimes.Length; ++boneIndex)
        {
            sb.Append(LEAPCore.ToFrame(boneTimes[boneIndex]));
            if (boneIndex < boneTimes.Length - 1)
                sb.Append(", ");
        }
        sb.Append(")");

        return sb.ToString();
    }
}

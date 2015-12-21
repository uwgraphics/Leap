using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Interface for timewarps applied to animations.
/// </summary>
public interface ITimewarp
{
    /// <summary>
    /// Get time index in the original animation segment that corresponds to the input time index.
    /// </summary>
    /// <param name="inTime">Input time index (in 0-1 range)</param>
    /// <returns>Time index in the original animation segment (in 0-1 range)</returns>
    float GetTime(float inTime);
}

/// <summary>
/// Timewarp implementing a hold in the animation.
/// </summary>
public struct HoldTimewarp : ITimewarp
{
    /// <summary>
    /// <see cref="ITimewarp.GetTime"/>
    /// </summary>
    public float GetTime(float inTime)
    {
        return 0f;
    }
}

/// <summary>
/// Timewarp implementing a linear compression or expansion of the animation.
/// </summary>
public struct LinearTimewarp : ITimewarp
{
    /// <summary>
    /// <see cref="ITimewarp.GetTime"/>
    /// </summary>
    public float GetTime(float inTime)
    {
        return inTime;
    }
}

/// <summary>
/// Timewarp implementing a moving hold in the animation.
/// </summary>
public struct MovingHoldTimewarp : ITimewarp
{
    private BezierCurve _twCurve;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="origTime">Time index of the original animation keyframe which is held (in 0-1 range)</param>
    /// <param name="easeOutFrameLength">Normalized time length in the timewarped animation segment
    /// over which animation eases out and into the hold (in 0-1 range)</param>
    public MovingHoldTimewarp(float origKeyTime, float easeOutLength)
    {
        // Compute control points of the timewarp curve
        Vector2 p0 = new Vector2(0f, 0f);
        Vector2 p1 = new Vector2(easeOutLength, origKeyTime);
        Vector2 p3 = new Vector2(1f, 1f);
        Vector2 b0 = p0 + 0.5f * (p1 - p0).magnitude * (new Vector2(1f, 1f)).normalized;
        Vector2 a1 = p1 - 0.1f * (p1 - p0).magnitude * (new Vector2(1f, 0f)).normalized;
        Vector2 b1 = p1 + 0.1f * (p3 - p1).magnitude * (new Vector2(1f, 0f)).normalized;
        Vector2 a3 = p3 - 0.5f * (p3 - p1).magnitude * (new Vector2(1f, 1f)).normalized;

        // Construct the timewarp curve
        _twCurve = new BezierCurve();
        _twCurve.controlPoints = new Vector3[7];
        _twCurve.controlPoints[0] = p0;
        _twCurve.controlPoints[1] = b0;
        _twCurve.controlPoints[2] = a1;
        _twCurve.controlPoints[3] = p1;
        _twCurve.controlPoints[4] = b1;
        _twCurve.controlPoints[5] = a3;
        _twCurve.controlPoints[6] = p3;
        _twCurve.Init();
    }

    /// <summary>
    /// <see cref="ITimewarp.GetTime"/>
    /// </summary>
    public float GetTime(float inTime)
    {
        float outTime = _SampleTWCurve(inTime);
        return outTime;
    }

    private float _SampleTWCurve(float x)
    {
        float umax = (float)_twCurve.NumberOfSegments;
        for (float u = 0f; u < umax; u += LEAPCore.eulerTimeStep)
        {
            Vector3 p = _twCurve.Sample(u);
            if (p.x >= x)
                return p.y;
        }

        return _twCurve.Sample(umax).y;
    }
}

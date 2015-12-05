using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Interface for timewarps applied to animations.
/// </summary>
public interface ITimewarp
{
    /// <summary>
    /// Length of the animation segment before timewarping.
    /// </summary>
    int OrigFrameLength { get; }

    /// <summary>
    /// Length of the animation segment after timewarping.
    /// </summary>
    int FrameLength { get; }
    
    /// <summary>
    /// Get frame index in the original animation segment that corresponds to the input frame index.
    /// </summary>
    /// <param name="inFrame">Input frame index</param>
    /// <returns>Frame index in the original animation segment</returns>
    int GetFrame(int inFrame);
}

/// <summary>
/// Timewarp implementing a hold in the animation.
/// </summary>
public struct HoldTimewarp : ITimewarp
{
    /// <summary>
    /// <see cref="ITimewarp.OrigFrameLength"/>
    /// </summary>
    public int OrigFrameLength { get { return 1; } }

    /// <summary>
    /// <see cref="ITimewarp.FrameLength"/>
    /// </summary>
    public int FrameLength { get { return _holdFrameLength; } }

    private int _holdFrameLength;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="holdFrameLength">Animation hold length in frames</param>
    public HoldTimewarp(int holdFrameLength)
    {
        _holdFrameLength = holdFrameLength;
    }

    /// <summary>
    /// <see cref="ITimewarp.GetFrame"/>
    /// </summary>
    public int GetFrame(int inFrame)
    {
        return 0;
    }
}

/// <summary>
/// Timewarp implementing a linear compression or expansion of the animation.
/// </summary>
public struct LinearTimewarp : ITimewarp
{
    /// <summary>
    /// <see cref="ITimewarp.OrigFrameLength"/>
    /// </summary>
    public int OrigFrameLength { get { return _origFrameLength; } }

    /// <summary>
    /// <see cref="ITimewarp.FrameLength"/>
    /// </summary>
    public int FrameLength { get { return _newFrameLength; } }

    private int _origFrameLength;
    private int _newFrameLength;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="origFrameLength">Original animation length in frames</param>
    /// <param name="newFrameLength">Timewarped animation length in frames</param>
    public LinearTimewarp(int origFrameLength, int newFrameLength)
    {
        _origFrameLength = origFrameLength;
        _newFrameLength = newFrameLength;
    }

    /// <summary>
    /// <see cref="ITimewarp.GetFrame"/>
    /// </summary>
    public int GetFrame(int inFrame)
    {
        float t = ((float)inFrame) / (FrameLength - 1);
        t = Mathf.Clamp01(t);
        return Mathf.RoundToInt(t * (OrigFrameLength - 1));
    }
}

/// <summary>
/// Timewarp implementing a moving hold in the animation.
/// </summary>
public struct MovingHoldTimewarp : ITimewarp
{
    /// <summary>
    /// <see cref="ITimewarp.OrigFrameLength"/>
    /// </summary>
    public int OrigFrameLength { get { return _origFrameLength; } }

    /// <summary>
    /// <see cref="ITimewarp.FrameLength"/>
    /// </summary>
    public int FrameLength { get { return _holdFrameLength; } }

    private int _origFrameLength;
    private int _holdFrameLength;
    private BezierCurve _twCurve;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="origKeyFrame">Index in the original animation keyframe which is held</param>
    /// <param name="origFrameLength">Original animation length in frames</param>
    /// <param name="holdFrameLength">Animation hold length in frames</param>
    /// <param name="easeOutFrameLength">Frame length over which animation eases out and into the hold</param>
    public MovingHoldTimewarp(int origKeyFrame, int origFrameLength,
        int holdFrameLength, int easeOutFrameLength)
    {
        _origFrameLength = origFrameLength;
        _holdFrameLength = holdFrameLength;

        // Compute control points of the timewarp curve
        Vector2 p0 = new Vector2(0f, 0f);
        Vector2 p1 = new Vector2(easeOutFrameLength, origKeyFrame);
        Vector2 p3 = new Vector2(holdFrameLength, origFrameLength);
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
    /// <see cref="ITimewarp.GetFrame"/>
    /// </summary>
    public int GetFrame(int inFrame)
    {
        int outFrame = Mathf.RoundToInt(_SampleTWCurve((float)inFrame));
        return outFrame;
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

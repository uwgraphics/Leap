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
    /// <param name="holdTime">Animation hold time</param>
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
/// Timewarp implementing a hold in the animation.
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
    /// <param name="holdTime">Animation hold time</param>
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

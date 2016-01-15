using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

/// <summary>
/// 
/// </summary>
public class GazeBlock : IComparable<GazeBlock> {

    public int StartFrame
    {
        get;
        set;
    }
    public int FixationStartFrame
    {
        get;
        set;
    }
    public int FixationEndFrame
    {
        get;
        set;
    }
    public string CharacterName
    {
        get;
        set;
    }
    public string AnimationClip
    {
        get;
        set;
    }

    public GameObject Target
    {
        get;
        set;
    }

    public string TargetName
    {
        get;
        set;
    }
    public double HeadAlign
    {
        get;
        set;
    }
    public double TorsoAlign
    {
        get;
        set;
    }
    public bool TurnBody
    {
        get;
        set;
    }

    public Vector3 FixationPoint
    {
        get;
        set;
    }

    public override string ToString()
    {
        return "[" + StartFrame + ", " + FixationStartFrame + ", " + FixationPoint + "]";
    }

    public int CompareTo(GazeBlock other) {
        return this.StartFrame - other.StartFrame == 0 ? 
            this.FixationStartFrame - other.FixationStartFrame : this.StartFrame - other.StartFrame;
    }

	//constructor
    public GazeBlock(int start, int fixationStart, string charName, string animationTitle, string targetName, GameObject target, bool turnBody, Vector3 fixationPoint) {
        //sanity check
        if (fixationStart < start) {
            UnityEngine.Debug.Log("Start: " + start + " End: " + fixationStart);
            throw new ArgumentException("Illegal GazeBlock!");
        }
        StartFrame = start; FixationStartFrame = fixationStart; 
        CharacterName = charName; AnimationClip = animationTitle;
        Target = target;
        TargetName = targetName;
        TurnBody = turnBody;
        //automatically sets these to -1, don't think I'll be adjusting these on the inference side...
        HeadAlign = -1.0;
        TorsoAlign = -1.0;
        //automatically sets fixation end frame here, will be handled separately when printed in files.  
        //must have neighborhood of other gazeblocks to make assessment.
        FixationEndFrame = -1;
        FixationPoint = fixationPoint;

    }

    //alternate constructor without targeting information (mostly for file input timelines)
    public GazeBlock(int start, int fixationStart, string charName, string animationTitle, string targetName, GameObject target, bool turnBody) :
        this(start, fixationStart, charName, animationTitle, targetName, target, turnBody, new Vector3(0, 0, 0)) { }
}

/// <summary>
/// 
/// </summary>
public class GazeBlockPair {
    public GazeBlock[] GazeBlocks
    {
        get;
        set;
    }
    // (lower is better for overlap score)
    public double Score
    {
        get;
        set;
    }

    public override string ToString()
    {
        return "< " + this.GazeBlocks[0] + ", " + this.GazeBlocks[1] + " Score: " + Score + " >";
    }

    //constructor
    public GazeBlockPair(GazeBlock a, GazeBlock b) {
        GazeBlocks = new GazeBlock[2];
        GazeBlocks[0] = a; GazeBlocks[1] = b;

        double startOverlap = Math.Abs(a.StartFrame - b.StartFrame);
        double endOverlap = Math.Abs(a.FixationStartFrame - b.FixationStartFrame);
        double scoreScalar = 0.1;
        Score = (startOverlap + endOverlap) * scoreScalar; // lower is better
    }
}

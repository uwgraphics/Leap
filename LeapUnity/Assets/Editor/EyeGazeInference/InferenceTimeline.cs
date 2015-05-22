using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.IO;

/// <summary>
/// Contains a list of GazeBlock objects.  Will be used to Officially house
/// the inferred gaze data.
/// </summary>
public class InferenceTimeline {
    public List<GazeBlock> GazeBlocks
    {
        get;
        set;
    }

    public override string ToString()
    {
        string temp = "< ";
        foreach (var g in GazeBlocks) {
            temp += (" " + g);
        }
        temp += " >";
        return temp;
    }

    //constructor
    public InferenceTimeline(List<GazeBlock> gbs) {
        GazeBlocks = gbs;
        GazeBlocks.Sort();
        calcEndFrames();
    }

    //alternate contructor that takes in hand-made annotations and 
    //manually populates the inference timeline
    public InferenceTimeline(string filePath) {
        var gbs = new List<GazeBlock>();

        StreamReader reader = File.OpenText(filePath);
        string line;
        while ((line = reader.ReadLine()) != null)
        {
            var tokens = line.Split(',');
            int test;
            //if not an integer, it is likely the first line so just continue
            if (!int.TryParse(tokens[2], out test)) continue;
            
            gbs.Add(new GazeBlock(int.Parse(tokens[2]), int.Parse(tokens[3]) + int.Parse(tokens[2]), tokens[0], tokens[1], tokens[6], null, bool.Parse(tokens[9])));
        }

        GazeBlocks = gbs;
        GazeBlocks.Sort();
        calcEndFrames();
    }

    public void TimelineFileOutput(string filePath) {
        var g = GazeBlocks; // shorthand

        using (System.IO.StreamWriter file = new System.IO.StreamWriter(filePath)) {
            //file.WriteLine(g[0].AnimationClip);
            file.WriteLine("Character,AnimationClip,StartFrame,FixationStartFrame,EndFrame,Target,AheadTargetPosition,HeadAlign,TorsoAlign,TurnBody");

            for (int i = 0; i < g.Count; i++) {
                file.WriteLine(g[i].CharacterName + "," + g[i].AnimationClip + "," +
                    g[i].StartFrame + "," + g[i].FixationStartFrame + "," + 
                    g[i].FixationEndFrame + "," + g[i].TargetName + "," + "0 0 0" + "," + g[i].HeadAlign + "," +
                    g[i].TorsoAlign + "," + g[i].TurnBody);
            }
        }
    }

    //utility function that calculates
    //UPDATE 05/21/15 : change from next block - 1 by default to a maximum of a second
    private void calcEndFrames() {
        
        var maxTimeFrame = 15;

        if (GazeBlocks.Count == 0)
        {
            UnityEngine.Debug.Log("Cannot calculate end frames.  Empty gaze block list");
            return;
        }
        for (int i = 0; i < GazeBlocks.Count - 1; i++) {
            var nextFrameDiff = GazeBlocks[i + 1].StartFrame - GazeBlocks[i].FixationStartFrame;
            GazeBlocks[i].FixationEndFrame = nextFrameDiff >= maxTimeFrame ? GazeBlocks[i].FixationStartFrame + maxTimeFrame : GazeBlocks[i+1].StartFrame - 1;
        }
        var lastFrame = AnimationTimeline.Instance.FrameLength;
        var l = GazeBlocks[GazeBlocks.Count - 1].FixationStartFrame + maxTimeFrame;
        GazeBlocks[GazeBlocks.Count - 1].FixationEndFrame = l > lastFrame ? AnimationTimeline.Instance.FrameLength : l;
    }
}


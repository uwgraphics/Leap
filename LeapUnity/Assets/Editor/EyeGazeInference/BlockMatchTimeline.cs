using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;


/// <summary>
/// Goes through primary timeline's gaze blocks and takes min and max start and end times of 
/// gaze block most overlapping in the secondary timeline
/// </summary>
public static class BlockMatchTimeline {

    public static InferenceTimeline MatchTimelines(InferenceTimeline primary, InferenceTimeline secondary, string animationName, string characterName, bool addScenePoints = true, bool targeting = true) {
        var gbs = new List<GazeBlock>();
        var gbsP = primary.GazeBlocks;
        var gbsS = secondary.GazeBlocks;

        //main loop that goes through all blocks in the primary 
        for (int i = 0; i < gbsP.Count; i++) {
            int index;
            var mostOverlapping = mostOverlappingBlock(gbsP[i], gbsS, out index);

            if (mostOverlapping == null) {
                gbs.Add(gbsP[i]);
                continue;
            }

            var max_fixation = Math.Max(gbsP[i].FixationStartFrame, mostOverlapping.FixationStartFrame);
            var min_start = Math.Min(gbsP[i].StartFrame, mostOverlapping.StartFrame);

            //delete block from secondary list
            gbsS.RemoveAt(index);

            var g = gbsP[i];
            gbs.Add(new GazeBlock(min_start, max_fixation, g.CharacterName, g.AnimationClip, g.TargetName, g.Target, g.TurnBody));

        } // end of for loop

        var gbs_overlapFix = overlapFix(gbs);

        List<GazeBlock> gbs_final = gbs_overlapFix;
        GazeTarget gt = null;
        if (targeting) {
            gt = new GazeTarget(animationName, characterName, gbs_overlapFix);
            gbs_final = gt.GazeTargetInference();
        }
        
        //if addScenePoints is true, have the gaze target object add gaze points to scene and return
        //an inference timeline with the updated gaze target information
        if (addScenePoints)
        {
            if (gt == null) gt = new GazeTarget(animationName, characterName, gbs_final);
            return new InferenceTimeline(gt.AddScenePoints());
        }
        //otherwise, just keep the inference timeline as is with the default scene targets
        else 
        {
            return new InferenceTimeline(gbs_final);  
        }
    }


    /// <summary>
    /// Glues two timelines together
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    public static InferenceTimeline GlueTimelines(InferenceTimeline a, InferenceTimeline b, string animationTitle, string characterName) {
        var gbs = new List<GazeBlock>();
        var gbsA = new List<GazeBlock>(a.GazeBlocks);
        var gbsB = new List<GazeBlock>(b.GazeBlocks);

        for (int i = 0; i < gbsA.Count; i++) {
            gbs.Add(gbsA[i]);
        }

        for (int i = 0; i < gbsB.Count; i++) {
            for (int j = 0; j < gbsA.Count; j++) {
                if (overlapTest(gbsA[j], gbsB[i])) break;

                if (j >= gbsA.Count - 1) gbs.Add(gbsB[i]);
            }
        }

        gbs.Sort();

        return new InferenceTimeline(gbs);
    }

    /// <summary>
    /// Find most overlapping block in another list
    /// </summary>
    /// <param name="p"></param>
    /// <param name="secondary"></param>
    /// <param name="index">index of the block to be removed from secondary list</param>
    /// <returns></returns>
    private static GazeBlock mostOverlappingBlock(GazeBlock p, List<GazeBlock> secondary, out int index) {
        double maxOverlap = 0.0;
        GazeBlock mostOverlapping = null;
        int maxIndex = -1;

        for (int i = 0; i < secondary.Count; i++) {
            var overlap = overlapScore(p, secondary[i]);
            if (overlap > maxOverlap) {
                maxOverlap = overlap;
                mostOverlapping = secondary[i];
                maxIndex = i;
            } 
        }

        index = maxIndex;
        return mostOverlapping;
    }

    /// <summary>
    /// Returns the overlap score of two gaze blocks.  If the second is subsumed by the first, the score will be 
    /// 1.  If the two don't overlap at all, the score will be 0.  The score is how many frames the second overlaps
    /// the first divided by the total length of the second block.  
    /// </summary>
    /// <param name="p"></param>
    /// <param name="s"></param>
    /// <returns></returns>
    private static double overlapScore(GazeBlock p, GazeBlock s) {
        if (subsumeTest(p, s)) return 1.0;
        if (subsumeTest(s, p)) return 1.0;

        if (overlapTest(p, s)) {
            var max_fixation = Math.Max(p.FixationStartFrame, s.FixationStartFrame);
            var min_fixation = Math.Min(p.FixationStartFrame, s.FixationStartFrame);
            var max_start = Math.Max(p.StartFrame, s.StartFrame);
            var min_start = Math.Min(p.StartFrame, s.StartFrame);
            var totalRange = max_fixation - min_start;
            var overlap = totalRange - (max_fixation - min_fixation) - (max_start - min_start);

            var sLength = s.FixationStartFrame - s.StartFrame;

            return (double)overlap / (double)sLength;

        }

        return -0.1;
    }

    /// <summary>
    /// Returns true if the two blocks overlap
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    private static bool overlapTest(GazeBlock a, GazeBlock b)
    {
        if (a.FixationStartFrame < b.StartFrame || a.StartFrame > b.FixationStartFrame) return false;
        else return true;
    }

    /// <summary>
    /// Check to see if b subsumes a
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    private static bool subsumeTest(GazeBlock a, GazeBlock b) {
        if(a.StartFrame >= b.StartFrame && a.FixationStartFrame <= b.FixationStartFrame) return true;
        else return false;
    }
    

    /// <summary>
    /// fixes overlaps in the timeline
    /// </summary>
    /// <param name="gbs"></param>
    /// <returns></returns>
    private static List<GazeBlock> overlapFix(List<GazeBlock> gbs) {
        if (gbs.Count == 0) return gbs;
        var _gbs = new List<GazeBlock>();

        for (int i = 0; i < gbs.Count - 1; i++)
        {
            var g = gbs[i];
            if (gbs[i].FixationStartFrame >= gbs[i + 1].StartFrame)
            {
                _gbs.Add(new GazeBlock(g.StartFrame, gbs[i + 1].StartFrame - 1, g.CharacterName, g.AnimationClip, g.TargetName, g.Target, g.TurnBody));
            }
            else _gbs.Add(g);
        }
        _gbs.Add(gbs[gbs.Count - 1]);

        return _gbs;
    }
}

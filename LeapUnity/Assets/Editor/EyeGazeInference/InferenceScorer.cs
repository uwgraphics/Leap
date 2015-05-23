using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

/// <summary>
/// Serves to score two inference timelines against each other to 
///     judge accuracy of the inferred gaze shifts against hand annotated
///     versions
/// </summary>
public class InferenceScorer {
    public InferenceTimeline[] Timelines
    {
        get;
        set;
    }

    public double Score
    {
        get;
        set;
    }

    public override string ToString()
    {
        return Timelines[0] + "\n" + Timelines[1] + "  Score: " +  Score;
    }

    //constructor
    public InferenceScorer(InferenceTimeline a, InferenceTimeline b) {
        //initialize score to 100.0
        Score = 100.0;

        Timelines = new InferenceTimeline[2];
        Timelines[0] = a; Timelines[1] = b;
        
        //1.) Check for block discards
        var Discarded = discards(Timelines);

        //2.) Check for subsumptions
        var Subsumptions = subsumption(Discarded);

        //3.) Score resulting overlap
        overlapScoring(Subsumptions);
    }

    //helper function
    private InferenceTimeline[] discards(InferenceTimeline[] Timelines) {
        var it = new InferenceTimeline[2];
        var discardedA = new List<GazeBlock>();
        var discardedB = new List<GazeBlock>();

        double autoSubtract = 0.2;
        double subtractScalar = 0.05;

        //check first list
        for (int i = 0; i < Timelines[0].GazeBlocks.Count; i++) {
            for (int j = 0; j < Timelines[1].GazeBlocks.Count; j++) {
                //GazeBlock is only added if it overlaps at least one of the blocks in list b
                if ( overlap(Timelines[0].GazeBlocks[i], Timelines[1].GazeBlocks[j]) ) {
                    discardedA.Add(Timelines[0].GazeBlocks[i]);
                    break;
                }
 
                //only gets here if block is discarded.  Must subtract some points.
                if (j == Timelines[1].GazeBlocks.Count - 1) {
                    

                    //automatically subtract 3 points per discard
                    Score -= autoSubtract;
                    Score -= subtractScalar * minBlockDis(Timelines[0].GazeBlocks[i], Timelines[1]);
                }
            }
        }

        //check second list
        for (int i = 0; i < Timelines[1].GazeBlocks.Count; i++)
        {
            for (int j = 0; j < Timelines[0].GazeBlocks.Count; j++)
            {
                //GazeBlock is only added if it overlaps at least one of the blocks in list b
                if (overlap(Timelines[1].GazeBlocks[i], Timelines[0].GazeBlocks[j]))
                {
                    discardedB.Add(Timelines[1].GazeBlocks[i]);
                    break;
                }

                //only gets here if block is discarded.  Must subtract some points.
                if (j == Timelines[0].GazeBlocks.Count - 1)
                {
                    //automatically subtract 3 points per discard
                    Score -= autoSubtract;
                    Score -= subtractScalar * minBlockDis(Timelines[1].GazeBlocks[i], Timelines[0]);
                }
            }
        }

        it[0] = new InferenceTimeline(discardedA);
        it[1] = new InferenceTimeline(discardedB);

        return it; 
    }

    //utility function to find shortest distance to block in other list
    private int minBlockDis(GazeBlock a, InferenceTimeline otherList) {
        int minDis = int.MaxValue; 
        //check block ends while those ends are less than a.start
        int tempDis = 0;
        int index = otherList.GazeBlocks.Count;
        //sort list
        otherList.GazeBlocks.Sort();
        for (int i = 0; i < otherList.GazeBlocks.Count; i++) {
            if (a.StartFrame - otherList.GazeBlocks[i].FixationStartFrame < 0) {
                index = i;
                break;
            }

            tempDis = a.StartFrame - otherList.GazeBlocks[i].FixationStartFrame;
            if (tempDis < minDis) minDis = tempDis;
        }

        //check block starts for the remainder
        for (int i = index; i < otherList.GazeBlocks.Count; i++) {
            tempDis = otherList.GazeBlocks[i].StartFrame - a.FixationStartFrame;
            if (tempDis < minDis) minDis = tempDis;
        }

        return minDis;

    }

    //utility function that checks for subsumptions and returns new lists
    //solves recursively
    private InferenceTimeline[] subsumption(InferenceTimeline[] Timelines) {
        if (Timelines[0].GazeBlocks.Count == Timelines[1].GazeBlocks.Count) return Timelines;

        var it = new InferenceTimeline[2];

        var a = Timelines[0];
        var b = Timelines[1];
        a.GazeBlocks.Sort();
        b.GazeBlocks.Sort();

        var subsumedA = subsumedList(a.GazeBlocks, b.GazeBlocks);
        var subsumedB = subsumedList(b.GazeBlocks, a.GazeBlocks);

        it[0] = new InferenceTimeline(subsumedA);
        it[1] = new InferenceTimeline(subsumedB);

        return subsumption(it);

    }

    //splits GazeBlock A.  Also, penalizes score on split in this function.
    private GazeBlock[] split(GazeBlock a1, GazeBlock b1, GazeBlock b2) {

        var gbs = new GazeBlock[2];
        
        gbs[0] = new GazeBlock(a1.StartFrame, b1.FixationStartFrame, a1.CharacterName, a1.AnimationClip, a1.TargetName, a1.Target, a1.TurnBody);
        gbs[1] = new GazeBlock(b2.StartFrame, a1.FixationStartFrame, a1.CharacterName, a1.AnimationClip, a1.TargetName, a1.Target, a1.TurnBody);

        double autoPenalty = 0.3;
        double penaltyScalar = 0.05;

        Score -= autoPenalty;
        Score -= penaltyScalar * (b2.StartFrame - b1.FixationStartFrame);

        return gbs;
    }

    //utility function used for subsuming lists together
    private List<GazeBlock> subsumedList(List<GazeBlock> a, List<GazeBlock> b) {
        var subsumedA = new List<GazeBlock>();

        for (int i = 0; i < a.Count; i++)
        {
            for (int j = 0; j < b.Count - 1; j++)
            {
                if (i == a.Count - 1)
                {
                    if (subsumeTest(a[i], b[j], b[j + 1]))
                    {
                        var s = split(a[i], b[j], b[j + 1]);
                        //add the resulting split blocks to the list
                        subsumedA.Add(s[0]); subsumedA.Add(s[1]);
                        break;
                    }
                }
                else
                {
                    if (subsumeTest(a[i], a[i + 1], b[j], b[j + 1]))
                    {
                        var s = split(a[i], b[j], b[j + 1]);
                        //add the resulting split blocks to the list
                        subsumedA.Add(s[0]); subsumedA.Add(s[1]);
                        break;
                    }
                }

                //only gets to this point if j made it to the end of the list without a subsumption
                if (j == b.Count - 2)
                {
                    subsumedA.Add(a[i]);
                }
            }
        } // end of for loop

        if (b.Count <= 1)
        {
            foreach (var g in a)
            {
                subsumedA.Add(g);
            }
        }

        return subsumedA;
    }

    private bool subsumeTest(GazeBlock a1, GazeBlock a2, GazeBlock b1, GazeBlock b2) {
        if (a2 == null) return false;

        if (overlap(a1, b1) && overlap(a1, b2))
        {
            if (!overlap(a2, b2))
            {
                return true;
            }
        }
        return false;
    }

    //alternate function when a2 does not exist, it automatically passes the second test
    private bool subsumeTest(GazeBlock a1, GazeBlock b1, GazeBlock b2) {
        if (overlap(a1, b1) && overlap(a1, b2)) return true;
        else return false;
    }

    //scores how much two lists overlap.  Automatically subtracts from score here
    private void overlapScoring(InferenceTimeline[] Timelines) {
        if (Timelines[0].GazeBlocks.Count != Timelines[1].GazeBlocks.Count) {
            throw new ArgumentException("Cannot score overlap.  Timelines do not have same number of blocks");
        }

        var a = Timelines[0];
        var b = Timelines[1];

        double startScalar = 0.05;
        double endScalar = 0.05;
        double targetErrorScalar = 1.0;

        int targetError = 7;

        int startTotal = 0;
        int endTotal = 0;
        int targetErrorTotal = 0;

        //compare starts and ends
        for (int i = 0; i < a.GazeBlocks.Count; i++) {
            startTotal += Math.Abs(a.GazeBlocks[i].StartFrame - b.GazeBlocks[i].StartFrame);
            endTotal += Math.Abs(a.GazeBlocks[i].FixationStartFrame - b.GazeBlocks[i].FixationStartFrame);
            if ( !a.GazeBlocks[i].TargetName.Equals(b.GazeBlocks[i].TargetName)) {
                targetErrorTotal += targetError;
            }
        }

        Score -= startScalar * startTotal;
        Score -= endScalar * endTotal;
        //comment this next line out if you don't want the scoring heuristic to take into account
        //target errors.  This will not be handled early on in the testing process.
        //Score -= targetErrorScalar * targetErrorTotal;
    }

    /// <summary>
    /// returns true iff the two blocks DO overlap
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    private bool overlap(GazeBlock a, GazeBlock b) {
        if (a.FixationStartFrame < b.StartFrame || a.StartFrame > b.FixationStartFrame) return false;
        else return true;
    }
}

using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Takes in two magnitude lists (one for gaze shift starts, and one 
/// for gaze fixation starts) and infers an inference timeline from this information
/// </summary>
public class SubGazeInference {
    public InferenceTimeline InferenceTimeline { get; set; }
    public string AnimationTitle { get; set; }
    public string CharacterName { get; set; }
    public AnimationClipInstance AnimationClip { get; set; }
    public InferenceCharacter Character { get; set; }
    private int BlockCounter { get; set; }
    public GazeTarget GazeTarget { get; set; }
    private bool Targeting { get; set; }
    
    //constructor
    /// <summary>
    /// constructor
    /// </summary>
    /// <param name="animationTitle"></param>
    /// <param name="characterName"></param>
    /// <param name="fileOutput">should end in ".csv" </param>
    /// <param name="sgi">contains important information for gaze shift inference</param>
    public SubGazeInference(string animationTitle, string characterName, string fileOutput, SubGazeInfo sgi, bool targeting = false) {
        AnimationTitle = animationTitle;
        CharacterName = characterName;
        Character = new InferenceCharacter(characterName);
        AnimationClip = new AnimationClipInstance(Character.CharModel, animationTitle);
        Targeting = targeting;
        BlockCounter = 1;

        var ta = inferGazeBlocks(sgi);
        InferenceTimeline = ta;
        //var tb = new InferenceTimeline(GlobalVars.MatlabPath + animationTitle + ".csv");
        //
        //var scorer = new InferenceScorer(ta, tb);
        //UnityEngine.Debug.Log(scorer);
        ta.TimelineFileOutput(GlobalVars.MatlabPath + fileOutput);
    }

    /// <summary>
    /// Infers the gaze blocks corresponding to gaze shifts and returns an inference timeline.  This is where
    /// most of the inference work is done.
    /// </summary>
    /// <returns></returns>
    private InferenceTimeline inferGazeBlocks(SubGazeInfo sgi) {
        var gbs = new List<GazeBlock>();

        //boolean list for gaze shift tests
        bool lastMax = true;

        //Main loop that goes through specified minima
        for (int i = 0; i < sgi.Ends_Mins.Count; i++) {

            //tests
            if (lastMax)
            {
                if (!lastMaxTest(sgi.Ends_Mins[i], sgi)) continue;
            }

            var start = gazeStartFrame(sgi.Ends_Mins[i], sgi);

            // discard gaze block if less than 7.5 frames
            if (sgi.Ends_Mins[i] - start < 7.5) continue;

            //TODO: Target testing
            string t = "";
            GameObject to = null;

            gbs.Add(new GazeBlock(start, sgi.Ends_Mins[i], sgi.AngleData.Character.CharName, sgi.AngleData.AnimationName + "Gaze" + BlockCounter.ToString(), t, to, true));
            BlockCounter++; //increment counter

        
        } // end of for loop

        //any extra tests or pruning methods
        //NOTE: gaze inference occurs in this function
        var gbs_final = tuneTimeline(gbs);

        return new InferenceTimeline(gbs_final);
    }


    /// <summary>
    /// Infers a start frame given the current fixation start frame, v
    /// </summary>
    /// <param name="v"></param>
    /// <param name="sgi"></param>
    /// <returns></returns>
    private int gazeStartFrame(int v, SubGazeInfo sgi) {
        var prevMax = InferenceUtil.findClosestValueBack(v, sgi.Starts_Max);
        var currMax = sgi.Starts_Mags[prevMax];

        var startFrame = InferenceUtil.findClosestValueBack(prevMax, sgi.Starts_Mins);
        var currMin = sgi.Starts_Mags[startFrame];
        double threshold = 0.25;
        //keep looking back if the min we find is outside of our desired range
        while (currMin < (currMax * 0.25 - threshold) || currMin > (currMax * 0.25 + threshold))
        {
            startFrame = InferenceUtil.findClosestValueBack(prevMax, sgi.Starts_Mins);
            currMin = sgi.Starts_Mags[startFrame];
        }

        return startFrame;
    }

    /// <summary>
    /// Ensures that we are not finding inconsequential minima, i.e. the last maximum should be at least 
    /// a certain multiple times this velocity minimum value.
    /// </summary>
    /// <param name="v"></param>
    /// <param name="sgi"></param>
    /// <returns></returns>
    private bool lastMaxTest(int v, SubGazeInfo sgi) {
        var lastMax = InferenceUtil.findClosestValueBack(v, sgi.Starts_Max);
        if (sgi.Ends_Mags[lastMax] > sgi.LastMaxThreshold * sgi.Ends_Mags[v]) return true;
        else return false;
    }

    /// <summary>
    /// Returns true if the resulting gaze block would be longer than 7.5 frames
    /// </summary>
    /// <param name="start"></param>
    /// <param name="fixationStart"></param>
    /// <returns></returns>
    private bool durationTest(int start, int fixationStart) {
        if (fixationStart - start < 7.5) return false;
        else return true;
    }

    /// <summary>
    /// Runs all available tests on the gazeblocks before returning a final version.  This includes gaze target inference.
    /// </summary>
    /// <param name="gbs"></param>
    /// <returns></returns>
    private List<GazeBlock> tuneTimeline(List<GazeBlock> gbs) {
        //var overlap = overalpCheck(gbs); // intentionally turned off.  Overlaps will be handled by BlockMatching

        List<GazeBlock> tuned = gbs;

        GazeTarget = new GazeTarget(AnimationTitle, CharacterName, gbs);
        if (Targeting) { 
            tuned = GazeTarget.GazeTargetInference();
        }

        return tuned;
    }


    /// <summary>
    /// Fixes overlaps in the inference timeline
    /// </summary>
    /// <param name="gbs"></param>
    /// <returns></returns>
    private List<GazeBlock> overalpCheck(List<GazeBlock> gbs) {
        List<GazeBlock> gbs_ = new List<GazeBlock>();
        gbs_.Add(gbs[0]);
        for (int i = 1; i < gbs.Count; i++)
        {
            if (gbs[i].StartFrame <= gbs[i - 1].FixationStartFrame)
            {
                //new start frame results in too small of a gaze block, do not add
                if (Math.Abs(gbs[i - 1].FixationStartFrame + 3 - gbs[i].FixationStartFrame) < 7.5) continue;
                gbs_.Add(new GazeBlock(gbs[i - 1].FixationStartFrame + 3, gbs[i].FixationStartFrame,
                    gbs[i].CharacterName, gbs[i].AnimationClip, gbs[i].TargetName, gbs[i].Target, gbs[i].TurnBody));
            }
            else
            {
                gbs_.Add(gbs[i]);
            }
        }

        return gbs_;
    }

}

/// <summary>
/// holds key information about inferring gaze shifts in a single location
/// </summary>
public class SubGazeInfo { 
    public AngleData AngleData
    {
        get;
        set;
    }

    //starts
    public List<int> Starts_Mins
    {
        get;
        set;
    }
    public List<int> Starts_Max
    {
        get;
        set;
    } //for maxima
    public List<double> Starts_Mags
    {
        get;
        set;
    }

    //ends
    public List<int> Ends_Mins
    {
        get;
        set;
    }
    public List<int> Ends_Max
    {
        get;
        set;
    } //for lastMaxTest
    public List<double> Ends_Mags
    {
        get;
        set;
    }

    public double LastMaxThreshold
    {
        get;
        set;
    }

    public SubGazeInfo(AngleData ad, List<double> starts_mags, List<double> ends_mags, double lastMaxThreshold = 1.5) { 
        AngleData = ad;
        Starts_Mins = MinMax.Minimize(starts_mags);
        Starts_Max = MinMax.Maximize(starts_mags);
        Starts_Mags = starts_mags;

        Ends_Mins = MinMax.Minimize(ends_mags);
        Ends_Max = MinMax.Maximize(ends_mags);
        Ends_Mags = ends_mags;

        LastMaxThreshold = lastMaxThreshold;
    }

    //alternate constructor where start mags and ends mags are the same
    public SubGazeInfo(AngleData ad, List<double> mags, double lastMaxThreshold = 1.5) : this(ad, mags, mags, lastMaxThreshold) { }


}




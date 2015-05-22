using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Update 5/1/15 latest iteration = 0 (old GazeInferenceTestC class)
/// Update 5/2/15 latest iteration = 1 (added simple targeting)
/// Update 5/12/15 latest iteration = 3 (trying to aggregate all bones along the spine in model)
/// </summary>
public class GazeInference {

    public InferenceTimeline InferenceTimeline
    {
        get;
        private set;
    }

    /// <summary>
    /// Intended for testing and iteration.  
    /// Final version will be an alternate constructor without the iteration option
    /// </summary>
    /// <param name="animationTitle"></param>
    /// <param name="characterName"></param>
    /// <param name="iteration"></param>
    public GazeInference(string animationTitle, string characterName, int iteration) {
        if (iteration == 0) 
        {
            //var i = new GazeInferenceTestC(animationTitle, characterName);
            //InferenceTimeline = i.InferenceTimeline;
        }
        else if (iteration == 1) {
            //var i = new GazeInferenceTestD(animationTitle, characterName);
            //InferenceTimeline = i.InferenceTimeline;
        }
        else if (iteration == 2) {
            //var i = new GazeInferenceTestE(animationTitle, characterName);
            //InferenceTimeline = i.InferenceTimeline;
        }
        else if (iteration == 3) {
            var i = new GazeInferenceTestF(animationTitle, characterName);
            InferenceTimeline = i.InferenceTimeline;
        }
        else if (iteration == 4) {
            var i = new GazeInferenceTestG(animationTitle, characterName);
            InferenceTimeline = i.InferenceTimeline;
        }
        else if (iteration == 5) {
            var i = new GazeInferenceSubTest(animationTitle, characterName);
            InferenceTimeline = i.InferenceTimeline;
        }
        else if (iteration == 6) {
            var ad = new AngleData(animationTitle, characterName);
            var chestMags = Filter.BilateralFilter(ad.Chest_Magnitudes_Root, 17, 3, 0.5);
            var headLocalMags = Filter.BilateralFilter(ad.Head_Magnitudes_HeadL, 17, 3, 0.5);

            var chest = new SubGazeInference(animationTitle, characterName, "GazeInference_Chest.csv", new SubGazeInfo(ad, chestMags));
            var headLocal = new SubGazeInference(animationTitle, characterName, "GazeInference_HeadLocal.csv", new SubGazeInfo(ad, headLocalMags, 2.0));
            var headLocalOrig = new SubGazeInference(animationTitle, characterName, "GazeInference_HeadLocalOrig.csv", new SubGazeInfo(ad, headLocalMags));

            
            var blockMatch = BlockMatchTimeline.MatchTimelines(headLocal.InferenceTimeline, chest.InferenceTimeline, animationTitle, characterName);
            var blockMatchOrig = BlockMatchTimeline.MatchTimelines(headLocalOrig.InferenceTimeline, chest.InferenceTimeline, animationTitle, characterName, false, false);
            blockMatch.TimelineFileOutput(GlobalVars.MatlabPath + "GazeInference_BlockMatch.csv");
            blockMatchOrig.TimelineFileOutput(GlobalVars.MatlabPath + "GazeInference_BlockMatchOrig.csv");

            InferenceTimeline = blockMatch;
            UnityEngine.Debug.Log(blockMatch);

            //scoring
            var ha = new InferenceTimeline(GlobalVars.MatlabPath + animationTitle + ".csv");
            var scorer = new InferenceScorer(blockMatch, ha);
            UnityEngine.Debug.Log(scorer);
        }
        //NOT USING
        else if (iteration == 7) {
            var ad = new AngleData(animationTitle, characterName);
            var chestMags = Filter.BilateralFilter(ad.Chest_Magnitudes_Root, 17, 3, 0.5);
            var headLocalMags = Filter.BilateralFilter(ad.Head_Magnitudes_HeadL, 17, 3, 0.5);
            var rootMags = Filter.BilateralFilter(ad.Hips_Magnitudes_Global, 17, 3, 0.5);

            var chest = new SubGazeInference(animationTitle, characterName, "GazeInference_Chest.csv", new SubGazeInfo(ad, chestMags));
            var headLocal = new SubGazeInference(animationTitle, characterName, "GazeInference_HeadLocal.csv", new SubGazeInfo(ad, headLocalMags, 2.0));
            var hips = new SubGazeInference(animationTitle, characterName, "GazeInference_Hips.csv", new SubGazeInfo(ad, rootMags, 4.0));

            var glue = BlockMatchTimeline.GlueTimelines(headLocal.InferenceTimeline, hips.InferenceTimeline, animationTitle, characterName);
            glue.TimelineFileOutput(GlobalVars.MatlabPath + "GazeInference_Glued.csv");

            var blockMatch = BlockMatchTimeline.MatchTimelines(glue, chest.InferenceTimeline, animationTitle, characterName);
            blockMatch.TimelineFileOutput(GlobalVars.MatlabPath + "GazeInference_BlockMatch.csv");

            InferenceTimeline = blockMatch;
            UnityEngine.Debug.Log(blockMatch);
        }
        else
        {
            UnityEngine.Debug.Log("Invalid iteration value");
        }
    }
   
}

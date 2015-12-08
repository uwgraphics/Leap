using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;


/// <summary>
/// Gaze targeting re-done
/// </summary>
public class GazeInferenceTestF
{

    public InferenceTimeline InferenceTimeline
    {
        get;
        private set;
    }

    public string AnimationTitle
    {
        get;
        set;
    }

    public string CharacterName
    {
        get;
        set;
    }

    public AnimationClipInstance AnimationClip
    {
        get;
        set;
    }

    public InferenceCharacter Character
    {
        get;
        set;
    }

    public SceneCollisions SceneCollisions
    {
        get;
        set;
    }

    private int TagCounter1
    {
        get;
        set;
    }


    private int TagCounter2
    {
        get;
        set;
    }

    //alternate constructor
    public GazeInferenceTestF(string animationTitle, string characterName)
    {
        AnimationTitle = animationTitle;
        CharacterName = characterName;
        Character = new InferenceCharacter(characterName);
        AnimationClip = new AnimationClipInstance(animationTitle, Character.CharModel, true, false);
        SceneCollisions = new SceneCollisions(animationTitle);

        TagCounter1 = 0;
        TagCounter2 = 0;

        string c = "Norman";
        string a = "Test";
        string t = "Target";
        bool b = true;

        //filepath portion
        /////////////////////////

        var ad = new AngleData(animationTitle, characterName);
        var headMags = Filter.BilateralFilter(ad.Head_Magnitudes_Root, 17, 3, 0.5);
        var chestMags = Filter.BilateralFilter(ad.Chest_Magnitudes_Root, 17, 3, 0.5);
        var spineAMags = Filter.BilateralFilter(ad.SpineA_Magnitudes_Root, 17, 3, 0.5);
        var spineBMags = Filter.BilateralFilter(ad.SpineB_Magnitudes_Root, 17, 3, 0.5);

        var headMins = MinMax.Minimize(headMags);
        var chestMins = MinMax.Minimize(chestMags);
        var headMax = MinMax.Maximize(headMags);
        var chestMax = MinMax.Maximize(chestMags);
        var spineAMins = MinMax.Minimize(spineAMags);
        var spineAMax = MinMax.Maximize(spineAMags);
        var spineBMins = MinMax.Minimize(spineBMags);
        var spineBMax = MinMax.Maximize(spineBMags);

        var ta = inferGazeBlocks(ad, chestMins, chestMax, headMins, headMax, headMags, chestMags);
        InferenceTimeline = ta;
        var tb = new InferenceTimeline(GlobalVars.MatlabPath + animationTitle + ".csv");
        
        var scorer = new InferenceScorer(ta, tb);
        UnityEngine.Debug.Log(scorer);
        ta.TimelineFileOutput(GlobalVars.MatlabPath + "GazeInferenceTestF.csv");
    }


    private InferenceTimeline inferGazeBlocks(AngleData ad, List<int> chestMins, List<int> chestMax, List<int> headMins, List<int> headMax, List<double> headMags, List<double> chestMags)
    {
        var gbs = new List<GazeBlock>();

        for (int i = 0; i < chestMins.Count; i++)
        {
            if (!lastMaxTest(chestMins[i], chestMax, chestMags))
            {
                if (!threadTest(chestMins[i], headMins))
                {
                    continue;
                }
            }


            //if it passed the above tests, add a gaze block
            int start = gazeStartFrame(chestMins[i], headMax, headMins, headMags);

            Vector3 fixationPoint;
            string targetName;
            GameObject target;
            gazeTargetInference(out fixationPoint, out targetName, out target, chestMins[i], SceneCollisions);

            ///////////////////////////////////////////////////////////

            //test to make sure gaze block is long enough
            if (chestMins[i] - start < 7.5) continue;

            // TODO: will need separate target deduction and turn body inference here
            gbs.Add(new GazeBlock(start, chestMins[i], ad.Character.CharName, ad.AnimationName, targetName, target, true, fixationPoint));

        }

        //adjust start times by overlap testing

        //runs various tests that fixes the list of gaze shifts
        var gbs_final = tuneGazeShifts(gbs);

        //TODO: function that adds GameObjects parented to gaze objects/ points in the scene
        addSceneGazePoints(gbs);

        return new InferenceTimeline(gbs_final);
    }




    /// <summary>
    /// Adds GameObjects to the scene corresponding to their inferred target locations
    /// If the inferred target is in empty space, i.e. not labeled as a scene object, the 
    /// GameObject will be parented to the environment itself
    /// </summary>
    /// <param name="gbs"></param>
    private void addSceneGazePoints(List<GazeBlock> gbs) {
        GameObject[] targets = GameObject.FindGameObjectsWithTag("GazeTarget");
        
        //will hold the count of each object in the scene, for labeling purposes
        var counterArray = new List<int>();

        //dictionary for mappings between indicies in the counter and names of scene objects
        var d = new Dictionary<string, int>();

        for(int i = 0; i < SceneCollisions.SceneObjects.Count; i++) {
            d.Add(SceneCollisions.SceneObjects[i].ObjectName, i);
            counterArray.Add(0);
        }
            
        //add a separate entry for the scene environment itself
        d.Add("", SceneCollisions.SceneObjects.Count);
        counterArray.Add(0);

        //go through each gaze block, and add an appropriate gaze point in the scene
        foreach (var g in gbs) {

            //this means the target should be parented to the scene
            if (g.TargetName == "")
            {
                var currEnvironement = AnimationManager.Instance.Environment;
                UnityEngine.Debug.Log(currEnvironement);

                var targetTest = targets.FirstOrDefault(m => m.name == currEnvironement.name + "_" + counterArray[d[""]].ToString());
                if (targetTest != null) {
                    g.Target = targetTest;
                    continue;
                }

                GameObject n = new GameObject();
                n.tag = "GazeTarget";
                n.name = currEnvironement.name + "_" + counterArray[d[""]].ToString();
                n.transform.parent = currEnvironement.transform;
                n.transform.position = g.FixationPoint;
                g.Target = n;

                //increment counter for the scene environment
                counterArray[d[""]]++;
            } 
            //otherwise, the target should be parented to the target itself
            else 
            {
                var targetTest = targets.FirstOrDefault(m => m.name == g.TargetName + "_" + counterArray[d[g.TargetName]].ToString());
                if (targetTest != null)
                {
                    g.Target = targetTest;
                    continue;
                }

                GameObject n = new GameObject();
                n.tag = "GazeTarget";
                n.name = g.TargetName + "_" + counterArray[d[g.TargetName]].ToString();
                n.transform.parent = g.Target.transform;
                n.transform.position = g.FixationPoint;
                g.Target = n; //reset target to be this new game object

                //increment counter for the scene environment
                counterArray[d[g.TargetName]]++;
            }
            //GameObject n = new GameObject();
            //n.tag = "GazeTarget";
            //n.name = currEnvironement.name + TagCounter1.ToString();
            //TagCounter1++;
            //n.transform.parent = currEnvironement.transform;
            //n.transform.position = g.FixationPoint;
        }
    }

    /// <summary>
    /// Will finalize list of gaze blocks
    /// Right now, only checks for overlaps, but can check for more later
    /// </summary>
    /// <param name="gbs"></param>
    /// <returns></returns>
    private List<GazeBlock> tuneGazeShifts(List<GazeBlock> gbs) {
        var gbs_tuned = new List<GazeBlock>();
        gbs_tuned = overlapCheck(gbs);
        return gbs_tuned;
    }

    private List<GazeBlock> pruneGazeShifts(List<GazeBlock> gbs, List<int> chestMins, List<int> headMax, List<double> headMags)
    {
        var gbs_ = new List<GazeBlock>();

        double threshold = 0.017;

        for (int i = 0; i < gbs.Count; i++) {
            var currChestMinFrame = gbs[i].FixationStartFrame;
            var prevHeadMaxFrame = findClosestValueBack(currChestMinFrame - 3, headMax);
            var prevHeadMax = headMags[prevHeadMaxFrame];
            if (!(prevHeadMax > threshold)) continue;

            gbs_.Add(gbs[i]);
        }

        return gbs_;
    }


    /// <summary>
    /// Checks and fixes overlaps of gaze blocks
    /// </summary>
    /// <param name="gbs"></param>
    /// <returns></returns>
    private List<GazeBlock> overlapCheck(List<GazeBlock> gbs) {
        List<GazeBlock> gbs_ = new List<GazeBlock>();
        gbs_.Add(gbs[0]);
        for (int i = 1; i < gbs.Count; i++) {
            if (gbs[i].StartFrame <= gbs[i - 1].FixationStartFrame)
            {
                //new start frame results in too small of a gaze block, do not add
                if (   Math.Abs(gbs[i - 1].FixationStartFrame + 3 - gbs[i].FixationStartFrame) < 7.5  ) continue;
                gbs_.Add(new GazeBlock(gbs[i - 1].FixationStartFrame + 3, gbs[i].FixationStartFrame,
                    gbs[i].CharacterName, gbs[i].AnimationClip, gbs[i].TargetName, gbs[i].Target, gbs[i].TurnBody));
            }
            else {
                gbs_.Add(gbs[i]);
            }
        }

        return gbs_;
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="fixationPoint"></param>
    /// <param name="targetName"></param>
    /// <param name="frame"></param>
    /// <param name="sc"></param>
    private void gazeTargetInference(out Vector3 fixationPoint, out string targetName, out GameObject target, int frame, SceneCollisions sc)
    {
        Transform headTransform;

        AnimationClip.Animation[AnimationTitle].normalizedTime = Mathf.Clamp01(((float)frame) / AnimationClip.FrameLength);
        AnimationClip.Animation[AnimationTitle].weight = AnimationClip.Weight;
        AnimationClip.Animation[AnimationTitle].enabled = true;

        AnimationClip.Animation.Sample();
        //////////////////////////////////////////////////////////////
        headTransform = Character.HeadBone.transform;
        //////////////////////////////////////////////////////////////
        AnimationClip.Animation[AnimationTitle].enabled = false;

        Vector3 fp;
        string tn;
        GameObject t;
        //max distance for forward head vector
        float headDrawDistance = 100.0f;

        //findTarget function takes care of looping through the scene collisions to find the point and target
        findTarget(frame, out fp, out tn, out t, sc, headTransform.position,
            headTransform.position + (headTransform.forward.normalized * headDrawDistance), headTransform.forward.normalized);

        fixationPoint = fp;
        targetName = tn;
        target = t;
    }

    /// <summary>
    /// Goes through scene collision objects and returns the point in space being looked at and the name of that target object
    /// In this case v1 and v2 should be the start and end points of the nose vector (calculated in the gazeTargetInference function above)
    /// </summary>
    /// <param name="fixationPoint"></param>
    /// <param name="targetName"></param>
    /// <param name="sc"></param>
    /// <param name="v1"></param>
    /// <param name="v2"></param>
    private void findTarget(int frame, out Vector3 fixationPoint, out string targetName, out GameObject target, SceneCollisions sc, Vector3 hv1, Vector3 hv2, Vector3 forward)
    {
        float minDis = float.MaxValue;
        Vector3 minDisVec = Vector3.zero;
        Vector3 minDisHeadVec = Vector3.zero;
        string minDisTarget = "";
        GameObject minDisTargetObj = null;


        var sceneObjects = sc.SceneObjects;

        //loop through all scene objects in the scene
        for (int i = 0; i < sceneObjects.Count; i++)
        {

            var ti = sceneObjects[i].TriangleIndices;
            var v = sceneObjects[i].Vertices;

            //loop through all indicies in the triangle array to get all of the edges
            for (int j = 0; j < ti.Length; j += 3)
            {
                Vector3 vert1 = v[ti[j]];
                Vector3 vert2 = v[ti[j + 1]];
                Vector3 vert3 = v[ti[j + 2]];

                //judge distances on all edges
                Vector3 pt1;
                Vector3 pt2;
                float dist;
                //edge 1-2
                edgeDistanceTest(hv1, hv2, vert1, vert2, out pt1, out pt2, out dist);
                if (dist < minDis)
                {
                    minDis = dist;
                    minDisVec = pt1;
                    minDisHeadVec = pt2;
                    minDisTarget = sceneObjects[i].ObjectName;
                    minDisTargetObj = sceneObjects[i].GameObject;
                }
                //edge 2-3
                edgeDistanceTest(hv1, hv2, vert2, vert3, out pt1, out pt2, out dist);
                if (dist < minDis)
                {
                    minDis = dist;
                    minDisVec = pt1;
                    minDisHeadVec = pt2;
                    minDisTarget = sceneObjects[i].ObjectName;
                    minDisTargetObj = sceneObjects[i].GameObject;
                }
                //edge 3-1
                edgeDistanceTest(hv1, hv2, vert3, vert1, out pt1, out pt2, out dist);
                if (dist < minDis)
                {
                    minDis = dist;
                    minDisVec = pt1;
                    minDisHeadVec = pt2;
                    minDisTarget = sceneObjects[i].ObjectName;
                    minDisTargetObj = sceneObjects[i].GameObject;
                }
            }
        }

        //how far in front of nose to draw auto gaze point if threshold distance is exceeded
        //this is achieved by scaling down the forward vector parameter 
        float autoScale = 10.0f;

        //distance between closest point on head vector and head itself.  This will act as a scaling factor for the target cone.
        float disFromHead = Vector3.Distance(minDisHeadVec, hv1);
        float distanceThreshold = 0.15f * (float)Math.Pow(disFromHead, 2);

        fixationPoint = minDis > distanceThreshold ? hv1 + forward * autoScale : minDisVec;
        targetName = minDis > distanceThreshold ? "" : minDisTarget;
        target = minDis > distanceThreshold ? null : minDisTargetObj;
    }

    /// <summary>
    /// utility function for checking distance from nose vector to one edge in a mesh triangle
    /// </summary>
    /// <param name="u1"></param>
    /// <param name="v1"></param>
    /// <param name="u2"></param>
    /// <param name="v2"></param>
    /// <param name="ut"></param>
    /// <param name="vt"></param>
    private void edgeDistanceTest(Vector3 hv1, Vector3 hv2, Vector3 vert1, Vector3 vert2, out Vector3 pt1, out Vector3 pt2, out float dist)
    {
        float ut;
        float vt;
        GeomUtil.ClosestPointsOn2Lines(hv1, hv2, vert1, vert2, out ut, out vt);
        if (vt < 0) pt1 = vert1;
        else if (vt > 1) pt1 = vert2;
        else pt1 = vert1 + vt * vert2;
        if (ut < 0) pt2 = hv1;
        else if (ut > 1) pt2 = hv2;
        else pt2 = hv1 + ut * hv2;

        dist = Vector3.Distance(pt1, pt2);
    }

    //returns true ONLY if there is a minimum in the head velocity in the very near vicinity of the current minimum
    //in the chest velocity.  Currently, I am using a window of 3 frames on either side.
    private bool threadTest(int v, List<int> headMinima)
    {
        int back = findClosestValueBack(v, headMinima);
        int forward = findClosestValueForward(v, headMinima);
        int window = 4;
        if (forward - v < window || v - back < window) return true;
        else return false;
    }

    private bool lastMaxTest(int v, List<int> maxima, List<double> chestMags)
    {
        var lastMax = findClosestValueBack(v, maxima);
        if (chestMags[lastMax] > 2.5 * chestMags[v]) return true;
        else return false;
    }

    private int findClosestValueBack(int v, List<int> list)
    {
        if (v < list[0]) return v;
        if (list.Count == 1) return list[0];

        int center = list.Count / 2;
        if (list[center] > v) return findClosestValueBack(v, list.GetRange(0, center));
        else return findClosestValueBack(v, list.GetRange(center, list.Count - center));

    }

    private int findClosestValueForward(int v, List<int> list)
    {
        if (v > list[list.Count - 1]) return v;
        if (list.Count == 1) return list[0];
        if (list.Count == 2) return list[0] > v ? list[0] : list[1];

        int center = list.Count / 2;
        if (v > list[center]) return findClosestValueForward(v, list.GetRange(center + 1, list.Count - center - 1));
        else return findClosestValueForward(v, list.GetRange(0, center + 1));
    }

    private int gazeStartFrame(int endFrame, List<int> headMax, List<int> headMins, List<double> headMags)
    {
        var prevHeadMax = findClosestValueBack(endFrame, headMax);
        //don't want the last max if it ended up being too close in the head velocities
        if (endFrame - prevHeadMax <= 5) prevHeadMax = findClosestValueBack(endFrame - 6, headMax);

        var currMax = headMags[prevHeadMax];

        var startFrame = findClosestValueBack(prevHeadMax, headMins);
        var currMin = headMags[startFrame];
        double threshold = 0.25;
        while (currMin < (currMax * 0.25 - threshold) || currMin > (currMax * 0.25 + threshold)) {
            startFrame = findClosestValueBack(startFrame, headMins);
            currMin = headMags[startFrame];
        }

        return startFrame;
    }

}

/// <summary>
/// Target aggregation
/// </summary>
public class GazeInferenceTestG
{

    public InferenceTimeline InferenceTimeline
    {
        get;
        private set;
    }

    public string AnimationTitle
    {
        get;
        set;
    }

    public string CharacterName
    {
        get;
        set;
    }

    public AnimationClipInstance AnimationClip
    {
        get;
        set;
    }

    public InferenceCharacter Character
    {
        get;
        set;
    }

    public SceneCollisions SceneCollisions
    {
        get;
        set;
    }

    public GameObject[] GazeTargetObjects
    {
        get;
        set;
    }

    private int BlockCounter
    {
        get;
        set;
    }

    public List<GazeBlock> GazeBlockFinal
    {
        get;
        set;
    }



    //alternate constructor
    public GazeInferenceTestG(string animationTitle, string characterName)
    {
        AnimationTitle = animationTitle;
        CharacterName = characterName;
        Character = new InferenceCharacter(characterName);
        AnimationClip = new AnimationClipInstance(animationTitle, Character.CharModel, true, false);
        SceneCollisions = new SceneCollisions(animationTitle);
        GazeTargetObjects = GameObject.FindGameObjectsWithTag("GazeTarget");

        BlockCounter = 1;

        string c = "Norman";
        string a = "Test";
        string t = "Target";
        bool b = true;


        var ad = new AngleData(animationTitle, characterName);
        var headMags = Filter.BilateralFilter(ad.Head_Magnitudes_Root, 17, 3, 0.5);
        var chestMags = Filter.BilateralFilter(ad.Chest_Magnitudes_Root, 17, 3, 0.5);
        var spineAMags = Filter.BilateralFilter(ad.SpineA_Magnitudes_Root, 17, 3, 0.5);
        var spineBMags = Filter.BilateralFilter(ad.SpineB_Magnitudes_Root, 17, 3, 0.5);

        var headMins = MinMax.Minimize(headMags);
        var chestMins = MinMax.Minimize(chestMags);
        var headMax = MinMax.Maximize(headMags);
        var chestMax = MinMax.Maximize(chestMags);
        var spineAMins = MinMax.Minimize(spineAMags);
        var spineAMax = MinMax.Maximize(spineAMags);
        var spineBMins = MinMax.Minimize(spineBMags);
        var spineBMax = MinMax.Maximize(spineBMags);

        var ta = inferGazeBlocks(ad, chestMins, chestMax, headMins, headMax, headMags, chestMags);
        InferenceTimeline = ta;
        var tb = new InferenceTimeline(GlobalVars.MatlabPath + animationTitle + ".csv");

        var scorer = new InferenceScorer(ta, tb);
        UnityEngine.Debug.Log(scorer);
        ta.TimelineFileOutput(GlobalVars.MatlabPath + "GazeInferenceTestG.csv");
    }


    private InferenceTimeline inferGazeBlocks(AngleData ad, List<int> chestMins, List<int> chestMax, List<int> headMins, List<int> headMax, List<double> headMags, List<double> chestMags)
    {
        var gbs = new List<GazeBlock>();

        for (int i = 0; i < chestMins.Count; i++)
        {
            if (!lastMaxTest(chestMins[i], chestMax, chestMags))
            {
                if (!threadTest(chestMins[i], headMins))
                {
                    continue;
                }
            }


            //if it passed the above tests, add a gaze block
            int start = gazeStartFrame(chestMins[i], headMax, headMins, headMags);

            Vector3 fixationPoint;
            string targetName;
            GameObject target;
            gazeTargetInference(out fixationPoint, out targetName, out target, chestMins[i], SceneCollisions);

            ///////////////////////////////////////////////////////////////

            //test to make sure gaze block is long enough
            if (chestMins[i] - start < 7.5) continue;

            // TODO: will need separate target deduction and turn body inference here
            gbs.Add(new GazeBlock(start, chestMins[i], ad.Character.CharName, ad.AnimationName, targetName, target, true, fixationPoint));

        }

        //adjust start times by overlap testing

        //runs various tests that fixes the list of gaze shifts
        var gbs_tuned = tuneGazeShifts(gbs);

        //add Gaze + count to gaze blocks
        foreach (var g in gbs_tuned)
        {
            g.AnimationClip = g.AnimationClip + "Gaze" + BlockCounter.ToString();
            BlockCounter = BlockCounter + 1;
        }

        //TODO: function that adds GameObjects parented to gaze objects/ points in the scene
        var gbs_final = addSceneGazePoints(gbs_tuned);

        return new InferenceTimeline(gbs_final);
    }

    /// <summary>
    /// Adds GameObjects to the scene corresponding to their inferred target locations
    /// If the inferred target is in empty space, i.e. not labeled as a scene object, the 
    /// GameObject will be parented to the environment itself
    /// </summary>
    /// <param name="gbs"></param>
    private List<GazeBlock> addSceneGazePoints(List<GazeBlock> gbs)
    {
        List<GazeBlock> _gbs = new List<GazeBlock>();

        GameObject[] targets = GameObject.FindGameObjectsWithTag("GazeTarget");

        //will hold the count of each object in the scene, for labeling purposes
        var counterArray = new List<int>();

        //dictionary for mappings between indicies in the counter and names of scene objects
        var d = new Dictionary<string, int>();

        for (int i = 0; i < SceneCollisions.SceneObjects.Count; i++)
        {
            d.Add(SceneCollisions.SceneObjects[i].ObjectName, i);
            counterArray.Add(0);
        }

        //add a separate entry for the scene environment itself
        d.Add("", SceneCollisions.SceneObjects.Count);
        counterArray.Add(0);

        //go through each gaze block, and add an appropriate gaze point in the scene
        foreach (var g in gbs)
        {

            // TODO: Add target aggregation:

            //this means the target should be parented to the scene
            if (g.TargetName == "")
            {
                var currEnvironement = AnimationManager.Instance.Environment;

                var targetTest = targets.FirstOrDefault(m => m.name == currEnvironement.name + "_" + d[""].ToString());
                if (targetTest != null)
                {
                    g.Target = targetTest;
                    _gbs.Add(new GazeBlock(g.StartFrame, g.FixationStartFrame, g.CharacterName, g.AnimationClip, g.TargetName, targetTest, g.TurnBody));
                    continue;
                }

                //distance check
                var closeOb = aggregationDisCheck(g.FixationPoint, true);

                if (closeOb == null)
                {
                    GameObject n = new GameObject();
                    n.tag = "GazeTarget";
                    n.name = currEnvironement.name + "_" + counterArray[d[""]].ToString();
                    n.transform.parent = currEnvironement.transform;
                    n.transform.position = g.FixationPoint;
                    g.Target = n;

                    //increment counter for the scene environment
                    counterArray[d[""]]++;
                }
                else g.Target = closeOb;
               
            }
            //otherwise, the target should be parented to the target itself
            else
            {
                var targetTest = targets.FirstOrDefault(m => m.name == g.TargetName + "_" + counterArray[d[g.TargetName]].ToString());
                if (targetTest != null)
                {
                    g.Target = targetTest;
                    _gbs.Add(new GazeBlock(g.StartFrame, g.FixationStartFrame, g.CharacterName, g.AnimationClip, g.TargetName, targetTest, g.TurnBody));
                    continue;
                }

                //distance check
                var closeOb = aggregationDisCheck(g.FixationPoint, false);

                if (closeOb == null)
                {
                    GameObject n = new GameObject();
                    n.tag = "GazeTarget";
                    n.name = g.TargetName + "_" + counterArray[d[g.TargetName]].ToString();
                    n.transform.parent = g.Target.transform;
                    n.transform.position = g.FixationPoint;
                    g.Target = n; //reset target to be this new game object

                    //increment counter for the scene environment
                    counterArray[d[g.TargetName]]++;
                }
                else g.Target = closeOb;    
            }

            _gbs.Add(new GazeBlock(g.StartFrame, g.FixationStartFrame, g.CharacterName, g.AnimationClip, g.TargetName, g.Target, g.TurnBody));
        }

        return _gbs;

    }

    /// <summary>
    /// checks distance against the other already created 
    /// </summary>
    /// <param name="fixationPoint"></param>
    /// <returns></returns>    
    private GameObject aggregationDisCheck(Vector3 fixationPoint, bool spacePoint) {
        GazeTargetObjects = GameObject.FindGameObjectsWithTag("GazeTarget");
        double distanceThreshold = spacePoint ? 7.0 : 1.5;

        double minDis = double.MaxValue;
        GameObject minDisObj = null;

        for (int i = 0; i < GazeTargetObjects.Length; i++) { 
            var curr = GazeTargetObjects[i];
            UnityEngine.Debug.Log(curr);
            //check to see if this is a gaze helper object.  If it is, ignore it.
            var obName = GazeTargetObjects[i].name;
            if (!(obName.Length < 6)) { 
                var l = obName.Length;
                if (curr.name.Substring(l - 6) == "Helper") continue;
            }

            var dis = Vector3.Distance(fixationPoint, curr.transform.position);
            if (dis < minDis) {
                minDis = dis;
                minDisObj = curr;
            }
        }

        if (minDis < distanceThreshold) return minDisObj;

        return null;
    }

    /// <summary>
    /// Will finalize list of gaze blocks
    /// Right now, only checks for overlaps, but can check for more later
    /// </summary>
    /// <param name="gbs"></param>
    /// <returns></returns>
    private List<GazeBlock> tuneGazeShifts(List<GazeBlock> gbs)
    {
        var gbs_tuned = new List<GazeBlock>();
        gbs_tuned = overlapCheck(gbs);
        return gbs_tuned;
    }

    private List<GazeBlock> pruneGazeShifts(List<GazeBlock> gbs, List<int> chestMins, List<int> headMax, List<double> headMags)
    {
        var gbs_ = new List<GazeBlock>();

        double threshold = 0.017;

        for (int i = 0; i < gbs.Count; i++)
        {
            var currChestMinFrame = gbs[i].FixationStartFrame;
            var prevHeadMaxFrame = findClosestValueBack(currChestMinFrame - 3, headMax);
            var prevHeadMax = headMags[prevHeadMaxFrame];
            if (!(prevHeadMax > threshold)) continue;

            gbs_.Add(gbs[i]);
        }

        return gbs_;
    }


    /// <summary>
    /// Checks and fixes overlaps of gaze blocks
    /// </summary>
    /// <param name="gbs"></param>
    /// <returns></returns>
    private List<GazeBlock> overlapCheck(List<GazeBlock> gbs)
    {
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




    /// <summary>
    /// 
    /// </summary>
    /// <param name="fixationPoint"></param>
    /// <param name="targetName"></param>
    /// <param name="frame"></param>
    /// <param name="sc"></param>
    private void gazeTargetInference(out Vector3 fixationPoint, out string targetName, out GameObject target, int frame, SceneCollisions sc)
    {
        Transform headTransform;

        AnimationClip.Animation[AnimationTitle].normalizedTime = Mathf.Clamp01(((float)frame) / AnimationClip.FrameLength);
        AnimationClip.Animation[AnimationTitle].weight = AnimationClip.Weight;
        AnimationClip.Animation[AnimationTitle].enabled = true;

        AnimationClip.Animation.Sample();
        //////////////////////////////////////////////////////////////
        headTransform = Character.HeadBone.transform;
        //////////////////////////////////////////////////////////////
        AnimationClip.Animation[AnimationTitle].enabled = false;

        Vector3 fp;
        string tn;
        GameObject t;
        //max distance for forward head vector
        float headDrawDistance = 100.0f;

        //findTarget function takes care of looping through the scene collisions to find the point and target
        findTarget(frame, out fp, out tn, out t, sc, headTransform.position,
            headTransform.position + (headTransform.forward.normalized * headDrawDistance), headTransform.forward.normalized);

        fixationPoint = fp;
        targetName = tn;
        target = t;
    }

    /// <summary>
    /// Goes through scene collision objects and returns the point in space being looked at and the name of that target object
    /// In this case v1 and v2 should be the start and end points of the nose vector (calculated in the gazeTargetInference function above)
    /// </summary>
    /// <param name="fixationPoint"></param>
    /// <param name="targetName"></param>
    /// <param name="sc"></param>
    /// <param name="v1"></param>
    /// <param name="v2"></param>
    private void findTarget(int frame, out Vector3 fixationPoint, out string targetName, out GameObject target, SceneCollisions sc, Vector3 hv1, Vector3 hv2, Vector3 forward)
    {
        float minDis = float.MaxValue;
        Vector3 minDisVec = Vector3.zero;
        Vector3 minDisHeadVec = Vector3.zero;
        string minDisTarget = "";
        GameObject minDisTargetObj = null;


        var sceneObjects = sc.SceneObjects;

        //loop through all scene objects in the scene
        for (int i = 0; i < sceneObjects.Count; i++)
        {

            var ti = sceneObjects[i].TriangleIndices;
            var v = sceneObjects[i].Vertices;

            //loop through all indicies in the triangle array to get all of the edges
            for (int j = 0; j < ti.Length; j += 3)
            {
                Vector3 vert1 = v[ti[j]];
                Vector3 vert2 = v[ti[j + 1]];
                Vector3 vert3 = v[ti[j + 2]];

                //judge distances on all edges
                Vector3 pt1;
                Vector3 pt2;
                float dist;
                //edge 1-2
                edgeDistanceTest(hv1, hv2, vert1, vert2, out pt1, out pt2, out dist);
                if (dist < minDis)
                {
                    minDis = dist;
                    minDisVec = pt1;
                    minDisHeadVec = pt2;
                    minDisTarget = sceneObjects[i].ObjectName;
                    minDisTargetObj = sceneObjects[i].GameObject;
                }
                //edge 2-3
                edgeDistanceTest(hv1, hv2, vert2, vert3, out pt1, out pt2, out dist);
                if (dist < minDis)
                {
                    minDis = dist;
                    minDisVec = pt1;
                    minDisHeadVec = pt2;
                    minDisTarget = sceneObjects[i].ObjectName;
                    minDisTargetObj = sceneObjects[i].GameObject;
                }
                //edge 3-1
                edgeDistanceTest(hv1, hv2, vert3, vert1, out pt1, out pt2, out dist);
                if (dist < minDis)
                {
                    minDis = dist;
                    minDisVec = pt1;
                    minDisHeadVec = pt2;
                    minDisTarget = sceneObjects[i].ObjectName;
                    minDisTargetObj = sceneObjects[i].GameObject;
                }
            }
        }

        //how far in front of nose to draw auto gaze point if threshold distance is exceeded
        //this is achieved by scaling down the forward vector parameter 
        float autoScale = 10.0f;

        //distance between closest point on head vector and head itself.  This will act as a scaling factor for the target cone.
        float disFromHead = Vector3.Distance(minDisHeadVec, hv1);
        float distanceThreshold = 0.15f * (float)Math.Pow(disFromHead, 2);

        fixationPoint = minDis > distanceThreshold ? hv1 + forward * autoScale : minDisVec;
        targetName = minDis > distanceThreshold ? "" : minDisTarget;
        target = minDis > distanceThreshold ? null : minDisTargetObj;
    }

    /// <summary>
    /// utility function for checking distance from nose vector to one edge in a mesh triangle
    /// </summary>
    /// <param name="u1"></param>
    /// <param name="v1"></param>
    /// <param name="u2"></param>
    /// <param name="v2"></param>
    /// <param name="ut"></param>
    /// <param name="vt"></param>
    private void edgeDistanceTest(Vector3 hv1, Vector3 hv2, Vector3 vert1, Vector3 vert2, out Vector3 pt1, out Vector3 pt2, out float dist)
    {
        float ut;
        float vt;
        GeomUtil.ClosestPointsOn2Lines(hv1, hv2, vert1, vert2, out ut, out vt);
        if (vt < 0) pt1 = vert1;
        else if (vt > 1) pt1 = vert2;
        else pt1 = vert1 + vt * vert2;
        if (ut < 0) pt2 = hv1;
        else if (ut > 1) pt2 = hv2;
        else pt2 = hv1 + ut * hv2;

        dist = Vector3.Distance(pt1, pt2);
    }

    //returns true ONLY if there is a minimum in the head velocity in the very near vicinity of the current minimum
    //in the chest velocity.  Currently, I am using a window of 3 frames on either side.
    private bool threadTest(int v, List<int> headMinima)
    {
        int back = findClosestValueBack(v, headMinima);
        int forward = findClosestValueForward(v, headMinima);
        int window = 4;
        if (forward - v < window || v - back < window) return true;
        else return false;
    }

    private bool lastMaxTest(int v, List<int> maxima, List<double> chestMags)
    {
        var lastMax = findClosestValueBack(v, maxima);
        if (chestMags[lastMax] > 2.5 * chestMags[v]) return true;
        else return false;
    }

    private int findClosestValueBack(int v, List<int> list)
    {
        if (v < list[0]) return v;
        if (list.Count == 1) return list[0];

        int center = list.Count / 2;
        if (list[center] > v) return findClosestValueBack(v, list.GetRange(0, center));
        else return findClosestValueBack(v, list.GetRange(center, list.Count - center));

    }

    private int findClosestValueForward(int v, List<int> list)
    {
        if (v > list[list.Count - 1]) return v;
        if (list.Count == 1) return list[0];
        if (list.Count == 2) return list[0] > v ? list[0] : list[1];

        int center = list.Count / 2;
        if (v > list[center]) return findClosestValueForward(v, list.GetRange(center + 1, list.Count - center - 1));
        else return findClosestValueForward(v, list.GetRange(0, center + 1));
    }

    private int gazeStartFrame(int endFrame, List<int> headMax, List<int> headMins, List<double> headMags)
    {
        var prevHeadMax = findClosestValueBack(endFrame, headMax);
        //don't want the last max if it ended up being too close in the head velocities
        if (endFrame - prevHeadMax <= 5) prevHeadMax = findClosestValueBack(endFrame - 6, headMax);

        var currMax = headMags[prevHeadMax];

        var startFrame = findClosestValueBack(prevHeadMax, headMins);
        var currMin = headMags[startFrame];
        double threshold = 0.25;
        while (currMin < (currMax * 0.25 - threshold) || currMin > (currMax * 0.25 + threshold))
        {
            startFrame = findClosestValueBack(startFrame, headMins);
            currMin = headMags[startFrame];
        }

        return startFrame;
    }

}

public class GazeInferenceSubTest {
    public InferenceTimeline InferenceTimeline { get; set; }

    //constructor
    public GazeInferenceSubTest(string animationTitle, string characterName) { 
        var ad = new AngleData(animationTitle, characterName);
        var headMags = Filter.BilateralFilter(ad.Head_Magnitudes_Root, 17, 3, 0.5);
        var chestMags = Filter.BilateralFilter(ad.Chest_Magnitudes_Root, 17, 3, 0.5);
        var spineAMags = Filter.BilateralFilter(ad.SpineA_Magnitudes_Root, 17, 3, 0.5);
        var spineBMags = Filter.BilateralFilter(ad.SpineB_Magnitudes_Root, 17, 3, 0.5);
        var hipsMags = Filter.BilateralFilter(ad.Hips_Magnitudes_Global, 17, 3, 0.5);
        var headLocalMags = Filter.BilateralFilter(ad.Head_Magnitudes_HeadL, 17, 3, 0.5);

        var head = new SubGazeInference(animationTitle, characterName, "GazeInference_Head.csv", new SubGazeInfo(ad, headMags));
        var chest = new SubGazeInference(animationTitle, characterName, "GazeInference_Chest.csv", new SubGazeInfo(ad, chestMags));
        var spineA = new SubGazeInference(animationTitle, characterName, "GazeInference_SpineA.csv", new SubGazeInfo(ad, spineAMags));
        var spineB = new SubGazeInference(animationTitle, characterName, "GazeInference_SpineB.csv", new SubGazeInfo(ad, spineBMags));
        var hips = new SubGazeInference(animationTitle, characterName, "GazeInference_Hips.csv", new SubGazeInfo(ad, hipsMags));
        var hips2 = new SubGazeInference(animationTitle, characterName, "GazeInference_Hips4_0.csv", new SubGazeInfo(ad, hipsMags, 4.0));
        var headLocal = new SubGazeInference(animationTitle, characterName, "GazeInference_HeadLocal.csv", new SubGazeInfo(ad, headLocalMags));
        var headLocal2 = new SubGazeInference(animationTitle, characterName, "GazeInference_HeadLocal4_0.csv", new SubGazeInfo(ad, headLocalMags, 4.0));
        var headLocal3 = new SubGazeInference(animationTitle, characterName, "GazeInference_HeadLocal3_5.csv", new SubGazeInfo(ad, headLocalMags, 2.2));
        var blockMatch = BlockMatchTimeline.MatchTimelines(headLocal.InferenceTimeline, chest.InferenceTimeline, animationTitle, characterName);        

        blockMatch.TimelineFileOutput(GlobalVars.MatlabPath + "GazeInference_BlockMatch.csv");
        var g = new GazeInferenceTestG(animationTitle, characterName);

        InferenceTimeline = head.InferenceTimeline;

    }
}







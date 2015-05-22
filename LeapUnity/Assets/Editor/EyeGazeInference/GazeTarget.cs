using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;

public class GazeTarget  {
    private SceneCollisions SceneCollisions { get; set; }
    private GameObject[] GazeTargetObjects { get; set; }
    private List<GazeBlock> GazeBlocks { get; set; }
    private AnimationClipInstance AnimationClip { get; set; }
    private InferenceCharacter Character { get; set; }
    private string AnimationTitle { get; set; }
    private string CharacterName { get; set; }

    /// <summary>
    /// Adds gaze targets to the specified GazeBlocks
    /// </summary>
    /// <param name="animationName"></param>
    /// <param name="characterName"></param>
    /// <param name="gbs"></param>
    /// <returns></returns>
    public GazeTarget(string animationTitle, string characterName, List<GazeBlock> gbs) {
        AnimationTitle = animationTitle;
        CharacterName = characterName;
        SceneCollisions = new SceneCollisions(animationTitle);
        GazeTargetObjects = GameObject.FindGameObjectsWithTag("GazeTarget");
        Character = new InferenceCharacter(characterName);
        AnimationClip = new AnimationClipInstance(Character.CharModel, animationTitle);
        GazeBlocks = gbs;
    }


    public List<GazeBlock> GazeTargetInference() {

        var gbs_ = new List<GazeBlock>();

        //main for loop that goes through each gaze block and infers a gaze target
        for (int i = 0; i < GazeBlocks.Count; i++) {
            var gb = GazeBlocks[i];
            int frame = gb.FixationStartFrame;

            Transform headTransform;

            AnimationClip.Animation[AnimationTitle].normalizedTime = Mathf.Clamp01(((float)frame) / AnimationClip.FrameLength);
            AnimationClip.Animation[AnimationTitle].weight = AnimationClip.Weight;
            AnimationClip.Animation[AnimationTitle].enabled = true;

            AnimationClip.Animation.Sample();
            //////////////////////////////////////////////////////////////
            headTransform = Character.HeadBone.transform;
            //////////////////////////////////////////////////////////////
            AnimationClip.Animation[AnimationTitle].enabled = false;

            Vector3 fp; //fixation point
            string tn; // target name
            GameObject t; // target
            //max distance for forward head vector
            float headDrawDistance = 100.0f;

            //findTarget function takes care of looping through the scene collisions to find the point and target
            findTarget(frame, out fp, out tn, out t, SceneCollisions, headTransform.position,
                headTransform.position + (headTransform.forward.normalized * headDrawDistance), headTransform.forward.normalized);

            gbs_.Add(new GazeBlock(gb.StartFrame, gb.FixationStartFrame, gb.CharacterName, gb.AnimationClip, tn, t, gb.TurnBody, fp));

        } // end of for loop

        GazeBlocks = gbs_;

        return gbs_;
    }


    public List<GazeBlock> AddScenePoints() {
        var gbs = GazeBlocks;
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
                var currEnvironement = AnimationTimeline.Instance.Environment;

                var targetTest = targets.FirstOrDefault(m => m.name == currEnvironement.name + "_" + counterArray[d[""]].ToString());
                if (targetTest != null)
                {
                    _gbs.Add(new GazeBlock(g.StartFrame, g.FixationStartFrame, g.CharacterName, g.AnimationClip, g.TargetName, targetTest, g.TurnBody, g.FixationPoint));
                    counterArray[d[""]]++;
                    continue;
                }

                GameObject n = new GameObject();
                n.tag = "GazeTarget";
                n.name = currEnvironement.name + "_" + counterArray[d[""]].ToString();
                n.transform.parent = currEnvironement.transform;
                n.transform.position = g.FixationPoint;

                _gbs.Add(new GazeBlock(g.StartFrame, g.FixationStartFrame, g.CharacterName, g.AnimationClip, n.name, n, g.TurnBody, g.FixationPoint));

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
                    _gbs.Add(new GazeBlock(g.StartFrame, g.FixationStartFrame, g.CharacterName, g.AnimationClip, g.TargetName, targetTest, g.TurnBody, g.FixationPoint));
                    counterArray[d[g.TargetName]]++;
                    continue;
                }

                GameObject n = new GameObject();
                n.tag = "GazeTarget";
                n.name = g.TargetName + "_" + counterArray[d[g.TargetName]].ToString();
                //TODO: check if parent object is a manipulatable object.  If it is, set parent to its parent
                var parent = g.Target.transform;
                if (g.Target.tag.Equals("ManipulatedObject")) parent = g.Target.transform.parent.transform;
                
                //TODO: check if the parent object is part of norman or roman's body.  If it is, set parent to environment
                if (g.Target.name.Equals("Norman_Hand") || g.Target.name.Equals("Norman_Head") ||
                    g.Target.name.Equals("Roman_Hand") || g.Target.name.Equals("Roman_Head")) {
                        var currEnvironement = AnimationTimeline.Instance.Environment;
                        parent = currEnvironement.transform;
                }

                n.transform.parent = parent;
                n.transform.position = g.FixationPoint;

                _gbs.Add(new GazeBlock(g.StartFrame, g.FixationStartFrame, g.CharacterName, g.AnimationClip, n.name, n, g.TurnBody, g.FixationPoint));

                //increment counter for the scene environment
                counterArray[d[g.TargetName]]++;
            }
        }

        return _gbs;
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
    


}

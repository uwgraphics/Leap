using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Runs when shelf button is pressed in order to output angle data files that can
/// be loaded into Matlab.  These files are in csv format.  
/// </summary>
public class FilterData {

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
    private string animationName;

    //constructor
    //if a specific inferenceAnimation is passed to this with implied character already,
    //I don't have to worry about assigning names at this abstraction level
    //i.e. if you want to use new animation, just add a new extended class to 
    //InferenceAnimation with specific character choice
    public FilterData(string animationName, string characterName) {
        Character = new InferenceCharacter(characterName);
        AnimationClip = new AnimationClipInstance(Character.CharModel, animationName);
        this.animationName = animationName;
    }

    //incomplete functionality right now.  This will be finished once jointIndex is
    //completed.  For now, jointIndex will always return the right arm joint.
    public List<Quaternion> queryJoint(string jointName) {
        var orientations = new List<Quaternion>();

        var rightArms = GameObject.FindGameObjectsWithTag("RElbow");
        var rightArm = rightArms[0];

        int frame;
        for (int i = 1; i < AnimationClip.FrameLength; i++) {
            frame = i;
            AnimationClip.Animation[animationName].normalizedTime = Mathf.Clamp01(((float)frame) / AnimationClip.FrameLength);
            AnimationClip.Animation[animationName].weight = AnimationClip.Weight;
            AnimationClip.Animation[animationName].enabled = true;

            AnimationClip.Animation.Sample();
            /////////////////////////////////
            orientations.Add(rightArm.transform.rotation); // uses the head rotations as default, will change this later
            /////////////////////////////////
            AnimationClip.Animation[animationName].enabled = false;
        }

        return orientations;
    }
    

    //TODO: complete this function.
    //For now, the GazeJoint will automatically be the rightArm with tag RElbow
    private GazeJoint jointIndex(string jointName) {
        return new GazeJoint();
    }


}

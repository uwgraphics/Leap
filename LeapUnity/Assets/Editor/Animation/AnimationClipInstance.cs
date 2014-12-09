using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Animation instance corresponding to a pre-made animation clip.
/// </summary>
public class AnimationClipInstance : AnimationInstance
{
    /// <summary>
    /// Length of the animation clip in seconds.
    /// </summary>
    public override float TimeLength
    {
        get { return AnimationClip.length; }
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="model">Character model</param>
    /// <param name="animationClipName">Animation clip name</param>
    public AnimationClipInstance(GameObject model, string animationClipName) : 
        base(model, animationClipName)
    {
    }

    /// <summary>
    /// <see cref="AnimationInstance.StartBake()"/>
    /// </summary>
    public override void StartBake()
    {
        return;
    }

    /// <summary>
    /// Apply animation instance to the character model at specified frame.
    /// </summary>
    /// <param name="frame">Frame index</param>
    /// <param name="layerMode">Animation layering mode</param>
    public override void Apply(int frame, AnimationLayerMode layerMode)
    {
        // Configure how the animation clip will be applied to the model
        Animation[AnimationClip.name].normalizedTime = ((float)frame) / FrameLength;
        Animation[AnimationClip.name].weight = Weight;
        if (layerMode == AnimationLayerMode.Additive)
        {
            Animation[AnimationClip.name].blendMode = AnimationBlendMode.Additive;
        }
        else
        {
            Animation[AnimationClip.name].blendMode = AnimationBlendMode.Blend;
        }

        // Apply the animation clip to the model
        Animation[AnimationClip.name].enabled = true;
        Animation.Sample();
        Animation[AnimationClip.name].enabled = false;
        //
        /*Debug.Log(string.Format("Applied animation {0} at frame {1} in layer moder {2} at weight {3}",
            AnimationClip.name, frame, layerMode.ToString(), Weight));*/
        //
    }

    /// <summary>
    /// <see cref="AnimationInstance.FinishBake()"/>
    /// </summary>
    public override void FinishBake()
    {
        return;
    }
}

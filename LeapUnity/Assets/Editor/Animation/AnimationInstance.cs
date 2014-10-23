using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Layer type defines how animations are applied
/// to the character. Supported modes are:
/// - Additive - animation is multiplied by a weight value and added to the current character pose
/// - Override - animation overwrites the current character pose
/// </summary>
public enum AnimationLayerMode
{
    Additive,
    Override
}

/// <summary>
/// Animation instance. This can be either an existing animation clip
/// or a procedural animation that can be baked into an animation clip.
/// </summary>
public class AnimationInstance
{
    /// <summary>
    /// Character model controller to which this animation instance is applied.
    /// </summary>
    public virtual ModelController Model
    {
        get;
        private set;
    }

    /// <summary>
    /// Animation component on the character.
    /// </summary>
    public virtual Animation Animation
    {
        get;
        private set;
    }

    /// <summary>
    /// Animation clip corresponding to the instance.
    /// </summary>
    public virtual AnimationClip AnimationClip
    {
        get;
        set;
    }

    /// <summary>
    /// Weight at which the animation instance is applied to the character.
    /// This is only used in additive layers.
    /// </summary>
    public virtual float Weight
    {
        get;
        set;
    }

    /// <summary>
    /// Length of this instance in frames.
    /// </summary>
    public virtual int FrameLength
    {
        get { return (int)(AnimationClip.length * LEAPCore.editFrameRate + 0.5f); }
    }

    /// <summary>
    /// Length of this instance in seconds.
    /// </summary>
    public virtual float TimeLength
    {
        get { return AnimationClip.length; }
    }

    protected bool _isBaked = false;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="model">Character model</param>
    /// <param name="animationClipName">Animation clip name</param>
    public AnimationInstance(GameObject model, string animationClipName)
    {
        Model = model.GetComponent<ModelController>();
        Animation = model.GetComponent<Animation>();
        if (Model == null)
        {
            throw new Exception("Unable to create animation instance for a character model without a Model Controller");
        }
        else if (Animation == null)
        {
            throw new Exception("Unable to create animation instance for a character model without an Animation component");
        }

        // Is there an animation clip already for this instance?
        foreach (AnimationState animationState in Animation)
        {
            var clip = animationState.clip;
            if (clip.name == animationClipName)
            {
                // There is already a defined clip for this animation instance,
                // so assume that is the animation
                this.AnimationClip = clip;
                _isBaked = true;
            }
            else
            {
                continue;
            }
        }

        if (!_isBaked)
        {
            // No clip found for this animation instance, create an empty one
            AnimationClip[] allClips = AnimationClip.FindObjectsOfType<AnimationClip>();
            while (allClips.Any(clip => clip.name == animationClipName))
            {
                animationClipName += "1";
            }
            this.AnimationClip = new AnimationClip();
            AnimationUtility.SetAnimationType(this.AnimationClip, ModelImporterAnimationType.Legacy);

            // Add the clip to character's animation component
            Animation.AddClip(this.AnimationClip, AnimationClip.name);
        }

        // Set default animation weight
        Weight = 1f;
    }

    /// <summary>
    /// Bake the animation instance into an animation clip.
    /// </summary>
    public virtual void Bake()
    {
    }

    /// <summary>
    /// Apply animation instance to the character model at specified frame.
    /// </summary>
    /// <param name="frame">Frame index</param>
    /// <param name="layerMode">Animation layering mode</param>
    public virtual void Apply(int frame, AnimationLayerMode layerMode)
    {
        // Configure how the animation clip will be applied to the model
        Animation[AnimationClip.name].time = ((float)frame) / TimeLength;
        if (layerMode == AnimationLayerMode.Additive)
        {
            Animation[AnimationClip.name].weight = Weight;
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
    }

    /// <summary>
    /// Get frame index at specified time index of an animation.
    /// </summary>
    /// <param name="time">Time index</param>
    /// <returns>Frame index</returns>
    public static int GetFrameAtTime(float time)
    {
        return (int)(time * LEAPCore.editFrameRate + 0.5f);
    }
}

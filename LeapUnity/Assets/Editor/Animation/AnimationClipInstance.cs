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
    /// <see cref="AnimationInstance.Name"/>
    /// </summary>
    public override string Name
    {
        get
        {
            return AnimationClip.name;
        }

        protected set
        {
            return;
        }
    }

    /// <summary>
    /// Animation controller activity duration.
    /// </summary>
    public override float TimeLength
    {
        get { return AnimationClip.length; }
        set { throw new InvalidOperationException("Cannot change the length of an animation clip instance"); }
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
    /// Animation component on the character.
    /// </summary>
    public virtual Animation Animation
    {
        get;
        protected set;
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="name">Animation clip name</param>
    /// <param name="model">Character model</param>
    public AnimationClipInstance(string name, GameObject model) : base(name, model)
    {
        Animation = model.GetComponent<Animation>();
        if (Animation == null)
        {
            throw new Exception("Unable to create animation clip instance for a character model without an Animation component");
        }

        // Is there an animation clip already for this instance?
        foreach (AnimationState animationState in Animation)
        {
            var clip = animationState.clip;
            if (clip == null)
            {
                // TODO: this is needed b/c Unity is a buggy piece of crap
                Debug.LogWarning(string.Format("Animation state {0} on model {1} has clip set to null",
                    animationState.name, Animation.gameObject.name));
                continue;
            }

            if (clip.name == name && AssetDatabase.GetAssetPath(clip) != "")
            {
                // There is already a defined clip for this animation instance,
                // so assume that is the animation
                this.AnimationClip = clip;
                // TODO: this is needed b/c Unity is a buggy piece of crap
                Animation.RemoveClip(clip);
                Animation.AddClip(clip, name);
                break;
            }
            else
            {
                this.AnimationClip = null;
            }
        }

        if (this.AnimationClip == null)
        {
            // No clip found for this animation instance, create an empty one
            AnimationClip = LEAPAssetUtils.CreateAnimationClipOnModel(name, model);
        }
    }

    /// <summary>
    /// Apply animation instance to the character model at specified frame.
    /// </summary>
    /// <param name="frame">Frame index</param>
    /// <param name="layerMode">Animation layering mode</param>
    public override void Apply(int frame, AnimationLayerMode layerMode)
    {
        // Configure how the animation clip will be applied to the model
        Animation[AnimationClip.name].normalizedTime = Mathf.Clamp01(((float)frame) / FrameLength);
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
    }
}

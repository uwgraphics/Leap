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

    protected Dictionary<string, AnimationClipInstance> _endEffectorTargetHelperAnimations =
        new Dictionary<string,AnimationClipInstance>();

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

            if (clip.name == name)
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
    /// Associate an animation encoding the end-effector trajectory in the current animation clip with
    /// the animation instance.
    /// </summary>
    /// <param name="endEffector">End effector tag</param>
    /// <param name="helperInstance">End-effector target helper animation</param>
    public virtual void AddEndEffectorTargetHelperAnimation(string endEffector, AnimationClipInstance helperInstance)
    {
        _endEffectorTargetHelperAnimations[endEffector] = helperInstance;
    }

    /// <summary>
    /// Dissociate an animation encoding the end-effector trajectory in the current animation clip
    /// from the animation instance.
    /// </summary>
    /// <param name="endEffector">End effector tag</param>
    public virtual void RemoveEndEffectorTargetHelperAnimation(string endEffector)
    {
        _endEffectorTargetHelperAnimations.Remove(endEffector);
    }

    /// <summary>
    /// Dissociate all animations encoding the end-effector trajectory in the current animation clip
    /// from the animation instance.
    /// </summary>
    public virtual void RemoveAllEndEffectorTargetHelperAnimations()
    {
        _endEffectorTargetHelperAnimations.Clear();
    }

    /// <summary>
    /// Get an animation encoding the trajectory of the specified end effector
    /// in the current animation clip.
    /// </summary>
    /// <param name="endEffector">End effector tag</param>
    /// <returns>End-effector target helper animation</returns>
    public virtual AnimationClipInstance GetEndEffectorTargetHelperAnimation(string endEffector)
    {
        return _endEffectorTargetHelperAnimations[endEffector];
    }

    /// <summary>
    /// Apply animation instance to the character model at specified frame.
    /// </summary>
    /// <param name="frame">Frame index</param>
    /// <param name="layerMode">Animation layering mode</param>
    public override void Apply(int frame, AnimationLayerMode layerMode)
    {
        if (layerMode == AnimationLayerMode.Additive)
            throw new Exception("Additive layer mode currently not supported in AnimationClipInstance");

        // Compute animation time
        float normalizedTime = FrameLength > 1 ? Mathf.Clamp01(((float)frame) / (FrameLength - 1)) : 0f;

        // Configure how the animation clip will be applied to the model
        Animation[AnimationClip.name].normalizedTime = normalizedTime;
        Animation[AnimationClip.name].weight = 1f;
        Animation[AnimationClip.name].blendMode = AnimationBlendMode.Blend;

        // Apply the animation clip to the model
        Animation[AnimationClip.name].enabled = true;
        Animation.Sample();
        Animation[AnimationClip.name].enabled = false;
    }
}

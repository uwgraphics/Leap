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
public abstract class AnimationInstance
{
    /// <summary>
    /// Character model controller to which this animation instance is applied.
    /// </summary>
    public virtual GameObject Model
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
        get { return (int)(TimeLength * LEAPCore.editFrameRate + 0.5f); }
    }

    /// <summary>
    /// Length of this instance in seconds.
    /// </summary>
    public abstract float TimeLength
    {
        get;
    }

    /// <summary>
    /// Set to true to have the animation instance baked into the animation clip.
    /// </summary>
    /// <remarks>When this is true, the AnimationInstance must save the result of each frame
    /// as new keyframes in the corresponding animation curves (_AnimationCurves array). It is the responsibility
    /// of each AnimationInstance subclass to implement this functionality in its override of the Apply() method.</remarks>
    public virtual bool IsBaking
    {
        get;
        set;
    }

    // Animation curves for baking the current instance
    protected virtual AnimationCurve[] _AnimationCurves
    {
        get;
        set;
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="model">Character model</param>
    /// <param name="animationClipName">Animation clip name</param>
    public AnimationInstance(GameObject model, string animationClipName)
    {
        Model = model;
        Animation = model.GetComponent<Animation>();
        if (Model == null)
        {
            throw new Exception("Must specify character model");
        }
        else if (Animation == null)
        {
            throw new Exception("Unable to create animation instance for a character model without an Animation component");
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

            if (clip.name == animationClipName && AssetDatabase.GetAssetPath(clip) != "")
            {
                // There is already a defined clip for this animation instance,
                // so assume that is the animation
                this.AnimationClip = clip;
                // TODO: this is needed b/c Unity is a buggy piece of crap
                Animation.RemoveClip(clip);
                Animation.AddClip(clip, animationClipName);
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
            AnimationClip = LEAPAssetUtils.CreateAnimationClipOnModel(animationClipName, model);
        }

        // Create empty animation curves for baking the animation instance
        _AnimationCurves = LEAPAssetUtils.CreateAnimationCurvesForModel(Model);

        // Set default animation weight
        Weight = 1f;
    }

    /// <summary>
    /// Start the application of the current animation instance.
    /// </summary>
    /// <remarks>AnimationTimeline calls this function during playback, on the first frame of the current instance.</remarks>
    public virtual void Start()
    {
    }

    /// <summary>
    /// Apply animation instance to the character model at specified frame.
    /// </summary>
    /// <param name="frame">Frame index</param>
    /// <param name="layerMode">Animation layering mode</param>
    public abstract void Apply(int frame, AnimationLayerMode layerMode);

    /// <summary>
    /// Finish the application of the current animation instance.
    /// </summary>
    /// <remarks>AnimationTimeline calls this function during playback, after the last frame of the current instance.</remarks>
    public virtual void Finish()
    {
    }

    /// <summary>
    /// Finalize baking the animation instance into an animation clip.
    /// </summary>
    /// <remarks>This will save baked animation curves to an animation clip.</remarks>
    public virtual void FinalizeBake()
    {
        IsBaking = false;
        AnimationClip.ClearCurves();
        LEAPAssetUtils.SetAnimationCurvesOnClip(Model.gameObject, AnimationClip, _AnimationCurves);
        _AnimationCurves = LEAPAssetUtils.CreateAnimationCurvesForModel(Model);

        // Write animation clip to file
        string path = LEAPAssetUtils.GetModelDirectory(Model) + AnimationClip.name + ".anim";
        if (AssetDatabase.GetAssetPath(AnimationClip) != path)
        {
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(AnimationClip, path);
        }
        AssetDatabase.SaveAssets();
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

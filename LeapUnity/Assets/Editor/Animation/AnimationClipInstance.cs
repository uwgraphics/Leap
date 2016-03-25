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
        get { return AnimationClip.name; }
        protected set { return; }
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
    /// End-effector constraints in the current animation.
    /// </summary>
    public virtual EndEffectorConstraintContainer EndEffectorConstraints
    {
        get { return _endEffectorConstraints; }
    }

    /// <summary>
    /// Timings of key poses in the animation.
    /// </summary>
    public virtual IDictionary<int, TimeSet> KeyTimes
    {
        get { return new ReadOnlyDictionary<int, TimeSet>(_keyTimes); }
    }

    // End-effector constraints:
    protected EndEffectorConstraintContainer _endEffectorConstraints = null;
    protected Dictionary<string, AnimationClip> _endEffectorTargetHelperClips =
        new Dictionary<string,AnimationClip>();

    // Animation segmentation:
    protected Dictionary<int, TimeSet> _keyTimes = new Dictionary<int, TimeSet>();

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="name">Animation clip name</param>
    /// <param name="model">Character model</param>
    /// <param name="loadEndEffectorConstraints">If true, end-effector constraints will be loaded</param>
    /// <param name="createEndEffectorTargetHelperAnimations">If true, end-effector target helper animations will be created</param>
    /// <param name="loadKeyFrames">If true, keyframe indexes will be loaded or extracted</param>
    public AnimationClipInstance(string name, GameObject model,
        bool loadEndEffectorConstraints = true, bool createEndEffectorTargetHelperAnimations = true,
        bool loadOrExtractKeyFrames = true) : base(name, model)
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
            AnimationClip = LEAPAssetUtil.CreateAnimationClipOnModel(name, model);
        }

        if (model.tag == "Agent")
        {
            if (loadEndEffectorConstraints)
            {
                // Load end-effector constraints on the clip
                var endEffectorConstraints = LEAPAssetUtil.LoadEndEffectorConstraintsForClip(Model, AnimationClip);
                _endEffectorConstraints = endEffectorConstraints != null ?
                    new EndEffectorConstraintContainer(AnimationClip, endEffectorConstraints) : null;

                if (_endEffectorConstraints != null && createEndEffectorTargetHelperAnimations)
                {
                    // Create end-effector target helper animations
                    var endEffectorTargetHelperAnimations = LEAPAssetUtil.InitEndEffectorTargetHelperAnimations(Model, AnimationClip);
                    foreach (var helperAnimation in endEffectorTargetHelperAnimations)
                    {
                        _endEffectorTargetHelperClips.Add(helperAnimation.endEffectorTag, helperAnimation.helperAnimationClip);
                    }
                }
            }

            if (loadOrExtractKeyFrames)
            {
                // Load or extract keyframe indexes
                KeyFrameSet[] keyFrameSets = null;
                if (!KeyFrameExtractor.LoadAnimationKeyFrames(model, AnimationClip, out keyFrameSets))
                {
                    var keyFrameExtractor = new KeyFrameExtractor(model, AnimationClip);
                    keyFrameSets = keyFrameExtractor.ExtractKeyFrames();
                    KeyFrameExtractor.SaveAnimationKeyFrames(model, AnimationClip, keyFrameSets);
                }

                // Initialize key times
                foreach (var keyFrameSet in keyFrameSets)
                    _keyTimes.Add(keyFrameSet.keyFrame, (TimeSet)keyFrameSet);
            }
        }
    }

    /// <summary>
    /// Get the animation clip encoding the trajectory of the specified end effector
    /// in the current animation clip.
    /// </summary>
    /// <param name="endEffectorTag">End effector tag</param>
    /// <returns>End-effector target helper animation clip</returns>
    public virtual AnimationClip GetEndEffectorTargetHelperClip(string endEffectorTag)
    {
        return _endEffectorTargetHelperClips.ContainsKey(endEffectorTag) ?
            _endEffectorTargetHelperClips[endEffectorTag] : null;
    }

    /// <summary>
    /// Apply animation instance to the character model at the specified times.
    /// </summary>
    /// <param name="times">Time indexes</param>
    /// <param name="layerMode">Animation layering mode</param>
    public override void Apply(TimeSet times, AnimationLayerMode layerMode)
    {
        if (layerMode == AnimationLayerMode.Additive)
            throw new Exception("Additive layer mode currently not supported in AnimationClipInstance");

        var root = ModelController != null ? ModelController.Root : Model.transform;

        // Compute timings of all animation tracks
        float rootTrackTime = TimeLength > 0.01f ? Mathf.Clamp01(times.rootTime / TimeLength) : 0f;
        var boneTrackTimes = new List<float>();
        for (int boneIndex = 0; boneIndex < times.boneTimes.Length; ++boneIndex)
            boneTrackTimes.Add(TimeLength > 0.01f ? Mathf.Clamp01(times.boneTimes[boneIndex] / TimeLength) : 0f);

        // Apply root animation track to the model
        Animation[AnimationClip.name].normalizedTime = rootTrackTime;
        Animation[AnimationClip.name].weight = 1f;
        Animation[AnimationClip.name].blendMode = AnimationBlendMode.Blend;
        Animation[AnimationClip.name].AddMixingTransform(root);
        Animation[AnimationClip.name].enabled = true;
        Animation.Sample();
        Animation[AnimationClip.name].enabled = false;
        Animation[AnimationClip.name].RemoveMixingTransform(root);

        // Store root position
        Vector3 rootPosition = root.position;

        // Apply bone animation tracks to the model
        for (int boneIndex = 0; boneIndex < times.boneTimes.Length; ++boneIndex)
        {
            var bone = ModelController.GetBone(boneIndex);
            Animation[AnimationClip.name].normalizedTime = boneTrackTimes[boneIndex];
            Animation[AnimationClip.name].weight = 1f;
            Animation[AnimationClip.name].blendMode = AnimationBlendMode.Blend;
            Animation[AnimationClip.name].AddMixingTransform(bone);
            Animation[AnimationClip.name].enabled = true;
            Animation.Sample();
            Animation[AnimationClip.name].enabled = false;
            Animation[AnimationClip.name].RemoveMixingTransform(bone);
        }

        // Reapply root position
        root.position = rootPosition;
    }

    /// <summary>
    /// Get active end-effector constraints at the specified times.
    /// </summary>
    /// <param name="times">Time indexes</param>
    /// <param name="constraints">Active end-effector constraints</param>
    /// <param name="weights">Active end-effector constraint weights</param>
    public virtual void GetEndEffectorConstraintsAtTime(TimeSet times,
        out EndEffectorConstraint[] constraints, out float[] weights)
    {
        var activeConstraints = new List<EndEffectorConstraint>();
        var activeConstraintWeights = new List<float>();
        var endEffectors = ModelUtil.GetEndEffectors(Model);
        foreach (var endEffector in endEffectors)
        {
            int endEffectorIndex = ModelController.GetBoneIndex(endEffector);
            int trackFrame = LEAPCore.ToFrame(times.boneTimes[endEffectorIndex]);
            
            // Get active constraints on the current end-effector and compute their weights
            var activeConstraintsForEndEffector = _endEffectorConstraints.GetConstraintsAtFrame(endEffector.tag, trackFrame);
            if (activeConstraintsForEndEffector != null)
            {
                activeConstraints.AddRange(activeConstraintsForEndEffector);
                foreach (var activeConstraint in activeConstraintsForEndEffector)
                {
                    float t = 1f;
                    if (trackFrame < activeConstraint.startFrame)
                        t = Mathf.Clamp01(1f - ((float)(activeConstraint.startFrame - trackFrame))
                            / activeConstraint.activationFrameLength);
                    else if (trackFrame > activeConstraint.startFrame + activeConstraint.frameLength - 1)
                        t = Mathf.Clamp01(1f - ((float)(trackFrame - (activeConstraint.startFrame + activeConstraint.frameLength - 1)))
                            / activeConstraint.deactivationFrameLength);
                    float t2 = t * t;
                    activeConstraintWeights.Add(-2f * t2 * t + 3f * t2);
                }
            }
        }
        
        constraints = activeConstraints.ToArray();
        weights = activeConstraintWeights.ToArray();
    }

    /// <summary>
    /// Get active end-effector constraints with object manipulation at the specified times.
    /// </summary>
    /// <param name="times">Time indexes</param>
    /// <returns>Active end-effector constraints with object manipulation</returns>
    public virtual EndEffectorConstraint[] GetManipulationEndEffectorConstraintsAtTime(TimeSet times)
    {
        var activeConstraints = new List<EndEffectorConstraint>();
        var endEffectors = ModelUtil.GetEndEffectors(Model);
        foreach (var endEffector in endEffectors)
        {
            int endEffectorIndex = ModelController.GetBoneIndex(endEffector);
            int trackFrame = LEAPCore.ToFrame(times.boneTimes[endEffectorIndex]);

            // Get active manipulation constraints on the current end-effector
            var activeConstraintsForEndEffector = _endEffectorConstraints
                .GetManipulationConstraintsAtFrame(endEffector.tag, trackFrame);
            if (activeConstraintsForEndEffector != null)
                activeConstraints.AddRange(activeConstraintsForEndEffector);
        }

        return activeConstraints.ToArray();
    }
}

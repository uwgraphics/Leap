using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Animation bone mask, specifying the blend weights with which
/// an animation is applied to bones.
/// </summary>
public class AnimationBoneMask
{
    public struct BoneWeightPair
    {
        public Transform bone;
        public float weight;

        public BoneWeightPair(Transform bone, float weight)
        {
            this.bone = bone;
            this.weight = weight;
        }
    }

    private List<BoneWeightPair> _boneWeights = new List<BoneWeightPair>();
    private float _rootPositionWeight = 0f;

    /// <summary>
    /// List of bone-weight pairs.
    /// </summary>
    public IList<BoneWeightPair> BoneWeights
    {
        get { return _boneWeights.AsReadOnly(); }
    }

    /// <summary>
    /// Root position weight.
    /// </summary>
    public float RootPositionWeight
    {
        get { return _rootPositionWeight; }
        set { _rootPositionWeight = Mathf.Clamp01(value); }
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    public AnimationBoneMask()
    {
    }

    /// <summary>
    /// Get blend weight for bone.
    /// </summary>
    /// <param name="bone">Bone</param>
    /// <returns>Blend weight</returns>
    public float GetBoneWeight(Transform bone)
    {
        for (int boneWeightIndex = 0; boneWeightIndex < _boneWeights.Count; ++boneWeightIndex)
        {
            if (_boneWeights[boneWeightIndex].bone == bone)
            {
                return _boneWeights[boneWeightIndex].weight;
            }
        }

        return 0f;
    }

    /// <summary>
    /// Set blend weight for bone.
    /// </summary>
    /// <param name="bone">Bone</param>
    /// <param name="weight">Blend weight</param>
    public void SetBoneWeight(Transform bone, float weight)
    {
        weight = Mathf.Clamp01(weight);
        for (int boneWeightIndex = 0; boneWeightIndex < _boneWeights.Count; ++boneWeightIndex)
        {
            if (_boneWeights[boneWeightIndex].bone == bone)
            {
                if (weight > 0f)
                {
                    _boneWeights[boneWeightIndex] = new BoneWeightPair(bone, weight);
                }
                else
                {
                    _boneWeights.RemoveAt(boneWeightIndex);
                }

                return;
            }
        }

        _boneWeights.Add(new BoneWeightPair(bone, weight));
    }

    /// <summary>
    /// Clear all bone-weight pairs.
    /// </summary>
    public void Clear()
    {
        _boneWeights.Clear();
    }
}

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
    /// End-effector constraints in the current animation.
    /// </summary>
    public virtual EndEffectorConstraintContainer EndEffectorConstraints
    {
        get { return _endEffectorConstraints; }
    }

    // Animation tracks:
    protected Dictionary<AnimationTrackType, AnimationClip> _trackClips =
        new Dictionary<AnimationTrackType, AnimationClip>();
    protected Dictionary<AnimationTrackType, AnimationBoneMask> _boneMasks =
        new Dictionary<AnimationTrackType, AnimationBoneMask>();
    protected Dictionary<Transform, List<AnimationTrackType>> _tracksPerBone =
        new Dictionary<Transform, List<AnimationTrackType>>();
    protected List<AnimationTrackType> _rootPositionTracks = new List<AnimationTrackType>();
    protected Dictionary<AnimationTrackType, List<string>> _endEffectorTagsForTracks =
        new Dictionary<AnimationTrackType, List<string>>();
    protected bool _isTimingControlledByTrack = false;
    protected AnimationTrackType _timingControlTrack = AnimationTrackType.Gaze;

    // End-effector constraints:
    protected EndEffectorConstraintContainer _endEffectorConstraints = null;
    protected Dictionary<string, AnimationClip> _endEffectorTargetHelperClips =
        new Dictionary<string,AnimationClip>();

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="name">Animation clip name</param>
    /// <param name="model">Character model</param>
    /// <param name="loadEndEffectorConstraints">If true, end-effector constraints will be loaded for the specified animation clip</param>
    public AnimationClipInstance(string name, GameObject model,
        bool loadEndEffectorConstraints = true, bool createEndEffectorTargetHelperAnimations = true) : base(name, model)
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

            // Initialize default animation track bone masks
            var boneMasks = LEAPAssetUtil.CreateDefaultAnimationTrackBoneMasks(model);
            var trackTypes = (AnimationTrackType[])Enum.GetValues(typeof(AnimationTrackType));
            foreach (AnimationTrackType trackType in trackTypes)
            {
                _boneMasks[trackType] = boneMasks[(int)trackType];
            }

            // Initialize default animation track clips
            var trackClips = LEAPAssetUtil.CreateAnimationClipsForTracks(model, AnimationClip, boneMasks);
            foreach (AnimationTrackType trackType in trackTypes)
            {
                _trackClips[trackType] = trackClips[(int)trackType];
            }
            _InitBoneTrackMappings();
            _InitTrackEndEffectorTagMappings();
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
    /// Get bone mask for the specified animation track.
    /// </summary>
    /// <param name="track">Animation track</param>
    /// <returns>Bone mask</returns>
    public virtual AnimationBoneMask GetBoneMask(AnimationTrackType track)
    {
        return _boneMasks[track];
    }

    /// <summary>
    /// Set bone mask for the specified animation track.
    /// </summary>
    /// <param name="track">Animation track</param>
    /// <param name="boneMask">Bone mask</param>
    public virtual void SetBoneMask(AnimationTrackType track, AnimationBoneMask boneMask)
    {
        _boneMasks[track] = boneMask;
    }

    /// <summary>
    /// Enable mode where the global timing of the animation instance (across all tracks) is controlled
    /// by the timing of a single animation track.
    /// </summary>
    /// <param name="trackType">Animation track that will control the timing of the instance</param>
    public virtual void InitTimingControlByTrack(AnimationTrackType trackType)
    {
        _isTimingControlledByTrack = true;
        _timingControlTrack = trackType;
    }

    /// <summary>
    /// Enable mode where the timing of each animation track in the instance is separately controlled.
    /// </summary>
    public virtual void StopTimingControlByTrack()
    {
        _isTimingControlledByTrack = false;
    }

    /// <summary>
    /// Apply animation instance to the character model at the specified times.
    /// </summary>
    /// <param name="times">Time indexes</param>
    /// <param name="layerMode">Animation layering mode</param>
    public override void Apply(TrackTimeSet times, AnimationLayerMode layerMode)
    {
        if (layerMode == AnimationLayerMode.Additive)
            throw new Exception("Additive layer mode currently not supported in AnimationClipInstance");

        if (_isTimingControlledByTrack || Model.tag != "Agent")
        {
            // Apply the entire animation with the timing of a pre-specified animation track
            Animation[AnimationClip.name].normalizedTime = TimeLength > 0.01f ?
                Mathf.Clamp01(times[_timingControlTrack] / TimeLength) : 0f;
            Animation[AnimationClip.name].weight = 1f;
            Animation[AnimationClip.name].blendMode = AnimationBlendMode.Blend;
            Animation[AnimationClip.name].enabled = true;
            Animation.Sample();
            Animation[AnimationClip.name].enabled = false;

            return;
        }

        _NormalizeBoneMaskWeights();

        // Compute timings of all animation tracks
        var _trackTimes = new Dictionary<AnimationTrackType, float>();
        foreach (var kvp in _trackClips)
        {
            float normalizedTime = TimeLength > 0.01f ? Mathf.Clamp01(times[kvp.Key] / TimeLength) : 0f;
            _trackTimes[kvp.Key] = normalizedTime;
        }

        if (_rootPositionTracks.Count > 0)
        {
            // Apply animation tracks to root position
            foreach (AnimationTrackType trackType in _rootPositionTracks)
            {
                var trackClip = _trackClips[trackType];

                // Configure how the animation clip will be applied to the model
                Animation[trackClip.name].normalizedTime = _trackTimes[trackType];
                Animation[trackClip.name].weight = _boneMasks[trackType].RootPositionWeight;
                Animation[trackClip.name].blendMode = AnimationBlendMode.Blend;
                Animation[trackClip.name].AddMixingTransform(ModelController.Root);
                Animation[trackClip.name].enabled = true;
            }

            // Apply animation clips to the model
            Animation.Sample();

            // Disable animations and clean up mixing transforms
            foreach (AnimationTrackType trackType in _rootPositionTracks)
            {
                var trackClip = _trackClips[trackType];

                // Configure how the animation clip will be applied to the model
                Animation[trackClip.name].enabled = false;
                Animation[trackClip.name].RemoveMixingTransform(ModelController.Root);
            }
        }

        // Store root position
        Vector3 rootPosition = ModelController != null ? ModelController.Root.localPosition : Model.transform.localPosition;

        // Apply animation tracks to each bone
        foreach (var kvp in _tracksPerBone)
        {
            foreach (AnimationTrackType trackType in kvp.Value)
            {
                var trackClip = _trackClips[trackType];

                // Configure how the animation clip will be applied to the model
                Animation[trackClip.name].normalizedTime = _trackTimes[trackType];
                Animation[trackClip.name].weight = _boneMasks[trackType].GetBoneWeight(kvp.Key);
                Animation[trackClip.name].blendMode = AnimationBlendMode.Blend;
                Animation[trackClip.name].AddMixingTransform(kvp.Key);
                Animation[trackClip.name].enabled = true;
            }

            // Apply animation clips to the model
            Animation.Sample();

            // Disable animations and clean up mixing transforms
            foreach (AnimationTrackType trackType in kvp.Value)
            {
                var trackClip = _trackClips[trackType];

                // Configure how the animation clip will be applied to the model
                Animation[trackClip.name].enabled = false;
                Animation[trackClip.name].RemoveMixingTransform(kvp.Key);
            }
        }

        // Reapply root position
        if (ModelController != null)
            ModelController.Root.localPosition = rootPosition;
        else
            Model.transform.localPosition = rootPosition;
    }

    /// <summary>
    /// Get active end-effector constraints at the specified times.
    /// </summary>
    /// <param name="times">Time indexes</param>
    /// <param name="constraints">Active end-effector constraints</param>
    /// <param name="weights">Active end-effector constraint weights</param>
    public virtual void GetEndEffectorConstraintsAtTime(TrackTimeSet times,
        out EndEffectorConstraint[] constraints, out float[] weights)
    {
        var activeConstraints = new List<EndEffectorConstraint>();
        var activeConstraintWeights = new List<float>();
        foreach (var kvp in _endEffectorTagsForTracks)
        {
            if (kvp.Value.Count <= 0)
                continue;

            int trackFrame = Mathf.RoundToInt(times[kvp.Key] * LEAPCore.editFrameRate);
            foreach (string endEffectorTag in kvp.Value)
            {
                // Get active constraints on the current end-effector and compute their weights
                var activeConstraintsForEndEffector = _endEffectorConstraints
                    .GetConstraintsAtFrame(endEffectorTag, trackFrame);
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
        }
        
        constraints = activeConstraints.ToArray();
        weights = activeConstraintWeights.ToArray();
    }

    /// <summary>
    /// Get active end-effector constraints with object manipulation at the specified times.
    /// </summary>
    /// <param name="times">Time indexes</param>
    /// <returns>Active end-effector constraints with object manipulation</returns>
    public virtual EndEffectorConstraint[] GetManipulationEndEffectorConstraintsAtTime(TrackTimeSet times)
    {
        var activeConstraints = new List<EndEffectorConstraint>();
        foreach (var kvp in _endEffectorTagsForTracks)
        {
            if (kvp.Value.Count <= 0)
                continue;

            int trackFrame = Mathf.RoundToInt(times[kvp.Key] * LEAPCore.editFrameRate);
            foreach (string endEffectorTag in kvp.Value)
            {
                // Get active manipulation constraints on the current end-effector
                var activeConstraintsForEndEffector = _endEffectorConstraints
                    .GetManipulationConstraintsAtFrame(endEffectorTag, trackFrame);
                if (activeConstraintsForEndEffector != null)
                    activeConstraints.AddRange(activeConstraintsForEndEffector);
            }
        }

        return activeConstraints.ToArray();
    }

    // Initialize and store mappings of bones to animation tracks for efficiency
    protected virtual void _InitBoneTrackMappings()
    {
        for (int boneIndex = 0; ModelController != null && boneIndex < ModelController.NumberOfBones; ++boneIndex)
        {
            var bone = ModelController.GetBone(boneIndex);
            foreach (var kvp in _boneMasks)
            {
                var boneMask = kvp.Value;
                if (boneMask == null)
                    continue;

                if (boneIndex == 0 && boneMask.RootPositionWeight > 0f)
                {
                    _rootPositionTracks.Add(kvp.Key);
                }

                if (boneMask.GetBoneWeight(bone) > 0f)
                {
                    if (!_tracksPerBone.ContainsKey(bone))
                        _tracksPerBone[bone] = new List<AnimationTrackType>();

                    _tracksPerBone[bone].Add(kvp.Key);
                }
            }
        }
    }

    // Initialize and store mappings of animation tracks to affected end effector tags for efficiency
    protected virtual void _InitTrackEndEffectorTagMappings()
    {
        _endEffectorTagsForTracks[AnimationTrackType.Gaze] = new List<string>();
        _endEffectorTagsForTracks[AnimationTrackType.Posture] = new List<string>();
        _endEffectorTagsForTracks[AnimationTrackType.LArmGesture] = new List<string>();
        _endEffectorTagsForTracks[AnimationTrackType.LArmGesture].Add(LEAPCore.lWristTag);
        _endEffectorTagsForTracks[AnimationTrackType.RArmGesture] = new List<string>();
        _endEffectorTagsForTracks[AnimationTrackType.RArmGesture].Add(LEAPCore.rWristTag);
        _endEffectorTagsForTracks[AnimationTrackType.Locomotion] = new List<string>();
        _endEffectorTagsForTracks[AnimationTrackType.Locomotion].Add(LEAPCore.lAnkleTag);
        _endEffectorTagsForTracks[AnimationTrackType.Locomotion].Add(LEAPCore.rAnkleTag);
    }

    // Normalize the non-zero blend weights on bones defined in bone masks, so that they sum to 1
    protected virtual void _NormalizeBoneMaskWeights()
    {
        foreach (var kvp in _tracksPerBone)
        {
            // Compute sum of blend weights for the current bone
            float sum = 0f;
            foreach (AnimationTrackType trackType in kvp.Value)
            {
                if (_boneMasks[trackType] == null)
                    continue;

                sum += _boneMasks[trackType].GetBoneWeight(kvp.Key);
            }

            if (sum > 0f)
            {
                // Normalize weights on the current bone
                foreach (AnimationTrackType trackType in kvp.Value)
                {
                    if (_boneMasks[trackType] == null)
                        continue;

                    float weight = _boneMasks[trackType].GetBoneWeight(kvp.Key);
                    _boneMasks[trackType].SetBoneWeight(kvp.Key, weight / sum);
                }
            }
        }

        // Compute sum of blend weights for the root position
        float sumRootPos = 0f;
        foreach (AnimationTrackType trackType in _rootPositionTracks)
        {
            sumRootPos += _boneMasks[trackType].RootPositionWeight;
        }

        if (sumRootPos > 0f)
        {
            // Normalize weights on the root position
            foreach (AnimationTrackType trackType in _rootPositionTracks)
            {
                float weight = _boneMasks[trackType].RootPositionWeight;
                _boneMasks[trackType].RootPositionWeight = weight / sumRootPos;
            }
        }
    }
}

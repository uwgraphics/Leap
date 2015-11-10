using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;

public enum AnimationTrackType
{
    Gaze,
    Posture,
    LArmGesture,
    RArmGesture,
    Locomotion,
    All
}

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
public class TimewarpedAnimationClipInstance : AnimationClipInstance
{
    /// <summary>
    /// <see cref="AnimationInstance.FrameLength"/>
    /// </summary>
    public override int FrameLength
    {
        get { return _frameLength; }
        set { throw new InvalidOperationException("Cannot change the length of an animation clip instance"); }
    }

    /// <summary>
    /// <see cref="AnimationInstance.TimeLength"/>
    /// </summary>
    public override float TimeLength
    {
        get { return ((float)FrameLength) / LEAPCore.editFrameRate; }
        set { throw new InvalidOperationException("Cannot change the length of an animation clip instance"); }
    }

    /// <summary>
    /// Original animation clip length (before timewarping).
    /// </summary>
    public virtual int OrigFrameLength
    {
        get { return Mathf.RoundToInt(AnimationClip.length * LEAPCore.editFrameRate); }
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

    // Timewarping:
    protected Dictionary<AnimationTrackType, List<ITimewarp>> _timewarps = new Dictionary<AnimationTrackType, List<ITimewarp>>();
    protected Dictionary<AnimationTrackType, List<int>> _timewarpStartFrames = new Dictionary<AnimationTrackType, List<int>>();
    protected Dictionary<AnimationTrackType, int> _frameLengths = new Dictionary<AnimationTrackType, int>();
    protected int _frameLength;

    protected List<Transform> _bonesAddedAsMixingTransforms = new List<Transform>();

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="name">Animation clip name</param>
    /// <param name="model">Character model</param>
    public TimewarpedAnimationClipInstance(string name, GameObject model)
        : base(name, model)
    {
        // Initialize default animation track bone masks
        var boneMasks = LEAPAssetUtils.CreateDefaultAnimationTrackBoneMasks(model);
        var trackTypes = (AnimationTrackType[])Enum.GetValues(typeof(AnimationTrackType));
        foreach (AnimationTrackType trackType in trackTypes)
        {
            _boneMasks[trackType] = boneMasks[(int)trackType];
        }

        // Initialize default animation track clips
        var trackClips = LEAPAssetUtils.CreateAnimationClipsForTracks(model, AnimationClip, boneMasks);
        foreach (AnimationTrackType trackType in trackTypes)
        {
            _trackClips[trackType] = trackClips[(int)trackType];
        }
        _InitBoneTrackMappings();
        _InitTrackEndEffectorTagMappings();
     
        // Initialize timewarping
        _InitTimewarps();
        _UpdateFrameLengths();
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
    /// Apply a timewarp to the animation.
    /// </summary>
    /// <param name="timewarp">Timewarp</param>
    /// <param name="startFrame">Timewarp start frame index (in the original animation time)</param>
    /// <param name="track">Animation track to which the timewarp applies</param>
    public virtual void AddTimewarp(AnimationTrackType track, ITimewarp timewarp, int startFrame)
    {
        var timewarps = _timewarps[track];
        var timewarpStartFrames = _timewarpStartFrames[track];

        // Find the timewarp that will follow the new timewarp
        int nextTimewarpIndex = -1;
        for (int timewarpIndex = 0; timewarpIndex < timewarps.Count; ++timewarpIndex)
        {
            if (timewarpStartFrames[timewarpIndex] > startFrame)
            {
                nextTimewarpIndex = timewarpIndex;
                break;
            }
        }

        // Add the new timewarp
        int newTimewarpIndex = -1;
        if (nextTimewarpIndex >= 0)
        {
            timewarps.Insert(nextTimewarpIndex, timewarp);
            timewarpStartFrames.Insert(nextTimewarpIndex, startFrame);
            newTimewarpIndex = nextTimewarpIndex;
        }
        else
        {
            timewarps.Add(timewarp);
            timewarpStartFrames.Add(startFrame);
            newTimewarpIndex = timewarps.Count - 1;
        }

        // Remove any timewarps that overlap the new timewarp
        for (int timewarpIndex = 0; timewarpIndex < timewarps.Count; ++timewarpIndex)
        {
            ITimewarp curTimewarp = timewarps[timewarpIndex];
            int curStartFrame = timewarpStartFrames[timewarpIndex];

            if (timewarpIndex != newTimewarpIndex &&
                curStartFrame <= startFrame + timewarp.OrigFrameLength &&
                curStartFrame + curTimewarp.OrigFrameLength >= startFrame + timewarp.OrigFrameLength)
            {
                RemoveTimewarp(track, timewarpIndex);
                newTimewarpIndex = timewarpIndex < newTimewarpIndex ? newTimewarpIndex - 1 : newTimewarpIndex;
                --timewarpIndex;
            }
        }

        _UpdateFrameLengths();

        // Add the same timewarp to the end-effector target helper animations
        foreach (string endEffectorTag in _endEffectorTagsForTracks[track])
        {
            if (_endEffectorTargetHelperAnimations.ContainsKey(endEffectorTag))
            {
                (_endEffectorTargetHelperAnimations[endEffectorTag] as TimewarpedAnimationClipInstance)
                    .AddTimewarp(AnimationTrackType.All, timewarp, startFrame);
            }
        }
    }

    /// <summary>
    /// Remove a timewarp applied to the animation.
    /// </summary>
    /// <param name="track">Animation track</param>
    /// <param name="timewarpIndex">Timewarp index</param>
    public virtual void RemoveTimewarp(AnimationTrackType track, int timewarpIndex)
    {
        var timewarps = _timewarps[track];
        var timewarpStartFrames = _timewarpStartFrames[track];
        timewarps.RemoveAt(timewarpIndex);
        timewarpStartFrames.RemoveAt(timewarpIndex);

        _UpdateFrameLengths();

        // Remove the same timewarp to the end-effector target helper animations
        foreach (string endEffectorTag in _endEffectorTagsForTracks[track])
        {
            if (_endEffectorTargetHelperAnimations.ContainsKey(endEffectorTag))
            {
                (_endEffectorTargetHelperAnimations[endEffectorTag] as TimewarpedAnimationClipInstance)
                    .RemoveTimewarp(AnimationTrackType.All, timewarpIndex);
            }
        }
    }

    /// <summary>
    /// Remove all timewarps applied to the animation.
    /// </summary>
    public virtual void RemoveAllTimewarps()
    {
        foreach (KeyValuePair<AnimationTrackType, List<ITimewarp>> kvp in _timewarps)
            kvp.Value.Clear();
        foreach (KeyValuePair<AnimationTrackType, List<int>> kvp in _timewarpStartFrames)
            kvp.Value.Clear();

        _UpdateFrameLengths();

        // Remove all timewarp from the end-effector target helper animations
        foreach (var kvp in _endEffectorTagsForTracks)
        {
            foreach (string endEffectorTag in kvp.Value)
            {
                if (_endEffectorTargetHelperAnimations.ContainsKey(endEffectorTag))
                {
                    (_endEffectorTargetHelperAnimations[endEffectorTag] as TimewarpedAnimationClipInstance)
                        .RemoveAllTimewarps();
                }
            }
        }
    }

    /// <summary>
    /// Get a timewarp applied to the animation.
    /// </summary>
    /// <param name="track">Animation track</param>
    /// <param name="timewarpIndex">Timewarp index</param>
    /// <returns>Timewarp</returns>
    public virtual ITimewarp GetTimewarp(AnimationTrackType track, int timewarpIndex)
    {
        return _timewarps[track][timewarpIndex];
    }

    /// <summary>
    /// Get the start frame of a timewarp applied to the animation.
    /// </summary>
    /// <param name="track">Animation track</param>
    /// <param name="timewarpIndex">Timewarp index</param>
    /// <returns>Timewarp</returns>
    public virtual int GetTimewarpStartFrame(AnimationTrackType track, int timewarpIndex)
    {
        return _timewarpStartFrames[track][timewarpIndex];
    }

    /// <summary>
    /// Get the number of timewarps applied to the specified animation track.
    /// </summary>
    /// <param name="track">Animation track</param>
    /// <returns>Number of timewarps</returns>
    public virtual int GetNumberOfTimewarps(AnimationTrackType track)
    {
        return _timewarps[track].Count;
    }

    /// <summary>
    /// Get the frame length of the specified animation track.
    /// </summary>
    /// <param name="track">Animation track</param>
    /// <returns></returns>
    public virtual int GetTrackFrameLength(AnimationTrackType track)
    {
        return _frameLengths[track];
    }

    /// <summary>
    /// Apply animation instance to the character model at specified frame.
    /// </summary>
    /// <param name="frame">Frame index</param>
    /// <param name="layerMode">Animation layering mode</param>
    public override void Apply(int frame, AnimationLayerMode layerMode)
    {
        if (layerMode == AnimationLayerMode.Additive)
            throw new Exception("Additive layer mode currently not supported in TimewarpedAnimationClipInstance");

        _NormalizeBoneMaskWeights();

        // Compute timings of all animation tracks
        var _trackTimes = new Dictionary<AnimationTrackType, float>();
        foreach (var kvp in _trackClips)
        {
            int origFrame = _GetOrigFrame(kvp.Key, frame);
            float normalizedTime = OrigFrameLength > 1 ? Mathf.Clamp01(((float)origFrame) / (OrigFrameLength - 1)) : 0f;
            _trackTimes[kvp.Key] = normalizedTime;
        }

        // Apply the entire animation with any global timewarps
        Animation[AnimationClip.name].normalizedTime = _trackTimes[AnimationTrackType.All];
        Animation[AnimationClip.name].weight = 1f;
        Animation[AnimationClip.name].blendMode = AnimationBlendMode.Blend;
        Animation[AnimationClip.name].enabled = true;
        Animation.Sample();
        Animation[AnimationClip.name].enabled = false;

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

    // Compute the frame index in the original animation clip
    protected virtual int _GetOrigFrame(AnimationTrackType track, int frame)
    {
        var timewarps = _timewarps[track];
        var timewarpStartFrames = _timewarpStartFrames[track];

        if (timewarps.Count <= 0 || !LEAPCore.timewarpsEnabled)
            return frame;

        int origFrame = -1;
        int curStartFrame = -1;
        for (int timewarpIndex = 0; timewarpIndex <= timewarps.Count; ++timewarpIndex)
        {
            int prevEndFrame = -1;
            int curEndFrame = -1;
            int curOrigStartFrame = -1;

            // Compute time intervals of the current timewarp, as well as the non-timewarped interval that might precede it
            if (timewarpIndex <= 0)
            {
                curOrigStartFrame = timewarpStartFrames[timewarpIndex];
                curStartFrame = curOrigStartFrame;
                curEndFrame = curStartFrame + timewarps[timewarpIndex].FrameLength - 1;
            }
            else
            {
                prevEndFrame = curStartFrame + timewarps[timewarpIndex - 1].FrameLength - 1;
                int prevOrigStartFrame = timewarpStartFrames[timewarpIndex - 1];
                int prevOrigFrameLength = timewarps[timewarpIndex - 1].OrigFrameLength;

                if (timewarpIndex >= timewarps.Count)
                {
                    curOrigStartFrame = OrigFrameLength;
                    curStartFrame = prevEndFrame + OrigFrameLength - prevOrigStartFrame - prevOrigFrameLength + 1;
                    curEndFrame = curStartFrame;
                }
                else
                {
                    curOrigStartFrame = timewarpStartFrames[timewarpIndex];
                    curStartFrame = prevEndFrame + curOrigStartFrame - prevOrigStartFrame - prevOrigFrameLength + 1;
                    curEndFrame = curStartFrame + timewarps[timewarpIndex].FrameLength - 1;
                }
            }

            if (frame > prevEndFrame && frame < curStartFrame)
            {
                // The applied frame is within the non-timewarped interval preceding the current timewarp
                origFrame = curOrigStartFrame - (curStartFrame - frame);
                break;
            }
            else if (frame >= curStartFrame && frame <= curEndFrame)
            {
                // The applied frame is within the interval of the current timewarp
                origFrame = timewarps[timewarpIndex].GetFrame(frame - curStartFrame) + curOrigStartFrame;
                break;
            }
        }

        return origFrame;
    }

    // Update the length of the animation instance in frames
    protected virtual void _UpdateFrameLengths()
    {
        int maxFrameLength = -1;
        foreach (KeyValuePair<AnimationTrackType, List<ITimewarp>> kvp in _timewarps)
        {
            int clipLength = Mathf.RoundToInt(AnimationClip.length * LEAPCore.editFrameRate);
            int timewarpLength = 0;
            int origTimewarpLength = 0;
            for (int timewarpIndex = 0; timewarpIndex < kvp.Value.Count; ++timewarpIndex)
            {
                timewarpLength += kvp.Value[timewarpIndex].FrameLength;
                origTimewarpLength += kvp.Value[timewarpIndex].OrigFrameLength;
            }

            int frameLength = timewarpLength + (clipLength - origTimewarpLength);
            maxFrameLength = Mathf.Max(frameLength, maxFrameLength);
            _frameLengths[kvp.Key] = frameLength;
        }

        _frameLength = maxFrameLength;
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
        _endEffectorTagsForTracks[AnimationTrackType.All] = new List<string>();
    }

    // Initialize timewarps
    protected virtual void _InitTimewarps()
    {
        _timewarps[AnimationTrackType.Gaze] = new List<ITimewarp>();
        _timewarpStartFrames[AnimationTrackType.Gaze] = new List<int>();
        _frameLengths[AnimationTrackType.Gaze] = 0;
        _timewarps[AnimationTrackType.LArmGesture] = new List<ITimewarp>();
        _timewarpStartFrames[AnimationTrackType.LArmGesture] = new List<int>();
        _frameLengths[AnimationTrackType.LArmGesture] = 0;
        _timewarps[AnimationTrackType.RArmGesture] = new List<ITimewarp>();
        _timewarpStartFrames[AnimationTrackType.RArmGesture] = new List<int>();
        _frameLengths[AnimationTrackType.RArmGesture] = 0;
        _timewarps[AnimationTrackType.Posture] = new List<ITimewarp>();
        _timewarpStartFrames[AnimationTrackType.Posture] = new List<int>();
        _frameLengths[AnimationTrackType.Posture] = 0;
        _timewarps[AnimationTrackType.Locomotion] = new List<ITimewarp>();
        _timewarpStartFrames[AnimationTrackType.Locomotion] = new List<int>();
        _frameLengths[AnimationTrackType.Locomotion] = 0;
        _timewarps[AnimationTrackType.All] = new List<ITimewarp>();
        _timewarpStartFrames[AnimationTrackType.All] = new List<int>();
        _frameLengths[AnimationTrackType.All] = 0;
    }
}

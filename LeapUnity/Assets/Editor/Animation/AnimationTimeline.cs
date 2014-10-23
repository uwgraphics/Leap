using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Animation timeline for playing back, layering, and adapting
/// character animation clips and procedural animations.
/// </summary>
public class AnimationTimeline
{
    /// <summary>
    /// Character animation instance scheduled to run at specified time
    /// on the timeline.
    /// </summary>
    public class ScheduledInstance
    {
        /// <summary>Underlying animation instance.</summary>
        public AnimationInstance Animation { get; private set; }

        /// <summary>Animation start time on the timeline.</summary>
        public int StartFrame { get; private set; }

        /// <summary>Animation instance ID.</summary>
        public int InstanceId { get; private set; }

        /// <summary>Layer container containing this animation instnace.</summary>
        public LayerContainer LayerContainer { get; private set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="startFrame"></param>
        /// <param name="animation"></param>
        /// <param name="layerContainer"></param>
        public ScheduledInstance(int id, int startFrame, AnimationInstance animation, LayerContainer layerContainer)
        {
            this.InstanceId = id;
            this.StartFrame = startFrame;
            this.Animation = animation;
            this.LayerContainer = layerContainer;
        }

        public void _SetStartFrame(int startFrame) { StartFrame = startFrame; }
    }

    // TOOD: need a way to enable/disable layers

    /// <summary>
    /// Layer container holds scheduled animation instances on
    /// the current layer of the timeline.
    /// </summary>
    public class LayerContainer
    {
        /// <summary>
        /// Layer mode for the current animation layer.
        /// </summary>
        public AnimationLayerMode LayerMode
        {
            get;
            private set;
        }

        /// <summary>
        /// Layer index, defining the order in which layers are applied.
        /// </summary>
        public int LayerIndex
        {
            get;
            private set;
        }

        /// <summary>
        /// Layer name.
        /// </summary>
        public string LayerName
        {
            get;
            set;
        }

        /// <summary>
        /// Timeline which owns the current layer container.
        /// </summary>
        public AnimationTimeline OwningTimeline
        {
            get;
            private set;
        }

        /// <summary>
        /// List of scheduled animation instances on the current layer of the timeline.
        /// </summary>
        public IList<ScheduledInstance> Animations
        {
            get { return _animationInstances.AsReadOnly(); }
        }

        private List<ScheduledInstance> _animationInstances;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="layerMode">Layer mode</param>
        /// <param name="layerIndex">Layer index, defining the order in which layers are applied</param>
        /// <param name="layerName">Layer name</param>
        /// <param name="timeline">Timeline which owns the layer</param>
        public LayerContainer(AnimationLayerMode layerMode, int layerIndex, string layerName, AnimationTimeline timeline)
        {
            LayerMode = layerMode;
            LayerIndex = layerIndex;
            LayerName = layerName;
            OwningTimeline = timeline;

            _animationInstances = new List<ScheduledInstance>();
        }

        public List<ScheduledInstance> _GetAnimations()
        {
            return _animationInstances;
        }
    }

    /// <summary>
    /// List of layers in the current timeline.
    /// </summary>
    public IList<LayerContainer> Layers
    {
        get { return _layerContainers.AsReadOnly(); }
    }

    /// <summary>
    /// Is the animation timeline active?
    /// </summary>
    public bool Active
    {
        get;
        set;
    }

    /// <summary>
    /// Is the animation on the timeline playing?
    /// </summary>
    public bool Playing
    {
        get;
        private set;
    }

    /// <summary>
    /// Current frame index on the timeline.
    /// </summary>
    public int CurrentFrame
    {
        get { return (int)(CurrentTime * LEAPCore.editFrameRate + 0.5f); }
    }

    /// <summary>
    /// Current time index on the timeline in seconds.
    /// </summary>
    public float CurrentTime
    {
        get { return _currentTime; }
        private set
        {
            _currentTime = value;
            if (_currentTime < 0f)
                _currentTime = 0f;
            else if (_currentTime > TimeLength)
                _currentTime = TimeLength;
        }
    }

    /// <summary>
    /// Length of the timeline in frames.
    /// </summary>
    public int FrameLength
    {
        get
        {
            ScheduledInstance lastInstance = null;
            int frameLength = 0;
            foreach (var layerContainer in _layerContainers)
            {
                foreach (var instance in layerContainer.Animations)
                {
                    if (lastInstance == null ||
                        (instance.StartFrame + instance.Animation.FrameLength) > frameLength)
                    {
                        lastInstance = instance;
                        frameLength = lastInstance.StartFrame + lastInstance.Animation.FrameLength;
                    }
                }
            }

            return frameLength;
        }
    }

    /// <summary>
    /// Length of the timeline in seconds.
    /// </summary>
    public float TimeLength
    {
        get { return (1f / LEAPCore.editFrameRate) * FrameLength; }
    }

    private List<LayerContainer> _layerContainers;
    private Dictionary<int, ScheduledInstance> _animationInstancesById;
    private float _currentTime = 0;
    private int _nextInstanceId = 0;

    /// <summary>
    /// Constructor.
    /// </summary>
    public AnimationTimeline()
    {
        _layerContainers = new List<LayerContainer>();
        _animationInstancesById = new Dictionary<int, ScheduledInstance>();

        Active = false;
        Playing = false;
    }

    /// <summary>
    /// Add a new layer to the timeline.
    /// </summary>
    /// <param name="layerMode">Layer mode</param>
    /// <param name="layerIndex">Layer index, defining the order in which layers are applied</param>
    /// <param name="layerName">Layer name</param>
    /// <returns>New layer container created by the method</returns>
    public LayerContainer AddLayer(AnimationLayerMode layerMode, int layerIndex, string layerName)
    {
        if (_layerContainers.Any(layerContainer => layerContainer.LayerName == layerName))
        {
            throw new Exception(string.Format("There already exists a layer named {0}", layerName));
        }

        // Add new layer container in appropriate order (based on layer index)
        var newLayerContainer = new LayerContainer(layerMode, layerIndex, layerName, this);
        bool newLayerInserted = false;
        for (int layerContainerIndex = 0; layerContainerIndex < _layerContainers.Count; ++layerContainerIndex)
        {
            var layerContainer = _layerContainers[layerContainerIndex];
            if (layerContainer.LayerIndex > layerIndex)
            {
                _layerContainers.Insert(layerContainerIndex, newLayerContainer);
                newLayerInserted = true;
                break;
            }
        }
        if (!newLayerInserted)
            _layerContainers.Add(newLayerContainer);

        return newLayerContainer;
    }

    /// <summary>
    /// Remove an existing layer from the timeline.
    /// </summary>
    /// <param name="layerName">Layer name</param>
    public void RemoveLayer(string layerName)
    {
        RemoveAllAnimations(layerName);
        _layerContainers.RemoveAll(layerContainer => layerContainer.LayerName == layerName);
    }

    /// <summary>
    /// Remove all layers from the timeline.
    /// </summary>
    public void RemoveAllLayers()
    {
        foreach (var layerContainer in _layerContainers)
            RemoveAllAnimations(layerContainer.LayerName);
        _layerContainers.Clear();
    }

    /// <summary>
    /// Get layer container by name.
    /// </summary>
    /// <param name="layerIndex">Layer name</param>
    /// <returns>Layer container</returns>
    public LayerContainer GetLayer(string layerName)
    {
        return _layerContainers.FirstOrDefault(layerContainer => layerContainer.LayerName == layerName);
    }

    /// <summary>
    /// Add a new animation instance to a layer.
    /// </summary>
    /// <param name="layerIndex">Layer name</param>
    /// <param name="animation">Animation instance</param>
    /// <param name="startFrame">Animation start frame on the timeline</param>
    /// <returns>Animation instance ID</returns>
    public int AddAnimation(string layerName, AnimationInstance animation, int startFrame)
    {
        if (!_layerContainers.Any(layerContainer => layerContainer.LayerName == layerName))
        {
            throw new Exception(string.Format("There is no layer named {0}", layerName));
        }

        // Schedule the animation instance in the appropriate order (based on start frame)
        var targetLayerContainer = GetLayer(layerName);
        var newInstance = new ScheduledInstance(_nextInstanceId++, startFrame, animation, targetLayerContainer);
        _AddAnimationToLayerContainer(newInstance, targetLayerContainer);

        // Also add the instance so it can be fetched by ID
        _animationInstancesById.Add(newInstance.InstanceId, newInstance);

        return newInstance.InstanceId;
    }

    /// <summary>
    /// Remove an existing animation instance from the timeline.
    /// </summary>
    /// <param name="animationInstanceId">Animation instance ID</param>
    /// <param name="startFrame">Animation start frame on the timeline</param>
    public void RemoveAnimation(int animationInstanceId, int startFrame)
    {
        if (!_animationInstancesById.ContainsKey(animationInstanceId))
            return;

        ScheduledInstance instanceToRemove = _animationInstancesById[animationInstanceId];
        instanceToRemove.LayerContainer._GetAnimations().Remove(instanceToRemove);
        _animationInstancesById.Remove(animationInstanceId);
    }

    /// <summary>
    /// Remove all animations from the specified layer.
    /// </summary>
    /// <param name="layerName">Layer name</param>
    public void RemoveAllAnimations(string layerName)
    {
        if (!_layerContainers.Any(layerContainer => layerContainer.LayerName == layerName))
            return;

        var targetLayerContainer = GetLayer(layerName);
        foreach (var instance in targetLayerContainer.Animations)
        {
            _animationInstancesById.Remove(instance.InstanceId);
        }
        targetLayerContainer._GetAnimations().Clear();
    }

    /// <summary>
    /// Get animation instance.
    /// </summary>
    /// <param name="animationInstanceId">Animation instance ID</param>
    /// <returns>Animation instance</returns>
    public AnimationInstance GetAnimation(int animationInstanceId)
    {
        if (!_animationInstancesById.ContainsKey(animationInstanceId))
            return null;

        return _animationInstancesById[animationInstanceId].Animation;
    }

    /// <summary>
    /// Get animation start frame.
    /// </summary>
    /// <param name="animationInstanceId">Animation instance ID</param>
    /// <returns>Animation start frame</returns>
    public int GetAnimationStartFrame(int animationInstanceId)
    {
        if (!_animationInstancesById.ContainsKey(animationInstanceId))
        {
            throw new Exception(string.Format("Animation instance with ID {0} does not exist", animationInstanceId));
        }

        return _animationInstancesById[animationInstanceId].StartFrame;
    }

    /// <summary>
    /// Set animation start frame.
    /// </summary>
    /// <param name="animationInstanceId">Animation instance ID</param>
    /// <param name="startFrame">Animation start frame</param>
    public void SetAnimationStartFrame(int animationInstanceId, int startFrame)
    {
        if (!_animationInstancesById.ContainsKey(animationInstanceId))
        {
            throw new Exception(string.Format("Animation instance with ID {0} does not exist", animationInstanceId));
        }

        // Temporarily remove animation from its layer container
        var modifiedInstance = _animationInstancesById[animationInstanceId];
        var targetLayerContainer = modifiedInstance.LayerContainer;
        targetLayerContainer._GetAnimations().Remove(modifiedInstance);
        
        // Update the instance start time and add it to the layer container again
        modifiedInstance._SetStartFrame(startFrame);
        _AddAnimationToLayerContainer(modifiedInstance, targetLayerContainer);
    }

    /// <summary>
    /// Start playback of animation on the timeline.
    /// </summary>
    public void Play()
    {
        Playing = true;
    }

    /// <summary>
    /// Stop playback of animation on the timeline.
    /// </summary>
    public void Stop()
    {
        Playing = false;
    }

    /// <summary>
    /// Step to next frame on the timeline.
    /// </summary>
    public void NextFrame()
    {
        CurrentTime = CurrentFrame < FrameLength - 1 ? ((float)(CurrentFrame + 1)) / LEAPCore.editFrameRate : CurrentTime;
    }

    /// <summary>
    /// Step to previous frame on the timeline.
    /// </summary>
    public void PreviousFrame()
    {
        CurrentTime = CurrentFrame > 0 ? ((float)(CurrentFrame - 1)) / LEAPCore.editFrameRate : CurrentTime;
    }

    /// <summary>
    /// Go to specified frame index on the timeline.
    /// </summary>
    /// <param name="frame">Frame index</param>
    public void GoToFrame(int frame)
    {
        frame = Mathf.Clamp(frame, 0, FrameLength - 1);
        CurrentTime = ((float)frame) / LEAPCore.editFrameRate;
    }

    /// <summary>
    /// Go to specified time index on the timeline.
    /// </summary>
    /// <param name="time">Time index</param>
    public void GoToTime(float time)
    {
        CurrentTime = time;
    }

    /// <summary>
    /// Bake all animation on the timeline into an animation clip.
    /// </summary>
    /// <param name="animationClipName">Animation clip name</param>
    /// <returns>Animation clip</returns>
    public void Bake(string animationClipName)
    {
        BakeRange(animationClipName, 0, FrameLength);
    }

    /// <summary>
    /// Bake a range of frames on the timeline into an animation clip.
    /// </summary>
    /// <param name="animationClipName">Animation clip name</param>
    /// <param name="startFrame">Start frame index</param>
    /// <param name="length">Range length in frames</param>
    /// <returns>Animation clip</returns>
    public void BakeRange(string animationClipName, int startFrame, int length)
    {
        // Get all character models animated using the timeline
        var models = new HashSet<ModelController>();
        foreach (KeyValuePair<int, ScheduledInstance> kvp in _animationInstancesById)
        {
            var instance = kvp.Value.Animation;
            models.Add(instance.Model);
        }

        // Ensure each model has an animation clip with the specified name
        foreach (var model in models)
        {
            AnimationClip newClip = null;

            // Does the model already have an animation clip with that name?
            var animationComponent = model.gameObject.animation;
            foreach (AnimationState animationState in animationComponent)
            {
                if (animationState.name == animationClipName)
                {
                    newClip = animationState.clip;
                    newClip.ClearCurves();
                }
            }

            if (newClip == null)
            {
                // Model does not have an animation clip with the specified name, so create a new one
                newClip = new AnimationClip();
                AnimationUtility.SetAnimationType(newClip, ModelImporterAnimationType.Legacy);
                animationComponent.AddClip(newClip, newClip.name);
            }
        }

        // For each model, retrieve its nodes and create empty anim. curves for them
        var curvesPerModel = new Dictionary<string, AnimationCurve[]>();
        foreach (var model in models)
        {
            Transform[] bones = ModelController.GetAllBones(model.gameObject);
            curvesPerModel[model.gameObject.name] = new AnimationCurve[6 + (bones.Length - 1)*3];
            // 6 animated properties for the root, 3 for the other bones
        }

        // Apply the animation at each frame in the range and bake the resulting frame to the curve on each model
        GoToFrame(startFrame);
        while (CurrentFrame <= startFrame + length && CurrentFrame != FrameLength)
        {
            _ApplyAnimation();

            foreach (var model in models)
            {
                Transform[] bones = ModelController.GetAllBones(model.gameObject);
                for (int boneIndex = 0; boneIndex < bones.Length; ++boneIndex)
                {
                    var bone = bones[boneIndex];

                    if (boneIndex == 0)
                    {
                        // Key position on the root bone

                        var positionKeyframe = new Keyframe();
                        positionKeyframe.time = ((float)(CurrentFrame - startFrame)) / LEAPCore.editFrameRate;

                        positionKeyframe.value = bone.localPosition.x;
                        curvesPerModel[model.gameObject.name][boneIndex * 6].AddKey(positionKeyframe);

                        positionKeyframe.value = bone.localPosition.y;
                        curvesPerModel[model.gameObject.name][boneIndex * 6 + 1].AddKey(positionKeyframe);

                        positionKeyframe.value = bone.localPosition.z;
                        curvesPerModel[model.gameObject.name][boneIndex * 6 + 2].AddKey(positionKeyframe);
                    }

                    // Key rotation

                    var rotationKeyFrame = new Keyframe();
                    rotationKeyFrame.time = ((float)(CurrentFrame - startFrame)) / LEAPCore.editFrameRate;

                    rotationKeyFrame.value = bone.localRotation.x;
                    curvesPerModel[model.gameObject.name][3 + boneIndex * 3].AddKey(rotationKeyFrame);

                    rotationKeyFrame.value = bone.localRotation.y;
                    curvesPerModel[model.gameObject.name][3 + boneIndex * 3 + 1].AddKey(rotationKeyFrame);

                    rotationKeyFrame.value = bone.localRotation.z;
                    curvesPerModel[model.gameObject.name][3 + boneIndex * 3 + 2].AddKey(rotationKeyFrame);
                }
            }
        }

        // Set the curves to their animation clips on each model
        foreach (var model in models)
        {
            AnimationClip newClip = null;

            // Get the animation clip
            var animationComponent = model.gameObject.animation;
            foreach (AnimationState animationState in animationComponent)
            {
                if (animationState.name == animationClipName)
                    newClip = animationState.clip;
            }

            // Set curves on that clip
            Transform[] bones = ModelController.GetAllBones(model.gameObject);
            for (int boneIndex = 0; boneIndex < bones.Length; ++boneIndex)
            {
                var bone = bones[boneIndex];

                if (boneIndex == 0)
                {
                    // Set position curves on root bone
                    newClip.SetCurve("", typeof(Transform), "localPosition.x", curvesPerModel[model.gameObject.name][boneIndex * 6]);
                    newClip.SetCurve("", typeof(Transform), "localPosition.y", curvesPerModel[model.gameObject.name][boneIndex * 6 + 1]);
                    newClip.SetCurve("", typeof(Transform), "localPosition.z", curvesPerModel[model.gameObject.name][boneIndex * 6 + 2]);
                }

                // Set rotation curves
                newClip.SetCurve("", typeof(Transform), "localRotation.x", curvesPerModel[model.gameObject.name][3 + boneIndex * 3]);
                newClip.SetCurve("", typeof(Transform), "localRotation.y", curvesPerModel[model.gameObject.name][3 + boneIndex * 3 + 1]);
                newClip.SetCurve("", typeof(Transform), "localRotation.z", curvesPerModel[model.gameObject.name][3 + boneIndex * 3 + 2]);
            }
        }
    }

    public void Update(float deltaTime)
    {
        if (!Active)
        {
            // Timeline inactive, do nothing
            return;
        }

        if (Playing)
        {
            // Animation is playing, so advance time
            _AddTime(deltaTime);
        }

        _ApplyAnimation();
    }

    private void _AddTime(float deltaTime)
    {
        if (CurrentTime + deltaTime > TimeLength)
            CurrentTime += deltaTime - TimeLength;
        else
            CurrentTime += deltaTime;
    }

    private void _ApplyAnimation()
    {
        // Apply active animation instances in layers in correct order
        foreach (var layer in _layerContainers)
        {
            var animationsInLayer = layer.Animations;
            foreach (var animation in animationsInLayer)
            {
                if (animation.StartFrame > CurrentFrame)
                {
                    continue;
                }
                else if (animation.StartFrame + animation.Animation.FrameLength <= CurrentFrame)
                {
                    break;
                }
                else
                {
                    animation.Animation.Apply(CurrentFrame - animation.StartFrame, layer.LayerMode);
                }
            }
        }
    }

    private void _AddAnimationToLayerContainer(ScheduledInstance newInstance, LayerContainer targetLayerContainer)
    {
        bool newInstanceAdded = false;
        for (int instanceIndex = 0; instanceIndex < targetLayerContainer.Animations.Count; ++instanceIndex)
        {
            var instance = targetLayerContainer.Animations[instanceIndex];
            if (instance.StartFrame > newInstance.StartFrame)
            {
                targetLayerContainer._GetAnimations().Insert(instanceIndex, newInstance);
                newInstanceAdded = true;
                break;
            }
        }
        if (!newInstanceAdded)
            targetLayerContainer._GetAnimations().Add(newInstance);
    }
}

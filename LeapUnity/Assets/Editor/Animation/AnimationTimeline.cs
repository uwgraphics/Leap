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
    public delegate void LayerEvtH(string layerName);
    public delegate void AnimationEvtH(int animationInstanceId);

    /// <summary>
    /// Event triggered on every animation frame when a layer has been applied.
    /// </summary>
    public event LayerEvtH LayerApplied;

    /// <summary>
    /// Event triggered when an animation instance becomes active during playback.
    /// </summary>
    public event AnimationEvtH AnimationStarted;

    /// <summary>
    /// Event triggered when an animation instance becomes inactive during playback.
    /// </summary>
    public event AnimationEvtH AnimationFinished;

    private static AnimationTimeline _instance = null;

    /// <summary>
    /// Animation timeline instance.
    /// </summary>
    public static AnimationTimeline Instance
    {
        get
        {
            if (_instance == null)
                _instance = new AnimationTimeline();

            return _instance;
        }
    }

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

        /// <summary>
        /// If true, layer is enabled and animations within it are applied to models.
        /// </summary>
        public bool Active
        {
            get;
            set;
        }

        /// <summary>
        /// If true, end-effector constraints are enforced on this animation layer.
        /// </summary>
        public bool IKEnabled
        {
            get;
            set;
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
            Active = true;
            IKEnabled = false;
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
            // TODO: catch a bug here
            if (_currentTime == float.NaN)
            {
                Debug.LogError("Tried setting current time on timeline to NaN");
                _currentTime = 0;
            }
            //
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

    /// <summary>
    /// Playback rate of the animation.
    /// </summary>
    public float TimeScale
    {
        get;
        set;
    }

    private Dictionary<string, AnimationInstance> _initialPoseAnimationInstances;
    private List<LayerContainer> _layerContainers;
    private Dictionary<int, ScheduledInstance> _animationInstancesById;
    private float _currentTime = 0;
    private int _nextInstanceId = 0;
    private HashSet<int> _activeAnimationInstanceIds;
    private Dictionary<string, ModelPose> _storedModelPoses;

    /// <summary>
    /// Constructor.
    /// </summary>
    private AnimationTimeline()
    {
        _initialPoseAnimationInstances = new Dictionary<string, AnimationInstance>();
        _layerContainers = new List<LayerContainer>();
        _animationInstancesById = new Dictionary<int, ScheduledInstance>();
        _activeAnimationInstanceIds = new HashSet<int>();
        _storedModelPoses = new Dictionary<string, ModelPose>();

        Active = false;
        Playing = false;
        TimeScale = 0.25f;
    }

    /// <summary>
    /// Get all character models animated on this animation timeline.
    /// </summary>
    /// <returns>List of models</returns>
    public ModelController[] GetAllModels()
    {
        var models = new HashSet<ModelController>();
        foreach (KeyValuePair<int, ScheduledInstance> kvp in _animationInstancesById)
        {
            var instance = kvp.Value.Animation;
            models.Add(instance.Model);
        }

        return models.ToArray();
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
    /// Initialize the animation timeline
    /// </summary>
    public void Init()
    {
        _InitControllers();
        _InitIK();
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
        ModelController[] models = GetAllModels();

        // Ensure each model has an animation clip with the specified name
        foreach (var model in models)
        {
            LEAPAssetUtils.CreateAnimationClipOnModel(animationClipName, model.gameObject);
        }

        // For each model, retrieve its nodes and create empty anim. curves for them
        var curvesPerModel = new Dictionary<string, AnimationCurve[]>();
        foreach (var model in models)
        {
            curvesPerModel[model.gameObject.name] = LEAPAssetUtils.CreateAnimationCurvesForModel(model.gameObject);
        }

        // Apply the animation at each frame in the range and bake the resulting frame to the curve on each model
        GoToFrame(startFrame);
        while (CurrentFrame < startFrame + length - 1 && CurrentFrame < FrameLength - 1)
        {
            _ApplyAnimation();

            foreach (var model in models)
            {
                Transform[] bones = ModelUtils.GetAllBones(model.gameObject);
                for (int boneIndex = 0; boneIndex < bones.Length; ++boneIndex)
                {
                    var bone = bones[boneIndex];

                    if (boneIndex == 0)
                    {
                        // Key position on the root bone

                        var positionKeyframe = new Keyframe();
                        positionKeyframe.time = ((float)(CurrentFrame - startFrame)) / LEAPCore.editFrameRate;

                        positionKeyframe.value = bone.localPosition.x;
                        curvesPerModel[model.gameObject.name][0].AddKey(positionKeyframe);

                        positionKeyframe.value = bone.localPosition.y;
                        curvesPerModel[model.gameObject.name][1].AddKey(positionKeyframe);

                        positionKeyframe.value = bone.localPosition.z;
                        curvesPerModel[model.gameObject.name][2].AddKey(positionKeyframe);
                    }

                    // Key rotation

                    var rotationKeyFrame = new Keyframe();
                    rotationKeyFrame.time = ((float)(CurrentFrame - startFrame)) / LEAPCore.editFrameRate;

                    rotationKeyFrame.value = bone.localRotation.x;
                    curvesPerModel[model.gameObject.name][3 + boneIndex * 4].AddKey(rotationKeyFrame);

                    rotationKeyFrame.value = bone.localRotation.y;
                    curvesPerModel[model.gameObject.name][3 + boneIndex * 4 + 1].AddKey(rotationKeyFrame);

                    rotationKeyFrame.value = bone.localRotation.z;
                    curvesPerModel[model.gameObject.name][3 + boneIndex * 4 + 2].AddKey(rotationKeyFrame);

                    rotationKeyFrame.value = bone.localRotation.w;
                    curvesPerModel[model.gameObject.name][3 + boneIndex * 4 + 3].AddKey(rotationKeyFrame);
                }
            }

            NextFrame();
        }

        // Set the curves to their animation clips on each model
        foreach (var model in models)
        {
            AnimationClip newClip = LEAPAssetUtils.GetAnimationClipOnModel(animationClipName, model.gameObject);
            LEAPAssetUtils.SetAnimationCurvesOnClip(model.gameObject, newClip, curvesPerModel[model.gameObject.name]);
        }
    }

    /// <summary>
    /// Go through the entire animation and bake the outputs of all animation
    /// instances into animation clips.
    /// </summary>
    public void BakeInstances()
    {
        Debug.Log("Baking all animation instances...");

        // Initialize each model's animation tree
        _InitControllers();

        // Set all instance to bake
        foreach (KeyValuePair<int, ScheduledInstance> kvp in _animationInstancesById)
            kvp.Value.Animation.IsBaking = true;
        
        // Apply & bake instances
        GoToFrame(0);
        /*while (CurrentFrame < FrameLength - 1)
        {
            _AddTime(1f / LEAPCore.editFrameRate);
            _ApplyAnimation();
        }

        // Save baked instances to animation clips
        foreach (KeyValuePair<int, ScheduledInstance> kvp in _animationInstancesById)
            kvp.Value.Animation.FinalizeBake();

        Debug.Log("Finished baking all animation instances.");*/
    }

    /// <summary>
    /// Reset all character models to initial pose, encoded in
    /// InitialPose animation clip.
    /// </summary>
    public void ResetModelsToInitialPose()
    {
        // Set all models to initial pose
        ModelController[] models = GetAllModels();
        foreach (var model in models)
        {
            AnimationInstance initialPoseInstance = null;
            if (!_initialPoseAnimationInstances.ContainsKey(model.gameObject.name))
            {
                initialPoseInstance = new AnimationClipInstance(model.gameObject, "InitialPose");
                _initialPoseAnimationInstances[model.gameObject.name] = initialPoseInstance;
            }
            else
            {
                initialPoseInstance = _initialPoseAnimationInstances[model.gameObject.name];
            }

            initialPoseInstance.Apply(0, AnimationLayerMode.Override);

            model.Init();
        }
    }

    /// <summary>
    /// Store model pose, so that it can be reapplied later.
    /// </summary>
    /// <param name="model">Character model name</param>
    /// <param name="poseName">Pose name</param>
    public void StoreModelPose(string modelName, string poseName)
    {
        // Get model
        ModelController[] models = GetAllModels();
        ModelController model = models.FirstOrDefault(mc => mc.gameObject.name == modelName);
        if (model == null)
        {
            throw new Exception(string.Format("Cannot store pose {0} on model {1}, because the model does not exist", poseName, modelName));
        }

        _storedModelPoses[poseName] = new ModelPose(model.gameObject);
    }

    /// <summary>
    /// Apply stored model pose.
    /// </summary>
    /// <param name="model">Character model name</param>
    /// <param name="poseName">Pose name</param>
    public void ApplyModelPose(string modelName, string poseName)
    {
        // Get model
        ModelController[] models = GetAllModels();
        ModelController model = models.FirstOrDefault(mc => mc.gameObject.name == modelName);
        if (model == null)
        {
            throw new Exception(string.Format("Cannot apply pose {0} to model {1}: model does not exist", poseName, modelName));
        }

        if (!_storedModelPoses.ContainsKey(poseName))
        {
            throw new Exception(string.Format("Cannot apply pose {0} to model {1}: pose does not exist", poseName, modelName));
        }

        _storedModelPoses[poseName].Apply(model.gameObject);
    }

    /// <summary>
    /// Remove stored model pose.
    /// </summary>
    /// <param name="poseName">Pose nmae</param>
    public void RemoveModelPose(string poseName)
    {
        _storedModelPoses.Remove(poseName);
    }

    /// <summary>
    /// Update the animation timeline.
    /// </summary>
    /// <param name="deltaTime">Elapsed time since last update</param>
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
            _AddTime(deltaTime * TimeScale);
        }

        _ApplyAnimation();
    }

    public void _ApplyAnimation()
    {
        ResetModelsToInitialPose();

        // Apply active animation instances in layers in correct order
        foreach (var layer in _layerContainers)
        {
            if (!layer.Active)
                continue;

            var animationsInLayer = layer.Animations;
            foreach (var animation in animationsInLayer)
            {
                if (animation.StartFrame > CurrentFrame ||
                    animation.StartFrame + animation.Animation.FrameLength <= CurrentFrame)
                {
                    if (_activeAnimationInstanceIds.Contains(animation.InstanceId))
                    {
                        // This animation instance has just become inactive

                        animation.Animation.Finish();
                        _activeAnimationInstanceIds.Remove(animation.InstanceId);

                        Debug.Log(string.Format("{0}: Deactivating animation instance {1} on model {2}",
                            CurrentFrame, animation.Animation.AnimationClip.name, animation.Animation.Model.gameObject.name));

                        // Notify listeners that the animation instance has just become inactive
                        AnimationFinished(animation.InstanceId);
                    }

                    continue;
                }
                else
                {
                    // This animation instance is active, so apply it

                    if (!_activeAnimationInstanceIds.Contains(animation.InstanceId))
                    {
                        // This animation instance has just become active

                        _activeAnimationInstanceIds.Add(animation.InstanceId);
                        animation.Animation.Start();

                        Debug.Log(string.Format("{0}: Activating animation instance {1} on model {2}",
                            CurrentFrame, animation.Animation.AnimationClip.name, animation.Animation.Model.gameObject.name));

                        // Notify listeners that the animation instance has just become active
                        AnimationStarted(animation.InstanceId);
                    }

                    animation.Animation.Apply(CurrentFrame - animation.StartFrame, layer.LayerMode);
                }
            }

            if (layer.IKEnabled)
                // Set up IK goals for this animation layer
                _SetIKGoals();

            // Notify listeners that the layer has finished applying
            LayerApplied(layer.LayerName);
        }

        // Apply any active end-effector constraints
        _SolveIK();
        _ClearIKGoals();

        _StoreModelsCurrentPose();
    }

    private void _AddTime(float deltaTime)
    {
        if (CurrentTime + deltaTime > TimeLength)
            CurrentTime += (deltaTime - TimeLength);
        else
            CurrentTime += deltaTime;

        //Debug.Log(string.Format("START FRAME (deltaTime = {0} sec, CurrentFrame = {1}", deltaTime, CurrentFrame));

        // Make sure controllers have access to the latest delta time
        ModelController[] models = GetAllModels();
        foreach (var model in models)
            model.GetComponent<AnimControllerTree>().DeltaTime = deltaTime;
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

    private void _StoreModelsCurrentPose()
    {
        ModelController[] models = GetAllModels();
        foreach (var model in models)
        {
            model._StoreCurrentPose();
        }
    }

    private void _InitControllers()
    {
        ModelController[] models = GetAllModels();
        foreach (var model in models)
        {
            var animControllerTree = model.gameObject.GetComponent<AnimControllerTree>();
            if (animControllerTree != null)
                animControllerTree.Start();

            var gazeController = model.gameObject.GetComponent<GazeController>();
            if (gazeController != null)
                gazeController.fixGaze = false;
        }
    }

    private void _InitIK()
    {
        ModelController[] models = GetAllModels();
        foreach (var model in models)
        {
            IKSolver[] solvers = model.gameObject.GetComponents<IKSolver>();
            foreach (var solver in solvers)
                solver.Init();
        }
    }

    private void _SetIKGoals()
    {
        // TODO: implement this properly - IK goals should be determined
        // from annotated end-effector constraints on animation clips

        ModelController[] models = GetAllModels();
        foreach (var model in models)
        {
            IKSolver[] solvers = model.gameObject.GetComponents<IKSolver>();
            foreach (var solver in solvers)
            {
                if (solver is LimbIKSolver)
                {
                    var limbSolver = solver as LimbIKSolver;
                    limbSolver.goals[0].position = limbSolver.goals[0].endEffector.position;
                    //limbSolver.goals[0].position = GameObject.FindGameObjectsWithTag("GazeTarget").First(obj => obj.name == "TestIK").transform.position;
                    limbSolver.goals[0].weight = 1f;
                }
            }
        }
    }

    private void _ClearIKGoals()
    {
        ModelController[] models = GetAllModels();
        foreach (var model in models)
        {
            IKSolver[] solvers = model.gameObject.GetComponents<IKSolver>();
            foreach (var solver in solvers)
            {
                foreach (var goal in solver.goals)
                    goal.weight = 0f;
            }
        }
    }

    private void _SolveIK()
    {
        ModelController[] models = GetAllModels();
        foreach (var model in models)
        {
            IKSolver[] solvers = model.gameObject.GetComponents<IKSolver>();
            foreach (var solver in solvers)
            {
                solver.Solve();
            }
        }
    }
}

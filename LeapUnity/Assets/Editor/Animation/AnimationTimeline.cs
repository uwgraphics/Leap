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
    public delegate void TimelineControlEvtH(bool controlState);
    public delegate void AllAnimationEvtH();
    public delegate void LayerEvtH(string layerName);
    public delegate void AnimationEvtH(int animationInstanceId);

    /// <summary>
    /// Event triggered when animation timeline is activated.
    /// </summary>
    public event TimelineControlEvtH TimelineActivated;

    /// <summary>
    /// Event triggered when animation timeline is deactivated.
    /// </summary>
    public event TimelineControlEvtH TimelineDeactivated;

    /// <summary>
    /// Event triggered when animation timeline is deactivated.
    /// </summary>
    public event AllAnimationEvtH AllAnimationApplied;

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

        /// <summary>
        /// Animation end frame on the timeline.
        /// </summary>
        public int EndFrame
        {
            get { return StartFrame + Animation.FrameLength - 1; }
        }

        /// <summary>Animation instance ID.</summary>
        public int InstanceId { get; private set; }

        /// <summary>Layer container containing this animation instnace.</summary>
        public LayerContainer OwningLayer { get; private set; }

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
            this.OwningLayer = layerContainer;
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
        /// If true, end-effector constraints are defined and enforced on this animation layer.
        /// </summary>
        public bool isIKEndEffectorConstr
        {
            get;
            set;
        }

        /// <summary>
        /// If true, the current layer contains base animation for the body IK solver.
        /// </summary>
        public bool isBase
        {
            get;
            set;
        }

        /// <summary>
        /// If true, the current layer contains gaze control for the body IK solver.
        /// </summary>
        public bool isGaze
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
            isIKEndEffectorConstr = false;
            isBase = false;
            isGaze = false;
        }

        public List<ScheduledInstance> _GetAnimations()
        {
            return _animationInstances;
        }
    }

    /// <summary>
    /// List of character models animated by the current timeline.
    /// </summary>
    public IList<GameObject> Models
    {
        get { return _models.AsReadOnly();  }
    }

    /// <summary>
    /// Root object of the environment.
    /// </summary>
    public GameObject Environment
    {
        get { return _environment; }
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
        get { return _active; }

        set
        { 
            _active = value;

            if (!_active)
                _activeAnimationInstanceIds.Clear();
        }
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
        get { return Mathf.RoundToInt(CurrentTime * LEAPCore.editFrameRate); }
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

    /// <summary>
    /// Playback rate of the animation.
    /// </summary>
    public float TimeScale
    {
        get;
        set;
    }

    /// <summary>
    /// If true, animation instances on the timeline are currently being baked into
    /// animation clips.
    /// </summary>
    public bool IsBakingInstances
    {
        get
        {
            return _animationInstancesById.Any(kvp => kvp.Value.Animation.IsBaking);
        }
    }

    private List<GameObject> _models;
    private GameObject _environment;
    private List<LayerContainer> _layerContainers;
    private Dictionary<int, ScheduledInstance> _animationInstancesById;
    private Dictionary<AnimationClip, EndEffectorConstraintContainer> _endEffectorConstraints;

    public bool _active = false;
    private float _currentTime = 0;
    private int _nextInstanceId = 0;
    private HashSet<int> _activeAnimationInstanceIds;
    private Dictionary<string, Dictionary<string, ModelPose>> _storedModelPoses;
    private Dictionary<GameObject, Transform> _activeManipulatedObjectHandles;

    /// <summary>
    /// Constructor.
    /// </summary>
    private AnimationTimeline()
    {
        _models = new List<GameObject>();
        _environment = null;
        _layerContainers = new List<LayerContainer>();
        _animationInstancesById = new Dictionary<int, ScheduledInstance>();
        _endEffectorConstraints = new Dictionary<AnimationClip, EndEffectorConstraintContainer>();
        _activeAnimationInstanceIds = new HashSet<int>();
        _storedModelPoses = new Dictionary<string, Dictionary<string, ModelPose>>();
        _activeManipulatedObjectHandles = new Dictionary<GameObject, Transform>();

        Active = false;
        Playing = false;
        TimeScale = 1f;
    }

    /// <summary>
    /// Add a character model to the timeline.
    /// </summary>
    /// <param name="model">Character model</param>
    public void AddModel(GameObject model)
    {
        if (_models.Any(m => m == model))
        {
            throw new Exception(string.Format("Character model {0} already added", model.name));
        }

        // Initialize the character's model controller
        var modelController = model.GetComponent<ModelController>();
        if (modelController == null)
        {
            throw new Exception(string.Format("Character model {0} does not have a ModelController", model.name));
        }
        modelController.Init();

        // Initialize the character morph controller
        var morphController = model.GetComponent<MorphController>();
        if (morphController != null)
        {
            morphController.Init();
        }

        // Apply & store initial pose for the model
        var initialPoseClip = model.GetComponent<Animation>().GetClip("InitialPose");
        if (initialPoseClip == null)
        {
            throw new Exception(string.Format("Character model {0} does not have an InitialPose animation defined", model.name));
        }
        var initialPoseInstance = new AnimationClipInstance(model, initialPoseClip.name);
        initialPoseInstance.Apply(0, AnimationLayerMode.Override);
        modelController.Init();

        // Add the model to the timeline
        _models.Add(model);
    }

    /// <summary>
    /// Remove a character model from the timeline.
    /// </summary>
    /// <param name="modelName">Character model name</param>
    public void RemoveModel(string modelName)
    {
        if (Layers.Any(l => l.Animations.Any(a => a.Animation.Model.name == modelName)))
        {
            throw new Exception(string.Format(
                "Cannot remove character model {0} from the timeline, because it is references in at least one animation instance", modelName));
        }

        _models.RemoveAll(m => m.name == modelName);
    }

    /// <summary>
    /// Remove all character models from the timeline.
    /// </summary>
    public void RemoveAllModels()
    {
        foreach (var layer in Layers)
            RemoveAllAnimations(layer.LayerName);
        _models.Clear();
    }

    /// <summary>
    /// Set environment containing objects manipulated in animations.
    /// </summary>
    /// <param name="environment">Environment root object</param>
    public void SetEnvironment(GameObject environment)
    {
        _environment = environment;
        if (environment == null)
            return;

        var envController = environment.GetComponent<EnvironmentController>();
        if (envController == null)
            throw new Exception(string.Format("Environment root object {0} does not have an EnvironmentController", environment.name));
        
        envController.Init();
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
    /// <param name="loadEndEffectorConstraints">If true, end-effector constraints will be loaded for the animation clip of the added instance</param>
    /// <returns>Animation instance ID</returns>
    public int AddAnimation(string layerName, AnimationInstance animation, int startFrame, bool loadEndEffectorConstraints = false)
    {
        if (!_layerContainers.Any(layerContainer => layerContainer.LayerName == layerName))
        {
            throw new Exception(string.Format("There is no layer named {0}", layerName));
        }

        // Ensure character model for this animation instance has been added to this timeline
        if (!_models.Any(m => m == animation.Model))
        {
            throw new Exception(string.Format("Character model {0} for animation {1} not defined on the current timeline",
                animation.Model.name, animation.AnimationClip.name));
        }

        // Schedule the animation instance in the appropriate order (based on start frame)
        var targetLayerContainer = GetLayer(layerName);
        var newInstance = new ScheduledInstance(_nextInstanceId++, startFrame, animation, targetLayerContainer);
        _AddAnimationToLayerContainer(newInstance, targetLayerContainer);

        // Also add the instance so it can be fetched by ID
        _animationInstancesById.Add(newInstance.InstanceId, newInstance);

        // Load end-effector constraints for that animation clip (if they exist and haven't already been loaded)
        if (loadEndEffectorConstraints && !_endEffectorConstraints.ContainsKey(newInstance.Animation.AnimationClip))
        {
            EndEffectorConstraint[] endEffectorConstraints = LEAPAssetUtils.LoadEndEffectorConstraintsForClip(newInstance.Animation.AnimationClip);
            if (endEffectorConstraints != null)
            {
                _endEffectorConstraints.Add(newInstance.Animation.AnimationClip,
                    new EndEffectorConstraintContainer(newInstance.Animation.AnimationClip, endEffectorConstraints));
            }
        }

        return newInstance.InstanceId;
    }

    /// <summary>
    /// Remove an existing animation instance from the timeline.
    /// </summary>
    /// <param name="animationInstanceId">Animation instance ID</param>
    public void RemoveAnimation(int animationInstanceId)
    {
        if (!_animationInstancesById.ContainsKey(animationInstanceId))
            return;

        // If this is the only instance using its animation clip, also remove end-effector constraints
        ScheduledInstance instanceToRemove = _animationInstancesById[animationInstanceId];
        var animationClip = instanceToRemove.Animation.AnimationClip;
        if (_endEffectorConstraints.ContainsKey(animationClip) &&
            !_animationInstancesById.Any(inst => inst.Value.Animation.AnimationClip == animationClip && inst.Value.InstanceId != animationInstanceId))
        {
            _endEffectorConstraints.Remove(animationClip);
        }

        // Then remove the animation instance itself
        instanceToRemove.OwningLayer._GetAnimations().Remove(instanceToRemove);
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

        var instanceIds = new List<int>(_animationInstancesById.Keys);
        foreach (int instanceId in instanceIds)
        {
            if (_animationInstancesById[instanceId].OwningLayer.LayerName == layerName)
                RemoveAnimation(instanceId);
        }
    }

    /// <summary>
    /// Remove all animations from the specified layer for a single model.
    /// </summary>
    /// <param name="layerName">Layer name</param>
    /// <param name="modelName">Character model name</param>
    public void RemoveAllAnimations(string layerName, string modelName)
    {
        if (!_layerContainers.Any(layerContainer => layerContainer.LayerName == layerName))
            return;

        var instanceIds = new List<int>(_animationInstancesById.Keys);
        foreach (int instanceId in instanceIds)
        {
            if (_animationInstancesById[instanceId].OwningLayer.LayerName == layerName &&
                _animationInstancesById[instanceId].Animation.Model.name == modelName)
                RemoveAnimation(instanceId);
        }
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
        var targetLayerContainer = modifiedInstance.OwningLayer;
        targetLayerContainer._GetAnimations().Remove(modifiedInstance);
        
        // Update the instance start time and add it to the layer container again
        modifiedInstance._SetStartFrame(startFrame);
        _AddAnimationToLayerContainer(modifiedInstance, targetLayerContainer);
    }

    /// <summary>
    /// Get layer containing the animation instance.
    /// </summary>
    /// <param name="animationInstanceId">Animation instance ID</param>
    /// <returns>Layer container</returns>
    public LayerContainer GetLayerForAnimation(int animationInstanceId)
    {
        if (!_animationInstancesById.ContainsKey(animationInstanceId))
        {
            throw new Exception(string.Format("Animation instance with ID {0} does not exist", animationInstanceId));
        }

        return _animationInstancesById[animationInstanceId].OwningLayer;
    }

    /// <summary>
    /// Find animation instance by animation clip name.
    /// </summary>
    /// <param name="animationName">Animation clip name</param>
    /// <returns>Animation instance ID</returns>
    public int FindAnimationByName(string animationName)
    {
        foreach (var layer in Layers)
        {
            foreach (var instance in layer.Animations)
            {
                if (instance.Animation.AnimationClip.name == animationName)
                    return instance.InstanceId;
            }
        }

        return -1;
    }

    /// <summary>
    /// Get constraints on the specific end-effector for specific animation.
    /// </summary>
    /// <param name="animationInstanceId">Animation instance ID</param>
    /// <returns>List of end-effector constraints</returns>
    public IList<EndEffectorConstraint> GetEndEffectorConstraintsForAnimation(int animationInstanceId, string endEffector)
    {
        if (!_animationInstancesById.ContainsKey(animationInstanceId))
        {
            throw new Exception(string.Format("Animation instance with ID {0} does not exist", animationInstanceId));
        }

        var animationClip = _animationInstancesById[animationInstanceId].Animation.AnimationClip;
        if (!_endEffectorConstraints.ContainsKey(animationClip))
        {
            // Animation has no end-effector constraints
            return null;
        }

        return _endEffectorConstraints[animationClip].GetConstraintsForEndEffector(endEffector);
    }

    /// <summary>
    /// Enable/disable all IK solvers on all loaded models.
    /// </summary>
    /// <param name="enabled">If true, solvers will be enabled, otherwise they will be disabled</param>
    public void SetIKEnabled(bool enabled = true)
    {
        var models = Models;
        foreach (var model in models)
        {
            IKSolver[] solvers = model.GetComponents<IKSolver>();
            foreach (var solver in solvers)
                solver.enabled = enabled;
        }
    }

    /// <summary>
    /// Add a new environment object animation instance to a layer.
    /// </summary>
    /// <param name="layerIndex">Layer name</param>
    /// <param name="animation">Animation instance</param>
    /// <param name="startFrame">Animation start frame on the timeline</param>
    /// <returns>Animation instance ID</returns>
    public int AddEnvironmentObjectAnimation(string layerName, AnimationInstance animation, int startFrame = 0)
    {
        if (!(animation is EnvironmentObjectAnimationInstance))
        {
            throw new Exception("Environment object animations must be of type EnvironmentObjectAnimationInstance");
        }
        var envObjAnimation = animation as EnvironmentObjectAnimationInstance;

        if (!_layerContainers.Any(layerContainer => layerContainer.LayerName == layerName))
        {
            throw new Exception(string.Format("There is no layer named {0}", layerName));
        }

        // Ensure character model for this animation instance has been added to this timeline
        if (_environment == null ||
            !_environment.GetComponent<EnvironmentController>().ManipulatedObjects.Any(obj => obj == envObjAnimation.Model))
        {
            throw new Exception(string.Format("Environment object {0} for animation {1} not defined on the current timeline",
                envObjAnimation.Model.name, envObjAnimation.AnimationClip.name));
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
    /// Get the ID of the animation instance applied to the specified model at the current frame in the specified layer.
    /// </summary>
    /// <param name="layerName">Layer name</param>
    /// <param name="modelName">Character model name</param>
    /// <returns>Animation instance ID</returns>
    public int GetCurrentAnimationInstanceId(string layerName, string modelName)
    {
        var layer = GetLayer(layerName);
        foreach (var scheduledInstance in layer.Animations)
        {
            var instance = scheduledInstance.Animation;

            if (CurrentFrame >= scheduledInstance.StartFrame &&
                CurrentFrame <= (scheduledInstance.StartFrame + instance.FrameLength - 1) &&
                scheduledInstance.Animation.Model.name == modelName)
                return scheduledInstance.InstanceId;
        }

        return -1;
    }

    /// <summary>
    /// Bake all animation on the timeline into animation clips.
    /// </summary>
    /// <param name="animationClipName">Animation clip names (one for each character model)</param>
    public void Bake(string[] animationClipNames)
    {
        BakeRange(animationClipNames, 0, FrameLength);
    }

    /// <summary>
    /// Bake a range of frames on the timeline into animation clips.
    /// </summary>
    /// <param name="animationClipName">Animation clip names (one for each character model)</param>
    /// <param name="startFrame">Start frame index</param>
    /// <param name="length">Range length in frames</param>
    /// <returns>Animation clip</returns>
    public void BakeRange(string[] animationClipNames, int startFrame, int length)
    {
        var models = Models;

        if (models.Count != animationClipNames.Length)
        {
            throw new Exception("Error baking the animation timeline: must specify an animation clip name for each model on the timeline");
        }

        // Ensure each model has an animation clip with the specified name
        for (int modelIndex = 0; modelIndex < models.Count; ++modelIndex)
        {
            var model = models[modelIndex];
            LEAPAssetUtils.CreateAnimationClipOnModel(animationClipNames[modelIndex], model);
        }

        // For each model, retrieve its nodes and create empty anim. curves for them
        var curvesPerModel = new Dictionary<string, AnimationCurve[]>();
        foreach (var model in models)
        {
            curvesPerModel[model.gameObject.name] = LEAPAssetUtils.CreateAnimationCurvesForModel(model);
        }

        // Apply the animation at each frame in the range and bake the resulting frame to the curve on each model
        GoToFrame(startFrame);
        while (CurrentFrame < startFrame + length - 1 && CurrentFrame < FrameLength - 1)
        {
            float time = ((float)(CurrentFrame - startFrame)) / LEAPCore.editFrameRate;
            ApplyAnimation();

            foreach (var model in models)
            {
                Transform[] bones = ModelUtils.GetAllBones(model);
                var modelController = model.GetComponent<ModelController>();

                // First bake bone properties
                for (int boneIndex = 0; boneIndex < bones.Length; ++boneIndex)
                {
                    var bone = bones[boneIndex];

                    if (boneIndex == 0)
                    {
                        // Key position on the root bone

                        var positionKeyframe = new Keyframe();
                        positionKeyframe.time = time;

                        positionKeyframe.value = bone.localPosition.x;
                        curvesPerModel[model.name][0].AddKey(positionKeyframe);

                        positionKeyframe.value = bone.localPosition.y;
                        curvesPerModel[model.name][1].AddKey(positionKeyframe);

                        positionKeyframe.value = bone.localPosition.z;
                        curvesPerModel[model.name][2].AddKey(positionKeyframe);
                    }

                    // Key rotation

                    var rotationKeyFrame = new Keyframe();
                    rotationKeyFrame.time = time;

                    rotationKeyFrame.value = bone.localRotation.x;
                    curvesPerModel[model.gameObject.name][3 + boneIndex * 4].AddKey(rotationKeyFrame);

                    rotationKeyFrame.value = bone.localRotation.y;
                    curvesPerModel[model.gameObject.name][3 + boneIndex * 4 + 1].AddKey(rotationKeyFrame);

                    rotationKeyFrame.value = bone.localRotation.z;
                    curvesPerModel[model.gameObject.name][3 + boneIndex * 4 + 2].AddKey(rotationKeyFrame);

                    rotationKeyFrame.value = bone.localRotation.w;
                    curvesPerModel[model.gameObject.name][3 + boneIndex * 4 + 3].AddKey(rotationKeyFrame);
                }

                // Next bake blend shape properties
                int numBlendShapes = modelController.NumberOfBlendShapes;
                for (int blendShapeIndex = 0; blendShapeIndex < numBlendShapes; ++blendShapeIndex)
                {
                    var keyFrame = new Keyframe();
                    keyFrame.time = time;
                    keyFrame.value = modelController.GetBlendShapeWeight(blendShapeIndex);
                    curvesPerModel[model.gameObject.name][3 + modelController.NumberOfBones * 4 + blendShapeIndex].AddKey(keyFrame);
                }
            }

            NextFrame();
        }

        // Set the curves to their animation clips on each model
        for (int modelIndex = 0; modelIndex < models.Count; ++modelIndex)
        {
            var model = models[modelIndex];
            AnimationClip newClip = LEAPAssetUtils.GetAnimationClipOnModel(animationClipNames[modelIndex], model);
            LEAPAssetUtils.SetAnimationCurvesOnClip(model.gameObject, newClip, curvesPerModel[model.name]);

            // Write animation clip to file
            string path = LEAPAssetUtils.GetModelDirectory(model) + newClip.name + ".anim";
            if (AssetDatabase.GetAssetPath(newClip) != path)
            {
                AssetDatabase.DeleteAsset(path);
                AssetDatabase.CreateAsset(newClip, path);
            }
            AssetDatabase.SaveAssets();
        }
    }

    /// <summary>
    /// Go through the entire animation and bake the outputs of all animation
    /// instances into animation clips.
    /// </summary>
    public void StartBakeInstances()
    {
        Debug.Log("Baking all animation instances...");

        GoToFrame(0);
        Advance(0);

        // Initialize each model's animation tree
        _InitControllers();

        // Set all instances to bake
        foreach (KeyValuePair<int, ScheduledInstance> kvp in _animationInstancesById)
            kvp.Value.Animation.StartBake();
    }

    /// <summary>
    /// Save all baked animation instances into animation clips and stop baking.
    /// </summary>
    public void FinalizeBakeInstances()
    {
        if (!IsBakingInstances)
        {
            throw new Exception("Tried to finalize baking animation instances when no animation instances are baking");
        }

        // Save any baked instances to animation clips
        foreach (KeyValuePair<int, ScheduledInstance> kvp in _animationInstancesById)
            if (kvp.Value.Animation.IsBaking)
                kvp.Value.Animation.FinalizeBake();

        Debug.Log("Finished baking all animation instances");
    }

    /// <summary>
    /// Go through the entire animation and bake the outputs of all animation
    /// instances into animation clips.
    /// </summary>
    public void BakeInstances()
    {
        StartBakeInstances();
        Active = false;
        Active = true;
        Play();
        for (int frameIndex = 0; frameIndex < FrameLength; ++frameIndex)
            Advance(1f / LEAPCore.editFrameRate);
        Stop();
        FinalizeBakeInstances();
    }

    /// <summary>
    /// Reset all character models to initial pose and the environment
    /// to initial layout.
    /// </summary>
    public void ResetModelsAndEnvironment()
    {
        // Set all models to initial pose
        var models = Models;
        foreach (var model in models)
        {
            model.GetComponent<ModelController>()._ResetToInitialPose();
            var morphController = model.GetComponent<MorphController>();
            if (morphController != null)
                morphController.ResetAllWeights();
        }

        if (Environment != null)
        {
            // Reset the environment to initial layout
            Environment.GetComponent<EnvironmentController>().ResetToInitialLayout();
        }
    }

    /// <summary>
    /// Store model pose, so that it can be reapplied later.
    /// </summary>
    /// <param name="model">Character model name</param>
    /// <param name="poseName">Pose name</param>
    public void StoreModelPose(string modelName, string poseName)
    {
        var model = Models.FirstOrDefault(m => m.name == modelName);
        if (!_storedModelPoses.ContainsKey(modelName))
            _storedModelPoses[modelName] = new Dictionary<string, ModelPose>();
        _storedModelPoses[modelName][poseName] = new ModelPose(model);
    }

    /// <summary>
    /// Apply stored model pose.
    /// </summary>
    /// <param name="model">Character model name</param>
    /// <param name="poseName">Pose name</param>
    public void ApplyModelPose(string modelName, string poseName)
    {
        // Get model
        var instKvp = _animationInstancesById.Where(kvp => kvp.Value.Animation.Model.name == modelName)
            .Select(kvp => (KeyValuePair<int, ScheduledInstance>?)kvp)
            .FirstOrDefault();
        if (instKvp == null)
        {
            throw new Exception(string.Format("Cannot apply pose {0} to model {1}: model does not exist", poseName, modelName));
        }

        if (!_storedModelPoses.ContainsKey(modelName) || !_storedModelPoses[modelName].ContainsKey(poseName))
        {
            throw new Exception(string.Format("Cannot apply pose {0} to model {1}: pose does not exist", poseName, modelName));
        }

        var model = instKvp.Value.Value.Animation.Model;
        _storedModelPoses[modelName][poseName].Apply(model);
    }

    /// <summary>
    /// Remove stored model pose.
    /// </summary>
    /// <param name="model">Character model name</param>
    /// <param name="poseName">Pose nmae</param>
    public void RemoveModelPose(string modelName, string poseName)
    {
        if (!_storedModelPoses.ContainsKey(modelName))
            return;

        _storedModelPoses[modelName].Remove(poseName);
    }

    /// <summary>
    /// Advance animation on the timeline.
    /// </summary>
    /// <param name="deltaTime">Elapsed time since last update</param>
    public void Advance(float deltaTime)
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

        ApplyAnimation();
    }

    /// <summary>
    /// Apply animation at the current time to all character models.
    /// </summary>
    public void ApplyAnimation()
    {
        // Reset models and IK solvers
        _ResetIK();
        ResetModelsAndEnvironment();

        // Apply active animation instances in layers in correct order
        foreach (var layer in _layerContainers)
        {
            if (!layer.Active)
                continue;

            var animationsInLayer = layer.Animations;

            // Deactivate animations in the layer that have finished
            foreach (var animation in animationsInLayer)
            {
                if (animation.StartFrame > CurrentFrame ||
                    animation.StartFrame + animation.Animation.FrameLength - 1 < CurrentFrame)
                {
                    if (_activeAnimationInstanceIds.Contains(animation.InstanceId))
                    {
                        // This animation instance has just become inactive

                        animation.Animation.Finish();
                        _activeAnimationInstanceIds.Remove(animation.InstanceId);

                        Debug.Log(string.Format("{0}: Deactivating animation instance {1} on model {2}",
                            CurrentFrame, animation.Animation.AnimationClip.name, animation.Animation.Model.name));

                        // Notify listeners that the animation instance has just become inactive
                        AnimationFinished(animation.InstanceId);
                    }

                    continue;
                }
            }

            // Apply animations in the layer that are active
            foreach (var animation in animationsInLayer)
            {
                if (CurrentFrame >= animation.StartFrame &&
                    CurrentFrame <= (animation.StartFrame + animation.Animation.FrameLength - 1))
                {
                    // This animation instance is active, so apply it

                    if (!_activeAnimationInstanceIds.Contains(animation.InstanceId))
                    {
                        // This animation instance has just become active

                        _activeAnimationInstanceIds.Add(animation.InstanceId);
                        animation.Animation.Start();

                        Debug.Log(string.Format("{0}: Activating animation instance {1} on model {2}",
                            CurrentFrame, animation.Animation.AnimationClip.name, animation.Animation.Model.name));

                        // Notify listeners that the animation instance has just become active
                        AnimationStarted(animation.InstanceId);
                    }

                    if (animation.Animation is EnvironmentObjectAnimationInstance)
                    {
                        // Environment object animations don't get applied until after IK has been run
                        continue;
                    }

                    if (layer.isIKEndEffectorConstr)
                    {
                        // First apply animation without any scaling, so that end-effector goals can be initialized
                        Vector3 modelScale = animation.Animation.Model.transform.localScale;
                        animation.Animation.Model.transform.localScale = new Vector3(1, 1, 1);
                        animation.Animation.Apply(CurrentFrame - animation.StartFrame, layer.LayerMode);

                        // Set up IK goals for this layer
                        _SetIKEndEffectorGoals(animation.Animation.Model, animation.Animation.AnimationClip);

                        // Restore model scale
                        animation.Animation.Model.transform.localScale = modelScale;
                    }

                    animation.Animation.Apply(CurrentFrame - animation.StartFrame, layer.LayerMode);
                }
            }

            foreach (var model in Models)
            {
                // Configure IK solver parameters for each model
                if (layer.isBase)
                    _SetIKBasePose(model);
                if (layer.isGaze)
                    _InitGazeIK(model, layer);
            
                // Store model poses after the current layer is applied
                StoreModelPose(model.gameObject.name, layer.LayerName + "Pose");
            }

            // Notify listeners that the layer has finished applying
            LayerApplied(layer.LayerName);
        }

        // Apply any active end-effector constraints
        _SolveIK();

        if (IsBakingInstances)
            // Apply morph deformations
            _ApplyMorph();

        // Store final pose and notify listeners that animation is applied
        _StoreModelsCurrentPose();
        AllAnimationApplied();
    }

    // Increment time on the timeline by the specified amount
    private bool _AddTime(float deltaTime)
    {
        bool loopedAround = false;

        // Add time (and check if we're looping around to the start of the timeline)
        if (CurrentTime + deltaTime > TimeLength)
        {
            loopedAround = true;
            CurrentTime += (deltaTime - TimeLength);
        }
        else
        {
            CurrentTime += deltaTime;
        }

        // Make sure controllers have access to the latest delta time
        foreach (var model in Models)
            model.GetComponent<AnimControllerTree>().DeltaTime = deltaTime;

        return loopedAround;
    }

    // Add animation to the specified layer container
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

    // Store the current pose of each model
    private void _StoreModelsCurrentPose()
    {
        foreach (var model in Models)
        {
            model.GetComponent<ModelController>()._StoreCurrentPose();
        }
    }

    // Initialize animation controllers on all models
    private void _InitControllers()
    {
        foreach (var model in Models)
        {
            var animControllerTree = model.GetComponent<AnimControllerTree>();
            if (animControllerTree != null)
                animControllerTree.Start();

            var gazeController = model.GetComponent<GazeController>();
            if (gazeController != null)
            {
                gazeController.fixGaze = false;
            }
        }
    }

    // Initialize IK solvers on all models
    private void _InitIK()
    {
        var models = Models;
        foreach (var model in models)
        {
            IKSolver[] solvers = model.GetComponents<IKSolver>();
            foreach (var solver in solvers)
                solver.Init();
        }
    }

    // Set end-effector goals for the IK solver on the specified model
    private void _SetIKEndEffectorGoals(GameObject model, AnimationClip animationClip)
    {
        if (!_endEffectorConstraints.ContainsKey(animationClip))
            return;

        // Set up end-effector goals
        EndEffectorConstraint[] activeConstraints = _endEffectorConstraints[animationClip].GetConstraintsAtFrame(CurrentFrame);
        if (activeConstraints != null)
        {
            IKSolver[] solvers = model.GetComponents<IKSolver>();

            foreach (var constraint in activeConstraints)
            {
                Transform endEffector = ModelUtils.FindBoneWithTag(model.transform, constraint.endEffector);

                // Compute constraint weight
                float t = 1f;
                if (CurrentFrame < constraint.startFrame)
                    t = Mathf.Clamp01(1f - ((float)(constraint.startFrame - CurrentFrame)) / constraint.activationFrameLength);
                else if (CurrentFrame > (constraint.startFrame + constraint.frameLength - 1))
                    t = Mathf.Clamp01(1f - ((float)(CurrentFrame - (constraint.startFrame + constraint.frameLength - 1))) / constraint.deactivationFrameLength);
                float t2 = t * t;
                float weight = -2f * t2 * t + 3f * t2;

                // Set the constraint goal in relevant IK solvers
                foreach (var solver in solvers)
                {
                    if (solver.endEffectors.Contains(constraint.endEffector))
                    {
                        IKGoal goal = new IKGoal(endEffector,
                            constraint.target == null ? endEffector.position : constraint.target.transform.position,
                            constraint.target == null ? endEffector.rotation : constraint.target.transform.rotation,
                            weight, constraint.preserveAbsoluteRotation);
                        solver.AddGoal(goal);
                    }
                }
            }
        }
        
        // Set up object manipulations
        EndEffectorConstraint[] activeManipulatedObjectConstraints =
            _endEffectorConstraints[animationClip].GetManipulationConstraintsAtFrame(CurrentFrame);
        if (LEAPCore.enableObjectManipulation && activeManipulatedObjectConstraints != null)
        {
            foreach (var constraint in activeManipulatedObjectConstraints)
            {
                Transform endEffector = ModelUtils.FindBoneWithTag(model.transform, constraint.endEffector);
                _activeManipulatedObjectHandles[constraint.manipulatedObjectHandle] = endEffector;
            }
        }
    }

    // Set current model pose as base pose for the IK solver on the specified model
    private void _SetIKBasePose(GameObject model)
    {
        IKSolver[] solvers = model.GetComponents<IKSolver>();
        foreach (var solver in solvers)
        {
            if (solver is BodyIKSolver)
            {
                var bodySolver = solver as BodyIKSolver;
                bodySolver.InitBasePose();
            }
        }
    }

    // Set up gaze constraints for the IK solver on the specified model
    private void _InitGazeIK(GameObject model, LayerContainer gazeLayer)
    {
        float gazeWeight = 0f;
        var curGazeInstance = gazeLayer.Animations.FirstOrDefault(inst =>
            CurrentFrame >= inst.StartFrame && CurrentFrame <= inst.EndFrame &&
            inst.Animation.Model == model);

        if (LEAPCore.useGazeIK && LEAPCore.useDynamicGazeIKWeights && curGazeInstance != null)
        {
            // Gaze is constrained by the current gaze instance

            // Compute gaze IK activation/deactivation time
            int gazeIKFrameLength = Mathf.RoundToInt(LEAPCore.gazeConstraintActivationTime * LEAPCore.editFrameRate);

            // Get all gaze instances on this model that follow the current one
            var curOrNextGazeInstances = gazeLayer.Animations.Where(inst =>
                inst.EndFrame >= CurrentFrame && inst.Animation.Model == model).ToArray();

            // Find the first gap in those instances
            int gazeIKEndFrame = FrameLength - 1;
            for (int gazeInstanceIndex = 0; gazeInstanceIndex < curOrNextGazeInstances.Length; ++gazeInstanceIndex)
            {
                int nextEndFrame = curOrNextGazeInstances[gazeInstanceIndex].EndFrame;
                int nextStartFrame = gazeInstanceIndex + 1 < curOrNextGazeInstances.Length ?
                    curOrNextGazeInstances[gazeInstanceIndex + 1].StartFrame : FrameLength;

                if (nextStartFrame - nextEndFrame > 1)
                {
                    gazeIKEndFrame = nextEndFrame;
                    break;
                }
            }

            // Get all gaze instances on this model that precede the current one
            var curOrPrevGazeInstances = gazeLayer.Animations.Where(inst =>
                inst.StartFrame <= CurrentFrame && inst.Animation.Model == model).ToArray();

            // Find the last gap in those instances
            int gazeIKStartFrame = 0;
            for (int gazeInstanceIndex = curOrPrevGazeInstances.Length - 1; gazeInstanceIndex >= 0; --gazeInstanceIndex)
            {
                int prevStartFrame = curOrPrevGazeInstances[gazeInstanceIndex].StartFrame;
                int prevEndFrame = gazeInstanceIndex - 1 >= 0 ?
                    curOrPrevGazeInstances[gazeInstanceIndex - 1].EndFrame : 0;

                if (prevStartFrame - prevEndFrame > 1)
                {
                    gazeIKStartFrame = prevStartFrame;
                    break;
                }
            }

            // TODO: quick hack to prevent constrained gaze from rapidly blending back into
            // the base motion at the end of the timeline
            if (curOrNextGazeInstances.Length >= 1 &&
                !curOrNextGazeInstances[curOrNextGazeInstances.Length - 1]
                .Animation.AnimationClip.name.EndsWith(LEAPCore.gazeAheadSuffix))
            {
                // This is the last gaze instance on the timeline, but it isn't gaze-ahead
                gazeIKEndFrame = FrameLength - 1 + gazeIKFrameLength;
            }

            // Compute gaze IK weight
            float gazeWeightIn = Mathf.Clamp01(((float)(CurrentFrame - gazeIKStartFrame)) / gazeIKFrameLength);
            float gazeWeightIn2 = gazeWeightIn * gazeWeightIn;
            gazeWeightIn = -2f * gazeWeightIn2 * gazeWeightIn + 3f * gazeWeightIn2;
            float gazeWeightOut = Mathf.Clamp01(((float)(gazeIKEndFrame - CurrentFrame)) / gazeIKFrameLength);
            float gazeWeightOut2 = gazeWeightOut * gazeWeightOut;
            gazeWeightOut = -2f * gazeWeightOut2 * gazeWeightOut + 3f * gazeWeightOut2;
            gazeWeight = Mathf.Min(gazeWeightIn, gazeWeightOut);
        }

        if (!LEAPCore.useDynamicGazeIKWeights)
        {
            // Use static weight for gaze constraints
            gazeWeight = 1f;
        }

        // Set gaze weights in the IK solver
        IKSolver[] solvers = model.GetComponents<IKSolver>();
        foreach (var solver in solvers)
        {
            if (solver is BodyIKSolver)
            {
                var bodySolver = solver as BodyIKSolver;
                bodySolver.InitGazeWeights(gazeWeight);
            }
        }
    }

    // Clear end-effector goals in all IK solvers on all models
    private void _ResetIK()
    {
        foreach (var model in Models)
        {
            IKSolver[] solvers = model.GetComponents<IKSolver>();
            foreach (var solver in solvers)
            {
                solver.ClearGoals();
            }
        }

        // Clear object manipulations
        _activeManipulatedObjectHandles.Clear();
    }

    // Solve for final model pose in all IK solvers on all models
    private void _SolveIK()
    {
        foreach (var model in Models)
        {
            if (!LEAPCore.useGazeIK)
            {
                // Solve for body posture
                var bodySolver = model.GetComponent<BodyIKSolver>();
                if (bodySolver.enabled)
                    bodySolver.Solve();
            }

            // Then solve for limb poses
            LimbIKSolver[] limbSolvers = model.GetComponents<LimbIKSolver>();
            foreach (var limbSolver in limbSolvers)
            {
                if (limbSolver.enabled)
                    limbSolver.Solve();
            }
        }

        if (LEAPCore.enableObjectManipulation && Environment != null)
        {
            // Solve for environment layout
            foreach (var kvp in _activeManipulatedObjectHandles)
            {
                var objHandle = kvp.Key.transform;
                var endEffector = kvp.Value;
                var obj = objHandle.transform.parent;

                obj.rotation = endEffector.rotation * Quaternion.Inverse(objHandle.localRotation);
                Vector3 objScale = ModelUtils.GetBoneScale(obj);
                Vector3 objHandleLocalPosition = objHandle.localPosition;
                objHandleLocalPosition.Scale(objScale);
                obj.position = endEffector.position - obj.rotation * objHandleLocalPosition;
            }

            if (IsBakingInstances)
            {
                // Bake all active environment object animations
                foreach (int instanceId in _activeAnimationInstanceIds)
                {
                    var instance = GetAnimation(instanceId);
                    if (!(instance is EnvironmentObjectAnimationInstance))
                        continue;

                    int startFrame = GetAnimationStartFrame(instanceId);
                    var layer = GetLayerForAnimation(instanceId);
                    instance.Apply(CurrentFrame - startFrame, layer.LayerMode);
                }
            }
        }
    }

    // Apply morph deformations on all models
    private void _ApplyMorph()
    {
        foreach (var model in Models)
        {
            var morphController = model.GetComponent<MorphController>();
            if (morphController != null)
                morphController.Apply();
        }
    }
}

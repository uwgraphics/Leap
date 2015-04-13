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
        public bool isIKBase
        {
            get;
            set;
        }

        /// <summary>
        /// If true, the current layer contains gaze control for the body IK solver.
        /// </summary>
        public bool isIKGaze
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
            isIKBase = false;
            isIKGaze = false;
        }

        public List<ScheduledInstance> _GetAnimations()
        {
            return _animationInstances;
        }
    }

    /// <summary>
    /// End-effector constraint specification.
    /// </summary>
    public struct EndEffectorConstraint
    {
        /// <summary>
        /// End-effector tag.
        /// </summary>
        public string endEffector;

        /// <summary>
        /// Constraint start frame.
        /// </summary>
        public int startFrame;

        /// <summary>
        /// Length of the constraint in frames.
        /// </summary>
        public int frameLength;

        /// <summary>
        /// If true, absolute rotation of the end-effector should be preserved.
        /// </summary>
        public bool preserveAbsoluteRotation;

        /// <summary>
        /// Scene object to which the end-effector should be aligned
        /// </summary>
        public GameObject target;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="endEffector">End-effector tag</param>
        /// <param name="startFrame">Constraint start frame</param>
        /// <param name="frameLength">Length of the constraint in frames</param>
        /// <param name="preserveAbsoluteRotation">If true, absolute rotation of the end-effector should be preserved</param>
        /// <param name="target">Scene object to which the end-effector should be aligned</param>
        public EndEffectorConstraint(string endEffector, int startFrame, int frameLength, bool preserveAbsoluteRotation,
            GameObject target = null)
        {
            this.endEffector = endEffector;
            this.startFrame = startFrame;
            this.frameLength = frameLength;
            this.preserveAbsoluteRotation = preserveAbsoluteRotation;
            this.target = target;
        }
    }

    /// <summary>
    /// Container holding end-effector constraint annotations for a particular animation clip.
    /// </summary>
    public class EndEffectorConstraintContainer
    {
        /// <summary>
        /// Animation clip.
        /// </summary>
        public AnimationClip AnimationClip
        {
            get;
            private set;
        }
        
        private Dictionary<string, List<EndEffectorConstraint>> _constraints;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="animationClip">Animation clip</param>
        /// <param name="constraints">End-effector constraints for the animation clip</param>
        public EndEffectorConstraintContainer(AnimationClip animationClip, EndEffectorConstraint[] constraints)
        {
            this.AnimationClip = animationClip;

            _constraints = new Dictionary<string,List<EndEffectorConstraint>>();
            for (int constraintIndex = 0; constraintIndex < constraints.Length; ++constraintIndex)
            {
                string endEffector = constraints[constraintIndex].endEffector;
                if (!_constraints.ContainsKey(endEffector))
                    _constraints[endEffector] = new List<EndEffectorConstraint>();

                _constraints[endEffector].Add(constraints[constraintIndex]);
            }
        }

        /// <summary>
        /// Get list of constraints on the specified end-effector.
        /// </summary>
        /// <param name="endEffector">End-effector tag</param>
        /// <returns>List of constraints</returns>
        public IList<EndEffectorConstraint> GetConstraintsForEndEffector(string endEffector)
        {
            return _constraints.ContainsKey(endEffector) ? _constraints[endEffector].AsReadOnly() : null;
        }

        /// <summary>
        /// Get end-effector constraints active at the specified frame.
        /// </summary>
        /// <param name="frame">Frame index</param>
        /// <param name="frameWindow">Defines neighborhood of frames on either side of the frame index
        /// within to search for active constraints - this is used mainly for detecting onset of constraints
        /// that need to be blended in.</param>
        /// <returns>Active end-effector constraints</returns>
        public EndEffectorConstraint[] GetConstraintsAtFrame(int frame, int frameWindow = 0)
        {
            List<EndEffectorConstraint> activeConstraints = new List<EndEffectorConstraint>();
            foreach (KeyValuePair<string, List<EndEffectorConstraint>> kvp in _constraints)
            {
                activeConstraints.AddRange(
                    kvp.Value.Where(eec => frame >= (eec.startFrame - frameWindow) && frame <= (eec.startFrame + eec.frameLength - 1 + frameWindow)));
            }

            return activeConstraints.Count > 0 ? activeConstraints.ToArray() : null;
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
    private List<LayerContainer> _layerContainers;
    private Dictionary<int, ScheduledInstance> _animationInstancesById;
    private Dictionary<AnimationClip, EndEffectorConstraintContainer> _endEffectorConstraints;
    private float _currentTime = 0;
    private int _nextInstanceId = 0;
    private HashSet<int> _activeAnimationInstanceIds;
    private Dictionary<string, Dictionary<string, ModelPose>> _storedModelPoses;

    /// <summary>
    /// Constructor.
    /// </summary>
    private AnimationTimeline()
    {
        _models = new List<GameObject>();
        _layerContainers = new List<LayerContainer>();
        _animationInstancesById = new Dictionary<int, ScheduledInstance>();
        _endEffectorConstraints = new Dictionary<AnimationClip, EndEffectorConstraintContainer>();
        _activeAnimationInstanceIds = new HashSet<int>();
        _storedModelPoses = new Dictionary<string, Dictionary<string, ModelPose>>();

        Active = false;
        Playing = false;
        TimeScale = 0.25f;
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

        var modelController = model.GetComponent<ModelController>();
        if (modelController == null)
        {
            throw new Exception(string.Format("Character model {0} does not have a ModelController", model.name));
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
    /// Bake all animation on the timeline into animation clips.
    /// </summary>
    /// <param name="animationClipName">Animation clip names (one for each character model)</param>
    public void Bake(string[] animationClipNames)
    {
        BakeRange(animationClipNames, 0, FrameLength);
    }

    /// <summary>
    /// Bake a range of frames on the timeline into animation clip.
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
            _ApplyAnimation();

            foreach (var model in models)
            {
                Transform[] bones = ModelUtils.GetAllBones(model);
                for (int boneIndex = 0; boneIndex < bones.Length; ++boneIndex)
                {
                    var bone = bones[boneIndex];

                    if (boneIndex == 0)
                    {
                        // Key position on the root bone

                        var positionKeyframe = new Keyframe();
                        positionKeyframe.time = ((float)(CurrentFrame - startFrame)) / LEAPCore.editFrameRate;

                        positionKeyframe.value = bone.localPosition.x;
                        curvesPerModel[model.name][0].AddKey(positionKeyframe);

                        positionKeyframe.value = bone.localPosition.y;
                        curvesPerModel[model.name][1].AddKey(positionKeyframe);

                        positionKeyframe.value = bone.localPosition.z;
                        curvesPerModel[model.name][2].AddKey(positionKeyframe);
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
        Update(0);

        // Initialize each model's animation tree
        _InitControllers();

        // Set all instance to bake
        foreach (KeyValuePair<int, ScheduledInstance> kvp in _animationInstancesById)
            kvp.Value.Animation.IsBaking = true;
    }

    /// <summary>
    /// Save all baked animation instances into animation clips and stop baking.
    /// </summary>
    public void FinalizeBakeInstances()
    {
        if (!IsBakingInstances)
        {
            throw new Exception("Tried to finalize baking animation instances when no animation instance are baking");
        }

        // Save any baked instances to animation clips
        foreach (KeyValuePair<int, ScheduledInstance> kvp in _animationInstancesById)
            if (kvp.Value.Animation.IsBaking)
                kvp.Value.Animation.FinalizeBake();

        Debug.Log("Finished baking all animation instances");
    }

    /// <summary>
    /// Reset all character models to initial pose, encoded in
    /// InitialPose animation clip.
    /// </summary>
    public void ResetModelsToInitialPose()
    {
        // Set all models to initial pose
        var models = Models;
        foreach (var model in models)
        {
            model.GetComponent<ModelController>()._ResetToInitialPose();
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
            bool loopedAround = _AddTime(deltaTime * TimeScale);

            if (loopedAround)
            {
                if (IsBakingInstances)
                {
                    FinalizeBakeInstances();
                    Playing = false;
                    return;
                }
            }
        }

        _ApplyAnimation();
    }

    public void _ApplyAnimation()
    {
        var models = Models;

        // Reset models and IK solvers
        _ClearIKGoals();
        ResetModelsToInitialPose();

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

                    animation.Animation.Apply(CurrentFrame - animation.StartFrame, layer.LayerMode);
                    
                    // Set up IK goals for this layer
                    if (layer.isIKEndEffectorConstr)
                        _SetIKGoals(animation.Animation.Model, animation.Animation.AnimationClip);
                }
            }

            foreach (var model in models)
            {
                // Configure IK solver parameters for each model
                if (layer.isIKBase)
                    _SetIKBasePose(model);
                if (layer.isIKGaze)
                    _SetIKEyeGazeParams(model);
            
                // Store model poses after the current layer is applied
                StoreModelPose(model.gameObject.name, layer.LayerName + "Pose");
            }

            // Notify listeners that the layer has finished applying
            LayerApplied(layer.LayerName);
        }

        // Apply any active end-effector constraints
        _SolveIK();

        // Store final pose and notify listeners that animation is applied
        _StoreModelsCurrentPose();
        AllAnimationApplied();
    }

    private bool _AddTime(float deltaTime)
    {
        bool loopedAround = false;

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
        var models = Models;
        foreach (var model in models)
            model.GetComponent<AnimControllerTree>().DeltaTime = deltaTime;

        return loopedAround;
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
        var models = Models;
        foreach (var model in models)
        {
            model.GetComponent<ModelController>()._StoreCurrentPose();
        }
    }

    private void _InitControllers()
    {
        var models = Models;
        foreach (var model in models)
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

    private void _SetIKGoals(GameObject model, AnimationClip animationClip)
    {
        if (!_endEffectorConstraints.ContainsKey(animationClip))
            return;

        int constraintFrameWindow = Mathf.RoundToInt(LEAPCore.endEffectorConstraintBlendTime * LEAPCore.editFrameRate);
        EndEffectorConstraint[] activeConstraints = _endEffectorConstraints[animationClip].GetConstraintsAtFrame(CurrentFrame, constraintFrameWindow);
        if (activeConstraints == null)
            return;
        IKSolver[] solvers = model.GetComponents<IKSolver>();

        // Set end-effector goals
        foreach (var constraint in activeConstraints)
        {
            Transform endEffector = ModelUtils.FindBoneWithTag(model.transform, constraint.endEffector);

            // Compute constraint weight
            float t = 1f;
            if (CurrentFrame < constraint.startFrame)
                t = Mathf.Clamp01(1f - ((float)(constraint.startFrame - CurrentFrame)) / constraintFrameWindow);
            else if (CurrentFrame > (constraint.startFrame + constraint.frameLength - 1))
                t = Mathf.Clamp01(1f - ((float)(CurrentFrame - (constraint.startFrame + constraint.frameLength - 1))) / constraintFrameWindow);
            float t2 = t * t;
            float weight = -2f * t2 * t + 3f * t2;
            //
            /*if (model.name == "Norman")
            {
                Debug.LogWarning(string.Format("frame = {0}: endEffector = {1}, target = {2}, weight = {3}", CurrentFrame,
                    constraint.endEffector, constraint.target != null ? constraint.target.name : null, weight));
            }*/
            //

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

    private void _SetIKBasePose(GameObject model)
    {
        IKSolver[] solvers = model.GetComponents<IKSolver>();

        foreach (var solver in solvers)
        {
            if (solver is BodyIKSolver)
            {
                var bodySolver = solver as BodyIKSolver;

                // Set current pose as base pose of the body IK solver
                bodySolver.InitBasePose();
            }
        }
    }

    private void _SetIKEyeGazeParams(GameObject model)
    {
        IKSolver[] solvers = model.GetComponents<IKSolver>();

        foreach (var solver in solvers)
        {
            if (solver is BodyIKSolver)
            {
                var bodySolver = solver as BodyIKSolver;

                // Set current gaze controller configuration as eye gaze parameters for the body IK solver
                bodySolver.InitGazeParams();
            }
        }
    }

    private void _ClearIKGoals()
    {
        var models = Models;
        foreach (var model in models)
        {
            IKSolver[] solvers = model.GetComponents<IKSolver>();
            foreach (var solver in solvers)
            {
                solver.ClearGoals();
            }
        }
    }

    private void _SolveIK()
    {
        var models = Models;
        foreach (var model in models)
        {
            // First solve for body pose
            var bodySolver = model.GetComponent<BodyIKSolver>();
            if (bodySolver.enabled)
                bodySolver.Solve();

            // Then solve for limb poses
            LimbIKSolver[] limbSolvers = model.GetComponents<LimbIKSolver>();
            foreach (var limbSolver in limbSolvers)
            {
                if (limbSolver.enabled)
                    limbSolver.Solve();
            }
        }
    }
}

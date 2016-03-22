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

    /// <summary>
    /// Character animation instance scheduled to run at specified time
    /// on the timeline.
    /// </summary>
    public class ScheduledInstance
    {
        /// <summary>Underlying animation instance.</summary>
        public AnimationInstance Animation { get; private set; }

        /// <summary>Animation start frame on the timeline.</summary>
        public int StartFrame { get; private set; }

        /// <summary>
        /// Animation end frame on the timeline.
        /// </summary>
        public int EndFrame
        {
            get { return StartFrame + Animation.FrameLength - 1; }
        }

        /// <summary>
        /// Animation start time on the timeline.
        /// </summary>
        public float StartTime { get { return ((float)StartFrame) / LEAPCore.editFrameRate; } }

        /// <summary>
        /// Animation end time on the timeline.
        /// </summary>
        public float EndTime { get { return ((float)EndFrame) / LEAPCore.editFrameRate; } }

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
    /// Container for timewarps applied to animation on a specific character.
    /// </summary>
    public class TimewarpContainer
    {
        /// <summary>
        /// Timeline which owns the current timewarp container.
        /// </summary>
        public LayerContainer OwningLayer
        {
            get;
            private set;
        }

        /// <summary>
        /// Character model to which the timewarps apply.
        /// </summary>
        public GameObject Model
        {
            get;
            private set;
        }

        /// <summary>
        /// List of timewarps in the container.
        /// </summary>
        public IList<ITimewarp> Timewarps
        {
            get { return _timewarps.AsReadOnly(); }
        }

        private List<ITimewarp> _timewarps = new List<ITimewarp>();
        private List<TimeSet> _timewarpStartTimes = new List<TimeSet>();
        private List<TimeSet> _origTimewarpLengths = new List<TimeSet>();
        private List<TimeSet> _timewarpLengths = new List<TimeSet>();

        /// <summary>
        /// Constructor.
        /// </summary>
        public TimewarpContainer(LayerContainer layer, GameObject model)
        {
            OwningLayer = layer;
            Model = model;
        }

        /// <summary>
        /// Apply a timewarp to the animation.
        /// </summary>
        /// <param name="timewarp">Timewarp</param>
        /// <param name="startTime">Timewarp start time indexes (in the original animation time)</param>
        /// <param name="origTimeLength">Timewarp time lengths (in the original animation time)</param>
        /// <param name="tTimeLength">Timewarp time lengths (after timewarping)</param>
        public void AddTimewarp(ITimewarp timewarp, TimeSet startTime, TimeSet origTimeLength, TimeSet timeLength)
        {
            // Find the timewarp that will follow the new timewarp
            int nextTimewarpIndex = -1;
            for (int timewarpIndex = 0; timewarpIndex < _timewarps.Count; ++timewarpIndex)
            {
                if (_timewarpStartTimes[timewarpIndex] > startTime)
                {
                    nextTimewarpIndex = timewarpIndex;
                    break;
                }
            }

            // Add the new timewarp
            int newTimewarpIndex = -1;
            if (nextTimewarpIndex >= 0)
            {
                _timewarps.Insert(nextTimewarpIndex, timewarp);
                _timewarpStartTimes.Insert(nextTimewarpIndex, startTime);
                _origTimewarpLengths.Insert(nextTimewarpIndex, origTimeLength);
                _timewarpLengths.Insert(nextTimewarpIndex, timeLength);
                newTimewarpIndex = nextTimewarpIndex;
            }
            else
            {
                _timewarps.Add(timewarp);
                _timewarpStartTimes.Add(startTime);
                _origTimewarpLengths.Add(origTimeLength);
                _timewarpLengths.Add(timeLength);
                newTimewarpIndex = _timewarps.Count - 1;
            }

            // Remove any timewarps that overlap the new timewarp
            for (int timewarpIndex = 0; timewarpIndex < _timewarps.Count; ++timewarpIndex)
            {
                ITimewarp curTimewarp = _timewarps[timewarpIndex];
                var curStartTime = _timewarpStartTimes[timewarpIndex];
                var curOrigTimeLength = _origTimewarpLengths[timewarpIndex];

                if (timewarpIndex != newTimewarpIndex &&
                    curStartTime + curOrigTimeLength > startTime &&
                    curStartTime < startTime + origTimeLength)
                {
                    RemoveTimewarp(timewarpIndex);
                    newTimewarpIndex = timewarpIndex < newTimewarpIndex ? newTimewarpIndex - 1 : newTimewarpIndex;
                    --timewarpIndex;
                }
            }

            OwningLayer.OwningTimeline._UpdateTimeLength();
        }

        /// <summary>
        /// Remove a timewarp applied to the animation.
        /// </summary>
        /// <param name="timewarpIndex">Timewarp index</param>
        public void RemoveTimewarp(int timewarpIndex)
        {
            _timewarps.RemoveAt(timewarpIndex);
            _timewarpStartTimes.RemoveAt(timewarpIndex);
            _origTimewarpLengths.RemoveAt(timewarpIndex);
            _timewarpLengths.RemoveAt(timewarpIndex);

            OwningLayer.OwningTimeline._UpdateTimeLength();
        }

        /// <summary>
        /// Remove all timewarps applied to the animation.
        /// </summary>
        public void RemoveAllTimewarps()
        {
            _timewarps.Clear();
            _timewarpStartTimes.Clear();
            _origTimewarpLengths.Clear();
            _timewarpLengths.Clear();

            OwningLayer.OwningTimeline._UpdateTimeLength();
        }

        /// <summary>
        /// Get a timewarp applied to the animation.
        /// </summary>
        /// <param name="timewarpIndex">Timewarp index</param>
        /// <returns>Timewarp</returns>
        public ITimewarp GetTimewarp(int timewarpIndex)
        {
            return _timewarps[timewarpIndex];
        }

        /// <summary>
        /// Get the start time of a timewarp applied to the animation.
        /// </summary>
        /// <param name="timewarpIndex">Timewarp index</param>
        /// <returns>Timewarp start time</returns>
        public TimeSet GetTimewarpStartTime(int timewarpIndex)
        {
            return _timewarpStartTimes[timewarpIndex];
        }

        /// <summary>
        /// Get the length of a timewarp in original animation time.
        /// </summary>
        /// <param name="timewarpIndex">Timewarp index</param>
        /// <returns>Timewarp length in original animation time</returns>
        public TimeSet GetOrigTimewarpLength(int timewarpIndex)
        {
            return _origTimewarpLengths[timewarpIndex];
        }

        /// <summary>
        /// Get the length of a timewarp.
        /// </summary>
        /// <param name="timewarpIndex">Timewarp index</param>
        /// <returns>Timewarp length</returns>
        public TimeSet GetTimewarpLength(int timewarpIndex)
        {
            return _timewarpLengths[timewarpIndex];
        }

        /// <summary>
        /// Compute animation time indexes in the original animation clip.
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public TimeSet GetOriginalTimes(float time)
        {
            var modelController = Model.GetComponent<ModelController>();
            var times = new TimeSet(Model);
            times.rootTime = _GetOriginalTime(0, time, true);
            for (int boneIndex = 0; boneIndex < times.boneTimes.Length; ++boneIndex)
                times.boneTimes[boneIndex] = _GetOriginalTime(boneIndex, time);

            return times;
        }

        // Compute the time index in the original animation clip for the specified animation track
        private float _GetOriginalTime(int boneIndex, float time, bool isRootPosition = false)
        {
            if (_timewarps.Count <= 0 || !LEAPCore.timewarpsEnabled)
                return time;

            float origTime = -1f;
            float curStartTime = -1f;
            for (int timewarpIndex = 0; timewarpIndex <= _timewarps.Count; ++timewarpIndex)
            {
                float prevEndTime = -1f;
                float curEndTime = -1f;
                float curOrigStartTime = -1f;
                float curTimeLength = -1f;

                // Compute time intervals of the current timewarp, as well as the non-timewarped interval that might precede it
                if (timewarpIndex <= 0)
                {
                    curTimeLength = isRootPosition ? _timewarpLengths[timewarpIndex].rootTime :
                        _timewarpLengths[timewarpIndex].boneTimes[boneIndex];
                    curOrigStartTime = isRootPosition ? _timewarpStartTimes[timewarpIndex].rootTime :
                        _timewarpStartTimes[timewarpIndex].boneTimes[boneIndex];
                    curStartTime = curOrigStartTime;
                    curEndTime = curStartTime + curTimeLength;
                }
                else
                {
                    float prevTimeLength = isRootPosition ? _timewarpLengths[timewarpIndex - 1].rootTime :
                        _timewarpLengths[timewarpIndex - 1].boneTimes[boneIndex];
                    prevEndTime = curStartTime + prevTimeLength;
                    float prevOrigStartTime = isRootPosition ? _timewarpStartTimes[timewarpIndex - 1].rootTime :
                        _timewarpStartTimes[timewarpIndex - 1].boneTimes[boneIndex];
                    float prevOrigTimeLength = isRootPosition ? _origTimewarpLengths[timewarpIndex - 1].rootTime :
                        _origTimewarpLengths[timewarpIndex - 1].boneTimes[boneIndex];

                    if (timewarpIndex >= _timewarps.Count)
                    {
                        curOrigStartTime = OwningLayer.OwningTimeline.OriginalTimeLength;
                        curStartTime = prevEndTime + OwningLayer.OwningTimeline.OriginalTimeLength - prevOrigStartTime - prevOrigTimeLength;
                        curEndTime = curStartTime;
                    }
                    else
                    {
                        curTimeLength = isRootPosition ? _timewarpLengths[timewarpIndex].rootTime :
                            _timewarpLengths[timewarpIndex].boneTimes[boneIndex];
                        curOrigStartTime = isRootPosition ? _timewarpStartTimes[timewarpIndex].rootTime :
                            _timewarpStartTimes[timewarpIndex].boneTimes[boneIndex];
                        curStartTime = prevEndTime + curOrigStartTime - prevOrigStartTime - prevOrigTimeLength;
                        curEndTime = curStartTime + curTimeLength;
                    }
                }

                if (time > prevEndTime && time < curStartTime)
                {
                    // The applied frame is within the non-timewarped interval preceding the current timewarp
                    origTime = curOrigStartTime - (curStartTime - time);
                    break;
                }
                else if (time >= curStartTime && time <= curEndTime)
                {
                    // The applied frame is within the interval of the current timewarp
                    curTimeLength = isRootPosition ? _timewarpLengths[timewarpIndex].rootTime :
                            _timewarpLengths[timewarpIndex].boneTimes[boneIndex];
                    float curOrigTimeLength = isRootPosition ? _origTimewarpLengths[timewarpIndex].rootTime :
                        _origTimewarpLengths[timewarpIndex].boneTimes[boneIndex];
                    float inTime = curTimeLength >= 0.0001f ? (time - curStartTime) / curTimeLength : 0f;
                    origTime = _timewarps[timewarpIndex].GetTime(inTime) * curOrigTimeLength + curOrigStartTime;
                    break;
                }
            }

            return origTime;
        }
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

        private List<ScheduledInstance> _animationInstances = new List<ScheduledInstance>();
        private Dictionary<GameObject, TimewarpContainer> _timewarpsByModel = new Dictionary<GameObject,TimewarpContainer>();

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

            Active = true;
            isIKEndEffectorConstr = false;
            isBase = false;
            isGaze = false;
        }

        /// <summary>
        /// Get timewarps on the specified character model's animation.
        /// </summary>
        /// <param name="modelName">Character model name</param>
        /// <returns>Timewarp container</returns>
        public TimewarpContainer GetTimewarps(string modelName)
        {
            foreach (var kvp in _timewarpsByModel)
            {
                if (kvp.Key.name == modelName)
                {
                    return kvp.Value;
                }
            }

            return null;
        }

        /// <summary>
        /// Get time indexes in the original animation on the specified character model
        /// at the specified time point on the animation timeline.
        /// </summary>
        /// <param name="model">Character model</param>
        /// <param name="time">Time index</param>
        /// <returns>Original time indexes</returns>
        public TimeSet _GetOriginalTimes(GameObject model, float time)
        {
            return _timewarpsByModel.ContainsKey(model) ? _timewarpsByModel[model].GetOriginalTimes(time)
                : new TimeSet(model, time);
        }

        // Add animation to the current layer container
        public void _AddAnimation(ScheduledInstance newInstance)
        {
            bool newInstanceAdded = false;
            for (int instanceIndex = 0; instanceIndex < Animations.Count; ++instanceIndex)
            {
                var instance = Animations[instanceIndex];
                if (instance.StartFrame > newInstance.StartFrame)
                {
                    _animationInstances.Insert(instanceIndex, newInstance);
                    newInstanceAdded = true;
                    break;
                }
            }

            if (!newInstanceAdded)
                _animationInstances.Add(newInstance);

            if (!_timewarpsByModel.ContainsKey(newInstance.Animation.Model))
            {
                // Add a container for timewarps on this model's animations
                _timewarpsByModel[newInstance.Animation.Model] = new TimewarpContainer(this, newInstance.Animation.Model);
            }
        }

        // Remove animation from the current layer container
        public void _RemoveAnimation(ScheduledInstance instanceToRemove)
        {
            _animationInstances.Remove(instanceToRemove);
        }

        // Get total length of animations in the layer in seconds before timewarping has been applied
        public float _GetOriginalTimeLength()
        {
            int maxFrameLength = -1;
            ScheduledInstance lastInstance = null;

            foreach (var instance in Animations)
            {
                if (lastInstance == null ||
                    (instance.StartFrame + instance.Animation.FrameLength) > maxFrameLength)
                {
                    lastInstance = instance;
                    maxFrameLength = lastInstance.StartFrame + lastInstance.Animation.FrameLength;
                }
            }

            return LEAPCore.ToTime(maxFrameLength);
        }

        // Get total length of animations in the layer in seconds after timewarping has been applied
        public float _GetTimeLength()
        {
            float maxTimeLength = 0f;

            foreach (var kvp in _timewarpsByModel)
            {
                var origTimeLength = new TimeSet(kvp.Key, _GetOriginalTimeLength());
                var timewarpLength = new TimeSet(kvp.Key);
                var origTimewarpLength = new TimeSet(kvp.Key);
                int numTimewarps = kvp.Value.Timewarps.Count;

                for (int timewarpIndex = 0; timewarpIndex < numTimewarps; ++timewarpIndex)
                {
                    timewarpLength += kvp.Value.GetTimewarpLength(timewarpIndex);
                    origTimewarpLength += kvp.Value.GetOrigTimewarpLength(timewarpIndex);
                }
                
                var timeLength = origTimeLength - origTimewarpLength + timewarpLength;
                maxTimeLength = Mathf.Max(
                    Mathf.Max(timeLength.rootTime, timeLength.boneTimes.Length > 0 ? timeLength.boneTimes.Max() : 0f),
                    maxTimeLength);
            }

            return maxTimeLength;
        }
    }

    /// <summary>
    /// Container for baked animation clips and controller states
    /// for all the models and objects animated on the timeline.
    /// </summary>
    public class BakedAnimationTimelineContainer
    {
        /// <summary>
        /// Baked animation timeline name.
        /// </summary>
        public string Name
        {
            get;
            private set;
        }

        /// <summary>
        /// Timeline which owns the current baked timeline container.
        /// </summary>
        public AnimationTimeline OwningTimeline
        {
            get;
            private set;
        }

        /// <summary>
        /// List of containers for baked animation and controllers states on individual models.
        /// </summary>
        public IList<BakedAnimationContainer> AnimationContainers
        {
            get { return _animationContainers.AsReadOnly(); }
        }

        /// <summary>
        /// List of containers for baked animations of environment objects.
        /// </summary>
        public IList<BakedAnimationContainer> ManipulatedObjectAnimationContainers
        {
            get { return _manipulatedObjectAnimationContainers.AsReadOnly(); }
        }

        private List<BakedAnimationContainer> _animationContainers = new List<BakedAnimationContainer>();
        private List<BakedAnimationContainer> _manipulatedObjectAnimationContainers = new List<BakedAnimationContainer>();

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="timeline">Owning animation timeline</param>
        public BakedAnimationTimelineContainer(string name, AnimationTimeline timeline)
        {
            Name = name;
            OwningTimeline = timeline;

            // Create baked animation containers
            foreach (var model in timeline.OwningManager.Models)
            {
                _animationContainers.Add(new BakedAnimationContainer(model, this));
            }

            // Create baked animation containers for environment objects
            var manipulatedObjects = timeline.OwningManager.Environment.GetComponent<EnvironmentController>().ManipulatedObjects;
            foreach (var manipulatedObject in manipulatedObjects)
            {
                _manipulatedObjectAnimationContainers.Add(
                    new BakedAnimationContainer(manipulatedObject, this)
                    );
            }
        }
    }

    /// <summary>
    /// Container for baked animation clip and controller states
    /// for a single model animated on the timeline.
    /// </summary>
    public class BakedAnimationContainer
    {
        /// <summary>
        /// Character model.
        /// </summary>
        public GameObject Model
        {
            get;
            private set;
        }
        
        /// <summary>
        /// Baked animation clip.
        /// </summary>
        public AnimationClip AnimationClip
        {
            get;
            set;
        }

        /// <summary>
        /// Baked animation timeline container that owns the current baked animation container.
        /// </summary>
        public BakedAnimationTimelineContainer OwningTimelineContainer
        {
            get;
            private set;
        }

        /// <summary>
        /// List of containers for baked controller states of the current model.
        /// </summary>
        public IList<BakedControllerContainer> ControllerContainers
        {
            get { return _controllerContainers.AsReadOnly(); }
        }

        // Array of animation curves
        public AnimationCurve[] _AnimationCurves
        {
            get;
            private set;
        }

        // Animation clip instance for applying the baked animation to the model
        public AnimationClipInstance _AnimationInstance
        {
            get;
            set;
        }

        private List<BakedControllerContainer> _controllerContainers = new List<BakedControllerContainer>();

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="model">Character model</param>
        /// <param name="timelineContainer">Owning baked animation timeline container</param>
        public BakedAnimationContainer(GameObject model, BakedAnimationTimelineContainer timelineContainer)
        {
            Model = model;
            OwningTimelineContainer = timelineContainer;

            // Get base animation clip
            var baseAnimation = timelineContainer.OwningTimeline
                .GetLayer(model.tag == "ManipulatedObject" ? LEAPCore.environmentAnimationLayerName : LEAPCore.baseAnimationLayerName)
                .Animations.FirstOrDefault(a => a.Animation.Model == model);
            if (baseAnimation == null)
                throw new Exception("No base animation found for character model " + model.name);

            // Get baked animation clip
            string clipName = baseAnimation.Animation.Name + "-" + timelineContainer.Name;
            AnimationClip = LEAPAssetUtil.GetAnimationClipOnModel(clipName, model);
            if (AnimationClip == null)
            {
                // Create baked animation clip
                AnimationClip = LEAPAssetUtil.CreateAnimationClipOnModel(clipName, model);
            }
            
            // Create an array of animation curves for the clip
            _AnimationCurves = LEAPAssetUtil.CreateAnimationCurvesForModel(model);

            // Create animation clip instance
            _AnimationInstance = new AnimationClipInstance(clipName, model, false, false, false);

            // Create baked controller containers
            AnimController[] controllers = model.GetComponents<AnimController>();
            foreach (var controller in controllers)
            {
                _controllerContainers.Add(new BakedControllerContainer(controller, this));
            }
        }
    }

    /// <summary>
    /// Container for baked states of one animation controller.
    /// </summary>
    public class BakedControllerContainer
    {
        /// <summary>
        /// Animation controller.
        /// </summary>
        public AnimController Controller
        {
            get;
            private set;
        }

        /// <summary>
        /// Baked animation container that owns the current baked controller container.
        /// </summary>
        public BakedAnimationContainer OwningAnimationContainer
        {
            get;
            private set;
        }

        /// <summary>
        /// Baked controller states.
        /// </summary>
        public List<IAnimControllerState> ControllerStates
        {
            get;
            private set;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="controller">Animation controller</param>
        /// <param name="animationContainer">Baked animation container</param>
        public BakedControllerContainer(AnimController controller, BakedAnimationContainer animationContainer)
        {
            Controller = controller;
            OwningAnimationContainer = animationContainer;
            ControllerStates = new List<IAnimControllerState>();

            // Initialize controller states
            int frameLength = animationContainer.OwningTimelineContainer.OwningTimeline.FrameLength;
            for (int frameIndex = 0; frameIndex < frameLength; ++frameIndex)
                ControllerStates.Add(controller.GetRuntimeState());
        }
    }

    /// <summary>
    /// Owning animation manager.
    /// </summary>
    public AnimationManager OwningManager
    {
        get { return _manager; }
    }

    /// <summary>
    /// List of layers in the current timeline.
    /// </summary>
    public IList<LayerContainer> Layers
    {
        get { return _layerContainers.AsReadOnly(); }
    }

    /// <summary>
    /// List of baked animation timeline containers.
    /// </summary>
    public IList<BakedAnimationTimelineContainer> BakedTimelineContainers
    {
        get { return _bakedTimelineContainers; }
    }

    /// <summary>
    /// Container of the currently active baked animation timeline.
    /// </summary>
    public BakedAnimationTimelineContainer ActiveBakedTimeline
    {
        get
        {
            return _activeBakedTimelineContainerIndex >= 0 && _activeBakedTimelineContainerIndex < _bakedTimelineContainers.Count ?
                _bakedTimelineContainers[_activeBakedTimelineContainerIndex] : null;
        }
    }

    /// <summary>
    /// Active baked animation timeline index.
    /// </summary>
    public int ActiveBakedTimelineIndex
    {
        get
        {
            return _activeBakedTimelineContainerIndex;
        }
        set
        {
            if (value < 0)
            {
                // Apply currently active animations on the timeline
                _activeBakedTimelineContainerIndex = -1;
            }
            else if (value >= 0 && value < _bakedTimelineContainers.Count)
            {
                // Apply one of the baked timeline animations
                _activeBakedTimelineContainerIndex = value;
            }
            else
                throw new IndexOutOfRangeException("ActiveBakedTimelineIndex may not exceed " +
                    (_bakedTimelineContainers.Count - 1));
        }
    }

    /// <summary>
    /// true if animation on the timeline is currently being baked, false otherwise.
    /// </summary>
    public bool IsBaking
    {
        get;
        private set;
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
        get
        {
            int frameIndex = Mathf.RoundToInt(CurrentTime * LEAPCore.editFrameRate);
            return frameIndex < FrameLength ? frameIndex : FrameLength - 1;
        }
        private set
        {
            float time = ((float)value) / LEAPCore.editFrameRate;
            CurrentTime = time;
        }
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
    /// Length of the timeline in frames (before timewarping).
    /// </summary>
    public int OriginalFrameLength
    {
        get { return LEAPCore.ToFrame(OriginalTimeLength); }
    }

    /// <summary>
    /// Length of the timeline in seconds (before timewarping).
    /// </summary>
    public float OriginalTimeLength
    {
        get;
        private set;
    }

    /// <summary>
    /// Length of the timeline in frames (after timewarping).
    /// </summary>
    public int FrameLength
    {
        get { return LEAPCore.ToFrame(TimeLength); }
    }

    /// <summary>
    /// Length of the timeline in seconds (after timewarping).
    /// </summary>
    public float TimeLength
    {
        get;
        private set;
    }

    /// <summary>
    /// Playback rate of the animation.
    /// </summary>
    public float TimeScale
    {
        get;
        set;
    }

    private AnimationManager _manager = null;

    // Animation layers and instances:
    private List<LayerContainer> _layerContainers;
    private Dictionary<int, ScheduledInstance> _animationInstancesById;
    private List<BakedAnimationTimelineContainer> _bakedTimelineContainers;
    private int _activeBakedTimelineContainerIndex = -1;
    private Dictionary<int, List<int>> _endEffectorTargetHelperInstances;

    // Current animation state:
    public bool _active = false;
    private float _currentTime = 0;
    private int _nextInstanceId = 0;
    private HashSet<int> _activeAnimationInstanceIds;
    private Dictionary<string, Dictionary<string, ModelPose>> _storedModelPoses;
    private Dictionary<GameObject, Transform> _activeManipulatedObjectHandles;

    /// <summary>
    /// Constructor.
    /// </summary>
    public AnimationTimeline(AnimationManager manager)
    {
        _manager = manager;
        _layerContainers = new List<LayerContainer>();
        _animationInstancesById = new Dictionary<int, ScheduledInstance>();
        _bakedTimelineContainers = new List<BakedAnimationTimelineContainer>();
        _endEffectorTargetHelperInstances = new Dictionary<int, List<int>>();
        _activeAnimationInstanceIds = new HashSet<int>();
        _storedModelPoses = new Dictionary<string, Dictionary<string, ModelPose>>();
        _activeManipulatedObjectHandles = new Dictionary<GameObject, Transform>();
        
        Active = false;
        Playing = false;
        TimeScale = 1f;
        CurrentFrame = 0;
        TimeLength = 0f;
        OriginalTimeLength = 0f;
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
    /// <param name="endEffectorTargetHelperLayerName">Layer name for end-effector target helper animations</param>
    /// <returns>Animation instance ID</returns>
    public int AddAnimation(string layerName, AnimationInstance animation, int startFrame,
        string endEffectorTargetHelperLayerName = "Helpers")
    {
        if (!_layerContainers.Any(layerContainer => layerContainer.LayerName == layerName))
        {
            throw new Exception(string.Format("There is no layer named {0}", layerName));
        }

        // Ensure character model for this animation instance has been added to this timeline
        if (!OwningManager.Models.Any(m => m == animation.Model))
        {
            throw new Exception(string.Format("Character model {0} for animation {1} not defined on the current timeline",
                animation.Model.name, animation.Name));
        }

        // Schedule the animation instance in the appropriate order (based on start frame)
        var targetLayerContainer = GetLayer(layerName);
        int instanceId = _nextInstanceId++;
        var newInstance = new ScheduledInstance(instanceId, startFrame, animation, targetLayerContainer);
        targetLayerContainer._AddAnimation(newInstance);

        // Also add the instance so it can be fetched by ID
        _animationInstancesById.Add(newInstance.InstanceId, newInstance);

        if (newInstance.Animation is AnimationClipInstance &&
            (newInstance.Animation as AnimationClipInstance).EndEffectorConstraints != null)
        {
            var clipInstance = newInstance.Animation as AnimationClipInstance;
            _endEffectorTargetHelperInstances[instanceId] = new List<int>();

            // Schedule end-effector target helper animations
            var endEffectors = ModelUtil.GetEndEffectors(clipInstance.Model);
            foreach (var endEffector in endEffectors)
            {
                // Get end-effector helper clip and object
                var endEffectorTargetHelperClip = clipInstance.GetEndEffectorTargetHelperClip(endEffector.tag);
                string endEffectorTargetHelperName = ModelUtil.GetEndEffectorTargetHelperName(animation.Model, endEffector.tag);
                var endEffectorTargetHelper = GameObject.FindGameObjectsWithTag("EndEffectorTarget")
                    .FirstOrDefault(t => t.name == endEffectorTargetHelperName);

                // Create and configure helper animation instance
                var endEffectorTargetHelperInstance = Activator.CreateInstance(clipInstance.GetType(),
                    endEffectorTargetHelperClip.name, endEffectorTargetHelper, false, false, false) as AnimationClipInstance;
                
                // Schedule helper animation instance
                var endEffectorTargetLayerContainer = GetLayer(endEffectorTargetHelperLayerName);
                int endEffectorTargetHelperInstanceId = _nextInstanceId++;
                var endEffectorTargetHelperScheduledInstance = new ScheduledInstance(endEffectorTargetHelperInstanceId,
                    startFrame, endEffectorTargetHelperInstance, endEffectorTargetLayerContainer);
                endEffectorTargetLayerContainer._AddAnimation(endEffectorTargetHelperScheduledInstance);
                _endEffectorTargetHelperInstances[instanceId].Add(endEffectorTargetHelperInstanceId);
            }
        }

        // Ensure cached timeline length is up to date
        _UpdateTimeLength();

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

        ScheduledInstance instanceToRemove = _animationInstancesById[animationInstanceId];
        if (instanceToRemove.Animation is AnimationClipInstance &&
            _endEffectorTargetHelperInstances.ContainsKey(animationInstanceId))
        {
            // Also remove end-effector target helper animations
            foreach (int endEffectorTargetHelperInstanceId in _endEffectorTargetHelperInstances[animationInstanceId])
            {
                if (!_animationInstancesById.ContainsKey(endEffectorTargetHelperInstanceId))
                    // Helper animation already deleted
                    continue;

                var endEffectorTargetHelperInstance = _animationInstancesById[endEffectorTargetHelperInstanceId];
                endEffectorTargetHelperInstance.OwningLayer._RemoveAnimation(endEffectorTargetHelperInstance);
                _animationInstancesById.Remove(endEffectorTargetHelperInstanceId);
            }
            _endEffectorTargetHelperInstances.Remove(animationInstanceId);
        }

        // Then remove the animation instance itself
        var layer = instanceToRemove.OwningLayer;
        var model = instanceToRemove.Animation.Model;
        layer._RemoveAnimation(instanceToRemove);
        _animationInstancesById.Remove(animationInstanceId);

        if (!layer.Animations.Any(inst => inst.Animation.Model == model))
        {
            // No more animations on the specified character, also remove the timewarps
            layer.GetTimewarps(model.name).RemoveAllTimewarps();
        }

        // Ensure cached timeline length is up to date
        _UpdateTimeLength();
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
                if (instance.Animation.Name == animationName)
                    return instance.InstanceId;
            }
        }

        return -1;
    }

    /// <summary>
    /// Enable/disable all IK solvers on all loaded models.
    /// </summary>
    /// <param name="enabled">If true, solvers will be enabled, otherwise they will be disabled</param>
    public void SetIKEnabled(bool enabled = true)
    {
        var models = OwningManager.Models;
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
    public int AddManipulatedObjectAnimation(string layerName, AnimationClipInstance animation, int startFrame = 0)
    {
        if (!_layerContainers.Any(layerContainer => layerContainer.LayerName == layerName))
        {
            throw new Exception(string.Format("There is no layer named {0}", layerName));
        }

        // Ensure model for this animation instance has been added to this timeline
        if (OwningManager.Environment == null ||
            !OwningManager.Environment.GetComponent<EnvironmentController>().ManipulatedObjects.Any(obj => obj == animation.Model))
        {
            throw new Exception(string.Format("Environment object {0} for animation {1} not found",
                animation.Model.name, animation.AnimationClip.name));
        }

        // Schedule the animation instance in the appropriate order (based on start frame)
        var targetLayerContainer = GetLayer(layerName);
        var newInstance = new ScheduledInstance(_nextInstanceId++, startFrame, animation, targetLayerContainer);
        targetLayerContainer._AddAnimation(newInstance);

        // Also add the instance so it can be fetched by ID
        _animationInstancesById.Add(newInstance.InstanceId, newInstance);

        return newInstance.InstanceId;
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
    /// Get the ID of the animation instance applied to the specified model
    /// at the current time in the specified layer.
    /// </summary>
    /// <param name="layerName">Layer name</param>
    /// <param name="modelName">Character model name</param>
    /// <returns>Animation instance ID</returns>
    public int GetCurrentAnimationInstanceId(string layerName, string modelName)
    {
        var layer = GetLayer(layerName);
        var model = OwningManager.Models.FirstOrDefault(m => m.name == modelName);
        var curTimes = layer._GetOriginalTimes(model, CurrentTime);

        foreach (var scheduledInstance in layer.Animations)
        {
            if (scheduledInstance.Animation.Model.name != modelName)
                continue;

            if (_IsBetweenFrames(curTimes, scheduledInstance.StartTime, scheduledInstance.EndTime))
                return scheduledInstance.InstanceId;
        }

        return -1;
    }

    /// <summary>
    /// Initialize baking of the current animation timeline.
    /// </summary>
    /// <param name="bakedTimelineName">Name under which baked animation data should be saved</param>
    public void InitBake(string bakedTimelineName)
    {
        Debug.Log("Initializing the baking of the current animation timeline");

        IsBaking = false;
        _activeBakedTimelineContainerIndex = -1;
        for (int bakedAnimationContainerIndex = 0; bakedAnimationContainerIndex < _bakedTimelineContainers.Count; ++bakedAnimationContainerIndex)
        {
            if (_bakedTimelineContainers[bakedAnimationContainerIndex].Name == bakedTimelineName)
            {
                _activeBakedTimelineContainerIndex = bakedAnimationContainerIndex;
                _bakedTimelineContainers[bakedAnimationContainerIndex] = new BakedAnimationTimelineContainer(bakedTimelineName, this);

                return;
            }
        }

        _bakedTimelineContainers.Add(new BakedAnimationTimelineContainer(bakedTimelineName, this));
        _activeBakedTimelineContainerIndex = _bakedTimelineContainers.Count - 1;
    }

    /// <summary>
    /// Bake all animation on the timeline into animation clips.
    /// </summary>
    public void Bake()
    {
        BakeRange(0, FrameLength);
    }

    /// <summary>
    /// Bake a range of frames on the timeline into animation clips.
    /// </summary>
    /// <param name="startFrame">Start frame index</param>
    /// <param name="frameLength">Range length in frames</param>
    /// <returns>Animation clip</returns>
    public void BakeRange(int startFrame, int frameLength)
    {
        // Get baked animation container
        var bakedTimelineContainer = _activeBakedTimelineContainerIndex >= 0 && _activeBakedTimelineContainerIndex < _bakedTimelineContainers.Count ?
            _bakedTimelineContainers[_activeBakedTimelineContainerIndex] : null;
        if (bakedTimelineContainer == null)
        {
            throw new Exception(string.Format("Trying to bake animation timeline without an active container. Did you remember to call InitBake()?"));
        }

        Debug.Log(string.Format("Baking the animation timeline {0}...", bakedTimelineContainer.Name));

        // Enable baking
        IsBaking = true;

        // Timer for measuring bake duration
        System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
        timer.Start();

        // Initialize timeline
        GoToFrame(startFrame);
        Advance(0);

        // Play through the animation timeline and bake each frame
        Active = false;
        Active = true;
        Play();
        for (int frameIndex = startFrame; frameIndex < frameLength; ++frameIndex)
        {
            _BakeCurrentFrame(bakedTimelineContainer);
            Advance(1f / LEAPCore.editFrameRate);
        }
        Stop();

        // Show bake duration
        float elapsedTime = timer.ElapsedMilliseconds / 1000f;
        Debug.Log(string.Format("Baked all animation instances in {0} seconds", elapsedTime));

        // Disable baking
        IsBaking = false;

        // Set the curves to their animation clips on each model and write them out
        var allBakedAnimationContainers = bakedTimelineContainer.AnimationContainers
            .Union(bakedTimelineContainer.ManipulatedObjectAnimationContainers);
        foreach (var bakedAnimationContainer in allBakedAnimationContainers)
        {
            var model = bakedAnimationContainer.Model;
            LEAPAssetUtil.SetAnimationCurvesOnClip(model, bakedAnimationContainer.AnimationClip, bakedAnimationContainer._AnimationCurves);

            // Determine path for the animation clip
            string path = "";
            if (model.GetComponent<ModelController>() != null)
            {
                path = LEAPAssetUtil.GetModelDirectory(model) + bakedAnimationContainer.AnimationClip.name + ".anim";
            }
            else
            {
                path = LEAPCore.environmentModelsDirectory + "/" + bakedAnimationContainer.AnimationClip.name + ".anim";
            }

            // Write animation clip to file
            if (AssetDatabase.GetAssetPath(bakedAnimationContainer.AnimationClip) != path)
            {
                AssetDatabase.DeleteAsset(path);
                AssetDatabase.CreateAsset(bakedAnimationContainer.AnimationClip, path);
            }
            AssetDatabase.SaveAssets();

            // Re-add the clip to its model
            LEAPAssetUtil.AddAnimationClipToModel(bakedAnimationContainer.AnimationClip, model);
            bakedAnimationContainer._AnimationInstance = new AnimationClipInstance(bakedAnimationContainer.AnimationClip.name,
                model, false, false, false);
        }
    }

    /// <summary>
    /// Remove baked animations under the specified animation timeline name.
    /// </summary>
    /// <param name="bakedAnimationTimelineName">Baked animation timeline name</param>
    public void RemoveBaked(string bakedTimelineName)
    {
        _bakedTimelineContainers.RemoveAll(c => c.Name == bakedTimelineName);
    }

    /// <summary>
    /// Reset all character models to initial pose and the environment
    /// to initial layout.
    /// </summary>
    public void ResetModelsAndEnvironment()
    {
        // Set all models to initial pose
        var models = OwningManager.Models;
        foreach (var model in models)
        {
            model.GetComponent<ModelController>()._ResetToInitialPose();
            var morphController = model.GetComponent<MorphController>();
            if (morphController != null)
                morphController.ResetAllWeights();
        }

        if (OwningManager.Environment != null)
        {
            // Reset the environment to initial layout
            OwningManager.Environment.GetComponent<EnvironmentController>().ResetToInitialLayout();
        }
    }

    /// <summary>
    /// Store model pose, so that it can be reapplied later.
    /// </summary>
    /// <param name="model">Character model name</param>
    /// <param name="poseName">Pose name</param>
    public void StoreModelPose(string modelName, string poseName)
    {
        var model = OwningManager.Models.FirstOrDefault(m => m.name == modelName);
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

        if (!IsBaking &&
            _activeBakedTimelineContainerIndex >= 0 && _activeBakedTimelineContainerIndex < _bakedTimelineContainers.Count)
        {
            // We have already evaluated and baked the animation timeline, so apply the baked animation
            ApplyBakedAnimation();
        }
        else
        {
            ApplyAnimation();
        }
    }

    /// <summary>
    /// Apply baked animation at the current time to all character models.
    /// </summary>
    public void ApplyBakedAnimation()
    {
        var bakedTimelineContainer = _bakedTimelineContainers[_activeBakedTimelineContainerIndex];

        // Apply each baked animation
        foreach (var bakedAnimationContainer in bakedTimelineContainer.AnimationContainers)
        {
            bakedAnimationContainer._AnimationInstance.Apply(CurrentFrame, AnimationLayerMode.Override);
            
            // Also apply animation controller states
            foreach (var controllerContainer in bakedAnimationContainer.ControllerContainers)
            {
                controllerContainer.Controller.SetRuntimeState(controllerContainer.ControllerStates[CurrentFrame]);
            }
        }

        // Apply baked animations of manipulated objects
        foreach (var bakedAnimationContainer in bakedTimelineContainer.ManipulatedObjectAnimationContainers)
        {
            bakedAnimationContainer._AnimationInstance.Apply(CurrentFrame, AnimationLayerMode.Override);
        }
    }

    /// <summary>
    /// Apply animation at the current time to all character models.
    /// </summary>
    public void ApplyAnimation()
    {
        // Reset models and IK solvers
        _ResetIK();
        ResetModelsAndEnvironment();

        // Update animation controllers
        _UpdateControllers();

        // Apply active animation instances in layers in correct order
        foreach (var layer in _layerContainers)
        {
            if (!layer.Active)
                continue;

            var animationsInLayer = layer.Animations;

            // Deactivate animations in the layer that have finished
            foreach (var animation in animationsInLayer)
            {
                var model = animation.Animation.Model;
                var curTimes = layer._GetOriginalTimes(model, CurrentTime);
                if (!_IsBetweenFrames(curTimes, animation.StartTime, animation.EndTime))
                {
                    // This animation instance is inactive
                    if (_activeAnimationInstanceIds.Contains(animation.InstanceId))
                    {
                        // This animation instance has just become inactive
                        _activeAnimationInstanceIds.Remove(animation.InstanceId);
                        
                        Debug.Log(string.Format("{0}: Deactivating animation instance {1} on model {2}; original frames {3}",
                            CurrentFrame, animation.Animation.Name, model.name, curTimes.ToString()));

                        // Notify listeners that the animation instance has just become inactive
                        if (AnimationFinished != null)
                            AnimationFinished(animation.InstanceId);
                    }

                    continue;
                }
            }

            // Apply animations in the layer that are active
            foreach (var animation in animationsInLayer)
            {
                var model = animation.Animation.Model;
                var curTimes = layer._GetOriginalTimes(model, CurrentTime);
                if (_IsBetweenFrames(curTimes, animation.StartTime, animation.EndTime))
                {
                    // This animation instance is active, so apply it
                    if (!_activeAnimationInstanceIds.Contains(animation.InstanceId))
                    {
                        // This animation instance has just become active
                        _activeAnimationInstanceIds.Add(animation.InstanceId);

                        Debug.Log(string.Format("{0}: Activating animation instance {1} on model {2}; original frames {3}",
                            CurrentFrame, animation.Animation.Name, model.name, curTimes.ToString()));

                        // Notify listeners that the animation instance has just become active
                        if (AnimationStarted != null)
                            AnimationStarted(animation.InstanceId);
                    }

                    // Apply the animation instance
                    animation.Animation.Apply(curTimes - animation.StartTime, layer.LayerMode);
                    
                    if (layer.isIKEndEffectorConstr && animation.Animation is AnimationClipInstance &&
                        model.tag == "Agent")
                    {
                        // Set up IK goals for this layer
                        _SetIKEndEffectorGoals(model, animation);
                    }
                }
            }

            foreach (var model in OwningManager.Models)
            {
                // Configure IK solver parameters for each model
                if (layer.isBase)
                    _SetIKBasePose(model);
                if (layer.isGaze)
                    _InitGazeIK(model, layer);
            
                // Store model poses after the current layer is applied
                StoreModelPose(model.name, layer.LayerName + "Pose");
            }

            // Notify listeners that the layer has finished applying
            if (LayerApplied != null)
                LayerApplied(layer.LayerName);
        }

        // Apply blendshape animations, animation controllers, and IK
        _ApplyMorph();
        _LateUpdateControllers();
        _SolveIK();

        // Notify listeners that animation is applied
        if (AllAnimationApplied != null)
            AllAnimationApplied();
    }

    /// <summary>
    /// Initialize the animation timeline
    /// </summary>
    public void _Init()
    {
        _activeBakedTimelineContainerIndex = -1;
        _InitControllers();
        _InitIK();
        
        Active = false;
        Stop();
    }

    // Update the cached length of the animation timeline in frames
    public void _UpdateTimeLength()
    {
        // Update original time length
        OriginalTimeLength = 0f;
        foreach (var layer in Layers)
        {
            float origTimeLength = layer._GetOriginalTimeLength();
            if (origTimeLength > OriginalTimeLength)
                OriginalTimeLength = origTimeLength;
        }

        // Update time length with timewarps applied
        TimeLength = 0;
        foreach (var layer in Layers)
        {
            float timeLength = layer._GetTimeLength();
            if (timeLength > TimeLength)
                TimeLength = timeLength;
        }
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
        AnimController.deltaTime = deltaTime;

        return loopedAround;
    }

    // Bake the current animation frame
    private void _BakeCurrentFrame(BakedAnimationTimelineContainer bakedTimelineContainer)
    {
        var allBakedAnimationContainers = bakedTimelineContainer.AnimationContainers
            .Union(bakedTimelineContainer.ManipulatedObjectAnimationContainers);
        foreach (var bakedAnimationContainer in allBakedAnimationContainers)
        {
            var model = bakedAnimationContainer.Model;
            Transform[] bones = null;
            ModelController modelController = model.GetComponent<ModelController>();

            // Get model bones
            if (modelController == null)
            {
                bones = new Transform[1];
                bones[0] = model.transform;
            }
            else
            {
                bones = ModelUtil.GetAllBones(model);
            }

            // Compute current frame time
            float time = ((float)CurrentFrame) / LEAPCore.editFrameRate;

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
                    bakedAnimationContainer._AnimationCurves[0].AddKey(positionKeyframe);

                    positionKeyframe.value = bone.localPosition.y;
                    bakedAnimationContainer._AnimationCurves[1].AddKey(positionKeyframe);

                    positionKeyframe.value = bone.localPosition.z;
                    bakedAnimationContainer._AnimationCurves[2].AddKey(positionKeyframe);
                }

                // Key rotation

                var rotationKeyFrame = new Keyframe();
                rotationKeyFrame.time = time;

                rotationKeyFrame.value = bone.localRotation.x;
                bakedAnimationContainer._AnimationCurves[3 + boneIndex * 4].AddKey(rotationKeyFrame);

                rotationKeyFrame.value = bone.localRotation.y;
                bakedAnimationContainer._AnimationCurves[3 + boneIndex * 4 + 1].AddKey(rotationKeyFrame);

                rotationKeyFrame.value = bone.localRotation.z;
                bakedAnimationContainer._AnimationCurves[3 + boneIndex * 4 + 2].AddKey(rotationKeyFrame);

                rotationKeyFrame.value = bone.localRotation.w;
                bakedAnimationContainer._AnimationCurves[3 + boneIndex * 4 + 3].AddKey(rotationKeyFrame);
            }

            // Next, bake blend shape properties
            int numBlendShapes = modelController == null ? 0 : modelController.NumberOfBlendShapes;
            for (int blendShapeIndex = 0; blendShapeIndex < numBlendShapes; ++blendShapeIndex)
            {
                var keyFrame = new Keyframe();
                keyFrame.time = time;
                keyFrame.value = modelController.GetBlendShapeWeight(blendShapeIndex);
                int curveIndex = 3 + modelController.NumberOfBones * 4 + blendShapeIndex;
                bakedAnimationContainer._AnimationCurves[curveIndex].AddKey(keyFrame);
            }

            // Finally, bake the animation controller states
            for (int controllerIndex = 0; controllerIndex < bakedAnimationContainer.ControllerContainers.Count; ++controllerIndex)
            {
                var controllerContainer = bakedAnimationContainer.ControllerContainers[controllerIndex];
                controllerContainer.ControllerStates[CurrentFrame] = controllerContainer.Controller.GetRuntimeState();
            }
        }
    }

    // Initialize animation controllers on all models
    private void _InitControllers()
    {
        foreach (var controllerType in OwningManager._ControllersByExecOrder)
        {
            foreach (var model in OwningManager.Models)
            {
                var component = model.GetComponent(controllerType);
                if (component == null)
                    continue;

                var controller = component as AnimController;
                controller.Start();
                if (controller is GazeController)
                {
                    (controller as GazeController).fixGaze = false;
                }
            }
        }
    }

    // Update animation controllers
    private void _UpdateControllers()
    {
        // Get active controllers
        var activeControllers = new HashSet<AnimController>();
        _GetActiveControllers(activeControllers);

        foreach (var controllerType in OwningManager._ControllersByExecOrder)
        {
            foreach (var model in OwningManager.Models)
            {
                var component = model.GetComponent(controllerType);
                if (component == null || !activeControllers.Contains(component as AnimController))
                    // Don't update inactive controllers
                    continue;

                var controller = component as AnimController;
                controller.Update();
            }
        }

        // Update model controllers
        foreach (var model in OwningManager.Models)
        {
            var modelController = model.GetComponent<ModelController>();
            if (modelController == null)
                continue;

            modelController.Update();
        }
    }

    // Update animation controllers after all animation is applied
    private void _LateUpdateControllers()
    {
        // Get active controllers
        var activeControllers = new HashSet<AnimController>();
        _GetActiveControllers(activeControllers);

        foreach (var controllerType in OwningManager._ControllersByExecOrder)
        {
            foreach (var model in OwningManager.Models)
            {
                var component = model.GetComponent(controllerType);
                if (component == null || !activeControllers.Contains(component as AnimController))
                    // Don't update inactive controllers
                    continue;

                var controller = component as AnimController;
                controller.LateUpdate();
            }
        }

        // Update model controllers
        foreach (var model in OwningManager.Models)
        {
            var modelController = model.GetComponent<ModelController>();
            if (modelController == null)
                continue;

            modelController.LateUpdate();
        }
    }

    // Get set of currently active animation controllers
    private void _GetActiveControllers(HashSet<AnimController> activeControllers)
    {
        foreach (int activeInstanceId in _activeAnimationInstanceIds)
        {
            var instance = GetAnimation(activeInstanceId);
            if (instance is AnimationControllerInstance)
            {
                var controller = (instance as AnimationControllerInstance).Controller;
                if (controller.enabled)
                    activeControllers.Add(controller);
            }
        }
    }

    // Initialize IK solvers on all models
    private void _InitIK()
    {
        var models = OwningManager.Models;
        foreach (var model in models)
        {
            IKSolver[] solvers = model.GetComponents<IKSolver>();
            foreach (var solver in solvers)
                solver.Start();
        }
    }

    // Set end-effector goals for the IK solver on the specified model
    private void _SetIKEndEffectorGoals(GameObject model, ScheduledInstance scheduledInstance)
    {
        var instance = scheduledInstance.Animation as AnimationClipInstance;
        if (instance.EndEffectorConstraints == null)
            return;

        // Get instance times
        var curTimes = scheduledInstance.OwningLayer._GetOriginalTimes(instance.Model, CurrentTime);
        float instanceStartTime = scheduledInstance.StartTime;

        // Set up end-effector goals
        EndEffectorConstraint[] activeConstraints = null;
        float[] activeConstraintWeights = null;
        instance.GetEndEffectorConstraintsAtTime(curTimes - instanceStartTime, out activeConstraints, out activeConstraintWeights);
        if (activeConstraints != null && activeConstraints.Length > 0)
        {
            IKSolver[] solvers = model.GetComponents<IKSolver>();

            for (int constraintIndex = 0; constraintIndex < activeConstraints.Length; ++constraintIndex)
            {
                var constraint = activeConstraints[constraintIndex];
                float weight = activeConstraintWeights[constraintIndex];
                Transform endEffectorBone = ModelUtil.FindBoneWithTag(model.transform, constraint.endEffector);

                // Set the constraint goal in relevant IK solvers
                foreach (var solver in solvers)
                {
                    if (solver.endEffectors.Contains(constraint.endEffector))
                    {
                        IKGoal goal = new IKGoal(endEffectorBone,
                            constraint.target == null ? endEffectorBone.position : constraint.target.transform.position,
                            constraint.target == null ? endEffectorBone.rotation : constraint.target.transform.rotation,
                            weight, constraint.preserveAbsoluteRotation);
                        solver.AddGoal(goal);
                    }
                }
            }
        }
        
        // Set up object manipulations
        var activeManipulatedObjectConstraints = instance.GetManipulationEndEffectorConstraintsAtTime(curTimes - instanceStartTime);
        if (LEAPCore.enableObjectManipulation &&
            activeManipulatedObjectConstraints != null && activeManipulatedObjectConstraints.Length > 0)
        {
            foreach (var constraint in activeManipulatedObjectConstraints)
            {
                var endEffectorBone = ModelUtil.FindBoneWithTag(model.transform, constraint.endEffector);
                _activeManipulatedObjectHandles[constraint.manipulatedObjectHandle] = endEffectorBone;
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
        if (!LEAPCore.useGazeIK)
            return;

        var gazeController = model.GetComponent<GazeController>();
        if (gazeController == null)
        {
            Debug.LogWarning(string.Format("Unable to set up gaze IK on model {0}, no GazeController found", model.name));
            return;
        }
        var modelController = model.GetComponent<ModelController>();
        int headIndex = modelController.GetBoneIndex(gazeController.head.Top);

        float gazeWeight = 0f;
        float gazeTime = gazeLayer._GetOriginalTimes(model, CurrentTime).boneTimes[headIndex];
        int gazeFrame = Mathf.RoundToInt(gazeTime * LEAPCore.editFrameRate);
        var curGazeInstance = gazeLayer.Animations.FirstOrDefault(inst =>
            gazeTime >= inst.StartTime && gazeTime <= inst.EndTime &&
            inst.Animation.Model == model);

        if (LEAPCore.useDynamicGazeIKWeights && curGazeInstance != null)
        {
            // Gaze is constrained by the current gaze instance

            // Compute gaze IK activation/deactivation time
            int gazeIKFrameLength = Mathf.RoundToInt(LEAPCore.gazeConstraintActivationTime * LEAPCore.editFrameRate);

            // Get all gaze instances on this model that follow the current one
            var curOrNextGazeInstances = gazeLayer.Animations.Where(inst =>
                inst.EndTime >= gazeTime && inst.Animation.Model == model).ToArray();

            // Find the first gap in those instances
            int gazeIKEndFrame = OriginalFrameLength - 1;
            for (int gazeInstanceIndex = 0; gazeInstanceIndex < curOrNextGazeInstances.Length; ++gazeInstanceIndex)
            {
                int nextEndFrame = curOrNextGazeInstances[gazeInstanceIndex].EndFrame;
                int nextStartFrame = gazeInstanceIndex + 1 < curOrNextGazeInstances.Length ?
                    curOrNextGazeInstances[gazeInstanceIndex + 1].StartFrame : OriginalFrameLength;

                if (nextStartFrame - nextEndFrame > 1)
                {
                    gazeIKEndFrame = nextEndFrame;
                    break;
                }
            }

            // Get all gaze instances on this model that precede the current one
            var curOrPrevGazeInstances = gazeLayer.Animations.Where(inst =>
                inst.StartTime <= gazeTime && inst.Animation.Model == model).ToArray();

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
                .Animation.Name.EndsWith(LEAPCore.gazeAheadSuffix))
            {
                // This is the last gaze instance on the timeline, but it isn't gaze-ahead
                gazeIKEndFrame = FrameLength - 1 + gazeIKFrameLength;
            }

            // Compute gaze IK weight
            float gazeWeightIn = gazeIKFrameLength > 0 ?
                Mathf.Clamp01(((float)(gazeFrame - gazeIKStartFrame)) / gazeIKFrameLength) : 1f;
            float gazeWeightIn2 = gazeWeightIn * gazeWeightIn;
            gazeWeightIn = -2f * gazeWeightIn2 * gazeWeightIn + 3f * gazeWeightIn2;
            float gazeWeightOut = gazeIKFrameLength > 0 ?
                Mathf.Clamp01(((float)(gazeIKEndFrame - gazeFrame)) / gazeIKFrameLength) : 1f;
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
        foreach (var model in OwningManager.Models)
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
        foreach (var model in OwningManager.Models)
        {
            if (!LEAPCore.useGazeIK)
            {
                // Solve for body posture
                var bodySolver = model.GetComponent<BodyIKSolver>();
                if (bodySolver != null && bodySolver.enabled)
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

        if (LEAPCore.enableObjectManipulation && OwningManager.Environment != null)
        {
            // Solve for environment layout
            foreach (var kvp in _activeManipulatedObjectHandles)
            {
                var objHandle = kvp.Key.transform;
                var endEffector = kvp.Value;
                var obj = objHandle.transform.parent;

                obj.rotation = endEffector.rotation * Quaternion.Inverse(objHandle.localRotation);
                Vector3 objScale = ModelUtil.GetBoneScale(obj);
                Vector3 objHandleLocalPosition = objHandle.localPosition;
                objHandleLocalPosition.Scale(objScale);
                obj.position = endEffector.position - obj.rotation * objHandleLocalPosition;
            }
        }
    }

    // Apply morph deformations on all models
    private void _ApplyMorph()
    {
        foreach (var model in OwningManager.Models)
        {
            var morphController = model.GetComponent<MorphController>();
            if (morphController != null && morphController.enabled)
                morphController.Apply();
        }
    }

    // Checks if a set of time indexes overlaps a particular time interval,
    // while rounding all time values to nearest frame indexes
    private bool _IsBetweenFrames(TimeSet t, float s, float e)
    {
        int sf = LEAPCore.ToFrame(s);
        int ef = LEAPCore.ToFrame(e);
        return sf <= LEAPCore.ToFrame(t.rootTime) && LEAPCore.ToFrame(t.rootTime) <= ef ||
            t.boneTimes.Any(t1 => sf <= LEAPCore.ToFrame(t1) && LEAPCore.ToFrame(t1) <= ef);
    }
}

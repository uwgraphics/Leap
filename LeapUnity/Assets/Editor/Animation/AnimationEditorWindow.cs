using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

/// <summary>
/// GUI window for our layered animation editor. Also the central class
/// for Leap animation editing system.
/// </summary>
public class AnimationEditorWindow : EditorWindow
{
    /// <summary>
    /// Timeline for scheduling and layering animation instances.
    /// </summary>
    public AnimationTimeline Timeline
    {
        get;
        private set;
    }

    /// <summary>
    /// Last selected character model in the scene.
    /// </summary>
    public GameObject LastSelectedModel
    {
        get;
        private set;
    }

    // Animation timeline stuff
    private Stopwatch _frameTimer = new Stopwatch();
    private GameObject _loggedGazeControllerStateModel = null;
    private bool _ikEnabled = true;

    // Editor window GUI
    private bool _guiInitialized = false;
    private Texture _timelineActiveTexture;
    private Texture _timelineActiveDownTexture;
    private Texture _timelinePlayTexture;
    private Texture _timelinePlayDownTexture;
    private Texture _timelinePrevFrameTexture;
    private Texture _timelineNextFrameTexture;
    private Texture _timelineAddEyeGazeTexture;
    private Texture _timelineRemoveEyeGazeTexture;
    private Texture _timelineSetEyeGazeStartTimeTexture;
    private Texture _timelineSetEyeGazeEndTimeTexture;
    private Texture _timelineSetEyeGazeTargetTexture;

    // Scene view rendering and interaction
    private AnimationEditGizmos _animationEditGizmos = null;
    private GameObject _newSelectedGazeTarget = null;
    private bool _changeGazeHeadAlign = false;
    private bool _changeGazeTorsoAlign = false;

    /// <summary>
    /// Initialize the animation editor.
    /// </summary>
    public void Init()
    {
        _InitTimeline();

        // Get component for drawing animation edit gizmos
        _animationEditGizmos = UnityEngine.Object.FindObjectOfType(typeof(AnimationEditGizmos)) as AnimationEditGizmos;
        if (_animationEditGizmos != null)
        {
            _animationEditGizmos._ClearGazeSequence();
            _animationEditGizmos._ClearGazeTargets();
            _animationEditGizmos._ClearEndEffectorGoals();
        }
        else
        {
            UnityEngine.Debug.LogError("Unable to find object AnimationEditGizmos in the scene");
        }

        // Initialize scene view
        SceneView.onSceneGUIDelegate = SceneView_GUI;
        _newSelectedGazeTarget = null;
        _changeGazeHeadAlign = _changeGazeTorsoAlign = false;

        UnityEngine.Debug.Log("Leap Animation Editor successfully initialized");
    }

    private void OnEnable()
    {
        Init();
    }

    private void Update()
    {
        // Update frame timing
        if (!_frameTimer.IsRunning)
            _frameTimer.Start();
        float deltaTime = _frameTimer.ElapsedMilliseconds / 1000f;
        if (deltaTime < 1f / LEAPCore.editFrameRate)
            return;
        deltaTime = 1f / LEAPCore.editFrameRate;
        _frameTimer.Reset();
        _frameTimer.Start();

        // Update animation and redraw the scene and timeline
        Timeline.Advance(deltaTime);
        if (Timeline.Active && Timeline.Playing)
        {
            SceneView.RepaintAll();
            this.Repaint();
        }

        // Update last selected character model
        var selectedModel = ModelUtil.GetSelectedModel();
        var prevSelectedModel = LastSelectedModel;
        LastSelectedModel = selectedModel != null ? selectedModel : LastSelectedModel;
        LastSelectedModel = Timeline.OwningManager.Models.Contains(LastSelectedModel) ? LastSelectedModel : null;
        if (LastSelectedModel != prevSelectedModel)
            _OnSelectedModelChanged();

        // Select gaze target (if one has been clicked in the scene view)
        // TODO: this is a hacky solution that prevents Unity from overriding the object selection
        if (_newSelectedGazeTarget != null && _newSelectedGazeTarget != Selection.activeGameObject)
        {
            Selection.activeGameObject = _newSelectedGazeTarget;
        }
        
        if (_loggedGazeControllerStateModel != null)
        {
            // Log gaze controller state for the specified character model
            EyeGazeEditor.PrintEyeGazeControllerState(_loggedGazeControllerStateModel);
            _loggedGazeControllerStateModel = null;
        }

        // TODO: remove this
        /*if (LastSelectedModel != null)
        {
            //Timeline.ResetModelsToInitialPose();
            var gazeController = LastSelectedModel.GetComponent<GazeController>();
            var target = GameObject.Find("UpperLeft");
            gazeController.Torso.bone.localRotation = gazeController.Torso._ComputeTargetRotation(target.transform.position);
            gazeController._ApplyRotation(gazeController.Torso);
        }*/
        //
    }

    private void OnGUI()
    {
        if (Timeline == null)
            return;

        _InitGUI();

        // Animation timeline activation/deactivation
        if (GUI.Button(new Rect(10, 10, 40, 40),
            Timeline.Active ? _timelineActiveDownTexture : _timelineActiveTexture))
        {
            Timeline.Active = !Timeline.Active;

            if (!Timeline.Active)
            {
                Timeline.Stop();
                Timeline.ResetModelsAndEnvironment();

                // Show all models
                var models = Timeline.OwningManager.Models;
                foreach (var model in models)
                    ModelUtil.ShowModel(model, true);

                SceneView.RepaintAll();
            }
        }

        // Enable/disable playback on the timeline
        if (GUI.Button(new Rect(60, 10, 40, 40),
            Timeline.Playing ? _timelinePlayDownTexture : _timelinePlayTexture)
            && Timeline.Active)
        {
            if (!Timeline.Playing)
                Timeline.Play();
            else
                Timeline.Stop();
        }

        // Go to next/previous frame on the timeline
        if (GUI.Button(new Rect(110, 10, 40, 40), _timelinePrevFrameTexture)
            && Timeline.Active && !Timeline.Playing)
        {
            Timeline.PreviousFrame();
            Timeline.Advance(0);
            SceneView.RepaintAll();
        }
        if (GUI.Button(new Rect(160, 10, 40, 40), _timelineNextFrameTexture)
            && Timeline.Active && !Timeline.Playing)
        {
            Timeline.NextFrame();
            Timeline.Advance(0);
            SceneView.RepaintAll();
        }

        // Show current position on the timeline
        GUI.Label(new Rect(210, 10, 60, 40), string.Format("{0} / {1}", Timeline.CurrentFrame, Timeline.FrameLength));

        // Update playback speed
        GUI.Label(new Rect(240, 30, 20, 19), "X");
        string timeScaleStr = ((double)Timeline.TimeScale).ToString("0.00");
        timeScaleStr = GUI.TextField(new Rect(210, 30, 30, 19), timeScaleStr);
        float timeScale = 1f;
        try { timeScale = (float)double.Parse(timeScaleStr); }
        catch (Exception) { timeScale = 1f; }
        Timeline.TimeScale = timeScale;

        // IK and timewarping controls
        {
            bool prevIKEnabled = _ikEnabled;
            _ikEnabled = GUI.Toggle(new Rect(20, 90, 100, 20), _ikEnabled, "IK");
            Timeline.SetIKEnabled(_ikEnabled);

            if (_ikEnabled != prevIKEnabled)
                SceneView.RepaintAll();

            LEAPCore.useGazeIK = GUI.Toggle(new Rect(120, 90, 100, 20), LEAPCore.useGazeIK, "GazeIK");
            LEAPCore.timewarpsEnabled = GUI.Toggle(new Rect(220, 90, 100, 20), LEAPCore.timewarpsEnabled, "Timewarps");
        }

        // Gaze editing controls
        if (GUI.Button(new Rect(20, 120, 32, 32), _timelineAddEyeGazeTexture)
            && Timeline.Active && !Timeline.Playing)
        {
            _OnAddEyeGaze();
        }
        if (GUI.Button(new Rect(60, 120, 32, 32), _timelineRemoveEyeGazeTexture)
            && Timeline.Active && !Timeline.Playing)
        {
            _OnRemoveEyeGaze();
        }
        if (GUI.Button(new Rect(100, 120, 32, 32), _timelineSetEyeGazeStartTimeTexture)
            && Timeline.Active && !Timeline.Playing)
        {
            _OnSetEyeGazeStartTime();
        }
        if (GUI.Button(new Rect(140, 120, 32, 32), _timelineSetEyeGazeEndTimeTexture)
            && Timeline.Active && !Timeline.Playing)
        {
            _OnSetEyeGazeEndTime();
        }
        if (GUI.Button(new Rect(180, 120, 32, 32), _timelineSetEyeGazeTargetTexture)
            && Timeline.Active && !Timeline.Playing)
        {
            _OnSetEyeGazeTarget();
        }

        GUI.Label(new Rect(20, 180, 80, 40), "Layers:");

        // Layer enable/disable controls
        float layerToggleLeft = 20;
        float layerToggleTop = 200;
        int layerCount = 0;
        foreach (var layer in Timeline.Layers)
        {
            layer.Active = GUI.Toggle(new Rect(layerToggleLeft, layerToggleTop, 140, 20), layer.Active, layer.LayerName);
            layerToggleLeft += 140;
            ++layerCount;
            if (layerCount >= 3)
            {
                layerToggleLeft = 20;
                layerToggleTop += 30;
            }
        }
        
        // Bake procedural anim. instances into clips
        if (GUI.Button(new Rect(this.position.width - 60, 10, 40, 40), "Apply"))
        {
            Timeline.InitBake(LEAPCore.defaultBakedTimelineName);
            Timeline.BakeRange(LEAPCore.timelineBakeRangeStart,
                LEAPCore.timelineBakeRangeEnd > LEAPCore.timelineBakeRangeStart ?
                LEAPCore.timelineBakeRangeEnd - LEAPCore.timelineBakeRangeStart + 1 :
                Timeline.FrameLength);
        }

        // Update the playback slider
        float sliderPosition = Timeline.FrameLength > 0 ? Timeline.CurrentTime / Timeline.TimeLength : 0f;
        float time = GUI.HorizontalSlider(new Rect(20, 60, this.position.width - 50, 20), sliderPosition, 0, 1);
        if (!Timeline.Playing && Timeline.Active)
        {
            Timeline.GoToTime(time * Timeline.TimeLength);
            SceneView.RepaintAll();
        }
    }

    // Log gaze controller state on currently selected character model
    private void _OnLogGazeControllerState()
    {
        if (LastSelectedModel != null && LastSelectedModel.GetComponent<GazeController>() != null)
        {
            _loggedGazeControllerStateModel = LastSelectedModel;
        }
    }

    // Add new eye gaze instance starting at the current frame
    private void _OnAddEyeGaze()
    {
        int currentFrame = Timeline.CurrentFrame;
        if (LastSelectedModel != null && currentFrame >= 0)
        {
            var gazeLayer = Timeline.GetLayer("Gaze");

            // Get the base animation instance
            var baseInstance = Timeline.GetLayer(LEAPCore.baseAnimationLayerName).Animations.FirstOrDefault(inst => inst.Animation.Model == LastSelectedModel);
            if (baseInstance == null)
            {
                UnityEngine.Debug.LogError("No base animation loaded for character model " + LastSelectedModel.name);
                return;
            }

            // Get gaze target for the new gaze instance
            var gazeTarget = Selection.activeGameObject != null && Selection.activeGameObject.tag == "GazeTarget" ?
                Selection.activeGameObject : null;
            if (gazeTarget == null)
            {
                UnityEngine.Debug.LogError("No gaze target selected");
                return;
            }

            // Generate a name for the new gaze instance
            int gazeInstanceIndex = gazeLayer.Animations.Count(inst => inst.Animation.Model == LastSelectedModel) + 1;
            string gazeInstanceName = "";
            int existingInstanceId = -1;
            do
            {
                gazeInstanceName = baseInstance.Animation.Name + "Gaze" + (gazeInstanceIndex++);
                existingInstanceId = Timeline.FindAnimationByName(gazeInstanceName);
            }
            while(existingInstanceId > -1);

            // Create the new gaze instance and schedule it
            var newGazeInstance = new EyeGazeInstance(gazeInstanceName, LastSelectedModel, 30, gazeTarget, 1f, 0f, 30, true);
            EyeGazeEditor.AddEyeGaze(Timeline, newGazeInstance, currentFrame, "Gaze", false);
        }
    }

    // Remove eye gaze instance at the current frame
    private void _OnRemoveEyeGaze()
    {
        if (LastSelectedModel != null && LastSelectedModel.GetComponent<GazeController>() != null)
        {
            // Get gaze instance at the current time
            int currentGazeInstanceId = Timeline.GetCurrentAnimationInstanceId("Gaze", LastSelectedModel.name);
            if (currentGazeInstanceId < 0)
            {
                UnityEngine.Debug.LogError("No eye gaze instance defined at current frame");
                return;
            }

            EyeGazeEditor.RemoveEyeGaze(Timeline, currentGazeInstanceId, "Gaze", false);
        }
    }

    // Set current time as the start time of the current eye gaze instance
    private void _OnSetEyeGazeStartTime()
    {
        if (LastSelectedModel != null && LastSelectedModel.GetComponent<GazeController>() != null)
        {
            // Get gaze instance at the current time
            int currentGazeInstanceId = Timeline.GetCurrentAnimationInstanceId("Gaze", LastSelectedModel.name);
            if (currentGazeInstanceId < 0)
            {
                UnityEngine.Debug.LogError("No eye gaze instance defined at current frame");
                return;
            }
            var currentGazeInstance = Timeline.GetAnimation(currentGazeInstanceId);
            int currentStartFrame = Timeline.GetAnimationStartFrame(currentGazeInstanceId);

            EyeGazeEditor.SetEyeGazeTiming(Timeline, currentGazeInstanceId, Timeline.CurrentFrame,
                currentStartFrame + currentGazeInstance.FrameLength - 1);
        }
    }

    // Set current time as the end time of the current eye gaze instance
    private void _OnSetEyeGazeEndTime()
    {
        if (LastSelectedModel != null && LastSelectedModel.GetComponent<GazeController>() != null)
        {
            // Get gaze instance at the current time
            int currentGazeInstanceId = Timeline.GetCurrentAnimationInstanceId("Gaze", LastSelectedModel.name);
            if (currentGazeInstanceId < 0)
            {
                UnityEngine.Debug.LogError("No eye gaze instance defined at current frame");
                return;
            }
            var currentGazeInstance = Timeline.GetAnimation(currentGazeInstanceId);
            int currentStartFrame = Timeline.GetAnimationStartFrame(currentGazeInstanceId);
            int currentEndFrame = currentStartFrame + currentGazeInstance.FrameLength - 1;

            // Get the next gaze instance
            var nextGazeInstance = Timeline.GetLayer("Gaze").Animations.FirstOrDefault(inst => inst.StartFrame > currentEndFrame &&
                inst.Animation.Model == currentGazeInstance.Model);
            if (nextGazeInstance != null)
            {
                // We change the end time of the current gaze instance by setting a new start time for the next gaze instance
                int nextStartFrame = nextGazeInstance.StartFrame;
                int nextEndFrame = nextStartFrame + nextGazeInstance.Animation.FrameLength - 1;
                EyeGazeEditor.SetEyeGazeTiming(Timeline, nextGazeInstance.InstanceId, Timeline.CurrentFrame + 1, nextEndFrame);
            }
        }
    }

    // Set currently selected gaze target as the new gaze target of the current eye gaze instance
    private void _OnSetEyeGazeTarget()
    {
        if (LastSelectedModel != null && LastSelectedModel.GetComponent<GazeController>() != null)
        {
            int currentGazeInstanceId = Timeline.GetCurrentAnimationInstanceId("Gaze", LastSelectedModel.name);
            if (currentGazeInstanceId < 0)
            {
                UnityEngine.Debug.LogError("No eye gaze instance defined at current frame");
                return;
            }

            // Get gaze target for the new gaze instance
            var gazeTarget = Selection.activeGameObject != null && Selection.activeGameObject.tag == "GazeTarget" ?
                Selection.activeGameObject : null;
            if (gazeTarget == null)
            {
                UnityEngine.Debug.LogError("No gaze target selected");
                return;
            }

            EyeGazeEditor.SetEyeGazeTarget(Timeline, currentGazeInstanceId, gazeTarget);
        }
    }

    // Handle change in model selection
    private void _OnSelectedModelChanged()
    {
        _changeGazeHeadAlign = _changeGazeTorsoAlign = false;
    }

    // Initialize animation timeline
    private void _InitTimeline()
    {
        AnimationManager.Instance.Init();

        // Initialize the animation timeline
        Timeline = AnimationManager.Instance.Timeline;
        Timeline.Active = false;
        Timeline.Stop();

        // Subscribe to events from the animation timeline
        Timeline.AllAnimationApplied += new AnimationTimeline.AllAnimationEvtH(AnimationTimeline_AllAnimationApplied);
        Timeline.LayerApplied += new AnimationTimeline.LayerEvtH(AnimationTimeline_LayerApplied);
        Timeline.AnimationStarted += new AnimationTimeline.AnimationEvtH(AnimationTimeline_AnimationStarted);
        Timeline.AnimationFinished += new AnimationTimeline.AnimationEvtH(AnimationTimeline_AnimationFinished);
    }

    // Initialize animation editor GUI
    private void _InitGUI()
    {
        if (_guiInitialized)
            return;

        // Load animation editor icons
        _timelineActiveTexture = Resources.Load<Texture2D>("LeapAnimationEditor/TimelineActive");
        _timelineActiveDownTexture = Resources.Load<Texture2D>("LeapAnimationEditor/TimelineActiveDown");
        _timelinePlayTexture = Resources.Load<Texture2D>("LeapAnimationEditor/TimelinePlay");
        _timelinePlayDownTexture = Resources.Load<Texture2D>("LeapAnimationEditor/TimelinePlayDown");
        _timelinePrevFrameTexture = Resources.Load<Texture2D>("LeapAnimationEditor/TimelinePrevFrame");
        _timelineNextFrameTexture = Resources.Load<Texture2D>("LeapAnimationEditor/TimelineNextFrame");
        _timelineAddEyeGazeTexture = Resources.Load<Texture2D>("LeapAnimationEditor/TimelineAddEyeGaze");
        _timelineRemoveEyeGazeTexture = Resources.Load<Texture2D>("LeapAnimationEditor/TimelineRemoveEyeGaze");
        _timelineSetEyeGazeStartTimeTexture = Resources.Load<Texture2D>("LeapAnimationEditor/TimelineSetGazeStartTime");
        _timelineSetEyeGazeEndTimeTexture = Resources.Load<Texture2D>("LeapAnimationEditor/TimelineSetGazeEndTime");
        _timelineSetEyeGazeTargetTexture = Resources.Load<Texture2D>("LeapAnimationEditor/TimelineSetGazeTarget");

        _guiInitialized = true;
    }

    // Called whenever there is a SceneView event
    private void SceneView_GUI(SceneView sceneView)
    {
        if (_animationEditGizmos == null)
            return;
        
        var gazeLayer = Timeline.GetLayer("Gaze");
        if (LastSelectedModel != null && gazeLayer != null)
        {
            var gazeController = LastSelectedModel.GetComponent<GazeController>();
            var currentGazeInstanceId = Timeline.GetCurrentAnimationInstanceId("Gaze", LastSelectedModel.name);
            var currentGazeInstance = currentGazeInstanceId > -1 ?
                Timeline.GetAnimation(currentGazeInstanceId) as EyeGazeInstance : null;

            if (currentGazeInstance == null && (_changeGazeHeadAlign || _changeGazeTorsoAlign))
            {
                UnityEngine.Debug.LogError("Somehow we are in head or torso alignment editing mode, even though there is no gaze instance at the current frame");
                _changeGazeHeadAlign = _changeGazeTorsoAlign = false;
            }

            // Handle mouse interaction in the scene view
            if (_changeGazeHeadAlign || _changeGazeTorsoAlign)
            {
                // Interaction mode is changing head or torso alignments

                // Project mouse position onto the gaze shift line
                Vector3 p1, p2;
                _animationEditGizmos._GetCurrentGazeTargetLine(out p1, out p2);
                p1 = Camera.current.WorldToScreenPoint(p1);
                p1 = new Vector3(p1.x, Camera.current.pixelHeight - p1.y, 0f);
                p2 = Camera.current.WorldToScreenPoint(p2);
                p2 = new Vector3(p2.x, Camera.current.pixelHeight - p2.y, 0f);
                Vector3 p = GeometryUtil.ProjectPointOntoLine(p1, p2, Event.current.mousePosition);
                float sign = Vector3.Dot(p2 - p1, p - p1);
                float t = sign > 0f && (p2 - p1).magnitude >= 0.0001f ? (p - p1).magnitude / (p2 - p1).magnitude : 0f;
                t = Mathf.Clamp01(t);

                // Update gaze head and torso alignment parameters
                if (_changeGazeHeadAlign)
                {
                    currentGazeInstance.HeadAlign = t;
                }
                else if (_changeGazeTorsoAlign)
                {
                    currentGazeInstance.TorsoAlign = t;
                    currentGazeInstance.TurnBody = t <= 0f ? false : true;
                }

                if (Event.current.button == 0 && Event.current.type == EventType.MouseDown)
                {
                    _changeGazeHeadAlign = _changeGazeTorsoAlign = false;
                }
            }
            else
            {
                // Interaction mode is free clicking
                if (Event.current.button == 0 && Event.current.type == EventType.MouseDown)
                {
                    // Are we selecting a gaze target or changing head or torso alignments?
                    _changeGazeHeadAlign = _animationEditGizmos._OnChangeGazeHeadAlign(Event.current.mousePosition);
                    _changeGazeTorsoAlign = _changeGazeHeadAlign ? false : _animationEditGizmos._OnChangeGazeTorsoAlign(Event.current.mousePosition);
                    if (!_changeGazeHeadAlign && !_changeGazeTorsoAlign)
                    {
                        _newSelectedGazeTarget = _animationEditGizmos._OnSelectGazeTarget(Event.current.mousePosition);
                    }

                    Repaint();
                }
            }

            // Process keyboard commands
            var e = Event.current;
            switch (e.type)
            {
                case EventType.KeyDown:

                    if (e.shift && e.keyCode == KeyCode.G)
                    {
                        _OnLogGazeControllerState();
                    }
                    else if (e.shift && e.keyCode == KeyCode.A)
                    {
                        _OnAddEyeGaze();
                    }
                    else if (e.shift && e.keyCode == KeyCode.R)
                    {
                        _OnRemoveEyeGaze();
                    }
                    else if (e.shift && e.keyCode == KeyCode.LeftBracket)
                    {
                        _OnSetEyeGazeStartTime();
                    }
                    else if (e.shift && e.keyCode == KeyCode.RightBracket)
                    {
                        _OnSetEyeGazeEndTime();
                    }
                    else if (e.shift && e.keyCode == KeyCode.T)
                    {
                        _OnSetEyeGazeTarget();
                    }
                    else if (e.shift && e.keyCode == KeyCode.P)
                    {
                        EyeGazeEditor.PrintEyeGaze(Timeline, "Gaze", LastSelectedModel);
                    }
                    else if (e.shift && e.keyCode == KeyCode.B)
                    {
                        Timeline.InitBake(LEAPCore.defaultBakedTimelineName);
                        Timeline.BakeRange(LEAPCore.timelineBakeRangeStart,
                            LEAPCore.timelineBakeRangeEnd > LEAPCore.timelineBakeRangeStart ?
                            LEAPCore.timelineBakeRangeEnd - LEAPCore.timelineBakeRangeStart + 1 :
                            Timeline.FrameLength);
                    }
                    else if (e.shift && e.keyCode == KeyCode.D)
                    {
                        bool show = !_animationEditGizmos.showGazeSequence;
                        _animationEditGizmos.showGazeSequence =
                            _animationEditGizmos.showGazeTargets =
                            _animationEditGizmos.showEndEffectorGoals = show;
                    }
                    else if (e.shift && e.keyCode == KeyCode.L)
                    {
                        AnimationManager.Instance.PrintAnimationInstances();
                    }

                    break;

                default:

                    break;
            }

            _animationEditGizmos._ClearGazeSequence();
            _animationEditGizmos._ClearEndEffectorGoals();

            // Update gaze shift sequence
            GameObject currentGazeTarget = null;
            List<AnimationEditGizmos.EyeGazeInstanceDesc> gazeSequence =
                new List<AnimationEditGizmos.EyeGazeInstanceDesc>(gazeLayer.Animations.Count);
            int gazeIndex = -1, currentGazeIndex = -1;
            bool currentIsFixated = false;
            for (int gazeInstanceIndex = 0; gazeInstanceIndex < gazeLayer.Animations.Count; ++gazeInstanceIndex)
            {
                var scheduledGazeInstance = gazeLayer.Animations[gazeInstanceIndex];
                if (scheduledGazeInstance.Animation.Model == LastSelectedModel)
                {
                    var gazeInstance = scheduledGazeInstance.Animation as EyeGazeInstance;

                    // Add gaze instance description to the sequence
                    Vector3 targetPosition = gazeInstance.Target != null ? gazeInstance.Target.transform.position : gazeInstance.AheadTargetPosition;
                    gazeSequence.Add(new AnimationEditGizmos.EyeGazeInstanceDesc(targetPosition,
                        gazeInstance.HeadAlign, gazeInstance.TorsoAlign, gazeInstance.TurnBody));
                    ++gazeIndex;

                    if (gazeInstance == currentGazeInstance)
                    {
                        // This is the current gaze instance
                        currentGazeTarget = gazeInstance.Target;
                        currentGazeIndex = gazeIndex;
                        currentIsFixated = gazeController.StateId == (int)GazeState.NoGaze;
                    }
                }
            }

            // Get all gaze targets in the scene
            var gazeTargets = GameObject.FindGameObjectsWithTag("GazeTarget");
            int currentGazeTargetIndex = -1;
            for (int gazeTargetIndex = 0; gazeTargetIndex < gazeTargets.Length; ++gazeTargetIndex)
            {
                if (gazeTargets[gazeTargetIndex] == currentGazeTarget)
                {
                    currentGazeTargetIndex = gazeTargetIndex;
                    break;
                }
            }

            // Initialize animation editing gizmos
            _animationEditGizmos._SetGazeTargets(gazeTargets, currentGazeTargetIndex, currentIsFixated);
            _animationEditGizmos._SetGazeSequence(gazeSequence.ToArray(), currentGazeIndex);

            // Update end-effector goals list
            IKSolver[] solvers = LastSelectedModel.GetComponents<IKSolver>();
            foreach (var solver in solvers)
            {
                _animationEditGizmos._SetEndEffectorGoals(solver.Goals.ToArray());
            }
        }
    }

    private void AnimationTimeline_AllAnimationApplied()
    {
    }

    private void AnimationTimeline_LayerApplied(string layerName)
    {
    }

    private void AnimationTimeline_AnimationStarted(int animationInstanceId)
    {
        /*var animation = Timeline.GetAnimation(animationInstanceId);
        var layer = Timeline.GetLayerForAnimation(animationInstanceId);
        if (layer.LayerName == LEAPCore.baseAnimationLayerName)
        {
            var model = animation.Model.gameObject;
            ModelUtils.ShowModel(model);
        }*/
    }

    private void AnimationTimeline_AnimationFinished(int animationInstanceId)
    {
        /*var animation = Timeline.GetAnimation(animationInstanceId);
        var layer = Timeline.GetLayerForAnimation(animationInstanceId);
        if (layer.LayerName == LEAPCore.baseAnimationLayerName)
        {
            var model = animation.Model.gameObject;
            ModelUtils.ShowModel(model, false);
        }*/
    }

    /// <summary>
    /// Show/hide Leap animation editor window.
    /// </summary>
    [MenuItem("Window/Leap Animation")]
    public static void ShowLeapAnimationEditor()
    {
        EditorWindow.GetWindow<AnimationEditorWindow>("Leap Animation");
    }
}

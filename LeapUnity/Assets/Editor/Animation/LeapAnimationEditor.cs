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
public class LeapAnimationEditor : EditorWindow
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

    // Scene view rendering and interaction
    private AnimationEditGizmos _animationEditGizmos = null;
    private GameObject _newSelectedGazeTarget = null;

    private void OnEnable()
    {
        _InitTimeline();

        // Initialize scene view
        SceneView.onSceneGUIDelegate = _UpdateSceneView;
    }

    private void Update()
    {
        // Update frame timing
        if (!_frameTimer.IsRunning)
            _frameTimer.Start();
        float deltaTime = _frameTimer.ElapsedMilliseconds / 1000f;
        if (deltaTime < 1f / LEAPCore.editFrameRate)
            return;
        deltaTime = Mathf.Min(deltaTime, 1f / LEAPCore.editFrameRate);
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
        var selectedModel = ModelUtils.GetSelectedModel();
        LastSelectedModel = selectedModel != null ? selectedModel : LastSelectedModel;
        LastSelectedModel = Timeline.Models.Contains(LastSelectedModel) ? LastSelectedModel : null;

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
                Timeline.ResetModelsToInitialPose();

                // Show all models
                var models = Timeline.Models;
                foreach (var model in models)
                    ModelUtils.ShowModel(model, true);

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

        // Enable/disable layers
        float layerToggleLeft = 20;
        float layerToggleTop = 90;
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

        // Enable/disable IK on all models
        {
            bool prevIKEnabled = _ikEnabled;
            _ikEnabled = GUI.Toggle(new Rect(20, layerToggleTop + 30, 100, 20), _ikEnabled, "IK");
            Timeline.SetIKEnabled(_ikEnabled);

            if (_ikEnabled != prevIKEnabled)
                SceneView.RepaintAll();

            LEAPCore.useGazeIK = GUI.Toggle(new Rect(120, layerToggleTop + 30, 100, 20), LEAPCore.useGazeIK, "GazeIK");
        }

        // Bake procedural anim. instances into clips
        if (GUI.Button(new Rect(this.position.width - 60, 10, 40, 40), "Bake"))
        {
            if (!Timeline.IsBakingInstances)
            {
                if (Timeline.Active && !Timeline.Playing)
                {
                    Timeline.StartBakeInstances();
                    SceneView.RepaintAll();
                }
            }
            else
            {
                Timeline.FinalizeBakeInstances();
            }
        }

        // Update the playback slider
        if (Timeline.Active && Timeline.Playing || !Timeline.Active)
        {
            GUI.HorizontalSlider(new Rect(20, 60, this.position.width - 50, 20), Timeline.CurrentTime / Timeline.TimeLength, 0, 1);
        }
        else
        {
            Timeline.GoToTime(
                GUI.HorizontalSlider(new Rect(20, 60, this.position.width - 50, 20), Timeline.CurrentTime / Timeline.TimeLength, 0, 1)
                * Timeline.TimeLength
                );
            SceneView.RepaintAll();
        }

        // Process keyboard input
        var e = Event.current;
        switch (e.type)
        {
            case EventType.KeyDown:

                if (e.keyCode == KeyCode.G)
                {
                    if (LastSelectedModel != null && LastSelectedModel.GetComponent<GazeController>() != null)
                    {
                        _loggedGazeControllerStateModel = LastSelectedModel;
                    }
                }

                break;

            case EventType.MouseDown:

                _RemoveFocus();
                break;

            default:

                break;
        }
    }

    // For removing focus from a control
    private void _RemoveFocus()
    {
        GUI.SetNextControlName("_RemoveFocus");
        GUI.TextField(new Rect(-100, -100, 1, 1), "");
        GUI.FocusControl("_RemoveFocus");
    }

    // Initialize animation timeline
    private void _InitTimeline()
    {
        // Initialize the animation timeline
        Timeline = AnimationTimeline.Instance;
        Timeline.Active = false;
        Timeline.Stop();

        // Subscribe to events from the animation timeline
        Timeline.AllAnimationApplied += new AnimationTimeline.AllAnimationEvtH(AnimationTimeline_AllAnimationApplied);
        Timeline.LayerApplied += new AnimationTimeline.LayerEvtH(AnimationTimeline_LayerApplied);
        Timeline.AnimationStarted += new AnimationTimeline.AnimationEvtH(AnimationTimeline_AnimationStarted);
        Timeline.AnimationFinished += new AnimationTimeline.AnimationEvtH(AnimationTimeline_AnimationFinished);

        // Get component for drawing animation edit gizmos
        _animationEditGizmos = UnityEngine.Object.FindObjectOfType(typeof(AnimationEditGizmos)) as AnimationEditGizmos;
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

        _guiInitialized = true;
    }

    // Update scene view GUI
    private void _UpdateSceneView(SceneView sceneView)
    {
        _animationEditGizmos._ClearGazeSequence();
        _animationEditGizmos._ClearEndEffectorGoals();

        if (LastSelectedModel != null)
        {
            var gazeController = LastSelectedModel.GetComponent<GazeController>();
            var gazeLayer = Timeline.GetLayer("Gaze");
            
            if (gazeLayer != null)
            {
                // Update gaze target set and gaze shift sequence
                GameObject currentGazeTarget = null;
                HashSet<GameObject> gazeTargetSet = new HashSet<GameObject>();
                List<Vector3> gazeTargetSequence = new List<Vector3>(gazeLayer.Animations.Count);
                int gazeIndex = -1, currentGazeIndex = -1;
                bool currentIsFixated = false;
                for (int gazeInstanceIndex = 0; gazeInstanceIndex < gazeLayer.Animations.Count; ++gazeInstanceIndex)
                {
                    var scheduledGazeInstance = gazeLayer.Animations[gazeInstanceIndex];
                    if (scheduledGazeInstance.Animation.Model == LastSelectedModel)
                    {
                        var gazeInstance = scheduledGazeInstance.Animation as EyeGazeInstance;

                        // Add gaze target
                        if (gazeInstance.Target != null)
                            gazeTargetSet.Add(gazeInstance.Target);

                        // Add gaze target position
                        Vector3 targetPosition = gazeInstance.Target != null ? gazeInstance.Target.transform.position : gazeInstance.AheadTargetPosition;
                        gazeTargetSequence.Add(targetPosition);
                        ++gazeIndex;

                        if (Timeline.CurrentFrame >= scheduledGazeInstance.StartFrame &&
                            Timeline.CurrentFrame <= (scheduledGazeInstance.StartFrame + gazeInstance.FrameLength - 1))
                        {
                            // This is the current gaze instance
                            currentGazeTarget = gazeInstance.Target;
                            currentGazeIndex = gazeIndex;
                            currentIsFixated = gazeController.StateId == (int)GazeState.NoGaze;
                        }
                    }
                }

                // Get list of gaze targets
                var gazeTargets = gazeTargetSet.ToList();
                int currentGazeTargetIndex = gazeTargets.IndexOf(currentGazeTarget);

                // Initialize animation editing gizmos
                _animationEditGizmos._SetGazeTargets(gazeTargets.ToArray(), currentGazeTargetIndex, currentIsFixated);
                _animationEditGizmos._SetGazeSequence(gazeTargetSequence.ToArray(), currentGazeIndex);
            }

            // Update end-effector goals list
            IKSolver[] solvers = LastSelectedModel.GetComponents<IKSolver>();
            foreach (var solver in solvers)
            {
                _animationEditGizmos._SetEndEffectorGoals(solver.Goals.ToArray());
            }
        }

        // Handle gaze target selection
        if (Event.current.button == 0 && Event.current.type == EventType.MouseDown)
        {
            _newSelectedGazeTarget = _animationEditGizmos._OnSelectGazeTarget(sceneView.camera, Event.current.mousePosition);
            Repaint();
        }
        else if (Event.current.button == 0 && Event.current.type == EventType.MouseUp)
        {
            return;
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
        if (layer.LayerName == "BaseAnimation")
        {
            var model = animation.Model.gameObject;
            ModelUtils.ShowModel(model);
        }*/
    }

    private void AnimationTimeline_AnimationFinished(int animationInstanceId)
    {
        /*var animation = Timeline.GetAnimation(animationInstanceId);
        var layer = Timeline.GetLayerForAnimation(animationInstanceId);
        if (layer.LayerName == "BaseAnimation")
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
        EditorWindow.GetWindow<LeapAnimationEditor>("Leap Animation");
    }
}

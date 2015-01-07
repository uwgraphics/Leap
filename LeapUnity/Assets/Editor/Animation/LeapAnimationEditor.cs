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

    private AnimationEditGizmos _animationEditGizmos = null;
    private Stopwatch _frameTimer = new Stopwatch();

    private bool _guiInitialized = false;
    private Texture _timelineActiveTexture;
    private Texture _timelineActiveDownTexture;
    private Texture _timelinePlayTexture;
    private Texture _timelinePlayDownTexture;
    private Texture _timelinePrevFrameTexture;
    private Texture _timelineNextFrameTexture;

    private void Update()
    {
        if (Timeline == null)
        {
            // Initialize the animation timeline
            Timeline = AnimationTimeline.Instance;
            Timeline.Active = false;
            Timeline.Stop();
            
            // Subscribe to events from the animation timeline
            Timeline.LayerApplied += new AnimationTimeline.LayerEvtH(AnimationTimeline_LayerApplied);
            Timeline.AnimationStarted += new AnimationTimeline.AnimationEvtH(AnimationTimeline_AnimationStarted);
            Timeline.AnimationFinished += new AnimationTimeline.AnimationEvtH(AnimationTimeline_AnimationStarted);

            // Get component for drawing animation edit gizmos
            _animationEditGizmos = UnityEngine.Object.FindObjectOfType(typeof(AnimationEditGizmos)) as AnimationEditGizmos;
        }

        // Update timings
        _frameTimer.Stop();
        float deltaTime = _frameTimer.ElapsedMilliseconds/1000f;
        _frameTimer.Reset();
        _frameTimer.Start();

        Timeline.Update(deltaTime);
        if (Timeline.Playing)
        {
            SceneView.RepaintAll();
            this.Repaint();
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
                Timeline.ResetModelsToInitialPose();
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
            SceneView.RepaintAll();
        }
        if (GUI.Button(new Rect(160, 10, 40, 40), _timelineNextFrameTexture)
            && Timeline.Active && !Timeline.Playing)
        {
            Timeline.NextFrame();
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

        // Bake anim. controllers
        if (GUI.Button(new Rect(this.position.width - 60, 10, 40, 40), "BC")
            && Timeline.Active && !Timeline.Playing)
        {
            Timeline.BakeInstances();
            SceneView.RepaintAll();
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
    }

    private void _InitGUI()
    {
        if (_guiInitialized)
            return;

        _timelineActiveTexture = Resources.Load<Texture2D>("LeapAnimationEditor/TimelineActive");
        _timelineActiveDownTexture = Resources.Load<Texture2D>("LeapAnimationEditor/TimelineActiveDown");
        _timelinePlayTexture = Resources.Load<Texture2D>("LeapAnimationEditor/TimelinePlay");
        _timelinePlayDownTexture = Resources.Load<Texture2D>("LeapAnimationEditor/TimelinePlayDown");
        _timelinePrevFrameTexture = Resources.Load<Texture2D>("LeapAnimationEditor/TimelinePrevFrame");
        _timelineNextFrameTexture = Resources.Load<Texture2D>("LeapAnimationEditor/TimelineNextFrame");

        _guiInitialized = true;
    }

    private void AnimationTimeline_LayerApplied(string layerName)
    {
        var model = ModelUtils.GetSelectedModel();
        if (model == null)
            return;

        if (layerName == "BaseAnimation")
        {
            // TODO:
            // 1) make the layer(s) for which constraints are shown configurable
            // 2) check for which end-effectors there are active constraints and only show those
            Transform[] lWrist = ModelUtils.GetAllBonesWithTag(model, "LWrist");
            Transform[] rWrist = ModelUtils.GetAllBonesWithTag(model, "RWrist");
            Transform[] lAnkle = ModelUtils.GetAllBonesWithTag(model, "LAnkle");
            Transform[] rAnkle = ModelUtils.GetAllBonesWithTag(model, "RAnkle");
            if (lWrist.Length > 0)
                _animationEditGizmos._SetEndEffectorConstraint(lWrist[0]);
            if (rWrist.Length > 0)
                _animationEditGizmos._SetEndEffectorConstraint(rWrist[0]);
            if (lAnkle.Length > 0)
                _animationEditGizmos._SetEndEffectorConstraint(lAnkle[0]);
            if (rAnkle.Length > 0)
                _animationEditGizmos._SetEndEffectorConstraint(rAnkle[0]);
        }
    }

    private void AnimationTimeline_AnimationStarted(int animationInstanceId)
    {
    }

    private void AnimationTimeline_AnimationFinished(int animationInstanceId)
    {
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

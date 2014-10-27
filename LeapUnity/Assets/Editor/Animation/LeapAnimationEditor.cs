﻿using UnityEngine;
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
            // Create the animation timeline
            Timeline = new AnimationTimeline();
            Timeline.Active = false;
            Timeline.Stop();
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

    /// <summary>
    /// Show/hide Leap animation editor window.
    /// </summary>
    [MenuItem("Window/LEAP Animation")]
    public static void ShowLeapAnimationEditor()
    {
        EditorWindow.GetWindow<LeapAnimationEditor>();
    }
}

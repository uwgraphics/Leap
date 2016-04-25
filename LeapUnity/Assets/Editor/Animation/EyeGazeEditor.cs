using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

/// <summary>
/// This class has static methods for performing eye gaze editing operations.
/// </summary>
public static class EyeGazeEditor
{
    /// <summary>
    /// Gaze editing operation. Add an eye gaze instance to the animation timeline.
    /// </summary>
    /// <param name="timeline">Animation timeline</param>
    /// <param name="newInstance">New eye gaze instance</param>
    /// <param name="newStartFrame">New eye gaze instance start frame</param>
    /// <param name="layerName">Animation layer holding eye gaze animations</param>
    /// <param name="fillGapsWithGazeAhead">If false, gaps on the timeline between gaze instances will be filled
    /// by extending the preceding gaze instances, rather than by inserting gaze-ahead instances.</param>
    public static void AddEyeGaze(AnimationTimeline timeline, EyeGazeInstance newInstance,
        int newStartFrame, string layerName = "Gaze", bool fillGapsWithGazeAhead = true)
    {
        int maxEyeGazeGapLength = Mathf.RoundToInt(LEAPCore.maxEyeGazeGapLength * LEAPCore.editFrameRate);
        int minEyeGazeLength = Mathf.RoundToInt(LEAPCore.minEyeGazeLength * LEAPCore.editFrameRate);

        // Make sure the new instance isn't too short
        newInstance.FrameLength = newInstance.FrameLength >= minEyeGazeLength ? newInstance.FrameLength : minEyeGazeLength;

        // Trim or remove overlapping eye gaze instances
        int newEndFrame = newStartFrame + newInstance.FrameLength - 1;
        List<AnimationTimeline.ScheduledInstance> overlappingInstances = timeline.GetLayer(layerName).Animations.Where(inst =>
            !((inst.StartFrame + inst.Animation.FrameLength - 1) < newStartFrame || inst.StartFrame > newEndFrame) &&
            inst.Animation.Model == newInstance.Model).ToList();
        foreach (var overlappingInstance in overlappingInstances)
        {
            if (timeline.GetAnimation(overlappingInstance.InstanceId) == null)
            {
                // Overlapping instance was removed on a previous iteration
                continue;
            }

            var overlappingGazeInstance = overlappingInstance.Animation as EyeGazeInstance;
            int overlappingStartFrame = overlappingInstance.StartFrame;
            int overlappingEndFrame = overlappingInstance.StartFrame + overlappingInstance.Animation.FrameLength - 1;
            bool overlappingIsGazeAhead = overlappingInstance.Animation.Name.EndsWith(LEAPCore.gazeAheadSuffix);

            if (overlappingStartFrame < newStartFrame)
            {
                // Overlapping instance starts before the new instance
                if (newStartFrame - overlappingStartFrame >= minEyeGazeLength && !overlappingIsGazeAhead)
                {
                    // Overlapping instance reaches fixation before the start of the new instance,
                    // so we can just trim the fixation phase
                    overlappingGazeInstance.FrameLength = newStartFrame - overlappingStartFrame;

                    // If overlapping instance is followed by a gaze-ahead instance,
                    // the gaze-ahead instance must be removed
                    var gazeAheadOverlappingInstance = timeline.GetLayer(layerName).Animations.FirstOrDefault(
                        inst => inst.Animation.Name == overlappingInstance.Animation.Name + LEAPCore.gazeAheadSuffix);
                    if (gazeAheadOverlappingInstance != null)
                        timeline.RemoveAnimation(gazeAheadOverlappingInstance.InstanceId);
                }
                else
                {
                    // Overlapping instance does not reach fixation before the start of the new instance,
                    // so it must be removed altogether
                    if (!overlappingIsGazeAhead)
                    {
                        // Overlapping instance isn't a gaze-ahead instance, so we remove it
                        _RemoveEyeGaze(timeline, overlappingInstance.InstanceId);
                    }
                    else
                    {
                        // Overlapping instance is the gaze-ahead instance of a preceding instance;
                        // we must remove the overlapping instance, but also extend the preceding instance
                        // so that its end lines up with the start of the new instance
                        string prevOverlappingName = overlappingInstance.Animation.Name;
                        prevOverlappingName = prevOverlappingName.Remove(
                            prevOverlappingName.Length - LEAPCore.gazeAheadSuffix.Length, LEAPCore.gazeAheadSuffix.Length);
                        var prevOverlappingInstance = timeline.GetLayer(layerName).Animations.FirstOrDefault(
                            inst => inst.Animation.Name == prevOverlappingName);

                        timeline.RemoveAnimation(overlappingInstance.InstanceId);
                        if (prevOverlappingInstance != null)
                            (prevOverlappingInstance.Animation as EyeGazeInstance).FrameLength = newStartFrame - prevOverlappingInstance.StartFrame;
                    }
                }
            }
            else
            {
                // Overlapping instance starts after the new instance
                if (overlappingEndFrame - newEndFrame >= minEyeGazeLength)
                {
                    // We can delay the start of the overlapping instance after the end of the new instance,
                    // and there will still be time for gaze to reach the target in the overlapping instance
                    if (!overlappingIsGazeAhead)
                    {
                        // Delay the start of the overlapping instance
                        overlappingStartFrame = newEndFrame + 1;
                        overlappingInstance._SetStartFrame(overlappingStartFrame);
                        overlappingGazeInstance.FrameLength = overlappingEndFrame - overlappingStartFrame + 1;
                    }
                    else
                    {
                        // We cannot insert a new gaze instance between a preceding gaze instance and its
                        // gaze-ahead instance, so we remove the gaze-ahead instance
                        timeline.RemoveAnimation(overlappingInstance.InstanceId);
                    }
                }
                else
                {
                    // Delaying the overlapping instance would make it too short for the gaze shift,
                    // so we remove it
                    _RemoveEyeGaze(timeline, overlappingInstance.InstanceId);
                }
            }
        }

        // Fill the gap to the next eye gaze instance
        var nextInstance = timeline.GetLayer(layerName).Animations.FirstOrDefault(inst => inst.StartFrame > newEndFrame &&
            inst.Animation.Model == newInstance.Model);
        if (nextInstance != null)
        {
            // There is another gaze instance following the new one
            int nextStartFrame = nextInstance.StartFrame;

            if (!newInstance.Name.EndsWith(LEAPCore.gazeAheadSuffix))
            {
                // New instance is not a gaze-ahead instance
                if (nextStartFrame - newEndFrame - 1 < maxEyeGazeGapLength ||
                    newInstance.Name.EndsWith(LEAPCore.gazeAheadSuffix) ||
                    !fillGapsWithGazeAhead)
                {
                    // The gap between the end of the new instance and the start of the next instance is small,
                    // so we can just extend the new gaze instance to the start of the next instance
                    newInstance.FrameLength = nextStartFrame - newStartFrame;
                    newEndFrame = nextStartFrame - 1;
                }
                else
                {
                    // The gap between the end of the new instance and the start of the next instance is long
                    if (!nextInstance.Animation.Name.EndsWith(LEAPCore.gazeAheadSuffix))
                    {
                        // We insert a gaze-ahead instance to follow the new instance and extend it to the start
                        // of the next instance
                        var gazeAheadInstance = new EyeGazeInstance(newInstance.Name + LEAPCore.gazeAheadSuffix, newInstance.Model,
                            maxEyeGazeGapLength, -1, null, 0f, 0f, true, newInstance.BaseAnimationClip, null);
                        timeline.AddAnimation(layerName, gazeAheadInstance, newEndFrame + 1);
                    }
                    else
                    {
                        // The next instance is already a gaze-ahead instance, so we just have it start earlier
                        int nextEndFrame = nextInstance.StartFrame + nextInstance.Animation.FrameLength - 1;
                        nextInstance._SetStartFrame(newEndFrame + 1);
                        (nextInstance.Animation as EyeGazeInstance).FrameLength = nextEndFrame - nextInstance.StartFrame + 1;
                    }
                }
            }
            else
            {
                // New instance is a gaze-ahead instance
                if (!nextInstance.Animation.Name.EndsWith(LEAPCore.gazeAheadSuffix))
                {
                    // We extend the new instance to the start of the next instance
                    newEndFrame = nextStartFrame - 1;
                    newInstance.FrameLength = nextStartFrame - newStartFrame;
                }
                else
                {
                    // Next instance is also a gaze-ahead instance, so we merge the two instances
                    newEndFrame = nextStartFrame + nextInstance.Animation.FrameLength - 1;
                    newInstance.FrameLength = newEndFrame - newStartFrame + 1;
                    timeline.RemoveAnimation(nextInstance.InstanceId);
                }
            }
        }
        else if ((timeline.FrameLength - newEndFrame - 1) >= maxEyeGazeGapLength && fillGapsWithGazeAhead)
        {
            // No follow-up gaze instance, but there is plenty more animation left on the timeline
            if (!newInstance.Name.EndsWith(LEAPCore.gazeAheadSuffix))
            {
                // We add a gaze-ahead instance to follow the new instance
                var gazeAheadInstance = new EyeGazeInstance(newInstance.Name + LEAPCore.gazeAheadSuffix,
                    newInstance.Model, timeline.FrameLength - newEndFrame - 1, -1, null, 0f, 0f, true,
                    newInstance.BaseAnimationClip, null);
                timeline.AddAnimation(layerName, gazeAheadInstance, newEndFrame + 1);
            }
            else
            {
                // New instance is a gaze-ahead instance, so we can just extend it to the end of the timeline
                newInstance.FrameLength = timeline.FrameLength - newStartFrame;
                newEndFrame = timeline.FrameLength - 1;
            }
        }
        else
        {
            // No follow-up gaze instance, so we extend the new gaze instance to the end of the animation timeline
            newInstance.FrameLength = timeline.FrameLength - newStartFrame;
        }

        // Fill the gap to the previous eye gaze instance
        var prevInstance = timeline.GetLayer(layerName).Animations.LastOrDefault(
            inst => inst.StartFrame + inst.Animation.FrameLength - 1 < newStartFrame &&
                inst.Animation.Model == newInstance.Model);
        if (prevInstance != null)
        {
            int prevStartFrame = prevInstance.StartFrame;
            int prevEndFrame = prevInstance.StartFrame + prevInstance.Animation.FrameLength - 1;

            if (!prevInstance.Animation.Name.EndsWith(LEAPCore.gazeAheadSuffix))
            {
                // There is a gaze instance preceding the new one, and it is not a gaze-ahead instance
                // of another gaze instance
                if (newStartFrame - prevEndFrame - 1 < maxEyeGazeGapLength || !fillGapsWithGazeAhead)
                {
                    // The gap between the end of the previous instance and the start of the new instance is small,
                    // so we extend the instance so its end lines up with the start of the new instance
                    (prevInstance.Animation as EyeGazeInstance).FrameLength = newStartFrame - prevStartFrame;
                }
                else
                {
                    // The gap between the end of the previous instance and the start of the new instance is large
                    if (!newInstance.Name.EndsWith(LEAPCore.gazeAheadSuffix))
                    {
                        // We add a gaze-ahead instance for the previous gaze instance
                        var prevGazeAheadInstance = new EyeGazeInstance(
                            prevInstance.Animation.Name + LEAPCore.gazeAheadSuffix, prevInstance.Animation.Model,
                            newStartFrame - prevEndFrame - 1, -1, null, 0f, 0f, true, newInstance.BaseAnimationClip, null);
                        timeline.AddAnimation(layerName, prevGazeAheadInstance, prevEndFrame + 1);
                    }
                    else
                    {
                        // The new gaze instance is a gaze-ahead instance, so we just have it start earlier
                        newStartFrame = prevEndFrame + 1;
                        newInstance.FrameLength = newEndFrame - newStartFrame + 1;
                        newEndFrame = newStartFrame + newInstance.FrameLength - 1;
                    }
                }
            }
            else
            {
                // The preceding gaze instance is a gaze-ahead instance of another gaze instance
                if (!newInstance.Name.EndsWith(LEAPCore.gazeAheadSuffix))
                {
                    // We extend the preceding instance to the start of the new instance
                    (prevInstance.Animation as EyeGazeInstance).FrameLength = newStartFrame - prevStartFrame;
                }
                else
                {
                    // The new instance is also a gaze-ahead instance, so we just merge the two instances
                    newStartFrame = prevInstance.StartFrame;
                    newInstance.FrameLength = newEndFrame - newStartFrame + 1;
                    newEndFrame = newStartFrame + newInstance.FrameLength - 1;
                    timeline.RemoveAnimation(prevInstance.InstanceId);
                }
            }
        }
        else
        {
            // No gaze instance is preceding the new one
            if (newStartFrame > minEyeGazeLength)
            {
                // Insert a gaze-ahead instance to fill the gap
                var prevGazeAheadInstance = new EyeGazeInstance(newInstance.Name + LEAPCore.gazeAheadAtStartSuffix,
                    newInstance.Model, newStartFrame - 1, -1, null, 0f, 0f, true, newInstance.BaseAnimationClip, null);
                timeline.AddAnimation(layerName, prevGazeAheadInstance, 1);
            }
            else
            {
                // Just start the new instance a bit earlier
                newStartFrame = 1;
            }
        }

        // Add the new eye gaze instance
        timeline.AddAnimation(layerName, newInstance, newStartFrame);

        UnityEngine.Debug.Log(string.Format("Added new eye gaze instance {0} to layer {1}, character {2}",
            newInstance.Name, layerName, newInstance.Model.name));
    }

    /// <summary>
    /// Gaze editing operation. Remove an eye gaze instance from the animation timeline.
    /// </summary>
    /// <param name="timeline">Animation timeline</param>
    /// <param name="newInstance">Eye gaze instance ID</param>
    /// <param name="layerName">Animation layer holding eye gaze animations</param>
    /// <param name="fillGapsWithGazeAhead">If false, gaps on the timeline between gaze instances will be filled
    /// by extending the preceding gaze instances, rather than by inserting gaze-ahead instances.</param>
    public static void RemoveEyeGaze(AnimationTimeline timeline, int instanceId, string layerName = "Gaze", bool fillGapsWithGazeAhead = true)
    {
        int maxEyeGazeGapLength = Mathf.RoundToInt(LEAPCore.maxEyeGazeGapLength * LEAPCore.editFrameRate);

        // Get the instance to remove
        var instanceToRemove = timeline.GetAnimation(instanceId);
        int startFrame = timeline.GetAnimationStartFrame(instanceId);
        int endFrame = startFrame + instanceToRemove.FrameLength - 1;

        // Get the previous eye gaze instance
        var prevInstance = timeline.GetLayer(layerName).Animations.LastOrDefault(
            inst => inst.StartFrame + inst.Animation.FrameLength - 1 < startFrame &&
                inst.Animation.Model == instanceToRemove.Model);

        // Get the start frame of the next eye gaze instance that is not the gaze-ahead instance for the instance being removed
        var nextInstance = timeline.GetLayer(layerName).Animations.FirstOrDefault(inst => inst.StartFrame > endFrame &&
            inst.Animation.Name != instanceToRemove.Name + LEAPCore.gazeAheadSuffix &&
            inst.Animation.Model == instanceToRemove.Model);
        int nextStartFrame = nextInstance != null ? nextInstance.StartFrame : timeline.FrameLength;

        // First remove the follow-up gaze-ahead instance, if one exists
        var gazeAheadInstanceToRemove = timeline.GetLayer(layerName).Animations.FirstOrDefault(inst =>
            inst.Animation.Name == instanceToRemove.Name + LEAPCore.gazeAheadSuffix);
        if (gazeAheadInstanceToRemove != null)
            timeline.RemoveAnimation(gazeAheadInstanceToRemove.InstanceId);

        // Then remove the actual instance
        timeline.RemoveAnimation(instanceId);

        if (prevInstance != null)
        {
            int prevStartFrame = prevInstance.StartFrame;
            int prevEndFrame = prevStartFrame + prevInstance.Animation.FrameLength - 1;

            if (!prevInstance.Animation.Name.EndsWith(LEAPCore.gazeAheadSuffix))
            {
                // There is a gaze instance preceding the one being removed, and it is not a gaze-ahead instance
                // of another gaze instance
                if (nextStartFrame - prevEndFrame - 1 < maxEyeGazeGapLength || !fillGapsWithGazeAhead)
                {
                    // The gap between the end of the previous instance and the start of the next instance is small,
                    // so we extend the instance so its end lines up with the start of the next instance
                    (prevInstance.Animation as EyeGazeInstance).FrameLength = nextStartFrame - prevStartFrame;
                }
                else
                {
                    // The gap between the end of the previous instance and the start of the next instance is long,
                    // so we add a gaze-ahead instance to follow the new instance
                    var prevGazeAheadInstance = new EyeGazeInstance(
                        prevInstance.Animation.Name + LEAPCore.gazeAheadSuffix, prevInstance.Animation.Model,
                        nextStartFrame - prevEndFrame - 1, -1, null, 0f, 0f, true,
                        (instanceToRemove as EyeGazeInstance).BaseAnimationClip, null);
                    timeline.AddAnimation(layerName, prevGazeAheadInstance, prevEndFrame + 1);
                }
            }
            else
            {
                // The preceding gaze instance is a gaze-ahead instance of another gaze instance -
                // we extend to the start of the next instance
                (prevInstance.Animation as EyeGazeInstance).FrameLength = nextStartFrame - prevStartFrame;
            }
        }

        UnityEngine.Debug.Log(string.Format("Removed eye gaze instance {0} from layer {1}, character {2}",
            instanceToRemove.Name, layerName, instanceToRemove.Model.name));
    }

    /// <summary>
    /// Gaze editing operation. Set new start and end times for the eye gaze instance.
    /// </summary>
    /// <param name="timeline">Animation timeline</param>
    /// <param name="instanceId">Eye gaze instance ID</param>
    /// <param name="startFrame">New start frame for the eye gaze instance</param>
    /// <param name="endFrame">New end frame for the eye gaze instance</param>
    public static void SetEyeGazeTiming(AnimationTimeline timeline, int instanceId,
        int startFrame, int endFrame)
    {
        int minEyeGazeLength = Mathf.RoundToInt(LEAPCore.minEyeGazeLength * LEAPCore.editFrameRate);
        if (endFrame - startFrame + 1 < minEyeGazeLength)
        {
            UnityEngine.Debug.LogError(
                string.Format("Unable to set eye gaze timing; gaze instance must not be shorter than {0} frames", minEyeGazeLength));
        }

        // Changing the timing of an instance is equivalent to removing the instance and then re-adding it with new times
        string layerName = timeline.GetLayerForAnimation(instanceId).LayerName;
        var instance = timeline.GetAnimation(instanceId) as EyeGazeInstance;
        RemoveEyeGaze(timeline, instanceId, layerName, false);
        instance.FrameLength = endFrame - startFrame + 1;
        AddEyeGaze(timeline, instance, startFrame, layerName, false);

        UnityEngine.Debug.Log(string.Format("Set timing of eye gaze instance {0} to start frame {1}, end frame {2}",
            instance.Name, startFrame, endFrame));
    }

    /// <summary>
    /// Gaze editing operation. Set new target for the eye gaze instance.
    /// </summary>
    /// <param name="timeline">Animation timeline</param>
    /// <param name="instanceId">Eye gaze istance ID</param>
    /// <param name="target">New gaze target</param>
    public static void SetEyeGazeTarget(AnimationTimeline timeline, int instanceId, GameObject target)
    {
        var instance = timeline.GetAnimation(instanceId) as EyeGazeInstance;
        instance.Target = target;

        UnityEngine.Debug.Log(string.Format(
            "Set target of eye gaze instance {0} to {1}", instance.Name, target.name));
    }

    /// <summary>
    /// Gaze editing operation. Set new gaze-ahead target position for the eye gaze instance.
    /// </summary>
    /// <param name="timeline">Animation timeline</param>
    /// <param name="instanceId">Eye gaze istance ID</param>
    /// <param name="target">New gaze-ahead target position</param>
    public static void SetEyeGazeAheadTarget(AnimationTimeline timeline, int instanceId, Vector3 aheadTargetPosition)
    {
        var instance = timeline.GetAnimation(instanceId) as EyeGazeInstance;
        instance.Target = null;
        instance.AheadTargetPosition = aheadTargetPosition;

        UnityEngine.Debug.Log(string.Format(
            "Set ahead target position of eye gaze instance {0} to {1}", instance.Name,
            aheadTargetPosition.ToString()));
    }

    /// <summary>
    /// Gaze editing operation. Set new head and torso alignment values for the eye gaze instance.
    /// </summary>
    /// <param name="timeline">Animation timeline</param>
    /// <param name="instanceId">Eye gaze instance ID</param>
    /// <param name="headAlign">New head alignment value for the eye gaze instance</param>
    /// <param name="torsoAlign">New torso alignment value for the eye gaze instance</param>
    /// <param name="turnBody">If true, gaze controller will recruit the body joints during the gaze shift;
    /// otherwise, only the eyes and head will move</param>
    public static void SetEyeGazeAlignments(AnimationTimeline timeline, int instanceId,
        float headAlign, float torsoAlign, bool turnBody)
    {
        headAlign = Mathf.Clamp01(headAlign);
        torsoAlign = Mathf.Clamp01(torsoAlign);
        
        var instance = timeline.GetAnimation(instanceId) as EyeGazeInstance;
        instance.HeadAlign = headAlign;
        instance.TorsoAlign = torsoAlign;
        instance.TurnBody = turnBody;

        UnityEngine.Debug.Log(string.Format(
            "Set alignments of eye gaze instance {0} to head alignment {1}, torso alignment {2}, turnBody = {3}",
            instance.Name, headAlign, torsoAlign, turnBody));
    }

    /// <summary>
    /// Load eye gaze behavior specification for the specified base animation from a file.
    /// </summary>
    /// <param name="timeline">Animation timeline</param>
    /// <param name="baseAnimationInstanceId">Base animation instance ID</param>
    /// <param name="layerName">Animation layer holding eye gaze animations</param>
    /// <param name="fileSuffix">Eye gaze behavior file suffix.</param>
    /// <returns>true if eye gaze instances were loaded successfully, false otherwise</returns>
    public static bool LoadEyeGaze(AnimationTimeline timeline, int baseAnimationInstanceId, string layerName = "Gaze", string fileSuffix = "")
    {
        // Get base animation and character model
        var baseAnimation = timeline.GetAnimation(baseAnimationInstanceId);
        string modelName = baseAnimation.Model.name;

        // Get gaze annotations file path
        string path = Application.dataPath + LEAPCore.eyeGazeAnnotationsDirectory.Substring(
            LEAPCore.eyeGazeAnnotationsDirectory.IndexOfAny(@"/\".ToCharArray()));
        if (path[path.Length - 1] != '/' && path[path.Length - 1] != '\\')
            path += '/';
        path += (baseAnimation.Name + fileSuffix + ".csv");

        // Load gaze behaviors
        try
        {
            var reader = new StreamReader(path);
            bool firstLine = true;
            string line = "";
            string[] lineElements = null;
            Dictionary<string, int> attributeIndices = new Dictionary<string, int>();
            var models = timeline.OwningManager.Models;
            GameObject[] gazeTargets = GameObject.FindGameObjectsWithTag("GazeTarget");

            // Clear existing gaze
            timeline.RemoveAllAnimations(layerName, modelName);

            while (!reader.EndOfStream && (line = reader.ReadLine()) != "")
            {
                if (line[0] == '#')
                {
                    // Comment line, skip
                    continue;
                }
                else if (firstLine)
                {
                    // Load attribute names from first line
                    firstLine = false;
                    lineElements = line.Split(",".ToCharArray());
                    for (int attributeIndex = 0; attributeIndex < lineElements.Length; ++attributeIndex)
                    {
                        attributeIndices[lineElements[attributeIndex]] = attributeIndex;
                    }
                }
                else
                {
                    // Load gaze shift attributes
                    
                    // Load gaze shift specification
                    lineElements = line.Split(",".ToCharArray());

                    // Get model name
                    string characterName = lineElements[attributeIndices["Character"]];
                    GameObject model = models.FirstOrDefault(m => m.name == characterName);
                    if (model == null)
                    {
                        UnityEngine.Debug.LogError(string.Format(
                            "Unable to load eye gaze shift for non-existent model {0}", characterName));
                        continue;
                    }

                    // Get animation name and timings
                    string animationClipName = lineElements[attributeIndices["AnimationClip"]];
                    int startFrame = int.Parse(lineElements[attributeIndices["StartFrame"]]);
                    int frameLength = int.Parse(lineElements[attributeIndices["EndFrame"]]) - startFrame + 1;
                    int fixationStartFrame = attributeIndices.ContainsKey("FixationStartFrame") ?
                        int.Parse(lineElements[attributeIndices["FixationStartFrame"]]) : -1;

                    // If this is a gaze-ahead instance, get ahead target position (if defined)
                    Vector3 aheadTargetPosition = Vector3.zero;
                    if (attributeIndices.ContainsKey("AheadTargetPosition"))
                    {
                        string aheadTargetPosStr = lineElements[attributeIndices["AheadTargetPosition"]];
                        aheadTargetPosStr = aheadTargetPosStr.Substring(1, aheadTargetPosStr.Length - 2);
                        var aheadTargetPosStrElements = aheadTargetPosStr.Split(" ".ToCharArray());
                        aheadTargetPosition = new Vector3(float.Parse(aheadTargetPosStrElements[0]),
                            float.Parse(aheadTargetPosStrElements[1]),
                            float.Parse(aheadTargetPosStrElements[2]));
                    }
                    
                    // Get gaze target
                    string gazeTargetName = lineElements[attributeIndices["Target"]];
                    GameObject gazeTarget = null;
                    if (gazeTargetName != "null")
                    {
                        if (gazeTargets != null)
                            gazeTarget = gazeTargets.FirstOrDefault(obj => obj.name == gazeTargetName);

                        if (gazeTarget == null)
                        {
                            UnityEngine.Debug.LogWarning(string.Format(
                                "Trying to create EyeGazeInstance towards target {0} on model {1}, but the target does not exist; using null instead",
                                gazeTargetName, model.name));
                        }
                    }
                    
                    // Get head and body coordination parameters
                    float headAlign = (float)double.Parse(lineElements[attributeIndices["HeadAlign"]]);
                    headAlign = Mathf.Clamp01(headAlign);
                    float torsoAlign = (float)double.Parse(lineElements[attributeIndices["TorsoAlign"]]);
                    torsoAlign = Mathf.Clamp01(torsoAlign);
                    bool turnBody = bool.Parse(lineElements[attributeIndices["TurnBody"]]);

                    // Create and schedule gaze instance
                    var gazeInstance = new EyeGazeInstance(animationClipName, model, frameLength, fixationStartFrame,
                        gazeTarget, headAlign, torsoAlign, turnBody, (baseAnimation as AnimationClipInstance).AnimationClip);
                    gazeInstance.AheadTargetPosition = aheadTargetPosition;
                    AddEyeGaze(timeline, gazeInstance, startFrame, layerName);
                }
            }

            reader.Close();
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError(string.Format("Unable to load eye gaze from asset file {0}: {1}", path, ex.Message));
            return false;
        }

        return true;
    }

    /// <summary>
    /// Save eye gaze behavior specification for the specified base animation.
    /// </summary>
    /// <param name="timeline">Animation timeline</param>
    /// <param name="baseAnimationInstanceId">Base animation instance ID</param>
    /// <param name="fileSuffix">Eye gaze behavior file suffix.</param>
    /// <returns>true if eye gaze instances were saved successfully, false otherwise</returns>
    public static bool SaveEyeGaze(AnimationTimeline timeline, int baseAnimationInstanceId, string fileSuffix = "")
    {
        // Get base animation
        var baseAnimation = timeline.GetAnimation(baseAnimationInstanceId);

        // Get gaze behavior file path
        string path = Application.dataPath + LEAPCore.eyeGazeAnnotationsDirectory.Substring(
            LEAPCore.eyeGazeAnnotationsDirectory.IndexOfAny(@"/\".ToCharArray()));
        if (path[path.Length - 1] != '/' && path[path.Length - 1] != '\\')
            path += '/';
        path += (baseAnimation.Name + fileSuffix + ".csv");

        // Save gaze behaviors
        try
        {
            var writer = new StreamWriter(path);
            writer.WriteLine("Character,AnimationClip,StartFrame,FixationStartFrame,EndFrame,Target,AheadTargetPosition,HeadAlign,TorsoAlign,TurnBody");

            // Get all gaze instances, sorted by start time
            var gazeInstances = new List<AnimationTimeline.ScheduledInstance>();
            foreach (var layer in timeline.Layers)
            {
                foreach (var instance in layer.Animations)
                {
                    if (!(instance.Animation is EyeGazeInstance) ||
                        instance.Animation.Model != baseAnimation.Model)
                    {
                        continue;
                    }

                    int gazeInstanceIndex = gazeInstances.Count - 1;
                    for (; gazeInstanceIndex >= 0; --gazeInstanceIndex)
                    {
                        if (gazeInstances[gazeInstanceIndex].StartFrame <= instance.StartFrame)
                        {
                            gazeInstances.Insert(gazeInstanceIndex + 1, instance);
                            break;
                        }
                    }

                    if (gazeInstanceIndex < 0)
                        gazeInstances.Insert(0, instance);
                }
            }

            // Write out the gaze instances
            var lineBuilder = new StringBuilder();
            foreach (var instance in gazeInstances)
            {
                var gazeInstance = instance.Animation as EyeGazeInstance;
                lineBuilder.Append(gazeInstance.Model.name);
                lineBuilder.Append(",");
                lineBuilder.Append(gazeInstance.Name);
                lineBuilder.Append(",");
                lineBuilder.Append(instance.StartFrame);
                lineBuilder.Append(",");
                lineBuilder.Append(gazeInstance.FixationStartFrame);
                lineBuilder.Append(",");
                lineBuilder.Append(instance.StartFrame + gazeInstance.FrameLength - 1);
                lineBuilder.Append(",");
                lineBuilder.Append(gazeInstance.Target != null ? gazeInstance.Target.name : "null");
                lineBuilder.Append(",");
                lineBuilder.Append(string.Format("\"{0} {1} {2}\"",
                    gazeInstance.AheadTargetPosition.x, gazeInstance.AheadTargetPosition.y, gazeInstance.AheadTargetPosition.z));
                lineBuilder.Append(",");
                lineBuilder.Append(gazeInstance.HeadAlign);
                lineBuilder.Append(",");
                lineBuilder.Append(gazeInstance.TorsoAlign);
                lineBuilder.Append(",");
                lineBuilder.Append(gazeInstance.TurnBody);

                writer.WriteLine(lineBuilder);
                lineBuilder.Length = 0;
            }

            writer.Close();
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError(string.Format("Unable to save eye gaze to asset file {0}: {1}", path, ex.Message));
            return false;
        }

        return true;
    }

    /// <summary>
    /// Print eye gaze instances to the Unity console.
    /// </summary>
    /// <param name="timeline">Animation timeline</param>
    /// <param name="layerName">Animation layer holding the eye gaze animations</param>
    /// <param name="model">Character model for which gaze should be printed (if null, all gaze is printed, regardless of the model)</param>
    public static void PrintEyeGaze(AnimationTimeline timeline, string layerName = "Gaze", GameObject model = null)
    {
        foreach (var instance in timeline.GetLayer(layerName).Animations)
        {
            if (model != null && instance.Animation.Model != model)
                continue;

            UnityEngine.Debug.Log(string.Format(
                "EyeGazeInstance: model = {0}, animationClip = {1}, startFrame = {2}, fixationStartFrame = {3}, endFrame = {4}, target = {5}, headAlign = {6}, torsoAlign = {7}, turnBody = {8}",
                instance.Animation.Model.name, instance.Animation.Name,
                instance.StartFrame,
                instance.StartFrame + (instance.Animation as EyeGazeInstance).FixationStartFrame,
                instance.StartFrame + instance.Animation.FrameLength - 1,
                (instance.Animation as EyeGazeInstance).Target != null ? (instance.Animation as EyeGazeInstance).Target.name : "null",
                (instance.Animation as EyeGazeInstance).HeadAlign, (instance.Animation as EyeGazeInstance).TorsoAlign,
                (instance.Animation as EyeGazeInstance).TurnBody));
        }
    }

    /// <summary>
    /// Extend eye gaze instances such that gaze remains fixed to the target until
    /// the next eye gaze instance. That way, gaze behavior is always constrained by
    /// artist annotations.
    /// </summary>
    /// <param name="timeline">Animation timeline</param>
    /// <param name="baseAnimationInstanceId">Base animation instance ID</param>
    /// <param name="layerName">Animation layer holding eye gaze animations</param>
    public static void FixEyeGazeBetweenShifts(AnimationTimeline timeline, string layerName = "Gaze")
    {
        bool timelineActive = timeline.Active;
        timeline.Active = true;

        var gazeLayer = timeline.GetLayer(layerName);
        EyeGazeInstance prevInstance = null;
        int prevStartFrame = 0;
        foreach (var curScheduledInstance in gazeLayer.Animations)
        {
            if (!(curScheduledInstance.Animation is EyeGazeInstance))
                continue;

            var curInstance = curScheduledInstance.Animation as EyeGazeInstance;
            if (prevInstance != null)
            {
                prevInstance.FrameLength = curScheduledInstance.StartFrame - prevStartFrame;
            }

            prevInstance = curInstance;
            prevStartFrame = curScheduledInstance.StartFrame;
        }

        if (prevInstance != null)
        {
            prevInstance.FrameLength = timeline.FrameLength - prevStartFrame;
        }

        timeline.Active = timelineActive;
    }

    /// <summary>
    /// Get initial state of the eye gaze controller at the start of the specified gaze shift.
    /// </summary>
    /// <param name="instance">Eye gaze instance</param>
    /// <returns>Gaze controller state</returns>
    public static GazeControllerState GetInitControllerForEyeGazeInstance(EyeGazeInstance instance)
    {
        return GetInitControllerForEyeGazeInstance(instance, Vector3.zero);
    }

    /// <summary>
    /// Get initial state of the eye gaze controller at the start of the specified gaze shift.
    /// </summary>
    /// <param name="instance">Eye gaze instance</param>
    /// <returns>Gaze controller state</returns>
    public static GazeControllerState GetInitControllerForEyeGazeInstance(EyeGazeInstance instance,
        Vector3 movingTargetPosOff)
    {
        var curState = instance.Controller.GetRuntimeState();

        // Get initial gaze controller state
        var gazeController = instance.GazeController;
        gazeController.useTorso = instance.TurnBody;
        if (instance.Target != null)
            gazeController.GazeAt(instance.Target, movingTargetPosOff);
        else
            gazeController.GazeAt(instance.AheadTargetPosition, movingTargetPosOff);
        gazeController.head.align = instance.HeadAlign >= 0f ? instance.HeadAlign : 0f;
        if (gazeController.torso != null)
            gazeController.torso.align = instance.TorsoAlign >= 0f ? instance.HeadAlign : 0f;
        var state = (GazeControllerState)gazeController.GetInitRuntimeState();

        instance.Controller.SetRuntimeState(curState);

        return state;
    }

    /// <summary>
    /// Reset all gaze controllers on the specified character models.
    /// </summary>
    /// <param name="models">Character models</param>
    public static void ResetEyeGazeControllers(GameObject[] models)
    {
        foreach (var model in models)
        {
            var gazeController = model.GetComponent<GazeController>();
            if (gazeController == null)
                continue;

            GazeControllerState zeroState = gazeController.GetZeroRuntimeState();
            gazeController.SetRuntimeState(zeroState);
        }
    }

    /// <summary>
    /// Estimate gaze shift duration (from gaze shift start to fixation start) of
    /// the specified eye gaze instance.
    /// </summary>
    /// <param name="instance">Eye gaze instance</param>
    /// <param name="state">Eye gaze controller state at the start of the gaze shift</param>
    /// <returns>Gaze shift duration in seconds</returns>
    public static float EstimateGazeShiftTimeLength(EyeGazeInstance instance, GazeControllerState state)
    {
        IAnimControllerState curState = instance.Controller.GetRuntimeState();
        instance.Controller.SetRuntimeState(state);
        var gazeController = instance.GazeController;

        // Estimate eye rotation time
        float lEyeDistRot = gazeController.lEye._Amplitude;
        float rEyeDistRot = gazeController.rEye._Amplitude;
        float lEyeVelocity = 0.75f * gazeController.lEye._MaxVelocity;
        float rEyeVelocity = 0.75f * gazeController.lEye._MaxVelocity;
        float eyeRotTime = Mathf.Max(lEyeVelocity > 0f ? lEyeDistRot / lEyeVelocity : 0f,
            rEyeVelocity > 0f ? rEyeDistRot / rEyeVelocity : 0f);

        // Estimate head rotation time
        float headDistRot = gazeController.head._Amplitude;
        float headVelocity = 0.625f * gazeController.head._MaxVelocity;
        float headRotTime = headVelocity > 0f ? headDistRot / headVelocity : 0f;

        // Estimate torso rotation time
        float torsoRotTime = 0f;
        if (gazeController.torso.gazeJoints.Length > 0 && gazeController.useTorso)
        {
            float torsoDistRot = gazeController.torso._Amplitude;
            float torsoVelocity = 0.625f * gazeController.torso._MaxVelocity;
            torsoRotTime = torsoVelocity > 0f ? torsoDistRot / torsoVelocity : 0f;
        }

        instance.Controller.SetRuntimeState(curState);

        float totalTime = Mathf.Max(eyeRotTime, headRotTime, torsoRotTime);
        // TODO: gaze shift duration may be getting underestimated
        return totalTime;
    }

    /// <summary>
    /// Compute difference in effective gaze target position due to moving base
    /// for the specified gaze shift.
    /// </summary>
    /// <param name="timeline">Animation timeline</param>
    /// <param name="baseAnimationInstanceId">Base animation instance ID</param>
    /// <param name="eyeGazeInstance">Eye gaze instance<param>
    /// <param name="eyeGazeStartFrame">Eye gaze start frame</param>
    /// <param name="eyeGazeTargetPosition">Eye gaze target position without offset</param>
    /// <param name="baseAnimationLayerName">Base animation layer name</param>
    public static Vector3 ComputeMovingGazeTargetPositionOffset(EyeGazeInstance instance, TimeSet startTimes,
        AnimationClipInstance baseInstance, AnimationClipInstance targetInstance = null)
    {
        if (!LEAPCore.adjustGazeTargetForMovingBase)
            return Vector3.zero;

        var model = instance.Model;
        var gazeController = instance.GazeController;
        var root = gazeController.Root;
        var target = instance.Target;

        var fixationStartTimes = startTimes + LEAPCore.ToTime(instance.FixationStartFrame);
        if (instance.FixationStartFrame < 0)
        {
            var initState = GetInitControllerForEyeGazeInstance(instance);
            float estTimeLength = EstimateGazeShiftTimeLength(instance, initState);
            fixationStartTimes = startTimes + estTimeLength;
        }

        // Store current model and target pose
        var curModelPose = new ModelPose(model);
        var curTargetPose = targetInstance != null ? new ModelPose(target) : new ModelPose();

        // Get base position and rotation at the start of the gaze shift
        baseInstance.Apply(startTimes, AnimationLayerMode.Override);
        Vector3 pos0 = root.position;
        Quaternion rot0 = root.rotation;

        // Get base position and rotation at the end of the gaze shift
        baseInstance.Apply(fixationStartTimes, AnimationLayerMode.Override);
        Vector3 pos1 = root.position;
        Quaternion rot1 = root.rotation;
        
        // Get target position at the end of the gaze shift
        if (targetInstance != null)
            targetInstance.Apply(LEAPCore.ToFrame(fixationStartTimes.rootTime), AnimationLayerMode.Override);
        var targetPos = target != null ? target.transform.position : instance.AheadTargetPosition;
        // TODO: incorrect use of fixationStartTimes, target animation may not have the same timewarps as the base animation

        // Compute target position offset
        Vector3 vt1 = targetPos - pos1;
        Quaternion dq = Quaternion.Inverse(rot1) * rot0;
        Vector3 vt0 = dq * vt1;
        Vector3 pt1 = pos0 + vt0;
        Vector3 targetPosOff = pt1 - targetPos;

        // Reapply current model and target pose
        curModelPose.Apply(model);
        if (targetInstance != null)
            curTargetPose.Apply(target);

        return targetPosOff;
    }

    /// <summary>
    /// Print the state of the gaze controller on the specified character model.
    /// </summary>
    /// <param name="model">Character model</param>
    public static void PrintEyeGazeControllerState(GameObject model)
    {
        var gazeController = model.GetComponent<GazeController>();
        if (gazeController == null)
            throw new Exception(string.Format("Cannot log gaze controller state becase model {0} has no gaze controller", model.name));

        GazeControllerState state = (GazeControllerState)gazeController.GetRuntimeState();

        UnityEngine.Debug.Log(string.Format("GazeController: state = {0}, gazeTarget = {1}, doGazeShift = {2}, stopGazeShift = {3}, " +
            "fixGaze = {4}, useTorso = {5}, pelvisAlign = {6}, currentGazeTarget = {7}, fixGazeTarget = {8}, amplitude = {9}, weight = {10}",
            gazeController.State, state.gazeTarget != null ? state.gazeTarget.name : "null",
            state.doGazeShift, state.stopGazeShift, state.fixGaze,
            state.useTorso, state.pelvisAlign,
            state.curGazeTarget != null ? state.curGazeTarget.name : "null",
            state.fixGazeTarget != null ? state.fixGazeTarget.name : "null",
            state.amplitude, state.weight));

        _PrintEyeGazeBodyPartState(state.lEyeState);
        _PrintEyeGazeBodyPartState(state.rEyeState);
        _PrintEyeGazeBodyPartState(state.headState);
        _PrintEyeGazeBodyPartState(state.torsoState);
    }

    // Gaze editing helper operation; remove an eye gaze instance from the animation timeline
    private static void _RemoveEyeGaze(AnimationTimeline timeline, int instanceId)
    {
        // First remove the follow-up gaze-ahead instance, if one exists
        string layerName = timeline.GetLayerForAnimation(instanceId).LayerName;
        var instance = timeline.GetAnimation(instanceId) as EyeGazeInstance;
        var gazeAheadInstance = timeline.GetLayer(layerName).Animations.FirstOrDefault(inst =>
            inst.Animation.Name == instance.Name + LEAPCore.gazeAheadSuffix);
        if (gazeAheadInstance != null)
            timeline.RemoveAnimation(gazeAheadInstance.InstanceId);

        // Then remove the actual instnace
        timeline.RemoveAnimation(instanceId);
    }

    // Print individual gaze joint state
    private static void _PrintEyeGazeBodyPartState(GazeControllerState.GazeBodyPartState state)
    {
        UnityEngine.Debug.Log(string.Format("{0}: curAlign = {1}, " +
            "curVelocity = {2} [maxVelocity = {3}], latency = {4}, cur*OMR = ({5}, {6}, {7}, {8}), " +
            "baseDir = ({9}, {10}, {11}), " +
            "srcDir0 = ({12}, {13}, {14}), srcDir = ({15}, {16}, {17}), trgDir = ({18}, {19}, {20}), trgDirAlign = ({21}, {22}, {23}), " +
            "rotParam = {24}, curDir = ({25}, {26}, {27}), " +
            "isFix = {28}, fixSrcDir0 = ({29}, {30}, {31}), fixSrcDir = ({32}, {33}, {34}), fixTrgDir = ({35}, {36}, {37}), " +
            "fixTrgDirAlign = ({38}, {39}, {40}), weight = {41}",
            ((GazeBodyPartType)state.gazeBodyPartType).ToString(),
            state.curAlign, state.curVelocity, state.maxVelocity, state.latency,
            state.curInOMR, state.curOutOMR, state.curUpOMR, state.curDownOMR,
            state.baseDir.x, state.baseDir.y, state.baseDir.z,
            state.srcDir0.x, state.srcDir0.y, state.srcDir0.z,
            state.srcDir.x, state.srcDir.y, state.srcDir.z,
            state.trgDir.x, state.trgDir.y, state.trgDir.z,
            state.trgDirAlign.x, state.trgDirAlign.y, state.trgDirAlign.z, state.rotParam,
            state.curDir.x, state.curDir.y, state.curDir.z,
            state.isFix, state.fixSrcDir0.x, state.fixSrcDir0.y, state.fixSrcDir0.z,
            state.fixSrcDir.x, state.fixSrcDir.y, state.fixSrcDir.z,
            state.fixTrgDir.x, state.fixTrgDir.y, state.fixTrgDir.z,
            state.fixTrgDirAlign.x, state.fixTrgDirAlign.y, state.fixTrgDirAlign.z, state.weight));
    }
}

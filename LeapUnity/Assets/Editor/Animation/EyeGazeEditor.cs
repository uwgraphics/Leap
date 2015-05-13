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
    public static void AddEyeGaze(AnimationTimeline timeline, EyeGazeInstance newInstance,
        int newStartFrame, string layerName = "Gaze")
    {
        int maxEyeGazeGapLength = Mathf.RoundToInt(LEAPCore.maxEyeGazeGapLength * LEAPCore.editFrameRate);
        int minEyeGazeLength = Mathf.RoundToInt(LEAPCore.minEyeGazeLength * LEAPCore.editFrameRate);

        // Make sure the new instance isn't too short
        newInstance.SetFrameLength(newInstance.FrameLength >= minEyeGazeLength ? newInstance.FrameLength : minEyeGazeLength);

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
            bool overlappingIsGazeAhead = overlappingInstance.Animation.AnimationClip.name.EndsWith(LEAPCore.gazeAheadSuffix);

            if (overlappingStartFrame < newStartFrame)
            {
                // Overlapping instance starts before the new instance

                if (newStartFrame - overlappingStartFrame >= minEyeGazeLength)
                {
                    // Overlapping instance reaches fixation before the start of the new instance,
                    // so we can just trim the fixation phase

                    overlappingGazeInstance.SetFrameLength(newStartFrame - overlappingStartFrame);

                    if (!overlappingIsGazeAhead)
                    {
                        // If overlapping instance is followed by a gaze-ahead instance,
                        // the gaze-ahead instance must be removed

                        var gazeAheadOverlappingInstance = timeline.GetLayer(layerName).Animations.FirstOrDefault(
                            inst => inst.Animation.AnimationClip.name == overlappingInstance.Animation.AnimationClip.name + LEAPCore.gazeAheadSuffix);
                        if (gazeAheadOverlappingInstance != null)
                            timeline.RemoveAnimation(gazeAheadOverlappingInstance.InstanceId);
                    }
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

                        string prevOverlappingName = overlappingInstance.Animation.AnimationClip.name;
                        prevOverlappingName = prevOverlappingName.Remove(
                            prevOverlappingName.Length - LEAPCore.gazeAheadSuffix.Length, LEAPCore.gazeAheadSuffix.Length);
                        var prevOverlappingInstance = timeline.GetLayer(layerName).Animations.FirstOrDefault(
                            inst => inst.Animation.AnimationClip.name == prevOverlappingName);

                        timeline.RemoveAnimation(overlappingInstance.InstanceId);
                        if (prevOverlappingInstance != null)
                            (prevOverlappingInstance.Animation as EyeGazeInstance).SetFrameLength(newStartFrame - prevOverlappingInstance.StartFrame);
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

                        overlappingInstance._SetStartFrame(newEndFrame + 1);
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

            if (!newInstance.AnimationClip.name.EndsWith(LEAPCore.gazeAheadSuffix))
            {
                // New instance is not a gaze-ahead instance

                if (nextStartFrame - newEndFrame - 1 < maxEyeGazeGapLength ||
                    newInstance.AnimationClip.name.EndsWith(LEAPCore.gazeAheadSuffix))
                {
                    // The gap between the end of the new instance and the start of the next instance is small,
                    // so we can just extend the new gaze instance to the start of the next instance
                    newInstance.SetFrameLength(nextStartFrame - newStartFrame);
                    newEndFrame = nextStartFrame - 1;
                }
                else
                {
                    // The gap between the end of the new instance and the start of the next instance is long

                    if (!nextInstance.Animation.AnimationClip.name.EndsWith(LEAPCore.gazeAheadSuffix))
                    {
                        // We insert a gaze-ahead instance to follow the new instance and extend it to the start
                        // of the next instance
                        var gazeAheadInstance = new EyeGazeInstance(newInstance.Model, newInstance.AnimationClip.name + LEAPCore.gazeAheadSuffix,
                            //Mathf.RoundToInt(LEAPCore.maxEyeGazeGapLength * LEAPCore.editFrameRate), null);
                            maxEyeGazeGapLength, null, 0f, 0f, -1, true, false);
                        timeline.AddAnimation(layerName, gazeAheadInstance, newEndFrame + 1);
                    }
                    else
                    {
                        // The next instance is already a gaze-ahead instance, so we just have it start earlier
                        int nextEndFrame = nextInstance.StartFrame + nextInstance.Animation.FrameLength - 1;
                        nextInstance._SetStartFrame(newEndFrame + 1);
                        (nextInstance.Animation as EyeGazeInstance).SetFrameLength(nextEndFrame - nextInstance.StartFrame + 1);
                    }
                }
            }
            else
            {
                // New instance is a gaze-ahead instance
                
                if (!nextInstance.Animation.AnimationClip.name.EndsWith(LEAPCore.gazeAheadSuffix))
                {
                    // We extend the new instance to the start of the next instance
                    newEndFrame = nextStartFrame - 1;
                    newInstance.SetFrameLength(nextStartFrame - newStartFrame);
                }
                else
                {
                    // Next instance is also a gaze-ahead instance, so we merge the two instances
                    newEndFrame = nextStartFrame + nextInstance.Animation.FrameLength - 1;
                    newInstance.SetFrameLength(newEndFrame - newStartFrame + 1);
                    timeline.RemoveAnimation(nextInstance.InstanceId);
                }
            }
        }
        else if (timeline.FrameLength - newEndFrame - 1 >= maxEyeGazeGapLength)
        {
            // No follow-up gaze instance, but there is plenty more animation left on the timeline

            if (!newInstance.AnimationClip.name.EndsWith(LEAPCore.gazeAheadSuffix))
            {
                // We add a gaze-ahead instance to follow the new instance
                var gazeAheadInstance = new EyeGazeInstance(
                    newInstance.Model, newInstance.AnimationClip.name + LEAPCore.gazeAheadSuffix,
                    //Mathf.RoundToInt(LEAPCore.maxEyeGazeGapLength * LEAPCore.editFrameRate), null);
                    timeline.FrameLength - newEndFrame - 1, null, 0f, 0f, -1, true, false);
                timeline.AddAnimation(layerName, gazeAheadInstance, newEndFrame + 1);
            }
            else
            {
                // New instance is a gaze-ahead instance, so we can just extend it to the end of the timeline
                newInstance.SetFrameLength(timeline.FrameLength - newStartFrame);
                newEndFrame = timeline.FrameLength - 1;
            }
        }
        else if (timeline.FrameLength - newEndFrame - 1 > 0)
        {
            // No follow-up gaze instance, so we extend the new gaze instance to the end of the animation timeline
            newInstance.SetFrameLength(timeline.FrameLength - newStartFrame);
        }

        // Fill the gap to the previous eye gaze instance
        var prevInstance = timeline.GetLayer(layerName).Animations.LastOrDefault(
            inst => inst.StartFrame + inst.Animation.FrameLength - 1 < newStartFrame &&
                inst.Animation.Model == newInstance.Model);
        if (prevInstance != null)
        {
            int prevStartFrame = prevInstance.StartFrame;
            int prevEndFrame = prevInstance.StartFrame + prevInstance.Animation.FrameLength - 1;

            if (!prevInstance.Animation.AnimationClip.name.EndsWith(LEAPCore.gazeAheadSuffix))
            {
                // There is a gaze instance preceding the new one, and it is not a gaze-ahead instance
                // of another gaze instance

                if (newStartFrame - prevEndFrame - 1 < maxEyeGazeGapLength)
                {
                    // The gap between the end of the previous instance and the start of the new instance is small,
                    // so we extend the instance so its end lines up with the start of the new instance

                    (prevInstance.Animation as EyeGazeInstance).SetFrameLength(newStartFrame - prevStartFrame);
                }
                else
                {
                    // The gap between the end of the previous instance and the start of the new instance is large

                    if (!newInstance.AnimationClip.name.EndsWith(LEAPCore.gazeAheadSuffix))
                    {
                        // We add a gaze-ahead instance for the previous gaze instance
                        var prevGazeAheadInstance = new EyeGazeInstance(
                            prevInstance.Animation.Model, prevInstance.Animation.AnimationClip.name + LEAPCore.gazeAheadSuffix,
                            //Mathf.RoundToInt(LEAPCore.maxEyeGazeGapLength * LEAPCore.editFrameRate), null);
                            newStartFrame - prevEndFrame - 1, null, 0f, 0f, -1, true, false);
                        timeline.AddAnimation(layerName, prevGazeAheadInstance, prevEndFrame + 1);
                    }
                    else
                    {
                        // The new gaze instance is a gaze-ahead instance, so we just have it start earlier
                        newStartFrame = prevEndFrame + 1;
                        newInstance.SetFrameLength(newEndFrame - newStartFrame + 1);
                        newEndFrame = newStartFrame + newInstance.FrameLength - 1;
                    }
                }
            }
            else
            {
                // The preceding gaze instance is a gaze-ahead instance of another gaze instance

                if (!newInstance.AnimationClip.name.EndsWith(LEAPCore.gazeAheadSuffix))
                {
                    // We extend the preceding instance to the start of the new instance
                    (prevInstance.Animation as EyeGazeInstance).SetFrameLength(newStartFrame - prevStartFrame);
                }
                else
                {
                    // The new instance is also a gaze-ahead instance, so we just merge the two instances
                    newStartFrame = prevInstance.StartFrame;
                    newInstance.SetFrameLength(newEndFrame - newStartFrame + 1);
                    newEndFrame = newStartFrame + newInstance.FrameLength - 1;
                    timeline.RemoveAnimation(prevInstance.InstanceId);
                }
            }
        }

        // Add the new eye gaze instance
        timeline.AddAnimation(layerName, newInstance, newStartFrame);

        UnityEngine.Debug.Log(string.Format("Added new eye gaze instance {0} to layer {1}, character {2}",
            newInstance.AnimationClip.name, layerName, newInstance.Model.name));
    }

    /// <summary>
    /// Gaze editing operation. Remove an eye gaze instance from the animation timeline.
    /// </summary>
    /// <param name="timeline">Animation timeline</param>
    /// <param name="newInstance">Eye gaze instance ID</param>
    /// <param name="layerName">Animation layer holding eye gaze animations</param>
    public static void RemoveEyeGaze(AnimationTimeline timeline, int instanceId, string layerName = "Gaze")
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
        int prevStartFrame = prevInstance.StartFrame;
        int prevEndFrame = prevStartFrame + prevInstance.Animation.FrameLength - 1;

        // Get the start frame of the next eye gaze instance that is not the gaze-ahead instance for the instance being removed
        var nextInstance = timeline.GetLayer(layerName).Animations.FirstOrDefault(inst => inst.StartFrame > endFrame &&
            inst.Animation.AnimationClip.name != instanceToRemove.AnimationClip.name + LEAPCore.gazeAheadSuffix &&
            inst.Animation.Model == instanceToRemove.Model);
        int nextStartFrame = nextInstance != null ? nextInstance.StartFrame : timeline.FrameLength;

        // First remove the follow-up gaze-ahead instance, if one exists
        var gazeAheadInstanceToRemove = timeline.GetLayer(layerName).Animations.FirstOrDefault(inst =>
            inst.Animation.AnimationClip.name == instanceToRemove.AnimationClip.name + LEAPCore.gazeAheadSuffix);
        if (gazeAheadInstanceToRemove != null)
            timeline.RemoveAnimation(gazeAheadInstanceToRemove.InstanceId);

        // Then remove the actual instance
        timeline.RemoveAnimation(instanceId);

        if (prevInstance != null)
        {
            if (!prevInstance.Animation.AnimationClip.name.EndsWith(LEAPCore.gazeAheadSuffix))
            {
                // There is a gaze instance preceding the one being removed, and it is not a gaze-ahead instance
                // of another gaze instance

                if (nextStartFrame - prevEndFrame - 1 < maxEyeGazeGapLength)
                {
                    // The gap between the end of the previous instance and the start of the next instance is small,
                    // so we extend the instance so its end lines up with the start of the next instance

                    (prevInstance.Animation as EyeGazeInstance).SetFrameLength(nextStartFrame - prevStartFrame);
                }
                else
                {
                    // The gap between the end of the previous instance and the start of the next instance is long,
                    // so we add a gaze-ahead instance to follow the new instance

                    var prevGazeAheadInstance = new EyeGazeInstance(
                        prevInstance.Animation.Model, prevInstance.Animation.AnimationClip.name + LEAPCore.gazeAheadSuffix,
                        //Mathf.RoundToInt(LEAPCore.maxEyeGazeGapLength * LEAPCore.editFrameRate), null);
                        nextStartFrame - prevEndFrame - 1, null, 0f, 0f, -1, true, false);
                    timeline.AddAnimation(layerName, prevGazeAheadInstance, prevEndFrame + 1);
                }
            }
            else
            {
                // The preceding gaze instance is a gaze-ahead instance of another gaze instance -
                // we extend to the start of the next instance
                (prevInstance.Animation as EyeGazeInstance).SetFrameLength(nextStartFrame - prevStartFrame);
            }
        }

        UnityEngine.Debug.Log(string.Format("Removed eye gaze instance {0} from layer {1}, character {2}",
            instanceToRemove.AnimationClip.name, layerName, instanceToRemove.Model.name));
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
        RemoveEyeGaze(timeline, instanceId, layerName);
        instance.SetFrameLength(endFrame - startFrame + 1);
        AddEyeGaze(timeline, instance, startFrame, layerName);

        UnityEngine.Debug.Log(string.Format("Set timing of eye gaze instance {0} to start frame {1}, end frame {2}",
            instance.AnimationClip.name, startFrame, endFrame));
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
            instance.AnimationClip.name, headAlign, torsoAlign, turnBody));
    }

    /// <summary>
    /// Load eye gaze behavior specification for the specified base animation.
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

        // Get gaze behavior file path
        string path = Application.dataPath + LEAPCore.eyeGazeDirectory.Substring(LEAPCore.eyeGazeDirectory.IndexOfAny(@"/\".ToCharArray()));
        if (path[path.Length - 1] != '/' && path[path.Length - 1] != '\\')
            path += '/';
        path += (baseAnimation.AnimationClip.name + fileSuffix + ".csv");

        // Load gaze behaviors
        try
        {
            var reader = new StreamReader(path);
            bool firstLine = true;
            string line = "";
            string[] lineElements = null;
            Dictionary<string, int> attributeIndices = new Dictionary<string, int>();
            var models = timeline.Models;
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
                            UnityEngine.Debug.LogError(string.Format(
                                "Trying to create EyeGazeInstance towards target {0} on model {1}, but the target does not exist!",
                                gazeTargetName, model.name));
                            continue;
                        }
                    }
                    
                    // Get head and body coordination parameters
                    float headAlign = (float)double.Parse(lineElements[attributeIndices["HeadAlign"]]);
                    headAlign = Mathf.Clamp01(headAlign);
                    float torsoAlign = (float)double.Parse(lineElements[attributeIndices["TorsoAlign"]]);
                    torsoAlign = Mathf.Clamp01(torsoAlign);
                    bool turnBody = bool.Parse(lineElements[attributeIndices["TurnBody"]]);

                    // Create and schedule gaze instance
                    var gazeInstance = new EyeGazeInstance(model,
                        animationClipName, frameLength, gazeTarget, headAlign, torsoAlign, fixationStartFrame, turnBody, true, startFrame);
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
        string path = Application.dataPath + LEAPCore.eyeGazeDirectory.Substring(LEAPCore.eyeGazeDirectory.IndexOfAny(@"/\".ToCharArray()));
        if (path[path.Length - 1] != '/' && path[path.Length - 1] != '\\')
            path += '/';
        path += (baseAnimation.AnimationClip.name + fileSuffix + ".csv");

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
                lineBuilder.Append(gazeInstance.AnimationClip.name);
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
    public static void PrintEyeGaze(AnimationTimeline timeline)
    {
        foreach (var instance in timeline.GetLayer("Gaze").Animations)
        {
            UnityEngine.Debug.Log(string.Format(
                "EyeGazeInstance: model = {0}, animationClip = {1}, startFrame = {2}, fixationStartFrame = {3}, endFrame = {4}, target = {5}, headAlign = {6}, torsoAlign = {7}, turnBody = {8}, isBase = {9}",
                instance.Animation.Model.name, instance.Animation.AnimationClip.name,
                instance.StartFrame,
                instance.StartFrame + (instance.Animation as EyeGazeInstance).FixationStartFrame,
                instance.StartFrame + instance.Animation.FrameLength - 1,
                (instance.Animation as EyeGazeInstance).Target != null ? (instance.Animation as EyeGazeInstance).Target.name : "null",
                (instance.Animation as EyeGazeInstance).HeadAlign, (instance.Animation as EyeGazeInstance).TorsoAlign,
                (instance.Animation as EyeGazeInstance).TurnBody,
                (instance.Animation as EyeGazeInstance).IsBase));
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
                prevInstance.SetFrameLength(curScheduledInstance.StartFrame - prevStartFrame);
            }

            prevInstance = curInstance;
            prevStartFrame = curScheduledInstance.StartFrame;
        }

        if (prevInstance != null)
        {
            prevInstance.SetFrameLength(timeline.FrameLength - prevStartFrame);
        }

        timeline.Active = timelineActive;
    }

    /// <summary>
    /// Infer gaze shift and fixation timings and targets for the specified base animation.
    /// </summary>
    /// <param name="timeline">Animation timeline</param>
    /// <param name="baseAnimationInstanceId">Base animation instance ID</param>
    /// <param name="layerName">Animation layer for eye gaze animations</param>
    public static void InferEyeGazeInstances(AnimationTimeline  timeline, int baseAnimationInstanceId, string layerName = "Gaze")
    {
        var baseAnimation = timeline.GetAnimation(baseAnimationInstanceId);
        var gazeLayer = timeline.GetLayer(layerName);

        // Clear all existing gaze instances from the timeline
        timeline.RemoveAllAnimations(layerName);

        // Infer gaze shift and fixation timings and targets
        //var eyeGazeInstance = new EyeGazeInstance(model, ...);
        //AddEyeGaze(timeline, eyeGazeInstance, newStartFrame);
    }

    /// <summary>
    /// Infer eye gaze alignment parameters for currently defined
    /// eye gaze instances on the character model.
    /// </summary>
    /// <param name="timeline">Animation timeline</param>
    /// <param name="baseAnimationInstanceId">Base animation instance ID</param>
    /// <param name="layerName">Animation layer holding eye gaze animations</param>
    /// <param name="createTargets">If gaze target could not be found for a gaze shift,
    /// a "dummy" gaze target object will be created in the scene</param>
    public static void InferEyeGazeAlignments(AnimationTimeline timeline, int baseAnimationInstanceId, string layerName = "Gaze")
    {
        var baseAnimation = timeline.GetAnimation(baseAnimationInstanceId);

        // Infer head and torso alignments
        var gazeLayer = timeline.GetLayer(layerName);
        foreach (var instance in gazeLayer.Animations)
        {
            if (!(instance.Animation is EyeGazeInstance) ||
                !(instance.Animation as EyeGazeInstance).IsBase ||
                instance.Animation.Model != baseAnimation.Model)
            {
                continue;
            }

            _InferEyeGazeAlignments(timeline, baseAnimationInstanceId, instance.InstanceId);
        }
    }

    /// <summary>
    /// For all gaze shifts and fixations in the base animation, extract displacement maps
    /// containing expressive motion.
    /// </summary>
    /// <param name="timeline">Animation timeline</param>
    /// <param name="baseAnimationInstanceId">Base animation instance ID</param>
    /// <param name="layerName">Animation layer holding eye gaze animations</param>
    /// <param name="createTargets">If gaze target could not be found for a gaze shift,
    /// a "dummy" gaze target object will be created in the scene</param>
    public static void ExtractExpressiveEyeGazeAnimations(AnimationTimeline timeline, int baseAnimationInstanceId, string layerName = "Gaze")
    {
        // Get base animation
        var baseAnimation = timeline.GetAnimation(baseAnimationInstanceId);

        bool timelineActive = timeline.Active;
        timeline.Active = true;

        // Extract expressive motion
        var gazeLayer = timeline.GetLayer(layerName);
        foreach (var instance in gazeLayer.Animations)
        {
            if (!(instance.Animation is EyeGazeInstance) ||
                !(instance.Animation as EyeGazeInstance).IsBase ||
                instance.Animation.Model != baseAnimation.Model)
            {
                continue;
            }

            _ExtractExpressiveEyeGazeAnimations(timeline, baseAnimationInstanceId, instance.InstanceId);
        }

        timeline.Active = timelineActive;
    }

    /// <summary>
    /// Load eye gaze expressive gaze animation descriptions for the eye gaze instance of the specified base animation.
    /// </summary>
    /// <param name="timeline">Animation timeline</param>
    /// <param name="baseAnimationInstanceId">Base animation instance ID</param>
    /// <param name="layerName">Animation layer holding eye gaze animations</param>
    /// <returns>true if eye gaze expressive animation descriptions were loaded successfully, false otherwise</returns>
    public static bool LoadExpressiveEyeGazeAnimations(AnimationTimeline timeline, int baseAnimationInstanceId, string layerName = "Gaze")
    {
        // Get base animation
        var baseAnimation = timeline.GetAnimation(baseAnimationInstanceId);

        // Get gaze behavior file path
        string path = Application.dataPath + LEAPCore.eyeGazeDirectory.Substring(LEAPCore.eyeGazeDirectory.IndexOfAny(@"/\".ToCharArray()));
        if (path[path.Length - 1] != '/' && path[path.Length - 1] != '\\')
            path += '/';
        path += (baseAnimation.AnimationClip.name + "#Expressive.csv");

        try
        {
            var reader = new StreamReader(path);
            bool firstLine = true;
            string line = "";
            string[] lineElements = null;
            Dictionary<string, int> attributeIndices = new Dictionary<string, int>();
            Dictionary<EyeGazeInstance, List<ExpressiveEyeGazeAnimation>> expressiveGazeAnimations =
                new Dictionary<EyeGazeInstance,List<ExpressiveEyeGazeAnimation>>();

            // Get layer with gaze instances
            var gazeLayer = timeline.GetLayer(layerName);

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
                    // Load expressive gaze description
                    lineElements = line.Split(",".ToCharArray());

                    // Get expressive gaze animation attributes
                    string expressiveGazeAnimationName = lineElements[attributeIndices["ExpressiveGazeClip"]];
                    string gazeInstanceName = lineElements[attributeIndices["GazeInstance"]];
                    string gazeShiftRotationStr = lineElements[attributeIndices["GazeShiftRotation"]];
                    gazeShiftRotationStr = gazeShiftRotationStr.Substring(1, gazeShiftRotationStr.Length - 2);
                    var gazeShiftRotationStrElements = gazeShiftRotationStr.Split(" ".ToCharArray());
                    Vector3 gazeShiftRotation = new Vector3(float.Parse(gazeShiftRotationStrElements[0]),
                        float.Parse(gazeShiftRotationStrElements[1]),
                        float.Parse(gazeShiftRotationStrElements[2]));

                    // Get eye gaze instance
                    var instance = gazeLayer.Animations.FirstOrDefault(inst => inst.Animation is EyeGazeInstance &&
                        inst.Animation.AnimationClip.name == gazeInstanceName);
                    if (instance == null)
                    {
                        UnityEngine.Debug.LogError(string.Format(
                            "Unable to load expressive gaze animation description for non-existent gaze instance {0}", gazeInstanceName));
                        continue;
                    }
                    var gazeInstance = instance.Animation as EyeGazeInstance;

                    // Get expressive gaze animation clip
                    var exprGazeAnimClip = gazeInstance.Animation.GetClip(expressiveGazeAnimationName);
                    if (exprGazeAnimClip == null)
                    {
                        UnityEngine.Debug.LogError(string.Format(
                            "Unable to load expressive gaze animation description because animation clip {0} does not exist", expressiveGazeAnimationName));
                        continue;
                    }

                    // Create expressive gaze animation description
                    ExpressiveEyeGazeAnimation exprGazeAnimDesc = new ExpressiveEyeGazeAnimation(exprGazeAnimClip, gazeShiftRotation);
                    if (!expressiveGazeAnimations.ContainsKey(gazeInstance))
                    {
                        expressiveGazeAnimations[gazeInstance] = new List<ExpressiveEyeGazeAnimation>();
                    }
                    expressiveGazeAnimations[gazeInstance].Add(exprGazeAnimDesc);
                }
            }

            reader.Close();

            // Set expressive gaze animations on their respective gaze instances
            foreach (var kvp in expressiveGazeAnimations)
            {
                kvp.Key.SetExpressiveGazeAnimations(kvp.Value.ToArray());
            }
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError(string.Format("Unable to load eye gaze from asset file {0}: {1}", path, ex.Message));
            return false;
        }

        return true;
    }

    /// <summary>
    /// For all eye gaze instances for the specified base animation, save descriptions of their expressive
    /// animation components to a file.
    /// </summary>
    /// <param name="timeline">Animation timeline</param>
    /// <param name="baseAnimationInstanceId">Base animation instance ID</param>
    /// <param name="layerName">Animation layer holding eye gaze animations</param>
    /// <returns>true if eye gaze expressive animation descriptions were saved successfully, false otherwise</returns>
    public static bool SaveExpressiveEyeGazeAnimations(AnimationTimeline timeline, int baseAnimationInstanceId, string layerName = "Gaze")
    {
        // Get base animation
        var baseAnimation = timeline.GetAnimation(baseAnimationInstanceId);

         // Get gaze behavior file path
        string path = Application.dataPath + LEAPCore.eyeGazeDirectory.Substring(LEAPCore.eyeGazeDirectory.IndexOfAny(@"/\".ToCharArray()));
        if (path[path.Length - 1] != '/' && path[path.Length - 1] != '\\')
            path += '/';
        path += (baseAnimation.AnimationClip.name + "#Expressive.csv");

        try
        {
            var writer = new StreamWriter(path);
            writer.WriteLine("ExpressiveGazeClip,GazeInstance,GazeShiftRotation");

            // Save expressive gaze descriptions
            var gazeLayer = timeline.GetLayer(layerName);
            foreach (var instance in gazeLayer.Animations)
            {
                if (!(instance.Animation is EyeGazeInstance) ||
                    instance.Animation.Model != baseAnimation.Model)
                {
                    continue;
                }

                var gazeInstance = instance.Animation as EyeGazeInstance;

                // Write out the gaze instances
                var lineBuilder = new StringBuilder();
                foreach (ExpressiveEyeGazeAnimation exprGazeAnim in gazeInstance.ExpressiveGazeAnimations)
                {
                    lineBuilder.Append(exprGazeAnim.clip.name);
                    lineBuilder.Append(",");
                    lineBuilder.Append(gazeInstance.AnimationClip.name);
                    lineBuilder.Append(",");
                    lineBuilder.Append(string.Format("\"{0} {1} {2}\"",
                        exprGazeAnim.gazeShiftRotation.x, exprGazeAnim.gazeShiftRotation.y, exprGazeAnim.gazeShiftRotation.z));

                    writer.WriteLine(lineBuilder);
                    lineBuilder.Length = 0;
                }
            }

            writer.Close();
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError(string.Format("Unable to save eye gaze expressive animation descriptions to asset file {0}: {1}", path, ex.Message));
            return false;
        }

        return true;
    }

    /// <summary>
    /// Get initial state of the eye gaze controller at the start of the specified gaze shift.
    /// </summary>
    /// <param name="instance">Eye gaze instance</param>
    /// <returns>Gaze controller state</returns>
    public static GazeControllerState GetInitControllerForEyeGazeInstance(EyeGazeInstance instance)
    {
        IAnimControllerState curState = instance.Controller.GetRuntimeState();

        // Initialize gaze controller
        var gazeController = instance.GazeController;
        gazeController.useTorso = instance.TurnBody;
        if (instance.Target != null)
            gazeController.GazeAt(instance.Target);
        else
            gazeController.GazeAt(instance.AheadTargetPosition);
        gazeController.Head.align = instance.HeadAlign >= 0f ? instance.HeadAlign : 0f;
        if (gazeController.Torso != null)
            gazeController.Torso.align = instance.TorsoAlign >= 0f ? instance.HeadAlign : 0f;
        gazeController._InitGazeParams();
        gazeController._InitTargetRotations();
        gazeController._InitLatencies();
        gazeController._CalculateMaxVelocities();

        GazeControllerState state = (GazeControllerState)instance.Controller.GetRuntimeState();
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

            GazeControllerState initState = gazeController.GetInitRuntimeState();
            gazeController.SetRuntimeState(initState);
        }
    }

    /// <summary>
    /// Estimate gaze shift duration (from gaze shift start to fixation start) of
    /// the specified eye gaze instance.
    /// </summary>
    /// <param name="instance">Eye gaze instance</param>
    /// <param name="state">Eye gaze controller state at the start of the gaze shift</param>
    /// <returns>Gaze shift duration in seconds</returns>
    public static float ComputeEstGazeShiftTimeLength(EyeGazeInstance instance, GazeControllerState state)
    {
        IAnimControllerState curState = instance.Controller.GetRuntimeState();
        instance.Controller.SetRuntimeState(state);
        var gazeController = instance.GazeController;

        // Estimate eye rotation time
        float eyeRotTime = 0f;
        foreach (var eye in gazeController.eyes)
        {
            float edr = GazeJoint.DistanceToRotate(eye.bone.localRotation, eye.trgRotMR);
            float eyeVelocity = 0.75f * eye.maxVelocity;
            eyeRotTime = Mathf.Max(eyeRotTime, eyeVelocity > 0f ? edr / eye.velocity : 0f);
        }

        // Estimate head rotation time
        float hdr = GazeJoint.DistanceToRotate(gazeController.Head.bone.localRotation, gazeController.Head.trgRotAlign);
        float headVelocity = 0.625f * gazeController.Head.maxVelocity;
        float headRotTime = headVelocity > 0f ? hdr / headVelocity : 0f;

        float torsoRotTime = 0f;
        if (gazeController.Torso != null)
        {
            // Estimate torso rotation time

            float tdr = GazeJoint.DistanceToRotate(gazeController.Torso.srcRot, gazeController.Torso.trgRotAlign);
            float torsoVelocity = 0.625f * gazeController.Torso.maxVelocity;
            torsoRotTime = torsoVelocity > 0f ? tdr / torsoVelocity : 0f;
        }

        instance.Controller.SetRuntimeState(curState);

        float totalTime = Mathf.Max(eyeRotTime, headRotTime, torsoRotTime);
        // TODO: this is a quick hack to fix the problem of underestimating gaze shift duration
        return 2f * totalTime;
        //return totalTime;
    }

    /// <summary>
    /// Compute difference in effective gaze target position due to moving base
    /// for the specified gaze shift.
    /// </summary>
    /// <param name="timeline">Animation timeline</param>
    /// <param name="baseAnimationInstanceId">Base animation instance ID</param>
    /// <param name="eyeGazeInstance">Eye gaze instance<param>
    /// <param name="eyeGazeStartFrame">Eye gaze start frame</param>
    public static Vector3 ComputeMovingTargetPositionOffset(AnimationTimeline timeline, int baseAnimationInstanceId,
        EyeGazeInstance eyeGazeInstance, int eyeGazeStartFrame)
    {
        if (!LEAPCore.adjustGazeTargetForMovingBase)
            return Vector3.zero;

        var baseAnimationInstance = timeline.GetAnimation(baseAnimationInstanceId);
        var model = eyeGazeInstance.Model;
        var clip = eyeGazeInstance.AnimationClip;
        var gazeController = eyeGazeInstance.GazeController;

        // Estimate gaze shift duration
        float estGazeShiftTimeLength = ComputeEstGazeShiftTimeLength(eyeGazeInstance,
            GetInitControllerForEyeGazeInstance(eyeGazeInstance));

        // Store current model pose
        timeline.StoreModelPose(model.name, clip.name + "Pose");

        // Get base position at the current frame
        baseAnimationInstance.Apply(eyeGazeStartFrame, AnimationLayerMode.Override);
        Vector3 currentBasePos = gazeController.gazeJoints[gazeController.LastGazeJointIndex].bone.position;

        // Apply the base animation at a time in near future
        int eyeGazeFixationStartFrame = eyeGazeStartFrame +
            Mathf.RoundToInt(((float)LEAPCore.editFrameRate) * estGazeShiftTimeLength); // look ahead to the estimated end of the current gaze shift
        baseAnimationInstance.Apply(eyeGazeFixationStartFrame, AnimationLayerMode.Override);

        // Get future base position at the current frame
        Vector3 aheadBasePos = gazeController.gazeJoints[gazeController.LastGazeJointIndex].bone.position;

        // Reapply current model pose
        //baseAnimationInstance.Animation.Apply(AnimationTimeline.Instance.CurrentFrame - baseAnimationInstance.StartFrame, AnimationLayerMode.Override);
        AnimationTimeline.Instance.ApplyModelPose(model.name, clip.name + "Pose");
        AnimationTimeline.Instance.RemoveModelPose(model.name, clip.name + "Pose");

        // Set gaze target position offset
        return currentBasePos - aheadBasePos;
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

        UnityEngine.Debug.Log(string.Format("GazeController: state = {0}, gazeTarget = {1}, doGazeShift = {2}, stopGazeShift = {3}, " +
            "fixGaze = {4}, useTorso = {5},  currentGazeTarget = {6}, fixGazeTarget = {7}, weight = {8}, fixWeight = {9}",
            gazeController.State, gazeController.gazeTarget != null ? gazeController.gazeTarget.name : "null",
            gazeController.doGazeShift, gazeController.stopGazeShift, gazeController.fixGaze, gazeController.useTorso,
            gazeController.CurrentGazeTarget != null ? gazeController.CurrentGazeTarget.name : "null",
            gazeController.FixGazeTarget != null ? gazeController.FixGazeTarget.name : "null",
            gazeController.weight, gazeController.fixWeight));

        for (int gazeJointIndex = 0; gazeJointIndex < gazeController.gazeJoints.Length; ++gazeJointIndex)
        {
            var gazeJoint = gazeController.gazeJoints[gazeJointIndex];
            var last = gazeController.GetLastGazeJointInChain(gazeJoint.type);
            int lastIndex = gazeController.FindGazeJointIndex(last);

            if (gazeJoint == last || LEAPCore.printDetailedGazeControllerState)
                _PrintEyeGazeJointState(gazeJoint, gazeJointIndex - lastIndex);
        }
    }

    // Gaze editing helper operation; remove an eye gaze instance from the animation timeline
    private static void _RemoveEyeGaze(AnimationTimeline timeline, int instanceId)
    {
        // First remove the follow-up gaze-ahead instance, if one exists
        string layerName = timeline.GetLayerForAnimation(instanceId).LayerName;
        var instance = timeline.GetAnimation(instanceId) as EyeGazeInstance;
        var gazeAheadInstance = timeline.GetLayer(layerName).Animations.FirstOrDefault(inst =>
            inst.Animation.AnimationClip.name == instance.AnimationClip.name + LEAPCore.gazeAheadSuffix);
        if (gazeAheadInstance != null)
            timeline.RemoveAnimation(gazeAheadInstance.InstanceId);

        // Then remove the actual instnace
        timeline.RemoveAnimation(instanceId);
    }

    // Infer eye gaze alignment values for the specified gaze instance
    private static void _InferEyeGazeAlignments(AnimationTimeline timeline, int baseAnimationInstanceId, int instanceId)
    {
        var baseAnimation = timeline.GetAnimation(baseAnimationInstanceId);
        var gazeInstance = timeline.GetAnimation(instanceId) as EyeGazeInstance;
        int startFrame = timeline.GetAnimationStartFrame(instanceId);
        var gazeController = gazeInstance.GazeController;
        
        timeline.ResetModelsAndEnvironment();

        // Disable stylized gaze, enable/disable torso
        gazeController.stylizeGaze = false;
        gazeController.useTorso = gazeInstance.TurnBody;

        // TODO: we really, really need accurate estimation of gaze shift timings - this is just a dumb hack
        int fixationStartFrame = startFrame + gazeInstance.FixationStartFrame;
        if (gazeInstance.FixationStartFrame < 0)
        {
            // Estimate gaze shift duration
            baseAnimation.Apply(startFrame, AnimationLayerMode.Override);
            gazeInstance.HeadAlign = 1f;
            gazeInstance.TorsoAlign = 1f;
            GazeControllerState gazeControllerState = EyeGazeEditor.GetInitControllerForEyeGazeInstance(gazeInstance);
            float estTimeLength = EyeGazeEditor.ComputeEstGazeShiftTimeLength(gazeInstance, gazeControllerState);
            fixationStartFrame = startFrame +
                Mathf.Min(gazeInstance.FrameLength - 1, Mathf.RoundToInt(((float)LEAPCore.editFrameRate) * estTimeLength));
            gazeInstance.SetFixationStartFrame(fixationStartFrame - startFrame);
        }
        //

        if (gazeInstance.Target == null)
        {
            // Determine position of the gaze target for gazing ahead
            baseAnimation.Apply(fixationStartFrame, AnimationLayerMode.Override);
            Vector3 aheadTargetPos = gazeController.Head.bone.position + 5f * gazeController.Head.Direction;
            gazeInstance.AheadTargetPosition = aheadTargetPos;
        }

        // Compute gaze target position offset due to moving base
        gazeController.movingTargetPositionOffset =
            ComputeMovingTargetPositionOffset(timeline, baseAnimationInstanceId, gazeInstance, startFrame);

        // Compute initial state of the gaze controller at the start of the current gaze instance
        baseAnimation.Apply(startFrame, AnimationLayerMode.Override);
        gazeInstance.HeadAlign = 0f;
        gazeInstance.TorsoAlign = 0f;
        GazeControllerState gazeControllerStateStart = EyeGazeEditor.GetInitControllerForEyeGazeInstance(gazeInstance);
        gazeController.SetRuntimeState(gazeControllerStateStart);

        // Key gaze joint rotations in the gaze instance
        Quaternion srcRotHead, trgRotHead, trgRotMinHead, trgRotAlignHead,
            srcRotTorso, trgRotTorso, trgRotMinTorso, trgRotAlignTorso;
        trgRotMinTorso = Quaternion.identity;

        // Apply animation at the end of the gaze shift
        //baseAnimation.Apply(fixationStartFrame, AnimationLayerMode.Override);

        // Get source gaze joint rotations
        srcRotHead = gazeController.Head.srcRot;
        srcRotTorso = gazeInstance.TurnBody ? gazeController.Torso.srcRot : Quaternion.identity;

        // Compute full target rotations of the head and torso
        /*gazeController.ApplySourcePose();
        trgRotHead = gazeController.Head._ComputeTargetRotation(gazeController.gazeTarget.transform.position);
        trgRotTorso = gazeController.Torso != null ?
            gazeController.Torso._ComputeTargetRotation(gazeController.gazeTarget.transform.position) : Quaternion.identity;*/
        trgRotHead = gazeController.Head.trgRot;
        trgRotTorso = gazeInstance.TurnBody ?
            gazeController.Torso.trgRot : Quaternion.identity;

        if (gazeInstance.TurnBody)
        {
            // Compute min. target rotation of the torso
            float distRotMinTorso = gazeInstance.GazeController._ComputeMinTorsoDistanceToRotate();
            float distRotTorso = GazeJoint.DistanceToRotate(srcRotTorso, trgRotTorso);
            float alignMinTorso = srcRotTorso != trgRotTorso ? distRotMinTorso / distRotTorso : 0f;
            trgRotMinTorso = Quaternion.Slerp(srcRotTorso, trgRotTorso, alignMinTorso);
        }

        // Apply animation at the end of the gaze shift
        baseAnimation.Apply(fixationStartFrame, AnimationLayerMode.Override);

        // Compute torso alignment
        if (gazeInstance.TurnBody)
        {
            // Compute torso target rotation at the end of the gaze shift
            Quaternion trgRotTorso1 = gazeController.Torso._ComputeTargetRotation(gazeController.EffGazeTargetPosition);

            if (srcRotTorso == trgRotTorso1)
            {
                trgRotAlignTorso = srcRotTorso;
                gazeInstance.TorsoAlign = 0f;
            }
            else
            {
                Quaternion curRot = gazeController.Torso.bone.localRotation;

                // Compute aligning target rotation for the torso
                trgRotAlignTorso = ModelUtils.ProjectRotationOntoArc(gazeController.Torso.bone,
                    srcRotTorso, trgRotTorso1, curRot);
                float r = _ComputeGazeJointAlignment(gazeController.Torso.bone, srcRotTorso, trgRotTorso1, srcRotTorso, trgRotAlignTorso);
                trgRotAlignTorso = Quaternion.Slerp(srcRotTorso, trgRotTorso, r);

                // Compute torso alignment
                gazeInstance.TorsoAlign = _ComputeGazeJointAlignment(gazeController.Torso.bone,
                    srcRotTorso, trgRotTorso, trgRotMinTorso, trgRotAlignTorso);
            }

            gazeController.Torso.align = gazeController.Torso.curAlign = gazeInstance.TorsoAlign;
        }

        // Apply animation at the start of the gaze shift
        baseAnimation.Apply(startFrame, AnimationLayerMode.Override);

        // Compute minimal head target rotation
        gazeController.Head.align = gazeController.Head.curAlign = 0f;
        gazeController._InitTargetRotations();
        trgRotMinHead = gazeController.Head.trgRotAlign;

        // Apply animation at the end of the gaze shift
        baseAnimation.Apply(fixationStartFrame, AnimationLayerMode.Override);

        // Compute head target rotation at the end of the gaze shift
        Quaternion trgRotHead1 = gazeController.Head._ComputeTargetRotation(gazeController.EffGazeTargetPosition);

        // Compute head alignment
        if (srcRotHead == trgRotHead1)
        {
            trgRotAlignHead = srcRotHead;
            gazeInstance.HeadAlign = 0f;
        }
        else
        {
            Quaternion curRot = gazeController.Head.bone.localRotation;

            // Get aligning target rotation for the head
            trgRotAlignHead = ModelUtils.ProjectRotationOntoArc(gazeController.Head.bone,
                    srcRotHead, trgRotHead1, curRot);
            float r = _ComputeGazeJointAlignment(gazeController.Head.bone, srcRotHead, trgRotHead1, srcRotHead, trgRotAlignHead);
            trgRotAlignHead = Quaternion.Slerp(srcRotHead, trgRotHead, r);

            // Compute head alignment
            gazeInstance.HeadAlign = _ComputeGazeJointAlignment(gazeController.Head.bone,
                srcRotHead, trgRotHead, trgRotMinHead, trgRotAlignHead);
        }
    }

    /// <summary>
    /// Compute alignment parameter value for the specified gaze joint during gaze shift
    /// with given source and target rotations.
    /// </summary>
    /// <param name="gazeJointBone">Gaze joint bone</param>
    /// <param name="srcRot">Source rotation</param>
    /// <param name="trgRot">Target rotation</param>
    /// <param name="trgRotMin">Minimally aligning target rotation</param>
    /// <param name="trgRotAlign">Aligning target rotation</param>
    /// <returns>Alignment</returns>
    private static float _ComputeGazeJointAlignment(Transform bone,
        Quaternion srcRot, Quaternion trgRot, Quaternion trgRotMin, Quaternion trgRotAlign)
    {
        if (srcRot == trgRot)
            return 0f;

        float align = 0f;
        Quaternion curRot = bone.localRotation;

        // Get gaze directions for each rotation
        bone.localRotation = srcRot;
        Vector3 srcDir = bone.forward;
        bone.localRotation = trgRot;
        Vector3 trgDir = bone.forward;
        bone.localRotation = trgRotAlign;
        Vector3 trgDirAlign = bone.forward;
        
        // Rotational plane normal
        Vector3 n = Vector3.Cross(srcDir, trgDir);

        if (srcDir == trgDirAlign)
        {
            align = 0f;
        }
        else if (srcDir == -trgDirAlign)
        {
            align = 1f;
        }
        else
        {
            float sa = Mathf.Sign(Vector3.Dot(Vector3.Cross(srcDir, trgDirAlign), n));

            if (sa > 0f)
            {
                align = trgRotMin != trgRot ?
                    GazeJoint.DistanceToRotate(trgRotMin, trgRotAlign) /
                    GazeJoint.DistanceToRotate(trgRotMin, trgRot) : 0f;
                align = Mathf.Clamp01(align);
            }
            else
            {
                align = 0f;
            }
        }

        bone.localRotation = curRot;

        return align;
    }

    // Extract expressive motion as a displacement map from the gaze shift and fixation in the base animation
    private static void _ExtractExpressiveEyeGazeAnimations(AnimationTimeline timeline, int baseAnimationInstanceId, int eyeGazeInstanceId)
    {
        // Base animation instance
        var baseAnimationInstance = timeline.GetAnimation(baseAnimationInstanceId);

        // Eye gaze instance
        var eyeGazeInstance = timeline.GetAnimation(eyeGazeInstanceId) as EyeGazeInstance;
        int eyeGazeStartFrame = timeline.GetAnimationStartFrame(eyeGazeInstanceId);
        int eyeGazeFixationStartFrame = eyeGazeStartFrame + eyeGazeInstance.FixationStartFrame;
        int eyeGazeEndFrame = eyeGazeStartFrame + eyeGazeInstance.FrameLength - 1;

        // Character model
        var gazeController = eyeGazeInstance.GazeController;
        var model = eyeGazeInstance.Model;
        Transform[] bones = ModelUtils.GetAllBones(model);

        // Gaze joint rotations at key points of the gaze instance
        Dictionary<Transform, Quaternion> startRots = new Dictionary<Transform, Quaternion>();
        Dictionary<Transform, Quaternion> fixationStartRots = new Dictionary<Transform, Quaternion>();

        // Expressive eye gaze
        string expressiveEyeGazeName = eyeGazeInstance.AnimationClip.name + LEAPCore.expressiveGazeSuffix;
        var expressiveEyeGazeClips = new Dictionary<Transform, AnimationClip>();
        var expressiveEyeGazeCurves = new Dictionary<Transform, AnimationCurve[]>();
        var expressiveEyeGazeRotations = new Dictionary<Transform, Vector3>();

        timeline.ResetModelsAndEnvironment();

        // Create expressive displacement animation clips on the model for this gaze instance
        for (int gazeJointIndex = gazeController.eyes.Length; gazeJointIndex < gazeController.gazeJoints.Length; ++gazeJointIndex)
        {
            var gazeJoint = gazeController.gazeJoints[gazeJointIndex];
            expressiveEyeGazeClips[gazeJoint.bone] = LEAPAssetUtils.CreateAnimationClipOnModel(
                expressiveEyeGazeName + (gazeJointIndex - gazeController.eyes.Length).ToString(), model);
            expressiveEyeGazeCurves[gazeJoint.bone] = LEAPAssetUtils.CreateAnimationCurvesForModel(model);
        }

        // Get gaze joint orientations at the the start of the gaze shift
        baseAnimationInstance.Apply(eyeGazeStartFrame, AnimationLayerMode.Override);
        for (int gazeJointIndex = gazeController.eyes.Length; gazeJointIndex < gazeController.gazeJoints.Length; ++gazeJointIndex)
        {
            var bone = gazeController.gazeJoints[gazeJointIndex].bone;
            startRots[bone] = bone.localRotation;
        }

        // Get gaze joint orientations at the the start of the gaze fixation
        baseAnimationInstance.Apply(eyeGazeFixationStartFrame, AnimationLayerMode.Override);
        for (int gazeJointIndex = gazeController.eyes.Length; gazeJointIndex < gazeController.gazeJoints.Length; ++gazeJointIndex)
        {
            var gazeJoint = gazeController.gazeJoints[gazeJointIndex];

            // Compute gaze directions for the current joint in the base motion and
            // along the shortest-arc path to the target
            Quaternion baseTrgRot = gazeJoint.bone.localRotation;
            Vector3 vfb = gazeJoint.Direction;
            Quaternion trgRot = gazeJoint._ComputeTargetRotation(eyeGazeInstance.Target.transform.position);
            gazeJoint.bone.localRotation = trgRot;
            Vector3 vt = gazeJoint.Direction;
            Quaternion qs = startRots[gazeJoint.bone];
            gazeJoint.bone.localRotation = qs;
            Vector3 vs = gazeJoint.Direction;
            gazeJoint.bone.localRotation = baseTrgRot;

            // Project the joint's direction vector onto the gaze shift rotational plane
            // to obtain the target rotation along the shortest-arc path to the target
            Vector3 n = !GeomUtil.Equal(vs, vt) ? Vector3.Cross(vs, vt) : Vector3.Cross(vs, -gazeJoint.bone.right);
            Vector3 vf = Mathf.Abs(Vector3.Dot(vfb, n)) <= 0.995 ? GeomUtil.ProjectVectorOntoPlane(vfb, n) : vs; // TODO: this could lead to errors and discontinuities
            vf.Normalize();
            Quaternion dqf = Quaternion.FromToRotation(vs, vf);
            Quaternion qf = startRots[gazeJoint.bone] * dqf;
            fixationStartRots[gazeJoint.bone] = qf;

            // Store shortest-arc gaze shift rotation in the base motion
            expressiveEyeGazeRotations[gazeJoint.bone] = QuaternionUtil.Log(Quaternion.Inverse(qs) * qf);
        }

        // Extract expressive displacement animation curve at each frame, for each gaze joint
        var baseFixationStartRots = new Dictionary<Transform, Quaternion>();
        var fixationStartExprRots = new Dictionary<Transform, Quaternion>();
        for (int frame = eyeGazeStartFrame; frame <= eyeGazeEndFrame; ++frame)
        {
            baseAnimationInstance.Apply(frame, AnimationLayerMode.Override);

            for (int boneIndex = 0; boneIndex < bones.Length; ++boneIndex)
            {
                var bone = bones[boneIndex];
                if (bone.tag != "HeadBone" && bone.tag != "TorsoBone")
                    continue;

                // Compute key time and rotational difference between current joint rotation and the shortest arc
                // between gaze start and end rotations
                float time = 0f;
                Quaternion q = bone.localRotation;
                Quaternion qf = fixationStartRots[bone];
                Quaternion dq = Quaternion.identity;
                if (frame <= eyeGazeFixationStartFrame)
                {
                    // Computing rotational difference for the gaze shift

                    Vector3 v = bone.forward;
                    Quaternion qs = startRots[bone];
                    bone.localRotation = qs;
                    Vector3 vs = bone.forward;
                    bone.localRotation = qf;
                    Vector3 vf = bone.forward;
                    bone.localRotation = q;

                    // Project the joint's direction vector onto the gaze shift rotational plane
                    // to obtain the expressive gaze rotation
                    Vector3 n = !GeomUtil.Equal(vs, vf) ? Vector3.Cross(vs, vf) : Vector3.Cross(vs, -bone.right);
                    Vector3 vp = Mathf.Abs(Vector3.Dot(v, n)) <= 0.995 ? GeomUtil.ProjectVectorOntoPlane(v, n) : vs; // TODO: this could lead to errors and discontinuities
                    vp.Normalize();
                    dq = Quaternion.FromToRotation(vp, v);

                    if (frame == eyeGazeFixationStartFrame - 1)
                    {
                        baseFixationStartRots[bone] = q;
                        fixationStartExprRots[bone] = dq;
                    }
                }
                else
                {
                    // Computing rotational difference for the gaze fixation
                    Quaternion qfb = baseFixationStartRots.ContainsKey(bone) ? baseFixationStartRots[bone] : startRots[bone];
                    Quaternion dqef = fixationStartExprRots.ContainsKey(bone) ? fixationStartExprRots[bone] : Quaternion.identity;
                    dq = dqef * (Quaternion.Inverse(qfb) * q);
                }

                // Compute expressive animation timing
                time = ((float)(frame - eyeGazeStartFrame)) / LEAPCore.editFrameRate;

                //
                /*if (bone.name.EndsWith("Head"))
                {
                    UnityEngine.Debug.LogWarning(string.Format("{0}{1}: {2}, t = {3}, disp = ({4}, {5}, {6})",
                        frame <= eyeGazeFixationStartFrame ? "s" : "f", frame, bone.name,
                        time,
                        dq.eulerAngles.x > 180f ? dq.eulerAngles.x - 360f : dq.eulerAngles.x,
                        dq.eulerAngles.y > 180f ? dq.eulerAngles.y - 360f : dq.eulerAngles.y,
                        dq.eulerAngles.z > 180f ? dq.eulerAngles.z - 360f : dq.eulerAngles.z));
                }*/
                //

                // Keyframe the expressive rotation
                var rotationKeyFrame = new Keyframe();
                rotationKeyFrame.time = time;
                rotationKeyFrame.value = dq.x;
                expressiveEyeGazeCurves[bone][3 + boneIndex * 4].AddKey(rotationKeyFrame);
                rotationKeyFrame.value = dq.y;
                expressiveEyeGazeCurves[bone][3 + boneIndex * 4 + 1].AddKey(rotationKeyFrame);
                rotationKeyFrame.value = dq.z;
                expressiveEyeGazeCurves[bone][3 + boneIndex * 4 + 2].AddKey(rotationKeyFrame);
                rotationKeyFrame.value = dq.w;
                expressiveEyeGazeCurves[bone][3 + boneIndex * 4 + 3].AddKey(rotationKeyFrame);
            }
        }

        for (int gazeJointIndex = gazeController.eyes.Length; gazeJointIndex < gazeController.gazeJoints.Length; ++gazeJointIndex)
        {
            // Set the curves on the expressive eye gaze clip
            var gazeJoint = gazeController.gazeJoints[gazeJointIndex];
            expressiveEyeGazeClips[gazeJoint.bone].ClearCurves();
            LEAPAssetUtils.SetAnimationCurvesOnClip(model, expressiveEyeGazeClips[gazeJoint.bone], expressiveEyeGazeCurves[gazeJoint.bone]);
        }

        // Write the expressive eye gaze clips to file
        foreach (var kvp in expressiveEyeGazeClips)
        {
            var expressiveEyeGazeClip = kvp.Value;
            string path = LEAPAssetUtils.GetModelDirectory(model.gameObject) + expressiveEyeGazeClip.name + ".anim";
            if (AssetDatabase.GetAssetPath(expressiveEyeGazeClip) != path)
            {
                AssetDatabase.DeleteAsset(path);
                AssetDatabase.CreateAsset(expressiveEyeGazeClip, path);
            }
        }
        AssetDatabase.SaveAssets();

        // Set the expressive eye gaze clips on the gaze instance
        ExpressiveEyeGazeAnimation[] exprGazeAnimations = new ExpressiveEyeGazeAnimation[gazeController.gazeJoints.Length - gazeController.eyes.Length];
        for (int gazeJointIndex = gazeController.eyes.Length; gazeJointIndex < gazeController.gazeJoints.Length; ++gazeJointIndex)
        {
            // Set the curves on the expressive eye gaze clip
            var gazeJoint = gazeController.gazeJoints[gazeJointIndex];
            exprGazeAnimations[gazeJointIndex - gazeController.eyes.Length] = new ExpressiveEyeGazeAnimation(
                expressiveEyeGazeClips[gazeJoint.bone],
                expressiveEyeGazeRotations[gazeJoint.bone]
                );
        }
        eyeGazeInstance.SetExpressiveGazeAnimations(exprGazeAnimations);
    }

    // Print individual gaze joint state
    private static void _PrintEyeGazeJointState(GazeJoint joint, int index = 0)
    {
        string jointName = joint.type + (index > 0 ? "-" + index.ToString() : "");

        UnityEngine.Debug.Log(string.Format("{0}: state = {1}, rotation = ({2}, {3}, {4}), " +
            "curVelocity = {5} [maxVelocity = {6}], latencyTime = {7}, cur*OMR = ({8}, {9}, {10}, {11}), curAlign = {12}, " +
            "srcRot = ({13}, {14}, {15}), trgRot = ({16}, {17}, {18}), trgRotAlign = ({19}, {20}, {21}), trgRotMR = ({22}, {23}, {24}), " +
            "distRotAlign = {25}, distRotMR = {26}, rotParamAlign = {27}, rotParamMR = {28}, mrReached = {29}, trgReached = {30}, isVOR = {31}, " +
            "fixSrcRot = ({32}, {33}, {34}), fixTrgRot = ({35}, {36}, {37}), fixTrgRotAlign = ({38}, {39}, {40}), fixRotParamAlign = {41}, " +
            "baseRot = ({42}, {43}, {44}), " +
            "expressiveRot = ({45}, {46}, {47}), fixExpressiveRot = ({48}, {49}, {50})",
            jointName, joint.GazeController.State,
            joint.bone.localRotation.eulerAngles.x, joint.bone.localRotation.eulerAngles.y, joint.bone.localRotation.eulerAngles.z,
            joint.curVelocity, joint.maxVelocity, joint.latencyTime, joint.curUpMR, joint.curDownMR, joint.curInMR, joint.curOutMR,
            joint.curAlign, joint.srcRot.eulerAngles.x, joint.srcRot.eulerAngles.y, joint.srcRot.eulerAngles.z,
            joint.trgRot.eulerAngles.x, joint.trgRot.eulerAngles.y, joint.trgRot.eulerAngles.z,
            joint.trgRotAlign.eulerAngles.x, joint.trgRotAlign.eulerAngles.y, joint.trgRotAlign.eulerAngles.z,
            joint.trgRotMR.eulerAngles.x, joint.trgRotMR.eulerAngles.y, joint.trgRotMR.eulerAngles.z,
            joint.distRotAlign, joint.distRotMR, joint.rotParamAlign, joint.rotParamMR, joint.mrReached, joint.trgReached, joint.isVOR,
            joint.fixSrcRot.eulerAngles.x, joint.fixSrcRot.eulerAngles.y, joint.fixSrcRot.eulerAngles.z,
            joint.fixTrgRot.eulerAngles.x, joint.fixTrgRot.eulerAngles.y, joint.fixTrgRot.eulerAngles.z,
            joint.fixTrgRotAlign.eulerAngles.x, joint.fixTrgRotAlign.eulerAngles.y, joint.fixTrgRotAlign.eulerAngles.z,
            joint.fixRotParamAlign, joint.baseRot.eulerAngles.x, joint.baseRot.eulerAngles.y, joint.baseRot.eulerAngles.z,
            joint.expressiveRot.eulerAngles.x, joint.expressiveRot.eulerAngles.y, joint.expressiveRot.eulerAngles.z,
            joint.fixExpressiveRot.eulerAngles.x, joint.fixExpressiveRot.eulerAngles.y, joint.fixExpressiveRot.eulerAngles.z));
    }
}

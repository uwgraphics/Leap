using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

/// <summary>
/// Type of eye gaze editing operation.
/// </summary>
public enum EyeGazeEditType
{
    Add,
    Remove,
    SetTiming,
    SetTarget,
    SetAlignments
}

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
            bool overlappingIsGazeBack = overlappingInstance.Animation.AnimationClip.name.EndsWith(LEAPCore.gazeBackSuffix);

            // Compute minimal length of the overlapping instance
            int overlappingMinLength = -1;
            /*if (overlappingGazeInstance.FixationStartFrame >= 0)
                overlappingMinLength = overlappingGazeInstance.FixationStartFrame;
            else */if (overlappingIsGazeBack)
                overlappingMinLength = maxEyeGazeGapLength;
            else
                overlappingMinLength = minEyeGazeLength;

            if (overlappingStartFrame < newStartFrame)
            {
                // Overlapping instance starts before the new instance

                if (newStartFrame - overlappingStartFrame >= overlappingMinLength)
                {
                    // Overlapping instance reaches fixation before the start of the new instance,
                    // so we can just trim the fixation phase

                    overlappingGazeInstance.SetFrameLength(newStartFrame - overlappingStartFrame);

                    if (!overlappingIsGazeBack)
                    {
                        // If overlapping instance is followed by a gaze-back instance,
                        // the gaze-back instance must be removed

                        var gazeBackOverlappingInstance = timeline.GetLayer(layerName).Animations.FirstOrDefault(
                            inst => inst.Animation.AnimationClip.name == overlappingInstance.Animation.AnimationClip.name + LEAPCore.gazeBackSuffix);
                        if (gazeBackOverlappingInstance != null)
                            timeline.RemoveAnimation(gazeBackOverlappingInstance.InstanceId);
                    }
                }
                else
                {
                    // Overlapping instance does not reach fixation before the start of the new instance,
                    // so it must be removed altogether

                    if (!overlappingIsGazeBack)
                    {
                        // Overlapping instance isn't a gaze-back instance, so we remove it

                        _RemoveEyeGaze(timeline, overlappingInstance.InstanceId);
                    }
                    else
                    {
                        // Overlapping instance is the gaze-back instance of a preceding instance;
                        // we must remove the overlapping instance, but also extend the preceding instance
                        // so that its end lines up with the start of the new instance

                        string prevOverlappingName = overlappingInstance.Animation.AnimationClip.name;
                        prevOverlappingName = prevOverlappingName.Remove(
                            prevOverlappingName.Length - LEAPCore.gazeBackSuffix.Length, LEAPCore.gazeBackSuffix.Length);
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

                if (overlappingEndFrame - newEndFrame >= overlappingMinLength)
                {
                    // We can delay the start of the overlapping instance after the end of the new instance,
                    // and there will still be time for gaze to reach the target in the overlapping instance

                    if (!overlappingIsGazeBack)
                    {
                        // Delay the start of the overlapping instance

                        overlappingInstance._SetStartFrame(newEndFrame + 1);
                    }
                    else
                    {
                        // We cannot insert a new gaze instance between a preceding gaze instance and its
                        // gaze back instance, so we remove the gaze back instance

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

            if (nextStartFrame - newEndFrame - 1 < maxEyeGazeGapLength)
            {
                // The gap between the end of the new instance and the start of the next instance is small,
                // so we can just extend the new gaze instance to the start of the next instance

                newInstance.SetFrameLength(nextStartFrame - newStartFrame);
                newEndFrame = nextStartFrame - 1;
            }
            else
            {
                // The gap between the end of the new instance and the start of the next instance is long,
                // so we insert a gaze-back instance to follow the new instance and extend it to the start
                // of the next instance

                var gazeBackInstance = new EyeGazeInstance(newInstance.Model, newInstance.AnimationClip.name + LEAPCore.gazeBackSuffix,
                    //Mathf.RoundToInt(LEAPCore.maxEyeGazeGapLength * LEAPCore.editFrameRate), null);
                    maxEyeGazeGapLength, null, 0f, 0f, maxEyeGazeGapLength, true, false);
                timeline.AddAnimation(layerName, gazeBackInstance, newEndFrame + 1);
            }
        }
        else if (timeline.FrameLength - newEndFrame - 1 >= maxEyeGazeGapLength)
        {
            // No follow-up gaze instance, but there is plenty more animation left on the timeline,
            // so we add a gaze-back instance to follow the new instance

            var gazeBackInstance = new EyeGazeInstance(
                newInstance.Model, newInstance.AnimationClip.name + LEAPCore.gazeBackSuffix,
                //Mathf.RoundToInt(LEAPCore.maxEyeGazeGapLength * LEAPCore.editFrameRate), null);
                maxEyeGazeGapLength, null, 0f, 0f, maxEyeGazeGapLength, true, false);
            timeline.AddAnimation(layerName, gazeBackInstance, newEndFrame + 1);
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
        if (prevInstance != null && !prevInstance.Animation.AnimationClip.name.EndsWith(LEAPCore.gazeBackSuffix))
        {
            // There is a gaze instance preceding the new one, and it is not a gaze-back instance
            // of another gaze instance

            int prevStartFrame = prevInstance.StartFrame;
            int prevEndFrame = prevInstance.StartFrame + prevInstance.Animation.FrameLength - 1;

            if (newStartFrame - prevEndFrame - 1 < maxEyeGazeGapLength)
            {
                // The gap between the end of the previous instance and the start of the new instance is small,
                // so we extend the instance so its end lines up with the start of the new instance

                (prevInstance.Animation as EyeGazeInstance).SetFrameLength(newStartFrame - prevStartFrame);
            }
            else
            {
                // The gap between the end of the previous instance and the start of the new instance is large,
                // so we add a gaze-back instance for the previous gaze instance

                var prevGazeBackInstance = new EyeGazeInstance(
                    prevInstance.Animation.Model, prevInstance.Animation.AnimationClip.name + LEAPCore.gazeBackSuffix,
                    //Mathf.RoundToInt(LEAPCore.maxEyeGazeGapLength * LEAPCore.editFrameRate), null);
                    maxEyeGazeGapLength, null, 0f, 0f, maxEyeGazeGapLength, true, false);
                timeline.AddAnimation(layerName, prevGazeBackInstance, prevEndFrame + 1);
            }
        }

        // Add the new eye gaze instance
        timeline.AddAnimation(layerName, newInstance, newStartFrame);
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

        // Get the start frame of the next eye gaze instance
        var nextInstance = timeline.GetLayer(layerName).Animations.FirstOrDefault(inst => inst.StartFrame > endFrame &&
            inst.Animation.Model == instanceToRemove.Model);
        int nextStartFrame = nextInstance != null ? nextInstance.StartFrame : timeline.FrameLength - 1;

        // First remove the follow-up gaze-back instance, if one exists
        var gazeBackInstanceToRemove = timeline.GetLayer(layerName).Animations.FirstOrDefault(inst =>
            inst.Animation.AnimationClip.name == instanceToRemove.AnimationClip.name + LEAPCore.gazeBackSuffix);
        if (gazeBackInstanceToRemove != null)
            timeline.RemoveAnimation(gazeBackInstanceToRemove.InstanceId);

        // Then remove the actual instance
        timeline.RemoveAnimation(instanceId);

        if (prevInstance != null && !prevInstance.Animation.AnimationClip.name.EndsWith(LEAPCore.gazeBackSuffix))
        {
            // There is a gaze instance preceding the one being removed, and it is not a gaze-back instance
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
                // so we add a gaze-back instance to follow the new instance

                var prevGazeBackInstance = new EyeGazeInstance(
                    prevInstance.Animation.Model, prevInstance.Animation.AnimationClip.name + LEAPCore.gazeBackSuffix,
                    //Mathf.RoundToInt(LEAPCore.maxEyeGazeGapLength * LEAPCore.editFrameRate), null);
                    nextStartFrame - prevEndFrame - 1, null, 0f, 0f, maxEyeGazeGapLength, true, false);
                timeline.AddAnimation(layerName, prevGazeBackInstance, prevEndFrame + 1);
            }
        }   
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
        if (endFrame - startFrame + 1 < Mathf.RoundToInt(LEAPCore.minEyeGazeLength * LEAPCore.editFrameRate))
        {
            throw new Exception(string.Format("Error setting eye gaze timing! Eye gaze instance must be longer than {0}s", LEAPCore.minEyeGazeLength));
        }

        // Changing the timing of an instance is equivalent to removing the instance and then re-adding it with new times
        string layerName = timeline.GetLayerForAnimation(instanceId).LayerName;
        var instance = timeline.GetAnimation(instanceId) as EyeGazeInstance;
        RemoveEyeGaze(timeline, instanceId, layerName);
        instance.SetFrameLength(endFrame - startFrame + 1);
        AddEyeGaze(timeline, instance, startFrame, layerName);
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
    }

    /// <summary>
    /// Load eye gaze behavior specification for the specified base animation.
    /// </summary>
    /// <param name="timeline">Animation timeline</param>
    /// <param name="baseAnimationInstanceId">Base animation instance ID</param>
    /// <param name="layerName">Animation layer holding eye gaze animations</param>
    /// <param name="edits">If true, loads annotations specifying eye gaze edits (i.e., gaze not encoded in the base animation)</param>
    /// <returns>true if eye gaze instances were loaded successfully, false otherwise</returns>
    public static bool LoadEyeGaze(AnimationTimeline timeline, int baseAnimationInstanceId, string layerName = "Gaze", bool edits = false)
    {
        // Get base animation
        var baseAnimation = timeline.GetAnimation(baseAnimationInstanceId);

        // Get gaze behavior file path
        string path = Application.dataPath + LEAPCore.eyeGazeDirectory.Substring(LEAPCore.eyeGazeDirectory.IndexOfAny(@"/\".ToCharArray()));
        if (path[path.Length - 1] != '/' && path[path.Length - 1] != '\\')
            path += '/';
        path += (baseAnimation.AnimationClip.name + (edits ? "#Edits.csv" : ".csv"));

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
                        int.Parse(lineElements[attributeIndices["FixationStartFrame"]]) : frameLength - 1;
                    
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
                    float torsoAlign = (float)double.Parse(lineElements[attributeIndices["TorsoAlign"]]);
                    bool turnBody = bool.Parse(lineElements[attributeIndices["TurnBody"]]);

                    // Get gaze edit settings
                    EyeGazeEditType editType = edits ? (EyeGazeEditType)Enum.Parse(typeof(EyeGazeEditType), lineElements[attributeIndices["EditType"]]) :
                        EyeGazeEditType.Add;
                    float exprGazeWeight = edits && attributeIndices.ContainsKey("ExpressiveGazeWeight") ?
                        float.Parse(lineElements[attributeIndices["FixationStartFrame"]]) : 1f;
                    
                    if (!edits)
                    {
                        // Create and schedule gaze instance
                        var gazeInstance = new EyeGazeInstance(model,
                            animationClipName, frameLength, gazeTarget, headAlign, torsoAlign, fixationStartFrame, turnBody, true, startFrame);
                        AddEyeGaze(timeline, gazeInstance, startFrame, layerName);
                    }
                    else
                    {
                        var scheduledGazeInstance = timeline.GetLayer(layerName).Animations.FirstOrDefault(
                            inst => inst.Animation.AnimationClip.name == animationClipName);

                        if (editType != EyeGazeEditType.Add && scheduledGazeInstance == null)
                        {
                            UnityEngine.Debug.LogError(string.Format(
                                "Unable to load editing operation {0} on eye gaze instance {1}: instance does not exist.",
                                editType, animationClipName));
                            continue;
                        }

                        switch (editType)
                        {
                            case EyeGazeEditType.Add:

                                // Add new eye gaze instance
                                var gazeInstance = new EyeGazeInstance(model,
                                    animationClipName, frameLength, gazeTarget, headAlign, torsoAlign, fixationStartFrame, turnBody, false);
                                AddEyeGaze(timeline, gazeInstance, startFrame, layerName);
                                break;
                                
                            case EyeGazeEditType.Remove:

                                // Remove existing eye gaze instance
                                RemoveEyeGaze(timeline, scheduledGazeInstance.InstanceId, layerName);
                                break;

                            case EyeGazeEditType.SetTiming:

                                // Change eye gaze timings
                                SetEyeGazeTiming(timeline, scheduledGazeInstance.InstanceId, startFrame, startFrame + frameLength - 1);
                                break;

                            case EyeGazeEditType.SetTarget:

                                // Change eye gaze target
                                (scheduledGazeInstance.Animation as EyeGazeInstance).Target = gazeTarget;
                                break;

                            case EyeGazeEditType.SetAlignments:

                                // Change eye gaze head and torso alignments
                                SetEyeGazeAlignments(timeline, scheduledGazeInstance.InstanceId, headAlign, torsoAlign, turnBody);
                                break;

                            default:

                                UnityEngine.Debug.LogError(string.Format(
                                    "Unrecognized edit operation {0} for eye gaze instance {1}", editType, animationClipName));
                                break;
                        }

                        (scheduledGazeInstance.Animation as EyeGazeInstance).ExpressiveGazeWeight = exprGazeWeight;
                    }
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
    /// <returns>true if eye gaze instances were saved successfully, false otherwise</returns>
    public static bool SaveEyeGaze(AnimationTimeline timeline, int baseAnimationInstanceId)
    {
        // Get base animation
        var baseAnimation = timeline.GetAnimation(baseAnimationInstanceId);

        // Get gaze behavior file path
        string path = Application.dataPath + LEAPCore.eyeGazeDirectory.Substring(LEAPCore.eyeGazeDirectory.IndexOfAny(@"/\".ToCharArray()));
        if (path[path.Length - 1] != '/' && path[path.Length - 1] != '\\')
            path += '/';
        path += (baseAnimation.AnimationClip.name + ".csv");

        // Save gaze behaviors
        try
        {
            var writer = new StreamWriter(path);
            writer.WriteLine("Character,AnimationClip,StartFrame,FixationStartFrame,EndFrame,Target,HeadAlign,TorsoAlign,TurnBody");

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
    /// Infer eye gaze target and alignment parameters for currently defined
    /// eye gaze instances on the character model.
    /// </summary>
    /// <param name="timeline">Animation timeline</param>
    /// <param name="baseAnimationInstanceId">Base animation instance ID</param>
    /// <param name="layerName">Animation layer holding eye gaze animations</param>
    /// <param name="createTargets">If gaze target could not be found for a gaze shift,
    /// a "dummy" gaze target object will be created in the scene</param>
    public static void InferEyeGazeAlignments(AnimationTimeline timeline, int baseAnimationInstanceId, string layerName = "Gaze")
    {
        // Get base animation
        var baseAnimation = timeline.GetAnimation(baseAnimationInstanceId);

        bool timelineActive = timeline.Active;
        timeline.Active = true;

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

            _InferEyeGazeAlignments(timeline, instance.InstanceId);
        }

        timeline.Active = timelineActive;
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
    public static EyeGazeControllerState GetInitControllerForEyeGazeInstance(EyeGazeInstance instance)
    {
        IAnimationControllerState curState = instance.GetControllerState();

        // Initialize gaze controller
        var gazeController = instance.GazeController;
        gazeController.GazeAt(instance.Target);
        gazeController.Head.align = instance.HeadAlign >= 0f ? instance.HeadAlign : 0f;
        if (gazeController.Torso != null)
            gazeController.Torso.align = instance.TorsoAlign >= 0f ? instance.HeadAlign : 0f;
        gazeController._InitGazeParams();
        gazeController._InitTargetRotations();
        gazeController._InitLatencies();
        gazeController._CalculateMaxVelocities();

        EyeGazeControllerState state = (EyeGazeControllerState)instance.GetControllerState();
        instance.SetControllerState(curState);

        return state;
    }

    /// <summary>
    /// Estimate gaze shift duration (from gaze shift start to fixation start) of
    /// the specified eye gaze instance.
    /// </summary>
    /// <param name="instance">Eye gaze instance</param>
    /// <param name="state">Eye gaze controller state at the start of the gaze shift</param>
    /// <returns>Gaze shift duration in seconds</returns>
    public static float ComputeEstGazeShiftTimeLength(EyeGazeInstance instance, EyeGazeControllerState state)
    {
        IAnimationControllerState curState = instance.GetControllerState();
        instance.SetControllerState(state);
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

        instance.SetControllerState(curState);

        float totalTime = Mathf.Max(eyeRotTime, headRotTime, torsoRotTime);
        // TODO: this is a quick hack to fix the problem of underestimating gaze shift duration
        return 2f * totalTime;
        //return totalTime;
    }

    // Gaze editing helper operation; remove an eye gaze instance from the animation timeline
    private static void _RemoveEyeGaze(AnimationTimeline timeline, int instanceId)
    {
        // First remove the follow-up gaze-back instance, if one exists
        string layerName = timeline.GetLayerForAnimation(instanceId).LayerName;
        var instance = timeline.GetAnimation(instanceId) as EyeGazeInstance;
        var gazeBackInstance = timeline.GetLayer(layerName).Animations.FirstOrDefault(inst =>
            inst.Animation.AnimationClip.name == instance.AnimationClip.name + LEAPCore.gazeBackSuffix);
        if (gazeBackInstance != null)
            timeline.RemoveAnimation(gazeBackInstance.InstanceId);

        // Then remove the actual instnace
        timeline.RemoveAnimation(instanceId);
    }

    // Infer eye gaze alignment values for the specified gaze instance
    private static void _InferEyeGazeAlignments(AnimationTimeline timeline, int instanceId)
    {
        var instance = timeline.GetAnimation(instanceId) as EyeGazeInstance;
        int startFrame = timeline.GetAnimationStartFrame(instanceId);
        var gazeController = instance.GazeController;

        // Apply animation at the start of the gaze shift
        timeline.GoToFrame(startFrame);
        timeline.Update(0);
        // TODO: roll in spine joints can throw off alignment computations, so remove it for now
        gazeController.RemoveRoll();
        //

        // TODO: this implementation does not work correctly for stylized gaze and when torso is disabled
        gazeController.stylizeGaze = false;
        gazeController.useTorso = true;
        //

        // Set source rotations, alignments, and gaze target
        gazeController.gazeTarget = instance.Target;
        gazeController._InitGazeParams(); // initial rotations, alignments, latencies...
        gazeController._InitTargetRotations(); // compute initial estimate of target pose
        gazeController._InitLatencies();
        gazeController._CalculateMaxVelocities();
        foreach (var joint in gazeController.gazeJoints)
        {
            joint.srcRot = joint.bone.localRotation;
        }

        // Apply animation at the end of the gaze shift
        instance.HeadAlign = 1f;
        instance.TorsoAlign = 1f;
        EyeGazeControllerState gazeControllerState = EyeGazeEditor.GetInitControllerForEyeGazeInstance(instance);
        float estTimeLength = EyeGazeEditor.ComputeEstGazeShiftTimeLength(instance, gazeControllerState); // TODO: get rid of this once we have accurate gaze shift inference
        int endFrame = startFrame + Mathf.Min(instance.FrameLength - 1, Mathf.RoundToInt(((float)LEAPCore.editFrameRate) * estTimeLength));
        timeline.GoToFrame(endFrame);
        timeline.Update(0);
        // TODO: roll in spine joints can throw off alignment computations, so remove it for now
        gazeController.RemoveRoll();
        //

        // Get target rotations of the torso and head
        Quaternion torsoTrgRot = gazeController.Torso != null ? gazeController.Torso.bone.rotation : Quaternion.identity;
        Quaternion headTrgRot = gazeController.Head.bone.rotation;

        // Set gaze joint rotations back to source rotations
        gazeController.ApplySourcePose();

        if (gazeController.Torso != null)
        {
            // Get min. and full target rotations for the torso
            Quaternion torsoTrgRotFull = gazeController.Torso._ComputeTargetRotation(gazeController.EffGazeTargetPosition);
            float distRot = gazeController._ComputeGazeShiftAmplitude();
            gazeController.Amplitude = distRot;
            float torsoDistRotFull = GazeJoint.DistanceToRotate(gazeController.Torso.srcRot, torsoTrgRotFull);
            float torsoDistRotMin = instance.GazeController._ComputeMinTorsoDistanceToRotate();
            float alignMin = gazeController.Torso.srcRot != torsoTrgRot ? torsoDistRotMin / torsoDistRotFull : 0f;
            Quaternion torsoTrgRotMin = Quaternion.Slerp(gazeController.Torso.srcRot, torsoTrgRot, alignMin);
            
            // Compute torso alignment
            instance.TorsoAlign = torsoTrgRotMin != torsoTrgRotFull ?
                GazeJoint.DistanceToRotate(torsoTrgRotMin, torsoTrgRot) / GazeJoint.DistanceToRotate(torsoTrgRotMin, torsoTrgRotFull) : 0f;
            instance.TorsoAlign = Mathf.Clamp01(instance.TorsoAlign);
            gazeController.Torso.align = gazeController.Torso.curAlign = instance.TorsoAlign;
        }

        // Compute minimal head target rotation
        gazeController.Head.align = gazeController.Head.curAlign = 0f;
        gazeController._InitTargetRotations();
        Quaternion headTrgRotMin = gazeController.Head.trgRotAlign;
        Quaternion headTrgRotFull = gazeController.Head._ComputeTargetRotation(gazeController.EffGazeTargetPosition);

        // Compute head alignment
        instance.HeadAlign = headTrgRotMin != headTrgRotFull ?
            GazeJoint.DistanceToRotate(headTrgRotMin, headTrgRot) / GazeJoint.DistanceToRotate(headTrgRotMin, headTrgRotFull) : 0f;
        instance.HeadAlign = Mathf.Clamp01(instance.HeadAlign);
        gazeController.Head.align = gazeController.Head.curAlign  = instance.HeadAlign;

        // Compute fixation start time in the gaze instance
        // TODO: get rid of this once we have accurate gaze shift inference
        gazeControllerState = EyeGazeEditor.GetInitControllerForEyeGazeInstance(instance);
        estTimeLength = EyeGazeEditor.ComputeEstGazeShiftTimeLength(instance, gazeControllerState);
        endFrame = startFrame + Mathf.Min(instance.FrameLength - 1, Mathf.RoundToInt(((float)LEAPCore.editFrameRate) * estTimeLength));
        int fixationStartFrame = endFrame - startFrame;
        instance.SetFixationStartFrame(fixationStartFrame);
    }

    // Extract expressive motion as a displacement map from the gaze shift and fixation in the base animation
    private static void _ExtractExpressiveEyeGazeAnimations(AnimationTimeline timeline, int baseAnimationInstanceId, int eyeGazeInstanceId)
    {
        var baseAnimationInstance = timeline.GetAnimation(baseAnimationInstanceId);
        var eyeGazeInstance = timeline.GetAnimation(eyeGazeInstanceId) as EyeGazeInstance;
        int eyeGazeStartFrame = timeline.GetAnimationStartFrame(eyeGazeInstanceId);
        int eyeGazeFixationStartFrame = eyeGazeStartFrame + eyeGazeInstance.FixationStartFrame;
        int eyeGazeEndFrame = eyeGazeStartFrame + eyeGazeInstance.FrameLength - 1;
        var gazeController = eyeGazeInstance.GazeController;
        var model = eyeGazeInstance.Model;
        Transform[] bones = ModelUtils.GetAllBones(model);
        string expressiveEyeGazeName = eyeGazeInstance.AnimationClip.name + LEAPCore.expressiveGazeSuffix;

        // Create expressive displacement animation clips on the model for this gaze instance
        var expressiveEyeGazeClips = new Dictionary<Transform, AnimationClip>();
        var expressiveEyeGazeCurves = new Dictionary<Transform, AnimationCurve[]>();
        var expressiveEyeGazeRotations = new Dictionary<Transform, Vector3>();
        for (int gazeJointIndex = gazeController.eyes.Length; gazeJointIndex < gazeController.gazeJoints.Length; ++gazeJointIndex)
        {
            var gazeJoint = gazeController.gazeJoints[gazeJointIndex];
            expressiveEyeGazeClips[gazeJoint.bone] = LEAPAssetUtils.CreateAnimationClipOnModel(
                expressiveEyeGazeName + (gazeJointIndex - gazeController.eyes.Length).ToString(), model);
            expressiveEyeGazeCurves[gazeJoint.bone] = LEAPAssetUtils.CreateAnimationCurvesForModel(model);
        }

        // Get gaze joint orientations at key points of the gaze shift/fixation
        Dictionary<Transform, Quaternion> startRots = new Dictionary<Transform, Quaternion>();
        Dictionary<Transform, Quaternion> fixationStartRots = new Dictionary<Transform, Quaternion>();
        baseAnimationInstance.Apply(eyeGazeStartFrame, AnimationLayerMode.Override);
        for (int gazeJointIndex = gazeController.eyes.Length; gazeJointIndex < gazeController.gazeJoints.Length; ++gazeJointIndex)
        {
            var bone = gazeController.gazeJoints[gazeJointIndex].bone;
            startRots[bone] = bone.localRotation;
        }
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

                    // Retime the animation based on gaze shift progress
                    float distRotAlign = Vector3.Angle(vs, vf);
                    float rotProgress = !GeomUtil.Equal(vs, vf) ?
                        Vector3.Angle(vs, vp) / distRotAlign :
                        ((float)(frame - eyeGazeStartFrame)) / (eyeGazeFixationStartFrame - eyeGazeStartFrame);
                    rotProgress = Mathf.Clamp01(rotProgress);
                    time = rotProgress * ((float)(frame - eyeGazeStartFrame)) / LEAPCore.editFrameRate;
                }
                else
                {
                    // Computing rotational difference for the gaze fixation
                    Quaternion qfb = baseFixationStartRots.ContainsKey(bone) ? baseFixationStartRots[bone] : startRots[bone];
                    Quaternion dqef = fixationStartExprRots.ContainsKey(bone) ? fixationStartExprRots[bone] : Quaternion.identity;
                    dq = dqef * (Quaternion.Inverse(qfb) * q);
                    
                    // Compute animation time
                    time = ((float)(frame - eyeGazeStartFrame)) / LEAPCore.editFrameRate;
                }

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
}

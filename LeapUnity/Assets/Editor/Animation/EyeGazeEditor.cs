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
            inst.Animation.Model.gameObject == newInstance.Model.gameObject).ToList();
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
            inst.Animation.Model.gameObject == newInstance.Model.gameObject);
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

                var gazeBackInstance = new EyeGazeInstance(newInstance.Model.gameObject, newInstance.AnimationClip.name + LEAPCore.gazeBackSuffix,
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
                newInstance.Model.gameObject, newInstance.AnimationClip.name + LEAPCore.gazeBackSuffix,
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
                inst.Animation.Model.gameObject == newInstance.Model.gameObject);
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
                    prevInstance.Animation.Model.gameObject, prevInstance.Animation.AnimationClip.name + LEAPCore.gazeBackSuffix,
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
                inst.Animation.Model.gameObject == instanceToRemove.Model.gameObject);
        int prevStartFrame = prevInstance.StartFrame;
        int prevEndFrame = prevStartFrame + prevInstance.Animation.FrameLength - 1;

        // Get the start frame of the next eye gaze instance
        var nextInstance = timeline.GetLayer(layerName).Animations.FirstOrDefault(inst => inst.StartFrame > endFrame &&
            inst.Animation.Model.gameObject == instanceToRemove.Model.gameObject);
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
                    prevInstance.Animation.Model.gameObject, prevInstance.Animation.AnimationClip.name + LEAPCore.gazeBackSuffix,
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
        _RemoveEyeGaze(timeline, instanceId);
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
    /// <param name="baseAnimationName">Base animation name</param>
    /// <param name="layerName">Animation layer holding eye gaze animations</param>
    /// <param name="edits">If true, loads annotations specifying eye gaze edits (i.e., gaze not encoded in the base animation)</param>
    /// <returns>true if eye gaze instances were loaded successfully, false otherwise</returns>
    public static bool LoadEyeGazeForModel(AnimationTimeline timeline, string baseAnimationName, string layerName = "Gaze", bool edits = false)
    {
        // Get gaze behavior file path
        string path = Application.dataPath + LEAPCore.eyeGazeDirectory.Substring(LEAPCore.eyeGazeDirectory.IndexOfAny(@"/\".ToCharArray()));
        if (path[path.Length - 1] != '/' && path[path.Length - 1] != '\\')
            path += '/';
        path += (baseAnimationName + (edits ? "#Edits.csv" : ".csv"));

        // Load gaze behaviors
        try
        {
            var reader = new StreamReader(path);
            bool firstLine = true;
            string line = "";
            string[] lineElements = null;
            Dictionary<string, int> attributeIndices = new Dictionary<string, int>();
            ModelController[] models = timeline.GetAllModels();
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
                    ModelController modelController = models.FirstOrDefault(m => m.gameObject.name == characterName);
                    if (modelController == null)
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
                    if (gazeTargets != null)
                        gazeTarget = gazeTargets.FirstOrDefault(obj => obj.name == gazeTargetName);
                    if (gazeTarget == null)
                    {
                        UnityEngine.Debug.LogError(string.Format(
                            "Trying to create EyeGazeInstance towards target {0} on model {1}, but the target does not exist!",
                            gazeTargetName, modelController.gameObject.name));
                        continue;
                    }
                    
                    // Get head and body coordination parameters
                    float headAlign = (float)double.Parse(lineElements[attributeIndices["HeadAlign"]]);
                    float torsoAlign = (float)double.Parse(lineElements[attributeIndices["TorsoAlign"]]);
                    bool turnBody = bool.Parse(lineElements[attributeIndices["TurnBody"]]);

                    // Is it a gaze shift in the base animation?
                    EyeGazeEditType editType = edits ? (EyeGazeEditType)Enum.Parse(typeof(EyeGazeEditType), lineElements[attributeIndices["EditType"]]) :
                        EyeGazeEditType.Add;
                    
                    if (!edits)
                    {
                        // Create and schedule gaze instance
                        var gazeInstance = new EyeGazeInstance(modelController.gameObject,
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
                                var gazeInstance = new EyeGazeInstance(modelController.gameObject,
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
                    }
                }
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
    /// Load eye gaze behavior specification for the specified base animation.
    /// </summary>
    /// <param name="timeline">Animation timeline</param>
    /// <param name="baseAnimationName">Base animation name</param>
    /// <returns>true if eye gaze instances were loaded successfully, false otherwise</returns>
    public static bool SaveEyeGazeForModel(AnimationTimeline timeline, string baseAnimationName)
    {
        // Get gaze behavior file path
        string path = Application.dataPath + LEAPCore.eyeGazeDirectory.Substring(LEAPCore.eyeGazeDirectory.IndexOfAny(@"/\".ToCharArray()));
        if (path[path.Length - 1] != '/' && path[path.Length - 1] != '\\')
            path += '/';
        path += (baseAnimationName + ".csv");

        // Save gaze behaviors
        try
        {
            var writer = new StreamWriter(path);
            writer.WriteLine("Character,AnimationClip,StartFrame,FixationStartFrame,EndFrame,Target,HeadAlign,TorsoAlign,TurnBody,EditType");

            // Get all gaze instances, sorted by start time
            var gazeInstances = new List<AnimationTimeline.ScheduledInstance>();
            foreach (var layer in timeline.Layers)
            {
                foreach (var instance in layer.Animations)
                {
                    if (!(instance.Animation is EyeGazeInstance))
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
                lineBuilder.Append(gazeInstance.Model.gameObject.name);
                lineBuilder.Append(",");
                lineBuilder.Append(gazeInstance.AnimationClip.name);
                lineBuilder.Append(",");
                lineBuilder.Append(instance.StartFrame);
                lineBuilder.Append(",");
                lineBuilder.Append(gazeInstance.FixationStartFrame);
                lineBuilder.Append(",");
                lineBuilder.Append(instance.StartFrame + gazeInstance.FrameLength - 1);
                lineBuilder.Append(",");
                lineBuilder.Append(gazeInstance.Target.name);
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
                instance.Animation.Model.gameObject.name, instance.Animation.AnimationClip.name,
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
    /// <param name="baseAnimationName">Base animation name</param>
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
    /// <param name="baseAnimationName">Base animation name</param>
    /// <param name="layerName">Animation layer holding eye gaze animations</param>
    /// <param name="createTargets">If gaze target could not be found for a gaze shift,
    /// a "dummy" gaze target object will be created in the scene</param>
    public static void InferEyeGazeAlignments(AnimationTimeline timeline, string baseAnimationName, string layerName = "Gaze")
    {
        bool timelineActive = timeline.Active;
        timeline.Active = true;

        var gazeLayer = timeline.GetLayer(layerName);
        foreach (var scheduledInstance in gazeLayer.Animations)
        {
            if (!(scheduledInstance.Animation is EyeGazeInstance) ||
                !(scheduledInstance.Animation as EyeGazeInstance).IsBase)
                continue;
            var instance = scheduledInstance.Animation as EyeGazeInstance;

            // Infer head and torso alignments
            _InferEyeGazeAlignments(timeline, scheduledInstance.InstanceId);
        }

        timeline.Active = timelineActive;
    }

    /// <summary>
    /// Estimate gaze shift duration (from gaze shift start to fixation start) of
    /// the specified eye gaze instance.
    /// </summary>
    /// <param name="instance">Eye gaze instance</param>
    /// <returns>Gaze shift duration in seconds</returns>
    public static float ComputeEstGazeShiftTimeLength(EyeGazeInstance instance)
    {
        IAnimationControllerState state = instance.GetControllerState();

        // Initialize gaze controller
        var gazeController = instance.GazeController;
        gazeController.gazeTarget = instance.Target;
        gazeController.Head.align = instance.HeadAlign >= 0f ? instance.HeadAlign : 0f;
        if (gazeController.Torso != null)
            gazeController.Torso.align = instance.TorsoAlign >= 0f ? instance.HeadAlign : 0f;
        gazeController._InitGazeParams();
        gazeController._InitTargetRotations();
        gazeController._InitLatencies();
        gazeController._CalculateMaxVelocities();

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

        instance.SetControllerState(state);

        return Mathf.Max(eyeRotTime, headRotTime, torsoRotTime);
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

        // TODO: this implementation does not work correctly for stylized gaze
        gazeController.stylizeGaze = false;
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
        float estTimeLength = EyeGazeEditor.ComputeEstGazeShiftTimeLength(instance);
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

        // Set fixation start time in the gaze instance
        // TODO: this should be done as part of the timing inference (Danny's workin' on it)
        int fixationStartFrame = endFrame - startFrame;
        instance.SetFixationStartFrame(fixationStartFrame);
    }
}


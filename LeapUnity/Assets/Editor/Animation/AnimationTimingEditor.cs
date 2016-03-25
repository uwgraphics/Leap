using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Collections;

/// <summary>
/// This class has static methods for performing timing editing operations.
/// </summary>
public static class AnimationTimingEditor
{
    /// <summary>
    /// Add a timewarp to the animation in the specified layer, on the specified character model.
    /// </summary>
    /// <param name="timeline">Animation timeline</param>
    /// <param name="model">Character model</param>
    /// <param name="layerName">Animation layer name</param>
    /// <param name="timewarp">Timewarp</param>
    /// <param name="startTime">Timewarp start times</param>
    /// <param name="origTimeLength">Timewarp length (in original animation time)</param>
    /// <param name="timeLength">Timewarp length (after timewarping)</param>
    /// <param name="endEffectorTargetHelperLayerName">Layer name for end-effector target helper animations</param>
    public static void AddTimewarp(AnimationTimeline timeline, string layerName, GameObject model,
        ITimewarp timewarp, TimeSet startTime, TimeSet origTimeLength, TimeSet timeLength,
        string endEffectorTargetHelperLayerName = "Helpers")
    {
        var layer = timeline.GetLayer(layerName);
        var modelController = model.GetComponent<ModelController>();
        var timewarps = layer.GetTimewarps(model.name);
        if (timewarps == null)
            return;
        timewarps.AddTimewarp(timewarp, startTime, origTimeLength, timeLength);

        // Add the same timewarp to end-effector target helper animations associated with the model
        var endEffectors = ModelUtil.GetEndEffectors(model);
        var endEffectorTargetHelperLayer = timeline.GetLayer(endEffectorTargetHelperLayerName);
        foreach (var endEffector in endEffectors)
        {
            int endEffectorIndex = modelController.GetBoneIndex(endEffector);
            string endEffectorTargetHelperName = ModelUtil.GetEndEffectorTargetHelperName(model, endEffector.tag);
            var endEffectorTargetHelperTimewarps = endEffectorTargetHelperLayer.GetTimewarps(endEffectorTargetHelperName);
            var endEffectorTargetHelper = endEffectorTargetHelperTimewarps.Model;
            var endEffectorStartTime = new TimeSet(endEffectorTargetHelper, startTime.boneTimes[endEffectorIndex]);
            var endEffectorOrigTimeLength = new TimeSet(endEffectorTargetHelper, origTimeLength.boneTimes[endEffectorIndex]);
            var endEffectorTimeLength = new TimeSet(endEffectorTargetHelper, timeLength.boneTimes[endEffectorIndex]);
            endEffectorTargetHelperTimewarps.AddTimewarp(timewarp, endEffectorStartTime, endEffectorOrigTimeLength, endEffectorTimeLength);
        }

        Debug.Log(string.Format(
            "Added {0}: layer = {1}, model = {2}, startTime = {3}, origTimeLength = {4}, timeLength = {5}",
            timewarp.GetType().Name, layerName, model.name, startTime.ToString(), origTimeLength.ToString(), timeLength.ToString()));
    }

    /// <summary>
    /// Remove a timewarp from the animation in the specified layer, on the specified character model.
    /// </summary>
    /// <param name="timeline">Animation timeline</param>
    /// <param name="model">Character model</param>
    /// <param name="layerName">Animation layer name</param>
    /// <param name="trackType">Animation track type</param>
    /// <param name="timewarpIndex">Timewarp index</param>
    /// <param name="endEffectorTargetHelperLayerName">Layer name for end-effector target helper animations</param>
    public static void RemoveTimewarp(AnimationTimeline timeline, string layerName, GameObject model, int timewarpIndex,
        string endEffectorTargetHelperLayerName = "Helpers")
    {
        var layer = timeline.GetLayer(layerName);
        var timewarps = layer.GetTimewarps(model.name);
        if (timewarps == null)
            return;
        timewarps.RemoveTimewarp(timewarpIndex);

        // Remove the same timewarp from end-effector target helper animations associated with the model
        var endEffectors = ModelUtil.GetEndEffectors(model);
        var endEffectorTargetHelperLayer = timeline.GetLayer(endEffectorTargetHelperLayerName);
        foreach (var endEffector in endEffectors)
        {
            string endEffectorTargetHelperName = ModelUtil.GetEndEffectorTargetHelperName(model, endEffector.tag);
            var endEffectorTargetHelperTimewarps = endEffectorTargetHelperLayer.GetTimewarps(endEffectorTargetHelperName);
            endEffectorTargetHelperTimewarps.RemoveTimewarp(timewarpIndex);
        }

        Debug.Log(string.Format(
            "Removed timewarp: layer = {0}, model = {1}, timewarpIndex = {2}",
            layerName, model.name, timewarpIndex));
    }

    /// <summary>
    /// Remove all timewarps from all animation tracks, in the specified layer,
    /// on the specified character model.
    /// </summary>
    /// <param name="timeline">Animation timeline</param>
    /// <param name="model">Character model</param>
    /// <param name="layerName">Animation layer name</param>
    public static void RemoveAllTimewarps(AnimationTimeline timeline, string layerName, GameObject model)
    {
        var layer = timeline.GetLayer(layerName);
        var timewarps = layer.GetTimewarps(model.name);
        if (timewarps == null)
            return;
        
        while (timewarps.Timewarps.Count > 0)
            timewarps.RemoveTimewarp(timewarps.Timewarps.Count - 1);

        Debug.Log(string.Format(
            "Removed all timewarps: layer = {0}, model = {1}", layerName, model.name));
    }

    /// <summary>
    /// Load timewarps from a file associated with a particular base animation.
    /// </summary>
    /// <param name="timeline">Animation timeline</param>
    /// <param name="baseAnimationInstanceId">Base animation instance ID</param>
    public static void LoadTimewarps(AnimationTimeline timeline, int baseAnimationInstanceId)
    {
        var instance = timeline.GetAnimation(baseAnimationInstanceId) as AnimationClipInstance;
        int instanceStartFrame = timeline.GetAnimationStartFrame(baseAnimationInstanceId);

        // Get timewarp annotations file path
        string path = Application.dataPath + LEAPCore.timewarpAnnotationsDirectory.Substring(
               LEAPCore.timewarpAnnotationsDirectory.IndexOfAny(@"/\".ToCharArray()));
        if (path[path.Length - 1] != '/' && path[path.Length - 1] != '\\')
            path += '/';
        path += (instance.AnimationClip.name + ".csv");

        if (!File.Exists(path))
        {
            UnityEngine.Debug.LogError(string.Format("No timewarp file at path " + path));
            return;
        }

        try
        {
            var csvData = new CSVDataFile();

            // Define timewarp attributes
            csvData.AddAttribute("Layer", typeof(string));
            csvData.AddAttribute("Timewarp", typeof(string));
            csvData.AddAttribute("StartFrame", typeof(int));
            csvData.AddAttribute("EndFrame", typeof(int));
            csvData.AddAttribute("OriginalFrameLength", typeof(int));
            csvData.AddAttribute("FrameLength", typeof(int));
            csvData.AddAttribute("TimewarpParams", typeof(float[]));
            
            // Read timewarp data
            csvData.ReadFromFile(path);
            for (int rowIndex = 0; rowIndex < csvData.NumberOfRows; ++rowIndex)
            {
                string layerName = csvData[rowIndex].GetValue<string>(0);
                var layer = timeline.GetLayer(layerName);
                if (layer == null)
                {
                    Debug.LogError("Unable to load timewarp: layer " + layerName + " does not exist");
                    continue;
                }
                string timewarpName = csvData[rowIndex].GetValue<string>(1);
                int startFrame = csvData[rowIndex].GetValue<int>(2);
                int endFrame = csvData[rowIndex].GetValue<int>(3);
                int origFrameLength = csvData[rowIndex].GetValue<int>(4);
                int frameLength = csvData[rowIndex].GetValue<int>(5);
                float[] timewarpParams = csvData[rowIndex].GetValue<float[]>(6);

                // Create timewarp
                ITimewarp timewarp;
                switch (timewarpName)
                {
                    case "Hold":

                        if (timewarpParams.Length != 0)
                        {
                            Debug.LogError("Wrong number of parameters for hold timewarp: " + timewarpParams.Length);
                            continue;
                        }

                        timewarp = new HoldTimewarp();
                        break;

                    case "Linear":

                        if (timewarpParams.Length != 0)
                        {
                            Debug.LogError("Wrong number of parameters for linear timewarp: " + timewarpParams.Length);
                            continue;
                        }

                        timewarp = new LinearTimewarp();
                        break;

                    case "MovingHold":

                        if (timewarpParams.Length != 2)
                        {
                            Debug.LogError("Wrong number of parameters for moving hold timewarp: " + timewarpParams.Length);
                            continue;
                        }

                        timewarp = new MovingHoldTimewarp(timewarpParams[0], timewarpParams[1]);
                        break;

                    default:

                        Debug.LogError("Unknown timewarp type: " + timewarpName);
                        continue;
                }

                // Compute timewarp start times
                if (!instance.KeyTimes.ContainsKey(startFrame))
                {
                    Debug.LogError(string.Format("Timewarp specified to start at keyframe {0}, but that is not a valid keyframe index",
                        startFrame));
                    continue;
                }
                var startTimes = instance.KeyTimes[startFrame];

                // Compute timewarp lengths
                var origTimeLength = new TimeSet(instance.Model);
                var timeLength = new TimeSet(instance.Model, LEAPCore.ToTime(frameLength));
                if (timewarp is LinearTimewarp)
                {
                    if (!instance.KeyTimes.ContainsKey(startFrame))
                    {
                        Debug.LogError(string.Format("Timewarp specified to end at keyframe {0}, but that is not a valid keyframe index",
                            startFrame));
                        continue;
                    }

                    var endTimes = instance.KeyTimes[startFrame];
                    origTimeLength = endTimes - startTimes;
                }
                else if (timewarp is MovingHoldTimewarp)
                {
                    origTimeLength = new TimeSet(instance.Model, LEAPCore.ToTime(origFrameLength));
                    startTimes -= origTimeLength / 2f;
                }

                // Add timewarp to the layer
                AddTimewarp(timeline, layerName, instance.Model, timewarp, startTimes, origTimeLength, timeLength);
            }
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError(string.Format("Unable to load timewarps from asset file {0}: {1}", path, ex.Message));
        }
    }
}

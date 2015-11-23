using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>
/// This class has static methods for performing timing editing operations.
/// </summary>
public static class AnimationTimingEditor
{
    /// <summary>
    /// Add a timewarp to the animation in the specified layers, on the specified character model.
    /// </summary>
    /// <param name="timeline">Animation timeline</param>
    /// <param name="model">Character model</param>
    /// <param name="layerName">Animation layer name</param>
    /// <param name="timewarp">Timewarp</param>
    /// <param name="startFrame">Timewarp start frame</param>
    /// <param name="trackType">Animation track type</param>
    /// <param name="endEffectorTargetHelperLayerName">Layer name for end-effector target helper animations</param>
    public static void AddTimewarp(AnimationTimeline timeline, string layerName, GameObject model,
        AnimationTrackType trackType, ITimewarp timewarp, int startFrame,
        string endEffectorTargetHelperLayerName = "Helpers")
    {
        var layer = timeline.GetLayer(layerName);
        var timewarps = layer.GetTimewarps(model.name);
        if (timewarps != null)
            timewarps.AddTimewarp(trackType, timewarp, startFrame);

        // Add the same timewarp to end-effector target helper animations associated with the model
        var endEffectors = ModelUtils.GetEndEffectors(model);
        var endEffectorTargetHelperLayer = timeline.GetLayer(endEffectorTargetHelperLayerName);
        foreach (var endEffector in endEffectors)
        {
            if (trackType != GetAnimationTrackForEndEffector(endEffector.tag))
                continue;

            string endEffectorTargetHelperName = ModelUtils.GetEndEffectorTargetHelperName(model, endEffector.tag);
            var endEffectorTargetHelperTimewarps = endEffectorTargetHelperLayer.GetTimewarps(endEffectorTargetHelperName);
            endEffectorTargetHelperTimewarps.AddTimewarp(trackType, timewarp, startFrame);
        }

        Debug.Log(string.Format(
            "Added {0} timewarp: layer = {1}, model = {2}, track = {3}, startFrame = {4}, origFrameLength = {5}, frameLength = {6}",
            timewarp.GetType().Name, layerName, model.name, trackType.ToString(),
            startFrame, timewarp.OrigFrameLength, timewarp.FrameLength));
    }

    /// <summary>
    /// Remove a timewarp from the animation in the specified layer, on the specifeid character model.
    /// </summary>
    /// <param name="timeline">Animation timeline</param>
    /// <param name="model">Character model</param>
    /// <param name="layerName">Animation layer name</param>
    /// <param name="trackType">Animation track type</param>
    /// <param name="timewarpIndex">Timewarp index</param>
    public static void RemoveTimewarp(AnimationTimeline timeline, string layerName, GameObject model,
        AnimationTrackType trackType, int timewarpIndex)
    {
        var layer = timeline.GetLayer(layerName);
        var timewarps = layer.GetTimewarps(model.name);
        if (timewarps != null)
            timewarps.RemoveTimewarp(trackType, timewarpIndex);

        Debug.Log(string.Format(
            "Removed timewarp: layer = {0}, model = {1}, track = {2}, timewarpIndex = {3}",
            layerName, model.name, trackType.ToString(), timewarpIndex));
    }

    /// <summary>
    /// Remove all timewarps from the specified animation track, in the specified layers,
    /// on the specified character model.
    /// </summary>
    /// <param name="timeline">Animation timeline</param>
    /// <param name="model">Character model</param>
    /// <param name="trackType">Animation track type</param>
    /// <param name="layerNames">Animation layer names; null manes timewarp will be added to all layers</param>
    public static void RemoveAllTimewarps(AnimationTimeline timeline, string layerName, GameObject model, AnimationTrackType trackType)
    {
        var layer = timeline.GetLayer(layerName);
        var timewarps = layer.GetTimewarps(model.name);
        if (timewarps != null)
            timewarps.RemoveAllTimewarps(trackType);

        Debug.Log(string.Format(
            "Removed all timewarps: layer = {0}, model = {1}, track = {2}",
            layerName, model.name, trackType.ToString()));
    }

    /// <summary>
    /// Remove all timewarps from all animation tracks, in the specified layers,
    /// on the specified character model.
    /// </summary>
    /// <param name="timeline">Animation timeline</param>
    /// <param name="model">Character model</param>
    /// <param name="layerNames">Animation layer names; null manes timewarp will be added to all layers</param>
    public static void RemoveAllTimewarps(AnimationTimeline timeline, string layerName, GameObject model)
    {
        var layer = timeline.GetLayer(layerName);
        var timewarps = layer.GetTimewarps(model.name);
        if (timewarps != null)
            timewarps.RemoveAllTimewarps();

        Debug.Log(string.Format(
            "Removed all timewarps: layer = {0}, model = {1}", layerName, model.name));
    }

    /// <summary>
    /// Load timewarps from a file.
    /// </summary>
    /// <param name="timeline">Animation timeline</param>
    /// <param name="model">Character model</param>
    /// <param name="timewarpFileName">Filename containing the timewarp definitions</param>
    public static void LoadTimewarps(AnimationTimeline timeline, GameObject model, string timewarpFilename)
    {
        // Get timewarp annotations file path
        string path = Application.dataPath + LEAPCore.timewarpAnnotationsDirectory.Substring(
               LEAPCore.timewarpAnnotationsDirectory.IndexOfAny(@"/\".ToCharArray()));
        if (path[path.Length - 1] != '/' && path[path.Length - 1] != '\\')
            path += '/';
        path += (timewarpFilename + ".csv");

        if (!File.Exists(path))
        {
            UnityEngine.Debug.LogError(string.Format("No timewarp file at path " + path));
            return;
        }

        try
        {
            var reader = new StreamReader(path);
            bool firstLine = true;
            string line = "";
            string[] lineElements = null;
            Dictionary<string, int> attributeIndices = new Dictionary<string, int>();

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
                    // Load timewarp specification
                    lineElements = line.Split(",".ToCharArray());

                    // Get timewarp data
                    string layerName = lineElements[attributeIndices["Layer"]];
                    var layer = timeline.GetLayer(layerName);
                    if (layer == null)
                    {
                        Debug.LogError("Unable to load timewarp: layer " + layerName + " does not exist");
                        continue;
                    }
                    AnimationTrackType trackType = (AnimationTrackType)Enum.Parse(typeof(AnimationTrackType),
                        lineElements[attributeIndices["Track"]], true);
                    int startFrame = int.Parse(lineElements[attributeIndices["StartFrame"]]);
                    string timewarpName = lineElements[attributeIndices["Timewarp"]];
                    string[] timewarpParams = lineElements[attributeIndices["TimewarpParams"]].Trim('\"').Split(' ');

                    // Create timewarp
                    ITimewarp timewarp;
                    switch (timewarpName)
                    {
                        case "Hold":

                            if (timewarpParams.Length != 1)
                            {
                                Debug.LogError("Wrong number of parameters for hold timewarp: " + timewarpParams.Length);
                                continue;
                            }

                            int holdFrameLength = int.Parse(timewarpParams[0]);
                            timewarp = new HoldTimewarp(holdFrameLength);
                            break;

                        case "Linear":

                            if (timewarpParams.Length != 2)
                            {
                                Debug.LogError("Wrong number of parameters for linear timewarp: " + timewarpParams.Length);
                                continue;
                            }

                            int origFrameLength = int.Parse(timewarpParams[0]);
                            int newFrameLength = int.Parse(timewarpParams[1]);
                            timewarp = new LinearTimewarp(origFrameLength, newFrameLength);
                            break;

                        default:

                            Debug.LogError("Unknown timewarp type: " + timewarpName);
                            continue;
                    }

                    // Add the timewarp
                    AddTimewarp(timeline, layerName, model, trackType, timewarp, startFrame);
                }
            }
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError(string.Format("Unable to load timewarps from asset file {0}: {1}", path, ex.Message));
        }
    }

    /// <summary>
    /// Get animation track that controls the specified end-effector.
    /// </summary>
    /// <param name="endEffectorTag">End effector tag</param>
    /// <returns>Animation track</returns>
    public static AnimationTrackType GetAnimationTrackForEndEffector(string endEffectorTag)
    {
        AnimationTrackType trackType = AnimationTrackType.Gaze;

        switch (endEffectorTag)
        {
            case LEAPCore.lWristTag:

                trackType = AnimationTrackType.LArmGesture;
                break;

            case LEAPCore.rWristTag:

                trackType = AnimationTrackType.RArmGesture;
                break;

            case LEAPCore.lAnkleTag:

                trackType = AnimationTrackType.Locomotion;
                break;

            case LEAPCore.rAnkleTag:

                trackType = AnimationTrackType.Locomotion;
                break;

            default:

                throw new Exception("Unrecognized end-effector tag: " + endEffectorTag);
        }

        return trackType;
    }
}

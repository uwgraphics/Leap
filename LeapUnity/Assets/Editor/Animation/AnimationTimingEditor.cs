using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>
/// Set of frame indexes determining the temporal location of
/// a key pose in an animation.
/// </summary>
public struct KeyFrameSet
{
    /// <summary>
    /// Global keyframe index.
    /// </summary>
    public int keyFrame;

    /// <summary>
    /// Local keyframe index for the root position.
    /// </summary>
    public int rootKeyFrame;

    /// <summary>
    /// Local keyframe indexes for bone rotations.
    /// </summary>
    public int[] boneKeyFrames;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="model">Character model</param>
    public KeyFrameSet(GameObject model)
    {
        keyFrame = 0;
        rootKeyFrame = 0;
        boneKeyFrames = new int[ModelUtil.GetAllBones(model).Length];
    }

    /// <summary>
    /// Explicit conversion from a keyframe set to a time set.
    /// </summary>
    /// <param name="keyFrameSet">Keyframe set</param>
    /// <returns>Time set</returns>
    public static explicit operator TimeSet(KeyFrameSet keyFrameSet)
    {
        var timeSet = new TimeSet();
        timeSet.rootTime = ((float)keyFrameSet.rootKeyFrame) / LEAPCore.editFrameRate;
        timeSet.boneTimes = new float[keyFrameSet.boneKeyFrames.Length];
        for (int boneIndex = 0;  boneIndex < keyFrameSet.boneKeyFrames.Length; ++boneIndex)
            timeSet.boneTimes[boneIndex] = ((float)keyFrameSet.boneKeyFrames[boneIndex]) / LEAPCore.editFrameRate;

        return timeSet;
    }
}

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
            var csvData = new CSVData();

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

    /// <summary>
    /// Extract clusters of key frame indexes in the specified animation.
    /// </summary>
    /// <param name="model">Character model</param>
    /// <param name="clip">Animation clip</param>
    /// <returns>Sets of key frame indexes</returns>
    public static KeyFrameSet[] ExtractAnimationKeyFrames(GameObject model, AnimationClip clip)
    {
        var bones = ModelUtil.GetAllBones(model);
        var endEffectors = ModelUtil.GetEndEffectors(model);
        var instance = new AnimationClipInstance(clip.name, model, true, false, false);
        var boneMask = new bool[bones.Length];

        // Compute mask over bones that aren't animated
        var curves = AnimationUtility.GetAllCurves(clip);
        for (int boneIndex = 0; boneIndex < bones.Length; ++boneIndex)
        {
            var bone = bones[boneIndex];
            string bonePath = ModelUtil.GetBonePath(bone);
            boneMask[boneIndex] = curves.Any(c => c.type == typeof(Transform) && c.path == bonePath);
        }

        // Create a CSV data table for per-frame data
        var csvDataPerFrame = new CSVData();
        csvDataPerFrame.AddAttribute("dRoot", typeof(float));
        for (int boneIndex = 0; boneIndex < bones.Length; ++boneIndex)
            if (boneMask[boneIndex])
                csvDataPerFrame.AddAttribute("dBones#" + bones[boneIndex].name, typeof(float));
        csvDataPerFrame.AddAttribute("aRoot", typeof(float));
        for (int boneIndex = 0; boneIndex < bones.Length; ++boneIndex)
            if (boneMask[boneIndex])
                csvDataPerFrame.AddAttribute("aBones#" + bones[boneIndex].name, typeof(float));
        csvDataPerFrame.AddAttribute("p0Root", typeof(float));
        for (int boneIndex = 0; boneIndex < bones.Length; ++boneIndex)
            if (boneMask[boneIndex])
                csvDataPerFrame.AddAttribute("p0Bones#" + bones[boneIndex].name, typeof(float));
        csvDataPerFrame.AddAttribute("pRoot", typeof(float));
        for (int boneIndex = 0; boneIndex < bones.Length; ++boneIndex)
            if (boneMask[boneIndex])
                csvDataPerFrame.AddAttribute("pBones#" + bones[boneIndex].name, typeof(float));
        csvDataPerFrame.AddAttribute("wRoot", typeof(float));
        for (int boneIndex = 0; boneIndex < bones.Length; ++boneIndex)
            if (boneMask[boneIndex]) 
                csvDataPerFrame.AddAttribute("wBones#" + bones[boneIndex].name, typeof(float));
        for (int endEffectorIndex = 0; endEffectorIndex < endEffectors.Length; ++endEffectorIndex)
            csvDataPerFrame.AddAttribute("pEndEff#" + endEffectors[endEffectorIndex].name, typeof(float));
        for (int endEffectorIndex = 0; endEffectorIndex < endEffectors.Length; ++endEffectorIndex)
            csvDataPerFrame.AddAttribute("wEndEff#" + endEffectors[endEffectorIndex].name, typeof(float));
        csvDataPerFrame.AddAttribute("p0", typeof(float));
        csvDataPerFrame.AddAttribute("p", typeof(float));

        // Create a CSV data table for extracted key data
        var csvDataPerKey = new CSVData();
        csvDataPerKey.AddAttribute("keyFrameIndex", typeof(int));
        csvDataPerKey.AddAttribute("keyFrameIndexRoot", typeof(int));
        for (int boneIndex = 0; boneIndex < bones.Length; ++boneIndex)
            if (boneMask[boneIndex])
                csvDataPerKey.AddAttribute("keyFrameIndexBone#" + bones[boneIndex].name, typeof(int));

        // Bone accelerations and movement magnitudes:
        float[] a0Root = new float[instance.FrameLength];
        float[,] a0Bones = new float[bones.Length, instance.FrameLength];
        float[] aRoot = new float[instance.FrameLength];
        float[,] aBones = new float[bones.Length, instance.FrameLength];
        float[] dRoot = new float[instance.FrameLength];
        float[,] dBones = new float[bones.Length, instance.FrameLength];

        // Estimate bone accelerations and movement magnitudes
        Vector3 pRootm1 = Vector3.zero;
        Quaternion[] qBonesm1 = new Quaternion[bones.Length];
        Vector3 pRootm2 = Vector3.zero;
        Quaternion[] qBonesm2 = new Quaternion[bones.Length];
        for (int frameIndex = 0; frameIndex < instance.FrameLength; ++frameIndex)
        {
            instance.Apply(frameIndex, AnimationLayerMode.Override);

            // Estimate bone acceleration
            if (frameIndex >= 2)
            {
                aRoot[frameIndex] = aRoot[frameIndex - 1] = (bones[0].position - 2f * pRootm1 + pRootm2).magnitude *
                    LEAPCore.editFrameRate * LEAPCore.editFrameRate;
                for (int boneIndex = 0; boneIndex < bones.Length; ++boneIndex)
                {
                    Quaternion dq2 = Quaternion.Inverse(Quaternion.Inverse(qBonesm2[boneIndex]) * qBonesm1[boneIndex]) *
                        (Quaternion.Inverse(qBonesm1[boneIndex]) * bones[boneIndex].localRotation);
                    aBones[boneIndex, frameIndex] = aBones[boneIndex, frameIndex - 1] =
                        QuaternionUtil.Angle(dq2) * LEAPCore.editFrameRate * LEAPCore.editFrameRate;
                }

                if (frameIndex == 2)
                {
                    aRoot[frameIndex - 2] = aRoot[frameIndex];
                    for (int boneIndex = 0; boneIndex < bones.Length; ++boneIndex)
                        aBones[boneIndex, frameIndex - 2] = aBones[boneIndex, frameIndex];
                }
            }

            // Compute bone movement magnitudes
            if (frameIndex >= 1)
            {
                dRoot[frameIndex] = (bones[0].position - pRootm1).magnitude;
                for (int boneIndex = 0; boneIndex < bones.Length; ++boneIndex)
                    dBones[boneIndex, frameIndex] = QuaternionUtil.Angle(Quaternion.Inverse(qBonesm1[boneIndex]) *
                        bones[boneIndex].localRotation);

                if (frameIndex == 1)
                {
                    dRoot[0] = dRoot[frameIndex];
                    for (int boneIndex = 0; boneIndex < bones.Length; ++boneIndex)
                        dBones[boneIndex, 0] = dBones[boneIndex, frameIndex];
                }
            }

            pRootm2 = pRootm1;
            pRootm1 = bones[0].position;
            for (int boneIndex = 0; boneIndex < bones.Length; ++boneIndex)
            {
                qBonesm2[boneIndex] = qBonesm1[boneIndex];
                qBonesm1[boneIndex] = bones[boneIndex].localRotation;
            }
        }

        // Store unnormalized acceleration values
        aRoot.CopyTo(a0Root, 0);
        for (int boneIndex = 0; boneIndex < bones.Length; ++boneIndex)
        {
            var data = CollectionUtil.GetRow<float>(aBones, boneIndex);
            CollectionUtil.SetRow<float>(a0Bones, boneIndex, data);
        }

        // Normalize acceleration values
        float maxARoot = aRoot.Max();
        float[] maxABones = new float[bones.Length];
        for (int boneIndex = 0; boneIndex < bones.Length; ++boneIndex)
            for (int frameIndex = 0; frameIndex < instance.FrameLength; ++frameIndex)
                maxABones[boneIndex] = Mathf.Max(maxABones[boneIndex], aBones[boneIndex, frameIndex]);
        for (int frameIndex = 0; frameIndex < instance.FrameLength; ++frameIndex)
        {
            aRoot[frameIndex] = maxARoot > 0f ? aRoot[frameIndex] / maxARoot : 0f;
            for (int boneIndex = 0; boneIndex < bones.Length; ++boneIndex)
                aBones[boneIndex, frameIndex] = maxABones[boneIndex] > 0f ?
                    aBones[boneIndex, frameIndex] / maxABones[boneIndex] : 0f;
        }

        // Limb lengths:
        float[] limbLengths = new float[bones.Length];
        for (int boneIndex = 0; boneIndex < bones.Length; ++boneIndex)
        {
            var bone = bones[boneIndex];
            limbLengths[boneIndex] = 0f;
            for (int childIndex = 0; childIndex < bone.childCount; ++childIndex)
            {
                var child = bone.GetChild(childIndex);
                if (!ModelUtil.IsBone(child))
                    continue;

                limbLengths[boneIndex] += child.localPosition.magnitude;
            }
        }

        // Probability signals and their weights:
        float[] p0 = new float[instance.FrameLength];
        float[] p = new float[instance.FrameLength];
        float[] p0Root = new float[instance.FrameLength];
        float[,] p0Bones = new float[bones.Length, instance.FrameLength];
        float[] pRoot = new float[instance.FrameLength];
        float[,] pBones = new float[bones.Length, instance.FrameLength];
        float[,] pEndEff = new float[endEffectors.Length, instance.FrameLength];
        float[] wRoot = new float[instance.FrameLength];
        float[,] wBones = new float[bones.Length, instance.FrameLength];
        float[,] wEndEff = new float[endEffectors.Length, instance.FrameLength];

        // Compute probability signals and their weights
        for (int frameIndex = 0; frameIndex < instance.FrameLength; ++frameIndex)
        {
            // Compute probabilities for single bone positions and rotations
            pRoot[frameIndex] = aRoot[frameIndex];
            for (int boneIndex = 0; boneIndex < bones.Length; ++boneIndex)
                pBones[boneIndex, frameIndex] = aBones[boneIndex, frameIndex];

            // Compute weights for bone probability signals
            wRoot[frameIndex] = limbLengths[0] * dRoot[frameIndex];
            for (int boneIndex = 0; boneIndex < bones.Length; ++boneIndex)
                wBones[boneIndex, frameIndex] = limbLengths[boneIndex] * dBones[boneIndex, frameIndex];

            if (instance.EndEffectorConstraints != null)
            {
                // Compute end-effector constraint probabilities and weights
                var time = new TimeSet(model, LEAPCore.ToTime(frameIndex));
                EndEffectorConstraint[] activeConstraints = null;
                float[] activeConstraintWeights = null;
                instance.GetEndEffectorConstraintsAtTime(time, out activeConstraints, out activeConstraintWeights);
                for (int endEffectorIndex = 0; endEffectorIndex < endEffectors.Length; ++endEffectorIndex)
                {
                    var endEffector = endEffectors[endEffectorIndex];
                    for (int activeConstraintIndex = 0; activeConstraintIndex < activeConstraints.Length; ++activeConstraintIndex)
                    {
                        if (activeConstraints[activeConstraintIndex].endEffector != endEffector.tag)
                            // Ignore constraint on a different end-effector
                            continue;

                        var constraint = activeConstraints[activeConstraintIndex];
                        pEndEff[endEffectorIndex, frameIndex] =
                            frameIndex == constraint.startFrame || frameIndex == (constraint.startFrame + constraint.frameLength - 1) ?
                            1f : pEndEff[endEffectorIndex, frameIndex];
                        wEndEff[endEffectorIndex, frameIndex] =
                            frameIndex == constraint.startFrame || frameIndex == (constraint.startFrame + constraint.frameLength - 1) ?
                            LEAPCore.keyExtractEndEffConstrWeight : wEndEff[endEffectorIndex, frameIndex];
                    }
                }
            }
        }

        /*// Store unsmoothed probability signals
        pRoot.CopyTo(p0Root, 0);
        for (int boneIndex = 0; boneIndex < bones.Length; ++boneIndex)
        {
            var data = CollectionUtil.GetRow<float>(pBones, boneIndex);
            CollectionUtil.SetRow<float>(p0Bones, boneIndex, data);
        }

        // Laplacian smoothing of probability signals
        GeometryUtil.SmoothCurve(pRoot, LEAPCore.keyExtractLaplaceNumIterations,
            LEAPCore.keyExtractLaplaceLambda, LEAPCore.keyExtractLaplaceMu);
        float[] pBones0 = new float[instance.FrameLength];
        for (int boneIndex = 0; boneIndex < bones.Length; ++boneIndex)
        {
            System.Buffer.BlockCopy(pBones, boneIndex * instance.FrameLength * sizeof(float),
                pBones0, 0, instance.FrameLength * sizeof(float));
            GeometryUtil.SmoothCurve(pBones0, LEAPCore.keyExtractLaplaceNumIterations,
                LEAPCore.keyExtractLaplaceLambda, LEAPCore.keyExtractLaplaceMu);
            System.Buffer.BlockCopy(pBones0, 0,
                pBones, boneIndex * instance.FrameLength * sizeof(float), instance.FrameLength * sizeof(float));
        }*/

        // Normalize bone probability signal weights
        float maxWRoot = wRoot.Max();
        float[] maxWBones = new float[bones.Length];
        for (int boneIndex = 0; boneIndex < bones.Length; ++boneIndex)
            for (int frameIndex = 0; frameIndex < instance.FrameLength; ++frameIndex)
                maxWBones[boneIndex] = Mathf.Max(maxWBones[boneIndex], wBones[boneIndex, frameIndex]);
        for (int frameIndex = 0; frameIndex < instance.FrameLength; ++frameIndex)
        {
            wRoot[frameIndex] = maxWRoot > 0f ? wRoot[frameIndex] / maxWRoot : 0f;
            for (int boneIndex = 0; boneIndex < bones.Length; ++boneIndex)
                wBones[boneIndex, frameIndex] = maxWBones[boneIndex] > 0f ?
                    wBones[boneIndex, frameIndex] / maxWBones[boneIndex] : 0f;
        }

        // Compute the global probability signal
        for (int frameIndex = 0; frameIndex < instance.FrameLength; ++frameIndex)
        {
            float sumP = 0f;
            float sumW = 0f;

            // Add bone probabilities and weights
            sumP += (wRoot[frameIndex] * pRoot[frameIndex]);
            sumW += wRoot[frameIndex];
            for (int boneIndex = 0; boneIndex < bones.Length; ++boneIndex)
            {
                sumP += (wBones[boneIndex, frameIndex] * pBones[boneIndex, frameIndex]);
                sumW += wBones[boneIndex, frameIndex];
            }

            // Add end-effector constraint probabilities and weights
            for (int endEffectorIndex = 0; endEffectorIndex < endEffectors.Length; ++endEffectorIndex)
            {
                sumP += (wEndEff[endEffectorIndex, frameIndex] * pEndEff[endEffectorIndex, frameIndex]);
                sumW += wEndEff[endEffectorIndex, frameIndex];
            }

            // Compute global probability
            p[frameIndex] = sumP / sumW;
        }

        // Smooth the global probability signal
        p.CopyTo(p0, 0);
        FilterUtil.Filter(p0, p, FilterUtil.GetTentKernel1D(LEAPCore.keyExtractLowPassKernelSize));

        // Smooth the local probability signals
        pRoot.CopyTo(p0Root, 0);
        FilterUtil.Filter(p0Root, pRoot, FilterUtil.GetTentKernel1D(LEAPCore.keyExtractLowPassKernelSize));
        for (int boneIndex = 0; boneIndex < bones.Length; ++boneIndex)
        {
            var data0 = CollectionUtil.GetRow<float>(pBones, boneIndex);
            CollectionUtil.SetRow<float>(p0Bones, boneIndex, data0);
            var data = new float[data0.Length];
            FilterUtil.Filter(data0, data, FilterUtil.GetTentKernel1D(LEAPCore.keyExtractLowPassKernelSize));
            CollectionUtil.SetRow<float>(pBones, boneIndex, data);
        }

        // Extract global key frames
        List<int> keyFrameIndexes = new List<int>();
        float lastP = 0f;
        for (int frameIndex = 1; frameIndex < instance.FrameLength - 1; ++frameIndex)
        {
            if (p[frameIndex] > p[frameIndex - 1] && p[frameIndex] > p[frameIndex + 1])
            {
                // This is a candidate key frame
                float time = frameIndex / ((float)LEAPCore.editFrameRate);

                if (keyFrameIndexes.Count > 0 &&
                    (time - keyFrameIndexes[keyFrameIndexes.Count - 1] / ((float)LEAPCore.editFrameRate))
                    < LEAPCore.keyExtractMaxClusterWidth / 2)
                {
                    // Last key frame is too close to the current candidate
                    if (p[frameIndex] < lastP)
                        // Skip current candidate
                        continue;
                    else
                        // Remove previous key
                        keyFrameIndexes.RemoveAt(keyFrameIndexes.Count - 1);
                }

                keyFrameIndexes.Add(frameIndex);
                lastP = p[frameIndex];
            }
        }

        // Extract local keyframe indexes
        KeyFrameSet[] keyFrameSets = new KeyFrameSet[keyFrameIndexes.Count];
        for (int keyIndex = 0; keyIndex < keyFrameIndexes.Count; ++keyIndex)
        {
            int keyFrameIndex = keyFrameIndexes[keyIndex];
            int prevKeyFrameIndex = keyIndex > 0 ? keyFrameIndexes[keyIndex - 1] : -1;
            int nextKeyFrameIndex = keyIndex < keyFrameIndexes.Count - 1 ? keyFrameIndexes[keyIndex + 1] : -1;
            int clusterFrameWidth = Mathf.RoundToInt(LEAPCore.keyExtractMaxClusterWidth * LEAPCore.editFrameRate);

            // Find local keyframe index for the root position
            int rootKeyFrameIndex = keyFrameIndex;
            float pRootMax = 0f;
            for (int frameIndex = Mathf.Max(keyFrameIndex - clusterFrameWidth / 2, 1);
                frameIndex <= Mathf.Min(keyFrameIndex + clusterFrameWidth / 2, instance.FrameLength - 2);
                ++frameIndex)
            {
                if (pRoot[frameIndex] > pRoot[frameIndex - 1] && pRoot[frameIndex] > pRoot[frameIndex + 1])
                {
                    // This is a candidate local keyframe index
                    if (Math.Abs(frameIndex - prevKeyFrameIndex) < Math.Abs(frameIndex - keyFrameIndex) ||
                        Math.Abs(nextKeyFrameIndex - frameIndex) < Math.Abs(frameIndex - keyFrameIndex))
                    {
                        // Candidate is closer to a neighboring key
                        continue;
                    }

                    if (pRoot[frameIndex] > pRootMax)
                    {
                        pRootMax = pRoot[frameIndex];
                        rootKeyFrameIndex = frameIndex;
                    }
                }
            }

            // Find local keyframe indexes for the bone rotations
            int[] boneKeyFrameIndexes = new int[bones.Length];
            for (int boneIndex = 0; boneIndex < bones.Length; ++boneIndex)
            {
                boneKeyFrameIndexes[boneIndex] = keyFrameIndex;

                float pBoneMax = 0f;
                for (int frameIndex = Mathf.Max(keyFrameIndex - clusterFrameWidth / 2, 1);
                    frameIndex <= Mathf.Min(keyFrameIndex + clusterFrameWidth / 2, instance.FrameLength - 2);
                    ++frameIndex)
                {
                    if (pBones[boneIndex, frameIndex] > pBones[boneIndex, frameIndex - 1] &&
                        pBones[boneIndex, frameIndex] > pBones[boneIndex, frameIndex + 1])
                    {
                        // This is a candidate local keyframe index
                        if (Math.Abs(frameIndex - prevKeyFrameIndex) < Math.Abs(frameIndex - keyFrameIndex) ||
                            Math.Abs(nextKeyFrameIndex - frameIndex) < Math.Abs(frameIndex - keyFrameIndex))
                        {
                            // Candidate is closer to a neighboring key
                            continue;
                        }

                        if (pBones[boneIndex, frameIndex] > pBoneMax)
                        {
                            pBoneMax = pBones[boneIndex, frameIndex];
                            boneKeyFrameIndexes[boneIndex] = frameIndex;
                        }
                    }
                }
            }

            // Create and add keyframe set
            KeyFrameSet keyFrameSet = new KeyFrameSet(model);
            keyFrameSet.keyFrame = keyFrameIndex;
            keyFrameSet.rootKeyFrame = rootKeyFrameIndex;
            keyFrameSet.boneKeyFrames = boneKeyFrameIndexes;
            keyFrameSets[keyIndex] = keyFrameSet;
        }
        
        // Add and write per-frame CSV data
        for (int frameIndex = 0; frameIndex < instance.FrameLength; ++frameIndex)
        {
            List<object> data = new List<object>();

            // Compose a row of data
            data.Add(dRoot[frameIndex]);
            for (int boneIndex = 0; boneIndex < bones.Length; ++boneIndex)
                if (boneMask[boneIndex])
                    data.Add(dBones[boneIndex, frameIndex]);
            data.Add(a0Root[frameIndex]);
            for (int boneIndex = 0; boneIndex < bones.Length; ++boneIndex)
                if (boneMask[boneIndex])
                    data.Add(a0Bones[boneIndex, frameIndex]);
            data.Add(p0Root[frameIndex]);
            for (int boneIndex = 0; boneIndex < bones.Length; ++boneIndex)
                if (boneMask[boneIndex])
                    data.Add(p0Bones[boneIndex, frameIndex]);
            data.Add(pRoot[frameIndex]);
            for (int boneIndex = 0; boneIndex < bones.Length; ++boneIndex)
                if (boneMask[boneIndex])
                    data.Add(pBones[boneIndex, frameIndex]);
            data.Add(wRoot[frameIndex]);
            for (int boneIndex = 0; boneIndex < bones.Length; ++boneIndex)
                if (boneMask[boneIndex])
                    data.Add(wBones[boneIndex, frameIndex]);
            for (int endEffectorIndex = 0; endEffectorIndex < endEffectors.Length; ++endEffectorIndex)
                data.Add(pEndEff[endEffectorIndex, frameIndex]);
            for (int endEffectorIndex = 0; endEffectorIndex < endEffectors.Length; ++endEffectorIndex)
                data.Add(wEndEff[endEffectorIndex, frameIndex]);
            data.Add(p0[frameIndex]);
            data.Add(p[frameIndex]);

            // Add it to the table
            csvDataPerFrame.AddData(data.ToArray());
        }
        csvDataPerFrame.WriteToFile("../Matlab/KeyExtraction/dataPerFrame.csv");

        // Add and write per-key CSV data
        for (int keyIndex = 0; keyIndex < keyFrameIndexes.Count; ++keyIndex)
        {
            List<object> data = new List<object>();

            // Compose a row of data
            data.Add(keyFrameIndexes[keyIndex]);
            int localKeyFrameIndex = keyFrameSets[keyIndex].rootKeyFrame;
            data.Add(localKeyFrameIndex);
            for (int boneIndex = 0; boneIndex < bones.Length; ++boneIndex)
            {
                if (!boneMask[boneIndex])
                    continue;

                localKeyFrameIndex = keyFrameSets[keyIndex].boneKeyFrames[boneIndex];
                data.Add(localKeyFrameIndex);
            }

            // Add it to the table
            csvDataPerKey.AddData(data.ToArray());
        }
        csvDataPerKey.WriteToFile("../Matlab/KeyExtraction/dataPerKey.csv");

        Debug.Log(string.Format("Extracted keyframes for animation {0} on character model {1}",
            clip.name, model.name));

        return keyFrameSets;
    }

    /// <summary>
    /// Load a sequence of animation keyframe index sets from a file.
    /// </summary>
    /// <param name="model">Character model</param>
    /// <param name="clip">Animation clip</param>
    /// <param name="keyFrameSets">Keyframe set sequence</param>
    /// <returns>true if keyframes were successfully loaded, false otherwise</returns>
    public static bool LoadAnimationKeyFrames(GameObject model, AnimationClip clip, out KeyFrameSet[] keyFrameSets)
    {
        keyFrameSets = null;

        // Get keyframe file path
        string path = Application.dataPath + LEAPCore.keyFrameAnnotationsDirectory.Substring(
            LEAPCore.keyFrameAnnotationsDirectory.IndexOfAny(@"/\".ToCharArray()));
        if (path[path.Length - 1] != '/' && path[path.Length - 1] != '\\')
            path += '/';
        path += (clip.name + ".csv");

        // Load keyframes
        try
        {
            var csvData = new CSVData();

            // Define keyframe data attributes
            csvData.AddAttribute("KeyFrame", typeof(int));
            csvData.AddAttribute("RootKeyFrame", typeof(int));
            csvData.AddAttribute("BoneKeyFrames", typeof(int[]));

            // Load keyframe data
            csvData.ReadFromFile(path);
            var keyFrameSetList = new List<KeyFrameSet>();
            for (int keyIndex = 0; keyIndex < csvData.NumberOfRows; ++keyIndex)
            {
                int keyFrameIndex = csvData[keyIndex].GetValue<int>(0);
                int rootKeyFrameIndex = csvData[keyIndex].GetValue<int>(1);
                int[] boneKeyFrameIndexes = csvData[keyIndex].GetValue<int[]>(2);

                var keyFrameSet = new KeyFrameSet();
                keyFrameSet.keyFrame = keyFrameIndex;
                keyFrameSet.rootKeyFrame = rootKeyFrameIndex;
                keyFrameSet.boneKeyFrames = boneKeyFrameIndexes;
                keyFrameSetList.Add(keyFrameSet);
            }

            keyFrameSets = keyFrameSetList.ToArray();
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError(string.Format("Unable to load keyframes from asset file {0}: {1}", path, ex.Message));
            return false;
        }

        return true;
    }

    /// <summary>
    /// Save a sequence of animation keyframe index sets to a file.
    /// </summary>
    /// <param name="model">Character model</param>
    /// <param name="clip">Animation clip</param>
    /// <param name="keyFrameSets">Keyframe set sequence</param>
    /// <returns>true if keyframes were successfully saved, false otherwise</returns>
    public static bool SaveAnimationKeyFrames(GameObject model, AnimationClip clip, KeyFrameSet[] keyFrameSets)
    {
        // Get keyframe file path
        string path = Application.dataPath + LEAPCore.keyFrameAnnotationsDirectory.Substring(
            LEAPCore.keyFrameAnnotationsDirectory.IndexOfAny(@"/\".ToCharArray()));
        if (path[path.Length - 1] != '/' && path[path.Length - 1] != '\\')
            path += '/';
        path += (clip.name + ".csv");

        // Save keyframes
        try
        {
            var csvData = new CSVData();
            
            // Define keyframe data attributes
            csvData.AddAttribute("KeyFrame", typeof(int));
            csvData.AddAttribute("RootKeyFrame", typeof(int));
            csvData.AddAttribute("BoneKeyFrames", typeof(int[]));

            // Add keyframe data
            for (int keyIndex = 0; keyIndex < keyFrameSets.Length; ++keyIndex)
            {
                csvData.AddData(keyFrameSets[keyIndex].keyFrame, keyFrameSets[keyIndex].rootKeyFrame,
                    keyFrameSets[keyIndex].boneKeyFrames);
            }

            // Write keyframe data to file
            csvData.WriteToFile(path);
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError(string.Format("Unable to save keyframes to asset file {0}: {1}", path, ex.Message));
            return false;
        }

        return true;
    }
}

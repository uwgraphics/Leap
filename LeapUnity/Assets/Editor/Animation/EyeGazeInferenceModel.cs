using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

/// <summary>
/// This class has static methods for inferring eye gaze behavior from a body animation.
/// </summary>
public static class EyeGazeInferenceModel
{
    private enum EyeGazeIntervalType
    {
        GazeShift,
        GazeFixation,
        Unknown
    }

    // Defines an interval in the body animation that corresponds to a single gaze shift or fixation
    private struct EyeGazeInterval
    {
        public KeyFrameSet startKeyFrameSet;
        public KeyFrameSet endKeyFrameSet;
        public EyeGazeIntervalType intervalType;

        public EyeGazeInterval(KeyFrameSet startKeyFrameSet, KeyFrameSet endKeyFrameSet, EyeGazeIntervalType intervalType)
        {
            this.startKeyFrameSet = startKeyFrameSet;
            this.endKeyFrameSet = endKeyFrameSet;
            this.intervalType = intervalType;
        }
    }

    /// <summary>
    /// Analyze a base body animation to infer an eye gaze behavior that matches it.
    /// </summary>
    /// <param name="timeline">Animation timeline</param>
    /// <param name="baseAnimationInstanceId">Base animation instance ID</param>
    /// <param name="layerName">Gaze animation layer name</param>
    public static void InferEyeGazeInstances(AnimationTimeline timeline, int baseAnimationInstanceId,
        string layerName = "Gaze", string envLayerName = "Environment")
    {
        // Clear any prior gaze instances
        var baseInstance = timeline.GetAnimation(baseAnimationInstanceId) as AnimationClipInstance;
        var model = baseInstance.Model;
        // TODO: bring this back
        timeline.RemoveAllAnimations(layerName, model.name);
        
        _InferEyeGazeTimings(timeline, baseAnimationInstanceId, layerName);
        var targetInferenceModel = new EyeGazeTargetInferenceModel(model, timeline.OwningManager.Environment);
        targetInferenceModel.InferTargets(timeline, baseAnimationInstanceId, layerName, envLayerName);
        targetInferenceModel.DestroyResources();
        _InferEyeGazeAlignments(timeline, baseAnimationInstanceId, layerName);

        Debug.Log("Gaze inference complete!");
    }

    // Infer start and end times of gaze shifts and fixations
    private static void _InferEyeGazeTimings(AnimationTimeline timeline, int baseAnimationInstanceId,
        string layerName)
    {
        Debug.Log("Inferring gaze instances and their timings...");

        var baseInstance = timeline.GetAnimation(baseAnimationInstanceId) as AnimationClipInstance;
        var model = baseInstance.Model;
        var root = ModelUtil.FindRootBone(model);
        var bones = ModelUtil.GetAllBones(model);
        var gazeController = model.GetComponent<GazeController>();

        // Create bone mask for gaze shift key time extraction
        var boneMask = new BitArray(bones.Length, false);
        var gazeJoints = gazeController.head.gazeJoints.Union(gazeController.torso.gazeJoints)
            .Union(new[] { root }).ToArray();
        foreach (var gazeJoint in gazeJoints)
        {
            int gazeJointIndex = ModelUtil.FindBoneIndex(bones, gazeJoint);
            boneMask[gazeJointIndex] = true;
        }
        
        // Extract keyframes that signify likely gaze shift starts and ends
        var gazeKeyFrames = AnimationTimingEditor.ExtractAnimationKeyFrames(model, baseInstance.AnimationClip,
            false, false, boneMask, LEAPCore.eyeGazeKeyExtractMaxClusterWidth);

        // Compute gaze joint rotations and velocities
        Quaternion[,] qBones = new Quaternion[bones.Length, baseInstance.FrameLength];
        float[,] v0Bones = new float[bones.Length, baseInstance.FrameLength];
        for (int frameIndex = 0; frameIndex < baseInstance.FrameLength; ++frameIndex)
        {
            baseInstance.Apply(frameIndex, AnimationLayerMode.Override);

            // Estimate gaze joint velocities
            if (frameIndex >= 1)
            {
                for (int boneIndex = 0; boneIndex < bones.Length; ++boneIndex)
                {
                    if (!boneMask[boneIndex])
                        continue;

                    v0Bones[boneIndex, frameIndex] = QuaternionUtil.Angle(
                        Quaternion.Inverse(qBones[boneIndex, frameIndex - 1]) * bones[boneIndex].localRotation)
                        * LEAPCore.editFrameRate;
                }

                if (frameIndex == 1)
                {
                    for (int boneIndex = 0; boneIndex < bones.Length; ++boneIndex)
                    {
                        if (!boneMask[boneIndex])
                            continue;

                        v0Bones[boneIndex, 0] = v0Bones[boneIndex, frameIndex];
                    }
                }
            }

            for (int boneIndex = 0; boneIndex < bones.Length; ++boneIndex)
            {
                if (!boneMask[boneIndex])
                    continue;

                qBones[boneIndex, frameIndex] = bones[boneIndex].localRotation;
            }
        }

        // Smooth gaze joint velocities
        float[,] vBones = new float[bones.Length, baseInstance.FrameLength];
        for (int boneIndex = 0; boneIndex < bones.Length; ++boneIndex)
        {
            if (!boneMask[boneIndex])
                continue;

            var data0 = CollectionUtil.GetRow<float>(v0Bones, boneIndex);
            var data = new float[data0.Length];
            FilterUtil.Filter(data0, data, FilterUtil.GetTentKernel1D(LEAPCore.gazeInferenceLowPassKernelSize));
            CollectionUtil.SetRow<float>(vBones, boneIndex, data);
        }

        // Write out gaze joint velocities
        var csvGazeJointVelocities = new CSVDataFile();
        for (int boneIndex = 0; boneIndex < bones.Length; ++boneIndex)
            if (boneMask[boneIndex])
                csvGazeJointVelocities.AddAttribute("vBones#" + bones[boneIndex].name, typeof(float));
        for (int frameIndex = 0; frameIndex < baseInstance.FrameLength; ++frameIndex)
        {
            List<object> data = new List<object>();
            
            for (int boneIndex = 0; boneIndex < bones.Length; ++boneIndex)
                if (boneMask[boneIndex])
                    data.Add(vBones[boneIndex, frameIndex]);
            csvGazeJointVelocities.AddData(data.ToArray());
        }
        csvGazeJointVelocities.WriteToFile("../Matlab/KeyExtraction/gazeJointVelocities.csv");

        // Classify gaze intervals
        var gazeIntervals = new List<EyeGazeInterval>();
        for (int gazeIntervalIndex = -1; gazeIntervalIndex < gazeKeyFrames.Length; ++gazeIntervalIndex)
        {
            // Get start and end keyframe sets for the current interval
            var startKeyFrameSet = gazeIntervalIndex < 0 ? new KeyFrameSet(model) : gazeKeyFrames[gazeIntervalIndex];
            var endKeyFrameSet = gazeIntervalIndex >= gazeKeyFrames.Length - 1 ?
                new KeyFrameSet(model, baseInstance.FrameLength - 1) : gazeKeyFrames[gazeIntervalIndex + 1];

            // Compute bone weights based on their contribution to the movement
            float[] wBones = new float[bones.Length];
            baseInstance.Apply(startKeyFrameSet.keyFrame, AnimationLayerMode.Override);
            for (int boneIndex = 0; boneIndex < bones.Length; ++boneIndex)
            {
                if (!boneMask[boneIndex])
                    continue;

                // Movement magnitude
                float d = QuaternionUtil.Angle(
                    Quaternion.Inverse(qBones[boneIndex, startKeyFrameSet.boneKeyFrames[boneIndex]]) *
                    qBones[boneIndex, endKeyFrameSet.boneKeyFrames[boneIndex]]);

                // Segment length
                var bone = bones[boneIndex];
                float length = 0f;
                for (int childIndex = 0; childIndex < bone.childCount; ++childIndex)
                {
                    var child = bone.GetChild(childIndex);
                    if (!ModelUtil.IsBone(child))
                        continue;

                    length += child.localPosition.magnitude;
                }

                // Bone weight
                wBones[boneIndex] = d * length;
            }

            // Compute per-joint gaze shift and fixation probabilities
            float[] pGSBones = new float[bones.Length];
            float[] pGFBones = new float[bones.Length];
            for (int boneIndex = 0; boneIndex < bones.Length; ++boneIndex)
            {
                if (!boneMask[boneIndex])
                    continue;

                int startFrameIndex = startKeyFrameSet.boneKeyFrames[boneIndex];
                int endFrameIndex = endKeyFrameSet.boneKeyFrames[boneIndex];

                // Get max. and min. velocities
                float vMax = 0f, vMin = float.MaxValue;
                for (int frameIndex = startFrameIndex + 1; frameIndex <= endFrameIndex - 1; ++frameIndex)
                {
                    float v = vBones[boneIndex, frameIndex];
                    if (v > vBones[boneIndex, frameIndex - 1] && v > vBones[boneIndex, frameIndex + 1])
                        // This is a velocity peak
                        vMax = Mathf.Max(vMax, v);

                    vMin = Mathf.Min(vMin, v);
                }

                if (vMax <= 0f)
                {
                    // No velocity peaks, this is not a gaze shift
                    pGSBones[boneIndex] = 0f;
                    pGFBones[boneIndex] = 1f;
                    continue;
                }

                // Compute relative velocity ratios
                float dvMax = vMax - Mathf.Min(vBones[boneIndex, startFrameIndex], vBones[boneIndex, endFrameIndex]);
                float dvMin = Mathf.Max(vBones[boneIndex, startFrameIndex], vBones[boneIndex, endFrameIndex]) - vMin;
                float rGS = 0f;
                if (dvMax < 0.0001f) rGS = 0f;
                else if (dvMin < 0.0001f) rGS = float.MaxValue;
                else rGS = dvMax / dvMin;
                float rGF = 0f;
                if (dvMin < 0.0001f) rGF = 0f;
                else if (dvMax < 0.0001f) rGF = float.MaxValue;
                else rGF = dvMin / dvMax;

                // Compute gaze shift and fixation probabilities
                pGSBones[boneIndex] = Mathf.Clamp01(2f /
                    (1f + Mathf.Exp(-LEAPCore.gazeShiftInferenceLogisticSlope * rGS)) - 1f);
                pGFBones[boneIndex] = Mathf.Clamp01(2f /
                    (1f + Mathf.Exp(-LEAPCore.gazeShiftInferenceLogisticSlope * rGF)) - 1f);
            }

            // Compute total probability that this is a gaze shift
            float pGS = 0f, pGF = 0f;
            float sumWBones = wBones.Sum();
            for (int boneIndex = 0; boneIndex < bones.Length; ++boneIndex)
            {
                if (!boneMask[boneIndex])
                    continue;

                pGS += (wBones[boneIndex] * pGSBones[boneIndex]);
                pGF += (wBones[boneIndex] * pGFBones[boneIndex]);
            }
            pGS /= sumWBones;
            pGF /= sumWBones;

            // Classify and add the gaze interval
            var gazeIntervalType = pGS > pGF ? EyeGazeIntervalType.GazeShift : EyeGazeIntervalType.GazeFixation;
            gazeIntervals.Add(new EyeGazeInterval(startKeyFrameSet, endKeyFrameSet, gazeIntervalType));
        }

        // Merge adjacent gaze fixation intervals
        for (int gazeIntervalIndex = 0; gazeIntervalIndex < gazeIntervals.Count - 1; ++gazeIntervalIndex)
        {
            var gazeInterval = gazeIntervals[gazeIntervalIndex];
            var nextGazeInterval = gazeIntervals[gazeIntervalIndex + 1];

            if (gazeInterval.intervalType == EyeGazeIntervalType.GazeFixation &&
                nextGazeInterval.intervalType == EyeGazeIntervalType.GazeFixation)
            {
                gazeInterval.endKeyFrameSet = nextGazeInterval.endKeyFrameSet;
                gazeIntervals.RemoveAt(gazeIntervalIndex + 1);
                gazeIntervals[gazeIntervalIndex] = gazeInterval;
                --gazeIntervalIndex;
            }
        }

        // Generate gaze instances
        int gazeInstanceIndex = 1;
        for (int gazeIntervalIndex = 0; gazeIntervalIndex < gazeIntervals.Count; ++gazeIntervalIndex)
        {
            var gazeInterval = gazeIntervals[gazeIntervalIndex];
            if (gazeInterval.intervalType != EyeGazeIntervalType.GazeShift)
                continue;

            // Determine gaze shift and fixation start frames
            int startFrame = gazeInterval.startKeyFrameSet.keyFrame;
            int fixationStartFrame = gazeInterval.endKeyFrameSet.keyFrame;
            for (int boneIndex = 0; boneIndex < bones.Length; ++boneIndex)
            {
                if (!boneMask[boneIndex])
                    continue;

                startFrame = Mathf.Min(startFrame, gazeInterval.startKeyFrameSet.boneKeyFrames[boneIndex]);
                fixationStartFrame = Mathf.Max(fixationStartFrame, gazeInterval.endKeyFrameSet.boneKeyFrames[boneIndex]);
            }

            // Determine gaze fixation end frame
            int endFrame = fixationStartFrame;
            if (gazeIntervalIndex + 1 < gazeIntervals.Count)
            {
                var nextGazeInterval = gazeIntervals[gazeIntervalIndex + 1];
                if (nextGazeInterval.intervalType == EyeGazeIntervalType.GazeFixation)
                {
                    endFrame = nextGazeInterval.endKeyFrameSet.keyFrame;
                    for (int boneIndex = 0; boneIndex < bones.Length; ++boneIndex)
                    {
                        if (!boneMask[boneIndex])
                            continue;

                        endFrame = Mathf.Max(endFrame, nextGazeInterval.endKeyFrameSet.boneKeyFrames[boneIndex]);
                    }
                }
            }

            // Add new eye gaze instance
            var gazeInstance = new EyeGazeInstance(baseInstance.AnimationClip.name + "Gaze" + gazeInstanceIndex,
                model, endFrame - startFrame + 1, fixationStartFrame - startFrame + 1, null, 0f, 0f, true,
                baseInstance.AnimationClip, null);
            EyeGazeEditor.AddEyeGaze(timeline, gazeInstance, startFrame, layerName);
            ++gazeInstanceIndex;
        }
    }

    // Infer gaze shift alignment parameter values
    private static void _InferEyeGazeAlignments(AnimationTimeline timeline, int baseAnimationInstanceId,
        string layerName)
    {
        Debug.Log("Inferring gaze instance alignment parameter values...");

        var baseInstance = timeline.GetAnimation(baseAnimationInstanceId);
        var gazeLayer = timeline.GetLayer(layerName);
        var gazeController = baseInstance.Model.GetComponent<GazeController>();

        // Deactivate gaze
        bool gazeControllerEnabled = gazeController.enabled;
        bool gazeLayerActive = gazeLayer.Active;
        gazeController.enabled = false;
        gazeLayer.Active = false;

        foreach (var instance in gazeLayer.Animations)
        {
            if (!(instance.Animation is EyeGazeInstance) ||
                instance.Animation.Model != baseInstance.Model)
            {
                continue;
            }

            _InferEyeGazeAlignments(timeline, baseAnimationInstanceId, instance.InstanceId);
        }

        // Reset to initial state
        gazeController.enabled = gazeControllerEnabled;
        gazeLayer.Active = gazeLayerActive;
        timeline.GoToFrame(0);
        timeline.ResetModelsAndEnvironment();
    }

    // Infer gaze shift alignment parameter values for the specified gaze shift
    private static void _InferEyeGazeAlignments(AnimationTimeline timeline, int baseAnimationInstanceId, int instanceId)
    {
        var baseInstance = timeline.GetAnimation(baseAnimationInstanceId) as AnimationClipInstance;
        var gazeInstance = timeline.GetAnimation(instanceId) as EyeGazeInstance;
        var targetInstance = gazeInstance.Target != null && gazeInstance.TargetAnimationClip != null ?
            new AnimationClipInstance(gazeInstance.TargetAnimationClip.name,
            gazeInstance.Target, false, false, false) : null;
        int startFrame = timeline.GetAnimationStartFrame(instanceId);
        int fixationStartFrame = startFrame + gazeInstance.FixationStartFrame;
        var model = gazeInstance.Model;
        var gazeController = gazeInstance.GazeController;
        var zeroState = gazeController.GetZeroRuntimeState();
        
        // Compute gaze target position offset due to moving base
        Vector3 movingTargetPosOff = EyeGazeEditor.ComputeMovingGazeTargetPositionOffset(gazeInstance,
            new TimeSet(model, LEAPCore.ToTime(startFrame)), baseInstance, targetInstance);

        // Compute initial state of the gaze controller at the start of the current gaze instance
        gazeInstance.HeadAlign = 0f;
        gazeInstance.TorsoAlign = 0f;
        gazeController.SetRuntimeState(zeroState);
        var initState = EyeGazeEditor.GetInitControllerForEyeGazeInstance(gazeInstance, movingTargetPosOff);
        gazeController.SetRuntimeState(initState);

        // Key gaze directions in the gaze instance
        Vector3 srcDirHead, trgDirHead, trgDirMinHead, trgDirAlignHead,
            srcDirTorso, trgDirTorso, trgDirMinTorso, trgDirAlignTorso;

        // Get source gaze directions
        srcDirHead = gazeController.head._SourceDirection;
        srcDirTorso = gazeController.torso.Defined ? gazeController.torso._SourceDirection : Vector3.zero;

        // Compute full target directions of the head and torso
        trgDirHead = gazeController.head._TargetDirection;
        trgDirTorso = gazeController.torso.Defined ? gazeController.torso._TargetDirection : Vector3.zero;

        // Compute torso alignment
        if (gazeInstance.TurnBody)
        {
            // Compute min. target direction of the torso
            float minDistRotTorso = gazeController.MinTorsoAmplitude;
            float fullDistRotTorso = Vector3.Angle(srcDirTorso, trgDirTorso);
            float alignMinTorso = srcDirTorso != trgDirTorso ? minDistRotTorso / fullDistRotTorso : 0f;
            Quaternion rotAlignMinTorso = Quaternion.Slerp(Quaternion.identity,
                Quaternion.FromToRotation(srcDirTorso, trgDirTorso), alignMinTorso);
            trgDirMinTorso = rotAlignMinTorso * srcDirTorso;

            // Apply animation at the end of the gaze shift
            baseInstance.Apply(fixationStartFrame, AnimationLayerMode.Override);
            if (targetInstance != null)
                targetInstance.Apply(fixationStartFrame, AnimationLayerMode.Override);
        
            // Compute torso target rotation at the end of the gaze shift
            Vector3 trgDirTorso1 = gazeController.torso.GetTargetDirection(gazeController.CurrentGazeTargetPosition);

            if (srcDirTorso == trgDirTorso1)
            {
                trgDirAlignTorso = srcDirTorso;
                gazeInstance.TorsoAlign = 0f;
            }
            else
            {
                Vector3 curDir = gazeController.torso.Direction;

                // Compute aligning target direction for the torso
                trgDirAlignTorso = GeometryUtil.ProjectVectorOntoPlane(curDir, Vector3.Cross(srcDirTorso, trgDirTorso1));
                float r = _ComputeGazeJointAlignment(srcDirTorso, trgDirTorso1, srcDirTorso, trgDirAlignTorso);
                Quaternion rotAlignTorso = Quaternion.Slerp(Quaternion.identity,
                    Quaternion.FromToRotation(srcDirTorso, trgDirTorso), r);
                trgDirAlignTorso = rotAlignTorso * srcDirTorso;

                // Compute torso alignment
                gazeInstance.TorsoAlign = _ComputeGazeJointAlignment(srcDirTorso, trgDirTorso, trgDirMinTorso, trgDirAlignTorso);
            }

            gazeController.torso.align = gazeInstance.TorsoAlign;
        }

        // Compute initial state of the gaze controller at the start of the current gaze instance,
        // but with correct torso alignment
        gazeInstance.HeadAlign = 0f;
        gazeController.SetRuntimeState(zeroState);
        initState = EyeGazeEditor.GetInitControllerForEyeGazeInstance(gazeInstance, movingTargetPosOff);
        gazeController.SetRuntimeState(initState);

        // Get min. target direction of the head
        trgDirMinHead = gazeController.head._TargetDirectionAlign;

        // Apply animation at the end of the gaze shift
        baseInstance.Apply(fixationStartFrame, AnimationLayerMode.Override);
        if (targetInstance != null)
            targetInstance.Apply(fixationStartFrame, AnimationLayerMode.Override);

        // Compute head target rotation at the end of the gaze shift
        Vector3 trgDirHead1 = gazeController.head.GetTargetDirection(gazeController.CurrentGazeTargetPosition);

        // Compute head alignment
        if (srcDirHead == trgDirHead1)
        {
            trgDirAlignHead = srcDirHead;
            gazeInstance.HeadAlign = 0f;
        }
        else
        {
            Vector3 curDir = gazeController.head.Direction;

            // Compute aligning target direction for the head
            trgDirAlignHead = GeometryUtil.ProjectVectorOntoPlane(curDir, Vector3.Cross(srcDirHead, trgDirHead1));
            float r = _ComputeGazeJointAlignment(srcDirHead, trgDirHead1, srcDirHead, trgDirAlignHead);
            Quaternion rotAlignHead = Quaternion.Slerp(Quaternion.identity,
                Quaternion.FromToRotation(srcDirHead, trgDirHead), r);
            trgDirAlignHead = rotAlignHead * srcDirHead;

            // Compute head alignment
            gazeInstance.HeadAlign = _ComputeGazeJointAlignment(srcDirHead, trgDirHead, trgDirMinHead, trgDirAlignHead);
        }

        // Leave gaze controller in zero state
        gazeController.SetRuntimeState(zeroState);
    }

    // Compute alignment parameter value for the specified gaze body part
    // in a gaze shift with given source and target directions.
    private static float _ComputeGazeJointAlignment(Vector3 srcDir, Vector3 trgDir, Vector3 trgDirMin, Vector3 trgDirAlign)
    {
        if (srcDir == trgDir)
            return 0f;

        float align = 0f;
        
        // Rotational plane normal
        Vector3 n = Vector3.Cross(srcDir, trgDir);

        if (srcDir == -trgDirAlign)
        {
            align = 1f;
        }
        else
        {
            float sa = Mathf.Sign(Vector3.Dot(Vector3.Cross(srcDir, trgDirAlign), n));

            if (sa > 0f)
            {
                align = trgDirMin != trgDir ?
                    Vector3.Angle(trgDirMin, trgDirAlign) /
                    Vector3.Angle(trgDirMin, trgDir) : 0f;
                align = Mathf.Clamp01(align);
            }
        }

        return align;
    }
}

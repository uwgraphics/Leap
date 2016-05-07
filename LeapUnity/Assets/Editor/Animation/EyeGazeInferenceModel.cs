using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

/// <summary>
/// Class representing a model for inferring eye gaze behavior from body animation.
/// </summary>
public class EyeGazeInferenceModel
{
    private enum _EyeGazeIntervalType
    {
        GazeShift,
        GazeFixation,
        Unknown
    }

    // Defines an interval in the body animation that corresponds to a single gaze shift or fixation
    private struct _EyeGazeInterval
    {
        public KeyFrameSet startKeyFrameSet;
        public KeyFrameSet endKeyFrameSet;
        public _EyeGazeIntervalType intervalType;
        public float pGS, pGF;

        public _EyeGazeInterval(KeyFrameSet startKeyFrameSet, KeyFrameSet endKeyFrameSet,
            _EyeGazeIntervalType intervalType, float pGS, float pGF)
        {
            this.startKeyFrameSet = startKeyFrameSet;
            this.endKeyFrameSet = endKeyFrameSet;
            this.intervalType = intervalType;
            this.pGS = pGS;
            this.pGF = pGF;
        }
    }

    /// <summary>
    /// Character model.
    /// </summary>
    public GameObject Model
    {
        get;
        private set;
    }

    /// <summary>
    /// Root object of the environment.
    /// </summary>
    public GameObject Environment
    {
        get;
        private set;
    }

    /// <summary>
    /// Inference submodel for gaze targets.
    /// </summary>
    public EyeGazeTargetInferenceModel TargetInferenceModel
    {
        get;
        private set;
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="model">Character model</param>
    /// <param name="env">Environment root object</param>
    public EyeGazeInferenceModel(GameObject model, GameObject env)
    {
        this.Model = model;
        this.Environment = env;
        this.TargetInferenceModel = new EyeGazeTargetInferenceModel(this, model, env);
    }

    /// <summary>
    /// Analyze a base body animation to infer an eye gaze behavior that matches it.
    /// </summary>
    /// <param name="timeline">Animation timeline</param>
    /// <param name="baseAnimationInstanceId">Base animation instance ID</param>
    /// <param name="layerName">Gaze animation layer name</param>
    public void InferEyeGazeInstances(AnimationTimeline timeline, int baseAnimationInstanceId,
        string layerName = "Gaze", string envLayerName = "Environment")
    {
        // Timers for measuring inference duration
        System.Diagnostics.Stopwatch timerInstances = new System.Diagnostics.Stopwatch();
        System.Diagnostics.Stopwatch timerTargets = new System.Diagnostics.Stopwatch();
        System.Diagnostics.Stopwatch timerAlignments = new System.Diagnostics.Stopwatch();
        

        // Perform gaze inference
        timerInstances.Start();
        InferEyeGazeTimings(timeline, baseAnimationInstanceId, layerName);
        timerInstances.Stop();
        timerTargets.Start();
        TargetInferenceModel.InferTargets(timeline, baseAnimationInstanceId, layerName, envLayerName);
        TargetInferenceModel.DestroyResources();
        timerTargets.Stop();
        timerAlignments.Start();
        InferEyeGazeAlignments(timeline, baseAnimationInstanceId, layerName);
        timerAlignments.Stop();

        // Show inference duration
        float elapsedTime = (timerInstances.ElapsedMilliseconds + timerTargets.ElapsedMilliseconds + timerAlignments.ElapsedMilliseconds) / 1000f;
        float elapsedTimeInstances = timerInstances.ElapsedMilliseconds / 1000f;
        float elapsedTimeTargets = timerTargets.ElapsedMilliseconds / 1000f;
        float elapsedTimeAlignments = timerAlignments.ElapsedMilliseconds / 1000f;
        Debug.Log(string.Format("Gaze inference complete in {0} s: {1} s for instances, {2} for targets, {3} for alignments",
            elapsedTime, elapsedTimeInstances, elapsedTimeTargets, elapsedTimeAlignments));
    }

    /// <summary>
    /// Infer gaze shift-fixation sequence for the specified base body animation.
    /// </summary>
    /// <param name="timeline">Animation timeline</param>
    /// <param name="baseAnimationInstanceId">Base animation instance ID</param>
    /// <param name="layerName">Gaze animation layer name</param>
    public void InferEyeGazeTimings(AnimationTimeline timeline, int baseAnimationInstanceId,
        string layerName = "Gaze")
    {
        Debug.Log("Inferring gaze instances and their timings...");

        // Clear prior gaze instances
        var baseInstance = timeline.GetAnimation(baseAnimationInstanceId) as AnimationClipInstance;
        var model = baseInstance.Model;
        timeline.RemoveAllAnimations(layerName, model.name);

        // Get model bones and controllers
        var root = ModelUtil.FindRootBone(model);
        var bones = ModelUtil.GetAllBones(model);
        var gazeController = model.GetComponent<GazeController>();
        
        // Extract keyframes that signify likely gaze shift starts and ends
        var gazeKeyFrameExtractor = new EyeGazeKeyFrameExtractor(model, baseInstance.AnimationClip);
        var gazeKeyFrames = gazeKeyFrameExtractor.ExtractKeyFrames();

        // Get head facing directions
        Vector3[] headDirections = new Vector3[baseInstance.FrameLength];
        for (int frameIndex = 0; frameIndex < baseInstance.FrameLength; ++frameIndex)
        {
            baseInstance.Apply(frameIndex, AnimationLayerMode.Override);
            headDirections[frameIndex] = gazeController.head.Direction;
        }

        // Smooth gaze joint velocities
        float[,] vBones = new float[bones.Length, baseInstance.FrameLength];
        for (int boneIndex = 0; boneIndex < bones.Length; ++boneIndex)
        {
            if (!gazeKeyFrameExtractor.BoneMask[boneIndex])
                continue;

            var data0 = CollectionUtil.GetRow<float>(gazeKeyFrameExtractor._KinematicFeatures.vBones, boneIndex);
            var data = new float[data0.Length];
            if (LEAPCore.gazeInferenceUseBilateralFilter)
                FilterUtil.BilateralFilter(data0, data, LEAPCore.gazeInferenceLowPassKernelSize,
                    LEAPCore.gazeInferenceBilateralFilterSpace, LEAPCore.gazeInferenceBilateralFilterRange);
            else
                FilterUtil.Filter(data0, data, FilterUtil.GetTentKernel1D(LEAPCore.gazeInferenceLowPassKernelSize));
            CollectionUtil.SetRow<float>(vBones, boneIndex, data);
        }

        // Classify gaze intervals
        var gazeIntervals = new List<_EyeGazeInterval>();
        float[] wBones = gazeKeyFrameExtractor.ComputeBoneWeights(); // weights for per-joint probability terms
        for (int gazeIntervalIndex = -1; gazeIntervalIndex < gazeKeyFrames.Length; ++gazeIntervalIndex)
        {
            // Get classification for the current candidate interval
            var startKeyFrameSet = gazeIntervalIndex < 0 ? new KeyFrameSet(model) : gazeKeyFrames[gazeIntervalIndex];
            var endKeyFrameSet = gazeIntervalIndex >= gazeKeyFrames.Length - 1 ?
                new KeyFrameSet(model, baseInstance.FrameLength - 1) : gazeKeyFrames[gazeIntervalIndex + 1];
            var gazeInterval = _GetClassifiedEyeGazeInterval(startKeyFrameSet, endKeyFrameSet, bones, gazeKeyFrameExtractor.BoneMask,
                wBones, vBones, headDirections);

            if (gazeIntervals.Count > 0)
            {
                var prevGazeInterval = gazeIntervals[gazeIntervals.Count - 1];
                if (gazeInterval.intervalType == prevGazeInterval.intervalType)
                {
                    // Merge adjacent intervals
                    if (gazeInterval.intervalType == _EyeGazeIntervalType.GazeFixation)
                    {
                        // Compute interval weights
                        float weightPrev = ((float)(prevGazeInterval.endKeyFrameSet.keyFrame - prevGazeInterval.startKeyFrameSet.keyFrame)) /
                            (gazeInterval.endKeyFrameSet.keyFrame - prevGazeInterval.startKeyFrameSet.keyFrame);
                        float weight = ((float)(gazeInterval.endKeyFrameSet.keyFrame - gazeInterval.startKeyFrameSet.keyFrame)) /
                            (gazeInterval.endKeyFrameSet.keyFrame - prevGazeInterval.startKeyFrameSet.keyFrame);

                        // Merge adjacent gaze fixations
                        gazeInterval.pGS = weightPrev * prevGazeInterval.pGS + weight * gazeInterval.pGS;
                        gazeInterval.pGF = weightPrev * prevGazeInterval.pGF + weight * gazeInterval.pGF;
                        gazeInterval.startKeyFrameSet = prevGazeInterval.startKeyFrameSet;
                        gazeIntervals[gazeIntervals.Count - 1] = gazeInterval;
                    }
                    else if (gazeInterval.intervalType == _EyeGazeIntervalType.GazeShift)
                    {
                        // Merge adjacent gaze shifts?
                        /*var mergedGazeInterval = _GetClassifiedEyeGazeInterval(prevGazeInterval.startKeyFrameSet, gazeInterval.endKeyFrameSet,
                            bones, gazeKeyFrameExtractor.BoneMask, wBones, vBones, headDirections);
                        if (mergedGazeInterval.pGS > prevGazeInterval.pGS && mergedGazeInterval.pGS > gazeInterval.pGS)
                            gazeIntervals[gazeIntervals.Count - 1] = mergedGazeInterval;
                        else*/
                        gazeIntervals.Add(gazeInterval);
                    }
                }
                else
                    gazeIntervals.Add(gazeInterval);
            }
            else
                gazeIntervals.Add(gazeInterval);
        }

        // Write out gaze interval properties
        var csvGazeIntervals = new CSVDataFile();
        csvGazeIntervals.AddAttribute("intervalType", typeof(string));
        csvGazeIntervals.AddAttribute("startFrame", typeof(int));
        csvGazeIntervals.AddAttribute("endFrame", typeof(int));
        csvGazeIntervals.AddAttribute("amplitude", typeof(float));
        csvGazeIntervals.AddAttribute("pGS", typeof(float));
        csvGazeIntervals.AddAttribute("pGF", typeof(float));
        for (int gazeIntervalIndex = 0; gazeIntervalIndex < gazeIntervals.Count - 1; ++gazeIntervalIndex)
        {
            var gazeInterval = gazeIntervals[gazeIntervalIndex];

            int startFrame = gazeInterval.startKeyFrameSet.keyFrame;
            int endFrame = gazeInterval.endKeyFrameSet.keyFrame;
            baseInstance.Apply(startFrame, AnimationLayerMode.Override);
            var srcDir = gazeController.head.Direction;
            baseInstance.Apply(endFrame, AnimationLayerMode.Override);
            var trgDir = gazeController.head.Direction;
            float a = Vector3.Angle(srcDir, trgDir);
            float pGS = gazeInterval.pGS;
            float pGF = gazeInterval.pGF;

            csvGazeIntervals.AddData(gazeInterval.intervalType.ToString(), startFrame, endFrame, a, pGS, pGF);
        }
        csvGazeIntervals.WriteToFile("../Matlab/KeyExtraction/gazeIntervals#" + baseInstance.Name + ".csv");

        // Generate gaze instances
        int gazeInstanceIndex = 1;
        for (int gazeIntervalIndex = 0; gazeIntervalIndex < gazeIntervals.Count; ++gazeIntervalIndex)
        {
            var gazeInterval = gazeIntervals[gazeIntervalIndex];
            if (gazeInterval.intervalType != _EyeGazeIntervalType.GazeShift)
                continue;

            // Determine gaze shift and fixation start frames
            int startFrame = gazeInterval.startKeyFrameSet.keyFrame;
            int fixationStartFrame = gazeInterval.endKeyFrameSet.keyFrame;
            /*for (int boneIndex = 0; boneIndex < bones.Length; ++boneIndex)
            {
                if (!gazeKeyFrameExtractor.BoneMask[boneIndex])
                    continue;

                startFrame = Mathf.Min(startFrame, gazeInterval.startKeyFrameSet.boneKeyFrames[boneIndex]);
                fixationStartFrame = Mathf.Max(fixationStartFrame, gazeInterval.endKeyFrameSet.boneKeyFrames[boneIndex]);
            }*/

            // Make gaze shift start earlier because eyes normally start first
            startFrame = Mathf.Max(0, startFrame - 1);

            // Determine gaze fixation end frame
            int endFrame = fixationStartFrame;
            /*if (gazeIntervalIndex + 1 < gazeIntervals.Count)
            {
                var nextGazeInterval = gazeIntervals[gazeIntervalIndex + 1];
                if (nextGazeInterval.intervalType == _EyeGazeIntervalType.GazeFixation)
                {
                    endFrame = nextGazeInterval.endKeyFrameSet.keyFrame;
                    for (int boneIndex = 0; boneIndex < bones.Length; ++boneIndex)
                    {
                        if (!gazeKeyFrameExtractor.BoneMask[boneIndex])
                            continue;

                        endFrame = Mathf.Max(endFrame, nextGazeInterval.endKeyFrameSet.boneKeyFrames[boneIndex]);
                    }
                }
            }*/

            // Add new eye gaze instance
            var gazeInstance = new EyeGazeInstance(baseInstance.AnimationClip.name + "Gaze" + gazeInstanceIndex,
                model, endFrame - startFrame + 1, fixationStartFrame - startFrame + 1, null, 0f, 0f, true,
                baseInstance.AnimationClip, null);
            EyeGazeEditor.AddEyeGaze(timeline, gazeInstance, startFrame, layerName);
            ++gazeInstanceIndex;
        }
    }

    // For the give range of frames, determine whether it is a gaze shift or fixation interval,
    // and the probability of each
    private _EyeGazeInterval _GetClassifiedEyeGazeInterval(KeyFrameSet startKeyFrameSet, KeyFrameSet endKeyFrameSet,
        Transform[] bones, BitArray boneMask, float[] wBones, float[,] vBones, Vector3[] headDirections)
    {
        // Compute per-joint gaze shift and fixation probabilities
        float[] pGSVBones = new float[bones.Length];
        float[] pGFVBones = new float[bones.Length];
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
                pGSVBones[boneIndex] = 0f;
                pGFVBones[boneIndex] = 1f;
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
            pGSVBones[boneIndex] = Mathf.Clamp01(2f /
                (1f + Mathf.Exp(-LEAPCore.gazeInferenceVelocityLogisticSlope * rGS)) - 1f);
            pGFVBones[boneIndex] = Mathf.Clamp01(2f /
                (1f + Mathf.Exp(-LEAPCore.gazeInferenceVelocityLogisticSlope * rGF)) - 1f);
        }

        // Compute velocity term of gaze shift probability
        float pGSV = 0f, pGFV = 0f;
        float sumWBones = wBones.Sum();
        for (int boneIndex = 0; boneIndex < bones.Length; ++boneIndex)
        {
            if (!boneMask[boneIndex])
                continue;

            pGSV += (wBones[boneIndex] * pGSVBones[boneIndex]);
            pGFV += (wBones[boneIndex] * pGFVBones[boneIndex]);
        }
        pGSV /= sumWBones;
        pGFV /= sumWBones;

        // Compute amplitude term of gaze shift probability
        float amplitude = Vector3.Angle(headDirections[startKeyFrameSet.keyFrame], headDirections[endKeyFrameSet.keyFrame]);
        float pGSA = 1f - Mathf.Exp(-LEAPCore.gazeInferenceAmplitudeExpRate * amplitude / 90f);
        float pGFA = 1f - pGSA;
            
        // Compute total gaze shift probability
        float pGS = (1f - LEAPCore.gazeInferenceAmplitudeWeight) * pGSV + LEAPCore.gazeInferenceAmplitudeWeight * pGSA;
        float pGF = (1f - LEAPCore.gazeInferenceAmplitudeWeight) * pGFV + LEAPCore.gazeInferenceAmplitudeWeight * pGFA;

        // Classify and create gaze interval
        var gazeIntervalType = pGS > pGF && pGS >= LEAPCore.gazeInferenceMinGazeShiftP ?
            _EyeGazeIntervalType.GazeShift : _EyeGazeIntervalType.GazeFixation;
        var gazeInterval = new _EyeGazeInterval(startKeyFrameSet, endKeyFrameSet, gazeIntervalType, pGS, pGF);

        return gazeInterval;
    }

    /// <summary>
    /// Infer gaze shift alignment parameter values.
    /// </summary>
    /// <param name="timeline">Animation timeline</param>
    /// <param name="baseAnimationInstanceId">Base animation instance ID</param>
    /// <param name="layerName">Gaze animation layer name</param>
    public void InferEyeGazeAlignments(AnimationTimeline timeline, int baseAnimationInstanceId,
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
    private void _InferEyeGazeAlignments(AnimationTimeline timeline, int baseAnimationInstanceId, int instanceId)
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
    private float _ComputeGazeJointAlignment(Vector3 srcDir, Vector3 trgDir, Vector3 trgDirMin, Vector3 trgDirAlign)
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

    /// <summary>
    /// Evaluate accuracy of gaze shift/fixation inference.
    /// </summary>
    /// <param name="timeline">Animation timeline</param>
    /// <param name="baseInstanceId">Base animation instance ID</param>
    public static void EvaluateInstances(AnimationTimeline timeline, int baseInstanceId)
    {
        var baseInstance = timeline.GetAnimation(baseInstanceId) as AnimationClipInstance;
        var model = baseInstance.Model;
        var gazeLayer = timeline.GetLayer(LEAPCore.eyeGazeAnimationLayerName);

        Debug.Log("Evaluating gaze shift inference performance for " + baseInstance.Name + "...");

        // Load ground-truth gaze shift annotations
        string path = FileUtil.MakeFilePath(LEAPCore.eyeTrackDataDirectory, baseInstance.Name + "#GazeShifts.csv");
        var data = new CSVDataFile();
        data.AddAttribute("EventType", typeof(string));
        data.AddAttribute("StartFrame", typeof(int));
        data.AddAttribute("EndFrame", typeof(int));
        data.ReadFromFile(path);
        if (data.NumberOfRows <= 0)
            // No ground-truth gaze shift data
            return;

        // Compute content frame range
        int baseStartFrameIndex = data.NumberOfRows > 0 ? data[0].GetValue<int>(1) : 0;
        int baseEndFrameIndex = data.NumberOfRows > 0 ? data[data.NumberOfRows - 1].GetValue<int>(2) : timeline.FrameLength - 1;

        // Count accurately inferred gaze instances
        int numCorrect = 0, numMissed = 0;
        for (int rowIndex = 0; rowIndex < data.NumberOfRows; ++rowIndex)
        {
            string eventType = data[rowIndex].GetValue<string>(0);
            if (eventType == "Other")
                continue;

            // Get gaze shift start and end frames
            int startFrameIndex = data[rowIndex].GetValue<int>(1);
            int fixationStartFrameIndex = data[rowIndex].GetValue<int>(2);

            // Find match for current ground-truth gaze shift
            bool matchFound = false;
            foreach (var gazeInstance in gazeLayer.Animations)
            {
                if ((gazeInstance.Animation as EyeGazeInstance).Target == null)
                    continue;

                int matchStartFrameIndex = Mathf.Max(startFrameIndex, gazeInstance.StartFrame);
                int matchFixationStartFrameIndex = Mathf.Min(fixationStartFrameIndex,
                    gazeInstance.StartFrame + (gazeInstance.Animation as EyeGazeInstance).FixationStartFrame);

                if (matchFixationStartFrameIndex >= matchStartFrameIndex)
                {
                    float matchGroundTruth = ((float)(matchFixationStartFrameIndex - matchStartFrameIndex + 1)) /
                        (fixationStartFrameIndex - startFrameIndex + 1);
                    float matchInferred = ((float)(matchFixationStartFrameIndex - matchStartFrameIndex + 1)) /
                        ((gazeInstance.Animation as EyeGazeInstance).FixationStartFrame + 1);

                    if (matchGroundTruth >= LEAPCore.gazeInferenceMatchThreshold &&
                        matchInferred >= LEAPCore.gazeInferenceMatchThreshold)
                    {
                        matchFound = true;
                        Debug.Log(string.Format("{0} matches ground-truth gaze shift from {1} to {2}",
                            gazeInstance.Animation.Name, startFrameIndex, fixationStartFrameIndex));
                        break;
                    }
                }
            }

            if (matchFound)
                ++numCorrect;
            else
            {
                ++numMissed;
                Debug.LogWarning(string.Format("No match for ground-truth gaze shift from {0} to {1}",
                    startFrameIndex, fixationStartFrameIndex));
            }
        }

        // Compute instance-based inference performance
        int numInferred = gazeLayer.Animations.Count(inst => inst.EndFrame > baseStartFrameIndex && inst.StartFrame < baseEndFrameIndex
            && (inst.Animation as EyeGazeInstance).Target != null);
        int numWrong = numInferred - numCorrect;
        float sens = ((float)numCorrect) / (numCorrect + numMissed);
        float prec = ((float)numCorrect) / (numCorrect + numWrong);
        float fdr = ((float)numWrong) / (numCorrect + numWrong);
        Debug.Log(string.Format("[Instances] Sensitivity = {0}%, Precision = {1}%, FDR = {2}%", sens * 100f, prec * 100f, fdr * 100f));

        // Count accurately inferred frames
        numCorrect = numMissed = numWrong = 0;
        for (int frameIndex = baseStartFrameIndex; frameIndex <= baseEndFrameIndex; ++frameIndex)
        {
            // Is current frame in a ground-truth gaze shift?
            bool isGazeShift = false;
            for (int rowIndex = 0; rowIndex < data.NumberOfRows; ++rowIndex)
            {
                string eventType = data[rowIndex].GetValue<string>(0);
                if (eventType == "Other")
                    continue;

                // Get gaze shift start and end frames
                int startFrameIndex = data[rowIndex].GetValue<int>(1);
                int fixationStartFrameIndex = data[rowIndex].GetValue<int>(2);

                if (frameIndex >= startFrameIndex && frameIndex <= fixationStartFrameIndex)
                {
                    isGazeShift = true;
                    break;
                }
                else if (frameIndex < startFrameIndex)
                    break;
            }

            // Is current frame in an inferred gaze shift?
            bool isGazeShiftInferred = gazeLayer.Animations.Any(inst => frameIndex >= inst.StartFrame &&
                frameIndex <= inst.StartFrame + (inst.Animation as EyeGazeInstance).FixationStartFrame &&
                (inst.Animation as EyeGazeInstance).Target != null);

            if (isGazeShift && isGazeShiftInferred)
                ++numCorrect;
            else if (isGazeShift && !isGazeShiftInferred)
                ++numMissed;
            else if (!isGazeShift && isGazeShiftInferred)
                ++numWrong;
        }

        // Compute frame-based inference performance
        sens = ((float)numCorrect) / (numCorrect + numMissed);
        prec = ((float)numCorrect) / (numCorrect + numWrong);
        fdr = ((float)numWrong) / (numCorrect + numWrong);
        Debug.Log(string.Format("[Instances] Sensitivity = {0}%, Precision = {1}%, FDR = {2}%", sens * 100f, prec * 100f, fdr * 100f));
    }

    /// <summary>
    /// Evaluate accuracy of gaze target direction inference.
    /// </summary>
    /// <param name="timeline">Animation timeline</param>
    /// <param name="baseInstanceId">Base animation instance ID</param>
    public static void EvaluateTargetDirections(AnimationTimeline timeline, int baseInstanceId)
    {
        var baseInstance = timeline.GetAnimation(baseInstanceId) as AnimationClipInstance;
        var model = baseInstance.Model;
        var gazeController = model.GetComponent<GazeController>();
        var lEye = gazeController.lEye.Top;
        var rEye = gazeController.rEye.Top;
        var head = gazeController.head.Top;
        int frameLength = baseInstance.FrameLength;
        var envLayer = timeline.GetLayer("Environment");

        // TODO: make sure we are applying base animation with gaze edits baked in

        Debug.Log(string.Format("Evaluating gaze target direction inference accuracy for {0}...", baseInstance.Name));

        // Load eye tracking data
        var eyeTrackData = new EyeTrackData(model, baseInstance.AnimationClip);
        eyeTrackData.InitAlignEyeRotations(timeline, Quaternion.Euler(0f, 0f, 90f)); // TODO: this will only work for NormanNew and its ilk
        Quaternion lEyeAlignRot = eyeTrackData.LEyeAlignRotation;
        Quaternion rEyeAlignRot = eyeTrackData.REyeAlignRotation;

        // Compute per-frame error in gaze directions
        var outCsvData = new CSVDataFile();
        outCsvData.AddAttribute("gazeDirectionDiff", typeof(float));
        outCsvData.AddAttribute("baseGazeDirectionDiff", typeof(float));
        float meanDiff = 0f, meanBaseDiff = 0f;
        for (int frameIndex = 0; frameIndex < timeline.FrameLength; ++frameIndex)
        {
            // Apply animation at current frame
            timeline.GoToFrame(frameIndex);
            timeline.ApplyAnimation();

            // Get eye gaze directions in the base animation
            var dle1 = lEye.InverseTransformDirection(gazeController.lEye.Direction).normalized;
            var dre1 = rEye.InverseTransformDirection(gazeController.rEye.Direction).normalized;

            // Get straight-ahead gaze directions
            gazeController.lEye.Yaw = gazeController.lEye.Pitch =
                gazeController.rEye.Yaw = gazeController.rEye.Pitch = 0f;
            var dle2 = lEye.InverseTransformDirection(gazeController.lEye.Direction).normalized;
            var dre2 = rEye.InverseTransformDirection(gazeController.rEye.Direction).normalized;

            // Get eye gaze directions in the eye tracking data
            var dle0 = eyeTrackData.Samples[frameIndex + eyeTrackData.FrameOffset].lEyeDirection;
            var dre0 = eyeTrackData.Samples[frameIndex + eyeTrackData.FrameOffset].rEyeDirection;
            dle0 = lEyeAlignRot * dle0;
            dre0 = lEyeAlignRot * dre0;

            // Compute averaged gaze directions
            var de1 = 0.5f * (dle1 + dre1);
            var de2 = 0.5f * (dle2 + dre2);
            var de0 = 0.5f * (dle0 + dre0);

            // Compute error in gaze direction
            var gazeDirectionDiff = QuaternionUtil.Angle(Quaternion.FromToRotation(de0, de1));
            meanDiff += gazeDirectionDiff;
            var baseGazeDirectionDiff = QuaternionUtil.Angle(Quaternion.FromToRotation(de0, de2));
            meanBaseDiff += baseGazeDirectionDiff;

            outCsvData.AddData(gazeDirectionDiff, baseGazeDirectionDiff);
        }
        outCsvData.WriteToFile("../Matlab/EyeGazeInference/gazeDirectionAccuracy#" + baseInstance.Name + ".csv");
        meanDiff /= timeline.FrameLength;
        meanBaseDiff /= timeline.FrameLength;

        Debug.Log(string.Format("Mean difference: {0}; mean base difference: {1}", meanDiff, meanBaseDiff));
    }

    /// <summary>
    /// Evaluate accuracy of gaze target inference.
    /// </summary>
    /// <param name="timeline">Animation timeline</param>
    /// <param name="baseInstanceId">Base animation instance ID</param>
    public static void EvaluateTargets(AnimationTimeline timeline, int baseInstanceId, string[] targetNames = null)
    {
        var baseInstance = timeline.GetAnimation(baseInstanceId) as AnimationClipInstance;
        var model = baseInstance.Model;
        var gazeLayer = timeline.GetLayer(LEAPCore.eyeGazeAnimationLayerName);

        Debug.Log("Evaluating gaze target inference accuracy for " + baseInstance.Name + "...");

        // Load eye tracking data
        var eyeTrackData = new EyeTrackData(model, baseInstance.AnimationClip);
        int frameOffset = eyeTrackData.FrameOffset;

        // Load ground-truth gaze target annotations
        string path = FileUtil.MakeFilePath(LEAPCore.eyeTrackDataDirectory, baseInstance.Name + "#GazeTargets.csv");
        var data = new CSVDataFile();
        data.AddAttribute("Target", typeof(string));
        data.AddAttribute("Frame", typeof(int));
        data.ReadFromFile(path);
        if (data.NumberOfRows <= 0)
            // No ground-truth gaze target data
            return;

        // Get ground-truth targets
        var fixationFrames = new List<int>();
        var fixationTargets = new List<string>();
        for (int rowIndex = 0; rowIndex < data.NumberOfRows; ++rowIndex)
        {
            string targetName = data[rowIndex].GetValue<string>(0);
            int frameIndex = data[rowIndex].GetValue<int>(1) - 1 - frameOffset;
            fixationFrames.Add(frameIndex);
            fixationTargets.Add(targetName);
        }
        var objTargets = new HashSet<string>(fixationTargets.Distinct().Where(t => t != "Background"));

        // Count accurately inferred fixation targets
        int numAllFixations = 0;
        int numAllCorrect = 0;
        int numObjFixations = 0;
        int numObjCorrect = 0;
        foreach (var scheduledGazeInstance in gazeLayer.Animations)
        {
            var gazeInstance = scheduledGazeInstance.Animation as EyeGazeInstance;
            int startFrame = scheduledGazeInstance.StartFrame;
            int endFrame = scheduledGazeInstance.EndFrame;

            // Get all ground-truth fixations that overlap with the current gaze instance
            var overlappingTargets = new List<string>();
            for (int fixationIndex = 0; fixationIndex < fixationFrames.Count; ++fixationIndex)
            {
                int fixationStartFrame = fixationIndex > 0 ? fixationFrames[fixationIndex - 1] : fixationFrames[fixationIndex];
                int fixationEndFrame = fixationIndex < fixationFrames.Count - 1 ? fixationFrames[fixationIndex + 1] : fixationFrames[fixationIndex];
                if (Mathf.Max(startFrame, fixationStartFrame) <= Mathf.Min(endFrame, fixationEndFrame))
                    overlappingTargets.Add(fixationTargets[fixationIndex]);
            }
            if (overlappingTargets.Count <= 0)
                // No ground-truth data for this part of the animation
                continue;
            else
                ++numAllFixations;

            // Is it a character?
            bool isCharacter = false;
            GameObject characterModel = null;
            if (gazeInstance.Target != null)
            {
                var target = gazeInstance.Target.transform;
                while (target != null)
                {
                    if (target.tag == "Agent")
                    {
                        isCharacter = true;
                        characterModel = target.gameObject;
                        ++numObjFixations;
                        break;
                    }

                    target = target.parent;
                }
            }

            // Is it a torso or head target?
            bool isTorso = false;
            bool isHead = false;
            if (isCharacter)
            {
                // Get head and torso height
                var headBones =  ModelUtil.GetAllBonesWithTag(characterModel, "HeadBone");
                float torsoHeight = 0f;
                float headHeight = float.MaxValue;
                foreach (var curHeadBone in headBones)
                {
                    if (curHeadBone.transform.position.y > torsoHeight)
                        torsoHeight = curHeadBone.transform.position.y;

                    if (curHeadBone.transform.position.y < headHeight)
                        headHeight = curHeadBone.transform.position.y;
                }
                isTorso = gazeInstance.Target.transform.position.y <= torsoHeight;
                isHead = gazeInstance.Target.transform.position.y >= headHeight;
            }

            // Is it a background target?
            bool isBackground = !isCharacter;
            if (gazeInstance.Target != null && !isCharacter)
            {
                var target = gazeInstance.Target.transform;
                while (target != null)
                {
                    if (objTargets.Any(t => target.name.StartsWith(t)))
                    {
                        isBackground = false;
                        ++numObjFixations;
                        break;
                    }

                    target = target.parent;
                }
            }

            // Is the gaze instance toward a target specified in ground-truth data?
            bool isSameTarget = false;
            foreach (var overlappingTarget in overlappingTargets)
            {
                if (isBackground)
                {
                    if (overlappingTarget == "Background")
                        isSameTarget = true;
                }
                else if (isCharacter)
                {
                    if (isHead && overlappingTarget == (characterModel.name + "Head") ||
                        isTorso && overlappingTarget == (characterModel.name + "Torso"))
                        isSameTarget = true;
                }
                else
                {
                    if (gazeInstance.Target != null)
                    {
                        var target = gazeInstance.Target.transform;
                        while (target != null)
                        {
                            if (target.name.StartsWith(overlappingTarget))
                            {
                                isSameTarget = true;
                                break;
                            }

                            target = target.parent;
                        }
                    }
                }

                if (isSameTarget)
                    break;
            }

            if (isSameTarget)
            {
                ++numAllCorrect;
                if (!isBackground)
                    ++numObjCorrect;
            }
            else
            {
                string targetName = gazeInstance.Target != null ? gazeInstance.Target.name : "null";
                Debug.LogWarning(string.Format("Gaze target mismatch for {0}, target {1}", gazeInstance.Name, targetName));
            }
        }

        // Get ground-truth targets
        /*var frameTargetPairs = new Dictionary<int, string>();
        var targets = new List<string>();
        for (int rowIndex = 0; rowIndex < data.NumberOfRows; ++rowIndex)
        {
            string targetName = data[rowIndex].GetValue<string>(0);
            int frameIndex = data[rowIndex].GetValue<int>(1) - 1 - frameOffset;
            if (!frameTargetPairs.ContainsKey(frameIndex))
                frameTargetPairs.Add(frameIndex, targetName);
            else
                Debug.LogError(string.Format("Gaze target already defined for frame {0}", frameIndex));
            targets.Add(targetName);
        }
        var objTargets = new HashSet<string>(targets.Distinct().Where(t => t != "Background"));

        // Count accurately inferred fixation targets
        int numAllFixations = 0;
        int numAllCorrect = 0;
        int numObjFixations = 0;
        int numObjCorrect = 0;
        foreach (var scheduledGazeInstance in gazeLayer.Animations)
        {
            var gazeInstance = scheduledGazeInstance.Animation as EyeGazeInstance;
            int startFrame = scheduledGazeInstance.StartFrame;
            int endFrame = scheduledGazeInstance.EndFrame;

            // Get all ground-truth fixations that overlap with the current gaze instance
            bool isSameTarget = false;
            var overlappingFrameTargetPairs = frameTargetPairs.Where(ftp => startFrame <= ftp.Key && ftp.Key <= endFrame);
            if (overlappingFrameTargetPairs.Count() <= 0)
                // No ground-truth data for this part of the animation
                continue;
            else
                ++numAllFixations;

            // Is it a background target?
            bool isBackground = true;
            if (gazeInstance.Target != null)
            {
                var target = gazeInstance.Target.transform;
                while (target != null)
                {
                    if (objTargets.Any(t => target.name.StartsWith(t)))
                    {
                        isBackground = false;
                        ++numObjFixations;
                        break;
                    }

                    target = target.parent;
                }
            }

            // Is the gaze instance toward a target specified in ground-truth data?
            foreach (var frameTargetPair in overlappingFrameTargetPairs)
            {
                if (isBackground)
                {
                    if (frameTargetPair.Value == "Background")
                        isSameTarget = true;
                }
                else
                {
                    if (gazeInstance.Target != null)
                    {
                        var target = gazeInstance.Target.transform;
                        while (target != null)
                        {
                            if (target.name.StartsWith(frameTargetPair.Value))
                            {
                                isSameTarget = true;
                                break;
                            }

                            target = target.parent;
                        }
                    }
                }

                if (isSameTarget)
                    break;
            }

            if (isSameTarget)
            {
                ++numAllCorrect;
                if (!isBackground)
                    ++numObjCorrect;
            }
            else
            {
                string targetName = gazeInstance.Target != null ? gazeInstance.Target.name : "null";
                Debug.LogWarning(string.Format("Gaze target mismatch for {0}, target {1}", gazeInstance.Name, targetName));
            }
        }*/

        // Compute inference accuracy
        float accAll = ((float)numAllCorrect) / numAllFixations;
        Debug.Log(string.Format("Gaze targets (all) for {0} inferred with accuracy {1}%", baseInstance.Name, accAll * 100f));
        float accObj = ((float)numObjCorrect) / numObjFixations;
        Debug.Log(string.Format("Gaze targets (non-background) for {0} inferred with accuracy {1}%", baseInstance.Name, accObj * 100f));
    }

    /// <summary>
    /// Extract frames from eye tracker video that correspond to distinct gaze fixations.
    /// </summary>
    /// <param name="timeline">Animation timeline</param>
    /// <param name="baseInstanceId">Base animation instance ID</param>
    /// <param name="framePath">Eye tracker video input frame path</param>
    /// <param name="outFramePath">Eye tracker video output frame path</param>
    /// <param name="startFrame">Start frame index</param>
    /// <param name="endFrame">End frame index</param>
    public static void ExtractEyeTrackFixationFrames(AnimationTimeline timeline, int baseInstanceId,
        string framePath, string outFramePath, int startFrame = 0, int endFrame = -1)
    {
        if (!Directory.Exists(framePath))
            throw new DirectoryNotFoundException("Frame path: " + framePath);
        if (!Directory.Exists(outFramePath))
            throw new DirectoryNotFoundException("Output frame path: " + outFramePath);
        if (Directory.GetFiles(outFramePath).Length > 0)
        {
            Debug.LogWarning("Eye track fixation frames already extracted at " + outFramePath);
            return;
        }

        var baseInstance = timeline.GetAnimation(baseInstanceId) as AnimationClipInstance;
        var model = baseInstance.Model;
        
        // Use eye tracking data to extract fixation frames
        var eyeTrackData = new EyeTrackData(model, baseInstance.AnimationClip);
        int frameLength = eyeTrackData.Samples.Count;
        startFrame = Mathf.Clamp(startFrame, 0, frameLength - 1);
        endFrame = endFrame < 0 ? frameLength - 1 : Mathf.Clamp(endFrame, startFrame, frameLength - 1);
        foreach (var eyeGazeEvent in eyeTrackData.Events)
        {
            if (eyeGazeEvent.eventType != EyeTrackEventType.Fixation)
                continue;

            int frameIndex = eyeGazeEvent.startFrame + eyeGazeEvent.frameLength / 2;
            if (frameIndex < startFrame || frameIndex > endFrame)
                continue;

            string frameFilename = string.Format("frame{0:D5}.png", frameIndex + 1);
            File.Copy(FileUtil.MakeFilePath(framePath, frameFilename), FileUtil.MakeFilePath(outFramePath, frameFilename));
        }
    }
}

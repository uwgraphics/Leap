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
        public float p;

        public _EyeGazeInterval(KeyFrameSet startKeyFrameSet, KeyFrameSet endKeyFrameSet,
            _EyeGazeIntervalType intervalType, float p)
        {
            this.startKeyFrameSet = startKeyFrameSet;
            this.endKeyFrameSet = endKeyFrameSet;
            this.intervalType = intervalType;
            this.p = p;
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
        // Timer for measuring inference duration
        System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
        timer.Start();

        InferEyeGazeTimings(timeline, baseAnimationInstanceId, layerName);
        TargetInferenceModel.InferTargets(timeline, baseAnimationInstanceId, layerName, envLayerName);
        TargetInferenceModel.DestroyResources();
        InferEyeGazeAlignments(timeline, baseAnimationInstanceId, layerName);

        // Show inference duration
        float elapsedTime = timer.ElapsedMilliseconds / 1000f;
        Debug.Log(string.Format("Gaze inference complete in {0} seconds", elapsedTime));
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
            FilterUtil.Filter(data0, data, FilterUtil.GetTentKernel1D(LEAPCore.gazeInferenceLowPassKernelSize));
            CollectionUtil.SetRow<float>(vBones, boneIndex, data);
        }

        // Classify gaze intervals
        var gazeIntervals = new List<_EyeGazeInterval>();
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
                if (!gazeKeyFrameExtractor.BoneMask[boneIndex])
                    continue;

                // Movement magnitude
                float d = QuaternionUtil.Angle(
                    Quaternion.Inverse(gazeKeyFrameExtractor._KinematicFeatures.qBones[boneIndex, startKeyFrameSet.boneKeyFrames[boneIndex]]) *
                    gazeKeyFrameExtractor._KinematicFeatures.qBones[boneIndex, endKeyFrameSet.boneKeyFrames[boneIndex]]);

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
            float[] pGSVBones = new float[bones.Length];
            float[] pGFVBones = new float[bones.Length];
            for (int boneIndex = 0; boneIndex < bones.Length; ++boneIndex)
            {
                if (!gazeKeyFrameExtractor.BoneMask[boneIndex])
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
                if (!gazeKeyFrameExtractor.BoneMask[boneIndex])
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

            // Classify and add the gaze interval
            var gazeIntervalType = pGS > pGF ? _EyeGazeIntervalType.GazeShift : _EyeGazeIntervalType.GazeFixation;
            gazeIntervals.Add(new _EyeGazeInterval(startKeyFrameSet, endKeyFrameSet, gazeIntervalType,
                gazeIntervalType == _EyeGazeIntervalType.GazeShift ? pGS : pGF));
        }

        // TODO: remove this
        // Write out gaze shift interval properties
        var csvGazeIntervals = new CSVDataFile();
        csvGazeIntervals.AddAttribute("intervalType", typeof(string));
        csvGazeIntervals.AddAttribute("startFrame", typeof(int));
        csvGazeIntervals.AddAttribute("endFrame", typeof(int));
        csvGazeIntervals.AddAttribute("amplitude", typeof(float));
        csvGazeIntervals.AddAttribute("probability", typeof(float));
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
            float p = gazeInterval.p;

            csvGazeIntervals.AddData(gazeInterval.intervalType.ToString(), startFrame, endFrame, a, p);
        }
        csvGazeIntervals.WriteToFile("../Matlab/KeyExtraction/gazeIntervals#" + baseInstance.Name + ".csv");
        //

        // Merge adjacent gaze fixation intervals
        for (int gazeIntervalIndex = 0; gazeIntervalIndex < gazeIntervals.Count - 1; ++gazeIntervalIndex)
        {
            var gazeInterval = gazeIntervals[gazeIntervalIndex];
            var nextGazeInterval = gazeIntervals[gazeIntervalIndex + 1];

            if (gazeInterval.intervalType == _EyeGazeIntervalType.GazeFixation &&
                nextGazeInterval.intervalType == _EyeGazeIntervalType.GazeFixation)
            {
                // Compute merged interval
                float weight = ((float)(gazeInterval.endKeyFrameSet.keyFrame - gazeInterval.startKeyFrameSet.keyFrame)) /
                    (nextGazeInterval.endKeyFrameSet.keyFrame - gazeInterval.startKeyFrameSet.keyFrame);
                float weightNext = ((float)(nextGazeInterval.endKeyFrameSet.keyFrame - nextGazeInterval.startKeyFrameSet.keyFrame)) /
                    (nextGazeInterval.endKeyFrameSet.keyFrame - gazeInterval.startKeyFrameSet.keyFrame);
                gazeInterval.p = weight * gazeInterval.p + weightNext * nextGazeInterval.p;
                gazeInterval.endKeyFrameSet = nextGazeInterval.endKeyFrameSet;

                // Replace the intervals with the merged interval
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
            if (gazeInterval.intervalType != _EyeGazeIntervalType.GazeShift)
                continue;

            // Determine gaze shift and fixation start frames
            int startFrame = gazeInterval.startKeyFrameSet.keyFrame;
            int fixationStartFrame = gazeInterval.endKeyFrameSet.keyFrame;
            for (int boneIndex = 0; boneIndex < bones.Length; ++boneIndex)
            {
                if (!gazeKeyFrameExtractor.BoneMask[boneIndex])
                    continue;

                startFrame = Mathf.Min(startFrame, gazeInterval.startKeyFrameSet.boneKeyFrames[boneIndex]);
                fixationStartFrame = Mathf.Max(fixationStartFrame, gazeInterval.endKeyFrameSet.boneKeyFrames[boneIndex]);
            }

            // Make gaze shift start earlier because eyes normally do
            startFrame = Mathf.Max(0, startFrame - 1);

            // Determine gaze fixation end frame
            int endFrame = fixationStartFrame;
            if (gazeIntervalIndex + 1 < gazeIntervals.Count)
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
            }

            // Add new eye gaze instance
            var gazeInstance = new EyeGazeInstance(baseInstance.AnimationClip.name + "Gaze" + gazeInstanceIndex,
                model, endFrame - startFrame + 1, fixationStartFrame - startFrame + 1, null, 0f, 0f, true,
                baseInstance.AnimationClip, null);
            EyeGazeEditor.AddEyeGaze(timeline, gazeInstance, startFrame, layerName);
            ++gazeInstanceIndex;
        }
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

        Debug.Log("Evaluating gaze shift inference accuracy for " + baseInstance.Name + "...");

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

        // Count accurately inferred gaze instances
        int numInstances = 0;
        int numCorrect = 0;
        for (int rowIndex = 0; rowIndex < data.NumberOfRows; ++rowIndex)
        {
            string eventType = data[rowIndex].GetValue<string>(0);
            if (eventType == "Other")
                continue;
            else
                ++numInstances;

            // Get gaze instance start and end frames
            int startFrameIndex = data[rowIndex].GetValue<int>(1);
            int endFrameIndex = baseInstance.FrameLength - 1;
            for (int nextRowIndex = rowIndex + 1; nextRowIndex < data.NumberOfRows; ++nextRowIndex)
            {
                string nextEventType = data[nextRowIndex].GetValue<string>(0);
                if (nextEventType == "GazeShift")
                {
                    int nextStartFrameIndex = data[nextRowIndex].GetValue<int>(1);
                    endFrameIndex = nextStartFrameIndex - 1;
                    break;
                }
            }

            // Find match for current ground-truth gaze instance
            bool matchFound = false;
            foreach (var gazeInstance in gazeLayer.Animations)
            {
                int matchStartFrameIndex = Mathf.Max(startFrameIndex, gazeInstance.StartFrame);
                int matchEndFrameIndex = Mathf.Min(endFrameIndex, gazeInstance.EndFrame);

                if (matchEndFrameIndex >= matchStartFrameIndex)
                {
                    float matchGroundTruth = ((float)(matchEndFrameIndex - matchStartFrameIndex + 1)) /
                        (endFrameIndex - startFrameIndex + 1);
                    float matchInferred = ((float)(matchEndFrameIndex - matchStartFrameIndex + 1)) /
                        (gazeInstance.EndFrame - gazeInstance.StartFrame + 1);

                    if (matchGroundTruth >= LEAPCore.gazeInferenceMatchThreshold &&
                        matchInferred >= LEAPCore.gazeInferenceMatchThreshold)
                    {
                        matchFound = true;
                        Debug.Log(string.Format("{0} matches ground-truth gaze instance from {1} to {2}",
                            gazeInstance.Animation.Name, startFrameIndex, endFrameIndex));
                    }
                }
            }

            if (matchFound)
                ++numCorrect;
            else
                Debug.LogWarning(string.Format("No match for ground-truth gaze instance from {0} to {1}",
                    startFrameIndex, endFrameIndex));
        }

        // Compute inference accuracy
        float acc = ((float)numCorrect) / numInstances;
        Debug.Log(string.Format("Gaze instances for {0} inferred with accuracy {1}%", baseInstance.Name, acc * 100f));
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

        // Compute gaze direction aligning rotations from eye tracking align points
        Vector3 vle = Vector3.zero;
        Vector3 vre = Vector3.zero;
        int numAlignPoints = 0;
        var markerSets = GameObject.FindGameObjectsWithTag("GazeMarkerSet");
        foreach (var alignPoint in eyeTrackData.AlignPoints)
        {
            // Get marker for the current align point
            GameObject marker = null;
            if (alignPoint.markerSet == "null")
            {
                marker = GameObject.FindGameObjectWithTag(alignPoint.marker);
            }
            else
            {
                var markerSet = markerSets.FirstOrDefault(ms => ms.name == alignPoint.markerSet);
                if (markerSet == null)
                {
                    Debug.LogWarning("Eye tracking align point specifies non-existent marker set " + alignPoint.markerSet);
                    continue;
                }
                marker = GameObject.FindGameObjectsWithTag(alignPoint.marker)
                    .FirstOrDefault(m => m.transform.parent == markerSet.transform);
            }

            if (marker == null)
            {
                Debug.LogWarning("Eye tracking align point specifies non-existent marker " + alignPoint.marker);
                continue;
            }

            ++numAlignPoints;

            // Get eye gaze directions in the base animation
            timeline.GoToFrame(alignPoint.frame - eyeTrackData.FrameOffset);
            timeline.ApplyAnimation();
            var dle1 = lEye.InverseTransformDirection((marker.transform.position - lEye.position)).normalized;
            var dre1 = rEye.InverseTransformDirection((marker.transform.position - rEye.position)).normalized;

            // Get eye gaze directions in the eye tracking data
            var dle0 = eyeTrackData.Samples[alignPoint.frame].lEyeDirection;
            var dre0 = eyeTrackData.Samples[alignPoint.frame].rEyeDirection;

            // Compute aligning rotation
            var qle = Quaternion.FromToRotation(dle0, dle1);
            var qre = Quaternion.FromToRotation(dre0, dre1);
            vle += QuaternionUtil.Log(qle);
            vre += QuaternionUtil.Log(qre);
        }
        vle = (1f / numAlignPoints) * vle;
        vre = (1f / numAlignPoints) * vre;
        var lEyeAlignRot = QuaternionUtil.Exp(vle);
        var rEyeAlignRot = QuaternionUtil.Exp(vre);

        // Compute per-frame error in gaze directions
        var outCsvData = new CSVDataFile();
        outCsvData.AddAttribute("gazeDirectionError", typeof(float));
        for (int frameIndex = 0; frameIndex < timeline.FrameLength; ++frameIndex)
        {
            // Apply animation at current frame
            timeline.GoToFrame(frameIndex);
            timeline.ApplyAnimation();

            // Get eye gaze directions in the base animation
            var dle1 = lEye.InverseTransformDirection(gazeController.lEye.Direction).normalized;
            var dre1 = rEye.InverseTransformDirection(gazeController.rEye.Direction).normalized;

            // Get eye gaze directions in the eye tracking data
            var dle0 = eyeTrackData.Samples[frameIndex + eyeTrackData.FrameOffset].lEyeDirection;
            var dre0 = eyeTrackData.Samples[frameIndex + eyeTrackData.FrameOffset].rEyeDirection;
            dle0 = lEyeAlignRot * dle0;
            dre0 = lEyeAlignRot * dre0;

            // Compute averaged gaze directions
            var de1 = 0.5f * (dle1 + dre1);
            var de0 = 0.5f * (dle0 + dre0);

            // Compute error in gaze direction
            var gazeDirectionError = QuaternionUtil.Angle(Quaternion.FromToRotation(de0, de1));
            outCsvData.AddData(gazeDirectionError);
        }
        outCsvData.WriteToFile("../Matlab/EyeGazeInference/gazeDirectionAccuracy#" + baseInstance.Name + ".csv");
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

        // Count accurately inferred fixation targets
        int numAllFixations = data.NumberOfRows;
        int numAllCorrect = 0;
        int numObjectFixations = 0;
        int numObjectCorrect = 0;
        for (int rowIndex = 0; rowIndex < data.NumberOfRows; ++rowIndex)
        {
            string targetName = data[rowIndex].GetValue<string>(0);
            int frameIndex = data[rowIndex].GetValue<int>(1) - 1 - frameOffset;
            if (targetName != "Background")
                ++numObjectFixations;
            
            // Get corresponding gaze instance
            var scheduledGazeInstance = gazeLayer.Animations.FirstOrDefault(inst => inst.StartFrame <= frameIndex && inst.EndFrame >= frameIndex);
            if (scheduledGazeInstance == null)
            {
                if (targetName == "Background")
                    ++numAllCorrect;

                continue;
            }
            var gazeInstance = scheduledGazeInstance.Animation as EyeGazeInstance;

            // Is the gaze instance toward the same target?
            bool isSameTarget = false;
            if (targetName == "Background")
            {
                isSameTarget = true;

                // Ground-truth target is background, so inferred target should not be one of the targets of interest
                if (targetNames != null && targetNames.Length > 0)
                {
                    if (gazeInstance.Target != null)
                    {
                        var target = gazeInstance.Target.transform;
                        while (target != null)
                        {
                            if (targetNames.Any(tn => tn == target.name))
                            {
                                isSameTarget = false;
                                break;
                            }

                            target = target.parent;
                        }
                    }
                }
            }
            else
            {
                // Ground-truth target is one of the targets of interest, so inferred target should be it
                if (gazeInstance.Target != null)
                {
                    var target = gazeInstance.Target.transform;
                    while (target != null)
                    {
                        if (target.name == targetName)
                        {
                            isSameTarget = true;
                            break;
                        }

                        target = target.parent;
                    }
                }
            }

            if (isSameTarget)
            {
                ++numAllCorrect;
                if (targetName != "Background")
                    ++numObjectCorrect;
            }
            else
                Debug.LogWarning(string.Format("Gaze target mismatch at frame {0}, target {1}", frameIndex, targetName));
        }

        // Compute inference accuracy
        float accAll = ((float)numAllCorrect) / numAllFixations;
        Debug.Log(string.Format("Gaze targets (all) for {0} inferred with accuracy {1}%", baseInstance.Name, accAll * 100f));
        float accObj = ((float)numObjectCorrect) / numObjectFixations;
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

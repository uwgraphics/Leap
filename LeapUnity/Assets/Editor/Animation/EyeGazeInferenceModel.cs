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

        Debug.Log(string.Format("Evaluating target location inference accuracy for {0}...", model.name));

        // Load eye tracking data
        var eyeTrackData = new EyeTrackData(model, baseInstance.AnimationClip);

        // Compute gaze direction aligning rotations from eye tracking align points
        Vector3 vle = Vector3.zero;
        Vector3 vre = Vector3.zero;
        int numAlignPoints = 0;
        var markerSets = GameObject.FindGameObjectsWithTag("GazeMarkerSet");
        foreach (var alignPoint in eyeTrackData.AlignPoints)
        {
            var markerSet = markerSets.FirstOrDefault(ms => ms.name == alignPoint.markerSet);
            if (markerSet == null)
            {
                Debug.LogWarning("Eye tracking align point specifies non-existent marker set " + alignPoint.markerSet);
                continue;
            }

            var marker = GameObject.FindGameObjectsWithTag(alignPoint.marker)
                .FirstOrDefault(m => m.transform.parent == markerSet.transform);
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
        outCsvData.WriteToFile("../Matlab/EyeGazeInference/targetDirectionAccuracy#" + baseInstance.Name + ".csv");

        /*Debug.Log("Getting marker positions...");
            
        // Get marker objects
        var markerSets = GameObject.FindGameObjectsWithTag("GazeMarkerSet");
        var markersUL = GameObject.FindGameObjectsWithTag("GazeMarkerUL");
        var markersUR = GameObject.FindGameObjectsWithTag("GazeMarkerUR");
        var markersLR = GameObject.FindGameObjectsWithTag("GazeMarkerLR");
        var markersLL = GameObject.FindGameObjectsWithTag("GazeMarkerLL");

        // Get world-space marker positions
        Vector3[][] worldPos = new Vector3[frameLength][];
        for (int frameIndex = 0; frameIndex < timeline.FrameLength; ++frameIndex)
        {
            // Apply animation at current frame
            timeline.GoToFrame(frameIndex);
            timeline.ApplyAnimation();

            // Find currently most visible marker set
            GameObject curMarkerSet = null;
            float curMarkerSetDist = float.MaxValue;
            Vector3 curHeadDir = gazeController.head.Direction;
            foreach (var markerSet in markerSets)
            {
                var markerUL = markersUL.FirstOrDefault(m => m.transform.parent == markerSet.transform);
                var markerDir = (markerUL.transform.position - head.position).normalized;
                float markerSetDist = Vector3.Angle(markerDir, curHeadDir);

                if (markerSetDist < curMarkerSetDist)
                {
                    curMarkerSetDist = markerSetDist;
                    curMarkerSet = markerSet;
                }
            }

            Debug.Log(string.Format("Frame {0}: using marker set {1}", frameIndex, curMarkerSet.name));

            // Get marker positions
            var curMarkerUL = markersUL.FirstOrDefault(m => m.transform.parent == curMarkerSet.transform);
            var curMarkerUR = markersUR.FirstOrDefault(m => m.transform.parent == curMarkerSet.transform);
            var curMarkerLR = markersLR.FirstOrDefault(m => m.transform.parent == curMarkerSet.transform);
            var curMarkerLL = markersLL.FirstOrDefault(m => m.transform.parent == curMarkerSet.transform);
            worldPos[frameIndex] = new Vector3[4];
            worldPos[frameIndex][0] = curMarkerUL.transform.position;
            worldPos[frameIndex][1] = curMarkerUR.transform.position;
            worldPos[frameIndex][2] = curMarkerLR.transform.position;
            worldPos[frameIndex][3] = curMarkerLL.transform.position;
        }

        // Create camera model for eye tracker video
        var eyeTrackCamModel = new VideoCameraModel(eyeTrackData.ImageWidth, eyeTrackData.ImageHeight);
        Matrix3x3 eyeTrackMatCamera;
        float[] eyeTrackDistCoeffs = new float[5];
        EyeTrackData.DefaultCameraModel.GetIntrinsics(out eyeTrackMatCamera, out eyeTrackDistCoeffs);
        eyeTrackCamModel.SetDefaultIntrinsics(eyeTrackMatCamera, eyeTrackDistCoeffs);

        // Estimate camera model for eye tracker video
        string imageDir = "../Matlab/EyeTracker/" + baseAnimation.Animation.Name + "#Frames" + "/";
        string outImageDir = "../Matlab/EyeTracker/" + baseAnimation.Animation.Name + "#OutFrames" + "/";
        int startFrame = eyeTrackData.FrameOffset;
        eyeTrackCamModel.Align(worldPos, imageDir, startFrame,
            eyeTrackData.CalibPatternWidth, eyeTrackData.CalibPatternHeight, true, true, outImageDir);
        */
        /*// Get marker objects
        var markers = GameObject.FindGameObjectsWithTag("GazeTarget");
        var chairLL = markers.FirstOrDefault(m => m.name == "B_Left");
        var chairUL = markers.FirstOrDefault(m => m.name == "Top_Left");
        var chairUR = markers.FirstOrDefault(m => m.name == "Top_Right");
        var chairLR = markers.FirstOrDefault(m => m.name == "B_Right");
        var dannyLL = markers.FirstOrDefault(m => m.name == "B_Left 1");
        var dannyUL = markers.FirstOrDefault(m => m.name == "Top_Left 1");
        var dannyUR = markers.FirstOrDefault(m => m.name == "Top_Right 1");
        var dannyLR = markers.FirstOrDefault(m => m.name == "B_Right 1");
        var dannyLM = markers.FirstOrDefault(m => m.name == "B_Middle");
        var bobbyLL = markers.FirstOrDefault(m => m.name == "B_Left 1");
        var bobbyUL = markers.FirstOrDefault(m => m.name == "Top_Left 1");
        var bobbyUR = markers.FirstOrDefault(m => m.name == "Top_Right 1");
        var bobbyMR1 = markers.FirstOrDefault(m => m.name == "Top_Right 1");
        var bobbyMR2 = markers.FirstOrDefault(m => m.name == "Top_Right 1");
        var bobbyLR = markers.FirstOrDefault(m => m.name == "B_Right 1");
            
        // Get image-space marker positions at frame 809 (1688)
        Vector2[] imgPos809 = new Vector2[4];
        imgPos809[0] = new Vector2(355, eyeTrackData.ImageHeight - 463 - 1);
        imgPos809[1] = new Vector2(204, eyeTrackData.ImageHeight - 302 - 1);
        imgPos809[2] = new Vector2(411, eyeTrackData.ImageHeight - 125 - 1);
        imgPos809[3] = new Vector2(555, eyeTrackData.ImageHeight - 302 - 1);

        // Get world-space marker positions at frame 809 (1688)
        bodyAnimationNorman.Apply(809, AnimationLayerMode.Override);
        timeline.GetLayer("Environment").Animations[0].Animation.Apply(809, AnimationLayerMode.Override);
        timeline.GetLayer("Environment").Animations[1].Animation.Apply(809, AnimationLayerMode.Override);

        Vector3[] worldPos809 = new Vector3[4];
        worldPos809[0] = head.InverseTransformPoint(chairLL.transform.position);
        worldPos809[1] = head.InverseTransformPoint(chairUL.transform.position);
        worldPos809[2] = head.InverseTransformPoint(chairUR.transform.position);
        worldPos809[3] = head.InverseTransformPoint(chairLR.transform.position);

        // Get image-space marker positions at frame 73 (952)
        Vector2[] imgPos73 = new Vector2[4];
        imgPos73[0] = new Vector2(375, eyeTrackData.ImageHeight - 288 - 1);
        imgPos73[1] = new Vector2(299, eyeTrackData.ImageHeight - 93 - 1);
        imgPos73[2] = new Vector2(535, eyeTrackData.ImageHeight - 11 - 1);
        imgPos73[3] = new Vector2(597, eyeTrackData.ImageHeight - 214 - 1);

        // Get world-space marker positions at frame 73 (952)
        bodyAnimationNorman.Apply(73, AnimationLayerMode.Override);
        timeline.GetLayer("Environment").Animations[0].Animation.Apply(73, AnimationLayerMode.Override);
        timeline.GetLayer("Environment").Animations[1].Animation.Apply(73, AnimationLayerMode.Override);
        Vector3[] worldPos73 = new Vector3[4];
        worldPos73[0] = head.InverseTransformPoint(chairLL.transform.position);
        worldPos73[1] = head.InverseTransformPoint(chairUL.transform.position);
        worldPos73[2] = head.InverseTransformPoint(chairUR.transform.position);
        worldPos73[3] = head.InverseTransformPoint(chairLR.transform.position);

        // Get image-space marker positions at frame 380 (1259)
        Vector2[] imgPos380 = new Vector2[5];
        imgPos380[0] = new Vector2(206, eyeTrackData.ImageHeight - 452 - 1);
        imgPos380[1] = new Vector2(174, eyeTrackData.ImageHeight - 388 - 1);
        imgPos380[2] = new Vector2(253, eyeTrackData.ImageHeight - 358 - 1);
        imgPos380[3] = new Vector2(281, eyeTrackData.ImageHeight - 419 - 1);
        imgPos380[4] = new Vector2(245, eyeTrackData.ImageHeight - 436 - 1);

        // Get world-space marker positions at frame 380 (1259)
        bodyAnimationNorman.Apply(380, AnimationLayerMode.Override);
        timeline.GetLayer("Environment").Animations[0].Animation.Apply(380, AnimationLayerMode.Override);
        timeline.GetLayer("Environment").Animations[1].Animation.Apply(380, AnimationLayerMode.Override);
        Vector3[] worldPos380 = new Vector3[5];
        worldPos380[0] = head.InverseTransformPoint(dannyLL.transform.position);
        worldPos380[1] = head.InverseTransformPoint(dannyUL.transform.position);
        worldPos380[2] = head.InverseTransformPoint(dannyUR.transform.position);
        worldPos380[3] = head.InverseTransformPoint(dannyLR.transform.position);
        worldPos380[4] = head.InverseTransformPoint(dannyLM.transform.position);

        // Calibrate eye tracker camera
        var cameraModel = new CameraModel();
        var matCamera = new Matrix3x3();
        matCamera.m00 = 1.1087157e+003f;
        matCamera.m01 = 0f;
        matCamera.m02 = 6.395e+002f;
        matCamera.m10 = 0f;
        matCamera.m11 = 1.1087157e+003f;
        matCamera.m12 = 4.795e+002f;
        matCamera.m20 = 0f;
        matCamera.m21 = 0f;
        matCamera.m22 = 1f;
        var distCoeffs = new float[5];
        distCoeffs[0] = 8.0114708e-002f;
        distCoeffs[1] = -7.9709385e-001f;
        distCoeffs[2] = 0f;
        distCoeffs[3] = 0f;
        distCoeffs[4] = 1.4157773e+000f;
        cameraModel.SetIntrinsics(matCamera, distCoeffs);
        cameraModel.InitOpenCV(worldPos809, imgPos809);

        // Test calibration
        Vector2[] estImgPos73 = new Vector2[4];
        estImgPos73[0] = cameraModel.GetImagePosition(worldPos73[0]);
        estImgPos73[1] = cameraModel.GetImagePosition(worldPos73[1]);
        estImgPos73[2] = cameraModel.GetImagePosition(worldPos73[2]);
        estImgPos73[3] = cameraModel.GetImagePosition(worldPos73[3]);
        Vector2[] estImgPos380 = new Vector2[5];
        estImgPos380[0] = cameraModel.GetImagePosition(worldPos380[0]);
        estImgPos380[1] = cameraModel.GetImagePosition(worldPos380[1]);
        estImgPos380[2] = cameraModel.GetImagePosition(worldPos380[2]);
        estImgPos380[3] = cameraModel.GetImagePosition(worldPos380[3]);
        estImgPos380[4] = cameraModel.GetImagePosition(worldPos380[4]);

        // Print test results
        Debug.Log("FRAME 73 (952):");
        Debug.Log(string.Format("Ground-truth: ({0}, {1}), estimated: ({2}, {3})",
            imgPos73[0].x, imgPos73[0].y, estImgPos73[0].x, estImgPos73[0].y));
        Debug.Log(string.Format("Ground-truth: ({0}, {1}), estimated: ({2}, {3})",
            imgPos73[1].x, imgPos73[1].y, estImgPos73[1].x, estImgPos73[1].y));
        Debug.Log(string.Format("Ground-truth: ({0}, {1}), estimated: ({2}, {3})",
            imgPos73[2].x, imgPos73[2].y, estImgPos73[2].x, estImgPos73[2].y));
        Debug.Log(string.Format("Ground-truth: ({0}, {1}), estimated: ({2}, {3})",
            imgPos73[3].x, imgPos73[3].y, estImgPos73[3].x, estImgPos73[3].y));
        Debug.Log("FRAME 380 (1259):");
        Debug.Log(string.Format("Ground-truth: ({0}, {1}), estimated: ({2}, {3})",
            imgPos380[0].x, imgPos380[0].y, estImgPos380[0].x, estImgPos380[0].y));
        Debug.Log(string.Format("Ground-truth: ({0}, {1}), estimated: ({2}, {3})",
            imgPos380[1].x, imgPos380[1].y, estImgPos380[1].x, estImgPos380[1].y));
        Debug.Log(string.Format("Ground-truth: ({0}, {1}), estimated: ({2}, {3})",
            imgPos380[2].x, imgPos380[2].y, estImgPos380[2].x, estImgPos380[2].y));
        Debug.Log(string.Format("Ground-truth: ({0}, {1}), estimated: ({2}, {3})",
            imgPos380[3].x, imgPos380[3].y, estImgPos380[3].x, estImgPos380[3].y));
        Debug.Log(string.Format("Ground-truth: ({0}, {1}), estimated: ({2}, {3})",
            imgPos380[4].x, imgPos380[4].y, estImgPos380[4].x, estImgPos380[4].y));*/

        /*var tableSpots = GameObject.FindGameObjectsWithTag("GazeTarget");
        var leftHandSpot = tableSpots.FirstOrDefault(obj => obj.name == "LeftHandSpot");
        var rightandSpot = tableSpots.FirstOrDefault(obj => obj.name == "RightHandSpot");
        var midTarget = tableSpots.FirstOrDefault(obj => obj.name == "MidTarget");
        var rightTarget = tableSpots.FirstOrDefault(obj => obj.name == "RightTarget");
        var leftTarget = tableSpots.FirstOrDefault(obj => obj.name == "LeftTarget");

        // Load eye tracking data
        var eyeTrackData = new EyeTrackData(testScenes.modelNormanNew, bodyAnimationNorman.AnimationClip);

        // Get ground-truth eye directions at frame 277 (104)
        var vle = eyeTrackData.Samples[277].lEyeDirection;
        var vre = eyeTrackData.Samples[277].rEyeDirection;

        // Get eye tracker bone space eye directions at frame 277 (104)
        bodyAnimationNorman.Apply(104, AnimationLayerMode.Override);
        var eyeTrackerBone = eyeTrackData.EyeTrackerBone;
        var ule = lEye.InverseTransformDirection((rightTarget.transform.position - lEye.position).normalized);
        var ure = rEye.InverseTransformDirection((rightTarget.transform.position - rEye.position).normalized);

        // Compute aligning rotations
        var qle = Quaternion.FromToRotation(ule, vle);
        var qre = Quaternion.FromToRotation(ure, vre);

        Debug.Log(string.Format("qle at 104: ({0}, {1}, {2})", qle.eulerAngles.x, qle.eulerAngles.y, qle.eulerAngles.z));
        Debug.Log(string.Format("qre at 104: ({0}, {1}, {2})", qre.eulerAngles.x, qre.eulerAngles.y, qre.eulerAngles.z));
            
        // Get ground-truth eye directions at frame 302 (129)
        vle = eyeTrackData.Samples[302].lEyeDirection;
        vre = eyeTrackData.Samples[302].rEyeDirection;

        // Get eye tracker bone space eye directions at frame 302 (129)
        bodyAnimationNorman.Apply(129, AnimationLayerMode.Override);
        ule = lEye.InverseTransformDirection((leftTarget.transform.position - lEye.position).normalized);
        ure = rEye.InverseTransformDirection((leftTarget.transform.position - rEye.position).normalized);

        // Compute aligning rotations
        qle = Quaternion.FromToRotation(ule, vle);
        qre = Quaternion.FromToRotation(ure, vre);

        Debug.Log(string.Format("qle at 129: ({0}, {1}, {2})", qle.eulerAngles.x, qle.eulerAngles.y, qle.eulerAngles.z));
        Debug.Log(string.Format("qre at 129: ({0}, {1}, {2})", qre.eulerAngles.x, qre.eulerAngles.y, qre.eulerAngles.z));

        // Get ground-truth eye directions at frame 332 (159)
        vle = eyeTrackData.Samples[332].lEyeDirection;
        vre = eyeTrackData.Samples[332].rEyeDirection;

        // Get eye tracker bone space eye directions at frame 332 (159)
        bodyAnimationNorman.Apply(159, AnimationLayerMode.Override);
        ule = lEye.InverseTransformDirection((midTarget.transform.position - lEye.position).normalized);
        ure = rEye.InverseTransformDirection((midTarget.transform.position - rEye.position).normalized);

        // Compute aligning rotations
        qle = Quaternion.FromToRotation(ule, vle);
        qre = Quaternion.FromToRotation(ure, vre);

        Debug.Log(string.Format("qle at 159: ({0}, {1}, {2})", qle.eulerAngles.x, qle.eulerAngles.y, qle.eulerAngles.z));
        Debug.Log(string.Format("qre at 159: ({0}, {1}, {2})", qre.eulerAngles.x, qre.eulerAngles.y, qre.eulerAngles.z));*/
        //
    }
}

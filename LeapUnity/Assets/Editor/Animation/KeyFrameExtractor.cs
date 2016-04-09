using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Collections;

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
    /// Constructor.
    /// </summary>
    /// <param name="model">Character model</param>
    /// <param name="defaultKeyFrameIndex">Default key frame index for each track in the set</param>
    public KeyFrameSet(GameObject model, int defaultKeyFrameIndex)
    {
        keyFrame = defaultKeyFrameIndex;
        rootKeyFrame = defaultKeyFrameIndex;
        boneKeyFrames = new int[ModelUtil.GetAllBones(model).Length];
        for (int boneIndex = 0; boneIndex < boneKeyFrames.Length; ++boneIndex)
            boneKeyFrames[boneIndex] = defaultKeyFrameIndex;
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
        for (int boneIndex = 0; boneIndex < keyFrameSet.boneKeyFrames.Length; ++boneIndex)
            timeSet.boneTimes[boneIndex] = ((float)keyFrameSet.boneKeyFrames[boneIndex]) / LEAPCore.editFrameRate;

        return timeSet;
    }
}

/// <summary>
/// Class representing an extractor of keyframes from animation clips.
/// </summary>
public class KeyFrameExtractor
{
    // Kinematic features of the animation
    public struct _KinematicFeatureSet
    {
        public Vector3[,] pBones;
        public Quaternion[,] qBones;
        public float[] vRoot;
        public float[,] vBones;
        public float[] aRoot;
        public float[,] aBones;

        public _KinematicFeatureSet(Transform[] bones, AnimationClipInstance instance)
        {
            pBones = new Vector3[bones.Length, instance.FrameLength];
            qBones = new Quaternion[bones.Length, instance.FrameLength];
            vRoot = new float[instance.FrameLength];
            vBones = new float[bones.Length, instance.FrameLength];
            aRoot = new float[instance.FrameLength];
            aBones = new float[bones.Length, instance.FrameLength];
        }
    }

    /// <summary>
    /// Weight of the end-effector constraint probability signal for keyframe extraction.
    /// </summary>
    public float EndEffConstrWeight
    {
        get { return _endEffConstrWeight; }
        set { _endEffConstrWeight = Mathf.Clamp01(value); }
    }

    /// <summary>
    /// Kernel size for the low-pass filter used for filtering the global probability signal in keyframe extraction.
    /// </summary>
    public int LowPassKernelSize
    {
        get { return _lowPassKernelSize; }
        set { _lowPassKernelSize = value < 0 ? 0 : value; }
    }

    /// <summary>
    /// Maximum width of a cluster of local key times corresponding to a single extracted key pose.
    /// </summary>
    public float MaxClusterWidth
    {
        get { return _maxClusterWidth; }
        set { _maxClusterWidth = value < 0 ? 0 : value; }
    }

    /// <summary>
    /// Use end-effector constraints to influence keyframe extraction.
    /// </summary>
    public bool UseEndEffectorConstraints
    {
        get;
        set;
    }
    
    /// <summary>
    /// Use translational movement of the root to influence keyframe extraction.
    /// </summary>
    public bool UseRootPosition
    {
        get;
        set;
    }
        
    /// <summary>
    /// Mask specifying which bones to analyze during keyframe extraction.
    /// </summary>
    public virtual BitArray BoneMask
    {
        get;
        protected set;
    }

    /// <summary>
    /// Write intermediate and output data from keyframe extraction into a CSV file.
    /// </summary>
    public bool WriteToCSV
    {
        get;
        set;
    }

    /// <summary>
    /// Character model.
    /// </summary>
    public GameObject Model
    {
        get;
        protected set;
    }

    /// <summary>
    /// Animation clip.
    /// </summary>
    public AnimationClip AnimationClip
    {
        get;
        protected set;
    }

    // Kinematic features of the animation, computed during keyframe extraction
    public virtual _KinematicFeatureSet _KinematicFeatures
    {
        get;
        protected set;
    }

    /// <summary>
    /// Extracted keyframes.
    /// </summary>
    public KeyFrameSet[] KeyFrames
    {
        get;
        protected set;
    }

    // Settings:
    private float _endEffConstrWeight = 1f;
    private int _lowPassKernelSize = 5;
    private float _maxClusterWidth = 0.5f;

    // Model and animation state:
    protected Transform[] _bones = null;
    protected Transform[] _endEffectors = null;
    protected AnimationClipInstance _instance = null;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="model"></param>
    /// <param name="clip"></param>
    public KeyFrameExtractor(GameObject model, AnimationClip clip)
    {
        Model = model;
        AnimationClip = clip;
        _bones = ModelUtil.GetAllBones(model);
        _endEffectors = ModelUtil.GetEndEffectors(model);
        _instance = new AnimationClipInstance(clip.name, model, true, false, false);

        // Initialize settings
        UseRootPosition = true;
        UseEndEffectorConstraints = true;
        _InitBoneMask(_bones);
        WriteToCSV = true;
    }

    /// <summary>
    /// Extract clusters of key frame indexes in the animation.
    /// </summary>
    /// <returns>Sets of key frame indexes</returns>
    public virtual KeyFrameSet[] ExtractKeyFrames()
    {
        // Create a CSV data table for per-frame data
        CSVDataFile csvDataPerFrame = null;
        if (WriteToCSV)
        {
            csvDataPerFrame = new CSVDataFile();

            csvDataPerFrame.AddAttribute("vRoot", typeof(float));
            for (int boneIndex = 0; boneIndex < _bones.Length; ++boneIndex)
                if (BoneMask[boneIndex])
                    csvDataPerFrame.AddAttribute("vBones#" + _bones[boneIndex].name, typeof(float));
            csvDataPerFrame.AddAttribute("aRoot", typeof(float));
            for (int boneIndex = 0; boneIndex < _bones.Length; ++boneIndex)
                if (BoneMask[boneIndex])
                    csvDataPerFrame.AddAttribute("aBones#" + _bones[boneIndex].name, typeof(float));
            csvDataPerFrame.AddAttribute("p0Root", typeof(float));
            for (int boneIndex = 0; boneIndex < _bones.Length; ++boneIndex)
                if (BoneMask[boneIndex])
                    csvDataPerFrame.AddAttribute("p0Bones#" + _bones[boneIndex].name, typeof(float));
            csvDataPerFrame.AddAttribute("pRoot", typeof(float));
            for (int boneIndex = 0; boneIndex < _bones.Length; ++boneIndex)
                if (BoneMask[boneIndex])
                    csvDataPerFrame.AddAttribute("pBones#" + _bones[boneIndex].name, typeof(float));
            csvDataPerFrame.AddAttribute("wRoot", typeof(float));
            for (int boneIndex = 0; boneIndex < _bones.Length; ++boneIndex)
                if (BoneMask[boneIndex])
                    csvDataPerFrame.AddAttribute("wBones#" + _bones[boneIndex].name, typeof(float));
            for (int endEffectorIndex = 0; endEffectorIndex < _endEffectors.Length; ++endEffectorIndex)
                csvDataPerFrame.AddAttribute("pEndEff#" + _endEffectors[endEffectorIndex].name, typeof(float));
            for (int endEffectorIndex = 0; endEffectorIndex < _endEffectors.Length; ++endEffectorIndex)
                csvDataPerFrame.AddAttribute("wEndEff#" + _endEffectors[endEffectorIndex].name, typeof(float));
            csvDataPerFrame.AddAttribute("p0", typeof(float));
            csvDataPerFrame.AddAttribute("p", typeof(float));
        }

        // Create a CSV data table for extracted key data
        CSVDataFile csvDataPerKey = new CSVDataFile();
        if (WriteToCSV)
        {
            csvDataPerKey = new CSVDataFile();

            csvDataPerKey.AddAttribute("keyFrameIndex", typeof(int));
            csvDataPerKey.AddAttribute("keyFrameIndexRoot", typeof(int));
            for (int boneIndex = 0; boneIndex < _bones.Length; ++boneIndex)
                if (BoneMask[boneIndex])
                    csvDataPerKey.AddAttribute("keyFrameIndexBone#" + _bones[boneIndex].name, typeof(int));
        }

        // Compute kinematic features
        float[] anRoot = new float[_instance.FrameLength];
        float[,] anBones = new float[_bones.Length, _instance.FrameLength];
        _KinematicFeatures = _ComputeKinematicFeatures(_bones, _instance);
        _NormalizeAccelerations(_bones, _instance, anRoot, anBones);

        // Compute bone and end-effector weights
        float wRoot = 0f;
        float[] wBones = new float[_bones.Length];
        float[] wEndEff = new float[_endEffectors.Length];
        _ComputeBoneWeights(_bones, _instance, out wRoot, wBones);
        _ComputeEndEffectorWeights(_endEffectors, _instance, wEndEff);

        // Compute local probability signals
        float[] pRoot = new float[_instance.FrameLength];
        float[,] pBones = new float[_bones.Length, _instance.FrameLength];
        float[,] pEndEff = new float[_endEffectors.Length, _instance.FrameLength];
        _ComputeBoneProbabilities(_bones, _instance, anRoot, anBones, pRoot, pBones);
        _ComputeEndEffectorProbabilities(_endEffectors, _instance, pEndEff);

        // Compute global probability signal
        float[] p = new float[_instance.FrameLength];
        _ComputeGlobalProbability(_bones, _endEffectors, _instance, p, pRoot, pBones, wRoot, wBones, pEndEff, wEndEff);

        // Filter probability signals
        float[] p0 = new float[_instance.FrameLength];
        float[] p0Root = new float[_instance.FrameLength];
        float[,] p0Bones = new float[_bones.Length, _instance.FrameLength];
        _FilterProbabilities(_bones, p, pRoot, pBones, p0, p0Root, p0Bones);

        // Extract keyframes
        var keyFrameSets = _ExtractKeyFrames(_bones, _instance, p, pRoot, pBones);

        if (WriteToCSV)
        {
            // Add and write per-frame CSV data
            for (int frameIndex = 0; frameIndex < _instance.FrameLength; ++frameIndex)
            {
                List<object> data = new List<object>();

                // Compose a row of data
                data.Add(_KinematicFeatures.vRoot[frameIndex]);
                for (int boneIndex = 0; boneIndex < _bones.Length; ++boneIndex)
                    if (BoneMask[boneIndex])
                        data.Add(_KinematicFeatures.vBones[boneIndex, frameIndex]);
                data.Add(_KinematicFeatures.aRoot[frameIndex]);
                for (int boneIndex = 0; boneIndex < _bones.Length; ++boneIndex)
                    if (BoneMask[boneIndex])
                        data.Add(_KinematicFeatures.aBones[boneIndex, frameIndex]);
                data.Add(p0Root[frameIndex]);
                for (int boneIndex = 0; boneIndex < _bones.Length; ++boneIndex)
                    if (BoneMask[boneIndex])
                        data.Add(p0Bones[boneIndex, frameIndex]);
                data.Add(pRoot[frameIndex]);
                for (int boneIndex = 0; boneIndex < _bones.Length; ++boneIndex)
                    if (BoneMask[boneIndex])
                        data.Add(pBones[boneIndex, frameIndex]);
                data.Add(wRoot);
                for (int boneIndex = 0; boneIndex < _bones.Length; ++boneIndex)
                    if (BoneMask[boneIndex])
                        data.Add(wBones[boneIndex]);
                for (int endEffectorIndex = 0; endEffectorIndex < _endEffectors.Length; ++endEffectorIndex)
                    data.Add(pEndEff[endEffectorIndex, frameIndex]);
                for (int endEffectorIndex = 0; endEffectorIndex < _endEffectors.Length; ++endEffectorIndex)
                    data.Add(wEndEff[endEffectorIndex]);
                data.Add(p0[frameIndex]);
                data.Add(p[frameIndex]);

                // Add it to the table
                csvDataPerFrame.AddData(data.ToArray());
            }
            csvDataPerFrame.WriteToFile("../Matlab/KeyExtraction/dataPerFrame#" + AnimationClip.name + ".csv");

            // Add and write per-key CSV data
            for (int keyIndex = 0; keyIndex < keyFrameSets.Length; ++keyIndex)
            {
                List<object> data = new List<object>();

                // Compose a row of data
                data.Add(keyFrameSets[keyIndex].keyFrame);
                int localKeyFrameIndex = keyFrameSets[keyIndex].rootKeyFrame;
                data.Add(localKeyFrameIndex);
                for (int boneIndex = 0; boneIndex < _bones.Length; ++boneIndex)
                {
                    if (!BoneMask[boneIndex])
                        continue;

                    localKeyFrameIndex = keyFrameSets[keyIndex].boneKeyFrames[boneIndex];
                    data.Add(localKeyFrameIndex);
                }

                // Add it to the table
                csvDataPerKey.AddData(data.ToArray());
            }
            csvDataPerKey.WriteToFile("../Matlab/KeyExtraction/dataPerKey#" + AnimationClip.name + ".csv");
        }

        Debug.Log(string.Format("Extracted keyframes for animation {0} on character model {1}",
            AnimationClip.name, Model.name));

        return keyFrameSets;
    }

    // Initialize bone mask
    protected virtual void _InitBoneMask(Transform[] bones)
    {
        BoneMask = new BitArray(bones.Length, true);

        // Compute mask over bones that aren't animated
        var curves = AnimationUtility.GetAllCurves(AnimationClip);
        for (int boneIndex = 0; boneIndex < bones.Length; ++boneIndex)
        {
            var bone = bones[boneIndex];
            string bonePath = ModelUtil.GetBonePath(bone);
            if (!curves.Any(c => c.type == typeof(Transform) && c.path == bonePath))
                BoneMask[boneIndex] = false;
        }
    }

    // Compute kinematic features of the animation
    protected virtual _KinematicFeatureSet _ComputeKinematicFeatures(Transform[] bones, AnimationClipInstance instance)
    {
        _KinematicFeatureSet features = new _KinematicFeatureSet(bones, instance);

        // Estimate bone accelerations and movement magnitudes
        for (int frameIndex = 0; frameIndex < instance.FrameLength; ++frameIndex)
        {
            instance.Apply(frameIndex, AnimationLayerMode.Override);

            // Store bone positions
            for (int boneIndex = 0; boneIndex < bones.Length; ++boneIndex)
            {
                if (!BoneMask[boneIndex])
                    continue;

                features.pBones[boneIndex, frameIndex] = bones[boneIndex].position;
            }

            // Store bone rotations
            for (int boneIndex = 0; boneIndex < bones.Length; ++boneIndex)
            {
                if (!BoneMask[boneIndex])
                    continue;

                features.qBones[boneIndex, frameIndex] = bones[boneIndex].localRotation;
            }

            // Estimate bone velocities
            if (frameIndex >= 1)
            {
                for (int boneIndex = 0; boneIndex < bones.Length; ++boneIndex)
                {
                    if (!BoneMask[boneIndex])
                        continue;

                    features.vBones[boneIndex, frameIndex] = QuaternionUtil.Angle(
                        Quaternion.Inverse(features.qBones[boneIndex, frameIndex - 1]) * bones[boneIndex].localRotation)
                        * LEAPCore.editFrameRate;
                }

                if (frameIndex == 1)
                {
                    for (int boneIndex = 0; boneIndex < bones.Length; ++boneIndex)
                    {
                        if (!BoneMask[boneIndex])
                            continue;

                        features.vBones[boneIndex, 0] = features.vBones[boneIndex, frameIndex];
                    }
                }
            }

            // Estimate bone acceleration
            if (frameIndex >= 2)
            {
                if (UseRootPosition)
                    features.aRoot[frameIndex] = features.aRoot[frameIndex - 1] =
                        (bones[0].position - 2f * features.pBones[0, frameIndex - 1] + features.pBones[0, frameIndex - 2]).magnitude *
                        LEAPCore.editFrameRate * LEAPCore.editFrameRate;

                for (int boneIndex = 0; boneIndex < bones.Length; ++boneIndex)
                {
                    if (!BoneMask[boneIndex])
                        continue;

                    Quaternion dq2 = Quaternion.Inverse(Quaternion.Inverse(features.qBones[boneIndex, frameIndex - 2]) *
                        features.qBones[boneIndex, frameIndex - 1]) *
                        (Quaternion.Inverse(features.qBones[boneIndex, frameIndex - 1]) * bones[boneIndex].localRotation);
                    features.aBones[boneIndex, frameIndex] = features.aBones[boneIndex, frameIndex - 1] =
                        QuaternionUtil.Angle(dq2) * LEAPCore.editFrameRate * LEAPCore.editFrameRate;
                }

                if (frameIndex == 2)
                {
                    if (UseRootPosition)
                        features.aRoot[frameIndex - 2] = features.aRoot[frameIndex];

                    for (int boneIndex = 0; boneIndex < bones.Length; ++boneIndex)
                    {
                        if (!BoneMask[boneIndex])
                            continue;

                        features.aBones[boneIndex, frameIndex - 2] = features.aBones[boneIndex, frameIndex];
                    }
                }
            }
        }

        return features;
    }

    // Normalize acceleration magnitudes to 0-1 range
    protected virtual void _NormalizeAccelerations(Transform[] bones, AnimationClipInstance instance,
        float[] anRoot, float[,] anBones)
    {
        if (UseRootPosition)
            _KinematicFeatures.aRoot.CopyTo(anRoot, 0);
        for (int boneIndex = 0; boneIndex < bones.Length; ++boneIndex)
        {
            if (!BoneMask[boneIndex])
                continue;

            var data = CollectionUtil.GetRow<float>(_KinematicFeatures.aBones, boneIndex);
            CollectionUtil.SetRow<float>(anBones, boneIndex, data);
        }

        // Normalize acceleration values
        float maxARoot = UseRootPosition ? anRoot.Max() : 0f;
        float[] maxABones = new float[bones.Length];
        for (int boneIndex = 0; boneIndex < bones.Length; ++boneIndex)
        {
            if (!BoneMask[boneIndex])
                continue;

            for (int frameIndex = 0; frameIndex < instance.FrameLength; ++frameIndex)
                maxABones[boneIndex] = Mathf.Max(maxABones[boneIndex], anBones[boneIndex, frameIndex]);
        }
        for (int frameIndex = 0; frameIndex < instance.FrameLength; ++frameIndex)
        {
            if (UseRootPosition)
                anRoot[frameIndex] = maxARoot > 0f ? anRoot[frameIndex] / maxARoot : 0f;

            for (int boneIndex = 0; boneIndex < bones.Length; ++boneIndex)
            {
                if (!BoneMask[boneIndex])
                    continue;

                anBones[boneIndex, frameIndex] = maxABones[boneIndex] > 0f ?
                    anBones[boneIndex, frameIndex] / maxABones[boneIndex] : 0f;
            }
        }
    }

    // Compute per-bone weights (how much each bone influences total keyframe probability)
    // TODO: extend metric to also include motion trail arc length
    protected virtual void _ComputeBoneWeights(Transform[] bones, AnimationClipInstance instance,
        out float wRoot, float[] wBones)
    {
        // Compute limb lengths
        float[] limbLengths = new float[bones.Length];
        for (int boneIndex = 0; boneIndex < bones.Length; ++boneIndex)
        {
            if (!BoneMask[boneIndex])
                continue;

            var bone = bones[boneIndex];
            limbLengths[boneIndex] = 0f;

            for (int childIndex = 0; childIndex < bone.childCount; ++childIndex)
            {
                var child = bone.GetChild(childIndex);
                if (!ModelUtil.IsBone(child))
                    continue;

                limbLengths[boneIndex] += (child.position - bone.position).magnitude;
            }
        }

        // Compute weights for bone probability signals
        wRoot = UseRootPosition ? limbLengths[0] : 0f;
        for (int boneIndex = 0; boneIndex < bones.Length; ++boneIndex)
        {
            if (!BoneMask[boneIndex])
                continue;

            wBones[boneIndex] = limbLengths[boneIndex];
        }
    }

    // Compute per-end-effector weights (how much each end-effector influences total keyframe probability)
    // TODO: extend metric to also include motion trail arc length
    protected virtual void _ComputeEndEffectorWeights(Transform[] endEffectors, AnimationClipInstance instance,
        float[] wEndEff)
    {
        if (UseEndEffectorConstraints && instance.EndEffectorConstraints != null)
        {
            for (int endEffectorIndex = 0; endEffectorIndex < endEffectors.Length; ++endEffectorIndex)
                wEndEff[endEffectorIndex] = EndEffConstrWeight;
        }
    }

    // Compute local, per-bone probabilities of keyframes
    protected virtual void _ComputeBoneProbabilities(Transform[] bones, AnimationClipInstance instance,
        float[] anRoot, float[,] anBones, float[] pRoot, float[,] pBones)
    {
        for (int frameIndex = 0; frameIndex < instance.FrameLength; ++frameIndex)
        {
            pRoot[frameIndex] = UseRootPosition ? anRoot[frameIndex] : 0f;
            for (int boneIndex = 0; boneIndex < bones.Length; ++boneIndex)
            {
                if (!BoneMask[boneIndex])
                    continue;

                pBones[boneIndex, frameIndex] = anBones[boneIndex, frameIndex];
            }
        }
    }

    // Compute local, per-end-effector probabilities of keyframes
    protected virtual void _ComputeEndEffectorProbabilities(Transform[] endEffectors, AnimationClipInstance instance,
        float[,] pEndEff)
    {
        if (UseEndEffectorConstraints && instance.EndEffectorConstraints != null)
        {
            for (int frameIndex = 0; frameIndex < instance.FrameLength; ++frameIndex)
            {
                var time = new TimeSet(Model, LEAPCore.ToTime(frameIndex));
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
                    }
                }
            }
        }
    }

    // Compute global probability of keyframes from per-bone and per-end-effector probabilities
    protected virtual void _ComputeGlobalProbability(Transform[] bones, Transform[] endEffectors, AnimationClipInstance instance,
        float[] p, float[] pRoot, float[,] pBones, float wRoot, float[] wBones,
        float[,] pEndEff, float[] wEndEff)
    {
        for (int frameIndex = 0; frameIndex < instance.FrameLength; ++frameIndex)
        {
            float sumP = 0f;
            float sumW = 0f;

            // Add bone probabilities and weights
            sumP += (UseRootPosition ? (wRoot * pRoot[frameIndex]) : 0f);
            sumW += (UseRootPosition ? wRoot : 0f);
            for (int boneIndex = 0; boneIndex < bones.Length; ++boneIndex)
            {
                if (!BoneMask[boneIndex])
                    continue;

                sumP += (wBones[boneIndex] * pBones[boneIndex, frameIndex]);
                sumW += wBones[boneIndex];
            }

            // Add end-effector constraint probabilities and weights
            for (int endEffectorIndex = 0; endEffectorIndex < endEffectors.Length; ++endEffectorIndex)
            {
                sumP += (wEndEff[endEffectorIndex] * pEndEff[endEffectorIndex, frameIndex]);
                sumW += wEndEff[endEffectorIndex];
            }

            // Compute global probability
            p[frameIndex] = sumP / sumW;
        }
    }

    // Filter keyframe probability signals
    protected virtual void _FilterProbabilities(Transform[] bones, float[] p, float[] pRoot, float[,] pBones,
        float[] p0, float[] p0Root, float[,] p0Bones)
    {
        // Smooth the global probability signal
        p.CopyTo(p0, 0);
        FilterUtil.Filter(p0, p, FilterUtil.GetTentKernel1D(LowPassKernelSize));

        // Smooth the local probability signals
        if (UseRootPosition)
        {
            pRoot.CopyTo(p0Root, 0);
            FilterUtil.Filter(p0Root, pRoot, FilterUtil.GetTentKernel1D(LowPassKernelSize));
        }
        for (int boneIndex = 0; boneIndex < bones.Length; ++boneIndex)
        {
            if (!BoneMask[boneIndex])
                continue;

            var data0 = CollectionUtil.GetRow<float>(pBones, boneIndex);
            CollectionUtil.SetRow<float>(p0Bones, boneIndex, data0);
            var data = new float[data0.Length];
            FilterUtil.Filter(data0, data, FilterUtil.GetTentKernel1D(LowPassKernelSize));
            CollectionUtil.SetRow<float>(pBones, boneIndex, data);
        }
    }

    // Extract local and global keyframes using probability signals
    protected virtual KeyFrameSet[] _ExtractKeyFrames(Transform[] bones, AnimationClipInstance instance,
        float[] p, float[] pRoot, float[,] pBones)
    {
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
                    < MaxClusterWidth / 2)
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

        // Keyframe sets:
        var keyFrameSets = new KeyFrameSet[keyFrameIndexes.Count];

        // Extract local keyframe indexes
        for (int keyIndex = 0; keyIndex < keyFrameIndexes.Count; ++keyIndex)
        {
            int keyFrameIndex = keyFrameIndexes[keyIndex];
            int prevKeyFrameIndex = keyIndex > 0 ? keyFrameIndexes[keyIndex - 1] : -1;
            int nextKeyFrameIndex = keyIndex < keyFrameIndexes.Count - 1 ? keyFrameIndexes[keyIndex + 1] : -1;
            int clusterFrameWidth = Mathf.RoundToInt(MaxClusterWidth * LEAPCore.editFrameRate);

            // Find local keyframe index for the root position
            int rootKeyFrameIndex = keyFrameIndex;
            if (UseRootPosition)
            {
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
            }

            // Find local keyframe indexes for the bone rotations
            int[] boneKeyFrameIndexes = new int[bones.Length];
            for (int boneIndex = 0; boneIndex < bones.Length; ++boneIndex)
            {
                if (!BoneMask[boneIndex])
                    continue;

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
            var keyFrameSet = new KeyFrameSet(Model);
            keyFrameSet.keyFrame = keyFrameIndex;
            keyFrameSet.rootKeyFrame = rootKeyFrameIndex;
            keyFrameSet.boneKeyFrames = boneKeyFrameIndexes;
            keyFrameSets[keyIndex] = keyFrameSet;
        }

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
            var csvData = new CSVDataFile();

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
            var csvData = new CSVDataFile();

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

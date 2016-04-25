using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Collections;

/// <summary>
/// Modified key frame extractor for gaze event detection.
/// </summary>
public class EyeGazeKeyFrameExtractor : KeyFrameExtractor
{
    protected GazeController _gazeController = null;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="model"></param>
    /// <param name="clip"></param>
    public EyeGazeKeyFrameExtractor(GameObject model, AnimationClip clip) : base(model, clip)
    {
        _gazeController = Model.GetComponent<GazeController>();
        if (_gazeController == null)
            throw new ArgumentException(string.Format("Model {0} does not have a gaze controller", model.name), "model");

        // Define bone mask
        BoneMask = new BitArray(_bones.Length, false);
        var gazeJoints = _gazeController.head.gazeJoints.Union(_gazeController.torso.gazeJoints)
            .Union(new[] { _bones[0] }).ToArray();
        foreach (var gazeJoint in gazeJoints)
        {
            int gazeJointIndex = ModelUtil.FindBoneIndex(_bones, gazeJoint);
            BoneMask[gazeJointIndex] = true;
        }

        // Settings for gaze keyframe extraction
        UseRootPosition = false;
        UseEndEffectorConstraints = false;
        LowPassKernelSize = LEAPCore.gazeInferenceLowPassKernelSize;
        UseBilateralFilter = LEAPCore.gazeInferenceUseBilateralFilter;
        BilateralFilterSpace = LEAPCore.gazeInferenceBilateralFilterSpace;
        BilateralFilterRange = LEAPCore.gazeInferenceBilateralFilterRange;
        MaxClusterWidth = LEAPCore.gazeInferenceKeyMaxClusterWidth;
        MinKeyFrameP = LEAPCore.gazeInferenceMinKeyFrameP;
    }

    /// <summary>
    /// Compute per-bone weights (how much each bone influences total keyframe probability)
    /// based on bone distance to the eyes.
    /// </summary>
    /// <returns>Bone weights</returns>
    public virtual float[] ComputeBoneWeights()
    {
        float wRoot;
        float[] wBones = new float[_bones.Length];
        _ComputeBoneWeights(_bones, null, out wRoot, wBones);

        return wBones;
    }

    // Compute per-bone weights (how much each bone influences total keyframe probability)
    // based on bone distance to the eyes
    protected override void _ComputeBoneWeights(Transform[] bones, AnimationClipInstance instance,
        out float wRoot, float[] wBones)
    {
        var head = _gazeController.head.Top;
        for (int boneIndex = 0; boneIndex < bones.Length; ++boneIndex)
        {
            if (!BoneMask[boneIndex])
                continue;

            var bone = bones[boneIndex];
            float boneDistance = ModelUtil.IsDescendantOf(bone, head) ?
                ModelUtil.GetBoneChainLength(head, bone) :
                ModelUtil.GetBoneChainLength(bone, head);

            wBones[boneIndex] = 1f / (1f + boneDistance);
        }
        wRoot = wBones[0];
    }
}

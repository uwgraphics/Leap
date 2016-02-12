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

    private enum DomeRenderMode
    {
        Disabled,
        ModelOnly,
        ShowScene
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
        /*timeline.RemoveAllAnimations(layerName, model.name);
        
        _InferEyeGazeTimings(timeline, baseAnimationInstanceId, layerName);*/
        _InferEyeGazeTargets(timeline, baseAnimationInstanceId, layerName, envLayerName);
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
            FilterUtil.Filter(data0, data, FilterUtil.GetTentKernel1D(LEAPCore.eyeGazeInferenceLowPassKernelSize));
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
                    (1f + Mathf.Exp(-LEAPCore.eyeGazeInferenceGazeShiftLogisticSlope * rGS)) - 1f);
                pGFBones[boneIndex] = Mathf.Clamp01(2f /
                    (1f + Mathf.Exp(-LEAPCore.eyeGazeInferenceGazeShiftLogisticSlope * rGF)) - 1f);
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

    // Infer gaze shift target locations in the scene
    private static void _InferEyeGazeTargets(AnimationTimeline timeline, int baseAnimationInstanceId,
        string layerName, string envLayerName)
    {
        Debug.Log("Inferring gaze instance targets in the scene...");

        var baseInstance = timeline.GetAnimation(baseAnimationInstanceId) as AnimationClipInstance;
        var model = baseInstance.Model;
        var root = ModelUtil.FindRootBone(model);
        var bones = ModelUtil.GetAllBones(model);
        var gazeController = model.GetComponent<GazeController>();
        var gazeLayer = timeline.GetLayer(layerName);
        var envRoot = timeline.OwningManager.Environment;

        // Deactivate gaze
        bool gazeControllerEnabled = gazeController.enabled;
        bool gazeLayerActive = gazeLayer.Active;
        gazeController.enabled = false;
        gazeLayer.Active = false;

        foreach (var scheduledGazeInstance in gazeLayer.Animations)
        {
            var gazeInstance = scheduledGazeInstance.Animation as EyeGazeInstance;
            if (gazeInstance.Model != baseInstance.Model) // Gaze shift on a different character
                continue;

            int startFrame = scheduledGazeInstance.StartFrame;
            int fixationStartFrame = startFrame + gazeInstance.FixationStartFrame;
            // TODO: remove this
            if (gazeInstance.Name != "StackBoxesGaze2")
                continue;
            //

            if (gazeInstance.Name.EndsWith(LEAPCore.gazeAheadSuffix))
            {
                timeline.GoToFrame(fixationStartFrame);
                timeline.ApplyAnimation();

                // Set target position for gaze shift straight ahead
                gazeInstance.Target = null;
                Vector3 aheadTargetPos = gazeController.head.Position + 5f * gazeController.head.Direction;
                gazeInstance.AheadTargetPosition = aheadTargetPos;
            }
            else
            {
                // Infer most likely gaze shift target
                GameObject gazeTarget = null;
                if (LEAPCore.useSimpleGazeTargetInference)
                {
                    timeline.GoToFrame(fixationStartFrame);
                    timeline.ApplyAnimation();

                    gazeTarget = _InferEyeGazeTargetSimple(model, gazeInstance, envRoot);
                }
                else
                {
                    // TODO: remove this
                    /*for (int frameIndex = scheduledGazeInstance.StartFrame; frameIndex <= fixationStartFrame; ++frameIndex)
                    {
                        timeline.GoToFrame(frameIndex);
                        timeline.ApplyAnimation();

                        // Determine gaze point
                        var eyeDir = gazeController.head.Direction;
                        var eyePos = gazeController.EyeCenter;
                        var gazePos = eyePos + 3.6f * eyeDir;

                        // Create sphere indicating a point along the gaze shift path
                        string sphereName = gazeInstance.Name + frameIndex;
                        var sphere = GameObject.Find(sphereName);
                        if (sphere == null)
                        {
                            sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                            sphere.name = sphereName;
                            sphere.renderer.material.color = Color.magenta;
                            sphere.transform.localScale = new Vector3(0.08f, 0.08f, 0.08f);
                        }
                        sphere.transform.position = gazePos;
                    }*/
                    //

                    // TODO: this should all go into a separate method
                    // Get model height
                    timeline.ResetModelsAndEnvironment();
                    float height = gazeController.head.Top.position.y;

                    // Get gaze shift path start point
                    timeline.GoToFrame(startFrame);
                    timeline.ApplyAnimation();
                    var eyeDir = gazeController.head.Direction;
                    var eyePos = gazeController.EyeCenter;
                    var startPos = eyePos + height * eyeDir;

                    // Get gaze shift path end point
                    timeline.GoToFrame(fixationStartFrame);
                    timeline.ApplyAnimation();
                    eyeDir = gazeController.head.Direction;
                    eyePos = gazeController.EyeCenter;
                    var endPos = eyePos + height * eyeDir;

                    // Compute OMR
                    float OMR = Mathf.Min(gazeController.lEye.outOMR, gazeController.lEye.inOMR);
                    // TODO: use adjusted OMR

                    // Compute extended gaze shift path end point
                    var startEyeDir = (startPos - eyePos).normalized;
                    var endEyeDir = (endPos - eyePos).normalized;
                    var eyePathRot = Quaternion.FromToRotation(startEyeDir, endEyeDir);
                    float eyePathAngle;
                    Vector3 eyePathAxis;
                    eyePathRot.ToAngleAxis(out eyePathAngle, out eyePathAxis);
                    var exEyePathRot = Quaternion.AngleAxis(OMR, eyePathAxis);
                    var exEndEyeDir = exEyePathRot * endEyeDir;
                    var exEndPos = eyePos + height * exEndEyeDir;
                    /*var lEyePos = gazeController.lEye.Position;
                    var rEyePos = gazeController.rEye.Position;
                    var lEyeRot = gazeController.lEye.Top.localRotation;
                    var rEyeRot = gazeController.rEye.Top.localRotation;
                    gazeController.lEye.RotateTowards((exEndPos - lEyePos).normalized);
                    gazeController.rEye.RotateTowards((exEndPos - rEyePos).normalized);
                    gazeController.lEye.ClampOMR(lEyeRot);
                    gazeController.rEye.ClampOMR(rEyeRot);
                    exEndEyeDir = (0.5f * (gazeController.lEye.Direction + gazeController.rEye.Direction)).normalized;
                    exEndPos = eyePos + height * exEndEyeDir;*/

                    //

                    gazeTarget = _InferEyeGazeTarget(model, gazeInstance, envRoot, OMR, endPos, exEndPos);
                }

                // Set gaze target
                gazeInstance.Target = gazeTarget;
                var targetInstance = timeline.GetLayer(envLayerName).Animations.FirstOrDefault(inst =>
                    inst.Animation.Model == gazeTarget);
                if (targetInstance != null)
                    gazeInstance._SetTargetAnimationClip((targetInstance.Animation as AnimationClipInstance).AnimationClip);
            }
        }
        
        // Reset to initial state
        gazeController.enabled = gazeControllerEnabled;
        gazeLayer.Active = gazeLayerActive;
        timeline.GoToFrame(0);
        timeline.ResetModelsAndEnvironment();
    }

    // Infer most likely gaze shift location by sampling from a spatial probability distribution
    private static GameObject _InferEyeGazeTarget(GameObject model, EyeGazeInstance eyeGazeInstance, GameObject envRoot,
        float OMR, Vector3 eyePathStartPos, Vector3 eyePathEndPos)
    {
        var gazeController = model.GetComponent<GazeController>();

        // Get gaze inference camera and texture size
        var cam = _GetEyeGazeCamera(model);
        int width = 0, height = 0;
        _GetEyeGazeTextureSize(cam, out width, out height);

        // Create target inference render textures
        var rtWorldPos = _CreateEyeGazeRenderTexture(width, height, RenderTextureFormat.ARGBFloat);
        var rtGameObjID = _CreateEyeGazeRenderTexture(width, height, RenderTextureFormat.RFloat);
        var rtPGazeShiftDir = _CreateEyeGazeRenderTexture(width, height, RenderTextureFormat.ARGB32);
        var rtPTaskRel = _CreateEyeGazeRenderTexture(width, height, RenderTextureFormat.ARGB32);
        var rtPHandCon = _CreateEyeGazeRenderTexture(width, height, RenderTextureFormat.ARGB32);
        var rtPTotal = _CreateEyeGazeRenderTexture(width, height, RenderTextureFormat.ARGB32);

        // Get target inference shaders and materials
        var shaderWorldPos = Shader.Find("EyeGazeInference/RenderWorldPosition");
        var matGameObjID = Resources.Load("AnimationEditor/RenderGameObjectID", typeof(Material)) as Material;
        var matPGazeShiftDir = Resources.Load("AnimationEditor/PGazeShiftDirection", typeof(Material)) as Material;
        var matPTaskRel = Resources.Load("AnimationEditor/PTaskRelevance", typeof(Material)) as Material;
        //var matPHandCon = Resources.Load("AnimationEditor/PHandContact", typeof(Material)) as Material;
        //var matPTotal = Resources.Load("AnimationEditor/PTotal", typeof(Material)) as Material;

        // Disable rendering of the current character model
        ModelUtil.ShowModel(model, false);

        // Render scene world positions
        _ShowModels(model, envRoot, true);
        _ShowDome(model, envRoot, DomeRenderMode.ShowScene);
        RenderTexture.active = rtWorldPos;
        cam.targetTexture = rtWorldPos;
        cam.RenderWithShader(shaderWorldPos, "");

        // Render scene game object IDs
        var materials = _GetMaterialsOnModels(model, envRoot);
        _SetMaterialOnModels(model, envRoot, matGameObjID);
        _SetGameObjectIDPropertyOnModels(model, envRoot);
        RenderTexture.active = rtGameObjID;
        cam.targetTexture = rtGameObjID;
        cam.Render();
        _SetMaterialsOnModels(model, envRoot, materials);

        // Get gaze shift direction properties
        var lEyePos = gazeController.lEye.Top.position;
        var rEyePos = gazeController.rEye.Top.position;
        gazeController.lEye.Yaw = gazeController.lEye.Pitch =
            gazeController.rEye.Yaw = gazeController.rEye.Pitch = 0f;
        var lEyeDir0 = gazeController.lEye.Direction;
        var rEyeDir0 = gazeController.rEye.Direction;

        // Set gaze shift direction properties in the material
        matPGazeShiftDir.SetVector("_LEyePosition", new Vector4(lEyePos.x, lEyePos.y, lEyePos.z, 1f));
        matPGazeShiftDir.SetVector("_REyePosition", new Vector4(rEyePos.x, rEyePos.y, rEyePos.z, 1f));
        matPGazeShiftDir.SetVector("_LEyeDirectionAhead", new Vector4(lEyeDir0.x, lEyeDir0.y, lEyeDir0.z, 0f));
        matPGazeShiftDir.SetVector("_REyeDirectionAhead", new Vector4(rEyeDir0.x, rEyeDir0.y, rEyeDir0.z, 0f));
        matPGazeShiftDir.SetFloat("_OMR", OMR);
        matPGazeShiftDir.SetVector("_EyePathStartPosition", new Vector4(eyePathStartPos.x, eyePathStartPos.y, eyePathStartPos.z, 0f));
        matPGazeShiftDir.SetVector("_EyePathEndPosition", new Vector4(eyePathEndPos.x, eyePathEndPos.y, eyePathEndPos.z, 0f));

        // Render gaze shift probability map
        _ShowModels(model, envRoot, false);
        _ShowDome(model, envRoot, DomeRenderMode.ModelOnly);
        RenderTexture.active = rtPGazeShiftDir;
        cam.targetTexture = rtPGazeShiftDir;
        cam.Render();
        _WriteRenderTextureToFile("../Matlab/EyeGazeInference/" + eyeGazeInstance.Name + "-PGazeShiftDir.png");

        // Render object task relevance probability map
        _ShowModels(model, envRoot, true);
        _ShowDome(model, envRoot, DomeRenderMode.Disabled);
        _SetMaterialOnModels(model, envRoot, matPTaskRel);
        _SetTaskRelevancePropertyOnModels(model, envRoot);
        RenderTexture.active = rtPTaskRel;
        cam.targetTexture = rtPTaskRel;
        cam.Render();
        _SetMaterialsOnModels(model, envRoot, materials);
        _WriteRenderTextureToFile("../Matlab/EyeGazeInference/" + eyeGazeInstance.Name + "-PTaskRel.png");

        // Render object hand constraint probability map
        // TODO

        // Compute and set view-projection matrix
        /*var matProj = GL.GetGPUProjectionMatrix(cam.projectionMatrix, false);
        var matView = cam.worldToCameraMatrix;
        var matViewProj = matProj * matView;
        matTest1.SetMatrix("_MatViewProj", matViewProj);*/

        // Show scene as normal
        _ShowDome(model, envRoot, DomeRenderMode.Disabled);
        _ShowModels(model, envRoot, true);
        ModelUtil.ShowModel(model, true);

        // Destroy render textures
        RenderTexture.Destroy(rtWorldPos);
        RenderTexture.Destroy(rtGameObjID);
        RenderTexture.Destroy(rtPGazeShiftDir);
        RenderTexture.Destroy(rtPTaskRel);
        RenderTexture.Destroy(rtPHandCon);
        RenderTexture.Destroy(rtPTotal);

        return null;
    }

    // Get eye gaze inference camera
    private static Camera _GetEyeGazeCamera(GameObject model)
    {
        var gazeController = model.GetComponent<GazeController>();
        var headBone = gazeController.head.Top;
        for (int childIndex = 0; childIndex < headBone.childCount; ++childIndex)
        {
            var child = headBone.GetChild(childIndex);
            if (child.gameObject.camera != null)
            {
                return child.gameObject.camera;
            }
        }

        return null;
    }

    // Get width and height of textures used for gaze target inference
    private static void _GetEyeGazeTextureSize(Camera cam, out int width, out int height)
    {
        float camAspect = cam.aspect;
        width = LEAPCore.eyeGazeInferenceRenderTextureWidth;
        height = Mathf.RoundToInt(((float)LEAPCore.eyeGazeInferenceRenderTextureWidth) / camAspect);
    }

    // Create a render texture used in gaze target inference
    private static RenderTexture _CreateEyeGazeRenderTexture(int width, int height, RenderTextureFormat format)
    {
        var tex = new RenderTexture(width, height, 24, format);
        tex.antiAliasing = 1;
        tex.filterMode = FilterMode.Point;

        return tex;
    }

    // Enable/disable renderers on characters and environment models
    private static void _ShowModels(GameObject model, GameObject envRoot, bool enabled = true)
    {
        var models = GameObject.FindGameObjectsWithTag("Agent");
        foreach (var curModel in models)
            if (curModel != model && curModel.active)
                ModelUtil.ShowModel(curModel, enabled);
        ModelUtil.ShowModel(envRoot, enabled);
    }

    // Set material for rendering the character/scene dome
    private static void _SetDomeMaterial(Material mat)
    {
        var dome = GameObject.FindGameObjectWithTag("GazeTargetDome");
        dome.renderer.material = mat;
    }

    // Enable/disable the rendering of a dome around the character/scene
    private static void _ShowDome(GameObject model, GameObject envRoot, DomeRenderMode mode)
    {
        var dome = GameObject.FindGameObjectWithTag("GazeTargetDome");
        if (mode == DomeRenderMode.Disabled)
        {
            dome.renderer.enabled = false;
        }
        else
        {
            dome.renderer.enabled = true;

            if (mode == DomeRenderMode.ModelOnly)
            {
                var gazeController = model.GetComponent<GazeController>();
                float modelHeight = gazeController.head.Top.position.y;
                dome.transform.position = gazeController.EyeCenter;
                dome.transform.localScale = new Vector3(modelHeight, modelHeight, modelHeight);
            }
            else
            {
                dome.transform.position = new Vector3(model.transform.position.x, 0f, model.transform.position.z);
                dome.transform.localScale = new Vector3(100f, 100f, 100f);
            }
        }   
    }

    // Get materials on characters and environment models in the scene
    private static Material[] _GetMaterialsOnModels(GameObject model, GameObject envRoot)
    {
        List<Material> materials = new List<Material>();

        var models = GameObject.FindGameObjectsWithTag("Agent");
        foreach (var curModel in models)
            if (curModel != model && curModel.active)
                materials.AddRange(ModelUtil.GetModelMaterials(curModel));
        materials.AddRange(ModelUtil.GetModelMaterials(envRoot));

        return materials.ToArray();
    }

    // Set specified material on all characters and environment models in the scene
    private static void _SetMaterialOnModels(GameObject model, GameObject envRoot, Material mat)
    {
        var models = GameObject.FindGameObjectsWithTag("Agent");
        foreach (var curModel in models)
            if (curModel != model && curModel.active)
                ModelUtil.SetModelMaterial(curModel, mat);
        ModelUtil.SetModelMaterial(envRoot, mat);
    }

    // Set materials on characters and environment models in the scene
    private static void _SetMaterialsOnModels(GameObject model, GameObject envRoot, Material[] materials)
    {
        int matIndex = 0;

        var models = GameObject.FindGameObjectsWithTag("Agent");
        foreach (var curModel in models)
        {
            if (curModel != model && curModel.active)
            {
                var curModelMaterials = ModelUtil.GetModelMaterials(curModel);
                var newModelMaterials = new Material[curModelMaterials.Length];
                Array.Copy(materials, matIndex, newModelMaterials, 0, newModelMaterials.Length);
                ModelUtil.SetModelMaterials(curModel, newModelMaterials);
                matIndex += curModelMaterials.Length;
            }
        }

        var curEnvMaterials = ModelUtil.GetModelMaterials(envRoot);
        var newEnvMaterials = new Material[curEnvMaterials.Length];
        Array.Copy(materials, matIndex, newEnvMaterials, 0, newEnvMaterials.Length);
        ModelUtil.SetModelMaterials(envRoot, newEnvMaterials);
    }

    // Set game object ID material property for all characters and environment models in the scene
    private static void _SetGameObjectIDPropertyOnModels(GameObject model, GameObject envRoot)
    {
        var models = GameObject.FindGameObjectsWithTag("Agent");
        foreach (var curModel in models)
        {
            if (curModel != model && curModel.active)
            {
                int gameObjectID = curModel.GetInstanceID();
                var modelMaterials = ModelUtil.GetModelMaterials(curModel, false);
                foreach (var mat in modelMaterials)
                    mat.SetInt("_GameObjectID", gameObjectID);
            }
        }

        var envModels = ModelUtil.GetSubModels(envRoot);
        foreach (var envModel in envModels)
        {
            if (envModel.renderer != null)
            {
                int gameObjectID = envModel.GetInstanceID();
                envModel.renderer.material.SetInt("_GameObjectID", gameObjectID);
            }
        }
    }

    // Set task relevance material property for all characters and environment models in the scene
    private static void _SetTaskRelevancePropertyOnModels(GameObject model, GameObject envRoot)
    {
        var models = GameObject.FindGameObjectsWithTag("Agent");
        foreach (var curModel in models)
        {
            if (curModel != model && curModel.active)
            {
                var modelMaterials = ModelUtil.GetModelMaterials(curModel, false);
                foreach (var mat in modelMaterials)
                    mat.SetInt("_IsTaskRelevant", 1);
            }
        }

        var envModels = ModelUtil.GetSubModels(envRoot);
        foreach (var envModel in envModels)
        {
            if (envModel.renderer != null &&
                (envModel.tag == "ManipulatedObject" || envModel.tag == "GazeTarget"))
            {
                envModel.renderer.material.SetInt("_IsTaskRelevant", 1);
            }
        }
    }

    // Write active render texture to a PNG file
    private static void _WriteRenderTextureToFile(string path)
    {
        int width = RenderTexture.active.width;
        int height = RenderTexture.active.height;
        var tex = new Texture2D(width, height, TextureFormat.ARGB32, false);
        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        var texData = tex.EncodeToPNG();
        System.IO.File.WriteAllBytes(path, texData);
    }

    // Infer most likely gaze shift location using simple raycast
    private static GameObject _InferEyeGazeTargetSimple(GameObject model, EyeGazeInstance eyeGazeInstance, GameObject envRoot)
    {
        var gazeController = model.GetComponent<GazeController>();
        GameObject targetParent = null;
        Vector3 targetPosition = Vector3.zero;
        var gazeDirection = new Ray(gazeController.EyeCenter, gazeController.head.Direction);
        var curGazeTargets = ModelUtil.GetSubModelsWithTag(envRoot, "GazeTarget");
        var newGazeTargets = new List<GameObject>();

        RaycastHit hitInfo;
        if (Physics.Raycast(gazeDirection, out hitInfo, 50f))
        {
            targetParent = hitInfo.collider.gameObject;
            targetPosition = hitInfo.point;
        }
        else
        {
            targetParent = envRoot;
            targetPosition = gazeDirection.origin + 5f * gazeDirection.direction;
        }

        // Get/create gaze target object
        GameObject gazeTarget = null;
        var curGazeTarget = curGazeTargets.FirstOrDefault(gt => (gt.transform.position - targetPosition).magnitude < 0.35f);
        if (curGazeTarget != null)
        {
            gazeTarget = curGazeTarget.gameObject;
        }
        else
        {
            // Generate gaze target name
            string gazeTargetName = "";
            int gazeTargetIndex = 1;
            do
            {
                gazeTargetName = targetParent.name + gazeTargetIndex;
                ++gazeTargetIndex;
            }
            while (curGazeTargets.Any(gt => gt.name == gazeTargetName) || newGazeTargets.Any(gt => gt.name == gazeTargetName));

            gazeTarget = new GameObject(gazeTargetName);
            gazeTarget.tag = "GazeTarget";
            gazeTarget.transform.position = targetPosition;
            gazeTarget.transform.SetParent(targetParent.transform, true);
            newGazeTargets.Add(gazeTarget);
        }

        return gazeTarget;
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

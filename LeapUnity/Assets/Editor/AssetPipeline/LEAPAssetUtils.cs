using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

/// <summary>
/// Some useful methods for working with LEAP assets.
/// </summary>
public static class LEAPAssetUtils
{
    /// <summary>
    /// Parses morph target name. 
    /// </summary>
    /// <param name="mtName">
    /// Morph target name. <see cref="System.String"/>
    /// </param>
    /// <param name="srcName">
    /// Source mesh name. <see cref="System.String"/>
    /// </param>
    /// <returns>
    /// true if morph target name is valid, otherwise false. <see cref="System.Boolean"/>
    /// </returns>
    public static bool ParseMorphTargetName(string mtName, out string srcName)
    {
        srcName = "";

        // Is it marked as morph target?
        if (!mtName.StartsWith(LEAPCore.morphTargetPrefix + "&"))
            return false;
        mtName = mtName.Substring((LEAPCore.morphTargetPrefix + "&").Length);

        // What is the source mesh?
        int srcname_i = mtName.IndexOf('&');
        if (srcname_i <= 0)
            return false;
        srcName = mtName.Substring(0, srcname_i);

        return true;
    }

    /// <summary>
    /// Recalculates normals for the specified mesh.
    /// </summary>
    /// <param name="mesh">
    /// Mesh. <see cref="Mesh"/>
    /// </param>
    public static void RecalculateNormals(Mesh mesh)
    {
        Vector3[] vertices = mesh.vertices;
        Vector3[] normals = mesh.normals;
        int[] triangles = mesh.triangles;

        for (int vi = 0; vi < vertices.Length; ++vi)
        {
            Vector3 v = vertices[vi];
            Vector3 norm = new Vector3(0, 0, 0);

            for (int ti = 0; ti < triangles.Length; ti += 3)
            {
                Vector3 v1, v2;

                if (triangles[ti] == vi)
                {
                    v1 = vertices[triangles[ti + 1]];
                    v2 = vertices[triangles[ti + 2]];
                }
                else if (triangles[ti + 1] == vi)
                {
                    v1 = vertices[triangles[ti + 2]];
                    v2 = vertices[triangles[ti]];
                }
                else if (triangles[ti + 2] == vi)
                {
                    v1 = vertices[triangles[ti]];
                    v2 = vertices[triangles[ti + 1]];
                }
                else
                {
                    continue;
                }

                norm += Vector3.Cross((v1 - v).normalized, (v2 - v).normalized);
            }

            normals[vi] = norm.normalized;
        }

        mesh.normals = normals;
    }

    /// <summary>
    /// Copies animation events from one animation clip to another.
    /// </summary>
    /// <param name="srcClip">
    /// Source animation clip. <see cref="AnimationClip"/>
    /// </param>
    /// <param name="dstClip">
    /// Destination animation clip. <see cref="AnimationClip"/>
    /// </param>
    public static void CopyAnimationEvents(AnimationClip srcClip, AnimationClip dstClip)
    {
        AnimationEvent[] events = AnimationUtility.GetAnimationEvents(srcClip);

        // Copy anim. events to destination clip
        for (int evt_i = 0; events != null && evt_i < events.Length; ++evt_i)
        {
            AnimationEvent evt = events[evt_i];

            // Scale clip time (just to make the copying a bit more robust)
            evt.time = dstClip.length / srcClip.length * evt.time;
        }
        AnimationUtility.SetAnimationEvents(dstClip, events);
    }

    /// <summary>
    /// Updates the specified instance of the agent model with
    /// new morph targets and animations without reseting
    /// any of the instance-specific data.
    /// </summary>
    /// <param name="mdlBase">
    /// Base agent model which should be used to update the instance.
    /// </param>
    /// <param name="gameObj">
    /// Agent model instance.<see cref="GameObject"/>
    /// </param>
    public static void RefreshAgentModel(GameObject mdlBase, GameObject mdlInst)
    {
        // Relink animations (Why the hell do they get unlinked, anyway?!! Stupid!!)
        AnimationUtility.SetAnimationClips(mdlInst.animation,
                                           AnimationUtility.GetAnimationClips(mdlBase.animation));

        // Refresh the morph controller
        MorphController base_mctrl = mdlBase.GetComponent<MorphController>();
        MorphController inst_mctrl = mdlInst.GetComponent<MorphController>();
        if (base_mctrl != null && inst_mctrl != null)
        {
            // Refresh morph channels
            inst_mctrl.morphChannels = new MorphChannel[base_mctrl.morphChannels.Length];
            for (int mci = 0; mci < base_mctrl.morphChannels.Length; ++mci)
            {
                MorphChannel base_mc = base_mctrl.morphChannels[mci];
                MorphChannel inst_mc = new MorphChannel();
                inst_mctrl.morphChannels[mci] = inst_mc;

                inst_mc.name = base_mc.name;
                inst_mc.weight = base_mc.weight;

                // Refresh morph target mappings
                inst_mc.morphTargets = new MorphChannel.MorphTargetMapping[base_mc.morphTargets.Length];
                for (int mti = 0; mti < base_mc.morphTargets.Length; ++mti)
                {
                    MorphChannel.MorphTargetMapping base_mtm = base_mc.morphTargets[mti];
                    MorphChannel.MorphTargetMapping inst_mtm = new MorphChannel.MorphTargetMapping();
                    inst_mc.morphTargets[mti] = inst_mtm;

                    inst_mtm.morphTargetIndexes = new int[base_mtm.morphTargetIndexes.Length];
                    base_mtm.morphTargetIndexes.CopyTo(inst_mtm.morphTargetIndexes, 0);
                    inst_mtm.refValues = new float[base_mtm.refValues.Length];
                    base_mtm.refValues.CopyTo(inst_mtm.refValues, 0);
                }

                // Refresh bone mappings
                inst_mc.bones = new MorphChannel.BoneMapping[base_mc.bones.Length];
                for (int bmi = 0; bmi < base_mc.bones.Length; ++bmi)
                {
                    MorphChannel.BoneMapping base_bm = base_mc.bones[bmi];
                    MorphChannel.BoneMapping inst_bm = new MorphChannel.BoneMapping();
                    inst_mc.bones[bmi] = inst_bm;

                    inst_bm.bone = ModelUtils.FindBone(mdlInst.transform, base_bm.bone.name);
                    inst_bm.refPosition = base_bm.refPosition;
                    inst_bm.refRotation = base_bm.refRotation;
                }

                // Refresh subchannel mappings
                inst_mc.subchannels = new MorphChannel.SubchannelMapping[base_mc.subchannels.Length];
                for (int smci = 0; smci < base_mc.subchannels.Length; ++smci)
                {
                    MorphChannel.SubchannelMapping base_smc = base_mc.subchannels[smci];
                    MorphChannel.SubchannelMapping inst_smc = new MorphChannel.SubchannelMapping();
                    inst_mc.subchannels[smci] = inst_smc;

                    inst_smc.subchannelIndex = base_smc.subchannelIndex;
                    inst_smc.refValue = base_smc.refValue;
                }
            }
        }
    }

    /// <summary>
    /// Create a new animation clip on the specified character model.
    /// </summary>
    /// <param name="animationClipName">Animation clip name</param>
    /// <param name="model">Character model</param>
    /// <returns>Animation clip</returns>
    public static AnimationClip CreateAnimationClipOnModel(string animationClipName, GameObject model)
    {
        // Does the model already have an animation clip with that name? If yes, then remove it
        var animationComponent = model.animation;
        foreach (AnimationState animationState in animationComponent)
        {
            if (animationState.name == animationClipName && animationState.clip != null)
            {
                animationComponent.RemoveClip(animationState.clip);
                break;
            }
        }

        // Create the new animation clip
        var newClip = new AnimationClip();
        newClip.name = animationClipName;
        AnimationUtility.SetAnimationType(newClip, ModelImporterAnimationType.Legacy);
        animationComponent.AddClip(newClip, newClip.name);

        return newClip;
    }

    /// <summary>
    /// Get an animation clip from the specified character model.
    /// </summary>
    /// <param name="animationClipName">Animation clip name</param>
    /// <param name="model">Character model</param>
    /// <returns>Animation clip</returns>
    public static AnimationClip GetAnimationClipOnModel(string animationClipName, GameObject model)
    {
        AnimationClip clip = null;
        var animationComponent = model.gameObject.animation;
        foreach (AnimationState animationState in animationComponent)
        {
            if (animationState.name == animationClipName)
                clip = animationState.clip;
        }

        return clip;
    }

    /// <summary>
    /// Get all animation clips from the specified character model.
    /// </summary>
    /// <param name="model">Character model</param>
    /// <returns>Array of animation clips</returns>
    public static AnimationClip[] GetAllAnimationClipsOnModel(GameObject model)
    {
        var animationComponent = model.gameObject.animation;
        var animationClips = new List<AnimationClip>();
        foreach (AnimationState animationState in animationComponent)
        {
            if (animationState.clip != null)
                animationClips.Add(animationState.clip);
        }

        return animationClips.ToArray();
    }

    /// <summary>
    /// Create an array of empty animation curves for specified character model.
    /// </summary>
    /// <param name="model">Character model</param>
    /// <returns>Animation curves</returns>
    public static AnimationCurve[] CreateAnimationCurvesForModel(GameObject model)
    {
        Transform[] bones = ModelUtils.GetAllBones(model);
        var modelController = model.GetComponent<ModelController>();
        int numBlendShapes = modelController.NumberOfBlendShapes;

        // 3 properties for the root position, 4 for the rotation of each bone (incl. root), 1 for each blend shape
        AnimationCurve[] curves = new AnimationCurve[3 + bones.Length * 4 + numBlendShapes];
        for (int curveIndex = 0; curveIndex < curves.Length; ++curveIndex)
            curves[curveIndex] = new AnimationCurve();

        return curves;
    }

    /// <summary>
    /// Get all animation curves from the specified animation clip.
    /// </summary>
    /// <param name="model">Character model</param>
    /// <param name="clip">Animation clip</param>
    /// <returns>Animation curves</returns>
    public static AnimationCurve[] GetAnimationCurvesFromClip(GameObject model, AnimationClip clip)
    {
        var modelController = model.GetComponent<ModelController>();
        int numBlendShapes = modelController.NumberOfBlendShapes;
        AnimationCurve[] curves = new AnimationCurve[3 + modelController.NumberOfBones * 4 + numBlendShapes];
        AnimationClipCurveData[] curveData = AnimationUtility.GetAllCurves(clip, true);

        // Create empty curves for all bone properties
        int curveIndex = 0;
        for (curveIndex = 0; curveIndex < curves.Length; ++curveIndex)
        {
            curves[curveIndex] = new AnimationCurve();
            curves[curveIndex].keys = null;
        }

        // Get curve data from the animation clip for each bone
        for (int boneIndex = 0; boneIndex < modelController.NumberOfBones; ++boneIndex)
        {
            var bone = modelController[boneIndex];
            string bonePath = ModelUtils.GetBonePath(bone);
            var curveDataForBone = curveData.Where(cd => cd.path == bonePath);
            curveIndex = 3 + boneIndex * 4;

            foreach (var curveDataForBoneProperty in curveDataForBone)
            {
                switch (curveDataForBoneProperty.propertyName)
                {
                    case "m_LocalPosition.x":

                        if (boneIndex == 0)
                        {
                            curves[0] = curveDataForBoneProperty.curve;
                        }

                        break;

                    case "m_LocalPosition.y":

                        if (boneIndex == 0)
                        {
                            curves[1] = curveDataForBoneProperty.curve;
                        }

                        break;

                    case "m_LocalPosition.z":

                        if (boneIndex == 0)
                        {
                            curves[2] = curveDataForBoneProperty.curve;
                        }

                        break;

                    case "m_LocalRotation.x":

                        curves[curveIndex] = curveDataForBoneProperty.curve;
                        break;

                    case "m_LocalRotation.y":

                        curves[curveIndex + 1] = curveDataForBoneProperty.curve;
                        break;

                    case "m_LocalRotation.z":

                        curves[curveIndex + 2] = curveDataForBoneProperty.curve;
                        break;

                    case "m_LocalRotation.w":

                        curves[curveIndex + 3] = curveDataForBoneProperty.curve;
                        break;

                    default:

                        break;
                }
            }
        }

        // Get curve data from the animation clip for each blend shape
        for (int blendShapeIndex = 0; blendShapeIndex < numBlendShapes; ++blendShapeIndex)
        {
            // Get blend shape info
            SkinnedMeshRenderer meshWithBlendShape = null;
            int blendShapeIndexWithinMesh = 0;
            modelController.GetBlendShape(blendShapeIndex, out meshWithBlendShape, out blendShapeIndexWithinMesh);
            string blendShapeName = "blendShape." + meshWithBlendShape.sharedMesh.GetBlendShapeName(blendShapeIndexWithinMesh);

            // Get curve data for blend shapes
            string blendShapePath = ModelUtils.GetBonePath(meshWithBlendShape.gameObject.transform);
            var curveDataForMesh = curveData.Where(cd => cd.path == blendShapePath && cd.propertyName == blendShapeName);
            curveIndex = 3 + modelController.NumberOfBones * 4 + blendShapeIndex;
            if (curveDataForMesh != null & curveDataForMesh.Count() > 0)
                curves[curveIndex] = curveDataForMesh.ToArray()[0].curve;
        }

        return curves;
    }

    /// <summary>
    /// Set animation curves on a clip animating a character model.
    /// </summary>
    /// <param name="model">Character model</param>
    /// <param name="clip">Animation clip</param>
    /// <param name="curves">Animation curves</param>
    public static void SetAnimationCurvesOnClip(GameObject model, AnimationClip clip, AnimationCurve[] curves)
    {
        Transform[] bones = ModelUtils.GetAllBones(model);
        var modelController = model.GetComponent<ModelController>();

        // Set curves for bone properties
        for (int boneIndex = 0; boneIndex < bones.Length; ++boneIndex)
        {
            var bone = bones[boneIndex];
            string bonePath = ModelUtils.GetBonePath(bone);

            if (boneIndex == 0)
            {
                // Set position curves on root bone
                if (curves[0].keys.Length > 0)
                    clip.SetCurve(bonePath, typeof(Transform), "localPosition.x", curves[0]);
                if (curves[1].keys.Length > 0)
                    clip.SetCurve(bonePath, typeof(Transform), "localPosition.y", curves[1]);
                if (curves[2].keys.Length > 0)
                    clip.SetCurve(bonePath, typeof(Transform), "localPosition.z", curves[2]);
            }

            // Set rotation curves
            if (curves[3 + boneIndex * 4].keys.Length > 0)
                clip.SetCurve(bonePath, typeof(Transform), "localRotation.x", curves[3 + boneIndex * 4]);
            if (curves[3 + boneIndex * 4 + 1].keys.Length > 0)
                clip.SetCurve(bonePath, typeof(Transform), "localRotation.y", curves[3 + boneIndex * 4 + 1]);
            if (curves[3 + boneIndex * 4 + 2].keys.Length > 0)
                clip.SetCurve(bonePath, typeof(Transform), "localRotation.z", curves[3 + boneIndex * 4 + 2]);
            if (curves[3 + boneIndex * 4 + 3].keys.Length > 0)
                clip.SetCurve(bonePath, typeof(Transform), "localRotation.w", curves[3 + boneIndex * 4 + 3]);
        }

        // Set curves for blend shapes
        int numBlendShapes = modelController.NumberOfBlendShapes;
        for (int blendShapeIndex = 0; blendShapeIndex < numBlendShapes; ++blendShapeIndex)
        {
            // Get blend shape info
            SkinnedMeshRenderer meshWithBlendShape = null;
            int blendShapeIndexWithinMesh = 0;
            modelController.GetBlendShape(blendShapeIndex, out meshWithBlendShape, out blendShapeIndexWithinMesh);
            string blendShapeName = "blendShape." + meshWithBlendShape.sharedMesh.GetBlendShapeName(blendShapeIndexWithinMesh);
            string blendShapePath = ModelUtils.GetBonePath(meshWithBlendShape.gameObject.transform);

            if (curves[3 + bones.Length * 4 + blendShapeIndex].keys.Length > 0)
                clip.SetCurve(blendShapePath, typeof(SkinnedMeshRenderer), blendShapeName, curves[3 + bones.Length * 4 + blendShapeIndex]);
        }
    }

    /// <summary>
    /// Make sure animation clips added to the character model's Animation component are
    /// correctly associated with the model.
    /// </summary>
    /// <param name="model">Character model</param>
    public static void FixModelAnimationClipAssoc(GameObject model)
    {
        AnimationClip[] clips = GetAllAnimationClipsOnModel(model);
        Transform rootBone = ModelUtils.FindRootBone(model);

        foreach (var clip in clips)
        {
            AnimationClipCurveData[] allCurvesData = AnimationUtility.GetAllCurves(clip, true);
            clip.ClearCurves();

            foreach (var curveData in allCurvesData)
            {
                string boneName = curveData.path.LastIndexOf('/') >= 0 ?
                    curveData.path.Substring(curveData.path.LastIndexOf('/') + 1) : curveData.path;
                Transform bone = ModelUtils.FindBone(rootBone, boneName);

                if (bone != null)
                {
                    // Bone found, make sure the curve has the right path
                    string bonePath = ModelUtils.GetBonePath(bone);
                    clip.SetCurve(bonePath, curveData.type, curveData.propertyName, curveData.curve);
                }
            }
        }
    }

    /// <summary>
    /// Get asset directory of the specified character model.
    /// </summary>
    /// <param name="model">Character model</param>
    /// <param name="full">If true, full directory is returned</param>
    /// <returns>Asset directory</returns>
    public static string GetModelDirectory(GameObject model, bool full = false)
    {
        string path = AssetDatabase.GetAssetPath(model.animation["InitialPose"].clip);
        string dir = path.Substring(0, path.LastIndexOf('/') + 1);
        if (full)
        {
            dir = Application.dataPath + dir.Substring(dir.IndexOf('/'));
        }
        
        return dir;
    }

    /// <summary>
    /// Load end-effector constraint annotations for the specified animation clip.
    /// </summary>
    /// <param name="clip"></param>
    public static AnimationTimeline.EndEffectorConstraint[] LoadEndEffectorConstraintsForClip(AnimationClip clip)
    {
        string clipPath = AssetDatabase.GetAssetPath(clip);
        if (clipPath == "")
        {
            UnityEngine.Debug.LogWarning(string.Format("No asset file exists for animation clip " + clip.name));
            return null;
        }

        string eecPath = clipPath.Substring(0, clipPath.LastIndexOf('.')) + "#EEC.csv";
        List<AnimationTimeline.EndEffectorConstraint> constraints = new List<AnimationTimeline.EndEffectorConstraint>();
        
        if (!File.Exists(eecPath))
        {
            UnityEngine.Debug.LogWarning(string.Format("No end-effector constraint asset file exists for animation clip " + clip.name));
            return null;
        }

        try
        {
            var reader = new StreamReader(eecPath);
            bool firstLine = true;
            string line = "";
            string[] lineElements = null;
            Dictionary<string, int> attributeIndices = new Dictionary<string, int>();
            Dictionary<string, int> lastConstraintEndFrames = new Dictionary<string, int>();
            GameObject[] endEffectorTargets = GameObject.FindGameObjectsWithTag("EndEffectorTarget");
            
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
                    // Load constraint specification
                    lineElements = line.Split(",".ToCharArray());

                    // Get constraint data
                    string endEffector = lineElements[attributeIndices["EndEffector"]];
                    int startFrame = int.Parse(lineElements[attributeIndices["StartFrame"]]);
                    int frameLength = int.Parse(lineElements[attributeIndices["EndFrame"]]) - startFrame + 1;
                    bool preserveAbsoluteRotation = bool.Parse(lineElements[attributeIndices["PreserveAbsoluteRotation"]]);
                    string endEffectorTargetName = lineElements[attributeIndices["Target"]];
                    var endEffectorTarget = endEffectorTargetName == "null" ? null :
                        endEffectorTargets.FirstOrDefault(t => t.name == endEffectorTargetName);
                    int activationFrameLength = attributeIndices.ContainsKey("ActivationFrameLength") ?
                        int.Parse(lineElements[attributeIndices["ActivationFrameLength"]]) : LEAPCore.endEffectorConstraintActivationFrameLength;
                    int deactivationFrameLength = attributeIndices.ContainsKey("DeactivationFrameLength") ?
                        int.Parse(lineElements[attributeIndices["DeactivationFrameLength"]]) : LEAPCore.endEffectorConstraintActivationFrameLength;

                    if (lastConstraintEndFrames.ContainsKey(endEffector) && lastConstraintEndFrames[endEffector] >= startFrame)
                    {
                        UnityEngine.Debug.LogWarning(string.Format("Error in end-effector constraint asset file {0}: constraint ({1}, {2}, {3}) precedes or  overlaps another constraint on the same end-effector",
                            eecPath, endEffector, startFrame, startFrame + frameLength - 1));
                    }
                    else
                    {
                        // Add constraint
                        constraints.Add(new AnimationTimeline.EndEffectorConstraint(
                            endEffector, startFrame, frameLength, preserveAbsoluteRotation, endEffectorTarget,
                            activationFrameLength, deactivationFrameLength));
                        lastConstraintEndFrames[endEffector] = startFrame + frameLength - 1;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError(string.Format("Unable to load end-effector constraints from asset file {0}: {1}", eecPath, ex.Message));
            return null;
        }

        return constraints.ToArray();
    }
}

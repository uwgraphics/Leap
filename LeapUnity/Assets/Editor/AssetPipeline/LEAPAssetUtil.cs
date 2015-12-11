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
public static class LEAPAssetUtil
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

                    inst_bm.bone = ModelUtil.FindBone(mdlInst.transform, base_bm.bone.name);
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
    /// Add an animation to the specified character model.
    /// </summary>
    /// <param name="clip">Animation clip</param>
    /// <param name="model">Character model</param>
    public static void AddAnimationClipToModel(AnimationClip clip, GameObject model)
    {
        var animationComponent = model.gameObject.animation;
        foreach (AnimationState animationState in animationComponent)
        {
            if (animationState.clip != null && animationState.clip.name == clip.name)
            {
                animationComponent.RemoveClip(animationState.clip);
                break;
            }
        }

        animationComponent.AddClip(clip, clip.name);
    }

    /// <summary>
    /// Create an array of empty animation curves for specified character model.
    /// </summary>
    /// <param name="model">Character model</param>
    /// <returns>Animation curves</returns>
    public static AnimationCurve[] CreateAnimationCurvesForModel(GameObject model)
    {
        // Get all bones on the model
        Transform[] bones = null;
        var modelController = model.GetComponent<ModelController>();
        if (modelController == null)
        {
            bones = new Transform[1];
            bones[0] = model.transform;
        }
        else
        {
            bones = ModelUtil.GetAllBones(model);
        }
        int numBlendShapes = modelController == null ? 0 : modelController.NumberOfBlendShapes;

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
        int numBones = modelController == null ? 0 : modelController.NumberOfBones;
        int numBlendShapes = modelController == null ? 0 : modelController.NumberOfBlendShapes;
        AnimationCurve[] curves = new AnimationCurve[3 + numBones * 4 + numBlendShapes];
        AnimationClipCurveData[] curveData = AnimationUtility.GetAllCurves(clip, true);

        // Create empty curves for all bone properties
        int curveIndex = 0;
        for (curveIndex = 0; curveIndex < curves.Length; ++curveIndex)
        {
            curves[curveIndex] = new AnimationCurve();
            curves[curveIndex].keys = null;
        }

        // Get curve data from the animation clip for each bone
        for (int boneIndex = 0; boneIndex < numBones; ++boneIndex)
        {
            var bone = modelController.GetBone(boneIndex);
            string bonePath = ModelUtil.GetBonePath(bone);
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
            string blendShapePath = ModelUtil.GetBonePath(meshWithBlendShape.gameObject.transform);
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
        var modelController = model.GetComponent<ModelController>();
        Transform[] bones = null;
        if (modelController == null)
        {
            bones = new Transform[1];
            bones[0] = model.transform;
        }
        else
        {
            bones = ModelUtil.GetAllBones(model);
        }

        // Set curves for bone properties
        for (int boneIndex = 0; boneIndex < bones.Length; ++boneIndex)
        {
            var bone = bones[boneIndex];
            string bonePath = ModelUtil.GetBonePath(bone);

            if (boneIndex == 0)
            {
                // Set position curves on root bone
                if (curves[0] != null && curves[0].keys.Length > 0)
                    clip.SetCurve(bonePath, typeof(Transform), "localPosition.x", curves[0]);
                if (curves[1] != null && curves[1].keys.Length > 0)
                    clip.SetCurve(bonePath, typeof(Transform), "localPosition.y", curves[1]);
                if (curves[2] != null && curves[2].keys.Length > 0)
                    clip.SetCurve(bonePath, typeof(Transform), "localPosition.z", curves[2]);
            }

            // Set rotation curves
            var curve = curves[3 + boneIndex * 4];
            if (curve != null && curve.keys.Length > 0)
                clip.SetCurve(bonePath, typeof(Transform), "localRotation.x", curve);
            curve = curves[3 + boneIndex * 4 + 1];
            if (curve != null && curve.keys.Length > 0)
                clip.SetCurve(bonePath, typeof(Transform), "localRotation.y", curve);
            curve = curves[3 + boneIndex * 4 + 2];
            if (curve != null && curve.keys.Length > 0)
                clip.SetCurve(bonePath, typeof(Transform), "localRotation.z", curve);
            curve = curves[3 + boneIndex * 4 + 3];
            if (curve != null && curve.keys.Length > 0)
                clip.SetCurve(bonePath, typeof(Transform), "localRotation.w", curve);
        }

        // Set curves for blend shapes
        int numBlendShapes = modelController == null ? 0 : modelController.NumberOfBlendShapes;
        for (int blendShapeIndex = 0; blendShapeIndex < numBlendShapes; ++blendShapeIndex)
        {
            // Get blend shape info
            SkinnedMeshRenderer meshWithBlendShape = null;
            int blendShapeIndexWithinMesh = 0;
            modelController.GetBlendShape(blendShapeIndex, out meshWithBlendShape, out blendShapeIndexWithinMesh);
            string blendShapeName = "blendShape." + meshWithBlendShape.sharedMesh.GetBlendShapeName(blendShapeIndexWithinMesh);
            string blendShapePath = ModelUtil.GetBonePath(meshWithBlendShape.gameObject.transform);

            var curve = curves[3 + bones.Length * 4 + blendShapeIndex];
            if (curve != null && curve.keys.Length > 0)
                clip.SetCurve(blendShapePath, typeof(SkinnedMeshRenderer), blendShapeName, curve);
        }
    }

    /// <summary>
    /// Copy the animation curve from one property to another.
    /// </summary>
    /// <param name="clip">Animation clip</param>
    /// <param name="fromProperty"></param>
    /// <param name="toProperty"></param>
    public static void CopyAnimationCurveFromToProperty(AnimationClip clip,
        Type propertyType, string fromPath, string fromProperty, string toPath, string toProperty)
    {
        AnimationClipCurveData[] allCurveData = AnimationUtility.GetAllCurves(clip);

        // Get original animation curve data
        var fromCurveData = allCurveData.FirstOrDefault(fc =>
            fc.path == fromPath && fc.type == propertyType && fc.propertyName == fromProperty);
        if (fromCurveData == null)
        {
            UnityEngine.Debug.LogError(
                string.Format("Trying to copy animation curve for non-existent property {0}, {1}, {2}",
                fromPath, propertyType.Name, fromProperty)
                );
        }

        clip.SetCurve(toPath, propertyType, toProperty, fromCurveData.curve);
    }

    /// <summary>
    /// Make sure animation clips added to the character model's Animation component are
    /// correctly associated with the model.
    /// </summary>
    /// <param name="model">Character model</param>
    public static void FixModelAnimationClipAssoc(GameObject model)
    {
        AnimationClip[] clips = GetAllAnimationClipsOnModel(model);
        Transform rootBone = ModelUtil.FindRootBone(model);

        foreach (var clip in clips)
        {
            AnimationClipCurveData[] allCurvesData = AnimationUtility.GetAllCurves(clip, true);
            clip.ClearCurves();

            foreach (var curveData in allCurvesData)
            {
                string boneName = curveData.path.LastIndexOf('/') >= 0 ?
                    curveData.path.Substring(curveData.path.LastIndexOf('/') + 1) : curveData.path;
                Transform bone = ModelUtil.FindBone(rootBone, boneName);

                if (bone != null)
                {
                    // Bone found, make sure the curve has the right path
                    string bonePath = ModelUtil.GetBonePath(bone);
                    clip.SetCurve(bonePath, curveData.type, curveData.propertyName, curveData.curve);
                }
            }
        }
    }

    /// <summary>
    /// Create default bone masks for animation tracks.
    /// </summary>
    /// <param name="model">Character model</param>
    /// <returns>Bone masks</returns>
    public static AnimationBoneMask[] CreateDefaultAnimationTrackBoneMasks(GameObject model)
    {
        // Get the bones animated by each track
        var rootBones = ModelUtil.GetAllBonesWithTag(model, "RootBone");
        var headBones = ModelUtil.GetAllBonesWithTag(model, "HeadBone");
        var gazeBones = rootBones.Length > 0 && headBones.Length > 0 ?
            ModelUtil.GetBoneChain(rootBones[0], headBones[0]).Union(ModelUtil.GetAllBonesInSubtree(headBones[0])).ToArray() :
            new Transform[0];
        var lShoulderBones = ModelUtil.GetAllBonesWithTag(model, "LShoulderBone");
        var lArmGestureBones = lShoulderBones.Length > 0 ?
            ModelUtil.GetAllBonesInSubtree(lShoulderBones[0]) :
            new Transform[0];
        var rShoulderBones = ModelUtil.GetAllBonesWithTag(model, "RShoulderBone");
        var rArmGestureBones = rShoulderBones.Length > 0 ?
            ModelUtil.GetAllBonesInSubtree(rShoulderBones[0]) :
            new Transform[0];
        var postureBones = rootBones.Length > 0 && headBones.Length > 0 ?
            ModelUtil.GetBoneChain(rootBones[0], headBones[0].parent) :
            new Transform[0];
        var lHipBones = ModelUtil.GetAllBonesWithTag(model, "LHipBone");
        var rHipBones = ModelUtil.GetAllBonesWithTag(model, "RHipBone");
        var locomotionBones = lHipBones.Length > 0 && rHipBones.Length > 0 ?
            ModelUtil.GetAllBonesInSubtree(lHipBones[0]).Union(ModelUtil.GetAllBonesInSubtree(rHipBones[0])).ToArray() :
            new Transform[0];

        // Initialize gaze bone mask
        AnimationBoneMask gazeBoneMask = null;
        if (gazeBones.Length > 0)
        {
            gazeBoneMask = new AnimationBoneMask();
            foreach (var gazeBone in gazeBones)
                gazeBoneMask.SetBoneWeight(gazeBone, 1f);
        }


        // Initialize posture bone mask
        AnimationBoneMask postureBoneMask = null;
        if (postureBones.Length > 0)
        {
            postureBoneMask = new AnimationBoneMask();
            /*foreach (var postureBone in postureBones)
                postureBoneMask.SetBoneWeight(postureBone, 1f);*/
        }

        // Initialize left arm gesture mask
        AnimationBoneMask lArmGestureBoneMask = null;
        if (lArmGestureBones.Length > 0)
        {
            lArmGestureBoneMask = new AnimationBoneMask();
            foreach (var lArmGestureBone in lArmGestureBones)
                lArmGestureBoneMask.SetBoneWeight(lArmGestureBone, 1f);
        }

        // Initialize right arm gesture mask
        AnimationBoneMask rArmGestureBoneMask = null;
        if (rArmGestureBones.Length > 0)
        {
            rArmGestureBoneMask = new AnimationBoneMask();
            foreach (var rArmGestureBone in rArmGestureBones)
                rArmGestureBoneMask.SetBoneWeight(rArmGestureBone, 1f);
        }

        // Initialize locomotion bone mask
        AnimationBoneMask locomotionBoneMask = null;
        if (locomotionBones.Length > 0)
        {
            locomotionBoneMask = new AnimationBoneMask();
            foreach (var locomotionBone in locomotionBones)
                locomotionBoneMask.SetBoneWeight(locomotionBone, 1f);
            locomotionBoneMask.RootPositionWeight = 1f;
        }

        // Create an array of bone masks
        AnimationBoneMask[] boneMasks = new AnimationBoneMask[Enum.GetValues(typeof(AnimationTrackType)).Length];
        boneMasks[(int)AnimationTrackType.Gaze] = gazeBoneMask;
        boneMasks[(int)AnimationTrackType.Posture] = postureBoneMask;
        boneMasks[(int)AnimationTrackType.LArmGesture] = lArmGestureBoneMask;
        boneMasks[(int)AnimationTrackType.RArmGesture] = rArmGestureBoneMask;
        boneMasks[(int)AnimationTrackType.Locomotion] = locomotionBoneMask;

        return boneMasks;
    }

    /// <summary>
    /// Create separate clips for animation tracks in the specified original clip.
    /// </summary>
    /// <param name="model">Character model</param>
    /// <param name="clip">Animation clip</param>
    /// <param name="boneMasks">Bone masks that define which bones are affected by each track</param>
    /// <returns>Track animation clips</returns>
    public static AnimationClip[] CreateAnimationClipsForTracks(GameObject model, AnimationClip clip,
        AnimationBoneMask[] boneMasks)
    {
        // Get model bones and curves
        var modelController = model.GetComponent<ModelController>();
        int numBones = modelController == null ? 0 : modelController.NumberOfBones;
        var curves = GetAnimationCurvesFromClip(model, clip);

        // Create track clips
        var trackTypes = (AnimationTrackType[])Enum.GetValues(typeof(AnimationTrackType));
        AnimationClip[] trackClips = new AnimationClip[trackTypes.Length];
        for (int trackIndex = 0; trackIndex < trackClips.Length; ++trackIndex)
        {
            if (boneMasks[trackIndex] == null)
            {
                trackClips[trackIndex] = null;
                continue;
            }

            trackClips[trackIndex] = CreateAnimationClipOnModel(clip.name + "#" + trackTypes[trackIndex].ToString(), model);

            // Create an array of curves on the current animation track
            var trackCurves = new AnimationCurve[curves.Length];
            for (int boneIndex = 0; boneIndex < numBones; ++boneIndex)
            {
                var bone = modelController.GetBone(boneIndex);

                if (boneIndex == 0 && boneMasks[trackIndex].RootPositionWeight > 0f)
                {
                    trackCurves[0] = curves[0];
                    trackCurves[1] = curves[1];
                    trackCurves[2] = curves[2];
                }

                if (boneMasks[trackIndex].GetBoneWeight(bone) > 0f)
                {
                    trackCurves[3 + boneIndex * 4] = curves[3 + boneIndex * 4];
                    trackCurves[3 + boneIndex * 4 + 1] = curves[3 + boneIndex * 4 + 1];
                    trackCurves[3 + boneIndex * 4 + 2] = curves[3 + boneIndex * 4 + 2];
                    trackCurves[3 + boneIndex * 4 + 3] = curves[3 + boneIndex * 4 + 3];
                }
            }

            // Set the curves on the track animation clip
            SetAnimationCurvesOnClip(model, trackClips[trackIndex], trackCurves);
        }

        return trackClips;
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
    public static EndEffectorConstraint[] LoadEndEffectorConstraintsForClip(GameObject model, AnimationClip clip)
    {
        // Get end-effector constraint annotations file path
        string eecPath = Application.dataPath + LEAPCore.endEffectorConstraintAnnotationsDirectory.Substring(
            LEAPCore.endEffectorConstraintAnnotationsDirectory.IndexOfAny(@"/\".ToCharArray()));
        if (eecPath[eecPath.Length - 1] != '/' && eecPath[eecPath.Length - 1] != '\\')
            eecPath += '/';
        eecPath += (clip.name + ".csv");
        List<EndEffectorConstraint> constraints = new List<EndEffectorConstraint>();
        
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
            GameObject[] manipulatedObjectHandles = GameObject.FindGameObjectsWithTag("ManipulatedObjectHandle");
            
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
                    string endEffectorTag = lineElements[attributeIndices["EndEffector"]];
                    int startFrame = int.Parse(lineElements[attributeIndices["StartFrame"]]);
                    int frameLength = int.Parse(lineElements[attributeIndices["EndFrame"]]) - startFrame + 1;
                    bool preserveAbsoluteRotation = bool.Parse(lineElements[attributeIndices["PreserveAbsoluteRotation"]]);
                    string endEffectorTargetName = lineElements[attributeIndices["Target"]];
                    var endEffectorTarget = endEffectorTargetName == "null" ? ModelUtil.GetEndEffectorTargetHelper(model, endEffectorTag) :
                        endEffectorTargets.FirstOrDefault(t => t.name == endEffectorTargetName);
                    string manipulatedObjectHandleName = attributeIndices.ContainsKey("ManipulatedObjectHandle") ?
                        lineElements[attributeIndices["ManipulatedObjectHandle"]] : "null";
                    var manipulatedObjectHandle = manipulatedObjectHandleName == "null" ? null :
                        manipulatedObjectHandles.FirstOrDefault(t => t.name == manipulatedObjectHandleName);
                    int manipulationEndFrame = manipulatedObjectHandle != null && attributeIndices.ContainsKey("ManipulationEndFrame") ?
                        int.Parse(lineElements[attributeIndices["ManipulationEndFrame"]]) : -1;
                    int manipulationFrameLength = manipulationEndFrame >= 0 ? manipulationEndFrame - startFrame + 1 : -1;
                    int activationFrameLength = attributeIndices.ContainsKey("ActivationFrameLength") ?
                        int.Parse(lineElements[attributeIndices["ActivationFrameLength"]]) :
                        Mathf.RoundToInt(LEAPCore.editFrameRate * LEAPCore.endEffectorConstraintActivationTime);
                    int deactivationFrameLength = attributeIndices.ContainsKey("DeactivationFrameLength") ?
                        int.Parse(lineElements[attributeIndices["DeactivationFrameLength"]]) :
                        Mathf.RoundToInt(LEAPCore.editFrameRate * LEAPCore.endEffectorConstraintActivationTime);

                    // Add constraint
                    constraints.Add(new EndEffectorConstraint(
                        endEffectorTag, startFrame, frameLength, preserveAbsoluteRotation, endEffectorTarget,
                        manipulatedObjectHandle, manipulationFrameLength, activationFrameLength, deactivationFrameLength));
                    lastConstraintEndFrames[endEffectorTag] = startFrame + frameLength - 1;
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

    /// <summary>
    /// Initialize animations of helper targets for end-effector constraints that constrain
    /// end-effectors to follow trajectories encoded in the original animation.
    /// </summary>
    /// <param name="model">Character model</param>
    /// <param name="clip">Original animation clip</param>
    /// <param name="endEffectorConstraints">End-effector constraints</param>
    /// <returns>End-effector target helper animation specifications</returns>
    public static EndEffectorTargetHelperAnimation[] InitEndEffectorTargetHelperAnimations(GameObject model, AnimationClip clip)
    {
        var endEffectors = ModelUtil.GetEndEffectors(model);
        var helpers = new Dictionary<string, GameObject>();
        var helperClips = new Dictionary<string, AnimationClip>();
        var helperCurves = new Dictionary<string, AnimationCurve[]>();
        var helperClipCreated = new Dictionary<string, bool>();

        // Create target helper objects and clips for end-effectors constraints
        var endEffectorTargets = GameObject.FindGameObjectsWithTag("EndEffectorTarget");
        foreach (var endEffector in endEffectors)
        {
            // Get end-effector target helper
            var helper = ModelUtil.GetEndEffectorTargetHelper(model, endEffector.tag);
            helpers[endEffector.tag] = helper;
            
            // Create a helper target animation clip for the current end-effector (if one does not exist already)
            if (helper.GetComponent<Animation>() == null)
                helper.AddComponent<Animation>();
            string helperClipName = clip.name + "-" + helper.name;
            var helperClip = GetAnimationClipOnModel(helperClipName, helper);
            helperClipCreated[endEffector.tag] = helperClip == null;
            if (helperClip == null)
                helperClip = CreateAnimationClipOnModel(helperClipName, helper);
            helperClips[endEffector.tag] = helperClip;

            // Also create animation curves for the clip
            helperCurves[endEffector.tag] = new AnimationCurve[7];
            for (int helperCurveIndex = 0; helperCurveIndex < helperCurves[endEffector.tag].Length; ++helperCurveIndex)
                helperCurves[endEffector.tag][helperCurveIndex] = new AnimationCurve();
        }

        if (helperClipCreated.Any(kvp => kvp.Value))
        {
            // Bake end-effector movements into helper animations
            var instance = new AnimationClipInstance(clip.name, model, false, false);
            for (int frameIndex = 0; frameIndex < instance.FrameLength; ++frameIndex)
            {
                instance.Apply(frameIndex, AnimationLayerMode.Override);

                // Compute key time
                float time = ((float)frameIndex) / LEAPCore.editFrameRate;

                foreach (var kvp in helperClips)
                {
                    var helperClip = kvp.Value;
                    var helperCurveSet = helperCurves[kvp.Key];
                    var endEffector = ModelUtil.GetAllBonesWithTag(model, kvp.Key)[0];

                    if (!helperClipCreated[endEffector.tag])
                        continue;

                    // Key the animation
                    helperCurveSet[0].AddKey(time, endEffector.position.x);
                    helperCurveSet[1].AddKey(time, endEffector.position.y);
                    helperCurveSet[2].AddKey(time, endEffector.position.z);
                    helperCurveSet[3].AddKey(time, endEffector.rotation.x);
                    helperCurveSet[4].AddKey(time, endEffector.rotation.y);
                    helperCurveSet[5].AddKey(time, endEffector.rotation.z);
                    helperCurveSet[6].AddKey(time, endEffector.rotation.w);
                }
            }

            // Set the animation curves on each helper clip
            foreach (var kvp in helperClips)
            {
                var helper = helpers[kvp.Key];
                var helperClip = kvp.Value;
                var helperCurveSet = helperCurves[kvp.Key];
                var endEffector = ModelUtil.GetAllBonesWithTag(model, kvp.Key)[0];

                if (!helperClipCreated[endEffector.tag])
                    continue;

                helperClip.SetCurve("", typeof(Transform), "localPosition.x", helperCurveSet[0]);
                helperClip.SetCurve("", typeof(Transform), "localPosition.y", helperCurveSet[1]);
                helperClip.SetCurve("", typeof(Transform), "localPosition.z", helperCurveSet[2]);
                helperClip.SetCurve("", typeof(Transform), "localRotation.x", helperCurveSet[3]);
                helperClip.SetCurve("", typeof(Transform), "localRotation.y", helperCurveSet[4]);
                helperClip.SetCurve("", typeof(Transform), "localRotation.z", helperCurveSet[5]);
                helperClip.SetCurve("", typeof(Transform), "localRotation.w", helperCurveSet[6]);

                // Write helper animation clip to file
                string path = LEAPAssetUtil.GetModelDirectory(model) + helperClip.name + ".anim";
                if (AssetDatabase.GetAssetPath(helperClip) != path)
                {
                    AssetDatabase.DeleteAsset(path);
                    AssetDatabase.CreateAsset(helperClip, path);
                }
                AssetDatabase.SaveAssets();

                // Re-add the clip to its model
                LEAPAssetUtil.AddAnimationClipToModel(helperClip, helper);
            }
        }

        // Return the end-effector target helper animation clips
        var endEffectorTargetHelperAnimations = new EndEffectorTargetHelperAnimation[endEffectors.Length];
        for (int endEffectorIndex = 0; endEffectorIndex < endEffectors.Length; ++endEffectorIndex)
        {
            var endEffector = endEffectors[endEffectorIndex];
            endEffectorTargetHelperAnimations[endEffectorIndex].endEffectorTag = endEffector.tag;
            endEffectorTargetHelperAnimations[endEffectorIndex].helper = helpers[endEffector.tag];
            endEffectorTargetHelperAnimations[endEffectorIndex].helperAnimationClip = helperClips[endEffector.tag];
        }

        return endEffectorTargetHelperAnimations;
    }
}

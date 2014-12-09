using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Diagnostics;
using System.IO;

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
        ModelController inst_mdlctrl = mdlInst.GetComponent<ModelController>();
        if (base_mctrl != null && inst_mctrl != null)
        {
            // Refresh morph targets
            inst_mctrl.morphTargets = new MorphTarget[base_mctrl.morphTargets.Length];
            for (int mti = 0; mti < base_mctrl.morphTargets.Length; ++mti)
            {
                MorphTarget base_mt = base_mctrl.morphTargets[mti];
                MorphTarget inst_mt = new MorphTarget(base_mt.name);
                inst_mctrl.morphTargets[mti] = inst_mt;

                inst_mt.vertexIndices = new int[base_mt.vertexIndices.Length];
                inst_mt.relVertices = new Vector3[base_mt.relVertices.Length];
                inst_mt.relNormals = new Vector3[base_mt.relNormals.Length];
                for (int mtvi = 0; mtvi < inst_mt.vertexIndices.Length; ++mtvi)
                {
                    inst_mt.vertexIndices[mtvi] = base_mt.vertexIndices[mtvi];
                    inst_mt.relVertices[mtvi] = base_mt.relVertices[mtvi];
                    inst_mt.relNormals[mtvi] = base_mt.relNormals[mtvi];
                }
            }

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
    /// Create an array of animation curves for specified character model.
    /// </summary>
    /// <param name="model">Character model</param>
    /// <returns>Animation curves</returns>
    public static AnimationCurve[] CreateAnimationCurvesForModel(GameObject model)
    {
        Transform[] bones = ModelUtils.GetAllBones(model.gameObject);
        // 3 properties for the root position, 4 for the rotation of each bone (incl. root)
        AnimationCurve[] curves = new AnimationCurve[3 + bones.Length * 4];
        for (int curveIndex = 0; curveIndex < curves.Length; ++curveIndex)
            curves[curveIndex] = new AnimationCurve();

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
    }

    public static string GetModelDirectory(GameObject model)
    {
        string path = AssetDatabase.GetAssetPath(model.animation["InitialPose"].clip);
        return path.Substring(0, path.LastIndexOf('/') + 1);
    }
}

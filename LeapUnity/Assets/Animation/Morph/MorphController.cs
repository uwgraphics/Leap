
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

/// <summary>
/// Controller for morph deformation of meshes.
/// </summary>
[RequireComponent(typeof(ModelController))]
public class MorphController : MonoBehaviour
{
    /// <summary>
    /// Morph channels for controlling model deformation.
    /// </summary>
    public MorphChannel[] morphChannels = null;

    private Dictionary<string, int> morphChannelIndexes = null;
    private ModelController mdlCtrl;

    /// <summary>
    /// Find morph channel by name. 
    /// </summary>
    /// <param name="mcName">
    /// Morph channel name. <see cref="System.String"/>
    /// </param>
    /// <returns>
    /// Morph channel, or null if it could not be found. <see cref="MorphChannel"/>
    /// </returns>
    public MorphChannel GetMorphChannel(string mcName)
    {
        if (morphChannelIndexes == null)
            _CacheMorphChannelIndexes();

        if (morphChannelIndexes.ContainsKey(mcName))
            return morphChannels[morphChannelIndexes[mcName]];

        return null;
    }

    /// <summary>
    /// Find morph channel index by name. 
    /// </summary>
    /// <param name="mtName">
    /// Morph channel name. <see cref="System.String"/>
    /// </param>
    /// <returns>
    /// Morph channel index, or -1 if it could not be found. <see cref="MeshArray"/>
    /// </returns>
    public int GetMorphChannelIndex(string mcName)
    {
        if (morphChannelIndexes == null)
            _CacheMorphChannelIndexes();

        if (morphChannelIndexes.ContainsKey(mcName))
            return morphChannelIndexes[mcName];

        return -1;
    }

    /// <summary>
    /// Initializes the morph controller from user-specified data. 
    /// </summary>
    /// <returns>
    /// true if initialization successful, false otherwise.
    /// </returns>
    public bool Init()
    {
        Debug.Log("Initializing a Morph Controller on agent " + gameObject.name);

        if (morphChannels == null || morphChannels.Length <= 0)
        {
            // No morph deformation channels specified
            return false;
        }

        // Are all morph channels correctly initialized?
        for (int i = 0; i < morphChannels.Length; i++)
        {
            if (morphChannels[i] == null)
            {
                Debug.Log("Morph channel " + i + " has not been assigned");
                return false;
            }

            // Set working bones
            for (int bone_i = 0; bone_i < morphChannels[i].bones.Length; ++bone_i)
            {
                Transform src_bone = morphChannels[i].bones[bone_i].bone;
                morphChannels[i].bones[bone_i].bone = ModelUtil.FindBone(transform, src_bone.name);
            }
        }

        _CacheMorphChannelIndexes();

        return true;
    }

    public void Awake()
    {
        Init();

        // Get model controller (needed to access initial bone transforms)
        mdlCtrl = gameObject.GetComponent<ModelController>();
    }

    public void Update()
    {
    }

    public void LateUpdate()
    {
        Apply();
    }

    /// <summary>
    /// Apply deformation to the model.
    /// </summary>
    public void Apply()
    {
        // Make sure all affected facial bones are set to initial pose
        for (int mci = 0; mci < morphChannels.Length; ++mci)
        {
            MorphChannel mc = morphChannels[mci];

            for (int bone_i = 0; bone_i < mc.bones.Length; ++bone_i)
            {
                Transform bone = mc.bones[bone_i].bone;
                bone.localPosition = mdlCtrl.GetInitPosition(bone);
                bone.localRotation = mdlCtrl.GetInitRotation(bone);
            }
        }

        // Apply morph target deformations on each channel
        for (int mci = 0; mci < morphChannels.Length; mci++)
        {
            MorphChannel mc = morphChannels[mci];
            float weight = mc.weight;

            for (int mtmi = 0; mtmi < mc.morphTargets.Length; ++mtmi)
            {
                MorphChannel.MorphTargetMapping mtm = mc.morphTargets[mtmi];

                var smr = mtm.sourceMesh.GetComponent<SkinnedMeshRenderer>();
                if (smr != null)
                {
                    for (int mtii = 0; mtm.morphTargetIndexes != null && mtii < mtm.morphTargetIndexes.Length; ++mtii)
                    {
                        int mti = mtm.morphTargetIndexes[mtii];
                        float mtw = weight * mtm.refValues[mtii];
                        smr.SetBlendShapeWeight(mti, mtw);
                    }
                }
            }
        }

        // Apply bone transformations on each channel
        foreach (MorphChannel mc in morphChannels)
        {
            for (int boneIndex = 0; boneIndex < mc.bones.Length; ++boneIndex)
            {
                Transform bone = mc.bones[boneIndex].bone;
                float weight = mc.weight;

                //bone.localPosition += mc.bones[bone_i].refPosition * weight + new Vector3(0, .08f, 0.16f);
                bone.localRotation *= Quaternion.Slerp(Quaternion.identity, mc.bones[boneIndex].refRotation, weight);
            }
        }
    }

    /// <summary>
    /// Reset all morph channel weights to zero.
    /// </summary>
    public void ResetAllWeights()
    {
        foreach (MorphChannel mc in morphChannels)
        {
            mc.weight = 0f;
        }
    }

    private void _CacheMorphChannelIndexes()
    {
        // Initialize morph channel name-index mappings
        morphChannelIndexes = new Dictionary<string, int>();
        for (int mci = 0; mci < morphChannels.Length; ++mci)
            morphChannelIndexes.Add(morphChannels[mci].name, mci);
    }
}

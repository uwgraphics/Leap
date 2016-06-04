using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

/// <summary>
/// Serializer class for LMC (LEAP Morph Channel Mapping) XML files.
/// </summary>
public class LMCSerializer
{
    [Serializable]
    public class LMC
    {
        public LMCAnimSettings animSettings;
        public LMCMorphChannel[] morphChannels = null;
    }

    [Serializable]
    public class LMCAnimSettings
    {
        public bool linkAnimations = true;
        public string animTrackPrefix = "MC_";
    }

    [Serializable]
    public class LMCMorphChannel
    {
        public string name = "";
        public LMCSubmeshMapping[] submeshes = null;
        public LMCBoneMapping[] bones = null;
        public LMCSubchannelMapping[] subchannels = null;
    }

    [Serializable]
    public class LMCSubmeshMapping
    {
        public string name = "";
        public string mtName = "";
        public float refValue = 0f;
    }

    [Serializable]
    public class LMCBoneMapping
    {
        public string name = "";
        public Vector3 refPosition;
        public Vector3 refRotation;
    }

    [Serializable]
    public class LMCSubchannelMapping
    {
        public string name = "";
        public float refValue = 0f;
    }

    /// <summary>
    /// Morph channel mapping data.
    /// </summary>
    public LMC lmc;

    /// <summary>
    /// Constructor. 
    /// </summary>
    public LMCSerializer()
    {
        lmc = new LMC();
    }

    /// <summary>
    /// Load morph channel mappings from a file. 
    /// </summary>
    /// <param name="gameObj">
    /// Character model. <see cref="UnityEngine.GameObject"/>
    /// </param>
    /// <param name="path">
    /// LMC file path. <see cref="System.String"/>
    /// </param>
    /// <returns>
    /// true if morph channel mappings were successfully loaded, false otherwise. <see cref="System.Boolean"/>
    /// </returns>
    public bool Load(GameObject gameObj, string path)
    {
        // Open LMC file for reading
        FileInfo lmc_inf = new FileInfo(path);
        if (!lmc_inf.Exists)
            return false;
        TextReader lmc_text = lmc_inf.OpenText();

        // Read in XML file
        XmlSerializer lmc_reader = new XmlSerializer(lmc.GetType());
        lmc = (LMC)lmc_reader.Deserialize(lmc_text);
        lmc_text.Close();

        // Get morph animation scripts
        MorphController morphCtrl = gameObj.GetComponent<MorphController>();
        if (morphCtrl == null)
        {
            UnityEngine.Debug.LogWarning("Morph channels cannot be initialized. Missing or invalid MorphController component.");

            return false;
        }

        // Reorder morph channels to account for subchannel dependencies
        if (!_FixMorphChannelOrder(lmc.morphChannels))
        {
            UnityEngine.Debug.LogWarning("Morph channels cannot be initialized due to errors in their definition");

            return false;
        }

        // Initialize all morph channels
        morphCtrl.morphChannels = new MorphChannel[lmc.morphChannels.Length];
        for (int mci = 0; mci < morphCtrl.morphChannels.Length; ++mci)
        {
            MorphChannel mc = new MorphChannel();
            morphCtrl.morphChannels[mci] = mc;
            LMCSerializer.LMCMorphChannel lmc_mc = lmc.morphChannels[mci];

            mc.name = lmc_mc.name;

            // Initialize morph targets
            if (lmc_mc.submeshes != null)
            {
                // Get all submeshes
                var submeshes = new Dictionary<string, GameObject>();
                foreach (var lmc_sm in lmc_mc.submeshes)
                {
                    var submesh = ModelUtil.GetSubModels(gameObj).FirstOrDefault(obj => obj.name ==lmc_sm.name);
                    if (submesh == null || submesh.GetComponent<SkinnedMeshRenderer>() == null)
                    {
                        Debug.LogWarning(string.Format("Morph channel {0} specifies a non-existent or invalid submesh {1}", mc.name, lmc_sm.name));
                        continue;
                    }

                    if (!submeshes.ContainsKey(submesh.name))
                        submeshes.Add(submesh.name, submesh);
                }

                // Load all morph target mappings
                var morphTargetList = new List<MorphChannel.MorphTargetMapping>();
                foreach (var kvp in submeshes)
                {
                    var mtm = new MorphChannel.MorphTargetMapping();
                    mtm.sourceMesh = kvp.Value;
                    var lmc_mts = lmc_mc.submeshes.Where(mt => mt.name == kvp.Key);

                    var mtiList = new List<int>();
                    var rvList = new List<float>();
                    foreach (var lmc_mt in lmc_mts)
                    {
                        int mti = mtm.sourceMesh.GetComponent<SkinnedMeshRenderer>().sharedMesh.GetBlendShapeIndex(lmc_mt.mtName);
                        mtiList.Add(mti);
                        rvList.Add(lmc_mt.refValue);
                    }
                    mtm.morphTargetIndexes = mtiList.ToArray();
                    mtm.refValues = rvList.ToArray();

                    morphTargetList.Add(mtm);
                }
                mc.morphTargets = morphTargetList.ToArray();
            }

            // Initialize bones
            if (lmc_mc.bones != null)
            {
                mc.bones = new MorphChannel.BoneMapping[lmc_mc.bones.Length];

                bool bone_missing = false;
                for (int bone_i = 0; bone_i < mc.bones.Length; ++bone_i)
                {
                    mc.bones[bone_i] = new MorphChannel.BoneMapping();
                    mc.bones[bone_i].bone = ModelUtil.FindBone(gameObj.transform, lmc_mc.bones[bone_i].name);

                    if (mc.bones[bone_i].bone == null)
                    {
                        bone_missing = true;
                        break;
                    }

                    mc.bones[bone_i].refPosition = lmc_mc.bones[bone_i].refPosition;
                    mc.bones[bone_i].refRotation = Quaternion.Euler(lmc_mc.bones[bone_i].refRotation);
                }

                if (bone_missing)
                    // At least one bone is missing, so we disable all bone mappings on this channel
                    mc.bones = null;
            }

            // Assign initial weight
            mc.weight = 0;
        }

        // Subchannel mappings need to be initialized separately,
        // after all morph channels have been created
        for (int mci = 0; mci < morphCtrl.morphChannels.Length; ++mci)
        {
            MorphChannel mc = morphCtrl.morphChannels[mci];
            LMCSerializer.LMCMorphChannel lmc_mc = lmc.morphChannels[mci];

            // Initialize subchannels
            if (lmc_mc.subchannels != null)
            {
                mc.subchannels = new MorphChannel.SubchannelMapping[lmc_mc.subchannels.Length];

                for (int smc_i = 0; smc_i < mc.subchannels.Length; ++smc_i)
                {
                    int tmci = morphCtrl.GetMorphChannelIndex(lmc_mc.subchannels[smc_i].name);
                    if (tmci < 0)
                    {
                        UnityEngine.Debug.LogWarning("Morph channel " + mc.name + " mapped to invalid subchannel " +
                                                     lmc_mc.subchannels[smc_i].name);
                        continue;
                    }

                    mc.subchannels[smc_i] = new MorphChannel.SubchannelMapping();
                    mc.subchannels[smc_i].subchannelIndex = tmci;
                    mc.subchannels[smc_i].refValue = lmc_mc.subchannels[smc_i].refValue;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Serialize morph channel mappings to a file.
    /// </summary>
    /// <param name="gameObj">
    /// Character model. <see cref="UnityEngine.GameObject"/>
    /// </param>
    /// <param name="path">
    /// LMC file path. <see cref="System.String"/>
    /// </param>
    /// <returns>
    /// true if morph channel mappings were successfully serialized, false otherwise. <see cref="System.Boolean"/>
    /// </returns>
    public bool Serialize(GameObject gameObj, string path)
    {
        throw new NotImplementedException("Morph target mapping serialization not implemented yet!");

        // Open LMC file for writing
        FileInfo lmc_inf = new FileInfo(path);
        if (lmc_inf.Exists)
            lmc_inf.Delete();
        TextWriter lmc_text = lmc_inf.CreateText();

        // Get morph animation scripts
        MorphController morphCtrl = gameObj.GetComponent<MorphController>();
        if (morphCtrl == null)
        {
            UnityEngine.Debug.LogWarning("Morph channels cannot be serialized. Missing or invalid MorphController component.");

            return false;
        }

        // Initialize morph animation settings
        lmc.animSettings = new LMCSerializer.LMCAnimSettings();

        // Initialize all morph channels
        lmc.morphChannels = new LMCSerializer.LMCMorphChannel[morphCtrl.morphChannels.Length];
        for (int mci = 0; mci < lmc.morphChannels.Length; ++mci)
        {
            lmc.morphChannels[mci] = new LMCSerializer.LMCMorphChannel();
            LMCMorphChannel lmc_mc = lmc.morphChannels[mci];
            MorphChannel mc = morphCtrl.morphChannels[mci];

            // Set name
            lmc_mc.name = mc.name;

            if (mc.morphTargets != null && mc.morphTargets.Length > 0)
            {
                // Set morph target mappings
                // TODO
            }

            if (mc.bones != null && mc.bones.Length > 0)
            {
                // Set bone mappings
                List<LMCBoneMapping> lmc_bmlist = new List<LMCBoneMapping>();
                for (int bi = 0; bi < mc.bones.Length; ++bi)
                {
                    MorphChannel.BoneMapping bm = mc.bones[bi];
                    LMCBoneMapping lmc_bm = new LMCBoneMapping();

                    lmc_bm.name = bm.bone.name;
                    lmc_bm.refPosition = bm.refPosition;
                    lmc_bm.refRotation = bm.refRotation.eulerAngles;

                    lmc_bmlist.Add(lmc_bm);
                }
                lmc_mc.bones = lmc_bmlist.ToArray();
            }

            if (mc.subchannels != null && mc.subchannels.Length > 0)
            {
                // Set subchannel mappings
                List<LMCSubchannelMapping> lmc_scmlist = new List<LMCSubchannelMapping>();
                for (int sci = 0; sci < mc.subchannels.Length; ++sci)
                {
                    MorphChannel.SubchannelMapping scm = mc.subchannels[sci];
                    LMCSubchannelMapping lmc_scm = new LMCSubchannelMapping();

                    lmc_scm.name = morphCtrl.morphChannels[scm.subchannelIndex].name;
                    lmc_scm.refValue = scm.refValue;

                    lmc_scmlist.Add(lmc_scm);
                }
                lmc_mc.subchannels = lmc_scmlist.ToArray();
            }
        }

        // Write out LMC file
        XmlSerializer lmc_writer = new XmlSerializer(lmc.GetType());
        lmc_writer.Serialize(lmc_text, lmc);
        lmc_text.Close();

        return true;
    }

    /// <summary>
    /// Reorder morph channels so that subchannels never
    /// precede their parent channels.
    /// </summary>
    /// <param name="morphChannels">Array of morph channel definitions.</param>
    /// <returns>true if reordering was successful, false if it failed due to circular dependencies.</returns>
    private static bool _FixMorphChannelOrder(LMCSerializer.LMCMorphChannel[] morphChannels)
    {
        // Add all morph channels to a linked list
        List<LMCSerializer.LMCMorphChannel> mclist = new List<LMCSerializer.LMCMorphChannel>();
        for (int mci = 0; mci < morphChannels.Length; ++mci)
        {
            mclist.Add(morphChannels[mci]);
        }

        // Detect circular dependencies
        HashSet<string> all_trav_channels = new HashSet<string>();
        foreach (LMCSerializer.LMCMorphChannel mc in mclist)
        {
            if (all_trav_channels.Contains(mc.name))
                // This channel belongs to a dependency tree that was already checked, ignore it
                continue;

            HashSet<string> trav_channels = new HashSet<string>();

            if (_DetectSubchannelLoops(mclist, mc, trav_channels))
            {
                UnityEngine.Debug.LogWarning("Morph channel " + mc.name + " is part of a circular dependency of subchannels");

                return false;
            }

            // Update set of all checked channels
            all_trav_channels.UnionWith(trav_channels);
        }

        // Reorder morph channels
        for (int mci = 0; mci < mclist.Count; ++mci)
        {
            LMCSerializer.LMCMorphChannel mc0 = mclist[mci];

            for (int mcj = mci + 1; mcj < mclist.Count; ++mcj)
            {
                LMCSerializer.LMCMorphChannel mc = mclist[mcj];

                if (mc.subchannels == null)
                    // Not mapped to any subchannels
                    continue;

                foreach (LMCSerializer.LMCSubchannelMapping smc in mc.subchannels)
                {
                    if (smc.name == mc0.name)
                    {
                        // mc0 is child of mc, so move mc before mc0

                        mclist.RemoveAt(mcj);
                        mclist.Insert(mci, mc);
                        mc0 = mc;
                        mcj = mci;

                        break;
                    }
                }
            }
        }

        // Update array of morph channels
        for (int mci = 0; mci < mclist.Count; ++mci)
        {
            morphChannels[mci] = mclist[mci];
        }

        return true;
    }

    private static bool _DetectSubchannelLoops(List<LMCSerializer.LMCMorphChannel> morphChannels,
                                              LMCSerializer.LMCMorphChannel root, HashSet<string> travChannels)
    {
        if (travChannels.Contains(root.name))
            // Loop detected - a channel encountered twice on traversal
            return true;

        // Add current morph channel to set of traversed channels
        travChannels.Add(root.name);

        if (root.subchannels == null)
            return false;

        foreach (LMCSerializer.LMCSubchannelMapping smcmap in root.subchannels)
        {
            // Find target morph subchannel and continue traversal
            foreach (LMCSerializer.LMCMorphChannel smc in morphChannels)
            {
                if (smc.name == smcmap.name &&
                   _DetectSubchannelLoops(morphChannels, smc, travChannels))
                    return true;
            }
        }

        return false;
    }

}


using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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
	public bool Load( GameObject gameObj, string path )
	{
		// Open LMC file for reading
		FileInfo lmc_inf = new FileInfo(path);
		if( !lmc_inf.Exists )
			return false;
		TextReader lmc_text = lmc_inf.OpenText();
		
		// Read in XML file
		XmlSerializer lmc_reader = new XmlSerializer( lmc.GetType() );
		lmc = (LMC)lmc_reader.Deserialize(lmc_text);
		lmc_text.Close();
		
		// Get morph animation scripts
		MorphController morphCtrl = gameObj.GetComponent<MorphController>();
		if( morphCtrl == null )
		{
			UnityEngine.Debug.LogWarning( "Morph channels cannot be initialized. Missing or invalid MorphController component." );
			
			return false;
        }
        MorphAnimationLinker morphAnimMapper = gameObj.GetComponent<MorphAnimationLinker>();
        if (morphAnimMapper == null)
        {
			UnityEngine.Debug.LogWarning( "Morph channels cannot be initialized. Missing MorphAnimationLinker component." );
			
			return false;
        }
		
		// Get morph target indexes
		Dictionary<string, int> mt_inds = new Dictionary<string, int>();
		for( int mti = 0; mti < morphCtrl.morphTargets.Length; ++mti )
			mt_inds[ morphCtrl.morphTargets[mti].name ] = mti;
		
		// Initialize morph animation settings
		morphAnimMapper.enabled = lmc.animSettings.linkAnimations;
		
		// Reorder morph channels to account for subchannel dependencies
		if( !_FixMorphChannelOrder(lmc.morphChannels) )
		{
			UnityEngine.Debug.LogWarning( "Morph channels cannot be initialized due to errors in their definition" );
			
			return false;
		}
		
		// Initialize all morph channels
		morphCtrl.morphChannels = new MorphChannel[ lmc.morphChannels.Length ];
		for( int mci = 0; mci < morphCtrl.morphChannels.Length; ++mci )
		{
			MorphChannel mc = new MorphChannel();
			morphCtrl.morphChannels[mci] = mc;
			LMCSerializer.LMCMorphChannel lmc_mc = lmc.morphChannels[mci];
			
			mc.name = lmc_mc.name;
			
			// Initialize submeshes
			if( morphCtrl.sourceMeshes != null )
			{
				mc.morphTargets = new MorphChannel.MorphTargetMapping[morphCtrl.sourceMeshes.Length];
				
				for( int smi = 0; smi < morphCtrl.sourceMeshes.Length; ++smi )
				{
					mc.morphTargets[smi] = new MorphChannel.MorphTargetMapping();
					mc.morphTargets[smi].morphTargetIndexes = null;
					mc.morphTargets[smi].refValues = null;
					
					// How many morph target meshes are mapped to this morph channel
					// on the current submesh?
					int num_sm = 0;
					LMCSerializer.LMCSubmeshMapping lmc_sm = null;
					for( int lmc_smi = 0; lmc_mc.submeshes != null &&
					    lmc_smi < lmc_mc.submeshes.Length; ++lmc_smi )
					{
						lmc_sm = lmc_mc.submeshes[lmc_smi];
						if( lmc_sm.name == morphCtrl.sourceMeshes[smi].name )
							++num_sm;
					}
					
					if( num_sm <= 0 )
						// No morph target meshes mapped
						continue;
					
					mc.morphTargets[smi].morphTargetIndexes = new int[num_sm];
					mc.morphTargets[smi].refValues = new float[num_sm];
					
					// Map corresponding morph target meshes on this submesh
					int sm_mti = 0;
					for( int lmc_smi = 0; lmc_smi < lmc_mc.submeshes.Length; ++lmc_smi )
					{
						lmc_sm = lmc_mc.submeshes[lmc_smi];
						if( lmc_sm.name != morphCtrl.sourceMeshes[smi].name )
							continue;
						
						// This morph channel controls a morph target on the current source mesh,
						// find the morph target
						int mti = -1;
						if( !mt_inds.ContainsKey(lmc_sm.mtName) )
						{
							UnityEngine.Debug.LogWarning( string.Format( "Morph target {0} not defined on model {1}",
							                                            lmc_sm.mtName, gameObj.name ) );
							
							mc.morphTargets[smi].morphTargetIndexes = null;
							mc.morphTargets[smi].refValues = null;
							
							break;
						}
						mti = mt_inds[lmc_sm.mtName];
						
						mc.morphTargets[smi].morphTargetIndexes[sm_mti] = mti;
						mc.morphTargets[smi].refValues[sm_mti] = lmc_sm.refValue;
						
						++sm_mti;
					}
				}
			}

			// Initialize bones
			if( lmc_mc.bones != null )
			{
				mc.bones = new MorphChannel.BoneMapping[ lmc_mc.bones.Length ];
				
				bool bone_missing = false;
				
				for( int bone_i = 0; bone_i < mc.bones.Length; ++bone_i )
				{
					mc.bones[bone_i] = new MorphChannel.BoneMapping();
                    mc.bones[bone_i].bone = ModelUtils.FindBone(gameObj.transform, lmc_mc.bones[bone_i].name);
					
					if( mc.bones[bone_i].bone == null )
					{
						bone_missing = true;
						break;
					}
					
					mc.bones[bone_i].refPosition = lmc_mc.bones[bone_i].refPosition;
					mc.bones[bone_i].refRotation = Quaternion.Euler( lmc_mc.bones[bone_i].refRotation );
				}
				
				if(bone_missing)
					// At least one bone is missing, so we disable all bone mappings on this channel
					mc.bones = null;
			}
			
			// Assign initial weight
			mc.weight = 0;
		}
		
		// Subchannel mappings need to be initialized separately,
		// after all morph channels have been created
		for( int mci = 0; mci < morphCtrl.morphChannels.Length; ++mci )
		{
			MorphChannel mc = morphCtrl.morphChannels[mci];
			LMCSerializer.LMCMorphChannel lmc_mc = lmc.morphChannels[mci];
			
			// Initialize subchannels
			if( lmc_mc.subchannels != null )
			{
				mc.subchannels = new MorphChannel.SubchannelMapping[ lmc_mc.subchannels.Length ];
				
				for( int smc_i = 0; smc_i < mc.subchannels.Length; ++smc_i )
				{
					int tmci = morphCtrl.GetMorphChannelIndex( lmc_mc.subchannels[smc_i].name );
					if( tmci < 0 )
					{
						UnityEngine.Debug.LogWarning( "Morph channel " + mc.name + " mapped to invalid subchannel " +
						                             lmc_mc.subchannels[smc_i].name );
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
	public bool Serialize( GameObject gameObj, string path )
	{
		// Open LMC file for writing
		FileInfo lmc_inf = new FileInfo(path);
		if( lmc_inf.Exists )
			lmc_inf.Delete();
		TextWriter lmc_text = lmc_inf.CreateText();
		
		// Get morph animation scripts
		MorphController morphCtrl = gameObj.GetComponent<MorphController>();
		if( morphCtrl == null || morphCtrl.morphTargets == null ||
		   morphCtrl.morphTargets.Length <= 0 )
		{
			UnityEngine.Debug.LogWarning( "Morph channels cannot be serialized. Missing or invalid MorphController component." );
			
			return false;
        }
        MorphAnimationLinker morphAnimMapper = gameObj.GetComponent<MorphAnimationLinker>();
        if (morphAnimMapper == null)
        {
			UnityEngine.Debug.LogWarning( "Morph channels cannot be serialized. Missing MorphAnimationLinker component." );
			
			return false;
        }
		
		// Get morph target indexes
		Dictionary<string, int> mt_inds = new Dictionary<string, int>();
		for( int mti = 0; mti < morphCtrl.morphTargets.Length; ++mti )
		{
			mt_inds[ morphCtrl.morphTargets[mti].name ] = mti;
		}
		
		// Initialize morph animation settings
		lmc.animSettings = new LMCSerializer.LMCAnimSettings();
		lmc.animSettings.linkAnimations = morphAnimMapper.enabled;
		
		// Initialize all morph channels
		lmc.morphChannels = new LMCSerializer.LMCMorphChannel[morphCtrl.morphChannels.Length];
		for( int mci = 0; mci < lmc.morphChannels.Length; ++mci )
		{
			lmc.morphChannels[mci] = new LMCSerializer.LMCMorphChannel();
			LMCMorphChannel lmc_mc = lmc.morphChannels[mci];
			MorphChannel mc = morphCtrl.morphChannels[mci];
			
			// Set name
			lmc_mc.name = mc.name;
			
			if( mc.morphTargets != null && mc.morphTargets.Length > 0 )
			{
				// Set morph target mappings
				List<LMCSubmeshMapping> lmc_mtmlist = new List<LMCSubmeshMapping>();
				for( int smi = 0; smi < morphCtrl.sourceMeshes.Length; ++smi )
				{
					MorphChannel.MorphTargetMapping mtm = mc.morphTargets[smi];
					
					for( int mti = 0; mti < mtm.morphTargetIndexes.Length; ++mti )
					{
						LMCSubmeshMapping lmc_sm = new LMCSubmeshMapping();
						lmc_sm.name = morphCtrl.sourceMeshes[smi].name;
						lmc_sm.mtName = morphCtrl.morphTargets[ mtm.morphTargetIndexes[mti] ].name;
						lmc_sm.refValue = mtm.refValues[mti];
						
						lmc_mtmlist.Add(lmc_sm);
					}
				}
				lmc_mc.submeshes = lmc_mtmlist.ToArray();
			}
			
			if( mc.bones != null && mc.bones.Length > 0 )
			{
				// Set bone mappings
				List<LMCBoneMapping> lmc_bmlist = new List<LMCBoneMapping>();
				for( int bi = 0; bi < mc.bones.Length; ++bi )
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
			
			if( mc.subchannels != null && mc.subchannels.Length > 0 )
			{
				// Set subchannel mappings
				List<LMCSubchannelMapping> lmc_scmlist = new List<LMCSubchannelMapping>();
				for( int sci = 0; sci < mc.subchannels.Length; ++sci )
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
		XmlSerializer lmc_writer = new XmlSerializer( lmc.GetType() );
		lmc_writer.Serialize( lmc_text, lmc );
		lmc_text.Close();
		
		return true;
	}
	
	/// <summary>
	/// Reorder morph channels so that subchannels never
	/// precede their parent channels.
	/// </summary>
	/// <param name="morphChannels">Array of morph channel definitions.</param>
	/// <returns>true if reordering was successful, false if it failed due to circular dependencies.</returns>
	private static bool _FixMorphChannelOrder( LMCSerializer.LMCMorphChannel[] morphChannels )
	{
		// Add all morph channels to a linked list
		List<LMCSerializer.LMCMorphChannel> mclist = new List<LMCSerializer.LMCMorphChannel>();
		for( int mci = 0; mci < morphChannels.Length; ++mci )
		{
			mclist.Add( morphChannels[mci] );
		}
		
		// Detect circular dependencies
		HashSet<string> all_trav_channels = new HashSet<string>();
		foreach( LMCSerializer.LMCMorphChannel mc in mclist )
		{
			if( all_trav_channels.Contains(mc.name) )
				// This channel belongs to a dependency tree that was already checked, ignore it
				continue;
			
			HashSet<string> trav_channels = new HashSet<string>();
			
			if( _DetectSubchannelLoops( mclist, mc, trav_channels ) )
			{
				UnityEngine.Debug.LogWarning( "Morph channel " + mc.name + " is part of a circular dependency of subchannels" );
				
				return false;
			}
			
			// Update set of all checked channels
			all_trav_channels.UnionWith(trav_channels);
		}
		
		// Reorder morph channels
		for( int mci = 0; mci < mclist.Count; ++mci )
		{
			LMCSerializer.LMCMorphChannel mc0 = mclist[mci];
			
			for( int mcj = mci + 1; mcj < mclist.Count; ++mcj )
			{
				LMCSerializer.LMCMorphChannel mc = mclist[mcj];
				
				if( mc.subchannels == null )
					// Not mapped to any subchannels
					continue;
				
				foreach( LMCSerializer.LMCSubchannelMapping smc in mc.subchannels )
				{
					if( smc.name == mc0.name )
					{
						// mc0 is child of mc, so move mc before mc0
						
						mclist.RemoveAt(mcj);
						mclist.Insert( mci, mc );
						mc0 = mc;
						mcj = mci;
						
						break;
					}
				}
			}
		}
		
		// Update array of morph channels
		for( int mci = 0; mci < mclist.Count; ++mci )
		{
			morphChannels[mci] = mclist[mci];
		}
		
		return true;
	}
	
	private static bool _DetectSubchannelLoops( List<LMCSerializer.LMCMorphChannel> morphChannels,
	                                          LMCSerializer.LMCMorphChannel root, HashSet<string> travChannels )
	{
		if( travChannels.Contains(root.name) )
			// Loop detected - a channel encountered twice on traversal
			return true;
		
		// Add current morph channel to set of traversed channels
		travChannels.Add(root.name);
		
		if( root.subchannels == null )
			return false;
		
		foreach( LMCSerializer.LMCSubchannelMapping smcmap in root.subchannels )
		{
			// Find target morph subchannel and continue traversal
			foreach( LMCSerializer.LMCMorphChannel smc in morphChannels )
			{
				if( smc.name == smcmap.name &&
				   _DetectSubchannelLoops( morphChannels, smc, travChannels ) )
					return true;
			}
		}
		
		return false;
	}
	
}


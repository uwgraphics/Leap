
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

/// <summary>
/// Controller for morph deformation of meshes.
/// </summary>
[RequireComponent (typeof(ModelController))]
public class MorphController : MonoBehaviour 
{
	/// <summary>
	/// The meshes affected by morph deformation.
	/// </summary>
	public Mesh[] sourceMeshes = null;
	
	/// <summary>
	/// Morph targets on this model.
	/// </summary>
	public MorphTarget[] morphTargets = null;
	
	/// <summary>
	/// Morph channels for controlling model deformation.
	/// </summary>
	public MorphChannel[] morphChannels = null;
	
	private Dictionary<string,int> morphChannelIndexes = null;
	private Mesh[] workingMeshes; // These meshes get deformed every frame, and rendered
	private bool[] submeshDirty; // Per-submesh flags indicating whether a submesh deformation needs to be recomputed
	private float[] prevMCWeights; // Morph channel weights from the previous frame
	
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
	public MorphChannel GetMorphChannel( string mcName )
	{
		if( morphChannelIndexes == null )
			_CacheMorphChannelIndexes();
		
		if( morphChannelIndexes.ContainsKey(mcName) )
			return morphChannels[ morphChannelIndexes[mcName] ];
		
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
	public int GetMorphChannelIndex( string mcName )
	{
		if( morphChannelIndexes == null )
			_CacheMorphChannelIndexes();
		
		if( morphChannelIndexes.ContainsKey(mcName) )
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
		Debug.Log( "Initializing a Morph Controller on agent " + gameObject.name );
		
		if( sourceMeshes == null || sourceMeshes.Length <= 0 ||
		   morphChannels == null || morphChannels.Length <= 0 )
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
			
            if( morphChannels[i].morphTargets == null ||
			   morphChannels[i].morphTargets.Length != sourceMeshes.Length )
            {
                Debug.LogError("Morph channel " + i + " has wrong number of submeshes");
				return false;
            }
			
			// Set working bones
			for( int bone_i = 0; bone_i < morphChannels[i].bones.Length; ++bone_i )
			{
				Transform src_bone = morphChannels[i].bones[bone_i].bone;
				morphChannels[i].bones[bone_i].bone = ModelController.FindBone( transform, src_bone.name );
			}
        }
		
		_CacheMorphChannelIndexes();
		
		return true;
	}
	
	void Awake()
    {
		Init();
		
		SkinnedMeshRenderer[] smr_list = gameObject.GetComponentsInChildren<SkinnedMeshRenderer>();
		//MeshFilter[] mr_list = gameObject.GetComponentsInChildren<MeshFilter>();

        // Create duplicate meshes that will be deformed
		workingMeshes = new Mesh[sourceMeshes.Length];
		submeshDirty = new bool[sourceMeshes.Length];
        for( int i = 0; i < sourceMeshes.Length; i++ )
        {
            workingMeshes[i] = Mesh.Instantiate( sourceMeshes[i] ) as Mesh;
			workingMeshes[i].name = sourceMeshes[i].name;
			submeshDirty[i] = false;
				
			// Render the working mesh instead
			foreach( SkinnedMeshRenderer smr in smr_list )
			{
				if( smr.sharedMesh.name == workingMeshes[i].name )
				{
					smr.sharedMesh = workingMeshes[i];
					break;
				}
			}
			
			/*foreach( MeshFilter mr in mr_list )
			{
				if( mr.sharedMesh.name == workingMeshes[i].name )
				{
					mr.sharedMesh = workingMeshes[i];
					break;
				}
			}*/
        }
		
		// Create array for storing previous-frame weights
		prevMCWeights = new float[morphChannels.Length];
		for( int mci = 0; mci < prevMCWeights.Length; ++mci )
		{
			prevMCWeights[mci] = 0;
		}
		
		// Get model controller (needed to access initial bone transforms)
		mdlCtrl = gameObject.GetComponent<ModelController>();
    }
	
	void Update()
    {
	}
	
	void LateUpdate()
	{
        if( workingMeshes == null )
		{
			// Morph Controller not initialized properly
            return;
		}
		
		if( _CheckWeights() )
			_ApplyMorph();
	}
	
	/// <summary>
    /// Check if the weights have been changed, and clamp them to 0-1 range.
    /// </summary>
    /// <returns>True if changed, false otherwise</returns>
	private bool _CheckWeights()
    {
		bool dirty = false;
		
		// Apply inherited weights
		foreach( MorphChannel mc in morphChannels )
		{
			if( mc.weight <= 0.001f )
				// Morph channel currently inactive, don't apply
				continue;
			
			foreach( MorphChannel.SubchannelMapping smc in mc.subchannels )
			{
				morphChannels[ smc.subchannelIndex ].weight += mc.weight * smc.refValue;
			}
		}
		
        // Clamp weights to 0-1 range;
		// flag submeshes that need to be updated
		float weight = 0;
		for( int mci = 0; mci < morphChannels.Length; ++mci )
		{
			weight = morphChannels[mci].weight;
			
			if( weight < 0 )
				morphChannels[mci].weight = 0;
			else if( weight > 1 )
				morphChannels[mci].weight = 1;
			
			// TODO: this is all messed up and needs to be fixed!
			//if( Mathf.Abs( weight - prevMCWeights[mci] ) > 0.001f )
			//if( weight > 0.001f )
			{
				for( int smi = 0; smi < sourceMeshes.Length; ++smi )
				{
					if( morphChannels[mci].morphTargets[smi].morphTargetIndexes != null )
					{
						submeshDirty[smi] = true;
					}
				}
				
				dirty = true;
				//prevMCWeights[mci] = weight;
				// TODO: why does this cause blink morphs to reset to neutral superfast?
			}
		}
		
		return dirty;
    }
	
	 /// <summary>
    /// Generate the blended mesh.
    /// </summary>
    private void _ApplyMorph()
	{
		// Make sure all affected facial bones are set to initial pose
		for( int mci = 0; mci < morphChannels.Length; ++mci )
		{
			MorphChannel mc = morphChannels[mci];
			
			for( int bone_i = 0; bone_i < mc.bones.Length; ++bone_i )
			{
				Transform bone = mc.bones[bone_i].bone;
				bone.localPosition = mdlCtrl.GetInitPosition(bone);
				bone.localRotation = mdlCtrl.GetInitRotation(bone);
			}
		}
		
		// Apply morph deformations
        for( int smi = 0; smi < sourceMeshes.Length; smi++ )
        {
			if( !submeshDirty[smi] )
				// Submesh deformation did not change in this frame
				continue;
			
			Vector3[] vertices = sourceMeshes[smi].vertices;
            Vector3[] normals = sourceMeshes[smi].normals;

            // Apply each morph target on each channel
            for( int mci = 0; mci < morphChannels.Length; mci++ )
            {
				MorphChannel mc = morphChannels[mci];
				float weight = mc.weight;
				
                if( weight < 0.001f )
                {
					// Weight zero, nothing to apply here
					
                    continue;
                }
				
				MorphChannel.MorphTargetMapping mc_sm = mc.morphTargets[smi];
				
				for( int mti = 0; mc_sm.morphTargetIndexes != null && mti < mc_sm.morphTargetIndexes.Length; ++mti )
				{
					MorphTarget mt = morphTargets[ mc_sm.morphTargetIndexes[mti] ];
					float mtweight = weight * mc_sm.refValues[mti];
					
	                for( int mtvi = 0; mtvi < mt.vertexIndices.Length; ++mtvi )
	                {
						int vi = mt.vertexIndices[mtvi];
						
						if( mc.name.StartsWith("Viseme"))
	                    	vertices[vi] += mt.relVertices[mtvi] * mtweight;
						// TODO: Why are the normals all wrong?
	                    //normals[vi] += mt.relNormals[mtvi] * mtweight;
	                }
					
				}
            }
			
			
			workingMeshes[smi].vertices = vertices;
            workingMeshes[smi].normals = normals;
            workingMeshes[smi].RecalculateBounds();
			
			// Submesh is now up to date
			submeshDirty[smi] = false;
        }
		
		// Apply bone transformations
		foreach( MorphChannel mc in morphChannels )
		{
			for( int bone_i = 0; bone_i < mc.bones.Length; ++bone_i )
			{
				Transform bone = mc.bones[bone_i].bone;
				float weight = mc.weight;
				
				//bone.localPosition += mc.bones[bone_i].refPosition * weight + new Vector3(0, .08f, 0.16f);
				bone.localRotation *= Quaternion.Slerp( Quaternion.identity, mc.bones[bone_i].refRotation, weight );
			}
		}
		
		// Reset all weights to zero
		for( int mti = 0; mti < morphChannels.Length; ++mti )
		{
			morphChannels[mti].weight = 0;
		}
    }
	
	private void _CacheMorphChannelIndexes()
	{
		// Initialize morph channel name-index mappings
		morphChannelIndexes = new Dictionary<string, int>();
		for( int mci = 0; mci < morphChannels.Length; ++mci )
			morphChannelIndexes.Add( morphChannels[mci].name, mci );
	}
}

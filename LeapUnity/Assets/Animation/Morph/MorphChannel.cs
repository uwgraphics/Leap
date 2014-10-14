using UnityEngine;
using System;

/// <summary>
/// Class representing a morph channel.
/// </summary>
[Serializable]
public class MorphChannel
{
	
	/// <summary>
	/// Class specifying all of current morph channel's
	/// morph target meshes on a particular submesh.
	/// </summary>
	[Serializable]
	public class MorphTargetMapping
	{
		
		/// <summary>
		/// Indexes of morph targets applied by this morph channel
		/// on the current submesh.
		/// </summary>
		public int[] morphTargetIndexes;
		
		/// <summary>
		/// Reference weight values of the morph targets.
		/// </summary>
		public float[] refValues;
		
	}
	
	/// <summary>
	/// Class specifying all of current morph channel's
	/// mapped bones.
	/// </summary>
	[Serializable]
	public class BoneMapping
	{
		
		/// <summary>
		/// Bone affected by the morph channel. 
		/// </summary>
		public Transform bone;
		
		/// <summary>
		/// Reference positions of bones.
		/// </summary>
		public Vector3 refPosition;
		
		/// <summary>
		/// Reference rotations of bones.
		/// </summary>
		public Quaternion refRotation;
		
	}
	
	/// <summary>
	/// Class specifying all of current morph channel's
	/// morph target meshes on a particular submesh.
	/// </summary>
	[Serializable]
	public class SubchannelMapping
	{
		
		/// <summary>
		/// Index of the child morph channel affected by the current morph channel.
		/// </summary>
		public int subchannelIndex;
		
		/// <summary>
		/// Reference weight value of the subchannel.
		/// </summary>
		public float refValue;
		
	}
	
	/// <summary>
	/// Morph channel name. 
	/// </summary>
	public string name;
	
	/// <summary>
	/// Array of morph target mappings for this morph channel.
	/// Mappings are specified per-submesh.
	/// </summary>
	public MorphTargetMapping[] morphTargets;
	
	/// <summary>
	/// Array of bone mappings for this morph channel. 
	/// </summary>
	public BoneMapping[] bones;
	
	/// <summary>
	/// Array of child morph channel mappings. 
	/// </summary>
	public SubchannelMapping[] subchannels;
	
	/// <summary>
	/// Morph channel weight 
	/// </summary>
	public float weight;
	
}

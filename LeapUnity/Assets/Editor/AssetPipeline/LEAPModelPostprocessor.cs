using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Diagnostics;
using System.IO;

public class LEAPModelPostprocessor : AssetPostprocessor
{
	
	/// <summary>
	/// Postprocess model.
	/// </summary>
	/// <param name="gameObj">
	/// Model <see cref="GameObject"/>
	/// </param>
	void OnPostprocessModel( GameObject gameObj )
	{
		// What kind of model is it?
		if( assetPath.Replace('\\','/').Contains( LEAPCore.agentModelDirectory ) )
		{
			// It's an agent model
			PostprocessAgentModel(gameObj);
		}
	}
	
	/// <summary>
	/// Postprocess agent model, by postprocessing its morph target meshes,
	/// loading morph channel mappings, and setting up animations.
	/// </summary>
	/// <param name="gameObj">
	/// Agent model <see cref="GameObject"/>
	/// </param>
	void PostprocessAgentModel( GameObject gameObj )
	{
		// TODO: for Jack model (or any other based on Toon figure from DAZ),
		// morph target EyeClosedL, vertices 9566 and 9567 aren't mapped automatically
		// TODO: how do I run FBXMorphPreprocess automatically before import?
		// (I get loop between and FBXMorphPreprocess when using OnPreprocessModel)
		// TODO: why doesn't Unity import names of meshes created by FBXMorphPreprocess?

		// Create requires components
		if( gameObj.GetComponent<ModelController>() == null )
			gameObj.AddComponent<ModelController>();
		if( gameObj.GetComponent<Animation>() == null )
			gameObj.AddComponent<Animation>();
		
		// Initialize all animation
		_InitMorphAnimation(gameObj);
		_InitAnimClips(gameObj);
		
		// Auto-tag the model
        ModelUtils.AutoTagModel(gameObj);
	}
	
	private void _InitMorphAnimation( GameObject gameObj )
	{
		// Find all meshes in the model
		Dictionary<string, Mesh> meshes = new Dictionary<string, Mesh>();
		SkinnedMeshRenderer[] smf_list = gameObj.GetComponentsInChildren<SkinnedMeshRenderer>();
		foreach( SkinnedMeshRenderer smf in smf_list )
		{
			if( !meshes.ContainsKey(smf.sharedMesh.name) )
				meshes.Add( smf.sharedMesh.name, smf.sharedMesh );
		}
		MeshFilter[] mf_list = gameObj.GetComponentsInChildren<MeshFilter>();
		foreach( MeshFilter mf in mf_list )
		{
			if( !meshes.ContainsKey(mf.sharedMesh.name) )
				meshes.Add( mf.sharedMesh.name, mf.sharedMesh );
		}
		
		// Find all source and morph target meshes
		List<Mesh> src_meshes = new List<Mesh>();
		Dictionary<string, List<Mesh>> mt_meshes = new Dictionary<string, List<Mesh>>();
		foreach( KeyValuePair<string, Mesh> mesh_pair in meshes )
		{
			Mesh mesh = mesh_pair.Value;
			string src_name;
			
			if( LEAPAssetUtils.ParseMorphTargetName( mesh.name, out src_name ) &&
			   meshes.ContainsKey(src_name) )
			{
				// This is a morph target mesh
				
				// Add source mesh if you haven't already
				Mesh src_mesh = meshes[src_name];
				if( !src_meshes.Contains(src_mesh) )
				{
					src_meshes.Add(src_mesh);
					mt_meshes[src_mesh.name] = new List<Mesh>();
				}
				
				// Add target mesh
				mt_meshes[src_mesh.name].Add(mesh);
			}
		}
		
		// Create morph animation scripts
		MorphController morphCtrl = gameObj.GetComponent<MorphController>();
		if (morphCtrl == null)
		{
			morphCtrl = gameObj.AddComponent<MorphController>();
        }
        MorphAnimationLinker morphAnimMapper = gameObj.GetComponent<MorphAnimationLinker>();
        if (morphAnimMapper == null)
        {
            morphAnimMapper =  gameObj.AddComponent<MorphAnimationLinker>();
        }
		
		// Initialize morph controller with source meshes
		if( src_meshes.Count > 0 )
			morphCtrl.sourceMeshes = src_meshes.ToArray();
		else
			return;
		
		// Recalculate normals in source meshes
		// (to eliminate visual artifacts due to Unity normal recalculation)
		if( ((ModelImporter)assetImporter).normalImportMode == ModelImporterTangentSpaceMode.Calculate )
		{
			foreach( Mesh mesh in morphCtrl.sourceMeshes )
			{
				LEAPAssetUtils.RecalculateNormals(mesh);
			}
		}
		
		// Compute morph targets for each source mesh
		List<MorphTarget> mts = new List<MorphTarget>();
		foreach( KeyValuePair<string, List<Mesh>> pair in mt_meshes )
		{
			MorphTarget[] mt_list = MorphTarget.BuildMorphTargets( meshes[pair.Key], pair.Value.ToArray() );
			
			if( mt_list == null )
			{
				UnityEngine.Debug.LogWarning( string.Format( "Morph targets could not be built for source mesh {0} on model {1} (are you missing vertex colors on source or target meshes?)",
				                                            pair.Key, gameObj.name ) );
				pair.Value.Clear();
				
				continue;
			}
			
			mts.AddRange(mt_list);
			
			// Destroy all the original morph target meshes
			foreach( Mesh mtmesh in pair.Value )
			{
				meshes.Remove(mtmesh.name);
				Mesh.DestroyImmediate( mtmesh, true );
			}
			pair.Value.Clear();
		}
		
		// Initialize morph controller with morph targets
		if( mts.Count <= 0 )
			return;
		morphCtrl.morphTargets = mts.ToArray();
		
		// Load morph channel mappings (if defined)
		string lmc_path = assetPath.Substring( 0, assetPath.LastIndexOf( ".fbx", StringComparison.InvariantCultureIgnoreCase ) )
			+ ".lmc";
		LMCSerializer lmc = new LMCSerializer();
		if( !lmc.Load( gameObj, lmc_path ) )
		{
			// Unable to load morph channel mappings
			
			return;
		}
	}
	
	private void _InitAnimClips( GameObject gameObj )
	{
		// Get anim. clip root path
		string root_path = assetPath.Substring( 0, assetPath.LastIndexOfAny( @"\/".ToCharArray() ) + 1 );
		
		// Get anim. clip names for the asset
		HashSet<string> clip_names = new HashSet<string>();
		ModelImporterClipAnimation[] imp_clips = ((ModelImporter)assetImporter).clipAnimations;
		foreach( ModelImporterClipAnimation imp_clip in imp_clips )
			clip_names.Add(imp_clip.name);
		
		// Get all anim. clips
		AnimationClip[] all_clips = AnimationClip.FindObjectsOfType(typeof(AnimationClip)) as AnimationClip[];
		
		// Iterate through anim. clips and process them
		List<AnimationClip> new_clips = new List<AnimationClip>();
		foreach( AnimationClip clip in all_clips )
		{
			if( !clip_names.Contains(clip.name) )
				continue;
			
			// Form anim. clip path
			string clip_path = root_path + clip.name + ".anim";
			AnimationClip old_clip = null;
			AnimationClip new_clip = UnityEngine.Object.Instantiate(clip) as AnimationClip;
			
			if( File.Exists(clip_path) )
			{
				// Anim. clip already exists - load it, and save anim. events
				
				old_clip = AssetDatabase.LoadAssetAtPath( clip_path, typeof(AnimationClip) ) as AnimationClip;
				LEAPAssetUtils.CopyAnimationEvents( old_clip, new_clip );
			}
			
			// Save anim. clip to a separate file (so it isn't write-locked)
			AssetDatabase.CreateAsset( new_clip, clip_path );
			new_clips.Add(new_clip);
			
			// Destroy original clip
			gameObj.animation.RemoveClip(clip.name);
			UnityEngine.Object.DestroyImmediate( clip, true );
		}
		
		// Link new (duplicate) clips to the agent's anim. component
		foreach( AnimationClip new_clip in new_clips )
			gameObj.animation.AddClip( new_clip, new_clip.name );
	}
}

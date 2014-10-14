using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Class representing a deformation of a particular mesh,
/// defined as a set of vertex and normal offsets.
/// </summary>
[Serializable]
public class MorphTarget
{
	/// <summary>
	/// Morph target name. 
	/// </summary>
	public string name;
	
	/// <summary>
	///  Indices of vertices in the source mesh.
	/// </summary>
	[HideInInspector]
	public int[] vertexIndices;
	
	/// <summary>
	/// Vertex position offsets. 
	/// </summary>
	[HideInInspector]
	public Vector3[] relVertices;
	
	/// <summary>
	/// Vertex normal offsets. 
	/// </summary>
	[HideInInspector]
	public Vector3[] relNormals;
	
	/// <summary>
	/// Constructor. 
	/// </summary>
	/// <param name="name">
	/// Morph target name. <see cref="System.String"/>
	/// </param>
	public MorphTarget( string name )
	{
		this.name = name;
	}
	
	/// <summary>
	/// Builds morph targets given a source mesh and a set of target meshes. 
	/// </summary>
	/// <param name="srcMesh">
	/// Source mesh. <see cref="Mesh"/>
	/// </param>
	/// <param name="mtMesh">
	/// Target meshes. <see cref="Mesh"/>
	/// </param>
	/// <returns>
	/// Morph targets. <see cref="MorphTarget"/>
	/// </returns>
	public static MorphTarget[] BuildMorphTargets( Mesh srcMesh, Mesh[] mtMeshes )
	{
		// TODO: It might be a good idea to normalize
		// each morph target mesh before computing the morph target

		if( srcMesh == null || mtMeshes == null )
		{
			// Jeez...
			
			return null;
		}
		
		foreach( Mesh mtmesh in mtMeshes )
		{
			if( mtmesh == null )
			{
				// Wha...?
				
				return null;
			}
		}
		
		Vector3[] src_positions = srcMesh.vertices;
		Vector3[] src_normals = srcMesh.normals;
		Color[] src_colors = srcMesh.colors;
		Vector2[] src_uv = srcMesh.uv;
		SortedDictionary<int, HashSet<int>> vimap = new SortedDictionary<int, HashSet<int>>(); // maps original vertex indexes to new (reordered) vertex indexes
		SortedDictionary<int, HashSet<int>> dvimap = new SortedDictionary<int, HashSet<int>>(); // maps vertex indexes to indexes of duplicate (split) vertices
		
		// Compute map of vertex indexes for the source mesh
		for( int vi = 0; vi < srcMesh.vertexCount && src_colors != null; ++vi )
		{
			int vi0 = GetIntFromColor( src_colors[vi] );
			
			if( vi0 != vi )
			{
				// Vertex has been moved
				
				if( !vimap.ContainsKey(vi0) )
				{
					vimap[vi0] = new HashSet<int>();
				}
				
				vimap[vi0].Add(vi);
			}
			
			// Also find duplicate vertices (those that have been split)
			for( int dvi = 0; LEAPCore.morphHandleSplitVertices && dvi < srcMesh.vertexCount; ++dvi )
			{
				if( vi != dvi &&
				   Mathf.Abs( src_positions[vi].x - src_positions[dvi].x ) <= 0.001f &&
				   Mathf.Abs( src_positions[vi].y - src_positions[dvi].y ) <= 0.001f &&
				   Mathf.Abs( src_positions[vi].z - src_positions[dvi].z ) <= 0.001f &&
				   ( Mathf.Abs( src_uv[vi].x - src_uv[dvi].x ) > 0.001f ||
				    Mathf.Abs( src_uv[vi].y - src_uv[dvi].y ) > 0.001f ) )
				{
					// This vertex is a duplicate
					
					if( !dvimap.ContainsKey(vi) )
						dvimap.Add( vi, new HashSet<int>() );
					
					dvimap[vi].Add(dvi);
				}
			}
		}
		
		MorphTarget[] mts = new MorphTarget[mtMeshes.Length];
		List<MorphedVertex> mtverts = new List<MorphedVertex>();
		
		for( int mti = 0; mti < mtMeshes.Length; ++mti )
		{
			Vector3[] mt_positions = mtMeshes[mti].vertices;
			Vector3[] mt_normals = mtMeshes[mti].normals;
			Color[] mt_colors = mtMeshes[mti].colors;

			// Compute all morphed vertices
			for( int mtvi = 0; mtvi < mtMeshes[mti].vertexCount; ++mtvi )
			{
				int vi0 = GetIntFromColor( mt_colors[mtvi] );
				
				// Find affected vertices in the source mesh
				HashSet<int> vilist = null;
				if( vimap.ContainsKey(vi0) )
				{
					 vilist = vimap[vi0];
				}
				else
				{
					vilist = new HashSet<int>();
					vilist.Add(vi0);
				}
				
				if(LEAPCore.morphHandleSplitVertices)
				{
					// Also add duplicate vertices
					
					HashSet<int> dvilist = new HashSet<int>();
					foreach( int vi in vilist )
					{
						if( dvimap.ContainsKey(vi) )
							dvilist.UnionWith( dvimap[vi] );
					}
					vilist.UnionWith(dvilist);
				}
				
				// Compute vertex offsets
				foreach( int vi in vilist )
				{
					if( vi < 0 || vi >= srcMesh.vertexCount )
					{
						continue;
					}
					
					mtverts.Add( new MorphedVertex( vi, mt_positions[mtvi] - src_positions[vi],
					                               mt_normals[mtvi] - src_normals[vi] ) );
				}
			}
			
			// Create and initialize morph target
			mts[mti] = new MorphTarget(mtMeshes[mti].name);
			mts[mti].vertexIndices = new int[mtverts.Count];
			mts[mti].relVertices = new Vector3[mtverts.Count];
			mts[mti].relNormals = new Vector3[mtverts.Count];
			for( int mtvi = 0; mtvi < mtverts.Count; ++mtvi )
			{
				mts[mti].vertexIndices[mtvi] = mtverts[mtvi].vertexIndex;
				mts[mti].relVertices[mtvi] = mtverts[mtvi].relVertex;
				mts[mti].relNormals[mtvi] = mtverts[mtvi].relNormal;
			}
	
			mtverts.Clear();
		}
		
		return mts;
	}
	
	private struct MorphedVertex
	{
		public int vertexIndex;
		public Vector3 relVertex;
		public Vector3 relNormal;
		
		public MorphedVertex( int vertexIndex,
		                     Vector3 relVertex, Vector3 relNormal )
		{
			this.vertexIndex = vertexIndex;
			this.relVertex = relVertex;
			this.relNormal = relNormal;
		}
	}
	
	private static int GetIntFromColor( Color color )
	{
		int i = 0;
		i |= Convert.ToInt32( Math.Round( color.r * 0xFF ) ) << 24;
		i |= Convert.ToInt32( Math.Round( color.g * 0xFF ) )  << 16;
		i |= Convert.ToInt32( Math.Round( color.b * 0xFF ) )  << 8;
		i |= Convert.ToInt32( Math.Round( color.a * 0xFF ) ) ;
		
		return i;
	}
	
}

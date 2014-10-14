using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Catmull-Rom spline for interpolating 3D points.
/// </summary>
public class CatmullRomSpline
{
	/// <summary>
	/// Spline control points. 
	/// </summary>
	public List<Vector3> controlPoints = new List<Vector3>();
	
	/// <summary>
	/// Spline tangents. 
	/// </summary>
	public List<Vector3> tangents = new List<Vector3>();
	
	private Matrix4x4 coeffs;
	
	public CatmullRomSpline()
	{
		coeffs[0,0] = 2;
		coeffs[0,1] = -2;
		coeffs[0,2] = 1;
		coeffs[0,3] = 1;
		coeffs[1,0] = -3;
		coeffs[1,1] = 3;
		coeffs[1,2] = -2;
		coeffs[1,3] = -1;
		coeffs[2,0] = 0;
		coeffs[2,1] = 0;
		coeffs[2,2] = 1;
		coeffs[2,3] = 0;
		coeffs[3,0] = 1;
		coeffs[3,1] = 0;
		coeffs[3,2] = 0;
		coeffs[3,3] = 0;
		
		controlPoints = new List<Vector3>();
		tangents = new List<Vector3>();
	}
	
	/// <summary>
	/// Gets a point on the spline computed
	/// by interpolating between control points nearest to the
	/// value of the t parameter (normalized to range 0-1).
	/// Points are assumed to be evenly spaced, so this should *not*
	/// be used for interpolating between key-frames.
	/// </summary>
	/// <param name="t">
	/// Parameter value.
	/// </param>
	/// <returns>
	/// Interpolated point.
	/// </returns>
	public Vector3 GetPoint( float t )
	{
		// Compute left-hand control point index
        float fcpi = t * (float)( controlPoints.Count - 1 );
        int cpi = (int)Math.Floor(fcpi);
		if( cpi >= controlPoints.Count - 1 ) 
			return controlPoints[controlPoints.Count-1];
        
		// Compute interp. param.
        t = fcpi - (float)cpi;

        return GetPoint( cpi, t );
	}
	
	/// <summary>
	///  Gets a point on the spline computed
	/// by interpolating between the control point at the specified index
	/// and its right-hand neighbor.
	/// </summary>
	/// <param name="index">
	/// Control point index. <see cref="System.Int32"/>
	/// </param>
	/// <param name="t">
	/// Parameter value.
	/// </param>
	/// <returns>
	/// Interpolated point.
	/// </returns>
	public Vector3 GetPoint( int index, float t )
	{
		if( tangents.Count != controlPoints.Count )
			RecomputeTangents();
		
		if( index >= controlPoints.Count - 1 )
			return controlPoints[ controlPoints.Count - 1 ];
		
		if( t == 0.0f )
			return controlPoints[index];
		else if( t == 1.0f )
			return controlPoints[index+1];

		float t2 = t*t;
		Vector4 tv = new Vector4();
		tv[0] = t2*t;
		tv[1] = t2;
		tv[2] = t;
		tv[3] = 1;
		
		tv = coeffs.transpose * tv;

		Vector3 cpt1 = controlPoints[index];
		Vector3 cpt2 = controlPoints[index+1];
		Vector3 tan1 = tangents[index];
		Vector3 tan2 = tangents[index+1];
		Vector3 pt = cpt1;
		
		for( int ei = 0; ei < 2; ++ei )
		{
			pt[ei] = tv[0] * cpt1[ei] +
				tv[1] * cpt2[ei] +
					tv[2] * tan1[ei] +
					tv[3] * tan2[ei];
		}
		// TODO: quaternions needs to be handled separately

		return pt;
	}
	
	/// <summary>
	///  Gets a tangent on the spline computed
	/// by interpolating between tangents at the control points
	/// nearest to the value of the t parameter (normalized to range 0-1).
	/// Points are assumed to be evenly spaced, so this should *not*
	/// be used for interpolating between key-frames.
	/// </summary>
	/// <param name="t">
	/// Parameter value.
	/// </param>
	/// <returns>
	/// Interpolated tangent.
	/// </returns>
	public Vector3 GetTangent( float t )
	{
		// compute left-hand control point index
        float fcpi = t * ( float )( controlPoints.Count - 1 );
        int cpi = ( int )fcpi;
		if( cpi >= controlPoints.Count ) 
			cpi = controlPoints.Count - 1;
        
		// compute interp. param.
        t = fcpi - ( float )cpi;

        return GetTangent( cpi, t );
	}
	
	/// <summary>
	/// Gets a tangent on the spline computed
	/// by interpolating between the control point at the specified index
	/// and its right-hand neighbor.
	/// </summary>
	/// <param name="index">
	/// Control point index. <see cref="System.Int32"/>
	/// </param>
	/// <param name="t">
	/// Parameter value.
	/// </param>
	/// <returns>
	/// Interpolated tangent.
	/// </returns>
	public Vector3 GetTangent( int index, float t )
	{
		if( tangents.Count != controlPoints.Count )
			RecomputeTangents();
		
		if( index >= controlPoints.Count - 1 )
			return tangents[ controlPoints.Count - 1 ];

		if( t == 0.0f )
			return tangents[index];
		else if( t == 1.0f )
			return tangents[index+1];

		float t2 = t*t;
		Vector4 tv = new Vector4();
		tv[0] = 3.0f*t2;
		tv[1] = 2.0f*t;
		tv[2] = 1.0f;
		tv[3] = 0.0f;
		tv  = coeffs.transpose * tv;

		Vector3 cpt1 = controlPoints[index];
		Vector3 cpt2 = controlPoints[index+1];
		Vector3 tan1 = tangents[index];
		Vector3 tan2 = tangents[index+1];
		Vector3 tang = cpt1;
		
		for( int ei = 0; ei < 2; ++ei )
		{
			tang[ei] = tv[0] * cpt1[ei] +
				tv[1] * cpt2[ei] +
					tv[2] * tan1[ei] +
					tv[3] * tan2[ei];
		}
		// TODO: quaternions needs to be handled separately

		return tang;
	}
	
	// TODO: Add ability to recompute a specific tangent
	/// <summary>
	/// Automatically computes the tangents for this set of control points.
	/// </summary>
	public void RecomputeTangents()
	{
		int ncpts = controlPoints.Count;
		if( ncpts < 2 )
			return;

		bool closed = controlPoints[0] == controlPoints[ncpts-1] ? true : false;
		
		tangents.Clear();

		for( int cpi = 0; cpi < ncpts; ++cpi )
		{
			tangents.Add( new Vector3() );
			if( cpi == 0 )
			{
				if(closed)
					tangents[cpi] = ( controlPoints[1] - controlPoints[ncpts-2] ) * 0.5f;
				else
					tangents[cpi] = ( controlPoints[1] - controlPoints[0] ) * 0.5f;
			}
			else if( cpi == ncpts - 1 )
			{
				if( closed )
					tangents[cpi] = tangents[0];
				else
					tangents[cpi] = ( controlPoints[cpi] - controlPoints[cpi-1] ) * 0.5f;
			}
			else
			{
				tangents[cpi] = ( controlPoints[cpi+1] - controlPoints[cpi-1] ) * 0.5f;
			}
		}
		// TODO: quaternions needs to be handled separately
	}
}


using UnityEngine;
using System;
using System.Collections;

/// <summary>
/// Cubic Bezier curve, for interpolating 3D points.
/// </summary>
[Serializable]
public class BezierCurve
{
	public Vector3[] controlPoints;
	public float[] sParam = new float[0];
	public float[] uParam = new float[0];
	
	public int NumSegments
	{
		get
		{
			return controlPoints.Length/3;
		}
	}
	
	public BezierCurve()
	{
		controlPoints = new Vector3[0];
	}
	
	public BezierCurve( Vector3[] pts )
	{
		sParam = new float[0];
		uParam = new float[0];
		
		int npts = pts.Length;
		controlPoints = new Vector3[npts*3-2];
		for( int i = 0; i < npts; ++i )
		{
			Vector3 vnm1 = i <= 0 ? pts[i] : pts[i-1];
			Vector3 vn = pts[i];
			Vector3 vn1 = i >= npts-1 ? pts[i] : pts[i+1];
			Vector3 an;
			if( i > 0 )
			{
				Vector3 dv = 2f*Vector3.Dot(vnm1,vn)*vn - vnm1;
				an = (dv+vn1).normalized;
			}
			else
				an = vn;
			Vector3 bn;
			if( i < npts-1 )
				bn = 2f*Vector3.Dot(an,vn)*vn - an;
			else
				bn = vn;
			controlPoints[3*i] = vn;
			if( i < npts-1 )
				controlPoints[3*i+1] = an;
			if( i > 0 )
				controlPoints[3*i-1] = bn;
		}
	}
	
	public void initParams( float[] s, float[] u )
	{
		sParam = s;
		uParam = u;
	}
	
	public Vector3 uSample( float u )
	{
		int segi = (int)Mathf.Floor(u);
		float t = u - segi;
		
		if( segi*4 >= controlPoints.Length )
		{
			--segi;
			t = 1;
		}
		
		return ( ( ( -controlPoints[3*segi] + 3*( controlPoints[3*segi+1] - controlPoints[3*segi+2] ) +
		            controlPoints[3*segi+3] )*t +
		          ( 3*( controlPoints[3*segi] + controlPoints[3*segi+2] ) - 6*controlPoints[3*segi+1] ) )*t +
		        3*( controlPoints[3*segi+1] - controlPoints[3*segi] ) )*t + controlPoints[3*segi];
	}
	
	public Vector3 sSample( float s )
	{
		if( sParam.Length <= 0 )
			return controlPoints[0];
		
		// Binary search for nearest samples
		int imin = 0;
		int imax = sParam.Length-1;
		while( imax > imin+1 )
		{
			int imid = (imin+imax)/2;
	 
			// determine which subarray to search
			if( sParam[imid] <= s )
				imin = imid;
			else if( sParam[imid] > s )
				imax = imid;
		}
		
		// Compute sample
		float u = 0;
		if( imin >= uParam.Length-1 )
			u = uParam[imin];
		else
		{
			float ds = sParam[imin+1] - sParam[imin];
			float t = ds > 0.00001f ? ( s - sParam[imin] )/ds : 0;
			u = (1-t)*uParam[imin] + t*uParam[imin+1];
		}

		return uSample(u);
	}
	
	/// <summary>
	/// Generate arc-length parametrization of the curve. 
	/// </summary>
	/// <param name="n">
	/// Number of samples.
	/// </param>
	/// <returns>Arc-length of the curve.</returns>
	public float arcLengthParam( int n )
	{
		sParam = new float[n];
		uParam = new float[n];
		
		float s = 0;
		Vector3 v = controlPoints[0];
		for( int i = 0; i < n; ++i )
		{
			float u = ((float)i)/(n-1)*NumSegments;
			uParam[i] = u;
			
			Vector3 v1 = uSample(u);
			s += Vector3.Distance(v,v1);
			v = v1;
			sParam[i] = s;
		}
		
		if( s < 0.00001f )
			return 0;
		
		// Normalize arc-lengths
		for( int i = 0; i < n; ++i )
			sParam[i] /= s;
		
		return s;
	}
}

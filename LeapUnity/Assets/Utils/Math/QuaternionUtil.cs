using UnityEngine;
using System.Collections;

/// <summary>
/// Utility class containing some helpful quaternion functions. 
/// </summary>
public static class QuaternionUtil
{
	// Multiply quaternion with a scalar.
	public static Quaternion Mul( Quaternion q, float v )
	{
		q.x *= v;
		q.y *= v;
		q.z *= v;
		q.w *= v;
		
		return q;
	}
	
	// Add two quaternions.
	public static Quaternion Add( Quaternion q1, Quaternion q2 )
	{
		q1.x += q2.x;
		q1.y += q2.y;
		q1.z += q2.z;
		q1.w += q2.w;
		
		return q1;
	}
	
	// Subtract two quaternions.
	public static Quaternion Sub( Quaternion q1, Quaternion q2 )
	{
		q1.x -= q2.x;
		q1.y -= q2.y;
		q1.z -= q2.z;
		q1.w -= q2.w;
		
		return q1;
	}
	
	// Compute the norm of a quaternion.
	public static float Norm( Quaternion q )
	{
		return Mathf.Sqrt( q.x*q.x + q.y*q.y + q.z*q.z + q.w*q.w );
	}
	
	// Quaternion log-map.
	public static Vector3 Log( Quaternion q )
	{
		Vector3 qv = new Vector3(q.x,q.y,q.z);
		Vector3 v = 2f*Mathf.Acos(q.w)/qv.magnitude * qv;
		
		return v;
	}
	
	// Quaternion exponential map.
	public static Quaternion Exp( Vector3 v )
	{
		float phi = v.magnitude;
		Vector3 qv = Mathf.Sin(0.5f*phi)/phi*v;
		Quaternion q = new Quaternion( qv.x, qv.y, qv.z, Mathf.Cos(0.5f*phi) );
		
		return q;
	}
	
	public static bool Equal( Vector3 v1, Vector3 v2 )
	{
		return Mathf.Abs(v1.x-v2.x) < 0.001f &&
			Mathf.Abs(v1.y-v2.y) < 0.001f &&
				Mathf.Abs(v1.z-v2.z) < 0.001f;
	}
}

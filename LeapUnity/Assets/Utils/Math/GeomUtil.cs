using UnityEngine;
using System.Collections;

/// <summary>
/// Some useful geometry algorithms.
/// </summary>
public class GeomUtil
{
    public static bool Equal(Vector3 v1, Vector3 v2)
    {
        return Mathf.Abs(v1.x - v2.x) < 0.001f &&
            Mathf.Abs(v1.y - v2.y) < 0.001f &&
                Mathf.Abs(v1.z - v2.z) < 0.001f;
    }

    /// <summary>
    /// Project vector onto a plane.
    /// </summary>
    /// <param name="v">Vector to project</param>
    /// <param name="n">Plane normal</param>
    /// <returns>Projected vector</returns>
    public static Vector3 ProjectVectorOntoPlane(Vector3 v, Vector3 n)
    {
        return v - (Vector3.Dot(v, n) / n.sqrMagnitude) * n;
    }

	/// <summary>
	/// Find points on two lines where the lines are the closest
	/// in 3D space.
	/// </summary>
	public static void ClosestPoints2Lines( Vector3 u1, Vector3 u2,
	                                       Vector3 v1, Vector3 v2,
	                                       out float ut, out float vt )
	{
		Vector3 u = u2-u1;
		Vector3 v = v2-v1;
		Vector3 rho = v1-u1;
		float uv = Vector3.Dot(u,v);
		float uu = Vector3.Dot(u,u);
		float vv = Vector3.Dot(v,v);
		float urho = Vector3.Dot(u,rho);
		float vrho = Vector3.Dot(v,rho);
		vt = ( vrho*uu - urho*uv )/( uv*uv - vv*uu );
		vt = float.IsNaN(vt) || float.IsInfinity(vt) ? 0 : vt;
		ut = ( uv*vt + urho )/uu;
		ut = float.IsNaN(ut) || float.IsInfinity(ut) ? 0 : ut;		
	}
	
	//Two non-parallel lines which may or may not touch each other have a point on each line which are closest
	//to each other. This function finds those two points. If the lines are parallel, the function 
	//outputs true, otherwise false.
	public static bool ClosestPointsOnTwoLines(out Vector3 closestPointLine1, out Vector3 closestPointLine2, Vector3 linePoint1, Vector3 lineVec1, Vector3 linePoint2, Vector3 lineVec2){
 
		closestPointLine1 = Vector3.zero;
		closestPointLine2 = Vector3.zero;
 
		float a = Vector3.Dot(lineVec1, lineVec1);
		float b = Vector3.Dot(lineVec1, lineVec2);
		float e = Vector3.Dot(lineVec2, lineVec2);
 
		float d = a*e - b*b;
 
		//lines are not parallel
		if(d != 0.0f){
 
			Vector3 r = linePoint1 - linePoint2;
			float c = Vector3.Dot(lineVec1, r);
			float f = Vector3.Dot(lineVec2, r);
 
			float s = (b*f - c*e) / d;
			float t = (a*f - c*b) / d;
 
			closestPointLine1 = linePoint1 + lineVec1 * s;
			closestPointLine2 = linePoint2 + lineVec2 * t;
 
			return false;
		}
 
		else{
			return true;
		}
	}
}


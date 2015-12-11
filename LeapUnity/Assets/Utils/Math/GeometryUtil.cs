using UnityEngine;
using System.Collections;

/// <summary>
/// Some useful geometry algorithms.
/// </summary>
public class GeometryUtil
{
    /// <summary>
    /// true if two vectors are equal, false otherwise.
    /// </summary>
    /// <param name="v1"></param>
    /// <param name="v2"></param>
    /// <returns></returns>
    public static bool Equal(Vector3 v1, Vector3 v2)
    {
        return Mathf.Abs(v1.x - v2.x) < 0.001f &&
            Mathf.Abs(v1.y - v2.y) < 0.001f &&
                Mathf.Abs(v1.z - v2.z) < 0.001f;
    }

    /// <summary>
    /// Remap angle to [-180, 180] range.
    /// </summary>
    /// <param name="angle">Angle</param>
    /// <returns>Angle in [-180, 180] range</returns>
    public static float RemapAngle(float angle)
    {
        while (angle > 180f)
            angle -= 360f;
        while (angle < -180f)
            angle += 360f;

        return angle;
    }

    /// <summary>
    /// Project point onto a line.
    /// </summary>
    /// <param name="p1">Line start point</param>
    /// <param name="p2">Line end point</param>
    /// <returns>Projected vector</returns>
    public static Vector3 ProjectPointOntoLine(Vector3 p1, Vector3 p2, Vector3 p)
    {
        Vector3 p12 = p2 - p1;
        return p1 + (Vector3.Dot(p - p1, p12) / Vector3.Dot(p12, p12)) * p12;
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
	public static void ClosestPointsOn2Lines(Vector3 u1, Vector3 u2,
        Vector3 v1, Vector3 v2, out float ut, out float vt)
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
	
	/// <summary>
    /// Two non-parallel lines which may or may not touch each other have a point on each line which are closest
    /// to each other. This function finds those two points. If the lines are parallel, the function 
    /// outputs true, otherwise false.
	/// </summary>
	/// <param name="p1"></param>
	/// <param name="p2"></param>
	/// <param name="u1"></param>
	/// <param name="v1"></param>
	/// <param name="u2"></param>
	/// <param name="v2"></param>
	/// <returns></returns>
	public static bool ClosestPointsOn2Lines(Vector3 u1, Vector3 v1, Vector3 u2, Vector3 v2, out Vector3 p1, out Vector3 p2)
    {
		p1 = Vector3.zero;
		p2 = Vector3.zero;
 
		float a = Vector3.Dot(v1, v1);
		float b = Vector3.Dot(v1, v2);
		float e = Vector3.Dot(v2, v2);
 
		float d = a*e - b*b;
 
		//lines are not parallel
		if(d != 0.0f)
        {
 
			Vector3 r = u1 - u2;
			float c = Vector3.Dot(v1, r);
			float f = Vector3.Dot(v2, r);
 
			float s = (b*f - c*e) / d;
			float t = (a*f - c*b) / d;
 
			p1 = u1 + v1 * s;
			p2 = u2 + v2 * t;
 
			return false;
		}
		else
        {
			return true;
		}
	}

    /// <summary>
    /// Laplacian smoothing of a curve.
    /// </summary>
    /// <param name="pts">Curve points</param>
    /// <param name="numIterations">Number of iterations</param>
    /// <param name="lambda">Lambda parameter</param>
    /// <param name="mu">Mu parameter</param>
    public static void SmoothCurve(float[] pts, int numIterations, float lambda, float mu)
    {
        lambda = lambda < 0f ? 0f : lambda;
        mu = mu > 0f ? 0f : mu;

        float[] pts0 = (float[])pts.Clone();
        for (int iter = 0; iter < numIterations; ++iter)
        {
            for (int index = 1; index < pts.Length - 1; ++index)
            {
                float lp = 0.5f * (pts[index + 1] - pts[index] + pts[index - 1] - pts[index]);
                pts0[index] = pts[index] + lambda * lp;
            }

            for (int index = 1; index < pts.Length - 1; ++index)
            {
                float lp = 0.5f * (pts0[index + 1] - pts0[index] + pts0[index - 1] - pts0[index]);
                pts[index] = pts0[index] + mu * lp;
            }
        }
    }
}

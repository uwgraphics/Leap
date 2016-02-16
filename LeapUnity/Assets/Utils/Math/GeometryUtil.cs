using UnityEngine;
using System.Collections;
using System;

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
    /// Project point onto a line segment.
    /// </summary>
    /// <param name="p">Point</param>
    /// <param name="v1">Line segment start point</param>
    /// <param name="v2">Line segment end point</param>
    /// <returns>Projected point</returns>
    public static Vector3 ProjectPointOntoLineSegment(Vector3 p, Vector3 v1, Vector3 v2)
    {
	    float l = Vector3.Distance(v1, v2);
	    if (l <= 0.001f)
		    return v1;

	    float t = Vector3.Dot(p - v1, v2 - v1) / (l * l);
	    if (t <= 0f)
		    return v1;
	    else if (t >= 1f)
		    return v2;

	    Vector3 pt = v1 + t * (v2 - v1);
	    return pt;
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
	public static bool ClosestPointsOn2Lines(Vector3 u1, Vector3 v1, Vector3 u2, Vector3 v2,
        out Vector3 p1, out Vector3 p2)
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

    /// <summary>
    /// Compute rigid transformation that aligns the "left" point set
    /// to the "right" point  set.
    /// </summary>
    /// <param name="pl">Left point set</param>
    /// <param name="pr">Right point set</param>
    /// <param name="t">Translation</param>
    /// <param name="R">Rotation</param>
    public static void AlignPointSets(Vector3[] pl, Vector3[] pr, out Vector3 t, out Matrix3x3 R)
    {
        if (pl.Length != pr.Length)
            throw new ArgumentException("Left and right point sets have different cardinality", "pl");

        int n = pl.Length;
        if (n < 3)
            throw new ArgumentException("Point sets have too few points: " + n, "pl");

        // Set initial transformation
        t = new Vector3(0f, 0f, 0f);
        R = Matrix3x3.zero;

        // Compute centroids of the point sets
        Vector3 pl0 = Vector3.zero;
        Vector3 pr0 = Vector3.zero;
        for (int i = 0; i < n; ++i)
        {
            pl0 += pl[i];
            pr0 += pr[i];
        }
        pl0 /= ((float)n);
        pr0 /= ((float)n);

        // Recenter the point sets
        Vector3[] plc = new Vector3[n];
        Vector3[] prc = new Vector3[n];
        for (int i = 0; i < n; ++i)
        {
            plc[i] = pl[i] - pl0;
            prc[i] = pr[i] - pr0;
        }

        // Compute matrix of correlations H
        Matrix3x3 H = Matrix3x3.zero;
        for (int i = 0; i < n; ++i)
            H += Matrix3x3.MultiplyVectors(plc[i], prc[i]);

        // Solve for SVD(H)
        double[,] aH = new double[3, 3];
        for (int i = 0; i < 3; ++i)
            for (int j = 0; j < 3; ++j)
                aH[i, j] = (double)H[i, j];
        double[] aw = new double[3];
        double[,] aU = new double[3,3];
        double[,] aVt = new double[3, 3];
        alglib.rmatrixsvd(aH, 3, 3, 2, 2, 2, out aw, out aU, out aVt);
        Matrix3x3 U = Matrix3x3.zero;
        Matrix3x3 Vt = Matrix3x3.zero;
        float[] w = new float[3];
        for (int i = 0; i < 3; ++i)
        {
            for (int j = 0; j < 3; ++j)
            {
                U[i, j] = (float)aU[i, j];
                Vt[i, j] = (float)aVt[i, j];
            }
            w[i] = (float)aw[i];
        }
        
        // Compute rotation
        Matrix3x3 V = Vt.transpose;
        R = V * U.transpose;
        if (R.determinant < 0f)
        {
            Matrix3x3 V1 = Matrix3x3.zero;
            V1.SetColumn(0, V.GetColumn(0));
            V1.SetColumn(1, V.GetColumn(1));
            V1.SetColumn(2, -V.GetColumn(2));
            R = V1 * U.transpose;
        }

        // Compute translation
        t = pr0 - R.MultiplyPoint(pl0);
    }

    /// <summary>
    /// Compute affine transformation that aligns the "left" point set
    /// to the "right" point  set.
    /// </summary>
    /// <param name="pl">Left point set</param>
    /// <param name="pr">Right point set</param>
    /// <param name="t">Translation</param>
    /// <param name="R">Rotation</param>
    /// <param name="s">Scale</param>
    public static void AlignPointSets(Vector3[] pl, Vector3[] pr, out Vector3 t, out Matrix3x3 R, out float s)
    {
        if (pl.Length != pr.Length)
            throw new ArgumentException("Left and right point sets have different cardinality", "pl");

        int n = pl.Length;
        if (n < 3)
            throw new ArgumentException("Point sets have too few points: " + n, "pl");

        // Set initial transformation
        t = new Vector3(0f, 0f, 0f);
        R = Matrix3x3.zero;
        s = 1f;

        // Compute centroids of the point sets
        Vector3 pl0 = Vector3.zero;
        Vector3 pr0 = Vector3.zero;
        for (int i = 0; i < n; ++i)
        {
            pl0 += pl[i];
            pr0 += pr[i];
        }
        pl0 /= ((float)n);
        pr0 /= ((float)n);

        // Recenter the point sets
        Vector3[] plc = new Vector3[n];
        Vector3[] prc = new Vector3[n];
        for (int i = 0; i < n; ++i)
        {
            plc[i] = pl[i] - pl0;
            prc[i] = pr[i] - pr0;
        }

        // Compute scale
        s = 1f;
        float s1 = 0f, s2 = 0f;
        for (int i = 0; i < n; ++i)
        {
            s1 += prc[i].sqrMagnitude;
            s2 += plc[i].sqrMagnitude;
        }
        s = Mathf.Sqrt(s1 / s2);

        // Compute matrix MtM
        Matrix3x3 M = Matrix3x3.zero;
        for (int i = 0; i < n; ++i)
            M += Matrix3x3.MultiplyVectors(prc[i], plc[i]);
        Matrix3x3 MtM = M.transpose * M;
        Vector3 u1, u2, u3;
        float l1, l2, l3;

        // Solve for eigenvalues and eigenvectors of MtM
        double[,] aMtM = new double[3, 3];
        for (int i = 0; i < 3; ++i)
            for (int j = 0; j < 3; ++j)
                aMtM[i, j] = (double)MtM[i, j];
        double[] al = new double[3];
        double[,] au = new double[3, 3];
        alglib.evd.smatrixevd(aMtM, 3, 1, false, ref al, ref au);
        l3 = (float)al[0];
        l2 = (float)al[1];
        l1 = (float)al[2];
        u3 = new Vector3((float)au[0, 0], (float)au[1, 0], (float)au[2, 0]);
        u2 = new Vector3((float)au[0, 1], (float)au[1, 1], (float)au[2, 1]);
        u1 = new Vector3((float)au[0, 2], (float)au[1, 2], (float)au[2, 2]);
        
        // Compute rotation
        Matrix3x3 R1, R2;
        if (Mathf.Abs(l3) < 0.01f)
        {
            R1 = Matrix3x3.MultiplyVectors(u1, u1) / Mathf.Sqrt(l1) +
                Matrix3x3.MultiplyVectors(u2, u2) / Mathf.Sqrt(l2);
            R1 = M * R1;
            R2 = Matrix3x3.MultiplyVectors(u3, u3);
            R = R1 + R2;
            if (R.determinant <= 0f)
                R = R1 - R2;
        }
        else
        {
            R1 = Matrix3x3.MultiplyVectors(u1, u1) / Mathf.Sqrt(l1) +
                Matrix3x3.MultiplyVectors(u2, u2) / Mathf.Sqrt(l2) +
                Matrix3x3.MultiplyVectors(u3, u3) / Mathf.Sqrt(l3);
            R = M * R1;
        }

        // Compute translation
        t = pr0 - s * R.MultiplyPoint(pl0);
    }
}

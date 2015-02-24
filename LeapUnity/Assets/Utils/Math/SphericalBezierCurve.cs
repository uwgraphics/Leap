using UnityEngine;
using System;
using System.Collections;

/// <summary>
/// Spherical Bezier curve, for interpolating quaternion rotations.
/// </summary>
[Serializable]
public class SphericalBezierCurve
{
    public Quaternion[] controlPoints = new Quaternion[0];
    public float[] sParam = new float[0];
    public float[] uParam = new float[0];

    public int NumSegments
    {
        get
        {
            return controlPoints.Length / 3;
        }
    }

    public float ArcLength
    {
        get
        {
            int n = sParam.Length - 1;
            Quaternion q = controlPoints[0];
            float s = 0;
            for (int i = 0; i < n; ++i)
            {
                float u = ((float)i) / (n - 1) * NumSegments;
                Quaternion q1 = uSample(u);
                s += Quaternion.Angle(q, q1);
                q = q1;
            }

            return s;
        }
    }

    public SphericalBezierCurve()
    {
        controlPoints = new Quaternion[0];
    }

    public SphericalBezierCurve(Quaternion[] pts)
    {
        sParam = new float[0];
        uParam = new float[0];

        int npts = pts.Length;
        controlPoints = new Quaternion[npts * 3 - 2];
        for (int i = 0; i < npts; ++i)
        {
            Quaternion qnm1 = i <= 0 ? pts[i] : pts[i - 1];
            Quaternion qn = pts[i];
            Quaternion qn1 = i >= npts - 1 ? pts[i] : pts[i + 1];
            Quaternion an;
            if (i > 0)
            {
                Quaternion dq = QuaternionUtil.Sub(QuaternionUtil.Mul(qn, 2f * Quaternion.Dot(qnm1, qn)), qnm1);
                an = QuaternionUtil.Add(dq, qn1);
                an = QuaternionUtil.Mul(an, 1f / QuaternionUtil.Norm(an));
            }
            else
                an = qn;
            Quaternion bn;
            if (i < npts - 1)
                bn = QuaternionUtil.Sub(QuaternionUtil.Mul(qn, 2f * Quaternion.Dot(an, qn)), an);
            else
                bn = qn;
            controlPoints[3 * i] = qn;
            if (i < npts - 1)
                controlPoints[3 * i + 1] = an;
            if (i > 0)
                controlPoints[3 * i - 1] = bn;
        }
    }

    public void initParams(float[] s, float[] u)
    {
        sParam = s;
        uParam = u;
    }

    public Quaternion uSample(float u)
    {
        int segi = (int)Mathf.Floor(u);
        float t = u - segi;

        if (segi * 4 >= controlPoints.Length)
        {
            --segi;
            t = 1;
        }

        Quaternion p01 = Quaternion.Slerp(controlPoints[3 * segi], controlPoints[3 * segi + 1], t);
        Quaternion p11 = Quaternion.Slerp(controlPoints[3 * segi + 1], controlPoints[3 * segi + 2], t);
        Quaternion p21 = Quaternion.Slerp(controlPoints[3 * segi + 2], controlPoints[3 * segi + 3], t);
        Quaternion p02 = Quaternion.Slerp(p01, p11, t);
        Quaternion p12 = Quaternion.Slerp(p11, p21, t);

        return Quaternion.Slerp(p02, p12, t);
    }

    public Quaternion sSample(float s)
    {
        if (sParam.Length <= 0)
            return controlPoints[0];

        // Binary search for nearest samples
        int imin = 0;
        int imax = sParam.Length - 1;
        while (imax > imin + 1)
        {
            int imid = (imin + imax) / 2;

            // determine which subarray to search
            if (sParam[imid] <= s)
                imin = imid;
            else if (sParam[imid] > s)
                imax = imid;
        }

        // Compute sample
        float u = 0;
        if (imin >= uParam.Length - 1)
            u = uParam[imin];
        else
        {
            float ds = sParam[imin + 1] - sParam[imin];
            float t = ds > 0.00001f ? (s - sParam[imin]) / ds : 0;
            u = (1 - t) * uParam[imin] + t * uParam[imin + 1];
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
    public float arcLengthParam(int n)
    {
        sParam = new float[n];
        uParam = new float[n];

        float s = 0;
        Quaternion q = controlPoints[0];
        for (int i = 0; i < n; ++i)
        {
            float u = ((float)i) / (n - 1) * NumSegments;
            uParam[i] = u;

            Quaternion q1 = uSample(u);
            s += Quaternion.Angle(q, q1);
            q = q1;
            sParam[i] = s;
        }

        if (s < 0.00001f)
            return 0;

        // Normalize arc-lengths
        for (int i = 0; i < n; ++i)
            sParam[i] /= s;

        return s;
    }
}

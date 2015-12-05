using UnityEngine;
using System;
using System.Collections;

/// <summary>
/// Cubic, composite Bezier curve.
/// </summary>
[Serializable]
public class BezierCurve
{
    public Vector3[] controlPoints = new Vector3[0];
    public int numberOfLengthSamples = 40;

    private float[] _s = new float[0];
    private float[] _u = new float[0];

    public int NumberOfSegments
    {
        get
        {
            return controlPoints.Length / 3;
        }
    }

    public BezierCurve()
    {
        controlPoints = new Vector3[0];
    }

    public BezierCurve(Vector3[] pts, int numLengthSamples = 40)
    {
        this.numberOfLengthSamples = numLengthSamples;

        // Compute control points
        int npts = pts.Length;
        controlPoints = new Vector3[npts * 3 - 2];
        for (int i = 0; i < npts; ++i)
        {
            Vector3 vnm1 = i <= 0 ? pts[i] : pts[i - 1];
            Vector3 vn = pts[i];
            Vector3 vn1 = i >= npts - 1 ? pts[i] : pts[i + 1];
            Vector3 an;
            if (i > 0)
            {
                Vector3 dv = 2f * Vector3.Dot(vnm1, vn) * vn - vnm1;
                an = (dv + vn1).normalized;
            }
            else
                an = vn;
            Vector3 bn;
            if (i < npts - 1)
                bn = 2f * Vector3.Dot(an, vn) * vn - an;
            else
                bn = vn;
            controlPoints[3 * i] = vn;
            if (i < npts - 1)
                controlPoints[3 * i + 1] = an;
            if (i > 0)
                controlPoints[3 * i - 1] = bn;
        }

        Init();
    }

    public void Init()
    {
        _ComputeLengthParam();
    }

    public Vector3 Sample(float u)
    {
        int segi = (int)Mathf.Floor(u);
        float t = u - segi;

        if (segi * 4 >= controlPoints.Length)
        {
            --segi;
            t = 1;
        }

        return (((-controlPoints[3 * segi] + 3 * (controlPoints[3 * segi + 1] - controlPoints[3 * segi + 2]) +
                    controlPoints[3 * segi + 3]) * t +
                  (3 * (controlPoints[3 * segi] + controlPoints[3 * segi + 2]) - 6 * controlPoints[3 * segi + 1])) * t +
                3 * (controlPoints[3 * segi + 1] - controlPoints[3 * segi])) * t + controlPoints[3 * segi];
    }

    public Vector3 SampleLength(float s)
    {
        if (_s.Length <= 0)
            return controlPoints[0];

        // Binary search for nearest samples
        int imin = 0;
        int imax = _s.Length - 1;
        while (imax > imin + 1)
        {
            int imid = (imin + imax) / 2;

            // Determine which subarray to search
            if (_s[imid] <= s)
                imin = imid;
            else if (_s[imid] > s)
                imax = imid;
        }

        // Compute sample
        float u = 0;
        if (imin >= _u.Length - 1)
            u = _u[imin];
        else
        {
            float ds = _s[imin + 1] - _s[imin];
            float t = ds > 0.00001f ? (s - _s[imin]) / ds : 0;
            u = (1 - t) * _u[imin] + t * _u[imin + 1];
        }

        return Sample(u);
    }

    // Generate arc-length parametrization of the curve. 
    private void _ComputeLengthParam()
    {
        int n = numberOfLengthSamples * NumberOfSegments;
        _s = new float[n];
        _u = new float[n];

        float s = 0;
        Vector3 v = controlPoints[0];
        for (int i = 0; i < n; ++i)
        {
            float u = ((float)i) / (n - 1) * NumberOfSegments;
            _u[i] = u;

            Vector3 v1 = Sample(u);
            s += Vector3.Distance(v, v1);
            v = v1;
            _s[i] = s;
        }

        if (s < 0.00001f)
            return;

        // Normalize arc-lengths
        for (int i = 0; i < n; ++i)
            _s[i] /= s;
    }
}

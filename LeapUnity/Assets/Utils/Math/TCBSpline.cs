using UnityEngine;
using System;
using System.Collections;

/// <summary>
/// Cubic Hermite spline with TCB (tension-continuity-bias) controls.
/// </summary>
[Serializable]
public class TCBSpline
{
    // Spline controls
    public Vector2[] points = new Vector2[0];
    public float[] tension = new float[0];
    public float[] continuity = new float[0];
    public float[] bias = new float[0];

    // Arc length parametrization
    public int numLengthSamples = 50;

    private Vector2[] _a, _b, _c, _d;
    private float[] _s = new float[0];
    private float[] _u = new float[0];

    public int NumberOfSegments
    {
        get { return points.Length - 1; }
    }

    public TCBSpline(Vector2[] points, int numLengthSamples = 50)
    {
        this.points = points;
        this.tension = new float[points.Length];
        this.continuity = new float[points.Length];
        this.bias = new float[points.Length];
        this.numLengthSamples = numLengthSamples;

        Init();
    }

    public void Init()
    {
        if (points == null || points.Length < 2)
            throw new Exception("TCBSpline must have more than 2 control points!");

        // Compute polynomial coefficients
        _ComputePolynomial(0, 0, 1, 2);
        for (int segmentIndex = 1; segmentIndex < NumberOfSegments - 1; ++segmentIndex)
            _ComputePolynomial(segmentIndex - 1, segmentIndex, segmentIndex + 1, segmentIndex + 2);
        _ComputePolynomial(NumberOfSegments - 2, NumberOfSegments - 1, NumberOfSegments, NumberOfSegments);

        _ComputeArcLengthParam();
    }

    public Vector2 Sample(float u)
    {
        // Compute segment and parameter value
        int i = (int)Math.Floor(u);
        if (i >= points.Length)
            return points[points.Length - 1];
        float t = u - (float)i;

        // Compute point on the curve
        Vector2 p = _a[i] + t * (_b[i] + t*(_c[i] + t * _d[i]));
        return p;
    }

    public Vector3 SampleLength(float s)
    {
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

    private void _ComputePolynomial(int i0, int i1, int i2, int i3)
    {
        Vector2 diff = points[i2] - points[i1];

        // Multipliers at first point
        float oneMinusT0 = 1f - tension[i1];
        float oneMinusC0 = 1f - continuity[i1];
        float onePlusC0 = 1f + continuity[i1];
        float oneMinusB0 = 1f - bias[i1];
        float onePlusB0 = 1f + bias[i1];
        float out0 = 0.5f * oneMinusT0 * onePlusC0 * onePlusB0;
        float out1 = 0.5f * oneMinusT0 * oneMinusC0 * oneMinusB0;

        // Outgoing tangent at first point
        Vector2 tOut = out1 * diff + out0 * (points[i1] - points[i0]);

        // Multipliers at second point
        float oneMinusT1 = 1f - tension[i2];
        float oneMinusC1 = 1f - continuity[i2];
        float onePlusC1 = 1f + continuity[i2];
        float oneMinusB1 = 1f - bias[i2];
        float onePlusB1 = 1f + bias[i2];
        float in0 = 0.5f * oneMinusT1 * oneMinusC1 * onePlusB1;
        float in1 = 0.5f * oneMinusT1 * onePlusC1 * oneMinusB1;

        // Incoming tangent at second point
        Vector2 tIn = in1 * (points[i3] - points[i2]) + in0 * diff;

        // Compute polynomial terms
        _a[i1] = points[i1];
        _b[i1] = tOut;
        _c[i1] = 3f * diff - 2f * tOut - tIn;
        _d[i1] = -2f * diff + tOut + tIn;
    }

    // Compute arc length parametrization of the spline curve
    public float _ComputeArcLengthParam()
    {
        int n = numLengthSamples * NumberOfSegments;
        _s = new float[n];
        _u = new float[n];

        float s = 0;
        Vector2 v = points[0];
        for (int i = 0; i < n; ++i)
        {
            float u = ((float)i) / (n - 1) * NumberOfSegments;
            _u[i] = u;

            Vector2 v1 = Sample(u);
            s += Vector2.Distance(v, v1);
            v = v1;
            _s[i] = s;
        }

        if (s < 0.00001f)
            return 0;

        // Normalize arc-lengths
        for (int i = 0; i < n; ++i)
            _s[i] /= s;

        return s;
    }
}

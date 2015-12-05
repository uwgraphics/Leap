using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using MathNet.Numerics.LinearAlgebra;

/// <summary>
/// Class representing a uniform, quadratic, N-dimensional B-spline.
/// </summary>
public class QBSpline
{
    /// <summary>
    /// Spline control points. 
    /// </summary>
    public Vector2[] controlPoints = new Vector2[0];

    // TODO: for the glory of Sean, fix this class!
    private Matrix4x4 coeffs;

    /// <summary>
    /// Constructor. 
    /// </summary>
    public QBSpline()
    {
        coeffs[0, 0] = 0.5f;
        coeffs[0, 1] = -1;
        coeffs[0, 2] = 0.5f;
        coeffs[0, 3] = 0;
        coeffs[1, 0] = -1;
        coeffs[1, 1] = 1;
        coeffs[1, 2] = 0;
        coeffs[1, 3] = 0;
        coeffs[2, 0] = 0.5f;
        coeffs[2, 1] = 0.5f;
        coeffs[2, 2] = 1;
        coeffs[2, 3] = 0;
        coeffs[3, 0] = 0;
        coeffs[3, 1] = 0;
        coeffs[3, 2] = 0;
        coeffs[3, 3] = 1;
        coeffs = coeffs.transpose; // TODO: needs to be transposed?
    }

    /// <summary>
    /// Gets the number of spline segments. 
    /// </summary>
    /// <returns>
    /// Number of spline segments.
    /// </returns>
    public int GetNumSegments()
    {
        return controlPoints.Length - 2;
    }

    /// <summary>
    /// Gets a point on the spline.
    /// </summary>
    /// <param name="t">
    /// Interpolation parameter (in [0-1] range).
    /// </param>
    /// <returns>
    /// Interpolated point.
    /// </returns>
    public Vector2 GetPoint(float t)
    {
        int si;
        float st;

        // Compute segment index and param.
        st = t * GetNumSegments();
        si = (int)st;
        if (si >= GetNumSegments())
            si = GetNumSegments() - 1;
        st -= (float)si;

        return GetPoint(si, st);
    }

    /// <summary>
    /// Gets a point on the spline.
    /// </summary>
    /// <param name="segIndex">
    /// Spline segment index.
    /// </param>
    /// <param name="t">
    /// Interpolation parameter (in [0-1] range).
    /// </param>
    /// <returns>
    /// Interpolated point.
    /// </returns>
    public Vector2 GetPoint(int segIndex, float t)
    {
        if (segIndex >= GetNumSegments())
        {
            segIndex = GetNumSegments() - 1;
            t = 1;
        }

        Vector4 tv = new Vector4(t * t, t, 1, 0);
        tv = coeffs * tv;

        return controlPoints[segIndex] * tv.x + controlPoints[segIndex + 1] * tv.y + controlPoints[segIndex + 2] * tv.z;
    }

    /// <summary>
    /// Gets a tangent on the spline.
    /// </summary>
    /// <param name="t">
    /// Interpolation parameter (in [0-1] range).
    /// </param>
    /// <returns>
    /// Interpolated tangent.
    /// </returns>
    public Vector2 GetTangent(float t)
    {
        int si;
        float st;

        // Compute segment index and param.
        st = t * GetNumSegments();
        si = (int)st;
        if (si >= GetNumSegments())
            si = GetNumSegments() - 1;
        st -= (float)si;

        return GetTangent(si, st);
    }

    /// <summary>
    /// Gets a tangent on the spline.
    /// </summary>
    /// <param name="segIndex">
    /// Spline segment index.
    /// </param>
    /// <param name="t">
    /// Interpolation parameter (in [0-1] range).
    /// </param>
    /// <returns>
    /// Interpolated tangent.
    /// </returns>
    public Vector2 GetTangent(int segIndex, float t)
    {
        if (segIndex >= GetNumSegments())
        {
            segIndex = GetNumSegments() - 1;
            t = 1;
        }

        Vector4 tv = new Vector4(2 * t, 1, 0, 0);
        tv = coeffs * tv;

        return controlPoints[segIndex] * tv.x + controlPoints[segIndex + 1] * tv.y + controlPoints[segIndex + 2] * tv.z;
    }

    /// <summary>
    /// Fits the spline to a set of data points using
    /// a least-squares method.
    /// </summary>
    public void FitToPoints(Vector2[] data)
    {
        // Prepare list of data point diffs
        List<Vector2> ddata = new List<Vector2>();
        for (int dpti = 1; dpti < data.Length; ++dpti)
            ddata.Add(data[dpti] - data[dpti - 1]);

        // Initialize matrix of coefficients
        Matrix coeffs = new Matrix(ddata.Count, ddata.Count);
        coeffs[0, 0] = -1;
        coeffs[0, 1] = 1;
        for (int dpti = 1; dpti < coeffs.RowCount; ++dpti)
        {
            coeffs[dpti, dpti - 1] = 0.5f;
            coeffs[dpti, dpti] = 0.5f;
        }
        coeffs = coeffs.Inverse();

        // Compute control points
        controlPoints = new Vector2[ddata.Count];
        for (int cpi = 0; cpi < ddata.Count; ++cpi)
        {
            Vector2 cp = new Vector2();

            for (int dpti = 0; dpti < ddata.Count; ++dpti)
            {
                cp += ddata[dpti] * (float)coeffs[cpi, dpti];
            }

            controlPoints[cpi] = cp;
        }

        // TODO: why is the last control point incorrect?
        controlPoints[controlPoints.Length - 1] =
            controlPoints[controlPoints.Length - 1] * 2f - controlPoints[controlPoints.Length - 2];
    }
}

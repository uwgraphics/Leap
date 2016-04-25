using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Utility class containing various methods for discretized signal filtering.
/// </summary>
public static class FilterUtil
{
    /// <summary>
    /// Filter a 1D signal.
    /// </summary>
    /// <param name="inData">Input data</param>
    /// <param name="outData">Filtered data</param>
    /// <param name="kernel">Kernel</param>
    public static void Filter(float[] inData, float[] outData, float[] kernel)
    {
        if (inData.Length != outData.Length)
            throw new ArgumentException("Buffer for output data has different size from input data", "outData");

        for (int index1 = 0; index1 < inData.Length; ++index1)
        {
            float value = 0f;
            for (int kIndex = 0; kIndex < kernel.Length; ++kIndex)
            {
                int index2 = index1 + kIndex - kernel.Length / 2;
                index2 = Math.Min(Math.Max(index2, 0), inData.Length - 1);
                value += kernel[kIndex] * inData[index2];
            }

            outData[index1] = value;
        }
    }

    /// <summary>
    /// Get a 1D tent kernel.
    /// </summary>
    /// <param name="size">Kernel size</param>
    /// <returns>Kernel</returns>
    public static float[] GetTentKernel1D(int size)
    {
        if (size < 1)
            throw new ArgumentOutOfRangeException("Filter kernel must have size > 0", "size");

        int halfSize = size / 2;
        float[] kernel = new float[size];
        for (int kIndex = 0; kIndex <= halfSize; ++kIndex)
        {
            int index1 = halfSize + kIndex;
            int index2 = halfSize - kIndex;
            kernel[index1] = kernel[index2] = ((float)index2 + 1) / (halfSize + 1);
        }

        float sum = kernel.Sum();
        for (int kIndex = 0; kIndex < size; ++kIndex)
            kernel[kIndex] /= sum;

        return kernel;
    }

    /// <summary>
    /// Perform bilateral filtering of a 1D signal.
    /// </summary>
    /// <param name="inData">Input data</param>
    /// <param name="outData">Filtered data</param>
    /// <param name="size">Kernel size</param>
    /// <param name="space">Space parameter</param>
    /// <param name="range">Range parameter</param>
    public static void BilateralFilter(float[] inData, float[] outData, int size, float space, float range)
    {
        if (inData.Length != outData.Length)
            throw new ArgumentException("Buffer for output data has different size from input data", "outData");

        if (size < 1)
            throw new ArgumentOutOfRangeException("Filter kernel must have size > 0", "size");

        int halfSize = size / 2;
        for (int index1 = 0; index1 < inData.Length; ++index1)
        {
            float sum = 0f, sumw = 0f;
            for (int index2 = Mathf.Max(0, index1 - halfSize);
                index2 <= Math.Min(inData.Length - 1, index1 + halfSize); ++index2)
            {
                float w = Gaussian(Mathf.Abs(index2 - index1), space) * Gaussian(Mathf.Abs(inData[index1] - inData[index2]), range);
                sum += w * inData[index2];
                sumw += w;
            }

            outData[index1] = sum / sumw;
        }
    }

    /// <summary>
    /// Compute Gaussian function.
    /// </summary>
    /// <param name="x">x</param>
    /// <param name="sd">sigma</param>
    /// <returns>Gaussian value</returns>
    public static float Gaussian(float x, float sd)
    {
        return (1f / (sd * Mathf.Sqrt(2f * Mathf.PI))) * Mathf.Exp(-x * x / (2f * sd * sd));
    }
}

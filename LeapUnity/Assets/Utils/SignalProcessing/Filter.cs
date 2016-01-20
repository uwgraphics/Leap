﻿using System;
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
        {
            throw new ArgumentException("Buffer for output data has different size from input data", "outData");
        }

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
        if (size % 2 == 0)
            throw new ArgumentException("Filter kernel must have odd size!", "size");

        float[] kernel = new float[size];
        for (int kIndex = 0; kIndex <= size / 2; ++kIndex)
        {
            int index1 = size / 2 + kIndex;
            int index2 = size / 2 - kIndex;
            kernel[index1] = kernel[index2] = ((float)index2 + 1) / (size / 2 + 1);
        }

        float sum = kernel.Sum();
        for (int kIndex = 0; kIndex < size; ++kIndex)
            kernel[kIndex] /= sum;

        return kernel;
    }
}

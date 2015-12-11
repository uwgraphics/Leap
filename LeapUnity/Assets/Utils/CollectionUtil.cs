using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/// <summary>
/// Utility class for arrays and collections.
/// </summary>
public static class CollectionUtil
{
    /// <summary>
    /// Get a row of a two-dimensional array as a one-dimensional array.
    /// </summary>
    /// <typeparam name="T">Array element type</typeparam>
    /// <param name="array">Array</param>
    /// <param name="index">Row index</param>
    /// <returns>Row</returns>
    public static T[] GetRow<T>(T[,] array, int index)
    {
        int numCols = array.GetLength(1);
        int byteSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(T));

        T[] row = new T[numCols];
        System.Buffer.BlockCopy(array, index * numCols * byteSize, row, 0, numCols * byteSize);
        return row;
    }

    /// <summary>
    /// Set a row of a two-dimensional array from a one-dimensional array.
    /// </summary>
    /// <typeparam name="T">Array element type</typeparam>
    /// <param name="array">Array</param>
    /// <param name="index">Row index</param>
    /// <param name="row">Row</param>
    public static void SetRow<T>(T[,] array, int index, T[] row)
    {
        int numCols = array.GetLength(1);
        int byteSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(T));

        System.Buffer.BlockCopy(row, 0, array, index * numCols * byteSize, numCols * byteSize);
    }
}


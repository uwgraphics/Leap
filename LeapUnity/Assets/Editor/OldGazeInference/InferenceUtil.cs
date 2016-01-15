using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;

public static class InferenceUtil {
    /// <summary>
    /// Find closest value back in a list.  Useful for finding a previous minimum/ maximum frame.
    /// </summary>
    /// <param name="v"></param>
    /// <param name="list"></param>
    /// <returns></returns>
    public static int findClosestValueBack(int v, List<int> list)
    {
        if (v < list[0]) return v;
        if (list.Count == 1) return list[0];

        int center = list.Count / 2;
        if (list[center] > v) return findClosestValueBack(v, list.GetRange(0, center));
        else return findClosestValueBack(v, list.GetRange(center, list.Count - center));
    }

    /// <summary>
    /// Find closest value forward in a list.  Useful for finding a subsequent minimum/ maximum frame.
    /// </summary>
    /// <param name="v"></param>
    /// <param name="list"></param>
    /// <returns></returns>
    public static int findClosestValueForward(int v, List<int> list)
    {
        if (v > list[list.Count - 1]) return v;
        if (list.Count == 1) return list[0];
        if (list.Count == 2) return list[0] > v ? list[0] : list[1];

        int center = list.Count / 2;
        if (v > list[center]) return findClosestValueForward(v, list.GetRange(center + 1, list.Count - center - 1));
        else return findClosestValueForward(v, list.GetRange(0, center + 1));
    }



}

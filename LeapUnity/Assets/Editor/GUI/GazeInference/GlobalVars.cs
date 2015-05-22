using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Linq;

public static class GlobalVars {
    public static bool work = true;
    public static string MatlabPath = work ? @"C:\Local Users\Leap\Matlab\" : @"E:\CS699-Gleicher\Leap_\Matlab\";
    public static string CurrAnimation = "StealDiamond";
    public static string CurrCharacter = "Norman";
    //index 0: local 
    //      1: global
    //      2: root
    //      3: headlocal
    public static int OrientationIndex = 2;
    // Update 05/1/15 : most recent iteration is 0
    // Update 05/2/15 : most recent iteration is 1
    // Update 05/11/15 : most recent iteration is 2
    // Update 05/12/15 : most recent iteration is 3
    // Update 05/18/15 : most recent iteration is 4
    // Update 05/19/15 : iteration is 5 (subGazeInference reorganization)
    // Update 05/20/15 : iteration is 6 (Just using block matching)
    // Update 05/21/15 :iteration is 7 (test timeline gluing) //NOT USING
    public static int InferenceIteration = 6;
}

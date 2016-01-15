using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;



public static class FileIO {
    //output angle csv file using euler angles
    public static void AngleCSV(string filePath, List<Vector3> angles) {
        using (System.IO.StreamWriter file = new System.IO.StreamWriter(filePath))
        {
            file.WriteLine("x,y,z");
            for (int i = 0; i < angles.Count; i++)
            {
                var vec3 = angles[i];
                file.WriteLine(vec3.x + "," + vec3.y + "," + vec3.z);
            }
        }
    }

    //alternate csv util file that converts quaternions to euler angles first
    public static void AngleCSV(string filePath, List<Quaternion> orientations) {
        var angles = new List<Vector3>();
        foreach (var o in orientations) {
            angles.Add(o.eulerAngles);
        }
        AngleCSV(filePath, angles);
    }

    public static void AngularVelocityCSV(string filePath, AngleData ad, bool local) {
        using (System.IO.StreamWriter file = new System.IO.StreamWriter(filePath)) {
            file.WriteLine(ad.AnimationName);
            if (local)
            {
                for (int i = 0; i < ad.Head_Magnitudes_Local.Count; i++)
                {
                    file.WriteLine(ad.Head_Magnitudes_Local[i] + "," + ad.Chest_Magnitudes_Local[i] +
                        "," + ad.SpineA_Magnitudes_Local[i] + "," + ad.SpineB_Magnitudes_Local[i] +
                        "," + ad.Hips_Magnitudes_Local[i]);
                }
            }
            else {
                for (int i = 0; i < ad.Head_Magnitudes_Global.Count; i++)
                {
                    file.WriteLine(ad.Head_Magnitudes_Global[i] + "," + ad.Chest_Magnitudes_Global[i] +
                        "," + ad.SpineA_Magnitudes_Global[i] + "," + ad.SpineB_Magnitudes_Global[i] +
                        "," + ad.Hips_Magnitudes_Global[i]);
                } 
            }
        }
    }
    
    //filters the data
    public static void AngularVelocityCSV(string filePath, AngleData ad, bool local, int kernelSize, double space, double range)
    {
        using (System.IO.StreamWriter file = new System.IO.StreamWriter(filePath))
        {
            file.WriteLine(ad.AnimationName);

            if (local)
            {
                var head = Filter.BilateralFilter(ad.Head_Magnitudes_Local, kernelSize, space, range);
                var chest = Filter.BilateralFilter(ad.Chest_Magnitudes_Local, kernelSize, space, range);
                var spineA = Filter.BilateralFilter(ad.SpineA_Magnitudes_Local, kernelSize, space, range);
                var spineB = Filter.BilateralFilter(ad.SpineB_Magnitudes_Local, kernelSize, space, range);
                var hips = Filter.BilateralFilter(ad.Hips_Magnitudes_Local, kernelSize, space, range);


                for (int i = 0; i < ad.Head_Magnitudes_Local.Count; i++)
                {
                    file.WriteLine(head[i] + "," + chest[i] +
                        "," + spineA[i] + "," + spineB[i] +
                        "," + hips[i]);
                }
            }
            else
            {
                var head = Filter.BilateralFilter(ad.Head_Magnitudes_Global, kernelSize, space, range);
                var chest = Filter.BilateralFilter(ad.Chest_Magnitudes_Global, kernelSize, space, range);
                var spineA = Filter.BilateralFilter(ad.SpineA_Magnitudes_Global, kernelSize, space, range);
                var spineB = Filter.BilateralFilter(ad.SpineB_Magnitudes_Global, kernelSize, space, range);
                var hips = Filter.BilateralFilter(ad.Hips_Magnitudes_Global, kernelSize, space, range);

                for (int i = 0; i < ad.Head_Magnitudes_Global.Count; i++)
                {
                    file.WriteLine(head[i] + "," + chest[i] +
                        "," + spineA[i] + "," + spineB[i] +
                        "," + hips[i]);
                }
            }
        }
    }

    //for the addition of root space and head-local space
    //index 0: local 
    //      1: global
    //      2: root
    //      3: headlocal
    public static void AngularVelocityCSV(string filePath, AngleData ad, int index)
    {
        using (System.IO.StreamWriter file = new System.IO.StreamWriter(filePath))
        {
            file.WriteLine(ad.AnimationName);
            if (index == 0)
            {
                for (int i = 0; i < ad.Head_Magnitudes_Local.Count; i++)
                {
                    file.WriteLine(ad.Head_Magnitudes_Local[i] + "," + ad.Chest_Magnitudes_Local[i] +
                        "," + ad.SpineA_Magnitudes_Local[i] + "," + ad.SpineB_Magnitudes_Local[i] +
                        "," + ad.Hips_Magnitudes_Local[i]);
                }
            }
            else if(index == 1)
            {
                for (int i = 0; i < ad.Head_Magnitudes_Global.Count; i++)
                {
                    file.WriteLine(ad.Head_Magnitudes_Global[i] + "," + ad.Chest_Magnitudes_Global[i] +
                        "," + ad.SpineA_Magnitudes_Global[i] + "," + ad.SpineB_Magnitudes_Global[i] +
                        "," + ad.Hips_Magnitudes_Global[i]);
                }
            }
            else if (index == 2) {
                for (int i = 0; i < ad.Head_Magnitudes_Root.Count; i++) {
                    file.WriteLine(ad.Head_Magnitudes_Root[i] + "," + ad.Chest_Magnitudes_Root[i] +
                        "," + ad.SpineA_Magnitudes_Root[i] + "," + ad.SpineB_Magnitudes_Root[i]);
                }
            }
            else if (index == 3) {
                for (int i = 0; i < ad.Head_Magnitudes_HeadL.Count; i++) {
                    file.WriteLine(ad.Head_Magnitudes_HeadL[i]);
                }
            }
        }
    }

    //index 0: local 
    //      1: global
    //      2: root
    //      3: headlocal
    public static void AngularVelocityCSV(string filePath, AngleData ad, int index, int kernelSize, double space, double range)
    {
        using (System.IO.StreamWriter file = new System.IO.StreamWriter(filePath))
        {
            file.WriteLine(ad.AnimationName);

            if (index == 0)
            {
                var head = Filter.BilateralFilter(ad.Head_Magnitudes_Local, kernelSize, space, range);
                var chest = Filter.BilateralFilter(ad.Chest_Magnitudes_Local, kernelSize, space, range);
                var spineA = Filter.BilateralFilter(ad.SpineA_Magnitudes_Local, kernelSize, space, range);
                var spineB = Filter.BilateralFilter(ad.SpineB_Magnitudes_Local, kernelSize, space, range);
                var hips = Filter.BilateralFilter(ad.Hips_Magnitudes_Local, kernelSize, space, range);


                for (int i = 0; i < ad.Head_Magnitudes_Local.Count; i++)
                {
                    file.WriteLine(head[i] + "," + chest[i] +
                        "," + spineA[i] + "," + spineB[i] +
                        "," + hips[i]);
                }
            }
            else if(index == 1)
            {
                var head = Filter.BilateralFilter(ad.Head_Magnitudes_Global, kernelSize, space, range);
                var chest = Filter.BilateralFilter(ad.Chest_Magnitudes_Global, kernelSize, space, range);
                var spineA = Filter.BilateralFilter(ad.SpineA_Magnitudes_Global, kernelSize, space, range);
                var spineB = Filter.BilateralFilter(ad.SpineB_Magnitudes_Global, kernelSize, space, range);
                var hips = Filter.BilateralFilter(ad.Hips_Magnitudes_Global, kernelSize, space, range);

                for (int i = 0; i < ad.Head_Magnitudes_Global.Count; i++)
                {
                    file.WriteLine(head[i] + "," + chest[i] +
                        "," + spineA[i] + "," + spineB[i] +
                        "," + hips[i]);
                }
            }
            else if (index == 2) {

                var head = Filter.BilateralFilter(ad.Head_Magnitudes_Root, kernelSize, space, range);
                var chest = Filter.BilateralFilter(ad.Chest_Magnitudes_Root, kernelSize, space, range);
                var spineA = Filter.BilateralFilter(ad.SpineA_Magnitudes_Root, kernelSize, space, range);
                var spineB = Filter.BilateralFilter(ad.SpineB_Magnitudes_Root, kernelSize, space, range);

                for (int i = 0; i < ad.Head_Magnitudes_Root.Count; i++)
                {
                    file.WriteLine(head[i] + "," + chest[i] +
                        "," + spineA[i] + "," + spineB[i]);
                }
            }
            else if (index == 3) {
                var head = Filter.BilateralFilter(ad.Head_Magnitudes_HeadL, kernelSize, space, range);

                for (int i = 0; i < ad.Head_Magnitudes_HeadL.Count; i++)
                {
                    file.WriteLine(head[i]);
                }
            }
        }
    }
}

using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;



public class AngleData {
    //access to the animationClipInstance
    public AnimationClipInstance AnimationClip
    {
        get;
        protected set;
    }

    //access to the inferenceCharacter
    public InferenceCharacter Character
    {
        get;
        set;
    }

    public List<Quaternion> Head_Orientations_Local
    {
        get;
        set;
    }
    public List<Quaternion> Head_Orientations_Global
    {
        get;
        set;
    }
    public List<Quaternion> Chest_Orientations_Local
    {
        get;
        set;
    }
    public List<Quaternion> Chest_Orientations_Global
    {
        get;
        set;
    }
    public List<Quaternion> SpineA_Orientations_Local
    {
        get;
        set;
    }
    public List<Quaternion> SpineA_Orientations_Global
    {
        get;
        set;
    }
    public List<Quaternion> SpineB_Orientations_Local
    {
        get;
        set;
    }
    public List<Quaternion> SpineB_Orientations_Global
    {
        get;
        set;
    }
    public List<Quaternion> Hips_Orientations_Local
    {
        get;
        set;
    }
    public List<Quaternion> Hips_Orientations_Global
    {
        get;
        set;
    }
    //root space
    public List<Quaternion> Head_Orientations_Root
    {
        get;
        set;
    }
    public List<Quaternion> Chest_Orientations_Root
    {
        get;
        set;
    }
    public List<Quaternion> SpineA_Orientations_Root
    {
        get;
        set;
    }
    public List<Quaternion> SpineB_Orientations_Root
    {
        get;
        set;
    }
    //head local space (with respect to chest 5/12/15)
    public List<Quaternion> Head_Orientations_HeadL
    {
        get;
        set;
    }

    //Rotation Vectors
    public List<Vector3> Head_Rotations_Local
    {
        get;
        set;
    }
    public List<Vector3> Head_Rotations_Global
    {
        get;
        set;
    }
    public List<Vector3> Chest_Rotations_Local
    {
        get;
        set;
    }
    public List<Vector3> Chest_Rotations_Global
    {
        get;
        set;
    }
    public List<Vector3> SpineA_Rotations_Local
    {
        get;
        set;
    }
    public List<Vector3> SpineA_Rotations_Global
    {
        get;
        set;
    }
    public List<Vector3> SpineB_Rotations_Local
    {
        get;
        set;
    }
    public List<Vector3> SpineB_Rotations_Global
    {
        get;
        set;
    }
    public List<Vector3> Hips_Rotations_Local
    {
        get;
        set;
    }
    public List<Vector3> Hips_Rotations_Global
    {
        get;
        set;
    }
    //root space
    public List<Vector3> Head_Rotations_Root
    {
        get;
        set;
    }
    public List<Vector3> Chest_Rotations_Root
    {
        get;
        set;
    }
    public List<Vector3> SpineA_Rotations_Root
    {
        get;
        set;
    }
    public List<Vector3> SpineB_Rotations_Root
    {
        get;
        set;
    }
    //head local space
    public List<Vector3> Head_Rotations_HeadL
    {
        get;
        set;
    }

    //Magnitudes
    public List<double> Head_Magnitudes_Local
    {
        get;
        set;
    }
    public List<double> Head_Magnitudes_Global
    {
        get;
        set;
    }
    public List<double> Chest_Magnitudes_Local
    {
        get;
        set;
    }
    public List<double> Chest_Magnitudes_Global
    {
        get;
        set;
    }
    public List<double> SpineA_Magnitudes_Local
    {
        get;
        set;
    }
    public List<double> SpineA_Magnitudes_Global
    {
        get;
        set;
    }
    public List<double> SpineB_Magnitudes_Local
    {
        get;
        set;
    }
    public List<double> SpineB_Magnitudes_Global
    {
        get;
        set;
    }
    public List<double> Hips_Magnitudes_Local
    {
        get;
        set;
    }
    public List<double> Hips_Magnitudes_Global
    {
        get;
        set;
    }
    //root space
    public List<double> Head_Magnitudes_Root
    {
        get;
        set;
    }
    public List<double> Chest_Magnitudes_Root
    {
        get;
        set;
    }
    public List<double> SpineA_Magnitudes_Root
    {
        get;
        set;
    }
    public List<double> SpineB_Magnitudes_Root
    {
        get;
        set;
    }
    //head local space
    public List<double> Head_Magnitudes_HeadL
    {
        get;
        set;
    }

    //animationName
    public string AnimationName
    {
        get;
        set;
    }

    public AngleData(string animationName, string characterName)
    {
        _init();
        AnimationName = animationName;

        Character = new InferenceCharacter(characterName);
        AnimationClip = new AnimationClipInstance(animationName, Character.CharModel, true, false, false);

        int frame;
        for (int i = 1; i < AnimationClip.FrameLength; i++)
        {
            frame = i;
            AnimationClip.Animation[animationName].normalizedTime = Mathf.Clamp01(((float)frame) / AnimationClip.FrameLength);
            AnimationClip.Animation[animationName].weight = AnimationClip.Weight;
            AnimationClip.Animation[animationName].enabled = true;

            AnimationClip.Animation.Sample();
            ////////////////////////////////////////////////////////////
            Head_Orientations_Local.Add (Character.HeadBone.localRotation);
            Head_Orientations_Global.Add(Character.HeadBone.rotation);
            Chest_Orientations_Local.Add (Character.ChestBone.localRotation);
            Chest_Orientations_Global.Add(Character.ChestBone.rotation);
            SpineA_Orientations_Local.Add (Character.SpineABone.localRotation);
            SpineA_Orientations_Global.Add(Character.SpineABone.rotation);
            SpineB_Orientations_Local.Add (Character.SpineBBone.localRotation);
            SpineB_Orientations_Global.Add(Character.SpineBBone.rotation);
            Hips_Orientations_Local.Add(Character.HipBone.localRotation);
            Hips_Orientations_Global.Add(Character.HipBone.rotation);
            //root space
            Head_Orientations_Root.Add(QuaternionUtil.DispQ(Character.HipBone.rotation, Character.InferenceBones[0].rotation));
            Chest_Orientations_Root.Add(QuaternionUtil.DispQ(Character.HipBone.rotation, Character.InferenceBones[1].rotation));
            SpineA_Orientations_Root.Add(QuaternionUtil.DispQ(Character.HipBone.rotation, Character.InferenceBones[2].rotation));
            SpineB_Orientations_Root.Add(QuaternionUtil.DispQ(Character.HipBone.rotation, Character.InferenceBones[3].rotation));
            //head local space
            Head_Orientations_HeadL.Add(QuaternionUtil.DispQ(Character.ChestBone.rotation, Character.HeadBone.rotation));
            /////////////////////////////////////////////////////////////////
            AnimationClip.Animation[animationName].enabled = false;
        }

        

        //set rotation vectors
        Head_Rotations_Local  = new List<Vector3>(QuaternionUtil.QuatsToRotVecs(Head_Orientations_Local.ToArray()));
        Head_Rotations_Global = new List<Vector3>(QuaternionUtil.QuatsToRotVecs(Head_Orientations_Global.ToArray()));
        Chest_Rotations_Local = new List<Vector3>(QuaternionUtil.QuatsToRotVecs(Chest_Orientations_Local.ToArray()));
        Chest_Rotations_Global = new List<Vector3>(QuaternionUtil.QuatsToRotVecs(Chest_Orientations_Global.ToArray()));
        SpineA_Rotations_Local = new List<Vector3>(QuaternionUtil.QuatsToRotVecs(SpineA_Orientations_Local.ToArray()));
        SpineA_Rotations_Global = new List<Vector3>(QuaternionUtil.QuatsToRotVecs(SpineA_Orientations_Global.ToArray()));
        SpineB_Rotations_Local = new List<Vector3>(QuaternionUtil.QuatsToRotVecs(SpineB_Orientations_Local.ToArray()));
        SpineB_Rotations_Global = new List<Vector3>(QuaternionUtil.QuatsToRotVecs(SpineB_Orientations_Global.ToArray()));
        Hips_Rotations_Local = new List<Vector3>(QuaternionUtil.QuatsToRotVecs(Hips_Orientations_Local.ToArray()));
        Hips_Rotations_Global = new List<Vector3>(QuaternionUtil.QuatsToRotVecs(Hips_Orientations_Global.ToArray()));
            //root space
        Head_Rotations_Root = new List<Vector3>(QuaternionUtil.QuatsToRotVecs(Head_Orientations_Root.ToArray()));
        Chest_Rotations_Root = new List<Vector3>(QuaternionUtil.QuatsToRotVecs(Chest_Orientations_Root.ToArray()));
        SpineA_Rotations_Root = new List<Vector3>(QuaternionUtil.QuatsToRotVecs(SpineA_Orientations_Root.ToArray()));
        SpineB_Rotations_Root = new List<Vector3>(QuaternionUtil.QuatsToRotVecs(SpineB_Orientations_Root.ToArray()));
            //head local space
        Head_Rotations_HeadL = new List<Vector3>(QuaternionUtil.QuatsToRotVecs(Head_Orientations_HeadL.ToArray()));

        //set magnitudes
        for (int i = 0; i < Head_Rotations_Local.Count; i++) {
            Head_Magnitudes_Local.Add(Head_Rotations_Local[i].magnitude);
            Head_Magnitudes_Global.Add(Head_Rotations_Global[i].magnitude);
            Chest_Magnitudes_Local.Add(Chest_Rotations_Local[i].magnitude);
            Chest_Magnitudes_Global.Add(Chest_Rotations_Global[i].magnitude);
            SpineA_Magnitudes_Local.Add(SpineA_Rotations_Local[i].magnitude);
            SpineA_Magnitudes_Global.Add(SpineA_Rotations_Global[i].magnitude);
            SpineB_Magnitudes_Local.Add(SpineB_Rotations_Local[i].magnitude);
            SpineB_Magnitudes_Global.Add(SpineB_Rotations_Global[i].magnitude);
            Hips_Magnitudes_Local.Add(Hips_Rotations_Local[i].magnitude);
            Hips_Magnitudes_Global.Add(Hips_Rotations_Global[i].magnitude);
                //root space
            Head_Magnitudes_Root.Add(Head_Rotations_Root[i].magnitude);
            Chest_Magnitudes_Root.Add(Chest_Rotations_Root[i].magnitude);
            SpineA_Magnitudes_Root.Add(SpineA_Rotations_Root[i].magnitude);
            SpineB_Magnitudes_Root.Add(SpineB_Rotations_Root[i].magnitude);
                //local head space
            Head_Magnitudes_HeadL.Add(Head_Rotations_HeadL[i].magnitude);
        }

        
    }

    
    private void _init() {
        Head_Orientations_Global = new List<Quaternion>();
        Head_Orientations_Local = new List<Quaternion>();
        Chest_Orientations_Global = new List<Quaternion>();
        Chest_Orientations_Local = new List<Quaternion>();
        SpineA_Orientations_Global = new List<Quaternion>();
        SpineA_Orientations_Local = new List<Quaternion>();
        SpineB_Orientations_Global = new List<Quaternion>();
        SpineB_Orientations_Local = new List<Quaternion>();
        Hips_Orientations_Global = new List<Quaternion>();
        Hips_Orientations_Local = new List<Quaternion>();
        Head_Orientations_Root = new List<Quaternion>();
        Chest_Orientations_Root = new List<Quaternion>();
        SpineA_Orientations_Root = new List<Quaternion>();
        SpineB_Orientations_Root = new List<Quaternion>();
        Head_Orientations_HeadL = new List<Quaternion>();
        


        Head_Rotations_Global =   new List<Vector3>();
        Head_Rotations_Local =    new List<Vector3>();
        Chest_Rotations_Global =  new List<Vector3>();
        Chest_Rotations_Local =   new List<Vector3>();
        SpineA_Rotations_Global = new List<Vector3>();
        SpineA_Rotations_Local =  new List<Vector3>();
        SpineB_Rotations_Global = new List<Vector3>();
        SpineB_Rotations_Local =  new List<Vector3>();
        Hips_Rotations_Global =   new List<Vector3>();
        Hips_Rotations_Local =    new List<Vector3>();
        Head_Rotations_Root = new   List<Vector3>();
        Chest_Rotations_Root = new  List<Vector3>();
        SpineA_Rotations_Root = new List<Vector3>();
        SpineB_Rotations_Root = new List<Vector3>();
        Head_Rotations_HeadL = new  List<Vector3>();

        Head_Magnitudes_Global =   new List<double>();
        Head_Magnitudes_Local =    new List<double>();
        Chest_Magnitudes_Global =  new List<double>();
        Chest_Magnitudes_Local =   new List<double>();
        SpineA_Magnitudes_Global = new List<double>();
        SpineA_Magnitudes_Local =  new List<double>();
        SpineB_Magnitudes_Global = new List<double>();
        SpineB_Magnitudes_Local =  new List<double>();
        Hips_Magnitudes_Global =   new List<double>();
        Hips_Magnitudes_Local =    new List<double>();
        Head_Magnitudes_Root = new List<double>();
        Chest_Magnitudes_Root = new List<double>();
        SpineA_Magnitudes_Root = new List<double>();
        SpineB_Magnitudes_Root = new List<double>();
        Head_Magnitudes_HeadL = new List<double>();


    }

}


/// <summary>
/// holds utility functions for finding minima and maxima of a list of data
/// </summary>
public static class MinMax {
    public static List<int> Minimize(List<double> magnitudes) {
        var minima = new List<int>();

        for (int i = 4; i < magnitudes.Count - 4; i++) {
            if (magnitudes[i - 1] > magnitudes[i] &&
                magnitudes[i + 1] > magnitudes[i] &&
                magnitudes[i - 2] > magnitudes[i - 1] &&
                magnitudes[i + 2] > magnitudes[i + 1] &&
                magnitudes[i - 3] > magnitudes[i - 2] &&
                magnitudes[i + 3] > magnitudes[i + 2] &&
                magnitudes[i - 4] > magnitudes[i - 3] &&
                magnitudes[i + 4] > magnitudes[i + 3]) {
                    minima.Add(i + 1);
            }
        }
        return minima;
    }

    public static List<int> Minimize(List<double> magnitudes, double threshold) {
        var minima = new List<int>();
        var derivatives = Derivative.Derive11(magnitudes);
        for (int i = 2; i < magnitudes.Count - 2; i++) {
            if ( Math.Abs(derivatives[i]) < threshold &&
                magnitudes[i - 1] > magnitudes[i] &&
                magnitudes[i + 1] > magnitudes[i] &&
                magnitudes[i - 2] > magnitudes[i-1] &&
                magnitudes[i + 2] > magnitudes[i + 1])
            {
                minima.Add(i+1);
            }
        }

        return minima;
    }

    public static List<int> Minimize(List<Vector3> velocities, double threshold)
    {
        var minima = new List<int>();
        var magnitudes = new List<double>();
        for (int i = 0; i < velocities.Count; i++) {
            magnitudes.Add(velocities[i].magnitude);
        }
        minima = Minimize(magnitudes, threshold);
        return minima;
    }

    public static List<int> Maximize(List<double> magnitudes) {
        var maxima = new List<int>();

        for (int i = 4; i < magnitudes.Count - 4; i++)
        {
            if (magnitudes[i - 1] < magnitudes[i] &&
                magnitudes[i + 1] < magnitudes[i] &&
                magnitudes[i - 2] < magnitudes[i - 1] &&
                magnitudes[i + 2] < magnitudes[i + 1] &&
                magnitudes[i - 3] < magnitudes[i - 2] &&
                magnitudes[i + 3] < magnitudes[i + 2] &&
                magnitudes[i - 4] < magnitudes[i - 3] &&
                magnitudes[i + 4] < magnitudes[i + 3])
            {
                maxima.Add(i + 1);
            }
        }

        return maxima;
    }
   
    public static List<int> Maximize(List<double> magnitudes, double threshold)
    {
        var maxima = new List<int>();
        var derivatives = Derivative.Derive11(magnitudes);
        for (int i = 2; i < magnitudes.Count - 2; i++)
        {
            if ( Math.Abs(derivatives[i]) < threshold &&
                magnitudes[i - 1] < magnitudes[i] &&
                magnitudes[i + 1] < magnitudes[i] &&
                magnitudes[i - 2] < magnitudes[i - 1] &&
                magnitudes[i - 2] < magnitudes[i - 2])
            {
                maxima.Add(1);
            }
            else
            {
                maxima.Add(0);
            }
        }

        return maxima;
    }

    public static List<int> Maximize(List<Vector3> velocities, double threshold)
    {
        var maxima = new List<int>();
        var magnitudes = new List<double>();
        for (int i = 0; i < velocities.Count; i++)
        {
            magnitudes.Add(velocities[i].magnitude);
        }
        maxima = Maximize(magnitudes, threshold);
        return maxima;
    }

}

/// <summary>
/// utility functions for filtering data
/// </summary>
public static class Filter {
    public static List<double> FilterList(List<double> data, double[] kernel) {
        int kernelCenter = (int) (Math.Floor( kernel.Length / (double)2.0 ));
        var filtered = new List<double>();
        double sum = 0.0;
        for (int i = 0; i < data.Count; i++) {
            //add the center element right away
            sum += kernel[kernelCenter] * data[i];
            for (int j = 1; j <= kernelCenter; j++ )
            {
                sum += (i - j < 0 ? 0 : data[i - j]) * kernel[kernelCenter + j];
                sum += (i + j >= data.Count) ? 0 : data[i + j] * kernel[kernelCenter - j];
            }
            filtered.Add(sum);
            sum = 0.0;
        }
        return filtered;
    }

    //simple filter function that uses [0.25, 0.5, 0.25] as kernel
    public static List<double> SimpleLowPass(List<double> data) {
        return FilterList(data, new double[] { 0.25, 0.5, 0.25 });
    }

    //Takes in animation curves and places adjusts for jumps in euler angle space
    //i.e. when displaying a curve, we will not want to see a big jump between 
    //5 degrees and 355 degrees
    public static List<Vector3> DisplayCurveCollapse(List<Vector3> curves)
    {
        if (curves.Count == 0)
        {
            throw new ArgumentException("Invalid argument in DisplayCurveCollapse function");
        }

        var collapsedCurves = new List<Vector3>();
        collapsedCurves.Add(curves[0]);

        for (int i = 1; i < curves.Count; i++)
        {
            var tempVec = new Vector3();
            tempVec.x = Math.Abs(curves[i].x - collapsedCurves[i - 1].x) > 180 ? curves[i].x - 360 : curves[i].x;
            tempVec.y = Math.Abs(curves[i].y - collapsedCurves[i - 1].y) > 180 ? curves[i].y - 360 : curves[i].y;
            tempVec.z = Math.Abs(curves[i].z - collapsedCurves[i - 1].z) > 180 ? curves[i].z - 360 : curves[i].z;
            collapsedCurves.Add(tempVec);
        }

        return collapsedCurves;
    }

    public static List<Vector3> DisplayCurveCollapse(List<Quaternion> curves)
    {
        var vec3Curves = new List<Vector3>();
        for (int i = 0; i < curves.Count; i++)
        {
            vec3Curves.Add(curves[i].eulerAngles);
        }

        return DisplayCurveCollapse(vec3Curves);
    }

    //Gaussian function
    //Tested, getting same results as Matlab function
    public static double Gaussian(double sd, double x) {
        return (1 / (sd * Math.Sqrt(2 * Math.PI))) * Math.Exp(-(Math.Pow(x, 2)) / (2 * Math.Pow(sd, 2)));
    }

    //bilateral filter function
    //for now, I am trying to convert to rotation vectors first, filtering those, then converting back
    //to quaternions.  Can try to use Lee's computation later...
    public static List<Quaternion> BilateralFilter(List<Quaternion> orientations, int kernelSize, double space, double range) {
        if (orientations == null || orientations.Count == 0) {
            throw new ArgumentException("Error in BilateralFilter function! Argument provided is a list of 0 elements!");
        } 

        //convert them all to rotation vectors corresponding to the displacement quaternions
        var rotVecs = new List<Vector3>(QuaternionUtil.QuatsToRotVecs(orientations.ToArray(), QuaternionUtil.Log(orientations[0])));
      
        //manually finish rotVecs?
        rotVecs.Add(new Vector3(0, 0, 0));

        var rotVecsFiltered = new List<Vector3>();
        //use bilateral filter numerator and denominator
        for (int i = 0; i < rotVecs.Count; i++) {
            rotVecsFiltered.Add(bilateralSum(i, rotVecs, kernelSize, space, range));
        }

        Quaternion q0 = orientations[0];

        return new List<Quaternion>(QuaternionUtil.RotVecsToQuats(rotVecsFiltered.ToArray(), q0));
    }

    //bilateral filter function for just a List of doubles.  Useful for angular velocities
    public static List<double> BilateralFilter(List<double> signal, int kernelSize, double space, double range) {
        if (signal == null) throw new ArgumentException("Illegal argument in BilateralFilter function!");

        var filtered = new List<double>();
        for (int i = 0; i < signal.Count; i++) {
            filtered.Add(bilateralSum(i, signal, kernelSize, space, range));
        }

        return filtered;
    }

    //helper function for bilateral filter
    private static Vector3 bilateralSum(int curr, List<Vector3> rotVec, int kernelSize, double space, double range) {
        if (rotVec.Count == 0) {
            throw new ArgumentException("Error in BilateralSum function! Argument provided is a list of 0 elements!");
        }
        double sumX_n = 0.0, sumY_n = 0.0, sumZ_n = 0.0, sumX_d = 0.0, sumY_d = 0.0, sumZ_d = 0.0;
        int kernelHalf = kernelSize / 2; // will give how far to go on either side of the value


        for(int i = curr - kernelHalf; i <= curr + kernelHalf; i++) {
            try{
                
                if( i < 0 || i >= rotVec.Count ) continue;

                sumX_n += Gaussian(space, Math.Abs(curr - i)) * Gaussian(range, Math.Abs(rotVec[curr].x - rotVec[i].x)) * rotVec[i].x;
                sumY_n += Gaussian(space, Math.Abs(curr - i)) * Gaussian(range, Math.Abs(rotVec[curr].y - rotVec[i].y)) * rotVec[i].y;
                sumZ_n += Gaussian(space, Math.Abs(curr - i)) * Gaussian(range, Math.Abs(rotVec[curr].z - rotVec[i].z)) * rotVec[i].z;
                                                                                                                 
                sumX_d += Gaussian(space, Math.Abs(curr - i)) * Gaussian(range, Math.Abs(rotVec[curr].x - rotVec[i].x));
                sumY_d += Gaussian(space, Math.Abs(curr - i)) * Gaussian(range, Math.Abs(rotVec[curr].y - rotVec[i].y));
                sumZ_d += Gaussian(space, Math.Abs(curr - i)) * Gaussian(range, Math.Abs(rotVec[curr].z - rotVec[i].z));
                

            } catch(ArgumentOutOfRangeException e) {
                UnityEngine.Debug.Log("Error in the bilateralSum function!  Kernel went out of bounds!");
            }
        }
        //if (sumX_d == 0) UnityEngine.Debug.Log("SumX_d " + curr);
        //if (sumX_n == 0) UnityEngine.Debug.Log("SumX_n " + curr);
        //if (sumY_d == 0) UnityEngine.Debug.Log("SumY_d " + curr);
        //if (sumY_n == 0) UnityEngine.Debug.Log("SumY_n " + curr);
        //if (sumZ_d == 0) UnityEngine.Debug.Log("SumZ_d " + curr);
        //if (sumZ_n == 0) UnityEngine.Debug.Log("SumZ_n " + curr);
        

        //if (sumX_d == 0) sumX_d = 0.0001f;
        //if (sumY_d == 0) sumY_d = 0.0001f;
        //if (sumZ_d == 0) sumZ_d = 0.0001f;

        return new Vector3((float)(sumX_n / sumX_d), (float)(sumY_n / sumY_d), (float)(sumZ_n / sumZ_d)); 
    }

    private static double bilateralSum(int curr, List<double> signal, int kernelSize, double space, double range) {
        double sum_n = 0.0, sum_d = 0.0;
        int kernelHalf = kernelSize / 2; // will give how far to go on either side of the value


        for (int i = curr - kernelHalf; i <= curr + kernelHalf; i++)
        {
            try
            {

                if (i < 0 || i >= signal.Count) continue;

                sum_n += Gaussian(space, Math.Abs(curr - i)) * Gaussian(range, Math.Abs(signal[curr] - signal[i])) * signal[i];
                sum_d += Gaussian(space, Math.Abs(curr - i)) * Gaussian(range, Math.Abs(signal[curr] - signal[i]));
            }
            catch (ArgumentOutOfRangeException e)
            {
                UnityEngine.Debug.Log("Error in the bilateralSum function!  Kernel went out of bounds!");
            }
        }

        return (double)(sum_n / sum_d); 
    }

    //median filter
    public static List<Vector3> MedianFilter(List<Vector3> vecs) {
        if (vecs == null || vecs.Count == 0) {
            throw new ArgumentException("Error in MedianFilter Function!  Invalid argument!");
        }

        var filtered = new List<Vector3>();
        //kickstart list manually
        filtered.Add( new Vector3( Median(vecs[0].x, vecs[0].x, vecs[1].x), 
                                   Median(vecs[0].y, vecs[0].y, vecs[1].y),
                                   Median(vecs[0].z, vecs[0].z, vecs[1].z) ));
        for (int i = 1; i < vecs.Count - 1; i++) {
            filtered.Add(new Vector3(Median(vecs[i - 1].x, vecs[i].x, vecs[i + 1].x),
                Median(vecs[i - 1].y, vecs[i].y, vecs[i + 1].y),
                Median(vecs[i - 1].z, vecs[i].z, vecs[i + 1].z)));
        }
        //finish list manually
        filtered.Add(new Vector3(Median(vecs[vecs.Count - 2].x, vecs[vecs.Count - 1].x, vecs[vecs.Count - 1].x),
                                 Median(vecs[vecs.Count - 2].y, vecs[vecs.Count - 1].y, vecs[vecs.Count - 1].y),
                                 Median(vecs[vecs.Count - 2].z, vecs[vecs.Count - 1].z, vecs[vecs.Count - 1].z)));

        return filtered;
        
    }

    //median
    public static float Median(float a, float b, float c) {
        if ((a >= b && a <= c) || (a >= c && a <= b)) return a;
        else if ((b >= a && b <= c) || (b >= c && b <= a)) return b;
        else return c;
    }
}

public static class Derivative {
    public static List<double> Derive2(List<double> a) {
        var d = new List<double>();

        for (int i = 0; i < a.Count-1; i++) {
            d.Add(a[i + 1] - a[i]);
        }
        return d;
    }

    public static List<double> Derive11(List<double> a) {
        if (a.Count < 11) {
            throw new System.Exception("Must give an array greater than 11 to derive with 11 point method!");
        }

        var d = new List<double>();
        for (int i = 0; i < a.Count - 11; i++) {
            d.Add(
                ( (-252 * a[i + 10]) + (2800 * a[i + 9]) + (-14175 * a[i + 8]) +
                (43200 * a[i + 7]) + (-88200 * a[i + 6]) + (127008 * a[i + 5]) +
                (-132300 * a[i + 4]) + (100800 * a[i + 3]) + (-56700 * a[i + 2]) +
                (25200 * a[i + 1]) + (-7381 * a[i]) ) / 2520.0
                );
            
        }

        for (int i = a.Count - 11; i < a.Count; i++) { 
           d.Add(
               ( (7381*a[i]) + (-25200*a[i-1]) + (56700*a[i-2]) +
               (-100800*a[i-3]) + (132300*a[i-4]) + (-127008*a[i-5]) + 
               (88200*a[i-6]) + (-43200*a[i-7]) + (14175*a[i-8]) + 
               (-2800*a[i-9]) + (252*a[i-10]) ) / 2520.0
               );
        }

        return d;

    }

    public static List<Vector3> Derive11(List<Vector3> v) {
        var d = new List<Vector3>();
        var xs = new List<double>();
        var ys = new List<double>();
        var zs = new List<double>();

        for (int i = 0; i < v.Count; i++) {
            xs.Add(v[i].x);
            ys.Add(v[i].y);
            zs.Add(v[i].z);
        }

        var x_d = Derive11(xs);
        var y_d = Derive11(ys);
        var z_d = Derive11(zs);

        for (int i = 0; i < x_d.Count; i++) {
            //if (x_d[i] < -180) x_d[i] = 360 + x_d[i];
            //else if (x_d[i] > 180) x_d[i] = x_d[i] - 360;
            //
            //if (y_d[i] < -180) y_d[i] = 360 + y_d[i];
            //else if (y_d[i] > 180) y_d[i] = y_d[i] - 360;
            //
            //if (z_d[i] < -180) z_d[i] = 360 + z_d[i];
            //else if (z_d[i] > 180) z_d[i] = z_d[i] - 360;

            var temp = new Vector3((float)x_d[i], (float)y_d[i], (float)z_d[i]);
            d.Add(temp);
        }

        return d;
    }
}

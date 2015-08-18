using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class GazeInferenceMenu {

    [MenuItem("GazeInference/ScoringTest")]
    private static void scoringTest() {
        
    }

    //TODO: Fix this.  Does not work.
    //clear the gaze targets from the current scene
    [MenuItem("GazeInference/Delete Gaze Points")]
    private static void deleteGazePoints() {
        var currEnvironement = AnimationManager.Instance.Environment;
        if (currEnvironement == null) return;
        var envName = currEnvironement.name;


        switch (currEnvironement.name)
        {
            case "WaitForBusEnv":
                deleteSceneGazePoints("WaitForBus");
                break;
            case "StealDiamondEnv":
                deleteSceneGazePoints("StealDiamond");
                break;
            case "BookShelfEnv":
                deleteSceneGazePoints("BookShelf");
                break;
            case "Walking90degEnv":
                deleteSceneGazePoints("Walking90deg");
                break;
            case "WindowWashingEnv":
                deleteSceneGazePoints("WindowWashingA");
                break;
        }
    }

    private static void deleteSceneGazePoints(string sceneName) {
        var sc = new SceneCollisions(sceneName);
        int count = 0;
        var targets = GameObject.FindGameObjectsWithTag("GazeTarget");
        var deleteTargets = new List<GameObject>();

        for (int i = 0; i < sc.SceneObjects.Count; i++) {
            var target = targets.FirstOrDefault(m => m.name == sc.SceneObjects[i].ObjectName + "_" + count.ToString());
            while (target != null) {
                deleteTargets.Add(target);
                count++;
                target = targets.FirstOrDefault(m => m.name == sc.SceneObjects[i].ObjectName + "_" + count.ToString());
            }
            count = 0;
        }

        //must delete the environment ones separately
        count = 0;
        string envName = sceneName + "Env";
        if (sceneName.Substring(sceneName.Length - 1).Equals("A") ||
            sceneName.Substring(sceneName.Length - 1).Equals("B"))
        {
            envName = sceneName.Substring(0, sceneName.Length - 1) + "Env";
        }

        var t = targets.FirstOrDefault(m => m.name == envName + "_" + count.ToString());
        while (t != null) {
            deleteTargets.Add(t);
            count++;
            t = targets.FirstOrDefault(m => m.name == envName + "_" + count.ToString());
        }

        //delete them one by one
        foreach (var d in deleteTargets) {
            GameObject.DestroyImmediate(d);
        }
    }


    //GazeInference Section//////////////////////////////////////////////////////

    [MenuItem("GazeInference/Inference/WindowWashing")]
    private static void inferenceWindowWashing() {
        var i = new GazeInference("WindowWashingA", "Norman", GlobalVars.InferenceIteration);
        angularVelocityOutput("WindowWashingA", "Norman");
    }

    [MenuItem("GazeInference/Inference/Walking90deg")]
    private static void inferenceWalking90deg()
    {
        var i = new GazeInference("Walking90deg", "Norman", GlobalVars.InferenceIteration);
        angularVelocityOutput("Walking90deg", "Norman");
    }

    [MenuItem("GazeInference/Inference/PassSoda/Norman")]
    private static void inferencePassSodaA()
    {
        var i = new GazeInference("PassSodaA", "Norman", GlobalVars.InferenceIteration);
        angularVelocityOutput("PassSodaA", "Norman");
    }

    [MenuItem("GazeInference/Inference/PassSoda/Roman")]
    private static void inferencePassSodaB()
    {
        var i = new GazeInference("PassSodaB", "Roman", GlobalVars.InferenceIteration);
        angularVelocityOutput("PassSodaB", "Roman");
    }

    [MenuItem("GazeInference/Inference/BookShelf")]
    private static void inferenceBookShelf()
    {
        var i = new GazeInference("BookShelf", "Norman", GlobalVars.InferenceIteration);
        angularVelocityOutput("BookShelf", "Norman");
    }

    [MenuItem("GazeInference/Inference/StealDiamond")]
    private static void inferenceStealDiamond()
    {
        var i = new GazeInference("StealDiamond", "Norman", GlobalVars.InferenceIteration);
        angularVelocityOutput("StealDiamond", "Norman");
    }

    [MenuItem("GazeInference/Inference/WaitForBus")]
    private static void inferenceWaitForBus()
    {
        var i = new GazeInference("WaitForBus", "Norman", GlobalVars.InferenceIteration);
        angularVelocityOutput("WaitForBus", "Norman");
    }

    [MenuItem("GazeInference/Inference/HandShake/Norman")]
    private static void inferenceHandShakeA()
    {
        var i = new GazeInference("HandShakeA", "Norman", GlobalVars.InferenceIteration);
        angularVelocityOutput("HandShakeA", "Norman");
    }

    [MenuItem("GazeInference/Inference/HandShake/Roman")]
    private static void inferenceHandShakeB()
    {
        var i = new GazeInference("HandShakeB", "Roman", GlobalVars.InferenceIteration);
        angularVelocityOutput("HandShakeB", "Roman");
    }


    //AngularVelocity section///////////////////////////////////////////

    [MenuItem("GazeInference/Angular Velocity Output/WindowWashing")]
    private static void angularVelocityOutputWindowWashing() {
        angularVelocityOutput("WindowWashingA", "Norman");
    }

    [MenuItem("GazeInference/Angular Velocity Output/Walking90deg")]
    private static void angularVelocityOutputWalking90deg()
    {
        angularVelocityOutput("Walking90deg", "Norman");
    }

    [MenuItem("GazeInference/Angular Velocity Output/PassSoda/Norman")]
    private static void angularVelocityOutputPassSodaA()
    {
        angularVelocityOutput("PassSodaA", "Norman");
    }

    [MenuItem("GazeInference/Angular Velocity Output/PassSoda/Roman")]
    private static void angularVelocityOutputPassSodaB()
    {
        angularVelocityOutput("PassSodaB", "Roman");
    }

    [MenuItem("GazeInference/Angular Velocity Output/StealDiamond")]
    private static void angularVelocityOutputStealDiamond()
    {
        angularVelocityOutput("StealDiamond", "Norman");
    }

    [MenuItem("GazeInference/Angular Velocity Output/BookShelf")]
    private static void angularVelocityOutputBookShelf()
    {
        angularVelocityOutput("BookShelf", "Norman");
    }

    [MenuItem("GazeInference/Angular Velocity Output/WaitForBus")]
    private static void angularVelocityOutputWaitForBus()
    {
        angularVelocityOutput("WaitForBus", "Norman");
    }

    [MenuItem("GazeInference/Angular Velocity Output/HandShake/Norman")]
    private static void angularVelocityOutputHandShakeA()
    {
        angularVelocityOutput("HandShakeA", "Norman");
    }

    [MenuItem("GazeInference/Angular Velocity Output/HandShake/Roman")]
    private static void angularVelocityOutputHandShakeB()
    {
        angularVelocityOutput("HandShakeB", "Roman");
    }



    //filter file section//////////////////////////////////////////

    [MenuItem("GazeInference/Filter File Output/WindowWashing")]
    private static void filterFileOutputWindowWashing() {
        filterFileOutput("WindowWashingA", "Norman");
    }

    [MenuItem("GazeInference/Filter File Output/Walking90deg")]
    private static void filterFileOutputWalking90deg()
    {
        filterFileOutput("Walking90deg", "Norman");
    }

    [MenuItem("GazeInference/Filter File Output/PassSoda/Norman")]
    private static void filterFileOutputPassSodaA()
    {
        filterFileOutput("PassSodaA", "Norman");
    }

    [MenuItem("GazeInference/Filter File Output/PassSoda/Roman")]
    private static void filterFileOutputPassSodaB()
    {
        filterFileOutput("PassSodaB", "Roman");
    }

    private static void angularVelocityOutput(string animationName, string characterName) {
        string fileOutput = GlobalVars.MatlabPath + "angularVelocity.csv";
        string fileOutput_filtered = GlobalVars.MatlabPath + "angularVelocityFiltered.csv";


        //if (GlobalVars.work)
        //{
        //    //fileOutput = @"C:\Local Users\drakita\Documents\cs699\angularVelocity.csv";
        //    //fileOutput_filtered = @"C:\Local Users\drakita\Documents\cs699\angularVelocityFiltered.csv";
        //    fileOutput = @"\\wfs1\users$\rakita\My Documents\cs699\Leap_\LeapUnity\Assets\Matlab\angularVelocity.csv";
        //    fileOutput_filtered = @"\\wfs1\users$\rakita\My Documents\cs699\Leap_\LeapUnity\Assets\Matlab\angularVelocityFiltered.csv";
        //}
        //else
        //{
        //    //fileOutput = @"C:\Users\Danny\Desktop\angularVelocity.csv";
        //    //fileOutput_filtered = @"C:\Users\Danny\Desktop\angularVelocityFiltered.csv";
        //    fileOutput = @"E:\CS699-Gleicher\Leap_\LeapUnity\Assets\Matlab\angularVelocity.csv";
        //    fileOutput_filtered = @"E:\CS699-Gleicher\Leap_\LeapUnity\Assets\Matlab\angularVelocityFiltered.csv";
        //}


        var ad = new AngleData(animationName, characterName);
        //index 0: local 
        //      1: global
        //      2: root
        //      3: headlocal
        int index = GlobalVars.OrientationIndex;
        FileIO.AngularVelocityCSV(fileOutput, ad, index);
        FileIO.AngularVelocityCSV(fileOutput_filtered, ad, index, 17, 3, 0.5);
    }

    private static void filterFileOutput(string animationName, string characterName) {
        var fd = new FilterData(animationName, characterName);
        var preCollapse = fd.queryJoint("RElbow"); // these are quaternions
        var eulers = Filter.DisplayCurveCollapse(preCollapse);
        var medians = Filter.MedianFilter(eulers);
        string fileOutput = GlobalVars.MatlabPath + "prefilter.csv";
        //if (GlobalVars.work) { fileOutput = @"C:\Local Users\drakita\Documents\cs699\prefilter.csv"; }
        //else { fileOutput = @"C:\Users\Danny\Desktop\prefilter.csv"; }

        //will want to change the file location based on what machine you're on...
        FileIO.AngleCSV(fileOutput, medians);

        //post filtered section...
        ////////////////////////////
        string fileOutputPost = GlobalVars.MatlabPath + "postfilter.csv";
        //if (GlobalVars.work) { fileOutputPost = @"C:\Local Users\drakita\Documents\cs699\postfilter.csv"; }
        //else { fileOutputPost = @"C:\Users\Danny\Desktop\postfilter.csv"; }

        //can still use the preCollapse and orientations variables from above
        //right now, 7,2,1 work as parameters for the Gaussian
        var qFilter = Filter.BilateralFilter(preCollapse, 7, 2, 1);
        var eulers2 = Filter.DisplayCurveCollapse(qFilter);
        FileIO.AngleCSV(fileOutputPost, eulers2);
    }
    

} //end of GazeInferenceMenu class

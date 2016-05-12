using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

public static class EyeGazeEditTestSceneManager
{
    /// <summary>
    /// Load one of the predefined example scenes for gaze editing.
    /// </summary>
    /// <param name="sceneName">Example scene name</param>
    public static void LoadExampleScene(string sceneName)
    {
        var timeline = AnimationManager.Instance.Timeline;
        var gazeTargets = GameObject.FindGameObjectsWithTag("GazeTarget");
        var testScenes = GameObject.FindGameObjectWithTag("EyeGazeEditor").GetComponent<EyeGazeEditTestSceneData>();
        timeline.RemoveAllLayers();
        timeline.OwningManager.RemoveAllModels();

        // Reload Leap configuration
        LEAPCore.LoadConfiguration();

        // Deactivate all characters and props
        testScenes.modelNorman.SetActive(false);
        testScenes.modelNormanette.SetActive(false);
        testScenes.modelRoman.SetActive(false);
        testScenes.modelNormanNew.SetActive(false);
        testScenes.modelRomanTall.SetActive(false);
        testScenes.modelNormanElliot.SetActive(false);
        testScenes.modelTestExpressiveGazeEnv.SetActive(false);
        testScenes.modelWindowWashingEnv.SetActive(false);
        testScenes.modelPassSodaEnv.SetActive(false);
        testScenes.modelWalking90degEnv.SetActive(false);
        testScenes.modelHandShakeEnv.SetActive(false);
        testScenes.modelBookShelfEnv.SetActive(false);
        testScenes.modelStealDiamondEnv.SetActive(false);
        testScenes.modelWaitForBusEnv.SetActive(false);
        testScenes.modelStackBoxesEnv.SetActive(false);
        testScenes.modelWindowWashingNewEnv.SetActive(false);
        testScenes.modelWalkConesNewEnv.SetActive(false);
        testScenes.modelStealDiamondNewEnv.SetActive(false);
        testScenes.modelBookShelfNewEnv.SetActive(false);
        testScenes.modelWaitForBusNewEnv.SetActive(false);
        testScenes.modelStackBoxesNewEnv.SetActive(false);
        testScenes.modelChatWithFriendEnv.SetActive(false);
        testScenes.modelMakeSandwichEnv.SetActive(false);
        testScenes.modelMakeSandwichDemoEnv.SetActive(false);
        testScenes.modelKinect.SetActive(false);
        testScenes.modelEyeTrackMocapTest1Env.SetActive(false);

        // Create and configure animation layers:
        timeline.AddLayer(AnimationLayerMode.Override, 0, LEAPCore.helperAnimationLayerName);
        timeline.GetLayer(LEAPCore.helperAnimationLayerName).isIKEndEffectorConstr = false;
        timeline.AddLayer(AnimationLayerMode.Override, 5, LEAPCore.environmentAnimationLayerName);
        timeline.GetLayer(LEAPCore.environmentAnimationLayerName).isIKEndEffectorConstr = false;
        timeline.AddLayer(AnimationLayerMode.Override, 10, LEAPCore.baseAnimationLayerName);
        timeline.GetLayer(LEAPCore.baseAnimationLayerName).isIKEndEffectorConstr = true;
        timeline.AddLayer(AnimationLayerMode.Override, 15, LEAPCore.cameraAnimationLayerName);
        timeline.GetLayer(LEAPCore.cameraAnimationLayerName).isIKEndEffectorConstr = false;
        timeline.AddLayer(AnimationLayerMode.Override, 20, LEAPCore.eyeGazeAnimationLayerName);

        if (sceneName == "TestExpressiveGaze")
        {
            testScenes.modelNormanNew.SetActive(true);
            testScenes.modelTestExpressiveGazeEnv.SetActive(true);

            // Add character models to the timeline
            timeline.OwningManager.AddModel(testScenes.modelNormanNew);

            // Set environment in the timeline
            timeline.OwningManager.SetEnvironment(testScenes.modelTestExpressiveGazeEnv);

            // Create animation instances
            var bodyAnimationNorman = new AnimationClipInstance("TestExpressiveGaze", testScenes.modelNormanNew, true, true, false);
            int bodyAnimationNormanInstanceId = timeline.AddAnimation(LEAPCore.baseAnimationLayerName, bodyAnimationNorman, 0);

            // Load eye gaze
            EyeGazeEditor.LoadEyeGaze(timeline, bodyAnimationNormanInstanceId, LEAPCore.eyeGazeAnimationLayerName);
            EyeGazeEditor.PrintEyeGaze(timeline);
        }
        else if (sceneName == "WindowWashing")
        {
            testScenes.modelNorman.SetActive(true);
            testScenes.modelWindowWashingEnv.SetActive(true);

            // Add character models to the timeline
            timeline.OwningManager.AddModel(testScenes.modelNorman);
            timeline.OwningManager.AddModel(testScenes.modelNormanette);

            // Set environment in the timeline
            timeline.OwningManager.SetEnvironment(testScenes.modelWindowWashingEnv);

            // Create animation instances
            var bodyAnimationNorman = new AnimationClipInstance("WindowWashingA", testScenes.modelNorman);
            var bodyAnimationNormanette = new AnimationClipInstance("WindowWashingB", testScenes.modelNormanette,
                false, false, false);

            // Add animations to characters
            int bodyAnimationNormanInstanceId = timeline.AddAnimation(LEAPCore.baseAnimationLayerName,
                bodyAnimationNorman, 0);
            timeline.AddAnimation(LEAPCore.baseAnimationLayerName, bodyAnimationNormanette, 0, LEAPCore.helperAnimationLayerName);

            // Load eye gaze
            EyeGazeEditor.LoadEyeGaze(timeline, bodyAnimationNormanInstanceId, LEAPCore.eyeGazeAnimationLayerName);
            EyeGazeEditor.PrintEyeGaze(timeline);

            // Create environment animations
            var envController = testScenes.modelWindowWashingEnv.GetComponent<EnvironmentController>();
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance(
                    "WindowWashingSponge", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "Sponge"),
                    false, false, false));

            // Add timewarps to the animations
            AnimationTimingEditor.LoadTimewarps(timeline, bodyAnimationNormanInstanceId);
        }
        else if (sceneName == "PassSoda")
        {
            testScenes.modelNorman.SetActive(true);
            testScenes.modelRoman.SetActive(true);
            testScenes.modelNormanette.SetActive(false);
            testScenes.modelPassSodaEnv.SetActive(true);

            // Add character models to the timeline
            timeline.OwningManager.AddModel(testScenes.modelNorman);
            timeline.OwningManager.AddModel(testScenes.modelNormanette);
            timeline.OwningManager.AddModel(testScenes.modelRoman);

            // Set environment in the timeline
            timeline.OwningManager.SetEnvironment(testScenes.modelPassSodaEnv);

            // Create animation instances
            var bodyAnimationNorman = new AnimationClipInstance("PassSodaA", testScenes.modelNorman);
            int bodyAnimationNormanInstanceId = timeline.AddAnimation(LEAPCore.baseAnimationLayerName,
                bodyAnimationNorman, 0, LEAPCore.helperAnimationLayerName);
            var bodyAnimationRoman = new AnimationClipInstance("PassSodaB", testScenes.modelRoman);
            int bodyAnimationRomanInstanceId = timeline.AddAnimation(LEAPCore.baseAnimationLayerName,
                bodyAnimationRoman, 0, LEAPCore.helperAnimationLayerName);

            // Create environment animations
            var envController = testScenes.modelPassSodaEnv.GetComponent<EnvironmentController>();
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance("PassSodaBottle", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "SodaBottle"),
                    false, false, false));

            // Load eye gaze
            EyeGazeEditor.LoadEyeGaze(timeline, bodyAnimationNormanInstanceId, LEAPCore.eyeGazeAnimationLayerName);
            EyeGazeEditor.LoadEyeGaze(timeline, bodyAnimationRomanInstanceId, LEAPCore.eyeGazeAnimationLayerName);
            EyeGazeEditor.PrintEyeGaze(timeline);
        }
        else if (sceneName == "PassSoda-Edits")
        {
            testScenes.modelNorman.SetActive(true);
            testScenes.modelRoman.SetActive(true);
            testScenes.modelNormanette.SetActive(true);
            testScenes.modelNormanette.transform.position = new Vector3(-4.97f, 0f, 1.24f);
            testScenes.modelNormanette.transform.localScale = new Vector3(0.96f, 0.91f, 0.96f);
            testScenes.modelPassSodaEnv.SetActive(true);

            // Add character models to the timeline
            timeline.OwningManager.AddModel(testScenes.modelNorman);
            timeline.OwningManager.AddModel(testScenes.modelNormanette);
            timeline.OwningManager.AddModel(testScenes.modelRoman);

            // Set environment in the timeline
            timeline.OwningManager.SetEnvironment(testScenes.modelPassSodaEnv);

            // Create animation instances
            var bodyAnimationNorman = new AnimationClipInstance("PassSodaA", testScenes.modelNorman);
            int bodyAnimationNormanInstanceId = timeline.AddAnimation(LEAPCore.baseAnimationLayerName,
                bodyAnimationNorman, 0, LEAPCore.helperAnimationLayerName);
            var bodyAnimationRoman = new AnimationClipInstance("PassSodaB", testScenes.modelRoman);
            int bodyAnimationRomanInstanceId = timeline.AddAnimation(LEAPCore.baseAnimationLayerName,
                bodyAnimationRoman, 0, LEAPCore.helperAnimationLayerName);
            var bodyAnimationNormanette = new AnimationClipInstance("PassSodaC", testScenes.modelNormanette);
            timeline.AddAnimation(LEAPCore.baseAnimationLayerName, bodyAnimationNormanette, 0);

            // Create environment animations
            var envController = testScenes.modelPassSodaEnv.GetComponent<EnvironmentController>();
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance("PassSodaBottle", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "SodaBottle"),
                    false, false, false));

            // Load eye gaze
            EyeGazeEditor.LoadEyeGaze(timeline, bodyAnimationNormanInstanceId, LEAPCore.eyeGazeAnimationLayerName, "#Edits");
            EyeGazeEditor.LoadEyeGaze(timeline, bodyAnimationRomanInstanceId, LEAPCore.eyeGazeAnimationLayerName, "#Edits");
            EyeGazeEditor.PrintEyeGaze(timeline);
        }
        else if (sceneName == "Walking90deg")
        {
            testScenes.modelNorman.SetActive(true);
            testScenes.modelWalking90degEnv.SetActive(true);

            // Add character models to the timeline
            timeline.OwningManager.AddModel(testScenes.modelNorman);

            // Set environment in the timeline
            timeline.OwningManager.SetEnvironment(testScenes.modelWalking90degEnv);

            // Create animation instances
            var bodyAnimationNorman = new AnimationClipInstance("Walking90deg", testScenes.modelNorman);
            int bodyAnimationNormanInstanceId = timeline.AddAnimation(LEAPCore.baseAnimationLayerName,
                bodyAnimationNorman, 0, LEAPCore.helperAnimationLayerName);

            // Create camera animations
            var envController = testScenes.modelWalking90degEnv.GetComponent<EnvironmentController>();
            var cameraAnimation = new AnimationClipInstance("Walking90degCamera",
                envController.Cameras.FirstOrDefault(cam => cam.gameObject.name == "CameraWalking90deg").gameObject,
                false, false, false);
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName, cameraAnimation);

            // Load eye gaze
            EyeGazeEditor.LoadEyeGaze(timeline, bodyAnimationNormanInstanceId, LEAPCore.eyeGazeAnimationLayerName);
            EyeGazeEditor.PrintEyeGaze(timeline);
        }
        else if (sceneName == "HandShake")
        {
            testScenes.modelNorman.SetActive(true);
            testScenes.modelRoman.SetActive(true);
            testScenes.modelHandShakeEnv.SetActive(true);

            // Add character models to the timeline
            timeline.OwningManager.AddModel(testScenes.modelNorman);
            timeline.OwningManager.AddModel(testScenes.modelRoman);

            // Set environment in the timeline
            timeline.OwningManager.SetEnvironment(testScenes.modelHandShakeEnv);

            // Create animation instances
            var bodyAnimationNorman = new AnimationClipInstance("HandShakeA", testScenes.modelNorman);
            int bodyAnimationNormanInstanceId = timeline.AddAnimation(LEAPCore.baseAnimationLayerName,
                bodyAnimationNorman, 0, LEAPCore.helperAnimationLayerName);
            var bodyAnimationRoman = new AnimationClipInstance("HandShakeB", testScenes.modelRoman);
            int bodyAnimationRomanInstanceId = timeline.AddAnimation(LEAPCore.baseAnimationLayerName,
                bodyAnimationRoman, 0, LEAPCore.helperAnimationLayerName);

            // Create camera animations
            var envController = testScenes.modelHandShakeEnv.GetComponent<EnvironmentController>();
            var cameraAnimation = new AnimationClipInstance("HandShakeCamera",
                envController.Cameras.FirstOrDefault(cam => cam.gameObject.name == "CameraHandShake").gameObject,
                false, false, false);
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName, cameraAnimation);

            // Load eye gaze
            EyeGazeEditor.LoadEyeGaze(timeline, bodyAnimationNormanInstanceId, LEAPCore.eyeGazeAnimationLayerName);
            EyeGazeEditor.LoadEyeGaze(timeline, bodyAnimationRomanInstanceId, LEAPCore.eyeGazeAnimationLayerName);
            EyeGazeEditor.PrintEyeGaze(timeline);
        }
        else if (sceneName == "HandShake-Edits")
        {
            testScenes.modelNorman.SetActive(true);
            testScenes.modelRoman.SetActive(true);
            testScenes.modelHandShakeEnv.SetActive(true);

            // Add character models to the timeline
            timeline.OwningManager.AddModel(testScenes.modelNorman);
            timeline.OwningManager.AddModel(testScenes.modelRoman);

            // Set environment in the timeline
            timeline.OwningManager.SetEnvironment(testScenes.modelHandShakeEnv);

            // Create animation instances
            var bodyAnimationNorman = new AnimationClipInstance("HandShakeA", testScenes.modelNorman);
            int bodyAnimationNormanInstanceId = timeline.AddAnimation(LEAPCore.baseAnimationLayerName,
                bodyAnimationNorman, 0, LEAPCore.helperAnimationLayerName);
            var bodyAnimationRoman = new AnimationClipInstance("HandShakeB", testScenes.modelRoman);
            int bodyAnimationRomanInstanceId = timeline.AddAnimation(LEAPCore.baseAnimationLayerName,
                bodyAnimationRoman, 0, LEAPCore.helperAnimationLayerName);

            // Create camera animations
            var envController = testScenes.modelHandShakeEnv.GetComponent<EnvironmentController>();
            var cameraAnimation = new AnimationClipInstance("HandShakeCamera",
                envController.Cameras.FirstOrDefault(cam => cam.gameObject.name == "CameraHandShake").gameObject,
                false, false, false);
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName, cameraAnimation);

            // Load eye gaze
            EyeGazeEditor.LoadEyeGaze(timeline, bodyAnimationNormanInstanceId, LEAPCore.eyeGazeAnimationLayerName, "#Edits");
            EyeGazeEditor.LoadEyeGaze(timeline, bodyAnimationRomanInstanceId, LEAPCore.eyeGazeAnimationLayerName, "#Edits");
            EyeGazeEditor.PrintEyeGaze(timeline);
        }
        else if (sceneName == "BookShelf")
        {
            testScenes.modelNorman.SetActive(true);
            testScenes.modelBookShelfEnv.SetActive(true);

            // Add character models to the timeline
            timeline.OwningManager.AddModel(testScenes.modelNorman);

            // Set environment in the timeline
            timeline.OwningManager.SetEnvironment(testScenes.modelBookShelfEnv);

            // Create animation instances
            var bodyAnimationNorman = new AnimationClipInstance("BookShelf", testScenes.modelNorman);
            int bodyAnimationNormanInstanceId = timeline.AddAnimation(LEAPCore.baseAnimationLayerName,
                bodyAnimationNorman, 0, LEAPCore.helperAnimationLayerName);

            // Load eye gaze
            EyeGazeEditor.LoadEyeGaze(timeline, bodyAnimationNormanInstanceId, LEAPCore.eyeGazeAnimationLayerName);
            EyeGazeEditor.PrintEyeGaze(timeline);

            // Create environment animations
            var envController = testScenes.modelBookShelfEnv.GetComponent<EnvironmentController>();
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance("BookShelfBook1", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "Book1"),
                    false, false, false));
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance("BookShelfBook2", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "Book2"),
                    false, false, false));
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance("BookShelfBook3", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "Book3"),
                    false, false, false));
        }
        else if (sceneName == "StealDiamond")
        {
            testScenes.modelNorman.SetActive(true);
            testScenes.modelStealDiamondEnv.SetActive(true);

            // Add character models to the timeline
            timeline.OwningManager.AddModel(testScenes.modelNorman);

            // Set environment in the timeline
            timeline.OwningManager.SetEnvironment(testScenes.modelStealDiamondEnv);

            // Create animation instances
            var bodyAnimationNorman = new AnimationClipInstance("StealDiamond", testScenes.modelNorman);
            int bodyAnimationNormanInstanceId = timeline.AddAnimation(LEAPCore.baseAnimationLayerName,
                bodyAnimationNorman, 0, LEAPCore.helperAnimationLayerName);

            // Create environment animations
            var envController = testScenes.modelStealDiamondEnv.GetComponent<EnvironmentController>();
            var envAnimationGem = new AnimationClipInstance("StealDiamondGem",
                envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "Gem"),
                false, false, false);
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName, envAnimationGem);
            var cameraAnimation = new AnimationClipInstance("StealDiamondCamera1",
                envController.Cameras.FirstOrDefault(cam => cam.gameObject.name == "CameraStealDiamond1").gameObject,
                false, false, false);
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName, cameraAnimation);
            cameraAnimation = new AnimationClipInstance("StealDiamondCamera2",
                envController.Cameras.FirstOrDefault(cam => cam.gameObject.name == "CameraStealDiamond2").gameObject,
                false, false, false);
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName, cameraAnimation);

            // Load eye gaze
            EyeGazeEditor.LoadEyeGaze(timeline, bodyAnimationNormanInstanceId, LEAPCore.eyeGazeAnimationLayerName);
            EyeGazeEditor.PrintEyeGaze(timeline);
        }
        else if (sceneName == "WaitForBus")
        {
            testScenes.modelNorman.SetActive(true);
            testScenes.modelWaitForBusEnv.SetActive(true);

            // Add character models to the timeline
            timeline.OwningManager.AddModel(testScenes.modelNorman);

            // Set environment in the timeline
            timeline.OwningManager.SetEnvironment(testScenes.modelWaitForBusEnv);

            // Create animation instances
            var bodyAnimationNorman = new AnimationClipInstance("WaitForBus", testScenes.modelNorman);
            int bodyAnimationNormanInstanceId = timeline.AddAnimation(LEAPCore.baseAnimationLayerName,
                bodyAnimationNorman, 0, LEAPCore.helperAnimationLayerName);

            // Create environment animations
            var envController = testScenes.modelWaitForBusEnv.GetComponent<EnvironmentController>();
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance("WaitForBusWatch", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "Watch"),
                    false, false, false));

            // Load eye gaze
            EyeGazeEditor.LoadEyeGaze(timeline, bodyAnimationNormanInstanceId, LEAPCore.eyeGazeAnimationLayerName);
            EyeGazeEditor.PrintEyeGaze(timeline);
        }
        else if (sceneName == "EyeTrackMocapTest1-1")
        {
            testScenes.modelKinect.SetActive(true);
            testScenes.modelEyeTrackMocapTest1Env.SetActive(true);

            // Add character models to the timeline
            timeline.OwningManager.AddModel(testScenes.modelKinect);

            // Set environment in the timeline
            timeline.OwningManager.SetEnvironment(testScenes.modelEyeTrackMocapTest1Env);

            // Create animation instances
            var bodyAnimation = new AnimationClipInstance("EyeTrackMocapTest1-1", testScenes.modelKinect);
            int bodyAnimationInstanceId = timeline.AddAnimation(LEAPCore.baseAnimationLayerName,
                bodyAnimation, 0, LEAPCore.helperAnimationLayerName);

            // Load eye gaze
            EyeGazeEditor.LoadEyeGaze(timeline, bodyAnimationInstanceId, LEAPCore.eyeGazeAnimationLayerName);
            EyeGazeEditor.PrintEyeGaze(timeline);
        }
        else if (sceneName == "StackBoxes")
        {
            testScenes.modelNormanNew.SetActive(true);
            testScenes.modelStackBoxesEnv.SetActive(true);

            // Add character models to the timeline
            timeline.OwningManager.AddModel(testScenes.modelNormanNew);

            // Set environment in the timeline
            timeline.OwningManager.SetEnvironment(testScenes.modelStackBoxesEnv);

            // Create animation instances
            var bodyAnimationNorman = new AnimationClipInstance("StackBoxes", testScenes.modelNormanNew);

            // Add animations to characters
            int bodyAnimationNormanInstanceId = timeline.AddAnimation(LEAPCore.baseAnimationLayerName,
                bodyAnimationNorman, 0);

            // Load eye gaze
            EyeGazeEditor.LoadEyeGaze(timeline, bodyAnimationNormanInstanceId, LEAPCore.eyeGazeAnimationLayerName);
            EyeGazeEditor.PrintEyeGaze(timeline);

            // Create environment animations
            var envController = testScenes.modelStackBoxesEnv.GetComponent<EnvironmentController>();
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance(
                    "StackBoxes1", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "pCube1"),
                    false, false, false));
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance(
                    "StackBoxes2", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "pCube2"),
                    false, false, false));
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance(
                    "StackBoxes3", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "pCube3"),
                    false, false, false));
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance(
                    "StackBoxes4", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "pCube4"),
                    false, false, false));
        }
        else if (sceneName == "WindowWashingNew")
        {
            testScenes.modelNormanNew.SetActive(true);
            testScenes.modelWindowWashingNewEnv.SetActive(true);

            // Add character models to the timeline
            timeline.OwningManager.AddModel(testScenes.modelNormanNew);

            // Set environment in the timeline
            timeline.OwningManager.SetEnvironment(testScenes.modelWindowWashingNewEnv);

            // Create animation instances
            var bodyAnimationNorman = new AnimationClipInstance("WindowWashingNew", testScenes.modelNormanNew);

            // Add animations to characters
            int bodyAnimationNormanInstanceId = timeline.AddAnimation(LEAPCore.baseAnimationLayerName,
                bodyAnimationNorman, 0);

            // Load eye gaze
            EyeGazeEditor.LoadEyeGaze(timeline, bodyAnimationNormanInstanceId, LEAPCore.eyeGazeAnimationLayerName);
            EyeGazeEditor.PrintEyeGaze(timeline);

            // Create environment animations
            var envController = testScenes.modelWindowWashingNewEnv.GetComponent<EnvironmentController>();
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance(
                    "WindowWashingNewSponge", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "Sponge"),
                    false, false, false));
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance(
                    "WindowWashingNewMarkers", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "Markers"),
                    false, false, false));

            // Extract eye tracking fixation frames
            ExtractEyeTrackFixationFrames(sceneName, timeline, testScenes.modelNormanNew, 970, 2045);
            ComputeIntercoderReliabilityInstances(sceneName, 91, 1165);
        }
        else if (sceneName == "WindowWashingNew-Edits")
        {
            testScenes.modelNormanNew.SetActive(true);
            testScenes.modelNormanette.SetActive(true);
            testScenes.modelNormanette.transform.position = new Vector3(2.58f, 0f, -3.72f);
            testScenes.modelNormanette.transform.localScale = new Vector3(0.96f, 0.91f, 0.96f);
            testScenes.modelWindowWashingNewEnv.SetActive(true);

            // Add character models to the timeline
            timeline.OwningManager.AddModel(testScenes.modelNormanNew);
            timeline.OwningManager.AddModel(testScenes.modelNormanette);

            // Set environment in the timeline
            timeline.OwningManager.SetEnvironment(testScenes.modelWindowWashingNewEnv);

            // Create animation instances
            var bodyAnimationNorman = new AnimationClipInstance("WindowWashingNew", testScenes.modelNormanNew);
            var bodyAnimationNormanette = new AnimationClipInstance("WindowWashingB", testScenes.modelNormanette);

            // Add animations to characters
            int bodyAnimationNormanInstanceId = timeline.AddAnimation(LEAPCore.baseAnimationLayerName,
                bodyAnimationNorman, 0);
            timeline.AddAnimation(LEAPCore.baseAnimationLayerName, new AnimationClipInstance("WindowWashingBStart",
                testScenes.modelNormanette, false, false, false), 0);
            int bodyAnimationNormanetteInstanceId = timeline.AddAnimation(LEAPCore.baseAnimationLayerName,
                bodyAnimationNormanette, 380);

            // Load eye gaze
            EyeGazeEditor.LoadEyeGaze(timeline, bodyAnimationNormanInstanceId, LEAPCore.eyeGazeAnimationLayerName, "#Edits");
            EyeGazeEditor.PrintEyeGaze(timeline);

            // Create environment animations
            var envController = testScenes.modelWindowWashingNewEnv.GetComponent<EnvironmentController>();
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance(
                    "WindowWashingNewSponge", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "Sponge"),
                    false, false, false));
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance(
                    "WindowWashingNewMarkers", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "Markers"),
                    false, false, false));
        }
        else if (sceneName == "WalkConesNew")
        {
            testScenes.modelNormanNew.SetActive(true);
            testScenes.modelWalkConesNewEnv.SetActive(true);

            // Add character models to the timeline
            timeline.OwningManager.AddModel(testScenes.modelNormanNew);

            // Set environment in the timeline
            timeline.OwningManager.SetEnvironment(testScenes.modelWalkConesNewEnv);

            // Create animation instances
            var bodyAnimationNorman = new AnimationClipInstance("WalkConesNew", testScenes.modelNormanNew);

            // Add animations to characters
            int bodyAnimationNormanInstanceId = timeline.AddAnimation(LEAPCore.baseAnimationLayerName,
                bodyAnimationNorman, 0);

            // Create environment animations
            var envController = testScenes.modelWalkConesNewEnv.GetComponent<EnvironmentController>();
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance(
                    "WalkConesNewMarkers", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "Markers"),
                    false, false, false));
            var root = ModelUtil.FindBoneWithTag(testScenes.modelNormanNew.transform, "RootBone");
            var cameraAnimation = new LookAtFrontInstance("WalkConesCamera",
                envController.Cameras.FirstOrDefault(cam => cam.gameObject.name == "CameraWalkConesNew").gameObject,
                bodyAnimationNorman.FrameLength, root, 7.6f, 5.2f, 3.2f); 
            timeline.AddEnvironmentAnimation(LEAPCore.cameraAnimationLayerName, cameraAnimation);

            // Load eye gaze
            EyeGazeEditor.LoadEyeGaze(timeline, bodyAnimationNormanInstanceId, LEAPCore.eyeGazeAnimationLayerName);
            EyeGazeEditor.PrintEyeGaze(timeline);
        }
        else if (sceneName == "BookShelfNew")
        {
            testScenes.modelNormanNew.SetActive(true);
            testScenes.modelBookShelfNewEnv.SetActive(true);
            
            // Add character models to the timeline
            timeline.OwningManager.AddModel(testScenes.modelNormanNew);

            // Set environment in the timeline
            timeline.OwningManager.SetEnvironment(testScenes.modelBookShelfNewEnv);

            // Set book layout
            var envController = testScenes.modelBookShelfNewEnv.GetComponent<EnvironmentController>();
            var book1 = envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "Book1");
            var book2 = envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "Book2");
            var book3 = envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "Book3");
            var endEffectorTargets = ModelUtil.GetEndEffectorTargets(envController.gameObject);
            var book1Handle = endEffectorTargets.FirstOrDefault(t => t.name == "Book1Handle");
            var book2Handle = endEffectorTargets.FirstOrDefault(t => t.name == "Book2Handle");
            var book3Handle = endEffectorTargets.FirstOrDefault(t => t.name == "Book3Handle");
            book1.transform.localPosition = new Vector3(-8.2f, 2.27f, -1.63f);
            book2.transform.localPosition = new Vector3(-7.99f, 2.53f, -0.56f);
            book3.transform.localPosition = new Vector3(-8f, 3.96f, -1.64f);
            book1Handle.localPosition = new Vector3(-7.47f, 2.50f, -1.63f);
            book2Handle.localPosition = new Vector3(-7.98f, 2.55f, -0.42f);
            book3Handle.localPosition = new Vector3(-7.4f, 3.83f, -1.39f);

            // Create animation instances
            var bodyAnimationNorman = new AnimationClipInstance("BookShelfNew", testScenes.modelNormanNew);
            int bodyAnimationNormanInstanceId = timeline.AddAnimation(LEAPCore.baseAnimationLayerName,
                bodyAnimationNorman, 0, LEAPCore.helperAnimationLayerName);

            // Load eye gaze
            EyeGazeEditor.LoadEyeGaze(timeline, bodyAnimationNormanInstanceId, LEAPCore.eyeGazeAnimationLayerName);
            EyeGazeEditor.PrintEyeGaze(timeline);

            // Create environment animations
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance("BookShelfNewBook1", book1, false, false, false));
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance("BookShelfNewBook2", book2, false, false, false));
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance("BookShelfNewBook3", book3, false, false, false));
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance(
                    "BookShelfNewMarkers", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "Markers"),
                    false, false, false));

            // Prepare eye gaze inference evaluations
            ExtractEyeTrackFixationFrames(sceneName, timeline, testScenes.modelNormanNew, 749, 2618);
            ComputeIntercoderReliabilityTargets(sceneName);
            ComputeIntercoderReliabilityInstances(sceneName, 163, 2032);
        }
        else if (sceneName == "BookShelfNew-Edits")
        {
            testScenes.modelNormanNew.SetActive(true);
            testScenes.modelBookShelfNewEnv.SetActive(true);

            // Add character models to the timeline
            timeline.OwningManager.AddModel(testScenes.modelNormanNew);

            // Set environment in the timeline
            timeline.OwningManager.SetEnvironment(testScenes.modelBookShelfNewEnv);

            // Set book layout
            var envController = testScenes.modelBookShelfNewEnv.GetComponent<EnvironmentController>();
            var book1 = envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "Book1");
            var book2 = envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "Book2");
            var book3 = envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "Book3");
            var endEffectorTargets = ModelUtil.GetEndEffectorTargets(envController.gameObject);
            var book1Handle = endEffectorTargets.FirstOrDefault(t => t.name == "Book1Handle");
            var book2Handle = endEffectorTargets.FirstOrDefault(t => t.name == "Book2Handle");
            var book3Handle = endEffectorTargets.FirstOrDefault(t => t.name == "Book3Handle");
            book1.transform.localPosition = new Vector3(-8.2f, 3.93f, -1.63f);
            book2.transform.localPosition = new Vector3(-7.99f, 4.18f, -0.37f);
            book3.transform.localPosition = new Vector3(-8f, 3.96f, -0.08f);
            book1Handle.localPosition = new Vector3(-7.44f, 3.8f, -1.47f);
            book2Handle.localPosition = new Vector3(-7.74f, 4.02f, -0.21f);
            book3Handle.localPosition = new Vector3(-7.32f, 3.8f, 0.15f);

            // Create animation instances
            var bodyAnimationNorman = new AnimationClipInstance("BookShelfNew", testScenes.modelNormanNew);
            int bodyAnimationNormanInstanceId = timeline.AddAnimation(LEAPCore.baseAnimationLayerName,
                bodyAnimationNorman, 0, LEAPCore.helperAnimationLayerName);

            // Load eye gaze
            EyeGazeEditor.LoadEyeGaze(timeline, bodyAnimationNormanInstanceId, LEAPCore.eyeGazeAnimationLayerName, "#Edits");
            EyeGazeEditor.PrintEyeGaze(timeline);

            // Create environment animations
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance("BookShelfNewBook1", book1, false, false, false));
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance("BookShelfNewBook2", book2, false, false, false));
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance("BookShelfNewBook3", book3, false, false, false));
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance(
                    "BookShelfNewMarkers", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "Markers"),
                    false, false, false));
        }
        else if (sceneName == "StealDiamondNew")
        {
            testScenes.modelNormanNew.SetActive(true);
            testScenes.modelStealDiamondNewEnv.SetActive(true);

            // Add character models to the timeline
            timeline.OwningManager.AddModel(testScenes.modelNormanNew);

            // Set environment in the timeline
            timeline.OwningManager.SetEnvironment(testScenes.modelStealDiamondNewEnv);

            // Create animation instances
            var bodyAnimationNorman = new AnimationClipInstance("StealDiamondNew", testScenes.modelNormanNew);
            int bodyAnimationNormanInstanceId = timeline.AddAnimation(LEAPCore.baseAnimationLayerName,
                bodyAnimationNorman, 0, LEAPCore.helperAnimationLayerName);

            // Create environment animations
            var envController = testScenes.modelStealDiamondNewEnv.GetComponent<EnvironmentController>();
            var envAnimationGem = new AnimationClipInstance("StealDiamondNewGem",
                envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "Gem"),
                false, false, false);
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance(
                    "StealDiamondNewMarkers", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "Markers"),
                    false, false, false));
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName, envAnimationGem);
            var cameraAnimation = new AnimationClipInstance("StealDiamondNewCamera1",
                envController.Cameras.FirstOrDefault(cam => cam.gameObject.name == "CameraStealDiamondNew1").gameObject,
                //envController.Cameras.FirstOrDefault(cam => cam.gameObject.name == "CameraStealDiamondNew1-CloseUp").gameObject,
                false, false, false);
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName, cameraAnimation);
            cameraAnimation = new AnimationClipInstance("StealDiamondNewCamera2",
                envController.Cameras.FirstOrDefault(cam => cam.gameObject.name == "CameraStealDiamondNew2").gameObject,
                //envController.Cameras.FirstOrDefault(cam => cam.gameObject.name == "CameraStealDiamondNew2-CloseUp").gameObject,
                false, false, false);
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName, cameraAnimation);

            // Load eye gaze
            EyeGazeEditor.LoadEyeGaze(timeline, bodyAnimationNormanInstanceId, LEAPCore.eyeGazeAnimationLayerName);
            EyeGazeEditor.PrintEyeGaze(timeline);

            // Prepare eye gaze inference evaluations
            ExtractEyeTrackFixationFrames(sceneName, timeline, testScenes.modelNormanNew, 1066, 1359);
            ComputeIntercoderReliabilityTargets(sceneName);
        }
        else if (sceneName == "StealDiamondNew-Edits")
        {
            testScenes.modelNormanNew.SetActive(true);
            testScenes.modelStealDiamondNewEnv.SetActive(true);

            // Add character models to the timeline
            timeline.OwningManager.AddModel(testScenes.modelNormanNew);

            // Set environment in the timeline
            timeline.OwningManager.SetEnvironment(testScenes.modelStealDiamondNewEnv);

            // Create animation instances
            var bodyAnimationNorman = new AnimationClipInstance("StealDiamondNew", testScenes.modelNormanNew);
            int bodyAnimationNormanInstanceId = timeline.AddAnimation(LEAPCore.baseAnimationLayerName,
                bodyAnimationNorman, 0, LEAPCore.helperAnimationLayerName);

            // Create environment animations
            var envController = testScenes.modelStealDiamondNewEnv.GetComponent<EnvironmentController>();
            var envAnimationGem = new AnimationClipInstance("StealDiamondNewGem",
                envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "Gem"),
                false, false, false);
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance(
                    "StealDiamondNewMarkers", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "Markers"),
                    false, false, false));
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName, envAnimationGem);
            var cameraAnimation = new AnimationClipInstance("StealDiamondNewCamera1",
                envController.Cameras.FirstOrDefault(cam => cam.gameObject.name == "CameraStealDiamondNew1").gameObject,
                false, false, false);
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName, cameraAnimation);
            cameraAnimation = new AnimationClipInstance("StealDiamondNewCamera2",
                envController.Cameras.FirstOrDefault(cam => cam.gameObject.name == "CameraStealDiamondNew2").gameObject,
                false, false, false);
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName, cameraAnimation);

            // Load eye gaze
            EyeGazeEditor.LoadEyeGaze(timeline, bodyAnimationNormanInstanceId, LEAPCore.eyeGazeAnimationLayerName, "#Edits");
            EyeGazeEditor.PrintEyeGaze(timeline);
        }
        else if (sceneName == "WaitForBusNew")
        {
            testScenes.modelNormanNew.SetActive(true);
            testScenes.modelWaitForBusNewEnv.SetActive(true);
            ModelUtil.FindChild(testScenes.modelWaitForBusNewEnv.transform, "Grass2").active = true;
            ModelUtil.FindChild(testScenes.modelWaitForBusNewEnv.transform, "Grass2-Edits1").active = false;
            ModelUtil.FindChild(testScenes.modelWaitForBusNewEnv.transform, "Grass2-Edits2").active = false;
            ModelUtil.FindChild(testScenes.modelWaitForBusNewEnv.transform, "Road").active = true;
            ModelUtil.FindChild(testScenes.modelWaitForBusNewEnv.transform, "Road-Edits1").active = false;
            ModelUtil.FindChild(testScenes.modelWaitForBusNewEnv.transform, "Road-Edits2").active = false;
            ModelUtil.FindChild(testScenes.modelWaitForBusNewEnv.transform, "Sidewalk").active = true;
            ModelUtil.FindChild(testScenes.modelWaitForBusNewEnv.transform, "Sidewalk-Edits1").active = false;
            ModelUtil.FindChild(testScenes.modelWaitForBusNewEnv.transform, "Sidewalk-Edits2").active = false;
            ModelUtil.FindChild(testScenes.modelWaitForBusNewEnv.transform, "Background").active = true;
            ModelUtil.FindChild(testScenes.modelWaitForBusNewEnv.transform, "Background-Edits1").active = false;
            ModelUtil.FindChild(testScenes.modelWaitForBusNewEnv.transform, "Background-Edits2").active = false;

            // Add character models to the timeline
            timeline.OwningManager.AddModel(testScenes.modelNormanNew);

            // Set environment in the timeline
            timeline.OwningManager.SetEnvironment(testScenes.modelWaitForBusNewEnv);

            // Create animation instances
            var bodyAnimationNorman = new AnimationClipInstance("WaitForBusNew", testScenes.modelNormanNew);

            // Add animations to characters
            int bodyAnimationNormanInstanceId = timeline.AddAnimation(LEAPCore.baseAnimationLayerName,
                bodyAnimationNorman, 0);

            // Create environment animations
            var envController = testScenes.modelWaitForBusNewEnv.GetComponent<EnvironmentController>();
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance("WaitForBusNewWatch", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "Watch"),
                    false, false, false));
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance(
                    "WaitForBusNewMarkers", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "Markers"),
                    false, false, false));

            // Load eye gaze
            EyeGazeEditor.LoadEyeGaze(timeline, bodyAnimationNormanInstanceId, LEAPCore.eyeGazeAnimationLayerName);
            EyeGazeEditor.PrintEyeGaze(timeline);

            // Prepare eye gaze inference evaluations
            ExtractEyeTrackFixationFrames(sceneName, timeline, testScenes.modelNormanNew, 590, 1782);
            ComputeIntercoderReliabilityInstances(sceneName, 131, 1323);
        }
        else if (sceneName == "WaitForBusNew-Edits1")
        {
            testScenes.modelNormanNew.SetActive(true);
            testScenes.modelWaitForBusNewEnv.SetActive(true);
            ModelUtil.FindChild(testScenes.modelWaitForBusNewEnv.transform, "Grass2").active = false;
            ModelUtil.FindChild(testScenes.modelWaitForBusNewEnv.transform, "Grass2-Edits1").active = true;
            ModelUtil.FindChild(testScenes.modelWaitForBusNewEnv.transform, "Grass2-Edits2").active = false;
            ModelUtil.FindChild(testScenes.modelWaitForBusNewEnv.transform, "Road").active = false;
            ModelUtil.FindChild(testScenes.modelWaitForBusNewEnv.transform, "Road-Edits1").active = true;
            ModelUtil.FindChild(testScenes.modelWaitForBusNewEnv.transform, "Road-Edits2").active = false;
            ModelUtil.FindChild(testScenes.modelWaitForBusNewEnv.transform, "Sidewalk").active = false;
            ModelUtil.FindChild(testScenes.modelWaitForBusNewEnv.transform, "Sidewalk-Edits1").active = true;
            ModelUtil.FindChild(testScenes.modelWaitForBusNewEnv.transform, "Sidewalk-Edits2").active = false;
            ModelUtil.FindChild(testScenes.modelWaitForBusNewEnv.transform, "Background").active = false;
            ModelUtil.FindChild(testScenes.modelWaitForBusNewEnv.transform, "Background-Edits1").active = true;
            ModelUtil.FindChild(testScenes.modelWaitForBusNewEnv.transform, "Background-Edits2").active = false;

            // Add character models to the timeline
            timeline.OwningManager.AddModel(testScenes.modelNormanNew);

            // Set environment in the timeline
            timeline.OwningManager.SetEnvironment(testScenes.modelWaitForBusNewEnv);

            // Create animation instances
            var bodyAnimationNorman = new AnimationClipInstance("WaitForBusNew", testScenes.modelNormanNew);

            // Add animations to characters
            int bodyAnimationNormanInstanceId = timeline.AddAnimation(LEAPCore.baseAnimationLayerName,
                bodyAnimationNorman, 0);

            // Create environment animations
            var envController = testScenes.modelWaitForBusNewEnv.GetComponent<EnvironmentController>();
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance("WaitForBusNewWatch",
                    envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "Watch"),
                    false, false, false));
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance(
                    "WaitForBusNewMarkers", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "Markers"),
                    false, false, false));

            // Load eye gaze
            EyeGazeEditor.LoadEyeGaze(timeline, bodyAnimationNormanInstanceId, LEAPCore.eyeGazeAnimationLayerName, "#Edits");
            EyeGazeEditor.PrintEyeGaze(timeline);
        }
        else if (sceneName == "WaitForBusNew-Edits2")
        {
            testScenes.modelNormanNew.SetActive(true);
            testScenes.modelWaitForBusNewEnv.SetActive(true);
            ModelUtil.FindChild(testScenes.modelWaitForBusNewEnv.transform, "Grass2").active = false;
            ModelUtil.FindChild(testScenes.modelWaitForBusNewEnv.transform, "Grass2-Edits1").active = false;
            ModelUtil.FindChild(testScenes.modelWaitForBusNewEnv.transform, "Grass2-Edits2").active = true;
            ModelUtil.FindChild(testScenes.modelWaitForBusNewEnv.transform, "Road").active = false;
            ModelUtil.FindChild(testScenes.modelWaitForBusNewEnv.transform, "Road-Edits1").active = false;
            ModelUtil.FindChild(testScenes.modelWaitForBusNewEnv.transform, "Road-Edits2").active = true;
            ModelUtil.FindChild(testScenes.modelWaitForBusNewEnv.transform, "Sidewalk").active = false;
            ModelUtil.FindChild(testScenes.modelWaitForBusNewEnv.transform, "Sidewalk-Edits1").active = false;
            ModelUtil.FindChild(testScenes.modelWaitForBusNewEnv.transform, "Sidewalk-Edits2").active = true;
            ModelUtil.FindChild(testScenes.modelWaitForBusNewEnv.transform, "Background").active = false;
            ModelUtil.FindChild(testScenes.modelWaitForBusNewEnv.transform, "Background-Edits1").active = false;
            ModelUtil.FindChild(testScenes.modelWaitForBusNewEnv.transform, "Background-Edits2").active = true;

            // Add character models to the timeline
            timeline.OwningManager.AddModel(testScenes.modelNormanNew);

            // Set environment in the timeline
            timeline.OwningManager.SetEnvironment(testScenes.modelWaitForBusNewEnv);

            // Create animation instances
            var bodyAnimationNorman = new AnimationClipInstance("WaitForBusNew", testScenes.modelNormanNew);

            // Add animations to characters
            int bodyAnimationNormanInstanceId = timeline.AddAnimation(LEAPCore.baseAnimationLayerName,
                bodyAnimationNorman, 0);

            // Create environment animations
            var envController = testScenes.modelWaitForBusNewEnv.GetComponent<EnvironmentController>();
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance("WaitForBusNewWatch",
                    envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "Watch"),
                    false, false, false));
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance(
                    "WaitForBusNewMarkers", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "Markers"),
                    false, false, false));

            // Load eye gaze
            EyeGazeEditor.LoadEyeGaze(timeline, bodyAnimationNormanInstanceId, LEAPCore.eyeGazeAnimationLayerName, "#Edits");
            EyeGazeEditor.PrintEyeGaze(timeline);
        }
        else if (sceneName == "StackBoxesNew")
        {
            testScenes.modelNormanNew.SetActive(true);
            testScenes.modelStackBoxesNewEnv.SetActive(true);

            // Add character models to the timeline
            timeline.OwningManager.AddModel(testScenes.modelNormanNew);

            // Set environment in the timeline
            timeline.OwningManager.SetEnvironment(testScenes.modelStackBoxesNewEnv);

            // Create animation instances
            var bodyAnimationNorman = new AnimationClipInstance("StackBoxesNew", testScenes.modelNormanNew);

            // Add animations to characters
            int bodyAnimationNormanInstanceId = timeline.AddAnimation(LEAPCore.baseAnimationLayerName,
                bodyAnimationNorman, 0);

            // Load eye gaze
            EyeGazeEditor.LoadEyeGaze(timeline, bodyAnimationNormanInstanceId, LEAPCore.eyeGazeAnimationLayerName);
            EyeGazeEditor.PrintEyeGaze(timeline);

            // Create environment animations
            var envController = testScenes.modelStackBoxesNewEnv.GetComponent<EnvironmentController>();
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance(
                    "StackBoxesNew1", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "pCube1"),
                    false, false, false));
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance(
                    "StackBoxesNew2", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "pCube2"),
                    false, false, false));
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance(
                    "StackBoxesNew3", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "pCube3"),
                    false, false, false));
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance(
                    "StackBoxesNew4", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "pCube4"),
                    false, false, false));
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance(
                    "StackBoxesNewMarkers", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "Markers"),
                    false, false, false));
            var cameraAnimation = new AnimationClipInstance("StackBoxesNewCamera1",
                envController.Cameras.FirstOrDefault(cam => cam.gameObject.name.StartsWith("CameraStackBoxesNew1") &&
                cam.gameObject.active).gameObject,
                false, false, false);
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName, cameraAnimation);
            cameraAnimation = new AnimationClipInstance("StackBoxesNewCamera2",
                envController.Cameras.FirstOrDefault(cam => cam.gameObject.name.StartsWith("CameraStackBoxesNew2") &&
                cam.gameObject.active).gameObject,
                false, false, false);
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName, cameraAnimation);

            // Extract eye tracking fixation frames
            ExtractEyeTrackFixationFrames(sceneName, timeline, testScenes.modelNormanNew, 416, 1651);
        }
        else if (sceneName == "ChatWithFriend")
        {
            testScenes.modelNormanNew.SetActive(true);
            testScenes.modelRomanTall.SetActive(true);
            testScenes.modelChatWithFriendEnv.SetActive(true);

            // Add character models to the timeline
            timeline.OwningManager.AddModel(testScenes.modelNormanNew);
            timeline.OwningManager.AddModel(testScenes.modelRomanTall);

            // Set environment in the timeline
            timeline.OwningManager.SetEnvironment(testScenes.modelChatWithFriendEnv);

            // Create animation instances
            var bodyAnimationNorman = new AnimationClipInstance("ChatWithFriend", testScenes.modelNormanNew);
            int bodyAnimationNormanInstanceId = timeline.AddAnimation(LEAPCore.baseAnimationLayerName,
                bodyAnimationNorman, 0, LEAPCore.helperAnimationLayerName);
            var bodyAnimationRoman = new AnimationClipInstance("InitialPose", testScenes.modelRomanTall);
            int bodyAnimationRomanInstanceId = timeline.AddAnimation(LEAPCore.baseAnimationLayerName,
                bodyAnimationRoman, 0, LEAPCore.helperAnimationLayerName);

            // Create environment animations
            var envController = testScenes.modelChatWithFriendEnv.GetComponent<EnvironmentController>();
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance(
                    "ChatWithFriendMarkers", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "Markers"),
                    false, false, false));

            // Load eye gaze
            EyeGazeEditor.LoadEyeGaze(timeline, bodyAnimationNormanInstanceId, LEAPCore.eyeGazeAnimationLayerName);
            EyeGazeEditor.LoadEyeGaze(timeline, bodyAnimationRomanInstanceId, LEAPCore.eyeGazeAnimationLayerName);
            EyeGazeEditor.PrintEyeGaze(timeline);

            // Extract eye tracking fixation frames
            ExtractEyeTrackFixationFrames(sceneName, timeline, testScenes.modelNormanNew, 967, 2868);
        }
        else if (sceneName == "ChatWithFriend-Edits1")
        {
            testScenes.modelNormanNew.SetActive(true);
            testScenes.modelRomanTall.SetActive(true);
            testScenes.modelChatWithFriendEnv.SetActive(true);

            // Add character models to the timeline
            timeline.OwningManager.AddModel(testScenes.modelNormanNew);
            timeline.OwningManager.AddModel(testScenes.modelRomanTall);

            // Set environment in the timeline
            timeline.OwningManager.SetEnvironment(testScenes.modelChatWithFriendEnv);

            // Create animation instances
            var bodyAnimationNorman = new AnimationClipInstance("ChatWithFriend", testScenes.modelNormanNew);
            int bodyAnimationNormanInstanceId = timeline.AddAnimation(LEAPCore.baseAnimationLayerName,
                bodyAnimationNorman, 0, LEAPCore.helperAnimationLayerName);
            var bodyAnimationRoman = new AnimationClipInstance("InitialPose", testScenes.modelRomanTall);
            int bodyAnimationRomanInstanceId = timeline.AddAnimation(LEAPCore.baseAnimationLayerName,
                bodyAnimationRoman, 0, LEAPCore.helperAnimationLayerName);

            // Create environment animations
            var envController = testScenes.modelChatWithFriendEnv.GetComponent<EnvironmentController>();
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance(
                    "ChatWithFriendMarkers", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "Markers"),
                    false, false, false));

            // Load eye gaze
            EyeGazeEditor.LoadEyeGaze(timeline, bodyAnimationNormanInstanceId, LEAPCore.eyeGazeAnimationLayerName, "#Edits1");
            EyeGazeEditor.PrintEyeGaze(timeline);
        }
        else if (sceneName == "ChatWithFriend-Edits2")
        {
            testScenes.modelNormanNew.SetActive(true);
            testScenes.modelRomanTall.SetActive(true);
            testScenes.modelChatWithFriendEnv.SetActive(true);

            // Add character models to the timeline
            timeline.OwningManager.AddModel(testScenes.modelNormanNew);
            timeline.OwningManager.AddModel(testScenes.modelRomanTall);

            // Set environment in the timeline
            timeline.OwningManager.SetEnvironment(testScenes.modelChatWithFriendEnv);

            // Create animation instances
            var bodyAnimationNorman = new AnimationClipInstance("ChatWithFriend", testScenes.modelNormanNew);
            int bodyAnimationNormanInstanceId = timeline.AddAnimation(LEAPCore.baseAnimationLayerName,
                bodyAnimationNorman, 0, LEAPCore.helperAnimationLayerName);
            var bodyAnimationRoman = new AnimationClipInstance("InitialPose", testScenes.modelRomanTall);
            int bodyAnimationRomanInstanceId = timeline.AddAnimation(LEAPCore.baseAnimationLayerName,
                bodyAnimationRoman, 0, LEAPCore.helperAnimationLayerName);

            // Create environment animations
            var envController = testScenes.modelChatWithFriendEnv.GetComponent<EnvironmentController>();
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance(
                    "ChatWithFriendMarkers", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "Markers"),
                    false, false, false));

            // Load eye gaze
            EyeGazeEditor.LoadEyeGaze(timeline, bodyAnimationNormanInstanceId, LEAPCore.eyeGazeAnimationLayerName, "#Edits2");
            EyeGazeEditor.PrintEyeGaze(timeline);
        }
        else if (sceneName == "MakeSandwich")
        {
            testScenes.modelNormanNew.SetActive(true);
            testScenes.modelMakeSandwichEnv.SetActive(true);
            ModelUtil.FindChild(testScenes.modelMakeSandwichEnv.transform, "Floor").active = true;
            ModelUtil.FindChild(testScenes.modelMakeSandwichEnv.transform, "Background").active = true;
            ModelUtil.FindChild(testScenes.modelMakeSandwichEnv.transform, "Floor-Edits").active = false;
            ModelUtil.FindChild(testScenes.modelMakeSandwichEnv.transform, "Background-Edits").active = false;

            // Add character models to the timeline
            timeline.OwningManager.AddModel(testScenes.modelNormanNew);

            // Set environment in the timeline
            timeline.OwningManager.SetEnvironment(testScenes.modelMakeSandwichEnv);

            // Create animation instances
            var bodyAnimationNorman = new AnimationClipInstance("MakeSandwich", testScenes.modelNormanNew);
            int bodyAnimationNormanInstanceId = timeline.AddAnimation(LEAPCore.baseAnimationLayerName,
                bodyAnimationNorman, 0, LEAPCore.helperAnimationLayerName);

            // Load eye gaze
            EyeGazeEditor.LoadEyeGaze(timeline, bodyAnimationNormanInstanceId, LEAPCore.eyeGazeAnimationLayerName);
            EyeGazeEditor.PrintEyeGaze(timeline);

            // Create environment animations
            var envController = testScenes.modelMakeSandwichEnv.GetComponent<EnvironmentController>();
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance("MakeSandwichBread1", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "Bread1"),
                    false, false, false));
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance("MakeSandwichBread2", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "Bread2"),
                    false, false, false));
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance("MakeSandwichBacon", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "Bacon"),
                    false, false, false));
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance("MakeSandwichLettuce", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "Lettuce"),
                    false, false, false));
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance("MakeSandwichTomato", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "Tomato"),
                    false, false, false));
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance("MakeSandwichSwiss", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "Swiss"),
                    false, false, false));
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance("MakeSandwichTurkey", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "Turkey"),
                    false, false, false));
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance(
                    "MakeSandwichMarkers", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "Markers"),
                    false, false, false));

            // Prepare eye gaze inference evaluations
            ExtractEyeTrackFixationFrames(sceneName, timeline, testScenes.modelNormanNew, 819, 1834);
            ComputeIntercoderReliabilityTargets(sceneName);
        }
        else if (sceneName == "MakeSandwich-Edits")
        {
            testScenes.modelNormanNew.SetActive(true);
            testScenes.modelMakeSandwichEnv.SetActive(true);
            ModelUtil.FindChild(testScenes.modelMakeSandwichEnv.transform, "Floor").active = false;
            ModelUtil.FindChild(testScenes.modelMakeSandwichEnv.transform, "Background").active = false;
            ModelUtil.FindChild(testScenes.modelMakeSandwichEnv.transform, "Floor-Edits").active = true;
            ModelUtil.FindChild(testScenes.modelMakeSandwichEnv.transform, "Background-Edits").active = true;

            // Add character models to the timeline
            timeline.OwningManager.AddModel(testScenes.modelNormanNew);

            // Set environment in the timeline
            timeline.OwningManager.SetEnvironment(testScenes.modelMakeSandwichEnv);

            // Create animation instances
            var bodyAnimationNorman = new AnimationClipInstance("MakeSandwich", testScenes.modelNormanNew);
            int bodyAnimationNormanInstanceId = timeline.AddAnimation(LEAPCore.baseAnimationLayerName,
                bodyAnimationNorman, 0, LEAPCore.helperAnimationLayerName);

            // Load eye gaze
            EyeGazeEditor.LoadEyeGaze(timeline, bodyAnimationNormanInstanceId, LEAPCore.eyeGazeAnimationLayerName, "#Edits");
            EyeGazeEditor.PrintEyeGaze(timeline);

            // Create environment animations
            var envController = testScenes.modelMakeSandwichEnv.GetComponent<EnvironmentController>();
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance("MakeSandwichBread1", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "Bread1"),
                    false, false, false));
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance("MakeSandwichBread2", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "Bread2"),
                    false, false, false));
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance("MakeSandwichBacon", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "Bacon"),
                    false, false, false));
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance("MakeSandwichLettuce", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "Lettuce"),
                    false, false, false));
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance("MakeSandwichTomato", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "Tomato"),
                    false, false, false));
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance("MakeSandwichSwiss", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "Swiss"),
                    false, false, false));
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance("MakeSandwichTurkey", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "Turkey"),
                    false, false, false));
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance(
                    "MakeSandwichMarkers", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "Markers"),
                    false, false, false));
        }
        else if (sceneName == "MakeSandwichDemo")
        {
            testScenes.modelNormanNew.SetActive(true);
            testScenes.modelMakeSandwichDemoEnv.SetActive(true);
            ModelUtil.FindChild(testScenes.modelMakeSandwichDemoEnv.transform, "Screen").active = true;
            ModelUtil.FindChild(testScenes.modelMakeSandwichDemoEnv.transform, "Screen-Edits").active = false;

            // Add character models to the timeline
            timeline.OwningManager.AddModel(testScenes.modelNormanNew);

            // Set environment in the timeline
            timeline.OwningManager.SetEnvironment(testScenes.modelMakeSandwichDemoEnv);

            // Create animation instances
            var bodyAnimationNorman = new AnimationClipInstance("MakeSandwichDemo", testScenes.modelNormanNew);
            int bodyAnimationNormanInstanceId = timeline.AddAnimation(LEAPCore.baseAnimationLayerName,
                bodyAnimationNorman, 0, LEAPCore.helperAnimationLayerName);

            // Load eye gaze
            EyeGazeEditor.LoadEyeGaze(timeline, bodyAnimationNormanInstanceId, LEAPCore.eyeGazeAnimationLayerName);
            EyeGazeEditor.PrintEyeGaze(timeline);

            // Create environment animations
            var envController = testScenes.modelMakeSandwichDemoEnv.GetComponent<EnvironmentController>();
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance("MakeSandwichDemoBread1", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "Bread1"),
                    false, false, false));
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance("MakeSandwichDemoBread2", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "Bread2"),
                    false, false, false));
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance("MakeSandwichDemoBacon", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "Bacon"),
                    false, false, false));
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance("MakeSandwichDemoLettuce", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "Lettuce"),
                    false, false, false));
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance("MakeSandwichDemoTomato", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "Tomato"),
                    false, false, false));
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance("MakeSandwichDemoSwiss", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "Swiss"),
                    false, false, false));
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance("MakeSandwichDemoTurkey", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "Turkey"),
                    false, false, false));
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance("MakeSandwichDemoCamera1", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "Screen"),
                    false, false, false));
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance("MakeSandwichDemoCamera2", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "Screen-Edits"),
                    false, false, false));
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance(
                    "MakeSandwichDemoMarkers", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "Markers"),
                    false, false, false));

            // Prepare eye gaze inference evaluations
            ExtractEyeTrackFixationFrames(sceneName, timeline, testScenes.modelNormanNew, 679, 2686);
            ComputeIntercoderReliabilityTargets(sceneName);
        }
        else if (sceneName == "MakeSandwichDemo-Edits")
        {
            testScenes.modelNormanNew.SetActive(true);
            testScenes.modelMakeSandwichDemoEnv.SetActive(true);
            ModelUtil.FindChild(testScenes.modelMakeSandwichDemoEnv.transform, "Screen").active = false;
            ModelUtil.FindChild(testScenes.modelMakeSandwichDemoEnv.transform, "Screen-Edits").active = true;

            // Add character models to the timeline
            timeline.OwningManager.AddModel(testScenes.modelNormanNew);

            // Set environment in the timeline
            timeline.OwningManager.SetEnvironment(testScenes.modelMakeSandwichDemoEnv);

            // Create animation instances
            var bodyAnimationNorman = new AnimationClipInstance("MakeSandwichDemo", testScenes.modelNormanNew);
            int bodyAnimationNormanInstanceId = timeline.AddAnimation(LEAPCore.baseAnimationLayerName,
                bodyAnimationNorman, 0, LEAPCore.helperAnimationLayerName);

            // Load eye gaze
            EyeGazeEditor.LoadEyeGaze(timeline, bodyAnimationNormanInstanceId, LEAPCore.eyeGazeAnimationLayerName, "#Edits");
            EyeGazeEditor.PrintEyeGaze(timeline);

            // Create environment animations
            var envController = testScenes.modelMakeSandwichDemoEnv.GetComponent<EnvironmentController>();
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance("MakeSandwichDemoBread1", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "Bread1"),
                    false, false, false));
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance("MakeSandwichDemoBread2", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "Bread2"),
                    false, false, false));
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance("MakeSandwichDemoBacon", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "Bacon"),
                    false, false, false));
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance("MakeSandwichDemoLettuce", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "Lettuce"),
                    false, false, false));
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance("MakeSandwichDemoTomato", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "Tomato"),
                    false, false, false));
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance("MakeSandwichDemoSwiss", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "Swiss"),
                    false, false, false));
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance("MakeSandwichDemoTurkey", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "Turkey"),
                    false, false, false));
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance("MakeSandwichDemoCamera1", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "Screen"),
                    false, false, false));
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance("MakeSandwichDemoCamera2", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "Screen-Edits"),
                    false, false, false));
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance(
                    "MakeSandwichDemoMarkers", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "Markers"),
                    false, false, false));
        }
        else if (sceneName == "WalkConesNew-GroundTruth")
        {
            testScenes.modelNormanNew.SetActive(true);
            testScenes.modelWalkConesNewEnv.SetActive(true);

            // Add character models to the timeline
            timeline.OwningManager.AddModel(testScenes.modelNormanNew);

            // Set environment in the timeline
            timeline.OwningManager.SetEnvironment(testScenes.modelWalkConesNewEnv);

            // Create animation instances
            var bodyAnimationNorman = new AnimationClipInstance("WalkConesNew-GroundTruth", testScenes.modelNormanNew);

            // Add animations to characters
            int bodyAnimationNormanInstanceId = timeline.AddAnimation(LEAPCore.baseAnimationLayerName,
                bodyAnimationNorman, 0);

            // Create environment animations
            var envController = testScenes.modelWalkConesNewEnv.GetComponent<EnvironmentController>();
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance(
                    "WalkConesNewMarkers", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "Markers"),
                    false, false, false));
            var root = ModelUtil.FindBoneWithTag(testScenes.modelNormanNew.transform, "RootBone");
            var cameraAnimation = new LookAtFrontInstance("WalkConesCamera",
                envController.Cameras.FirstOrDefault(cam => cam.gameObject.name == "CameraWalkConesNew").gameObject,
                bodyAnimationNorman.FrameLength, root, 7.6f, 5.2f, 3.2f);
            timeline.AddEnvironmentAnimation(LEAPCore.cameraAnimationLayerName, cameraAnimation);
        }
        else if (sceneName == "StealDiamondNew-GroundTruth")
        {
            testScenes.modelNormanNew.SetActive(true);
            testScenes.modelStealDiamondNewEnv.SetActive(true);

            // Add character models to the timeline
            timeline.OwningManager.AddModel(testScenes.modelNormanNew);

            // Set environment in the timeline
            timeline.OwningManager.SetEnvironment(testScenes.modelStealDiamondNewEnv);

            // Create animation instances
            var bodyAnimationNorman = new AnimationClipInstance("StealDiamondNew-GroundTruth", testScenes.modelNormanNew);
            int bodyAnimationNormanInstanceId = timeline.AddAnimation(LEAPCore.baseAnimationLayerName,
                bodyAnimationNorman, 0, LEAPCore.helperAnimationLayerName);

            // Create environment animations
            var envController = testScenes.modelStealDiamondNewEnv.GetComponent<EnvironmentController>();
            var envAnimationGem = new AnimationClipInstance("StealDiamondNewGem",
                envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "Gem"),
                false, false, false);
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName, envAnimationGem);
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance(
                    "StealDiamondNewMarkers", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "Markers"),
                    false, false, false));
            var cameraAnimation = new AnimationClipInstance("StealDiamondNewCamera1",
                envController.Cameras.FirstOrDefault(cam => cam.gameObject.name == "CameraStealDiamondNew1").gameObject,
                //envController.Cameras.FirstOrDefault(cam => cam.gameObject.name == "CameraStealDiamondNew1-CloseUp").gameObject,
                false, false, false);
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName, cameraAnimation);
            cameraAnimation = new AnimationClipInstance("StealDiamondNewCamera2",
                envController.Cameras.FirstOrDefault(cam => cam.gameObject.name == "CameraStealDiamondNew2").gameObject,
                //envController.Cameras.FirstOrDefault(cam => cam.gameObject.name == "CameraStealDiamondNew2-CloseUp").gameObject,
                false, false, false);
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName, cameraAnimation);
        }
        else if (sceneName == "StackBoxesNew-GroundTruth")
        {
            testScenes.modelNormanNew.SetActive(true);
            testScenes.modelStackBoxesNewEnv.SetActive(true);

            // Add character models to the timeline
            timeline.OwningManager.AddModel(testScenes.modelNormanNew);

            // Set environment in the timeline
            timeline.OwningManager.SetEnvironment(testScenes.modelStackBoxesNewEnv);

            // Create animation instances
            var bodyAnimationNorman = new AnimationClipInstance("StackBoxesNew-GroundTruth", testScenes.modelNormanNew);

            // Add animations to characters
            int bodyAnimationNormanInstanceId = timeline.AddAnimation(LEAPCore.baseAnimationLayerName,
                bodyAnimationNorman, 0);

            // Create environment animations
            var envController = testScenes.modelStackBoxesNewEnv.GetComponent<EnvironmentController>();
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance(
                    "StackBoxesNew1", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "pCube1"),
                    false, false, false));
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance(
                    "StackBoxesNew2", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "pCube2"),
                    false, false, false));
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance(
                    "StackBoxesNew3", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "pCube3"),
                    false, false, false));
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance(
                    "StackBoxesNew4", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "pCube4"),
                    false, false, false));
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance(
                    "StackBoxesNewMarkers", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "Markers"),
                    false, false, false));
            var cameraAnimation = new AnimationClipInstance("StackBoxesNewCamera1",
                envController.Cameras.FirstOrDefault(cam => cam.gameObject.name.StartsWith("CameraStackBoxesNew1") &&
                cam.gameObject.active).gameObject,
                false, false, false);
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName, cameraAnimation);
            cameraAnimation = new AnimationClipInstance("StackBoxesNewCamera2",
                envController.Cameras.FirstOrDefault(cam => cam.gameObject.name.StartsWith("CameraStackBoxesNew2") &&
                cam.gameObject.active).gameObject,
                false, false, false);
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName, cameraAnimation);
        }
        else if (sceneName == "MakeSandwich-GroundTruth")
        {
            testScenes.modelNormanNew.SetActive(true);
            testScenes.modelMakeSandwichEnv.SetActive(true);
            ModelUtil.FindChild(testScenes.modelMakeSandwichEnv.transform, "Floor").active = true;
            ModelUtil.FindChild(testScenes.modelMakeSandwichEnv.transform, "Background").active = true;
            ModelUtil.FindChild(testScenes.modelMakeSandwichEnv.transform, "Floor-Edits").active = false;
            ModelUtil.FindChild(testScenes.modelMakeSandwichEnv.transform, "Background-Edits").active = false;

            // Add character models to the timeline
            timeline.OwningManager.AddModel(testScenes.modelNormanNew);

            // Set environment in the timeline
            timeline.OwningManager.SetEnvironment(testScenes.modelMakeSandwichEnv);

            // Create animation instances
            var bodyAnimationNorman = new AnimationClipInstance("MakeSandwich-GroundTruth", testScenes.modelNormanNew);
            int bodyAnimationNormanInstanceId = timeline.AddAnimation(LEAPCore.baseAnimationLayerName,
                bodyAnimationNorman, 0, LEAPCore.helperAnimationLayerName);

            // Create environment animations
            var envController = testScenes.modelMakeSandwichEnv.GetComponent<EnvironmentController>();
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance("MakeSandwichBread1", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "Bread1"),
                    false, false, false));
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance("MakeSandwichBread2", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "Bread2"),
                    false, false, false));
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance("MakeSandwichBacon", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "Bacon"),
                    false, false, false));
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance("MakeSandwichLettuce", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "Lettuce"),
                    false, false, false));
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance("MakeSandwichTomato", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "Tomato"),
                    false, false, false));
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance("MakeSandwichSwiss", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "Swiss"),
                    false, false, false));
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance("MakeSandwichTurkey", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "Turkey"),
                    false, false, false));
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance(
                    "MakeSandwichMarkers", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "Markers"),
                    false, false, false));
        }
        else if (sceneName == "ChatWithFriend-GroundTruth")
        {
            testScenes.modelNormanNew.SetActive(true);
            testScenes.modelRomanTall.SetActive(true);
            testScenes.modelChatWithFriendEnv.SetActive(true);

            // Add character models to the timeline
            timeline.OwningManager.AddModel(testScenes.modelNormanNew);
            timeline.OwningManager.AddModel(testScenes.modelRomanTall);

            // Set environment in the timeline
            timeline.OwningManager.SetEnvironment(testScenes.modelChatWithFriendEnv);

            // Create animation instances
            var bodyAnimationNorman = new AnimationClipInstance("ChatWithFriend-GroundTruth", testScenes.modelNormanNew);
            int bodyAnimationNormanInstanceId = timeline.AddAnimation(LEAPCore.baseAnimationLayerName,
                bodyAnimationNorman, 0, LEAPCore.helperAnimationLayerName);
            var bodyAnimationRoman = new AnimationClipInstance("InitialPose", testScenes.modelRomanTall);
            int bodyAnimationRomanInstanceId = timeline.AddAnimation(LEAPCore.baseAnimationLayerName,
                bodyAnimationRoman, 0, LEAPCore.helperAnimationLayerName);

            // Create environment animations
            var envController = testScenes.modelChatWithFriendEnv.GetComponent<EnvironmentController>();
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance(
                    "ChatWithFriendMarkers", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "Markers"),
                    false, false, false));
        }
        else if (sceneName == "PassSoda-Eyes")
        {
            testScenes.modelNorman.SetActive(true);
            testScenes.modelRoman.SetActive(true);
            //testScenes.modelNormanette.SetActive(true);
            testScenes.modelNormanette.SetActive(false);
            testScenes.modelNormanette.transform.position = new Vector3(-4.97f, 0f, 1.24f);
            testScenes.modelNormanette.transform.localScale = new Vector3(0.96f, 0.91f, 0.96f);
            testScenes.modelPassSodaEnv.SetActive(true);

            // Add character models to the timeline
            timeline.OwningManager.AddModel(testScenes.modelNorman);
            timeline.OwningManager.AddModel(testScenes.modelNormanette);
            timeline.OwningManager.AddModel(testScenes.modelRoman);

            // Set environment in the timeline
            timeline.OwningManager.SetEnvironment(testScenes.modelPassSodaEnv);

            // Create animation instances
            var bodyAnimationNorman = new AnimationClipInstance("PassSodaA-Eyes", testScenes.modelNorman);
            int bodyAnimationNormanInstanceId = timeline.AddAnimation(LEAPCore.baseAnimationLayerName,
                bodyAnimationNorman, 0, LEAPCore.helperAnimationLayerName);
            var bodyAnimationRoman = new AnimationClipInstance("PassSodaB-Eyes", testScenes.modelRoman);
            int bodyAnimationRomanInstanceId = timeline.AddAnimation(LEAPCore.baseAnimationLayerName,
                bodyAnimationRoman, 0, LEAPCore.helperAnimationLayerName);
            var bodyAnimationNormanette = new AnimationClipInstance("PassSodaC", testScenes.modelNormanette);
            timeline.AddAnimation(LEAPCore.baseAnimationLayerName, bodyAnimationNormanette, 0);

            // Create environment animations
            var envController = testScenes.modelPassSodaEnv.GetComponent<EnvironmentController>();
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance("PassSodaBottle", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "SodaBottle"),
                    false, false, false));
        }
        else if (sceneName == "HandShake-Eyes")
        {
            testScenes.modelNorman.SetActive(true);
            testScenes.modelRoman.SetActive(true);
            testScenes.modelHandShakeEnv.SetActive(true);

            // Add character models to the timeline
            timeline.OwningManager.AddModel(testScenes.modelNorman);
            timeline.OwningManager.AddModel(testScenes.modelRoman);

            // Set environment in the timeline
            timeline.OwningManager.SetEnvironment(testScenes.modelHandShakeEnv);

            // Create animation instances
            var bodyAnimationNorman = new AnimationClipInstance("HandShakeA-Eyes", testScenes.modelNorman);
            int bodyAnimationNormanInstanceId = timeline.AddAnimation(LEAPCore.baseAnimationLayerName,
                bodyAnimationNorman, 0, LEAPCore.helperAnimationLayerName);
            var bodyAnimationRoman = new AnimationClipInstance("HandShakeB-Eyes", testScenes.modelRoman);
            int bodyAnimationRomanInstanceId = timeline.AddAnimation(LEAPCore.baseAnimationLayerName,
                bodyAnimationRoman, 0, LEAPCore.helperAnimationLayerName);

            // Create camera animations
            var envController = testScenes.modelHandShakeEnv.GetComponent<EnvironmentController>();
            var cameraAnimation = new AnimationClipInstance("HandShakeCamera",
                envController.Cameras.FirstOrDefault(cam => cam.gameObject.name == "CameraHandShake").gameObject,
                false, false, false);
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName, cameraAnimation);
        }
        else if (sceneName == "WalkConesNew-Eyes")
        {
            testScenes.modelNormanElliot.SetActive(true);
            testScenes.modelWalkConesNewEnv.SetActive(true);

            // Add character models to the timeline
            timeline.OwningManager.AddModel(testScenes.modelNormanElliot);

            // Set environment in the timeline
            timeline.OwningManager.SetEnvironment(testScenes.modelWalkConesNewEnv);

            // Create animation instances
            var bodyAnimationNorman = new AnimationClipInstance("WalkConesNew-Eyes", testScenes.modelNormanElliot);

            // Add animations to characters
            int bodyAnimationNormanInstanceId = timeline.AddAnimation(LEAPCore.baseAnimationLayerName,
                bodyAnimationNorman, 0);

            // Create environment animations
            var envController = testScenes.modelWalkConesNewEnv.GetComponent<EnvironmentController>();
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance(
                    "WalkConesNewMarkers", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "Markers"),
                    false, false, false));
            var root = ModelUtil.FindBoneWithTag(testScenes.modelNormanElliot.transform, "RootBone");
            var cameraAnimation = new LookAtFrontInstance("WalkConesCamera",
                envController.Cameras.FirstOrDefault(cam => cam.gameObject.name == "CameraWalkConesNew").gameObject,
                bodyAnimationNorman.FrameLength, root, 7.6f, 5.2f, 3.2f);
            timeline.AddEnvironmentAnimation(LEAPCore.cameraAnimationLayerName, cameraAnimation);
        }
        else if (sceneName == "StealDiamondNew-Eyes")
        {
            testScenes.modelNormanElliot.SetActive(true);
            testScenes.modelStealDiamondNewEnv.SetActive(true);

            // Add character models to the timeline
            timeline.OwningManager.AddModel(testScenes.modelNormanElliot);

            // Set environment in the timeline
            timeline.OwningManager.SetEnvironment(testScenes.modelStealDiamondNewEnv);

            // Create animation instances
            var bodyAnimationNorman = new AnimationClipInstance("StealDiamondNew-Eyes", testScenes.modelNormanElliot);
            int bodyAnimationNormanInstanceId = timeline.AddAnimation(LEAPCore.baseAnimationLayerName,
                bodyAnimationNorman, 0, LEAPCore.helperAnimationLayerName);

            // Create environment animations
            var envController = testScenes.modelStealDiamondNewEnv.GetComponent<EnvironmentController>();
            var envAnimationGem = new AnimationClipInstance("StealDiamondNewGem",
                envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "Gem"),
                false, false, false);
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance(
                    "StealDiamondNewMarkers", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "Markers"),
                    false, false, false));
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName, envAnimationGem);
            var cameraAnimation = new AnimationClipInstance("StealDiamondNewCamera1",
                envController.Cameras.FirstOrDefault(cam => cam.gameObject.name.StartsWith("CameraStealDiamondNew1") &&
                cam.gameObject.active).gameObject,
                false, false, false);
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName, cameraAnimation);
            cameraAnimation = new AnimationClipInstance("StealDiamondNewCamera2",
                envController.Cameras.FirstOrDefault(cam => cam.gameObject.name.StartsWith("CameraStealDiamondNew2") &&
                cam.gameObject.active).gameObject,
                false, false, false);
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName, cameraAnimation);
        }
        else if (sceneName == "StackBoxesNew-Eyes")
        {
            testScenes.modelNormanElliot.SetActive(true);
            testScenes.modelStackBoxesNewEnv.SetActive(true);

            // Add character models to the timeline
            timeline.OwningManager.AddModel(testScenes.modelNormanElliot);

            // Set environment in the timeline
            timeline.OwningManager.SetEnvironment(testScenes.modelStackBoxesNewEnv);

            // Create animation instances
            var bodyAnimationNorman = new AnimationClipInstance("StackBoxesNew-Eyes", testScenes.modelNormanElliot);

            // Add animations to characters
            int bodyAnimationNormanInstanceId = timeline.AddAnimation(LEAPCore.baseAnimationLayerName,
                bodyAnimationNorman, 0);

            // Create environment animations
            var envController = testScenes.modelStackBoxesNewEnv.GetComponent<EnvironmentController>();
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance(
                    "StackBoxesNew1", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "pCube1"),
                    false, false, false));
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance(
                    "StackBoxesNew2", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "pCube2"),
                    false, false, false));
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance(
                    "StackBoxesNew3", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "pCube3"),
                    false, false, false));
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance(
                    "StackBoxesNew4", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "pCube4"),
                    false, false, false));
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance(
                    "StackBoxesNewMarkers", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "Markers"),
                    false, false, false));
            var cameraAnimation = new AnimationClipInstance("StackBoxesNewCamera1",
                envController.Cameras.FirstOrDefault(cam => cam.gameObject.name.StartsWith("CameraStackBoxesNew1") &&
                cam.gameObject.active).gameObject,
                false, false, false);
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName, cameraAnimation);
            cameraAnimation = new AnimationClipInstance("StackBoxesNewCamera2",
                envController.Cameras.FirstOrDefault(cam => cam.gameObject.name.StartsWith("CameraStackBoxesNew2") &&
                cam.gameObject.active).gameObject,
                false, false, false);
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName, cameraAnimation);
        }
        else if (sceneName == "ChatWithFriend-Eyes")
        {
            testScenes.modelNormanElliot.SetActive(true);
            testScenes.modelRomanTall.SetActive(true);
            testScenes.modelChatWithFriendEnv.SetActive(true);

            // Add character models to the timeline
            timeline.OwningManager.AddModel(testScenes.modelNormanElliot);
            timeline.OwningManager.AddModel(testScenes.modelRomanTall);

            // Set environment in the timeline
            timeline.OwningManager.SetEnvironment(testScenes.modelChatWithFriendEnv);

            // Create animation instances
            var bodyAnimationNorman = new AnimationClipInstance("ChatWithFriend-Eyes", testScenes.modelNormanElliot);
            int bodyAnimationNormanInstanceId = timeline.AddAnimation(LEAPCore.baseAnimationLayerName,
                bodyAnimationNorman, 0, LEAPCore.helperAnimationLayerName);
            var bodyAnimationRoman = new AnimationClipInstance("InitialPose", testScenes.modelRomanTall);
            int bodyAnimationRomanInstanceId = timeline.AddAnimation(LEAPCore.baseAnimationLayerName,
                bodyAnimationRoman, 0, LEAPCore.helperAnimationLayerName);

            // Create environment animations
            var envController = testScenes.modelChatWithFriendEnv.GetComponent<EnvironmentController>();
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance(
                    "ChatWithFriendMarkers", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "Markers"),
                    false, false, false));
        }
        else if (sceneName == "MakeSandwich-Eyes")
        {
            testScenes.modelNormanElliot.SetActive(true);
            testScenes.modelMakeSandwichEnv.SetActive(true);

            // Add character models to the timeline
            timeline.OwningManager.AddModel(testScenes.modelNormanElliot);

            // Set environment in the timeline
            timeline.OwningManager.SetEnvironment(testScenes.modelMakeSandwichEnv);

            // Create animation instances
            var bodyAnimationNorman = new AnimationClipInstance("MakeSandwich-Eyes", testScenes.modelNormanElliot);
            int bodyAnimationNormanInstanceId = timeline.AddAnimation(LEAPCore.baseAnimationLayerName,
                bodyAnimationNorman, 0, LEAPCore.helperAnimationLayerName);

            // Load eye gaze
            EyeGazeEditor.LoadEyeGaze(timeline, bodyAnimationNormanInstanceId, LEAPCore.eyeGazeAnimationLayerName);
            EyeGazeEditor.PrintEyeGaze(timeline);

            // Create environment animations
            var envController = testScenes.modelMakeSandwichEnv.GetComponent<EnvironmentController>();
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance("MakeSandwichBread1", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "Bread1"),
                    false, false, false));
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance("MakeSandwichBread2", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "Bread2"),
                    false, false, false));
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance("MakeSandwichBacon", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "Bacon"),
                    false, false, false));
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance("MakeSandwichLettuce", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "Lettuce"),
                    false, false, false));
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance("MakeSandwichTomato", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "Tomato"),
                    false, false, false));
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance("MakeSandwichSwiss", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "Swiss"),
                    false, false, false));
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance("MakeSandwichTurkey", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "Turkey"),
                    false, false, false));
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance(
                    "MakeSandwichMarkers", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "Markers"),
                    false, false, false));
        }
        else if (sceneName == "PassSoda-HandEdits")
        {
            testScenes.modelNorman.SetActive(true);
            testScenes.modelRoman.SetActive(true);
            testScenes.modelNormanette.SetActive(true);
            testScenes.modelNormanette.transform.position = new Vector3(-4.97f, 0f, 1.24f);
            testScenes.modelNormanette.transform.localScale = new Vector3(0.96f, 0.91f, 0.96f);
            testScenes.modelPassSodaEnv.SetActive(true);

            // Add character models to the timeline
            timeline.OwningManager.AddModel(testScenes.modelNorman);
            timeline.OwningManager.AddModel(testScenes.modelNormanette);
            timeline.OwningManager.AddModel(testScenes.modelRoman);

            // Set environment in the timeline
            timeline.OwningManager.SetEnvironment(testScenes.modelPassSodaEnv);

            // Create animation instances
            var bodyAnimationNorman = new AnimationClipInstance("PassSodaA-HandEdits", testScenes.modelNorman);
            int bodyAnimationNormanInstanceId = timeline.AddAnimation(LEAPCore.baseAnimationLayerName,
                bodyAnimationNorman, 0, LEAPCore.helperAnimationLayerName);
            var bodyAnimationRoman = new AnimationClipInstance("PassSodaB-HandEdits", testScenes.modelRoman);
            int bodyAnimationRomanInstanceId = timeline.AddAnimation(LEAPCore.baseAnimationLayerName,
                bodyAnimationRoman, 0, LEAPCore.helperAnimationLayerName);
            var bodyAnimationNormanette = new AnimationClipInstance("PassSodaC", testScenes.modelNormanette);
            timeline.AddAnimation(LEAPCore.baseAnimationLayerName, bodyAnimationNormanette, 0);

            // Create environment animations
            var envController = testScenes.modelPassSodaEnv.GetComponent<EnvironmentController>();
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance("PassSodaBottle", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "SodaBottle"),
                    false, false, false));
        }
        else if (sceneName == "HandShake-HandEdits")
        {
            testScenes.modelNorman.SetActive(true);
            testScenes.modelRoman.SetActive(true);
            testScenes.modelHandShakeEnv.SetActive(true);

            // Add character models to the timeline
            timeline.OwningManager.AddModel(testScenes.modelNorman);
            timeline.OwningManager.AddModel(testScenes.modelRoman);

            // Set environment in the timeline
            timeline.OwningManager.SetEnvironment(testScenes.modelHandShakeEnv);

            // Create animation instances
            var bodyAnimationNorman = new AnimationClipInstance("HandShakeA-HandEdits", testScenes.modelNorman);
            int bodyAnimationNormanInstanceId = timeline.AddAnimation(LEAPCore.baseAnimationLayerName,
                bodyAnimationNorman, 0, LEAPCore.helperAnimationLayerName);
            var bodyAnimationRoman = new AnimationClipInstance("HandShakeB-HandEdits", testScenes.modelRoman);
            int bodyAnimationRomanInstanceId = timeline.AddAnimation(LEAPCore.baseAnimationLayerName,
                bodyAnimationRoman, 0, LEAPCore.helperAnimationLayerName);

            // Create camera animations
            var envController = testScenes.modelHandShakeEnv.GetComponent<EnvironmentController>();
            var cameraAnimation = new AnimationClipInstance("HandShakeCamera",
                envController.Cameras.FirstOrDefault(cam => cam.gameObject.name == "CameraHandShake").gameObject,
                false, false, false);
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName, cameraAnimation);
        }
        else if (sceneName == "StealDiamondNew-HandEdits")
        {
            testScenes.modelNormanElliot.SetActive(true);
            testScenes.modelStealDiamondNewEnv.SetActive(true);

            // Add character models to the timeline
            timeline.OwningManager.AddModel(testScenes.modelNormanElliot);

            // Set environment in the timeline
            timeline.OwningManager.SetEnvironment(testScenes.modelStealDiamondNewEnv);

            // Create animation instances
            var bodyAnimationNorman = new AnimationClipInstance("StealDiamondNew-HandEdits", testScenes.modelNormanElliot);
            int bodyAnimationNormanInstanceId = timeline.AddAnimation(LEAPCore.baseAnimationLayerName,
                bodyAnimationNorman, 0, LEAPCore.helperAnimationLayerName);

            // Create environment animations
            var envController = testScenes.modelStealDiamondNewEnv.GetComponent<EnvironmentController>();
            var envAnimationGem = new AnimationClipInstance("StealDiamondNewGem",
                envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "Gem"),
                false, false, false);
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName, envAnimationGem);
            var cameraAnimation = new AnimationClipInstance("StealDiamondNewCamera1",
                envController.Cameras.FirstOrDefault(cam => cam.gameObject.name == "CameraStealDiamondNew1").gameObject,
                false, false, false);
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName, cameraAnimation);
            cameraAnimation = new AnimationClipInstance("StealDiamondNewCamera2",
                envController.Cameras.FirstOrDefault(cam => cam.gameObject.name == "CameraStealDiamondNew2").gameObject,
                false, false, false);
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName, cameraAnimation);
        }
        else // if (sceneName == "InitialPose")
        {
            testScenes.modelNorman.SetActive(true);

            // Add character models to the timeline
            timeline.OwningManager.AddModel(testScenes.modelNorman);

            // Set environment in the timeline
            timeline.OwningManager.SetEnvironment(null);

            // Create animation instances
            var bodyAnimationNorman = new AnimationClipInstance("InitialPose", testScenes.modelNorman,
                false, false, false);
            int bodyAnimationNormanInstanceId = timeline.AddAnimation(LEAPCore.baseAnimationLayerName,
                bodyAnimationNorman, 0, LEAPCore.helperAnimationLayerName);

            // Load eye gaze
            EyeGazeEditor.LoadEyeGaze(timeline, bodyAnimationNormanInstanceId, LEAPCore.eyeGazeAnimationLayerName);
            EyeGazeEditor.PrintEyeGaze(timeline);
        }

        timeline.OwningManager.Init();
    }

    /// <summary>
    /// Capture video of the currently loaded scenario.
    /// </summary>
    public static void CaptureVideo(string suffix = "")
    {
        var timeline = AnimationManager.Instance.Timeline;
        var videoCapture = GameObject.FindGameObjectWithTag("EyeGazeEditor").GetComponent<VideoCapture>();
        if (timeline.ActiveBakedTimeline == null)
        {
            Debug.LogError("No baked animation on the timeline or baked animation inactive");
            return;
        }
        var envController = timeline.OwningManager.Environment.GetComponent<EnvironmentController>();

        // Create screenshot texture
        var rtScreen = new RenderTexture(Screen.width, Screen.height, 24);
        rtScreen.antiAliasing = 4;
        var texScreen = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);

        // Advance through the animation and capture a sceenshot at each frame

        int startFrame = Mathf.Clamp(LEAPCore.gazeVideoCaptureStartFrame, 0, timeline.FrameLength - 1);
        int endFrame = LEAPCore.gazeVideoCaptureEndFrame < 0 ? timeline.FrameLength - 1 :
            Mathf.Clamp(LEAPCore.gazeVideoCaptureEndFrame, startFrame, timeline.FrameLength - 1);
        videoCapture.Start();
        for (int frameIndex = startFrame; frameIndex <= endFrame; ++frameIndex)
        {
            timeline.GoToFrame(frameIndex);
            timeline.ApplyBakedAnimation();

            // Render active cameras
            foreach (var camera in envController.Cameras)
            {
                if (camera.enabled)
                {
                    camera.targetTexture = rtScreen;
                    camera.Render();
                }
            }

            // Get screenshot
            RenderTexture.active = rtScreen;
            texScreen.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
            RenderTexture.active = null;

            // Reset camera render targets
            foreach (var camera in envController.Cameras)
                if (camera.enabled)
                    camera.targetTexture = null;

            // Save screenshot to file
            var data = texScreen.EncodeToPNG();
            string path = string.Format(videoCapture.videoCaptureDirectory + "frame{0:D5}.png", frameIndex + 1 - startFrame);
            File.WriteAllBytes(path, data);
        }

        // Get current scene name
        string sceneName = envController.gameObject.name.Substring(0, envController.gameObject.name.IndexOf("Env"));

        // Generate a video file from the frame image sequence
        string cmd = "";
        string args = "";
        switch (LEAPCore.gazeVideoCaptureFormat)
        {
            case "mp4":
                cmd = "GenerateVideoMP4Baseline";
                args = sceneName + suffix;
                break;
            case "mov":
                cmd = "GenerateVideoMOV";
                args = sceneName + suffix;
                break;
            case "wmv":
                cmd = "GenerateVideoWMV";
                args = sceneName + suffix + " " + Screen.width;
                break;
            default:
                cmd = "GenerateVideoMP4Baseline";
                args = sceneName + suffix;
                break;
        }
        System.Diagnostics.Process.Start(cmd, args);
    }

    /// <summary>
    /// Extract fixation frames from eye tracking data.
    /// </summary>
    /// <param name="sceneName">Scene name</param>
    /// <param name="timeline">Animation timeline</param>
    /// <param name="model">Character model</param>
    public static void ExtractEyeTrackFixationFrames(string sceneName, AnimationTimeline timeline, GameObject model,
        int startFrame = 0, int endFrame = -1)
    {
        string framePath = "C:\\Local Users\\tpejsa\\OneDrive\\Gaze Editing Project\\EyeTrackMocap\\" + sceneName + "\\EyeTrack\\Frames";
        string outFramePath = "C:\\Local Users\\tpejsa\\OneDrive\\Gaze Editing Project\\Annotations\\GazeTargets\\"  + sceneName;
        var baseInstance = timeline.GetLayer(LEAPCore.baseAnimationLayerName).Animations.FirstOrDefault(inst =>
            inst.Animation.Model == model);
        EyeGazeInferenceModel.ExtractEyeTrackFixationFrames(timeline, baseInstance.InstanceId, framePath, outFramePath,
            startFrame, endFrame);
    }

    /// <summary>
    /// Compute intercoder reliability for gaze instance annotations.
    /// </summary>
    /// <param name="sceneName">Scene name</param>
    /// <param name="startFrame">Scene start frame</param>
    /// <param name="endFrame">Scene end frame</param>
    public static void ComputeIntercoderReliabilityInstances(string sceneName,
        int sceneStartFrame, int sceneEndFrame)
    {
        // Get data paths for current scene
        string path1 = "C:/Local Users/tpejsa/OneDrive/Gaze Editing Project/Annotations/GazeShifts/Tom/"
            + sceneName + "#GazeShifts.csv";
        string path2 = "C:/Local Users/tpejsa/OneDrive/Gaze Editing Project/Annotations/GazeShifts/"
            + sceneName + "#GazeShifts.csv";

        // Load data from both coders
        var data1 = new CSVDataFile();
        data1.AddAttribute("EventType", typeof(string));
        data1.AddAttribute("StartFrame", typeof(int));
        data1.AddAttribute("EndFrame", typeof(int));
        data1.ReadFromFile(path1);
        var data2 = new CSVDataFile();
        data2.AddAttribute("EventType", typeof(string));
        data2.AddAttribute("StartFrame", typeof(int));
        data2.AddAttribute("EndFrame", typeof(int));
        data2.ReadFromFile(path2);

        // Remove non-gaze shifts
        for (int rowIndex1 = 0; rowIndex1 < data1.NumberOfRows; ++rowIndex1)
            if (data1[rowIndex1].GetValue<string>(0) != "GazeShift")
                data1.RemoveData(rowIndex1--);
        for (int rowIndex2 = 0; rowIndex2 < data2.NumberOfRows; ++rowIndex2)
            if (data1[rowIndex2].GetValue<string>(0) != "GazeShift")
                data1.RemoveData(rowIndex2--);

        // Identify pairs of overlapping annotations
        var startTimes1 = new List<float>();
        var startTimes2 = new List<float>();
        var endTimes1 = new List<float>();
        var endTimes2 = new List<float>();
        var matched1 = new HashSet<int>();
        var matched2 = new HashSet<int>();
        int num1 = data1.NumberOfRows;
        int num2 = data2.NumberOfRows;
        for (int rowIndex1 = 0; rowIndex1 < data1.NumberOfRows; ++rowIndex1)
        {
            float startTime1 = LEAPCore.ToTime(data1[rowIndex1].GetValue<int>(1));
            float endTime1 = LEAPCore.ToTime(data1[rowIndex1].GetValue<int>(2));
            float startTime2 = -1f;
            float endTime2 = -1f;
            float maxOverlapLength = -1f;
            int maxOverlapRowIndex = -1;

            for (int rowIndex2 = 0; rowIndex2 < data2.NumberOfRows; ++rowIndex2)
            {
                float curStartTime2 = LEAPCore.ToTime(data2[rowIndex2].GetValue<int>(1));
                float curEndTime2 = LEAPCore.ToTime(data2[rowIndex2].GetValue<int>(2));

                float overlapLength = Mathf.Min(endTime1, curEndTime2) - Mathf.Max(startTime1, curStartTime2);
                if (overlapLength > 0f)
                {
                    startTimes1.Add(startTime1);
                    startTimes2.Add(curStartTime2);
                    endTimes1.Add(endTime1);
                    endTimes2.Add(curEndTime2);

                    matched1.Add(rowIndex1);
                    matched2.Add(rowIndex2);
                }
                /*if (overlapLength > 0f && overlapLength > maxOverlapLength)
                {
                    startTime2 = curStartTime2;
                    endTime2 = curEndTime2;
                    maxOverlapLength = overlapLength;
                    maxOverlapRowIndex = rowIndex2;
                }*/
            }

            /*if (maxOverlapLength > 0f)
            {
                startTimes1.Add(startTime1);
                startTimes2.Add(startTime2);
                endTimes1.Add(endTime1);
                endTimes2.Add(endTime2);
                data2.RemoveData(maxOverlapRowIndex);

                matched1.Add(rowIndex1);
                matched2.Add(maxOverlapRowIndex);
            }*/
        }

        // Report Fisher's r
        float rs = StatUtil.ComputeFisherR(startTimes1.ToArray(), startTimes2.ToArray());
        float re = StatUtil.ComputeFisherR(endTimes1.ToArray(), endTimes2.ToArray());
        Debug.Log(string.Format("Gaze instance intercoder agreement for scene {0} is rs = {1}, re = {2}; unmatched {3}/{4} and {5}/{6} gaze shifts, respectively",
            sceneName, rs, re, num1 - matched1.Count, num1, num2 - matched2.Count, num2));
    }

    /// <summary>
    /// Compute intercoder reliability for gaze target annotations.
    /// </summary>
    /// <param name="sceneName">Scene name</param>
    public static void ComputeIntercoderReliabilityTargets(string sceneName)
    {
        // Get target names and data paths for current scene
        string path1 = "C:/Local Users/tpejsa/OneDrive/Gaze Editing Project/Annotations/GazeTargets/Tom/"
            + sceneName + "#GazeTargets.csv";
        string path2 = "C:/Local Users/tpejsa/OneDrive/Gaze Editing Project/Annotations/GazeTargets/"
            + sceneName + "#GazeTargets.csv";
        string[] targetNames = null;
        switch (sceneName)
        {
            case "BookShelfNew":

                targetNames = new string[] { "Book1", "Book2", "Book3", "Background" };
                break;

            case "MakeSandwich":

                targetNames = new string[] { "Bread1", "Bread2", "Bacon", "Swiss", "Turkey", "Lettuce", "Tomato", "Background" };
                break;

            case "MakeSandwichDemo":

                targetNames = new string[] { "Bread1", "Bread2", "Bacon", "Swiss", "Turkey", "Lettuce", "Tomato", "Screen", "Background" };
                break;

            case "StealDiamondNew":

                targetNames = new string[] { "Gem", "Background" };
                break;

            default:

                throw new ArgumentException(sceneName + " was not coded for gaze targets by multiple coders!", sceneName);
        }

        // Load data from both coders
        var data1 = new CSVDataFile();
        data1.AddAttribute("Target", typeof(string));
        data1.AddAttribute("Frame", typeof(int));
        data1.ReadFromFile(path1);
        var data2 = new CSVDataFile();
        data2.AddAttribute("Target", typeof(string));
        data2.AddAttribute("Frame", typeof(int));
        data2.ReadFromFile(path2);

        // Get all frame indexes and remove duplicates from the dataset
        var frameSet1 = new HashSet<int>();
        for (int rowIndex = 0; rowIndex < data1.NumberOfRows; ++rowIndex)
        {
            int frame = data1[rowIndex].GetValue<int>(1);
            string targetName = data1[rowIndex].GetValue<string>(0);

            if (!targetNames.Contains(targetName))
                continue;

            if (frameSet1.Contains(frame))
                data1.RemoveData(rowIndex--);
            else
                frameSet1.Add(frame);
        }
        var frameSet2 = new HashSet<int>();
        for (int rowIndex = 0; rowIndex < data2.NumberOfRows; ++rowIndex)
        {
            int frame = data2[rowIndex].GetValue<int>(1);
            string targetName = data2[rowIndex].GetValue<string>(0);

            if (!targetNames.Contains(targetName))
                continue;

            if (frameSet2.Contains(frame))
                data2.RemoveData(rowIndex--);
            else
                frameSet2.Add(frame);
        }
        var frameSet = new HashSet<int>(frameSet1.Intersect(frameSet2));
        
        // Get all per-frame targets
        var frames1 = new List<int>();
        var frames2 = new List<int>();
        var frameTargets1 = new List<string>();
        var frameTargets2 = new List<string>();
        for (int rowIndex = 0; rowIndex < data1.NumberOfRows; ++rowIndex)
        {
            int frame = data1[rowIndex].GetValue<int>(1);
            string targetName = data1[rowIndex].GetValue<string>(0);

            if (!frameSet.Contains(frame))
                continue;

            frames1.Add(frame);
            frameTargets1.Add(targetName);
        }
        for (int rowIndex = 0; rowIndex < data2.NumberOfRows; ++rowIndex)
        {
            int frame = data2[rowIndex].GetValue<int>(1);
            string targetName = data2[rowIndex].GetValue<string>(0);

            if (!frameSet.Contains(frame))
                continue;

            frames2.Add(frame);
            frameTargets2.Add(targetName);
        }

        if (!frames1.SequenceEqual(frames2))
            throw new Exception(string.Format("One or both gaze target data sets for {0} specify frames out of order",
                sceneName));

        // Report Cohen's kappa
        float kappa = StatUtil.ComputeCohenKappa<int, string>(frames1.ToArray(),
            targetNames.ToArray(), frameTargets1.ToArray(), frameTargets2.ToArray());
        Debug.Log(string.Format("Gaze target intercoder reliability for scene {0} is kappa = {1}",
            sceneName, kappa));
    }
}

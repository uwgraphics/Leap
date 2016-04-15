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
        timeline.GetLayer(LEAPCore.helperAnimationLayerName).isBase = false;
        timeline.GetLayer(LEAPCore.helperAnimationLayerName).isIKEndEffectorConstr = false;
        timeline.AddLayer(AnimationLayerMode.Override, 5, LEAPCore.environmentAnimationLayerName);
        timeline.GetLayer(LEAPCore.environmentAnimationLayerName).isBase = false;
        timeline.GetLayer(LEAPCore.environmentAnimationLayerName).isIKEndEffectorConstr = false;
        timeline.AddLayer(AnimationLayerMode.Override, 10, LEAPCore.baseAnimationLayerName);
        timeline.GetLayer(LEAPCore.baseAnimationLayerName).isIKEndEffectorConstr = true;
        timeline.GetLayer(LEAPCore.baseAnimationLayerName).isBase = true;
        timeline.AddLayer(AnimationLayerMode.Override, 15, LEAPCore.cameraAnimationLayerName);
        timeline.GetLayer(LEAPCore.cameraAnimationLayerName).isBase = false;
        timeline.GetLayer(LEAPCore.cameraAnimationLayerName).isIKEndEffectorConstr = false;
        timeline.AddLayer(AnimationLayerMode.Override, 20, LEAPCore.eyeGazeAnimationLayerName);
        timeline.GetLayer(LEAPCore.eyeGazeAnimationLayerName).isBase = false;
        timeline.GetLayer(LEAPCore.eyeGazeAnimationLayerName).isGaze = true;

        if (sceneName == "TestExpressiveGaze")
        {
            LEAPCore.gazeConstraintActivationTime = 0f;
            //testScenes.modelNorman.GetComponent<GazeController>().torso.postureWeight = 0f;

            testScenes.modelNorman.SetActive(true);
            testScenes.modelTestExpressiveGazeEnv.SetActive(true);

            // Add character models to the timeline
            timeline.OwningManager.AddModel(testScenes.modelNorman);

            // Set environment in the timeline
            timeline.OwningManager.SetEnvironment(testScenes.modelTestExpressiveGazeEnv);

            // Create animation instances
            var bodyAnimationNorman = new AnimationClipInstance("TestExpressiveGaze", testScenes.modelNorman, true, true, false);
            int bodyAnimationNormanInstanceId = timeline.AddAnimation(LEAPCore.baseAnimationLayerName,
                bodyAnimationNorman, 0);

            // Load eye gaze
            EyeGazeEditor.LoadEyeGaze(timeline, bodyAnimationNormanInstanceId, LEAPCore.eyeGazeAnimationLayerName);
            EyeGazeEditor.PrintEyeGaze(timeline);
        }
        else if (sceneName == "WindowWashing")
        {
            testScenes.modelNorman.SetActive(true);
            //testScenes.modelNormanette.SetActive(true);
            testScenes.modelNormanette.transform.position = new Vector3(2.58f, 0f, -3.72f);
            testScenes.modelNormanette.transform.localScale = new Vector3(0.96f, 0.91f, 0.96f);
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
            //testScenes.modelNormanette.SetActive(true);
            testScenes.modelNormanette.SetActive(false);
            testScenes.modelNormanette.transform.position = new Vector3(-4.97f, 0f, 1.24f);
            testScenes.modelNormanette.transform.localScale = new Vector3(0.96f, 0.91f, 0.96f);
            testScenes.modelPassSodaEnv.SetActive(true);

            // Some end-effector goals are affected by gaze, so reconfigure IK for layers
            /*timeline.GetLayer(LEAPCore.baseAnimationLayerName).isIKEndEffectorConstr = false;
            timeline.GetLayer(LEAPCore.eyeGazeAnimationLayerName).isIKEndEffectorConstr = true;*/

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
            EyeGazeEditor.LoadEyeGaze(timeline, bodyAnimationNormanInstanceId, LEAPCore.eyeGazeAnimationLayerName);
            EyeGazeEditor.LoadEyeGaze(timeline, bodyAnimationRomanInstanceId, LEAPCore.eyeGazeAnimationLayerName);
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

            // Load eye gaze
            EyeGazeEditor.LoadEyeGaze(timeline, bodyAnimationNormanInstanceId, LEAPCore.eyeGazeAnimationLayerName);
            EyeGazeEditor.LoadEyeGaze(timeline, bodyAnimationRomanInstanceId, LEAPCore.eyeGazeAnimationLayerName);
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

            // Add timewarps to the animations
            AnimationTimingEditor.LoadTimewarps(timeline, bodyAnimationNormanInstanceId);
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

            // Add timewarps to the animations
            AnimationTimingEditor.LoadTimewarps(timeline, bodyAnimationNormanInstanceId);

            // Extract eye tracking fixation frames
            ExtractEyeTrackFixationFrames(sceneName, timeline, testScenes.modelNormanNew, 970, 2045);
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
            /*var cameraAnimation = new AnimationClipInstance("WalkConesNewCamera",
                envController.Cameras.FirstOrDefault(cam => cam.gameObject.name == "CameraWalkConesNew").gameObject,
                false, false, false);
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName, cameraAnimation);*/
            var gazeController = testScenes.modelNormanNew.GetComponent<GazeController>();
            var root = ModelUtil.FindBoneWithTag(testScenes.modelNormanNew.transform, "RootBone");
            var cameraAnimation = new LookAtFrontInstance("WalkConesCamera",
                envController.Cameras.FirstOrDefault(cam => cam.gameObject.name == "CameraWalkConesNew").gameObject,
                bodyAnimationNorman.FrameLength, root, 7.6f, 5.2f, 3.2f); 
            timeline.AddEnvironmentAnimation(LEAPCore.cameraAnimationLayerName, cameraAnimation);

            // Load eye gaze
            EyeGazeEditor.LoadEyeGaze(timeline, bodyAnimationNormanInstanceId, LEAPCore.eyeGazeAnimationLayerName);
            EyeGazeEditor.PrintEyeGaze(timeline);

            // Add timewarps to the animations
            AnimationTimingEditor.LoadTimewarps(timeline, bodyAnimationNormanInstanceId);
        }
        else if (sceneName == "BookShelfNew")
        {
            testScenes.modelNormanNew.SetActive(true);
            testScenes.modelBookShelfNewEnv.SetActive(true);

            // Add character models to the timeline
            timeline.OwningManager.AddModel(testScenes.modelNormanNew);

            // Set environment in the timeline
            timeline.OwningManager.SetEnvironment(testScenes.modelBookShelfNewEnv);

            // Create animation instances
            var bodyAnimationNorman = new AnimationClipInstance("BookShelfNew", testScenes.modelNormanNew);
            int bodyAnimationNormanInstanceId = timeline.AddAnimation(LEAPCore.baseAnimationLayerName,
                bodyAnimationNorman, 0, LEAPCore.helperAnimationLayerName);

            // Load eye gaze
            EyeGazeEditor.LoadEyeGaze(timeline, bodyAnimationNormanInstanceId, LEAPCore.eyeGazeAnimationLayerName);
            EyeGazeEditor.PrintEyeGaze(timeline);

            // Create environment animations
            var envController = testScenes.modelBookShelfNewEnv.GetComponent<EnvironmentController>();
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance("BookShelfNewBook1", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "Book1"),
                    false, false, false));
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance("BookShelfNewBook2", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "Book2"),
                    false, false, false));
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance("BookShelfNewBook3", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "Book3"),
                    false, false, false));
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance(
                    "BookShelfNewMarkers", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "Markers"),
                    false, false, false));

            // Extract eye tracking fixation frames
            ExtractEyeTrackFixationFrames(sceneName, timeline, testScenes.modelNormanNew, 749, 2618);
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
                false, false, false);
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName, cameraAnimation);
            cameraAnimation = new AnimationClipInstance("StealDiamondNewCamera2",
                envController.Cameras.FirstOrDefault(cam => cam.gameObject.name == "CameraStealDiamondNew2").gameObject,
                false, false, false);
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName, cameraAnimation);

            // Load eye gaze
            EyeGazeEditor.LoadEyeGaze(timeline, bodyAnimationNormanInstanceId, LEAPCore.eyeGazeAnimationLayerName);
            EyeGazeEditor.PrintEyeGaze(timeline);

            // Extract eye tracking fixation frames
            ExtractEyeTrackFixationFrames(sceneName, timeline, testScenes.modelNormanNew, 1066, 1359);
        }
        else if (sceneName == "WaitForBusNew")
        {
            testScenes.modelNormanNew.SetActive(true);
            testScenes.modelWaitForBusNewEnv.SetActive(true);

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

            // Add timewarps to the animations
            AnimationTimingEditor.LoadTimewarps(timeline, bodyAnimationNormanInstanceId);

            // Extract eye tracking fixation frames
            ExtractEyeTrackFixationFrames(sceneName, timeline, testScenes.modelNormanNew, 590, 1782);
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
                envController.Cameras.FirstOrDefault(cam => cam.gameObject.name == "CameraStackBoxesNew1").gameObject,
                false, false, false);
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName, cameraAnimation);
            cameraAnimation = new AnimationClipInstance("StackBoxesNewCamera2",
                envController.Cameras.FirstOrDefault(cam => cam.gameObject.name == "CameraStackBoxesNew2").gameObject,
                false, false, false);
            timeline.AddEnvironmentAnimation(LEAPCore.environmentAnimationLayerName, cameraAnimation);

            // Extract eye tracking fixation frames
            ExtractEyeTrackFixationFrames(sceneName, timeline, testScenes.modelNormanNew, 544, 1486);
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
        else if (sceneName == "MakeSandwich")
        {
            testScenes.modelNormanNew.SetActive(true);
            testScenes.modelMakeSandwichEnv.SetActive(true);

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

            // Extract eye tracking fixation frames
            ExtractEyeTrackFixationFrames(sceneName, timeline, testScenes.modelNormanNew, 942, 1957);
        }
        else if (sceneName == "MakeSandwichDemo")
        {
            testScenes.modelNormanNew.SetActive(true);
            testScenes.modelMakeSandwichDemoEnv.SetActive(true);

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
                new AnimationClipInstance(
                    "MakeSandwichDemoMarkers", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "Markers"),
                    false, false, false));

            // Extract eye tracking fixation frames
            ExtractEyeTrackFixationFrames(sceneName, timeline, testScenes.modelNormanNew, 756, 2763);
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
}

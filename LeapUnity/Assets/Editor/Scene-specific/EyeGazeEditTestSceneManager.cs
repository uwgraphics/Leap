﻿using UnityEngine;
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

        var editTestScenario = GameObject.FindGameObjectWithTag("ScenarioManager").GetComponent<GazeEditTestScenario>();
        var testScenes = GameObject.Find("EyeGazeEditor").GetComponent<EyeGazeEditTestSceneData>();
        timeline.RemoveAllLayers();
        timeline.OwningManager.RemoveAllModels();

        // Reload Leap configuration
        LEAPCore.LoadConfiguration();

        // Deactivate all characters and props
        testScenes.modelNorman.SetActive(false);
        testScenes.modelNormanette.SetActive(false);
        testScenes.modelRoman.SetActive(false);
        testScenes.modelNormanNew.SetActive(false);
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
        testScenes.modelWaitForBusNewEnv.SetActive(false);
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
        timeline.AddLayer(AnimationLayerMode.Override, 15, LEAPCore.eyeGazeAnimationLayerName);
        timeline.GetLayer(LEAPCore.eyeGazeAnimationLayerName).isBase = false;
        timeline.GetLayer(LEAPCore.eyeGazeAnimationLayerName).isGaze = true;

        // Reset test scenario
        editTestScenario.models = null;
        editTestScenario.objectAnimations = null;
        editTestScenario.cameraAnimations = null;

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

            // Initialize test scenario
            editTestScenario.models = new GameObject[2];
            editTestScenario.models[0] = testScenes.modelNorman;
            editTestScenario.models[1] = testScenes.modelNormanette;
            editTestScenario.animations = new string[2];
            editTestScenario.animations[0] = "WindowWashingA-" + LEAPCore.defaultBakedTimelineName;
            editTestScenario.animations[1] = "WindowWashingB-" + LEAPCore.defaultBakedTimelineName;
            editTestScenario.objectAnimations = new string[1];
            editTestScenario.objectAnimations[0] = "WindowWashingSponge-" + LEAPCore.defaultBakedTimelineName;
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

            // Initialize test scenario
            editTestScenario.models = new GameObject[3];
            editTestScenario.models[0] = testScenes.modelNorman;
            editTestScenario.models[1] = testScenes.modelRoman;
            editTestScenario.models[2] = testScenes.modelNormanette;
            editTestScenario.animations = new string[3];
            editTestScenario.animations[0] = "PassSodaA-" + LEAPCore.defaultBakedTimelineName;
            editTestScenario.animations[1] = "PassSodaB-" + LEAPCore.defaultBakedTimelineName;
            editTestScenario.animations[2] = "PassSodaC-" + LEAPCore.defaultBakedTimelineName;
            editTestScenario.objectAnimations = new string[1];
            editTestScenario.objectAnimations[0] = "PassSodaBottle-" + LEAPCore.defaultBakedTimelineName;
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

            // Initialize test scenario
            editTestScenario.models = new GameObject[1];
            editTestScenario.models[0] = testScenes.modelNorman;
            editTestScenario.animations = new string[1];
            editTestScenario.animations[0] = "Walking90deg-" + LEAPCore.defaultBakedTimelineName;
            editTestScenario.cameraAnimations = new string[1];
            editTestScenario.cameraAnimations[0] = "Walking90degCamera";
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

            // Initialize test scenario
            editTestScenario.models = new GameObject[2];
            editTestScenario.models[0] = testScenes.modelNorman;
            editTestScenario.models[1] = testScenes.modelRoman;
            editTestScenario.animations = new string[2];
            editTestScenario.animations[0] = "HandShakeA-" + LEAPCore.defaultBakedTimelineName;
            editTestScenario.animations[1] = "HandShakeB-" + LEAPCore.defaultBakedTimelineName;
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

            // Initialize test scenario
            editTestScenario.models = new GameObject[1];
            editTestScenario.models[0] = testScenes.modelNorman;
            editTestScenario.animations = new string[1];
            editTestScenario.animations[0] = "BookShelf-" + LEAPCore.defaultBakedTimelineName;
            editTestScenario.objectAnimations = new string[3];
            editTestScenario.objectAnimations[0] = "BookShelfBook1-" + LEAPCore.defaultBakedTimelineName;
            editTestScenario.objectAnimations[1] = "BookShelfBook2-" + LEAPCore.defaultBakedTimelineName;
            editTestScenario.objectAnimations[2] = "BookShelfBook3-" + LEAPCore.defaultBakedTimelineName;
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

            // Initialize test scenario
            editTestScenario.models = new GameObject[1];
            editTestScenario.models[0] = testScenes.modelNorman;
            editTestScenario.animations = new string[1];
            editTestScenario.animations[0] = "StealDiamond-" + LEAPCore.defaultBakedTimelineName;
            editTestScenario.objectAnimations = new string[1];
            editTestScenario.objectAnimations[0] = "StealDiamondGem-" + LEAPCore.defaultBakedTimelineName;
            editTestScenario.cameraAnimations = new string[1];
            editTestScenario.cameraAnimations[0] = "StealDiamondCamera1";
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

            // Initialize test scenario
            editTestScenario.models = new GameObject[1];
            editTestScenario.models[0] = testScenes.modelNorman;
            editTestScenario.animations = new string[1];
            editTestScenario.animations[0] = "WaitForBus-" + LEAPCore.defaultBakedTimelineName;
            editTestScenario.objectAnimations = new string[1];
            editTestScenario.objectAnimations[0] = "WaitForBusWatch-" + LEAPCore.defaultBakedTimelineName;
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

            // Initialize test scenario
            editTestScenario.models = new GameObject[1];
            editTestScenario.models[0] = testScenes.modelKinect;
            editTestScenario.animations = new string[1];
            editTestScenario.animations[0] = "EyeTrackMocapTest1-1-" + LEAPCore.defaultBakedTimelineName;
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

            // Initialize test scenario
            editTestScenario.models = new GameObject[1];
            editTestScenario.models[0] = testScenes.modelNormanNew;
            editTestScenario.animations = new string[1];
            editTestScenario.animations[0] = "StackBoxes-" + LEAPCore.defaultBakedTimelineName;
            editTestScenario.objectAnimations = new string[4];
            editTestScenario.objectAnimations[0] = "StackBoxes1-" + LEAPCore.defaultBakedTimelineName;
            editTestScenario.objectAnimations[1] = "StackBoxes2-" + LEAPCore.defaultBakedTimelineName;
            editTestScenario.objectAnimations[2] = "StackBoxes3-" + LEAPCore.defaultBakedTimelineName;
            editTestScenario.objectAnimations[3] = "StackBoxes4-" + LEAPCore.defaultBakedTimelineName;
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

            // Initialize test scenario
            editTestScenario.models = new GameObject[1];
            editTestScenario.models[0] = testScenes.modelNormanNew;
            editTestScenario.animations = new string[1];
            editTestScenario.animations[0] = "WindowWashingNew-" + LEAPCore.defaultBakedTimelineName;
            editTestScenario.objectAnimations = new string[2];
            editTestScenario.objectAnimations[0] = "WindowWashingNewSponge-" + LEAPCore.defaultBakedTimelineName;
            editTestScenario.objectAnimations[1] = "WindowWashingNewMarkers-" + LEAPCore.defaultBakedTimelineName;
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

            // Load eye gaze
            EyeGazeEditor.LoadEyeGaze(timeline, bodyAnimationNormanInstanceId, LEAPCore.eyeGazeAnimationLayerName);
            EyeGazeEditor.PrintEyeGaze(timeline);

            // Add timewarps to the animations
            AnimationTimingEditor.LoadTimewarps(timeline, bodyAnimationNormanInstanceId);

            // Initialize test scenario
            editTestScenario.models = new GameObject[1];
            editTestScenario.models[0] = testScenes.modelNormanNew;
            editTestScenario.animations = new string[1];
            editTestScenario.animations[0] = "WalkConesNew-" + LEAPCore.defaultBakedTimelineName;
            editTestScenario.objectAnimations = new string[1];
            editTestScenario.objectAnimations[0] = "WalkConesNewMarkers-" + LEAPCore.defaultBakedTimelineName;
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
                new AnimationClipInstance(
                    "WaitForBusNewMarkers", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "Markers"),
                    false, false, false));

            // Load eye gaze
            EyeGazeEditor.LoadEyeGaze(timeline, bodyAnimationNormanInstanceId, LEAPCore.eyeGazeAnimationLayerName);
            EyeGazeEditor.PrintEyeGaze(timeline);

            // Add timewarps to the animations
            AnimationTimingEditor.LoadTimewarps(timeline, bodyAnimationNormanInstanceId);

            // Initialize test scenario
            editTestScenario.models = new GameObject[1];
            editTestScenario.models[0] = testScenes.modelNormanNew;
            editTestScenario.animations = new string[1];
            editTestScenario.animations[0] = "WaitForBusNew-" + LEAPCore.defaultBakedTimelineName;
            editTestScenario.objectAnimations = new string[1];
            editTestScenario.objectAnimations[0] = "WaitForBusNewMarkers-" + LEAPCore.defaultBakedTimelineName;
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
    public static void CaptureVideo(string suffix = "", string ext = "mp4")
    {
        var timeline = AnimationManager.Instance.Timeline;
        var videoCapture = GameObject.FindGameObjectWithTag("ScenarioManager").GetComponent<VideoCapture>();
        if (timeline.ActiveBakedTimeline == null)
        {
            Debug.LogError("No baked animation on the timeline or baked animation inactive");
            return;
        }

        // Create screenshot texture
        var rtScreen = new RenderTexture(Screen.width, Screen.height, 24);
        rtScreen.antiAliasing = 4;
        var texScreen = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);

        // Advance through the animation and capture a sceenshot at each frame
        var envController = timeline.OwningManager.Environment.GetComponent<EnvironmentController>();
        videoCapture.Start();
        for (int frameIndex = 0; frameIndex < timeline.FrameLength; ++frameIndex)
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
            string path = string.Format(videoCapture.videoCaptureDirectory + "frame{0:D5}.png", frameIndex + 1);
            File.WriteAllBytes(path, data);
        }

        // Get current scene name
        string sceneName = envController.gameObject.name.Substring(0, envController.gameObject.name.IndexOf("Env"));

        // Generate a video file from the frame image sequence
        ext = ext.ToLower();
        string cmd = "";
        string args = "";
        switch (ext)
        {
            case "mp4":
                cmd = "GenerateVideoMP4Baseline";
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
}

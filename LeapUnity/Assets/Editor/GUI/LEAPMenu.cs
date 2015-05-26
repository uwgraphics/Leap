using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Linq;

public class LEAPMenu
{
    [MenuItem("LEAP/Animation/Test: ExpressiveGaze", false, 30)]
    private static void TestExpressiveGaze()
    {
        TestScene("TestExpressiveGaze");
    }

    [MenuItem("LEAP/Animation/Test: InitialPose", true, 30)]
    private static bool ValidateTestInitialPose()
    {
        var wnd = EditorWindow.GetWindow<LeapAnimationEditor>();
        if (wnd.Timeline == null)
        {
            return false;
        }

        return true;
    }

    [MenuItem("LEAP/Animation/Test: WindowWashing", true, 31)]
    private static bool ValidateTestWindowWashing()
    {
        var wnd = EditorWindow.GetWindow<LeapAnimationEditor>();
        if (wnd.Timeline == null)
        {
            return false;
        }

        return true;
    }

    [MenuItem("LEAP/Animation/Test: WindowWashing", false, 31)]
    private static void TestWindowWashing()
    {
        TestScene("WindowWashing");
    }

    [MenuItem("LEAP/Animation/Test: PassSoda", true, 32)]
    private static bool ValidateTestPassSoda()
    {
        var wnd = EditorWindow.GetWindow<LeapAnimationEditor>();
        if (wnd.Timeline == null)
        {
            return false;
        }

        return true;
    }

    [MenuItem("LEAP/Animation/Test: PassSoda", false, 32)]
    private static void TestPassSoda()
    {
        TestScene("PassSoda");
    }

    [MenuItem("LEAP/Animation/Test: Walking90deg", true, 33)]
    private static bool ValidateTestWalking90deg()
    {
        var wnd = EditorWindow.GetWindow<LeapAnimationEditor>();
        if (wnd.Timeline == null)
        {
            return false;
        }

        return true;
    }

    [MenuItem("LEAP/Animation/Test: Walking90deg", false, 33)]
    private static void TestWalking90deg()
    {
        TestScene("Walking90deg");
    }

    [MenuItem("LEAP/Animation/Test: HandShake", true, 34)]
    private static bool ValidateTestHandShake()
    {
        var wnd = EditorWindow.GetWindow<LeapAnimationEditor>();
        if (wnd.Timeline == null)
        {
            return false;
        }

        return true;
    }

    [MenuItem("LEAP/Animation/Test: HandShake", false, 34)]
    private static void TesHandShake()
    {
        TestScene("HandShake");
    }

    [MenuItem("LEAP/Animation/Test: BookShelf", true, 35)]
    private static bool ValidateTestBookShelf()
    {
        var wnd = EditorWindow.GetWindow<LeapAnimationEditor>();
        if (wnd.Timeline == null)
        {
            return false;
        }

        return true;
    }

    [MenuItem("LEAP/Animation/Test: BookShelf", false, 35)]
    private static void TestBookShelf()
    {
        TestScene("BookShelf");
    }

    [MenuItem("LEAP/Animation/Test: StealDiamond", true, 36)]
    private static bool ValidateTestStealDiamond()
    {
        var wnd = EditorWindow.GetWindow<LeapAnimationEditor>();
        if (wnd.Timeline == null)
        {
            return false;
        }

        return true;
    }

    [MenuItem("LEAP/Animation/Test: StealDiamond", false, 36)]
    private static void TesStealDiamond()
    {
        TestScene("StealDiamond");
    }

    [MenuItem("LEAP/Animation/Test: WaitForBus", true, 37)]
    private static bool ValidateTestWaitForBus()
    {
        var wnd = EditorWindow.GetWindow<LeapAnimationEditor>();
        if (wnd.Timeline == null)
        {
            return false;
        }

        return true;
    }

    [MenuItem("LEAP/Animation/Test: WaitForBus", false, 37)]
    private static void TesWaitForBus()
    {
        TestScene("WaitForBus");
    }

    private static void TestScene(string sceneName, bool loadEditedGaze = false)
    {
        var wnd = EditorWindow.GetWindow<LeapAnimationEditor>();
        var timeline = wnd.Timeline;

        var editTestScenario = GameObject.FindGameObjectWithTag("ScenarioManager").GetComponent<GazeEditTestScenario>();
        var testScenes = GameObject.Find("EyeGazeEditor").GetComponent<EyeGazeEditTestScenes>();
        timeline.RemoveAllLayers();
        timeline.RemoveAllModels();

        // Reload Leap configuration
        LEAPCore.LoadConfiguration();

        // Deactivate all characters and props
        testScenes.modelNorman.SetActive(false);
        testScenes.modelNormanette.SetActive(false);
        testScenes.modelRoman.SetActive(false);
        testScenes.modelTestExpressiveGazeEnv.SetActive(false);
        testScenes.modelWindowWashingEnv.SetActive(false);
        testScenes.modelPassSodaEnv.SetActive(false);
        testScenes.modelWalking90degEnv.SetActive(false);
        testScenes.modelHandShakeEnv.SetActive(false);
        testScenes.modelBookShelfEnv.SetActive(false);
        testScenes.modelStealDiamondEnv.SetActive(false);
        testScenes.modelWaitForBusEnv.SetActive(false);
        testScenes.cameraWindowWashing.enabled = false;
        testScenes.cameraPassSoda.enabled = false;
        testScenes.cameraWalking90deg.enabled = false;
        testScenes.cameraHandShake.enabled = false;
        testScenes.cameraBookShelf1.enabled = false;
        testScenes.cameraBookShelf2.enabled = false;
        testScenes.cameraStealDiamond1.enabled = false;
        testScenes.cameraStealDiamond2.enabled = false;
        testScenes.cameraWaitForBus.enabled = false;

        // Create and configure animation layers
        timeline.AddLayer(AnimationLayerMode.Override, 0, "BaseAnimation");
        timeline.GetLayer("BaseAnimation").isIKEndEffectorConstr = true;
        timeline.GetLayer("BaseAnimation").isBase = true;
        timeline.AddLayer(AnimationLayerMode.Override, 7, "Gaze");
        timeline.GetLayer("Gaze").isBase = false;
        timeline.GetLayer("Gaze").isGaze = true;
        timeline.AddLayer(AnimationLayerMode.Override, 2, "Environment");
        timeline.GetLayer("Environment").isIKEndEffectorConstr = false;
        timeline.GetLayer("Environment").isBase = false;

        // Configure gaze controllers
        testScenes.modelNorman.GetComponent<GazeController>().headPostureWeight = 0f;
        testScenes.modelNorman.GetComponent<GazeController>().torsoPostureWeight = 1f;
        testScenes.modelRoman.GetComponent<GazeController>().headPostureWeight = 0f;
        testScenes.modelRoman.GetComponent<GazeController>().torsoPostureWeight = 1f;

        if (sceneName == "TestExpressiveGaze")
        {
            LEAPCore.gazeConstraintActivationTime = 0f;

            testScenes.modelNorman.SetActive(true);
            testScenes.modelTestExpressiveGazeEnv.SetActive(true);
            testScenes.cameraWindowWashing.enabled = true;

            // Add character models to the timeline
            timeline.AddModel(testScenes.modelNorman);

            // Set environment in the timeline
            timeline.SetEnvironment(testScenes.modelTestExpressiveGazeEnv);

            // Create animation instances
            var bodyAnimationNorman = new AnimationClipInstance(testScenes.modelNorman, "TestExpressiveGaze");
            int bodyAnimationNormanInstanceId = timeline.AddAnimation("BaseAnimation", bodyAnimationNorman, 0, true);

            // Load eye gaze
            EyeGazeEditor.LoadEyeGaze(timeline, bodyAnimationNormanInstanceId, "Gaze");
            if (LEAPCore.useExpressiveGaze)
                EyeGazeEditor.LoadExpressiveEyeGazeAnimations(timeline, bodyAnimationNormanInstanceId, "Gaze");
            EyeGazeEditor.PrintEyeGaze(timeline);
        }
        else if (sceneName == "WindowWashing")
        {
            testScenes.modelNorman.SetActive(true);
            testScenes.modelNormanette.SetActive(true);
            testScenes.modelNormanette.transform.position = new Vector3(2.58f, 0f, -3.72f);
            testScenes.modelNormanette.transform.localScale = new Vector3(0.96f, 0.91f, 0.96f);
            testScenes.modelWindowWashingEnv.SetActive(true);
            testScenes.cameraWindowWashing.enabled = true;

            // Add character models to the timeline
            timeline.AddModel(testScenes.modelNorman);
            timeline.AddModel(testScenes.modelNormanette);

            // Set environment in the timeline
            timeline.SetEnvironment(testScenes.modelWindowWashingEnv);

            // Create animation instances
            var bodyAnimationNorman = new AnimationClipInstance(testScenes.modelNorman, "WindowWashingA");
            int bodyAnimationNormanInstanceId = timeline.AddAnimation("BaseAnimation", bodyAnimationNorman, 0, true);
            var bodyAnimationNormanette = new AnimationClipInstance(testScenes.modelNormanette, "WindowWashingB");
            timeline.AddAnimation("BaseAnimation", bodyAnimationNormanette, 0);

            // Load eye gaze
            EyeGazeEditor.LoadEyeGaze(timeline, bodyAnimationNormanInstanceId, "Gaze");
            if (LEAPCore.useExpressiveGaze)
                EyeGazeEditor.LoadExpressiveEyeGazeAnimations(timeline, bodyAnimationNormanInstanceId, "Gaze");
            EyeGazeEditor.PrintEyeGaze(timeline);

            // Create environment animations
            var envController = testScenes.modelWindowWashingEnv.GetComponent<EnvironmentController>();
            timeline.AddEnvironmentObjectAnimation("Environment",
                new EnvironmentObjectAnimationInstance(envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "Sponge"),
                    "WindowWashingSponge", timeline.FrameLength));

            // Initialize test scenario
            editTestScenario.models = new GameObject[2];
            editTestScenario.models[0] = testScenes.modelNorman;
            editTestScenario.models[1] = testScenes.modelNormanette;
            editTestScenario.animations = new string[2];
            editTestScenario.animations[0] = "WindowWashingAwEdits";
            editTestScenario.animations[1] = "WindowWashingBwEdits";
            editTestScenario.objectAnimations = new string[1];
            editTestScenario.objectAnimations[0] = "WindowWashingSponge";
        }
        else if (sceneName == "PassSoda")
        {
            testScenes.modelNorman.SetActive(true);
            testScenes.modelRoman.SetActive(true);
            testScenes.modelNormanette.SetActive(true);
            testScenes.modelNormanette.transform.position = new Vector3(-0.91f, 0f, -3.72f);
            testScenes.modelNormanette.transform.localScale = new Vector3(0.96f, 0.91f, 0.96f);
            testScenes.modelPassSodaEnv.SetActive(true);
            testScenes.cameraPassSoda.enabled = true;

            // Add character models to the timeline
            timeline.AddModel(testScenes.modelNorman);
            timeline.AddModel(testScenes.modelNormanette);
            timeline.AddModel(testScenes.modelRoman);

            // Set environment in the timeline
            timeline.SetEnvironment(testScenes.modelPassSodaEnv);

            // Create animation instances
            var bodyAnimationNorman = new AnimationClipInstance(testScenes.modelNorman, "PassSodaA");
            int bodyAnimationNormanInstanceId = timeline.AddAnimation("BaseAnimation", bodyAnimationNorman, 0, true);
            var bodyAnimationRoman = new AnimationClipInstance(testScenes.modelRoman, "PassSodaB");
            int bodyAnimationRomanInstanceId = timeline.AddAnimation("BaseAnimation", bodyAnimationRoman, 0, true);
            var bodyAnimationNormanette = new AnimationClipInstance(testScenes.modelNormanette, "PassSodaC");
            timeline.AddAnimation("BaseAnimation", bodyAnimationNormanette, 0);

            // Create environment animations
            var envController = testScenes.modelPassSodaEnv.GetComponent<EnvironmentController>();
            timeline.AddEnvironmentObjectAnimation("Environment",
                new EnvironmentObjectAnimationInstance(envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "SodaBottle"),
                    "PassSodaBottle", timeline.FrameLength));

            // Load eye gaze
            EyeGazeEditor.LoadEyeGaze(timeline, bodyAnimationNormanInstanceId, "Gaze");
            EyeGazeEditor.LoadEyeGaze(timeline, bodyAnimationRomanInstanceId, "Gaze");
            if (LEAPCore.useExpressiveGaze)
            {
                EyeGazeEditor.LoadExpressiveEyeGazeAnimations(timeline, bodyAnimationNormanInstanceId, "Gaze");
                EyeGazeEditor.LoadExpressiveEyeGazeAnimations(timeline, bodyAnimationRomanInstanceId, "Gaze");
            }
            EyeGazeEditor.PrintEyeGaze(timeline);

            // Initialize test scenario
            editTestScenario.models = new GameObject[3];
            editTestScenario.models[0] = testScenes.modelNorman;
            editTestScenario.models[1] = testScenes.modelRoman;
            editTestScenario.models[2] = testScenes.modelNormanette;
            editTestScenario.animations = new string[3];
            editTestScenario.animations[0] = "PassSodaAwEdits";
            editTestScenario.animations[1] = "PassSodaBwEdits";
            editTestScenario.animations[2] = "PassSodaCwEdits";
            editTestScenario.objectAnimations = new string[1];
            editTestScenario.objectAnimations[0] = "PassSodaBottle";
        }
        else if (sceneName == "Walking90deg")
        {
            testScenes.modelNorman.SetActive(true);
            testScenes.modelWalking90degEnv.SetActive(true);
            testScenes.cameraWalking90deg.enabled = true;

            // Add character models to the timeline
            timeline.AddModel(testScenes.modelNorman);

            // Set environment in the timeline
            timeline.SetEnvironment(testScenes.modelWalking90degEnv);

            // Create animation instances
            var bodyAnimationNorman = new AnimationClipInstance(testScenes.modelNorman, "Walking90deg");
            int bodyAnimationNormanInstanceId = timeline.AddAnimation("BaseAnimation", bodyAnimationNorman, 0, true);

            // Load eye gaze
            EyeGazeEditor.LoadEyeGaze(timeline, bodyAnimationNormanInstanceId, "Gaze");
            if (LEAPCore.useExpressiveGaze)
                EyeGazeEditor.LoadExpressiveEyeGazeAnimations(timeline, bodyAnimationNormanInstanceId, "Gaze");
            EyeGazeEditor.PrintEyeGaze(timeline);

            // Initialize test scenario
            editTestScenario.models = new GameObject[1];
            editTestScenario.models[0] = testScenes.modelNorman;
            editTestScenario.animations = new string[1];
            editTestScenario.animations[0] = "Walking90degwEdits";
            editTestScenario.cameraAnimations = new string[1];
            editTestScenario.cameraAnimations[0] = "Walking90degCamera";
        }
        else if (sceneName == "HandShake")
        {
            testScenes.modelNorman.SetActive(true);
            testScenes.modelRoman.SetActive(true);
            testScenes.modelHandShakeEnv.SetActive(true);
            testScenes.cameraHandShake.enabled = true;

            // Add character models to the timeline
            timeline.AddModel(testScenes.modelNorman);
            timeline.AddModel(testScenes.modelRoman);

            // Set environment in the timeline
            timeline.SetEnvironment(testScenes.modelHandShakeEnv);

            // Create animation instances
            var bodyAnimationNorman = new AnimationClipInstance(testScenes.modelNorman, "HandShakeA");
            int bodyAnimationNormanInstanceId = timeline.AddAnimation("BaseAnimation", bodyAnimationNorman, 0, true);
            var bodyAnimationRoman = new AnimationClipInstance(testScenes.modelRoman, "HandShakeB");
            int bodyAnimationRomanInstanceId = timeline.AddAnimation("BaseAnimation", bodyAnimationRoman, 0, true);

            // Load eye gaze
            EyeGazeEditor.LoadEyeGaze(timeline, bodyAnimationNormanInstanceId, "Gaze");
            EyeGazeEditor.LoadEyeGaze(timeline, bodyAnimationRomanInstanceId, "Gaze");
            if (LEAPCore.useExpressiveGaze)
            {
                EyeGazeEditor.LoadExpressiveEyeGazeAnimations(timeline, bodyAnimationNormanInstanceId, "Gaze");
                EyeGazeEditor.LoadExpressiveEyeGazeAnimations(timeline, bodyAnimationRomanInstanceId, "Gaze");
            }
            EyeGazeEditor.PrintEyeGaze(timeline);

            // Initialize test scenario
            editTestScenario.models = new GameObject[2];
            editTestScenario.models[0] = testScenes.modelNorman;
            editTestScenario.models[1] = testScenes.modelRoman;
            editTestScenario.animations = new string[2];
            editTestScenario.animations[0] = "HandShakeAwEdits";
            editTestScenario.animations[1] = "HandShakeBwEdits";
        }
        else if (sceneName == "BookShelf")
        {
            testScenes.modelNorman.SetActive(true);
            testScenes.modelBookShelfEnv.SetActive(true);
            testScenes.cameraBookShelf1.enabled = true;
            testScenes.cameraBookShelf2.enabled = true;

            // Add character models to the timeline
            timeline.AddModel(testScenes.modelNorman);

            // Set environment in the timeline
            timeline.SetEnvironment(testScenes.modelBookShelfEnv);

            // Create animation instances
            var bodyAnimationNorman = new AnimationClipInstance(testScenes.modelNorman, "BookShelf");
            int bodyAnimationNormanInstanceId = timeline.AddAnimation("BaseAnimation", bodyAnimationNorman, 0, true);

            // Load eye gaze
            EyeGazeEditor.LoadEyeGaze(timeline, bodyAnimationNormanInstanceId, "Gaze");
            if (LEAPCore.useExpressiveGaze)
                EyeGazeEditor.LoadExpressiveEyeGazeAnimations(timeline, bodyAnimationNormanInstanceId, "Gaze");
            EyeGazeEditor.PrintEyeGaze(timeline);

            // Create environment animations
            var envController = testScenes.modelBookShelfEnv.GetComponent<EnvironmentController>();
            timeline.AddEnvironmentObjectAnimation("Environment",
                new EnvironmentObjectAnimationInstance(envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "Book1"),
                    "BookShelfBook1", timeline.FrameLength));
            timeline.AddEnvironmentObjectAnimation("Environment",
                new EnvironmentObjectAnimationInstance(envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "Book2"),
                    "BookShelfBook2", timeline.FrameLength));
            timeline.AddEnvironmentObjectAnimation("Environment",
                new EnvironmentObjectAnimationInstance(envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "Book3"),
                    "BookShelfBook3", timeline.FrameLength));

            // Initialize test scenario
            editTestScenario.models = new GameObject[1];
            editTestScenario.models[0] = testScenes.modelNorman;
            editTestScenario.animations = new string[1];
            editTestScenario.animations[0] = "BookShelfwEdits";
            editTestScenario.objectAnimations = new string[3];
            editTestScenario.animations[0] = "BookShelfBook1";
            editTestScenario.animations[1] = "BookShelfBook2";
            editTestScenario.animations[2] = "BookShelfBook3";
        }
        else if (sceneName == "StealDiamond")
        {
            testScenes.modelNorman.SetActive(true);
            testScenes.modelStealDiamondEnv.SetActive(true);
            testScenes.cameraStealDiamond1.enabled = true;
            testScenes.cameraStealDiamond2.enabled = true;

            // Add character models to the timeline
            timeline.AddModel(testScenes.modelNorman);

            // Set environment in the timeline
            timeline.SetEnvironment(testScenes.modelStealDiamondEnv);

            // Create animation instances
            var bodyAnimationNorman = new AnimationClipInstance(testScenes.modelNorman, "StealDiamond");
            int bodyAnimationNormanInstanceId = timeline.AddAnimation("BaseAnimation", bodyAnimationNorman, 0, true);

            // Create environment animations
            var envController = testScenes.modelStealDiamondEnv.GetComponent<EnvironmentController>();
            timeline.AddEnvironmentObjectAnimation("Environment",
                new EnvironmentObjectAnimationInstance(envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "Gem"),
                    "StealDiamondGem", timeline.FrameLength));

            // Load eye gaze
            EyeGazeEditor.LoadEyeGaze(timeline, bodyAnimationNormanInstanceId, "Gaze");
            if (LEAPCore.useExpressiveGaze)
                EyeGazeEditor.LoadExpressiveEyeGazeAnimations(timeline, bodyAnimationNormanInstanceId, "Gaze");
            EyeGazeEditor.PrintEyeGaze(timeline);

            // Initialize test scenario
            editTestScenario.models = new GameObject[1];
            editTestScenario.models[0] = testScenes.modelNorman;
            editTestScenario.animations = new string[1];
            editTestScenario.animations[0] = "StealDiamondwEdits";
            editTestScenario.objectAnimations = new string[1];
            editTestScenario.objectAnimations[0] = "StealDiamondGem";
            editTestScenario.cameraAnimations = new string[1];
            editTestScenario.cameraAnimations[0] = "StealDiamondCamera1";
        }
        else if (sceneName == "WaitForBus")
        {
            testScenes.modelNorman.SetActive(true);
            testScenes.modelWaitForBusEnv.SetActive(true);
            testScenes.cameraWaitForBus.enabled = true;

            // Add character models to the timeline
            timeline.AddModel(testScenes.modelNorman);

            // Set environment in the timeline
            timeline.SetEnvironment(testScenes.modelWaitForBusEnv);

            // Create animation instances
            var bodyAnimationNorman = new AnimationClipInstance(testScenes.modelNorman, "WaitForBus");
            int bodyAnimationNormanInstanceId = timeline.AddAnimation("BaseAnimation", bodyAnimationNorman, 0, true);

            // Load eye gaze
            EyeGazeEditor.LoadEyeGaze(timeline, bodyAnimationNormanInstanceId, "Gaze");
            if (LEAPCore.useExpressiveGaze)
                EyeGazeEditor.LoadExpressiveEyeGazeAnimations(timeline, bodyAnimationNormanInstanceId, "Gaze");
            EyeGazeEditor.PrintEyeGaze(timeline);

            // Initialize test scenario
            editTestScenario.models = new GameObject[1];
            editTestScenario.models[0] = testScenes.modelNorman;
            editTestScenario.animations = new string[1];
            editTestScenario.animations[0] = "WaitForBuswEdits";
        }
        else // if (sceneName == "InitialPose")
        {
            testScenes.modelNorman.SetActive(true);
            testScenes.cameraWindowWashing.enabled = true;

            // Add character models to the timeline
            timeline.AddModel(testScenes.modelNorman);

            // Set environment in the timeline
            timeline.SetEnvironment(null);

            // Create animation instances
            var bodyAnimationNorman = new AnimationClipInstance(testScenes.modelNorman, "InitialPose");
            int bodyAnimationNormanInstanceId = timeline.AddAnimation("BaseAnimation", bodyAnimationNorman, 0, true);

            // Load eye gaze
            EyeGazeEditor.LoadEyeGaze(timeline, bodyAnimationNormanInstanceId, "Gaze");
            if (LEAPCore.useExpressiveGaze)
                EyeGazeEditor.LoadExpressiveEyeGazeAnimations(timeline, bodyAnimationNormanInstanceId, "Gaze");
            EyeGazeEditor.PrintEyeGaze(timeline);
        }

        timeline.Init();
    }

    [MenuItem("LEAP/Animation/Reset Models to Initial Pose", true, 5)]
    private static bool ValidateResetModelsToInitialPose()
    {
        var wnd = EditorWindow.GetWindow<LeapAnimationEditor>();
        return wnd.Timeline != null;
    }

    [MenuItem("LEAP/Animation/Reset Models to Initial Pose", false, 5)]
    private static void ResetModelsToInitialPose()
    {
        var wnd = EditorWindow.GetWindow<LeapAnimationEditor>();
        var timeline = wnd.Timeline;
        timeline.ResetModelsAndEnvironment();
        EyeGazeEditor.ResetEyeGazeControllers(timeline.Models.ToArray());
        SceneView.RepaintAll();
    }

    [MenuItem("LEAP/Animation/Bake Animation", true, 6)]
    private static bool ValidateBakeAnimation()
    {
        var wnd = EditorWindow.GetWindow<LeapAnimationEditor>();
        return wnd.Timeline != null;
    }

    [MenuItem("LEAP/Animation/Bake Animation", false, 6)]
    private static void BakeAnimation()
    {
        var wnd = EditorWindow.GetWindow<LeapAnimationEditor>();
        var timeline = wnd.Timeline;
        var models = timeline.Models;
        string[] baseAnimationClipNames = new string[models.Count];
        
        for (int modelIndex = 0; modelIndex < models.Count; ++modelIndex)
        {
            var model = models[modelIndex];
            var baseAnimation = timeline.GetLayer("BaseAnimation").Animations.FirstOrDefault(a => a.Animation.Model == model).Animation;
            baseAnimationClipNames[modelIndex] = baseAnimation.AnimationClip.name + "wEdits";
        }
        
        timeline.Bake(baseAnimationClipNames);
    }

    [MenuItem("LEAP/Animation/Infer Eye Gaze/Instances", true, 7)]
    private static bool ValidateInferEyeGazeInstances()
    {
        var wnd = EditorWindow.GetWindow<LeapAnimationEditor>();
        return wnd.Timeline != null && wnd.Timeline.GetLayer("Gaze") != null;
    }

    [MenuItem("LEAP/Animation/Infer Eye Gaze/Instances", false, 7)]
    private static void InferEyeGazeInstances()
    {
        var wnd = EditorWindow.GetWindow<LeapAnimationEditor>();
        var timeline = wnd.Timeline;

        // Disable IK and gaze layers
        timeline.SetIKEnabled(false);
        timeline.GetLayer("Gaze").Active = false;

        // Infer gaze shifts and fixations in the base animation
        var baseLayer = timeline.GetLayer("BaseAnimation");
        foreach (var baseAnimation in baseLayer.Animations)
        {
            EyeGazeEditor.InferEyeGazeInstances(timeline, baseAnimation.InstanceId);

            // Save and print inferred eye gaze
            EyeGazeEditor.SaveEyeGaze(timeline, baseAnimation.InstanceId, "#Inferred");
            EyeGazeEditor.PrintEyeGaze(timeline);
        }
        SceneView.RepaintAll();
    }

    [MenuItem("LEAP/Animation/Infer Eye Gaze/Alignments", true, 7)]
    private static bool ValidateInferEyeGazeAlignments()
    {
        var wnd = EditorWindow.GetWindow<LeapAnimationEditor>();
        return wnd.Timeline != null && wnd.Timeline.GetLayer("Gaze") != null;
    }

    [MenuItem("LEAP/Animation/Infer Eye Gaze/Alignments", false, 7)]
    private static void InferEyeGazeAlignments()
    {
        var wnd = EditorWindow.GetWindow<LeapAnimationEditor>();
        var timeline = wnd.Timeline;

        // Disable IK and gaze layers
        timeline.SetIKEnabled(false);
        timeline.GetLayer("Gaze").Active = false;

        // Infer head and torso alignments for all gaze shifts in the base animation
        var baseLayer = timeline.GetLayer("BaseAnimation");
        foreach (var baseAnimation in baseLayer.Animations)
        {
            EyeGazeEditor.InferEyeGazeAlignments(timeline, baseAnimation.InstanceId);

            // Save and print inferred eye gaze
            EyeGazeEditor.SaveEyeGaze(timeline, baseAnimation.InstanceId, "#Inferred");
            EyeGazeEditor.PrintEyeGaze(timeline);
        }
        SceneView.RepaintAll();
    }

    [MenuItem("LEAP/Animation/Infer Eye Gaze/Expressive Motion", true, 8)]
    private static bool ValidateInferEyeGazeExpressiveMotion()
    {
        var wnd = EditorWindow.GetWindow<LeapAnimationEditor>();
        return wnd.Timeline != null && wnd.Timeline.GetLayer("Gaze") != null;
    }

    [MenuItem("LEAP/Animation/Infer Eye Gaze/Expressive Motion", false, 8)]
    private static void InferEyeGazeExpressiveMotion()
    {
        var wnd = EditorWindow.GetWindow<LeapAnimationEditor>();
        var timeline = wnd.Timeline;

        // Disable IK and gaze layers
        timeline.SetIKEnabled(false);
        timeline.GetLayer("Gaze").Active = false;

        // Extract expressive motion for all gaze shifts in the base animation
        var baseLayer = timeline.GetLayer("BaseAnimation");
        foreach (var baseAnimation in baseLayer.Animations)
        {
            EyeGazeEditor.ExtractExpressiveEyeGazeAnimations(timeline, baseAnimation.InstanceId);
            EyeGazeEditor.SaveExpressiveEyeGazeAnimations(timeline, baseAnimation.InstanceId);
        }
        SceneView.RepaintAll();
    }

    [MenuItem("LEAP/Animation/Load Eye Gaze/Hand-annotated", true, 10)]
    private static bool ValidateLoadEyeGaze()
    {
        var wnd = EditorWindow.GetWindow<LeapAnimationEditor>();
        return wnd.Timeline != null;
    }

    [MenuItem("LEAP/Animation/Load Eye Gaze/Hand-annotated", false, 10)]
    private static void LoadEyeGaze()
    {
        var wnd = EditorWindow.GetWindow<LeapAnimationEditor>();
        var timeline = wnd.Timeline;
        var models = timeline.Models;

        for (int modelIndex = 0; modelIndex < models.Count; ++modelIndex)
        {
            var model = models[modelIndex];
            var baseAnimation = timeline.GetLayer("BaseAnimation").Animations.FirstOrDefault(a => a.Animation.Model == model);
            EyeGazeEditor.LoadEyeGaze(timeline, baseAnimation.InstanceId, "Gaze");
        }

        EyeGazeEditor.PrintEyeGaze(timeline);
    }

    [MenuItem("LEAP/Animation/Load Eye Gaze/Inferred", true, 11)]
    private static bool ValidateLoadEyeGazeInferred()
    {
        var wnd = EditorWindow.GetWindow<LeapAnimationEditor>();
        return wnd.Timeline != null;
    }

    [MenuItem("LEAP/Animation/Load Eye Gaze/Inferred", false, 11)]
    private static void LoadEyeGazeInferred()
    {
        var wnd = EditorWindow.GetWindow<LeapAnimationEditor>();
        var timeline = wnd.Timeline;
        var models = timeline.Models;

        for (int modelIndex = 0; modelIndex < models.Count; ++modelIndex)
        {
            var model = models[modelIndex];
            var baseAnimation = timeline.GetLayer("BaseAnimation").Animations.FirstOrDefault(a => a.Animation.Model == model);
            EyeGazeEditor.LoadEyeGaze(timeline, baseAnimation.InstanceId, "Gaze", "#Inferred");
        }

        EyeGazeEditor.PrintEyeGaze(timeline);
    }

    [MenuItem("LEAP/Animation/Load Eye Gaze/Edits", true, 12)]
    private static bool ValidateLoadEyeGazeEdits()
    {
        var wnd = EditorWindow.GetWindow<LeapAnimationEditor>();
        return wnd.Timeline != null;
    }

    [MenuItem("LEAP/Animation/Load Eye Gaze/Edits", false, 12)]
    private static void LoadEyeGazeEdits()
    {
        var wnd = EditorWindow.GetWindow<LeapAnimationEditor>();
        var timeline = wnd.Timeline;
        var models = timeline.Models;

        for (int modelIndex = 0; modelIndex < models.Count; ++modelIndex)
        {
            var model = models[modelIndex];
            var baseAnimation = timeline.GetLayer("BaseAnimation").Animations.FirstOrDefault(a => a.Animation.Model == model);
            EyeGazeEditor.LoadEyeGaze(timeline, baseAnimation.InstanceId, "Gaze", "#Edits");
        }

        EyeGazeEditor.PrintEyeGaze(timeline);
    }

    [MenuItem("LEAP/Animation/Save Eye Gaze", true, 13)]
    private static bool ValidateSaveEyeGaze()
    {
        var wnd = EditorWindow.GetWindow<LeapAnimationEditor>();
        return wnd.Timeline != null && wnd.Timeline.GetLayer("Gaze") != null;
    }

    [MenuItem("LEAP/Animation/Save Eye Gaze", false, 13)]
    private static void SaveEyeGaze()
    {
        var wnd = EditorWindow.GetWindow<LeapAnimationEditor>();
        var timeline = wnd.Timeline;
        var models = timeline.Models;

        for (int modelIndex = 0; modelIndex < models.Count; ++modelIndex)
        {
            var model = models[modelIndex];
            var baseAnimation = timeline.GetLayer("BaseAnimation").Animations.FirstOrDefault(a => a.Animation.Model == model);
            EyeGazeEditor.SaveEyeGaze(timeline, baseAnimation.InstanceId, "#Edits");
        }
    }

    [MenuItem("LEAP/Animation/Fix Animation Clip Assoc.", true, 15)]
    private static bool ValidateFixAnimationClipAssoc()
    {
        GameObject obj = Selection.activeGameObject;
        if (obj == null || obj.GetComponent<Animation>() == null)
        {
            return false;
        }

        return true;
    }

    [MenuItem("LEAP/Animation/Fix Animation Clip Assoc.", false, 15)]
    private static void FixAnimationClipAssoc()
    {
        GameObject obj = Selection.activeGameObject;
        LEAPAssetUtils.FixModelAnimationClipAssoc(obj);
    }

    /// <summary>
    /// Validates the specified menu item.
    /// </summary>
    /// <returns>
    /// true if an agent is selected, false otherwise. <see cref="System.Boolean"/>
    /// </returns>
    [MenuItem("LEAP/Animation/Load Morph Channels", true)]
    private static bool ValidateLoadMorphChannels()
    {
        GameObject obj = Selection.activeGameObject;
        if (obj == null || obj.GetComponent<MorphController>() == null)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Saves morph channel mappings for the selected agent
    /// when user clicks a menu item.
    /// </summary>
    [MenuItem("LEAP/Animation/Load Morph Channels")]
    private static void LoadMorphChannels()
    {
        GameObject obj = Selection.activeGameObject;

        // Determine LMC file path
        string lmc_path = _GetLMCPath(obj);
        if (lmc_path == "")
        {
            Debug.LogError("Model " + obj.name +
                           " does not have a link to its prefab. Morph channel mappings cannot be loaded.");
            return;
        }

        // Load LMC
        LMCSerializer lmc = new LMCSerializer();
        if (!lmc.Load(obj, lmc_path))
        {
            // Unable to load morph channel mappings
            Debug.LogError("LMC file not found: " + lmc_path);

            return;
        }

        UnityEngine.Debug.Log("Loaded morph channel mappings for model " + obj.name);
    }

    /// <summary>
    /// Validates the specified menu item.
    /// </summary>
    /// <returns>
    /// true if an agent is selected, false otherwise. <see cref="System.Boolean"/>
    /// </returns>
    [MenuItem("LEAP/Animation/Save Morph Channels", true)]
    private static bool ValidateSaveMorphChannels()
    {
        GameObject obj = Selection.activeGameObject;
        if (obj == null || obj.GetComponent<MorphController>() == null)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Saves morph channel mappings for the selected agent
    /// when user clicks a menu item.
    /// </summary>
    [MenuItem("LEAP/Animation/Save Morph Channels")]
    private static void SaveMorphChannels()
    {
        GameObject obj = Selection.activeGameObject;

        // Determine LMC file path
        string lmc_path = _GetLMCPath(obj);
        if (lmc_path == "")
        {
            lmc_path = "./Assets/Agents/Models/" + obj.name + ".lmc";
            Debug.LogWarning("Model " + obj.name +
                             " does not have a link to its prefab. Saving to default path " + lmc_path);
            return;
        }

        // Serialize LMC
        LMCSerializer lmc = new LMCSerializer();
        if (!lmc.Serialize(obj, lmc_path))
        {
            // Unable to serialize morph channel mappings
            Debug.LogError("LMC file could not be saved: " + lmc_path);

            return;
        }

        UnityEngine.Debug.Log("Saved morph channel mappings for model " + obj.name);
    }

    /// <summary>
    /// Validates the specified menu item.
    /// </summary>
    /// <returns>
    /// true if an agent is selected, false otherwise. <see cref="System.Boolean"/>
    /// </returns>
    [MenuItem("LEAP/Agent Setup/Set Up Default Agent", true)]
    private static bool ValidateSetupDefaultAgent()
    {
        GameObject obj = Selection.activeGameObject;
        if (obj == null)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Automatically tags the selected agent and adds default
    /// functional components when user clicks a menu item.
    /// </summary>
    [MenuItem("LEAP/Agent Setup/Set Up Default Agent")]
    private static void SetupDefaultAgent()
    {
        GameObject obj = Selection.activeGameObject;

        // Tag model parts
        ModelUtils.AutoTagModel(obj);

        // Create default anim. controllers
        AnimControllerTree atree = obj.AddComponent<AnimControllerTree>();
        RootController rootctr = obj.AddComponent<RootController>();
        LocomotionController lococtr = obj.AddComponent<LocomotionController>();
        BodyIdleController idlectr = obj.AddComponent<BodyIdleController>();
        GestureController gestctr = obj.AddComponent<GestureController>();
        //obj.AddComponent<PostureController>();
        FaceController facectr = obj.AddComponent<FaceController>();
        ExpressionController exprctr = obj.AddComponent<ExpressionController>();
        SpeechController spctr = obj.AddComponent<SpeechController>();
        GazeController gazectr = obj.AddComponent<GazeController>();
        BlinkController blinkctr = obj.AddComponent<BlinkController>();
        EyesAliveController eactr = obj.AddComponent<EyesAliveController>();
        // Link the controllers into a hierarchy
        atree.rootController = obj.GetComponent<RootController>();
        rootctr.childControllers = new AnimController[2];
        rootctr.childControllers[0] = lococtr;
        rootctr.childControllers[1] = facectr;
        lococtr.childControllers = new AnimController[2];
        lococtr.childControllers[0] = idlectr;
        lococtr.childControllers[1] = gestctr;
        facectr.childControllers = new AnimController[3];
        facectr.childControllers[0] = exprctr;
        facectr.childControllers[1] = spctr;
        facectr.childControllers[2] = gazectr;
        gazectr.childControllers = new AnimController[2];
        gazectr.childControllers[0] = blinkctr;
        gazectr.childControllers[1] = eactr;
        // Initialize controller states
        rootctr._CreateStates();
        lococtr._CreateStates();
        idlectr._CreateStates();
        gestctr._CreateStates();
        facectr._CreateStates();
        exprctr._CreateStates();
        spctr._CreateStates();
        gazectr._CreateStates();
        blinkctr._CreateStates();
        eactr._CreateStates();
        FaceController._InitRandomHeadMotion(obj);

        // Add GUI helper components
        obj.AddComponent<EyeLaserGizmo>();
    }

    /// <summary>
    /// Validates the specified menu item.
    /// </summary>
    /// <returns>
    /// true if an agent is selected, false otherwise. <see cref="System.Boolean"/>
    /// </returns>
    [MenuItem("LEAP/Agent Setup/Set Up Gaze Agent", true)]
    private static bool ValidateSetupGazeAgent()
    {
        GameObject obj = Selection.activeGameObject;
        if (obj == null)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Automatically tags the selected agent and adds default
    /// functional components when user clicks a menu item.
    /// </summary>
    [MenuItem("LEAP/Agent Setup/Set Up Gaze Agent")]
    private static void SetupGazeAgent()
    {
        GameObject obj = Selection.activeGameObject;

        // Tag model parts
        ModelUtils.AutoTagModel(obj);

        // Create default anim. controllers
        AnimControllerTree atree = obj.AddComponent<AnimControllerTree>();
        RootController rootctr = obj.AddComponent<RootController>();
        GazeController gazectr = obj.AddComponent<GazeController>();
        BlinkController blinkctr = obj.AddComponent<BlinkController>();

        // Link the controllers into a hierarchy
        atree.rootController = obj.GetComponent<RootController>();
        rootctr.childControllers = new AnimController[1];
        rootctr.childControllers[0] = gazectr;
        gazectr.childControllers = new AnimController[1];
        gazectr.childControllers[0] = blinkctr;

        // Initialize controller states
        rootctr._CreateStates();
        gazectr._CreateStates();
        blinkctr._CreateStates();

        // Add GUI helper components
        obj.AddComponent<EyeLaserGizmo>();
    }

    /// <summary>
    /// Validates the specified menu item.
    /// </summary>
    /// <returns>
    /// true if an agent is selected, false otherwise. <see cref="System.Boolean"/>
    /// </returns>
    [MenuItem("LEAP/Agent Setup/Refresh Agent", true)]
    private static bool ValidateRefreshAgent()
    {
        GameObject obj = Selection.activeGameObject;
        if (obj == null)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Automatically tags the selected agent and adds default
    /// functional components when user clicks a menu item.
    /// </summary>
    [MenuItem("LEAP/Agent Setup/Refresh Agent")]
    private static void RefreshAgent()
    {
        GameObject obj = Selection.activeGameObject;
        GameObject baseobj = (GameObject)EditorUtility.GetPrefabParent(obj);

        // Find the modified imported model
        GameObject[] objs = GameObject.FindObjectsOfTypeIncludingAssets(typeof(GameObject)) as GameObject[];
        foreach (GameObject new_baseobj in objs)
        {
            if (new_baseobj.name != baseobj.name ||
               EditorUtility.GetPrefabType(new_baseobj) != PrefabType.ModelPrefab)
                continue;

            LEAPAssetUtils.RefreshAgentModel(new_baseobj, obj);
            return;
        }
    }

    /// <summary>
    /// Validates the specified menu item.
    /// </summary>
    /// <returns>
    /// true if an agent is selected, false otherwise.
    /// </returns>
    [MenuItem("LEAP/Agent Setup/Init. Controller States", true)]
    private static bool ValidateCreateFSMStates()
    {
        GameObject obj = Selection.activeGameObject;
        if (obj == null)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Initializes default state definitions of the 
    /// animation controllers defined on the selected agent.
    /// </summary>
    [MenuItem("LEAP/Agent Setup/Init. Controller States")]
    private static void CreateFSMStates()
    {
        GameObject obj = Selection.activeGameObject;
        Component[] comp_list = obj.GetComponents<AnimController>();

        foreach (Component comp in comp_list)
        {
            if (!(comp is AnimController))
                continue;

            AnimController anim_ctrl = (AnimController)comp;
            anim_ctrl._CreateStates();
        }
    }

    /// <summary>
    /// Validates the specified menu item.
    /// </summary>
    /// <returns>
    /// true if a photo mosaic is selected, false otherwise.
    /// </returns>
    [MenuItem("LEAP/Scenario/Mosaic Keyframe")]
    private static bool ValidateMosaicKeyframe()
    {
        GameObject obj = Selection.activeGameObject;
        if (obj == null || obj.GetComponent<PhotoMosaic>() == null)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Keyframes the current position and scale of the photo mosaic.
    /// </summary>
    [MenuItem("LEAP/Scenario/Mosaic Keyframe")]
    private static void MosaicKeyframe()
    {
        GameObject obj = Selection.activeGameObject;
        PhotoMosaic pm = obj.GetComponent<PhotoMosaic>();

        // Extend keyframe array
        int nkfi = pm.keyFrames.Length;
        PhotoMosaic.KeyFrame[] kfs = new PhotoMosaic.KeyFrame[nkfi + 1];
        pm.keyFrames.CopyTo(kfs, 0);
        pm.keyFrames = kfs;
        pm.keyFrames[nkfi] = new PhotoMosaic.KeyFrame();

        // Fill new keyframe
        pm.keyFrames[nkfi].name = "NewKeyFrame";
        pm.keyFrames[nkfi].position = pm.transform.localPosition;
        pm.keyFrames[nkfi].scale = pm.transform.localScale;
    }

    private static string _GetLMCPath(GameObject obj)
    {
        // Determine original asset path
        string asset_path = "";
        UnityEngine.Object prefab = EditorUtility.GetPrefabParent(obj);
        if (prefab != null)
        {
            asset_path = AssetDatabase.GetAssetPath(prefab);
        }
        else
        {
            return "";
        }

        // Determine LMC file path
        string lmc_path = "";
        int ext_i = asset_path.LastIndexOf(".", StringComparison.InvariantCultureIgnoreCase);
        if (ext_i >= 0)
        {
            lmc_path = asset_path.Substring(0, ext_i) + ".lmc";
        }
        else
        {
            lmc_path += ".lmc";
        }

        return lmc_path;
    }

}

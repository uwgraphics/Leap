using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Core class for the Leap animation system.
/// </summary>
public class AnimationManager
{
    private static AnimationManager _instance = null;

    /// <summary>
    /// Animation manager instance.
    /// </summary>
    public static AnimationManager Instance
    {
        get
        {
            if (_instance == null)
                _instance = new AnimationManager();

            return _instance;
        }
    }

    /// <summary>
    /// Animation timeline.
    /// </summary>
    public AnimationTimeline Timeline
    {
        get;
        private set;
    }

    /// <summary>
    /// List of animated character models.
    /// </summary>
    public IList<GameObject> Models
    {
        get { return _models.AsReadOnly(); }
    }

    /// <summary>
    /// Root object of the environment.
    /// </summary>
    public GameObject Environment
    {
        get { return _environment; }
    }

    /// <summary>
    /// List of animation controller types listed by their execution order.
    /// </summary>
    public IList<Type> _ControllersByExecOrder
    {
        get { return _controllersByExecOrder.AsReadOnly(); }
    }

    private List<GameObject> _models = new List<GameObject>();
    private GameObject _environment = null;
    private List<Type> _controllersByExecOrder = null;

    /// <summary>
    /// Constructor.
    /// </summary>
    private AnimationManager()
    {
        Timeline = new AnimationTimeline(this);
    }

    /// <summary>
    /// Initialize the animation manager.
    /// </summary>
    public void Init()
    {
        _InitControllerExecOrder();
        Timeline._Init();
    }

    /// <summary>
    /// Add a character model to the timeline.
    /// </summary>
    /// <param name="model">Character model</param>
    public void AddModel(GameObject model)
    {
        if (_models.Any(m => m == model))
        {
            throw new Exception(string.Format("Character model {0} already added", model.name));
        }

        // Initialize the character's model controller
        var modelController = model.GetComponent<ModelController>();
        if (modelController == null)
        {
            throw new Exception(string.Format("Character model {0} does not have a ModelController", model.name));
        }
        modelController.Init();

        // Initialize the character morph controller
        var morphController = model.GetComponent<MorphController>();
        if (morphController != null)
        {
            morphController.Init();
        }

        // Apply & store initial pose for the model
        var initialPoseClip = model.GetComponent<Animation>().GetClip("InitialPose");
        if (initialPoseClip == null)
        {
            throw new Exception(string.Format("Character model {0} does not have an InitialPose animation defined", model.name));
        }
        var initialPoseInstance = new AnimationClipInstance(initialPoseClip.name, model, false, false, false);
        initialPoseInstance.Apply(0, AnimationLayerMode.Override);
        modelController.Init();

        // Add the model to the timeline
        _models.Add(model);
    }

    /// <summary>
    /// Remove a character model from the timeline.
    /// </summary>
    /// <param name="modelName">Character model name</param>
    public void RemoveModel(string modelName)
    {
        if (Timeline.Layers.Any(l => l.Animations.Any(a => a.Animation.Model.name == modelName)))
        {
            throw new Exception(string.Format(
                "Cannot remove character model {0}, because it is referenced in at least one animation instance", modelName));
        }

        _models.RemoveAll(m => m.name == modelName);
    }

    /// <summary>
    /// Remove all character models from the timeline.
    /// </summary>
    public void RemoveAllModels()
    {
        if (Timeline.Layers.Any(l => l.Animations.Count > 0))
        {
            throw new Exception(string.Format(
                "Cannot remove character models, because there are still animation instances referencing them"));
        }

        _models.Clear();
    }

    /// <summary>
    /// Set environment containing objects manipulated in animations.
    /// </summary>
    /// <param name="environment">Environment root object</param>
    public void SetEnvironment(GameObject environment)
    {
        _environment = environment;
        if (environment == null)
            return;

        var envController = environment.GetComponent<EnvironmentController>();
        if (envController == null)
            throw new Exception(string.Format("Environment root object {0} does not have an EnvironmentController", environment.name));

        envController.Init();
    }

    // Initialize the list specifying the execution order for animation controllers
    private void _InitControllerExecOrder()
    {
        _controllersByExecOrder = new List<Type>();

        var scripts = UnityEditor.MonoImporter.GetAllRuntimeMonoScripts();
        var scriptsByExecOrder = new List<UnityEditor.MonoScript>();
        foreach (var script in scripts)
        {
            if (script.name != "" && script.GetClass() != null &&
                script.GetClass().IsSubclassOf(typeof(AnimController)))
            {
                int scriptExecOrder = UnityEditor.MonoImporter.GetExecutionOrder(script);

                // Add the controller to the execution order list
                bool added = false;
                for (int scriptIndex = 0; scriptIndex < scriptsByExecOrder.Count; ++scriptIndex)
                {
                    int curScriptExecOrder = UnityEditor.MonoImporter.GetExecutionOrder(scriptsByExecOrder[scriptIndex]);
                    if (scriptExecOrder < curScriptExecOrder)
                    {
                        scriptsByExecOrder.Insert(scriptIndex, script);
                        added = true;
                        break;
                    }
                }
                if (!added)
                    scriptsByExecOrder.Add(script);
            }
        }

        foreach (var script in scriptsByExecOrder)
        {
            _controllersByExecOrder.Add(script.GetClass());
        }

    }

    /// <summary>
    /// Print animation instances scheduled on the timeline.
    /// </summary>
    public void PrintAnimationInstances()
    {
        foreach (var layer in Timeline.Layers)
        {
            Debug.Log("LAYER " + layer.LayerName);

            foreach (var scheduledInstance in layer.Animations)
            {
                var instance = scheduledInstance.Animation;

                Debug.Log(string.Format("{0}::{1}, S = {2}, E = {3}", instance.Model.name, instance.Name,
                    scheduledInstance.StartFrame, scheduledInstance.EndFrame));
            }
        }
    }

    /// <summary>
    /// Load one of the predefined example scenes
    /// </summary>
    /// <param name="sceneName">Example scene name</param>
    public static void LoadExampleScene(string sceneName)
    {
        var timeline = AnimationManager.Instance.Timeline;
        var gazeTargets = GameObject.FindGameObjectsWithTag("GazeTarget");

        var editTestScenario = GameObject.FindGameObjectWithTag("ScenarioManager").GetComponent<GazeEditTestScenario>();
        var testScenes = GameObject.Find("EyeGazeEditor").GetComponent<EyeGazeEditTestScenes>();
        timeline.RemoveAllLayers();
        timeline.OwningManager.RemoveAllModels();

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
        testScenes.cameraStackBoxes.enabled = false;
        testScenes.modelKinect.SetActive(false);
        testScenes.modelEyeTrackMocapTest1Env.SetActive(false);
        testScenes.cameraEyeTrackMocapTest1.enabled = false;
        testScenes.modelNormanNew.SetActive(false);
        testScenes.modelWindowWashingNewEnv.SetActive(false);
        testScenes.modelStackBoxesEnv.SetActive(false);

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
            testScenes.cameraWindowWashing.enabled = true;

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
            testScenes.cameraWindowWashing.enabled = true;

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
            timeline.AddManipulatedObjectAnimation(LEAPCore.environmentAnimationLayerName,
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
            testScenes.modelNormanette.SetActive(true);
            testScenes.modelNormanette.transform.position = new Vector3(-4.97f, 0f, 1.24f);
            testScenes.modelNormanette.transform.localScale = new Vector3(0.96f, 0.91f, 0.96f);
            testScenes.modelPassSodaEnv.SetActive(true);
            testScenes.cameraPassSoda.enabled = true;

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
            timeline.AddManipulatedObjectAnimation(LEAPCore.environmentAnimationLayerName,
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
            testScenes.cameraWalking90deg.enabled = true;

            // Add character models to the timeline
            timeline.OwningManager.AddModel(testScenes.modelNorman);

            // Set environment in the timeline
            timeline.OwningManager.SetEnvironment(testScenes.modelWalking90degEnv);

            // Create animation instances
            var bodyAnimationNorman = new AnimationClipInstance("Walking90deg", testScenes.modelNorman);
            int bodyAnimationNormanInstanceId = timeline.AddAnimation(LEAPCore.baseAnimationLayerName,
                bodyAnimationNorman, 0, LEAPCore.helperAnimationLayerName);

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
            testScenes.cameraHandShake.enabled = true;

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
            testScenes.cameraBookShelf1.enabled = true;
            testScenes.cameraBookShelf2.enabled = true;

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
            timeline.AddManipulatedObjectAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance("BookShelfBook1", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "Book1"),
                    false, false, false));
            timeline.AddManipulatedObjectAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance("BookShelfBook2", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "Book2"),
                    false, false, false));
            timeline.AddManipulatedObjectAnimation(LEAPCore.environmentAnimationLayerName,
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
            testScenes.cameraStealDiamond1.enabled = true;
            testScenes.cameraStealDiamond2.enabled = true;

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
            timeline.AddManipulatedObjectAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance("StealDiamondGem", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "Gem"),
                    false, false, false));

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
            testScenes.cameraWaitForBus.enabled = true;

            // Add character models to the timeline
            timeline.OwningManager.AddModel(testScenes.modelNorman);

            // Set environment in the timeline
            timeline.OwningManager.SetEnvironment(testScenes.modelWaitForBusEnv);

            // Create animation instances
            var bodyAnimationNorman = new AnimationClipInstance("WaitForBus", testScenes.modelNorman);
            int bodyAnimationNormanInstanceId = timeline.AddAnimation(LEAPCore.baseAnimationLayerName,
                bodyAnimationNorman, 0, LEAPCore.helperAnimationLayerName);

            // Load eye gaze
            EyeGazeEditor.LoadEyeGaze(timeline, bodyAnimationNormanInstanceId, LEAPCore.eyeGazeAnimationLayerName);
            EyeGazeEditor.PrintEyeGaze(timeline);

            // Initialize test scenario
            editTestScenario.models = new GameObject[1];
            editTestScenario.models[0] = testScenes.modelNorman;
            editTestScenario.animations = new string[1];
            editTestScenario.animations[0] = "WaitForBus-" + LEAPCore.defaultBakedTimelineName;
        }
        else if (sceneName == "EyeTrackMocapTest1-1")
        {
            testScenes.modelKinect.SetActive(true);
            testScenes.modelEyeTrackMocapTest1Env.SetActive(true);
            testScenes.cameraEyeTrackMocapTest1.enabled = true;

            // Add character models to the timeline
            timeline.OwningManager.AddModel(testScenes.modelKinect);

            // Set environment in the timeline
            timeline.OwningManager.SetEnvironment(testScenes.modelEyeTrackMocapTest1Env);

            // Create animation instances
            var bodyAnimation = new AnimationClipInstance("EyeTrackMocapTest1-1", testScenes.modelKinect);
            int bodyAnimationInstanceId = timeline.AddAnimation(LEAPCore.baseAnimationLayerName,
                bodyAnimation, 0, LEAPCore.helperAnimationLayerName);
            // TODO: remove this
            /*// Get objects in the scene
            var gazeController = bodyAnimation.Model.GetComponent<GazeController>();
            var lEye = gazeController.lEye.Top;
            var rEye = gazeController.rEye.Top;
            var tableSpots = GameObject.FindGameObjectsWithTag("GazeTarget");
            var leftHandSpot = tableSpots.FirstOrDefault(obj => obj.name == "LeftHandSpot");
            var rightandSpot = tableSpots.FirstOrDefault(obj => obj.name == "RightHandSpot");
            var midTarget = tableSpots.FirstOrDefault(obj => obj.name == "MidTarget");
            var rightTarget = tableSpots.FirstOrDefault(obj => obj.name == "RightTarget");
            var leftTarget = tableSpots.FirstOrDefault(obj => obj.name == "LeftTarget");

            // Load eye tracking data
            var eyeTrackData = new EyeTrackData(testScenes.modelKinect, bodyAnimation.AnimationClip);

            // Get ground-truth eye directions at frame 277 (104)
            var vle = eyeTrackData.Samples[277].lEyeDirection;
            var vre = eyeTrackData.Samples[277].rEyeDirection;

            // Get eye tracker bone space eye directions at frame 277 (104)
            bodyAnimation.Apply(104, AnimationLayerMode.Override);
            var eyeTrackerBone = eyeTrackData.EyeTrackerBone;
            var ule = lEye.InverseTransformDirection((rightTarget.transform.position - lEye.position).normalized);
            var ure = rEye.InverseTransformDirection((rightTarget.transform.position - rEye.position).normalized);

            // Compute aligning rotations
            var qle = Quaternion.FromToRotation(ule, vle);
            var qre = Quaternion.FromToRotation(ure, vre);

            Debug.Log(string.Format("qle at 104: ({0}, {1}, {2})", qle.eulerAngles.x, qle.eulerAngles.y, qle.eulerAngles.z));
            Debug.Log(string.Format("qre at 104: ({0}, {1}, {2})", qre.eulerAngles.x, qre.eulerAngles.y, qre.eulerAngles.z));
            
            // Get ground-truth eye directions at frame 302 (129)
            vle = eyeTrackData.Samples[302].lEyeDirection;
            vre = eyeTrackData.Samples[302].rEyeDirection;

            // Get eye tracker bone space eye directions at frame 302 (129)
            bodyAnimation.Apply(129, AnimationLayerMode.Override);
            ule = lEye.InverseTransformDirection((leftTarget.transform.position - lEye.position).normalized);
            ure = rEye.InverseTransformDirection((leftTarget.transform.position - rEye.position).normalized);

            // Compute aligning rotations
            qle = Quaternion.FromToRotation(ule, vle);
            qre = Quaternion.FromToRotation(ure, vre);

            Debug.Log(string.Format("qle at 129: ({0}, {1}, {2})", qle.eulerAngles.x, qle.eulerAngles.y, qle.eulerAngles.z));
            Debug.Log(string.Format("qre at 129: ({0}, {1}, {2})", qre.eulerAngles.x, qre.eulerAngles.y, qre.eulerAngles.z));

            // Get ground-truth eye directions at frame 332 (159)
            vle = eyeTrackData.Samples[332].lEyeDirection;
            vre = eyeTrackData.Samples[332].rEyeDirection;

            // Get eye tracker bone space eye directions at frame 332 (159)
            bodyAnimation.Apply(159, AnimationLayerMode.Override);
            ule = lEye.InverseTransformDirection((midTarget.transform.position - lEye.position).normalized);
            ure = rEye.InverseTransformDirection((midTarget.transform.position - rEye.position).normalized);

            // Compute aligning rotations
            qle = Quaternion.FromToRotation(ule, vle);
            qre = Quaternion.FromToRotation(ure, vre);

            Debug.Log(string.Format("qle at 159: ({0}, {1}, {2})", qle.eulerAngles.x, qle.eulerAngles.y, qle.eulerAngles.z));
            Debug.Log(string.Format("qre at 159: ({0}, {1}, {2})", qre.eulerAngles.x, qre.eulerAngles.y, qre.eulerAngles.z));*/
            //

            // Load eye gaze
            EyeGazeEditor.LoadEyeGaze(timeline, bodyAnimationInstanceId, LEAPCore.eyeGazeAnimationLayerName);
            EyeGazeEditor.PrintEyeGaze(timeline);

            // Initialize test scenario
            editTestScenario.models = new GameObject[1];
            editTestScenario.models[0] = testScenes.modelKinect;
            editTestScenario.animations = new string[1];
            editTestScenario.animations[0] = "EyeTrackMocapTest1-1-" + LEAPCore.defaultBakedTimelineName;
        }
        else if (sceneName == "WindowWashingNew")
        {
            testScenes.modelNormanNew.SetActive(true);
            testScenes.modelWindowWashingNewEnv.SetActive(true);
            testScenes.cameraWindowWashing.enabled = true;

            // Add character models to the timeline
            timeline.OwningManager.AddModel(testScenes.modelNormanNew);

            // Set environment in the timeline
            timeline.OwningManager.SetEnvironment(testScenes.modelWindowWashingEnv);

            // Create animation instances
            var bodyAnimationNorman = new AnimationClipInstance("WindowWashingNew", testScenes.modelNormanNew);

            // Add animations to characters
            int bodyAnimationNormanInstanceId = timeline.AddAnimation(LEAPCore.baseAnimationLayerName,
                bodyAnimationNorman, 0);

            // Load eye gaze
            EyeGazeEditor.LoadEyeGaze(timeline, bodyAnimationNormanInstanceId, LEAPCore.eyeGazeAnimationLayerName);
            EyeGazeEditor.PrintEyeGaze(timeline);

            // Create environment animations
            var envController = testScenes.modelWindowWashingEnv.GetComponent<EnvironmentController>();
            timeline.AddManipulatedObjectAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance(
                    "WindowWashingNewSponge", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "Sponge"),
                    false, false, false));

            // Add timewarps to the animations
            AnimationTimingEditor.LoadTimewarps(timeline, bodyAnimationNormanInstanceId);

            // Initialize test scenario
            editTestScenario.models = new GameObject[1];
            editTestScenario.models[0] = testScenes.modelNormanNew;
            editTestScenario.animations = new string[1];
            editTestScenario.animations[0] = "WindowWashingNew-" + LEAPCore.defaultBakedTimelineName;
            editTestScenario.objectAnimations = new string[1];
            editTestScenario.objectAnimations[0] = "WindowWashingNewSponge-" + LEAPCore.defaultBakedTimelineName;
        }
        else if (sceneName == "StackBoxes")
        {
            testScenes.modelNormanNew.SetActive(true);
            testScenes.modelStackBoxesEnv.SetActive(true);
            testScenes.cameraStackBoxes.enabled = true;

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
            timeline.AddManipulatedObjectAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance(
                    "StackBoxes1", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "pCube1"),
                    false, false, false));
            timeline.AddManipulatedObjectAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance(
                    "StackBoxes2", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "pCube2"),
                    false, false, false));
            timeline.AddManipulatedObjectAnimation(LEAPCore.environmentAnimationLayerName,
                new AnimationClipInstance(
                    "StackBoxes3", envController.ManipulatedObjects.FirstOrDefault(obj => obj.name == "pCube3"),
                    false, false, false));
            timeline.AddManipulatedObjectAnimation(LEAPCore.environmentAnimationLayerName,
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
        }
        else // if (sceneName == "InitialPose")
        {
            testScenes.modelNorman.SetActive(true);
            testScenes.cameraWindowWashing.enabled = true;

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
}

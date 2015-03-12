using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Linq;

public class LEAPMenu
{
    [MenuItem("LEAP/Animation/Test: WindowWashing", true, 20)]
    private static bool ValidateTestWindowWashing()
    {
        var wnd = EditorWindow.GetWindow<LeapAnimationEditor>();
        if (wnd.Timeline == null)
        {
            return false;
        }

        return true;
    }

    [MenuItem("LEAP/Animation/Test: WindowWashing", false, 20)]
    private static void TestWindowWashing()
    {
        TestScene("WindowWashing");
    }

    [MenuItem("LEAP/Animation/Test: PassSoda", true, 21)]
    private static bool ValidateTestPassSoda()
    {
        var wnd = EditorWindow.GetWindow<LeapAnimationEditor>();
        if (wnd.Timeline == null)
        {
            return false;
        }

        return true;
    }

    [MenuItem("LEAP/Animation/Test: PassSoda", false, 21)]
    private static void TestPassSoda()
    {
        TestScene("PassSoda");
    }

    [MenuItem("LEAP/Animation/Test: Walking90deg", true, 22)]
    private static bool ValidateTestWalking90deg()
    {
        var wnd = EditorWindow.GetWindow<LeapAnimationEditor>();
        if (wnd.Timeline == null)
        {
            return false;
        }

        return true;
    }

    [MenuItem("LEAP/Animation/Test: Walking90deg", false, 22)]
    private static void TestWalking90deg()
    {
        TestScene("Walking90deg");
    }

    [MenuItem("LEAP/Animation/Test: InitialPose", true, 23)]
    private static bool ValidateTestInitialPose()
    {
        var wnd = EditorWindow.GetWindow<LeapAnimationEditor>();
        if (wnd.Timeline == null)
        {
            return false;
        }

        return true;
    }

    [MenuItem("LEAP/Animation/Test: InitialPose", false, 23)]
    private static void TestInitialPose()
    {
        TestScene("InitialPose");
    }

    private static void TestScene(string sceneName, bool loadEditedGaze = false)
    {
        var wnd = EditorWindow.GetWindow<LeapAnimationEditor>();
        var timeline = wnd.Timeline;

        var editTestScenario = GameObject.FindGameObjectWithTag("ScenarioManager").GetComponent<GazeEditTestScenario>();
        var testScenes = GameObject.Find("EyeGazeEditor").GetComponent<EyeGazeEditTestScenes>();
        timeline.RemoveAllLayers();

        // Get all characters and props in the scene
        if (testScenes.modelNorman == null)
            testScenes.modelNorman = GameObject.Find("Norman");
        if (testScenes.modelNormanette == null)
            testScenes.modelNormanette = GameObject.Find("Normanette");
        if (testScenes.modelRoman == null)
            testScenes.modelRoman = GameObject.Find("Roman");
        if (testScenes.modelWindowWashingRoom == null)
            testScenes.modelWindowWashingRoom = GameObject.Find("WindowWashingRoom");

        // Deactivate all characters and props
        testScenes.modelNorman.SetActive(false);
        testScenes.modelNormanette.SetActive(false);
        testScenes.modelRoman.SetActive(false);
        testScenes.modelWindowWashingRoom.SetActive(false);
        testScenes.modelPassSodaRoom.SetActive(false);
        testScenes.modelWalking90degCones.SetActive(false);
        testScenes.cameraWindowWashing.enabled = false;
        testScenes.cameraPassSoda.enabled = false;
        testScenes.cameraWalking90deg.enabled = false;

        // Create and configure animation layers
        timeline.AddLayer(AnimationLayerMode.Override, 0, "BaseAnimation");
        timeline.GetLayer("BaseAnimation").isIKEndEffectorConstr = true;
        timeline.GetLayer("BaseAnimation").isIKBase = true;
        timeline.AddLayer(AnimationLayerMode.Override, 7, "Gaze");
        timeline.GetLayer("Gaze").isIKBase = true;
        timeline.GetLayer("Gaze").isIKGaze = true;

        if (sceneName == "WindowWashing")
        {
            testScenes.modelNorman.SetActive(true);
            testScenes.modelNormanette.SetActive(true);
            testScenes.modelNormanette.transform.position = new Vector3(2.58f, 0f, -3.72f);
            testScenes.modelWindowWashingRoom.SetActive(true);
            testScenes.cameraWindowWashing.enabled = true;

            // Create animation instances
            var bodyAnimationNorman = new AnimationClipInstance(testScenes.modelNorman, "WindowWashingA");
            timeline.AddAnimation("BaseAnimation", bodyAnimationNorman, 0, true);
            var bodyAnimationNormanette = new AnimationClipInstance(testScenes.modelNormanette, "WindowWashingB");
            timeline.AddAnimation("BaseAnimation", bodyAnimationNormanette, 0);

            // Load eye gaze
            EyeGazeEditor.LoadEyeGazeForModel(timeline, bodyAnimationNorman.AnimationClip.name, "Gaze");
            PrintEyeGaze();

            // Initialize test scenario
            editTestScenario.models = new GameObject[2];
            editTestScenario.models[0] = testScenes.modelNorman;
            editTestScenario.models[1] = testScenes.modelNormanette;
            editTestScenario.animations = new string[2];
            editTestScenario.animations[0] = "WindowWashingAwEdits";
            editTestScenario.animations[1] = "WindowWashingBwEdits";
        }
        else if (sceneName == "PassSoda")
        {
            testScenes.modelNorman.SetActive(true);
            testScenes.modelRoman.SetActive(true);
            testScenes.modelNormanette.SetActive(true);
            testScenes.modelNormanette.transform.position = new Vector3(0.36f, 0f, -3.72f);
            testScenes.modelPassSodaRoom.SetActive(true);
            testScenes.cameraPassSoda.enabled = true;

            // Create animation instances
            var bodyAnimationNorman = new AnimationClipInstance(testScenes.modelNorman, "PassSodaA");
            timeline.AddAnimation("BaseAnimation", bodyAnimationNorman, 0, true);
            var bodyAnimationRoman = new AnimationClipInstance(testScenes.modelRoman, "PassSodaB");
            timeline.AddAnimation("BaseAnimation", bodyAnimationRoman, 0, true);
            var bodyAnimationNormanette = new AnimationClipInstance(testScenes.modelNormanette, "PassSodaC");
            timeline.AddAnimation("BaseAnimation", bodyAnimationNormanette, 0);

            // Load eye gaze
            EyeGazeEditor.LoadEyeGazeForModel(timeline, bodyAnimationNorman.AnimationClip.name, "Gaze");
            PrintEyeGaze();

            // Initialize test scenario
            editTestScenario.models = new GameObject[3];
            editTestScenario.models[0] = testScenes.modelNorman;
            editTestScenario.models[1] = testScenes.modelRoman;
            editTestScenario.models[2] = testScenes.modelNormanette;
            editTestScenario.animations = new string[3];
            editTestScenario.animations[0] = "PassSodaAwEdits";
            editTestScenario.animations[1] = "PassSodaBwEdits";
            editTestScenario.animations[2] = "PassSodaCwEdits";
        }
        else if (sceneName == "Walking90deg")
        {
            testScenes.modelNorman.SetActive(true);
            testScenes.modelWalking90degCones.SetActive(true);
            testScenes.cameraWalking90deg.enabled = true;

            // Create animation instances
            var bodyAnimationNorman = new AnimationClipInstance(testScenes.modelNorman, "Walking90deg");
            timeline.AddAnimation("BaseAnimation", bodyAnimationNorman, 0, true);

            // Load eye gaze
            EyeGazeEditor.LoadEyeGazeForModel(timeline, bodyAnimationNorman.AnimationClip.name, "Gaze");
            PrintEyeGaze();

            // Initialize test scenario
            editTestScenario.models = new GameObject[1];
            editTestScenario.models[0] = testScenes.modelNorman;
            editTestScenario.animations = new string[1];
            editTestScenario.animations[0] = "Walking90degwEdits";
        }
        else // if (sceneName == "InitialPose")
        {
            testScenes.modelNorman.SetActive(true);
            testScenes.cameraWindowWashing.enabled = true;

            // Create animation instances
            var bodyAnimationNorman = new AnimationClipInstance(testScenes.modelNorman, "InitialPose");
            timeline.AddAnimation("BaseAnimation", bodyAnimationNorman, 0, true);
            //
            timeline.AddLayer(AnimationLayerMode.Override, 1, "HeadAnimation");
            var headAnimationNorman = new AnimationClipInstance(testScenes.modelNorman, "LookRight");
            timeline.AddAnimation("HeadAnimation", headAnimationNorman, 0);
            timeline.AddAnimation("HeadAnimation", headAnimationNorman, 30);
            timeline.AddAnimation("HeadAnimation", headAnimationNorman, 60);
            /*timeline.AddAnimation("HeadAnimation", headAnimationNorman, 90);
            timeline.AddAnimation("HeadAnimation", headAnimationNorman, 100);
            timeline.AddAnimation("HeadAnimation", headAnimationNorman, 125);*/
            //

            // Load eye gaze
            EyeGazeEditor.LoadEyeGazeForModel(timeline, bodyAnimationNorman.AnimationClip.name, "Gaze");
            PrintEyeGaze();
        }

        timeline.Init();
    }

    private static void PrintEyeGaze()
    {
        var wnd = EditorWindow.GetWindow<LeapAnimationEditor>();
        var timeline = wnd.Timeline;

        foreach (var instance in timeline.GetLayer("Gaze").Animations)
        {
            Debug.Log(string.Format(
                "EyeGazeInstance: model = {0}, animationClip = {1}, startFrame = {2}, fixationStartFrame = {3}, endFrame = {4}, target = {5}, headAlign = {6}, torsoAlign = {7}, isEdit = {8}",
                instance.Animation.Model.gameObject.name, instance.Animation.AnimationClip.name,
                instance.StartFrame,
                instance.StartFrame + (instance.Animation as EyeGazeInstance).FixationStartFrame,
                instance.StartFrame + instance.Animation.FrameLength - 1,
                (instance.Animation as EyeGazeInstance).Target != null ? (instance.Animation as EyeGazeInstance).Target.name : "null",
                (instance.Animation as EyeGazeInstance).HeadAlign, (instance.Animation as EyeGazeInstance).TorsoAlign,
                (instance.Animation as EyeGazeInstance).IsEdit));
        }
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
        timeline.ResetModelsToInitialPose();
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
        var models = timeline.GetAllModels();
        string[] baseAnimationClipNames = new string[models.Length];
        
        for (int modelIndex = 0; modelIndex < models.Length; ++modelIndex)
        {
            var model = models[modelIndex];
            var baseAnimation = timeline.GetLayer("BaseAnimation").Animations.FirstOrDefault(a => a.Animation.Model == model).Animation;
            baseAnimationClipNames[modelIndex] = baseAnimation.AnimationClip.name + "wEdits";
        }
        
        timeline.Bake(baseAnimationClipNames);
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

        var baseLayer = timeline.GetLayer("BaseAnimation");
        foreach (var baseAnimation in baseLayer.Animations)
        {
            var baseAnimationClip = baseAnimation.Animation.AnimationClip;
            EyeGazeEditor.InferEyeGazeAlignments(timeline, baseAnimationClip.name);

            // Save and print inferred eye gaze
            EyeGazeEditor.SaveEyeGazeForModel(timeline, baseAnimationClip.name);
            PrintEyeGaze();
        }
        SceneView.RepaintAll();
    }

    [MenuItem("LEAP/Animation/Fix Animation Clip Assoc.", true, 9)]
    private static bool ValidateFixAnimationClipAssoc()
    {
        GameObject obj = Selection.activeGameObject;
        if (obj == null || obj.GetComponent<Animation>() == null)
        {
            return false;
        }

        return true;
    }

    [MenuItem("LEAP/Animation/Fix Animation Clip Assoc.", false, 9)]
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
        //BlinkController blinkctr = obj.AddComponent<BlinkController>();

        // Link the controllers into a hierarchy
        atree.rootController = obj.GetComponent<RootController>();
        rootctr.childControllers = new AnimController[1];
        rootctr.childControllers[0] = gazectr;

        // Initialize controller states
        rootctr._CreateStates();
        gazectr._CreateStates();

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

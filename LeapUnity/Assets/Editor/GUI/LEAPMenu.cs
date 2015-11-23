using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Linq;

public class LEAPMenu
{
    [MenuItem("LEAP/Animation/E: ExpressiveGaze", false, 30)]
    private static void TestExpressiveGaze()
    {
        AnimationManager.LoadExampleScene("TestExpressiveGaze");
    }

    [MenuItem("LEAP/Animation/E: InitialPose", true, 30)]
    private static bool ValidateTestInitialPose()
    {
        var wnd = EditorWindow.GetWindow<AnimationEditorWindow>();
        if (wnd.Timeline == null)
        {
            return false;
        }

        return true;
    }

    [MenuItem("LEAP/Animation/E: WindowWashing", true, 31)]
    private static bool ValidateTestWindowWashing()
    {
        var wnd = EditorWindow.GetWindow<AnimationEditorWindow>();
        if (wnd.Timeline == null)
        {
            return false;
        }

        return true;
    }

    [MenuItem("LEAP/Animation/E: WindowWashing", false, 31)]
    private static void TestWindowWashing()
    {
        AnimationManager.LoadExampleScene("WindowWashing");
    }

    [MenuItem("LEAP/Animation/E: PassSoda", true, 32)]
    private static bool ValidateTestPassSoda()
    {
        var wnd = EditorWindow.GetWindow<AnimationEditorWindow>();
        if (wnd.Timeline == null)
        {
            return false;
        }

        return true;
    }

    [MenuItem("LEAP/Animation/E: PassSoda", false, 32)]
    private static void TestPassSoda()
    {
        AnimationManager.LoadExampleScene("PassSoda");
    }

    [MenuItem("LEAP/Animation/E: Walking90deg", true, 33)]
    private static bool ValidateTestWalking90deg()
    {
        var wnd = EditorWindow.GetWindow<AnimationEditorWindow>();
        if (wnd.Timeline == null)
        {
            return false;
        }

        return true;
    }

    [MenuItem("LEAP/Animation/E: Walking90deg", false, 33)]
    private static void TestWalking90deg()
    {
        AnimationManager.LoadExampleScene("Walking90deg");
    }

    [MenuItem("LEAP/Animation/E: HandShake", true, 34)]
    private static bool ValidateTestHandShake()
    {
        var wnd = EditorWindow.GetWindow<AnimationEditorWindow>();
        if (wnd.Timeline == null)
        {
            return false;
        }

        return true;
    }

    [MenuItem("LEAP/Animation/E: HandShake", false, 34)]
    private static void TesHandShake()
    {
        AnimationManager.LoadExampleScene("HandShake");
    }

    [MenuItem("LEAP/Animation/E: BookShelf", true, 35)]
    private static bool ValidateTestBookShelf()
    {
        var wnd = EditorWindow.GetWindow<AnimationEditorWindow>();
        if (wnd.Timeline == null)
        {
            return false;
        }

        return true;
    }

    [MenuItem("LEAP/Animation/E: BookShelf", false, 35)]
    private static void TestBookShelf()
    {
        AnimationManager.LoadExampleScene("BookShelf");
    }

    [MenuItem("LEAP/Animation/E: StealDiamond", true, 36)]
    private static bool ValidateTestStealDiamond()
    {
        var wnd = EditorWindow.GetWindow<AnimationEditorWindow>();
        if (wnd.Timeline == null)
        {
            return false;
        }

        return true;
    }

    [MenuItem("LEAP/Animation/E: StealDiamond", false, 36)]
    private static void TestStealDiamond()
    {
        AnimationManager.LoadExampleScene("StealDiamond");
    }

    [MenuItem("LEAP/Animation/E: WaitForBus", true, 37)]
    private static bool ValidateTestWaitForBus()
    {
        var wnd = EditorWindow.GetWindow<AnimationEditorWindow>();
        if (wnd.Timeline == null)
        {
            return false;
        }

        return true;
    }

    [MenuItem("LEAP/Animation/E: WaitForBus", false, 37)]
    private static void TestWaitForBus()
    {
        AnimationManager.LoadExampleScene("WaitForBus");
    }

    [MenuItem("LEAP/Animation/Reset Models to Initial Pose", true, 5)]
    private static bool ValidateResetModelsToInitialPose()
    {
        var wnd = EditorWindow.GetWindow<AnimationEditorWindow>();
        return wnd.Timeline != null;
    }

    [MenuItem("LEAP/Animation/Reset Models to Initial Pose", false, 5)]
    private static void ResetModelsToInitialPose()
    {
        var wnd = EditorWindow.GetWindow<AnimationEditorWindow>();
        var timeline = wnd.Timeline;
        timeline.ResetModelsAndEnvironment();
        EyeGazeEditor.ResetEyeGazeControllers(timeline.OwningManager.Models.ToArray());
        SceneView.RepaintAll();
    }

    [MenuItem("LEAP/Animation/Bake Animation", true, 6)]
    private static bool ValidateBakeAnimation()
    {
        var wnd = EditorWindow.GetWindow<AnimationEditorWindow>();
        return wnd.Timeline != null;
    }

    [MenuItem("LEAP/Animation/Bake Animation", false, 6)]
    private static void BakeAnimation()
    {
        var wnd = EditorWindow.GetWindow<AnimationEditorWindow>();
        var timeline = wnd.Timeline;

        timeline.InitBake(LEAPCore.defaultBakedTimelineName);
        timeline.BakeRange(LEAPCore.timelineBakeRangeStart,
            LEAPCore.timelineBakeRangeEnd > LEAPCore.timelineBakeRangeStart ?
            LEAPCore.timelineBakeRangeEnd - LEAPCore.timelineBakeRangeStart + 1 :
            timeline.FrameLength);
    }

    [MenuItem("LEAP/Animation/Infer Eye Gaze/Instances", true, 7)]
    private static bool ValidateInferEyeGazeInstances()
    {
        var wnd = EditorWindow.GetWindow<AnimationEditorWindow>();
        return wnd.Timeline != null && wnd.Timeline.GetLayer("Gaze") != null;
    }

    [MenuItem("LEAP/Animation/Infer Eye Gaze/Instances", false, 7)]
    private static void InferEyeGazeInstances()
    {
        var wnd = EditorWindow.GetWindow<AnimationEditorWindow>();
        var timeline = wnd.Timeline;

        // Disable IK and gaze layers
        timeline.SetIKEnabled(false);
        timeline.GetLayer("Gaze").Active = false;

        // Infer gaze shifts and fixations in the base animation
        var baseLayer = timeline.GetLayer(LEAPCore.baseAnimationLayerName);
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
        var wnd = EditorWindow.GetWindow<AnimationEditorWindow>();
        return wnd.Timeline != null && wnd.Timeline.GetLayer("Gaze") != null;
    }

    [MenuItem("LEAP/Animation/Infer Eye Gaze/Alignments", false, 7)]
    private static void InferEyeGazeAlignments()
    {
        var wnd = EditorWindow.GetWindow<AnimationEditorWindow>();
        var timeline = wnd.Timeline;

        // Disable IK and gaze layers
        timeline.SetIKEnabled(false);
        timeline.GetLayer("Gaze").Active = false;

        // Infer head and torso alignments for all gaze shifts in the base animation
        var baseLayer = timeline.GetLayer(LEAPCore.baseAnimationLayerName);
        foreach (var baseAnimation in baseLayer.Animations)
        {
            EyeGazeEditor.InferEyeGazeAheadTargets(timeline, baseAnimation.InstanceId);
            EyeGazeEditor.InferEyeGazeAlignments(timeline, baseAnimation.InstanceId);

            // Save and print inferred eye gaze
            EyeGazeEditor.SaveEyeGaze(timeline, baseAnimation.InstanceId, "#Inferred");
            EyeGazeEditor.PrintEyeGaze(timeline);
        }
        SceneView.RepaintAll();
    }

    [MenuItem("LEAP/Animation/Load Eye Gaze/Hand-annotated", true, 10)]
    private static bool ValidateLoadEyeGaze()
    {
        var wnd = EditorWindow.GetWindow<AnimationEditorWindow>();
        return wnd.Timeline != null;
    }

    [MenuItem("LEAP/Animation/Load Eye Gaze/Hand-annotated", false, 10)]
    private static void LoadEyeGaze()
    {
        var wnd = EditorWindow.GetWindow<AnimationEditorWindow>();
        var timeline = wnd.Timeline;
        var models = timeline.OwningManager.Models;

        for (int modelIndex = 0; modelIndex < models.Count; ++modelIndex)
        {
            var model = models[modelIndex];
            var baseAnimation = timeline.GetLayer(LEAPCore.baseAnimationLayerName).Animations.FirstOrDefault(a => a.Animation.Model == model);
            EyeGazeEditor.LoadEyeGaze(timeline, baseAnimation.InstanceId, "Gaze");
        }

        EyeGazeEditor.PrintEyeGaze(timeline);
    }

    [MenuItem("LEAP/Animation/Load Eye Gaze/Inferred", true, 11)]
    private static bool ValidateLoadEyeGazeInferred()
    {
        var wnd = EditorWindow.GetWindow<AnimationEditorWindow>();
        return wnd.Timeline != null;
    }

    [MenuItem("LEAP/Animation/Load Eye Gaze/Inferred", false, 11)]
    private static void LoadEyeGazeInferred()
    {
        var wnd = EditorWindow.GetWindow<AnimationEditorWindow>();
        var timeline = wnd.Timeline;
        var models = timeline.OwningManager.Models;

        for (int modelIndex = 0; modelIndex < models.Count; ++modelIndex)
        {
            var model = models[modelIndex];
            var baseAnimation = timeline.GetLayer(LEAPCore.baseAnimationLayerName).Animations.FirstOrDefault(a => a.Animation.Model == model);
            EyeGazeEditor.LoadEyeGaze(timeline, baseAnimation.InstanceId, "Gaze", "#Inferred");
        }

        EyeGazeEditor.PrintEyeGaze(timeline);
    }

    [MenuItem("LEAP/Animation/Load Eye Gaze/Edits", true, 12)]
    private static bool ValidateLoadEyeGazeEdits()
    {
        var wnd = EditorWindow.GetWindow<AnimationEditorWindow>();
        return wnd.Timeline != null;
    }

    [MenuItem("LEAP/Animation/Load Eye Gaze/Edits", false, 12)]
    private static void LoadEyeGazeEdits()
    {
        var wnd = EditorWindow.GetWindow<AnimationEditorWindow>();
        var timeline = wnd.Timeline;
        var models = timeline.OwningManager.Models;

        for (int modelIndex = 0; modelIndex < models.Count; ++modelIndex)
        {
            var model = models[modelIndex];
            var baseAnimation = timeline.GetLayer(LEAPCore.baseAnimationLayerName).Animations.FirstOrDefault(a => a.Animation.Model == model);
            EyeGazeEditor.LoadEyeGaze(timeline, baseAnimation.InstanceId, "Gaze", "#Edits");
        }

        EyeGazeEditor.PrintEyeGaze(timeline);
    }

    [MenuItem("LEAP/Animation/Save Eye Gaze", true, 13)]
    private static bool ValidateSaveEyeGaze()
    {
        var wnd = EditorWindow.GetWindow<AnimationEditorWindow>();
        return wnd.Timeline != null && wnd.Timeline.GetLayer("Gaze") != null;
    }

    [MenuItem("LEAP/Animation/Save Eye Gaze", false, 13)]
    private static void SaveEyeGaze()
    {
        var wnd = EditorWindow.GetWindow<AnimationEditorWindow>();
        var timeline = wnd.Timeline;
        var models = timeline.OwningManager.Models;

        for (int modelIndex = 0; modelIndex < models.Count; ++modelIndex)
        {
            var model = models[modelIndex];
            var baseAnimation = timeline.GetLayer(LEAPCore.baseAnimationLayerName).Animations.FirstOrDefault(a => a.Animation.Model == model);
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

        // Create default anim. controllers
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

        // Initialize controller states
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

        // Create default anim. controllers
        GazeController gazectr = obj.AddComponent<GazeController>();
        BlinkController blinkctr = obj.AddComponent<BlinkController>();

        // Initialize controller states
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
        string assetPath = "";
        UnityEngine.Object prefab = EditorUtility.GetPrefabParent(obj);
        if (prefab != null)
        {
            assetPath = AssetDatabase.GetAssetPath(prefab);
        }
        else
        {
            return "";
        }

        // Determine LMC file path
        string lmcPath = "";
        int ext_i = assetPath.LastIndexOf(".", StringComparison.InvariantCultureIgnoreCase);
        if (ext_i >= 0)
        {
            lmcPath = assetPath.Substring(0, ext_i) + ".lmc";
        }
        else
        {
            lmcPath += ".lmc";
        }

        return lmcPath;
    }


    /// <summary>
    /// Validate run a custom script.
    /// </summary>
    /// <returns>
    /// Always true
    /// </returns>
    [MenuItem("LEAP/Custom Scripts/Run Script 1", true)]
    private static bool ValidateRunScript1()
    {
        return true;
    }

    /// <summary>
    /// Run a custom script.
    /// </summary>
    [MenuItem("LEAP/Custom Scripts/Run Script 1")]
    private static void RunScript1()
    {
        var model = Selection.activeGameObject;
        var animationComponent = model.GetComponent<Animation>();
        var clip = animationComponent.GetClip("WindowWashingA-Eyes");

        string blendShapePath = ModelUtils.GetBonePath(ModelUtils.FindBone(model.transform, "srfBind_Cn_Head"));
        LEAPAssetUtils.CopyAnimationCurveFromToProperty(clip, typeof(SkinnedMeshRenderer), blendShapePath,
            "blendShape.BottomLidUp_2.L", blendShapePath, "blendShape.BottomLidUp_2.R");
        LEAPAssetUtils.CopyAnimationCurveFromToProperty(clip, typeof(SkinnedMeshRenderer), blendShapePath,
            "blendShape.TopLidDown_2.L", blendShapePath, "blendShape.TopLidDown_2.R");

        return;
    }

    /// <summary>
    /// Validate run a custom script.
    /// </summary>
    /// <returns>
    /// Always true
    /// </returns>
    [MenuItem("LEAP/Custom Scripts/Run Script 2", true)]
    private static bool ValidateRunScript2()
    {
        return true;
    }

    /// <summary>
    /// Run a custom script.
    /// </summary>
    [MenuItem("LEAP/Custom Scripts/Run Script 2")]
    private static void RunScript2()
    {
        // Create output files for velocities
        System.IO.StreamWriter swh = new System.IO.StreamWriter("../Matlab/GazeController/HeadVelocities.csv", false);
        System.IO.StreamWriter swt = new System.IO.StreamWriter("../Matlab/GazeController/TorsoVelocities.csv", false);

        // Get logged gaze controller states and print out velocities
        var timeline = AnimationManager.Instance.Timeline;
        var controllerContainer = timeline.BakedTimelineContainers[0].AnimationContainers[0].ControllerContainers
            .FirstOrDefault(c => c.Controller is GazeController);
        for (int frameIndex = 0; frameIndex < controllerContainer.ControllerStates.Count; ++frameIndex)
        {
            GazeControllerState gazeControllerState = (GazeControllerState)controllerContainer.ControllerStates[frameIndex];
            float curHeadVelocity = gazeControllerState.stateId == (int)GazeState.Shifting &&
                gazeControllerState.headState.latency <= 0f ?
                gazeControllerState.headState.curVelocity : 0f; ;
            float curTorsoVelocity = gazeControllerState.stateId == (int)GazeState.Shifting &&
                gazeControllerState.torsoState.latency <= 0f ?
                gazeControllerState.torsoState.curVelocity : 0f; ;
            swh.WriteLine(string.Format("{0},{1}", frameIndex, curHeadVelocity));
            swt.WriteLine(string.Format("{0},{1}", frameIndex, curTorsoVelocity));
        }
        swh.Close();
        swt.Close();

        return;
    }
}

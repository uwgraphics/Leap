using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Linq;

public class LEAPMenu
{
    [MenuItem("LEAP/Animation/Init Editor", true)]
    private static bool ValidateInitAnimationEditor()
    {
        return true;
    }

    [MenuItem("LEAP/Animation/Init Editor", false)]
    private static void InitAnimationEditor()
    {
        var wnd = EditorWindow.GetWindow<AnimationEditorWindow>();
        wnd.Init();
        SceneView.RepaintAll();
    }

    [MenuItem("LEAP/Animation/Reset Models to Initial Pose", true)]
    private static bool ValidateResetModelsToInitialPose()
    {
        var wnd = EditorWindow.GetWindow<AnimationEditorWindow>();
        return wnd.Timeline != null;
    }

    [MenuItem("LEAP/Animation/Reset Models to Initial Pose", false)]
    private static void ResetModelsToInitialPose()
    {
        var wnd = EditorWindow.GetWindow<AnimationEditorWindow>();
        var timeline = wnd.Timeline;
        timeline.ResetModelsAndEnvironment();
        EyeGazeEditor.ResetEyeGazeControllers(timeline.OwningManager.Models.ToArray());
        SceneView.RepaintAll();
    }

    [MenuItem("LEAP/Animation/Bake Animation", true)]
    private static bool ValidateBakeAnimation()
    {
        var wnd = EditorWindow.GetWindow<AnimationEditorWindow>();
        return wnd.Timeline != null;
    }

    [MenuItem("LEAP/Animation/Bake Animation", false)]
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

    [MenuItem("LEAP/Animation/Infer Eye Gaze/From Body Animation", true)]
    private static bool ValidateInferEyeGazeInstances()
    {
        var wnd = EditorWindow.GetWindow<AnimationEditorWindow>();
        var selectedModel = ModelUtil.GetSelectedModel();
        return wnd.Timeline != null && wnd.Timeline.GetLayer(LEAPCore.eyeGazeAnimationLayerName) != null &&
            selectedModel != null && selectedModel.tag == "Agent";
    }

    [MenuItem("LEAP/Animation/Infer Eye Gaze/From Body Animation", false)]
    private static void InferEyeGazeInstances()
    {
        var wnd = EditorWindow.GetWindow<AnimationEditorWindow>();
        var timeline = wnd.Timeline;

        // Disable IK and gaze layers
        timeline.SetIKEnabled(false);
        timeline.GetLayer(LEAPCore.eyeGazeAnimationLayerName).Active = false;

        // Infer gaze shifts and fixations in the base animation
        var selectedModel = ModelUtil.GetSelectedModel();
        var baseLayer = timeline.GetLayer(LEAPCore.baseAnimationLayerName);
        foreach (var baseAnimation in baseLayer.Animations)
        {
            if (selectedModel != baseAnimation.Animation.Model)
                continue;

            var eyeGazeInferenceModel = new EyeGazeInferenceModel(selectedModel, timeline.OwningManager.Environment);
            eyeGazeInferenceModel.InferEyeGazeInstances(timeline, baseAnimation.InstanceId, LEAPCore.eyeGazeAnimationLayerName);

            // Save and print inferred eye gaze
            EyeGazeEditor.SaveEyeGaze(timeline, baseAnimation.InstanceId, "#Inferred");
            EyeGazeEditor.PrintEyeGaze(timeline, LEAPCore.eyeGazeAnimationLayerName);
        }
        SceneView.RepaintAll();
    }

    [MenuItem("LEAP/Animation/Infer Eye Gaze/Generate Ground-truth", true)]
    private static bool ValidateInferEyeGazeGenerateGroundTruth()
    {
        var wnd = EditorWindow.GetWindow<AnimationEditorWindow>();
        return wnd.Timeline != null && wnd.Timeline.GetLayer(LEAPCore.eyeGazeAnimationLayerName) != null;
    }

    [MenuItem("LEAP/Animation/Infer Eye Gaze/Generate Ground-truth", false)]
    private static void InferEyeGazeGenerateGroundTruth()
    {
        var wnd = EditorWindow.GetWindow<AnimationEditorWindow>();
        var timeline = wnd.Timeline;

        // Disable IK and gaze layers
        timeline.SetIKEnabled(false);
        timeline.GetLayer(LEAPCore.eyeGazeAnimationLayerName).Active = false;

        // Infer gaze shifts and fixations in the base animation
        var baseLayer = timeline.GetLayer(LEAPCore.baseAnimationLayerName);
        foreach (var baseAnimation in baseLayer.Animations)
        {
            var eyeTrackData = new EyeTrackData(baseAnimation.Animation.Model,
                (baseAnimation.Animation as AnimationClipInstance).AnimationClip);
            eyeTrackData.GenerateEyeGazeInstances(timeline, LEAPCore.eyeGazeAnimationLayerName);

            // Save and print ground-truth eye gaze
            EyeGazeEditor.SaveEyeGaze(timeline, baseAnimation.InstanceId, "#GroundTruth");
            EyeGazeEditor.PrintEyeGaze(timeline, LEAPCore.eyeGazeAnimationLayerName);
        }
        SceneView.RepaintAll();
    }

    [MenuItem("LEAP/Animation/Infer Eye Gaze/Evaluate Target Locations", true)]
    private static bool ValidateInferEyeGazeEvaluateTargetLocations()
    {
        var wnd = EditorWindow.GetWindow<AnimationEditorWindow>();
        var selectedModel = ModelUtil.GetSelectedModel();
        return wnd.Timeline != null && wnd.Timeline.GetLayer(LEAPCore.eyeGazeAnimationLayerName) != null &&
            selectedModel != null && selectedModel.tag == "Agent" &&
            wnd.Timeline.BakedTimelineContainers.Count > 0;
    }

    [MenuItem("LEAP/Animation/Infer Eye Gaze/Evaluate Target Locations", false)]
    private static void InferEyeGazeEvaluateTargetLocations()
    {
        var wnd = EditorWindow.GetWindow<AnimationEditorWindow>();
        var timeline = wnd.Timeline;

        // Disable IK and gaze layers, show baked gaze animation
        timeline.SetIKEnabled(false);
        timeline.GetLayer(LEAPCore.eyeGazeAnimationLayerName).Active = false;
        timeline.ActiveBakedTimelineIndex = timeline.BakedTimelineContainers.Count - 1;

        // Compute evaluation of gaze target location inference
        var selectedModel = ModelUtil.GetSelectedModel();
        var baseLayer = timeline.GetLayer(LEAPCore.baseAnimationLayerName);
        foreach (var baseAnimation in baseLayer.Animations)
        {
            if (selectedModel != baseAnimation.Animation.Model)
                continue;

            EyeGazeInferenceModel.EvaluateTargetDirections(timeline, baseAnimation.InstanceId);
        }
        SceneView.RepaintAll();
    }

    [MenuItem("LEAP/Animation/Load Eye Gaze/Hand-annotated", true)]
    private static bool ValidateLoadEyeGaze()
    {
        var wnd = EditorWindow.GetWindow<AnimationEditorWindow>();
        return wnd.Timeline != null;
    }

    [MenuItem("LEAP/Animation/Load Eye Gaze/Hand-annotated", false)]
    private static void LoadEyeGaze()
    {
        var wnd = EditorWindow.GetWindow<AnimationEditorWindow>();
        var timeline = wnd.Timeline;
        var models = timeline.OwningManager.Models;

        for (int modelIndex = 0; modelIndex < models.Count; ++modelIndex)
        {
            var model = models[modelIndex];
            var baseAnimation = timeline.GetLayer(LEAPCore.baseAnimationLayerName).Animations.FirstOrDefault(a => a.Animation.Model == model);
            EyeGazeEditor.LoadEyeGaze(timeline, baseAnimation.InstanceId, LEAPCore.eyeGazeAnimationLayerName);
        }

        EyeGazeEditor.PrintEyeGaze(timeline, LEAPCore.eyeGazeAnimationLayerName);
    }

    [MenuItem("LEAP/Animation/Load Eye Gaze/Inferred", true)]
    private static bool ValidateLoadEyeGazeInferred()
    {
        var wnd = EditorWindow.GetWindow<AnimationEditorWindow>();
        return wnd.Timeline != null;
    }

    [MenuItem("LEAP/Animation/Load Eye Gaze/Inferred", false)]
    private static void LoadEyeGazeInferred()
    {
        var wnd = EditorWindow.GetWindow<AnimationEditorWindow>();
        var timeline = wnd.Timeline;
        var models = timeline.OwningManager.Models;

        for (int modelIndex = 0; modelIndex < models.Count; ++modelIndex)
        {
            var model = models[modelIndex];
            var baseAnimation = timeline.GetLayer(LEAPCore.baseAnimationLayerName).Animations.FirstOrDefault(a => a.Animation.Model == model);
            EyeGazeEditor.LoadEyeGaze(timeline, baseAnimation.InstanceId, LEAPCore.eyeGazeAnimationLayerName, "#Inferred");
        }

        EyeGazeEditor.PrintEyeGaze(timeline);
    }

    [MenuItem("LEAP/Animation/Load Eye Gaze/Ground-truth", true)]
    private static bool ValidateLoadEyeGazeGroundTruth()
    {
        var wnd = EditorWindow.GetWindow<AnimationEditorWindow>();
        return wnd.Timeline != null;
    }

    [MenuItem("LEAP/Animation/Load Eye Gaze/Ground-truth", false)]
    private static void LoadEyeGazeGroundTruth()
    {
        var wnd = EditorWindow.GetWindow<AnimationEditorWindow>();
        var timeline = wnd.Timeline;
        var models = timeline.OwningManager.Models;

        for (int modelIndex = 0; modelIndex < models.Count; ++modelIndex)
        {
            var model = models[modelIndex];
            var baseAnimation = timeline.GetLayer(LEAPCore.baseAnimationLayerName).Animations.FirstOrDefault(a => a.Animation.Model == model);
            EyeGazeEditor.LoadEyeGaze(timeline, baseAnimation.InstanceId, LEAPCore.eyeGazeAnimationLayerName, "#GroundTruth");
        }

        EyeGazeEditor.PrintEyeGaze(timeline);
    }

    [MenuItem("LEAP/Animation/Load Eye Gaze/Edits", true)]
    private static bool ValidateLoadEyeGazeEdits()
    {
        var wnd = EditorWindow.GetWindow<AnimationEditorWindow>();
        return wnd.Timeline != null;
    }

    [MenuItem("LEAP/Animation/Load Eye Gaze/Edits", false)]
    private static void LoadEyeGazeEdits()
    {
        var wnd = EditorWindow.GetWindow<AnimationEditorWindow>();
        var timeline = wnd.Timeline;
        var models = timeline.OwningManager.Models;

        for (int modelIndex = 0; modelIndex < models.Count; ++modelIndex)
        {
            var model = models[modelIndex];
            var baseAnimation = timeline.GetLayer(LEAPCore.baseAnimationLayerName).Animations.FirstOrDefault(a => a.Animation.Model == model);
            EyeGazeEditor.LoadEyeGaze(timeline, baseAnimation.InstanceId, LEAPCore.eyeGazeAnimationLayerName, "#Edits");
        }

        EyeGazeEditor.PrintEyeGaze(timeline);
    }

    [MenuItem("LEAP/Animation/Save Eye Gaze", true)]
    private static bool ValidateSaveEyeGaze()
    {
        var wnd = EditorWindow.GetWindow<AnimationEditorWindow>();
        return wnd.Timeline != null && wnd.Timeline.GetLayer(LEAPCore.eyeGazeAnimationLayerName) != null;
    }

    [MenuItem("LEAP/Animation/Save Eye Gaze", false)]
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

    [MenuItem("LEAP/Animation/Fix Animation Clip Assoc.", true)]
    private static bool ValidateFixAnimationClipAssoc()
    {
        GameObject obj = Selection.activeGameObject;
        if (obj == null || obj.GetComponent<Animation>() == null)
        {
            return false;
        }

        return true;
    }

    [MenuItem("LEAP/Animation/Fix Animation Clip Assoc.", false)]
    private static void FixAnimationClipAssoc()
    {
        GameObject obj = Selection.activeGameObject;
        LEAPAssetUtil.FixModelAnimationClipAssoc(obj);
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
    [MenuItem("LEAP/Animation/Load Morph Channels", false)]
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

    [MenuItem("LEAP/Scenes/ExpressiveGaze", false)]
    private static void TestExpressiveGaze()
    {
        EyeGazeEditTestSceneManager.LoadExampleScene("TestExpressiveGaze");
    }

    [MenuItem("LEAP/Scenes/InitialPose", true)]
    private static bool ValidateTestInitialPose()
    {
        var wnd = EditorWindow.GetWindow<AnimationEditorWindow>();
        if (wnd.Timeline == null)
        {
            return false;
        }

        return true;
    }

    [MenuItem("LEAP/Scenes/WindowWashing", true)]
    private static bool ValidateTestWindowWashing()
    {
        var wnd = EditorWindow.GetWindow<AnimationEditorWindow>();
        if (wnd.Timeline == null)
        {
            return false;
        }

        return true;
    }

    [MenuItem("LEAP/Scenes/WindowWashing", false)]
    private static void TestWindowWashing()
    {
        EyeGazeEditTestSceneManager.LoadExampleScene("WindowWashing");
    }

    [MenuItem("LEAP/Scenes/PassSoda", true)]
    private static bool ValidateTestPassSoda()
    {
        var wnd = EditorWindow.GetWindow<AnimationEditorWindow>();
        if (wnd.Timeline == null)
        {
            return false;
        }

        return true;
    }

    [MenuItem("LEAP/Scenes/PassSoda", false)]
    private static void TestPassSoda()
    {
        EyeGazeEditTestSceneManager.LoadExampleScene("PassSoda");
    }

    [MenuItem("LEAP/Scenes/Walking90deg", true)]
    private static bool ValidateTestWalking90deg()
    {
        var wnd = EditorWindow.GetWindow<AnimationEditorWindow>();
        if (wnd.Timeline == null)
        {
            return false;
        }

        return true;
    }

    [MenuItem("LEAP/Scenes/Walking90deg", false)]
    private static void TestWalking90deg()
    {
        EyeGazeEditTestSceneManager.LoadExampleScene("Walking90deg");
    }

    [MenuItem("LEAP/Scenes/HandShake", true)]
    private static bool ValidateTestHandShake()
    {
        var wnd = EditorWindow.GetWindow<AnimationEditorWindow>();
        if (wnd.Timeline == null)
        {
            return false;
        }

        return true;
    }

    [MenuItem("LEAP/Scenes/HandShake", false)]
    private static void TesHandShake()
    {
        EyeGazeEditTestSceneManager.LoadExampleScene("HandShake");
    }

    [MenuItem("LEAP/Scenes/BookShelf", true)]
    private static bool ValidateTestBookShelf()
    {
        var wnd = EditorWindow.GetWindow<AnimationEditorWindow>();
        if (wnd.Timeline == null)
        {
            return false;
        }

        return true;
    }

    [MenuItem("LEAP/Scenes/BookShelf", false)]
    private static void TestBookShelf()
    {
        EyeGazeEditTestSceneManager.LoadExampleScene("BookShelf");
    }

    [MenuItem("LEAP/Scenes/StealDiamond", true)]
    private static bool ValidateTestStealDiamond()
    {
        var wnd = EditorWindow.GetWindow<AnimationEditorWindow>();
        if (wnd.Timeline == null)
        {
            return false;
        }

        return true;
    }

    [MenuItem("LEAP/Scenes/StealDiamond", false)]
    private static void TestStealDiamond()
    {
        EyeGazeEditTestSceneManager.LoadExampleScene("StealDiamond");
    }

    [MenuItem("LEAP/Scenes/WaitForBus", true)]
    private static bool ValidateTestWaitForBus()
    {
        var wnd = EditorWindow.GetWindow<AnimationEditorWindow>();
        if (wnd.Timeline == null)
        {
            return false;
        }

        return true;
    }

    [MenuItem("LEAP/Scenes/WaitForBus", false)]
    private static void TestWaitForBus()
    {
        EyeGazeEditTestSceneManager.LoadExampleScene("WaitForBus");
    }

    [MenuItem("LEAP/Scenes/EyeTrackMocapTest1-1", true)]
    private static bool ValidateTestEyeTrackMocapTest11()
    {
        var wnd = EditorWindow.GetWindow<AnimationEditorWindow>();
        if (wnd.Timeline == null)
        {
            return false;
        }

        return true;
    }

    [MenuItem("LEAP/Scenes/EyeTrackMocapTest1-1", false)]
    private static void TestEyeTrackMocapTest11()
    {
        EyeGazeEditTestSceneManager.LoadExampleScene("EyeTrackMocapTest1-1");
    }

    [MenuItem("LEAP/Scenes/StackBoxes", true)]
    private static bool ValidateTestStackBoxes()
    {
        var wnd = EditorWindow.GetWindow<AnimationEditorWindow>();
        if (wnd.Timeline == null)
        {
            return false;
        }

        return true;
    }

    [MenuItem("LEAP/Scenes/StackBoxes", false)]
    private static void TestStackBoxes()
    {
        EyeGazeEditTestSceneManager.LoadExampleScene("StackBoxes");
    }

    [MenuItem("LEAP/Scenes/WindowWashingNew", true)]
    private static bool ValidateTestWindowWashingNew()
    {
        var wnd = EditorWindow.GetWindow<AnimationEditorWindow>();
        if (wnd.Timeline == null)
        {
            return false;
        }

        return true;
    }

    [MenuItem("LEAP/Scenes/WindowWashingNew", false)]
    private static void TestWindowWashingNew()
    {
        EyeGazeEditTestSceneManager.LoadExampleScene("WindowWashingNew");
    }

    [MenuItem("LEAP/Scenes/WalkConesNew", true)]
    private static bool ValidateTestWalkConesNew()
    {
        var wnd = EditorWindow.GetWindow<AnimationEditorWindow>();
        if (wnd.Timeline == null)
        {
            return false;
        }

        return true;
    }

    [MenuItem("LEAP/Scenes/WalkConesNew", false)]
    private static void TestWalkConesNew()
    {
        EyeGazeEditTestSceneManager.LoadExampleScene("WalkConesNew");
    }

    [MenuItem("LEAP/Scenes/BookShelfNew", true)]
    private static bool ValidateTestBookShelfNew()
    {
        var wnd = EditorWindow.GetWindow<AnimationEditorWindow>();
        if (wnd.Timeline == null)
        {
            return false;
        }

        return true;
    }

    [MenuItem("LEAP/Scenes/BookShelfNew", false)]
    private static void TestBookShelfNew()
    {
        EyeGazeEditTestSceneManager.LoadExampleScene("BookShelfNew");
    }

    [MenuItem("LEAP/Scenes/StealDiamondNew", true)]
    private static bool ValidateTestStealDiamondNew()
    {
        var wnd = EditorWindow.GetWindow<AnimationEditorWindow>();
        if (wnd.Timeline == null)
        {
            return false;
        }

        return true;
    }

    [MenuItem("LEAP/Scenes/StealDiamondNew", false)]
    private static void TestStealDiamondNew()
    {
        EyeGazeEditTestSceneManager.LoadExampleScene("StealDiamondNew");
    }

    [MenuItem("LEAP/Scenes/WaitForBusNew", true)]
    private static bool ValidateTestWaitForBusNew()
    {
        var wnd = EditorWindow.GetWindow<AnimationEditorWindow>();
        if (wnd.Timeline == null)
        {
            return false;
        }

        return true;
    }

    [MenuItem("LEAP/Scenes/WaitForBusNew", false)]
    private static void TestWaitForBusNew()
    {
        EyeGazeEditTestSceneManager.LoadExampleScene("WaitForBusNew");
    }

    [MenuItem("LEAP/Scenes/ChatWithFriend", true)]
    private static bool ValidateTestChatWithFriend()
    {
        var wnd = EditorWindow.GetWindow<AnimationEditorWindow>();
        if (wnd.Timeline == null)
        {
            return false;
        }

        return true;
    }

    [MenuItem("LEAP/Scenes/ChatWithFriend", false)]
    private static void TestChatWithFriend()
    {
        EyeGazeEditTestSceneManager.LoadExampleScene("ChatWithFriend");
    }

    [MenuItem("LEAP/Capture Video/[None]", true)]
    private static bool ValidateCaptureVideo()
    {
        var wnd = EditorWindow.GetWindow<AnimationEditorWindow>();
        if (wnd.Timeline == null || wnd.Timeline.ActiveBakedTimeline == null)
        {
            return false;
        }

        return true;
    }

    [MenuItem("LEAP/Capture Video/[None]", false)]
    private static void CaptureVideo()
    {
        EyeGazeEditTestSceneManager.CaptureVideo();
    }

    [MenuItem("LEAP/Capture Video/Original", true)]
    private static bool ValidateCaptureVideoOriginal()
    {
        var wnd = EditorWindow.GetWindow<AnimationEditorWindow>();
        if (wnd.Timeline == null || wnd.Timeline.ActiveBakedTimeline == null)
        {
            return false;
        }

        return true;
    }

    [MenuItem("LEAP/Capture Video/Original", false)]
    private static void CaptureVideoOriginal()
    {
        EyeGazeEditTestSceneManager.CaptureVideo("Original");
    }

    [MenuItem("LEAP/Capture Video/Inferred", true)]
    private static bool ValidateCaptureVideoInferred()
    {
        var wnd = EditorWindow.GetWindow<AnimationEditorWindow>();
        if (wnd.Timeline == null || wnd.Timeline.ActiveBakedTimeline == null)
        {
            return false;
        }

        return true;
    }

    [MenuItem("LEAP/Capture Video/Inferred", false)]
    private static void CaptureVideoInferred()
    {
        EyeGazeEditTestSceneManager.CaptureVideo("Inferred");
    }

    [MenuItem("LEAP/Capture Video/Ground-truth", true)]
    private static bool ValidateCaptureVideoGroundTruth()
    {
        var wnd = EditorWindow.GetWindow<AnimationEditorWindow>();
        if (wnd.Timeline == null || wnd.Timeline.ActiveBakedTimeline == null)
        {
            return false;
        }

        return true;
    }

    [MenuItem("LEAP/Capture Video/Ground-truth", false)]
    private static void CaptureVideoGroundTruth()
    {
        EyeGazeEditTestSceneManager.CaptureVideo("GroundTruth");
    }

    [MenuItem("LEAP/Capture Video/Edits", true)]
    private static bool ValidateCaptureVideoEdits()
    {
        var wnd = EditorWindow.GetWindow<AnimationEditorWindow>();
        if (wnd.Timeline == null || wnd.Timeline.ActiveBakedTimeline == null)
        {
            return false;
        }

        return true;
    }

    [MenuItem("LEAP/Capture Video/Edits", false)]
    private static void CaptureVideoEdits()
    {
        EyeGazeEditTestSceneManager.CaptureVideo("Edits");
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
    [MenuItem("LEAP/Animation/Save Morph Channels", false)]
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
    [MenuItem("LEAP/Models/Show Bone Gizmos", true)]
    private static bool ValidateShowBoneGizmos()
    {
        GameObject obj = Selection.activeGameObject;
        if (obj == null)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Show bone visualization gizmos for the selected character model.
    /// </summary>
    [MenuItem("LEAP/Models/Show Bone Gizmos", false)]
    private static void ShowBoneGizmos()
    {
        GameObject obj = Selection.activeGameObject;
        ModelUtil.ShowBoneGizmos(obj);
    }

    /// <summary>
    /// Validates the specified menu item.
    /// </summary>
    /// <returns>
    /// true if an agent is selected, false otherwise. <see cref="System.Boolean"/>
    /// </returns>
    [MenuItem("LEAP/Models/Delete Gaze Targets", true)]
    private static bool ValidateDeleteGazeTargets()
    {
        GameObject obj = Selection.activeGameObject;
        if (obj == null)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Delete gaze targets attached to the specified model.
    /// </summary>
    [MenuItem("LEAP/Models/Delete Gaze Targets", false)]
    private static void DeleteGazeTargets()
    {
        GameObject obj = Selection.activeGameObject;
        ModelUtil.DeleteBonesWithTag(obj.transform, "GazeTarget");
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
    [MenuItem("LEAP/Agent Setup/Set Up Default Agent", false)]
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
    [MenuItem("LEAP/Agent Setup/Set Up Gaze Agent", false)]
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
    [MenuItem("LEAP/Agent Setup/Refresh Agent", false)]
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

            LEAPAssetUtil.RefreshAgentModel(new_baseobj, obj);
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
    [MenuItem("LEAP/Agent Setup/Init. Controller States", false)]
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
    }
}

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
            selectedModel != null && selectedModel.tag == "Agent";
    }

    [MenuItem("LEAP/Animation/Infer Eye Gaze/Evaluate Target Locations", false)]
    private static void InferEyeGazeEvaluateTargetLocations()
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

            // TODO: move all this to a separate class
            //
            var model = baseAnimation.Animation.Model;
            var gazeController = model.GetComponent<GazeController>();
            var lEye = gazeController.lEye.Top;
            var rEye = gazeController.rEye.Top;
            var head = gazeController.head.Top;
            int frameLength = baseAnimation.Animation.FrameLength;
            var envLayer = timeline.GetLayer("Environment");

            Debug.Log(string.Format("Evaluating target location inference accuracy for {0}...", model.name));

            // Load eye tracking data
            var eyeTrackData = new EyeTrackData(model, (baseAnimation.Animation as AnimationClipInstance).AnimationClip);

            Debug.Log("Getting marker positions...");

            // Get world-space marker positions
            Vector3[][] worldPos = new Vector3[frameLength][];
            for (int frameIndex = 0; frameIndex < timeline.FrameLength; ++frameIndex)
            {
                // Apply animation at current frame
                timeline.GoToFrame(frameIndex);
                timeline.ApplyAnimation();

                // Get marker objects
                var markerSets = GameObject.FindGameObjectsWithTag("GazeMarkerSet");
                var markersUL = GameObject.FindGameObjectsWithTag("GazeMarkerUL");
                var markersUR = GameObject.FindGameObjectsWithTag("GazeMarkerUR");
                var markersLR = GameObject.FindGameObjectsWithTag("GazeMarkerLR");
                var markersLL = GameObject.FindGameObjectsWithTag("GazeMarkerLL");

                // Find currently most visible marker set
                GameObject curMarkerSet = null;
                float curMarkerSetDist = float.MaxValue;
                Vector3 curHeadDir = gazeController.head.Direction;
                foreach (var markerSet in markerSets)
                {
                    var markerUL = markersUL.FirstOrDefault(m => m.transform.parent == markerSet.transform);
                    var markerDir = (markerUL.transform.position - head.position).normalized;
                    float markerSetDist = Vector3.Angle(markerDir, curHeadDir);

                    if (markerSetDist < curMarkerSetDist)
                    {
                        curMarkerSetDist = markerSetDist;
                        curMarkerSet = markerSet;
                    }
                }

                Debug.Log(string.Format("Frame {0}: using marker set {1}", frameIndex, curMarkerSet.name));

                // Get marker positions
                var curMarkerUL = markersUL.FirstOrDefault(m => m.transform.parent == curMarkerSet.transform);
                var curMarkerUR = markersUR.FirstOrDefault(m => m.transform.parent == curMarkerSet.transform);
                var curMarkerLR = markersLR.FirstOrDefault(m => m.transform.parent == curMarkerSet.transform);
                var curMarkerLL = markersLL.FirstOrDefault(m => m.transform.parent == curMarkerSet.transform);
                worldPos[frameIndex] = new Vector3[4];
                worldPos[frameIndex][0] = curMarkerUL.transform.position;
                worldPos[frameIndex][1] = curMarkerUR.transform.position;
                worldPos[frameIndex][2] = curMarkerLR.transform.position;
                worldPos[frameIndex][3] = curMarkerLL.transform.position;
            }

            // Create camera model for eye tracker video
            var eyeTrackCamModel = new VideoCameraModel(eyeTrackData.ImageWidth, eyeTrackData.ImageHeight);
            Matrix3x3 eyeTrackMatCamera;
            float[] eyeTrackDistCoeffs = new float[5];
            EyeTrackData.DefaultCameraModel.GetIntrinsics(out eyeTrackMatCamera, out eyeTrackDistCoeffs);
            eyeTrackCamModel.SetDefaultIntrinsics(eyeTrackMatCamera, eyeTrackDistCoeffs);

            // Estimate camera model for eye tracker video
            string imageDir = "../Matlab/EyeTracker/" + baseAnimation.Animation.Name + "#Frames" + "/";
            string outImageDir = "../Matlab/EyeTracker/" + baseAnimation.Animation.Name + "#OutFrames" + "/";
            int startFrame = eyeTrackData.FrameOffset;
            eyeTrackCamModel.Align(worldPos, imageDir, startFrame,
                eyeTrackData.CalibPatternWidth, eyeTrackData.CalibPatternHeight, true, true, outImageDir);

            /*// Get marker objects
            var markers = GameObject.FindGameObjectsWithTag("GazeTarget");
            var chairLL = markers.FirstOrDefault(m => m.name == "B_Left");
            var chairUL = markers.FirstOrDefault(m => m.name == "Top_Left");
            var chairUR = markers.FirstOrDefault(m => m.name == "Top_Right");
            var chairLR = markers.FirstOrDefault(m => m.name == "B_Right");
            var dannyLL = markers.FirstOrDefault(m => m.name == "B_Left 1");
            var dannyUL = markers.FirstOrDefault(m => m.name == "Top_Left 1");
            var dannyUR = markers.FirstOrDefault(m => m.name == "Top_Right 1");
            var dannyLR = markers.FirstOrDefault(m => m.name == "B_Right 1");
            var dannyLM = markers.FirstOrDefault(m => m.name == "B_Middle");
            var bobbyLL = markers.FirstOrDefault(m => m.name == "B_Left 1");
            var bobbyUL = markers.FirstOrDefault(m => m.name == "Top_Left 1");
            var bobbyUR = markers.FirstOrDefault(m => m.name == "Top_Right 1");
            var bobbyMR1 = markers.FirstOrDefault(m => m.name == "Top_Right 1");
            var bobbyMR2 = markers.FirstOrDefault(m => m.name == "Top_Right 1");
            var bobbyLR = markers.FirstOrDefault(m => m.name == "B_Right 1");
            
            // Get image-space marker positions at frame 809 (1688)
            Vector2[] imgPos809 = new Vector2[4];
            imgPos809[0] = new Vector2(355, eyeTrackData.ImageHeight - 463 - 1);
            imgPos809[1] = new Vector2(204, eyeTrackData.ImageHeight - 302 - 1);
            imgPos809[2] = new Vector2(411, eyeTrackData.ImageHeight - 125 - 1);
            imgPos809[3] = new Vector2(555, eyeTrackData.ImageHeight - 302 - 1);

            // Get world-space marker positions at frame 809 (1688)
            bodyAnimationNorman.Apply(809, AnimationLayerMode.Override);
            timeline.GetLayer("Environment").Animations[0].Animation.Apply(809, AnimationLayerMode.Override);
            timeline.GetLayer("Environment").Animations[1].Animation.Apply(809, AnimationLayerMode.Override);

            Vector3[] worldPos809 = new Vector3[4];
            worldPos809[0] = head.InverseTransformPoint(chairLL.transform.position);
            worldPos809[1] = head.InverseTransformPoint(chairUL.transform.position);
            worldPos809[2] = head.InverseTransformPoint(chairUR.transform.position);
            worldPos809[3] = head.InverseTransformPoint(chairLR.transform.position);

            // Get image-space marker positions at frame 73 (952)
            Vector2[] imgPos73 = new Vector2[4];
            imgPos73[0] = new Vector2(375, eyeTrackData.ImageHeight - 288 - 1);
            imgPos73[1] = new Vector2(299, eyeTrackData.ImageHeight - 93 - 1);
            imgPos73[2] = new Vector2(535, eyeTrackData.ImageHeight - 11 - 1);
            imgPos73[3] = new Vector2(597, eyeTrackData.ImageHeight - 214 - 1);

            // Get world-space marker positions at frame 73 (952)
            bodyAnimationNorman.Apply(73, AnimationLayerMode.Override);
            timeline.GetLayer("Environment").Animations[0].Animation.Apply(73, AnimationLayerMode.Override);
            timeline.GetLayer("Environment").Animations[1].Animation.Apply(73, AnimationLayerMode.Override);
            Vector3[] worldPos73 = new Vector3[4];
            worldPos73[0] = head.InverseTransformPoint(chairLL.transform.position);
            worldPos73[1] = head.InverseTransformPoint(chairUL.transform.position);
            worldPos73[2] = head.InverseTransformPoint(chairUR.transform.position);
            worldPos73[3] = head.InverseTransformPoint(chairLR.transform.position);

            // Get image-space marker positions at frame 380 (1259)
            Vector2[] imgPos380 = new Vector2[5];
            imgPos380[0] = new Vector2(206, eyeTrackData.ImageHeight - 452 - 1);
            imgPos380[1] = new Vector2(174, eyeTrackData.ImageHeight - 388 - 1);
            imgPos380[2] = new Vector2(253, eyeTrackData.ImageHeight - 358 - 1);
            imgPos380[3] = new Vector2(281, eyeTrackData.ImageHeight - 419 - 1);
            imgPos380[4] = new Vector2(245, eyeTrackData.ImageHeight - 436 - 1);

            // Get world-space marker positions at frame 380 (1259)
            bodyAnimationNorman.Apply(380, AnimationLayerMode.Override);
            timeline.GetLayer("Environment").Animations[0].Animation.Apply(380, AnimationLayerMode.Override);
            timeline.GetLayer("Environment").Animations[1].Animation.Apply(380, AnimationLayerMode.Override);
            Vector3[] worldPos380 = new Vector3[5];
            worldPos380[0] = head.InverseTransformPoint(dannyLL.transform.position);
            worldPos380[1] = head.InverseTransformPoint(dannyUL.transform.position);
            worldPos380[2] = head.InverseTransformPoint(dannyUR.transform.position);
            worldPos380[3] = head.InverseTransformPoint(dannyLR.transform.position);
            worldPos380[4] = head.InverseTransformPoint(dannyLM.transform.position);

            // Calibrate eye tracker camera
            var cameraModel = new CameraModel();
            var matCamera = new Matrix3x3();
            matCamera.m00 = 1.1087157e+003f;
            matCamera.m01 = 0f;
            matCamera.m02 = 6.395e+002f;
            matCamera.m10 = 0f;
            matCamera.m11 = 1.1087157e+003f;
            matCamera.m12 = 4.795e+002f;
            matCamera.m20 = 0f;
            matCamera.m21 = 0f;
            matCamera.m22 = 1f;
            var distCoeffs = new float[5];
            distCoeffs[0] = 8.0114708e-002f;
            distCoeffs[1] = -7.9709385e-001f;
            distCoeffs[2] = 0f;
            distCoeffs[3] = 0f;
            distCoeffs[4] = 1.4157773e+000f;
            cameraModel.SetIntrinsics(matCamera, distCoeffs);
            cameraModel.InitOpenCV(worldPos809, imgPos809);

            // Test calibration
            Vector2[] estImgPos73 = new Vector2[4];
            estImgPos73[0] = cameraModel.GetImagePosition(worldPos73[0]);
            estImgPos73[1] = cameraModel.GetImagePosition(worldPos73[1]);
            estImgPos73[2] = cameraModel.GetImagePosition(worldPos73[2]);
            estImgPos73[3] = cameraModel.GetImagePosition(worldPos73[3]);
            Vector2[] estImgPos380 = new Vector2[5];
            estImgPos380[0] = cameraModel.GetImagePosition(worldPos380[0]);
            estImgPos380[1] = cameraModel.GetImagePosition(worldPos380[1]);
            estImgPos380[2] = cameraModel.GetImagePosition(worldPos380[2]);
            estImgPos380[3] = cameraModel.GetImagePosition(worldPos380[3]);
            estImgPos380[4] = cameraModel.GetImagePosition(worldPos380[4]);

            // Print test results
            Debug.Log("FRAME 73 (952):");
            Debug.Log(string.Format("Ground-truth: ({0}, {1}), estimated: ({2}, {3})",
                imgPos73[0].x, imgPos73[0].y, estImgPos73[0].x, estImgPos73[0].y));
            Debug.Log(string.Format("Ground-truth: ({0}, {1}), estimated: ({2}, {3})",
                imgPos73[1].x, imgPos73[1].y, estImgPos73[1].x, estImgPos73[1].y));
            Debug.Log(string.Format("Ground-truth: ({0}, {1}), estimated: ({2}, {3})",
                imgPos73[2].x, imgPos73[2].y, estImgPos73[2].x, estImgPos73[2].y));
            Debug.Log(string.Format("Ground-truth: ({0}, {1}), estimated: ({2}, {3})",
                imgPos73[3].x, imgPos73[3].y, estImgPos73[3].x, estImgPos73[3].y));
            Debug.Log("FRAME 380 (1259):");
            Debug.Log(string.Format("Ground-truth: ({0}, {1}), estimated: ({2}, {3})",
                imgPos380[0].x, imgPos380[0].y, estImgPos380[0].x, estImgPos380[0].y));
            Debug.Log(string.Format("Ground-truth: ({0}, {1}), estimated: ({2}, {3})",
                imgPos380[1].x, imgPos380[1].y, estImgPos380[1].x, estImgPos380[1].y));
            Debug.Log(string.Format("Ground-truth: ({0}, {1}), estimated: ({2}, {3})",
                imgPos380[2].x, imgPos380[2].y, estImgPos380[2].x, estImgPos380[2].y));
            Debug.Log(string.Format("Ground-truth: ({0}, {1}), estimated: ({2}, {3})",
                imgPos380[3].x, imgPos380[3].y, estImgPos380[3].x, estImgPos380[3].y));
            Debug.Log(string.Format("Ground-truth: ({0}, {1}), estimated: ({2}, {3})",
                imgPos380[4].x, imgPos380[4].y, estImgPos380[4].x, estImgPos380[4].y));*/
            
            /*var tableSpots = GameObject.FindGameObjectsWithTag("GazeTarget");
            var leftHandSpot = tableSpots.FirstOrDefault(obj => obj.name == "LeftHandSpot");
            var rightandSpot = tableSpots.FirstOrDefault(obj => obj.name == "RightHandSpot");
            var midTarget = tableSpots.FirstOrDefault(obj => obj.name == "MidTarget");
            var rightTarget = tableSpots.FirstOrDefault(obj => obj.name == "RightTarget");
            var leftTarget = tableSpots.FirstOrDefault(obj => obj.name == "LeftTarget");

            // Load eye tracking data
            var eyeTrackData = new EyeTrackData(testScenes.modelNormanNew, bodyAnimationNorman.AnimationClip);

            // Get ground-truth eye directions at frame 277 (104)
            var vle = eyeTrackData.Samples[277].lEyeDirection;
            var vre = eyeTrackData.Samples[277].rEyeDirection;

            // Get eye tracker bone space eye directions at frame 277 (104)
            bodyAnimationNorman.Apply(104, AnimationLayerMode.Override);
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
            bodyAnimationNorman.Apply(129, AnimationLayerMode.Override);
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
            bodyAnimationNorman.Apply(159, AnimationLayerMode.Override);
            ule = lEye.InverseTransformDirection((midTarget.transform.position - lEye.position).normalized);
            ure = rEye.InverseTransformDirection((midTarget.transform.position - rEye.position).normalized);

            // Compute aligning rotations
            qle = Quaternion.FromToRotation(ule, vle);
            qre = Quaternion.FromToRotation(ure, vre);

            Debug.Log(string.Format("qle at 159: ({0}, {1}, {2})", qle.eulerAngles.x, qle.eulerAngles.y, qle.eulerAngles.z));
            Debug.Log(string.Format("qre at 159: ({0}, {1}, {2})", qre.eulerAngles.x, qre.eulerAngles.y, qre.eulerAngles.z));*/
            //
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
        AnimationManager.LoadExampleScene("TestExpressiveGaze");
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
        AnimationManager.LoadExampleScene("WindowWashing");
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
        AnimationManager.LoadExampleScene("PassSoda");
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
        AnimationManager.LoadExampleScene("Walking90deg");
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
        AnimationManager.LoadExampleScene("HandShake");
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
        AnimationManager.LoadExampleScene("BookShelf");
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
        AnimationManager.LoadExampleScene("StealDiamond");
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
        AnimationManager.LoadExampleScene("WaitForBus");
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
        AnimationManager.LoadExampleScene("EyeTrackMocapTest1-1");
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
        AnimationManager.LoadExampleScene("StackBoxes");
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
        AnimationManager.LoadExampleScene("WindowWashingNew");
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
        AnimationManager.LoadExampleScene("WalkConesNew");
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
        AnimationManager.LoadExampleScene("WaitForBusNew");
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

using UnityEngine;
using System;
using System.Collections;
using System.IO;

public class LEAPCore : MonoBehaviour
{
    public const string morphAnimationPrefix = "MC";
	public const string morphTargetPrefix = "MT";
    public const string lWristTag = "LWristBone";
    public const string lElbowTag = "LElbowBone";
    public const string lShoulderTag = "LShoulderBone";
    public const string rWristTag = "RWristBone";
    public const string rElbowTag = "RElbowBone";
    public const string rShoulderTag = "RShoulderBone";
    public const string lAnkleTag = "LAnkleBone";
    public const string lKneeTag = "LKneeBone";
    public const string lHipTag = "LHipBone";
    public const string rAnkleTag = "RAnkleBone";
    public const string rKneeTag = "RKneeBone";
    public const string rHipTag = "RHipBone";
    public const string lWristTargetHelper = "LWristTargetHelper";
    public const string rWristTargetHelper = "RWristTargetHelper";
    public const string lAnkleTargetHelper = "LAnkleTargetHelper";
    public const string rAnkleTargetHelper = "RAnkleTargetHelper";

	/// <summary>
	/// Whether morph target import should explicitly look for split
	/// vertices, and split them in the morph target.
	/// </summary>
	public static bool morphHandleSplitVertices = true;
	
	/// <summary>
	/// What time step to take in forward Euler (e.g. used in gaze shifts).
	/// </summary>
	public static float eulerTimeStep = 0.015f;

    /// <summary>
    /// Frame rate to use for animation viewing and editing.
    /// </summary>
    public static int editFrameRate = 30;

    /// <summary>
    /// Name of the base animation layer in the animation timeline.
    /// </summary>
    public static string baseAnimationLayerName = "BaseAnimation";

    /// <summary>
    /// Name of the environment animation layer in the animation timeline.
    /// </summary>
    public static string environmentAnimationLayerName = "Environment";

    /// <summary>
    /// Name of the helper animation layer in the animation timeline.
    /// </summary>
    public static string helperAnimationLayerName = "Helpers";

    /// <summary>
    /// Name of the gaze animation layer in the animation timeline.
    /// </summary>
    public static string eyeGazeAnimationLayerName = "Gaze";

    /// <summary>
    /// Name of the camera animation layer in the animation timeline.
    /// </summary>
    public static string cameraAnimationLayerName = "Camera";

    /// <summary>
    /// Default baked animation timeline name.
    /// </summary>
    public static string defaultBakedTimelineName = "Edits";

    /// <summary>
    /// Default time over which end-effector constraints become active or inactive.
    /// </summary>
    public static float endEffectorConstraintActivationTime = 0.7f;

    /// <summary>
    /// How far the IK solver is allowed to extend a limb when solving for its pose.
    /// </summary>
    public static float maxLimbExtension = 0.95f;

    /// <summary>
    /// Asset subdirectory for agent models.
    /// </summary>
    public static string agentModelDirectory = "Assets/Agents";

    /// <summary>
    /// Asset subdirectory for end-effector constraint annotations.
    /// </summary>
    public static string endEffectorConstraintAnnotationsDirectory = "Assets/AnimationAnnotations/EndEffectorConstraints";

    /// <summary>
    /// Asset subdirectory for eye gaze annotations.
    /// </summary>
    public static string eyeGazeAnnotationsDirectory = "Assets/AnimationAnnotations/EyeGaze";

    /// <summary>
    /// Asset subdirectory for timewarp annotations.
    /// </summary>
    public static string timewarpAnnotationsDirectory = "Assets/AnimationAnnotations/Timewarps";

    /// <summary>
    /// Asset subdirectory for keyframe annotations.
    /// </summary>
    public static string keyFrameAnnotationsDirectory = "Assets/AnimationAnnotations/KeyFrames";

    /// <summary>
    /// Asset subdirectory for eye tracking data.
    /// </summary>
    public static string eyeTrackDataDirectory = "Assets/AnimationAnnotations/EyeTrack";

    /// <summary>
    /// Asset subdirectory for environment object models.
    /// </summary>
    public static string environmentModelsDirectory = "Assets/Environment/Models";

    /// <summary>
    /// If true, agents can manipulate objects in the environment.
    /// </summary>
    public static bool enableObjectManipulation = true;

    /// <summary>
    /// Eye gaze editor: minimal length of an eye gaze instance (gaze shift + fixation).
    /// </summary>
    public static float minEyeGazeLength = 0.6f;

    /// <summary>
    /// Eye gaze editor: maximal length of gap between two neighboring eye gaze instances
    /// (gaze shift + fixation), also minimal length of a gaze-ahead instance.
    /// </summary>
    public static float maxEyeGazeGapLength = 2f;

    /// <summary>
    /// Suffix appended to names of the gaze-ahead instance at the start of the animation.
    /// </summary>
    public static string gazeAheadAtStartSuffix = "Start-Ahead";
    
    /// <summary>
    /// Suffix appended to names of gaze-ahead instances.
    /// </summary>
    public static string gazeAheadSuffix = "-Ahead";

    /// <summary>
    /// Time window over which gaze is blended out when gazing ahead.
    /// </summary>
    public static float gazeAheadBlendTime = 2f;

    /// <summary>
    /// If true, posture IK will incorporate gaze constraints.
    /// </summary>
    public static bool useGazeIK = true;

    /// <summary>
    /// If true, gaze constraint weight will be dynamically computed based on importance.
    /// </summary>
    public static bool useDynamicGazeIKWeights = true;

    /// <summary>
    /// Default time over which gaze constraints become active or inactive.
    /// </summary>
    public static float gazeConstraintActivationTime = 1f;

    /// <summary>
    /// If true, effective gaze target in a gaze shift will be adjusted for movement
    /// in the base animation.
    /// </summary>
    public static bool adjustGazeTargetForMovingBase = true;

    /// <summary>
    /// If true, gaze controller state log will contained detailed state of every gaze joint.
    /// </summary>
    public static bool printDetailedGazeControllerState = false;

    /// <summary>
    /// Blend weight with which gaze controller animation is applied to the body.
    /// </summary>
    /// <remarks>Set this to less than 0 to prevent blend weight overriding</remarks>
    public static float gazeBlendWeightOverride = -1f;

    /// <summary>
    /// Maximum width of a cluster of local gaze shift key times corresponding to a single extracted key pose.
    /// </summary>
    public static float gazeInferenceKeyMaxClusterWidth = 0.5f;

    /// <summary>
    /// Kernel size for the low-pass filter used for filtering gaze joint joint velocities in eye gaze inference.
    /// </summary>
    public static int gazeInferenceLowPassKernelSize = 5;

    /// <summary>
    /// Slope of the logistic curve used in computing the velocity term of the probability
    /// that some animation interval is a gaze shift.
    /// </summary>
    public static float gazeInferenceVelocityLogisticSlope = 1f;

    /// <summary>
    /// Growth rate of the exponential curve used in computing the amplitude term of the probability
    /// that some animation interval is a gaze shift.
    /// </summary>
    public static float gazeInferenceAmplitudeExpRate = 6.2f;

    /// <summary>
    /// Influence of head facing amplitude on the probability of a particular animation interval being a gaze shift.
    /// </summary>
    public static float gazeInferenceAmplitudeWeight = 0.5f;

    /// <summary>
    /// If true, simple gaze target inference will be used.
    /// </summary>
    public static bool useSimpleGazeTargetInference = false;

    /// <summary>
    /// Render texture width for spatial probability distributions used in gaze target inference.
    /// </summary>
    public static int gazeInferenceRenderTextureWidth = 1280;

    /// <summary>
    /// If true, render textures used for gaze target inference will be written to PNG files.
    /// </summary>
    public static bool writeGazeInferenceRenderTextures = true;

    /// <summary>
    /// Number of seconds before hand contact when gaze fixations of manipulated objects
    /// begin occurring.
    /// </summary>
    public static float gazeInferenceHandContactStartTime = -3f;

    /// <summary>
    /// Number of seconds before hand contact when probability of gaze fixations of manipulated objects
    /// is the highest.
    /// </summary>
    public static float gazeInferenceHandContactMaxTime = -1f;

    /// <summary>
    /// Number of seconds after hand contact when gaze fixations of manipulated objects
    /// are no longer occurring.
    /// </summary>
    public static float gazeInferenceHandContactEndTime = 0.6f;

    /// <summary>
    /// Weight of the gaze shift direction term for gaze target inference.
    /// </summary>
    public static float gazeInferencePGazeShiftDirWeight = 0.7f;

    /// <summary>
    /// Weight of the task relevance term for gaze target inference.
    /// </summary>
    public static float gazeInferencePTaskRelWeight = 0.2f;

    /// <summary>
    /// Weight of the hand contact term for gaze target inference.
    /// </summary>
    public static float gazeInferencePHandConWeight = 0.1f;

    /// <summary>
    /// Head alignment propensity for gaze target inference.
    /// </summary>
    public static float gazeInferenceHeadAlignPropensity = 0.5f;

    /// <summary>
    /// Maximum distance between two eye gaze targets at which they are
    /// still considered a single target.
    /// </summary>
    public static float gazeInferenceMaxColocatedTargetDistance = 0.17f;

    /// <summary>
    /// Names of objects that won't be considered as task-relevant during gaze target inference.
    /// </summary>
    public static string[] gazeInferenceTaskRelevantObjectFilter = new string[0];

    /// <summary>
    /// Threshold for matching gaze instances in ground-truth and inferred gaze.
    /// </summary>
    public static float gazeInferenceMatchThreshold = 0.8f;

    /// <summary>
    /// Format used for gaze video capture. Supported formats:
    /// mov: QuickTime mov / XviD
    /// mp4: MP4 / h.264
    /// wmv: Windows Media Video / msmpeg4
    /// </summary>
    public static string gazeVideoCaptureFormat = "mov";

    /// <summary>
    /// Start frame for captured gaze video.
    /// </summary>
    public static int gazeVideoCaptureStartFrame = 0;

    /// <summary>
    /// End frame for captured gaze video.
    /// </summary>
    public static int gazeVideoCaptureEndFrame = -1;

    /// <summary>
    /// If false, defined timewarps will not be applied to animations.
    /// </summary>
    public static bool timewarpsEnabled = false;

    /// <summary>
    /// Start frame of the animation timeline segment that will get baked.
    /// </summary>
    public static int timelineBakeRangeStart = 0;

    /// <summary>
    /// End frame of the animation timeline segment that will get baked.
    /// </summary>
    public static int timelineBakeRangeEnd = -1;

    /// <summary>
    /// Scaling factor for bone visualization gizmos.
    /// </summary>
    public static float boneGizmoScale = 2f;

    /// <summary>
    /// Eye tracking sample rate.
    /// </summary>
    public static int eyeTrackSampleRate = 30;

    /// <summary>
    /// Convert time in seconds to time in frames.
    /// </summary>
    /// <param name="time">Time (in seconds)</param>
    /// <returns>Time in frames</returns>
    public static int ToFrame(float time)
    {
        return Mathf.RoundToInt(time * editFrameRate);
    }

    /// <summary>
    /// Convert time in frames to time in seconds.
    /// </summary>
    /// <param name="frame">Time (in frames)</param>
    /// <returns>Time in seconds</returns>
    public static float ToTime(int frame)
    {
        return ((float)frame) / editFrameRate;
    }

    /// <summary>
    /// Convert eye tracking sample time in seconds to time in frames.
    /// </summary>
    /// <param name="time">Time (in seconds)</param>
    /// <returns>Time in frames</returns>
    public static int ToEyeTrackFrame(float time)
    {
        return Mathf.RoundToInt(time * eyeTrackSampleRate);
    }

    /// <summary>
    /// Convert eye tracking sample time in frames to time in seconds.
    /// </summary>
    /// <param name="frame">Time (in frames)</param>
    /// <returns>Time in seconds</returns>
    public static float ToEyeTrackTime(int frame)
    {
        return ((float)frame) / eyeTrackSampleRate;
    }

    /// <summary>
    /// Load Leap configuration from Leap.cfg file
    /// </summary>
    public static void LoadConfiguration()
    {
        var cfgFile = new ConfigFile();

        // Define configuration parameters
        cfgFile.AddParam("editFrameRate", typeof(int));
        cfgFile.AddParam("endEffectorConstraintActivationTime", typeof(float));
        cfgFile.AddParam("enableObjectManipulation", typeof(bool));
        cfgFile.AddParam("minEyeGazeLength", typeof(float));
        cfgFile.AddParam("maxEyeGazeGapLength", typeof(float));
        cfgFile.AddParam("useGazeIK", typeof(bool));
        cfgFile.AddParam("useDynamicGazeIKWeights", typeof(bool));
        cfgFile.AddParam("gazeConstraintActivationTime", typeof(float));
        cfgFile.AddParam("adjustGazeTargetForMovingBase", typeof(bool));
        cfgFile.AddParam("printDetailedGazeControllerState", typeof(bool));
        cfgFile.AddParam("gazeBlendWeightOverride", typeof(float));
        cfgFile.AddParam("gazeInferenceKeyMaxClusterWidth", typeof(float));
        cfgFile.AddParam("gazeInferenceLowPassKernelSize", typeof(int));
        cfgFile.AddParam("gazeInferenceLogisticSlope", typeof(float));
        cfgFile.AddParam("gazeInferenceAmplitudeExpRate", typeof(float));
        cfgFile.AddParam("gazeInferenceAmplitudeWeight", typeof(float));
        cfgFile.AddParam("useSimpleGazeTargetInference", typeof(bool));
        cfgFile.AddParam("gazeInferenceRenderTextureWidth", typeof(int));
        cfgFile.AddParam("writeGazeInferenceRenderTextures", typeof(bool));
        cfgFile.AddParam("gazeInferenceHandContactStartTime", typeof(float));
        cfgFile.AddParam("gazeInferenceHandContactMaxTime", typeof(float));
        cfgFile.AddParam("gazeInferenceHandContactEndTime", typeof(float));
        cfgFile.AddParam("gazeInferencePGazeShiftDirWeight", typeof(float));
        cfgFile.AddParam("gazeInferencePTaskRelWeight", typeof(float));
        cfgFile.AddParam("gazeInferencePHandConWeight", typeof(float));
        cfgFile.AddParam("gazeInferenceHeadAlignPropensity", typeof(float));
        cfgFile.AddParam("gazeInferenceMaxColocatedTargetDistance", typeof(float));
        cfgFile.AddParam("gazeInferenceTaskRelevantObjectFilter", typeof(string[]));
        cfgFile.AddParam("gazeInferenceMatchThreshold", typeof(float));
        cfgFile.AddParam("gazeVideoCaptureFormat", typeof(string));
        cfgFile.AddParam("gazeVideoCaptureStartFrame", typeof(int));
        cfgFile.AddParam("gazeVideoCaptureEndFrame", typeof(int));
        cfgFile.AddParam("timewarpsEnabled", typeof(bool));
        cfgFile.AddParam("timelineBakeRangeStart", typeof(int));
        cfgFile.AddParam("timelineBakeRangeEnd", typeof(int));

        // Read parameter values
        cfgFile.ReadFromFile(Application.dataPath + "/../Leap/Leap.cfg");
        editFrameRate = cfgFile.HasValue("editFrameRate") ?
            cfgFile.GetValue<int>("editFrameRate") : editFrameRate;
        endEffectorConstraintActivationTime = cfgFile.HasValue("endEffectorConstraintActivationTime") ?
            cfgFile.GetValue<float>("endEffectorConstraintActivationTime") : endEffectorConstraintActivationTime;
        enableObjectManipulation = cfgFile.HasValue("enableObjectManipulation") ?
            cfgFile.GetValue<bool>("enableObjectManipulation") : enableObjectManipulation;
        minEyeGazeLength = cfgFile.HasValue("minEyeGazeLength") ?
            cfgFile.GetValue<float>("minEyeGazeLength") : minEyeGazeLength;
        maxEyeGazeGapLength = cfgFile.HasValue("maxEyeGazeGapLength") ?
            cfgFile.GetValue<float>("maxEyeGazeGapLength") : maxEyeGazeGapLength;
        useGazeIK = cfgFile.HasValue("useGazeIK") ?
            cfgFile.GetValue<bool>("useGazeIK") : useGazeIK;
        useDynamicGazeIKWeights = cfgFile.HasValue("useDynamicGazeIKWeights") ?
            cfgFile.GetValue<bool>("useDynamicGazeIKWeights") : useDynamicGazeIKWeights;
        gazeConstraintActivationTime = cfgFile.HasValue("gazeConstraintActivationTime") ?
            cfgFile.GetValue<float>("gazeConstraintActivationTime") : gazeConstraintActivationTime;
        adjustGazeTargetForMovingBase = cfgFile.HasValue("adjustGazeTargetForMovingBase") ?
            cfgFile.GetValue<bool>("adjustGazeTargetForMovingBase") : adjustGazeTargetForMovingBase;
        printDetailedGazeControllerState = cfgFile.HasValue("printDetailedGazeControllerState") ?
            cfgFile.GetValue<bool>("printDetailedGazeControllerState") : printDetailedGazeControllerState;
        gazeBlendWeightOverride = cfgFile.HasValue("gazeBlendWeightOverride") ?
            cfgFile.GetValue<float>("gazeBlendWeightOverride") : gazeBlendWeightOverride;
        gazeInferenceKeyMaxClusterWidth = cfgFile.HasValue("gazeInferenceKeyMaxClusterWidth") ?
            cfgFile.GetValue<float>("gazeInferenceKeyMaxClusterWidth") : gazeInferenceKeyMaxClusterWidth;
        gazeInferenceLowPassKernelSize = cfgFile.HasValue("gazeInferenceLowPassKernelSize") ?
            cfgFile.GetValue<int>("gazeInferenceLowPassKernelSize") : gazeInferenceLowPassKernelSize;
        gazeInferenceVelocityLogisticSlope = cfgFile.HasValue("gazeInferenceLogisticSlope") ?
            cfgFile.GetValue<float>("gazeInferenceLogisticSlope") : gazeInferenceVelocityLogisticSlope;
        gazeInferenceAmplitudeExpRate = cfgFile.HasValue("gazeInferenceAmplitudeExpRate") ?
            cfgFile.GetValue<float>("gazeInferenceAmplitudeExpRate") : gazeInferenceAmplitudeExpRate;
        gazeInferenceAmplitudeWeight = cfgFile.HasValue("gazeInferenceAmplitudeWeight") ?
            cfgFile.GetValue<float>("gazeInferenceAmplitudeWeight") : gazeInferenceAmplitudeWeight;
        useSimpleGazeTargetInference = cfgFile.HasValue("useSimpleGazeTargetInference") ?
            cfgFile.GetValue<bool>("useSimpleGazeTargetInference") : useSimpleGazeTargetInference;
        gazeInferenceRenderTextureWidth = cfgFile.HasValue("gazeInferenceRenderTextureWidth") ?
            cfgFile.GetValue<int>("gazeInferenceRenderTextureWidth") : gazeInferenceRenderTextureWidth;
        writeGazeInferenceRenderTextures = cfgFile.HasValue("writeGazeInferenceRenderTextures") ?
            cfgFile.GetValue<bool>("writeGazeInferenceRenderTextures") : writeGazeInferenceRenderTextures;
        gazeInferenceHandContactStartTime = cfgFile.HasValue("gazeInferenceHandContactStartTime") ?
            cfgFile.GetValue<float>("gazeInferenceHandContactStartTime") : gazeInferenceHandContactStartTime;
        gazeInferenceHandContactMaxTime = cfgFile.HasValue("gazeInferenceHandContactMaxTime") ?
            cfgFile.GetValue<float>("gazeInferenceHandContactMaxTime") : gazeInferenceHandContactMaxTime;
        gazeInferenceHandContactEndTime = cfgFile.HasValue("gazeInferenceHandContactEndTime") ?
            cfgFile.GetValue<float>("gazeInferenceHandContactEndTime") : gazeInferenceHandContactEndTime;
        gazeInferencePGazeShiftDirWeight = cfgFile.HasValue("gazeInferencePGazeShiftDirWeight") ?
            cfgFile.GetValue<float>("gazeInferencePGazeShiftDirWeight") : gazeInferencePGazeShiftDirWeight;
        gazeInferencePTaskRelWeight = cfgFile.HasValue("gazeInferencePTaskRelWeight") ?
            cfgFile.GetValue<float>("gazeInferencePTaskRelWeight") : gazeInferencePTaskRelWeight;
        gazeInferencePHandConWeight = cfgFile.HasValue("gazeInferencePHandConWeight") ?
            cfgFile.GetValue<float>("gazeInferencePHandConWeight") : gazeInferencePHandConWeight;
        gazeInferenceHeadAlignPropensity = cfgFile.HasValue("gazeInferenceHeadAlignPropensity") ?
            cfgFile.GetValue<float>("gazeInferenceHeadAlignPropensity") : gazeInferenceHeadAlignPropensity;
        gazeInferenceMaxColocatedTargetDistance = cfgFile.HasValue("gazeInferenceMaxColocatedTargetDistance") ?
            cfgFile.GetValue<float>("gazeInferenceMaxColocatedTargetDistance") : gazeInferenceMaxColocatedTargetDistance;
        gazeInferenceTaskRelevantObjectFilter = cfgFile.HasValue("gazeInferenceTaskRelevantObjectFilter") ?
            cfgFile.GetValue<string[]>("gazeInferenceTaskRelevantObjectFilter") : gazeInferenceTaskRelevantObjectFilter;
        gazeInferenceMatchThreshold = Mathf.Clamp01(cfgFile.HasValue("gazeInferenceMatchThreshold") ?
            cfgFile.GetValue<float>("gazeInferenceMatchThreshold") : gazeInferenceMatchThreshold);
        gazeVideoCaptureFormat = cfgFile.HasValue("gazeVideoCaptureFormat") ?
            cfgFile.GetValue<string>("gazeVideoCaptureFormat") : gazeVideoCaptureFormat;
        gazeVideoCaptureStartFrame = cfgFile.HasValue("gazeVideoCaptureStartFrame") ?
            cfgFile.GetValue<int>("gazeVideoCaptureStartFrame") : gazeVideoCaptureStartFrame;
        gazeVideoCaptureEndFrame = cfgFile.HasValue("gazeVideoCaptureEndFrame") ?
            cfgFile.GetValue<int>("gazeVideoCaptureEndFrame") : gazeVideoCaptureEndFrame;
        timewarpsEnabled = cfgFile.HasValue("timewarpsEnabled") ?
            cfgFile.GetValue<bool>("timewarpsEnabled") : timewarpsEnabled;
        timelineBakeRangeStart = cfgFile.HasValue("timelineBakeRangeStart") ?
            cfgFile.GetValue<int>("timelineBakeRangeStart") : timelineBakeRangeStart;
        timelineBakeRangeEnd = cfgFile.HasValue("timelineBakeRangeEnd") ?
            cfgFile.GetValue<int>("timelineBakeRangeEnd") : timelineBakeRangeEnd;
    }
}

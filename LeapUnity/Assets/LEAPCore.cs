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
    /// Kernel size for the low-pass filter used for filtering gaze joint joint velocities in eye gaze inference.
    /// </summary>
    public static int eyeGazeInferenceLowPassKernelSize = 5;

    /// <summary>
    /// Slope of the logistic curve used in computing the probability that some motion interval is a gaze shift.
    /// </summary>
    public static float eyeGazeInferenceGazeShiftLogisticSlope = 1f;

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
    /// Weight of the end-effector constraint probability signal for keyframe extraction.
    /// </summary>
    public static float keyExtractEndEffConstrWeight = 1f;

    /// <summary>
    /// Number of iterations for Laplacian smoothing of probability signals in keyframe extraction.
    /// </summary>
    public static int keyExtractLaplaceNumIterations = 30;

    /// <summary>
    /// Lambda parameter for Laplacian smoothing of probability signals in keyframe extraction.
    /// </summary>
    public static float keyExtractLaplaceLambda = 0.33f;

    /// <summary>
    /// Mu parameter for Laplacian smoothing of probability signals in keyframe extraction.
    /// </summary>
    public static float keyExtractLaplaceMu = -0.34f;

    /// <summary>
    /// Kernel size for the low-pass filter used for filtering the global probability signal in keyframe extraction.
    /// </summary>
    public static int keyExtractLowPassKernelSize = 5;

    /// <summary>
    /// Maximum width of a cluster of local key times corresponding to a single extracted key pose.
    /// </summary>
    public static float keyExtractMaxClusterWidth = 0.5f;

    /// <summary>
    /// Maximum width of a cluster of local gaze shift key times corresponding to a single extracted key pose.
    /// </summary>
    public static float eyeGazeKeyExtractMaxClusterWidth = 0.5f;

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
        cfgFile.AddParam("eyeGazeInferenceGazeShiftLogisticSlope", typeof(float));
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
        eyeGazeInferenceGazeShiftLogisticSlope = cfgFile.HasValue("eyeGazeInferenceGazeShiftLogisticSlope") ?
            cfgFile.GetValue<float>("eyeGazeInferenceGazeShiftLogisticSlope") : eyeGazeInferenceGazeShiftLogisticSlope;
        timewarpsEnabled = cfgFile.HasValue("timewarpsEnabled") ?
            cfgFile.GetValue<bool>("timewarpsEnabled") : timewarpsEnabled;
        timelineBakeRangeStart = cfgFile.HasValue("timelineBakeRangeStart") ?
            cfgFile.GetValue<int>("timelineBakeRangeStart") : timelineBakeRangeStart;
        timelineBakeRangeEnd = cfgFile.HasValue("timelineBakeRangeEnd") ?
            cfgFile.GetValue<int>("timelineBakeRangeEnd") : timelineBakeRangeEnd;
    }
}

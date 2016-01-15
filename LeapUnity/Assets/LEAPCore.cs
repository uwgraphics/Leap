using UnityEngine;
using System;
using System.Collections;
using System.IO;

public class LEAPCore : MonoBehaviour
{
	public const string agentModelDirectory = "Assets/Agents";
    public const string endEffectorConstraintAnnotationsDirectory = "Assets/AnimationAnnotations/EndEffectorConstraints";
    public const string eyeGazeAnnotationsDirectory = "Assets/AnimationAnnotations/EyeGaze";
    public const string timewarpAnnotationsDirectory = "Assets/AnimationAnnotations/Timewarps";
    public const string keyFrameAnnotationsDirectory = "Assets/AnimationAnnotations/KeyFrames";
    public const string eyeTrackDataDirectory = "Assets/AnimationAnnotations/EyeTrack";
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
    /// If true, gaze controller state log will contained detailed state of every gaze joint
    /// </summary>
    public static bool printDetailedGazeControllerState = false;

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
    /// Scaling factor for bone visualization gizmos.
    /// </summary>
    public static float boneGizmoScale = 2f;

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
    /// Load Leap configuration from Leap.cfg file
    /// </summary>
    public static bool LoadConfiguration()
    {
        try
        {
            var reader = new StreamReader(Application.dataPath + "/../Leap/Leap.cfg");
            string line;
            int lineIndex = -1;
            while (!reader.EndOfStream)
            {
                line = reader.ReadLine().Trim();
                ++lineIndex;
                if (line == "")
                    continue;

                int commentStartIndex = line.IndexOf('#');
                line = commentStartIndex >= 0 ? line.Substring(0, commentStartIndex) : line;
                if (line.IndexOf('=') <= 0)
                {
                    Debug.LogError("Error reading Leap.cfg at line " + lineIndex);
                    continue;
                }

                string[] lineElements = line.Split('=');
                string keyStr = lineElements[0].Trim();
                string valueStr = lineElements[1].Trim();
                switch (keyStr)
                {
                    case "editFrameRate":

                        editFrameRate = int.Parse(valueStr);
                        break;

                    case "endEffectorConstraintActivationTime":

                        endEffectorConstraintActivationTime = float.Parse(valueStr);
                        break;

                    case "enableObjectManipulation":

                        enableObjectManipulation = bool.Parse(valueStr);
                        break;

                    case "minEyeGazeLength":

                        minEyeGazeLength = float.Parse(valueStr);
                        break;

                    case "maxEyeGazeGapLength":

                        maxEyeGazeGapLength = float.Parse(valueStr);
                        break;

                    case "useGazeIK":

                        useGazeIK = bool.Parse(valueStr);
                        break;

                    case "useDynamicGazeIKWeights":

                        useDynamicGazeIKWeights = bool.Parse(valueStr);
                        break;

                    case "gazeConstraintActivationTime":

                        gazeConstraintActivationTime = float.Parse(valueStr);
                        break;

                    case "adjustGazeTargetForMovingBase":

                        adjustGazeTargetForMovingBase = bool.Parse(valueStr);
                        break;

                    case "printDetailedGazeControllerState":

                        printDetailedGazeControllerState = bool.Parse(valueStr);
                        break;

                    case "timewarpsEnabled":

                        timewarpsEnabled = bool.Parse(valueStr);
                        break;

                    case "timelineBakeRangeStart":

                        timelineBakeRangeStart = int.Parse(valueStr);
                        break;

                    case "timelineBakeRangeEnd":

                        timelineBakeRangeEnd = int.Parse(valueStr);
                        break;

                    default:

                        Debug.LogError("Unknown configuration option in Leap.cfg at line " + lineIndex);
                        break;
                }
            }

            reader.Close();
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError(string.Format("Unable to load Leap.cfg", ex.Message));
            return false;
        }

        return true;
    }
}

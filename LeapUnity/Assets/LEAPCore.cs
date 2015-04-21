using UnityEngine;
using System;
using System.Collections;
using System.IO;

public class LEAPCore : MonoBehaviour
{
	public const string agentModelDirectory = "Assets/Agents";
	public const string morphAnimationPrefix = "MC";
	public const string morphTargetPrefix = "MT";
	
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
    /// Time (in seconds) over which end-effector constraints are blended in and out.
    /// </summary>
    public static float endEffectorConstraintBlendTime = 1.5f;

    /// <summary>
    /// Eye gaze editor: minimal length of an eye gaze instance (gaze shift + fixation).
    /// </summary>
    public static float minEyeGazeLength = 0.6f;

    /// <summary>
    /// Eye gaze editor: maximal length of gap between two neighboring eye gaze instances
    /// (gaze shift + fixation), also minimal length of a gaze-back instance.
    /// </summary>
    public static float maxEyeGazeGapLength = 2f;

    /// <summary>
    /// Asset subdirectory for eye gaze annotations.
    /// </summary>
    public static string eyeGazeDirectory = "Assets/EyeGaze";
    
    /// <summary>
    /// Suffix appended to names of gaze-back instances.
    /// </summary>
    public static string gazeAheadSuffix = "-Ahead";

    /// <summary>
    /// Suffix appended to names of expressive gaze instances.
    /// </summary>
    public static string expressiveGazeSuffix = "-Expressive";

    /// <summary>
    /// If true, expressive animation will be applied to gaze movements.
    /// </summary>
    public static bool useExpressiveGaze = true;

    /// <summary>
    /// If true, posture IK will incorporate gaze constraints.
    /// </summary>
    public static bool useGazeIK = true;

    /// <summary>
    /// If true, effective gaze target in a gaze shift will be adjusted for movement
    /// in the base animation.
    /// </summary>
    public static bool adjustGazeTargetForMovingBase = false;

    /// <summary>
    /// Load Leap configuration from Leap.cfg file
    /// </summary>
    public static bool LoadConfiguration()
    {
        try
        {
            var reader = new StreamReader(Application.dataPath + "/Leap.cfg");
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

                    case "endEffectorConstraintBlendTime":

                        endEffectorConstraintBlendTime = float.Parse(valueStr);
                        break;

                    case "minEyeGazeLength":

                        minEyeGazeLength = float.Parse(valueStr);
                        break;

                    case "maxEyeGazeGapLength":

                        maxEyeGazeGapLength = float.Parse(valueStr);
                        break;

                    case "useExpressiveGaze":

                        useExpressiveGaze = bool.Parse(valueStr);
                        break;

                    case "useGazeIK":

                        useGazeIK = bool.Parse(valueStr);
                        break;

                    case "adjustGazeTargetForMovingBase":

                        adjustGazeTargetForMovingBase = bool.Parse(valueStr);
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

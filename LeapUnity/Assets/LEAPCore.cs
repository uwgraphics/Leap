using UnityEngine;
using System.Collections;

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
    public static float minEyeGazeLength = 1f;

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
    public static string gazeBackSuffix = "-Back";
}

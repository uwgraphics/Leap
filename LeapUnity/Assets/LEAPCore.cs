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
}

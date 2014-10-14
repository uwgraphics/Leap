using UnityEngine;
using System;
using System.Collections;
using System.IO;

/// <summary>
/// Script for capturing video of scenario execution
/// at constant framerate.
/// </summary>
public class VideoCapture : MonoBehaviour
{
	public int captureFrameRate = 30;
	
	int frameIndex = 1;
	
	public void Start()
	{
		Time.captureFramerate = captureFrameRate;
		
		if( !Directory.Exists("./VideoCapture") )
			Directory.CreateDirectory("./VideoCapture");
		
		frameIndex = 1;
	}
	
	void Update()
	{
		if( !Directory.Exists("./VideoCapture") )
			return;
		
		string filename = string.Format( "./VideoCapture/frame{0:D5}.png", frameIndex++ );
		Application.CaptureScreenshot(filename);
	}
}

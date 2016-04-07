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
    public string videoCaptureDirectory = "./VideoCapture";
	
	private int _frameIndex = 1;

    /// <summary>
    /// Capture screenshot of the application.
    /// </summary>
    /// <param name="frameIndex">Frame index (appended as suffix of the screenshot file name)</param>
    public void CaptureScreenshot(int frameIndex = -1)
    {
        if (!Directory.Exists(videoCaptureDirectory))
        {
            Debug.LogError("Unable to capture video, no capture directory " + videoCaptureDirectory);
            return;
        }

        string path = frameIndex >= 0 ?
            string.Format(videoCaptureDirectory + "frame{0:D5}.png", frameIndex) : "frame.png";
        Application.CaptureScreenshot(path);
    }
	
	public void Start()
	{
		Time.captureFramerate = captureFrameRate;

        // Process video capture directory name
        if (videoCaptureDirectory == "")
            videoCaptureDirectory = "./VideoCapture/";
        if (!videoCaptureDirectory.EndsWith("/") && !videoCaptureDirectory.EndsWith("\\"))
            videoCaptureDirectory += "/";

        // Make sure video capture directory exists and is empty
        if (!Directory.Exists(videoCaptureDirectory))
            Directory.CreateDirectory(videoCaptureDirectory);
        else
            FileUtil.DeleteAllFilesInDirectory(videoCaptureDirectory);

		_frameIndex = 1;
	}

    public void Update()
	{
        CaptureScreenshot(_frameIndex++);
	}
}

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
    public string outputDirectory = "./VideoCapture";
    public string outputFilename = "Video";
	
	private int _frameIndex = 1;

    /// <summary>
    /// Capture screenshot of the application.
    /// </summary>
    /// <param name="frameIndex">Frame index (appended as suffix of the screenshot file name)</param>
    public void CaptureScreenshot(int frameIndex = -1)
    {
        if (!Directory.Exists(outputDirectory))
        {
            Debug.LogError("Unable to capture video, no capture directory " + outputDirectory);
            return;
        }

        string path = frameIndex >= 0 ?
            string.Format(outputDirectory + "frame{0:D5}.png", frameIndex) : "frame.png";
        Application.CaptureScreenshot(path);
    }

    /// <summary>
    /// Generate an output video file from the captured frame sequence.
    /// </summary>
    public void GenerateVideo()
    {
        // Generate a video file from the frame image sequence
        string cmd = "";
        string args = "";
        switch (LEAPCore.videoCaptureFormat)
        {
            case "mp4":
                cmd = "GenerateVideoMP4Baseline";
                args = outputFilename;
                break;
            case "mov":
                cmd = "GenerateVideoMOV";
                args = outputFilename;
                break;
            case "wmv":
                cmd = "GenerateVideoWMV";
                args = outputFilename + " " + Screen.width;
                break;
            default:
                cmd = "GenerateVideoMP4Baseline";
                args = outputFilename;
                break;
        }
        var process = System.Diagnostics.Process.Start(cmd, args);
        process.WaitForExit();
    }
	
	public void Start()
	{
		Time.captureFramerate = captureFrameRate;

        // Process video capture directory name
        if (outputDirectory == "")
            outputDirectory = "./VideoCapture/";
        if (!outputDirectory.EndsWith("/") && !outputDirectory.EndsWith("\\"))
            outputDirectory += "/";

        // Make sure video capture directory exists and is empty
        if (!Directory.Exists(outputDirectory))
            Directory.CreateDirectory(outputDirectory);
        else
            FileUtil.DeleteAllFilesInDirectory(outputDirectory);

		_frameIndex = 1;
	}

    public void Update()
	{
        CaptureScreenshot(_frameIndex++);
	}
}

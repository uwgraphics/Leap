using UnityEngine;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using Emgu.CV;
using Emgu.CV.Util;
//using Emgu.CV.UI;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.Util;

/// <summary>
/// Model for aligning 3D points with a 2D camera image sequence.
/// </summary>
public class VideoCameraModel
{
    /// <summary>
    /// Camera image width (in pixels).
    /// </summary>
    public int ImageWidth
    {
        get;
        private set;
    }

    /// <summary>
    /// Camera image height (in pixels).
    /// </summary>
    public int ImageHeight
    {
        get;
        private set;
    }

    private Matrix3x3 _matCameraDefault = Matrix3x3.identity;
    private float _k1Default, _k2Default, _p1Default, _p2Default, _k3Default;
    private List<CameraModel> _perFrameModels = new List<CameraModel>();

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="imgWidth">Camera image width</param>
    /// <param name="imgHeight">Camera image height</param>
    public VideoCameraModel(int imgWidth = 1280, int imgHeight = 960)
    {
    }

    /// <summary>
    /// Get default camera intrinsics.
    /// </summary>
    /// <param name="matCamera">Camera matrix</param>
    /// <param name="distCoeffs">Distortion coefficients (array of 5 values obtaimed)</param>
    public void GetDefaultIntrinsics(out Matrix3x3 matCamera, out float[] distCoeffs)
    {
        matCamera = _matCameraDefault;
        distCoeffs = new float[5];
        distCoeffs[0] = _k1Default;
        distCoeffs[1] = _k2Default;
        distCoeffs[2] = _p1Default;
        distCoeffs[3] = _p2Default;
        distCoeffs[4] = _k3Default;
    }

    /// <summary>
    /// Set default camera intrinsic parameters.
    /// </summary>
    /// <param name="matCamera">Camera matrix</param>
    /// <param name="distParams">Distortion coefficients (array of 5 values expected)</param>
    public void SetDefaultIntrinsics(Matrix3x3 matCamera, float[] distCoeffs)
    {
        _matCameraDefault = matCamera;
        _k1Default = distCoeffs[0];
        _k2Default = distCoeffs[1];
        _p1Default = distCoeffs[2];
        _p2Default = distCoeffs[3];
        _k3Default = distCoeffs[4];
    }

    /// <summary>
    /// Initialize camera parameters for the image sequence.
    /// </summary>
    /// <param name="wPos">World-space positions of 4 calibration pattern corner points at each frame</param>
    /// <param name="imageDir">Source directory for frame images</param>
    /// <param name="startFrame">Start frame index</param>
    /// <param name="endFrame">End frame index</param>
    /// <param name="calibPatternWidth">Calibration pattern width</param>
    /// <param name="calibPatternHeight">Calibration pattern height</param>
    /// <param name="useDefaultIntrinsics">If false, intrinsic parameters will be estimated for each frame,
    /// otherwise default ones will be used</param>
    /// <param name="writeOutImages">If true, frame images will be written out with calibration pattern corners marked</param>
    /// <param name="outImageDir">Directory for writing output frame images</param>
    public void Align(Vector3[][] wPos, string imageDir, int startFrame, int calibPatternWidth, int calibPatternHeight,
        bool useDefaultIntrinsics = true, bool writeOutImages = false, string outImageDir = "")
    {
        if (startFrame < 0)
            throw new ArgumentOutOfRangeException("startFrame", startFrame, "Start frame index must be 0 or higher");

        if (wPos.Length <= 0)
            throw new ArgumentException("Point set for at least one frame must be specified", "wPos");

        if (imageDir.Length > 0 && imageDir[imageDir.Length - 1] != '/' &&
            imageDir[imageDir.Length - 1] != '\\')
            imageDir += "/";

        int endFrame = startFrame + wPos.Length - 1;
        for (int frameIndex = startFrame; frameIndex <= endFrame; ++frameIndex)
        {
            if (wPos[frameIndex - startFrame] == null)
            {
                // Ignore current frame
                _perFrameModels.Add(null);
                continue;
            }

            // Load frame image
            string imageFilename = "frame" +  (frameIndex + 1).ToString("D5") + ".png";
            var image = Emgu.CV.CvInvoke.Imread(imageDir + imageFilename, Emgu.CV.CvEnum.LoadImageType.Grayscale);
            var outImage = writeOutImages ?
                new Emgu.CV.Mat(image.Width, image.Height, Emgu.CV.CvEnum.DepthType.Cv8U, 3) : null;
            Emgu.CV.CvInvoke.CvtColor(image, outImage, Emgu.CV.CvEnum.ColorConversion.Gray2Bgr);

            // Compute alignment for frame
            var camModel = new CameraModel(ImageWidth, ImageHeight);
            Debug.Log(string.Format("Frame {0} camera model:", frameIndex - startFrame));
            if (camModel.Align(wPos[frameIndex - startFrame], image,
                calibPatternWidth, calibPatternHeight, true, outImage))
            {
                _perFrameModels.Add(camModel);
            }
            else
            {
                Debug.LogWarning(string.Format("Cannot estimate camera model for frame {0}:",
                    frameIndex - startFrame));
                _perFrameModels.Add(null);
            }

            if (writeOutImages)
            {
                // Store image with marked corners
                Emgu.CV.CvInvoke.Imwrite(outImageDir + imageFilename, outImage);
            }
        }
    }
}

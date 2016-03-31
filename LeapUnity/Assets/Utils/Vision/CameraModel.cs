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
/// Model for aligning 3D points with a 2D camera image.
/// </summary>
public class CameraModel
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

    /// <summary>
    /// Matrix for mapping from world space to camera space (camera extrinsics matrix).
    /// </summary>
    public Matrix4x4 WorldToCameraMatrix
    {
        get;
        set;
    }

    // Camera intrinsics:
    private Matrix3x3 _matCamera = Matrix3x3.identity;
    private float _k1, _k2, _p1, _p2, _k3;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="imgWidth">Camera image width</param>
    /// <param name="imgHeight">Camera image height</param>
    public CameraModel(int imgWidth = 1280, int imgHeight = 960)
    {
        WorldToCameraMatrix = Matrix4x4.identity;
        ImageWidth = imgWidth;
        ImageHeight = imgHeight;
    }

    /// <summary>
    /// Get camera intrinsics.
    /// </summary>
    /// <param name="matCamera">Camera matrix</param>
    /// <param name="distCoeffs">Distortion coefficients (array of 5 values obtaimed)</param>
    public void GetIntrinsics(out Matrix3x3 matCamera, out float[] distCoeffs)
    {
        matCamera = _matCamera;
        distCoeffs = new float[5];
        distCoeffs[0] = _k1;
        distCoeffs[1] = _k2;
        distCoeffs[2] = _p1;
        distCoeffs[3] = _p2;
        distCoeffs[4] = _k3;
    }

    /// <summary>
    /// Set camera intrinsic parameters.
    /// </summary>
    /// <param name="matCamera">Camera matrix</param>
    /// <param name="distParams">Distortion coefficients (array of 5 values expected)</param>
    public void SetIntrinsics(Matrix3x3 matCamera, float[] distCoeffs)
    {
        _matCamera = matCamera;
        _k1 = distCoeffs[0];
        _k2 = distCoeffs[1];
        _p1 = distCoeffs[2];
        _p2 = distCoeffs[3];
        _k3 = distCoeffs[4];
    }

    /// <summary>
    /// Get pixel position in the image space that corresponds
    /// to the specified world-space position.
    /// </summary>
    /// <param name="wPos">World-space position</param>
    /// <returns>Image-space position</returns>
    public Vector2 GetImagePosition(Vector3 wPos)
    {
        Vector3 cPos = WorldToCameraMatrix.MultiplyPoint(wPos);
        Vector3 nPos = new Vector3(cPos.x / cPos.z, cPos.y / cPos.z, 1f);
        nPos = new Vector3(-nPos.x, -nPos.y, nPos.z);
        Vector3 dPos = _GetDistortedPoint(nPos);
        Vector3 pPos = _matCamera.MultiplyPoint(dPos);

        return new Vector2(pPos.x, pPos.y);
    }

    /// <summary>
    /// Initialize camera parameters using OpenCV camera calibration.
    /// </summary>
    /// <param name="wPos">World-space positions of 4 calibration pattern corner points</param>
    /// <param name="image">Image containing a calibration pattern</param>
    /// <param name="calibPatternWidth">Calibration pattern width</param>
    /// <param name="calibPatternHeight">Calibration pattern height</param>
    /// <param name="useCurrentIntrinsics">If false, intrinsic parameters will be estimated, otherwise current ones will be used</param>
    /// <param name="outImage">If output image is specified, calibration patterns corners will be draw in</param>
    /// <returns>true if camera parameters estimated successfully, false if calibration pattern could not be found</returns>
    /// <remarks>Calibration pattern corner points must be specified from top left, in clockwise order.</remarks>
    public bool Align(Vector3[] wPos, Mat image, int calibPatternWidth, int calibPatternHeight,
        bool useCurrentIntrinsics = true, Mat outImage = null)
    {
        if (wPos.Length != 4)
        {
            throw new ArgumentException("Must specify 4 calibration pattern corner points", "wPos");
        }

        var cornerPoints = _FindCalibPatternCorners(image, calibPatternWidth, calibPatternHeight, outImage);
        if (cornerPoints == null)
            return false;

        //Align(wPos, cornerPoints, useCurrentIntrinsics);
        return true;
    }

    /// <summary>
    /// Initialize camera parameters using OpenCV camera calibration.
    /// </summary>
    /// <param name="wPos">World-space positions</param>
    /// <param name="pPos">Corresponding image-space positions</param>
    /// <param name="useCurrentIntrinsics">If false, intrinsic parameters will be estimated, otherwise current ones will be used</param>
    public void Align(Vector3[] wPos, Vector2[] pPos, bool useCurrentIntrinsics = true)
    {
        int n = wPos.Length;
        if (n < 4)
            throw new ArgumentException("OpenCV camera calibration requires at least 4 point correspondences!", "wPos"); 
        if (pPos.Length != n)
            throw new ArgumentException("Number of world-space points is not the same as the number of image-space points!", "pPos"); 

        // Get alignment points in image space
        PointF[][] xp = new PointF[1][];
        xp[0] = new PointF[n];
        for (int i = 0; i < n; ++i)
        {
            xp[0][i] = new PointF(pPos[i].x, pPos[i].y);
        }

        // Get alignment points in world space
        MCvPoint3D32f[][] x = new MCvPoint3D32f[1][];
        x[0] = new MCvPoint3D32f[n];
        for (int i = 0; i < n; ++i)
        {
            var xi = wPos[i];
            x[0][i] = new MCvPoint3D32f(xi.x, xi.y, xi.z);
        }

        // Initialize OpenCV camera instrinsics
        var intrinsics = new IntrinsicCameraParameters(5);
        if (useCurrentIntrinsics)
        {
            intrinsics.DistortionCoeffs[0, 0] = _k1;
            intrinsics.DistortionCoeffs[1, 0] = _k2;
            intrinsics.DistortionCoeffs[2, 0] = _p1;
            intrinsics.DistortionCoeffs[3, 0] = _p2;
            intrinsics.DistortionCoeffs[4, 0] = _k3;
            intrinsics.IntrinsicMatrix[0, 0] = _matCamera[0, 0];
            intrinsics.IntrinsicMatrix[0, 1] = _matCamera[0, 1];
            intrinsics.IntrinsicMatrix[0, 2] = _matCamera[0, 2];
            intrinsics.IntrinsicMatrix[1, 0] = _matCamera[1, 0];
            intrinsics.IntrinsicMatrix[1, 1] = _matCamera[1, 1];
            intrinsics.IntrinsicMatrix[1, 2] = _matCamera[1, 2];
            intrinsics.IntrinsicMatrix[2, 0] = _matCamera[2, 0];
            intrinsics.IntrinsicMatrix[2, 1] = _matCamera[2, 1];
            intrinsics.IntrinsicMatrix[2, 2] = _matCamera[2, 2];
        }

        // Compute OpenCV camera parameters
        ExtrinsicCameraParameters[] extrinsics = null;
        CameraCalibration.CalibrateCamera(x, xp, new Size(ImageWidth, ImageHeight), intrinsics,
            useCurrentIntrinsics ? CalibType.UserIntrinsicGuess : CalibType.Default,
            new MCvTermCriteria(0.01), out extrinsics);
        double[,] matExtrinsics = new double[3, 4];
        extrinsics[0].ExtrinsicMatrix.CopyTo(matExtrinsics);

        // Get estimated camera intrinsics
        _k1 = (float)intrinsics.DistortionCoeffs[0, 0];
        _k2 = (float)intrinsics.DistortionCoeffs[1, 0];
        _p1 = (float)intrinsics.DistortionCoeffs[2, 0];
        _p2 = (float)intrinsics.DistortionCoeffs[3, 0];
        _k3 = (float)intrinsics.DistortionCoeffs[4, 0];
        _matCamera[0, 0] = (float)intrinsics.IntrinsicMatrix[0, 0];
        _matCamera[0, 1] = (float)intrinsics.IntrinsicMatrix[0, 1];
        _matCamera[0, 2] = (float)intrinsics.IntrinsicMatrix[0, 2];
        _matCamera[1, 0] = (float)intrinsics.IntrinsicMatrix[1, 0];
        _matCamera[1, 1] = (float)intrinsics.IntrinsicMatrix[1, 1];
        _matCamera[1, 2] = (float)intrinsics.IntrinsicMatrix[1, 2];
        _matCamera[2, 0] = (float)intrinsics.IntrinsicMatrix[2, 0];
        _matCamera[2, 1] = (float)intrinsics.IntrinsicMatrix[2, 1];
        _matCamera[2, 2] = (float)intrinsics.IntrinsicMatrix[2, 2];

        // Get estimated world-to-camera matrix
        var matWorldToLocal = Matrix4x4.identity;
        for (int i = 0; i < 3; ++i)
            for (int j = 0; j < 4; ++j)
                matWorldToLocal[i, j] = (float)matExtrinsics[i, j];
        WorldToCameraMatrix = matWorldToLocal;

        // Extract translation, rotation, and scale components
        var transAlign = matWorldToLocal.GetColumn(3);
        var matRotAlign = new Matrix3x3();
        for (int i = 0; i < 3; ++i)
            for (int j = 0; j < 3; ++j)
                matRotAlign[i, j] = matWorldToLocal[i, j];
        Vector3 axisAlign;
        float angleAlign;
        matRotAlign.ToAngleAxis(out angleAlign, out axisAlign);
        var scaleAlign = new Vector3(matRotAlign.GetRow(0).magnitude,
            matRotAlign.GetRow(1).magnitude, matRotAlign.GetRow(2).magnitude);

        Debug.Log(string.Format("World-to-camera matrix: tc = ({0}, {1}, {2}), Rc = ({3}, {4}, {5}, {6}), sc = ({7}, {8}, {9})",
                transAlign.x, transAlign.y, transAlign.z, angleAlign, axisAlign.x, axisAlign.y, axisAlign.z,
                scaleAlign.x, scaleAlign.y, scaleAlign.z));

        // Test computed parameters
        for (int i = 0; i < n; ++i)
        {
            var pPosi0 = pPos[i];
            var wPosi = wPos[i];
            var pPosi = GetImagePosition(wPosi);

            Debug.Log(string.Format("Ground-truth: ({0}, {1}); computed point: ({2}, {3})",
                pPosi0.x, pPosi0.y, pPosi.x, pPosi.y));
        }
    }

    // Find calibration pattern corners in the image
    private Vector2[] _FindCalibPatternCorners(Mat image, int calibPatternWidth, int calibPatternHeight, Mat outImage)
    {
        if (image.NumberOfChannels == 1)
            // Grayscale image, we can improve contrast
            Emgu.CV.CvInvoke.EqualizeHist(image, image);

        var cornerPoints = new Emgu.CV.Util.VectorOfPoint();
        CvInvoke.FindChessboardCorners(image, new Size(calibPatternWidth, calibPatternHeight), cornerPoints);

        if (cornerPoints.Size < 4)
        {
            // Calibration pattern not found
            return null;
        }

        // Compute rectangle that envelops the pattern
        var outCornerPoints = new Vector2[4];
        outCornerPoints[0] = new Vector2(cornerPoints[0].X, cornerPoints[0].Y);
        outCornerPoints[1] = new Vector2(cornerPoints[calibPatternWidth - 1].X, cornerPoints[calibPatternWidth - 1].Y);
        outCornerPoints[2] = new Vector2(cornerPoints[cornerPoints.Size - 1].X, cornerPoints[cornerPoints.Size - 1].Y);
        outCornerPoints[3] = new Vector2(cornerPoints[cornerPoints.Size - calibPatternWidth].X,
            cornerPoints[cornerPoints.Size - calibPatternWidth].Y);

        if (outImage != null)
        {
            // Mark corners in the image
            int ptRadius = 5;
            CvInvoke.Circle(outImage, new Point((int)outCornerPoints[0].x, (int)outCornerPoints[0].y), ptRadius,
                new Emgu.CV.Structure.MCvScalar(0, 0, 255), -1);
            CvInvoke.Circle(outImage, new Point((int)outCornerPoints[1].x, (int)outCornerPoints[1].y), ptRadius,
                new Emgu.CV.Structure.MCvScalar(0, 255, 0), -1);
            CvInvoke.Circle(outImage, new Point((int)outCornerPoints[2].x, (int)outCornerPoints[2].y), ptRadius,
                new Emgu.CV.Structure.MCvScalar(255, 0, 0), -1);
            CvInvoke.Circle(outImage, new Point((int)outCornerPoints[3].x, (int)outCornerPoints[3].y), ptRadius,
                new Emgu.CV.Structure.MCvScalar(255, 0, 255), -1);
        }

        return outCornerPoints;
    }

    /*
    // Compute aligning transformation between eye tracker camera frame
    // and the eye tracker bone on the character model
    private void _ComputeEyeTrackerExtrinsics(EyeTrackAlignPoint[] alignPoints, int frameIndex)
    {
        int n = alignPoints.Length;

        Debug.Log("Getting alignment points in eye tracker image space...");

        // Get alignment points in image space
        Vector3[] xp = new Vector3[n];
        for (int i = 0; i < n; ++i)
        {
            xp[i] = new Vector3(alignPoints[i].imagePosition.x, alignPoints[i].imagePosition.y, 1f);
            
            Debug.Log(string.Format("xp[{0}] = ({1}, {2})", i, xp[i].x, xp[i].y));
        }

        Debug.Log("Mapping alignment points from eye tracker image space to camera space...");

        // Map the alignment points from image space to distorted camera space
        Vector3[] xd = new Vector3[n];
        Matrix3x3 matCameraInv = _matCamera.inverse;
        for (int i = 0; i < n; ++i)
        {
            xd[i] = matCameraInv.MultiplyPoint(xp[i]);

            Debug.Log(string.Format("xd[{0}] = ({1}, {2}, {3})", i, xd[i].x, xd[i].y, xd[i].z));
        }

        Debug.Log("Removing radial lens distortion from alignment point positions...");

        // Remove radial lens distortion from alignment point positions
        Vector3[] xn = new Vector3[n];
        for (int i = 0; i < n; ++i)
        {
            xn[i] = _RemoveRadialDistorsionFromPoint(xd[i]);
            xn[i] = new Vector3(-xn[i].x, -xn[i].y, xn[i].z); // flip handedness

            Debug.Log(string.Format("xn[{0}] = ({1}, {2}, {3})", i, xn[i].x, xn[i].y, xn[i].z));
        }

        Debug.Log("Getting alignment points in eye tracker bone space...");

        // Get alignment points in eye tracker bone space
        Vector3[] x = new Vector3[n];
        for (int i = 0; i < n; ++i)
        {
            var alignPoint = alignPoints[i];
            BaseAnimation.Apply(frameIndex, AnimationLayerMode.Override);
            x[i] = EyeTrackerBone.InverseTransformPoint(alignPoint.alignPointObj.transform.position);

            Debug.Log(string.Format("x[{0}] = ({1}, {2}, {3})", i, x[i].x, x[i].y, x[i].z));
        }
        Model.GetComponent<ModelController>()._ResetToInitialPose();

        Debug.Log("Estimating affine transformation from eye tracker bone space to camera space...");

        // Initialize estimation of camera extrinsics (aligning transformation)
        float[] zc = new float[n];
        float[] zcPrev = new float[n];
        float dzc = 0f;
        Vector3[] xc = new Vector3[n];
        // Initial estimate of depth in the camera system
        _GetPointZ(x, zc);

        // Estimate camera extrinsics
        do
        {
            zc.CopyTo(zcPrev, 0);
            for (int i = 0; i < n; ++i)
                xc[i] = new Vector3(xn[i].x * zc[i], xn[i].y * zc[i], zc[i]);

            // Solve for aligning affine transformation
            GeometryUtil.AlignPointSets(x, xc, out _transAlign, out _rotAlign, out _scaleAlign);

            Debug.Log(string.Format("sc = {0}, Rc = {1}, tc = ({2}, {3}, {4})",
                _scaleAlign, _rotAlign.ToString(), _transAlign.x, _transAlign.y, _transAlign.z));

            // Solve for improved estimate of zc
            for (int i = 0; i < n; ++i)
                xc[i] = _scaleAlign * _rotAlign.MultiplyPoint(x[i]) + _transAlign;
            _GetPointZ(xc, zc);

            string zcStr = "";
            for (int i = 0; i < n; ++i)
                zcStr += (i < n - 1 ? zc[i].ToString() + ", " : zc[i].ToString());
            Debug.Log("zc = (" + zcStr + ")");

            // How much did zc change?
            dzc = 0f;
            for (int i = 0; i < n; ++i)
            {
                float dzci = zc[i] - zcPrev[i];
                dzc += (dzci * dzci);
            }
        }
        while (dzc > 0.01f);
    }

    // Solve for position of a point that does not have radial distorsion applied to it
    private Vector3 _RemoveRadialDistorsionFromPoint(Vector3 xd)
    {
        Vector2 x0 = new Vector2(xd.x, xd.y);
        Vector2 x = x0;
        float dx = 0f;
        do
        {
            Vector2 xprev = x;
            float r = x.sqrMagnitude;
            float r2 = r * r;
            float r4 = r2 * r2;
            float d = 1f + _k1 * r2 + _k2 * r4 + _k3 * r4 * r2;
            x = x0 / d;
            dx = (x - xprev).magnitude;
        }
        while (dx > 0.0001f);

        return new Vector3(x.x, x.y, 1f);
    }

    // Get z coordinates of the specified set of points
    private void _GetPointZ(Vector3[] pts, float[] z)
    {
        for (int i = 0; i < pts.Length; ++i)
            z[i] = pts[i].z;
    }*/

    // Apply lens distortion to camera-space point
    private Vector3 _GetDistortedPoint(Vector3 x)
    {
        float r2 = x.x * x.x + x.y * x.y;
        float r4 = r2 * r2;
        float d = 1f + _k1 * r2 + _k2 * r4 + _k3 * r4 * r2;

        return new Vector3(x.x * d, x.y * d, 1f);
    }
}

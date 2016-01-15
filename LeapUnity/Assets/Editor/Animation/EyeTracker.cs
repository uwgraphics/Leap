using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

public enum EyeTrackEventType
{
    Fixation,
    Saccade,
    Blink,
    Unknown
}

public struct EyeTrackSample
{
    /// <summary>
    /// Position in eye tracker screen space.
    /// </summary>
    public Vector2 position;

    /// <summary>
    /// Eye tracker event type.
    /// </summary>
    public EyeTrackEventType eventType;

    public EyeTrackSample(Vector2 position, EyeTrackEventType eventType)
    {
        this.position = position;
        this.eventType = eventType;
    }
}

public struct EyeTrackAlignPoint
{
    /// <summary>
    /// Scene object that marks the alignment point.
    /// </summary>
    public GameObject alignPointObj;

    /// <summary>
    /// Position of the alignment point in the eye tracker image  space.
    /// </summary>
    public Vector2 imagePosition;

    public EyeTrackAlignPoint(GameObject alignPointObj, Vector2 imgPos)
    {
        this.alignPointObj = alignPointObj;
        this.imagePosition = imgPos;
    }
}

/// <summary>
/// Class representing an offline eye tracker. It enables loading of
/// recorded eye tracking data and its alignment with Unity coordinate frame.
/// </summary>
public class EyeTracker
{
    /// <summary>
    /// Animation clip instance that owns this eye tracker instance.
    /// </summary>
    public AnimationClipInstance BaseAnimation
    {
        get;
        private set;
    }

    /// <summary>
    /// Character model.
    /// </summary>
    public GameObject Model
    {
        get { return BaseAnimation.Model; }
    }

    /// <summary>
    /// Eye tracker bone.
    /// </summary>
    public Transform EyeTrackerBone
    {
        get;
        private set;
    }

    /// <summary>
    /// Eye tracker image width.
    /// </summary>
    public int ImageWidth
    {
        get;
        private set;
    }

    /// <summary>
    /// Eye tracker image height.
    /// </summary>
    public int ImageHeight
    {
        get;
        private set;
    }

    /// <summary>
    /// Eye tracking samples.
    /// </summary>
    public IList<EyeTrackSample> Samples
    {
        get { return _samples.AsReadOnly(); }
    }

    // Eye tracking dataset:
    private List<EyeTrackSample> _samples = new List<EyeTrackSample>();
    
    // Eye tracker camera intrinsics:
    private Matrix3x3 _matCamera = Matrix3x3.identity;
    private float _k1, _k2, _k3;

    // Eye tracker camera extrinsics:
    private Vector3 _transAlign = Vector3.zero;
    private Matrix3x3 _rotAlign = Matrix3x3.identity;
    private float _scaleAlign = 1f;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="model">Character model</param>
    /// <param name="imgWidth">Eye tracker image width</param>
    /// <param name="imgHeight">Eye tracker image height</param>
    /// <param name="imgXCor">Correction along x direction in eye tracker image space</param>
    /// <param name="imgYCor">Correction along y direction in eye tracker image space</param>
    public EyeTracker(AnimationClipInstance instance, int imgWidth = 1280, int imgHeight = 960,
        int imgXCor = 0, int imgYCor = 0)
    {
        BaseAnimation = instance;
        var root = ModelUtil.FindRootBone(Model);
        EyeTrackerBone = ModelUtil.FindBoneWithTag(root, "EyeTracker");
        ImageWidth = imgWidth;
        ImageHeight = imgHeight;

        if (EyeTrackerBone == null)
            throw new Exception("Character model " + Model.name + " does not have an eye tracker bone defined");

        _LoadEyeTrackSamples(imgXCor, imgYCor);
    }

    /// <summary>
    /// Initialize the eye tracker from specified animation data.
    /// </summary>
    /// <param name="alignPoints">Eye tracker alignment points</param>
    /// <param name="frameIndex">Frame in the base animation at which the alignment point set is viewed</param>
    public void Init(EyeTrackAlignPoint[] alignPoints, int frameIndex)
    {
        _InitEyeTrackerIntrinsics();
        _ComputeEyeTrackerExtrinsics(alignPoints, frameIndex);
    }

    /// <summary>
    /// Get position in eye tracker image space that corresponds
    /// to the specified world-space position.
    /// </summary>
    /// <param name="wPos">World-space position</param>
    /// <returns>Eye tracker image-space position</returns>
    public Vector2 GetImagePosition(Vector3 wPos)
    {
        Vector3 pos = EyeTrackerBone.InverseTransformPoint(wPos);
        Vector3 cPos = _scaleAlign * _rotAlign.MultiplyPoint(pos) + _transAlign;
        Vector3 nPos = new Vector3(cPos.x / cPos.z, cPos.y / cPos.z, 1f);
        nPos = new Vector3(-nPos.x, -nPos.y, nPos.z);
        Vector3 dPos = _GetDistortedPoint(nPos);
        Vector3 pPos = _matCamera.MultiplyPoint(dPos);

        return new Vector2(pPos.x, pPos.y);
    }

    // Load eye tracking samples for the specified animation clip
    private void _LoadEyeTrackSamples(int imgXCor, int imgYCor)
    {
        // Get eye tracking samples file path
        string path = Application.dataPath + LEAPCore.eyeTrackDataDirectory.Substring(
               LEAPCore.eyeTrackDataDirectory.IndexOfAny(@"/\".ToCharArray()));
        if (path[path.Length - 1] != '/' && path[path.Length - 1] != '\\')
            path += '/';
        path += (BaseAnimation.AnimationClip.name + "#Samples.txt");

        if (!File.Exists(path))
        {
            UnityEngine.Debug.LogError(string.Format("No eye tracking samples file at path " + path));
            return;
        }

        try
        {
            var csvData = new CSVData();

            // Define sample attributes
            csvData.AddAttribute("Time", typeof(int));
            csvData.AddAttribute("Type", typeof(string));
            csvData.AddAttribute("Trial", typeof(int));
            csvData.AddAttribute("L Dia X [px]", typeof(float));
            csvData.AddAttribute("L Dia Y [px]", typeof(float));
            csvData.AddAttribute("L Pupil Diameter [mm]", typeof(float));
            csvData.AddAttribute("R Dia X [px]", typeof(float));
            csvData.AddAttribute("R Dia Y [px]", typeof(float));
            csvData.AddAttribute("R Pupil Diameter [mm]", typeof(float));
            csvData.AddAttribute("B POR X [px]", typeof(float));
            csvData.AddAttribute("B POR Y [px]", typeof(float));
            csvData.AddAttribute("L POR X [px]", typeof(float));
            csvData.AddAttribute("L POR Y [px]", typeof(float));
            csvData.AddAttribute("R POR X [px]", typeof(float));
            csvData.AddAttribute("R POR Y [px]", typeof(float));
            csvData.AddAttribute("B AOI Hit", typeof(string));
            csvData.AddAttribute("L EPOS X", typeof(float));
            csvData.AddAttribute("L EPOS Y", typeof(float));
            csvData.AddAttribute("L EPOS Z", typeof(float));
            csvData.AddAttribute("R EPOS X", typeof(float));
            csvData.AddAttribute("R EPOS Y", typeof(float));
            csvData.AddAttribute("R EPOS Z", typeof(float));
            csvData.AddAttribute("L GVEC X", typeof(float));
            csvData.AddAttribute("L GVEC Y", typeof(float));
            csvData.AddAttribute("L GVEC Z", typeof(float));
            csvData.AddAttribute("R GVEC X", typeof(float));
            csvData.AddAttribute("R GVEC Y", typeof(float));
            csvData.AddAttribute("R GVEC Z", typeof(float));
            csvData.AddAttribute("Trigger", typeof(int));
            csvData.AddAttribute("Frame", typeof(string));
            csvData.AddAttribute("Aux1", typeof(string));
            csvData.AddAttribute("B Event Info", typeof(string));
            csvData.AddAttribute("Stimulus", typeof(string));

            // Get attribute indexes
            int positionXIndex = csvData.GetAttributeIndex("B POR X [px]");
            int positionYIndex = csvData.GetAttributeIndex("B POR Y [px]");
            int eventTypeIndex = csvData.GetAttributeIndex("B Event Info");

            // Read eye tracking samples
            _samples.Clear();
            csvData.ReadFromFile(path);
            for (int rowIndex = 0; rowIndex < csvData.NumberOfRows; ++rowIndex)
            {
                // Read sample
                float posX = csvData[rowIndex].GetValue<float>(positionXIndex);
                float posY = csvData[rowIndex].GetValue<float>(positionYIndex);
                string eventTypeStr = csvData[rowIndex].GetValue<string>(eventTypeIndex);
                var eventType = EyeTrackEventType.Unknown;
                switch (eventTypeStr)
                {
                    case "Blink":
                        eventType = EyeTrackEventType.Blink;
                        break;
                    case "Fixation":
                        eventType = EyeTrackEventType.Fixation;
                        break;
                    case "Saccade":
                        eventType = EyeTrackEventType.Saccade;
                        break;
                    default:
                        eventType = EyeTrackEventType.Unknown;
                        break;
                }

                // Add sample
                _samples.Add(new EyeTrackSample(new Vector2(posX + imgXCor, posY + imgYCor), eventType));
            }

            Debug.Log(string.Format("Loaded {0} eye track samples", _samples.Count));

            // Write out the samples for MATLAB visualization
            var outCsvData = new CSVData();
            outCsvData.AddAttribute("positionX", typeof(float));
            outCsvData.AddAttribute("positionY", typeof(float));
            outCsvData.AddAttribute("eventType", typeof(int));
            for (int sampleIndex = 0; sampleIndex < _samples.Count; ++sampleIndex)
            {
                var sample = _samples[sampleIndex];
                outCsvData.AddData(sample.position.x, sample.position.y, (int)sample.eventType);
            }
            outCsvData.WriteToFile("../Matlab/EyeTracker/samples.csv");
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError(string.Format("Unable to load eye tracking samples from asset file {0}: {1}", path, ex.Message));
        }
    }

    // Initialize intrinsic parameters of the eye tracker camera
    // Note: these are for the SMI eye tracker camera
    private void _InitEyeTrackerIntrinsics()
    {
        // Initialize camera matrix
        _matCamera[0, 0] = 1108.715726f;
        _matCamera[0, 1] = 0f;
        _matCamera[0, 2] = 639.5f;
        _matCamera[1, 0] = 0f;
        _matCamera[1, 1] = _matCamera[0, 0];
        _matCamera[1, 2] = 479.5f;
        _matCamera[2, 0] = 0f;
        _matCamera[2, 1] = 0f;
        _matCamera[2, 2] = 1f;

        // Set radial lens distortion parameters
        _k1 = 0.080115f;
        _k2 = -0.797094f;
        _k3 = 1.415777f;

        Debug.Log("Set eye tracker camera intrinsics");
    }

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
    }

    // Apply radial distorsion to the specified point
    private Vector3 _GetDistortedPoint(Vector3 x)
    {
        float r2 = x.x * x.x + x.y * x.y;
        float r4 = r2 * r2;
        float d = 1f + _k1 * r2 + _k2 * r4 + _k3 * r4 * r2;

        return new Vector3(x.x * d, x.y * d, 1f);
    }
}

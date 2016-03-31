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
    /// Position in eye tracker image space.
    /// </summary>
    public Vector2 imagePosition;

    /// <summary>
    /// Gaze direction of the left eye.
    /// </summary>
    public Vector3 lEyeDirection;

    /// <summary>
    /// Gaze direction of the right eye.
    /// </summary>
    public Vector3 rEyeDirection;

    /// <summary>
    /// Eye tracker event type.
    /// </summary>
    public EyeTrackEventType eventType;

    public EyeTrackSample(Vector2 position, Vector3 lEyeDir, Vector3 rEyeDir,
        EyeTrackEventType eventType)
    {
        this.imagePosition = position;
        this.lEyeDirection = lEyeDir;
        this.rEyeDirection = rEyeDir;
        this.eventType = eventType;
    }
}

public struct EyeTrackEvent
{
    /// <summary>
    /// Eye track event type.
    /// </summary>
    public EyeTrackEventType eventType;

    /// <summary>
    /// Event start frame.
    /// </summary>
    public int startFrame;

    /// <summary>
    /// Event frame length.
    /// </summary>
    public int frameLength;

    /// <summary>
    /// Eye position in the eye tracker image at event start.
    /// </summary>
    public Vector2 imageStartPosition;

    /// <summary>
    /// Eye position in the eye tracker image at event end.
    /// </summary>
    public Vector2 imageEndPosition;

    public EyeTrackEvent(EyeTrackEventType eventType, int startFrame, int frameLength,
        Vector2 imageStartPosition, Vector2 imageEndPosition)
    {
        this.eventType = eventType;
        this.startFrame = startFrame;
        this.frameLength = frameLength;
        this.imageStartPosition = imageStartPosition;
        this.imageEndPosition = imageEndPosition;
    }
}

/// <summary>
/// Container for data from an offline eye tracker. It enables loading of
/// and operations on recorded eye tracking data.
/// </summary>
public class EyeTrackData
{
    /// <summary>
    /// Default eye tracker camera model with precomputed intrinsics.
    /// Should work for SMI eye tracking glasses.
    /// </summary>
    public static CameraModel DefaultCameraModel
    {
        get
        {
            var camModel = new CameraModel();
            var matCamera = new Matrix3x3();
            matCamera.m00 = 1.1087157e+003f;
            matCamera.m01 = 0f;
            matCamera.m02 = 6.395e+002f;
            matCamera.m10 = 0f;
            matCamera.m11 = 1.1087157e+003f;
            matCamera.m12 = 4.795e+002f;
            matCamera.m20 = 0f;
            matCamera.m21 = 0f;
            matCamera.m22 = 1f;
            var distCoeffs = new float[5];
            distCoeffs[0] = 8.0114708e-002f;
            distCoeffs[1] = -7.9709385e-001f;
            distCoeffs[2] = 0f;
            distCoeffs[3] = 0f;
            distCoeffs[4] = 1.4157773e+000f;
            camModel.SetIntrinsics(matCamera, distCoeffs);

            return camModel;
        }
    }

    /// <summary>
    /// Character model.
    /// </summary>
    public GameObject Model
    {
        get;
        private set;
    }

    /// <summary>
    /// Eye tracker bone.
    /// </summary>
    public Transform EyeTrackerBone
    {
        get
        {
            return ModelUtil.FindBoneWithTag(Model.transform, "EyeTracker");
        }
    }

    /// <summary>
    /// Base animation clip that matches the eye tracking data.
    /// </summary>
    public AnimationClip BaseAnimationClip
    {
        get;
        private set;
    }

    /// <summary>
    /// Frame offset of eye tracking data relative to the animation data.
    /// </summary>
    public int FrameOffset
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
    /// Eye tracking image-space location correction along x-axis.
    /// </summary>
    public int ImageXCorrection
    {
        get;
        private set;
    }

    /// <summary>
    /// Eye tracking image-space location correction along y-axis.
    /// </summary>
    public int ImageYCorrection
    {
        get;
        private set;
    }

    /// <summary>
    /// Eye tracker camera calibration pattern width.
    /// </summary>
    public int CalibPatternWidth
    {
        get;
        private set;
    }

    /// <summary>
    /// Eye tracker camera calibration pattern height.
    /// </summary>
    public int CalibPatternHeight
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

    /// <summary>
    /// Eye tracking events.
    /// </summary>
    public IList<EyeTrackEvent> Events
    {
        get { return _events.AsReadOnly(); }
    }

    // Eye tracking dataset:
    private List<EyeTrackSample> _samples = new List<EyeTrackSample>();
    private List<EyeTrackEvent> _events = new List<EyeTrackEvent>();

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="model">Character model</param>
    /// <param name="baseAnimationClip">Base animation clip</param>
    public EyeTrackData(GameObject model, AnimationClip baseAnimationClip)
    {
        Model = model;
        BaseAnimationClip = baseAnimationClip;

        // Set default parameter values
        FrameOffset = 0;
        ImageWidth = 1280;
        ImageHeight = 960;
        ImageXCorrection = 0;
        ImageYCorrection = 0;
        CalibPatternWidth = 9;
        CalibPatternHeight = 6;

        _LoadParams();
        _LoadSamples();
        _LoadEvents();
        _FillEventGaps();
        _RemoveBlinkEvents();
        _MergeSaccadeFixationPairs();
        _PrintEvents();
    }

    /// <summary>
    /// Generate gaze animation instances from eye tracking events.
    /// </summary>
    /// <param name="timeline">Animation timeline</param>
    /// <param name="layerName">Name of the layer for gaze animation instances</param>
    public void GenerateEyeGazeInstances(AnimationTimeline timeline, string layerName = "Gaze")
    {
        timeline.RemoveAllAnimations(layerName, Model.name);

        for (int eventIndex = 0; eventIndex < Events.Count; ++eventIndex)
        {
            var evt = Events[eventIndex];
            var instance = new EyeGazeInstance(BaseAnimationClip.name + "Gaze" + (eventIndex + 1),
                Model, evt.frameLength, -1, null, 0f, 0f, true, BaseAnimationClip, null);
            EyeGazeEditor.AddEyeGaze(timeline, instance, evt.startFrame, layerName);
        }
    }

    // Load eye tracking data parameters
    private void _LoadParams()
    {
        // Get eye tracking parameters file path
        string path = Application.dataPath + LEAPCore.eyeTrackDataDirectory.Substring(
               LEAPCore.eyeTrackDataDirectory.IndexOfAny(@"/\".ToCharArray()));
        if (path[path.Length - 1] != '/' && path[path.Length - 1] != '\\')
            path += '/';
        path += (BaseAnimationClip.name + "#Params.txt");

        if (!File.Exists(path))
        {
            UnityEngine.Debug.LogError(string.Format("No eye tracking parameters file at path " + path));
            return;
        }

        Debug.Log("Loading eye tracking parameters...");

        var paramFile = new ConfigFile();

        // Define parameters
        paramFile.AddParam("frameOffset", typeof(int));
        paramFile.AddParam("imageWidth", typeof(int));
        paramFile.AddParam("imageHeight", typeof(int));
        paramFile.AddParam("imageXCorrection", typeof(int));
        paramFile.AddParam("imageYCorrection", typeof(int));
        paramFile.AddParam("calibPatternWidth", typeof(int));
        paramFile.AddParam("calibPatternHeight", typeof(int));

        // Read parameter values
        paramFile.ReadFromFile(path);
        FrameOffset = paramFile.HasValue("frameOffset") ? paramFile.GetValue<int>("frameOffset") : FrameOffset;
        ImageWidth = paramFile.HasValue("imageWidth") ? paramFile.GetValue<int>("imageWidth") : ImageWidth;
        ImageHeight = paramFile.HasValue("imageHeight") ? paramFile.GetValue<int>("imageHeight") : ImageHeight;
        ImageXCorrection = paramFile.HasValue("imageXCorrection") ? paramFile.GetValue<int>("imageXCorrection") : ImageXCorrection;
        ImageYCorrection = paramFile.HasValue("imageYCorrection") ? paramFile.GetValue<int>("imageYCorrection") : ImageYCorrection;
        CalibPatternWidth = paramFile.HasValue("calibPatternWidth") ? paramFile.GetValue<int>("calibPatternWidth") : CalibPatternWidth;
        CalibPatternHeight = paramFile.HasValue("calibPatternHeight") ? paramFile.GetValue<int>("calibPatternHeight") : CalibPatternHeight;
    }

    // Load eye tracking samples for the current animation clip
    private void _LoadSamples()
    {
        // Get eye tracking samples file path
        string path = Application.dataPath + LEAPCore.eyeTrackDataDirectory.Substring(
               LEAPCore.eyeTrackDataDirectory.IndexOfAny(@"/\".ToCharArray()));
        if (path[path.Length - 1] != '/' && path[path.Length - 1] != '\\')
            path += '/';
        path += (BaseAnimationClip.name + "#Samples.txt");

        if (!File.Exists(path))
        {
            UnityEngine.Debug.LogError(string.Format("No eye tracking samples file at path " + path));
            return;
        }

        Debug.Log("Loading eye tracking samples...");

        try
        {
            var csvData = new CSVDataFile();

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
            int imgPosXIndex = csvData.GetAttributeIndex("B POR X [px]");
            int imgPosYIndex = csvData.GetAttributeIndex("B POR Y [px]");
            int lEyeDirXIndex = csvData.GetAttributeIndex("L GVEC X");
            int lEyeDirYIndex = csvData.GetAttributeIndex("L GVEC Y");
            int lEyeDirZIndex = csvData.GetAttributeIndex("L GVEC Z");
            int rEyeDirXIndex = csvData.GetAttributeIndex("R GVEC X");
            int rEyeDirYIndex = csvData.GetAttributeIndex("R GVEC Y");
            int rEyeDirZIndex = csvData.GetAttributeIndex("R GVEC Z");
            int eventTypeIndex = csvData.GetAttributeIndex("B Event Info");

            // Read eye tracking samples
            _samples.Clear();
            csvData.ReadFromFile(path);
            for (int rowIndex = 0; rowIndex < csvData.NumberOfRows; ++rowIndex)
            {
                // Read sample
                float imgPosX = csvData[rowIndex].GetValue<float>(imgPosXIndex);
                float imgPosY = csvData[rowIndex].GetValue<float>(imgPosYIndex);
                float lEyeDirX = csvData[rowIndex].GetValue<float>(lEyeDirXIndex);
                float lEyeDirY = csvData[rowIndex].GetValue<float>(lEyeDirYIndex);
                float lEyeDirZ = csvData[rowIndex].GetValue<float>(lEyeDirZIndex);
                float rEyeDirX = csvData[rowIndex].GetValue<float>(rEyeDirXIndex);
                float rEyeDirY = csvData[rowIndex].GetValue<float>(rEyeDirYIndex);
                float rEyeDirZ = csvData[rowIndex].GetValue<float>(rEyeDirZIndex);
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
                var sample = new EyeTrackSample(
                    new Vector2(imgPosX + ImageXCorrection, imgPosY + ImageYCorrection),
                    new Vector3(lEyeDirX, lEyeDirY, lEyeDirZ),
                    new Vector3(rEyeDirX, rEyeDirY, rEyeDirZ),
                    eventType);
                _samples.Add(sample);
            }

            Debug.Log(string.Format("Loaded {0} eye track samples", _samples.Count));

            // Write out the samples for MATLAB visualization
            var outCsvData = new CSVDataFile();
            outCsvData.AddAttribute("imagePositionX", typeof(float));
            outCsvData.AddAttribute("imagePositionY", typeof(float));
            outCsvData.AddAttribute("eventType", typeof(int));
            for (int sampleIndex = 0; sampleIndex < _samples.Count; ++sampleIndex)
            {
                var sample = _samples[sampleIndex];
                outCsvData.AddData(sample.imagePosition.x, sample.imagePosition.y, (int)sample.eventType);
            }
            outCsvData.WriteToFile("../Matlab/EyeTracker/samples.csv");
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError(string.Format("Unable to load eye tracking samples from asset file {0}: {1}",
                path, ex.Message));
        }
    }

    // Load eye tracking events for the current animation clip
    private void _LoadEvents()
    {
        // Get eye tracking samples file path
        string path = Application.dataPath + LEAPCore.eyeTrackDataDirectory.Substring(
               LEAPCore.eyeTrackDataDirectory.IndexOfAny(@"/\".ToCharArray()));
        if (path[path.Length - 1] != '/' && path[path.Length - 1] != '\\')
            path += '/';
        path += (BaseAnimationClip.name + "#Events.txt");

        if (!File.Exists(path))
        {
            UnityEngine.Debug.LogError(string.Format("No eye tracking events file at path " + path));
            return;
        }

        Debug.Log("Loading eye tracking events...");

        try
        {
            var reader = new StreamReader(path);

            string line = "";
            double firstStartTimeStamp = -0;
            while (!reader.EndOfStream && (line = reader.ReadLine()) != "")
            {
                var lineElements = line.Split(' ', ',');

                // Get gaze event type
                var eventType = EyeTrackEventType.Unknown;
                if (lineElements[0] == "Fixation")
                    eventType = EyeTrackEventType.Fixation;
                else if (lineElements[0] == "Saccade")
                    eventType = EyeTrackEventType.Saccade;
                else if (lineElements[0] == "Blink")
                    eventType = EyeTrackEventType.Blink;
                else
                    continue;

                // Get gaze event timings
                double startTimeStamp = double.Parse(lineElements[4]);
                if (firstStartTimeStamp <= 0)
                    firstStartTimeStamp = startTimeStamp;
                startTimeStamp -= firstStartTimeStamp;
                int startFrame = LEAPCore.ToEyeTrackFrame((float)(startTimeStamp / 1000000));
                int frameLength = LEAPCore.ToEyeTrackFrame((float)(double.Parse(lineElements[6]) / 1000000));

                // Get gaze location
                var imgStartPos = eventType == EyeTrackEventType.Blink ? Vector2.zero :
                    new Vector2(float.Parse(lineElements[7]), float.Parse(lineElements[8]));
                var imgEndPos = eventType == EyeTrackEventType.Saccade ? new Vector2(
                    float.Parse(lineElements[9]),
                    float.Parse(lineElements[10])) :
                    imgStartPos;

                // Add eye track event
                var evt = new EyeTrackEvent(eventType, startFrame, frameLength, imgStartPos, imgEndPos);
                _events.Add(evt);
            }
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError(string.Format("Unable to load eye tracking events from asset file {0}: {1}", path, ex.Message));
        }
    }

    // Fill gaps between eye tracking events
    private void _FillEventGaps()
    {
        Debug.Log("Filling gaps between eye tracking events...");

        for (int eventIndex = 0; eventIndex < Events.Count - 1; ++eventIndex)
        {
            var curEvt = _events[eventIndex];
            curEvt.frameLength = _events[eventIndex + 1].startFrame - _events[eventIndex].startFrame;
            _events[eventIndex] = curEvt;
        }
    }

    // Remove eye blink events from the event data
    private void _RemoveBlinkEvents()
    {
        Debug.Log("Removing eye blink events...");

        // Merge adjacent blinks
        for (int eventIndex = 0; eventIndex < Events.Count - 1; ++eventIndex)
        {
            var curEvt = _events[eventIndex];
            var nextEvt = _events[eventIndex + 1];

            if (curEvt.eventType == EyeTrackEventType.Blink && nextEvt.eventType == EyeTrackEventType.Blink)
            {
                curEvt.frameLength += nextEvt.frameLength;
                _events.RemoveAt(eventIndex + 1);
                _events[eventIndex] = curEvt;
                --eventIndex;
            }
        }

        // Remove or replace each blink
        for (int eventIndex = 0; eventIndex < Events.Count; ++eventIndex)
        {
            var curEvt = _events[eventIndex];
            if (curEvt.eventType != EyeTrackEventType.Blink)
                continue;

            if (eventIndex == 0)
            {
                var nextEvt = _events[eventIndex + 1];

                if (nextEvt.eventType == EyeTrackEventType.Fixation)
                {
                    // Extend next fixation
                    nextEvt.startFrame = curEvt.startFrame;
                    nextEvt.frameLength += curEvt.frameLength;
                    _events[eventIndex + 1] = nextEvt;
                    
                    // Remove blink
                    _events.RemoveAt(eventIndex);
                    --eventIndex;
                }
                else // if (nextEvt.eventType == EyeTrackEventType.Saccade)
                {
                    // Replace blink with a fixation
                    curEvt.eventType = EyeTrackEventType.Fixation;
                    curEvt.imageStartPosition = curEvt.imageEndPosition = nextEvt.imageStartPosition;
                    _events[eventIndex] = curEvt;
                }
            }
            else if (eventIndex > 0 && eventIndex < Events.Count - 1)
            {
                var prevEvt = _events[eventIndex - 1];
                var nextEvt = _events[eventIndex + 1];

                if (prevEvt.eventType == EyeTrackEventType.Fixation)
                {
                    // Extend previous fixation
                    prevEvt.frameLength += curEvt.frameLength;
                    _events[eventIndex - 1] = prevEvt;

                    // Remove blink
                    _events.RemoveAt(eventIndex);
                    --eventIndex;
                }
                else if (nextEvt.eventType == EyeTrackEventType.Fixation)
                {
                    // Extend next fixation
                    nextEvt.startFrame = curEvt.startFrame;
                    nextEvt.frameLength += curEvt.frameLength;
                    _events[eventIndex + 1] = nextEvt;

                    // Remove blink
                    _events.RemoveAt(eventIndex);
                    --eventIndex;
                }
                else // if (prevEvt.eventType == EyeTrackEventType.Saccade && nextEvt.eventType == EyeTrackEventType.Saccade)
                {
                    // Replace blink with a fixation
                    curEvt.eventType = EyeTrackEventType.Fixation;
                    curEvt.imageStartPosition = curEvt.imageEndPosition = prevEvt.imageEndPosition;
                    _events[eventIndex] = curEvt;
                }
            }
            else // if (eventIndex == Events.Count - 1)
            {
                var prevEvt = _events[eventIndex - 1];

                if (prevEvt.eventType == EyeTrackEventType.Fixation)
                {
                    // Extend previous fixation
                    prevEvt.frameLength += curEvt.frameLength;
                    _events[eventIndex - 1] = prevEvt;

                    // Remove blink
                    _events.RemoveAt(eventIndex);
                    --eventIndex;
                }
                else // if (prevEvt.eventType == EyeTrackEventType.Saccade)
                {
                    // Replace blink with a fixation
                    curEvt.eventType = EyeTrackEventType.Fixation;
                    curEvt.imageStartPosition = curEvt.imageEndPosition = prevEvt.imageEndPosition;
                    _events[eventIndex] = curEvt;
                }
            }
        }
    }

    // Merge saccade-fixation pairs
    private void _MergeSaccadeFixationPairs()
    {
        Debug.Log("Merging saccade-fixation pairs...");

        for (int eventIndex = 0; eventIndex < _events.Count; ++eventIndex)
        {
            var curEvt = _events[eventIndex];
            if (curEvt.eventType != EyeTrackEventType.Saccade)
                continue;

            if (eventIndex < _events.Count - 1)
            {
                var nextEvt = _events[eventIndex + 1];

                if (nextEvt.eventType == EyeTrackEventType.Fixation)
                {
                    // Merge saccade-fixation into a single fixation
                    nextEvt.startFrame = curEvt.startFrame;
                    nextEvt.frameLength += curEvt.frameLength;
                    _events[eventIndex + 1] = nextEvt;
                    _events.RemoveAt(eventIndex);
                    --eventIndex;
                }
                else // if (nextEvt.eventType == EyeTrackEventType.Saccade)
                {
                    // Replace the saccade with a fixation
                    curEvt.eventType = EyeTrackEventType.Fixation;
                    curEvt.imageStartPosition = curEvt.imageEndPosition;
                    _events[eventIndex] = curEvt;
                }
            }
            else
            {
                // Replace the saccade with a fixation
                curEvt.eventType = EyeTrackEventType.Fixation;
                curEvt.imageStartPosition = curEvt.imageEndPosition;
                _events[eventIndex] = curEvt;
            }
        }
    }

    // Print eye tracking events
    private void _PrintEvents()
    {
        for (int eventIndex = 0; eventIndex < Events.Count - 1; ++eventIndex)
        {
            var curEvt = _events[eventIndex];
            Debug.Log(string.Format("eventType = {0}, startFrame: {1}, frame length: {2}, " +
                "imageStartPosition = ({3}, {4}), imageEndPosition = ({5}, {6})",
                curEvt.eventType.ToString(), curEvt.startFrame, curEvt.frameLength,
                curEvt.imageStartPosition.x, curEvt.imageStartPosition.y,
                curEvt.imageEndPosition.x, curEvt.imageEndPosition.y));
        }
    }
}

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Class representing a body part in gaze shifts and fixations.
/// It can be one of the following: left eye, right eye, head, torso.
/// </summary>
[Serializable]
public class GazeBodyPart
{
    /// <summary>
    /// Joint chain that comprises the body part.
    /// </summary>
    public Transform[] gazeJoints = new Transform[0];

    /// <summary>
    /// How much the body part should align with the gaze target.
    /// </summary>
    public float align = 1f;

    /// <summary>
    /// How fast the joint moves (in deg/s).
    /// </summary>
    /// <remarks>Should be 360 for the eyes, 100 for the head, and 12 for the torso.</remarks>
    public float velocity = 360f;

    /// <summary>
    /// Ocular motor range (eyes only).
    /// </summary>
    public float inOMR = 45f, outOMR = 45f, upOMR = 45f, downOMR = 45f;

    /// <summary>
    /// Gaze body part type.
    /// </summary>
    public GazeBodyPartType GazeBodyPartType
    {
        get { return _gazeBodyPartType; }
    }

    /// <summary>
    /// Gaze controller that owns this body part.
    /// </summary>
    public GazeController GazeController
    {
        get { return _gazeController; }
    }

    /// <summary>
    /// true if the body part has gaze joints defined, false otherwise.
    /// </summary>
    public bool Defined
    {
        get { return gazeJoints != null && gazeJoints.Length > 0; }
    }

    /// <summary>
    /// true if this body part is an eye, false otherwise.
    /// </summary>
    public bool IsEye
    {
        get
        {
            return GazeBodyPartType == GazeBodyPartType.LEye ||
                GazeBodyPartType == GazeBodyPartType.REye;
        }
    }

    /// <summary>
    /// Top gaze joint of the body part.
    /// </summary>
    public Transform Top
    {
        get { return gazeJoints[0]; }
    }

    /// <summary>
    /// Pitch angle of the body part's top gaze joint.
    /// </summary>
    public float Pitch
    {
        get { return GetPitch(0); }
        set { SetPitch(0, value); }
    }

    /// <summary>
    /// Yaw angle of the body part's top gaze joint.
    /// </summary>
    public float Yaw
    {
        get { return GetYaw(0); }
        set { SetYaw(0, value); }
    }

    /// <summary>
    /// Roll angle of the body part's top gaze joint.
    /// </summary>
    public float Roll
    {
        get { return GetRoll(0); }
        set { SetRoll(0, value); }
    }

    /// <summary>
    /// Current position of the body part.
    /// </summary>
    public Vector3 Position
    {
        get { return gazeJoints[0].position; }
    }

    /// <summary>
    /// Current facing direction of the body part.
    /// </summary>
    public Vector3 Direction
    {
        get { return GetDirection(0); }
    }

    /// <summary>
    /// Align parameter value for the current gaze shift.
    /// </summary>
    public float _Align
    {
        get { return _curAlign; }
    }

    /// <summary>
    /// Source direction at the start of the current gaze shift.
    /// </summary>
    public Vector3 _SourceDirectionOriginal
    {
        get { return _srcDir0; }
        set { _srcDir0 = value.normalized; }
    }

    /// <summary>
    /// Current gaze shift source direction.
    /// </summary>
    public Vector3 _SourceDirection
    {
        get { return _srcDir; }
        set { _srcDir = value.normalized; }
    }

    /// <summary>
    /// Aligning target direction for the current gaze shift.
    /// </summary>
    public Vector3 _TargetDirectionAlign
    {
        get { return _trgDirAlign; }
        set { _trgDirAlign = value.normalized; }
    }

    // How far the body part must rotate to align with the target.
    public float _Amplitude
    {
        get { return Vector3.Angle(_srcDir, _trgDirAlign); }
    }

    /// <summary>
    /// Fully aligning target direction for the current gaze shift.
    /// </summary>
    public Vector3 _TargetDirection
    {
        get { return _trgDir; }
    }

    /// <summary>
    /// If true, gaze shift is performed along longer rotational arc.
    /// </summary>
    public bool _UseLongArc
    {
        get { return _useLongArc; }
    }

    /// <summary>
    /// Gaze latency of the body part.
    /// </summary>
    public float _Latency
    {
        get { return _latency; }
        set { _latency = value; }
    }

    /// <summary>
    /// Max. gaze shift velocity of the body part.
    /// </summary>
    public float _MaxVelocity
    {
        get { return _maxVelocity; }
        set { _maxVelocity = value; }
    }

    /// <summary>
    /// Previous gaze shift source direction.
    /// </summary>
    public Vector3 _FixSourceDirection
    {
        get { return _fixSrcDir; }
        set { _fixSrcDir = value.normalized; }
    }

    /// <summary>
    /// Source direction at the start of the previous gaze shift.
    /// </summary>
    public Vector3 _FixSourceDirectionOriginal
    {
        get { return _fixSrcDir0; }
        set { _fixSrcDir0 = value.normalized; }
    }

    /// <summary>
    /// Current gaze shift source direction.
    /// </summary>
    public Vector3 _FixTargetDirectionAlign
    {
        get { return _fixTrgDirAlign; }
    }

    /// <summary>
    /// Current gaze shift source direction.
    /// </summary>
    public Vector3 _FixTargetDirection
    {
        get { return _fixTrgDir; }
    }

    /// <summary>
    /// true if the body part is fixating a gaze target, false otherwise.
    /// </summary>
    public bool _IsFix
    {
        get { return _isFix; }
    }

    private GazeBodyPartType _gazeBodyPartType;
    private GazeController _gazeController;
    private bool _usePelvis = false;
    private Transform[] _gazeRightHelpers;
    private Transform[] _gazeForwardHelpers;
    private Quaternion[] _basisRots;

    // Current gaze shift state:
    private float _curAlign = 1f;
    private float _maxVelocity = 0f;
    private float _curVelocity = 0f;
    private float _latency = 0f;
    private float _adjInOMR = 0f, _adjOutOMR = 0f, _adjUpOMR = 0f, _adjDownOMR = 0f;
    private float _curInOMR = 0f, _curOutOMR = 0f, _curUpOMR = 0f, _curDownOMR = 0f;
    private Quaternion[] _baseRots;
    private Vector3 _baseDir;
    private Vector3 _srcDir0 = Vector3.zero;
    private Vector3 _srcDir = Vector3.zero;
    private Vector3 _trgDir = Vector3.zero;
    private Vector3 _trgDirAlign = Vector3.zero;
    private float _rotParam = 0f;
    private bool _useLongArc = false;
    private Vector3 _curDir = Vector3.zero;
    private bool _isFix = false;
    private Vector3 _fixSrcDir0 = Vector3.zero;
    private Vector3 _fixSrcDir = Vector3.zero;
    private Vector3 _fixTrgDir = Vector3.zero;
    private Vector3 _fixTrgDirAlign = Vector3.zero;
    private bool _fixUseLongArc = false;
    private float _weight = 1f; // for logging purposes

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="gazeBodyPartType">Gaze body part type (left eye, right eye, head, or torso)</param>
    /// <param name="gazeController">Gaze controller</param>
    public GazeBodyPart(GazeBodyPartType gazeBodyPartType, GazeController gazeController)
    {
        this._gazeBodyPartType = gazeBodyPartType;
        this._gazeController = gazeController;
    }

    /// <summary>
    /// Get gaze joint pitch angle.
    /// </summary>
    /// <param name="gazeJointIndex">Gaze joint index</param>
    /// <returns>Pitch angle</returns>
    public float GetPitch(int gazeJointIndex)
    {
        var gazeJoint = gazeJoints[gazeJointIndex];
        var rot = gazeJoint.localRotation;
        if (!_HasHelpersForGazeJoint(gazeJointIndex))
            return GeometryUtil.RemapAngle(rot.eulerAngles.x);

        var basisRot = _basisRots[gazeJointIndex];
        rot = rot * basisRot;
        rot = Quaternion.Inverse(basisRot) * rot;

        return GeometryUtil.RemapAngle(rot.eulerAngles.x);
    }

    /// <summary>
    /// Set gaze joint pitch.
    /// </summary>
    /// <param name="gazeJointIndex">Gaze joint index</param>
    /// <param name="pitch">Pitch angle</param>
    public void SetPitch(int gazeJointIndex, float pitch)
    {
        var gazeJoint = gazeJoints[gazeJointIndex];
        if (!_HasHelpersForGazeJoint(gazeJointIndex))
        {
            gazeJoint.localEulerAngles = new Vector3(pitch, gazeJoint.localEulerAngles.y, gazeJoint.localEulerAngles.z);
            return;
        }

        var basisRot = _basisRots[gazeJointIndex];
        var rot = gazeJoint.localRotation * basisRot;
        rot = Quaternion.Inverse(basisRot) * rot;
        rot = Quaternion.Euler(pitch, rot.eulerAngles.y, rot.eulerAngles.z);
        rot = basisRot * rot;
        rot = rot * Quaternion.Inverse(basisRot);
        gazeJoint.localRotation = rot;
    }

    /// <summary>
    /// Get gaze joint yaw angle.
    /// </summary>
    /// <param name="gazeJointIndex">Gaze joint index</param>
    /// <returns>Yaw angle</returns>
    public float GetYaw(int gazeJointIndex)
    {
        var gazeJoint = gazeJoints[gazeJointIndex];
        var rot = gazeJoint.localRotation;
        if (!_HasHelpersForGazeJoint(gazeJointIndex))
            return GeometryUtil.RemapAngle(rot.eulerAngles.y);

        var basisRot = _basisRots[gazeJointIndex];
        rot = rot * basisRot;
        rot = Quaternion.Inverse(basisRot) * rot;

        return GeometryUtil.RemapAngle(rot.eulerAngles.y);
    }

    /// <summary>
    /// Set gaze joint yaw.
    /// </summary>
    /// <param name="gazeJointIndex">Gaze joint index</param>
    /// <param name="yaw">Yaw angle</param>
    public void SetYaw(int gazeJointIndex, float yaw)
    {
        var gazeJoint = gazeJoints[gazeJointIndex];
        if (!_HasHelpersForGazeJoint(gazeJointIndex))
        {
            gazeJoint.localEulerAngles = new Vector3(gazeJoint.localEulerAngles.x, yaw, gazeJoint.localEulerAngles.z);
            return;
        }

        var basisRot = _basisRots[gazeJointIndex];
        var rot = gazeJoint.localRotation * basisRot;
        rot = Quaternion.Inverse(basisRot) * rot;
        rot = Quaternion.Euler(rot.eulerAngles.x, yaw, rot.eulerAngles.z);
        rot = basisRot * rot;
        rot = rot * Quaternion.Inverse(basisRot);
        gazeJoint.localRotation = rot;
    }

    /// <summary>
    /// Get gaze joint roll angle.
    /// </summary>
    /// <param name="gazeJointIndex">Gaze joint index</param>
    /// <returns>Roll angle</returns>
    public float GetRoll(int gazeJointIndex)
    {
        var gazeJoint = gazeJoints[gazeJointIndex];
        var rot = gazeJoint.localRotation;
        if (!_HasHelpersForGazeJoint(gazeJointIndex))
            return GeometryUtil.RemapAngle(rot.eulerAngles.z);

        var basisRot = _basisRots[gazeJointIndex];
        rot = rot * basisRot;
        rot = Quaternion.Inverse(basisRot) * rot;

        return GeometryUtil.RemapAngle(rot.eulerAngles.z);
    }

    /// <summary>
    /// Set gaze joint roll.
    /// </summary>
    /// <param name="gazeJointIndex">Gaze joint index</param>
    /// <param name="roll">Roll angle</param>
    public void SetRoll(int gazeJointIndex, float roll)
    {
        var gazeJoint = gazeJoints[gazeJointIndex];
        if (!_HasHelpersForGazeJoint(gazeJointIndex))
        {
            gazeJoint.localEulerAngles = new Vector3(gazeJoint.localEulerAngles.x, gazeJoint.localEulerAngles.y, roll);
            return;
        }

        var basisRot = _basisRots[gazeJointIndex];
        var rot = gazeJoint.localRotation * basisRot;
        rot = Quaternion.Inverse(basisRot) * rot;
        rot = Quaternion.Euler(rot.eulerAngles.x, rot.eulerAngles.y, roll);
        rot = basisRot * rot;
        rot = rot * Quaternion.Inverse(basisRot);
        gazeJoint.localRotation = rot;
    }

    /// <summary>
    /// Get forward direction of the gaze joint.
    /// </summary>
    /// <param name="gazeJointIndex">Gaze joint index</param>
    /// <returns>Forward direction vector</returns>
    public Vector3 GetDirection(int gazeJointIndex)
    {
        var gazeJoint = gazeJoints[gazeJointIndex];
        var gazeForwardHelper = _HasHelpersForGazeJoint(gazeJointIndex) ?
            _gazeForwardHelpers[gazeJointIndex] : null;
        return gazeForwardHelper != null ? (gazeForwardHelper.position - gazeJoint.position).normalized : gazeJoint.forward;
    }

    /// <summary>
    /// Set forward direction of the gaze joint.
    /// </summary>
    /// <param name="gazeJointIndex">Gaze joint index</param>
    /// <param name="dir">Forward direction vector</param>
    public void SetDirection(int gazeJointIndex, Vector3 direction)
    {
        var gazeJoint = gazeJoints[gazeJointIndex];
        var gazeForwardHelper = _HasHelpersForGazeJoint(gazeJointIndex) ?
            _gazeForwardHelpers[gazeJointIndex] : null;
        var dir0 = gazeForwardHelper != null ? gazeForwardHelper.localPosition.normalized :
            gazeJoint.InverseTransformDirection(gazeJoint.forward).normalized;
        var dir1 = gazeJoint.InverseTransformDirection(direction).normalized;
        var rot = Quaternion.FromToRotation(dir0, dir1);
        gazeJoint.localRotation *= rot;
    }

    /// <summary>
    /// Rotate the body part's gaze joints in the specified direction.
    /// </summary>
    /// <param name="direction">Gaze direction</param>
    public void RotateTowards(Vector3 direction)
    {
        direction.Normalize();
        int nj = gazeJoints.Length;
        if (nj <= 1)
        {
            // This is a single-joint body part
            SetDirection(0, direction);
            if (IsEye || _gazeController.removeRoll) Roll = 0f;
            return;
        }

        // Compute blend weight
        float weightOverride = GazeBodyPartType == GazeBodyPartType.Torso ?
            LEAPCore.gazeTorsoBlendWeightOverride : LEAPCore.gazeHeadBlendWeightOverride;
        _weight = GazeController.weight * (IsEye ? 1f : (weightOverride >= 0f ? weightOverride :
            1f - Mathf.Clamp01(Vector3.Dot(direction, _baseDir))));

        // Gaze directions and joint rotations
        Vector3 trgDir, trgDirAlign, srcDir, baseDir;
        Quaternion trgRot, trgRotAlign, trgRotHAlign;

        // Initialize joint indices and contributions
        int jin = 0;
        int ji1 = jin + nj - 1;
        float cprev = 0f, c, c1;
        int jic;
        bool isTorso = _usePelvis && GazeBodyPartType == GazeBodyPartType.Torso;

        // Apply rotational contribution of each joint in the chain
        for (int ji = ji1; ji >= jin; --ji)
        {
            var curJoint = gazeJoints[ji];

            // Compute joint contribution
            if (isTorso && ji == ji1)
            {
                c1 = _gazeController.pelvisAlign;
            }
            else
            {
                jic = ji - jin + 1;
                c = isTorso ? ((float)((nj - jic) * (nj - jic + 1))) / (nj * (nj - 1)) :
                    ((float)((nj - jic + 1) * (nj - jic + 2))) / (nj * (nj + 1));
                c1 = (c - cprev) / (1f - cprev);
                cprev = c;
            }

            // Get current joint's source and target gaze directions
            curJoint.localRotation = Quaternion.identity;
            srcDir = curJoint.InverseTransformDirection(GetDirection(ji));
            trgDir = curJoint.InverseTransformDirection(direction);

            // Compute current joint's contribution to the overall rotation
            trgRot = Quaternion.FromToRotation(srcDir, trgDir);
            trgRotAlign = Quaternion.Slerp(Quaternion.identity, trgRot, c1);
            if (IsEye || _gazeController.removeRoll)
            {
                curJoint.localRotation = trgRotAlign;
                SetRoll(ji, 0f);
                trgRotAlign = curJoint.localRotation;
            }

            // Get current joint's target gaze direction in the horizontal plane
            curJoint.localRotation = trgRotAlign;
            trgDirAlign = GetDirection(ji);
            trgDirAlign = new Vector3(trgDirAlign.x, 0f, trgDirAlign.z);
            curJoint.localRotation = Quaternion.identity;
            trgDirAlign = curJoint.InverseTransformDirection(trgDirAlign);

            // Get current joint's base gaze direction in horizontal plane
            curJoint.localRotation = _baseRots[ji];
            baseDir = GetDirection(ji);
            baseDir = new Vector3(baseDir.x, 0f, baseDir.z);
            curJoint.localRotation = Quaternion.identity;
            baseDir = curJoint.InverseTransformDirection(baseDir);

            // Align base gaze direction with the target gaze direction in the horizontal plane
            trgRotHAlign = Quaternion.FromToRotation(baseDir, trgDirAlign);
            trgRotHAlign = _baseRots[ji] * trgRotHAlign;

            // Blend rotations
            Quaternion rot = GazeBodyPartType == GazeBodyPartType.Torso ? trgRotHAlign : trgRotAlign;
            rot = Quaternion.Slerp(_baseRots[ji], rot, _weight);
            curJoint.localRotation = rot;
        }
    }

    /// <summary>
    /// Compute gaze direction which would fully align the body part with
    /// the specified target location.
    /// </summary>
    /// <param name="targetPosition">Gaze target position</param>
    /// <returns>Gaze target direction</returns>
    public Vector3 GetTargetDirection(Vector3 targetPosition)
    {
        // Store current rotation of the top joint
        Quaternion curRot = Top.localRotation;

        Quaternion trgRot = ModelUtil.LookAtRotation(Top, targetPosition);
        if (!IsEye)
        {
            // Compute target rotation adjusted for offset betwen the eyes and current body part
            Top.localRotation = trgRot;
            Vector3 llr = Top.InverseTransformDirection((_gazeController.lEye.Position -
                _gazeController.rEye.Position).normalized);
            Vector3 lvt1 = Top.InverseTransformDirection(Direction);
            Vector3 lup = Vector3.Cross(llr, lvt1);
            Plane hpl = new Plane(lup, Top.localPosition);
            float h = hpl.GetDistanceToPoint(Top.InverseTransformPoint(_gazeController.EyeCenter));
            Vector3 ldir1 = (Top.InverseTransformPoint(targetPosition) - (Top.localPosition + h * lup)).normalized;
            Quaternion ldq = Quaternion.FromToRotation(lvt1, ldir1);
            trgRot *= ldq; // final joint rotation
        }
        Top.localRotation = trgRot;
        Vector3 trgDir = Direction;

        // Restore original rotation of the top joint
        Top.localRotation = curRot;

        return trgDir.normalized;
    }

    /// <summary>
    /// Compute gaze direction which would align the body part with the specified target location,
    /// while constraining its movement by OMR.
    /// </summary>
    /// <param name="targetPosition">Gaze target position</param>
    /// <param name="fix">If true, OMR-constrained target direction will be computed relative to fixation source direction</param>
    /// <returns>Gaze target direction</returns>
    public Vector3 GetOMRTargetDirection(Vector3 targetPosition, bool fix = false)
    {
        // Get fully aligning gaze direction
        Vector3 trgDir = GetTargetDirection(targetPosition);
        if (!IsEye)
            return trgDir;

        // Get OMR-constrained gaze direction
        Quaternion curRot = Top.localRotation;
        RotateTowards(trgDir);
        ClampOMR();
        trgDir = Direction;
        Top.localRotation = curRot;

        return trgDir.normalized;
    }

    /// <summary>
    /// true if the current eye orientation violates OMR limits, otherwise false.
    /// </summary>
    public bool CheckOMR()
    {
        float yaw = _gazeBodyPartType == GazeBodyPartType.REye ? Yaw : -Yaw;
        float pitch = Pitch;
        float y2 = yaw * yaw;
        float p2 = pitch * pitch;

        float res = 0f;
        if (yaw >= 0f && pitch >= 0f)
            res = y2 / (_curOutOMR * _curOutOMR) + p2 / (_curDownOMR * _curDownOMR);
        else if (yaw <= 0f && pitch >= 0f)
            res = y2 / (_curInOMR * _curInOMR) + p2 / (_curDownOMR * _curDownOMR);
        else if (yaw <= 0f && pitch <= 0f)
            res = y2 / (_curInOMR * _curInOMR) + p2 / (_curUpOMR * _curUpOMR);
        else if (yaw >= 0f && pitch <= 0f)
            res = y2 / (_curOutOMR * _curOutOMR) + p2 / (_curUpOMR * _curUpOMR);

        return res >= 1f;
    }

    /// <summary>
    /// Clamp current eye orientation to OMR limits.
    /// </summary>
    public void ClampOMR()
    {
        if (!CheckOMR())
            return;

        // TODO: use a smarter/more efficient method to compute the clamped orientation,
        // e.g., find closest point on OMR ellipse to line between source and target orientations (in yaw-pitch space)
        var trgRot = Top.localRotation;
        for (float t = 0f; t <= 1f; )
        {
            // Update joint rotation
            Quaternion prevRot = Top.localRotation;
            Top.localRotation = Quaternion.Slerp(Quaternion.identity, trgRot, t);

            // Has the joint violated OMR limits?
            if (CheckOMR())
            {
                // Yes, previous rotation is as far as we can go
                Top.localRotation = prevRot;
                return;
            }

            // Advance joint rotation
            t += 0.01f;
        }
    }

    // Initialize gaze body part
    public void _Init()
    {
        // Get helpers for all the gaze joints
        _gazeRightHelpers = new Transform[gazeJoints.Length];
        _gazeForwardHelpers = new Transform[gazeJoints.Length];
        for (int gazeJointIndex = 0; gazeJointIndex < gazeJoints.Length; ++gazeJointIndex)
        {
            var gazeJoint = gazeJoints[gazeJointIndex];
            _gazeRightHelpers[gazeJointIndex] = null;
            _gazeForwardHelpers[gazeJointIndex] = null;
            for (int childIndex = 0; childIndex < gazeJoint.childCount; ++childIndex)
            {
                var child = gazeJoint.GetChild(childIndex);
                if (child.tag == "GazeRightHelper")
                    _gazeRightHelpers[gazeJointIndex] = child;
                else if (child.tag == "GazeForwardHelper")
                    _gazeForwardHelpers[gazeJointIndex] = child;
            }
        }

        // Initialize gaze joint standard basis rotations
        _InitBasisRotations();

        _baseRots = new Quaternion[gazeJoints.Length];
        _usePelvis = GazeBodyPartType == global::GazeBodyPartType.Torso &&
            gazeJoints.Length > 0 &&
            gazeJoints[gazeJoints.Length - 1].tag == "RootBone";
    }

    // Initialize base rotations of the gaze joints (before gaze is applied)
    public void _InitBaseRotations()
    {
        _baseDir = Direction;
        for (int gji = 0; gji < gazeJoints.Length; ++gji)
            _baseRots[gji] = gazeJoints[gji].localRotation;
    }

    // Apply base rotations to the gaze joints (from before gaze was applied)
    public void _ApplyBaseRotations()
    {
        for (int gji = 0; gji < gazeJoints.Length; ++gji)
            gazeJoints[gji].localRotation = _baseRots[gji];
    }

    // Initialize gaze fixation for this body part
    public void _InitFix()
    {
        _isFix = true;
        if (_gazeController.FixGazeTarget != null)
        {
            _fixSrcDir0 = _srcDir0;
            _fixSrcDir = _srcDir;
            _fixTrgDir = _trgDir;
            _fixTrgDirAlign = _curDir;
        }
        else
        {
            _fixSrcDir0 = _fixSrcDir = _fixTrgDir = _fixTrgDirAlign = Direction;
        }
        _fixUseLongArc = _useLongArc;
    }

    // Apply gaze fixation for this body part
    public void _ApplyFix()
    {
        // Apply the new body part posture
        RotateTowards(_fixTrgDirAlign);
        if (GazeBodyPartType == GazeBodyPartType.Torso)
            _SolveBodyIK();
        else if (IsEye)
            // Fixation must not violate OMR
            ClampOMR();
    }

    // Stop gaze  fixation for this body part
    public void _StopFix()
    {
        _isFix = false;
    }

    // Initialize gaze shift parameters
    public void _InitGazeShift()
    {
        align = Mathf.Clamp01(align);
        _curAlign = align;
        _maxVelocity = 0f;
        _curVelocity = 0f;
        _latency = 0f;
        _adjInOMR = inOMR;
        _adjOutOMR = outOMR;
        _adjUpOMR = upOMR;
        _adjDownOMR = downOMR;
        _curInOMR = _curInOMR <= 0f ? _adjInOMR : _curInOMR;
        _curOutOMR = _curOutOMR <= 0f ? _adjOutOMR : _curOutOMR;
        _curUpOMR = _curUpOMR <= 0f ? _adjUpOMR : _curUpOMR;
        _curDownOMR = _curDownOMR <= 0f ? _adjDownOMR : _curDownOMR;
        _srcDir0 = _fixTrgDirAlign; // source direction is initialized from current direction at gaze fixation end
        _srcDir = _srcDir0;
        _trgDir = _srcDir0;
        _trgDirAlign = _srcDir0;
        _rotParam = 0f;
        _useLongArc = false;
        _curDir = _srcDir0;
    }

    // Initialize fully aligning target direction for the current gaze target
    public void _InitTargetDirection()
    {
        _trgDir = GetTargetDirection(_gazeController.CurrentGazeTargetPosition);
    }

    // Should gaze shift be performed along the longer rotational arc?
    public void _InitLongArcGazeShift()
    {
        // Get source, target, and root directions
        Vector3 srcDir = GeometryUtil.ProjectVectorOntoPlane(_srcDir, Vector3.up).normalized;
        Vector3 trgDir = GeometryUtil.ProjectVectorOntoPlane(_trgDir, Vector3.up).normalized;
        Vector3 rootDir = GeometryUtil.ProjectVectorOntoPlane(GazeController.Root.forward, Vector3.up);

        if (Mathf.Abs((srcDir - trgDir).magnitude) <= 0.0001f)
        {
            // No gaze shift
            _useLongArc = false;
            return;
        }

        // Does -rootDir lie along the shortest arc?
        float rootAngle = Vector3.Angle(srcDir, -rootDir);
        float trgAngle = Vector3.Angle(srcDir, trgDir);
        float t = rootAngle / trgAngle;
        Quaternion rootRot = Quaternion.Slerp(Quaternion.identity, Quaternion.FromToRotation(srcDir, trgDir), t);
        Vector3 dir = rootRot * srcDir;
        _useLongArc = Vector3.Angle(dir, -rootDir) <= 1f;
    }

    // Initialize OMR-constrained target direction for the current gaze target
    public void _InitOMRTargetDirection()
    {
        _trgDirAlign = GetOMRTargetDirection(_gazeController.CurrentGazeTargetPosition);
        _trgDirAlign = _srcDir != _trgDir ?
            GeometryUtil.ProjectVectorOntoPlane(_trgDirAlign, Vector3.Cross(_srcDir, _trgDir)).normalized
            : _trgDir;
    }

    // Initialize motor range of the eye
    public void _InitOMR()
    {
        if (!IsEye)
            return;

        // Compute mean initial eye position (IEP)
        float lEyePitch, lEyeYaw, rEyePitch, rEyeYaw;
        _gazeController.lEye._GetIEP(out lEyePitch, out lEyeYaw);
        _gazeController.rEye._GetIEP(out rEyePitch, out rEyeYaw);
        float pitch = (lEyePitch + rEyePitch) / 2f;
        float yaw = (lEyeYaw + rEyeYaw) / 2f;

        // Adjust OMR by IEP
        float pitchAdj = 1f / 360f * pitch + 0.75f;
        float yawAdj = 1f / 360f * yaw + 0.75f;
        _adjInOMR = inOMR * yawAdj;
        _adjOutOMR = outOMR * yawAdj;
        _adjUpOMR = upOMR * pitchAdj;
        _adjDownOMR = downOMR * pitchAdj;
    }

    // Update gaze direction that will align the body part with the target
    public void _UpdateTargetDirection()
    {
        float prevDistRotAlign = Vector3.Angle(_srcDir, _trgDirAlign);
        float prevDistRot = Vector3.Angle(_srcDir, _trgDir);
        _trgDir = GetTargetDirection(_gazeController.CurrentGazeTargetPosition);

        if (!IsEye)
        {
            float prevAlign = prevDistRot > 0.0001f ? prevDistRotAlign / prevDistRot : 1f;
            Quaternion rotAlign = Quaternion.FromToRotation(_srcDir, _trgDir);
            rotAlign = Quaternion.Slerp(Quaternion.identity, _useLongArc ? Quaternion.Inverse(rotAlign) : rotAlign, prevAlign);
            _trgDirAlign = rotAlign * _srcDir;
        }
        else
        {
            _trgDirAlign = GetOMRTargetDirection(_gazeController.CurrentGazeTargetPosition);
            _trgDirAlign = _srcDir != _trgDir ?
                GeometryUtil.ProjectVectorOntoPlane(_trgDirAlign, Vector3.Cross(_srcDir, _trgDir)).normalized
                : _trgDir;
        }

        // Renormalize gaze shift progress
        float distRotAlign = Vector3.Angle(_srcDir, _trgDirAlign);
        _rotParam = Mathf.Max(Mathf.Clamp01(_rotParam * (distRotAlign > 0.00001f ? prevDistRotAlign / distRotAlign : 1f)),
            _rotParam);
    }

    // Update gaze direction that will align the body part with the target during fixation
    public void _UpdateFixTargetDirection()
    {
        if (_gazeController.FixGazeTarget == null)
            return;

        float prevDistRotAlign = Vector3.Angle(_fixSrcDir, _fixTrgDirAlign);
        float prevDistRot = Vector3.Angle(_fixSrcDir, _fixTrgDir);
        _fixTrgDir = GetTargetDirection(_gazeController.FixGazeTargetPosition);

        if (!IsEye)
        {
            float prevAlign = prevDistRot > 0.0001f ? prevDistRotAlign / prevDistRot : 1f;
            Quaternion rotAlign = Quaternion.FromToRotation(_fixSrcDir, _fixTrgDir);
            rotAlign = Quaternion.Slerp(Quaternion.identity, _fixUseLongArc ? Quaternion.Inverse(rotAlign) : rotAlign, prevAlign);
            _fixTrgDirAlign = rotAlign * _fixSrcDir;
        }
        else
        {
            _fixTrgDirAlign = GetOMRTargetDirection(_gazeController.FixGazeTargetPosition, true);
            _fixTrgDirAlign = _fixSrcDir != _fixTrgDir ?
                GeometryUtil.ProjectVectorOntoPlane(_fixTrgDirAlign, Vector3.Cross(_fixSrcDir, _fixTrgDir)).normalized
                : _fixTrgDir;
        }
    }

    // Update source direction at gaze shift start to account for movement during latency phase
    public void _UpdateSourceDirectionOnLatency()
    {
        if (_latency <= 0f)
            return;

        Quaternion rootRot1 = _gazeController.Root.rotation;
        Quaternion dq = Quaternion.Inverse(rootRot1) * _gazeController._RootRotation;
        dq.eulerAngles = new Vector3(0f, dq.eulerAngles.y, 0f);
        _srcDir = _fixTrgDirAlign;
        _srcDir0 = (dq * _srcDir).normalized;
    }

    // Rotate the body part's gaze joints toward the current target
    public bool _AdvanceGazeShift(float deltaTime)
    {
        if (!Defined)
            return true;
        
        float dt = 0f;
        for (float t = 0; t < deltaTime; )
        {
            // Compute delta time
            t += LEAPCore.eulerTimeStep;
            dt = (t <= deltaTime) ? LEAPCore.eulerTimeStep :
                deltaTime - t + LEAPCore.eulerTimeStep;

            if (_latency > 0f)
            {
                // Still in latency phase
                _latency -= dt;
                continue;
            }
            else
            {
                // Latency phase finished, stop fixating previous target
                _StopFix();
            }

            // Update body part velocity and (if eye) OMR
            _UpdateVelocity();
            if (IsEye)
                _UpdateOMR();

            // Rotate the body part toward the target
            float distRotDiff = dt * _curVelocity;
            float distRotAlign = Vector3.Angle(_SourceDirection, _TargetDirectionAlign);
            _rotParam = _rotParam < 1f ? Mathf.Clamp01(_rotParam + distRotDiff / distRotAlign) : 1f;
            Quaternion rot = Quaternion.Slerp(Quaternion.identity, Quaternion.FromToRotation(_srcDir, _trgDirAlign), _rotParam);
            _curDir = rot * _srcDir;

            if (_rotParam >= 1f)
                // Gaze shift finished
                return true;
        }

        return false;
    }

    // Apply current gaze shift posture
    public void _ApplyGazeShift()
    {
        if (_isFix)
        {
            // Body part not ready to move yet, just maintain fixation
            // TODO: currently not updating fixation source directions during latency phase
            _ApplyFix();
            return;
        }

        // Apply new body part posture
        RotateTowards(_curDir);
        if (GazeBodyPartType == GazeBodyPartType.Torso)
            _SolveBodyIK();
        else if (IsEye)
            // If OMR has been reached, clamp the rotation
            ClampOMR(); // TODO: keep an eye out for discontinuities in orientation (LOL)
    }

    // Get current OMR
    public void _GetOMR(out float inOMR, out float outOMR, out float upOMR, out float downOMR)
    {
        inOMR = _curInOMR;
        outOMR = _curOutOMR;
        upOMR = _curUpOMR;
        downOMR = _curDownOMR;
    }

    // Compute the difference between OMR-constrained eye target rotation and the target rotation
    // needed to align the eye with the target
    public Quaternion _GetOMRTargetRotationDiff()
    {
        Quaternion trgRot = ModelUtil.LookAtRotation(Top, _gazeController.CurrentGazeTargetPosition);
        Quaternion curRot = Top.localRotation;
        Top.localRotation = trgRot;
        ClampOMR();
        Quaternion trgRotOMR = Top.localRotation;
        Top.localRotation = curRot;
        
        return Quaternion.Inverse(trgRotOMR) * trgRot;
    }

    // Compute IEP of the specified eye (pitch and yaw given a contralateral target)
    private void _GetIEP(out float pitch, out float yaw)
    {
        // Get source and target eye orientations
        Quaternion srcRot = Top.localRotation;
        Top.localRotation = ModelUtil.LookAtRotation(Top, _gazeController.MovingGazeTargetPosition);
        float trgPitch = Pitch;
        float trgYaw = Yaw;
        Top.localRotation = srcRot;

        // Compute IEP
        pitch = ((Pitch > 0f && trgPitch < 0f) || (Pitch < 0f && trgPitch > 0f)) ?
            Mathf.Abs(Pitch) : 0f;
        yaw = ((Yaw > 0f && trgYaw < 0f) || (Yaw < 0f && trgYaw > 0f)) ?
            Mathf.Abs(Yaw) : 0f;
    }

    // Update body part velocity based on gaze shift progress
    private void _UpdateVelocity()
    {
        if (IsEye)
        {
            // Compute gaze shift progress based on the eye that has farthest to move
            var lEye = _gazeController.lEye;
            var rEye = _gazeController.rEye;
            float lEyeDistRot = Vector3.Angle(lEye._SourceDirection, lEye._TargetDirectionAlign);
            float rEyeDistRot = Vector3.Angle(rEye._SourceDirection, rEye._TargetDirectionAlign);
            float p = Mathf.Clamp01(lEyeDistRot > rEyeDistRot ? lEye._rotParam : rEye._rotParam);
            float p2 = p * p;

            // Update eye velocity
            _curVelocity = p < 0.5f ?
                _curVelocity = (p + 0.5f) * _maxVelocity :
                (8f * p2 * p - 18f * p2 + 12f * p - 1.5f) * _maxVelocity;
        }
        else
        {
            // Get gaze shift progress
            float p = _rotParam;
            float p2 = p * p;

            // Update body part velocity
            _curVelocity = p < 0.5f ?
                (p * 0.75f / 0.5f + 0.25f) * _maxVelocity :
                (12f * p2 * p - 27f * p2 + 18f * p - 2.75f) * _maxVelocity;
        }
    }

    // Update eye motor range based on gaze shift progress
    private void _UpdateOMR()
    {
        if (!IsEye)
            return;

        // Compute OMR for the new frame
        float headVelocity = _gazeController.head._curVelocity;
        float newUpOMR = _adjUpOMR * (-1f / 600f * headVelocity + 1f);
        float newDownOMR = _adjDownOMR * (-1f / 600f * headVelocity + 1f);
        float newInOMR = _adjInOMR * (-1f / 600f * headVelocity + 1f);
        float newOutOMR = _adjOutOMR * (-1f / 600f * headVelocity + 1f);

        // Compute actual OMR as a weighted combination of previous and new
        float coeff = 0.85f / 12f; // weight (< 1) / OMR difference
        _curUpOMR = coeff * Mathf.Abs(_curUpOMR - newUpOMR) * (_curUpOMR - newUpOMR) + newUpOMR;
        _curDownOMR = coeff * Mathf.Abs(_curDownOMR - newDownOMR) * (_curDownOMR - newDownOMR) + newDownOMR;
        _curInOMR = coeff * Mathf.Abs(_curInOMR - newInOMR) * (_curInOMR - newInOMR) + newInOMR;
        _curOutOMR = coeff * Mathf.Abs(_curOutOMR - newOutOMR) * (_curOutOMR - newOutOMR) + newOutOMR;
        // TODO: this is a stupid hack to smooth out OMR discontinuities
    }

    // Solve for body posture using an IK solver
    private void _SolveBodyIK()
    {
        var bodySolver = _gazeController.gameObject.GetComponent<BodyIKSolver>();
        if (bodySolver != null && bodySolver.enabled && LEAPCore.useGazeIK)
            bodySolver.Solve();
    }

    // true if right and forward helpers are defined for the specified gaze joint, false otherwise
    private bool _HasHelpersForGazeJoint(int gazeJointIndex)
    {
        return _gazeRightHelpers != null && _gazeRightHelpers.Length == gazeJoints.Length &&
            _gazeRightHelpers[gazeJointIndex] != null &&
            _gazeForwardHelpers != null && _gazeForwardHelpers.Length == gazeJoints.Length &&
            _gazeForwardHelpers[gazeJointIndex] != null;
    }

    // Initialize standard basis rotations for each gaze joint
    private void _InitBasisRotations()
    {
        _basisRots = new Quaternion[gazeJoints.Length];
        for (int gazeJointIndex = 0; gazeJointIndex < gazeJoints.Length; ++gazeJointIndex)
        {
            if (!_HasHelpersForGazeJoint(gazeJointIndex))
            {
                _basisRots[gazeJointIndex] = Quaternion.identity;
                continue;
            }

            var gazeJoint = gazeJoints[gazeJointIndex];
            var gazeRightHelper = _gazeRightHelpers[gazeJointIndex];
            var gazeForwardHelper = _gazeForwardHelpers[gazeJointIndex];

            Vector3 vx = gazeRightHelper.localPosition.normalized;
            Vector3 vz = gazeForwardHelper.localPosition.normalized;
            Vector3 vy = Vector3.Cross(vz, vx);
            var matRot = new Matrix3x3();
            matRot.SetRow(0, vx);
            matRot.SetRow(1, vy);
            matRot.SetRow(2, vz);
            Quaternion basisRot = Quaternion.Inverse(matRot.ToRotation());

            _basisRots[gazeJointIndex] = basisRot;
        }
    }

    // Get a snapshot of the current state of the gaze body part
    public GazeControllerState.GazeBodyPartState _GetRuntimeState()
    {
        GazeControllerState.GazeBodyPartState state = new GazeControllerState.GazeBodyPartState();
        state.gazeBodyPartType = (int)GazeBodyPartType;
        state.align = align;
        state.velocity = velocity;
        state.inOMR = inOMR;
        state.outOMR = outOMR;
        state.upOMR = upOMR;
        state.downOMR = downOMR;
        state.curAlign = _curAlign;
        state.maxVelocity = _maxVelocity;
        state.curVelocity = _curVelocity;
        state.latency = _latency;
        state.adjInOMR = _adjInOMR;
        state.adjOutOMR = _adjOutOMR;
        state.adjUpOMR = _adjUpOMR;
        state.adjDownOMR = _adjDownOMR;
        state.curInOMR = _curInOMR;
        state.curOutOMR = _curOutOMR;
        state.curUpOMR = _curUpOMR;
        state.curDownOMR = _curDownOMR;
        state.baseDir = _baseDir;
        state.baseRots = (Quaternion[])_baseRots.Clone();
        state.srcDir0 = _srcDir0;
        state.srcDir = _srcDir;
        state.trgDir = _trgDir;
        state.trgDirAlign = _trgDirAlign;
        state.rotParam = _rotParam;
        state.useLongArc = _useLongArc;
        state.curDir = _curDir;
        state.isFix = _isFix;
        state.fixSrcDir0 = _fixSrcDir0;
        state.fixSrcDir = _fixSrcDir;
        state.fixTrgDir = _fixTrgDir;
        state.fixTrgDirAlign = _fixTrgDirAlign;
        state.fixUseLongArc = _fixUseLongArc;
        state.weight = _weight;

        return state;
    }

    // Set the current state of the gaze body part from the snapshot
    public void _SetRuntimeState(GazeControllerState.GazeBodyPartState state)
    {
        _gazeBodyPartType = (GazeBodyPartType)state.gazeBodyPartType;
        align = state.align;
        velocity = state.velocity;
        inOMR = state.inOMR;
        outOMR = state.outOMR;
        upOMR = state.upOMR;
        downOMR = state.downOMR;
        _curAlign = state.curAlign;
        _maxVelocity = state.maxVelocity;
        _curVelocity = state.curVelocity;
        _latency = state.latency;
        _adjInOMR = state.adjInOMR;
        _adjOutOMR = state.adjOutOMR;
        _adjUpOMR = state.adjUpOMR;
        _adjDownOMR = state.adjDownOMR;
        _curInOMR = state.curInOMR;
        _curOutOMR = state.curOutOMR;
        _curUpOMR = state.curUpOMR;
        _curDownOMR = state.curDownOMR;
        _baseDir = state.baseDir;
        _baseRots = (Quaternion[])state.baseRots.Clone();
        _srcDir0 = state.srcDir0;
        _srcDir = state.srcDir;
        _trgDir = state.trgDir;
        _trgDirAlign = state.trgDirAlign;
        _rotParam = state.rotParam;
        _useLongArc = state.useLongArc;
        _curDir = state.curDir;
        _isFix = state.isFix;
        _fixSrcDir0 = state.fixSrcDir0;
        _fixSrcDir = state.fixSrcDir;
        _fixTrgDir = state.fixTrgDir;
        _fixTrgDirAlign = state.fixTrgDirAlign;
        _fixUseLongArc = state.fixUseLongArc;
        _weight = state.weight;
    }

    // Get zero gaze body part state (before any gaze shifts have been performed)
    public GazeControllerState.GazeBodyPartState _GetZeroRuntimeState()
    {
        GazeControllerState.GazeBodyPartState state = new GazeControllerState.GazeBodyPartState();
        state.gazeBodyPartType = (int)GazeBodyPartType;
        state.align = 1f;
        state.velocity = velocity;
        state.inOMR = inOMR;
        state.outOMR = outOMR;
        state.upOMR = upOMR;
        state.downOMR = downOMR;
        state.curAlign = 1f;
        state.maxVelocity = 0f;
        state.curVelocity = 0f;
        state.latency = 0f;
        state.adjInOMR = inOMR;
        state.adjOutOMR = outOMR;
        state.adjUpOMR = upOMR;
        state.adjDownOMR = downOMR;
        state.curInOMR = inOMR;
        state.curOutOMR = outOMR;
        state.curUpOMR = upOMR;
        state.curDownOMR = downOMR;
        state.baseDir = Vector3.zero;
        state.baseRots = new Quaternion[gazeJoints.Length];
        state.srcDir0 = Vector3.zero;
        state.srcDir = Vector3.zero;
        state.trgDir = Vector3.zero;
        state.trgDirAlign = Vector3.zero;
        state.rotParam = 0f;
        state.useLongArc = false;
        state.curDir = Vector3.zero;
        state.isFix = false;
        state.fixSrcDir0 = Vector3.zero;
        state.fixSrcDir = Vector3.zero;
        state.fixTrgDir = Vector3.zero;
        state.fixTrgDirAlign = Vector3.zero;
        state.fixUseLongArc = false;
        state.weight = 1f;

        return state;
    }
}

using UnityEngine;
using System.Collections;
using MathNet.Numerics;
using MathNet.Numerics.Distributions;
using MathNet.Numerics.RandomSources;

public enum EyesAliveState
{
    NoGaze,
    GazeAway
}

/// <summary>
/// Animation controller for random eye movement. 
/// </summary>
public class EyesAliveController : AnimController
{
    public float maxEyeOffset = 5f;
    public float eyeOffsetMultiplier = 1f;
    public float gazeRateMultiplier = 1f;

    protected bool cancelGazeShift = false;
    protected float gazeTime = 0;
    protected float gazeLength = 0;
    protected UnityEngine.Quaternion[] srcEyeRot;
    protected UnityEngine.Quaternion[] trgEyeRot;

    protected GazeController gazeCtrl = null;
    protected BlinkController blinkCtrl = null;
    protected GazeAversionController gazeAversionCtrl = null;
    protected bool holdGaze;

    protected MersenneTwisterRandomSource randNumGen = null; //random number generator for seeding the distributions below
    protected NormalDistribution normDist1 = null; //Length (seconds) of gaze shift
    protected NormalDistribution normDist2 = null; //Time until next gaze shift
    protected ContinuousUniformDistribution uniDist1 = null; //Amplitude of gaze shift
    protected ContinuousUniformDistribution uniDist2 = null; //Direction of gaze shift

    protected override void _Init()
    {
        // Get relevant animation controllers
        blinkCtrl = GetComponent<BlinkController>();
        gazeAversionCtrl = GetComponent<GazeAversionController>();
        gazeCtrl = Parent as GazeController;
        //gazeCtrl.StateChange += new StateChangeEvtH(GazeController_StateChange);

        // Set up random number generators
        randNumGen = new MersenneTwisterRandomSource();
        normDist1 = new NormalDistribution(randNumGen);
        normDist1.SetDistributionParameters(0.8f, 0.3f);
        normDist2 = new NormalDistribution(randNumGen);
        normDist2.SetDistributionParameters(5f, 0.5f);
        uniDist1 = new ContinuousUniformDistribution(randNumGen);
        uniDist1.SetDistributionParameters(1f, 15f);
        uniDist2 = new ContinuousUniformDistribution(randNumGen);
        uniDist2.SetDistributionParameters(0f, 100f);

        // Create arrays for eye rotations
        srcEyeRot = new UnityEngine.Quaternion[gazeCtrl.eyes.Length];
        trgEyeRot = new UnityEngine.Quaternion[gazeCtrl.eyes.Length];

        gazeLength = (float)normDist1.NextDouble();
        if (gazeLength > 2f)
            gazeLength = 2f;
        if (gazeLength < 0.2f)
            gazeLength = 0.2f;
        gazeTime = 0f;
    }

    public void CancelEyesAlive()
    {
        cancelGazeShift = true;
    }

    protected virtual void LateUpdate_NoGaze()
    {
        gazeTime += DeltaTime;

        if (gazeTime > gazeLength) //Time to trigger a gaze shift
        {
            if (gazeAversionCtrl != null)
            {
                //Check if we have a gaze aversion controller operating
                //Gaze aversion controller and the base gaze controller have precedence in initiating gaze shifts
                if (gazeCtrl.StateId == (int)GazeState.NoGaze && ((gazeAversionCtrl.StateId == (int)GazeAversionState.MutualGaze && gazeAversionCtrl.condition != GazeAversionCondition.BadModel) ||
                                                                  (gazeAversionCtrl.StateId == (int)GazeAversionState.GazeAway && gazeAversionCtrl.condition == GazeAversionCondition.BadModel)))
                {
                    // Initate the gaze shift
                    GoToState((int)EyesAliveState.GazeAway);
                }
                else
                {
                    gazeTime = 0f;
                }
            }
            else
            {
                if (gazeCtrl.StateId == (int)GazeState.NoGaze)
                {
                    // Time to initate the gaze shift
                    GoToState((int)EyesAliveState.GazeAway);
                }
                else
                {
                    gazeTime = 0f;
                }
            }
        }
    }

    protected virtual void LateUpdate_GazeAway()
    {
        gazeTime += DeltaTime;

        if (gazeAversionCtrl != null && ((gazeAversionCtrl.StateId == (int)GazeAversionState.GazeAway && gazeAversionCtrl.condition != GazeAversionCondition.BadModel) ||
                                         (gazeAversionCtrl.StateId == (int)GazeAversionState.MutualGaze && gazeAversionCtrl.condition == GazeAversionCondition.BadModel)))
        {
            cancelGazeShift = true;
        }

        if (gazeTime > gazeLength || cancelGazeShift)
        {
            // Time to initate the gaze shift back	
            GoToState((int)EyesAliveState.NoGaze);
        }
    }

    protected virtual void Transition_NoGazeGazeAway()
    {
        // Compute gaze shift amplitude
        float P = (float)uniDist1.NextDouble();
        float A = -6.9f * Mathf.Log(P / 15.7f);
        if (eyeOffsetMultiplier >= 0f)
            A *= eyeOffsetMultiplier;
        // TODO: allow asymmetric OMR for eye
        if (A > maxEyeOffset)
            A = maxEyeOffset;

        // Compute gaze direction
        float U = (float)uniDist2.NextDouble();
        float pitch = 0f;
        float yaw = 0f;
        if (U < 15.54f)
        {
            yaw = A;
        }
        else if (U < 22f)
        {
            pitch = A / Mathf.Sqrt(2f);
            yaw = A / Mathf.Sqrt(2f);
        }
        else if (U < 39.69f)
        {
            pitch = A;
        }
        else if (U < 47.13f)
        {
            pitch = A / Mathf.Sqrt(2f);
            yaw = -A / Mathf.Sqrt(2f);
        }
        else if (U < 63.93f)
        {
            yaw = -A;
        }
        else if (U < 71.82f)
        {
            pitch = -A / Mathf.Sqrt(2f);
            yaw = -A / Mathf.Sqrt(2f);
        }
        else if (U < 92.2f)
        {
            pitch = -A;
        }
        else
        {
            pitch = -A / Mathf.Sqrt(2f);
            yaw = A / Mathf.Sqrt(2f);
        }

        // Compute source and target rotations
        for (int ei = 0; ei < gazeCtrl.eyes.Length; ++ei)
        {
            GazeJoint eye = gazeCtrl.eyes[ei];

            // Current rotation is source rotation
            srcEyeRot[ei] = eye.bone.localRotation;

            // Compute target rotation
            eye.Pitch += pitch;
            eye.Yaw += yaw;
            if (eye.CheckMR())
                eye.ClampMR();
            trgEyeRot[ei] = eye.bone.localRotation;
            eye.bone.localRotation = srcEyeRot[ei];
        }

        // Compute time when gaze shift back to initial position should begin
        gazeLength = (float)normDist1.NextDouble();
        if (gazeLength > 2f)
            gazeLength = 2f;
        if (gazeLength < 0.2f)
            gazeLength = 0.2f;
        gazeTime = 0f;

        //Compute the point in space where the eyes should be shifting to, given the target rotations.
        bool parallelEyes = false;
        Vector3 targetPos = new Vector3();
        if (gazeCtrl.eyes.Length > 1)
        { //We have (at least) two eyes
            Vector3 p1 = new Vector3();
            Vector3 p2 = new Vector3();
            GazeJoint eye1 = gazeCtrl.eyes[0];
            GazeJoint eye2 = gazeCtrl.eyes[1];
            UnityEngine.Quaternion savedRot1 = eye1.bone.localRotation;
            UnityEngine.Quaternion savedRot2 = eye2.bone.localRotation;
            eye1.bone.localRotation = trgEyeRot[0];
            eye2.bone.localRotation = trgEyeRot[1];
            parallelEyes = GeomUtil.ClosestPointsOn2Lines(
                eye1.bone.position, (eye1.helper.position - eye1.bone.position),
                eye2.bone.position, (eye2.helper.position - eye2.bone.position), out p1, out p2);
            targetPos = 0.5f * (p1 + p2);
            eye1.bone.localRotation = savedRot1;
            eye2.bone.localRotation = savedRot2;
        }

        if (gazeCtrl.eyes.Length == 1 || parallelEyes)
        { //We only have one eye, or the eyes are currently looking in a parallel direction
            GazeJoint eye1 = gazeCtrl.eyes[0];
            UnityEngine.Quaternion savedRot = eye1.bone.localRotation;
            eye1.bone.localRotation = trgEyeRot[0];
            targetPos = eye1.bone.position + 1000f * (eye1.helper.position - eye1.bone.position);
            eye1.bone.localRotation = savedRot;
        }

        //Execute the gaze shift, with no head alignment
        gazeCtrl.Head.align = 0f;
        gazeCtrl.GazeAt(targetPos);
    }

    protected virtual void Transition_GazeAwayNoGaze()
    {
        // Compute time when next gaze shift should begin
        gazeLength = (float)normDist2.NextDouble();

        if (gazeRateMultiplier > 0f)
        {
            gazeLength /= gazeRateMultiplier;
        }

        if (gazeLength > 8f)
            gazeLength = 8f;
        if (gazeLength < 2f)
            gazeLength = 2f;
        gazeTime = 0f;

        if (gazeCtrl.StateId == (int)GazeState.NoGaze && !cancelGazeShift)
        {
            //Compute the point in space where the eyes should be shifting back to, given the source rotations.
            bool parallelEyes = false;
            Vector3 targetPos = new Vector3();
            if (gazeCtrl.eyes.Length > 1)
            { //We have (at least) two eyes
                Vector3 p1 = new Vector3();
                Vector3 p2 = new Vector3();
                GazeJoint eye1 = gazeCtrl.eyes[0];
                GazeJoint eye2 = gazeCtrl.eyes[1];
                UnityEngine.Quaternion savedRot1 = eye1.bone.localRotation;
                UnityEngine.Quaternion savedRot2 = eye2.bone.localRotation;
                eye1.bone.localRotation = srcEyeRot[0];
                eye2.bone.localRotation = srcEyeRot[1];
                parallelEyes = GeomUtil.ClosestPointsOn2Lines(
                    eye1.bone.position, (eye1.helper.position - eye1.bone.position),
                    eye2.bone.position, (eye2.helper.position - eye2.bone.position),
                    out p1, out p2);
                targetPos = 0.5f * (p1 + p2);
                eye1.bone.localRotation = savedRot1;
                eye2.bone.localRotation = savedRot2;
            }

            if (gazeCtrl.eyes.Length == 1 || parallelEyes)
            { //We only have one eye, or the eyes are currently looking in a parallel direction
                GazeJoint eye1 = gazeCtrl.eyes[0];
                UnityEngine.Quaternion savedRot = eye1.bone.localRotation;
                eye1.bone.localRotation = srcEyeRot[0];
                targetPos = eye1.bone.position + 1000f * (eye1.helper.position - eye1.bone.position);
                eye1.bone.localRotation = savedRot;
            }

            //Execute the gaze shift
            gazeCtrl.GazeAt(targetPos);
        }
        cancelGazeShift = false;
    }

    public override void _CreateStates()
    {
        // Initialize states
        _InitStateDefs<EyesAliveState>();
        _InitStateTransDefs((int)EyesAliveState.NoGaze, 1);
        _InitStateTransDefs((int)EyesAliveState.GazeAway, 1);
        states[(int)EyesAliveState.NoGaze].lateUpdateHandler = "LateUpdate_NoGaze";
        states[(int)EyesAliveState.NoGaze].nextStates[0].nextState = "GazeAway";
        states[(int)EyesAliveState.NoGaze].nextStates[0].transitionHandler = "Transition_NoGazeGazeAway";
        states[(int)EyesAliveState.GazeAway].lateUpdateHandler = "LateUpdate_GazeAway";
        states[(int)EyesAliveState.GazeAway].nextStates[0].nextState = "NoGaze";
        states[(int)EyesAliveState.GazeAway].nextStates[0].transitionHandler = "Transition_GazeAwayNoGaze";
    }


}


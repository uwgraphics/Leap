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
    public bool resetGazeAversion = false;

    protected float gazeTime = 0f;
    protected float gazeLength = 0f;
    protected GameObject prevGazeTarget;

    protected GazeController gazeCtrl = null;
    protected BlinkController blinkCtrl = null;
    protected GazeAversionController gazeAversionCtrl = null;

    protected MersenneTwisterRandomSource randNumGen = null; //random number generator for seeding the distributions below
    protected NormalDistribution normDist1 = null; //Length (seconds) of gaze shift
    protected NormalDistribution normDist2 = null; //Time until next gaze shift
    protected ContinuousUniformDistribution uniDist1 = null; //Amplitude of gaze shift
    protected ContinuousUniformDistribution uniDist2 = null; //Direction of gaze shift

    public override void Start()
    {
        base.Start();

        // Get relevant animation controllers
        blinkCtrl = GetComponent<BlinkController>();
        gazeAversionCtrl = GetComponent<GazeAversionController>();
        gazeCtrl = GetComponent<GazeController>();
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

        gazeLength = (float)normDist1.NextDouble();
        if (gazeLength > 2f)
            gazeLength = 2f;
        if (gazeLength < 0.2f)
            gazeLength = 0.2f;
        gazeTime = 0f;
    }

    protected virtual void LateUpdate_NoGaze()
    {
        gazeTime += DeltaTime;

        if (gazeTime > gazeLength) // Time to trigger a gaze shift
        {
            if (gazeAversionCtrl != null)
            {
                // Check if we have a gaze aversion controller operating
                // Gaze aversion controller and the base gaze controller have precedence in initiating gaze shifts
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
            resetGazeAversion = true;
        }

        if (gazeTime > gazeLength || resetGazeAversion)
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

        // Initiate gaze aversion
        prevGazeTarget = gazeCtrl.FixGazeTarget;
        gazeCtrl.head.align = 0f;
        if (gazeCtrl.torso != null)
            gazeCtrl.torso.align = 0f;
        gazeCtrl.GazeAway(yaw, pitch);
    }

    protected virtual void Transition_GazeAwayNoGaze()
    {
        // Compute time when next gaze shift should begin
        gazeLength = (float)normDist2.NextDouble();

        // Compute gaze fixation length
        if (gazeRateMultiplier > 0f)
            gazeLength /= gazeRateMultiplier;
        if (gazeLength > 8f)
            gazeLength = 8f;
        if (gazeLength < 2f)
            gazeLength = 2f;
        gazeTime = 0f;

        if (gazeCtrl.StateId == (int)GazeState.NoGaze && !resetGazeAversion)
        {
            // Initiate gaze aversion
            gazeCtrl.head.align = 0f;
            if (gazeCtrl.torso != null)
                gazeCtrl.torso.align = 0f;
            gazeCtrl.GazeAt(prevGazeTarget);
        }
        resetGazeAversion = false;
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


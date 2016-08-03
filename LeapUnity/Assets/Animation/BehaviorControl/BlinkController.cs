using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MathNet.Numerics;
using MathNet.Numerics.Distributions;
using MathNet.Numerics.RandomSources;

public enum BlinkState
{
    NoBlink = 0,
    WaitForStart,
    Blinking,
    GazeBlink
};

/// <summary>
/// Animation controller for blinking and
/// general eyelid animation.
/// </summary>
public class BlinkController : AnimController
{
    /// <summary>
    /// Number of blinks per second. 
    /// </summary>
    public float blinkRate = 0.2f;

    /// <summary>
    /// Maximum length of period between two blinks
    /// (to prevent unnatural "staring"). 
    /// </summary>
    public float maxBlinkPeriod = 10f;

    /// <summary>
    /// Mean blink length (0.19 s for natural human blinks). 
    /// </summary>
    public float blinkLength = 0.19f;

    /// <summary>
    /// Offset between left and right eye blink (in seconds).
    /// </summary>
    /// <remarks>0.04s is a good value for cartoon characters.</remarks>
    public float blinkEyeOffTime = 0f;

    /// <summary>
    /// Blink ease-in time (normalized). 
    /// </summary>
    public float blinkEaseInTime = 0.35f;

    /// <summary>
    /// Blink sustain time (normalized). 
    /// </summary>
    public float blinkSustainTime = 0.25f;

    /// <summary>
    /// Blink ease-out time (normalized). 
    /// </summary>
    public float blinkEaseOutTime = 0.65f;

    /// <summary>
    /// Mean blink decay length (how long it takes
    /// for eyelids to go back to base weight).
    /// </summary>
    public float blinkDecayLength = 6f;

    /// <summary>
    /// Amount of droop applied to eyelids based on eye movement.
    /// </summary>
    public float eyelidDroopFactor = 0.32f;

    /// <summary>
    /// Apply downward rotation to the eyes (to follow eyelid movement).
    /// </summary>
    public bool moveDownEyes = false;

    /// <summary>
    /// How much eyelids droop between blinks when no other eyelid motion
    /// is applied.
    /// </summary>
    public float blinkBaseWeight = 0f;

    /// <summary>
    /// How much the eyelids will be closed at the apex of the blink.
    /// </summary>
    public float blinkMaxWeight = 1f;

    /// <summary>
    /// Blink decay magnitude, as percentage of maximum blink magnitude
    /// (how much the eyes will remain closed at the end of the blink).
    /// </summary>
    public float blinkDecayWeight = 0.05f;

    /// <summary>
    /// Enable/disable gaze evoked blinking. 
    /// </summary>
    public bool gazeEvokedBlinks = true;

    /// <summary>
    /// Name of the agent's left morph target.
    /// </summary>
    public string blinkLeftMTName = "ModifierBlinkLeft";

    /// <summary>
    /// Name of the agent's right morph target.
    /// </summary>
    public string blinkRightMTName = "ModifierBlinkRight";

    protected float lBlinkWeight = 0;
    protected float rBlinkWeight = 0;
    protected float nextBlinkTime = 0;
    protected float curBlinkTime = 0;
    protected float curBlinkLength = 0;
    protected float curBlinkDecayTime = 0;
    protected float curBlinkDecayLength = 0;
    protected float curBlinkMaxWeight = 0;
    protected float curBlinkDecayWeight = 0;
    protected bool explBlink = false;
    protected float explNextBlinkTime = 0;
    protected float explBlinkLength = 0;
    protected float explBlinkMaxWeight = 0;

    protected GazeController gazeCtrl = null;
    protected bool gazeShiftStarted = false;
    protected float prevEyePitch = 0;

    protected MersenneTwisterRandomSource randNumGen;
    protected ContinuousUniformDistribution uniDist;
    protected NormalDistribution normDist;
    protected ExponentialDistribution expDist;

    protected int lBlinkMTIndex = -1;
    protected int rBlinkMTIndex = -1;

    /// <summary>
    /// Current left blink weight.
    /// </summary>
    public float LBlinkWeight
    {
        get
        {
            return lBlinkWeight;
        }
    }

    /// <summary>
    /// Current right blink weight.
    /// </summary>
    public float RBlinkWeight
    {
        get
        {
            return rBlinkWeight;
        }
    }

    /// <summary>
    /// Do an explicitly specified blink. 
    /// </summary>
    /// <param name="startTime">
    /// Blink start time (in seconds, relative to now).
    /// </param>
    /// <param name="weight">
    /// Blink magnitude (0-1).
    /// </param>
    public void Blink(float startTime, float weight)
    {
        explBlink = true;
        explNextBlinkTime = startTime;
        explBlinkLength = _GenerateBlinkLength();
        explBlinkMaxWeight = weight;

        if (StateId == (int)BlinkState.WaitForStart)
            _InitExplicitBlink();
    }

    /// <summary>
    /// Do an explicitly specified blink. 
    /// </summary>
    /// <param name="startTime">
    /// Blink start time (in seconds, relative to now).
    /// </param>
    /// <param name="weight">
    /// Blink magnitude (0-1).
    /// </param>
    /// <param name="prob">
    /// Blink probability (0-1).
    /// </param>
    /// <returns>true if there will be a blink, false otherwise.</returns>
    public bool Blink(float startTime, float weight, float prob)
    {
        if (uniDist == null || uniDist.NextDouble() >= prob)
            return false;

        explBlink = true;
        explNextBlinkTime = startTime;
        explBlinkLength = _GenerateBlinkLength();
        explBlinkMaxWeight = weight;

        if (StateId == (int)BlinkState.WaitForStart)
            _InitExplicitBlink();

        return true;
    }

    public override void Start()
    {
        base.Start();

        // Try to find blink morph channels and anim.
        lBlinkMTIndex = GetComponent<MorphController>().GetMorphChannelIndex(blinkLeftMTName);
        rBlinkMTIndex = GetComponent<MorphController>().GetMorphChannelIndex(blinkRightMTName);
        if (lBlinkMTIndex < 0 || rBlinkMTIndex < 0)
        {
            Debug.LogWarning("Blink Controller on agent " + gameObject.name +
                             " could not be initialized; eye blink morph targets missing");
            this.enabled = false;

            return;
        }

        _RenormalizeBlinkTimes();

        // Set up random number generators
        randNumGen = new MersenneTwisterRandomSource();
        uniDist = new ContinuousUniformDistribution(randNumGen);
        normDist = new NormalDistribution(randNumGen);
        expDist = new ExponentialDistribution(randNumGen);

        // If a gaze controller is defined, enable gaze-evoked blinking
        gazeCtrl = gameObject.GetComponent<GazeController>();
        if (gazeCtrl != null)
            gazeCtrl.StateChange += new StateChangeEvtH(GazeController_StateChange);
    }

    public override void LateUpdate()
    {
        prevEyePitch = gazeCtrl.lEye.Pitch;

        base.LateUpdate();
    }

    protected virtual void Update_NoBlink()
    {
        _ResetEyelids();
        _DroopEyelids();

        if (gazeCtrl.StateId == (int)GazeState.NoGaze || gazeEvokedBlinks && gazeShiftStarted || explBlink)
            GoToState((int)BlinkState.WaitForStart);
    }

    protected virtual void Update_WaitForStart()
    {
        _ResetEyelids();
        _DroopEyelids();

        if (!explBlink && gazeShiftStarted)
            // Try to generate gaze-evoked blink
            GoToState((int)BlinkState.GazeBlink);

        curBlinkTime += DeltaTime;
        if (curBlinkTime >= nextBlinkTime)
            // Execute blink
            GoToState((int)BlinkState.Blinking);
    }

    protected virtual void Update_Blinking()
    {
        _ResetEyelids();

        curBlinkTime += DeltaTime;
        bool finished = _UpdateBlink(lBlinkMTIndex, curBlinkTime);
        finished = _UpdateBlink(rBlinkMTIndex, curBlinkTime - blinkEyeOffTime) && finished;

        _DroopEyelids();

        if (finished)
            GoToState((int)BlinkState.NoBlink);
    }

    protected virtual void LateUpdate_Blinking()
    {
        if (moveDownEyes && gazeCtrl != null &&
           gazeCtrl.StateId != (int)GazeState.Shifting &&
           gazeCtrl.stylizeGaze)
        {
            // Apply downward rotation to the eyes (to follow eyelid movement)

            curBlinkTime += DeltaTime;

            float t = 0;
            float nbt = curBlinkTime / curBlinkLength;
            float beist = blinkEaseInTime + blinkSustainTime;
            if (nbt < blinkEaseInTime)
            {
                // Blink still in ease-in phase
                t = nbt / blinkEaseInTime;
            }
            else if (nbt >= blinkEaseInTime && nbt <= beist)
            {
                // Sustain blink for a while
                t = 1f;
            }
            else if (nbt > beist && nbt <= 1)
            {
                // Blink in ease-out phase
                t = 1f - (nbt - beist) / (1 - beist);
            }

            gazeCtrl.lEye.Pitch += (t * gazeCtrl.lEye.downOMR / 2f);
            gazeCtrl.rEye.Pitch += (t * gazeCtrl.rEye.downOMR / 2f);
        }
    }

    protected virtual void Update_GazeBlink()
    {
        _ResetEyelids();
        _DroopEyelids();

        // Compute rotation distance
        float hrotd = 0;
        if (gazeCtrl.head != null)
            hrotd = UnityEngine.Vector3.Angle(gazeCtrl.head._SourceDirection, gazeCtrl.head._TargetDirection);

        // Compute blink probability
        float pb = 0.4f * (blinkRate / 0.3f) * hrotd / 30f - 0.067f;

        if (!explBlink && gazeEvokedBlinks && uniDist.NextDouble() < pb)
        {
            // Generate gaze-evoked blink

            // Compute blink length
            curBlinkLength = _GenerateBlinkLength();
            curBlinkTime = 0f;

            // Compute blink magnitude
            curBlinkMaxWeight = 0.3f * hrotd / 16f + 0.35125f;
            if (curBlinkMaxWeight > 1 || gazeCtrl.stylizeGaze)
                curBlinkMaxWeight = 1;

            // Compute time of next blink
            nextBlinkTime = 0;

            GoToState((int)BlinkState.WaitForStart);
        }
        else
        {
            // Don't blink until gaze shift is over
            GoToState((int)BlinkState.NoBlink);
        }
    }

    protected virtual void Transition_NoBlinkWaitForStart()
    {
        if (explBlink)
        {
            // Do an explicitly specified blink
            _InitExplicitBlink();
        }
        else
        {
            // Compute next blink parameters
            curBlinkTime = 0f;
            curBlinkLength = _GenerateBlinkLength();
            curBlinkMaxWeight = blinkMaxWeight;
            expDist.SetDistributionParameters(blinkRate);
            nextBlinkTime = (float)expDist.NextDouble();
            if (nextBlinkTime > maxBlinkPeriod)
                nextBlinkTime = maxBlinkPeriod;
        }
    }

    protected virtual void Transition_WaitForStartBlinking()
    {
        curBlinkTime = 0;
        explBlink = false;

        // Initialize blink decay parameters
        curBlinkDecayLength = blinkDecayLength * curBlinkLength / blinkLength;
        curBlinkDecayTime = curBlinkDecayLength;
        curBlinkDecayWeight = blinkBaseWeight + blinkDecayWeight * (curBlinkMaxWeight - blinkBaseWeight);
    }

    protected virtual void Transition_BlinkingNoBlink()
    {
        lBlinkWeight = 0f;
        rBlinkWeight = 0f;
    }

    protected virtual void Transition_WaitForStartGazeBlink()
    {
        gazeShiftStarted = false;
    }

    protected virtual void Transition_GazeBlinkNoBlink()
    {
    }

    protected virtual void Transition_GazeBlinkWaitForStart()
    {
    }

    protected virtual void GazeController_StateChange(AnimController sender, int srcState, int trgState)
    {
        if (trgState == (int)GazeState.Shifting)
            gazeShiftStarted = true;
    }

    protected bool _UpdateBlink(int blinkMTIndex, float time)
    {
        if (time < 0)
            // Too soon
            return false;

        float nbt = time / curBlinkLength;
        float beist = blinkEaseInTime + blinkSustainTime;
        if (nbt < blinkEaseInTime)
        {
            // Blink still in ease-in phase

            float t = nbt / blinkEaseInTime;
            float t2 = t * t;
            float dw = curBlinkMaxWeight * (-2 * t2 * t + 3 * t2);
            _morphController.morphChannels[blinkMTIndex].weight = dw;
        }
        else if (nbt >= blinkEaseInTime && nbt <= beist)
        {
            // Sustain blink for a while

            _morphController.morphChannels[blinkMTIndex].weight = curBlinkMaxWeight;
        }
        else if (nbt > beist && nbt <= 1)
        {
            // Blink in ease-out phase

            float t = (nbt - beist) / (1 - beist);
            float t2 = t * t;
            float dw = (curBlinkMaxWeight - curBlinkDecayWeight) * (1 + 2 * t2 * t - 3 * t2);
            _morphController.morphChannels[blinkMTIndex].weight = curBlinkDecayWeight + dw;
        }
        else
        {
            // Blink finished

            // Make sure the eye is still a bit closed
            _morphController.morphChannels[blinkMTIndex].weight = curBlinkDecayWeight;

            return true;
        }

        if (blinkMTIndex == lBlinkMTIndex)
            lBlinkWeight = _morphController.morphChannels[blinkMTIndex].weight;
        else if (blinkMTIndex == rBlinkMTIndex)
            rBlinkWeight = _morphController.morphChannels[blinkMTIndex].weight;

        return false;
    }

    protected virtual float _GenerateBlinkLength()
    {
        normDist.SetDistributionParameters(blinkLength, 0.04);
        float len = (float)normDist.NextDouble();

        return len < 0.001f ? 0.001f : len;
    }

    protected virtual void _RenormalizeBlinkTimes()
    {
        float s = 1f / (blinkEaseInTime + blinkSustainTime + blinkEaseOutTime);
        blinkEaseInTime *= s;
        blinkSustainTime *= s;
        blinkEaseOutTime *= s;
    }

    protected virtual void _InitExplicitBlink()
    {
        curBlinkTime = 0;
        nextBlinkTime = explNextBlinkTime;
        curBlinkLength = explBlinkLength;
        curBlinkMaxWeight = explBlinkMaxWeight;
    }

    protected virtual void _ResetEyelids()
    {
        _morphController.morphChannels[lBlinkMTIndex].weight = 0f;
        _morphController.morphChannels[rBlinkMTIndex].weight = 0f;
    }

    protected virtual void _DroopEyelids()
    {
        // Apply droop to eyelids
        float droop = blinkBaseWeight;
        if (StateId != (int)BlinkState.Blinking)
        {
            // Apply extra droop if a blink just finished
            curBlinkDecayTime -= DeltaTime;
            if (curBlinkDecayTime > 0)
                droop = blinkBaseWeight + curBlinkDecayTime / curBlinkDecayLength * (curBlinkDecayWeight - blinkBaseWeight);
        }
        droop += eyelidDroopFactor * prevEyePitch / 35f;
        droop = droop < 0 ? 0 : droop; // Partial blinks don't look quite right...
        _morphController.morphChannels[lBlinkMTIndex].weight += droop;
        _morphController.morphChannels[rBlinkMTIndex].weight += droop;

        lBlinkWeight = _morphController.morphChannels[lBlinkMTIndex].weight;
        rBlinkWeight = _morphController.morphChannels[rBlinkMTIndex].weight;
    }

    public override void _CreateStates()
    {
        // Initialize states
        _InitStateDefs<BlinkState>();
        _InitStateTransDefs((int)BlinkState.NoBlink, 1);
        _InitStateTransDefs((int)BlinkState.WaitForStart, 2);
        _InitStateTransDefs((int)BlinkState.Blinking, 1);
        _InitStateTransDefs((int)BlinkState.GazeBlink, 2);
        //_InitAnimStates( (int)BlinkState.Blinking, 1 );
        states[(int)BlinkState.NoBlink].updateHandler = "Update_NoBlink";
        states[(int)BlinkState.NoBlink].nextStates[0].nextState = "WaitForStart";
        states[(int)BlinkState.NoBlink].nextStates[0].transitionHandler = "Transition_NoBlinkWaitForStart";
        states[(int)BlinkState.WaitForStart].updateHandler = "Update_WaitForStart";
        states[(int)BlinkState.WaitForStart].nextStates[0].nextState = "Blinking";
        states[(int)BlinkState.WaitForStart].nextStates[0].transitionHandler = "Transition_WaitForStartBlinking";
        states[(int)BlinkState.WaitForStart].nextStates[1].nextState = "GazeBlink";
        states[(int)BlinkState.WaitForStart].nextStates[1].transitionHandler = "Transition_WaitForStartGazeBlink";
        states[(int)BlinkState.Blinking].updateHandler = "Update_Blinking";
        states[(int)BlinkState.Blinking].lateUpdateHandler = "LateUpdate_Blinking";
        states[(int)BlinkState.Blinking].nextStates[0].nextState = "NoBlink";
        states[(int)BlinkState.Blinking].nextStates[0].transitionHandler = "Transition_BlinkingNoBlink";
        states[(int)BlinkState.GazeBlink].updateHandler = "Update_GazeBlink";
        states[(int)BlinkState.GazeBlink].nextStates[0].nextState = "NoBlink";
        states[(int)BlinkState.GazeBlink].nextStates[0].transitionHandler = "Transition_GazeBlinkNoBlink";
        states[(int)BlinkState.GazeBlink].nextStates[1].nextState = "WaitForStart";
        states[(int)BlinkState.GazeBlink].nextStates[1].transitionHandler = "Transition_GazeBlinkWaitForStart";
    }
}

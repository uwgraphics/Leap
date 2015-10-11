using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MathNet.Numerics;
using MathNet.Numerics.Distributions;
using MathNet.Numerics.RandomSources;
using System.Text;

public enum GazeAversionState
{
    MutualGaze,
    GazeAway
};

public enum GazeAversionCondition
{
    None,
    RandomGaze,
    OurModel,
    BadModel
};

public enum GazeAversionTarget
{
    None,
    Up,
    Down,
    Side
};

public enum GazeAversionType
{
    None,
    Cognitive,
    Intimacy,
    TurnTaking,
    Manual
};

public class GazeAversionController : AnimController
{
    protected GazeController gazeCtrl = null;
    protected SpeechController speechCtrl = null;

    public GazeAversionCondition condition = GazeAversionCondition.OurModel;
    protected float timeElapsed = 0f;
    public GameObject mutualGazeObject = null;

    //Distributions for drawing timing parameters from
    protected MersenneTwisterRandomSource randNumGen = null; //random number generator for seeding the distributions below
    protected NormalDistribution cogStartDistributionQuestion = null;
    protected NormalDistribution cogStartDistributionAnswer = null;
    protected NormalDistribution cogStartDistributionOther = null;
    protected NormalDistribution cogEndDistribution = null;
    protected NormalDistribution turnBackDistribution = null;
    protected NormalDistribution nextIntimacyDistributionSpeaking = null;
    protected NormalDistribution nextIntimacyDistributionListening = null;
    protected NormalDistribution intimacyLengthDistribution = null;

    //timing in the "random" condition
    protected double randomMutualGazeTime = 0f;
    protected double randomGazeAwayTime = 0f;

    //Internal timing parameters
    protected double cogStartTime = 2.0;
    protected double cogEndTime = 0.5;
    protected double turnBackTime = 2.0;
    protected double nextIntimacyGaze = 6.0;
    protected double intimacyGazeLength = 2.0;
    protected double manualEndTime = 0f;
    protected GazeAversionType aversionType = GazeAversionType.None;

    [HideInInspector]
    public bool doCognitiveGazeShift = true;
    [HideInInspector]
    public GazeAversionTarget aversionTarget = GazeAversionTarget.None;
    protected GameObject[] upTargets = null;
    protected GameObject[] sideTargets = null;
    protected GameObject[] downTargets = null;

    public override void Start()
    {
        base.Start();

        gazeCtrl = GetComponent<GazeController>();
        speechCtrl = gameObject.GetComponent<SpeechController>();
        speechCtrl.StateChange += new StateChangeEvtH(SpeechController_StateChange);

        //Initialize distributions
        randNumGen = new MersenneTwisterRandomSource();
        cogStartDistributionAnswer = new NormalDistribution(randNumGen);
        cogStartDistributionAnswer.SetDistributionParameters(1.0, 0.25);
        cogStartDistributionQuestion = new NormalDistribution(randNumGen);
        cogStartDistributionQuestion.SetDistributionParameters(1.0, 0.25);
        cogStartDistributionOther = new NormalDistribution(randNumGen);
        cogStartDistributionOther.SetDistributionParameters(1.0, 0.5);
        cogEndDistribution = new NormalDistribution(randNumGen);
        cogEndDistribution.SetDistributionParameters(1.2, 0.25);
        turnBackDistribution = new NormalDistribution(randNumGen);
        turnBackDistribution.SetDistributionParameters(2.3, 0.5);
        nextIntimacyDistributionSpeaking = new NormalDistribution(randNumGen);
        nextIntimacyDistributionSpeaking.SetDistributionParameters(4.0, 1.0);
        nextIntimacyDistributionListening = new NormalDistribution(randNumGen);
        nextIntimacyDistributionListening.SetDistributionParameters(5.0, 1.0);
        intimacyLengthDistribution = new NormalDistribution(randNumGen);
        intimacyLengthDistribution.SetDistributionParameters(1.0, 0.2);

        randomMutualGazeTime = (randNumGen.NextDouble() * 8.0) + 1.0;
        randomGazeAwayTime = (randNumGen.NextDouble() * 2.0) + 0.2;

        upTargets = GameObject.FindGameObjectsWithTag("AvertTargetUp");
        sideTargets = GameObject.FindGameObjectsWithTag("AvertTargetSide");
        downTargets = GameObject.FindGameObjectsWithTag("AvertTargetDown");

        planListeningIntimacyGazeAversion();
    }

    //This function receives notifications from the speech controller every time it changes state
    protected virtual void SpeechController_StateChange(AnimController sender, int srcState, int trgState)
    {
        if (enabled)
        {
            // No Speech -> Prepare Speech
            if (srcState == (int)SpeechState.NoSpeech && trgState == (int)SpeechState.PrepareSpeech)
            {
                resetTime();
                if (doCognitiveGazeShift)
                { //Plan and execute a cognitive gaze shift
                    speechCtrl.prepareSpeech = true;
                    planCognitiveGazeAversion();
                    GoToState((int)GazeAversionState.GazeAway);
                }
                else
                {
                    planSpeakingIntimacyGazeAversion();
                    speechCtrl.prepareSpeech = false;
                }
            }
            // Prepare Speech -> Speaking
            else if (srcState == (int)SpeechState.PrepareSpeech && trgState == (int)SpeechState.Speaking)
            {
                resetTime();
                planTurnBack();
            }
            //Speaking -> No Speech
            else if (srcState == (int)SpeechState.Speaking && trgState == (int)SpeechState.NoSpeech)
            {
                resetTime();
                planListeningIntimacyGazeAversion();
            }
        }
    }

    public virtual void resetTime()
    {
        timeElapsed = 0f;
    }

    public virtual void setCognitiveParameters(float cs, float ce)
    {
        cogStartTime = cs;
        cogEndTime = ce;
    }

    public virtual void setTargetType(GazeAversionTarget t)
    {
        aversionTarget = t;
    }

    public virtual void triggerManualGazeAversion(float length, GazeAversionTarget target)
    {
        manualEndTime = length;
        aversionType = GazeAversionType.Manual;
        aversionTarget = target;
        resetTime();
    }

    public override void Update()
    {
        timeElapsed += DeltaTime;

        base.Update();
    }

    protected virtual void Update_GazeAway()
    {
        //RANDOM MODEL
        if (condition == GazeAversionCondition.RandomGaze)
        {
            if (timeElapsed >= randomGazeAwayTime)
            {
                GoToState((int)GazeAversionState.MutualGaze);
            }
        }

        //OUR MODEL
        else
        {
            //Check if we are near the end of an utterance, and go into "turn taking" mode
            if (speechCtrl.StateId == (int)SpeechState.Speaking && ((speechCtrl.SpeechLength - speechCtrl.curPlayTime) < turnBackTime))
            {
                aversionType = GazeAversionType.TurnTaking;
                GoToState((int)GazeAversionState.MutualGaze);
                return;
            }
            //We are in a normal intimacy gaze aversion
            if (aversionType == GazeAversionType.Intimacy)
            {
                if (timeElapsed >= intimacyGazeLength)
                {
                    if (speechCtrl.StateId == (int)SpeechState.Speaking)
                    {
                        planSpeakingIntimacyGazeAversion();
                    }
                    else
                    {
                        planListeningIntimacyGazeAversion();
                    }
                    GoToState((int)GazeAversionState.MutualGaze);
                    return;
                }
            }
            //We are in a cognitive gaze aversion
            else if (aversionType == GazeAversionType.Cognitive)
            {
                //Pre speech
                if (speechCtrl.StateId == (int)SpeechState.PrepareSpeech)
                {
                    if (timeElapsed >= cogStartTime)
                    {
                        speechCtrl.prepareSpeech = false;
                        resetTime();
                    }
                }
                //Speech has started
                else if (speechCtrl.StateId == (int)SpeechState.Speaking)
                {
                    if (timeElapsed >= cogEndTime)
                    {
                        planSpeakingIntimacyGazeAversion();
                        GoToState((int)GazeAversionState.MutualGaze);
                    }
                }
            }
            else if (aversionType == GazeAversionType.Manual)
            {
                if (timeElapsed >= manualEndTime)
                {
                    planSpeakingIntimacyGazeAversion();
                    GoToState((int)GazeAversionState.MutualGaze);
                }
            }
        }
    }

    protected virtual void Update_MutualGaze()
    {

        //RANDOM CONDITION
        if (condition == GazeAversionCondition.RandomGaze)
        {
            if (timeElapsed >= randomMutualGazeTime)
            {
                GoToState((int)GazeAversionState.GazeAway);
            }
        }

        //OUR MODEL
        else
        {
            //Check if we are near the end of an utterance, and go into "turn taking" mode
            if (speechCtrl.StateId == (int)SpeechState.Speaking && ((speechCtrl.SpeechLength - speechCtrl.curPlayTime) < turnBackTime))
            {
                aversionType = GazeAversionType.TurnTaking;
            }
            else if (aversionType == GazeAversionType.Manual)
            {
                GoToState((int)GazeAversionState.GazeAway);
            }
            //We are in an "intimacy gaze aversion" mode
            else if (aversionType == GazeAversionType.Intimacy)
            {
                if (timeElapsed >= nextIntimacyGaze)
                {
                    GoToState((int)GazeAversionState.GazeAway);
                }
            }

        }
    }

    protected virtual void planCognitiveGazeAversion()
    {
        aversionType = GazeAversionType.Cognitive;

        //When to start the cognitive shift
        if (speechCtrl.speechType == SpeechType.Answer)
        {
            cogStartTime = cogStartDistributionAnswer.NextDouble();
        }
        else if (speechCtrl.speechType == SpeechType.Question)
        {
            cogStartTime = cogStartDistributionQuestion.NextDouble();
        }
        else if (speechCtrl.speechType == SpeechType.Other)
        {
            cogStartTime = cogStartDistributionOther.NextDouble();
        }

        //Hard limits to between 0.5 and 3 seconds
        if (cogStartTime >= 3.0)
            cogStartTime = 3.0;
        else if (cogStartTime <= 0.5)
            cogStartTime = 0.5;

        //When to end the cognitive shift
        cogEndTime = cogEndDistribution.NextDouble();
        if (cogEndTime >= 3.0)
            cogEndTime = 3.0;
        else if (cogEndTime <= 0.5)
            cogEndTime = 0.5;

    }

    protected virtual void planTurnBack()
    {
        turnBackTime = turnBackDistribution.NextDouble();
        if (turnBackTime <= 0.5)
            turnBackTime = 0.5;
        if (turnBackTime >= 4.0)
            turnBackTime = 4.0;

        //Make sure that it fits withing the length of speech!
        float speechLength = speechCtrl.SpeechLength;
        if (turnBackTime >= speechLength)
        {
            turnBackTime = 0.25f * speechLength;
        }
    }

    //Plan for next intimacy gaze aversion, while speaking
    public virtual void planSpeakingIntimacyGazeAversion()
    {
        aversionType = GazeAversionType.Intimacy;

        nextIntimacyGaze = nextIntimacyDistributionSpeaking.NextDouble();

        intimacyGazeLength = intimacyLengthDistribution.NextDouble();
        if (intimacyGazeLength <= 0.5)
            intimacyGazeLength = 0.5;
        else if (intimacyGazeLength >= 2.5)
            intimacyGazeLength = 2.5;
    }

    //Plan for next intimacy gaze aversion, while listening
    public virtual void planListeningIntimacyGazeAversion()
    {
        aversionType = GazeAversionType.Intimacy;

        nextIntimacyGaze = nextIntimacyDistributionListening.NextDouble();

        intimacyGazeLength = intimacyLengthDistribution.NextDouble();
        if (intimacyGazeLength <= 0.5)
            intimacyGazeLength = 0.5;
        else if (intimacyGazeLength >= 2.5)
            intimacyGazeLength = 2.5;
    }

    protected virtual void Transition_MutualGazeGazeAway()
    {
        if (condition != GazeAversionCondition.None)
        {
            //We do the opposite thing in the "bad" model
            if (condition == GazeAversionCondition.BadModel)
            {
                Transition_GazeAwayMutualGaze_helper();
                return;
            }
            Transition_MutualGazeGazeAway_helper();
        }
    }

    protected virtual void Transition_MutualGazeGazeAway_helper()
    {
        //Minimal use of head for intimacy gaze aversions
        if (aversionType == GazeAversionType.Intimacy)
            gazeCtrl.head.align = 0.1f;
        else
            gazeCtrl.head.align = 0.4f;

        if (gazeCtrl.torso != null)
            gazeCtrl.torso.align = 0f;

        if (aversionTarget == GazeAversionTarget.None)
        {
            //Look away in a random direction
            double randomNumber = randNumGen.NextDouble();
            if (aversionType == GazeAversionType.Intimacy)
            {
                if (randomNumber < 0.7)
                {
                    //gaze side
                    int randomInt = randNumGen.Next(0, sideTargets.Length);
                    gazeCtrl.GazeAt(sideTargets[randomInt]);
                }
                else if (randomNumber < 0.8)
                {
                    //gaze up
                    int randomInt = randNumGen.Next(0, upTargets.Length);
                    gazeCtrl.GazeAt(upTargets[randomInt]);
                }
                else
                {
                    //gaze down
                    int randomInt = randNumGen.Next(0, downTargets.Length);
                    gazeCtrl.GazeAt(downTargets[randomInt]);
                }
            }
            else /*if (aversionType == GazeAversionType.Cognitive || aversionType == GazeAversionType.Manual )*/
            {
                if (randomNumber < 0.1)
                {
                    //gaze side
                    int randomInt = randNumGen.Next(0, sideTargets.Length);
                    gazeCtrl.GazeAt(sideTargets[randomInt]);
                }
                else if (randomNumber < 0.8)
                {
                    //gaze up
                    int randomInt = randNumGen.Next(0, upTargets.Length);
                    gazeCtrl.GazeAt(upTargets[randomInt]);
                }
                else
                {
                    //gaze down
                    int randomInt = randNumGen.Next(0, downTargets.Length);
                    gazeCtrl.GazeAt(downTargets[randomInt]);
                }
            }
        }
        else if (aversionTarget == GazeAversionTarget.Up)
        {
            //gaze up
            int randomInt = randNumGen.Next(0, upTargets.Length);
            gazeCtrl.GazeAt(upTargets[randomInt]);
        }
        else if (aversionTarget == GazeAversionTarget.Down)
        {
            //gaze down
            int randomInt = randNumGen.Next(0, downTargets.Length);
            gazeCtrl.GazeAt(downTargets[randomInt]);
        }
        else if (aversionTarget == GazeAversionTarget.Side)
        {
            //gaze side
            int randomInt = randNumGen.Next(0, sideTargets.Length);
            gazeCtrl.GazeAt(sideTargets[randomInt]);
        }

        resetTime();
    }

    protected virtual void Transition_GazeAwayMutualGaze()
    {
        if (condition != GazeAversionCondition.None)
        {
            //We do the opposite thing in the "bad" model
            if (condition == GazeAversionCondition.BadModel)
            {
                Transition_MutualGazeGazeAway_helper();
                return;
            }
            Transition_GazeAwayMutualGaze_helper();
        }
    }

    protected virtual void Transition_GazeAwayMutualGaze_helper()
    {
        //Look at the interlocutor
        gazeCtrl.head.align = 1f;
        if (gazeCtrl.torso != null)
            gazeCtrl.torso.align = 0f;
        gazeCtrl.GazeAt(mutualGazeObject);
        resetTime();

        if (condition == GazeAversionCondition.RandomGaze)
        {
            randomMutualGazeTime = (randNumGen.NextDouble() * 8.0) + 1.0;
            randomGazeAwayTime = (randNumGen.NextDouble() * 2.0) + 0.2;
        }
    }

    public override void _CreateStates()
    {
        // Initialize states
        _InitStateDefs<GazeAversionState>();
        _InitStateTransDefs((int)GazeAversionState.GazeAway, 2);
        _InitStateTransDefs((int)GazeAversionState.MutualGaze, 1);
        states[(int)GazeAversionState.GazeAway].updateHandler = "Update_GazeAway";
        states[(int)GazeAversionState.GazeAway].nextStates[0].nextState = "MutualGaze";
        states[(int)GazeAversionState.GazeAway].nextStates[0].transitionHandler = "Transition_GazeAwayMutualGaze";
        states[(int)GazeAversionState.GazeAway].nextStates[1].nextState = "GazeAway";
        states[(int)GazeAversionState.GazeAway].nextStates[1].transitionHandler = "Transition_MutualGazeGazeAway";
        states[(int)GazeAversionState.MutualGaze].updateHandler = "Update_MutualGaze";
        states[(int)GazeAversionState.MutualGaze].nextStates[0].nextState = "GazeAway";
        states[(int)GazeAversionState.MutualGaze].nextStates[0].transitionHandler = "Transition_MutualGazeGazeAway";
    }
};
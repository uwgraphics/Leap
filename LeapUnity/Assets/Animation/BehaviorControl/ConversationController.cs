using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum ConversationState
{
    WaitForStart,
    Intro, // say hi
    WaitForSpeech, // wait for someone to say something
    Listen, // listen to someone speak
    Address, // address/respond to someone
    NewParticipant // acknowledge/include new participant
};

public class ConversationController : AnimController
{
    public GameObject[] targets = new GameObject[0];
    public GameObject prefTarget = null;
    public float defaultTorsoAlign = 0;
    public float prefTorsoAlign = 0;
    public bool introduce = false;
    public GameObject listenTarget = null;
    public float listenTime = 0;
    public bool listen = false;
    public GameObject addressTarget = null;
    public bool address = false;
    public bool addressAll = false;
    public GameObject newTarget = null;
    public bool addNewTarget = false;
    public bool ackNewTarget = true;
    public float speechPauseTime = 0.5f;
    public string[] speechClips = new string[0];

    public GameObject targetDummy = null;

    protected float curTurnTime = 0;
    protected float turnTime = 0;
    protected float curGazeHoldTime = 0;
    protected float gazeHoldTime = 0;
    protected MathNet.Numerics.Distributions.GammaDistribution gazeHoldDist
        = new MathNet.Numerics.Distributions.GammaDistribution(1.5f, 1f);
    protected bool newTargetDuringWaitForSpeech = false;
    protected bool newTargetDuringListen = false;
    protected int curSpeechClip = 0;
    protected float curSpeechTime = 0;
    protected bool speechPaused = false;
    protected GazeController gazeCtrl = null;
    protected GazeAversionController gazeAvCtrl = null;
    protected SpeechController speechCtrl = null;

    public virtual void Introduce(string[] speechClips)
    {
        introduce = true;
        this.speechClips = speechClips;
    }

    public virtual void Listen(string targetName, float listenTime)
    {
        listenTarget = null;
        foreach (GameObject obj in targets)
        {
            if (obj.name == targetName)
            {
                listenTarget = obj;
                break;
            }
        }

        if (listenTarget == null)
            Debug.LogError(string.Format(
                "Listen target {0} for ConversationController must be specified in the targets list.",
                targetName));

        this.listenTime = listenTime;
        listen = true;
    }

    public virtual void Address(string targetName, string[] speechClips)
    {
        addressTarget = null;
        foreach (GameObject obj in targets)
        {
            if (obj.name == targetName)
            {
                addressTarget = obj;
                break;
            }
        }

        if (addressTarget == null)
            Debug.LogError(string.Format(
                "Address target {0} for ConversationController must be specified in the targets list.",
                targetName));

        address = true;
        this.speechClips = speechClips;
    }

    public virtual void Address(string[] speechClips)
    {
        Address(listenTarget.name, speechClips);
    }

    public virtual void AddNewParticipant(GameObject target)
    {
        newTarget = target;
        addNewTarget = true;
    }

    public override void Start()
    {
        base.Start();

        gazeCtrl = GetComponent<GazeController>();
        if (gazeCtrl == null)
            Debug.LogError("ConversationController requires a GazeController component to be present on the agent.");
        gazeAvCtrl = GetComponent<GazeAversionController>();
        if (gazeAvCtrl == null)
            Debug.LogError("ConversationController requires a GazeAversionController component to be present on the agent.");
        speechCtrl = GetComponent<SpeechController>();
        if (speechCtrl == null)
            Debug.LogError("ConversationController requires a SpeechController component to be present on the agent.");

        speechCtrl.StateChange += new StateChangeEvtH(SpeechController_StateChange);
        gazeAvCtrl.mutualGazeObject = targetDummy;
    }

    protected virtual void Update_WaitForStart()
    {
        if (introduce)
            GoToState((int)ConversationState.Intro);
    }

    protected virtual void Update_Intro()
    {
        if (speechClips == null || curSpeechClip >= speechClips.Length)
        {
            // Done with introduction, wait for someone to ask a question
            GoToState((int)ConversationState.WaitForSpeech);
            return;
        }

        curTurnTime += DeltaTime;
        curSpeechTime += DeltaTime;
        if (speechPaused)
        {
            if (curSpeechTime >= speechPauseTime)
            {
                speechPaused = false;
                curSpeechTime = 0;
                ++curSpeechClip;
                if (curSpeechClip < speechClips.Length)
                    speechCtrl.Speak(speechClips[curSpeechClip]);
            }
        }

        _UpdateGaze();
    }

    protected virtual void Update_WaitForSpeech()
    {
        if (addNewTarget && newTarget != null)
        {
            if (ackNewTarget)
                GoToState((int)ConversationState.NewParticipant);
            else
                _AddNewTarget();

            return;
        }
        else if (listen && listenTarget != null)
        {
            GoToState((int)ConversationState.Listen);
            return;
        }

        _UpdateGaze();
    }

    protected virtual void Update_Listen()
    {
        if (addNewTarget && newTarget != null)
        {
            if (ackNewTarget)
                GoToState((int)ConversationState.NewParticipant);
            else
                _AddNewTarget();

            return;
        }
        else if (address && addressTarget != null)
        {
            GoToState((int)ConversationState.Address);
            return;
        }

        _UpdateGaze();
    }

    protected virtual void Update_Address()
    {
        if (curSpeechClip >= speechClips.Length)
        {
            // Done with speech, wait of someone to ask a question
            GoToState((int)ConversationState.WaitForSpeech);
            return;
        }

        curTurnTime += DeltaTime;
        curSpeechTime += DeltaTime;
        if (speechPaused)
        {
            if (curSpeechTime >= speechPauseTime)
            {
                speechPaused = false;
                curSpeechTime = 0;
                ++curSpeechClip;
                if (curSpeechClip < speechClips.Length)
                    speechCtrl.Speak(speechClips[curSpeechClip]);
            }
        }

        _UpdateGaze();
    }

    protected virtual void Update_NewParticipant()
    {
        curGazeHoldTime += DeltaTime;
        if (curGazeHoldTime >= gazeHoldTime &&
            (gazeCtrl.StateId == (int)GazeState.NoGaze &&
            gazeAvCtrl.StateId == (int)GazeAversionState.MutualGaze ||
            !gazeCtrl.enabled))
        {
            // Done acknowledging the new participant
            _GazeAtNextTarget(false);
            if (newTargetDuringWaitForSpeech)
                GoToState((int)ConversationState.WaitForSpeech);
            else // if(newTargetDuringListen)
                GoToState((int)ConversationState.Listen);
        }
    }

    protected virtual void Transition_WaitForStartIntro()
    {
        introduce = false;
        _InitTurn();
        _InitGaze();
        _GazeAtNextTarget(true);
    }

    protected virtual void Transition_IntroWaitForSpeech()
    {
        speechClips = new string[0];
    }

    protected virtual void Transition_WaitForSpeechListen()
    {
        listen = false;
        _GazeAtNextTarget(true);
    }

    protected virtual void Transition_WaitForSpeechNewParticipant()
    {
        newTargetDuringWaitForSpeech = true;
        _AddNewTarget();
        _GazeAtNextTarget(false);
        gazeHoldTime = 0.75f + (float)gazeHoldDist.NextDouble() / 2f;
    }

    protected virtual void Transition_ListenAddress()
    {
        address = false;
        _InitTurn();
        _GazeAtNextTarget(true);
    }

    protected virtual void Transition_ListenNewParticipant()
    {
        newTargetDuringListen = true;
        _AddNewTarget();
        _GazeAtNextTarget(false);
        gazeHoldTime = 0.75f + (float)gazeHoldDist.NextDouble();
    }

    protected virtual void Transition_AddressWaitForSpeech()
    {
        speechClips = new string[0];
        _GazeAtNextTarget(true);
    }

    protected virtual void Transition_NewParticipantWaitForSpeech()
    {
        newTargetDuringWaitForSpeech = false;
    }

    protected virtual void Transition_NewParticipantListen()
    {
        newTargetDuringListen = false;
    }

    protected virtual void SpeechController_StateChange(AnimController sender,
        int srcState, int trgState)
    {
        if (srcState == (int)SpeechState.Speaking &&
            trgState == (int)SpeechState.NoSpeech)
        {
            speechPaused = true;
            curSpeechTime = 0;
        }
    }

    protected virtual void _InitTurn()
    {
        curTurnTime = 0;
        turnTime = 0;
        if (speechClips != null)
        {
            foreach (AudioClip clip in speechCtrl.speechClips)
                foreach (string conv_clip in speechClips)
                    if (clip.name == conv_clip)
                        turnTime += clip.length;

            turnTime += (1f + (speechClips.Length - 1) * speechPauseTime);
        }
        curSpeechClip = -1;
        curSpeechTime = 0;
        speechPaused = true;
        if (StateId == (int)ConversationState.Listen)
            speechCtrl.speechType = SpeechType.Answer;
        else
            speechCtrl.speechType = SpeechType.Other;
    }

    protected virtual void _InitGaze()
    {
        curGazeHoldTime = 0;
        gazeHoldTime = 0;
    }

    protected virtual void _UpdateGaze()
    {
        curGazeHoldTime += DeltaTime;
        if (curGazeHoldTime >= gazeHoldTime &&
            (gazeCtrl.StateId == (int)GazeState.NoGaze &&
            gazeAvCtrl.StateId == (int)GazeAversionState.MutualGaze ||
            !gazeCtrl.enabled))
        {
            _GazeAtNextTarget(false);
        }
    }

    protected virtual void _GazeAtNextTarget(bool turnChange)
    {
        // Choose next target
        GameObject next_target = gazeCtrl.gazeTarget;
        if (turnChange)
        {
            if (StateId == (int)ConversationState.WaitForStart)
            {
                next_target = (prefTarget == null) ?
                    targets[UnityEngine.Random.Range(0, targets.Length)] :
                        prefTarget;
                gazeCtrl.Torso.align = (prefTarget == null) ?
                    0 : prefTorsoAlign;
                //
                Debug.Log(string.Format("Gazing at pref. target {0} with alignment {1}",
                    next_target, gazeCtrl.Torso.align));
                //
            }
            else if (StateId == (int)ConversationState.WaitForSpeech)
            {
                next_target = listenTarget;
                gazeCtrl.Torso.align = defaultTorsoAlign;
                //
                Debug.Log(string.Format("Gazing at listen target {0} with alignment {1}",
                    next_target, gazeCtrl.Torso.align));
                //
            }
            else if (StateId == (int)ConversationState.Listen)
            {
                next_target = addressTarget;
                gazeCtrl.Torso.align = defaultTorsoAlign;
                //
                Debug.Log(string.Format("Gazing at address target {0} with alignment {1}",
                    next_target, gazeCtrl.Torso.align));
                //
            }
            else if (StateId == (int)ConversationState.Address)
            {
                next_target = (prefTarget == null) ?
                    targets[UnityEngine.Random.Range(0, targets.Length)] :
                        prefTarget;
                gazeCtrl.Torso.align = (prefTarget == null) ?
                    0 : prefTorsoAlign;
                //
                Debug.Log(string.Format("Gazing at pref./rand. target {0} with alignment {1}",
                    next_target, gazeCtrl.Torso.align));
                //
            }
        }
        else if (targets.Length > 1)
        {
            if (StateId == (int)ConversationState.WaitForSpeech &&
                newTargetDuringWaitForSpeech ||
                StateId == (int)ConversationState.Listen &&
                newTargetDuringListen)
            {
                next_target = targets[targets.Length - 1]; // b/c newly added target is always appended to the end
                if (prefTarget == null)
                    gazeCtrl.Torso.align = 0.5f;
                else
                    gazeCtrl.Torso.align = 0f;
                //
                Debug.Log(string.Format("Gazing at new target {0} with alignment {1}",
                    next_target, gazeCtrl.Torso.align));
                //
            }
            else if (StateId == (int)ConversationState.NewParticipant)
            {
                if (newTargetDuringListen)
                    next_target = listenTarget;
                else
                    while (next_target == gazeCtrl.gazeTarget)
                        next_target = targets[UnityEngine.Random.Range(0, targets.Length)];
                gazeCtrl.Torso.align = 0f;
                //
                Debug.Log(string.Format("Gazing back at target {0} with alignment {1}",
                    next_target, gazeCtrl.Torso.align));
                //
            }
            else if (StateId == (int)ConversationState.Intro ||
                (StateId == (int)ConversationState.WaitForSpeech ||
                StateId == (int)ConversationState.Address))
            {
                // Choose next target
                List<GameObject> target_list = new List<GameObject>(targets);
                if (addressAll || addressTarget == null)
                {
                    // Treat all candidate targets equally
                    next_target = target_list[UnityEngine.Random.Range(0, target_list.Count)];
                }
                else
                {
                    // Treat one target as addressee and others as bystanders
                    target_list.Remove(addressTarget);
                    if (UnityEngine.Random.Range(1, 21) <= 5)
                        next_target = target_list[UnityEngine.Random.Range(0, target_list.Count)];

                    else
                        next_target = addressTarget;
                }

                gazeCtrl.Torso.align = 0;
                //
                Debug.Log(string.Format("Gazing at target {0} with alignment {1}",
                    next_target, gazeCtrl.Torso.align));
                //
            }
            else
            {
                return;
            }
        }
        else
        {
            return;
        }

        if (next_target != gazeCtrl.gazeTarget)
        {
            // Initiate gaze shift towards the new target
            targetDummy.transform.position = next_target.transform.position;
            gazeCtrl.GazeAt(next_target);
        }

        // How long should gaze be held?
        curGazeHoldTime = 0;
        gazeHoldTime = 1.5f + (float)gazeHoldDist.NextDouble();
        // 1s is approx. duration of a gaze shift
    }

    protected virtual void _AddNewTarget()
    {
        GameObject[] targets_ext = new GameObject[targets.Length + 1];
        targets.CopyTo(targets_ext, 0);
        targets_ext[targets_ext.Length - 1] = newTarget;
        targets = targets_ext;

        newTarget = null;
        addNewTarget = false;
    }

    public override void _CreateStates()
    {
        // Initialize states
        _InitStateDefs<ConversationState>();
        _InitStateTransDefs((int)ConversationState.WaitForStart, 1);
        _InitStateTransDefs((int)ConversationState.Intro, 1);
        _InitStateTransDefs((int)ConversationState.WaitForSpeech, 2);
        _InitStateTransDefs((int)ConversationState.Listen, 2);
        _InitStateTransDefs((int)ConversationState.Address, 1);
        _InitStateTransDefs((int)ConversationState.NewParticipant, 2);
        states[(int)ConversationState.WaitForStart].updateHandler = "Update_WaitForStart";
        states[(int)ConversationState.WaitForStart].nextStates[0].nextState = "Intro";
        states[(int)ConversationState.WaitForStart].nextStates[0].transitionHandler = "Transition_WaitForStartIntro";
        states[(int)ConversationState.Intro].updateHandler = "Update_Intro";
        states[(int)ConversationState.Intro].nextStates[0].nextState = "WaitForSpeech";
        states[(int)ConversationState.Intro].nextStates[0].transitionHandler = "Transition_IntroWaitForSpeech";
        states[(int)ConversationState.WaitForSpeech].updateHandler = "Update_WaitForSpeech";
        states[(int)ConversationState.WaitForSpeech].nextStates[0].nextState = "Listen";
        states[(int)ConversationState.WaitForSpeech].nextStates[0].transitionHandler = "Transition_WaitForSpeechListen";
        states[(int)ConversationState.WaitForSpeech].nextStates[1].nextState = "NewParticipant";
        states[(int)ConversationState.WaitForSpeech].nextStates[1].transitionHandler = "Transition_WaitForSpeechNewParticipant";
        states[(int)ConversationState.Listen].updateHandler = "Update_Listen";
        states[(int)ConversationState.Listen].nextStates[0].nextState = "Address";
        states[(int)ConversationState.Listen].nextStates[0].transitionHandler = "Transition_ListenAddress";
        states[(int)ConversationState.Listen].nextStates[1].nextState = "NewParticipant";
        states[(int)ConversationState.Listen].nextStates[1].transitionHandler = "Transition_ListenNewParticipant";
        states[(int)ConversationState.Address].updateHandler = "Update_Address";
        states[(int)ConversationState.Address].nextStates[0].nextState = "WaitForSpeech";
        states[(int)ConversationState.Address].nextStates[0].transitionHandler = "Transition_AddressWaitForSpeech";
        states[(int)ConversationState.NewParticipant].updateHandler = "Update_NewParticipant";
        states[(int)ConversationState.NewParticipant].nextStates[0].nextState = "WaitForSpeech";
        states[(int)ConversationState.NewParticipant].nextStates[0].transitionHandler = "Transition_NewParticipantWaitForSpeech";
        states[(int)ConversationState.NewParticipant].nextStates[1].nextState = "Listen";
        states[(int)ConversationState.NewParticipant].nextStates[1].transitionHandler = "Transition_NewParticipantListen";
    }
}

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public enum ConversationState
{
    WaitForSpeech, // wait for someone to say something
    Listen, // listen to someone speak
    Address, // address/respond to someone
    NewParticipant // acknowledge/include new participant
};

public enum ConversationTargetType
{
    Addressee,
    Bystander
}

/// <summary>
/// Target for conversational behaviors.
/// </summary>
[Serializable]
[RequireComponent(typeof(GazeController))]
[RequireComponent(typeof(SpeechController))]
[RequireComponent(typeof(ListenController))]
public class ConversationTarget
{
    public ConversationTargetType targetType;
    public GameObject target, bodyTarget, leftEnvTarget, rightEnvTarget, downEnvTarget, upEnvTarget;
    public float thetaRange, thetaRangeBody, thetaRangeEnv;

    private Dictionary<GameObject, MathNet.Numerics.Distributions.ContinuousDistribution> _distributions;

    public ConversationTarget(ConversationTargetType targetType, GameObject target, GameObject bodyTarget,
        GameObject leftEnvTarget = null, GameObject rightEnvTarget = null, GameObject downEnvTarget = null, GameObject upEnvTarget = null,
        float thetaRange = 1f, float thetaRangeBody = 4f, float thetaRangeEnv = 15f)
    {
        if (target == null)
            throw new ArgumentNullException("target");

        this.targetType = targetType;
        this.target = target;
        this.bodyTarget = bodyTarget;
        this.leftEnvTarget = leftEnvTarget;
        this.rightEnvTarget = rightEnvTarget;
        this.downEnvTarget = downEnvTarget;
        this.upEnvTarget = upEnvTarget;
        this.thetaRange = thetaRange;
        this.thetaRangeBody = thetaRangeBody;
        this.thetaRangeEnv = thetaRangeEnv;

        Init();
    }

    /// <summary>
    /// Initialize gaze target probability distributions.
    /// </summary>
    public void Init()
    {
        _distributions = new Dictionary<GameObject, MathNet.Numerics.Distributions.ContinuousDistribution>();
        _distributions[target] = new MathNet.Numerics.Distributions.NormalDistribution(0f, thetaRange / 2f);
        if (bodyTarget != null)
            _distributions[bodyTarget] = new MathNet.Numerics.Distributions.NormalDistribution(0f, thetaRangeBody / 2f);
        var distEnv = new MathNet.Numerics.Distributions.ContinuousUniformDistribution(0, thetaRangeEnv);
        if (leftEnvTarget != null)
            _distributions[leftEnvTarget] = distEnv;
        if (rightEnvTarget != null)
            _distributions[rightEnvTarget] = distEnv;
        if (downEnvTarget != null)
            _distributions[downEnvTarget] = distEnv;
        if (upEnvTarget != null)
            _distributions[upEnvTarget] = distEnv;
    }

    /// <summary>
    /// Generate a gaze target position by sampling from a probability distribution around
    /// the target object.
    /// </summary>
    /// <param name="eyeCenter">Eye centroid position</param>
    /// <returns>Target position</returns>
    public Vector3 GenerateGazeTargetPosition(Vector3 eyeCenter, GameObject gazeTarget)
    {
        if (gazeTarget == null ||
            (gazeTarget != target && gazeTarget != bodyTarget && gazeTarget != leftEnvTarget && gazeTarget != rightEnvTarget
             && gazeTarget != downEnvTarget && gazeTarget != upEnvTarget))
            throw new ArgumentException("Gaze target not specified as part of conversational target " + target.name);

        // Get probability distribution
        if (!_distributions.ContainsKey(gazeTarget))
            Init();
        var thetaDist = _distributions[gazeTarget];

        // Compute theta and phi angles
        float phi = UnityEngine.Random.Range(0f, 360f);
        float thetaRange = 0f;
        if (thetaDist is MathNet.Numerics.Distributions.NormalDistribution)
            thetaRange = ((float)(thetaDist as MathNet.Numerics.Distributions.NormalDistribution).Sigma) * 2f;
        else // if (thetaDist is MathNet.Numerics.Distributions.ContinuousUniformDistribution)
            thetaRange = (float)(thetaDist as MathNet.Numerics.Distributions.ContinuousUniformDistribution).UpperLimit;
        float theta = 0f;
        if (thetaRange > 0f)
            theta = Mathf.Clamp((float)thetaDist.NextDouble(), 0f, thetaRange);

        // Compute gaze target position
        if (theta < 0.00001f)
            return gazeTarget.transform.position;
        Vector3 trgDir0 = gazeTarget.transform.position - eyeCenter;
        float trgDistance = trgDir0.magnitude;
        trgDir0.Normalize();
        Vector3 trgDir = Quaternion.AngleAxis(theta, Vector3.up) * trgDir0;
        trgDir = Quaternion.AngleAxis(phi, trgDir0) * trgDir;
        Vector3 trgPos = eyeCenter + trgDistance * trgDir.normalized;

        return trgPos;
    }

    /// <summary>
    /// Select an environment target using a uniform distribution.
    /// </summary>
    /// <returns>Selected target</returns>
    public GameObject SelectRandomEnvironmentTarget()
    {
        var envTargets = new List<GameObject>();
        if (leftEnvTarget != null)
            envTargets.Add(leftEnvTarget);
        if (rightEnvTarget != null)
            envTargets.Add(rightEnvTarget);
        if (downEnvTarget != null)
            envTargets.Add(downEnvTarget);
        if (upEnvTarget != null)
            envTargets.Add(upEnvTarget);

        if (envTargets.Count <= 0)
            return target;

        return envTargets[UnityEngine.Random.Range(0, envTargets.Count)];
    }
}

/// <summary>
/// Controller for conversational behaviors such as gaze and speech.
/// </summary>
public class ConversationController : AnimController
{
    // Targets are specified from left to right
    public ConversationTarget[] targets = new ConversationTarget[0];
    public string nextTargetName = null;
    public bool listen = false;
    public bool address = false;
    public bool addressAll = false;
    public string[] nextSpeechClips = new string[0];
    public float speechPauseTime = 0.2f;
    public ConversationTarget newTarget = null;
    public bool addNewTarget = false;
    public int newTargetIndex = -1;
    public bool reorientBody = true;
    public float bodyOrientationBias = 0f;
    public float gazeDelayTime = 0f;

    //  Conversational behavior state:
    protected int curTurnTargetIndex = -1;
    protected ConversationTarget curTarget = null;
    protected GameObject curGazeTarget = null;
    protected bool curAddressAll = false;
    protected float curGazeHoldTime = 0f;
    protected float gazeHoldTime = 0f;
    protected string[] curSpeechClips = null;
    protected int curSpeechIndex = -1;
    protected bool curSpeechPaused = false;
    protected float curSpeechPauseTime = 0f;
    protected ConversationState stateOnNewTarget = ConversationState.WaitForSpeech;
    protected bool doSmileExpr = false;
    protected bool doNeutralExpr = false;

    // Probability distributions for gaze target selection and gaze hold times:
    protected float pAF1A = 0.26f, pAB1A = 0.48f, pE1A = 0.26f,
        pAF1AB = 0.25f, pAB1AB = 0.51f, pBF1AB = 0.05f, pBB1AB = 0.03f, pE1AB = 0.16f,
        pAF2A = 0.28f, pAB2A = 0.075f, pE2A = 0.29f;
    protected MathNet.Numerics.Distributions.GammaDistribution dAF1A = new MathNet.Numerics.Distributions.GammaDistribution(1.65, 0.56);
    protected MathNet.Numerics.Distributions.GammaDistribution dAB1A = new MathNet.Numerics.Distributions.GammaDistribution(1.92, 0.84);
    protected MathNet.Numerics.Distributions.GammaDistribution dE1A = new MathNet.Numerics.Distributions.GammaDistribution(0.9, 1.14);
    protected MathNet.Numerics.Distributions.GammaDistribution dAF1AB = new MathNet.Numerics.Distributions.GammaDistribution(0.74, 1.55);
    protected MathNet.Numerics.Distributions.GammaDistribution dAB1AB = new MathNet.Numerics.Distributions.GammaDistribution(1.72, 1.2);
    protected MathNet.Numerics.Distributions.GammaDistribution dBF1AB = new MathNet.Numerics.Distributions.GammaDistribution(2.19, 0.44);
    protected MathNet.Numerics.Distributions.GammaDistribution dBB1AB = new MathNet.Numerics.Distributions.GammaDistribution(1.76, 0.57);
    protected MathNet.Numerics.Distributions.GammaDistribution dE1AB = new MathNet.Numerics.Distributions.GammaDistribution(1.84, 0.59);
    protected MathNet.Numerics.Distributions.GammaDistribution dAF2A = new MathNet.Numerics.Distributions.GammaDistribution(1.48, 1.1);
    protected MathNet.Numerics.Distributions.GammaDistribution dAB2A = new MathNet.Numerics.Distributions.GammaDistribution(1.92, 0.52);
    protected MathNet.Numerics.Distributions.GammaDistribution dE2A = new MathNet.Numerics.Distributions.GammaDistribution(2.23, 0.41);

    // Low-level behavior controllers:
    protected GazeController gazeCtrl = null;
    protected SpeechController speechCtrl = null;
    protected SimpleListenController listenCtrl = null;
    protected FaceController faceCtrl = null;
    protected ExpressionController exprCtrl = null;

    /// <summary>
    /// Get speech recognition results from the listen controller.
    /// </summary>
    public IList<ListenResult> ListenResults
    {
        get { return listenCtrl.Results.AsReadOnly(); }
    }
    
    /// <summary>
    /// Perform listening behavior toward the specified target.
    /// </summary>
    /// <param name="targetName">Listening target</param>
    public virtual void Listen(string targetName)
    {
        nextTargetName = targetName;
        listen = true;

        Debug.Log(string.Format("Listening to target {0} in the conversation controller on {1}", targetName, gameObject.name));
    }

    /// <summary>
    /// Perform addressing behavior toward the specified target.
    /// </summary>
    /// <param name="targetName"></param>
    /// <param name="speechClips"></param>
    public virtual void Address(string targetName, string[] speechClips)
    {
        nextTargetName = targetName;
        nextSpeechClips = speechClips;
        address = true;
        addressAll = false;

        Debug.Log(string.Format("Addressing target {0} in the conversation controller on {1}", targetName, gameObject.name));
    }

    /// <summary>
    /// Perform addressing behavior toward any addressee.
    /// </summary>
    /// <param name="speechClips"></param>
    public virtual void Address(string[] speechClips)
    {
        var addresseeList = new List<ConversationTarget>(targets.Where(t => t.targetType == ConversationTargetType.Addressee));
        int addresseeIndex = UnityEngine.Random.Range(0, addresseeList.Count);
        nextTargetName = addresseeList[addresseeIndex].target.name;
        nextSpeechClips = speechClips;
        address = true;
        addressAll = true;

        Debug.Log(string.Format("Addressing all targets in the conversation controller on {0}", gameObject.name));
    }

    /// <summary>
    /// Add a new target to the conversation and perform
    /// acknowledging/including behaviors if necessary.
    /// </summary>
    /// <param name="target">Target representing the new participant</param>
    /// <param name="targetIndex">Index specifying the location of the new target relative to the others</param>
    public virtual void AddNewTarget(ConversationTarget target, int targetIndex)
    {
        newTarget = target;
        newTargetIndex = targetIndex;
        addNewTarget = true;

        Debug.Log(string.Format("Adding new target {0} to the conversation controller on {1}", target.target.name, gameObject.name));
    }

    /// <summary>
    /// Get index of the specified target.
    /// </summary>
    /// <param name="targetName">Target name</param>
    /// <returns>Target index</returns>
    public virtual int GetTargetIndex(string targetName)
    {
        for (int targetIndex = 0; targetIndex < targets.Length; ++targetIndex)
            if (targetName == targets[targetIndex].target.name)
                return targetIndex;

        return -1;
    }

    public override void Start()
    {
        base.Start();

        // Get other behavior controllers
        gazeCtrl = GetComponent<GazeController>();
        speechCtrl = GetComponent<SpeechController>();
        listenCtrl = GetComponent<SimpleListenController>();
        listenCtrl.Listen();
        faceCtrl = GetComponent<FaceController>();
        exprCtrl = GetComponent<ExpressionController>();

        // Register for speech and listen events
        speechCtrl.StateChange += new StateChangeEvtH(SpeechController_StateChange);
        listenCtrl.StateChange += new StateChangeEvtH(ListenController_StateChange);

        // Initialize conversational behavior targets
        foreach (var target in targets)
            target.Init();
    }

    protected virtual void Update_WaitForSpeech()
    {
        if (listen && targets.Any(t => t.target.name == nextTargetName))
        {
            GoToState((int)ConversationState.Listen);
            return;
        }
        else if (addNewTarget && newTarget != null)
        {
            GoToState((int)ConversationState.NewParticipant);
            return;
        }
        else if (address && targets.Any(t => t.target.name == nextTargetName))
        {
            GoToState((int)ConversationState.Address);
            return;
        }

        _UpdateGaze();
    }

    protected virtual void Update_Listen()
    {
        if (addNewTarget && newTarget != null)
        {
            GoToState((int)ConversationState.NewParticipant);
            return;
        }
        else if (address && targets.Any(t => t.target.name == nextTargetName))
        {
            GoToState((int)ConversationState.Address);
            return;
        }
        else if (listen && targets.Any(t => t.target.name == nextTargetName))
        {
            // Switch to listening to somebody else
            curTurnTargetIndex = GetTargetIndex(nextTargetName);
            _GazeAtNextTarget(true);
            listen = false;
        }

        _UpdateGaze();
    }

    protected virtual void Update_Address()
    {
        if (curSpeechIndex >= curSpeechClips.Length)
        {
            // Done with speech, wait of someone to take the floor
            GoToState((int)ConversationState.WaitForSpeech);
            return;
        }

        // Update speech pause timing
        if (curSpeechPaused)
        {
            curSpeechPauseTime += DeltaTime;
            if (curSpeechPauseTime >= speechPauseTime)
            {
                curSpeechPaused = false;
                curSpeechPauseTime = 0f;
            }
        }
        
        if (!curSpeechPaused && speechCtrl.StateId == (int)SpeechState.NoSpeech &&
            gazeCtrl.StateId == (int)GazeState.NoGaze && !gazeCtrl.doGazeShift)
        {
            // Speak the next utterance
            speechCtrl.Speak(curSpeechClips[curSpeechIndex]);
        }

        _UpdateGaze();
    }

    protected virtual void Update_NewParticipant()
    {
        curGazeHoldTime += DeltaTime;
        if (curGazeHoldTime >= gazeHoldTime && gazeCtrl.StateId == (int)GazeState.NoGaze)
        {
            // Done acknowledging the new participant
            _GazeAtNextTarget(false);
            if (stateOnNewTarget == ConversationState.WaitForSpeech)
                GoToState((int)ConversationState.WaitForSpeech);
            else // if(stateOnNewTarget == ConversationState.Listen)
                GoToState((int)ConversationState.Listen);
        }
    }

    protected virtual void Transition_WaitForSpeechListen()
    {
        curTurnTargetIndex = GetTargetIndex(nextTargetName);
        _GazeAtNextTarget(true);
        listen = false;
    }

    protected virtual void Transition_WaitForSpeechAddress()
    {
        listenCtrl.StopListening();
        curTurnTargetIndex = GetTargetIndex(nextTargetName);
        curAddressAll = addressAll;
        curSpeechClips = nextSpeechClips;
        curSpeechIndex = 0;
        curSpeechPaused = false;
        curSpeechPauseTime = 0f;
        _GazeAtNextTarget(true);
        address = false;
        addressAll = false;
        doNeutralExpr = true;
    }

    protected virtual void Transition_WaitForSpeechNewParticipant()
    {
        stateOnNewTarget = ConversationState.WaitForSpeech;
        _AddNewTarget();
        _GazeAtNextTarget(false, true);
        addNewTarget = false;
        doSmileExpr = true;
    }

    protected virtual void Transition_ListenAddress()
    {
        listenCtrl.StopListening();
        curTurnTargetIndex = GetTargetIndex(nextTargetName);
        curAddressAll = addressAll;
        curSpeechClips = nextSpeechClips;
        curSpeechIndex = 0;
        curSpeechPaused = false;
        curSpeechPauseTime = 0f;
        _GazeAtNextTarget(true);
        address = false;
        addressAll = false;
        doNeutralExpr = true;
    }

    protected virtual void Transition_ListenNewParticipant()
    {
        stateOnNewTarget = ConversationState.Listen;
        _AddNewTarget();
        _GazeAtNextTarget(false, true);
        addNewTarget = false;
    }

    protected virtual void Transition_AddressWaitForSpeech()
    {
        listenCtrl.Listen();
        nextSpeechClips = new string[0];
        _GazeAtNextTarget(true);
        doSmileExpr = true;
    }

    protected virtual void Transition_NewParticipantWaitForSpeech()
    {
        doSmileExpr = true;
    }

    protected virtual void Transition_NewParticipantListen()
    {
    }

    protected virtual void SpeechController_StateChange(AnimController sender,
        int srcState, int trgState)
    {
        if (srcState == (int)SpeechState.Speaking &&
            trgState == (int)SpeechState.NoSpeech)
        {
            ++curSpeechIndex;
            if (curSpeechIndex < curSpeechClips.Length)
            {
                // Insert a pause before starting next speech clip
                curSpeechPaused = true;
                curSpeechPauseTime = 0f;
            }
        }
    }

    protected virtual void ListenController_StateChange(AnimController sender,
        int srcState, int trgState)
    {
        if (srcState == (int)SimpleListenState.SpeechDetected &&
            trgState == (int)SimpleListenState.Listening)
        {
            //  Speech recognized, nod at the speaker
            /*var curTurnTarget = targets[curTurnTargetIndex];
            if (curTurnTarget.target == curGazeTarget && faceCtrl != null && UnityEngine.Random.Range(0f, 1f) > 0f)
            {
                faceCtrl.Nod(1, FaceGestureSpeed.Normal, UnityEngine.Random.Range(5f, 15f), 0f);
            }*/
        }
    }

    protected virtual void _UpdateGaze()
    {
        curGazeHoldTime += DeltaTime;
        if (gazeCtrl.StateId == (int)GazeState.NoGaze)
        {
            if (curGazeHoldTime >= gazeHoldTime)
                _GazeAtNextTarget(false);

            if (exprCtrl != null && !gazeCtrl.doGazeShift)
            {
                if (doSmileExpr)
                {
                    exprCtrl.magnitude = 1f;
                    exprCtrl.ChangeExpression("ExpressionSmileClosed");
                    exprCtrl.changeTime = 1f;
                    doSmileExpr = false;
                }
                else if (doNeutralExpr)
                {
                    exprCtrl.magnitude = 0f;
                    exprCtrl.ChangeExpression("ExpressionSmileClosed");
                    exprCtrl.changeTime = 1f;
                    doNeutralExpr = false;
                }
            }
        }
    }

    protected virtual void _GazeAtNextTarget(bool turnChange, bool newTargetAdded = false)
    {
        // Initialize gaze timings
        curGazeHoldTime = 0f;
        gazeHoldTime = 0.25f;

        // Set default alignment parameters
        gazeCtrl.head.align = 1f;
        gazeCtrl.torso.align = 0f;
        gazeCtrl.pelvisAlign = 0.5f;
        gazeCtrl.bodyAlign = 0f;
        gazeCtrl.useTorso = false;

        // Get lists of addressees and bystanders
        var addresseeList = new List<ConversationTarget>(targets.Where(t => t.targetType == ConversationTargetType.Addressee));
        var bystanderList = new List<ConversationTarget>(targets.Where(t => t.targetType == ConversationTargetType.Bystander));

        // Determine gaze target
        var prevGazeTarget = curGazeTarget;
        bool holdCurGaze = false;
        if (newTargetAdded)
        {
            // Gaze at the new participant

            if (newTarget.targetType == ConversationTargetType.Addressee && reorientBody)
            {
                // New participant is an addressee, set body alignment so they are included in the conversational formation
                if (addresseeList.Count() >= 2)
                {
                    // TODO: implement sophisticated reconfiguration of the conversational formation
                    // TODO: make it robust to formations where targets lie along the long arc
                    Vector3 srcDir = GeometryUtil.ProjectVectorOntoPlane(gazeCtrl.torso.Direction, Vector3.up);
                    Vector3 trgDir = GeometryUtil.ProjectVectorOntoPlane(
                        gazeCtrl.torso.GetTargetDirection(newTarget.target.transform.position), Vector3.up);
                    Vector3 leftDir = GeometryUtil.ProjectVectorOntoPlane(
                        gazeCtrl.torso.GetTargetDirection(targets[0].target.transform.position), Vector3.up);
                    Vector3 rightDir = GeometryUtil.ProjectVectorOntoPlane(
                        gazeCtrl.torso.GetTargetDirection(targets[targets.Length - 1].target.transform.position), Vector3.up);
                    Vector3 trgDirAlign = Vector3.Slerp(leftDir, rightDir, Mathf.Clamp01(0.5f + bodyOrientationBias));
                    gazeCtrl.torso.align = Vector3.Angle(srcDir, trgDir) > 0f ?
                        Mathf.Clamp01(Vector3.Angle(srcDir, trgDirAlign) / Vector3.Angle(srcDir, trgDir)) : 0f;
                    gazeCtrl.bodyAlign = 1f;

                    // Hold gaze a bit longer due to body movement
                    gazeHoldTime = 0.75f;
                }
                else
                {
                    // First addressee, face them fully
                    gazeCtrl.torso.align = 1f;
                    gazeCtrl.bodyAlign = 1f;

                    // Hold gaze a bit longer due to body movement
                    gazeHoldTime = 0.75f;
                }

                gazeCtrl.useTorso = true;
                prevGazeTarget = null; // this gaze shift must get executed, since it shifts the body
            }
            
            // Set the next gaze target
            curTarget = newTarget;
            curGazeTarget = newTarget.target;
        }
        else if (turnChange)
        {
            // We are at a boundary between turns, gaze at the target of the new conversational action
            if (StateId == (int)ConversationState.Address)
            {
                // Just finished our speaking turn, gaze at an addressee
                if (curAddressAll && !addresseeList.Any(a => a.target == curGazeTarget))
                {
                    // Gaze at any addressee
                    curTarget = addresseeList[UnityEngine.Random.Range(0, addresseeList.Count)];
                    curGazeTarget = curTarget.target;

                }
                else if (!curAddressAll)
                {
                    // Gaze back at the addressee
                    curTarget = targets[curTurnTargetIndex];
                    curGazeTarget = curTarget.target;
                }
            }
            else if (listen)
            {
                // Participant just started speaking, gaze at them
                curTarget = targets[curTurnTargetIndex];
                curGazeTarget = curTarget.target;
            }
            else if (address)
            {
                // Just starting to speak, gaze at the addressee
                curTarget = targets[curTurnTargetIndex];
                curGazeTarget = curTarget.target;
            }
        }
        else
        {
            // Switch to next target within the same turn
            if (StateId == (int)ConversationState.Address && !curAddressAll)
            {
                // Addressing the same participant, keep gazing at them
                holdCurGaze = true;
            }
            else if (faceCtrl != null && (faceCtrl.doGesture || faceCtrl.StateId == (int)FaceState.Gesturing))
            {
                // Head gesture in progress, do not shift gaze
                holdCurGaze = true;
            }
            else if (StateId == (int)ConversationState.WaitForSpeech && !curAddressAll ||
                StateId == (int)ConversationState.Listen)
            {
                // Listening to a participant or waiting for them to say something, keep gazing at them
                curTarget = curTurnTargetIndex > 0 ? targets[curTurnTargetIndex] : targets[0];
                float p = UnityEngine.Random.Range(0f, 1f);
                if (p >= 0f && p < pAF1A)
                    curGazeTarget = curTarget.target;
                else if (p >= pAF1A && p < pAF1A + pAB1A)
                    curGazeTarget = curTarget.bodyTarget;
                else
                    curGazeTarget = curTarget.SelectRandomEnvironmentTarget();
            }
            else
            {
                // Pick a gaze target using the probability distribution for the current conversational party
                float p = UnityEngine.Random.Range(0f, 1f);
                if (addresseeList.Count >= 2) // 2A
                {
                    if (p >= 0f && p < pE2A)
                    {
                        if (curTarget == null)
                            curTarget = targets[UnityEngine.Random.Range(0, targets.Length)];
                        curGazeTarget = curTarget.SelectRandomEnvironmentTarget();
                    }
                    else
                    {
                        curTarget = targets[UnityEngine.Random.Range(0, targets.Length)];
                        float pF = pAF2A / (pAF2A + pAB2A);
                        curGazeTarget = UnityEngine.Random.Range(0f, 1f) < pF ? curTarget.target : curTarget.bodyTarget;
                    }
                }
                else if (addresseeList.Count == 1 && bystanderList.Count >= 1) // 1A+B
                {
                    if (p >= 0f && p < pAF1AB)
                    {
                        curTarget = addresseeList[0];
                        curGazeTarget = curTarget.target;
                    }
                    else if (p >= pAF1AB && p < pAF1AB + pAB1AB)
                    {
                        curTarget = addresseeList[0];
                        curGazeTarget = curTarget.bodyTarget;
                    }
                    else if (p >= pAF1AB + pAB1AB && p < pAF1AB + pAB1AB + pBF1AB)
                    {
                        curTarget = bystanderList[UnityEngine.Random.Range(0, bystanderList.Count)];
                        curGazeTarget = curTarget.target;
                    }
                    else if (p >= pAF1AB + pAB1AB + pBF1AB && p < pAF1AB + pAB1AB + pBF1AB + pBB1AB)
                    {
                        curTarget = bystanderList[UnityEngine.Random.Range(0, bystanderList.Count)];
                        curGazeTarget = curTarget.bodyTarget;
                    }
                    else
                    {
                        if (curTarget == null)
                            curTarget = addresseeList[0];
                        curGazeTarget = curTarget.SelectRandomEnvironmentTarget();
                    }
                }
                else if (addresseeList.Count == 1) // 1A
                {
                    curTarget = addresseeList[0];
                    if (p >= 0f && p < pAF1A)
                        curGazeTarget = curTarget.target;
                    else if (p >= pAF1A && p < pAF1A + pAB1A)
                        curGazeTarget = curTarget.bodyTarget;
                    else
                        curGazeTarget = curTarget.SelectRandomEnvironmentTarget();
                }
                else
                {
                    curTarget = null;
                    curGazeTarget = null;
                }
            }
        }

        if (!holdCurGaze && curGazeTarget != null && curGazeTarget != prevGazeTarget)
        {
            // Initiate gaze shift
            bool isFace = targets.Any(t => t.target == curGazeTarget);
            gazeCtrl.head.align = isFace ? 1f : 0f;
            //gazeCtrl.GazeAt(curTarget.GenerateGazeTargetPosition(gazeCtrl.EyeCenter, curGazeTarget));
            gazeCtrl.GazeAt(curGazeTarget);

            Debug.Log(string.Format("Initiating conversational gaze at target {0}; turnChange = {1}, newTargetAdded = {2}",
                curGazeTarget.name, turnChange, addNewTarget));
        }
        else
        {
            Debug.Log(string.Format("Holding conversational gaze at target {0}; turnChange = {1}, newTargetAdded = {2}",
                curGazeTarget != null ? curGazeTarget.name : "null", turnChange, addNewTarget));
        }
        
        // How long to hold gaze?
        MathNet.Numerics.Distributions.GammaDistribution curDist = null;
        if (addresseeList.Count >= 2) // 2A
        {
            if (curTarget.targetType == ConversationTargetType.Addressee)
            {
                if (curGazeTarget == curTarget.target)
                    curDist = dAF2A;
                else if (curGazeTarget == curTarget.bodyTarget)
                    curDist = dAB2A;
                else // environment target
                    curDist = dE2A;
            }
            else // bystander
            {
                if (curGazeTarget == curTarget.target)
                    curDist = dBF1AB;
                else if (curGazeTarget == curTarget.bodyTarget)
                    curDist = dBB1AB;
                else // environment target
                    curDist = dE1AB;
            }
        }
        else if (addresseeList.Count == 1 && bystanderList.Count >= 1) // 1A+B
        {
            if (curTarget.targetType == ConversationTargetType.Addressee)
            {
                if (curGazeTarget == curTarget.target)
                    curDist = dAF1AB;
                else if (curGazeTarget == curTarget.bodyTarget)
                    curDist = dAB1AB;
                else // environment target
                    curDist = dE1AB;
            }
            else // bystander
            {
                if (curGazeTarget == curTarget.target)
                    curDist = dBF1AB;
                else if (curGazeTarget == curTarget.bodyTarget)
                    curDist = dBB1AB;
                else // environment target
                    curDist = dE1AB;
            }
        }
        else // 1A
        {
            if (curGazeTarget == curTarget.target)
                curDist = dAF1A;
            else if (curGazeTarget == curTarget.bodyTarget)
                curDist = dAB1A;
            else // environment target
                curDist = dE1A;
        }
        gazeHoldTime += Mathf.Clamp(((float)curDist.NextDouble() + gazeDelayTime), 0f, float.MaxValue);
    }

    protected virtual void _AddNewTarget()
    {
        var targetList = new List<ConversationTarget>(targets);
        newTargetIndex = newTargetIndex < 0 ? 0 : newTargetIndex;
        if (newTargetIndex < targets.Length)
            targetList.Insert(newTargetIndex, newTarget);
        else
            targetList.Add(newTarget);
        targets = targetList.ToArray();
    }

    public override void _CreateStates()
    {
        // Initialize states
        _InitStateDefs<ConversationState>();
        _InitStateTransDefs((int)ConversationState.WaitForSpeech, 3);
        _InitStateTransDefs((int)ConversationState.Listen, 2);
        _InitStateTransDefs((int)ConversationState.Address, 1);
        _InitStateTransDefs((int)ConversationState.NewParticipant, 2);
        states[(int)ConversationState.WaitForSpeech].updateHandler = "Update_WaitForSpeech";
        states[(int)ConversationState.WaitForSpeech].nextStates[0].nextState = "Listen";
        states[(int)ConversationState.WaitForSpeech].nextStates[0].transitionHandler = "Transition_WaitForSpeechListen";
        states[(int)ConversationState.WaitForSpeech].nextStates[1].nextState = "Address";
        states[(int)ConversationState.WaitForSpeech].nextStates[1].transitionHandler = "Transition_WaitForSpeechAddress";
        states[(int)ConversationState.WaitForSpeech].nextStates[2].nextState = "NewParticipant";
        states[(int)ConversationState.WaitForSpeech].nextStates[2].transitionHandler = "Transition_WaitForSpeechNewParticipant";
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

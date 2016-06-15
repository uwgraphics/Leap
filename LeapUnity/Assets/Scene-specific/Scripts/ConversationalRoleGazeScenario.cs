using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;

public class ConversationalRoleGazeScenario : Scenario
{
    public enum FactorGazePattern
    {
        LowGaze,
        HighGaze,
    }

    public enum FactorBodyOrientation
    {
        Exclusive,
        Inclusive
    }

    public enum FactorSetting
    {
        Screen2D,
        VR
    }

    [Serializable]
    public class Condition
    {
        public FactorGazePattern gazePattern;
        public FactorBodyOrientation bodyOrientation;
        public FactorSetting setting;

        public Condition(FactorGazePattern gazePattern, FactorBodyOrientation bodyOrientation, FactorSetting setting)
        {
            this.gazePattern = gazePattern;
            this.bodyOrientation = bodyOrientation;
            this.setting = setting;
        }

        public static bool operator ==(Condition c1, Condition c2)
        {
            return c1.gazePattern == c2.gazePattern &&
                c1.bodyOrientation == c2.bodyOrientation &&
                c1.setting == c2.setting;
        }

        public static bool operator !=(Condition c1, Condition c2)
        {
            return !(c1 == c2);
        }

        public override string ToString()
        {
            return string.Format("{0}, {1}, {2}", gazePattern.ToString(), bodyOrientation.ToString(), setting.ToString());
        }
    }

    public enum ConditionAssignmentType
    {
        Fixed,
        Randomized,
        Stratified
    }

    public enum Phase
    {
        ParticipantIdEntry,
        Start,
        AskQuestion,
        WaitForAnswer,
        Farewell
    }

    public enum PartnerClass
    {
        Participant,
        Confederate
    }

    public enum PartnerSet
    {
        All,
        LastSpeaker,
        NotLastSpeaker
    }

    [Serializable]
    public class QuestionDef
    {
        public string[] statementClips = new string[0];
        public PartnerSet statementAddressee = PartnerSet.All;
        public string[] questionClips = new string[0];
        public PartnerSet questionAddressee = PartnerSet.All;
        public string[] commentClips = new string[0];
        public PartnerSet commentAddressee = PartnerSet.LastSpeaker;
    }

    [Serializable]
    public class ConfedAnswerDef
    {
        public string[] answerClips = new string[0];
    }

    /// <summary>
    /// Scenario ID.
    /// </summary>
    public int scenarioId = 0;

    /// <summary>
    /// Experimental condition.
    /// </summary>
    public Condition condition;

    /// <summary>
    /// Experimental conditions that a participant can be assigned to.
    /// </summary>
    public Condition[] activeConditions;

    /// <summary>
    /// How the participant will be assigned to a condition.
    /// </summary>
    public ConditionAssignmentType conditionAssign = ConditionAssignmentType.Stratified;

    /// <summary>
    /// The starting/current experiment phase.
    /// </summary>
    public Phase phase = Phase.ParticipantIdEntry;

    /// <summary>
    /// The name of the agent.
    /// </summary>
    public string agentName = "Jasmin";

    /// <summary>
    /// Name of the confederate agent.
    /// </summary>
    public string confedName = "Jason";

    /// <summary>
    /// How long the confederate waits before taking the floor
    /// when it was intending to speak.
    /// </summary>
    public float meanPauseTime = 0.4f;

    /// <summary>
    /// How long the confederate or agent wait before taking the floor
    /// when confederate was not intending to speak.
    /// </summary>
    public float maxPauseTime = 3f;

    /// <summary>
    /// How long the agent or confederate will wait before taking the floor
    /// after the participant has spoken.
    /// </summary>
    public float pauseTimeAfterAnswer = 1f;

    /// <summary>
    /// Questions that the agent will ask.
    /// </summary>
    public QuestionDef[] questions = new QuestionDef[0];

    /// <summary>
    /// Prerecorded confederate answers - a set of questions for
    /// every confederate.
    /// </summary>
    public ConfedAnswerDef[] confedAnswers = new ConfedAnswerDef[0];

    /// <summary>
    /// Visual indicator for when the speech recognition system is ready.
    /// </summary>
    public Texture2D canSpeakIcon = null;

    protected struct _AnswerEntry
    {
        public int questionIndex;
        public float questionEndTime;
        public float timeToSpeech;
        public float speechLength;

        public _AnswerEntry(int questionIndex, float questionEndTime, float timeToSpeech, float speechLength)
        {
            this.questionIndex = questionIndex;
            this.questionEndTime = questionEndTime;
            this.timeToSpeech = timeToSpeech;
            this.speechLength = speechLength;
        }
    }

    protected ConversationController conversationController = null;
    protected GazeController gazeController = null;
    protected SimpleListenController listenController = null;
    protected StreamWriter participantLog = null;
    protected ushort participantId = 0;
    protected string participantIdStr = "";
    protected bool addParticipant = false;
    protected List<_AnswerEntry> participantAnswers = new List<_AnswerEntry>();
    protected float scenarioStartTime;
    protected int curQuestionIndex = 0;
    protected PartnerClass lastSpeaker = PartnerClass.Participant;
    protected float lastQuestionEndTime = 0f;

    /// <summary>
    /// Hide the scene by deactivating lights and characters.
    /// </summary>
    protected virtual void _HideScene()
    {
        // Hide lights
        var lightRoot = GameObject.Find("Lights");
        var lights = lightRoot.GetComponentsInChildren<Light>();
        foreach (var light in lights)
            light.enabled = false;

        // Hide characters
        var meshRenderers = agents[agentName].GetComponentsInChildren<SkinnedMeshRenderer>();
        foreach (var meshRenderer in meshRenderers)
            meshRenderer.enabled = false;
        meshRenderers = agents[confedName].GetComponentsInChildren<SkinnedMeshRenderer>();
        foreach (var meshRenderer in meshRenderers)
            meshRenderer.enabled = false;
    }

    /// <summary>
    /// Show the scene by activating lights and characters. 
    /// </summary>
    protected virtual void _ShowScene()
    {
        // Show lights
        var lightRoot = GameObject.Find("Lights");
        var lights = lightRoot.GetComponentsInChildren<Light>();
        foreach (var light in lights)
            light.enabled = true;
        
        // Show characters
        var meshRenderers = agents[agentName].GetComponentsInChildren<SkinnedMeshRenderer>();
        foreach (var meshRenderer in meshRenderers)
            meshRenderer.enabled = true;
        meshRenderers = agents[confedName].GetComponentsInChildren<SkinnedMeshRenderer>();
        foreach (var meshRenderer in meshRenderers)
            meshRenderer.enabled = true;
    }

    // Get confederate or participant name
    protected virtual string _GetPartnerName(PartnerClass partner)
    {
        return partner == PartnerClass.Confederate ? confedName : "Participant";
    }

    // Participant if last speaker was confederate, confederate otherwise
    protected virtual PartnerClass _GetNotLastSpeaker()
    {
        return lastSpeaker == PartnerClass.Confederate ? PartnerClass.Participant : PartnerClass.Confederate;
    }

    // Get participant walk up animation script
    protected virtual ParticipantWalkUp _GetParticipantWalkUp()
    {
        return cameras["Camera"].transform.parent.GetComponent<ParticipantWalkUp>();
    }

    // Scenario execution will pause until the participant has started approaching the interaction area
    protected virtual IEnumerator _WaitUntilParticipantWalkUpStarted()
    {
        while (!addParticipant)
            yield return 0;

        yield break;
    }

    // Scenario execution will pause until the participant has arrived at its final spot
    protected virtual IEnumerator _WaitUntilParticipantArrived()
    {
        while (!_GetParticipantWalkUp().Arrived)
            yield return 0;

        yield break;
    }

    // Get the current question definition
    protected virtual QuestionDef _GetCurrentQuestion()
    {
        return questions[curQuestionIndex];
    }

    // true if the participant is eligible to answer the current question
    protected virtual bool _CanParticipantAnswerQuestion()
    {
        return _GetCurrentQuestion().questionAddressee == PartnerSet.All ||
            _GetCurrentQuestion().questionAddressee == PartnerSet.LastSpeaker && lastSpeaker == PartnerClass.Participant ||
            _GetCurrentQuestion().questionAddressee == PartnerSet.NotLastSpeaker && lastSpeaker != PartnerClass.Participant;
    }

    // true if the participant has spoken on this turn
    protected virtual bool _HasParticipantSpoken()
    {
        return listenController.StateId == (int)SimpleListenState.SpeechDetected ||
            conversationController.ListenResults.Count > 0;
    }

    // true if the participant is still holding the floor, false if they are done speaking
    protected virtual bool _IsParticipantSpeaking()
    {
        return listenController.StateId == (int)SimpleListenState.SpeechDetected ||
            conversationController.ListenResults.Count == 1 &&
            Time.timeSinceLevelLoad - conversationController.ListenResults[0].RecognizeTime <= pauseTimeAfterAnswer ||
            conversationController.ListenResults.Count > 1 &&
            conversationController.ListenResults.Any(r => Time.timeSinceLevelLoad - r.RecognizeTime <= 2f * pauseTimeAfterAnswer);
        // TODO: this simple heuristic gives the person more time to answer if they are giving a long answer
    }

    // Scenario execution will pause until the participant has finished speaking
    // or enough time has elapsed
    protected virtual IEnumerator _WaitUntilPartnerHasSpoken(float waitTime)
    {
        while (!_HasParticipantSpoken() &&  Time.timeSinceLevelLoad - lastQuestionEndTime < waitTime ||
            _HasParticipantSpoken() && _IsParticipantSpeaking())
        {
            yield return 0;
        }

        yield break;
    }

    /// <see cref="Scenario._Init()"/>
    protected override void _Init()
    {
        // Get task settings from the command line
        string[] args = System.Environment.GetCommandLineArgs();
        if (args.Length == 3)
        {
            FactorGazePattern gazePattern = (FactorGazePattern)Enum.Parse(typeof(FactorGazePattern), args[0]);
            FactorBodyOrientation bodyOrientation = (FactorBodyOrientation)Enum.Parse(typeof(FactorBodyOrientation), args[1]);
            FactorSetting setting = (FactorSetting)Enum.Parse(typeof(FactorSetting), args[2]);
            condition = new Condition(gazePattern, bodyOrientation, setting);
        }

        string participantLogPath = "ConversationalRoleGaze_Log.csv";
        if (!File.Exists(participantLogPath))
        {
            participantLog = new StreamWriter(participantLogPath, false);
            participantLog.WriteLine("participantID,scenarioID,gazePattern,bodyOrientation,setting,numSpeakTurns,meanTimeToSpeech,meanSpeakTurnLength");
            participantLog.Flush();
        }
        else
        {
            if (conditionAssign == ConditionAssignmentType.Stratified)
            {
                // Find last condition from current log entries
                StreamReader curParticipantLog = new StreamReader(participantLogPath);
                string entry = "";
                Condition lastCondition = activeConditions[activeConditions.Length - 1];
                string[] values;
                curParticipantLog.ReadLine();
                while (!curParticipantLog.EndOfStream)
                {
                    entry = curParticipantLog.ReadLine();
                    values = entry.Split(",".ToCharArray());
                    FactorGazePattern lastGazePattern = (FactorGazePattern)Enum.Parse(typeof(FactorGazePattern), values[1]);
                    FactorBodyOrientation lastBodyOrientation = (FactorBodyOrientation)Enum.Parse(typeof(FactorBodyOrientation), values[2]);
                    FactorSetting lastSetting = (FactorSetting)Enum.Parse(typeof(FactorSetting), values[3]);
                    lastCondition = new Condition(lastGazePattern, lastBodyOrientation, lastSetting);
                }
                curParticipantLog.Close();

                // Assign participant to next condition
                for (int conditionIndex = 0; conditionIndex < activeConditions.Length; ++conditionIndex)
                {
                    if (activeConditions[conditionIndex] == lastCondition)
                    {
                        condition = activeConditions[(conditionIndex + 1) % activeConditions.Length];
                        break;
                    }
                }
            }
            else if (conditionAssign == ConditionAssignmentType.Randomized)
            {
                condition = activeConditions[UnityEngine.Random.Range(0, activeConditions.Length)];
            }

            // Open the participant log for writing
            participantLog = new StreamWriter(participantLogPath, true);
        }

        // Initialize state
        phase = Phase.ParticipantIdEntry;
        participantId = 0;
        addParticipant = false;
        participantAnswers.Clear();
        scenarioStartTime = Time.timeSinceLevelLoad;
        curQuestionIndex = 0;
        lastSpeaker = PartnerClass.Confederate;
        lastQuestionEndTime = 0f;

        // Initialize the agent's conversation and listen controllers
        conversationController = agents[agentName].GetComponent<ConversationController>();
        conversationController.targets = new ConversationTarget[1];
        conversationController.targets[0] = new ConversationTarget(ConversationTargetType.Addressee,
            gazeTargets.ContainsKey(confedName + "Face") ? gazeTargets[confedName + "Face"] : null,
            gazeTargets.ContainsKey(confedName + "Body") ? gazeTargets[confedName + "Body"] : null,
            gazeTargets.ContainsKey(confedName + "EnvLeft") ? gazeTargets[confedName + "EnvLeft"] : null,
            gazeTargets.ContainsKey(confedName + "EnvRight") ? gazeTargets[confedName + "EnvRight"] : null,
            gazeTargets.ContainsKey(confedName + "EnvDown") ? gazeTargets[confedName + "EnvDown"] : null,
            gazeTargets.ContainsKey(confedName + "EnvUp") ? gazeTargets[confedName + "EnvUp"] : null
            );
        conversationController.bodyOrientationBias = -0.2f;
        listenController = agents[agentName].GetComponent<SimpleListenController>();

        // Initialize the agent's gaze controller
        gazeController = agents[agentName].GetComponent<GazeController>();
        gazeController.head.align = 0.8f;
        gazeController.useTorso = true;
        gazeController.pelvisAlign = 1f;
        gazeController.bodyAlign = 0f;
        gazeController.adjustForRootMotion = false;

        // Initialize camera
        cameras["Camera"].GetComponent<MouseLook>().enabled = condition.setting == FactorSetting.Screen2D;
        if (condition.setting == FactorSetting.Screen2D)
            cameras["Camera"].GetComponent<Camera>().fieldOfView = 45f;
    }

    /// <see cref="Scenario._Run()"/>
    protected override IEnumerator _Run()
    {
        Debug.Log("Experimental condition: " + condition.ToString());

        // Hide scene and allow the experimenter to enter the participant ID
        _HideScene();
        while (phase != Phase.Start)
            yield return 0;

        yield return StartCoroutine(_RunTask());

        yield return null;
    }

    protected virtual IEnumerator _RunTask()
    {
        // Prepare scenario
        yield return new WaitForSeconds(0.5f);
        var gazeTarget = GameObject.Find(confedName + "Face");
        int gazeId = GazeAt(agentName, gazeTarget.name, 1f, 1f);
        yield return StartCoroutine(WaitUntilFinished(gazeId));
        yield return new WaitForSeconds(0.5f);
        _ShowScene();

        // Wait for the participant's approach
        yield return StartCoroutine(_WaitUntilParticipantWalkUpStarted());
        conversationController.reorientBody = condition.bodyOrientation == FactorBodyOrientation.Inclusive;
        conversationController.AddNewTarget(new ConversationTarget(
            condition.gazePattern == FactorGazePattern.LowGaze ? ConversationTargetType.Bystander : ConversationTargetType.Addressee,
            gazeTargets["ParticipantFace"], gazeTargets["ParticipantBody"],
            gazeTargets["ParticipantEnvRight"], gazeTargets["ParticipantEnvLeft"],
            gazeTargets["ParticipantEnvDown"], gazeTargets["ParticipantEnvLeft"]), 0);
        yield return StartCoroutine(_WaitUntilParticipantArrived());
        yield return new WaitForSeconds(1f);
        phase = Phase.AskQuestion;

        while (phase != Phase.Farewell)
        {
            var question = _GetCurrentQuestion();
            bool isParticipantAddressee = question.questionAddressee == PartnerSet.All ||
                question.questionAddressee == PartnerSet.LastSpeaker && lastSpeaker == PartnerClass.Participant ||
                question.questionAddressee == PartnerSet.NotLastSpeaker && lastSpeaker != PartnerClass.Participant;

            if (phase == Phase.AskQuestion)
            {
                // Make the statements preceding the question
                if (question.statementAddressee == PartnerSet.All)
                    conversationController.Address(question.statementClips);
                else
                    conversationController.Address(question.statementAddressee == PartnerSet.LastSpeaker ?
                        _GetPartnerName(lastSpeaker) : _GetPartnerName(_GetNotLastSpeaker()),
                        question.statementClips);
                yield return StartCoroutine(
                    WaitForControllerState(agentName, "ConversationController", "Address"));
                yield return StartCoroutine(
                    WaitForControllerState(agentName, "ConversationController", "WaitForSpeech"));

                // Ask the current question
                if (question.questionAddressee == PartnerSet.All)
                    conversationController.Address(question.questionClips);
                else
                    conversationController.Address(question.questionAddressee == PartnerSet.LastSpeaker ?
                        _GetPartnerName(lastSpeaker) + "Face" : _GetPartnerName(_GetNotLastSpeaker()) + "Face",
                        question.questionClips);
                yield return StartCoroutine(
                    WaitForControllerState(agentName, "ConversationController", "Address"));
                yield return StartCoroutine(
                    WaitForControllerState(agentName, "ConversationController", "WaitForSpeech"));

                lastQuestionEndTime = Time.timeSinceLevelLoad;
                phase = Phase.WaitForAnswer;
            }

            if (phase == Phase.WaitForAnswer)
            {
                // Wait for an addressee's answer to the question
                if (question.questionAddressee == PartnerSet.All)
                {
                    // Will the confederate speak on this turn?
                    bool confedWillSpeak = UnityEngine.Random.Range(0, 2) != 0;
                    float confedSpeakTime = confedWillSpeak ?
                        UnityEngine.Random.Range(meanPauseTime - 0.1f, meanPauseTime + 0.1f) : maxPauseTime;

                    // Wait until participant has spoken or it's time for the confederate to speak
                    yield return StartCoroutine(_WaitUntilPartnerHasSpoken(confedSpeakTime));
                    bool confedMustSpeak = conversationController.ListenResults.Count <= 0;
                    if (confedMustSpeak)
                    {
                        // Confederate must speak
                        conversationController.Listen(confedName + "Face");
                        var confedSpeechController = agents[confedName].GetComponent<SpeechController>();
                        // TODO: this hack is to ensure that confederate audio is restored after cutting out
                        agents[confedName].GetComponent<AudioSource>().enabled = true;
                        agents[confedName].GetComponent<OVRLipSyncContextMorphTarget>().enabled = true;
                        agents[confedName].GetComponent<OVRLipSyncContext>().enabled = true;
                        //
                        foreach (string confedAnswerClip in confedAnswers[curQuestionIndex].answerClips)
                        {
                            confedSpeechController.Speak(confedAnswerClip);
                            yield return StartCoroutine(
                                WaitForControllerState(confedName, "SpeechController", "Speaking"));
                            yield return StartCoroutine(
                                WaitForControllerState(confedName, "SpeechController", "NoSpeech"));
                        }
                        yield return new WaitForSeconds(pauseTimeAfterAnswer);
                    }
                    lastSpeaker = !confedMustSpeak ? PartnerClass.Participant : PartnerClass.Confederate;
                }
                else
                {
                    // Wait for the (single) addressee to answer the question
                    PartnerClass addressee = question.questionAddressee == PartnerSet.LastSpeaker ?
                        lastSpeaker : _GetNotLastSpeaker();
                    if (addressee == PartnerClass.Confederate)
                    {
                        // Confederate is the addressee and must speak
                        float confedSpeakTime = UnityEngine.Random.Range(meanPauseTime - 0.1f, meanPauseTime + 0.1f);
                        yield return new WaitForSeconds(confedSpeakTime);
                        conversationController.Listen(confedName + "Face");
                        var confedSpeechController = agents[confedName].GetComponent<SpeechController>();
                        // TODO: this hack is to ensure that confederate audio is restored after cutting out
                        agents[confedName].GetComponent<AudioSource>().enabled = true;
                        agents[confedName].GetComponent<OVRLipSyncContextMorphTarget>().enabled = true;
                        agents[confedName].GetComponent<OVRLipSyncContext>().enabled = true;
                        //
                        foreach (string confedAnswerClip in confedAnswers[curQuestionIndex].answerClips)
                        {
                            confedSpeechController.Speak(confedAnswerClip);
                            yield return StartCoroutine(
                                WaitForControllerState(confedName, "SpeechController", "Speaking"));
                            yield return StartCoroutine(
                                WaitForControllerState(confedName, "SpeechController", "NoSpeech"));
                        }
                        lastSpeaker = PartnerClass.Confederate;
                        yield return new WaitForSeconds(pauseTimeAfterAnswer);
                    }
                    else
                    {
                        // Wait for the participant to speak
                        yield return StartCoroutine(_WaitUntilPartnerHasSpoken(maxPauseTime));
                        lastSpeaker = PartnerClass.Participant;
                    }
                }
                // TODO: this hack is to ensure that confederate audio is restored after cutting out
                agents[confedName].GetComponent<AudioSource>().enabled = false;
                agents[confedName].GetComponent<OVRLipSyncContextMorphTarget>().enabled = false;
                agents[confedName].GetComponent<OVRLipSyncContext>().enabled = false;
                //

                // If the participant had the opportunity to speak, log information about their answer
                if (isParticipantAddressee)
                {
                    if (conversationController.ListenResults.Count > 0)
                    {
                        // Participant did speak
                        float speechStartTime = conversationController.ListenResults[0].DetectTime;
                        float speechEndTime = conversationController
                            .ListenResults[conversationController.ListenResults.Count - 1].RecognizeTime;
                        participantAnswers.Add(new _AnswerEntry(curQuestionIndex, lastQuestionEndTime,
                            speechStartTime - lastQuestionEndTime, speechEndTime - speechStartTime));
                    }
                    else
                    {
                        // Participant did not speak
                        participantAnswers.Add(new _AnswerEntry(curQuestionIndex, lastQuestionEndTime, -1f, 0f));
                    }
                }

                if (question.commentClips.Length > 0)
                {
                    // Agent will now utter comments on the partner's answer
                    if (question.commentAddressee == PartnerSet.All)
                        conversationController.Address(question.commentClips);
                    else
                        conversationController.Address(question.commentAddressee == PartnerSet.LastSpeaker ?
                            _GetPartnerName(lastSpeaker) + "Face" : _GetPartnerName(_GetNotLastSpeaker()) + "Face",
                            question.commentClips);
                    yield return StartCoroutine(
                        WaitForControllerState(agentName, "ConversationController", "Address"));
                    yield return StartCoroutine(
                        WaitForControllerState(agentName, "ConversationController", "WaitForSpeech"));
                }

                // Move on to the next question
                ++curQuestionIndex;
                if (curQuestionIndex >= questions.Length)
                    phase = Phase.Farewell;
                else
                    phase = Phase.AskQuestion;
            }
        }

        // We are done!
        yield return new WaitForSeconds(1f);
    }

    /// <see cref="Scenario._Finish()"/>
    protected override void _Finish()
    {
        if (phase != Phase.Farewell)
            return;

        // Write participant answer data
        var answerLog = new StreamWriter(string.Format("ConversationalRoleGaze_{0}.csv", participantId));
        answerLog.WriteLine("participantID,scenarioID,gazePattern,bodyOrientation,setting,questionIndex,questionEndTime,timeToSpeech,speechLength");
        foreach (var answerEntry in participantAnswers)
        {
            answerLog.WriteLine(string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8}", participantId, scenarioId,
                condition.gazePattern.ToString(), condition.bodyOrientation.ToString(), condition.setting.ToString(),
                answerEntry.questionIndex, answerEntry.questionEndTime, answerEntry.timeToSpeech, answerEntry.speechLength));
        }
        answerLog.Close();

        // Aggregate measures
        int numSpeakTurnsWhenAddressee = 0, numSpeakTurnsAll = 0;
        float meanTimeToSpeech = 0f, meanSpeakTurnLength = 0f;
        foreach (var answerEntry in participantAnswers)
        {
            if (questions[answerEntry.questionIndex].questionAddressee == PartnerSet.All &&
                answerEntry.timeToSpeech >= 0f)
                ++numSpeakTurnsWhenAddressee;

            if (answerEntry.timeToSpeech >= 0f)
            {
                ++numSpeakTurnsAll;
                meanTimeToSpeech += answerEntry.timeToSpeech;
                meanSpeakTurnLength += answerEntry.speechLength;
            }
        }
        meanTimeToSpeech = numSpeakTurnsAll > 0 ? meanTimeToSpeech / numSpeakTurnsAll : 0f;
        meanSpeakTurnLength = numSpeakTurnsAll > 0 ? meanSpeakTurnLength / numSpeakTurnsAll : 0f;

        // Write results into participant log
        participantLog.WriteLine(string.Format("{0},{1},{2},{3},{4},{5},{6},{7}", participantId, scenarioId,
            condition.gazePattern.ToString(), condition.bodyOrientation.ToString(), condition.setting.ToString(),
            numSpeakTurnsWhenAddressee, meanTimeToSpeech, meanSpeakTurnLength));
        participantLog.Close();
    }

    protected virtual void Update()
    {
        // Process movement controls
        GameObject cam = GameObject.FindGameObjectWithTag("MainCamera");
        if (phase == Phase.Start && (Input.GetKeyDown(KeyCode.UpArrow) ||
            Input.GetKeyDown(KeyCode.Space) ||
            Input.GetKeyDown(KeyCode.Mouse0) ||
            Input.GetKeyDown(KeyCode.Mouse1)))
            _GetParticipantWalkUp().walkUp = true;

        // Has the participant joined the interaction?
        if (_GetParticipantWalkUp().walkUp && _GetParticipantWalkUp().WalkUpTime >= 1f)
            addParticipant = true;
    }

    protected virtual void OnGUI()
    {
        GUI.skin.box.normal.textColor = new Color(1, 1, 1, 1);
        GUI.skin.box.wordWrap = true;
        GUI.skin.box.alignment = TextAnchor.UpperLeft;
        GUI.skin.box.fontSize = 20;
        GUI.skin.button.fontSize = 20;
        GUI.skin.textField.fontSize = 20;

        if (phase == Phase.ParticipantIdEntry)
        {
            bool participantIdEntered = false;
            if (Event.current.isKey && Event.current.keyCode == KeyCode.Return)
                participantIdEntered = true;

            int width = condition.setting == FactorSetting.Screen2D ? Screen.width : 1280;
            int height = condition.setting == FactorSetting.Screen2D ? Screen.height : 1200;
            GUI.Box(new Rect(width / 2 - 150, height / 2 - 75, 300, 150),
                "Enter participant number:");
            GUI.SetNextControlName("txtParticipantId");
            participantIdStr = GUI.TextField(new Rect(width / 2 - 130, height / 2 - 30, 260, 40),
                participantIdStr);
            GUI.FocusControl("txtParticipantId");
            if (participantIdEntered)
            {
                phase = Phase.Start;
                participantId = ushort.Parse(participantIdStr);
            }
        }
        else if (phase == Phase.Start && !_GetParticipantWalkUp().walkUp)
        {
            int width = condition.setting == FactorSetting.Screen2D ? Screen.width : 1280;
            int height = condition.setting == FactorSetting.Screen2D ? Screen.height : 1200;
            GUI.Box(new Rect(width / 2 - 240, height / 2 - 30, 480, 40), "Press LEFT MOUSE BUTTON to approach.");
        }
        else if (phase == Phase.WaitForAnswer &&
            (conversationController.StateId == (int)ConversationState.WaitForSpeech ||
            _HasParticipantSpoken() && conversationController.StateId == (int)ConversationState.Listen))
        {
            int width = condition.setting == FactorSetting.Screen2D ? Screen.width : 1280;
            int height = condition.setting == FactorSetting.Screen2D ? Screen.height : 1200;
            float hpos = (width - 50) / 2;
            float vpos = height - (condition.setting == FactorSetting.Screen2D ? 120 : 360);
            GUI.DrawTexture(new Rect(hpos, vpos, 50, 100), canSpeakIcon, ScaleMode.StretchToFill, true, 0);
        }
    }
}

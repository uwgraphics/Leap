using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class ConversationalRoleGazeScenario : Scenario
{
    public enum FactorGazeType
    {
        LowGaze,
        HighGaze,
        BodyGaze
    };

    public enum FactorSettingType
    {
        Screen2D,
        VR
    };

    public enum ConditionAssignmentType
    {
        Fixed,
        Randomized,
        Stratified
    };

    public enum Phase
    {
        PartIdEntry,
        Start,
        ShowNextPainting,
        PromptQuestion,
        WaitForQuestion,
        AnswerQuestion,
        ToNextPerson,
        ToNextPainting,
        PromptNextPerson,
        CannotUnderstand,
        Farewell
    };

    public enum PartnerClass
    {
        Participant,
        Confederate
    };

    [Serializable]
    public class ConfedQuestionMapping
    {
        public string[] questionClips = new string[0];
    }

    /// <summary>
    /// Experimental condition.
    /// </summary>
    public FactorGazeType gazeTypeCondition = FactorGazeType.LowGaze;

    /// <summary>
    /// How the participant will be assigned to a condition.
    /// </summary>
    public ConditionAssignmentType conditionAssign = ConditionAssignmentType.Stratified;

    /// <summary>
    /// Duration of the experiment (in seconds).
    /// </summary>
    public float expDuration = 270f;

    /// <summary>
    /// The starting/current experiment phase.
    /// </summary>
    public Phase expPhase = Phase.PartIdEntry;

    /// <summary>
    /// The name of the agent.
    /// </summary>
    public string agentName = "Jasmin";

    /// <summary>
    /// Names of confederate embodiments.
    /// </summary>
    public string[] confedNames = new string[0];

    /// <summary>
    /// Available locations for participant and confederates.
    /// </summary>
    public GameObject[] partLocations = new GameObject[0];

    /// <summary>
    /// Animations for moving the camera up to each location.
    /// </summary>
    public string[] partWalkUpAnims = new string[0];

    /// <summary>
    /// How long to wait before agent moves to next painting or
    /// the experiment is terminated.
    /// </summary>
    public float maxSilenceTime = 4f;

    /// <summary>
    /// Keywords for questions the participant can ask.
    /// </summary>
    public string[] questionKeywords = new string[0];

    /// <summary>
    /// Prerecorded confederate questions - a set of questions for
    /// every confederate.
    /// </summary>
    public ConfedQuestionMapping[] confedQuestionsMap = new ConfedQuestionMapping[0];

    /// <summary>
    /// Visual indicator for when speech recognition system is ready.
    /// </summary>
    public Texture2D canSpeakIcon = null;

    protected ConversationController convCtrl = null;
    protected SimpleListenController listenCtrl = null;
    protected GazeController gazeCtrl = null;
    protected Queue<Texture2D> imageQueue = new Queue<Texture2D>();
    protected Queue<Texture2D> imagesSeen = new Queue<Texture2D>();
    protected StreamWriter expLog = null;
    protected ushort partId;
    protected int partNumInterrupts;
    protected int partNumQuestions;
    protected float partInteractTime;
    protected Dictionary<PartnerClass, string> partLocationsAssigned =
        new Dictionary<PartnerClass, string>();
    protected bool partAtLocation = false;
    protected bool partApproaching = false;
    protected float partApproachTime = 0;
    protected PartnerClass curPart;
    protected PartnerClass prevPart;
    protected Texture2D curImage = null;
    protected float expStartTime = 0;
    protected float lastUtteranceTime = 0;
    protected bool partSpeaking = false;
    protected string questionAsked = "NextPainting";
    protected int numQuestionsPainting = 0;
    protected int numQuestionsPart = 0;
    protected int numCannotUnderstand = 0;
    protected HashSet<string> questionsAskedPainting = new HashSet<string>();
    protected bool confedWillSpeak = false;
    protected float confedSpeakTime = 0;
    // Participant ID entry
    protected string partIdStr = "";

    /// <summary>
    /// Hide the scene by deactivating lights and unlit objects.
    /// </summary>
    protected virtual void HideScene()
    {
        SetObjectActive("SpotLight", false);
        SetObjectActive("PointLight1", false);
        SetObjectActive("PointLight2", false);
        SetObjectActive("Room", false);
        // Hide characters
        SkinnedMeshRenderer[] smr_list = agents[agentName].GetComponentsInChildren<SkinnedMeshRenderer>();
        foreach (SkinnedMeshRenderer smr in smr_list)
            smr.enabled = false;
    }

    /// <summary>
    /// Show the scene by activating lights and unlit objects. 
    /// </summary>
    protected virtual void ShowScene()
    {
        SetObjectActive("SpotLight", true);
        SetObjectActive("PointLight1", true);
        SetObjectActive("PointLight2", true);
        SetObjectActive("Room", true);
        // Hide characters
        SkinnedMeshRenderer[] smr_list = agents[agentName].GetComponentsInChildren<SkinnedMeshRenderer>();
        foreach (SkinnedMeshRenderer smr in smr_list)
            smr.enabled = true;
    }

    /// <summary>
    /// Have an agent gaze at something.
    /// </summary>
    /// <param name='agentName'>
    /// Agent name.
    /// </param>
    /// <param name="part">
    /// Which participant or confederate.
    /// </param>
    /// <param name="headAlign">
    /// Head alignment.
    /// </param>
    /// <param name="torsoAlign">
    /// Torso alignment.
    /// </param>
    /// <returns>Action ID</returns>
    public virtual int GazeAt(string agentName, PartnerClass part,
        float headAlign, float torsoAlign)
    {
        GazeController gctrl = agents[agentName].GetComponent<GazeController>();
        gctrl.useTorso = true;

        return GazeAt(agentName, partLocationsAssigned[part], headAlign, torsoAlign);
    }

    /// <summary>
    /// Scenario execution will pause until a participant speaks.
    /// </summary>
    /// <returns>
    protected virtual IEnumerator WaitUntilParticipantSpeaking()
    {
        // Will the confederate speak on this turn?
        confedWillSpeak = UnityEngine.Random.Range(0f, 1f) > (numQuestionsPainting % 6 + 1f) / 8f;
        confedSpeakTime = Time.timeSinceLevelLoad + 0.2f + UnityEngine.Random.Range(0f, 1f) * (maxSilenceTime - 0.2f);

        while (Time.timeSinceLevelLoad - lastUtteranceTime < maxSilenceTime)
        {
            if (listenCtrl.SpeechDetected)
            {
                Debug.Log("(1) Participant");

                prevPart = curPart;
                curPart = PartnerClass.Participant;
                partSpeaking = true;
                if (prevPart != curPart)
                {
                    ++partNumInterrupts;
                    numQuestionsPart = 0;
                }

                yield break;
            }
            else if (confedWillSpeak && Time.timeSinceLevelLoad >= confedSpeakTime)
            {
                Debug.Log("(2) Confederate");

                prevPart = curPart;
                curPart = PartnerClass.Confederate;
                partSpeaking = true;

                yield break;
            }

            yield return 0;
        }

        partSpeaking = false;
        yield break;
    }

    /// <summary>
    /// Scenario execution will pause until a participant has asked a question.
    /// </summary>
    protected virtual IEnumerator WaitUntilQuestionAsked()
    {
        while (Time.timeSinceLevelLoad - lastUtteranceTime < maxSilenceTime)
        {
            questionAsked = "Unintelligible";

            if (curPart != PartnerClass.Participant)
            {
                if (numQuestionsPainting >= 6)
                    questionAsked = "NextPainting";
                else
                {
                    do
                        questionAsked = questionKeywords[UnityEngine.Random.Range(0, 6)];
                    while (questionsAskedPainting.Contains(questionAsked));
                }
            }
            else if (listenCtrl.SpeechRecognized)
            {
                // Participant asked a question - which question is it?
                string phrase = listenCtrl.RecognizedPhrase;
                int phrase_i = 0;
                for (; phrase_i < listenCtrl.phrases.Length;
                    ++phrase_i)
                    if (listenCtrl.phrases[phrase_i] == phrase)
                        break;

                questionAsked = phrase_i < questionKeywords.Length ?
                    questionKeywords[phrase_i] : "NextPainting";
            }
            else if (listenCtrl.SpeechRejected)
            {
                // Participant said something unintelligible
                questionAsked = "Unintelligible";
            }

            if (questionAsked != "Unintelligible")
            {
                ++numQuestionsPart;
                yield break;
            }

            yield return 0;
        }

        questionAsked = "Unintelligible";
        yield break;
    }

    /// <summary>
    /// Scenario execution will pause until the participant has started
    /// approaching the interaction area.
    /// </summary>
    protected virtual IEnumerator WaitForPartApproaching()
    {
        while (!partApproaching)
            yield return 0;

        yield break;
    }

    /// <summary>
    /// Finds the index of the question.
    /// </summary>
    /// <returns>
    /// The question index.
    /// </returns>
    /// <param name='qkw'>
    /// Question keyword.
    /// </param>
    protected virtual int FindQuestionIndex(string qkw)
    {
        for (int qi = 0; qi < questionKeywords.Length; ++qi)
            if (qkw == questionKeywords[qi])
                return qi;

        return 0;
    }

    /// <see cref="Scenario._Init()"/>
    protected override void _Init()
    {
        // TODO: remove this
        return;
        //
        // Get task settings from command line
        string[] args = System.Environment.GetCommandLineArgs();
        if (args.Length > 1)
            gazeTypeCondition = (FactorGazeType)Enum.Parse(typeof(FactorGazeType), args[2]);

        string logfilename = "ConversationalRoleGaze_Log.csv";
        if (!File.Exists(logfilename))
        {
            expLog = new StreamWriter(logfilename, false);
            expLog.WriteLine("partID,location,condition,images,numQuestions,interactTime,avgProximity,timeTaken");
            expLog.Flush();
        }
        else
        {
            // Iterate through current log entries
            StreamReader old_log = new StreamReader(logfilename);
            string entry = "";
            int num_part = 0;
            FactorGazeType last_cond = FactorGazeType.LowGaze;
            string[] values;
            old_log.ReadLine();
            while (!old_log.EndOfStream)
            {
                entry = old_log.ReadLine();
                ++num_part;

                values = entry.Split(",".ToCharArray());
                last_cond = (FactorGazeType)Enum.Parse(
                    typeof(FactorGazeType), values[2]
                    );
            }
            old_log.Close();

            // Assign participant to a condition
            if (conditionAssign == ConditionAssignmentType.Randomized)
            {
                gazeTypeCondition = UnityEngine.Random.Range(0, 2) > 0 ?
                    FactorGazeType.HighGaze : FactorGazeType.BodyGaze;
            }
            else if (conditionAssign == ConditionAssignmentType.Stratified)
            {
                int cond_i = ((int)last_cond + 1) %
                    Enum.GetValues(typeof(FactorGazeType)).Length;
                gazeTypeCondition = (FactorGazeType)cond_i;
            }

            // Generate new participant ID
            /*values = entry.Split(",".ToCharArray());
            if( values != null && values[0] != "" )
                partId = (ushort)(ushort.Parse(values[0])+1);
            else
                partId = 1;*/

            // Open the log for writing
            expLog = new StreamWriter(logfilename, true);
        }

        // Initialize measurements
        partNumInterrupts = 0;
        partNumQuestions = 0;
        partInteractTime = 0;
        expStartTime = Time.timeSinceLevelLoad;

        // Decide which characters to show
        SetObjectActive(confedNames[((int)PartnerClass.Confederate) - 1], false);

        // Assign locations
        List<GameObject> avlocs = new List<GameObject>(partLocations);
        avlocs.RemoveAt(avlocs.Count - 1);
        foreach (int part_i in Enum.GetValues(typeof(PartnerClass)))
        {
            if (avlocs.Count <= 0)
                break;

            int loc_i = UnityEngine.Random.Range(0, avlocs.Count);
            GameObject loc = avlocs[loc_i];
            avlocs.RemoveAt(loc_i);
            partLocationsAssigned.Add((PartnerClass)part_i, loc.name);
        }
        partAtLocation = false;

        // Position camera
        GameObject cam = GameObject.FindGameObjectWithTag("MainCamera");
        cam.transform.position =
            gazeTargets[partLocationsAssigned[PartnerClass.Participant] + "Start"].transform.position;
        cam.transform.rotation =
            gazeTargets[partLocationsAssigned[PartnerClass.Participant] + "Start"].transform.rotation;
        cam.GetComponent<MouseLook>().initialX = cam.transform.localEulerAngles.y;

        // Position confederates
        foreach (int conf_i in Enum.GetValues(typeof(PartnerClass)))
        {
            if (conf_i == 0)
                // Not the participant, just embodied confederates
                continue;

            GameObject conf = agents[confedNames[conf_i - 1]];
            if (!partLocationsAssigned.ContainsKey((PartnerClass)conf_i))
                continue;
            GameObject loc = gazeTargets[partLocationsAssigned[(PartnerClass)conf_i]];
            Vector3 pos = conf.transform.position;
            pos.x = loc.transform.position.x;
            pos.z = loc.transform.position.z;
            conf.transform.position = pos;
            Vector3 rot = conf.transform.eulerAngles;
            rot.y = loc.transform.eulerAngles.y;
            conf.transform.eulerAngles = rot;
        }

        // Initialize agent's conversation controller
        convCtrl = agents[agentName].GetComponent<ConversationController>();
        convCtrl.targets = new GameObject[1];
        convCtrl.targets[0] = gazeTargets[partLocationsAssigned[PartnerClass.Confederate]];
        convCtrl.prefTarget = gazeTypeCondition == FactorGazeType.LowGaze ?
            gazeTargets[partLocationsAssigned[PartnerClass.Confederate]] : null;
        convCtrl.prefTorsoAlign = (gazeTypeCondition == FactorGazeType.BodyGaze) ?
            0f : 0.6f;
        convCtrl.ackNewTarget = true;
        convCtrl.addressAll = true;
        convCtrl.prefTorsoAlign = 0f;
        convCtrl.listenTarget = convCtrl.addressTarget = null;
        convCtrl.defaultTorsoAlign = 0f;

        // Initialize agent's gaze controller
        gazeCtrl = agents[agentName].GetComponent<GazeController>();
        gazeCtrl.head.align = 0.6f;
        gazeCtrl.useTorso = true;

        // Initialize agent's listen controller
        listenCtrl = agents[agentName].GetComponent<SimpleListenController>();
        listenCtrl.LaunchServer();
        listenCtrl.ConnectToServer();
    }

    /// <see cref="Scenario._Run()"/>
    protected override IEnumerator _Run()
    {
        Debug.Log("Experimental condition: " + gazeTypeCondition);

        /*// Hide scene and allow the experimenter
        // to enter the participant number
        HideScene();
        while (expPhase != Phase.Start)
            yield return 0;

        yield return StartCoroutine(_RunTask());*/

        yield return new WaitForSeconds(2f);
        GazeAt("Jasmin", "Ahead", 1f, 0f, 0f);
        yield return new WaitForSeconds(2f);
        GazeAt("Jasmin", "UpperLeft", 1f, 1f, 1f);
        yield return new WaitForSeconds(2f);
        GazeAt("Jasmin", "Ahead", 1f, 1f, 1f);
        yield return new WaitForSeconds(2f);
        GazeAt("Jasmin", "UpperLeft", 1f, 1f, 1f);
        yield return new WaitForSeconds(2f);

        yield return null;
    }

    protected virtual IEnumerator _RunTask()
    {
        int speech_id = -1;
        int gaze_id = -1;

        // Prepare scenario
        yield return new WaitForSeconds(0.5f);
        gaze_id = GazeAt(agentName, PartnerClass.Confederate, 1f, 0.7f);
        curPart = PartnerClass.Confederate;
        convCtrl.targetDummy.transform.position =
            gazeTargets[partLocationsAssigned[curPart]].transform.position;
        convCtrl.addressTarget = gazeTargets[partLocationsAssigned[curPart]];
        yield return StartCoroutine(WaitUntilFinished(gaze_id));
        convCtrl.Introduce(null);
        yield return new WaitForSeconds(0.5f);
        ShowScene();

        // Wait for participant to approach
        yield return StartCoroutine(WaitForPartApproaching());
        yield return new WaitForSeconds(1f);
        expPhase = Phase.ShowNextPainting;

        while (expPhase != Phase.Farewell)
        {
            if (expPhase == Phase.ShowNextPainting)
            {
                if (convCtrl.StateId != (int)ConversationState.Listen)
                    convCtrl.Listen(partLocationsAssigned[curPart], 0.01f);
                if (numQuestionsPainting >= questionKeywords.Length)
                {
                    // Indicate that a new painting is about to be shown
                    yield return new WaitForSeconds(1f);
                    speech_id = Speak(agentName, "ToNextPainting2");
                    yield return StartCoroutine(WaitUntilFinished(speech_id));
                }
                numQuestionsPainting = 0;
                questionsAskedPainting.Clear();

                // Introduce the next painting
                yield return new WaitForSeconds(0.5f);
                convCtrl.Address(/*clips*/null);
                yield return StartCoroutine(
                    WaitForControllerState(agentName, "ConversationController", "Address")
                    );
                yield return StartCoroutine(
                    WaitForControllerState(agentName, "ConversationController", "WaitForSpeech")
                    );

                expPhase = Phase.WaitForQuestion;
            }

            if (expPhase == Phase.WaitForQuestion)
            {
                // Wait for question from participant/confederate

                listenCtrl.Listen();

                lastUtteranceTime = Time.timeSinceLevelLoad;
                yield return StartCoroutine(WaitUntilParticipantSpeaking());

                if (!partSpeaking)
                {
                    // Prompt the participants to speak
                    speech_id = Speak(agentName, "AnyoneHaveQuestions", SpeechType.Question);
                    yield return StartCoroutine(WaitUntilFinished(speech_id));
                    lastUtteranceTime = Time.timeSinceLevelLoad;
                    yield return StartCoroutine(WaitUntilParticipantSpeaking());
                }

                if (!partSpeaking)
                {
                    // No one is asking anything, move on to next painting
                    // or say goodbye

                    listenCtrl.StopListening();

                    if (Time.timeSinceLevelLoad - expStartTime > expDuration)
                        expPhase = Phase.Farewell;
                    else
                        expPhase = Phase.ToNextPainting;
                }
                else
                {
                    // Someone (participant or confederate) is speaking

                    lastUtteranceTime = Time.timeSinceLevelLoad;
                    convCtrl.addressAll = false;
                    convCtrl.addressTarget = gazeTargets[partLocationsAssigned[curPart]];

                    if (curPart != PartnerClass.Participant)
                    {
                        yield return StartCoroutine(WaitUntilQuestionAsked());
                        int qi = FindQuestionIndex(questionAsked);
                        speech_id = Speak(confedNames[((int)curPart) - 1],
                            confedQuestionsMap[((int)curPart) - 1].questionClips[qi]);
                        yield return new WaitForSeconds(0.2f);
                        convCtrl.Listen(partLocationsAssigned[curPart], 1f);
                        yield return StartCoroutine(WaitUntilFinished(speech_id));
                    }
                    else
                    {
                        convCtrl.Listen(partLocationsAssigned[curPart], 1f);
                        yield return StartCoroutine(WaitUntilQuestionAsked());
                    }
                    partSpeaking = false;

                    if (questionAsked == "Unintelligible")
                    {
                        if (numCannotUnderstand < 1)
                            expPhase = Phase.CannotUnderstand;
                        else
                            expPhase = Phase.ToNextPainting;
                    }
                    else
                        expPhase = Phase.AnswerQuestion;

                    listenCtrl.StopListening();
                }
            }

            if (expPhase == Phase.AnswerQuestion)
            {
                numCannotUnderstand = 0;

                // Respond/react to participant/confederate utterance
                if (questionAsked == "NextPainting")
                {
                    if (imageQueue.Count > 0)
                        // Participant has asked to see the next painting
                        expPhase = Phase.ShowNextPainting;
                    else
                        // There are no more paintings
                        // TODO: should say something along those lines before quitting
                        expPhase = Phase.Farewell;
                }
                else
                {
                    // Answer the question posed by participant/confederate

                    yield return new WaitForSeconds(0.3f);

                    // What question did they ask?
                    int qi = FindQuestionIndex(questionAsked);
                    if (curPart == PartnerClass.Participant)
                        ++partNumQuestions;
                    ++numQuestionsPainting;
                    questionsAskedPainting.Add(questionAsked);

                    // Have the agent answer the question
                    convCtrl.Address(/*clips*/null);
                    yield return StartCoroutine(
                        WaitForControllerState(agentName, "ConversationController", "WaitForSpeech")
                        );
                    convCtrl.addressAll = true;

                    if (Time.timeSinceLevelLoad - expStartTime > expDuration)
                        expPhase = Phase.Farewell;
                    else if (numQuestionsPainting >= questionKeywords.Length)
                        expPhase = Phase.ShowNextPainting;
                    else
                        expPhase = Phase.WaitForQuestion;
                }
            }
            else if (expPhase == Phase.ToNextPainting)
            {
                // No more questions, so move on to another painting

                numCannotUnderstand = 0;

                convCtrl.Listen(partLocationsAssigned[curPart], 0.01f);
                yield return StartCoroutine(
                    WaitForControllerState(agentName, "ConversationController", "Listen")
                    );
                string[] clips = { "ToNextPainting1" };
                convCtrl.Address(clips);
                convCtrl.addressAll = true;
                yield return StartCoroutine(
                    WaitForControllerState(agentName, "ConversationController", "Address")
                    );
                yield return StartCoroutine(
                    WaitForControllerState(agentName, "ConversationController", "WaitForSpeech")
                    );

                expPhase = Phase.ShowNextPainting;
            }
            else if (expPhase == Phase.CannotUnderstand)
            {
                yield return new WaitForSeconds(0.3f);

                ++numCannotUnderstand;
                string[] clips = { "RepeatQuestion" };
                convCtrl.Address(clips);
                convCtrl.addressAll = false;
                yield return StartCoroutine(
                    WaitForControllerState(agentName, "ConversationController", "WaitForSpeech")
                    );
                convCtrl.addressAll = true;

                expPhase = Phase.WaitForQuestion;
            }
        }

        // Say goodbye to participants
        convCtrl.addressAll = true;
        yield return new WaitForSeconds(1f);
        speech_id = Speak(agentName, "Farewell", SpeechType.Question);
        yield return StartCoroutine(WaitUntilFinished(speech_id));
        yield return new WaitForSeconds(0.5f);
    }

    /// <see cref="Scenario._Finish()"/>
    protected override void _Finish()
    {
        if (expPhase != Phase.Farewell)
            return;

        // Which images did the participants see?
        string imgstr = "\"";
        foreach (Texture2D img in imagesSeen)
            imgstr += (img.name + ",");
        imgstr += "\"";

        // Write log entry
        expLog.WriteLine(string.Format("{0},{1},{2},{3},{4},{5},{6},{7}",
            partId, partLocationsAssigned[PartnerClass.Participant],
            gazeTypeCondition.ToString(), imgstr, partNumQuestions, partInteractTime,
            0, Time.timeSinceLevelLoad - expStartTime));
        expLog.Close();

        // Take a screenshot
        if (!Directory.Exists("./Screenshots"))
            Directory.CreateDirectory("./Screenshots");
        string filename = string.Format("./Screenshots/scr-{0}-{1}.png", partId, gazeTypeCondition);
        Application.CaptureScreenshot(filename);
    }

    protected virtual void Update()
    {
        // TODO: remove this
        return;
        //
        GameObject cam = GameObject.FindGameObjectWithTag("MainCamera");
        if ((Input.GetKeyDown(KeyCode.UpArrow) ||
            Input.GetKeyDown(KeyCode.Space) ||
            Input.GetKeyDown(KeyCode.Mouse0) ||
            Input.GetKeyDown(KeyCode.Mouse1)) && !partAtLocation &&
            expPhase == Phase.Start)
            partApproaching = true;

        if (partApproaching)
        {
            partApproachTime += Time.deltaTime;

            int loc_i = 0;
            for (; loc_i < partLocations.Length; ++loc_i)
                if (partLocations[loc_i].name ==
                    partLocationsAssigned[PartnerClass.Participant])
                    break;
            string anim = partWalkUpAnims[loc_i];
            if (partApproachTime >= 0.5f && !cam.GetComponent<Animation>()[anim].enabled)
            {
                cam.GetComponent<Animation>()[anim].enabled = true;
                cam.GetComponent<Animation>()[anim].blendMode = AnimationBlendMode.Blend;
                cam.GetComponent<Animation>()[anim].time = 0;
                cam.GetComponent<Animation>()[anim].weight = 1f;
                cam.GetComponent<Animation>()[anim].speed = 0.7f;

                partApproachTime = 0;
            }


            if (partApproachTime >= 1f)
            {
                convCtrl.AddNewParticipant(
                    gazeTargets[partLocationsAssigned[PartnerClass.Participant]]
                    );
                partAtLocation = true;
                partApproaching = false;
            }
        }

        // Aggregate measurements
        if (curPart == PartnerClass.Participant &&
            expPhase == Phase.AnswerQuestion)
            partInteractTime += Time.deltaTime;
    }

    protected virtual void OnGUI()
    {
        // TODO: remove this
        return;
        //
        GUI.skin.box.normal.textColor = new Color(1, 1, 1, 1);
        GUI.skin.box.wordWrap = true;
        GUI.skin.box.alignment = TextAnchor.UpperLeft;
        GUI.skin.box.fontSize = 20;
        GUI.skin.button.fontSize = 20;
        GUI.skin.textField.fontSize = 20;

        if (expPhase == Phase.PartIdEntry)
        {
            GUI.Box(new Rect(Screen.width / 2 - 150, Screen.height / 2 - 75, 300, 150),
                "Enter participant number:");
            partIdStr = GUI.TextField(new Rect(Screen.width / 2 - 130, Screen.height / 2 - 30, 260, 40),
                partIdStr);
            if (GUI.Button(new Rect(Screen.width / 2 - 30, Screen.height / 2 + 20, 60, 40),
                "OK"))
            {
                expPhase = Phase.Start;
                partId = ushort.Parse(partIdStr);
            }
        }
        else if (expPhase == Phase.Start && !partApproaching)
        {
            GUI.Box(new Rect(Screen.width / 2 - 288, 60, 576, 480),
                "Welcome to the virtual art gallery. Our virtual guide will show you " +
                "a series of classic paintings from various time periods. If you have questions " +
                "about the paintings, you can ask the guide and she will do her best to answer them.\n\n" +
                "The guide uses a speech recognition system to detect and understand your questions. " +
                "When the speech recognition system is ready, the following icon will be shown on " +
                "the screen:\n\n\n\n\n\n\n" +
                "When this icon is shown, it means you can ask a question and the system " +
                "should understand it.\n\n" +
                "The guide is already engaged in interaction with a group " +
                "of participants. To approach the guide and join the interaction, press the LEFT MOUSE BUTTON.");
            GUI.DrawTexture(new Rect((Screen.width - 100) / 2, 262, 50, 100),
                canSpeakIcon, ScaleMode.StretchToFill, true, 0);
        }
        else if (expPhase == Phase.WaitForQuestion)
        {
            float hpos = (Screen.width - 50) / 2;
            float vpos = Screen.height - 120;
            GUI.DrawTexture(new Rect(hpos, vpos, 50, 100), canSpeakIcon, ScaleMode.StretchToFill, true, 0);
        }
    }
}

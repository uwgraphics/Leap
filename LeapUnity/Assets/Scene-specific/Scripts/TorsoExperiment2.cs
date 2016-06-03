using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class TorsoExperiment2 : Scenario
{
	public enum ExperimentTask
	{
		Task1,
		Task2
	};
	
	public enum ExperimentCondition
	{
		ReorientBody,
		StaticBody,
		NoGaze
	};
	
	public enum ExperimentCondAssignment
	{
		Fixed,
		Randomized,
		Stratified
	};
	
	public enum ExperimentPhase
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
	
	public enum ParticipantClass
	{
		Participant,
		Confederate1,
		Confederate2
	};
	
	[Serializable]
	public class TaskImageMapping
	{
		public Texture2D[] imageList;
	}
	
	[Serializable]
	public class ConfedQuestionMapping
	{
		public string[] questionClips = new string[0];
	}
	
	/// <summary>
	/// Experimental task.
	/// </summary>
	public ExperimentTask expTask = ExperimentTask.Task1;
	
	/// <summary>
	/// Experimental condition.
	/// </summary>
	public ExperimentCondition expCondition = ExperimentCondition.ReorientBody;
	
	/// <summary>
	/// How the participant will be assigned to a condition.
	/// </summary>
	public ExperimentCondAssignment expCondAssign = ExperimentCondAssignment.Stratified;
	
	/// <summary>
	/// Duration of the experiment (in seconds).
	/// </summary>
	public float expDuration = 270f;
	
	/// <summary>
	/// The starting/current experiment phase.
	/// </summary>
	public ExperimentPhase expPhase = ExperimentPhase.PartIdEntry;
	
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
	/// Pool of images that will be shown to participants
	/// (separate list for each task).
	/// </summary>
	public TaskImageMapping[] taskImages = new TaskImageMapping[0];
	
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
	protected Dictionary<ParticipantClass, string> partLocationsAssigned =
		new Dictionary<ParticipantClass, string>();
	protected bool partAtLocation = false;
	protected bool partApproaching = false;
	protected float partApproachTime = 0;
	protected ParticipantClass curPart;
	protected ParticipantClass prevPart;
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
		SetObjectActive("SpotLight",false);
		SetObjectActive("PointLight1",false);
		SetObjectActive("PointLight2",false);
		SetObjectActive("BgPanel",false);
		SetObjectActive("GTPainting",false);
		SetObjectActive("Room",false);
		// Hide characters
		SkinnedMeshRenderer[] smr_list = agents[agentName].GetComponentsInChildren<SkinnedMeshRenderer>();
		foreach( SkinnedMeshRenderer smr in smr_list )
			smr.enabled = false;
	}
	
	/// <summary>
	/// Show the scene by activating lights and unlit objects. 
	/// </summary>
	protected virtual void ShowScene()
	{
		SetObjectActive("SpotLight",true);
		SetObjectActive("PointLight1",true);
		SetObjectActive("PointLight2",true);
		SetObjectActive("BgPanel",true);
		//SetObjectActive("GTPainting",true);
		SetObjectActive("Room",true);
		// Hide characters
		SkinnedMeshRenderer[] smr_list = agents[agentName].GetComponentsInChildren<SkinnedMeshRenderer>();
		foreach( SkinnedMeshRenderer smr in smr_list )
			smr.enabled = true;
	}
	
	/// <summary>
	/// Sets the image on plane, such that image proportions and
	/// plane scale and position are preserved.
	/// </summary>
	/// <param name='img'>
	/// Image.
	/// </param>
	/// <param name='plane'>
	/// Target plane.
	/// </param>
	protected virtual void SetImageOnPlane( Texture2D img, GameObject plane )
	{
		plane.GetComponent<Renderer>().material.mainTexture = img;
		Vector3 scal = plane.transform.localScale;
		scal.x = scal.z = scal.y;
		if( img.width > img.height )
			scal.z /= ((float)img.width)/img.height;
		else
			scal.x /= ((float)img.height)/img.width;
		plane.transform.localScale = scal;
		
		plane.SetActive(true);
		SetObjectActive( ( plane.name == "GTPainting1" ) ?
			"GTPainting2" : "GTPainting1", false );
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
	public virtual int GazeAt( string agentName, ParticipantClass part,
		float headAlign, float torsoAlign )
	{
		GazeController gctrl = agents[agentName].GetComponent<GazeController>();
        gctrl.useTorso = true;//expCondition == ExperimentCondition.ReorientBody ? true : false;
		
		return GazeAt(agentName, partLocationsAssigned[part], headAlign, torsoAlign);
	}
	
	/// <summary>
	/// Scenario execution will pause until a participant speaks.
	/// </summary>
	/// <returns>
	protected virtual IEnumerator WaitUntilParticipantSpeaking()
	{
		// Will the confederate speak on this turn?
		confedWillSpeak = expTask == ExperimentTask.Task2 ? 
			( UnityEngine.Random.Range(0f,1f) > (numQuestionsPart + 1f)/8f ) :
				( UnityEngine.Random.Range(0f,1f) > (numQuestionsPainting%6 + 1f)/8f );
		confedSpeakTime = Time.timeSinceLevelLoad + 0.2f + UnityEngine.Random.Range(0f,1f)*(maxSilenceTime-0.2f);
		
		while( Time.timeSinceLevelLoad - lastUtteranceTime < maxSilenceTime )
		{
			if(listenCtrl.SpeechDetected)
			{
				Debug.Log("(1) Participant");
				
				prevPart = curPart;
				curPart = ParticipantClass.Participant;
				partSpeaking = true;
				if( prevPart != curPart )
				{
					++partNumInterrupts;
					numQuestionsPart = 0;
				}
				
				yield break;
			}
			else if( confedWillSpeak && Time.timeSinceLevelLoad >= confedSpeakTime &&
				( expTask == ExperimentTask.Task1 ||
				expTask == ExperimentTask.Task2 && curPart != ParticipantClass.Participant ) )
			{
				if( expTask == ExperimentTask.Task1 )
					Debug.Log("(2) Confederate 1");
				else
					Debug.Log("Current confederate");
				
				prevPart = curPart;
				if( expTask == ExperimentTask.Task1 )
					curPart = ParticipantClass.Confederate1;
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
		while( Time.timeSinceLevelLoad - lastUtteranceTime < maxSilenceTime )
		{
			questionAsked = "Unintelligible";
			
			if( curPart != ParticipantClass.Participant )
			{
				if( numQuestionsPainting >= 6 )
					questionAsked = "NextPainting";
				else
				{
					do
						questionAsked = questionKeywords[UnityEngine.Random.Range(0,6)];
					while( questionsAskedPainting.Contains(questionAsked) );
				}
			}
			else if(listenCtrl.SpeechRecognized)
			{
				// Participant asked a question - which question is it?
				string phrase = listenCtrl.RecognizedPhrase;
				int phrase_i = 0;
				for( ; phrase_i < listenCtrl.phrases.Length;
					++phrase_i )
					if( listenCtrl.phrases[phrase_i] == phrase )
						break;
				
				questionAsked = phrase_i < questionKeywords.Length ?
					questionKeywords[phrase_i] : "NextPainting";
			}
			else if(listenCtrl.SpeechRejected)
			{
				// Participant said something unintelligible
				questionAsked = "Unintelligible";
			}
			
			if( questionAsked != "Unintelligible" )
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
		while(!partApproaching)
			yield return 0;
		
		yield break;
	}
	
	/// <summary>
	/// Finds the index of the image.
	/// </summary>
	/// <returns>
	/// The image index.
	/// </returns>
	/// <param name='img'>
	/// Image.
	/// </param>
	protected virtual int FindImageIndex( Texture2D img )
	{
		for( int img_i = 0; img_i < taskImages[(int)expTask].imageList.Length; ++img_i )
			if( taskImages[(int)expTask].imageList[img_i] == img )
				return img_i;
		
		return -1;
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
	protected virtual int FindQuestionIndex( string qkw )
	{
		for( int qi = 0; qi < questionKeywords.Length; ++qi )
			if( qkw == questionKeywords[qi] )
				return qi;
		
		return 0;
	}
	
	/// <see cref="Scenario._Init()"/>
    protected override void _Init()
	{
		// Get task settings from command line
		string[] args = System.Environment.GetCommandLineArgs();
		if( args.Length > 1 )
			expTask = (ExperimentTask)Enum.Parse(typeof(ExperimentTask), args[1]);
		if( args.Length > 2 )
			expCondition = (ExperimentCondition)Enum.Parse(typeof(ExperimentCondition), args[2]);
		
		string logfilename = expTask == ExperimentTask.Task1 ?
			"VirtualGallery1_Log.csv" : "VirtualGallery2_Log.csv";
		if( !File.Exists(logfilename) )
		{
			expLog = new StreamWriter(logfilename, false);
			if( expTask == ExperimentTask.Task1 )
				expLog.WriteLine("partID,location,condition,images,numQuestions,interactTime,avgProximity,timeTaken");
			else
				expLog.WriteLine("partID,location,condition,images,numInterrupts,numQuestions,interactTime,avgProximity,timeTaken");
			expLog.Flush();
		}
		else
		{
			// Iterate through current log entries
			StreamReader old_log = new StreamReader(logfilename);
			string entry = "";
			int num_part = 0;
			ExperimentCondition last_cond = ExperimentCondition.NoGaze;
			string[] values;
			old_log.ReadLine();
			while(!old_log.EndOfStream)
			{
				entry = old_log.ReadLine();
				++num_part;
				
				values = entry.Split(",".ToCharArray());
				last_cond = (ExperimentCondition)Enum.Parse(
					typeof(ExperimentCondition), values[2]
					);
			}
			old_log.Close();
			
			// Assign participant to a condition
			if(expCondAssign == ExperimentCondAssignment.Randomized)
			{
				expCondition = UnityEngine.Random.Range(0,2) > 0 ?
					ExperimentCondition.StaticBody : ExperimentCondition.ReorientBody;
			}
			else if( expCondAssign == ExperimentCondAssignment.Stratified )
			{
				int cond_i = ((int)last_cond+1) %
					Enum.GetValues(typeof(ExperimentCondition)).Length;
				expCondition = (ExperimentCondition)cond_i;
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
		
		// Randomize order of paintings
		List<Texture2D> all_images = new List<Texture2D>(taskImages[(int)expTask].imageList);
		while( all_images.Count > 0 )
		{
			int next_img = UnityEngine.Random.Range(0, all_images.Count);
			imageQueue.Enqueue(all_images[next_img]);
			all_images.RemoveAt(next_img);
		}
		
		// Initialize measurements
		partNumInterrupts = 0;
		partNumQuestions = 0;
		partInteractTime = 0;
		expStartTime = Time.timeSinceLevelLoad;
		
		// Decide which characters to show
		if( expTask == ExperimentTask.Task1 )
			SetObjectActive( confedNames[((int)ParticipantClass.Confederate2)-1],
				false );
		else
			SetObjectActive( confedNames[((int)ParticipantClass.Confederate2)-1],
				true );
		
		// Assign locations
		List<GameObject> avlocs = new List<GameObject>(partLocations);
		if( expTask == ExperimentTask.Task2 )
			partLocationsAssigned[ParticipantClass.Confederate2] = partLocations[2].name;
		avlocs.RemoveAt(avlocs.Count-1);
		foreach( int part_i in Enum.GetValues(typeof(ParticipantClass)) )
		{
			if( avlocs.Count <= 0 )
				break;
			
			int loc_i = UnityEngine.Random.Range(0,avlocs.Count);
			GameObject loc = avlocs[loc_i];
			avlocs.RemoveAt(loc_i);
			partLocationsAssigned.Add((ParticipantClass)part_i, loc.name);
		}
		partAtLocation = false;
		
		// Position camera
		GameObject cam = GameObject.FindGameObjectWithTag("MainCamera");
		cam.transform.position =
			gazeTargets[partLocationsAssigned[ParticipantClass.Participant] + "Start"].transform.position;
		cam.transform.rotation =
			gazeTargets[partLocationsAssigned[ParticipantClass.Participant] + "Start"].transform.rotation;
		cam.GetComponent<MouseLook>().initialX = cam.transform.localEulerAngles.y;
		
		// Position confederates
		foreach( int conf_i in Enum.GetValues(typeof(ParticipantClass)) )
		{
			if( conf_i == 0 )
				// Not the participant, just embodied confederates
				continue;
			
			GameObject conf = agents[confedNames[conf_i-1]];
			if( !partLocationsAssigned.ContainsKey((ParticipantClass)conf_i) )
				continue;
			GameObject loc = gazeTargets[ partLocationsAssigned[(ParticipantClass)conf_i] ];
			Vector3 pos = conf.transform.position;
			pos.x = loc.transform.position.x;
			pos.z = loc.transform.position.z;
			conf.transform.position = pos;
			Vector3 rot = conf.transform.eulerAngles;
			rot.y = loc.transform.eulerAngles.y;
			conf.transform.eulerAngles = rot;
		}
		
		// Position painting canvas
		if( partLocationsAssigned[ParticipantClass.Participant] == "GTLoc1" )
			gazeTargets["GTPainting"].transform.position =
			gazeTargets["GTPainting1Loc"].transform.position;
		else
			gazeTargets["GTPainting"].transform.position =
			gazeTargets["GTPainting2Loc"].transform.position;
		
		// Initialize agent's conversation controller
		convCtrl = agents[agentName].GetComponent<ConversationController>();
		convCtrl.targets = new GameObject[ expTask == ExperimentTask.Task1 ? 1 : 2 ];
		convCtrl.targets[0] = gazeTargets[partLocationsAssigned[ParticipantClass.Confederate1]];
		if( expTask == ExperimentTask.Task1 )
		{
			convCtrl.prefTarget = expCondition == ExperimentCondition.StaticBody ?
				gazeTargets[partLocationsAssigned[ParticipantClass.Confederate1]] : null;
			convCtrl.prefTorsoAlign = ( expCondition == ExperimentCondition.ReorientBody ) ?
				0f : 0.6f;
			convCtrl.ackNewTarget = true;
			convCtrl.addressAll = true;
		}
		else
		{
			convCtrl.targets[1] = gazeTargets[partLocationsAssigned[ParticipantClass.Confederate2]];
			convCtrl.prefTarget = null;
			convCtrl.addressAll = false;
		}
		convCtrl.prefTorsoAlign = 0f;
		convCtrl.listenTarget = convCtrl.addressTarget = null;
		convCtrl.defaultTorsoAlign = 0f;
		
		// Initialize agent's gaze controller
		gazeCtrl = agents[agentName].GetComponent<GazeController>();
		gazeCtrl.head.align = 0.6f;
        gazeCtrl.useTorso = true;//expCondition == ExperimentCondition.ReorientBody ? true : false;
		
		// Initialize agent's listen controller
		listenCtrl = agents[agentName].GetComponent<SimpleListenController>();
		listenCtrl.LaunchServer();
		listenCtrl.ConnectToServer();
	}
	
	/// <see cref="Scenario._Run()"/>
	protected override IEnumerator _Run()
	{
		Debug.Log( "Experimental task: " + expTask );
		Debug.Log( "Experimental condition: " + expCondition );
		
		// Hide scene and allow the experimenter
		// to enter the participant number
		HideScene();
		while( expPhase != ExperimentPhase.Start )
			yield return 0;
		
		if( expTask == ExperimentTask.Task1 )
			yield return StartCoroutine(_RunTask1());
		else
			yield return StartCoroutine(_RunTask2());
	}
	
	protected virtual IEnumerator _RunTask1()
	{
		int speech_id = -1;
		int gaze_id = -1;
		
		// Prepare scenario
		yield return new WaitForSeconds(0.5f);
		gaze_id = GazeAt(agentName, ParticipantClass.Confederate1, 1f, 0.7f );
		curPart = ParticipantClass.Confederate1;
		convCtrl.targetDummy.transform.position =
			gazeTargets[partLocationsAssigned[curPart]].transform.position;
		convCtrl.addressTarget = gazeTargets[partLocationsAssigned[curPart]];
		yield return StartCoroutine(WaitUntilFinished(gaze_id));
		convCtrl.Introduce(null);
		yield return new WaitForSeconds(0.5f);
		ShowScene();
		if( expCondition == ExperimentCondition.NoGaze )
			gazeCtrl.enabled = false;
		
		// Wait for participant to approach
		yield return StartCoroutine(WaitForPartApproaching());
		yield return new WaitForSeconds(1f);
		expPhase = ExperimentPhase.ShowNextPainting;
		
		while( expPhase != ExperimentPhase.Farewell )
		{
			if( expPhase == ExperimentPhase.ShowNextPainting )
			{
				if( convCtrl.StateId != (int)ConversationState.Listen )
					convCtrl.Listen(partLocationsAssigned[curPart], 0.01f);
				if( numQuestionsPainting >= questionKeywords.Length )
				{
					// Indicate that a new painting is about to be shown
					yield return new WaitForSeconds(1f);
					speech_id = Speak(agentName, "ToNextPainting2");
					yield return StartCoroutine(WaitUntilFinished(speech_id));
				}
				numQuestionsPainting = 0;
				questionsAskedPainting.Clear();
				
				// Select the next painting
				curImage = imageQueue.Dequeue();
				imageQueue.Enqueue(curImage); // repeating image sequence
				SetObjectActive("GTPainting",true);
				SetImageOnPlane(curImage, gazeTargets["GTPainting"]);
				imagesSeen.Enqueue(curImage);
				
				// Introduce the next painting
				yield return new WaitForSeconds(0.5f);
				int img_i = FindImageIndex(curImage);
				string[] clips = { taskImages[(int)expTask].imageList[img_i].name + "-Intro" };
				convCtrl.Address(clips);
				yield return StartCoroutine(
					WaitForControllerState(agentName, "ConversationController", "Address")
					);
				yield return StartCoroutine(
					WaitForControllerState(agentName, "ConversationController", "WaitForSpeech")
					);
				
				expPhase = ExperimentPhase.WaitForQuestion;
			}
			
			if( expPhase == ExperimentPhase.WaitForQuestion )
			{
				// Wait for question from participant/confederate
				
				listenCtrl.Listen();
				
				lastUtteranceTime = Time.timeSinceLevelLoad;
				yield return StartCoroutine(WaitUntilParticipantSpeaking());
				
				if(!partSpeaking)
				{
					// Prompt the participants to speak
					speech_id = Speak(agentName, "AnyoneHaveQuestions", SpeechType.Question);
					yield return StartCoroutine(WaitUntilFinished(speech_id));
					lastUtteranceTime = Time.timeSinceLevelLoad;
					yield return StartCoroutine(WaitUntilParticipantSpeaking());
				}
				
				if(!partSpeaking)
				{
					// No one is asking anything, move on to next painting
					// or say goodbye
					
					listenCtrl.StopListening();
					
					if( Time.timeSinceLevelLoad - expStartTime > expDuration )
						expPhase = ExperimentPhase.Farewell;
					else
						expPhase = ExperimentPhase.ToNextPainting;
				}
				else
				{
					// Someone (participant or confederate) is speaking
					
					lastUtteranceTime = Time.timeSinceLevelLoad;
					convCtrl.addressAll = false;
					convCtrl.addressTarget = gazeTargets[partLocationsAssigned[curPart]];
					
					if( curPart != ParticipantClass.Participant )
					{
						yield return StartCoroutine(WaitUntilQuestionAsked());
						int qi = FindQuestionIndex(questionAsked);
						speech_id = Speak( confedNames[((int)curPart)-1],
							confedQuestionsMap[((int)curPart)-1].questionClips[qi] );
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
					
					if( questionAsked == "Unintelligible" )
					{
						if( numCannotUnderstand < 1 )
							expPhase = ExperimentPhase.CannotUnderstand;
						else
							expPhase = ExperimentPhase.ToNextPainting;
					}
					else
						expPhase = ExperimentPhase.AnswerQuestion;
					
					listenCtrl.StopListening();
				}
			}
			
			if( expPhase == ExperimentPhase.AnswerQuestion )
			{
				numCannotUnderstand = 0;
				
				// Respond/react to participant/confederate utterance
				if( questionAsked == "NextPainting" )
				{
					if( imageQueue.Count > 0 )
						// Participant has asked to see the next painting
						expPhase = ExperimentPhase.ShowNextPainting;
					else
						// There are no more paintings
						// TODO: should say something along those lines before quitting
						expPhase = ExperimentPhase.Farewell;
				}
				else
				{
					// Answer the question posed by participant/confederate
					
					yield return new WaitForSeconds(0.3f);
					
					// What question did they ask?
					int qi = FindQuestionIndex(questionAsked);
					if( curPart == ParticipantClass.Participant )
						++partNumQuestions;
					++numQuestionsPainting;
					questionsAskedPainting.Add(questionAsked);
				
					// Have the agent answer the question
					int img_i = FindImageIndex(curImage);
					string[] clips = { taskImages[(int)expTask].imageList[img_i].name + "-" + questionKeywords[qi] };
					convCtrl.Address(clips);
					yield return StartCoroutine(
						WaitForControllerState(agentName, "ConversationController", "WaitForSpeech")
						);
					convCtrl.addressAll = true;
					
					if( Time.timeSinceLevelLoad - expStartTime > expDuration )
						expPhase = ExperimentPhase.Farewell;
					else if( numQuestionsPainting >= questionKeywords.Length )
						expPhase = ExperimentPhase.ShowNextPainting;
					else
						expPhase = ExperimentPhase.WaitForQuestion;
				}
			}
			else if( expPhase == ExperimentPhase.ToNextPainting )
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
				
				expPhase = ExperimentPhase.ShowNextPainting;
			}
			else if( expPhase == ExperimentPhase.CannotUnderstand )
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
				
				expPhase = ExperimentPhase.WaitForQuestion;
			}
		}
		
		// Say goodbye to participants
		convCtrl.addressAll = true;
		yield return new WaitForSeconds(1f);
		speech_id = Speak(agentName, "Farewell", SpeechType.Question);
		yield return StartCoroutine(WaitUntilFinished(speech_id));
		yield return new WaitForSeconds(0.5f);
	}
	
	protected virtual IEnumerator _RunTask2()
	{
		int speech_id = -1;
		int gaze_id = -1;
		
		// Prepare scenario
		yield return new WaitForSeconds(0.5f);
		curPart = ParticipantClass.Confederate2;
		convCtrl.targetDummy.transform.position =
			gazeTargets[partLocationsAssigned[curPart]].transform.position;
		convCtrl.prefTarget = gazeTargets[partLocationsAssigned[curPart]];
		convCtrl.addressTarget = gazeTargets[partLocationsAssigned[curPart]];
		gaze_id = GazeAt(agentName, curPart, 1f, 0.6f);
		yield return StartCoroutine(WaitUntilFinished(gaze_id));
		convCtrl.Introduce(null);
		yield return new WaitForSeconds(0.5f);
		ShowScene();
		if( expCondition == ExperimentCondition.NoGaze )
			gazeCtrl.enabled = false;
		
		// Wait for participant to approach
		yield return StartCoroutine(WaitForPartApproaching());
		yield return new WaitForSeconds(1f);
		expPhase = ExperimentPhase.ShowNextPainting;
		
		while( expPhase != ExperimentPhase.Farewell )
		{
			if( expPhase == ExperimentPhase.ShowNextPainting )
			{
				// Show next painting
				
				if( convCtrl.StateId != (int)ConversationState.Listen )
					convCtrl.Listen(partLocationsAssigned[curPart], 0.01f);
				if( numQuestionsPainting >= questionKeywords.Length )
				{
					// Indicate that a new painting is about to be shown
					yield return new WaitForSeconds(1f);
					speech_id = Speak(agentName, "ToNextPainting2");
					yield return StartCoroutine(WaitUntilFinished(speech_id));
				}
				numQuestionsPainting = 0;
				questionsAskedPainting.Clear();
				
				// Select the next painting
				curImage = imageQueue.Dequeue();
				imageQueue.Enqueue(curImage); // repeating image sequence
				SetObjectActive("GTPainting",true);
				SetImageOnPlane(curImage, gazeTargets["GTPainting"]);
				imagesSeen.Enqueue(curImage);
				
				// Introduce the next painting
				yield return new WaitForSeconds(0.5f);
				int img_i = FindImageIndex(curImage);
				string[] clips = { taskImages[(int)expTask].imageList[img_i].name + "-Intro" };
				convCtrl.Address(clips);
				yield return StartCoroutine(
					WaitForControllerState(agentName, "ConversationController", "Address")
					);
				yield return StartCoroutine(
					WaitForControllerState(agentName, "ConversationController", "WaitForSpeech")
					);
				
				expPhase = ExperimentPhase.WaitForQuestion;
			}
			
			if( expPhase == ExperimentPhase.WaitForQuestion )
			{
				// Wait for question from participant/confederate
				
				listenCtrl.Listen();
				
				lastUtteranceTime = Time.timeSinceLevelLoad;
				yield return StartCoroutine(WaitUntilParticipantSpeaking());
				
				if(!partSpeaking)
				{
					// Prompt the participants to speak
					speech_id = Speak(agentName, numQuestionsPart > 0 ? "YouFurtherQuestions" : "YouAnyQuestions",
						SpeechType.Question);
					yield return StartCoroutine(WaitUntilFinished(speech_id));
					lastUtteranceTime = Time.timeSinceLevelLoad;
					yield return StartCoroutine(WaitUntilParticipantSpeaking());
				}
				
				if(!partSpeaking)
				{
					// No one is asking anything, move on to next person
					// or say goodbye
					
					listenCtrl.StopListening();
					
					if( Time.timeSinceLevelLoad - expStartTime > expDuration )
						expPhase = ExperimentPhase.Farewell;
					else
						expPhase = ExperimentPhase.ToNextPerson;
				}
				else
				{
					// Someone is speaking - it could either be the current person
					// or an interruption by the participant
					
					lastUtteranceTime = Time.timeSinceLevelLoad;
					
					if( curPart != prevPart && expCondition == ExperimentCondition.ReorientBody )
						// Interruption by participant - turn towards them
						convCtrl.defaultTorsoAlign = 0.6f;
					convCtrl.prefTarget = gazeTargets[partLocationsAssigned[curPart]];
					convCtrl.addressTarget = gazeTargets[partLocationsAssigned[curPart]];
					
					if( curPart != ParticipantClass.Participant )
					{
						yield return StartCoroutine(WaitUntilQuestionAsked());
						int qi = FindQuestionIndex(questionAsked);
						speech_id = Speak( confedNames[((int)curPart)-1],
							confedQuestionsMap[((int)curPart)-1].questionClips[qi] );
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
					
					// Continue to face the current person
					convCtrl.defaultTorsoAlign = 0f;
					
					if( questionAsked == "Unintelligible" )
					{
						if( numCannotUnderstand < 1 )
							expPhase = ExperimentPhase.CannotUnderstand;
						else
							expPhase = ExperimentPhase.ToNextPerson;
					}
					else
						expPhase = ExperimentPhase.AnswerQuestion;
					
					listenCtrl.StopListening();
				}
			}
			
			if( expPhase == ExperimentPhase.AnswerQuestion )
			{
				numCannotUnderstand = 0;
				
				// Respond/react to participant/confederate utterance
				if( questionAsked == "NextPainting" )
				{
					if( imageQueue.Count > 0 )
						// Participant has asked to see the next painting
						expPhase = ExperimentPhase.ShowNextPainting;
					else
						// There are no more paintings
						// TODO: should say something along those lines before quitting
						expPhase = ExperimentPhase.Farewell;
				}
				else
				{
					// Answer the question posed by participant/confederate
					
					yield return new WaitForSeconds(0.3f);
					
					// What question did they ask?
					int qi = FindQuestionIndex(questionAsked);
					if( curPart == ParticipantClass.Participant )
						++partNumQuestions;
					++numQuestionsPainting;
					questionsAskedPainting.Add(questionAsked);
				
					// Have the agent answer the question
					int img_i = FindImageIndex(curImage);
					string[] clips = { taskImages[(int)expTask].imageList[img_i].name + "-" + questionKeywords[qi] };
					convCtrl.Address(clips);
					convCtrl.addressAll = false;
					yield return StartCoroutine(
						WaitForControllerState(agentName, "ConversationController", "WaitForSpeech")
						);
					
					if( Time.timeSinceLevelLoad - expStartTime > expDuration )
						expPhase = ExperimentPhase.Farewell;
					else if( numQuestionsPainting >= questionKeywords.Length )
						expPhase = ExperimentPhase.ShowNextPainting;
					else
						expPhase = ExperimentPhase.WaitForQuestion;
				}
			}
			else if( expPhase == ExperimentPhase.ToNextPerson )
			{
				// Randomly choose next confederate, turn towards them and address them
				
				numCannotUnderstand = 0;
				
				// Make sure you are actually gazing at the current person first
				// TODO: this is needed b/c GazeController is a piece of shit that
				// should be deleted forever
				gaze_id = GazeAt(agentName, curPart, 0f, 0f);
				yield return StartCoroutine(WaitUntilFinished(gaze_id));
				//
				
				// Choose next person
				prevPart = curPart;
				curPart = curPart == ParticipantClass.Confederate1 ?
					 ParticipantClass.Confederate2 : ParticipantClass.Confederate1;
				numQuestionsPart = 0;
				
				// Turn to next person
				if( expCondition == ExperimentCondition.ReorientBody )
					convCtrl.defaultTorsoAlign = 0.6f;
				convCtrl.prefTarget = gazeTargets[partLocationsAssigned[curPart]];
				convCtrl.addressTarget = gazeTargets[partLocationsAssigned[curPart]];
				convCtrl.Listen(partLocationsAssigned[curPart], 1f);
				yield return StartCoroutine(
					WaitForControllerState(agentName, "ConversationController", "Listen")
					);
				// Verbally announce that you are moving on to next person
				string[] clips = { "ToNextPerson", "YouAnyQuestions" };
				convCtrl.Address(clips);
				convCtrl.addressAll = false;
				yield return StartCoroutine(
					WaitForControllerState(agentName, "ConversationController", "Address")
					);
				// Continue to face the next person
				// Make sure you are actually gazing at the next person
				// TODO: this is needed b/c GazeController is a piece of shit that
				// should be deleted forever
				gaze_id = GazeAt(agentName, curPart, 0.6f, 0.6f);
				yield return StartCoroutine(WaitUntilFinished(gaze_id));
				//
				convCtrl.defaultTorsoAlign = 0f;
				// Wait for that person to ask a question
				yield return StartCoroutine(
					WaitForControllerState(agentName, "ConversationController", "WaitForSpeech")
					);
				
				expPhase = ExperimentPhase.WaitForQuestion;
			}
			else if( expPhase == ExperimentPhase.CannotUnderstand )
			{
				yield return new WaitForSeconds(0.3f);
				
				++numCannotUnderstand;
				string[] clips = { "RepeatQuestion" };
				convCtrl.Address(clips);
				convCtrl.addressAll = false;
				yield return StartCoroutine(
					WaitForControllerState(agentName, "ConversationController", "WaitForSpeech")
					);
				
				expPhase = ExperimentPhase.WaitForQuestion;
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
		if( expPhase != ExperimentPhase.Farewell )
			return;
		
		// Which images did the participants see?
		string imgstr = "\"";
		foreach( Texture2D img in imagesSeen ) 
			imgstr += ( img.name + ",");
		imgstr += "\"";
		
		// Write log entry
		if( expTask == ExperimentTask.Task1 )
			expLog.WriteLine( string.Format("{0},{1},{2},{3},{4},{5},{6},{7}",
				partId, partLocationsAssigned[ParticipantClass.Participant],
				expCondition.ToString(), imgstr, partNumQuestions, partInteractTime,
				0, Time.timeSinceLevelLoad - expStartTime) );
		else
			expLog.WriteLine( string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8}",
				partId, partLocationsAssigned[ParticipantClass.Participant],
				expCondition.ToString(), imgstr, partNumInterrupts,
				partNumQuestions, partInteractTime,
				0, Time.timeSinceLevelLoad - expStartTime) );
		expLog.Close();
		
		// Take a screenshot
		if( !Directory.Exists("./Screenshots") )
			Directory.CreateDirectory("./Screenshots");
		string filename = string.Format("./Screenshots/scr-{0}-{1}-{2}.png", partId, expCondition, expTask);
		Application.CaptureScreenshot(filename);
	}
	
	protected virtual void Update()
	{
		//
		// Participant movement handling:
		//
		
		GameObject cam = GameObject.FindGameObjectWithTag("MainCamera");
		if( ( Input.GetKeyDown(KeyCode.UpArrow) ||
			Input.GetKeyDown(KeyCode.Space) ||
			Input.GetKeyDown(KeyCode.Mouse0) ||
			Input.GetKeyDown(KeyCode.Mouse1) ) && !partAtLocation &&
			expPhase == ExperimentPhase.Start )
			partApproaching = true;
		
		if(partApproaching)
		{
			partApproachTime += Time.deltaTime;
			
			int loc_i = 0;
			for( ; loc_i < partLocations.Length; ++loc_i )
				if( partLocations[loc_i].name ==
					partLocationsAssigned[ParticipantClass.Participant] )
					break;
			string anim = partWalkUpAnims[loc_i];
			if( partApproachTime >= 0.5f && !cam.GetComponent<Animation>()[anim].enabled )
			{
				cam.GetComponent<Animation>()[anim].enabled = true;
				cam.GetComponent<Animation>()[anim].blendMode = AnimationBlendMode.Blend;
				cam.GetComponent<Animation>()[anim].time = 0;
				cam.GetComponent<Animation>()[anim].weight = 1f;
				cam.GetComponent<Animation>()[anim].speed = 0.7f;
				
				partApproachTime = 0;
			}
			
			
			if( partApproachTime >= 1f )
			{
				convCtrl.AddNewParticipant(
					gazeTargets[partLocationsAssigned[ParticipantClass.Participant]]
					);
				partAtLocation = true;
				partApproaching = false;
			}
		}
		
		//
		// Accumulating measurements:
		//
		
		if( curPart == ParticipantClass.Participant &&
			( expTask == ExperimentTask.Task2 ||
			expTask == ExperimentTask.Task1 &&
			expPhase == ExperimentPhase.AnswerQuestion ) )
			partInteractTime += Time.deltaTime;
	}
	
	protected virtual void OnGUI()
	{
		GUI.skin.box.normal.textColor = new Color(1,1,1,1);
		GUI.skin.box.wordWrap = true;
		GUI.skin.box.alignment = TextAnchor.UpperLeft;
		GUI.skin.box.fontSize = 20;
		GUI.skin.button.fontSize = 20;
		GUI.skin.textField.fontSize = 20;
		
		if( expPhase == ExperimentPhase.PartIdEntry )
		{
			GUI.Box( new Rect(Screen.width/2-150, Screen.height/2-75, 300, 150),
				"Enter participant number:" );
			partIdStr = GUI.TextField( new Rect(Screen.width/2-130, Screen.height/2-30, 260, 40),
				partIdStr );
			if( GUI.Button( new Rect(Screen.width/2-30, Screen.height/2+20, 60, 40),
				"OK" ) )
			{
				expPhase = ExperimentPhase.Start;
				partId = ushort.Parse(partIdStr);
			}
		}
		else if( expPhase == ExperimentPhase.Start && !partApproaching )
		{
			GUI.Box( new Rect(Screen.width/2-288, 60, 576, 480),
				"Welcome to the virtual art gallery. Our virtual guide will show you " +
				"a series of classic paintings from various time periods. If you have questions " +
				"about the paintings, you can ask the guide and she will do her best to answer them.\n\n" +
				"The guide uses a speech recognition system to detect and understand your questions. " +
				"When the speech recognition system is ready, the following icon will be shown on " +
				"the screen:\n\n\n\n\n\n\n" +
				"When this icon is shown, it means you can ask a question and the system " +
				"should understand it.\n\n" +
				"The guide is already engaged in interaction with a group " +
				"of participants. To approach the guide and join the interaction, press the LEFT MOUSE BUTTON." );
			GUI.DrawTexture(new Rect((Screen.width-100)/2, 262, 50, 100),
				canSpeakIcon, ScaleMode.StretchToFill, true, 0);
		}
		else if( expPhase == ExperimentPhase.WaitForQuestion )
		{
			float hpos = (Screen.width-50)/2;
			float vpos = Screen.height-120;
			GUI.DrawTexture(new Rect(hpos, vpos, 50, 100), canSpeakIcon, ScaleMode.StretchToFill, true, 0);
		}
	}
}

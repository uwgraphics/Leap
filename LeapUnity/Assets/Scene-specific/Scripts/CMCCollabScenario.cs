using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public enum CMCCollabCondition
{
	AvNeutral,
	AvSmile,
	VidNeutral,
	VidSmile
}

public enum CMCCollabPhase
{
	SelectCondition,
	ShowExpInstr,
	ShowGameInstr,
	PlayClip,
	WaitForNext,
	PlayGame,
	ShowFinalScore
}

public class CMCCollabScenario : Scenario
{
	/// <summary>
	/// Experiment condition. 
	/// </summary>
	public CMCCollabCondition condition = CMCCollabCondition.AvNeutral;
	
	/// <summary>
	/// Scenario phase. 
	/// </summary>
	public CMCCollabPhase phase = CMCCollabPhase.SelectCondition;
	
	/// <summary>
	/// Table of score rewards for the Prisoner's Dilemma game. 
	/// </summary>
	public int[,] rewardTable = { {30,30}, {0,50}, {50,0}, {10,10} };
	
	/// <summary>
	/// Maximum number of game turns. 
	/// </summary>
	public int numTurns = 15;
	
	/// <summary>
	/// Number of initial turns which aren't logged.
	/// </summary>
	public int numPracticeTurns = 5;
	
	/// <summary>
	/// List of collaborative partner decisions.
	/// </summary>
	public bool[] partnerCollabs = { true, true, true, false, true, true, false, true, true, false, false, true, true, false, true };
	
	/// <summary>
	/// Subject's score. 
	/// </summary>
	public int subjectScore = 100;
	
	/// <summary>
	/// Partner's score. 
	/// </summary>
	public int partnerScore = 100;
	
	/// <summary>
	/// Name of the partner in human videos. 
	/// </summary>
	public string vidPartnerName = "Allie";
	
	/// <summary>
	/// Name of the avatar partner.
	/// </summary>
	public string avPartnerName = "Sophie";
	
	/// <summary>
	/// Human video list. 
	/// </summary>
	public MovieTexture[] videos = {};
	
	/// <summary>
	/// Static human or avatar image list. 
	/// </summary>
	public Texture2D[] staticImages = {};
	
	public Texture2D imgExpInstructions;
	public Texture2D imgGameInstructions;
	public Texture2D icoPassOn;
	public Texture2D icoKeep;
	
	/*public string experimentInstructions = "This experiment is a study of computer-mediated communication. " +
		"You will be paired with one virtual partner through computer-mediated " +
			"communication - at no time will your true identity be revealed to the " +
			"other participant.\n" + 
			"You and your partner will play a game together. To view the game description and detailed instructions, click NEXT.";
	
	public string gameInstructions = "Your goal is to earn a considerable amount of money starting with the " +
			"initial portfolio of 100 credits. " +
			"Both you and the other partner will have two possible choices. You can " +
			"choose to keep your money or to invest. If you both choose INVEST, you " +
			"will both get a payoff of 30 credits. If you both choose KEEP the money, " +
			"you will both get a payoff of 10 credits. If you KEEP the money, but the " +
			"other player chooses to INVEST, you will get a payoff of 50 credits, but " +
			"your partner will receive nothing. Likewise, if you choose to INVEST " +
			"but the other player KEEPS the money, then you receive nothing and " +
			"your partner receives 50 credits. These payoffs are summarized in the " +
			"table below:\n\n" +
			"          INVEST    KEEP\n" +
			"INVEST [30,30]   [0,50]\n" + 
			" KEEP     [50,0]    [30,30]";*/
	
	protected bool condSelected = false;
	protected int curTurn = 0; // current turn
	protected int logSubjectScore;
	protected int logPartnerScore;
	protected int numSubjectCollabs = 0;
	protected bool lastSubjCollab = false;
	protected bool lastPartnerCollab = false;
	protected int lastSubjGain = 0;
	protected bool fiveTurnsPassed0 = false; // separate flag to signal that score should be shown before playing next clip
	
	protected bool btnNextClicked = false;
	protected bool waitForConfirmCollab = false;
	protected bool waitForConfirmDefect = false;
	protected bool fiveTurnsPassed = false;
	protected bool showGameInstr = false;
	
	protected GameObject videoPlane; // this is where real human video is shown
	protected Dictionary<string, MovieTexture> videoMap;
	protected Dictionary<string, Texture2D> staticImageMap;
	protected GameObject avatar; // virtual avatar
	protected FaceController faceCtrl;
	protected GestureController gestCtrl;
	protected SpeechController speechCtrl;
	protected ExpressionController expressionCtrl;

	/// <summary>
	/// Joint score (subject's + partner's)
	/// </summary>
	public int TotalScore
	{
		get
		{
			return subjectScore + partnerScore;
		}
	}
	
	/// <summary>
	/// Plays specified avatar/video clip. 
	/// </summary>
	/// <param name="clipIndex">
	/// Clip index. <see cref="System.Int32"/>
	/// </param>
	public void PlayClip( int clipIndex )
	{
		if( condition == CMCCollabCondition.AvNeutral ||
		   condition == CMCCollabCondition.AvSmile )
		{
			videoPlane.active = false;
			videoPlane.renderer.material.color = Color.white;
			
			// Unfreeze all animation
			faceCtrl.randomMotionEnabled = true;
			avatar.GetComponent<BodyIdleController>().randomMotionEnabled = true;
			avatar.GetComponent<BlinkController>().enabled = true;
			avatar.GetComponent<EyesAliveController>().enabled = true;
			
			string clip_name = avPartnerName +
				(( condition == CMCCollabCondition.AvNeutral ) ? "Neutral" : "Smiling" ) +
					clipIndex;
			speechCtrl.Speak(clip_name);
			
			if( clipIndex == 1 || clipIndex == 2 || clipIndex == 6 )
			{
				// Do a little nod
				//gestCtrl.states[(int)GestureState.Gesturing].animSpeed = 0.2f + Random.value * 0.8f;
				gestCtrl.DoGesture("Nod");
			}
			else
			{
				// Raise eyebrows
				//gestCtrl.states[(int)GestureState.Gesturing].animSpeed = 0.8f + Random.value * 0.4f;
				gestCtrl.DoGesture("RaiseBrow");
			}
			
			if( condition == CMCCollabCondition.AvSmile )
			{
				// Decrease smile intensity a bit
				expressionCtrl.magnitude = 0.2f;
				expressionCtrl.maxMagnitude = 0.2f;
				expressionCtrl.changeExpression = true;
			}
			
			// Enable more intense head motion
			// TODO
		}
		else
		{
			videoPlane.active = true;
			videoPlane.renderer.material.color = Color.white;
			
			string clip_name = vidPartnerName +
				(( condition == CMCCollabCondition.VidNeutral ) ? "Neutral" : "Smiling" ) +
					clipIndex;
			videoPlane.renderer.material.mainTexture = videoMap[clip_name];
			MovieTexture vid_tex = (MovieTexture)videoPlane.renderer.material.mainTexture;
			vid_tex.Play();
			// Play audio, too
			videoPlane.audio.clip = vid_tex.audioClip;
			videoPlane.audio.Play();
		}
	}
	
	/// <summary>
	/// Shows a static image from the specified clip.
	/// </summary>
	/// <param name="clipIndex">
	/// Clip index. <see cref="System.Int32"/>
	/// </param>
	public void ShowStaticImage( int clipIndex )
	{
		if( condition == CMCCollabCondition.AvNeutral ||
		   condition == CMCCollabCondition.AvSmile )
		{
			videoPlane.active = false;
			
			// Don't show a static image, just temp. freeze all animation
			avatar.GetComponent<BodyIdleController>().randomMotionEnabled = false;
			faceCtrl.randomMotionEnabled = false;
			avatar.GetComponent<BlinkController>().enabled = false;
			avatar.GetComponent<EyesAliveController>().enabled = false;
		}
		else
		{
			videoPlane.active = true;
			
			string clip_name = vidPartnerName +
				(( condition == CMCCollabCondition.VidNeutral ) ? "Neutral" : "Smiling" ) +
					clipIndex;
			videoPlane.renderer.material.mainTexture = staticImageMap[clip_name];
		}
	}
	
	/// <summary>
	/// Advances the Prisoner's Dilemma game. 
	/// </summary>
	/// <param name="subjectCollab">
	/// Subject's decision (true - collaborate, false - defect) <see cref="System.Boolean"/>
	/// </param>
	/// <returns>
	/// true if the game is still ongoing, false if it is over. <see cref="System.Boolean"/>
	/// </returns>
	public bool AdvanceGame( bool subjectCollab )
	{
		if( curTurn >= numTurns )
			// Game is over, can't advance
			return false;
		
		// Update score
		lastSubjCollab = subjectCollab; // So we know which icon to show
		lastPartnerCollab = partnerCollabs[curTurn]; // So we know which icon to show
		int si = subjectCollab ? 0 : 1;
		int ci = partnerCollabs[curTurn] ? 0 : 1;
		int dssc = rewardTable[si*2+ci,0];
		int dpsc = rewardTable[si*2+ci,1];
		subjectScore += dssc;
		partnerScore += dpsc;
		lastSubjGain = dssc; // This will be shown to the subject
		if( curTurn >= numPracticeTurns )
		{
			// Update "real" score (the one we log)
			
			logSubjectScore += dssc;
			logPartnerScore += dpsc;
			
			if(subjectCollab)
				++numSubjectCollabs;
		}
		
		++curTurn; // next turn
		if( curTurn > 0 && curTurn%5 == 0 )
			fiveTurnsPassed0 = true;
		else
			fiveTurnsPassed0 = false;
		
		return true;
	}
	
	protected override void _Init()
	{
		// Set initial score
		logSubjectScore = subjectScore;
		logPartnerScore = partnerScore;
		
		// Set initial phase
		phase = CMCCollabPhase.SelectCondition;
	}
	
	protected override IEnumerator _Run()
	{
		// Choose experimental condition
		yield return StartCoroutine( WaitForConditionSelected() );
		phase = CMCCollabPhase.ShowExpInstr;
		
		_InitScene();
		// Show experiment instructions until user clicks Next
		yield return StartCoroutine( WaitForNextClicked() );
		phase = CMCCollabPhase.ShowGameInstr;
		
		// Show game instructions until user clicks Next
		yield return StartCoroutine( WaitForNextClicked() );
		phase = CMCCollabPhase.PlayClip;
		
		// Play clip 1 (video or anim./speech) until finished
		PlayClip(1);
		yield return StartCoroutine( WaitForClipFinished() );
		phase = CMCCollabPhase.WaitForNext;
		
		// Wait until user clicks Next
		ShowStaticImage(1);
		yield return StartCoroutine( WaitForNextClicked() );
		phase = CMCCollabPhase.PlayClip;
		
		// Play clip 2 until finished
		PlayClip(2);
		yield return StartCoroutine( WaitForClipFinished() );
		phase = CMCCollabPhase.WaitForNext;
		
		// Wait until user clicks Next
		ShowStaticImage(2);
		yield return StartCoroutine( WaitForNextClicked() );
		phase = CMCCollabPhase.PlayGame;
		
		// When user clicks Collaborate or Defect, advance game
		// Wait until 5 turns have passed
		yield return StartCoroutine( WaitFor5TurnsPassed() );
		phase = CMCCollabPhase.PlayClip;
		
		// Play clip 3 until finished
		PlayClip(3);
		yield return StartCoroutine( WaitForClipFinished() );
		phase = CMCCollabPhase.WaitForNext;
		
		// Wait until user clicks Next
		ShowStaticImage(3);
		yield return StartCoroutine( WaitForNextClicked() );
		phase = CMCCollabPhase.PlayGame;
		
		// When user clicks Collaborate or Defect, advance game
		// Wait until 5 turns have passed
		yield return StartCoroutine( WaitFor5TurnsPassed() );
		phase = CMCCollabPhase.PlayClip;
		
		// Play clip 4 until finished
		PlayClip(4);
		yield return StartCoroutine( WaitForClipFinished() );
		phase = CMCCollabPhase.WaitForNext;
		
		// Wait until user clicks Next
		ShowStaticImage(4);
		yield return StartCoroutine( WaitForNextClicked() );
		phase = CMCCollabPhase.PlayGame;
		
		// When user clicks Collaborate or Defect, advance game
		// Wait until 5 turns have passed
		yield return StartCoroutine( WaitFor5TurnsPassed() );
		phase = CMCCollabPhase.PlayClip;
		
		// Play clip 5 until finished
		PlayClip(5);
		yield return StartCoroutine( WaitForClipFinished() );
		phase = CMCCollabPhase.WaitForNext;
		
		// Wait until user clicks Next
		ShowStaticImage(5);
		yield return StartCoroutine( WaitForNextClicked() );
		phase = CMCCollabPhase.ShowFinalScore;
		
		_WriteExpResults();
		// Show final score until user clicks Next
		yield return StartCoroutine( WaitForNextClicked() );
		phase = CMCCollabPhase.PlayClip;
		
		// Play clip 6 until finished
		PlayClip(6);
		yield return StartCoroutine( WaitForClipFinished() );
		phase = CMCCollabPhase.WaitForNext;
		
		// Wait until user clicks Next
		ShowStaticImage(6);
		yield return StartCoroutine( WaitForNextClicked() );
		
		Application.Quit();
	}
	
	protected override void _Finish()
	{
	}
	
	protected IEnumerator WaitForConditionSelected()
	{
		while( !condSelected )
		{
			yield return 0;
		}
	}
	
	protected IEnumerator WaitForNextClicked()
	{
		while( !btnNextClicked )
		{
			yield return 0;
		}
		
		btnNextClicked = false;
	}
	
	protected IEnumerator WaitForClipFinished()
	{
		while( ( condition == CMCCollabCondition.AvNeutral ||
		      condition == CMCCollabCondition.AvSmile ) &&
		      speechCtrl.doSpeech ||
		      ( condition == CMCCollabCondition.VidNeutral ||
		      condition == CMCCollabCondition.VidSmile ) &&
		      ((MovieTexture)videoPlane.renderer.material.mainTexture).isPlaying )
		{	
			if( faceCtrl != null )
			{
				// Slow down head motion
				// TODO
			}
			
			if( condition == CMCCollabCondition.AvSmile )
			{
				// Bring smile intensity up to max
				expressionCtrl.magnitude = 0.5f;
				expressionCtrl.maxMagnitude = 0.5f;
				expressionCtrl.changeExpression = true;
			}
			
			yield return 0;
		}
	}
	
	protected IEnumerator WaitFor5TurnsPassed()
	{
		while( !fiveTurnsPassed )
		{
			yield return 0;
		}
		
		fiveTurnsPassed = false;
	}
	
	protected virtual void OnGUI()
	{	
		// Initialize GUI styles
		GUIStyle style_btnSelectCond = new GUIStyle(GUI.skin.button);
		style_btnSelectCond.fontSize = 60;
		style_btnSelectCond.fontStyle = FontStyle.Bold;
		style_btnSelectCond.hover.textColor = Color.blue;
		GUIStyle style_btnNext = new GUIStyle(GUI.skin.button);
		style_btnNext.fontSize = 32;
		style_btnNext.fontStyle = FontStyle.Bold;
		style_btnNext.hover.textColor = Color.blue;
		GUIStyle style_btnCollab = new GUIStyle(style_btnNext);
		GUIStyle style_btnDefect = new GUIStyle(style_btnNext);
		style_btnCollab.active.textColor = Color.blue;
		style_btnDefect.active.textColor = Color.blue;
		if(waitForConfirmCollab)
			style_btnCollab.normal = style_btnCollab.active;
		else if(waitForConfirmDefect)
			style_btnDefect.normal = style_btnDefect.active;
		GUIStyle style_btnConfirm = new GUIStyle(style_btnNext);
		style_btnConfirm.normal.textColor = Color.green;
		GUIStyle style_boxScore = new GUIStyle(GUI.skin.box);
		//style_boxScore.font = (Font)Font.FindObjectsOfTypeIncludingAssets( typeof(Font) )[0];
		style_boxScore.fontSize = 32;
		style_boxScore.fontStyle = FontStyle.Bold;
		GUIStyle style_boxInstr = new GUIStyle(GUI.skin.box);
		//style_boxInstr.font = (Font)Font.FindObjectsOfTypeIncludingAssets( typeof(Font) )[0];
		style_boxInstr.fontSize = 18;
		style_boxInstr.fontStyle = FontStyle.Normal;
		style_boxInstr.wordWrap = true;
		GUIStyle style_tglShowInstr = new GUIStyle(GUI.skin.toggle);
		style_tglShowInstr.fontSize = 18;
		
		if( phase == CMCCollabPhase.PlayGame )
		{
			// Show game score
			
			if( curTurn%5 == 0 && !fiveTurnsPassed0 )
			{
				GUI.Box( new Rect( Screen.width/2 - 200, 25, 440, 50 ),
				        "Your Current Credits: " + subjectScore + "\n", style_boxScore );
			}
			else
			{
				GUI.Box( new Rect( Screen.width/2 - 300, 25, 600, 160 ),
				        "\nYOU:           PARTNER:                        \n" +
				        "You Gained: " + lastSubjGain + "\n" +
				        "Your Current Credits: " + subjectScore, style_boxScore );
				GUI.Box( new Rect( Screen.width/2 - 80, 40, 64, 64 ), lastSubjCollab ? icoPassOn : icoKeep );
				GUI.Box( new Rect( Screen.width/2 + 180, 40, 64, 64 ), lastPartnerCollab ? icoPassOn : icoKeep );
			}
		}
		else if( phase == CMCCollabPhase.ShowFinalScore )
		{
			GUI.Box( new Rect( Screen.width/2 - 200, 25, 400, 120 ),
			        "Total Score: " + TotalScore + "\n" +
			        "Your Share: " + subjectScore + "\n" +
			        "Partner's Share: " + partnerScore, style_boxScore );
		}
		
		if( phase == CMCCollabPhase.SelectCondition )
		{
			if( GUI.Button( new Rect( Screen.width/2 - 100, Screen.height/2 - 100, 80, 80 ),
			               "1", style_btnSelectCond ) )
			{
				condition = CMCCollabCondition.AvNeutral;
				condSelected = true;
			}
			else if( GUI.Button( new Rect( Screen.width/2 + 20, Screen.height/2 - 100, 80, 80 ),
			                    "2", style_btnSelectCond ) )
			{
				condition = CMCCollabCondition.AvSmile;
				condSelected = true;
			}
			else if( GUI.Button( new Rect( Screen.width/2 - 100, Screen.height/2 + 20, 80, 80 ),
			               "3", style_btnSelectCond ) )
			{
				condition = CMCCollabCondition.VidNeutral;
				condSelected = true;
			}
			else if( GUI.Button( new Rect( Screen.width/2 + 20, Screen.height/2 + 20, 80, 80 ),
			                    "4", style_btnSelectCond ) )
			{
				condition = CMCCollabCondition.VidSmile;
				condSelected = true;
			}
		}
		else if( phase == CMCCollabPhase.ShowExpInstr )
		{
			// Show experiment instructions
			
			/*GUI.Box( new Rect( Screen.width/2 - 400, 200, 800, 400 ),
			        experimentInstructions, style_boxInstr );*/
			GUI.Box( new Rect( Screen.width/2 - 400, 150, 800, 500 ),
			        imgExpInstructions, style_boxInstr );
		}
		else if( phase == CMCCollabPhase.ShowGameInstr )
		{
			// Show game instructions
			
			/*GUI.Box( new Rect( Screen.width/2 - 400, 200, 800, 400 ),
			        "You and your partner will play an Investment Game. The instructions " +
			        "are simple. You need to follow them carefully and make good decisions. " +
			        gameInstructions + "\n\nClick NEXT to meet your partner.", style_boxInstr );*/
			GUI.Box( new Rect( Screen.width/2 - 400, 150, 800, 500 ),
			        imgGameInstructions, style_boxInstr );
		}
		else if( phase == CMCCollabPhase.PlayGame )
		{
			if(fiveTurnsPassed0)
			{
				// Let the subject see the last round results before moving on
				
				if( GUI.Button( new Rect( Screen.width/2 - 75, Screen.height - 100, 150, 40 ),
				               "NEXT>", style_btnNext ) )
				{
					fiveTurnsPassed0 = false;
					fiveTurnsPassed = true;
				}
			}
			else
			{
				// Show game interface
				
				if( GUI.Button( new Rect( Screen.width/2 - 325, Screen.height - 100, 150, 40 ),
				               "PASS ON", style_btnCollab ) )
				{
					waitForConfirmCollab = true;
					waitForConfirmDefect = false;
				}
			
				if( GUI.Button( new Rect( Screen.width/2 + 125, Screen.height - 100, 150, 40 ),
				               "KEEP", style_btnDefect ) )
				{
					waitForConfirmCollab = false;
					waitForConfirmDefect = true;
				}
			
				showGameInstr = GUI.Toggle( new Rect(20, 20, 250, 40 ), showGameInstr,
				                           "Show Instructions", style_tglShowInstr );
				if(showGameInstr)
				{
					/*GUI.Box( new Rect( Screen.width/2 - 400, 200, 800, 400 ),
					        gameInstructions, style_boxInstr );*/
					GUI.Box( new Rect( Screen.width/2 - 400, 150, 800, 500 ),
					        imgGameInstructions, style_boxInstr );
				}
				
				if( ( waitForConfirmCollab || waitForConfirmDefect ) &&
				   GUI.Button( new Rect( Screen.width/2 - 90, Screen.height - 100, 180, 40 ),
				              "CONFIRM>", style_btnConfirm ) )
				{
					AdvanceGame( waitForConfirmCollab ? true : false );
					
					waitForConfirmCollab = false;
					waitForConfirmDefect = false;
					showGameInstr = false;
				}
			}
		}
		
		if( ( phase == CMCCollabPhase.ShowExpInstr ||
		   phase == CMCCollabPhase.ShowGameInstr ||
		   phase == CMCCollabPhase.WaitForNext ||
		   phase == CMCCollabPhase.ShowFinalScore ) &&
		   GUI.Button( new Rect( Screen.width/2 - 75, Screen.height - 100, 150, 40 ),
				                          "NEXT>", style_btnNext ) )
		{
			btnNextClicked = true;
		}
	}
	
	protected virtual void _InitScene()
	{
		// Get the avatar
		avatar = GameObject.FindGameObjectWithTag("Player");
		if( ( condition == CMCCollabCondition.AvNeutral ||
		     condition == CMCCollabCondition.AvSmile ) &&
		   avatar == null )
		{
			// No avatar in the scene
			Debug.LogError( "Cannot find avatar in the scene, canceling scenario execution." );
			
			return;
		}
		
		if( avatar != null )
		{
			// Get the avatar animation controllers
			
			faceCtrl = avatar.GetComponent<FaceController>();
			if( faceCtrl == null )
			{
				return;
			}
			
			gestCtrl = avatar.GetComponent<GestureController>();
			if( gestCtrl == null )
			{
				return;
			}
			
			speechCtrl = avatar.GetComponent<SpeechController>();
			if( speechCtrl == null )
			{
				return;
			}
			
			expressionCtrl = avatar.GetComponent<ExpressionController>();
			if ( expressionCtrl	== null )
			{
				return;
			}
		}
		
		videoPlane = GameObject.FindGameObjectWithTag("VideoPlane");
		if( ( condition == CMCCollabCondition.VidNeutral ||
		     condition == CMCCollabCondition.VidSmile ) &&
		   videoPlane == null )
		{
			// No video plane in the scene
			Debug.LogError( "Cannot find video plane in the scene, canceling scenario execution." );
			
			return;
		}
		
		if( videoPlane != null )
		{
			// Get the human videos
		
			videoMap = new Dictionary<string, MovieTexture>();
			foreach( MovieTexture video in videos )
			{
				videoMap[video.name] = video;
			}
			
			staticImageMap = new Dictionary<string, Texture2D>();
			foreach( Texture2D img in staticImages )
			{
				staticImageMap[img.name] = img;
			}
			
			videoPlane.renderer.material.color = Color.black;
		}
		
		if( condition == CMCCollabCondition.AvNeutral ||
		   condition == CMCCollabCondition.AvSmile )
		{
			// Configure display settings for avatar condition
			
			avatar.active = true;
			videoPlane.active = true;
			GetComponent<BlurEffect>().enabled = true;
			
			if( condition == CMCCollabCondition.AvSmile )
			{
				// Enable avatar smiling
				expressionCtrl.magnitude = 0.5f;
				expressionCtrl.maxMagnitude = 0.5f;
				expressionCtrl.ChangeExpression("ExpressionJoy");
			}
			else
			{
				// Avatar should have a neutral expression
				expressionCtrl.magnitude = 0;
				expressionCtrl.maxMagnitude = 0;
				expressionCtrl.ChangeExpression("ExpressionJoy");
			}
		}
		else
		{
			// Configure display settings for human video condition
			
			if( avatar != null )
				avatar.active = false;
			videoPlane.active = true;
			GetComponent<BlurEffect>().enabled = false;
		}
	}
	
	protected virtual void _WriteExpResults()
	{
		StreamWriter sw = File.AppendText("Experiment.log");
		sw.WriteLine( condition.ToString() + " " + logSubjectScore + " " + logPartnerScore + " " + numSubjectCollabs );
		sw.Close();
	}
}

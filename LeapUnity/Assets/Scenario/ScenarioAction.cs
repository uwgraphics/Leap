using UnityEngine;
using System.Collections;

/// <summary>
/// Class representing an action executed as part of a scenario.
/// </summary>
public abstract class ScenarioAction
{
	protected GameObject subj;
	
	/// <summary>
	/// Game object which is the subject of the action
	/// (e.g. agent performing the action).
	/// </summary>
	public GameObject Subject
	{
		get
		{
			return subj;
		}
	}
	
	/// <summary>
	/// true if action has finished, false otherwise. 
	/// </summary>
	public abstract bool Finished
	{
		get;
	}
	
	/// <summary>
	/// Constructor.
	/// </summary>
	/// <param name="subj">
	/// Game object which is the subject of the action
	/// (e.g. agent performing the action).<see cref="GameObject"/>
	/// </param>
	public ScenarioAction( GameObject subj )
	{
		this.subj = subj;
	}
	
	/// <summary>
	/// Executes the scenario action. 
	/// </summary>
	public abstract void Execute();
	
	/// <summary>
	/// Stops the scenario action. 
	/// </summary>
	public abstract void Stop();
};

/// <summary>
/// Class representing an agent gaze shift. 
/// </summary>
public class GazeAtAction : ScenarioAction
{
	protected GameObject target;
	protected Vector3 targetPos;
	protected GazeController gazeCtrl;
	protected float headAlign = 1f;
	protected float headLatency = 1f;
	protected float torsoAlign = 0f;
	protected float torsoLatency = 0f;
	protected float eyeAlign = 1f;
	protected bool dummyTarget = false;
	
	/// <summary>
	/// <see cref="ScenarioAction::Finished"/> 
	/// </summary>
	public override bool Finished
	{
		get
		{
			return !gazeCtrl.doGazeShift &&
				gazeCtrl.StateId != (int)GazeState.Shifting;
		}
	}
	
	/// <summary>
	/// Constructor.
	/// </summary>
	/// <param name="agent">
	/// Virtual agent object.
	/// </param>
	/// <param name="target">
	/// Gaze target object.
	/// </param>
	public GazeAtAction( GameObject agent, GameObject target ) : base(agent)
	{
		this.target = target;
		gazeCtrl = Subject.GetComponent<GazeController>();
		if( gazeCtrl == null )
		{
			UnityEngine.Debug.LogWarning( "Gaze Controller not defined on object " + Subject.name );
			return;
		}
	}
	
	/// <summary>
	/// Constructor.
	/// </summary>
	/// <param name="agent">
	/// Virtual agent object.
	/// </param>
	/// <param name="target">
	/// Gaze target object.
	/// </param>
	/// <param name="headAlign">
	/// Head alignment parameter.
	/// </param>
	/// /// <param name="headLatency">
	/// Head latency parameter.
	/// </param>
	public GazeAtAction( GameObject agent, GameObject target,
	                    float headAlign, float headLatency ) : base(agent)
	{
		this.target = target;
		this.headAlign = headAlign;
		this.headLatency = headLatency;
		gazeCtrl = Subject.GetComponent<GazeController>();
		if( gazeCtrl == null )
		{
			UnityEngine.Debug.LogWarning( "Gaze Controller not defined on object " + Subject.name );
			return;
		}
	}
	
	/// <summary>
	/// Constructor.
	/// </summary>
	/// <param name="agent">
	/// Virtual agent object.
	/// </param>
	/// <param name="gazeTargetWPos">
	/// Gaze target world position.
	/// </param>
	public GazeAtAction( GameObject agent, Vector3 gazeTargetWPos ) : base(agent)
	{
		this.targetPos = gazeTargetWPos;
		this.dummyTarget = true;
		gazeCtrl = Subject.GetComponent<GazeController>();
		if( gazeCtrl == null )
		{
			UnityEngine.Debug.LogWarning( "Gaze Controller not defined on object " + Subject.name );
			return;
		}
	}
	
	/// <summary>
	/// Constructor.
	/// </summary>
	/// <param name="agent">
	/// Virtual agent object.
	/// </param>
	/// <param name="gazeTargetWPos">
	/// Gaze target world position.
	/// </param>
	/// <param name="headAlign">
	/// Head alignment parameter.
	/// </param>
	/// <param name="headLatency">
	/// Head latency parameter.
	/// </param>
	/// <param name="torsoAlign">
	/// Torso lalignment parameter.
	/// </param>
	/// <param name="torsoLatency">
	/// Torso latency parameter.
	/// </param>
	/// <param name="eyeAlign">
	/// Eye alignment parameter.
	/// </param>
	public GazeAtAction( GameObject agent, Vector3 gazeTargetWPos,
	                    float headAlign, float headLatency ) : base(agent)
	{
		this.targetPos = gazeTargetWPos;
		this.dummyTarget = true;
		this.headAlign = headAlign;
		this.headLatency = headLatency;
		gazeCtrl = Subject.GetComponent<GazeController>();
		if( gazeCtrl == null )
		{
			UnityEngine.Debug.LogWarning( "Gaze Controller not defined on object " + Subject.name );
			return;
		}
	}
	
	/// <summary>
	/// Constructor.
	/// </summary>
	/// <param name="agent">
	/// Virtual agent object.
	/// </param>
	/// <param name="target">
	/// Gaze target object.
	/// </param>
	/// <param name="headAlign">
	/// Head alignment parameter.
	/// </param>
	/// <param name="headLatency">
	/// Head latency parameter.
	/// </param>
	/// <param name="torsoAlign">
	/// Torso lalignment parameter.
	/// </param>
	/// <param name="torsoLatency">
	/// Torso latency parameter.
	/// </param>
	/// <param name="eyeAlign">
	/// Eye alignment parameter.
	/// </param>
	public GazeAtAction( GameObject agent, GameObject target,
	                    float headAlign, float headLatency,
	                    float torsoAlign, float torsoLatency, float eyeAlign ) : base(agent)
	{
		this.target = target;
		this.headAlign = headAlign;
		this.headLatency = headLatency;
		this.torsoAlign = torsoAlign;
		this.torsoLatency = torsoLatency;
		this.eyeAlign = eyeAlign;
		gazeCtrl = Subject.GetComponent<GazeController>();
		if( gazeCtrl == null )
		{
			UnityEngine.Debug.LogWarning( "Gaze Controller not defined on object " + Subject.name );
			return;
		}
	}
	
	/// <summary>
	/// Executes the scenario action. 
	/// </summary>
	public override void Execute()
	{
		if( !gazeCtrl.isEnabled )
			return;
		
		gazeCtrl.Head.align = headAlign;
		if( headLatency != 0 ) gazeCtrl.Head.latency = headLatency;
		if( gazeCtrl.Torso != null )
		{
			gazeCtrl.Torso.align = torsoAlign;
			gazeCtrl.Torso.latency = torsoLatency;
		}
		gazeCtrl.eyeAlign = eyeAlign;
		gazeCtrl.GazeAt(target);
	}
	
	/// <summary>
	/// Stops the scenario action. 
	/// </summary>
	public override void Stop()
	{
	}
};

/// <summary>
/// Class representing an agent head gesture.
/// </summary>
public class HeadGestureAction : ScenarioAction
{
	protected FaceController faceCtrl;
	protected int numGestures = 1;
	protected FaceGestureSpeed gestSpeed = FaceGestureSpeed.Normal;
	protected float gestTargetVert = 0;
	protected float gestTargetHor = 0;
	protected float gestReturnVert = 0;
	protected float gestReturnHor = 0;
	protected float gestFinalVert = 0;
	protected float gestFinalHor = 0;
	protected float gestSustainLength = 0f;
	
	/// <summary>
	/// <see cref="ScenarioAction::Finished"/> 
	/// </summary>
	public override bool Finished
	{
		get
		{
			return !faceCtrl.doGesture &&
				faceCtrl.StateId != (int)FaceState.Gesturing;
		}
	}
	
	/// <summary>
	/// Constructor.
	/// </summary>
	/// <param name="agent">
	/// Virtual agent object.
	/// </param>
	/// <param name="numGestures">
	/// Number of gestures to perform in quick sequence.
	/// </param>
	/// <param name="speed">
	/// Speed of the gesture.
	/// </param>
	/// <param name="targetVert">
	/// Vertical target rotation of the gesture, given in terms of
	/// pitch angle.
	/// </param>
	/// <param name="targetHor">
	/// Horizontal target rotation of the gesture, given in terms of
	/// yaw angle.
	/// </param>
	/// <param name="returnVert">
	/// Vertical return rotation of the gesture, given in terms of
	/// pitch angle.
	/// </param>
	/// <param name="returnHor">
	/// Horizontal return rotation of the gesture, given in terms of
	/// yaw angle.
	/// </param>
	/// <param name="finalVert">
	/// Vertical final rotation of the gesture, given in terms of
	/// pitch angle.
	/// </param>
	/// <param name="finalHor">
	/// Horizontal final rotation of the gesture, given in terms of
	/// yaw angle.
	/// </param>
	/// <param name="sustainLength">
	/// How long the head should remain in the target pose before moving to
	/// return pose, and vice versa (in seconds).
	/// </param>
	public HeadGestureAction( GameObject agent,
	                         int numGestures, FaceGestureSpeed speed,
	                         float targetVert, float targetHor,
	                         float returnVert, float returnHor,
	                         float finalVert, float finalHor,
	                         float sustainLength ) : base(agent)
	{
		this.numGestures = numGestures;
		gestSpeed = speed;
		gestTargetVert = targetVert;
		gestTargetHor = targetHor;
		gestReturnVert = returnVert;
		gestReturnHor = returnHor;
		gestFinalVert = finalVert;
		gestFinalHor = finalHor;
		gestSustainLength = sustainLength;
		faceCtrl = Subject.GetComponent<FaceController>();
		if( faceCtrl == null )
		{
			UnityEngine.Debug.LogWarning( "Face Controller not defined on object " + Subject.name );
			return;
		}
	}
	
	/// <summary>
	/// Executes the scenario action. 
	/// </summary>
	public override void Execute()
	{
		faceCtrl.DoGesture( numGestures, gestSpeed,
		                   gestTargetVert, gestTargetHor,
		                   gestReturnVert, gestReturnHor,
		                   gestFinalVert, gestFinalHor,
		                   gestSustainLength );
	}
	
	/// <summary>
	/// Stops the scenario action. 
	/// </summary>
	public override void Stop()
	{
		faceCtrl.doGesture = false;
	}
};

/// <summary>
/// Class representing an agent facial expression change.
/// </summary>
public class ChangeExpressionAction : ScenarioAction
{
	protected string expr;
	protected float mag;
	protected float time;
	protected ExpressionController exprCtrl;
	
	/// <summary>
	/// <see cref="ScenarioAction::Finished"/> 
	/// </summary>
	public override bool Finished
	{
		get
		{
			return !exprCtrl.changeExpression &&
				exprCtrl.StateId != (int)ExpressionState.Changing;
		}
	}
	
	/// <summary>
	/// Constructor.
	/// </summary>
	/// <param name="agent">
	/// Virtual agent object.
	/// </param>
	/// <param name="expr">
	/// New facial expression.
	/// </param>
	public ChangeExpressionAction( GameObject agent, string expr,
	                              float mag, float time ) : base(agent)
	{
		this.expr = expr;
		this.mag = mag;
		this.time = time;
		exprCtrl = Subject.GetComponent<ExpressionController>();
		if( exprCtrl == null )
		{
			UnityEngine.Debug.LogWarning( "Expression Controller not defined on object " + Subject.name );
			return;
		}
	}
	
	/// <summary>
	/// Executes the scenario action.
	/// </summary>
	public override void Execute()
	{
		exprCtrl.magnitude = mag;
		exprCtrl.changeTime = time;
		exprCtrl.ChangeExpression(expr);
	}
	
	/// <summary>
	/// Stops the scenario action. 
	/// </summary>
	public override void Stop()
	{
	}
};

/// <summary>
/// Class representing an agent arm gesture.
/// </summary>
public class DoGestureAction : ScenarioAction
{
	protected string gesture;
	protected GestureController gestCtrl;
	
	/// <summary>
	/// <see cref="ScenarioAction::Finished"/> 
	/// </summary>
	public override bool Finished
	{
		get
		{
			return !gestCtrl.doGesture &&
				gestCtrl.StateId != (int)GestureState.Gesturing;
		}
	}
	
	/// <summary>
	/// Constructor.
	/// </summary>
	/// <param name="agent">
	/// Virtual agent object.
	/// </param>
	/// <param name="gest">
	/// Arm gesture to perform.
	/// </param>
	public DoGestureAction( GameObject agent, string gesture ) : base(agent)
	{
		this.gesture = gesture;
		gestCtrl = Subject.GetComponent<GestureController>();
		if( gestCtrl == null )
		{
			UnityEngine.Debug.LogWarning( "Gesture Controller not defined on object " + Subject.name );
			return;
		}
	}
	
	/// <summary>
	/// Executes the scenario action. 
	/// </summary>
	public override void Execute()
	{
		gestCtrl.DoGesture(gesture);
	}
	
	/// <summary>
	/// Stops the scenario action. 
	/// </summary>
	public override void Stop()
	{
	}
};

/// <summary>
/// Class representing an agent posture shift.
/// </summary>
public class PostureShiftAction : ScenarioAction
{
	protected string postureAnim;
	protected float time;
	protected float weight;
	protected BodyIdleController bodyIdleCtrl;
	
	/// <summary>
	/// <see cref="ScenarioAction::Finished"/> 
	/// </summary>
	public override bool Finished
	{
		get
		{
			return !bodyIdleCtrl.changePosture &&
				!bodyIdleCtrl.gameObject.animation[postureAnim].enabled;
		}
	}
	
	/// <summary>
	/// Constructor.
	/// </summary>
	/// <param name="agent">
	/// Virtual agent object.
	/// </param>
	/// <param name="postureAnim">
	/// Posture shift to perform.
	/// </param>
	public PostureShiftAction( GameObject agent, string postureAnim ) : base(agent)
	{
		this.postureAnim = postureAnim;
		bodyIdleCtrl = Subject.GetComponent<BodyIdleController>();
		if( bodyIdleCtrl == null )
		{
			UnityEngine.Debug.LogWarning( "Body Idle Controller not defined on object " + Subject.name );
			return;
		}
	}
	
	/// <summary>
	/// Constructor.
	/// </summary>
	/// <param name="agent">
	/// Virtual agent object.
	/// </param>
	/// <param name="postureAnim">
	/// Posture shift to perform.
	/// </param>
	/// <param name="time">How long posture shift should take.</param>
	/// <param name="weight">How strongly the new posture should be applied.</param>
	public PostureShiftAction( GameObject agent, string postureAnim,
	                          float time, float weight ) : base(agent)
	{
		this.postureAnim = postureAnim;
		this.time = time;
		this.weight = weight;
		bodyIdleCtrl = Subject.GetComponent<BodyIdleController>();
		if( bodyIdleCtrl == null )
		{
			UnityEngine.Debug.LogWarning( "Body Idle Controller not defined on object " + Subject.name );
			return;
		}
	}
	
	/// <summary>
	/// Executes the scenario action. 
	/// </summary>
	public override void Execute()
	{
		bodyIdleCtrl.ChangePosture(postureAnim);
		bodyIdleCtrl.postureChangeTime = time;
		bodyIdleCtrl.postureWeight = weight;
	}
	
	/// <summary>
	/// Stops the scenario action. 
	/// </summary>
	public override void Stop()
	{
	}
};

/// <summary>
/// Class representing an agent speech utterance.
/// </summary>
public class SpeakAction : ScenarioAction
{
	protected string speech;
	protected SpeechController speechCtrl;
	protected GazeAversionController gazeAversionCtrl;
	protected SpeechType typeOfSpeech = SpeechType.Other;
	
	/// <summary>
	/// <see cref="ScenarioAction::Finished"/> 
	/// </summary>
	public override bool Finished
	{
		get
		{
			return !speechCtrl.doSpeech &&
				speechCtrl.StateId != (int)SpeechState.Speaking &&
				speechCtrl.StateId != (int)SpeechState.PrepareSpeech;
		}
	}
	
	/// <summary>
	/// Constructor. A simple speech utterance straight to the camera.
	/// </summary>
	/// <param name="agent">
	/// Virtual agent object.
	/// </param>
	/// <param name="speech">
	/// Speech utterance to render.
	/// </param>
	public SpeakAction( GameObject agent, string speech ) : base(agent)
	{
		this.speech = speech;
		speechCtrl = agent.GetComponent<SpeechController>();
		if( speechCtrl == null )
		{
			UnityEngine.Debug.LogWarning( "Speech Controller not defined on object " + agent.name );
			return;
		}
	}
	
	/// <summary>
	/// Constructor. A specific type of utterance, directed to the camera.
	/// </summary>
	/// <param name="agent">
	/// Virtual agent object.
	/// </param>
	/// <param name="speech">
	/// Speech utterance name.
	/// </param>
	/// <param name="typeOfSpeech">
	/// Type of speech utterance (Question, Answer, or Other)
	/// </param>
	public SpeakAction( GameObject agent, string speech, SpeechType typeOfSpeech ) : base(agent)
	{
		this.speech = speech;
		this.typeOfSpeech = typeOfSpeech;
		speechCtrl = agent.GetComponent<SpeechController>();
		gazeAversionCtrl = agent.GetComponent<GazeAversionController>();
		if( speechCtrl == null )
		{
			UnityEngine.Debug.LogWarning( "Speech Controller not defined on object " + agent.name );
			return;
		}
		if( gazeAversionCtrl == null )
		{
			UnityEngine.Debug.LogWarning( "Gaze Aversion Controller not defined on object " + agent.name );
			return;
		}
		gazeAversionCtrl.doCognitiveGazeShift = false;
	}
	
	public SpeakAction( GameObject agent, string speech, SpeechType typeOfSpeech, bool doCog) : base(agent)
	{
		this.speech = speech;
		this.typeOfSpeech = typeOfSpeech;
		speechCtrl = agent.GetComponent<SpeechController>();
		gazeAversionCtrl = agent.GetComponent<GazeAversionController>();
		if( speechCtrl == null )
		{
			UnityEngine.Debug.LogWarning( "Speech Controller not defined on object " + agent.name );
			return;
		}
		if( gazeAversionCtrl == null )
		{
			UnityEngine.Debug.LogWarning( "Gaze Aversion Controller not defined on object " + agent.name );
			return;
		}
		gazeAversionCtrl.doCognitiveGazeShift = doCog;
	}
	
	/// <summary>
	/// Constructor. A specific type of utterance, directed to another agent.
	/// </summary>
	/// <param name="agent">
	/// Virtual agent object.
	/// </param>
	/// <param name="otherAgent">
	/// Virtual agent being spoken to.
	/// </param>
	/// <param name="speech">
	/// Speech utterance name.
	/// </param>
	/// <param name="typeOfSpeech">
	/// Type of speech utterance (Question, Answer, or Other)
	/// </param>
	public SpeakAction( GameObject agent, GameObject otherAgent, string speech, SpeechType typeOfSpeech ) : base(agent)
	{
		this.speech = speech;
		this.typeOfSpeech = typeOfSpeech;
		speechCtrl = agent.GetComponent<SpeechController>();
		gazeAversionCtrl = agent.GetComponent<GazeAversionController>();
		if( speechCtrl == null )
		{
			UnityEngine.Debug.LogWarning( "Speech Controller not defined on object " + agent.name );
			return;
		}
		if( gazeAversionCtrl == null )
		{
			UnityEngine.Debug.LogWarning( "Gaze Aversion Controller not defined on object " + agent.name );
			return;
		}
		gazeAversionCtrl.doCognitiveGazeShift = false;
	}
	
	public SpeakAction( GameObject agent, GameObject otherAgent, string speech, SpeechType typeOfSpeech, float cogStart, float cogEnd, GazeAversionTarget target ) : base(agent)
	{
		this.speech = speech;
		this.typeOfSpeech = typeOfSpeech;
		speechCtrl = agent.GetComponent<SpeechController>();
		gazeAversionCtrl = agent.GetComponent<GazeAversionController>();
		if( speechCtrl == null )
		{
			UnityEngine.Debug.LogWarning( "Speech Controller not defined on object " + agent.name );
			return;
		}
		if( gazeAversionCtrl == null )
		{
			UnityEngine.Debug.LogWarning( "Gaze Aversion Controller not defined on object " + agent.name );
			return;
		}
		gazeAversionCtrl.doCognitiveGazeShift = true;
		gazeAversionCtrl.setTargetType(target);
		gazeAversionCtrl.setCognitiveParameters(cogStart, cogEnd);
	}
	
	/// <summary>
	/// Executes the scenario action. 
	/// </summary>
	public override void Execute()
	{
		speechCtrl.speechType = this.typeOfSpeech;
		speechCtrl.Speak(speech);
	}
	
	/// <summary>
	/// Stops the scenario action. 
	/// </summary>
	public override void Stop()
	{
		speechCtrl.StopSpeech();
	}
};

public class ListenAction : ScenarioAction
{
	protected ListenController listenCtrl;
	private bool openListening = false;
	
	public override bool Finished
	{
		get
		{
			//if (!listenCtrl.startListening && listenCtrl.StateId == (int)ListenState.NotListening)
			//	Debug.Log("Listening Finished!");
			//return !listenCtrl.startListening && listenCtrl.StateId == (int)ListenState.NotListening;
			return !listenCtrl.startListening && !listenCtrl.startOpenListening && listenCtrl.StateId == (int)ListenState.NotListening;
		}
	}
	
	public ListenAction( GameObject agent ) : base(agent)
	{
		listenCtrl = agent.GetComponent<ListenController>();
		openListening = false;
		if( listenCtrl == null )
		{
			UnityEngine.Debug.LogWarning( "Listen Controller not defined on object " + agent.name );
			return;
		}
	}
	
	public ListenAction( GameObject agent, bool open ) : base(agent)
	{
		listenCtrl = agent.GetComponent<ListenController>();
		openListening = open;
		if( listenCtrl == null )
		{
			UnityEngine.Debug.LogWarning( "Listen Controller not defined on object " + agent.name );
			return;
		}
	}
	
	public override void Execute()
	{
		if (openListening)
			listenCtrl.startOpenListening = true;
		else
			listenCtrl.startListening = true;
	}
	
	public override void Stop()
	{
		listenCtrl.stopListening = true;
	}
}

/// <summary>
/// Class representing an animation play action on an object.
/// </summary>
public class PlayAnimationAction : ScenarioAction
{
	protected string anim;
	protected bool loop = false;
	protected float speed = 1f;
	
	/// <summary>
	/// <see cref="ScenarioAction::Finished"/> 
	/// </summary>
	public override bool Finished
	{
		get
		{
			return !subj.animation[anim].enabled;
		}
	}
	
	/// <summary>
	/// Constructor.
	/// </summary>
	/// <param name="subj">
	/// Game object which is the subject of the action
	/// (e.g. agent performing the action).<see cref="GameObject"/>
	/// </param>
	/// <param name="anim">
	/// Animation name.
	/// </param>
	public PlayAnimationAction( GameObject subj, string anim ) : base(subj)
	{
		this.anim = anim;
	}
	
	/// <summary>
	/// Constructor.
	/// </summary>
	/// <param name="subj">
	/// Game object which is the subject of the action
	/// (e.g. agent performing the action).<see cref="GameObject"/>
	/// </param>
	/// <param name="anim">
	/// Animation name.
	/// </param>
	/// <param name="loop">
	/// If true, animation will loop.
	/// </param>
	/// <param name="speed">
	/// Animation playback speed (1 is normal speed).
	/// </param>
	public PlayAnimationAction( GameObject subj, string anim,
	                          bool loop, float speed ) : base(subj)
	{
		this.anim = anim;
		this.loop = loop;
		this.speed = speed;
	}
	
	/// <summary>
	/// Executes the scenario action. 
	/// </summary>
	public override void Execute()
	{
		subj.animation[anim].weight = 1f;
		subj.animation[anim].wrapMode = loop ? WrapMode.Loop : WrapMode.Once;
		subj.animation[anim].speed = speed;
		subj.animation[anim].enabled = true;
	}
	
	/// <summary>
	/// Stops the scenario action. 
	/// </summary>
	public override void Stop()
	{
		subj.animation[anim].enabled = false;
	}
};

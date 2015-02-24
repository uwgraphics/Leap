using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum ScenarioState
{
    Starting,
    Running,
    Finished,
    Error
};

/// <summary>
/// Base class for classes implementing scenario logic
/// for scenarios involving one or more virtual agents.
/// </summary>
public abstract class Scenario : MonoBehaviour, System.Collections.IEnumerable
{
    /// <summary>
    /// Scenario name (also scene file name).
    /// </summary>
    public string sceneName = "";

    // TODO: implement scenario branching based on experimental conditions

    // Scenario state variables:

    protected ScenarioState state = ScenarioState.Starting;
    protected string errorMsg = "";
    protected Dictionary<int, ScenarioAction> actionsInProgress = new Dictionary<int, ScenarioAction>();
    protected int nextActionId = 0;

    // Some cached objects:

    protected Dictionary<string, GameObject> agents = new Dictionary<string, GameObject>();
    protected Dictionary<string, GameObject> photoPanels = new Dictionary<string, GameObject>();
    protected Dictionary<string, GameObject> props = new Dictionary<string, GameObject>();
    protected Dictionary<string, GameObject> gazeTargets = new Dictionary<string, GameObject>();
    protected Dictionary<string, GameObject> cameras = new Dictionary<string, GameObject>();
    protected Dictionary<string, GameObject> lights = new Dictionary<string, GameObject>();
    protected Dictionary<string, GameObject> objects = new Dictionary<string, GameObject>();

    /// <summary>
    /// The current state of scenario execution.
    /// </summary>
    public virtual ScenarioState State
    {
        get
        {
            return state;
        }
    }

    /// <summary>
    /// Last error message.
    /// </summary>
    public virtual string ErrorMessage
    {
        get
        {
            return errorMsg;
        }
    }

    /// <summary>
    /// Indexer for getting a scenario object.
    /// </summary>
    /// <param name="objName">
    /// Object name.
    /// </param>
    public virtual GameObject this[string objName]
    {
        get
        {
            return objects[objName];
        }
    }

    /// <summary>
    /// Performs an action.
    /// </summary>
    /// <param name="action">
    /// Action to execute <see cref="ScenarioAction"/>
    /// </param>
    /// <returns>
    /// Action ID.
    /// </returns>
    public virtual int DoAction(ScenarioAction action)
    {
        int action_id = nextActionId++;
        actionsInProgress.Add(action_id, action);
        action.Execute();

        return action_id;
    }

    /// <summary>
    /// Cancels an ongoing action.
    /// </summary>
    /// <param name="actionId">
    /// Action ID.
    /// </param>
    public virtual void CancelAction(int actionId)
    {
        if (!actionsInProgress.ContainsKey(actionId))
            return;

        actionsInProgress[actionId].Stop();
        actionsInProgress.Remove(actionId);
    }

    /// <summary>
    /// Cancels all ongoing actions.
    /// </summary>
    public virtual void CancelAllActions()
    {
        foreach (KeyValuePair<int, ScenarioAction> idact in actionsInProgress)
            idact.Value.Stop();
        actionsInProgress.Clear();
    }

    /// <summary>
    /// Gets an ongoing action by ID.
    /// </summary>
    /// <param name="actionId">
    /// Action ID.
    /// </param>
    /// <returns>
    /// Action. <see cref="ScenarioAction"/>
    /// </returns>
    public virtual ScenarioAction GetAction(int actionId)
    {
        return actionsInProgress.ContainsKey(actionId) ? actionsInProgress[actionId] :
            null;
    }

    public System.Collections.IEnumerator GetEnumerator()
    {
        foreach (KeyValuePair<int, ScenarioAction> idact in actionsInProgress)
            yield return idact;
    }

    /// <summary>
    /// Have an agent gaze at something.
    /// </summary>
    /// <param name="agentName">
    /// Virtual agent.
    /// </param>
    /// <param name="target">
    /// Target object. <see cref="GameObject"/>
    /// </param>
    /// <returns>
    /// Action ID.
    /// </returns>
    public virtual int GazeAt(string agentName, string targetName)
    {
        return DoAction(new GazeAtAction(agents[agentName], objects[targetName]));
    }

    /// <summary>
    /// Have an agent gaze at something.
    /// </summary>
    /// <param name="agentName">
    /// Virtual agent.
    /// </param>
    /// <param name="target">
    /// Target object. <see cref="GameObject"/>
    /// </param>
    /// <param name="headAlign">
    /// Head alignment parameter.
    /// </param>
    /// <param name="headLatency">
    /// Head latency parameter.
    /// </param>
    /// <returns>
    /// Action ID.
    /// </returns>
    public virtual int GazeAt(string agentName, string targetName,
                              float headAlign, float headLatency)
    {
        return DoAction(new GazeAtAction(agents[agentName], objects[targetName],
                                          headAlign, headLatency));
    }

    /// <summary>
    /// Have an agent gaze at something.
    /// </summary>
    /// <param name="agentName">
    /// Virtual agent.
    /// </param>
    /// <param name="target">
    /// Target object. <see cref="GameObject"/>
    /// </param>
    /// <param name="headAlign">
    /// Head alignment parameter.
    /// </param>
    /// <param name="headLatency">
    /// Head latency parameter.
    /// </param>
    /// <returns>
    /// Action ID.
    /// </returns>
    public virtual int GazeAt(string agentName, GameObject target,
                              float headAlign, float headLatency)
    {
        return DoAction(new GazeAtAction(agents[agentName], target,
                                          headAlign, headLatency));
    }

    /// <summary>
    /// Have an agent gaze at something.
    /// </summary>
    /// <param name="agentName">
    /// Virtual agent.
    /// </param>
    /// <param name="target">
    /// Target object. <see cref="GameObject"/>
    /// </param>
    /// <param name="headAlign">
    /// Head alignment parameter.
    /// </param>
    /// <param name="headLatency">
    /// Head latency parameter.
    /// </param>
    /// <param name="torsoAlign">
    /// Torso alignment parameter.
    /// </param>
    /// <param name="torsoLatency">
    /// Torso latency parameter.
    /// </param>
    /// <param name="eyeAlign">
    /// Torso alignment parameter.
    /// </param>
    /// <returns>
    /// Action ID.
    /// </returns>
    public virtual int GazeAt(string agentName, string targetName,
                              float headAlign, float headLatency,
                              float torsoAlign, float torsoLatency,
                              float eyeAlign)
    {
        return DoAction(new GazeAtAction(agents[agentName], objects[targetName],
                                          headAlign, headLatency, torsoAlign, torsoLatency, eyeAlign));
    }

    /// <summary>
    /// Have an agent gaze at something.
    /// </summary>
    /// <param name="agentName">
    /// Virtual agent.
    /// </param>
    /// <returns>
    /// Action ID.
    /// </returns>
    public virtual int GazeAtCamera(string agentName)
    {
        GameObject cam = GameObject.FindGameObjectWithTag("EyeContactHelper");
        if (cam == null)
            cam = GameObject.FindGameObjectWithTag("MainCamera");
        return DoAction(new GazeAtAction(agents[agentName], cam));
    }

    /// <summary>
    /// Have an agent gaze at something.
    /// </summary>
    /// <param name="agentName">
    /// Virtual agent.
    /// </param>
    /// <param name="headAlign">
    /// Head alignment parameter.
    /// </param>
    /// <param name="headLatency">
    /// Head latency parameter.
    /// </param>
    /// <returns>
    /// Action ID.
    /// </returns>
    public virtual int GazeAtCamera(string agentName, float headAlign, float headLatency)
    {
        GameObject cam = GameObject.FindGameObjectWithTag("EyeContactHelper");
        if (cam == null)
            cam = GameObject.FindGameObjectWithTag("MainCamera");
        return DoAction(new GazeAtAction(agents[agentName], cam,
                                          headAlign, headLatency));
    }

    /// <summary>
    /// Have an agent gaze at something.
    /// </summary>
    /// <param name="agentName">
    /// Virtual agent.
    /// </param>
    /// <param name="headAlign">
    /// Head alignment parameter.
    /// </param>
    /// <param name="headLatency">
    /// Head latency parameter.
    /// </param>
    /// <param name="torsoAlign">
    /// Torso alignment parameter.
    /// </param>
    /// <param name="torsoLatency">
    /// Head latency parameter.
    /// </param>
    /// <returns>
    /// Action ID.
    /// </returns>
    public virtual int GazeAtCamera(string agentName, float headAlign, float headLatency,
                                    float torsoAlign, float torsoLatency, float eyeAlign)
    {
        GameObject cam = GameObject.FindGameObjectWithTag("EyeContactHelper");
        if (cam == null)
            cam = GameObject.FindGameObjectWithTag("MainCamera");
        return DoAction(new GazeAtAction(agents[agentName], cam,
                                          headAlign, headLatency,
                                          torsoAlign, torsoLatency, eyeAlign));
    }

    /// <summary>
    /// Have an agent change its facial expression.
    /// </summary>
    /// <param name="agentName">
    /// Virtual agent.
    /// </param>
    /// <param name="expr">
    /// New facial expression.
    /// </param>
    /// <param name="mag">
    /// Facial expression magnitude.
    /// </param>
    /// <param name="time">
    /// How long the expression change should take (in seconds).
    /// </param>
    /// <returns>
    /// Action ID.
    /// </returns>
    public virtual int ChangeExpression(string agentName, string expr, float mag, float time)
    {
        return DoAction(new ChangeExpressionAction(agents[agentName], expr, mag, time));
    }

    /// <summary>
    /// Performs a head nod gesture.
    /// </summary>
    /// <param name="agentName">
    /// Virtual agent.
    /// </param>
    /// <param name="numNods">
    /// Number of gestures to perform in quick sequence.
    /// </param>
    /// <param name="speed">
    /// Speed of the gesture.
    /// </param>
    /// <param name="targetVert">
    /// Vertical target rotation of the gesture, given in terms of
    /// pitch angle.
    /// </param>
    /// <param name="sustainLength">
    /// How long the head should remain in the target pose before moving to
    /// return pose, and vice versa (in seconds).
    /// </param>
    public virtual int HeadNod(string agentName, int numNods, FaceGestureSpeed speed,
                               float targetVert, float sustainLength)
    {
        return DoAction(new HeadGestureAction(agents[agentName],
                                               numNods, speed, targetVert, 0,
                                               0, 0, 0, 0, sustainLength));
    }

    /// <summary>
    /// Performs a head shake gesture.
    /// </summary>
    /// <param name="agentName">
    /// Virtual agent.
    /// </param>
    /// <param name="numShakes">
    /// Number of gestures to perform in quick sequence.
    /// </param>
    /// <param name="speed">
    /// Speed of the gesture.
    /// </param>
    /// <param name="targetHor">
    /// Vertical target rotation of the gesture, given in terms of
    /// pitch angle.
    /// </param>
    /// <param name="sustainLength">
    /// How long the head should remain in the target pose before moving to
    /// return pose, and vice versa (in seconds).
    /// </param>
    public virtual int HeadShake(string agentName, int numShakes, FaceGestureSpeed speed,
                                 float targetHor, float sustainLength)
    {
        return DoAction(new HeadGestureAction(agents[agentName],
                                               numShakes, speed, 0, -targetHor,
                                               targetHor, 0, 0, 0, sustainLength));
    }

    /// <summary>
    /// Have an agent perform an arm gesture.
    /// </summary>
    /// <param name="agentName">
    /// Virtual agent.
    /// </param>
    /// <param name="target">
    /// Gesture name.
    /// </param>
    /// <returns>
    /// Action ID.
    /// </returns>
    public virtual int DoGesture(string agentName, string gest)
    {
        return DoAction(new DoGestureAction(agents[agentName], gest));
    }

    /// <summary>
    /// Have an agent perform a posture shift.
    /// </summary>
    /// <param name="agentName">
    /// Virtual agent.
    /// </param>
    /// <param name="postureAnim">
    /// Posture shift to perform.
    /// </param>
    /// <returns>
    /// Action ID.
    /// </returns>
    public virtual int PostureShift(string agentName, string postureAnim)
    {
        return DoAction(new PostureShiftAction(agents[agentName], postureAnim));
    }

    /// <summary>
    /// Have an agent perform a posture shift.
    /// </summary>
    /// <param name="agentName">
    /// Virtual agent.
    /// </param>
    /// <param name="postureAnim">
    /// Posture shift to perform.
    /// </param>
    /// <param name="time">How long posture shift should take.</param>
    /// <param name="weight">How strongly the new posture should be applied.</param>
    /// <returns>
    /// Action ID.
    /// </returns>
    public virtual int PostureShift(string agentName, string postureAnim,
                                    float time, float weight)
    {
        return DoAction(new PostureShiftAction(agents[agentName], postureAnim, time, weight));
    }

    /// <summary>
    /// Have an agent perform a speech utterance.
    /// </summary>
    /// <param name="agentName">
    /// Virtual agent.
    /// </param>
    /// <param name="speech">
    /// Speech utterance name.
    /// </param>
    /// <returns>
    /// Action ID.
    /// </returns>
    public virtual int Speak(string agentName, string speech)
    {
        return DoAction(new SpeakAction(agents[agentName], speech));
    }

    /// <summary>
    /// Have an agent perform a speech utterance.
    /// </summary>
    /// <param name="agentName">
    /// Virtual agent.
    /// </param>
    /// <param name="speech">
    /// Speech utterance name.
    /// </param>
    /// <param name="typeOfSpeech">
    /// Type of speech utterance (Question, Answer, or Other)
    /// </param>
    /// <returns>
    /// Action ID.
    /// </returns>
    public virtual int Speak(string agentName, string speech, SpeechType typeOfSpeech)
    {
        return DoAction(new SpeakAction(agents[agentName], speech, typeOfSpeech));
    }

    public virtual int Speak(string agentName, string speech, SpeechType typeOfSpeech, bool doCog)
    {
        return DoAction(new SpeakAction(agents[agentName], speech, typeOfSpeech, doCog));
    }

    /// <summary>
    /// Have an agent perform a speech utterance. A specific type of utterance, directed to another agent.
    /// </summary>
    /// <param name="agentName">
    /// Virtual agent.
    /// </param>
    /// <param name="otherAgentName">
    /// Virtual agent being spoken to.
    /// </param>
    /// <param name="speech">
    /// Speech utterance name.
    /// </param>
    /// <param name="typeOfSpeech">
    /// Type of speech utterance (Question, Answer, or Other)
    /// </param>
    /// <returns>
    /// Action ID.
    /// </returns>
    public virtual int Speak(string agentName, string otherAgentName, string speech, SpeechType typeOfSpeech)
    {
        return DoAction(new SpeakAction(agents[agentName], agents[otherAgentName], speech, typeOfSpeech));
    }


    public virtual int Speak(string agentName, string otherAgentName, string speech, SpeechType typeOfSpeech, float cogStart, float cogEnd)
    {
        return DoAction(new SpeakAction(agents[agentName], agents[otherAgentName], speech, typeOfSpeech, cogStart, cogEnd, GazeAversionTarget.None));
    }

    public virtual int Speak(string agentName, string otherAgentName, string speech, SpeechType typeOfSpeech, float cogStart, float cogEnd, GazeAversionTarget target)
    {
        return DoAction(new SpeakAction(agents[agentName], agents[otherAgentName], speech, typeOfSpeech, cogStart, cogEnd, target));
    }

    /// <summary>
    /// Have an agent listen to someone speaking.
    /// </summary>
    /// <param name="agentName">
    /// Virtual agent.
    /// </param>
    /// <returns>
    /// Action ID.
    /// </returns>
    public virtual int Listen(string agentName)
    {
        return DoAction(new ListenAction(agents[agentName]));
    }

    /// <summary>
    /// Have an agent listen to someone speaking. (but no response)
    /// </summary>
    /// <param name="agentName">
    /// Virtual agent.
    /// </param>
    /// <returns>
    /// Action ID.
    /// </returns>
    public virtual int OpenListen(string agentName)
    {
        return DoAction(new ListenAction(agents[agentName], true));
    }

    /// <summary>
    /// Play an animation on an object.
    /// </summary>
    /// <param name="obj">
    /// Affected object.
    /// </param>
    /// <param name="anim">
    /// Animation name.
    /// </param>
    /// <returns>
    /// Action ID.
    /// </returns>
    public virtual int PlayAnimation(string objName, string anim)
    {
        return DoAction(new PlayAnimationAction(objects[objName], anim));
    }

    /// <summary>
    /// Play an animation on an object.
    /// </summary>
    /// <param name="obj">
    /// Affected object.
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
    /// <returns>
    /// Action ID.
    /// </returns>
    public virtual int PlayAnimation(string objName, string anim,
                                     bool loop, float speed)
    {
        return DoAction(new PlayAnimationAction(objects[objName], anim,
                                                 loop, speed));
    }

    /// <summary>
    /// Sets the photo to be displayed on a photo panel.
    /// </summary>
    /// <param name="panelName">
    /// Photo panel name.
    /// </param>
    /// <param name="photoName">
    /// Photo texture name.
    /// </param>
    public virtual void ShowPhoto(string panelName, string photoName)
    {
        PhotoPanel pp = photoPanels[panelName].GetComponent<PhotoPanel>();
        pp.ShowPhoto(photoName);
    }

    /// <summary>
    /// Set values of light settings.
    /// </summary>
    /// <param name="lightName">
    /// Light name.
    /// </param>
    /// <param name="color">
    /// Light color.
    /// </param>
    /// <param name="intensity">
    /// Light intensity.
    /// </param>
    public virtual void SetLight(string lightName, Color color, float intensity)
    {
        Light light = lights[lightName].GetComponent<Light>();
        light.color = color;
        light.intensity = intensity;
    }

    /// <summary>
    /// Activate/deactivate game object.
    /// </summary>
    /// <param name="objName">
    /// Object name.
    /// </param>
    /// <param name="active">
    /// true to activate, false to deactivate.
    /// </param>
    public virtual void SetObjectActive(string objName, bool active)
    {
        if (!objects.ContainsKey(objName))
            return;

        objects[objName].active = active;
    }

    /// <summary>
    /// Executes the scenario
    /// </summary>
    /// <returns>
    /// A <see cref="IEnumerator"/>
    /// </returns>
    public virtual IEnumerator Run()
    {
        _CacheObjects();

        // Initialize the scenario
        _Init();

        if (state != ScenarioState.Error)
        {
            // Initialization went OK

            state = ScenarioState.Running;

            // Execute the scenario
            yield return StartCoroutine(_Run());
        }

        // Finish scenario execution
        _Finish();

        if (state != ScenarioState.Error)
        {
            // Scenario execution successful

            state = ScenarioState.Finished;
        }
    }

    /// <summary>
    /// Initializes the scenario.
    /// </summary>
    /// <remarks>Put any initialization logic here.</remarks>
    protected abstract void _Init();

    /// <summary>
    /// Executes the scenario.
    /// </summary>
    /// <remarks>The bulk of your scenario logic goes here.</remarks>
    protected abstract IEnumerator _Run();

    /// <summary>
    /// Finishes scenario execution.
    /// </summary>
    /// <remarks>If you need to release any resources or revert any changes
    /// made by the scenario, you can do that here.</remarks>
    protected abstract void _Finish();

    /// <summary>
    /// Signals error in scenario execution.
    /// </summary>
    /// <param name="message">
    /// Error message <see cref="System.String"/>
    /// </param>
    /// <remarks>Invoke this when you hit an error condition.
    /// Finish() will still execute.</remarks>
    protected virtual void _Error(string message)
    {
        state = ScenarioState.Error;
        errorMsg = message;

        Debug.LogError(message);
    }

    protected virtual void Awake()
    {
    }

    protected virtual void Start()
    {
        if (Object.FindObjectOfType(typeof(ScenarioManager)) == null)
        {
            // No scenario manager in the scene - run just this scenario

            StartCoroutine(Run());
        }
    }

    /// <summary>
    /// Scenario execution will pause until an action has finished executing.
    /// </summary>
    /// <param name="actionId">
    /// Action to wait for.
    /// </param>
    protected virtual IEnumerator WaitUntilFinished(int actionId)
    {
        while (actionsInProgress.ContainsKey(actionId) &&
              !actionsInProgress[actionId].Finished)
        {
            yield return 0;
        }
    }

    /// <summary>
    /// Scenario execution will pause until the animation controller
    /// is in a specified state.
    /// </summary>
    /// <param name="agentName">
    /// Agent name.
    /// </param>
    /// <param name="ctrlName">
    /// Controller name.
    /// </param>
    /// <param name="stateName">
    /// Controller state name.
    /// </param>
    protected virtual IEnumerator WaitForControllerState(string agentName, string ctrlName, string stateName)
    {
        AnimController ctrl = agents[agentName].GetComponent(ctrlName) as AnimController;
        while (ctrl.State != stateName)
        {
            yield return 0;
        }
    }

    protected virtual void _CacheObjects()
    {
        GameObject[] objs = GameObject.FindGameObjectsWithTag("Agent");
        foreach (GameObject obj in objs)
        {
            agents[obj.name] = obj;
            objects[obj.name] = obj;
        }
        objs = GameObject.FindGameObjectsWithTag("PhotoPanel");
        foreach (GameObject obj in objs)
        {
            photoPanels[obj.name] = obj;
            objects[obj.name] = obj;
        }
        objs = GameObject.FindGameObjectsWithTag("Prop");
        foreach (GameObject obj in objs)
        {
            props[obj.name] = obj;
            objects[obj.name] = obj;
        }
        objs = GameObject.FindGameObjectsWithTag("GazeTarget");
        foreach (GameObject obj in objs)
        {
            gazeTargets[obj.name] = obj;
            objects[obj.name] = obj;
        }
        objs = GameObject.FindGameObjectsWithTag("MainCamera");
        foreach (GameObject obj in objs)
        {
            cameras[obj.name] = obj;
            objects[obj.name] = obj;
        }
        objs = GameObject.FindGameObjectsWithTag("MainLight");
        foreach (GameObject obj in objs)
        {
            lights[obj.name] = obj;
            objects[obj.name] = obj;
        }
    }
}

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Abstract base class for atomic behavior actions.
/// </summary>
[Serializable]
public abstract class BehaviorAction
{
    protected GameObject agent = null;

    public virtual GameObject _Agent
    {
        get
        {
            return agent;
        }
        set
        {
            agent = value;
        }
    }

    /// <summary>
    /// Executes the behavior action.
    /// </summary>
    /// <returns>
    /// true if successful, false otherwise.
    /// </returns>
    public abstract bool Execute();

    /// <summary>
    /// Stops the behavior action.
    /// </summary>
    public abstract void Stop();

    /// <summary>
    /// Updates the behavior action.
    /// </summary>
    /// <returns>
    /// true if successful, false otherwise.
    /// </returns>
    public abstract bool Update();

    /// <summary>
    /// true if the behavior action has finished executing, false otherwise.
    /// </summary>
    public abstract bool IsFinished();
}

/// <summary>
/// Gaze behavior action.
/// </summary>
[Serializable]
public class GazeAction : BehaviorAction
{
    /// <summary>
    /// Gaze shift start time.
    /// </summary>
    public float startTime;

    /// <summary>
    /// Gaze shift end time.
    /// </summary>
    public float endTime;

    /// <summary>
    /// Gaze target name.
    /// </summary>
    public string target;

    /// <summary>
    /// How much the head aligns with the gaze target.
    /// </summary>
    public float headAlign = 1f;

    protected GazeController gazeCtrl;
    protected GameObject trgObj;
    protected float time = 0;
    protected bool gazing = false;
    protected bool finished = false;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="startTime">
    /// Gaze shift start time.
    /// </param>
    /// <param name="endTime">
    /// Gaze end time.
    /// </param>
    /// <param name="target">
    /// Gaze target name.
    /// </param>
    public GazeAction(float startTime, float endTime,
                      string target)
    {
        this.startTime = startTime;
        this.endTime = endTime;
        this.target = target;
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="startTime">
    /// Gaze shift start time.
    /// </param>
    /// <param name="endTime">
    /// Gaze end time.
    /// </param>
    /// <param name="target">
    /// Gaze target name.
    /// </param>
    /// <param name="headAlign">
    /// How much the head aligns with the gaze target.
    /// </param>
    public GazeAction(float startTime, float endTime,
                      string target, float headAlign)
    {
        this.startTime = startTime;
        this.endTime = endTime;
        this.target = target;
        this.headAlign = headAlign;
    }

    /// <see cref="BehaviorAction.Execute"/>
    public override bool Execute()
    {
        // Get the gaze controller
        gazeCtrl = agent.GetComponent<GazeController>();
        if (gazeCtrl == null)
        {
            Debug.LogWarning("Unable to execute gaze behavior action: agent " +
                             agent.name + " does not have a Gaze Controller.");
            return false;
        }

        if (target == "unknown")
        {
            // This just means we keep gazing at the previous target
            finished = true;
            return false;
        }

        // Find the gaze target
        trgObj = GameObject.FindGameObjectWithTag(target);
        if (trgObj == null)
            trgObj = GameObject.Find(target);
        if (trgObj == null)
        {
            Debug.LogWarning("Unable to execute gaze behavior action: gaze target " +
                             target + " does not exist.");
            return false;
        }

        // Must know when done gazing
        gazeCtrl.StateChange += new StateChangeEvtH(GazeController_StateChange);

        return true;
    }

    /// <see cref="BehaviorAction.Stop"/>
    public override void Stop()
    {
        // TODO: stop gaze shift
    }

    /// <see cref="BehaviorAction.Update"/>
    public override bool Update()
    {
        time += Time.deltaTime;

        if (time > startTime && !gazing)
        {
            // Time to gaze on target
            gazeCtrl.GazeAt(trgObj);
            // TODO: set gaze duration correctly
            //gazeCtrl.gazeHoldTime = endTime - startTime;
            gazing = true;

            // Set gaze parameters
            gazeCtrl.Head.align = headAlign;
        }

        return true;
    }

    /// <see cref="BehaviorAction.IsFinished"/>
    public override bool IsFinished()
    {
        return finished;
    }

    private void GazeController_StateChange(AnimController sender, int srcState, int trgState)
    {
        if ( /*srcState == (int)GazeState.FixedOnTarget &&*/
           trgState == (int)GazeState.NoGaze)
        {
            finished = true;
        }
    }
}

/// <summary>
/// Speech behavior action.
/// </summary>
[Serializable]
public class SpeechAction : BehaviorAction
{
    /// <summary>
    /// Audio clip name or speech content.
    /// </summary>
    public string speech;

    protected SpeechController speechCtrl;
    protected bool finished = false;
    // TODO: this is a hack to introduce pause between sentences!
    // (this should be addressed in a *principled way*)
    protected bool speechFinished = false;
    protected float pauseLength = 0.8f;
    protected float pauseTime = 0f;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="speech">
    /// Audio clip name or speech content.
    /// </param>
    public SpeechAction(string speech)
    {
        this.speech = speech;
    }

    /// <see cref="BehaviorAction.Execute"/>
    public override bool Execute()
    {
        // Get the speech controller
        speechCtrl = agent.GetComponent<SpeechController>();
        if (speechCtrl == null)
        {
            Debug.LogWarning("Unable to execute speech behavior action: agent " +
                             agent.name + " does not have a Speech Controller.");
            return false;
        }

        // Find the speech clip, or use as text-to-speech
        speechCtrl.Speak(speech);
        if (!speechCtrl.doSpeech)
            speechCtrl.SpeakText(speech);
        if (!speechCtrl.doSpeech)
        {
            Debug.LogWarning("Unable to execute speech behavior action: speech string " +
                             speech + " does not refer to a speech clip or utterable speech content.");
            return false;
        }

        // Must know when done speaking
        speechCtrl.StateChange += new StateChangeEvtH(SpeechController_StateChange);

        return true;
    }

    /// <see cref="BehaviorAction.Stop"/>
    public override void Stop()
    {
        speechCtrl.StopSpeech();
    }

    /// <see cref="BehaviorAction.Update"/>
    public override bool Update()
    {
        if (speechFinished)
            pauseTime += Time.deltaTime;

        if (pauseTime > pauseLength)
            // Post-utterance pause is over
            finished = true;

        return true;
    }

    /// <see cref="BehaviorAction.IsFinished"/>
    public override bool IsFinished()
    {
        return finished;
    }

    private void SpeechController_StateChange(AnimController sender, int srcState, int trgState)
    {
        if (srcState == (int)SpeechState.Speaking &&
           trgState == (int)SpeechState.NoSpeech)
        {
            speechFinished = true;
            pauseTime = 0f;
        }
    }
}

/// <summary>
/// Class representing a complete behavior, consisting of a set of actions.
/// </summary>
[Serializable]
public class Behavior
{
    /// <summary>
    /// Actions comprising the behavior.
    /// </summary>
    public BehaviorAction[] actions = new SpeechAction[0];

    private GameObject agent;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="agent">
    /// Virtual agent.
    /// </param>
    public Behavior(GameObject agent)
    {
        if (agent == null)
        {
            throw new ArgumentNullException("agent",
                                            "Unable to create behavior: no agent specified.");
        }

        this.agent = agent;
    }

    /// <summary>
    /// Adds an action to the behavior.
    /// </summary>
    /// <param name="action">
    /// Behavior action.
    /// </param>
    public void AddAction(BehaviorAction action)
    {
        if (action == null)
        {
            Debug.LogWarning("Unable to add null behavior action.");
            return;
        }

        action._Agent = agent;
        List<BehaviorAction> action_list = new List<BehaviorAction>(actions);
        if (action_list.Contains(action))
            // Behavior already added as action
            return;
        action_list.Add(action);
        actions = action_list.ToArray();
    }

    /// <summary>
    /// Removes an action from the behavior.
    /// </summary>
    /// <param name="action">
    /// Behavior action.
    /// </param>
    public void RemoveAction(BehaviorAction action)
    {
        List<BehaviorAction> action_list = new List<BehaviorAction>(actions);
        action_list.Remove(action);
        actions = action_list.ToArray();
    }

    /// <summary>
    /// Executes the behavior.
    /// </summary>
    public void Execute()
    {
        for (int bai = 0; bai < actions.Length; ++bai)
        {
            BehaviorAction action = actions[bai];
            if (!action.Execute())
            {
                RemoveAction(action);
                --bai;
            }
        }
    }

    /// <summary>
    /// Stops the behavior.
    /// </summary>
    public void Stop()
    {
        for (int bai = 0; bai < actions.Length; ++bai)
        {
            BehaviorAction action = actions[bai];
            action.Stop();
        }
    }

    /// <summary>
    /// Updates the behavior.
    /// </summary>
    public void Update()
    {
        for (int bai = 0; bai < actions.Length; ++bai)
        {
            BehaviorAction action = actions[bai];
            if (!action.Update())
            {
                RemoveAction(action);
                --bai;
            }
        }
    }

    /// <summary>
    /// true if the behavior has finished executing, false otherwise.
    /// </summary>
    public bool IsFinished()
    {
        for (int bai = 0; bai < actions.Length; ++bai)
        {
            BehaviorAction action = actions[bai];
            if (!action.IsFinished())
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Reads the behavior data from an XML message.
    /// </summary>
    /// <param name="bhvXml">
    /// XML message content.
    /// </param>
    /// <returns>
    /// true if XML successfully parsed, false otherwise.
    /// </returns>
    public bool Parse(string bhvXml)
    {
        BehaviorParser parser = new BehaviorParser();

        return parser.Parse(this, bhvXml);
    }
}

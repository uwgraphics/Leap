using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Core class for the Leap animation system.
/// </summary>
public class AnimationManager
{
    private static AnimationManager _instance = null;

    /// <summary>
    /// Animation manager instance.
    /// </summary>
    public static AnimationManager Instance
    {
        get
        {
            if (_instance == null)
                _instance = new AnimationManager();

            return _instance;
        }
    }

    /// <summary>
    /// Animation timeline.
    /// </summary>
    public AnimationTimeline Timeline
    {
        get;
        private set;
    }

    /// <summary>
    /// List of animated character models.
    /// </summary>
    public IList<GameObject> Models
    {
        get { return _models.AsReadOnly(); }
    }

    /// <summary>
    /// Root object of the environment.
    /// </summary>
    public GameObject Environment
    {
        get { return _environment; }
    }

    /// <summary>
    /// List of animation controller types listed by their execution order.
    /// </summary>
    public IList<Type> _ControllersByExecOrder
    {
        get { return _controllersByExecOrder.AsReadOnly(); }
    }

    private List<GameObject> _models;
    private GameObject _environment;
    private List<Type> _controllersByExecOrder = null;

    /// <summary>
    /// Constructor.
    /// </summary>
    private AnimationManager()
    {
        Timeline = new AnimationTimeline(this);
        _models = new List<GameObject>();
        _environment = null;
    }

    /// <summary>
    /// Initialize the animation manager.
    /// </summary>
    public void Init()
    {
        _InitControllerExecOrder();

        Timeline._Init();
    }

    /// <summary>
    /// Add a character model to the timeline.
    /// </summary>
    /// <param name="model">Character model</param>
    public void AddModel(GameObject model)
    {
        if (_models.Any(m => m == model))
        {
            throw new Exception(string.Format("Character model {0} already added", model.name));
        }

        // Initialize the character's model controller
        var modelController = model.GetComponent<ModelController>();
        if (modelController == null)
        {
            throw new Exception(string.Format("Character model {0} does not have a ModelController", model.name));
        }
        modelController.Init();

        // Initialize the character morph controller
        var morphController = model.GetComponent<MorphController>();
        if (morphController != null)
        {
            morphController.Init();
        }

        // Apply & store initial pose for the model
        var initialPoseClip = model.GetComponent<Animation>().GetClip("InitialPose");
        if (initialPoseClip == null)
        {
            throw new Exception(string.Format("Character model {0} does not have an InitialPose animation defined", model.name));
        }
        var initialPoseInstance = new AnimationClipInstance(initialPoseClip.name, model);
        initialPoseInstance.Apply(0, AnimationLayerMode.Override);
        modelController.Init();

        // Add the model to the timeline
        _models.Add(model);
    }

    /// <summary>
    /// Remove a character model from the timeline.
    /// </summary>
    /// <param name="modelName">Character model name</param>
    public void RemoveModel(string modelName)
    {
        if (Timeline.Layers.Any(l => l.Animations.Any(a => a.Animation.Model.name == modelName)))
        {
            throw new Exception(string.Format(
                "Cannot remove character model {0}, because it is referenced in at least one animation instance", modelName));
        }

        _models.RemoveAll(m => m.name == modelName);
    }

    /// <summary>
    /// Remove all character models from the timeline.
    /// </summary>
    public void RemoveAllModels()
    {
        if (Timeline.Layers.Any(l => l.Animations.Count > 0))
        {
            throw new Exception(string.Format(
                "Cannot remove character models, because there are still animation instances referencing them"));
        }

        _models.Clear();
    }

    /// <summary>
    /// Set environment containing objects manipulated in animations.
    /// </summary>
    /// <param name="environment">Environment root object</param>
    public void SetEnvironment(GameObject environment)
    {
        _environment = environment;
        if (environment == null)
            return;

        var envController = environment.GetComponent<EnvironmentController>();
        if (envController == null)
            throw new Exception(string.Format("Environment root object {0} does not have an EnvironmentController", environment.name));

        envController.Init();
    }

    // Initialize the list specifying the execution order for animation controllers
    private void _InitControllerExecOrder()
    {
        _controllersByExecOrder = new List<Type>();

        var scripts = UnityEditor.MonoImporter.GetAllRuntimeMonoScripts();
        var scriptsByExecOrder = new List<UnityEditor.MonoScript>();
        foreach (var script in scripts)
        {
            if (script.name != "" && script.GetClass() != null &&
                script.GetClass().IsSubclassOf(typeof(AnimController)))
            {
                int scriptExecOrder = UnityEditor.MonoImporter.GetExecutionOrder(script);

                // Add the controller to the execution order list
                bool added = false;
                for (int scriptIndex = 0; scriptIndex < scriptsByExecOrder.Count; ++scriptIndex)
                {
                    int curScriptExecOrder = UnityEditor.MonoImporter.GetExecutionOrder(scriptsByExecOrder[scriptIndex]);
                    if (scriptExecOrder < curScriptExecOrder)
                    {
                        scriptsByExecOrder.Insert(scriptIndex, script);
                        added = true;
                        break;
                    }
                }
                if (!added)
                    scriptsByExecOrder.Add(script);
            }
        }

        foreach (var script in scriptsByExecOrder)
        {
            _controllersByExecOrder.Add(script.GetClass());
        }

    }
}

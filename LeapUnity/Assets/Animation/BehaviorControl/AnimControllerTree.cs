using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Hierarchy of animation controllers.
/// </summary>
[RequireComponent(typeof(ModelController))]
public class AnimControllerTree : MonoBehaviour
{
    /// <summary>
    /// Elapsed time since last update.
    /// </summary>
    /// <remarks>There are two reasons for using this instead of Time.deltaTime:
    /// (1) we can more easily control the time increment at which animation controllers are updated, and
    /// (2) we can use animation controllers in Editor mode, when Time.deltaTime is unavailable.</remarks>
    public virtual float DeltaTime
    {
        get
        {
            float deltaTime = Application.isPlaying ? Time.deltaTime : _deltaTime;
            return deltaTime;
        }

        set { _deltaTime = value; }
    }

    /// <summary>
    /// The root of this animation controller tree.
    /// </summary>
    public AnimController rootController = null;

    private ModelController _modelController = null;
    private float _deltaTime = 0f;

    /// <summary>
    /// Initialize the animation controller tree.
    /// </summary>
    public void Start()
    {
        if (rootController == null)
            return;

        // Initialize the controller hierarchy
        _InitChildParentConnections(rootController);

        // Initialize the model controller
        _modelController = GetComponent<ModelController>();
        _modelController._Init();

        // Initialize animation controllers
        rootController._InitTree();
    }

    /// <summary>
    /// Update the animation controller tree.
    /// </summary>
    public void Update()
    {
        if (rootController == null)
            return;

        GetComponent<ModelController>()._ResetToInitialPose();
        rootController._UpdateTree();
    }

    /// <summary>
    /// Update the animation controller tree.
    /// </summary>
    /// <param name="deltaTime">Elapsed  time since last update</param>
    public void Update(float deltaTime)
    {
        DeltaTime = deltaTime;
        Update();
    }

    /// <summary>
    /// Update the animation controller tree after all animation has been applied.
    /// </summary>
    public void LateUpdate()
    {
        if (rootController == null)
            return;

        rootController._LateUpdateTree();

        // Store the current pose, so it can be accessed on next frame
        gameObject.GetComponent<ModelController>()._StoreCurrentPose();
    }

    private void _InitChildParentConnections(AnimController root)
    {
        foreach (AnimController child in root.childControllers)
        {
            if (child == null)
                continue;

            root.AddChild(child);

            _InitChildParentConnections(child);
        }
    }
}

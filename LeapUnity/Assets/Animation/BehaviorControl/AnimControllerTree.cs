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
    /// The root of this animation controller tree.
    /// </summary>
    public AnimController rootController = null;

    private ModelController mdlCtrl = null;

    void Awake()
    {
        if (rootController == null)
            return;

        // Initialize the controller hierarchy
        _InitChildParentConnections(rootController);
    }

    void Start()
    {
        if (rootController == null)
            return;

        // Initialize the model controller
        mdlCtrl = GetComponent<ModelController>();
        mdlCtrl._Init();
        // Initialize animation controllers
        rootController._InitTree();
    }

    void Update()
    {
        if (rootController == null)
            return;

        GetComponent<ModelController>()._ResetToInitialPose();
        rootController._UpdateTree();
    }

    void LateUpdate()
    {
        if (rootController == null)
            return;

        rootController._LateUpdateTree();

        // Store the current pose, so it can be accessed on next frame
        gameObject.GetComponent<ModelController>()._StoreCurrentPose();
    }

    void _InitChildParentConnections(AnimController root)
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

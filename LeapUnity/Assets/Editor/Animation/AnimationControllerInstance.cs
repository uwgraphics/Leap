using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Animation instance corresponding to a dynamic animation controller
/// on the character model.
/// </summary>
public abstract class AnimationControllerInstance : AnimationInstance
{
    /// <summary>
    /// Animation controller activity duration.
    /// </summary>
    public override float TimeLength
    {
        get { return _timeLength; }
    }

    /// <summary>
    /// Animation controller.
    /// </summary>
    public virtual AnimController Controller
    {
        get { return _controller; }
    }

    protected AnimController _controller = null;
    protected float _timeLength = 1f;
    protected List<IAnimControllerState> _bakedControllerStates;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="model">Character model</param>
    /// <param name="animationClipName">Animation clip name</param>
    /// <param name="controllerType">AnimController type</param>
    /// <param name="timeLength">Animation controller activity duration.</param>
    public AnimationControllerInstance(GameObject model, string animationClipName, Type controllerType,
        int frameLength = 30)
        : base(model, animationClipName)
    {
        _controller = model.GetComponent(controllerType) as AnimController;
        SetFrameLength(frameLength);
    }

    /// <summary>
    /// Set animation controller activity duration.
    /// </summary>
    /// <param name="frameLength"></param>
    public virtual void SetFrameLength(int frameLength)
    {
        _timeLength = ((float)frameLength) / LEAPCore.editFrameRate;
    }

    /// <summary>
    /// <see cref="AnimationInstance.Start()"/>
    /// </summary>
    public override void Start()
    {
        base.Start();

        if (IsBaking)
        {
            // Initialize list of baked controller states
            _bakedControllerStates = new List<IAnimControllerState>(FrameLength);
            for (int frameIndex = 0; frameIndex < FrameLength; ++frameIndex)
                _bakedControllerStates.Add(Controller.GetRuntimeState());
        }
    }

    /// <summary>
    /// <see cref="AnimationInstance.Finish()"/>
    /// </summary>
    public override void Finish()
    {
        base.Finish();
    }

    /// <summary>
    /// Apply animation instance to the character model at specified frame.
    /// </summary>
    /// <param name="frame">Frame index</param>
    /// <param name="layerMode">Animation layering mode</param>
    protected override void _Apply(int frame, AnimationLayerMode layerMode)
    {
        if (layerMode == AnimationLayerMode.Additive)
        {
            throw new Exception("Additive layering not supported for AnimationControllerInstance");
        }

        if (IsBaking)
        {
            // Update the controller to get new body pose
            Controller.weight = 1f;
            Controller._UpdateTree();
            Controller._LateUpdateTree();

            // Bake the current controller state
            _bakedControllerStates[frame] = Controller.GetRuntimeState();
        }
        else
        {
            if (_bakedControllerStates != null && frame < _bakedControllerStates.Count)
            {
                // Apply the baked controller state
                Controller.SetRuntimeState(_bakedControllerStates[frame]);
            }
        }
    }
}

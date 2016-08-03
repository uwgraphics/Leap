using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Animation instance for controlling the erandom ye blink behavior.
/// </summary>
public class EyesAliveInstance : AnimationControllerInstance
{
    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="name">Animation instance name</param>
    /// <param name="model">Character model</param>
    /// <param name="frameLength">Animation controller activity duration.</param>
    public EyesAliveInstance(string name, GameObject model, int frameLength) :
        base(name, model, typeof(EyesAliveController), frameLength)
    {
    }

    /// <summary>
    /// <see cref="AnimationControllerInstance._ApplyController"/>
    /// </summary>
    protected override void _ApplyController(TimeSet times)
    {
    }
}

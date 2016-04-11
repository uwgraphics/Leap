using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Animation instance that makes an object (usually camera)
/// look at the front of another object, while maintaining constant height.
/// </summary>
public class LookAtFrontInstance : AnimationInstance
{
    public override float TimeLength
    {
        get;
        set;
    }
    /// <summary>
    /// Target to look at.
    /// </summary>
    public virtual Transform Target
    {
        get;
        protected set;
    }

    /// <summary>
    /// Object distance from target.
    /// </summary>
    public virtual float Distance
    {
        get;
        protected set;
    }

    /// <summary>
    /// Object height.
    /// </summary>
    public virtual float Height
    {
        get;
        protected set;
    }

    /// <summary>
    /// Target height.
    /// </summary>
    public virtual float TargetHeight
    {
        get;
        protected set;
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="name">Animation clip name</param>
    /// <param name="model">Object</param>
    /// <param name="distance">Object distance from target</param>
    /// <param name="height">Object height</param>
    public LookAtFrontInstance(string name, GameObject model, int frameLength,
        Transform target, float distance, float height, float targetHeight)
        : base(name, model)
    {
        TimeLength = LEAPCore.ToTime(frameLength);
        Target = target;
        Distance = distance;
        Height = height;
        TargetHeight = targetHeight;
    }

    /// <summary>
    /// Apply animation instance to the character model at the specified times.
    /// </summary>
    /// <param name="times">Time indexes</param>
    /// <param name="layerMode">Animation layering mode</param>
    public override void Apply(TimeSet times, AnimationLayerMode layerMode)
    {
        var obj = Model.transform;
        var targetPos = new Vector3(Target.position.x, TargetHeight, Target.position.z);
        var targetDir = new Vector3(Target.forward.x, 0f, Target.forward.z);
        targetDir.Normalize();
        obj.position = targetPos + targetDir * Distance;
        obj.position = new Vector3(obj.position.x, Height, obj.position.z);
        obj.LookAt(targetPos);
    }
}

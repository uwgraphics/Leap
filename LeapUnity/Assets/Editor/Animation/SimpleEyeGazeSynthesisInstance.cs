using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Animation instance for simple eye saccade synthesis from body motion.
/// </summary>
public class SimpleEyeGazeSynthesisInstance : AnimationInstance
{
    /// <summary>
    /// Body animation for which eye saccades should be synthesized.
    /// </summary>
    public virtual AnimationInstance BodyAnimation
    {
        get;
        set;
    }
    
    /// <summary>
    /// <see cref="AnimationInstance.TimeLength"/>
    /// </summary>
    public override float TimeLength
    {
        get { return BodyAnimation.TimeLength; }
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="model">Character model</param>
    /// <param name="animationClipName">Animation clip name</param>
    public SimpleEyeGazeSynthesisInstance(GameObject model, string animationClipName, AnimationInstance bodyAnimation)
        : base(model, animationClipName)
    {
        BodyAnimation = bodyAnimation;
    }

    /// <summary>
    /// Apply animation instance to the character model at specified frame.
    /// </summary>
    /// <param name="frame">Frame index</param>
    /// <param name="layerMode">Animation layering mode</param>
    public override void Apply(int frame, AnimationLayerMode layerMode)
    {
        _ApplyEye(frame, layerMode, Model.LEye);
        _ApplyEye(frame, layerMode, Model.REye);
    }

    public virtual void _ApplyEye(int frame, AnimationLayerMode layerMode, Transform eye)
    {
        Transform head = Model.Head;

        // Get previous eye pose
        float prevEyeYaw = Mathf.Clamp(Model.GetPrevRotation(eye).eulerAngles.y, -45f, 45f);
        float prevEyePitch = Mathf.Clamp(Model.GetPrevRotation(eye).eulerAngles.x, -45f, 45f);

        // Get previous head pose
        float prevHeadYaw = Model.GetPrevRotation(head).eulerAngles.y;
        float prevHeadPitch = Model.GetPrevRotation(head).eulerAngles.x;

        // Get current head pose
        float headYaw = Model.Head.localEulerAngles.y;
        float headPitch = Model.Head.localEulerAngles.x;

        // Compute new eye pose
        float eyeYaw = headYaw / 65f * 45f;
        float eyePitch = headYaw / 65f * 45f;

        /*// Compute eye velocity difference
        float headYawDiff = DirectableJoint.ClampAngle(headYaw - prevHeadYaw);
        float headPitchDiff = DirectableJoint.ClampAngle(headPitch - prevHeadPitch);
        float eyeYawDiff = 2f * headYawDiff;
        float eyePitchDiff = 2f * headPitchDiff;

        //Debug.Log(string.Format("Head has rotated by ({0}, {1})", headYawDiff, headPitchDiff));

        // Compute eye distance to OMR
        float a = eyeYawDiff * eyeYawDiff + eyePitchDiff * eyePitchDiff;
        float b = 2f * eyeYawDiff * prevEyeYaw + 2f * eyePitchDiff * prevEyePitch;
        float c = prevEyeYaw * prevEyeYaw + prevEyePitch * prevEyePitch - 45f;
        float d = b * b - 4f * a * c;
        if (d <= 0f)
        {
            Debug.LogError("Unable to compute eye distance to OMR!");
            d = 0f;
        }
        float t = 2f * c / (-b + d);
        var eyePos = new Vector2(prevEyeYaw, prevEyePitch);
        var eyeDir = new Vector2(eyeYawDiff, eyePitchDiff);
        var eyePosOMR = eyePos + eyeDir * t;
        float eyeDistanceOMR = Vector2.Distance(eyePos, eyePosOMR);

        // Dampen eye movement based on proximity to OMR
        float dampOMR = Mathf.Sin(Mathf.Clamp(Mathf.PI * eyeDistanceOMR / 30f, 0, Mathf.PI / 2f));
        //eyeYawDiff *= dampOMR;
        //eyePitchDiff *= dampOMR;

        // Upper limit on eye movement velocity
        eyeYawDiff = Mathf.Clamp(eyeYawDiff, -450f / LEAPCore.editFrameRate, 450f / LEAPCore.editFrameRate);

        // Compute new eye position
        float eyeYaw = Mathf.Clamp(prevEyeYaw + eyeYawDiff, -45f, 45f);
        float eyePitch = Mathf.Clamp(prevEyePitch + eyePitchDiff, -45f, 45f);

        //Debug.Log(string.Format("Prev. rotation of {0} was ({1}, {2})", eye.name, prevEyeYaw, prevEyePitch));*/

        // Rotate the eye
        eye.localEulerAngles = new Vector3(eyePitch, eyeYaw, 0f);

        //Debug.Log(string.Format("Rotating {0} by ({1}, {2})", eye.name, eyeYawDiff, eyePitchDiff));
        //Debug.Log(string.Format("New rotation of {0} is ({1}, {2})", eye.name, eyeYaw, eyePitch));
    }
}

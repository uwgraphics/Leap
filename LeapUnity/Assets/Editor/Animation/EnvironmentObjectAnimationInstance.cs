using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Animation instance corresponding to a pre-made animation clip.
/// </summary>
public class EnvironmentObjectAnimationInstance : AnimationInstance
{
    /// <summary>
    /// Always null for environment objects.
    /// </summary>
    public override ModelController ModelController
    {
        get { return null; }
    }

    /// <summary>
    /// <see cref="AnimationInstance.TimeLength"/>
    /// </summary>
    public override float TimeLength
    {
        get { return _timeLength; }
    }

    protected float _timeLength = 1f;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="model">Environment object</param>
    /// <param name="animationClipName">Animation clip name</param>
    /// <param name="frameLength">Animation duration.</param>
    public EnvironmentObjectAnimationInstance(GameObject model, string animationClipName, int frameLength = 30)
    {
        Model = model;
        Animation = model.GetComponent<Animation>();
        if (Model == null)
        {
            throw new Exception("Must specify environment object model");
        }
        else if (Animation == null)
        {
            throw new Exception("Unable to create animation instance for a model without an Animation component");
        }

        SetFrameLength(frameLength);

        // Is there an animation clip already for this instance?
        foreach (AnimationState animationState in Animation)
        {
            var clip = animationState.clip;
            if (clip == null)
            {
                // TODO: this is needed b/c Unity is a buggy piece of crap
                Debug.LogWarning(string.Format("Animation state {0} on model {1} has clip set to null",
                    animationState.name, Animation.gameObject.name));
                continue;
            }

            if (clip.name == animationClipName && AssetDatabase.GetAssetPath(clip) != "")
            {
                // There is already a defined clip for this animation instance,
                // so assume that is the animation
                this.AnimationClip = clip;
                // TODO: this is needed b/c Unity is a buggy piece of crap
                Animation.RemoveClip(clip);
                Animation.AddClip(clip, animationClipName);
                break;
            }
            else
            {
                this.AnimationClip = null;
            }
        }

        if (this.AnimationClip == null)
        {
            // No clip found for this animation instance, create an empty one
            AnimationClip = LEAPAssetUtils.CreateAnimationClipOnModel(animationClipName, model);
            BakeMask = new BitArray(7);
            BakeMask.SetAll(true);
        }
        else
        {
            // There is already an animation clip, so retrieve the curves from it
            AnimationClipCurveData[] curveData = AnimationUtility.GetAllCurves(AnimationClip, true);
            _AnimationCurves = new AnimationCurve[curveData.Length];
            for (int curveIndex = 0; curveIndex < _AnimationCurves.Length; ++curveIndex)
            {
                _AnimationCurves[curveIndex] = curveData[curveIndex].curve;
            }
        }

        // Set default animation weight
        Weight = 1f;
    }

    /// <summary>
    /// Set animation duration.
    /// </summary>
    /// <param name="frameLength"></param>
    public virtual void SetFrameLength(int frameLength)
    {
        _timeLength = ((float)frameLength) / LEAPCore.editFrameRate;
    }

    /// <summary>
    /// <see cref="AnimationInstance.StartBake"/>
    /// </summary>
    public override void StartBake()
    {
        IsBaking = true;
        _AnimationCurves = new AnimationCurve[7];
        for (int curveIndex = 0; curveIndex < _AnimationCurves.Length; ++curveIndex)
        {
            _AnimationCurves[curveIndex] = new AnimationCurve();
        }
    }

    /// <summary>
    /// <see cref="AnimationInstance.FinalizeBake"/>
    /// </summary>
    public override void FinalizeBake()
    {
        IsBaking = false;
        AnimationClip.ClearCurves();

        // Set position curves
        if (_AnimationCurves[0].keys.Length > 0)
            AnimationClip.SetCurve("", typeof(Transform), "localPosition.x", _AnimationCurves[0]);
        if (_AnimationCurves[1].keys.Length > 0)
            AnimationClip.SetCurve("", typeof(Transform), "localPosition.y", _AnimationCurves[1]);
        if (_AnimationCurves[2].keys.Length > 0)
            AnimationClip.SetCurve("", typeof(Transform), "localPosition.z", _AnimationCurves[2]);
        
        // Set rotation curves
        if (_AnimationCurves[3].keys.Length > 0)
            AnimationClip.SetCurve("", typeof(Transform), "localRotation.x", _AnimationCurves[3]);
        if (_AnimationCurves[4].keys.Length > 0)
            AnimationClip.SetCurve("", typeof(Transform), "localRotation.y", _AnimationCurves[4]);
        if (_AnimationCurves[5].keys.Length > 0)
            AnimationClip.SetCurve("", typeof(Transform), "localRotation.z", _AnimationCurves[5]);
        if (_AnimationCurves[6].keys.Length > 0)
            AnimationClip.SetCurve("", typeof(Transform), "localRotation.w", _AnimationCurves[6]);

        // Write animation clip to file
        string path = LEAPCore.environmentModelsDirectory + "/" + AnimationClip.name + ".anim";
        if (AssetDatabase.GetAssetPath(AnimationClip) != path)
        {
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(AnimationClip, path);
        }
        AssetDatabase.SaveAssets();
    }

    /// <summary>
    /// <see cref="AnimationInstance.SampleRootPosition"/>
    /// </summary>
    public override Vector3 SampleRootPosition(int frame)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// <see cref="AnimationInstance.SampleRootVelocity"/>
    /// </summary>
    public override Vector3 SampleRootVelocity(int frame)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// <see cref="AnimationInstance.SampleRotation"/>
    /// </summary>
    public virtual Quaternion SampleRotation(int frame, int boneIndex)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// <see cref="AnimationInstance.SampleAngularVelocity"/>
    /// </summary>
    public virtual float SampleAngularVelocity(int frame, int boneIndex)
    {
        throw new NotImplementedException();
    }

    // Apply animation instance to the character model at specified frame
    protected override void _Apply(int frame, AnimationLayerMode layerMode)
    {
    }

    // Bake applied animation or apply baked animation
    protected override void _ApplyBake(int frame, AnimationLayerMode layerMode)
    {
        if (IsBaking)
        {
            // Bake the applied model pose into animation curves
            float time = ((float)frame) / LEAPCore.editFrameRate;
            float value = 0f;
            Keyframe key;

            // First bake position
            value = Model.transform.localPosition.x;
            key = new Keyframe(time, value);
            _AnimationCurves[0].AddKey(key);
            value = Model.transform.localPosition.y;
            key = new Keyframe(time, value);
            _AnimationCurves[1].AddKey(key);
            value = Model.transform.localPosition.z;
            key = new Keyframe(time, value);
            _AnimationCurves[2].AddKey(key);

            // Then bake rotation
            value = Model.transform.localRotation.x;
            key = new Keyframe(time, value);
            _AnimationCurves[3].AddKey(key);
            value = Model.transform.localRotation.y;
            key = new Keyframe(time, value);
            _AnimationCurves[4].AddKey(key);
            value = Model.transform.localRotation.z;
            key = new Keyframe(time, value);
            _AnimationCurves[5].AddKey(key);
            value = Model.transform.localRotation.w;
            key = new Keyframe(time, value);
            _AnimationCurves[6].AddKey(key);
        }
        else
        {
            // Configure how the animation clip will be applied to the model
            Animation[AnimationClip.name].normalizedTime = ((float)frame) / FrameLength;
            Animation[AnimationClip.name].weight = Weight;
            Animation[AnimationClip.name].blendMode = layerMode == AnimationLayerMode.Override ?
                AnimationBlendMode.Blend : AnimationBlendMode.Additive;

            // Apply the animation clip to the model
            Animation[AnimationClip.name].enabled = true;
            Animation.Sample();
            Animation[AnimationClip.name].enabled = false;
        }
    }
}

using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Layer type defines how animations are applied
/// to the character. Supported modes are:
/// - Additive - animation is multiplied by a weight value and added to the current character pose
/// - Override - animation overwrites the current character pose
/// </summary>
public enum AnimationLayerMode
{
    Additive,
    Override
}

/// <summary>
/// Animation instance. This can be either an existing animation clip
/// or a procedural animation that can be baked into an animation clip.
/// </summary>
public abstract class AnimationInstance
{
    /// <summary>
    /// Character model controller to which this animation instance is applied.
    /// </summary>
    public virtual GameObject Model
    {
        get;
        protected set;
    }

    /// <summary>
    /// Shorthand for getting the character model controller.
    /// </summary>
    public virtual ModelController ModelController
    {
        get { return Model.GetComponent<ModelController>(); }
    }

    /// <summary>
    /// Animation component on the character.
    /// </summary>
    public virtual Animation Animation
    {
        get;
        protected set;
    }

    /// <summary>
    /// Animation clip corresponding to the instance.
    /// </summary>
    public virtual AnimationClip AnimationClip
    {
        get;
        set;
    }

    /// <summary>
    /// Weight at which the animation instance is applied to the character.
    /// This is only used in additive layers.
    /// </summary>
    public virtual float Weight
    {
        get;
        set;
    }

    /// <summary>
    /// Length of this instance in frames.
    /// </summary>
    public virtual int FrameLength
    {
        get { return Mathf.RoundToInt(TimeLength * LEAPCore.editFrameRate); }
    }

    /// <summary>
    /// Length of this instance in seconds.
    /// </summary>
    public abstract float TimeLength
    {
        get;
    }

    /// <summary>
    /// Set to true to have the animation instance baked into the animation clip.
    /// </summary>
    /// <remarks>When this is true, the AnimationInstance must save the result of each frame
    /// as new keyframes in the corresponding animation curves (_AnimationCurves array). It is the responsibility
    /// of each AnimationInstance subclass to implement this functionality in its override of the Apply() method.</remarks>
    public virtual bool IsBaking
    {
        get;
        protected set;
    }

    /// <summary>
    /// Specifies which animation curves are actually getting keyframed
    /// when baking the instance.
    /// </summary>
    public virtual BitArray BakeMask
    {
        get;
        protected set;
    }

    // Animation curves for baking the current instance
    protected virtual AnimationCurve[] _AnimationCurves
    {
        get;
        set;
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    public AnimationInstance()
    {
        Model = null;
        Animation = null;
        AnimationClip = null;
        BakeMask = null;
        _AnimationCurves = null;
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="model">Character model</param>
    /// <param name="animationClipName">Animation clip name</param>
    /// <param name="bakeMask">Specifies which animation curves are actually getting keyframed
    /// when baking the instance.</param>
    public AnimationInstance(GameObject model, string animationClipName, BitArray bakeMask = null)
    {
        Model = model;
        Animation = model.GetComponent<Animation>();
        if (Model == null)
        {
            throw new Exception("Must specify character model");
        }
        else if (Animation == null)
        {
            throw new Exception("Unable to create animation instance for a character model without an Animation component");
        }

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
            _AnimationCurves = LEAPAssetUtils.GetAnimationCurvesFromClip(model, AnimationClip);

            // Set bake mask
            if (bakeMask != null)
            {
                BakeMask = bakeMask;
            }
            else
            {
                // Create bake mask for all animation curves
                BakeMask = new BitArray(_AnimationCurves.Length);
                BakeMask.SetAll(true);
            }
        }
        else
        {
            // There is already an animation clip, so retrieve the curves from it
            _AnimationCurves = LEAPAssetUtils.GetAnimationCurvesFromClip(model, AnimationClip);

            // Create bake mask for the animation curves
            BakeMask = new BitArray(_AnimationCurves.Length);
            for (int curveIndex = 0; curveIndex < _AnimationCurves.Length; ++curveIndex)
                BakeMask.Set(curveIndex, _AnimationCurves[curveIndex].keys != null);
        }

        // Set default animation weight
        Weight = 1f;
    }

    /// <summary>
    /// Start the application of the current animation instance.
    /// </summary>
    /// <remarks>AnimationTimeline calls this function during playback, on the first frame of the current instance.</remarks>
    public virtual void Start()
    {
    }

    /// <summary>
    /// Apply animation instance to the character model at specified frame.
    /// </summary>
    /// <param name="frame">Frame index</param>
    /// <param name="layerMode">Animation layering mode</param>
    public virtual void Apply(int frame, AnimationLayerMode layerMode)
    {
        _Apply(frame, layerMode);
        _ApplyBake(frame, layerMode);
    }

    /// <summary>
    /// Finish the application of the current animation instance.
    /// </summary>
    /// <remarks>AnimationTimeline calls this function during playback, after the last frame of the current instance.</remarks>
    public virtual void Finish()
    {
    }

    /// <summary>
    /// Start baking the animation instance.
    /// </summary>
    public virtual void StartBake()
    {
        IsBaking = true;
        _AnimationCurves = LEAPAssetUtils.CreateAnimationCurvesForModel(Model);
    }

    /// <summary>
    /// Finalize baking the animation instance into an animation clip.
    /// </summary>
    /// <remarks>This will save baked animation curves to an animation clip.</remarks>
    public virtual void FinalizeBake()
    {
        IsBaking = false;
        AnimationClip.ClearCurves();
        LEAPAssetUtils.SetAnimationCurvesOnClip(Model.gameObject, AnimationClip, _AnimationCurves);

        // Write animation clip to file
        string path = LEAPAssetUtils.GetModelDirectory(Model) + AnimationClip.name + ".anim";
        if (AssetDatabase.GetAssetPath(AnimationClip) != path)
        {
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(AnimationClip, path);
        }
        AssetDatabase.SaveAssets();
    }

    /// <summary>
    /// Sample root position baked in this animation instance.
    /// </summary>
    /// <param name="frame">Frame index</param>
    /// <returns>Root position</returns>
    public virtual Vector3 SampleRootPosition(int frame)
    {
        Vector3 pos = new Vector3(
            BakeMask.Get(0) ? _AnimationCurves[0].keys[frame].value : 0f,
            BakeMask.Get(1) ? _AnimationCurves[1].keys[frame].value : 0f,
            BakeMask.Get(2) ? _AnimationCurves[2].keys[frame].value : 0f
            );

        return pos;
    }

    /// <summary>
    /// Sample root velocity baked in this animation instance.
    /// </summary>
    /// <param name="frame">Frame index</param>
    /// <returns>Root velocity</returns>
    /// <remarks>Velocity is computed using backward finite difference method</remarks>
    public virtual Vector3 SampleRootVelocity(int frame)
    {
        if (frame == 0)
            return Vector3.zero;

        Vector3 pm1 = SampleRootPosition(frame - 1);
        Vector3 p = SampleRootPosition(frame);
        return (-pm1 + p) * ((float)LEAPCore.editFrameRate);
    }

    /// <summary>
    /// Sample bone rotation baked in this animation instance.
    /// </summary>
    /// <param name="frame">Frame index</param>
    /// <param name="boneIndex">Bone index</param>
    /// <returns>Bone rotation</returns>
    public virtual Quaternion SampleRotation(int frame, int boneIndex)
    {
        int curveIndex = 3 + boneIndex * 4;
        Quaternion rot = new Quaternion(
            BakeMask.Get(curveIndex) ? _AnimationCurves[curveIndex].keys[frame].value : 0f,
            BakeMask.Get(curveIndex + 1) ? _AnimationCurves[curveIndex + 1].keys[frame].value : 0f,
            BakeMask.Get(curveIndex + 2) ? _AnimationCurves[curveIndex + 2].keys[frame].value : 0f,
            BakeMask.Get(curveIndex + 3) ? _AnimationCurves[curveIndex + 3].keys[frame].value : 0f
            );

        return rot;
    }

    /// <summary>
    /// Sample angular velocity baked in this animation instance.
    /// </summary>
    /// <param name="frame">Frame index</param>
    /// <param name="boneIndex">Bone index</param>
    /// <returns>Angular velocity</returns>
    /// <remarks>Velocity is computed using backward finite difference method</remarks>
    public virtual float SampleAngularVelocity(int frame, int boneIndex)
    {
        if (frame == 0)
            return 0f;

        Quaternion qm1 = SampleRotation(frame - 1, boneIndex);
        Quaternion q = SampleRotation(frame, boneIndex);
        Quaternion dq = Quaternion.Inverse(qm1) * q;
        float av = QuaternionUtil.Log(dq).magnitude;

        return av;
    }

    // Apply animation instance to the character model at specified frame
    protected abstract void _Apply(int frame, AnimationLayerMode layerMode);

    // Bake applied animation or apply baked animation
    protected virtual void _ApplyBake(int frame, AnimationLayerMode layerMode)
    {
        if (IsBaking)
        {
            // Bake the applied model pose into animation curves
            int curveIndex = 0;
            float time = ((float)frame) / LEAPCore.editFrameRate;
            float value = 0f;
            Keyframe key;

            // First bake bone properties
            for (int boneIndex = 0; boneIndex < ModelController.NumberOfBones; ++boneIndex)
            {
                var bone = ModelController[boneIndex];

                if (boneIndex == 0)
                {
                    // Key position of the root bone:

                    value = bone.localPosition.x;
                    key = new Keyframe(time, value);
                    if (BakeMask.Get(curveIndex))
                        _AnimationCurves[curveIndex].AddKey(key);
                    ++curveIndex;

                    value = bone.localPosition.y;
                    key = new Keyframe(time, value);
                    if (BakeMask.Get(curveIndex))
                        _AnimationCurves[curveIndex].AddKey(key);
                    ++curveIndex;

                    value = bone.localPosition.z;
                    key = new Keyframe(time, value);
                    if (BakeMask.Get(curveIndex))
                        _AnimationCurves[curveIndex].AddKey(key);
                    ++curveIndex;
                }

                // Key rotation:

                value = bone.localRotation.x;
                key = new Keyframe(time, value);
                if (BakeMask.Get(curveIndex))
                    _AnimationCurves[curveIndex].AddKey(key);
                ++curveIndex;

                value = bone.localRotation.y;
                key = new Keyframe(time, value);
                if (BakeMask.Get(curveIndex))
                    _AnimationCurves[curveIndex].AddKey(key);
                ++curveIndex;

                value = bone.localRotation.z;
                key = new Keyframe(time, value);
                if (BakeMask.Get(curveIndex))
                    _AnimationCurves[curveIndex].AddKey(key);
                ++curveIndex;

                value = bone.localRotation.w;
                key = new Keyframe(time, value);
                if (BakeMask.Get(curveIndex))
                    _AnimationCurves[curveIndex].AddKey(key);
                ++curveIndex;
            }

            // Next bake blend shape properties
            int numBlendShapes = ModelController.NumberOfBlendShapes;
            for (int blendShapeIndex = 0; blendShapeIndex < numBlendShapes; ++blendShapeIndex)
            {
                curveIndex = 3 + 4 * ModelController.NumberOfBones + blendShapeIndex;
                value = ModelController.GetBlendShapeWeight(blendShapeIndex);
                key = new Keyframe(time, value);
                if (BakeMask.Get(curveIndex))
                    _AnimationCurves[curveIndex].AddKey(key);
            }
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

    /// <summary>
    /// Get frame index at specified time index of an animation.
    /// </summary>
    /// <param name="time">Time index</param>
    /// <returns>Frame index</returns>
    public static int GetFrameAtTime(float time)
    {
        return Mathf.RoundToInt(time * LEAPCore.editFrameRate);
    }
}

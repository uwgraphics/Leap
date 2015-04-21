using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Snapshot of the runtime state of an animation controller.
/// </summary>
public interface IAnimationControllerState
{
    void Get(AnimController controller);
    void Set(AnimController controller);
}

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

    /// <summary>
    /// Configure which animation curves are actually to get keyframed
    /// when baking the instance.
    /// </summary>
    public virtual BitArray BakeMask
    {
        get;
        protected set;
    }

    protected AnimController _controller = null;
    protected float _timeLength = 1f;
    protected List<IAnimationControllerState> _bakedControllerStates;

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

        // Bake all animation curves
        BakeMask = new BitArray(_AnimationCurves.Length);
        BakeMask.SetAll(true);
    }

    /// <summary>
    /// Get a snapshot of the current runtime state of the animation controller.
    /// </summary>
    /// <returns>Controller state</returns>
    public virtual IAnimationControllerState GetControllerState()
    {
        IAnimationControllerState state = _CreateControllerState();
        state.Get(Controller);
        return state;
    }

    /// <summary>
    /// Set the current runtime state of the animation controller from a snapshot.
    /// </summary>
    /// <param name="state">Controller state</param>
    public virtual void SetControllerState(IAnimationControllerState state)
    {
        state.Set(Controller);
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
            _bakedControllerStates = new List<IAnimationControllerState>(FrameLength);
            for (int frameIndex = 0; frameIndex < FrameLength; ++frameIndex)
                _bakedControllerStates.Add(_CreateControllerState());
        }
    }

    /// <summary>
    /// Apply animation instance to the character model at specified frame.
    /// </summary>
    /// <param name="frame">Frame index</param>
    /// <param name="layerMode">Animation layering mode</param>
    public override void Apply(int frame, AnimationLayerMode layerMode)
    {
        if (layerMode == AnimationLayerMode.Additive)
        {
            throw new Exception("Additive layering not supported for AnimationControllerInstance");
        }

        if (IsBaking)
        {
            // Update the controller to get new body pose
            Controller.weight = Weight;
            Controller._UpdateTree();
            Controller._LateUpdateTree();

            // Bake the current controller state
            _bakedControllerStates[frame] = GetControllerState();

            // Bake that pose into the animation clip
            Transform[] bones = ModelUtils.GetAllBones(Model.gameObject);
            int curveIndex = 0;
            for (int boneIndex = 0; boneIndex < bones.Length; ++boneIndex)
            {
                var bone = bones[boneIndex];
                float time = ((float)frame) / LEAPCore.editFrameRate;

                if (boneIndex == 0)
                {
                    // Key position on the root bone

                    var positionKeyframe = new Keyframe();
                    positionKeyframe.time = time;

                    positionKeyframe.value = bone.localPosition.x;
                    if (BakeMask.Get(curveIndex))
                        _AnimationCurves[curveIndex].AddKey(positionKeyframe);
                    ++curveIndex;

                    positionKeyframe.value = bone.localPosition.y;
                    if (BakeMask.Get(curveIndex))
                        _AnimationCurves[curveIndex].AddKey(positionKeyframe);
                    ++curveIndex;

                    positionKeyframe.value = bone.localPosition.z;
                    if (BakeMask.Get(curveIndex))
                        _AnimationCurves[curveIndex].AddKey(positionKeyframe);
                    ++curveIndex;
                }

                // Key rotation

                var rotationKeyFrame = new Keyframe();
                rotationKeyFrame.time = time;

                rotationKeyFrame.value = bone.localRotation.x;
                if (BakeMask.Get(curveIndex))
                    _AnimationCurves[curveIndex].AddKey(rotationKeyFrame);
                ++curveIndex;

                rotationKeyFrame.value = bone.localRotation.y;
                if (BakeMask.Get(curveIndex))
                    _AnimationCurves[curveIndex].AddKey(rotationKeyFrame);
                ++curveIndex;

                rotationKeyFrame.value = bone.localRotation.z;
                if (BakeMask.Get(curveIndex))
                    _AnimationCurves[curveIndex].AddKey(rotationKeyFrame);
                ++curveIndex;

                rotationKeyFrame.value = bone.localRotation.w;
                if (BakeMask.Get(curveIndex))
                    _AnimationCurves[curveIndex].AddKey(rotationKeyFrame);
                ++curveIndex;
            }
        }
        else
        {
            if (_bakedControllerStates != null && frame < _bakedControllerStates.Count)
            {
                // Apply the baked controller state
                SetControllerState(_bakedControllerStates[frame]);
            }

            // Configure how the animation clip will be applied to the model
            Animation[AnimationClip.name].normalizedTime = ((float)frame) / FrameLength;
            Animation[AnimationClip.name].weight = 1f;
            Animation[AnimationClip.name].blendMode = AnimationBlendMode.Blend;

            // Apply the animation clip to the model
            Animation[AnimationClip.name].enabled = true;
            Animation.Sample();
            Animation[AnimationClip.name].enabled = false;
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
    /// Create an uninitialized snapshot of the animation controller state.
    /// </summary>
    /// <returns>Uninitialized controller state</returns>
    protected abstract IAnimationControllerState _CreateControllerState();
}

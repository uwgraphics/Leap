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
public class AnimationControllerInstance : AnimationInstance
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
    /// Set animation controller activity duration.
    /// </summary>
    /// <param name="frameLength"></param>
    public virtual void SetFrameLength(int frameLength)
    {
        _timeLength = ((float)frameLength) / LEAPCore.editFrameRate;
    }

    /// <summary>
    /// <see cref="AnimationInstance.StartBake()"/>
    /// </summary>
    public override void StartBake()
    {
        base.StartBake();

        // TODO: initialize the controller
    }

    /// <summary>
    /// Apply animation instance to the character model at specified frame.
    /// </summary>
    /// <param name="frame">Frame index</param>
    /// <param name="layerMode">Animation layering mode</param>
    public override void Apply(int frame, AnimationLayerMode layerMode)
    {
        if (IsBaking)
        {
            // Update the controller to get new body pose
            Controller._UpdateTree();
            Controller._LateUpdateTree();

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
            // Configure how the animation clip will be applied to the model
            Animation[AnimationClip.name].time = ((float)frame) / TimeLength;
            Animation[AnimationClip.name].weight = Weight;
            if (layerMode == AnimationLayerMode.Additive)
            {
                Animation[AnimationClip.name].blendMode = AnimationBlendMode.Additive;
            }
            else
            {
                Animation[AnimationClip.name].blendMode = AnimationBlendMode.Blend;
            }

            // Apply the animation clip to the model
            Animation[AnimationClip.name].enabled = true;
            Animation.Sample();
            Animation[AnimationClip.name].enabled = false;
            //
            /*Debug.Log(string.Format("Applied animation {0} at frame {1} in layer moder {2} at weight {3}",
                AnimationClip.name, frame, layerMode.ToString(), Weight));*/
            //
        }
    }

    /// <summary>
    /// <see cref="AnimationInstance.FinishBake()"/>
    /// </summary>
    public override void FinishBake()
    {
        base.FinishBake();

        // TODO: stop the controller
    }
}

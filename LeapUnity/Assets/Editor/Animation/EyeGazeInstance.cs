using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Eye gaze animation instance that specified a gaze shift to
/// a specific target, followed by a gaze fixation on that target.
/// </summary>
public class EyeGazeInstance : AnimationControllerInstance
{
    /// <summary>
    /// Gaze animation controller.
    /// </summary>
    public virtual GazeController GazeController
    {
        get { return _controller as GazeController; }
    }

    /// <summary>
    /// Gaze target.
    /// </summary>
    public virtual GameObject Target
    {
        get;
        set;
    }

    /// <summary>
    /// How much the head should align with the target.
    /// </summary>
    public virtual float HeadAlign
    {
        get;
        set;
    }

    /// <summary>
    /// How much the upper body should align with the target.
    /// </summary>
    public virtual float TorsoAlign
    {
        get;
        set;
    }

    protected bool _gazeShiftStarted = false;

    // Runtime state of an eye gaze controller
    protected struct _GazeControllerState
    {
        public float amplitude;
        public _GazeJointState[] gazeJointStates;

        public _GazeControllerState(GazeController controller)
        {
            amplitude = controller.Amplitude;
            gazeJointStates = new _GazeJointState[controller.gazeJoints.Length];
            for (int jointIndex = 0; jointIndex < controller.gazeJoints.Length; ++jointIndex)
                gazeJointStates[jointIndex] = new _GazeJointState(controller.gazeJoints[jointIndex]);
        }

        public void Apply(GazeController controller)
        {
            controller.Amplitude = amplitude;
            for (int jointIndex = 0; jointIndex < controller.gazeJoints.Length; ++jointIndex)
                gazeJointStates[jointIndex].Apply(controller.gazeJoints[jointIndex]);
        }
    }

    // Runtime state of an eye gaze joint
    protected struct _GazeJointState
    {
        public Quaternion srcRot, trgRot, trgRotAlign, trgRotMR;
        public float distRotAlign, distRotMR, rotParamAlign, rotParamMR;
        public float maxVelocity, curVelocity, latency, latencyTime;
        public bool mrReached, trgReached;
        public float adjUpMR, adjDownMR, adjInMR, adjOutMR, curUpMR, curDownMR, curInMR, curOutMR;

        public _GazeJointState(GazeJoint joint)
        {
            srcRot = joint.srcRot;
            trgRot = joint.trgRot;
            trgRotAlign = joint.trgRotAlign;
            trgRotMR = joint.trgRotMR;
            distRotAlign = joint.distRotAlign;
            distRotMR = joint.distRotMR;
            rotParamAlign = joint.rotParamAlign;
            rotParamMR = joint.rotParamMR;
            maxVelocity = joint.maxVelocity;
            curVelocity = joint.curVelocity;
            latency = joint.latency;
            latencyTime = joint.latencyTime;
            mrReached = joint.mrReached;
            trgReached = joint.trgReached;
            adjUpMR = joint.adjUpMR;
            adjDownMR = joint.adjDownMR;
            adjInMR = joint.adjInMR;
            adjOutMR = joint.adjOutMR;
            curUpMR = joint.curUpMR;
            curDownMR = joint.curDownMR;
            curInMR = joint.curInMR;
            curOutMR = joint.curOutMR;
        }

        public void Apply(GazeJoint joint)
        {
            joint.srcRot = srcRot;
            joint.trgRot = trgRot;
            joint.trgRotAlign = trgRotAlign;
            joint.trgRotMR = trgRotMR;
            joint.distRotAlign = distRotAlign;
            joint.distRotMR = distRotMR;
            joint.rotParamAlign = rotParamAlign;
            joint.rotParamMR = rotParamMR;
            joint.maxVelocity = maxVelocity;
            joint.curVelocity = curVelocity;
            joint.latency = latency;
            joint.latencyTime = latencyTime;
            joint.mrReached = mrReached;
            joint.trgReached = trgReached;
            joint.adjUpMR = adjUpMR;
            joint.adjDownMR = adjDownMR;
            joint.adjInMR = adjInMR;
            joint.adjOutMR = adjOutMR;
            joint.curUpMR = curUpMR;
            joint.curDownMR = curDownMR;
            joint.curInMR = curInMR;
            joint.curOutMR = curOutMR;
        }
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="model">Character model</param>
    /// <param name="animationClipName">Animation clip name</param>
    /// <param name="frameLength">Duration of the fixation on the target</param>
    /// <param name="target">Gaze target (null will generate a random gaze aversion)</param>
    /// <param name="headAlign">Head alignment</param>
    /// <param name="torsoAlign">Torso alignment</param>
    public EyeGazeInstance(GameObject model, string animationClipName,
        int frameLength = 30, GameObject target = null, float headAlign = 0f, float torsoAlign = 0f)
        : base(model, animationClipName, typeof(GazeController), frameLength)
    {
        Target = target;
        HeadAlign = headAlign;
        TorsoAlign = torsoAlign;

        // Bake only animation curves for eyes, head, and torso
        BakeMask.SetAll(false);
        foreach (var gazeJoint in GazeController.gazeJoints)
        {
            int boneIndex = ModelUtils.FindBoneIndex(model, gazeJoint.bone);
            int curveIndex = 3 + boneIndex * 4;
            BakeMask.Set(curveIndex++, true);
            BakeMask.Set(curveIndex++, true);
            BakeMask.Set(curveIndex++, true);
            BakeMask.Set(curveIndex, true);
        }

        // Register handler for gaze controller state changes
        GazeController.StateChange += new StateChangeEvtH(GazeController_StateChange);
    }

    /// <summary>
    /// <see cref="AnimationInstance.StartBake"/>
    /// </summary>
    public override void StartBake()
    {
        base.StartBake();

        // Initiate gaze shift to target
        if (GazeController.Head != null)
        {
            GazeController.Head.align = HeadAlign;
            GazeController.Head.weight = 1f;
        }
        if (GazeController.Torso != null)
        {
            GazeController.Torso.align = TorsoAlign;
            GazeController.Head.weight = 0f;
        }
        GazeController.GazeAt(Target);
    }

    /// <summary>
    /// <see cref="AnimationInstance.Apply"/>
    /// </summary>
    public override void Apply(int frame, AnimationLayerMode layerMode)
    {
        if (IsBaking)
        {
            if (GazeController.StateId == (int)GazeState.Shifting)
            {
                if (!GazeController.fixGaze)
                {
                    // Make sure gaze remains fixated after gaze shift completion
                    GazeController.fixGaze = true;
                }
                // TODO: need better way to handle going back to original motion when gaze instance is finished

                if (_gazeShiftStarted)
                {
                    // Gaze shift just started, compute updated gaze parameters for anticipated body movement
                    _gazeShiftStarted = false;
                    _AdjustGazeParamsForMovingBase();
                }
            }
        }

        base.Apply(frame, layerMode);
    }

    /// <summary>
    /// <see cref="AnimationInstance.FinishBake"/>
    /// </summary>
    public override void FinishBake()
    {
        /*if (GazeController.StateId == (int)GazeState.Shifting)
            GazeController.StopGaze();*/

        base.FinishBake();

        // TODO: stop fixating gaze at the target
        // (maybe gaze straight ahead again?)
    }

    // Compute gaze shift parameters to account for anticipated body movement
    protected virtual void _AdjustGazeParamsForMovingBase()
    {
        // Store current model pose
        AnimationTimeline.Instance.StoreModelPose(Model.gameObject.name, AnimationClip.name + "Pose");

        // Store current gaze controller state
        var curState = new _GazeControllerState(GazeController);

        // Find the frame in base animation at which gaze shift amplitude is the greatest
        var baseAnimationInstance = AnimationTimeline.Instance.GetLayer("BaseAnimation").Animations[0].Animation;
        int curFrame = AnimationTimeline.Instance.CurrentFrame;
        int endFrame = curFrame + LEAPCore.editFrameRate; // look 1s ahead
        var maxAmpState = new _GazeControllerState(GazeController);
        for (int frame = curFrame + 1; frame < endFrame; ++frame)
        {
            baseAnimationInstance.Apply(frame, AnimationLayerMode.Override);

            // Recompute gaze shift parameters
            GazeController._InitGazeParams();
            GazeController._InitTargetRotations();
            GazeController._InitLatencies();
            GazeController._CalculateMaxVelocities();

            if (GazeController.Amplitude > maxAmpState.amplitude)
            {
                // Gaze shift amplitude is higher at this frame
                maxAmpState = new _GazeControllerState(GazeController);
            }
        }
        
        // Reapply current gaze controller state 
        curState.Apply(GazeController);

        // Apply velocities and latencies from the max. amplitude state
        for (int jointIndex = 0; jointIndex < GazeController.gazeJoints.Length; ++jointIndex)
        {
            GazeController.gazeJoints[jointIndex].maxVelocity = maxAmpState.gazeJointStates[jointIndex].maxVelocity;
            GazeController.gazeJoints[jointIndex].latency = maxAmpState.gazeJointStates[jointIndex].latency;
            GazeController.gazeJoints[jointIndex].latencyTime = maxAmpState.gazeJointStates[jointIndex].latencyTime;
        }

        // Reapply current model pose
        AnimationTimeline.Instance.ApplyModelPose(Model.gameObject.name, AnimationClip.name + "Pose");
        AnimationTimeline.Instance.RemoveModelPose(AnimationClip.name + "Pose");
    }

    // Handler for gaze controller state changes
    protected virtual void GazeController_StateChange(AnimController sender, int  srcState, int trgState)
    {
        if (trgState == (int)GazeState.Shifting)
        {
            _gazeShiftStarted = true;
        }
    }
}

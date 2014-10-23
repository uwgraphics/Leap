using UnityEngine;
using System;
using System.Collections;
using MathNet.Numerics;
using MathNet.Numerics.Distributions;
using MathNet.Numerics.RandomSources;

public enum FaceState
{
    Idle,
    Speech,
    Gesturing
}

public enum FaceGestureSpeed
{
    Slow,
    Normal,
    Fast
}

/// <summary>
/// Face animation controller.
/// </summary>
public class FaceController : AnimController
{
    [Serializable]
    public class GestureSpeedMultDef
    {
        public FaceGestureSpeed speed;
        public float mult = 1f;

        public GestureSpeedMultDef(FaceGestureSpeed speed, float mult)
        {
            this.speed = speed;
            this.mult = mult;
        }
    }

    /// <summary>
    /// If true, subtle, random motion generated using Perlin noise
    /// will be applied to the head.
    /// </summary>
    public bool randomMotionEnabled = true;

    /// <summary>
    /// For generating random head motion. 
    /// </summary>
    public PerlinMotionGenerator randomMotionGen = new PerlinMotionGenerator();

    /// <summary>
    /// If true, random motion generated using Perlin noise
    /// is applied to the head, similar to the kind of head motion
    /// humans exibit while speaking.
    /// </summary>
    public bool speechMotionEnabled = false;

    /// <summary>
    /// For generating random head motion accompanying speech.
    /// </summary>
    public PerlinMotionGenerator speechMotionGen = new PerlinMotionGenerator();

    // TODO: magnitude and frequency of speech motion

    /// <summary>
    /// If true, the agent will begin a head gesture on next frame.
    /// </summary>
    public bool doGesture = false;

    /// <summary>
    /// If true, the agent will stop the ongoing head gesture on next frame.
    /// </summary>
    public bool stopGesture = false;

    /// <summary>
    /// How many gestures to execute in quick succession. 
    /// </summary>
    public int numGestures = 1;

    /// <summary>
    /// Speed of the gesture. 
    /// </summary>
    public FaceGestureSpeed gestSpeed = FaceGestureSpeed.Normal;

    /// <summary>
    /// Vertical target rotation of the gesture, given in terms of
    /// pitch angle.
    /// </summary>
    public float gestTargetVert = 0;

    /// <summary>
    /// Horizontal target rotation of the gesture, given in terms of
    /// yaw angle.
    /// </summary>
    public float gestTargetHor = 0;

    /// <summary>
    /// Vertical return rotation of the gesture, given in terms of
    /// pitch angle.
    /// </summary>
    public float gestReturnVert = 0;

    /// <summary>
    /// Horizontal return rotation of the gesture, given in terms of
    /// yaw angle.
    /// </summary>
    public float gestReturnHor = 0;

    /// <summary>
    /// Vertical final rotation of the gesture, given in terms of
    /// pitch angle.
    /// </summary>
    public float gestFinalVert = 0;

    /// <summary>
    /// Horizontal final rotation of the gesture, given in terms of
    /// yaw angle.
    /// </summary>
    public float gestFinalHor = 0;

    /// <summary>
    /// How long the head should remain in the target pose before moving to
    /// return pose, and vice versa (in seconds).
    /// </summary>
    public float gestSustainLength = 0f;

    /// <summary>
    /// Multiplies applied to head velocity for different head gesture speeds.
    /// Don't change these unless you know what you're doing.
    /// </summary>
    public GestureSpeedMultDef[] gestSpeedMults;

    // Gesture parameters:
    protected int gestLeft;
    protected bool ret;
    protected bool sustain;
    protected float time;
    protected float length;
    protected UnityEngine.Quaternion origRot;
    protected UnityEngine.Quaternion trgRot;
    protected UnityEngine.Quaternion retRot;
    protected UnityEngine.Quaternion finRot;
    protected UnityEngine.Quaternion sustainRot;
    protected float maxHeadVelocity;

    // Random gesture generation:
    protected MersenneTwisterRandomSource randNumGen = null;
    protected NormalDistribution normDist = null;
    protected float pauseTime;
    protected float pauseLength;

    /// <summary>
    /// Shorthand for getting the head joint.
    /// </summary>
    public virtual DirectableJoint Head
    {
        get
        {
            foreach (DirectableJoint joint in puppetJoints)
                if (joint.bone.tag == "HeadBone")
                    return joint;

            return null;
        }
    }

    /// <summary>
    /// Shorthand for getting the neck joint.
    /// </summary>
    public virtual DirectableJoint Neck
    {
        get
        {
            foreach (DirectableJoint joint in puppetJoints)
                if (joint.bone.tag == "NeckBone")
                    return joint;

            return null;
        }
    }

    /// <summary>
    /// Performs a head gesture. 
    /// </summary>
    /// <param name="numGestures">
    /// Number of gestures to perform in quick sequence.
    /// </param>
    /// <param name="speed">
    /// Speed of the gesture.
    /// </param>
    /// <param name="targetVert">
    /// Vertical target rotation of the gesture, given in terms of
    /// pitch angle.
    /// </param>
    /// <param name="targetHor">
    /// Horizontal target rotation of the gesture, given in terms of
    /// yaw angle.
    /// </param>
    /// <param name="returnVert">
    /// Vertical return rotation of the gesture, given in terms of
    /// pitch angle.
    /// </param>
    /// <param name="returnHor">
    /// Horizontal return rotation of the gesture, given in terms of
    /// yaw angle.
    /// </param>
    /// <param name="finalVert">
    /// Vertical final rotation of the gesture, given in terms of
    /// pitch angle.
    /// </param>
    /// <param name="finalHor">
    /// Horizontal final rotation of the gesture, given in terms of
    /// yaw angle.
    /// </param>
    /// <param name="sustainLength">
    /// How long the head should remain in the target pose before moving to
    /// return pose, and vice versa (in seconds).
    /// </param>
    public virtual void DoGesture(int numGestures, FaceGestureSpeed speed,
                                  float targetVert, float targetHor,
                                  float returnVert, float returnHor,
                                  float finalVert, float finalHor,
                                  float sustainLength)
    {
        this.numGestures = numGestures;
        gestSpeed = speed;
        gestTargetVert = targetVert;
        gestTargetHor = targetHor;
        gestReturnVert = returnVert;
        gestReturnHor = returnHor;
        gestFinalVert = finalVert;
        gestFinalHor = finalHor;
        gestSustainLength = sustainLength;
        doGesture = true;
    }

    /// <summary>
    /// Performs a nod gesture.
    /// </summary>
    /// <param name="numNods">
    /// Number of gestures to perform in quick sequence.
    /// </param>
    /// <param name="speed">
    /// Speed of the gesture.
    /// </param>
    /// <param name="targetVert">
    /// Vertical target rotation of the gesture, given in terms of
    /// pitch angle.
    /// </param>
    /// <param name="sustainLength">
    /// How long the head should remain in the target pose before moving to
    /// return pose, and vice versa (in seconds).
    /// </param>
    public virtual void Nod(int numNods, FaceGestureSpeed speed,
                            float targetVert, float sustainLength)
    {
        DoGesture(numNods, speed, targetVert, 0,
                  0, 0, 0, 0, sustainLength);
    }

    /// <summary>
    /// Performs a shake gesture.
    /// </summary>
    /// <param name="numShakes">
    /// Number of gestures to perform in quick sequence.
    /// </param>
    /// <param name="speed">
    /// Speed of the gesture.
    /// </param>
    /// <param name="targetHor">
    /// Vertical target rotation of the gesture, given in terms of
    /// pitch angle.
    /// </param>
    /// <param name="sustainLength">
    /// How long the head should remain in the target pose before moving to
    /// return pose, and vice versa (in seconds).
    /// </param>
    public virtual void Shake(int numShakes, FaceGestureSpeed speed,
                              float targetHor, float sustainLength)
    {
        DoGesture(numShakes, speed, 0, -targetHor,
                  targetHor, 0, 0, 0, sustainLength);
    }

    /// <summary>
    /// Stops an ongoing head gesture.
    /// </summary>
    public virtual void StopGesture()
    {
        if (StateId == (int)FaceState.Gesturing)
            stopGesture = true;
    }

    protected override void _Init()
    {
        // Initialize random motion generators
        randomMotionGen.Init(gameObject);
        speechMotionGen.Init(gameObject);

        // Set up random number generators
        randNumGen = new MersenneTwisterRandomSource();
        normDist = new NormalDistribution(randNumGen);
        normDist.SetDistributionParameters(0.5f, 0.2f);
    }

    protected virtual void Update_Idle()
    {
        if (randomMotionEnabled)
        {
            if (!randomMotionGen.Running)
                randomMotionGen.Start();

            // Update and apply random head motion
            randomMotionGen.Update();
            randomMotionGen.Apply();
        }
        else
        {
            randomMotionGen.Stop();
        }
    }

    protected virtual void LateUpdate_Idle()
    {
        if (randomMotionEnabled)
            // Apply random head motion
            randomMotionGen.LateApply();

        if (doGesture)
            // Start head gesture
            GoToState((int)FaceState.Gesturing);
        else if (randomMotionEnabled && speechMotionEnabled)
            // Start performing more pronounced random head motion
            GoToState((int)FaceState.Speech);
    }

    protected virtual void Update_Speech()
    {
        if (!speechMotionGen.Running)
            speechMotionGen.Start();
        speechMotionGen.Update();
    }

    protected virtual void LateUpdate_Speech()
    {
        speechMotionGen.LateApply();
        pauseTime += Time.deltaTime;

        if (doGesture /*|| pauseTime > pauseLength*/ )
            // Start head gesture
            GoToState((int)FaceState.Gesturing);
        else if (!speechMotionEnabled)
            // Go back to more subtle random head motion
            GoToState((int)FaceState.Idle);
        return;
    }

    protected virtual void Update_Gesturing()
    {
    }

    protected virtual void LateUpdate_Gesturing()
    {
        // Compute gesture head rotation, and apply it
        float t = length > 0.0001f ? time / length : 1f;
        float t2 = t * t;
        t = -2 * t2 * t + 3 * t2;
        UnityEngine.Quaternion rot = UnityEngine.Quaternion.identity;
        if (sustain)
            // Sustain phase, just keep previous pose
            rot = sustainRot;
        else if (gestLeft == numGestures && gestLeft > 0 && !ret)
            // First attack phase of the gesture
            rot = UnityEngine.Quaternion.Slerp(origRot, trgRot, t);
        else if (gestLeft > 0 && ret)
            // Return phase of the gesture
            rot = UnityEngine.Quaternion.Slerp(trgRot, retRot, t);
        else if (gestLeft > 0 && !ret)
            // Subsequent attack phase of the gesture
            rot = UnityEngine.Quaternion.Slerp(retRot, trgRot, t);
        else if (gestLeft <= 0 && ret)
            // Final phase of the gesture
            rot = UnityEngine.Quaternion.Slerp(trgRot, finRot, t);
        else
            rot = finRot;
        Head.bone.localRotation = rot;

        time += Time.deltaTime;
        if (time > length)
        {
            time = 0;

            // Compute parameters for the next phase of the gesture
            if (!sustain)
            {
                // We just finished a movement phase

                if (!ret)
                    --gestLeft;
                ret = !ret;

                // Compute length of next phase
                if (gestSustainLength > 0)
                {
                    // Insert a pause/sustain phase
                    sustain = true;
                    length = gestSustainLength;
                    sustainRot = rot;
                }
            }
            else
            {
                // We just finished a sustain phase

                sustain = false;

                // Compute length of next phase
                if (gestLeft == numGestures && gestLeft > 0 && !ret)
                    // First attack phase of the gesture
                    length = _ComputeHeadRotationLength(origRot, trgRot);
                else if (gestLeft > 0 && ret)
                    // Return phase of the gesture
                    length = _ComputeHeadRotationLength(trgRot, retRot);
                else if (gestLeft > 0 && !ret)
                    // Subsequent attack phase of the gesture
                    length = _ComputeHeadRotationLength(retRot, trgRot);
                else if (gestLeft == 0 && ret)
                    // Final phase of the gesture
                    length = _ComputeHeadRotationLength(trgRot, finRot);
            }
        }

        if (stopGesture || gestLeft < 0)
        {
            // Gesture canceled, or completed
            GoToState(randomMotionEnabled && speechMotionEnabled ?
                      (int)FaceState.Speech :
                      (int)FaceState.Idle);
        }
    }

    protected virtual void Transition_IdleSpeech()
    {
        randomMotionGen.Stop();
        //_GenerateNextSpeechGesture();
    }

    protected virtual void Transition_SpeechIdle()
    {
        speechMotionGen.Stop();
    }

    protected virtual void Transition_IdleGesturing()
    {
        randomMotionGen.Stop();
        _InitGesture();
    }

    protected virtual void Transition_GesturingIdle()
    {
        stopGesture = false;
    }

    protected virtual void Transition_SpeechGesturing()
    {
        speechMotionGen.Stop();
        _InitGesture();
    }

    protected virtual void Transition_GesturingSpeech()
    {
        stopGesture = false;
        //_GenerateNextSpeechGesture();
    }

    // Initialize gesture parameters
    private void _InitGesture()
    {
        doGesture = false;
        stopGesture = false;
        maxHeadVelocity = gestSpeedMults[(int)gestSpeed].mult * Head.velocity;
        gestLeft = numGestures;
        ret = false;
        sustain = false;
        origRot = Head.bone.localRotation;
        trgRot = _ComputeTargetHeadRotation(gestTargetVert, gestTargetHor);
        retRot = _ComputeTargetHeadRotation(gestReturnVert, gestReturnHor);
        finRot = _ComputeTargetHeadRotation(gestFinalVert, gestFinalHor);
        time = 0;
        length = _ComputeHeadRotationLength(origRot, trgRot);
    }

    // Compute bone rotation after change to yaw and pitch has been applied
    private UnityEngine.Quaternion _ComputeTargetHeadRotation(float vert, float hor)
    {
        Head.Pitch += vert;
        Head.Yaw += hor;
        UnityEngine.Quaternion rot = Head.bone.localRotation;
        Head.bone.localRotation = origRot;

        return rot;
    }

    // Compute time length of head rotation from one pose to another
    private float _ComputeHeadRotationLength(UnityEngine.Quaternion srcRot, UnityEngine.Quaternion trgRot)
    {
        return UnityEngine.Quaternion.Angle(srcRot, trgRot) * 2f / maxHeadVelocity;
    }

    private void _GenerateNextSpeechGesture()
    {
        // Compute gesture timing
        pauseTime = 0f;
        pauseLength = (float)normDist.NextDouble();
        if (pauseLength > 1.5f)
            pauseLength = 1.5f;

        // Compute gesture shape
        Vector3 drotv = speechMotionGen.SampleRotation(0);
        //float fvm = UnityEngine.Random.Range(-1f,1f);
        //DoGesture( 1, gestSpeed, drotv.x, drotv.y, 0, 0, fvm*drotv.x, fvm*drotv.y, 0 );
        DoGesture(1, FaceGestureSpeed.Slow, drotv.x, drotv.y, drotv.x, drotv.y, drotv.x, drotv.y, 0);
        doGesture = false;
        // TODO: sample from Perlin noise!
    }

    public static void _InitRandomHeadMotion(GameObject agent)
    {
        Transform bone = ModelController.FindBoneWithTag(agent.transform, "HeadBone");

        if (bone == null)
        {
            Debug.LogWarning("Unable to initialize random head motion on agent " + agent.name +
                             " because head bone is not defined.");

            return;
        }

        FaceController faceCtrl = agent.GetComponent<FaceController>();

        if (faceCtrl == null)
        {
            Debug.LogWarning("Unable to initialize random head motion on agent " + agent.name +
                             " because the agent does not have a face controller.");

            return;
        }

        PerlinMotionGenerator gen = faceCtrl.randomMotionGen;

        gen.transforms = new PerlinMotionGenerator.TransformMapping[1];
        gen.transforms[0] = new PerlinMotionGenerator.TransformMapping();
        gen.transforms[0].bone = bone;
        gen.transforms[0].transfTypes = PerlinMotionGenerator.TransformType.Rotation;
        gen.transforms[0].rotationAxis = PerlinMotionGenerator.Axis.All;

        // Initialize gesture speed multipliers
        faceCtrl.gestSpeedMults = new FaceController.GestureSpeedMultDef[3];
        faceCtrl.gestSpeedMults[0] = new FaceController.GestureSpeedMultDef(FaceGestureSpeed.Slow, 0.3f);
        faceCtrl.gestSpeedMults[1] = new FaceController.GestureSpeedMultDef(FaceGestureSpeed.Normal, 1f);
        faceCtrl.gestSpeedMults[2] = new FaceController.GestureSpeedMultDef(FaceGestureSpeed.Fast, 3f);
    }

    public override void _CreateStates()
    {
        // Initialize states
        _InitStateDefs<FaceState>();
        _InitStateTransDefs((int)FaceState.Idle, 3);
        _InitStateTransDefs((int)FaceState.Speech, 3);
        _InitStateTransDefs((int)FaceState.Gesturing, 2);
        //_InitAnimStates<FaceState>( FaceState.Idle, 1 );
        states[(int)FaceState.Idle].updateHandler = "Update_Idle";
        states[(int)FaceState.Idle].lateUpdateHandler = "LateUpdate_Idle";
        states[(int)FaceState.Idle].nextStates[0].nextState = "Idle";
        states[(int)FaceState.Idle].nextStates[0].transitionHandler = "Transition_Idle";
        states[(int)FaceState.Idle].nextStates[1].nextState = "Speech";
        states[(int)FaceState.Idle].nextStates[1].transitionHandler = "Transition_IdleSpeech";
        states[(int)FaceState.Idle].nextStates[2].nextState = "Gesturing";
        states[(int)FaceState.Idle].nextStates[2].transitionHandler = "Transition_IdleGesturing";
        states[(int)FaceState.Speech].updateHandler = "Update_Speech";
        states[(int)FaceState.Speech].lateUpdateHandler = "LateUpdate_Speech";
        states[(int)FaceState.Speech].nextStates[0].nextState = "Speech";
        states[(int)FaceState.Speech].nextStates[0].transitionHandler = "Transition_Speech";
        states[(int)FaceState.Speech].nextStates[1].nextState = "Idle";
        states[(int)FaceState.Speech].nextStates[1].transitionHandler = "Transition_SpeechIdle";
        states[(int)FaceState.Speech].nextStates[2].nextState = "Gesturing";
        states[(int)FaceState.Speech].nextStates[2].transitionHandler = "Transition_SpeechGesturing";
        states[(int)FaceState.Gesturing].updateHandler = "Update_Gesturing";
        states[(int)FaceState.Gesturing].lateUpdateHandler = "LateUpdate_Gesturing";
        states[(int)FaceState.Gesturing].nextStates[0].nextState = "Idle";
        states[(int)FaceState.Gesturing].nextStates[0].transitionHandler = "Transition_GesturingIdle";
        states[(int)FaceState.Gesturing].nextStates[1].nextState = "Speech";
        states[(int)FaceState.Gesturing].nextStates[1].transitionHandler = "Transition_GesturingSpeech";
    }
}

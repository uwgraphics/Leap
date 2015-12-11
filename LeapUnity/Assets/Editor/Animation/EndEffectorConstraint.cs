using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// End-effector constraint specification.
/// </summary>
public struct EndEffectorConstraint
{
    /// <summary>
    /// End-effector name.
    /// </summary>
    public string endEffector;

    /// <summary>
    /// Constraint start frame.
    /// </summary>
    public int startFrame;

    /// <summary>
    /// Length of the constraint in frames.
    /// </summary>
    public int frameLength;

    /// <summary>
    /// If true, absolute rotation of the end-effector should be preserved.
    /// </summary>
    public bool preserveAbsoluteRotation;

    /// <summary>
    /// Scene object to which the end-effector should be aligned
    /// </summary>
    public GameObject target;

    /// <summary>
    /// Handle on the manipulated object that must be aligned with the end-effector during manipulation.
    /// </summary>
    public GameObject manipulatedObjectHandle;

    /// <summary>
    /// If constraint specifies an object manipulation, this is the manipulation length in frames.
    /// </summary>
    public int manipulationFrameLength;

    /// <summary>
    /// Length of the frame window over which the constraint will become active.
    /// </summary>
    public int activationFrameLength;

    /// <summary>
    /// Length of the frame window over which the constraint will become inactive.
    /// </summary>
    public int deactivationFrameLength;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="endEffector">End-effector name</param>
    /// <param name="startFrame">Constraint start frame</param>
    /// <param name="frameLength">Length of the constraint in frames</param>
    /// <param name="preserveAbsoluteRotation">If true, absolute rotation of the end-effector should be preserved</param>
    /// <param name="target">Scene object to which the end-effector should be aligned</param>
    /// <param name="manipulatedObjectHandle">Handle on the manipulated object that must be aligned with the end-effector during manipulation</param>
    /// <param name="manipulationFrameLength">If constraint specifies an object manipulation, this is the manipulation length in frames</param>
    /// <param name="activationFrameLength">Length of the frame window over which the constraint will become active</param>
    /// <param name="deactivationFrameLength">Length of the frame window over which the constraint will become inactive</param>
    public EndEffectorConstraint(string endEffector, int startFrame, int frameLength, bool preserveAbsoluteRotation,
        GameObject target = null, GameObject manipulatedObjectHandle = null, int manipulationFrameLength = -1,
        int activationFrameLength = -1, int deactivationFrameLength = -1)
    {
        this.endEffector = endEffector;
        this.startFrame = startFrame;
        this.frameLength = frameLength;
        this.preserveAbsoluteRotation = preserveAbsoluteRotation;
        this.target = target;
        this.manipulatedObjectHandle = manipulatedObjectHandle;
        this.manipulationFrameLength = manipulationFrameLength;
        this.activationFrameLength = activationFrameLength > -1 ? activationFrameLength :
            Mathf.RoundToInt(LEAPCore.editFrameRate * LEAPCore.endEffectorConstraintActivationTime);
        this.deactivationFrameLength = deactivationFrameLength > -1 ? deactivationFrameLength :
            Mathf.RoundToInt(LEAPCore.editFrameRate * LEAPCore.endEffectorConstraintActivationTime);
    }
}

/// <summary>
/// Container holding end-effector constraint annotations for a particular animation clip.
/// </summary>
public class EndEffectorConstraintContainer
{
    /// <summary>
    /// Animation clip.
    /// </summary>
    public AnimationClip AnimationClip
    {
        get;
        private set;
    }

    private Dictionary<string, List<EndEffectorConstraint>> _constraints;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="animationClip">Animation clip</param>
    /// <param name="constraints">End-effector constraints for the animation clip</param>
    public EndEffectorConstraintContainer(AnimationClip animationClip, EndEffectorConstraint[] constraints)
    {
        this.AnimationClip = animationClip;

        _constraints = new Dictionary<string, List<EndEffectorConstraint>>();
        for (int constraintIndex = 0; constraintIndex < constraints.Length; ++constraintIndex)
        {
            string endEffector = constraints[constraintIndex].endEffector;
            if (!_constraints.ContainsKey(endEffector))
                _constraints[endEffector] = new List<EndEffectorConstraint>();

            _constraints[endEffector].Add(constraints[constraintIndex]);
        }
    }

    /// <summary>
    /// Get list of constraints on the specified end-effector.
    /// </summary>
    /// <param name="endEffector">End-effector tag</param>
    /// <returns>List of constraints</returns>
    public IList<EndEffectorConstraint> GetConstraintsForEndEffector(string endEffector)
    {
        return _constraints.ContainsKey(endEffector) ? _constraints[endEffector].AsReadOnly() : null;
    }

    /// <summary>
    /// Get end-effector constraints active at the specified frame.
    /// </summary>
    /// <param name="endEffector">End-effector tag</param>
    /// <param name="frame">Frame index</param>
    /// <returns>Active end-effector constraints</returns>
    public EndEffectorConstraint[] GetConstraintsAtFrame(string endEffector, int frame)
    {
        if (!_constraints.ContainsKey(endEffector))
            return null;

        var activeConstraints = _constraints[endEffector].Where(c =>
                frame >= (c.startFrame - c.activationFrameLength) &&
                frame <= (c.startFrame + c.frameLength - 1 + c.deactivationFrameLength) &&
                c.manipulatedObjectHandle == null).ToArray();

        return activeConstraints.Length > 0 ? activeConstraints : null;
    }

    /// <summary>
    /// Get end-effector constraints with active object manipulation at the specified frame.
    /// </summary>
    /// <param name="endEffector">End-effector tag</param>
    /// <param name="frame">Frame index</param>
    /// <returns>Active end-effector constraints</returns>
    public EndEffectorConstraint[] GetManipulationConstraintsAtFrame(string endEffector, int frame)
    {
        if (!_constraints.ContainsKey(endEffector))
            return null;

        var activeConstraints = _constraints[endEffector].Where(c =>
                frame >= c.startFrame &&
                frame <= (c.startFrame + c.manipulationFrameLength - 1) &&
                c.manipulatedObjectHandle != null).ToArray();

        return activeConstraints.Length > 0 ? activeConstraints : null;
    }
}

/// <summary>
/// Specifies an animation of a helper target for end-effector constraints that constrain
/// end-effectors to follow trajectories encoded in the original animation.
/// </summary>
public struct EndEffectorTargetHelperAnimation
{
    /// <summary>
    /// End-effector tag.
    /// </summary>
    public string endEffectorTag;

    /// <summary>
    /// End-effector target helper object.
    /// </summary>
    public GameObject helper;

    /// <summary>
    /// End-effector target helper animation clip.
    /// </summary>
    public AnimationClip helperAnimationClip;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="endEffectorTag">End-effector tag</param>
    /// <param name="helper">End-effector target helper object</param>
    /// <param name="helperAnimationClip">End-effector target helper animation clip</param>
    public EndEffectorTargetHelperAnimation(string endEffectorTag, GameObject helper, AnimationClip helperAnimationClip)
    {
        this.endEffectorTag = endEffectorTag;
        this.helper = helper;
        this.helperAnimationClip = helperAnimationClip;
    }
}

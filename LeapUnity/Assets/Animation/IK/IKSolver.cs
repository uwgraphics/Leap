using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// IK goal descriptor.
/// </summary>
[Serializable]
public class IKGoal
{
    /// <summary>
    /// End-effector bone.
    /// </summary>
    public Transform endEffector;

    /// <summary>
    /// Target world-space position of the end-effector.
    /// </summary>
    public Vector3 position;

    /// <summary>
    /// Goal weight.
    /// </summary>
    public float weight;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="endEffector">End-effector bone</param>
    /// <param name="position">Target world-space position of the end-effector</param>
    /// <param name="weight">Goal weight</param>
    public IKGoal(Transform endEffector, Vector3 position, float weight)
    {
        this.endEffector = endEffector;
        this.position = position;
        this.weight = weight;
    }
}

/// <summary>
/// Abstract class representing an IK solver.
/// </summary>
public abstract class IKSolver : MonoBehaviour
{
    /// <summary>
    /// IK goals.
    /// </summary>
    public IKGoal[] goals = new IKGoal[0];

    /// <summary>
    /// Character model controller.
    /// </summary>
    public ModelController Model
    {
        get;
        protected set;
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    public IKSolver()
    {
    }

    /// <summary>
    /// Initialize the IK solver.
    /// </summary>
    public virtual void Init()
    {
        Model = gameObject.GetComponent<ModelController>();
        if (Model == null)
            Debug.LogError("Cannot initialize IKSolver on a character without ModelController");
    }

    /// <summary>
    /// Given the current set of IK goals, solve for the character's pose.
    /// </summary>
    public abstract void Solve();

    public virtual void Start()
    {
        Init();
    }

    public virtual void LateUpdate()
    {
        Solve();
    }
}

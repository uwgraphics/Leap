using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// IK goal descriptor.
/// </summary>
public struct IKGoal
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
    /// Target world-space rotation of the end-effector.
    /// </summary>
    public Quaternion rotation;

    /// <summary>
    /// Goal weight.
    /// </summary>
    public float weight;

    /// <summary>
    /// Preserve absolute orientation of the end-effector.
    /// </summary>
    public bool preserveAbsoluteRotation;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="endEffector">End-effector bone</param>
    /// <param name="position">Target world-space position of the end-effector</param>
    /// <param name="rotation">Target world-space rotation of the end-effector</param>
    /// <param name="weight">Goal weight</param>
    /// <param name="preserveAbsoluteRotation">Preserve aboslute orientation of the end-effector</param>
    public IKGoal(Transform endEffector, Vector3 position, Quaternion rotation, float weight,
        bool preserveAbsoluteRotation = false)
    {
        this.endEffector = endEffector;
        this.position = position;
        this.rotation = rotation;
        this.weight = weight;
        this.preserveAbsoluteRotation = preserveAbsoluteRotation;
    }
}

/// <summary>
/// Abstract class representing an IK solver.
/// </summary>
public abstract class IKSolver : MonoBehaviour
{
    /// <summary>
    /// Tags on end-effectors handled by this IK solver.
    /// </summary>
    public string[] endEffectors = new string[0];

    /// <summary>
    /// Goals specified for the IK goal.
    /// </summary>
    public IEnumerable<IKGoal> Goals
    {
        get { return _goals.AsEnumerable(); }
    }

    /// <summary>
    /// Character model controller.
    /// </summary>
    public ModelController Model
    {
        get;
        protected set;
    }

    protected List<IKGoal> _goals = new List<IKGoal>();

    /// <summary>
    /// Specify a new goal for the IK solver.
    /// </summary>
    /// <param name="goal">IK goal</param>
    public void AddGoal(IKGoal goal)
    {
        _goals.Add(goal);
    }

    /// <summary>
    /// Clear all goals specified for the IK solver.
    /// </summary>
    public void ClearGoals()
    {
        _goals.Clear();
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
    public virtual void Solve()
    {
        if (!enabled)
            return;

        if (_goals.Any(g => !endEffectors.Any(ee => ee == g.endEffector.tag)))
        {
            Debug.LogError("Specified IK goals for end-effectors not handled by the IK solver");
            return;
        }

        _ConsolidateGoals();
        _Solve();
    }

    public virtual void Start()
    {
        Init();
    }

    public virtual void LateUpdate()
    {
        Solve();
    }

    // Do the actual solve - subclasses of IKSolver must implement this
    protected abstract void _Solve();

    // Merge all goals on the same end-effector into a single goal
    protected virtual void _ConsolidateGoals()
    {
        IKGoal[] goals = _goals.ToArray();
        _goals.Clear();

        for (int endEffectorIndex = 0; endEffectorIndex < endEffectors.Length; ++endEffectorIndex)
        {
            float sumWeights = 0f;
            Vector3 pos = Vector3.zero;
            Vector3 rot = Vector3.zero;

            IKGoal[] curEndEffGoals = goals.Where(g => g.endEffector.tag == endEffectors[endEffectorIndex]).ToArray();

            // Sum weights of all goals for the current end-effector
            for (int goalIndex = 0; goalIndex < curEndEffGoals.Length; ++goalIndex)
            {
                IKGoal goal = curEndEffGoals[goalIndex];
                sumWeights += goal.weight;
            }

            if (sumWeights < 0.005f)
                // No goals for the current end-effector
                continue;

            // Compute consolidated goal
            for (int goalIndex = 0; goalIndex < curEndEffGoals.Length; ++goalIndex)
            {
                IKGoal goal = curEndEffGoals[goalIndex];
                float normWeight = goal.weight / sumWeights;
                pos += normWeight * goal.position;
                rot += normWeight * QuaternionUtil.Log(
                    goal.preserveAbsoluteRotation ? goal.rotation : goal.endEffector.rotation);
            }

            // Add consolidated goal
            IKGoal newGoal = new IKGoal(
                curEndEffGoals[0].endEffector,
                pos, QuaternionUtil.Exp(rot),
                Mathf.Min(sumWeights, 1f), true);
            _goals.Add(newGoal);
        }
    }
}

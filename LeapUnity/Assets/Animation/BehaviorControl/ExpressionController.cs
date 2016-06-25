using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MathNet.Numerics;
using MathNet.Numerics.Distributions;
using MathNet.Numerics.RandomSources;

public enum ExpressionState
{
    Static,
    Changing
};

/// <summary>
/// Animation controller for facial expressions.
/// </summary>
public class ExpressionController : AnimController
{
    /// <summary>
    /// Target facial expression.
    /// </summary>
    public string expression = "ExpressionSmileOpen";

    /// <summary>
    /// Execute expression change right away.
    /// </summary>
    public bool changeExpression = false;

    /// <summary>
    /// Target expression magnitude (0-1).
    /// </summary>
    public float magnitude = 0.06f;

    /// <summary>
    /// How long the expression change should take (in seconds).
    /// </summary>
    public float changeTime = 2.5f;

    protected int curExprIndex = -1;
    protected float curMag = 0f;
    protected int prevExprIndex = -1;
    protected float prevMag = 0f;
    protected float curTime = 0f;
    protected float curLength = 0f;

    /// <summary>
    /// Initiates facial expression change. 
    /// </summary>
    /// <param name="exprName">
    /// New facial expression.
    /// </param>
    public virtual void ChangeExpression(string exprName)
    {
        changeExpression = true;
        expression = exprName;
    }

    public override void Start()
    {
        base.Start();

        _morphController = gameObject.GetComponent<MorphController>();
        curExprIndex = -1;
        curMag = 0f;
    }

    protected virtual void Update_Static()
    {
        if (curExprIndex >= 0)
            // Apply current expression
            _morphController.morphChannels[curExprIndex].weight = curMag;

        if (changeExpression)
        {
            // Change expression to something else
            int nextExprIndex = _morphController.GetMorphChannelIndex(expression);
            if (nextExprIndex != -1)
                GoToState((int)ExpressionState.Changing);
            else
                changeExpression = false;
        }
    }

    protected virtual void Update_Changing()
    {
        curTime += DeltaTime;
        float t = curTime / curLength;

        if (curTime >= curLength)
        {
            // Expression change done
            if (prevExprIndex >= 0)
                _morphController.morphChannels[prevExprIndex].weight = 0f;
            _morphController.morphChannels[curExprIndex].weight = curMag;
            GoToState((int)ExpressionState.Static);
        }

        // Apply expression change
        if (curExprIndex == prevExprIndex)
            _morphController.morphChannels[curExprIndex].weight = _ComputeExpressionWeight(prevMag, curMag, t);
        else
        {
            _morphController.morphChannels[curExprIndex].weight = _ComputeExpressionWeight(0f, curMag, t);
            if (prevExprIndex > -1)
                _morphController.morphChannels[prevExprIndex].weight = _ComputeExpressionWeight(prevMag, 0f, t);
        }
    }

    protected virtual void Transition_StaticChanging()
    {
        changeExpression = false;
        prevExprIndex = curExprIndex;
        prevMag = curMag;
        curExprIndex = _morphController.GetMorphChannelIndex(expression);
        curMag = magnitude;
        curTime = 0f;
        curLength = changeTime;
    }

    protected virtual void Transition_ChangingStatic()
    {
        curTime = 0f;
        curLength = 0f;
    }

    protected virtual float _ComputeExpressionWeight(float mag0, float mag1, float t)
    {
        float t2 = t * t;
        float mag = (1f + 2 * t2 * t - 3 * t2) * mag0 + (-2 * t2 * t + 3 * t2) * mag1;
        return mag;
    }

    public override void _CreateStates()
    {
        // Initialize states
        _InitStateDefs<ExpressionState>();
        _InitStateTransDefs((int)ExpressionState.Static, 1);
        _InitStateTransDefs((int)ExpressionState.Changing, 1);
        states[(int)ExpressionState.Static].updateHandler = "Update_Static";
        states[(int)ExpressionState.Static].nextStates[0].nextState = "Changing";
        states[(int)ExpressionState.Static].nextStates[0].transitionHandler = "Transition_StaticChanging";
        states[(int)ExpressionState.Changing].updateHandler = "Update_Changing";
        states[(int)ExpressionState.Changing].nextStates[0].nextState = "Static";
        states[(int)ExpressionState.Changing].nextStates[0].transitionHandler = "Transition_ChangingStatic";
    }
}

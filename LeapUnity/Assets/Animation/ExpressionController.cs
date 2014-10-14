using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MathNet.Numerics;
using MathNet.Numerics.Distributions;
using MathNet.Numerics.RandomSources;

public enum ExpressionState
{
	Base,
	Maximum,
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
	/// How long it takes to change from one expression to another (in seconds).
	/// </summary>
	public float changeTime = 2.5f;
	
	/// <summary>
	/// Maximum expression magnitude (0-1).
	/// </summary>
	public float maxMagnitude = 0.4f;
	
	/// <summary>
	/// If true, agent expression will periodically change
	/// to maximum magnitude and back.
	/// </summary>
	public bool randomChange = false;
	
	/// <summary>
	/// Duration of maximum magnitude expression (in seconds).
	/// </summary>
	public float maxLength = 1f;
	
	/// <summary>
	/// Rate of maximum magnitude expression onsets (per second).
	/// </summary>
	public float maxOnsetRate = 0f;
	
	/// <summary>
	/// Duration of maximum magnitude expression onset (in seconds).
	/// </summary>
	public float maxOnsetTime = 0.3f;
	
	protected ExpressionState exprState = ExpressionState.Base;
	protected string curExpr = "";
	protected float curMag = 0;
	protected float curTime = 0;
	protected float curOnsetTime = 0;
	protected float curOutsetTime = 0;
	protected float curLength = 0;
	protected Dictionary<int,float> fixedExpr = new Dictionary<int,float>();
	
	protected int exprMTIndex = -1;
	protected float mag = 0;
	protected int curExprMTIndex = -1;
	
	protected MersenneTwisterRandomSource randNumGen;
	protected ExponentialDistribution expDist;
	
	/// <summary>
	/// Initiates facial expression change. 
	/// </summary>
	/// <param name="exprName">
	/// New facial expression.
	/// </param>
	public virtual void ChangeExpression( string exprName )
	{
		changeExpression = true;
		expression = exprName;
	}
	
	/// <summary>
	/// Instantly apply new facial expression. 
	/// </summary>
	/// <param name="exprName">
	/// New facial expression.
	/// </param>
	public virtual void InstaChangeExpression( string exprName )
	{
		expression = exprName;
		curExpr = expression;
		curExprMTIndex = morphCtrl.GetMorphChannelIndex(curExpr);
		curMag = magnitude;
	}
	
	/// <summary>
	/// Fixes the current facial expression, so it won't be affected
	/// by subsequent expression changes.
	/// </summary>
	public virtual void FixExpression()
	{
		if( changeExpression == false &&
		   StateId == (int)ExpressionState.Base &&
		   curExprMTIndex >= 0 )
		{
			if( !fixedExpr.ContainsKey(curExprMTIndex) )
				fixedExpr.Add( curExprMTIndex, curMag );
			curExpr = "";
			curExprMTIndex = -1;
		}
	}
	
	/// <summary>
	/// Unfix the specified facial expression, so it will be affected
	/// by subsquented expression changes.
	/// </summary>
	/// <param name="exprName">
	/// Expression to unfix.
	/// </param>
	public virtual void UnfixExpression( string exprName )
	{
		if( changeExpression == false &&
		   StateId == (int)ExpressionState.Base )
		{
			curExpr = exprName;
			curExprMTIndex = morphCtrl.GetMorphChannelIndex(exprName);
			if( fixedExpr.ContainsKey(curExprMTIndex) )
			{
				curMag = fixedExpr[curExprMTIndex];
				fixedExpr.Remove(curExprMTIndex);
			}
		}
	}

	protected override void _Init()
	{
		// Set up random number generators
		randNumGen = new MersenneTwisterRandomSource();
		expDist = new ExponentialDistribution(randNumGen);
		
		// Find morph targets
		morphCtrl = gameObject.GetComponent<MorphController>();
		exprMTIndex = morphCtrl.GetMorphChannelIndex(expression);
		if( exprMTIndex < 0 )
		{
			enabled = false;
			return;
		}
		curExprMTIndex = exprMTIndex;
		
		// Initial expression is also the current expression
		curExpr = expression;
		curMag = magnitude;
		
		// Compute time until next maximum
		curLength = _GenerateNextMaxOnsetTime();
		curTime = 0;
	}
	
	protected override void _Update()
	{
		// Apply fixed expressions
		foreach( KeyValuePair<int,float> fxmag in fixedExpr )
			morphCtrl.morphChannels[fxmag.Key].weight += fxmag.Value;
	}
	
	protected virtual void Update_Base()
	{
		if( curExprMTIndex >= 0 )
			// Apply current expression
			morphCtrl.morphChannels[curExprMTIndex].weight += curMag;
		
		curTime += Time.deltaTime;
		
		if(changeExpression)
		{
			// Change expression to something else
			
			exprMTIndex = morphCtrl.GetMorphChannelIndex(expression);
			
			if( exprMTIndex != -1 )
			{
				GoToState((int)ExpressionState.Changing);
			}
			else
			{
				changeExpression = false;
				expression = curExpr;
				exprMTIndex = curExprMTIndex;
			}
		}
		
		if( changeExpression || randomChange && curTime >= curLength )
		{
			// Time to go to maximum expression magnitude
			
			GoToState((int)ExpressionState.Maximum);
		}
	}
	
	protected virtual void Update_Maximum()
	{
		curTime += Time.deltaTime;
		
		if( curTime < curOnsetTime )
		{
			// Expression not yet at maximum, apply partially
			
			float t = curTime/curOnsetTime;
			float t2 = t*t;
			morphCtrl.morphChannels[curExprMTIndex].weight += curMag +
				(maxMagnitude-curMag)*(-2*t2*t+3*t2);
		}
		else if( curTime >= curOnsetTime && curTime < curOutsetTime )
		{
			// Expression is at maximum, apply fully
			
			morphCtrl.morphChannels[curExprMTIndex].weight += maxMagnitude;
		}
		else if( curTime >= curOutsetTime && curTime < curLength )
		{
			// Expression going back to base, apply partially
			
			float t = (curTime-curOutsetTime)/(curLength-curOutsetTime);
			float t2 = t*t;
			morphCtrl.morphChannels[curExprMTIndex].weight += maxMagnitude -
				(maxMagnitude-curMag)*(-2*t2*t+3*t2);
		}
		else if( curTime >= curLength )
		{
			// Expression is now at base
			
			GoToState((int)ExpressionState.Base);
		}
	}
	
	protected virtual void Update_Changing()
	{
		curTime += Time.deltaTime;
		
		// Apply current expression
		float t = Mathf.Clamp01(curTime/curLength);
		float t2 = t*t;
		if( curExprMTIndex >= 0 )
			morphCtrl.morphChannels[curExprMTIndex].weight += curMag*(1f+2*t2*t-3*t2);
		morphCtrl.morphChannels[exprMTIndex].weight += mag*(-2*t2*t+3*t2);
		
		if( curTime >= curLength )
		{
			// Expression change done
			
			GoToState((int)ExpressionState.Base);
		}
	}
	
	protected virtual void Transition_BaseMaximum()
	{
		changeExpression = false;
		
		// Compute maximum onset time and length
		curTime = 0;
		curOnsetTime = maxOnsetTime;
		curOutsetTime = curOnsetTime + maxLength;
		curLength = curOutsetTime + curOnsetTime;
	}
	
	protected virtual void Transition_MaximumBase()
	{
		changeExpression = false;
		
		// Compute time until next maximum
		curLength = _GenerateNextMaxOnsetTime();
		curTime = 0;
	}
	
	protected virtual void Transition_BaseChanging()
	{
		changeExpression = false;
		
		// Compute expression change time
		curLength = changeTime > 0 ? changeTime : 0.00001f;
		curTime = 0;
		// Target magnitude?
		mag = magnitude;
	}
	
	protected virtual void Transition_ChangingBase()
	{
		// Set new current expression
		curExpr = expression;
		curExprMTIndex = exprMTIndex;
		curMag = mag;
		
		// Compute time until next maximum
		curLength = _GenerateNextMaxOnsetTime();
		curTime = 0;
	}
	
	private float _GenerateNextMaxOnsetTime()
	{
		if( maxOnsetRate <= 0 )
			return float.MaxValue;
		
		expDist.SetDistributionParameters(maxOnsetRate);
		float time = (float)expDist.NextDouble();
		
		float min_length = 1f/maxOnsetRate;
		if( time < min_length )
			// To prevent multiple expression changes in quick succession
			time = min_length;
			
		return time;
	}
	
	public override void UEdCreateStates()
	{
		// Initialize states
		_InitStateDefs<ExpressionState>();
		_InitStateTransDefs( (int)ExpressionState.Base, 2 );
		_InitStateTransDefs( (int)ExpressionState.Maximum, 1 );
		_InitStateTransDefs( (int)ExpressionState.Changing, 1 );
		states[(int)ExpressionState.Base].updateHandler = "Update_Base";
		states[(int)ExpressionState.Base].nextStates[0].nextState = "Maximum";
		states[(int)ExpressionState.Base].nextStates[0].transitionHandler = "Transition_BaseMaximum";
		states[(int)ExpressionState.Base].nextStates[1].nextState = "Changing";
		states[(int)ExpressionState.Base].nextStates[1].transitionHandler = "Transition_BaseChanging";
		states[(int)ExpressionState.Maximum].updateHandler = "Update_Maximum";
		states[(int)ExpressionState.Maximum].nextStates[0].nextState = "Base";
		states[(int)ExpressionState.Maximum].nextStates[0].transitionHandler = "Transition_MaximumBase";
		states[(int)ExpressionState.Changing].updateHandler = "Update_Changing";
		states[(int)ExpressionState.Changing].nextStates[0].nextState = "Base";
		states[(int)ExpressionState.Changing].nextStates[0].transitionHandler = "Transition_ChangingBase";
	}
}

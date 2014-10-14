using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class BehaviorScheduler : MonoBehaviour
{
	private Behavior curBehavior = null;
	private Queue<Behavior> behaviors = new Queue<Behavior>();
	
	/// <summary>
	/// Returns the currently executing behavior.
	/// </summary>
	public Behavior CurrentBehavior
	{
		get
		{
			return curBehavior;
		}
	}
	
	/// <summary>
	/// Queue of scheduled behaviors.
	/// </summary>
	public Queue<Behavior> Schedule
	{
		get
		{
			return behaviors;
		}
	}
	
	/// <summary>
	/// Creates a behavior and schedules it for execution.
	/// </summary>
	public Behavior CreateBehavior()
	{
		Behavior bhv = new Behavior(gameObject);
		behaviors.Enqueue(bhv);
		return bhv;
	}
	
	/// <summary>
	/// Interrupts the currently executing behavior and
	/// moves on to the first scheduled one.
	/// </summary>
	public void InterruptBehavior()
	{
		if( curBehavior != null )
		{
			curBehavior.Stop();
			curBehavior = null;
		}
	}
	
	/// <summary>
	/// Stops and clears the current and scheduled behaviors.
	/// </summary>
	public void ClearBehaviors()
	{
		curBehavior = null;
		behaviors.Clear();
		
	}
	
	private void Start()
	{
	}
	
	private void Update()
	{
		if( curBehavior == null && behaviors.Count <= 0 )
		{
			// No behaviors scheduled
			return;
		}
		else if( curBehavior == null ) // && behaviors.Count > 0 )
		{
			// Schedule next behavior
			curBehavior = behaviors.Dequeue();
			curBehavior.Execute();
			
			return;
		}
		
		if( !curBehavior.IsFinished() )
		{
			curBehavior.Update();
		}
		else
		{
			// Current behavior is done
			curBehavior = null;
		}
	}
}

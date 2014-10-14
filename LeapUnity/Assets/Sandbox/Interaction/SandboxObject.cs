using UnityEngine;
using System;
using System.Collections;

public delegate void SandboxObjMouseEnterEvtH( SandboxObject sender );
public delegate void SandboxObjMouseExitEvtH( SandboxObject sender );
public delegate void SandboxObjMouseClickEvtH( SandboxObject sender );
public delegate void SandboxObjMouseClickHoldEvtH( SandboxObject sender );
public delegate void SandboxObjMouseClickDragEvtH( SandboxObject sender );
public delegate void SandboxObjMouseClickDropEvtH( SandboxObject sender );

/// <summary>
/// Sandbox game object. This class serves as "glue"
/// between sandbox interface and game object logic.
/// </summary>
public class SandboxObject : MonoBehaviour
{
	
	/// <summary>
	/// The time mouse button must be held down
	/// before mouse click event becomes mouse click hold event.
	/// </summary>
	public float mouseHoldTime = 1f;
	
	/// <summary>
	/// If true, the object can be dragged and dropped by the mouse.
	/// </summary>
	public bool isDragDroppable = false;
	
	/// <summary>
	/// Event triggered when mouse enters the sandbox object.
	/// </summary>
	public event SandboxObjMouseEnterEvtH MouseEnter;
	
	/// <summary>
	/// Event triggered when mouse leaves the sandbox object.
	/// </summary>
	public event SandboxObjMouseExitEvtH MouseExit;
	
	/// <summary>
	/// Event triggered when mouse clicks the sandbox object.
	/// </summary>
	public event SandboxObjMouseClickEvtH MouseClick;
	
	/// <summary>
	/// Event triggered when mouse clicks the sandbox object
	/// and holds for a specified amount of time.
	/// </summary>
	public event SandboxObjMouseClickHoldEvtH MouseClickHold;
	
	/// <summary>
	/// Event triggered when mouse clicks the sandbox object
	/// and drags it.
	/// </summary>
	public event SandboxObjMouseClickDragEvtH MouseClickDrag;
	
	/// <summary>
	/// Event triggered when mouse clicks the dragged sandbox object
	/// to drop it.
	/// </summary>
	public event SandboxObjMouseClickDropEvtH MouseClickDrop;
	
	[HideInInspector]
	public bool dragDropMode = false;
	
	private bool mouseDown = false;
	private float mouseDownTime = 0;

	protected virtual void Awake()
	{
	}
	
	protected virtual void Start()
	{
	}
	
	protected virtual void Update()
	{
		if( isDragDroppable && dragDropMode )
		{
			Camera cam = GameObject.FindGameObjectWithTag("MainCamera").camera;
			/*transform.position += ( cam.ScreenToWorldPoint(Input.mousePosition) -
			                       cam.ScreenToWorldPoint(cam.WorldToScreenPoint(transform.position)) );*/
		}
		else
		{
			if(mouseDown)
				mouseDownTime += Time.deltaTime;
			
			if( mouseDownTime >= mouseHoldTime )
			{
				// Mouse button held over the object long enough
				
				mouseDown = false;
				mouseDownTime = 0;
				
				if( MouseClickHold != null )
					MouseClickHold(this);
			}
		}
	}
	
	protected virtual void OnMouseEnter()
	{
		// Mouse is over the object
		
		if( MouseEnter != null )
			MouseEnter(this);
	}
	
	protected virtual void OnMouseExit()
	{
		// Mouse is no longer over the object
		
		if( MouseExit != null )
			MouseExit(this);
	}
	
	protected virtual void OnMouseUp()
	{
		// Mouse unclicked over the object
		
		if(mouseDown)
		{
			mouseDown = false;
			
			if(isDragDroppable)
			{
				if( !dragDropMode )
				{
					dragDropMode = true;
					
					if( MouseClickDrag != null )
					{
						MouseClickDrag(this);
					}
				}
				else
				{
					dragDropMode = false;
					
					if( MouseClickDrop != null )
					{
						MouseClickDrop(this);
					}
				}
			}
			else
			{
				if( MouseClick != null )
				{
					MouseClick(this);
				}
			}
		}
	}
	
	protected virtual void OnMouseDown()
	{
		// Mouse clicked over the object
		
		mouseDown = true;
		mouseDownTime = 0;
	}
}

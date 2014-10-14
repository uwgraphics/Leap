using UnityEngine;
using System;
using System.Collections;

/// <summary>
/// Add this script to an object to make it a photo mosaic.
/// </summary>
public class PhotoMosaic : MonoBehaviour
{
	[Serializable]
	public class KeyFrame
	{
		public string name;
		public Vector3 position;
		public Vector3 scale;
	}
	
	/// <summary>
	/// Key positions and scales of the mosaic.
	/// </summary>
	public KeyFrame[] keyFrames = new KeyFrame[0];
	
	/// <summary>
	/// Keyframe to which the mosaic should be moved.
	/// </summary>
	public string moveToKeyFrame = "";
	
	/// <summary>
	/// Move time.
	/// </summary>
	public float moveTime = 0.6f;
	
	/// <summary>
	/// If true, the mosaic will be moved to the specified keyframe.
	/// </summary>
	public bool doMoveTo = false;
	
	protected bool moving = false;
	protected Vector3 srcPos;
	protected Vector3 srcScal;
	protected KeyFrame trgKf;
	protected float time = 0f;
	protected float length = 0f;
	
	/// <summary>
	/// Indexer for getting a photo object.
	/// </summary>
	/// <param name="photoName">
	/// Photo name.
	/// </param>
	public virtual GameObject this[string photoName]
	{
		get
		{
			GameObject photo = null;
			for( int chi = 0; chi < transform.GetChildCount(); ++chi )
			{
				photo = transform.GetChild(chi).gameObject;
				if( photo.name == photoName )
					return photo;
			}
			
			return null;
		}
	}
	
	/// <summary>
	/// Moves the mosaic to the specified keyframe.
	/// </summary>
	/// <param name="kfName">
	/// Keyframe name.
	/// </param>
	/// <param name="time">
	/// Move time.
	/// </param>
	public virtual void MoveTo( string kfName, float time )
	{
		doMoveTo = true;
		moveToKeyFrame = kfName;
		moveTime = time;
	}
	
	public KeyFrame FindKeyFrame( string kfName )
	{
		foreach( KeyFrame kf in keyFrames )
			if( kf.name == kfName )
				return kf;
		
		return null;
	}

	protected virtual void Awake()
	{
	}

	protected virtual void Start()
	{
	}
	
	protected virtual void Update()
	{
	}
	
	protected virtual void LateUpdate()
	{
		if(moving)
		{
			if( time > moveTime )
			{
				moving = false;
				return;
			}
			
			float t = moveTime >= 0.00001f ? time/moveTime : 1f;
			float t2 = t*t;
			float pt = -2*t2*t+3*t2;
			Vector3 pos = srcPos + ( trgKf.position - srcPos )*pt;
			Vector3 scal = srcScal + ( trgKf.scale - srcScal )*pt;
			transform.localPosition = pos;
			transform.localScale = scal;
			
			time += Time.deltaTime;
		}
		else if(doMoveTo)
		{
			trgKf = FindKeyFrame(moveToKeyFrame);
			if( trgKf == null )
				return;
			srcPos = transform.localPosition;
			srcScal = transform.localScale;
			time = 0;
			length = moveTime;
			moving = true;
			doMoveTo = false;
		}
	}
}

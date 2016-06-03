using UnityEngine;
using System.Collections;

/// <summary>
/// Add this script to an object to make it a photo panel.
/// </summary>
public class PhotoPanel : MonoBehaviour
{
	public Texture[] photos = new Texture[0];
	public int photoIndex = 0;
	
	protected int curPhotoIndex = -1;
	protected float initSx = 1;
	protected float initSz = 1;
	
	/// <summary>
	/// Sets the photo to be displayed on a photo panel.
	/// </summary>
	/// <param name="panelName">
	/// Photo panel name.
	/// </param>
	/// <param name="photoName">
	/// Photo texture name.
	/// </param>
	public virtual void ShowPhoto( string photoName )
	{
		for( int phi = 0; phi < photos.Length; ++phi )
		{
			if( photos[phi].name == photoName )
			{
				photoIndex = phi;
				break;
			}
		}
	}
		
	protected virtual void Awake()
	{
		initSx = transform.localScale.x;
		initSz = transform.localScale.z;
	}

	protected virtual void Start()
	{
	}
	
	protected virtual void Update()
	{
		// Apply new photo
		if( curPhotoIndex != photoIndex &&
		   photoIndex > 0 && photoIndex < photos.Length )
		{
			GetComponent<Renderer>().material.mainTexture = photos[photoIndex];
			curPhotoIndex = photoIndex;
		
			// Scale the panel correctly
			float new_ratio = (float)photos[photoIndex].width/photos[photoIndex].height;
			float init_ratio = initSx/initSz;
			Vector3 scale = transform.localScale;
			scale.x = initSx;
			scale.z = initSx/new_ratio;
			transform.localScale = scale;
		}
	}
	
	protected virtual void LateUpdate()
	{
	}
}

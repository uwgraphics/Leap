
using UnityEngine;

/// <summary>
/// This camera focuses on a single (moving) object and rotates around it
/// </summary>
[AddComponentMenu("Camera-Control/Rotate Follow")]
public class RotateFollowCamera : MonoBehaviour
{
	// TODO: How can we define input axes automatically (e.g. on package import),
	// so the user doesn't have to?

	/// <summary>
	/// The target we are following
	/// </summary>
	public Transform target = null;
	
	/// <summary>
	/// The distance in the x-z plane to the target
	/// </summary>
	public float distance = 0.7f;
	
	/// <summary>
	/// The height we want the camera to be above the target
	/// <summary>
	public float height = 0.5f;
	
	/// <summary>
	/// How much the camera is initially rotated about the vertical axis (in degrees)
	/// </summary>
	public float rotationOffset = 45f;
	
	void Awake()
	{
	}
	
	void Start()
	{
		if (!target )
		{
			Debug.LogWarning( "Target for Rotate Follow camera not set.");
			return;
		}
	}
	
	void LateUpdate()
	{
		// Early out if we don't have a target
		if (!target )
			return;
		
		// Make sure appropriate input axes are defined
		try
		{
			Input.GetAxis("Horizontal");
			Input.GetAxis("Vertical");
			Input.GetAxis("Mouse ScrollWheel");
		}
		catch( UnityException )
		{
			Debug.LogWarning( "Input axes for Rotate Follow camera not defined." );
			return;
		}
		
		// Calculate the current rotation angles
		float currentRotationAngle = transform.eulerAngles.y;
		float currentHeight = transform.position.y;
		
		// Apply user input to camera rotation and distance
		currentRotationAngle = currentRotationAngle + 5f * Input.GetAxis( "Horizontal" );
		distance = distance - Input.GetAxis( "Mouse ScrollWheel" );
	
		// Convert the angle into a rotation
		Quaternion currentRotation = Quaternion.Euler (0, currentRotationAngle, 0);
		
		// Set the position of the camera on the x-z plane to:
		// distance meters behind the target
		transform.position = target.position;
		transform.position -= currentRotation * Vector3.forward * distance;
		
		// Apply user input to camera height
		currentHeight = currentHeight + 0.3f * Input.GetAxis( "Vertical" );
		if( currentHeight < 0 ) currentHeight = 0;
	
		// Set the height of the camera
		Vector3 cam_pos = transform.position;
		cam_pos.y = currentHeight;
		transform.position = cam_pos;
		
		// Always look at the target
		transform.LookAt (target);
	}
}

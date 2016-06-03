using UnityEngine;
using System.Collections;

// Animate the sound location by pulsing the scale
public class Pulse : MonoBehaviour 
{
	Vector3 scale = new Vector3(0,0,0);
	float pulse   = 1.0f;
	int state     = 0;
	float timeout = 0.0f;
	float bpm     = 130.0f;

	// Use this for initialization
	void Start () 
	{
		scale = transform.localScale;
	}
	
	// Update is called once per frame
	void Update () 
	{
		timeout -= Time.deltaTime;
		if(timeout < 0.0f)
		{
			state = 1;
			timeout += 60.0f / bpm;
			pulse = 1.5f;
		}

		if(state == 1)
		{
			pulse -= 0.05f;
			if(pulse < 1.0f)
			{
				pulse = 1.0f;
				state = 0;
			}
		}

		transform.localScale = scale * pulse;
	}
}

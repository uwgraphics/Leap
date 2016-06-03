using UnityEngine;
using System.Collections;

public class PlayTest : MonoBehaviour 
{
    public float timeToStart;
    private AudioSource audioSource = null;

    void Awake()
    {
        if (!audioSource) audioSource = GetComponent<AudioSource>();
    }

	// Use this for initialization
	void Start () 
    {
	}
	
	// Update is called once per frame
	void Update () 
    {
        if (!audioSource) return;
        if (audioSource.isPlaying) return;

        timeToStart -= Time.deltaTime;
     	// Comment here
		if(timeToStart < 0.0f)
        {
            timeToStart = 0.0f;

            if(!audioSource.isPlaying)
            {
                // we are calling Play on audioSource to see if it will be correctly
                // handled by OSPAudioSource
                audioSource.Play();
            }
        }
	}
}

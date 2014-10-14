using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// A class for connecting between "dummy" morph channel bones 
/// and the actual morph channel weights.
/// </summary>
[RequireComponent (typeof(MorphController))]
public class MorphAnimationLinker : MonoBehaviour 
{	
	private class MorphChannelLink
    {
        public Transform sourceBone;
        public MorphChannel channel;
    }
	
	private List<MorphChannelLink> mcLinks = null;
    private MorphController morphCtrl = null;
	private ModelController mdlCtrl = null;
	
	void Start()
	{
        morphCtrl = gameObject.GetComponent<MorphController>();
        if (morphCtrl == null)
        {
            Debug.LogWarning( "No morph animation on character " + gameObject.name );
            return;
        }
		mdlCtrl = gameObject.GetComponent<ModelController>();
        
		_InitializeLinks();
	}
	
	void Update()
	{
        if (morphCtrl == null)
            return;
		
		// Apply new morph weights
        foreach( MorphChannelLink mclink in mcLinks )
        {
			float weight = mclink.sourceBone.localPosition.y;
			if( weight > 0.001f )
			{
				mclink.channel.weight += weight;
			}
			
			// Reset bone
			mclink.sourceBone.localPosition = mdlCtrl.GetInitPosition(mclink.sourceBone);
        }
	}

    void _InitializeLinks()
    {
		// Initialize the links between corresponding "dummy" bones and morph channels
		
        mcLinks = new List<MorphChannelLink>();
        Transform[] childTransforms = gameObject.GetComponentsInChildren<Transform>();
        foreach (Transform transform in childTransforms)
        {
            if( !transform.name.StartsWith( LEAPCore.morphAnimationPrefix + "&" ) )
                continue;
			
            string mcName = transform.name.Substring( ( LEAPCore.morphAnimationPrefix + "&" ).Length );
			MorphChannel mc = morphCtrl.GetMorphChannel(mcName);
			
			if( mc == null )
			{
				Debug.LogWarning( "Unable to link morph channel " + mcName );
				
				continue;
			}
			
			MorphChannelLink mclink = new MorphChannelLink();
			mclink.sourceBone = transform;
			mclink.channel = mc;
			mcLinks.Add(mclink);
        }
    }
}

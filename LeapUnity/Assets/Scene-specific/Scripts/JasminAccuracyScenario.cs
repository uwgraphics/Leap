using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class JasminAccuracyScenario : TestScenario
{
	public string customTarget = "";
	public bool randomTarget = false;
	public bool lightUpTarget = false;
	public Texture[] gtLitTextures = new Texture[14];
	
	protected Texture[] gtUnlitTextures = new Texture[14];
	
	/// <see cref="Scenario._Run()"/>
	protected override IEnumerator _Run()
	{	
		yield return StartCoroutine(base._Run());
		
		GazeController gctrl = agents[agentName].GetComponent<GazeController>();
		ExpressionController exprCtrl = agents[agentName].GetComponent<ExpressionController>();
		FaceController faceCtrl = agents[agentName].GetComponent<FaceController>();
		int curexpr = -1;
		int curgaze = -1;
		int curnod = -1;
		
		yield return new WaitForSeconds(1.5f);
		
		// Nod
		curnod = HeadNod(agentName,1,FaceGestureSpeed.Normal,5f,0f);
		yield return StartCoroutine( WaitUntilFinished(curnod) );
		
		// Choose gaze target
		if(randomTarget)
			customTarget = gazeTargets[ "GT" + Random.Range(1,gazeTargets.Count) ].name;
		Debug.Log( "Looking at " + customTarget );
		
		// Gaze at target
		curgaze = GazeAt(agentName,customTarget,0f,0f,0f,0f,eyeAlign);
		yield return StartCoroutine( WaitUntilFinished(curgaze) );
		if( lightUpTarget )
			_SwitchLitTexture(customTarget);
				
		yield return new WaitForSeconds(2f);
		
		// Gaze back at camera
		curgaze = GazeAtCamera(agentName,1f,0f);
		yield return StartCoroutine( WaitUntilFinished(curgaze) );
		if(lightUpTarget)
			_SwitchUnlitTexture(customTarget);
				
		// Smile
		exprCtrl.UnfixExpression("ExpressionSmileClosed");
		yield return new WaitForSeconds(0f);
		curexpr = ChangeExpression(agentName,"ExpressionSmileOpen",0.8f,0.4f);
		yield return StartCoroutine( WaitUntilFinished(curexpr) );
		
		yield return new WaitForSeconds(5f);
		
		HideScene();
		
		/*// Gaze targets
		List<int> gts = new List<int>();
		for( int gti = 0; gti < 9; ++gti )
			gts.Add(gti+1);
		List<string> chosen_gts = new List<string>();
		
		yield return new WaitForSeconds(1.5f);
		
		// Nod
		curnod = HeadNod(agentName,1,FaceGestureSpeed.Normal,5f,0f);
		yield return StartCoroutine( WaitUntilFinished(curnod) );
		
		//for( int i = 0; i < 20; ++i )
		for( int i = 0; i < 9; ++i )
		{
			string gtname = "";
			if( i == 0 )
				gtname = "GT1";
			else if( i == 1 )
				gtname = "GT6";
			else
			{
				// Choose the next gaze target
				int gtii = Random.Range(0,gts.Count-1);
				int gti = gts[gtii];
				gtname = "GT" + gti;
			}
			//gtname = "GT" + (i+1);
			chosen_gts.Add(gtname);
			
			yield return new WaitForSeconds(0.5f);
			
			//for( int j = 0; j < 2; ++j )
			{
				// Gaze at target
				curgaze = GazeAt(agentName,gtname,0f,0f,0f,0f,eyeAlign);
				yield return StartCoroutine( WaitUntilFinished(curgaze) );
				if( i < 2 )
					_SwitchLitTexture(gtname);
				
				yield return new WaitForSeconds(2f);
				
				// Gaze back at camera
				curgaze = GazeAtCamera(agentName,1f,0f);
				yield return StartCoroutine( WaitUntilFinished(curgaze) );
				if( i < 2 )
					_SwitchUnlitTexture(gtname);
				
				yield return new WaitForSeconds(2f);
			}
			yield return new WaitForSeconds(3f);
		}
		
		string report = "Jasmin has gazed at the following targets: ";
		foreach( string cgtname in chosen_gts )
			report += ( cgtname + " " );
		Debug.Log(report);
		
		// Smile
		exprCtrl.UnfixExpression("ExpressionSmileClosed");
		yield return new WaitForSeconds(0f);
		curexpr = ChangeExpression(agentName,"ExpressionSmileOpen",0.8f,0.4f);
		yield return StartCoroutine( WaitUntilFinished(curexpr) );
		
		yield return new WaitForSeconds(1.5f);*/
	}
	
	protected virtual void _SwitchLitTexture( string gtName )
	{
		string texname = gtName + "Lit";
		for( int texi = 0; texi < gtLitTextures.Length; ++texi )
		{
			Texture tex = gtLitTextures[texi];
			if( tex.name == texname )
			{
				gtUnlitTextures[texi] = gazeTargets[gtName].renderer.material.mainTexture;
				gazeTargets[gtName].renderer.material.mainTexture = tex;
			}
		}
	}
	
	protected virtual void _SwitchUnlitTexture( string gtName )
	{
		string texname = gtName + "Lit";
		for( int texi = 0; texi < gtLitTextures.Length; ++texi )
		{
			Texture tex = gtLitTextures[texi];
			if( tex.name == texname )
				gazeTargets[gtName].renderer.material.mainTexture = gtUnlitTextures[texi];
		}
	}
}

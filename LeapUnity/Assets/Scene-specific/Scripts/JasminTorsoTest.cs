using UnityEngine;
using System.Collections;

public class JasminTorsoTest : TestScenario
{
	/// <see cref="Scenario._Run()"/>
	protected override IEnumerator _Run()
	{
		yield return StartCoroutine(base._Run());
		ChangeExpression(agentName,"ExpressionSmileClosed",0.6f,0f);
		
		yield return new WaitForSeconds(1f);
		//yield return new WaitForSeconds(0.3f);
			
		int curgaze = GazeAt(agentName,"GTTestL",0.3f,0f,0.3f,0f,1f);
		yield return StartCoroutine( WaitUntilFinished(curgaze) );
		yield return new WaitForSeconds(0.5f);
		
		curgaze = GazeAt(agentName,"GTTestUR",0f,0f,0f,0f,1f);
		yield return StartCoroutine( WaitUntilFinished(curgaze) );
		yield return new WaitForSeconds(0.5f);
		
		curgaze = GazeAt(agentName,"GTTestLR",0.5f,0f,0.5f,0f,1f);
		yield return StartCoroutine( WaitUntilFinished(curgaze) );
		yield return new WaitForSeconds(0.5f);
		
		curgaze = GazeAt(agentName,"GTTestLL",0.3f,0f,0.3f,0f,1f);
		yield return StartCoroutine( WaitUntilFinished(curgaze) );
		yield return new WaitForSeconds(0.5f);
		
		curgaze = GazeAtCamera(agentName,0f,0f,0.2f,0f,1f);
		yield return StartCoroutine( WaitUntilFinished(curgaze) );
		yield return new WaitForSeconds(0.5f);
		
		curgaze = GazeAt(agentName,"GTTestUL",0f,0f,0.2f,0f,1f);
		yield return StartCoroutine( WaitUntilFinished(curgaze) );
		yield return new WaitForSeconds(0.5f);
		
		curgaze = GazeAtCamera(agentName,1f,0f,1f,0f,1f);
		yield return StartCoroutine( WaitUntilFinished(curgaze) );
		yield return new WaitForSeconds(0.5f);
	}
}

using UnityEngine;
using System.Collections;

public class JasminTestScenario : TestScenario
{
	/// <see cref="Scenario._Run()"/>
	protected override IEnumerator _Run()
	{
		yield return StartCoroutine(base._Run());
		
		HideScene();
		yield return new WaitForSeconds(1f);
		int gaze_id = GazeAtCamera(agentName,1f,0f,1f,0f,1f);
		yield return StartCoroutine( WaitUntilFinished(gaze_id) );
		ShowScene();
		yield return new WaitForSeconds(1f);
		
		gaze_id = GazeAt(agentName,"GTTest",0f,0f,0f,0f,1f);
		yield return StartCoroutine( WaitUntilFinished(gaze_id) );
		
		/*ChangeExpression(agentName, "ExpressionSmileClosed",0.6f,0f);
		yield return new WaitForSeconds(2f);
		
		// Glance sideways and back
		int curgaze = GazeAt(agentName,"GTTestR",0f,0f,0f,0f,0.17f);
		yield return StartCoroutine( WaitUntilFinished(curgaze) );
		yield return new WaitForSeconds(0.4f);
		curgaze = GazeAtCamera(agentName,0f,0f,0f,0f,1f);
		yield return new WaitForSeconds(2f);
		
		// Glance sideways and back
		curgaze = GazeAt(agentName,"GTTestL",0f,0f,0f,0f,0.17f);
		yield return StartCoroutine( WaitUntilFinished(curgaze) );
		yield return new WaitForSeconds(0.4f);
		curgaze = GazeAtCamera(agentName,0f,0f,0f,0f,1f);
		yield return new WaitForSeconds(2f);
		
		ChangeExpression(agentName, "ExpressionSmileClosed",0f,0.3f);
		yield return new WaitForSeconds(2f);
			
		// Gaze at target
		curgaze = GazeAt(agentName,"GTTest",0f,0f,0f,0f,1f);
		ChangeExpression(agentName, "ExpressionSad",0.7f,0.7f);
		PlayAnimation(agentName,"FloatDown");
		yield return StartCoroutine( WaitUntilFinished(curgaze) );
		yield return new WaitForSeconds(3f);
		
		// Look up!
		curgaze = GazeAtCamera(agentName,1f,0f,0f,0f,1f);
		ChangeExpression(agentName, "ExpressionSmileClosed",1f,0.5f);
		PlayAnimation(agentName,"FloatUp");
		yield return StartCoroutine( WaitUntilFinished(curgaze) );
		yield return new WaitForSeconds(3f);*/
	}
}

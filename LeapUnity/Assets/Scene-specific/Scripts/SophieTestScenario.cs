using UnityEngine;
using System.Collections;

public class SophieTestScenario : StoryScenario
{
	/// <see cref="Scenario._Run()"/>
	protected override IEnumerator _Run()
	{
		ExpressionController exprCtrl = agents[agentName].GetComponent<ExpressionController>();
		BlinkController blinkCtrl = agents[agentName].GetComponent<BlinkController>();
		int curspeak = -1;
		int curgaze = -1;
		int curexpr = -1;
		float pitch = agents[agentName].audio.pitch;
		
		yield return StartCoroutine(base._Run());
		
		curexpr = ChangeExpression(agentName,"ExpressionSmileClosed",1f,0f);
		yield return StartCoroutine( WaitUntilFinished(curexpr) );
		exprCtrl.FixExpression();
		
		//phMosaic.gameObject.SetActiveRecursively(false);
		//Time.timeScale = 0.2f;
		
		yield return new WaitForSeconds(Time.timeScale);
		
		// PP -> PPC
		SetGazePathArcs(agentName,0f,0f,0f);
		curgaze = GazeAt(agentName,"GTTestL",1f,0f,0f,0f,0.4f);
		//curgaze = GazeAtCamera(agentName,0f,0f,0f,0f,1f);
		//blinkCtrl.Blink(0.65f,0.8f,1f);
		yield return StartCoroutine( WaitUntilFinished(curgaze) );
		yield return new WaitForSeconds(Time.timeScale);
	}
}

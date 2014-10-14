using UnityEngine;
using System.Collections;

public class Test2Scenario : TestScenario
{

	protected override void _Init()
	{
		Debug.Log( "This is Test2Scenario" );
	}
	
	protected override IEnumerator _Run()
	{
		Debug.Log( "Starting Test2Scenario..." );
		
		yield return new WaitForSeconds(2f);
		
		Debug.Log( "Test2Scenario executing..." );
		
		yield return new WaitForSeconds(2f);
	}
	
	protected override void _Finish()
	{
		Debug.Log( "Test2Scenario done!" );
	}
	
}

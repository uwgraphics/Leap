
function OnGUI()
{
	// Create scenario selection box
	GUI.Box( Rect( 10, 10, 130, 170 ), "Select a Scenario:" );
	
	// Create scenario load buttons:
	
	if( GUI.Button( Rect( 20, 40, 110, 20 ), "Alex Lecture" ) )
	{
		Application.LoadLevel("AlexScene");
	}
	
	if( GUI.Button( Rect( 20, 70, 110, 20 ), "Chris Lecture" ) )
	{
		Application.LoadLevel("ChrisScene");
	}
	
	if( GUI.Button( Rect( 20, 100, 110, 20 ), "Erin Lecture" ) )
	{
		Application.LoadLevel("ErinScene");
	}
	
	if( GUI.Button( Rect( 20, 130, 110, 20 ), "Pat Lecture" ) )
	{
		Application.LoadLevel("PatScene");
	}
}

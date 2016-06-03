function OnGUI()
{
	if( GameObject.FindWithTag("LectureInfo") == null )
	{
		return;
	}
		
	var lectinf = GameObject.FindWithTag("LectureInfo").GetComponent(LectureInfo);
	
	if( lectinf == null )
	{
		return;
	}
	
	if (lectinf.IsLectureLoading()) {
		GUI.Label( Rect( Screen.width/2 - 120, Screen.height/2, 240, 60 ), "Connecting to the Next Lecture..." );
	}

	if (lectinf.curLectureIndex == -1) {
		if( lectinf.IsLectureFinished() && GUI.Button( Rect( 5/*Screen.width/2 - 120*/, 5/*Screen.height/2*/, 240, 60 ), "Begin the First Lecture" ) )
		{
			lectinf.LoadNextLecture();
		}
	
	}
	else if (lectinf.curLectureIndex == (lectinf.lectures.Length - 1) ) {
		if ( lectinf.IsLectureFinished() ) {
			GameObject.FindGameObjectWithTag("MainLight").GetComponent.<Light>().intensity = 0f;
			if ( GUI.Button( Rect( Screen.width/2 - 120, Screen.height/2, 240, 60 ), "Thank You!" ) ) {
				Application.Quit();
			}
		}
	}
	else {
		if( lectinf.IsLectureFinished() && GUI.Button( Rect( 5 /*Screen.width/2 - 120*/, 5 /*Screen.height/2*/, 240, 60 ), "Begin Next Lecture" ) )
		{
			lectinf.LoadNextLecture();
		}
	}
}

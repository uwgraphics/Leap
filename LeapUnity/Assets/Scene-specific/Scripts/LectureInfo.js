
var lectures : String[] = [ "ChrisLectureScenario", "AlexLectureScenario", "ErinLectureScenario", "PatLectureScenario" ];

public var curLectureIndex : int = -1;

function Awake()
{
	DontDestroyOnLoad(this);
	
	lectures = GenerateLectureOrder(lectures);
	
	Screen.SetResolution(1280, 800, true);
}

function Update () {
    if (Input.GetKey ("escape")) {
        Application.Quit();
    }
}

function IsLectureFinished() : boolean
{
	var cam = GameObject.FindWithTag("MainCamera");
	
	if( curLectureIndex < 0 ||
	curLectureIndex >= lectures.length || 
	cam.GetComponent( lectures[curLectureIndex] ) != null &&
	cam.GetComponent( lectures[curLectureIndex] ).finished )
	{
		return true;
	}
	
	return false;
}

function IsLectureLoading() : boolean
{
	var cam = GameObject.FindWithTag("MainCamera");

	if (curLectureIndex < 0 || curLectureIndex >= lectures.length ) {
		return false;
	}

	if( cam.GetComponent( lectures[curLectureIndex] ) != null && cam.GetComponent( lectures[curLectureIndex] ).loading )
	{
		return true;
	}
	
	return false;
}

function LoadNextLecture()
{
	curLectureIndex += 1;
	
	if( curLectureIndex >= lectures.Length )
		Application.Quit();
	
	Application.LoadLevel( lectures[curLectureIndex] );
}

function GenerateLectureOrder( lectureList : String[] ) : String[]
{
	var values = new Array(lectureList);
	var out_values = new Array();
	
	while( values.length > 0 )
	{
		var lect_i = Random.Range( 0, values.length - 1 );
		out_values.Add( values[lect_i] );
		values.RemoveAt(lect_i);
	}
	
	return out_values.ToBuiltin(String);
}

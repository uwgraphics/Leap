using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class TorsoExperiment1 : Scenario
{
	public enum ExperimentPhase
	{
		Init,
		ReadInstructions,
		WatchGazeShifts,
		InputResponses,
		ResponsesSubmitted,
		ThankYou
	};
	
	public enum GazeShiftType
	{
		EyeHead,
		UpperBody0,
		UpperBody1
	};
	
	public Texture2D[] images = new Texture2D[0];
	public Texture2D guiBox = null;
	public Texture2D guiButtonNormal = null;
	public Texture2D guiButtonHover = null;
	public Texture2D guiButtonOnNormal = null;
	public Texture2D guiButtonOnHover = null;
	public Texture2D guiButtonActive = null;
	
	protected List<Texture2D> imagePool = new List<Texture2D>();
	protected ExperimentPhase expPhase = ExperimentPhase.Init;
	protected ushort subjectId = 0;
	protected bool[,] subjectResponses = new bool[4,7];
	protected StreamWriter expLog = null;
	
	/// <summary>
	/// Hide the scene by deactivating lights and unlit objects.
	/// </summary>
	public virtual void HideScene()
	{
		SetObjectActive("SunUpLeft",false);
		SetObjectActive("SunLeft",false);
		SetObjectActive("SunDownRight",false);
		SetObjectActive("BgPanel",false);
		SetObjectActive("GTPaintingLL",false);
		SetObjectActive("GTPaintingLR",false);
		SetObjectActive("GTPaintingUL",false);
		SetObjectActive("GTPaintingUR",false);
	}
	
	/// <summary>
	/// Show the scene by activating lights and unlit objects. 
	/// </summary>
	public virtual void ShowScene()
	{
		SetObjectActive("SunUpLeft",true);
		SetObjectActive("SunLeft",true);
		SetObjectActive("SunDownRight",true);
		SetObjectActive("BgPanel",true);
		SetObjectActive("GTPaintingLL",true);
		SetObjectActive("GTPaintingLR",true);
		SetObjectActive("GTPaintingUL",true);
		SetObjectActive("GTPaintingUR",true);
	}
	
	/// <summary>
	/// Sets the image on plane, such that image proportions and
	/// plane scale and position are preserved.
	/// </summary>
	/// <param name='img'>
	/// Image.
	/// </param>
	/// <param name='plane'>
	/// Target plane.
	/// </param>
	public virtual void SetImageOnPlane( Texture2D img, GameObject plane )
	{
		plane.renderer.material.mainTexture = img;
		Vector3 scal = plane.transform.localScale;
		scal.x = scal.z = scal.y;
		if( img.width > img.height )
			scal.z /= ((float)img.width)/img.height;
		else
			scal.x /= ((float)img.height)/img.width;
		plane.transform.localScale = scal;
	}
	
	/// <summary>
	/// Have an agent gaze at something.
	/// </summary>
	/// <param name='agentName'>
	/// Agent name.
	/// </param>
	/// <param name='targetName'>
	/// Gaze target name.
	/// </param>
	/// <param name='gsType'>
	/// Gaze shift type.
	/// </param>
	/// <returns>Action ID</returns>
	public virtual int GazeAt( string agentName, string targetName, GazeShiftType gsType )
	{
		int action_id = -1;
		GazeController gctrl = agents[agentName].GetComponent<GazeController>();
		gctrl.enableAutoTorso = true;
		switch(gsType)
		{
			
		case GazeShiftType.EyeHead:
			gctrl.enableAutoTorso = false;
			action_id = GazeAt(agentName,targetName,0.3f,0f,0f,0f,1f );
			break;
			
		case GazeShiftType.UpperBody0:
			action_id = GazeAt(agentName,targetName,0.3f,0f,0f,0f,1f );
			break;
			
		case GazeShiftType.UpperBody1:
			action_id = GazeAt(agentName,targetName,0.3f,0f,0.3f,0f,1f );
			break;
		
		default:
			break;
		}
		
		return action_id;
	}
	
	/// <summary>
	/// Scenario execution will pause until the experiment
	/// has entered the specified phase.
	/// </summary>
	/// <param name="phase">Target phase.</param>
	protected virtual IEnumerator WaitForPhase( ExperimentPhase phase )
	{
		while( expPhase != phase )
			yield return 0;
	}
	
	/// <see cref="Scenario._Init()"/>
	protected override void _Init()
	{
		subjectId = (ushort)Random.Range(1, ushort.MaxValue);
		if( !File.Exists("TorsoGaze_Log.csv") )
		{
			expLog = new StreamWriter("TorsoGaze_Log.csv", false);
			expLog.WriteLine("SubjectID,condition,image,interested");
		}
		else
			expLog = new StreamWriter("TorsoGaze_Log.csv", true);
	}
	
	/// <see cref="Scenario._Run()"/>
	protected override IEnumerator _Run()
	{
		// Temporarily hide scene
		HideScene();
		// Smile!
		int curexpr = -1;
		int curgaze = -1;
		ExpressionController exprCtrl = agents["Jasmin"].GetComponent<ExpressionController>();
		curexpr = ChangeExpression("Jasmin","ExpressionSmileClosed",0.5f,0f);
		yield return StartCoroutine( WaitUntilFinished(curexpr) );
		exprCtrl.FixExpression();
		// Initialize gaze
		yield return new WaitForSeconds(0.1f);
		curgaze = GazeAtCamera("Jasmin",1f,0f,1f,0f,1f);
		yield return StartCoroutine( WaitUntilFinished(curgaze) );
		// Show scene again
		ShowScene();
		yield return new WaitForSeconds(0.3f);
		
		// Create a pool of images
		foreach( Texture2D img in images )
			imagePool.Add(img);
		
		// Show instructions first
		expPhase = ExperimentPhase.ReadInstructions;
		yield return StartCoroutine( WaitForPhase(ExperimentPhase.WatchGazeShifts) );
		
		for( int tri = 0; tri < 8; ++tri )
		{
			expPhase = ExperimentPhase.WatchGazeShifts;
			
			// Draw images from pool
			List<Texture2D> sel_images = new List<Texture2D>();
			for( int i = 0; i < 4; ++i )
			{
				int img_i = Random.Range(0, imagePool.Count);
				sel_images.Add( imagePool[img_i] );
				imagePool.RemoveAt(img_i);
			}
			
			// Place images on planes
			SetImageOnPlane( sel_images[0], gazeTargets["GTPaintingLL"] );
			SetImageOnPlane( sel_images[1], gazeTargets["GTPaintingLR"] );
			SetImageOnPlane( sel_images[2], gazeTargets["GTPaintingUL"] );
			SetImageOnPlane( sel_images[3], gazeTargets["GTPaintingUR"] );
			// Remember which planes images are placed on
			Dictionary<Texture2D, int> plane_map = new Dictionary<Texture2D, int>();
			plane_map.Add(sel_images[0], 0);
			plane_map.Add(sel_images[1], 1);
			plane_map.Add(sel_images[2], 2);
			plane_map.Add(sel_images[3], 3);
			
			// Gaze shift pool
			List<GazeShiftType> gaze_shifts = new List<GazeShiftType>();
			gaze_shifts.Add(GazeShiftType.EyeHead);
			gaze_shifts.Add(GazeShiftType.UpperBody0);
			gaze_shifts.Add(GazeShiftType.UpperBody1);
			int gs_i = -1;
			// Image plane pool
			List<GameObject> planes = new List<GameObject>();
			planes.Add(gazeTargets["GTPaintingLL"]);
			planes.Add(gazeTargets["GTPaintingLR"]);
			planes.Add(gazeTargets["GTPaintingUL"]);
			planes.Add(gazeTargets["GTPaintingUR"]);
			int plane_i = -1;
			GameObject plane = null;
			
			Dictionary<int, string> cond_map =
				new Dictionary<int, string>();
			Dictionary<int, string> img_map =
				new Dictionary<int, string>();
			while( gaze_shifts.Count > 0 )
			{
				// Randomly choose gaze shift
				gs_i = Random.Range(0, gaze_shifts.Count);
				GazeShiftType gs = gaze_shifts[gs_i];
				gaze_shifts.RemoveAt(gs_i);
				
				// Randomly choose target image
				plane_i = Random.Range(0, planes.Count);
				plane = planes[plane_i];
				planes.RemoveAt(plane_i);
				
				// Do gaze shift
				yield return new WaitForSeconds(0.8f);
				curgaze = GazeAt("Jasmin",plane.name,gs);
				yield return StartCoroutine( WaitUntilFinished(curgaze) );
				yield return new WaitForSeconds(0.8f);
				curgaze = GazeAtCamera("Jasmin",1f,0f,gs==GazeShiftType.EyeHead?0f:1f,75,1f);
				yield return StartCoroutine( WaitUntilFinished(curgaze) );
				
				// Store info needed for response logging
				string cond = "";
				if( gs == GazeShiftType.EyeHead )
					cond = "1-EyeHead";
				else if( gs == GazeShiftType.UpperBody0 )
					cond = "2-UpperBody0";
				else if( gs == GazeShiftType.UpperBody1 )
					cond = "3-UpperBody1";
				Texture2D img = (Texture2D)plane.renderer.material.mainTexture;
				int p0_i = plane_map[img];
				cond_map.Add(p0_i, cond);
				img_map.Add(p0_i, img.name);
			}
			
			// Store info for no gaze
			Texture2D ng_img = (Texture2D)planes[0].renderer.material.mainTexture;
			int p1_i = plane_map[ng_img];
			cond_map.Add(p1_i, "0-NoGaze");
			img_map.Add(p1_i, ng_img.name);
			
			// Wait for subject to rate the gaze shifts
			yield return new WaitForSeconds(0.8f);
			expPhase = ExperimentPhase.InputResponses;
			yield return StartCoroutine( WaitForPhase(ExperimentPhase.ResponsesSubmitted) );
			
			// Log subject's responses
			for( int ri = 0; ri < 4; ++ri )
				expLog.WriteLine( string.Format( "{0},{1},{2},{3}",
					subjectId, cond_map[ri], img_map[ri],
					_GetSubjectResponseValue(ri) ) );
			expLog.Flush();
			_ResetSubjectResponses();
		}
		
		expPhase = ExperimentPhase.ThankYou;
		yield return new WaitForSeconds(30f);
		
		// Choose target object
		/*string gtname = "";
		if( Random.Range(0,2) == 0 )
			gtname = "GreenCylinder";
		else
			gtname = "RedCube";
		GameObject gt = gazeTargets[gtname];
		
		// Position target object
		Vector3 lpos = new Vector3( -0.01f, 1.04f, -0.5f );
		Vector3 rpos = new Vector3( -0.64f, 1.04f, -0.5f );
		if( Random.Range(0,2) == 0 )
			gt.transform.position = lpos;
		else
			gt.transform.position = rpos;
		
		// Position actual gaze target
		GameObject real_gt = gazeTargets["GTTest"];
		if( Random.Range(0,2) == 0 )
			real_gt.transform.position = lpos;
		else
			real_gt.transform.position = rpos;
		
		// Perform gaze shift
		GazeAt("Jasmin",real_gt.name,0f,0f,0f,75,1f);
		yield return new WaitForSeconds(1.4f);
		// Reveal object
		gt.renderer.enabled = true;
		yield return new WaitForSeconds(2f);*/
		
		// TODO: process and write out subject input
	}
	
	/// <see cref="Scenario._Finish()"/>
	protected override void _Finish()
	{
		expLog.Close();
	}
	
	protected virtual void OnGUI()
	{
		GUI.skin.box.normal.background = guiBox;
		GUI.skin.box.fontSize = 20;
		GUI.skin.button.fontSize = 28;
		GUI.skin.button.normal.background = guiButtonNormal;
		GUI.skin.button.hover.background = guiButtonHover;
		GUI.skin.button.onNormal.background = guiButtonOnNormal;
		GUI.skin.button.onHover.background = guiButtonOnHover;
		GUI.skin.button.active.background = guiButtonActive;
		GUI.skin.button.onActive.background = guiButtonActive;
		GUI.skin.button.focused.background = guiButtonNormal;
		GUI.skin.button.onFocused.background = guiButtonOnNormal;
		GUI.skin.label.fontSize = 20;
		GUI.skin.label.wordWrap = true;
		GUI.skin.toggle.fontSize = 20;
		
		if( expPhase == ExperimentPhase.ReadInstructions )
		{
			GUI.skin.box.fontSize = 14;
			GUI.skin.box.wordWrap = true;
			GUI.skin.box.alignment = TextAnchor.UpperLeft;
			GUI.Box( new Rect(Screen.width-180, 20, 160, 24),
				"Participant ID: " + subjectId );
			GUI.skin.box.fontSize = 20;
			GUI.Box( new Rect(Screen.width/2-288, 60, 576, 480),
				"In this experiment you will watch a virtual agent (Jasmin) as " +
				"she examines some impressionist paintings. Jasmin will examine " +
				"four paintings at a time, and will show varying degrees of interest in " +
				"each painting - some will pique her interest, while she may ignore others " +
				"altogether.\n\n" +
				"Your task is to watch Jasmin as she examines each set of four paintings, " +
				"and then gauge her interest in each painting. After Jasmin has finished " +
				"looking at a set of paintings, you will need to rate her interest in each " +
				"painting on a scale of 1 to 7, and click DONE.\n\n" +
				"Jasmin will examine eight sets of paintings over the course of this experiment. " +
				"Once the experiment has finished, you will be instructed to " +
				"notify the experimenter.\n\n" +
				"If you have questions at any time during the experiment, please notify " +
				"the experimenter.\n\n" +
				"The experiment will begin when you click READY." );
			GUI.skin.box.wordWrap = false;
			GUI.skin.box.alignment = TextAnchor.MiddleCenter;
			
			if( GUI.Button( new Rect(Screen.width/2-64, 545, 128, 64), "READY" ) )
				expPhase = ExperimentPhase.WatchGazeShifts;
		}
		else if( expPhase == ExperimentPhase.WatchGazeShifts )
		{
			// TODO: display nothing?
		}
		else if( expPhase == ExperimentPhase.InputResponses )
		{
			GUI.Box( new Rect(Screen.width/2-320f, 10f, 640f, 36f),
				"On the scale of 1 to 7, how interested was Jasmin in each painting?" );
			
			// Lower-left reponse
			GUI.Box( new Rect(10f, Screen.height-60f, 570, 36f), "" );
			GUI.Label( new Rect(20f, Screen.height-57f, 120f, 36f), "Uninterested" );
			_SetSubjectResponse(0, 0, GUI.Toggle( new Rect(140f +10f, Screen.height-60f +5f, 40f, 30f),
				subjectResponses[0,0], "1" ) );
			_SetSubjectResponse(0, 1, GUI.Toggle( new Rect(140f +50f, Screen.height-60f +5f, 40f, 30f),
				subjectResponses[0,1], "2" ) );
			_SetSubjectResponse(0, 2, GUI.Toggle( new Rect(140f +90f, Screen.height-60f +5f, 40f, 30f),
				subjectResponses[0,2], "3" ) );
			_SetSubjectResponse(0, 3, GUI.Toggle( new Rect(140f +130f, Screen.height-60f +5f, 40f, 30f),
				subjectResponses[0,3], "4" ) );
			_SetSubjectResponse(0, 4, GUI.Toggle( new Rect(140f +170f, Screen.height-60f +5f, 40f, 30f),
				subjectResponses[0,4], "5" ) );
			_SetSubjectResponse(0, 5, GUI.Toggle( new Rect(140f +210f, Screen.height-60f +5f, 40f, 30f),
				subjectResponses[0,5], "6" ) );
			_SetSubjectResponse(0, 6, GUI.Toggle( new Rect(140f +250f, Screen.height-60f +5f, 40f, 30f),
				subjectResponses[0,6], "7" ) );
			GUI.Label( new Rect(435f, Screen.height-57f, 180f, 36f), "Very Interested" );
			
			// Lower-right response
			GUI.Box( new Rect(Screen.width-580, Screen.height-60f, 570, 36f), "" );
			GUI.Label( new Rect(Screen.width-580 +10, Screen.height-57, 120, 36), "Uninterested" );
			_SetSubjectResponse(1, 0, GUI.Toggle( new Rect(Screen.width-450 +10f, Screen.height-60f +5f, 40f, 30f),
				subjectResponses[1,0], "1" ) );
			_SetSubjectResponse(1, 1, GUI.Toggle( new Rect(Screen.width-450 +50f, Screen.height-60f +5f, 40f, 30f),
				subjectResponses[1,1], "2" ) );
			_SetSubjectResponse(1, 2, GUI.Toggle( new Rect(Screen.width-450 +90f, Screen.height-60f +5f, 40f, 30f),
				subjectResponses[1,2], "3" ) );
			_SetSubjectResponse(1, 3, GUI.Toggle( new Rect(Screen.width-450 +130f, Screen.height-60f +5f, 40f, 30f),
				subjectResponses[1,3], "4" ) );
			_SetSubjectResponse(1, 4, GUI.Toggle( new Rect(Screen.width-450 +170f, Screen.height-60f +5f, 40f, 30f),
				subjectResponses[1,4], "5" ) );
			_SetSubjectResponse(1, 5, GUI.Toggle( new Rect(Screen.width-450 +210f, Screen.height-60f +5f, 40f, 30f),
				subjectResponses[1,5], "6" ) );
			_SetSubjectResponse(1, 6, GUI.Toggle( new Rect(Screen.width-450 +250f, Screen.height-60f +5f, 40f, 30f),
				subjectResponses[1,6], "7" ) );
			GUI.Label( new Rect(Screen.width-580 +425, Screen.height-57, 180, 36), "Very Interested" );
			
			// Upper-left reponse
			GUI.Box( new Rect(10f, Screen.height/2f-60f, 570, 36f), "" );
			GUI.Label( new Rect(20f, Screen.height/2f-57f, 120f, 36f), "Uninterested" );
			_SetSubjectResponse(2, 0, GUI.Toggle( new Rect(140f +10f, Screen.height/2-60f +5f, 40f, 30f),
				subjectResponses[2,0], "1" ) );
			_SetSubjectResponse(2, 1, GUI.Toggle( new Rect(140f +50f, Screen.height/2-60f +5f, 40f, 30f),
				subjectResponses[2,1], "2" ) );
			_SetSubjectResponse(2, 2, GUI.Toggle( new Rect(140f +90f, Screen.height/2-60f +5f, 40f, 30f),
				subjectResponses[2,2], "3" ) );
			_SetSubjectResponse(2, 3, GUI.Toggle( new Rect(140f +130f, Screen.height/2-60f +5f, 40f, 30f),
				subjectResponses[2,3], "4" ) );
			_SetSubjectResponse(2, 4, GUI.Toggle( new Rect(140f +170f, Screen.height/2-60f +5f, 40f, 30f),
				subjectResponses[2,4], "5" ) );
			_SetSubjectResponse(2, 5, GUI.Toggle( new Rect(140f +210f, Screen.height/2-60f +5f, 40f, 30f),
				subjectResponses[2,5], "6" ) );
			_SetSubjectResponse(2, 6, GUI.Toggle( new Rect(140f +250f, Screen.height/2-60f +5f, 40f, 30f),
				subjectResponses[2,6], "7" ) );
			GUI.Label( new Rect(435f, Screen.height/2-57f, 180f, 36f), "Very Interested" );
			
			// Upper-right response
			GUI.Box( new Rect(Screen.width-580, Screen.height/2-60f, 570, 36f), "" );
			GUI.Label( new Rect(Screen.width-580 +10, Screen.height/2-57, 120, 36), "Uninterested" );
			_SetSubjectResponse(3, 0, GUI.Toggle( new Rect(Screen.width-450 +10f, Screen.height/2-60f +5f, 40f, 30f),
				subjectResponses[3,0], "1" ) );
			_SetSubjectResponse(3, 1, GUI.Toggle( new Rect(Screen.width-450 +50f, Screen.height/2-60f +5f, 40f, 30f),
				subjectResponses[3,1], "2" ) );
			_SetSubjectResponse(3, 2, GUI.Toggle( new Rect(Screen.width-450 +90f, Screen.height/2-60f +5f, 40f, 30f),
				subjectResponses[3,2], "3" ) );
			_SetSubjectResponse(3, 3, GUI.Toggle( new Rect(Screen.width-450 +130f, Screen.height/2-60f +5f, 40f, 30f),
				subjectResponses[3,3], "4" ) );
			_SetSubjectResponse(3, 4, GUI.Toggle( new Rect(Screen.width-450 +170f, Screen.height/2-60f +5f, 40f, 30f),
				subjectResponses[3,4], "5" ) );
			_SetSubjectResponse(3, 5, GUI.Toggle( new Rect(Screen.width-450 +210f, Screen.height/2-60f +5f, 40f, 30f),
				subjectResponses[3,5], "6" ) );
			_SetSubjectResponse(3, 6, GUI.Toggle( new Rect(Screen.width-450 +250f, Screen.height/2-60f +5f, 40f, 30f),
				subjectResponses[3,6], "7" ) );
			GUI.Label( new Rect(Screen.width-580 +425, Screen.height/2-57, 180, 36), "Very Interested" );
			
			if( _HaveAllSubjectResponses() &&
				GUI.Button( new Rect(Screen.width/2-64, Screen.height-72, 128, 64), "DONE" ) )
				expPhase = ExperimentPhase.ResponsesSubmitted;
		}
		else if( expPhase == ExperimentPhase.ResponsesSubmitted )
		{
			// TODO: display nothing?
		}
		else if( expPhase == ExperimentPhase.ThankYou )
		{
			GUI.skin.box.fontSize = 20;
			GUI.skin.box.wordWrap = true;
			GUI.skin.box.alignment = TextAnchor.UpperLeft;
			GUI.Box( new Rect(Screen.width/2-288, Screen.height/2-56, 576, 112),
				"The experiment is now complete. Thank you for your participation!\n\n" +
				"Please notify the experimenter now." );
			GUI.skin.box.wordWrap = false;
			GUI.skin.box.alignment = TextAnchor.MiddleCenter;
		}
	}
	
	protected virtual void _SetSubjectResponse( int qi, int ri, bool resp )
	{
		if(resp)
		{
			for( int j = 0; j < subjectResponses.GetLength(1); ++j )
				subjectResponses[qi,j] = false;
		}
		subjectResponses[qi,ri] = resp;
	}
	
	protected virtual void _ResetSubjectResponses()
	{
		for( int i = 0; i < subjectResponses.GetLength(0); ++i )
			for( int j = 0; j < subjectResponses.GetLength(1); ++j )
				subjectResponses[i,j] = false;
	}
	
	protected virtual bool _HaveAllSubjectResponses()
	{
		for( int i = 0; i < subjectResponses.GetLength(0); ++i )
		{
			bool has_true = false;
			for( int j = 0; j < subjectResponses.GetLength(1); ++j )
			{
				if(subjectResponses[i,j])
				{
					has_true = true;
					break;
				}
			}
			
			if( !has_true )
				return false;
		}
		
		return true;
	}
	
	protected virtual string _GetSubjectResponseValue( int respIndex )
	{
		for( int fi = 0; fi < 7; ++fi )
		{
			if(subjectResponses[respIndex,fi])
				return (fi+1).ToString();
		}
		
		return "-1";
	}
}

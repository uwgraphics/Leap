using UnityEngine;
using UnityEditor;
using System;
using System.Collections;

public class LEAPMenu
{
	/// <summary>
	/// Validates the specified menu item.
	/// </summary>
	/// <returns>
	/// true if an agent is selected, false otherwise. <see cref="System.Boolean"/>
	/// </returns>
	[MenuItem( "LEAP/Animation/Load Morph Channels", true )]
	private static bool vMiLoadMorphChannels()
	{
		GameObject obj = Selection.activeGameObject;
		if( obj == null || obj.GetComponent<MorphController>() == null )
		{
		   return false;
		}
		
		return true;
	}
	
	/// <summary>
	/// Saves morph channel mappings for the selected agent
	/// when user clicks a menu item.
	/// </summary>
	[MenuItem( "LEAP/Animation/Load Morph Channels" )]
	private static void hMiLoadMorphChannels()
	{
		GameObject obj = Selection.activeGameObject;
		
		// Determine LMC file path
		string lmc_path = _GetLMCPath(obj);
		if( lmc_path == "" )
		{
			Debug.LogError( "Model " + obj.name +
			               " does not have a link to its prefab. Morph channel mappings cannot be loaded." );
			return;
		}
		
		// Load LMC
		LMCSerializer lmc = new LMCSerializer();
		if( !lmc.Load( obj, lmc_path ) )
		{
			// Unable to load morph channel mappings
			Debug.LogError( "LMC file not found: " + lmc_path );
			
			return;
		}
		
		UnityEngine.Debug.Log( "Loaded morph channel mappings for model " + obj.name );
	}

	/// <summary>
	/// Validates the specified menu item.
	/// </summary>
	/// <returns>
	/// true if an agent is selected, false otherwise. <see cref="System.Boolean"/>
	/// </returns>
	[MenuItem( "LEAP/Animation/Save Morph Channels", true )]
	private static bool vMiSaveMorphChannels()
	{
		GameObject obj = Selection.activeGameObject;
		if( obj == null || obj.GetComponent<MorphController>() == null )
		{
		   return false;
		}
		
		return true;
	}
	
	/// <summary>
	/// Saves morph channel mappings for the selected agent
	/// when user clicks a menu item.
	/// </summary>
	[MenuItem( "LEAP/Animation/Save Morph Channels" )]
	private static void hMiSaveMorphChannels()
	{
		GameObject obj = Selection.activeGameObject;
		
		// Determine LMC file path
		string lmc_path = _GetLMCPath(obj);
		if( lmc_path == "" )
		{
			lmc_path = "./Assets/Agents/Models/" + obj.name + ".lmc";
			Debug.LogWarning( "Model " + obj.name +
			                 " does not have a link to its prefab. Saving to default path " + lmc_path );
			return;
		}
		
		// Serialize LMC
		LMCSerializer lmc = new LMCSerializer();
		if( !lmc.Serialize( obj, lmc_path ) )
		{
			// Unable to serialize morph channel mappings
			Debug.LogError( "LMC file could not be saved: " + lmc_path );
			
			return;
		}
		
		UnityEngine.Debug.Log( "Saved morph channel mappings for model " + obj.name );
	}
	
	/// <summary>
	/// Validates the specified menu item.
	/// </summary>
	/// <returns>
	/// true if an agent is selected, false otherwise. <see cref="System.Boolean"/>
	/// </returns>
	[MenuItem( "LEAP/Agent Setup/Set Up Default Agent", true )]
	private static bool vMiSetupDefaultAgent()
	{
		GameObject obj = Selection.activeGameObject;
		if( obj == null )
		{
		   return false;
		}
		
		return true;
	}
	
	/// <summary>
	/// Automatically tags the selected agent and adds default
	/// functional components when user clicks a menu item.
	/// </summary>
	[MenuItem( "LEAP/Agent Setup/Set Up Default Agent" )]
	private static void hMiSetupDefaultAgent()
	{
		GameObject obj = Selection.activeGameObject;
		
		// Tag model parts
		ModelController.AutoTagModel(obj);
		
		// Create default anim. controllers
		AnimControllerTree atree = obj.AddComponent<AnimControllerTree>();
		RootController rootctr = obj.AddComponent<RootController>();
		LocomotionController lococtr = obj.AddComponent<LocomotionController>();
		BodyIdleController idlectr = obj.AddComponent<BodyIdleController>();
		GestureController gestctr = obj.AddComponent<GestureController>();
		//obj.AddComponent<PostureController>();
		FaceController facectr = obj.AddComponent<FaceController>();
		ExpressionController exprctr = obj.AddComponent<ExpressionController>();
		SpeechController spctr = obj.AddComponent<SpeechController>();
		GazeController gazectr = obj.AddComponent<GazeController>();
		BlinkController blinkctr = obj.AddComponent<BlinkController>();
		EyesAliveController eactr = obj.AddComponent<EyesAliveController>();
		// Link the controllers into a hierarchy
		atree.rootController = obj.GetComponent<RootController>();
		rootctr.childControllers = new AnimController[2];
		rootctr.childControllers[0] = lococtr;
		rootctr.childControllers[1] = facectr;
		lococtr.childControllers = new AnimController[2];
		lococtr.childControllers[0] = idlectr;
		lococtr.childControllers[1] = gestctr;
		facectr.childControllers = new AnimController[3];
		facectr.childControllers[0] = exprctr;
		facectr.childControllers[1] = spctr;
		facectr.childControllers[2] = gazectr;
		gazectr.childControllers = new AnimController[2];
		gazectr.childControllers[0] = blinkctr;
		gazectr.childControllers[1] = eactr;
		// Initialize controller states
		rootctr.UEdCreateStates();
		lococtr.UEdCreateStates();
		idlectr.UEdCreateStates();
		gestctr.UEdCreateStates();
		facectr.UEdCreateStates();
		exprctr.UEdCreateStates();
		spctr.UEdCreateStates();
		gazectr.UEdCreateStates();
		blinkctr.UEdCreateStates();
		eactr.UEdCreateStates();
		FaceController.UEdInitRandomHeadMotion(obj);
		
		// Add GUI helper components
		obj.AddComponent<EyeLaserGizmo>();
	}
	
	/// <summary>
	/// Validates the specified menu item.
	/// </summary>
	/// <returns>
	/// true if an agent is selected, false otherwise. <see cref="System.Boolean"/>
	/// </returns>
	[MenuItem( "LEAP/Agent Setup/Refresh Agent", true )]
	private static bool vMiRefreshAgent()
	{
		GameObject obj = Selection.activeGameObject;
		if( obj == null )
		{
		   return false;
		}
		
		return true;
	}
	
	/// <summary>
	/// Automatically tags the selected agent and adds default
	/// functional components when user clicks a menu item.
	/// </summary>
	[MenuItem( "LEAP/Agent Setup/Refresh Agent" )]
	private static void hMiRefreshAgent()
	{
		GameObject obj = Selection.activeGameObject;
		GameObject baseobj = (GameObject)EditorUtility.GetPrefabParent(obj);
		
		// Find the modified imported model
		GameObject[] objs = GameObject.FindObjectsOfTypeIncludingAssets(typeof(GameObject)) as GameObject[];
		foreach( GameObject new_baseobj in objs )
		{
			if( new_baseobj.name != baseobj.name ||
			   EditorUtility.GetPrefabType(new_baseobj) != PrefabType.ModelPrefab )
				continue;
			
			LEAPAssetUtils.RefreshAgentModel( new_baseobj, obj );
			return;
		}
	}
	
	/// <summary>
	/// Validates the specified menu item.
	/// </summary>
	/// <returns>
	/// true if an agent is selected, false otherwise.
	/// </returns>
	[MenuItem( "LEAP/Agent Setup/Init. Controller States", true )]
	private static bool vMiCreateFSMStates()
	{
		GameObject obj = Selection.activeGameObject;
		if( obj == null )
		{
		   return false;
		}
		
		return true;
	}
	
	/// <summary>
	/// Initializes default state definitions of the 
	/// animation controllers defined on the selected agent.
	/// </summary>
	[MenuItem( "LEAP/Agent Setup/Init. Controller States" )]
	private static void hMiCreateFSMStates()
	{
		GameObject obj = Selection.activeGameObject;
		Component[] comp_list = obj.GetComponents<AnimController>();
		
		foreach( Component comp in comp_list )
		{
			if( !( comp is AnimController ) )
				continue;
			
			AnimController anim_ctrl = (AnimController)comp;
			anim_ctrl.UEdCreateStates();
		}
	}
	
	/// <summary>
	/// Validates the specified menu item.
	/// </summary>
	/// <returns>
	/// true if a photo mosaic is selected, false otherwise.
	/// </returns>
	[MenuItem( "LEAP/Scenario/Mosaic Keyframe" )]
	private static bool vMiMosaicKeyframe()
	{
		GameObject obj = Selection.activeGameObject;
		if( obj == null || obj.GetComponent<PhotoMosaic>() == null )
		{
		   return false;
		}
		
		return true;
	}
	
	/// <summary>
	/// Keyframes the current position and scale of the photo mosaic.
	/// </summary>
	[MenuItem( "LEAP/Scenario/Mosaic Keyframe" )]
	private static void hMiMosaicKeyframe()
	{
		GameObject obj = Selection.activeGameObject;
		PhotoMosaic pm = obj.GetComponent<PhotoMosaic>();
		
		// Extend keyframe array
		int nkfi = pm.keyFrames.Length;
		PhotoMosaic.KeyFrame[] kfs = new PhotoMosaic.KeyFrame[nkfi+1];
		pm.keyFrames.CopyTo(kfs,0);
		pm.keyFrames = kfs;
		pm.keyFrames[nkfi] = new PhotoMosaic.KeyFrame();
		
		// Fill new keyframe
		pm.keyFrames[nkfi].name = "NewKeyFrame";
		pm.keyFrames[nkfi].position = pm.transform.localPosition;
		pm.keyFrames[nkfi].scale = pm.transform.localScale;
	}
	
	private static string _GetLMCPath( GameObject obj )
	{
		// Determine original asset path
		string asset_path = "";
		UnityEngine.Object prefab = EditorUtility.GetPrefabParent(obj);
		if( prefab != null )
		{
			asset_path = AssetDatabase.GetAssetPath(prefab);
		}
		else
		{
			return "";
		}
		
		// Determine LMC file path
		string lmc_path = "";
		int ext_i = asset_path.LastIndexOf( ".", StringComparison.InvariantCultureIgnoreCase );
		if( ext_i >= 0 )
		{
			lmc_path = asset_path.Substring( 0, ext_i ) + ".lmc";
		}
		else
		{
			lmc_path += ".lmc";
		}
		
		return lmc_path;
	}

}

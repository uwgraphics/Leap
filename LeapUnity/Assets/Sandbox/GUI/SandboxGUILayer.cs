using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SandboxGUILayer : MonoBehaviour
{
	// For turning GUI elements on and off
	public bool hideGUI = false;
	public bool hideViewSettings = false;
	public bool hideMacroControls = false;
	
	// Overhead icon
	public Texture overheadIcon;
	public float overheadIconHeight = 0.2f;
	public float overheadIconScale = 8f;
	
	// Camera list
	public Texture[] cameraList_Items;
	public Texture[] cameraList_ItemsDown;
	public int cameraList_SelectedItem = 0;
	
	public const int viewWnd_ID = 0;
	public const int macroWnd_ID = 1;

	private SandboxObject[] sandboxObjs = null;
	private SandboxAgentObject selSBAgentObj = null;
	
	private GUIContent[] actionMenu_items = null;
	private bool actionMenu_show = false;
	private int actionMenu_selItem = 0;
	private SandboxObject actionTarget = null;
	
	private bool mouseOnAgent = false;
	
	void Awake()
	{	
		// Initialize camera list
		if( cameraList_Items != null && cameraList_ItemsDown != null &&
		   cameraList_ItemsDown.Length == cameraList_Items.Length &&
		   cameraList_ItemsDown.Length > 0 )
		{
			// Selection changed, swap icons
			
			Texture ico = cameraList_Items[cameraList_SelectedItem];
			cameraList_Items[cameraList_SelectedItem] = cameraList_ItemsDown[cameraList_SelectedItem];
			cameraList_ItemsDown[cameraList_SelectedItem] = ico;
		}
		
		// Subscribe to events from all sandbox objects
		sandboxObjs = Object.FindObjectsOfType(typeof(SandboxObject)) as SandboxObject[];
		if( sandboxObjs != null )
		{
			foreach( SandboxObject sbobj in sandboxObjs )
			{
				sbobj.MouseClick += new SandboxObjMouseClickEvtH(SandboxObject_MouseClick);
				sbobj.MouseClickHold += new SandboxObjMouseClickHoldEvtH(SandboxObject_MouseClickHold);
				sbobj.MouseClickDrag += new SandboxObjMouseClickDragEvtH(SandboxObject_MouseClickDrag);
				sbobj.MouseClickDrop += new SandboxObjMouseClickDropEvtH(SandboxObject_MouseClickDrop);
				sbobj.MouseEnter += new SandboxObjMouseEnterEvtH(SandboxObject_MouseEnter);
				sbobj.MouseExit += new SandboxObjMouseExitEvtH(SandboxObject_MouseExit);
			}
		}
	}
	
	void Update()
	{
		// Show/hide GUI elements
		if( Input.GetKeyDown(KeyCode.H) )
		{
			hideGUI = !hideGUI;
		}
		if( Input.GetKeyDown(KeyCode.V) )
		{
			hideViewSettings = !hideViewSettings;
		}
		if( Input.GetKeyDown(KeyCode.M) )
		{
			hideMacroControls = !hideMacroControls;
		}
	}

	void OnGUI()
	{	
		if(hideGUI)
		{
			// Don't render GUI
			
			return;
		}
	
		GUI.Label( new Rect( Screen.width/2 - 120, 2, 240, 24 ), "Press H to toggle GUI overlay" );
		
		int next_top = 10;
		if( !hideViewSettings )
		{
			GUI.Window( viewWnd_ID, new Rect( 10, 10, 200, 80 ), ViewWndCB, "View" );
			next_top += 80;
		}
		if( !hideMacroControls )
		{
			GUI.Window( macroWnd_ID, new Rect( 10, next_top + 10, 200, 80 ), MacroWndCB, "Macro" );
			next_top += 10;
		}
		
		if( selSBAgentObj != null )
		{
			// Indicate selected agent with an overhead icon
			
			// Determine icon location
			Vector3 pos;
            Transform head = ModelUtil.FindBoneWithTag(selSBAgentObj.gameObject.transform, "HeadBone");
			if( head != null )
				pos = head.position;
			else
				// Just guess where it might be
				pos = selSBAgentObj.gameObject.transform.position + new Vector3( 0, 1.8f, 0 );
			pos -= new Vector3( 0, overheadIconHeight, 0 );
			Vector3 scrpos = Camera.main.WorldToScreenPoint(pos);
			
			// Render icon
			GUI.DrawTexture( new Rect( scrpos.x - 20 * overheadIconScale, scrpos.y - 20 * overheadIconScale, 40 * overheadIconScale, 40 * overheadIconScale ),
			                overheadIcon, ScaleMode.StretchToFill, true, 0 );
		}
		
		// Action context menu
		if( SandboxGUI.PopUpMenu( ref actionMenu_show, ref actionMenu_selItem, actionMenu_items ) )
		{
			selSBAgentObj.doAction( actionMenu_items[actionMenu_selItem].text, actionTarget.gameObject );
		}
	}
	
	private void ViewWndCB( int wndId )
	{
		if( cameraList_Items == null )
			return;
		
		// Camera list
		int sel0 = cameraList_SelectedItem;
		cameraList_SelectedItem = GUI.Toolbar( new Rect( 10, 30, 180, 35 ), cameraList_SelectedItem, cameraList_Items );
		if( cameraList_ItemsDown != null && cameraList_ItemsDown.Length == cameraList_Items.Length &&
		   cameraList_SelectedItem != sel0 )
		{
			// Selection changed, swap icons
			
			Texture ico = cameraList_Items[sel0];
			cameraList_Items[sel0] = cameraList_ItemsDown[sel0];
			cameraList_ItemsDown[sel0] = ico;
			
			ico = cameraList_Items[cameraList_SelectedItem];
			cameraList_Items[cameraList_SelectedItem] = cameraList_ItemsDown[cameraList_SelectedItem];
			cameraList_ItemsDown[cameraList_SelectedItem] = ico;
		}
	}
	
	private void MacroWndCB( int wndId )
	{
		// Macro play/record controls
		// TODO
	}
		
	private void SandboxObject_MouseClick( SandboxObject sbObj )
	{
		if( sbObj is SandboxAgentObject )
		{
			// User has selected an agent
			
			if( selSBAgentObj != null )
			{
				// An agent is already selected
				
				// Remove highlight from previously selected agent
				/*HashSet<Material> mats0 = _GetObjectMaterials(sbObj.gameObject);
				foreach( Material mat in mats0 )
				{
					mat.color -= new Color( 0f, 0.35f, 0f, 0 );
				}*/
			}
			
			selSBAgentObj = (SandboxAgentObject)sbObj;
			
			// Indicate selected agent by adding some highlight
			/*HashSet<Material> mats = _GetObjectMaterials(selSBAgentObj.gameObject);
			foreach( Material mat in mats )
			{
				mat.color += new Color( 0f, 0.35f, 0f, 0 );
			}*/
			
			// TODO: add ability to deselect agent (e.g. by clicking at nothing)
			// and remove highlight
		}
		else // if( sbObj is SandboxPropObject )
		{
			if( selSBAgentObj != null )
			{
				// Perform default action on target object
				
				selSBAgentObj.doAction( "", sbObj.gameObject );
			}
		}
	}
	
	private void SandboxObject_MouseClickHold( SandboxObject sbObj )
	{
		if( !hideGUI && selSBAgentObj != null )
		{
			// Show a context menu of supported agent actions
			
			string[] actions = selSBAgentObj.GetActionList();
			actionMenu_items = new GUIContent[actions.Length];
			for( int ai = 0; ai < actions.Length; ++ai )
				actionMenu_items[ai] = new GUIContent( actions[ai] );
			actionMenu_show = true;
			actionTarget = sbObj;
		}
	}
	
	private void SandboxObject_MouseClickDrag( SandboxObject sbObj )
	{
	}
	
	private void SandboxObject_MouseClickDrop( SandboxObject sbObj )
	{
	}
	
	private void SandboxObject_MouseEnter( SandboxObject sbObj )
	{
		if(hideGUI)
			// Don't highlight if GUI is disabled
			return;
		
		mouseOnAgent = true;
		
		HashSet<Material> mats = _GetObjectMaterials(sbObj.gameObject);
		
		// Highlight object
		foreach( Material mat in mats )
		{
			mat.color += new Color( 0.35f, 0.35f, 0.35f, 0 );
		}
	}
	
	private void SandboxObject_MouseExit( SandboxObject sbObj )
	{
		if( !mouseOnAgent || hideGUI && !mouseOnAgent )
			return;
		
		mouseOnAgent = false;
		
		HashSet<Material> mats = _GetObjectMaterials(sbObj.gameObject);
		
		// Remove highlight from object
		foreach( Material mat in mats )
		{
			mat.color -= new Color( 0.35f, 0.35f, 0.35f, 0 );
		}
	}
	
	private HashSet<Material> _GetObjectMaterials( GameObject obj )
	{
		// Get all materials in object
		HashSet<Material> mats = new HashSet<Material>();
		MeshRenderer[] mr_list = obj.gameObject.GetComponentsInChildren<MeshRenderer>();
		SkinnedMeshRenderer[] smr_list = obj.gameObject.GetComponentsInChildren<SkinnedMeshRenderer>();
		foreach( MeshRenderer mr in mr_list )
		{
			foreach( Material mat in mr.materials )
			{
				if( mats.Contains(mat) )
					continue;
				
				mats.Add(mat);
			}
		}
		foreach( SkinnedMeshRenderer smr in smr_list )
		{
			foreach( Material mat in smr.materials )
			{
				if( mats.Contains(mat) )
					continue;
				
				mats.Add(mat);
			}
		}
		
		return mats;
	}
	
}

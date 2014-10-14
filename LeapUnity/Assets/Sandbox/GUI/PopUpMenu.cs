using UnityEngine;

public static class SandboxGUI
{
	
	private static int popUpMenu_Hash = "PopUpMenu".GetHashCode();
	private static GUIStyle popUpMenu_DefaultItemStyle = PopUpMenu_DefaultItemStyle();
	private static GUIStyle popUpMenu_DefaultBoxStyle = PopUpMenu_DefaultBoxStyle();
	private static int popUpMenu_DefaultItemWidth = 160;
	private static int popUpMenu_DefaultItemHeight = 20;
	private static Rect popUpMenu_Rect;
	private static bool popUpMenu_RecalcRect = true;
	private static bool popUpMenu_Selecting = false;
	
	private static GUIStyle PopUpMenu_DefaultItemStyle()
	{
		GUIStyle style = new GUIStyle();
		style.normal.textColor = Color.white;
		Texture2D tex = new Texture2D( 2, 2 );
		Color[] colors = new Color[4];
		for( int col_i = 0; col_i < colors.Length; ++col_i )
			colors[col_i] = Color.white;
		tex.SetPixels(colors);
		tex.Apply();
		style.hover.background = tex;
		style.onHover.background = tex;
		style.padding.left = style.padding.right = style.padding.top = style.padding.bottom = 4;
		
		return style;
	}
	
	private static GUIStyle PopUpMenu_DefaultBoxStyle()
	{
		return GUI.skin.box;
	}
	
	public static bool PopUpMenu( ref bool show, ref int selItem, GUIContent[] items )
	{
		return PopUpMenu( ref show, ref selItem, items,
		                 popUpMenu_DefaultItemStyle, popUpMenu_DefaultBoxStyle );
	}
	
	public static bool PopUpMenu( ref bool show, ref int selItem, GUIContent[] items,
	                             GUIStyle itemStyle, GUIStyle boxStyle )
	{
		if( items == null )
			return false;
		
		int cid = GUIUtility.GetControlID( popUpMenu_Hash, FocusType.Passive );
		bool close = false;

		if(show)
		{
			// Compute rectangle for the menu
			if(popUpMenu_RecalcRect)
			{
				float sizex = popUpMenu_DefaultItemWidth;
				float sizey = items.Length * popUpMenu_DefaultItemHeight;
				float posx = Input.mousePosition.x;
				float posy = Screen.height - Input.mousePosition.y;
				if( posx + sizex > Screen.width )
					posx = Screen.width - sizex;
				if( posy + sizey > Screen.height )
					posy = Screen.height - sizey;
				if( posx < 0 )
					posx = 0;
				if( posy < 0 )
					posy = 0;
				popUpMenu_Rect = new Rect( posx, posy, sizex, sizey );
				
				popUpMenu_RecalcRect = false;
			}
			
			if( Event.current.GetTypeForControl(cid) == EventType.mouseDown )
			{
				// User has clicked...
				
				popUpMenu_Selecting = true;
			}
			else if( popUpMenu_Selecting && Event.current.GetTypeForControl(cid) == EventType.mouseUp )
			{
				// User has unclicked, don't show the menu anymore!
				
				popUpMenu_RecalcRect = true;
				popUpMenu_Selecting = false;
				show = false;
				close = true;
			}
			
			GUI.Box( popUpMenu_Rect, "", boxStyle );
			selItem = GUI.SelectionGrid( popUpMenu_Rect, selItem, items, 1, itemStyle );
		}
		
		return close;
	}
	
}

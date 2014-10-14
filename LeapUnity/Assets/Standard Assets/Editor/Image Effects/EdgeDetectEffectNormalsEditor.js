
@script ExecuteInEditMode()

@CustomEditor (EdgeDetectEffectNormals)

class EdgeDetectEffectNormalsEditor extends Editor 
{	
	var serObj : SerializedObject;	
		
	var mode : SerializedProperty;
	var sensitivityDepth : SerializedProperty;
	var sensitivityNormals : SerializedProperty;

	var edgesOnly : SerializedProperty;
	var edgesOnlyBgColor : SerializedProperty;	
	

	function OnEnable () {
		serObj = new SerializedObject (target);
		
		mode = serObj.FindProperty("mode");
		
		sensitivityDepth = serObj.FindProperty("sensitivityDepth");
		sensitivityNormals = serObj.FindProperty("sensitivityNormals");

		edgesOnly = serObj.FindProperty("edgesOnly");
		edgesOnlyBgColor = serObj.FindProperty("edgesOnlyBgColor");	
	}
    		
    function OnInspectorGUI ()
    {         
    	EditorGUILayout.PropertyField (mode, new GUIContent("Mode"));
    	
   		EditorGUILayout.PropertyField (sensitivityDepth, new GUIContent("Depth sensitivity"));
   		EditorGUILayout.PropertyField (sensitivityNormals, new GUIContent("Normals sensitivity"));
   		    		
   		EditorGUILayout.Separator ();
   		
   		edgesOnly.floatValue = EditorGUILayout.Slider ("Draw edges only", edgesOnly.floatValue, 0.0, 1.0);
   		EditorGUILayout.PropertyField (edgesOnlyBgColor, new GUIContent ("Background color"));    		
    	    	
    	serObj.ApplyModifiedProperties();
    }
}

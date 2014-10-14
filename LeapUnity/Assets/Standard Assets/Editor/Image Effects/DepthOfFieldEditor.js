
@script ExecuteInEditMode()

@CustomEditor (DepthOfField)

class DepthOfFieldEditor extends Editor 
{	
	var serObj : SerializedObject;	
		 
	var quality : SerializedProperty; // = DofQualitySetting.High;

	var focalZDistance : SerializedProperty;//float = 0.0;
	var focalZStart : SerializedProperty;//float = 0.0;
	var focalZEnd : SerializedProperty;//float = 10000.0;
	var focalFalloff : SerializedProperty;//float = 1.0;

	var objectFocus : SerializedProperty;//Transform = null;
	var focalSize : SerializedProperty;//float = 0.0375;

	var blurIterations : SerializedProperty;//int = 2;
	var blurSpread : SerializedProperty;//float = 1.5;
	var foregroundBlurIterations : SerializedProperty;// : float = 1.0;
	var foregroundBlurSpread : SerializedProperty;// : float = 1.0;
	var foregroundBlurWeight : SerializedProperty;
	
	var divider : SerializedProperty;
	var enableBokeh : SerializedProperty;
	var enableNoise : SerializedProperty;
	var noiseAmount : SerializedProperty;
	var bokehFalloff : SerializedProperty;
	
	var debugBlurRegions : SerializedProperty;
	
	var bokehThreshhold : SerializedProperty;
	var downsampleRadius : SerializedProperty;
	  
	function OnEnable () {
		serObj = new SerializedObject (target);
		
		quality = serObj.FindProperty("quality");
		
		
        focalZDistance = serObj.FindProperty("focalZDistance");
        focalZStart = serObj.FindProperty("focalStartCurve");
        focalZEnd = serObj.FindProperty("focalEndCurve");
        focalFalloff = serObj.FindProperty("focalFalloff");
        
        objectFocus = serObj.FindProperty("objectFocus");
        focalSize = serObj.FindProperty("focalSize");
        
        blurIterations = serObj.FindProperty("blurIterations");
        foregroundBlurIterations = serObj.FindProperty("foregroundBlurIterations");
                
        blurSpread = serObj.FindProperty("blurSpread");
        downsampleRadius = serObj.FindProperty("downsampleRadius");
        
        enableNoise = serObj.FindProperty("enableNoise");
        noiseAmount = serObj.FindProperty("noiseAmount");
        bokehThreshhold = serObj.FindProperty("bokehThreshhold");
        enableBokeh = serObj.FindProperty("enableBokeh");
        bokehFalloff = serObj.FindProperty("bokehFalloff"); 
        divider = serObj.FindProperty("divider");

		foregroundBlurIterations = serObj.FindProperty("foregroundBlurIterations");
		foregroundBlurSpread = serObj.FindProperty("foregroundBlurSpread");
		foregroundBlurWeight = serObj.FindProperty("foregroundBlurWeight");
				
		debugBlurRegions = serObj.FindProperty("debugBlurRegions");
	}
    		
    function OnInspectorGUI ()
    {       
    	serObj.Update();
    	
		GUILayout.Label("Defines performance and quality");
        EditorGUILayout.PropertyField (quality,  new GUIContent("Quality"));
        divider.floatValue = Mathf.Floor( EditorGUILayout.Slider(new GUIContent("Downsample"),divider.floatValue,1.0,4.0));
        
        
        EditorGUILayout.Separator ();
        
        focalZDistance.floatValue = EditorGUILayout.FloatField(new GUIContent("Focal Distance","Camera focal point in [nearClip, farClip]"), focalZDistance.floatValue);
        focalSize.floatValue = EditorGUILayout.Slider(new GUIContent("Focal Size", "Camera focal size in [0, 1]"),focalSize.floatValue,0.0,0.2);
        
        EditorGUILayout.Separator ();
        GUILayout.Label("Curve tweaking");
        EditorGUILayout.BeginHorizontal();
        focalZStart.floatValue = EditorGUILayout.FloatField("Start Curve", focalZStart.floatValue);
        focalZEnd.floatValue = EditorGUILayout.FloatField("End Curve", focalZEnd.floatValue);
        EditorGUILayout.EndHorizontal();
        focalFalloff.floatValue = EditorGUILayout.FloatField("Global Curve", focalFalloff.floatValue);
		
		EditorGUILayout.Separator ();
          
        GUILayout.Label("Autofocus settings");
        
        EditorGUILayout.PropertyField (objectFocus,  new GUIContent("Object focus")); 	
        

        EditorGUILayout.Separator ();
        
        EditorGUILayout.PropertyField (enableBokeh, new GUIContent("Bokeh"));
         EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PropertyField (bokehThreshhold, new GUIContent("Cutoff"));
        EditorGUILayout.PropertyField (bokehFalloff, new GUIContent("Amplify"));
       // EditorGUILayout.PropertyField (noiseAmount, new GUIContent("Noise"));
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Separator ();
        
        if(quality.intValue > 1)
        	GUILayout.Label("Background Blur");
        else
        	GUILayout.Label("General Blur");
        	
        blurSpread.floatValue = EditorGUILayout.Slider ("Spread",blurSpread.floatValue,0.0,5.0);
        blurIterations.intValue = EditorGUILayout.IntSlider ("Iterations", blurIterations.intValue, 1,2);
        
        EditorGUILayout.Separator ();
	        
       	GUILayout.Label("Foreground Blur (HQ only)");
       	
       	foregroundBlurSpread.floatValue = EditorGUILayout.Slider ("Spread", foregroundBlurSpread.floatValue,0.0,5.0);  
       	foregroundBlurIterations.intValue = EditorGUILayout.IntSlider("Iterations", foregroundBlurIterations.intValue, 0,2); 
        foregroundBlurWeight.floatValue = EditorGUILayout.Slider ("Weight",foregroundBlurWeight.floatValue,0.0,7.0);

		EditorGUILayout.Separator();

		// will come back
        //EditorGUILayout.PropertyField (debugBlurRegions,  new GUIContent("Debug Blur Areas","Enable this to see the blur amounts and radii in red and green")); 	
		    	
    	serObj.ApplyModifiedProperties();
    	
    	
    }
}

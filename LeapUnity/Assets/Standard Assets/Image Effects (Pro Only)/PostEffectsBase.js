
@script ExecuteInEditMode
@script RequireComponent (Camera)

class PostEffectsBase extends MonoBehaviour 
{	
	function CheckShaderAndCreateMaterial(s : Shader, m2Create : Material) : Material 
	{
		if(m2Create && m2Create.shader == s) return m2Create;
		if(!s) { 
			Debug.Log("Missing shader in "+this.ToString());
			enabled = false;
			return null;
		}
		
		if(!s.isSupported) 
		{
			ReportNotSupported();
			Debug.LogError("The shader " + s.ToString() + " on effect "+this.ToString()+" is not supported on this platform!");
			return null;
		}
		else 
		{
			m2Create = new Material(s);	
			m2Create.hideFlags = HideFlags.HideAndDontSave;		
			if(m2Create) return m2Create;
			else return null;
		}
	}
		
	function CheckSupport(needDepth : boolean) : boolean
	{
		if (!SystemInfo.supportsImageEffects || !SystemInfo.supportsRenderTextures) 
		{
			ReportNotSupported();
			enabled = false;
			return false;
		}		
		
		if(needDepth && !SystemInfo.SupportsRenderTextureFormat (RenderTextureFormat.Depth))
		{
			ReportNotSupported();
			enabled = false;
			return false;
		}
		
		return true;
	}
	
	// Was used by Image Effect scripts in 3.0/3.1, keeping it here so that old
	// scripts still work.
	function CheckShader(s : Shader) : boolean {
		Debug.Log("The shader " + s.ToString() + " on effect "+this.ToString()+" is not part of the Unity 3.2 effects suite anymore. For best performance and quality, please ensure you are using the latest Standard Assets Image Effects (Pro only) package.");		
		if(!s.isSupported) {
			ReportNotSupported ();
			return false;
		} else {
			return false;
		}
	}
	
	function ReportNotSupported() {
		Debug.LogError("The image effect " + this.ToString() + " on "+this.name+" is not supported on this platform!");
		enabled = false;
		return;
	}
	
	function OnRenderImage(source : RenderTexture, destination : RenderTexture) {
		Debug.Log("OnRenderImage in Base called ...");
	}
	
	function DrawLowLevelPlaneAlignedWithCamera(
		dist : float,
		source : RenderTexture, dest : RenderTexture, 
		material : Material, 
		cameraForProjectionMatrix : Camera )
	{
        // Make the destination texture the target for all rendering
        RenderTexture.active = dest;
        // Assign the source texture to a property from a shader
        material.SetTexture("_MainTex", source);
        var invertY : boolean = true; // source.texelSize.y < 0.0f;
        // Set up the simple Matrix
        GL.PushMatrix();
        GL.LoadIdentity();	
        GL.LoadProjectionMatrix(cameraForProjectionMatrix.projectionMatrix);
        
        var fovYHalfRad : float = cameraForProjectionMatrix.fieldOfView * 0.5 * Mathf.Deg2Rad;
        var cotangent : float = Mathf.Cos(fovYHalfRad) / Mathf.Sin(fovYHalfRad);
        var asp : float = cameraForProjectionMatrix.aspect;
        
        var x1 : float = asp/-cotangent;
        var x2 : float = asp/cotangent;
        var y1 : float = 1.0/-cotangent;
        var y2 : float = 1.0/cotangent;
        
        var sc : float = 1.0; // magic constant (for now)
        
        x1 *= dist * sc;
        x2 *= dist * sc;
        y1 *= dist * sc;
        y2 *= dist * sc;
                
        var z1 : float = -dist;
        
        for (var i : int = 0; i < material.passCount; i++)
        {
            material.SetPass(i);

	        GL.Begin(GL.QUADS);
	        var y1_ : float; var y2_ : float;
	        if (invertY)
	        {
	            y1_ = 1.0; y2_ = 0.0;
	        }
	        else
	        {
	            y1_ = 0.0; y2_ = 1.0;
	        }
	        GL.TexCoord2(0.0, y1_); GL.Vertex3(x1, y1, z1);
	        GL.TexCoord2(1.0, y1_); GL.Vertex3(x2, y1, z1);
	        GL.TexCoord2(1.0, y2_); GL.Vertex3(x2, y2, z1);
	        GL.TexCoord2(0.0, y2_); GL.Vertex3(x1, y2, z1);
	        GL.End();	
        }	
        
        GL.PopMatrix();        
	}
	
	function DrawBorder (
		dest : RenderTexture, 
		material : Material )
	{
		var x1 : float;	
		var x2 : float;
		var y1 : float;
		var y2 : float;		
		
		RenderTexture.active = dest;
        var invertY : boolean = true; // source.texelSize.y < 0.0f;
        // Set up the simple Matrix
        GL.PushMatrix();
        GL.LoadOrtho();		
        
        for (var i : int = 0; i < material.passCount; i++)
        {
            material.SetPass(i);
	        
	        var y1_ : float; var y2_ : float;
	        if (invertY)
	        {
	            y1_ = 1.0; y2_ = 0.0;
	        }
	        else
	        {
	            y1_ = 0.0; y2_ = 1.0;
	        }
	        	        
	        // left	        
	        x1 = 0.0;
	        x2 = 0.0 + 1.0/(dest.width*1.0);
	        y1 = 0.0;
	        y2 = 1.0;
	        GL.Begin(GL.QUADS);
	        
	        GL.TexCoord2(0.0, y1_); GL.Vertex3(x1, y1, 0.1);
	        GL.TexCoord2(1.0, y1_); GL.Vertex3(x2, y1, 0.1);
	        GL.TexCoord2(1.0, y2_); GL.Vertex3(x2, y2, 0.1);
	        GL.TexCoord2(0.0, y2_); GL.Vertex3(x1, y2, 0.1);
	
	        // right
	        x1 = 1.0 - 1.0/(dest.width*1.0);
	        x2 = 1.0;
	        y1 = 0.0;
	        y2 = 1.0;

	        GL.TexCoord2(0.0, y1_); GL.Vertex3(x1, y1, 0.1);
	        GL.TexCoord2(1.0, y1_); GL.Vertex3(x2, y1, 0.1);
	        GL.TexCoord2(1.0, y2_); GL.Vertex3(x2, y2, 0.1);
	        GL.TexCoord2(0.0, y2_); GL.Vertex3(x1, y2, 0.1);	        
	
	        // top
	        x1 = 0.0;
	        x2 = 1.0;
	        y1 = 0.0;
	        y2 = 0.0 + 1.0/(dest.height*1.0);

	        GL.TexCoord2(0.0, y1_); GL.Vertex3(x1, y1, 0.1);
	        GL.TexCoord2(1.0, y1_); GL.Vertex3(x2, y1, 0.1);
	        GL.TexCoord2(1.0, y2_); GL.Vertex3(x2, y2, 0.1);
	        GL.TexCoord2(0.0, y2_); GL.Vertex3(x1, y2, 0.1);
	        
	        // bottom
	        x1 = 0.0;
	        x2 = 1.0;
	        y1 = 1.0 - 1.0/(dest.height*1.0);
	        y2 = 1.0;

	        GL.TexCoord2(0.0, y1_); GL.Vertex3(x1, y1, 0.1);
	        GL.TexCoord2(1.0, y1_); GL.Vertex3(x2, y1, 0.1);
	        GL.TexCoord2(1.0, y2_); GL.Vertex3(x2, y2, 0.1);
	        GL.TexCoord2(0.0, y2_); GL.Vertex3(x1, y2, 0.1);	
	                	              
	        GL.End();	
        }	
        
        GL.PopMatrix();
	}
	
	function DrawLowLevelQuad( x1 : float, x2 : float, y1 : float, y2 : float, source : RenderTexture, dest : RenderTexture, material : Material ) 
	{
        // Make the destination texture the target for all rendering
        RenderTexture.active = dest;
        // Assign the source texture to a property from a shader
        material.SetTexture("_MainTex", source);
        var invertY : boolean = true; // source.texelSize.y < 0.0f;
        // Set up the simple Matrix
        GL.PushMatrix();
        GL.LoadOrtho();

        for (var i : int = 0; i < material.passCount; i++)
        {
            material.SetPass(i);

	        GL.Begin(GL.QUADS);
	        var y1_ : float; var y2_ : float;
	        if (invertY)
	        {
	            y1_ = 1.0; y2_ = 0.0;
	        }
	        else
	        {
	            y1_ = 0.0; y2_ = 1.0;
	        }
	        GL.TexCoord2(0.0, y1_); GL.Vertex3(x1, y1, 0.1);
	        GL.TexCoord2(1.0, y1_); GL.Vertex3(x2, y1, 0.1);
	        GL.TexCoord2(1.0, y2_); GL.Vertex3(x2, y2, 0.1);
	        GL.TexCoord2(0.0, y2_); GL.Vertex3(x1, y2, 0.1);
	        GL.End();	
        }	
        
        GL.PopMatrix();
	}
}
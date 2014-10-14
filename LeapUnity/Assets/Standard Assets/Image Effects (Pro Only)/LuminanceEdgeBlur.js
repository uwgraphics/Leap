
public var showGeneratedNormals : boolean = false;
public var offsetScale : float = 0.1;
public var blurRadius : float = 18.0;

@script ExecuteInEditMode

@script RequireComponent (Camera)
@script AddComponentMenu ("Image Effects/Edge Blur (Luminance)")

class LuminanceEdgeBlur extends PostEffectsBase 
{	
	public var luminance2Normals : Shader;
	private var _luminance2NormalsBasedBlur : Material = null;	

	function CreateMaterials () 
	{
		_luminance2NormalsBasedBlur = CheckShaderAndCreateMaterial(luminance2Normals,_luminance2NormalsBasedBlur);
	}
	
	function Start () 
	{
		CreateMaterials();
		CheckSupport(false);
	}

	function OnRenderImage (source : RenderTexture, destination : RenderTexture)
	{	
		CreateMaterials ();
		
		_luminance2NormalsBasedBlur.SetFloat("_OffsetScale", offsetScale);
		_luminance2NormalsBasedBlur.SetFloat("_BlurRadius", blurRadius);

		if (showGeneratedNormals) 
		{
			luminance2Normals.EnableKeyword("SHOW_DEBUG_ON");		
			luminance2Normals.DisableKeyword("SHOW_DEBUG_OFF");		
		} 
		else
		{
			luminance2Normals.DisableKeyword("SHOW_DEBUG_ON");		
			luminance2Normals.EnableKeyword("SHOW_DEBUG_OFF");		
		}

		Graphics.Blit (source, destination, _luminance2NormalsBasedBlur);	
	}
}
	



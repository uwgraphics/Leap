

@script ExecuteInEditMode
@script RequireComponent (Camera)

@script AddComponentMenu ("Image Effects/Vignette")
		
class Vignetting extends PostEffectsBase {
	
	public var vignetteIntensity : float = 0.375;
	public var chromaticAberrationIntensity : float = 0.0;
	public var blurVignette : float = 0.0;
	
    // needed shaders & materials
	
	public var vignetteShader : Shader;
	private var _vignetteMaterial : Material;
	
	public var separableBlurShader : Shader;
	private var _separableBlurMaterial : Material;	
	
	public var chromAberrationShader : Shader;
	private var _chromAberrationMaterial : Material;


	function Start () 
	{
		CreateMaterials ();
		CheckSupport(false);
	}
	
	function CreateMaterials () 
	{			
		_vignetteMaterial = CheckShaderAndCreateMaterial(vignetteShader,_vignetteMaterial);
		_separableBlurMaterial = CheckShaderAndCreateMaterial(separableBlurShader,_separableBlurMaterial);
		_chromAberrationMaterial = CheckShaderAndCreateMaterial(chromAberrationShader,_chromAberrationMaterial);
	}
	
	function OnEnable () { }
	
	function OnRenderImage (source : RenderTexture, destination : RenderTexture)
	{	
		// needed for most of the new and improved image FX
		CreateMaterials ();	
		
		// get render targets	
		var color : RenderTexture = RenderTexture.GetTemporary(source.width, source.height, 0);	
		var halfRezColor : RenderTexture = RenderTexture.GetTemporary(source.width / 2.0, source.height / 2.0, 0);		
		var quarterRezColor : RenderTexture = RenderTexture.GetTemporary(source.width / 4.0, source.height / 4.0, 0);	
		var secondQuarterRezColor : RenderTexture = RenderTexture.GetTemporary(source.width / 4.0, source.height / 4.0, 0);	
		
		// do the downsample and blur
		Graphics.Blit (source, halfRezColor, _chromAberrationMaterial, 0);
		Graphics.Blit (halfRezColor, quarterRezColor);	
				
		// blur the result to get a nicer bloom radius
		for (var it : int = 0; it < 2; it++ ) {
			_separableBlurMaterial.SetVector ("offsets", Vector4 (0.0, (1.5 * 1.0) / quarterRezColor.height, 0.0, 0.0));	
			Graphics.Blit (quarterRezColor, secondQuarterRezColor, _separableBlurMaterial); 
			_separableBlurMaterial.SetVector ("offsets", Vector4 ((1.5 * 1.0) / quarterRezColor.width, 0.0, 0.0, 0.0));	
			Graphics.Blit (secondQuarterRezColor, quarterRezColor, _separableBlurMaterial);		
		}		
		
		_vignetteMaterial.SetFloat ("vignetteIntensity", vignetteIntensity);
		_vignetteMaterial.SetFloat ("blurVignette", blurVignette);
		_vignetteMaterial.SetTexture ("_VignetteTex", quarterRezColor);
		Graphics.Blit(source, color,_vignetteMaterial); 				
		
		_chromAberrationMaterial.SetFloat ("chromaticAberrationIntensity", chromaticAberrationIntensity);
		Graphics.Blit (color, destination, _chromAberrationMaterial, 1);	
		
		RenderTexture.ReleaseTemporary (color);
		RenderTexture.ReleaseTemporary (halfRezColor);			
		RenderTexture.ReleaseTemporary (quarterRezColor);	
		RenderTexture.ReleaseTemporary (secondQuarterRezColor);	
	
	}

}
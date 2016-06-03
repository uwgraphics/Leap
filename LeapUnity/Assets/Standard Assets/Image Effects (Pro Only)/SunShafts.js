

@script ExecuteInEditMode
@script RequireComponent (Camera)
@script AddComponentMenu ("Image Effects/Sun Shafts")

enum SunShaftsResolution {
    Low = 0,
    Normal = 1,
	High = 2,
}
		
class SunShafts extends PostEffectsBase 
{	
	public var resolution : SunShaftsResolution;
	
	public var sunTransform : Transform;
	public var radialBlurIterations : int = 2;
	public var sunColor : Color = Color.white;
	public var sunShaftBlurRadius : float = 0.0164;
	public var sunShaftIntensity : float = 1.25;
	public var useSkyBoxAlpha : float = 0.75;
	
	public var maxRadius : float = 1.25;
	
	public var useDepthTexture : boolean = true;
	
	public var clearShader : Shader;
	private var _clearMaterial : Material;
	
	public var depthDecodeShader : Shader;
	private var _encodeDepthRGBA8Material : Material;
	
	public var depthBlurShader : Shader;
	private var _radialDepthBlurMaterial : Material;
	
	public var sunShaftsShader : Shader;
	private var _sunShaftsMaterial : Material;	
	
	public var simpleClearShader : Shader;
	private var _simpleClearMaterial : Material;
	
	public var compShader : Shader;
	private var _compMaterial : Material;

	
	function CreateMaterials () 
	{			
		_clearMaterial = CheckShaderAndCreateMaterial(clearShader,_clearMaterial);
		_sunShaftsMaterial = CheckShaderAndCreateMaterial(sunShaftsShader,_sunShaftsMaterial);
		_encodeDepthRGBA8Material = CheckShaderAndCreateMaterial(depthDecodeShader,_encodeDepthRGBA8Material);
		_radialDepthBlurMaterial = CheckShaderAndCreateMaterial(depthBlurShader,_radialDepthBlurMaterial);
		_simpleClearMaterial = CheckShaderAndCreateMaterial(simpleClearShader,_simpleClearMaterial);
		_compMaterial = CheckShaderAndCreateMaterial(compShader,_compMaterial);
	}
	
	function Start () 
	{		
		CreateMaterials();	
		CheckSupport(useDepthTexture);
		
		if(useDepthTexture) { 
			GetComponent.<Camera>().depthTextureMode |= DepthTextureMode.Depth;	
		}
	}
	
	function OnRenderImage (source : RenderTexture, destination : RenderTexture)
	{	
		CreateMaterials ();	
		
        var divider : float = 4.0;
        if(resolution == SunShaftsResolution.Normal)
            divider = 2.0;
        if(resolution == SunShaftsResolution.High)
            divider = 1.0;
			
		// get render targets		
		var secondQuarterRezColor : RenderTexture = RenderTexture.GetTemporary(source.width / divider, source.height / divider, 0);	
        var lrDepthBuffer : RenderTexture = RenderTexture.GetTemporary(source.width / divider, source.height / divider, 0);
		
		// save the color buffer
		Graphics.Blit (source, destination); 
		
		// mask skybox (some pixels are clip()'ped, others are kept ...)
		if(!useDepthTexture) {
			var tmpBuffer : RenderTexture = RenderTexture.GetTemporary(source.width, source.height, 0);	
			
			RenderTexture.active = tmpBuffer;
			GL.ClearWithSkybox(false, GetComponent.<Camera>());
			
			_compMaterial.SetTexture("_Skybox", tmpBuffer);
			Graphics.Blit (source, source, _compMaterial);
			
			RenderTexture.ReleaseTemporary(tmpBuffer);
		}
		else
			Graphics.Blit (source, source, _clearMaterial); // don't care about source :-)

		// get depth values
		
        _encodeDepthRGBA8Material.SetFloat("noSkyBoxMask", 1.0-useSkyBoxAlpha);
		_encodeDepthRGBA8Material.SetFloat("dontUseSkyboxBrightness", 0.0);		
		Graphics.Blit (source, lrDepthBuffer, _encodeDepthRGBA8Material);
		
        // black small pixel border to get rid of clamping annoyances
        
		DrawBorder(lrDepthBuffer,_simpleClearMaterial);
		
		var v : Vector3 = Vector3.one * 0.5;
		if (sunTransform)
			v = GetComponent.<Camera>().WorldToViewportPoint (sunTransform.position);
		else {
			v = Vector3(0.5, 0.5, 0.0);
		}
        			
		// radial depth blur now
		_radialDepthBlurMaterial.SetVector ("blurRadius4", Vector4 (1.0, 1.0, 0.0, 0.0) * sunShaftBlurRadius );
		_radialDepthBlurMaterial.SetVector ("sunPosition", Vector4 (v.x, v.y, v.z, maxRadius));
				
		if (radialBlurIterations<1)
			radialBlurIterations = 1;
				
		for (var it2 : int = 0; it2 < radialBlurIterations; it2++ ) {
			Graphics.Blit (lrDepthBuffer, secondQuarterRezColor, _radialDepthBlurMaterial);
			Graphics.Blit (secondQuarterRezColor, lrDepthBuffer, _radialDepthBlurMaterial);		
		}
		
		// composite now			
		_sunShaftsMaterial.SetFloat ("sunShaftIntensity", sunShaftIntensity);
		if (v.z >= 0.0)
			_sunShaftsMaterial.SetVector ("sunColor", Vector4 (sunColor.r,sunColor.g,sunColor.b,sunColor.a));
		else
			_sunShaftsMaterial.SetVector ("sunColor", Vector4 (0.0,0.0,0.0,0.0)); // no backprojection !
				
		_sunShaftsMaterial.SetTexture("_ColorBuffer", source);
		Graphics.Blit(lrDepthBuffer, destination, _sunShaftsMaterial); 	

		
		RenderTexture.ReleaseTemporary (lrDepthBuffer);	
		RenderTexture.ReleaseTemporary (secondQuarterRezColor);	
	}

}
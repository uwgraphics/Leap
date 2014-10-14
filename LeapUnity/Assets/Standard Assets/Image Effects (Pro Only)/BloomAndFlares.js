
@script ExecuteInEditMode
@script RequireComponent (Camera)
@script AddComponentMenu ("Image Effects/Bloom and Flares")

enum LensflareStyle {
	Ghosting = 0,
	Hollywood = 1,
	Combined = 2,
}

enum TweakMode {
	Simple = 0,
	Advanced = 1,
}
				
class BloomAndFlares extends PostEffectsBase 
{
	public var tweakMode : TweakMode = 1;
	
	public var bloomThisTag : String;
	
	public var sepBlurSpread : float = 1.5;
	public var useSrcAlphaAsMask : float = 0.5;
	
	public var bloomIntensity : float = 1.0;
	public var bloomThreshhold : float = 0.4;
	public var bloomBlurIterations : int = 3;	
		
	public var lensflares : boolean = true;
	public var hollywoodFlareBlurIterations : int = 4;
	public var lensflareMode : LensflareStyle = 0;
	public var hollyStretchWidth : float = 2.5;
	public var lensflareIntensity : float = 0.75;
	public var lensflareThreshhold : float = 0.5;
	public var flareColorA : Color = Color(0.4,0.4,0.8,0.75);
	public var flareColorB : Color = Color(0.4,0.8,0.8,0.75);
	public var flareColorC : Color = Color(0.8,0.4,0.8,0.75);
	public var flareColorD : Color = Color(0.8,0.4,0.0,0.75);
	public var blurWidth : float = 1.0;	
	
	// needed shaders & materials ...
	
	public var addAlphaHackShader : Shader;
	
	private var _alphaAddMaterial : Material;
	
	public var lensFlareShader : Shader; 
	private var _lensFlareMaterial : Material;
	
	public var vignetteShader : Shader;
	private var _vignetteMaterial : Material;
	
	public var separableBlurShader : Shader;
	private var _separableBlurMaterial : Material;
	
	public var addBrightStuffOneOneShader: Shader;
	private var _addBrightStuffBlendOneOneMaterial : Material;
	
	public var hollywoodFlareBlurShader: Shader;
	private var _hollywoodFlareBlurMaterial : Material;
	
	public var hollywoodFlareStretchShader: Shader;	
	private var _hollywoodFlareStretchMaterial : Material;	
	
	public var brightPassFilterShader : Shader;
	private var _brightPassFilterMaterial : Material;
	
	
	function Start () 
	{
		CreateMaterials ();	
		CheckSupport(false);
	}
	
	// @TODO group shaders into material passes
	function CreateMaterials () 
	{
		_lensFlareMaterial = CheckShaderAndCreateMaterial(lensFlareShader,_lensFlareMaterial);
		_vignetteMaterial = CheckShaderAndCreateMaterial(vignetteShader,_vignetteMaterial);
		_separableBlurMaterial = CheckShaderAndCreateMaterial(separableBlurShader,_separableBlurMaterial);
		_addBrightStuffBlendOneOneMaterial = CheckShaderAndCreateMaterial(addBrightStuffOneOneShader,_addBrightStuffBlendOneOneMaterial);
		_hollywoodFlareBlurMaterial = CheckShaderAndCreateMaterial(hollywoodFlareBlurShader,_hollywoodFlareBlurMaterial);
		_hollywoodFlareStretchMaterial = CheckShaderAndCreateMaterial(hollywoodFlareStretchShader,_hollywoodFlareStretchMaterial);
		_brightPassFilterMaterial = CheckShaderAndCreateMaterial(brightPassFilterShader,_brightPassFilterMaterial);
		_alphaAddMaterial = CheckShaderAndCreateMaterial(addAlphaHackShader,_alphaAddMaterial);			
	}
	
	function OnRenderImage (source : RenderTexture, destination : RenderTexture)
	{			
		CreateMaterials ();	
		
		// some objects should ignore the alpha threshhold limit,
		// so draw .a = 1 into the color buffer for those ...
		// 
		// the drawing is scheduled here
		if(bloomThisTag && bloomThisTag != "Untagged") {
			var gos : GameObject[] = GameObject.FindGameObjectsWithTag(bloomThisTag);
			for (var go : GameObject in gos) {
				if(go.GetComponent(MeshFilter)) {
					var mesh : Mesh = (go.GetComponent(MeshFilter) as MeshFilter).sharedMesh;
					_alphaAddMaterial.SetPass(0);
					Graphics.DrawMeshNow(mesh,go.transform.localToWorldMatrix);
				}
			}		
		}		
		
		var halfRezColor : RenderTexture = RenderTexture.GetTemporary(source.width / 2.0, source.height / 2.0, 0);			
		var quarterRezColor : RenderTexture = RenderTexture.GetTemporary(source.width / 4.0, source.height / 4.0, 0);	
		var secondQuarterRezColor : RenderTexture = RenderTexture.GetTemporary(source.width / 4.0, source.height / 4.0, 0);	
		var thirdQuarterRezColor : RenderTexture = RenderTexture.GetTemporary(source.width / 4.0, source.height / 4.0, 0);	
		
		// at this point, we have massaged the alpha channel enough to start downsampling process for bloom	
		Graphics.Blit (source, halfRezColor);
		Graphics.Blit (halfRezColor, quarterRezColor);		
		
		RenderTexture.ReleaseTemporary (halfRezColor);			

		// cut colors (threshholding)			
		_brightPassFilterMaterial.SetVector ("threshhold", Vector4 (bloomThreshhold, 1.0/(1.0-bloomThreshhold), 0.0, 0.0));
		_brightPassFilterMaterial.SetFloat ("useSrcAlphaAsMask", useSrcAlphaAsMask);
		Graphics.Blit (quarterRezColor, secondQuarterRezColor, _brightPassFilterMaterial);		
				
		// blurring
		if (bloomBlurIterations < 1)
			bloomBlurIterations = 1;	
				
        Graphics.Blit(secondQuarterRezColor, quarterRezColor);
		for (var iter : int = 0; iter < bloomBlurIterations; iter++ ) {
			_separableBlurMaterial.SetVector ("offsets", Vector4 (0.0, (sepBlurSpread * 1.0) / quarterRezColor.height, 0.0, 0.0));	
			Graphics.Blit (quarterRezColor, thirdQuarterRezColor, _separableBlurMaterial); 
			_separableBlurMaterial.SetVector ("offsets", Vector4 ((sepBlurSpread * 1.0) / quarterRezColor.width, 0.0, 0.0, 0.0));	
			Graphics.Blit (thirdQuarterRezColor, quarterRezColor, _separableBlurMaterial);		
		}

		Graphics.Blit (source, destination);

		if (lensflares) 
		{	
			// lens flare fun: cut some additional values 
			// (yes, they will be cut on top of the already cut bloom values, 
			//  so just optimize away if not really needed)
			_brightPassFilterMaterial.SetVector ("threshhold", Vector4 (lensflareThreshhold, 1.0/(1.0-lensflareThreshhold), 0.0, 0.0));
			_brightPassFilterMaterial.SetFloat ("useSrcAlphaAsMask", 0.0);
			Graphics.Blit (secondQuarterRezColor, thirdQuarterRezColor, _brightPassFilterMaterial); 				
			
			if(lensflareMode == 0) // ghosting
			{
				// smooth out a little
				_separableBlurMaterial.SetVector ("offsets", Vector4 (0.0, (sepBlurSpread*1.0)/quarterRezColor.height, 0.0, 0.0));	
				Graphics.Blit (thirdQuarterRezColor, secondQuarterRezColor, _separableBlurMaterial);				
				_separableBlurMaterial.SetVector ("offsets", Vector4 ((sepBlurSpread*1.0)/quarterRezColor.width, 0.0, 0.0, 0.0));	
				Graphics.Blit (secondQuarterRezColor, thirdQuarterRezColor, _separableBlurMaterial); 
				
				// vignette for lens flares so that we don't notice any hard edges
				_vignetteMaterial.SetFloat ("vignetteIntensity", 0.975);
				Graphics.Blit (thirdQuarterRezColor, secondQuarterRezColor, _vignetteMaterial); 
				
				// generating flares (_lensFlareMaterial has One One Blend)
				_lensFlareMaterial.SetVector ("colorA", Vector4(flareColorA.r,flareColorA.g,flareColorA.b,flareColorA.a) * lensflareIntensity);
				_lensFlareMaterial.SetVector ("colorB", Vector4(flareColorB.r,flareColorB.g,flareColorB.b,flareColorB.a) * lensflareIntensity);
				_lensFlareMaterial.SetVector ("colorC", Vector4(flareColorC.r,flareColorC.g,flareColorC.b,flareColorC.a) * lensflareIntensity);
				_lensFlareMaterial.SetVector ("colorD", Vector4(flareColorD.r,flareColorD.g,flareColorD.b,flareColorD.a) * lensflareIntensity);
				Graphics.Blit (secondQuarterRezColor, quarterRezColor, _lensFlareMaterial);					
			
			}				
			else 
			{
				_hollywoodFlareBlurMaterial.SetVector ("offsets", Vector4(0.0, (sepBlurSpread * 1.0) / quarterRezColor.height, 0.0, 0.0));	
				_hollywoodFlareBlurMaterial.SetTexture("_NonBlurredTex", quarterRezColor);
				_hollywoodFlareBlurMaterial.SetVector ("tintColor", Vector4(flareColorA.r,flareColorA.g,flareColorA.b,flareColorA.a) * flareColorA.a * lensflareIntensity);
				Graphics.Blit (thirdQuarterRezColor, secondQuarterRezColor, _hollywoodFlareBlurMaterial); 	
				
				_hollywoodFlareStretchMaterial.SetVector ("offsets", Vector4 ((sepBlurSpread * 1.0) / quarterRezColor.width, 0.0, 0.0, 0.0));	
				_hollywoodFlareStretchMaterial.SetFloat("stretchWidth", hollyStretchWidth);
				Graphics.Blit (secondQuarterRezColor, thirdQuarterRezColor, _hollywoodFlareStretchMaterial);	
								
				if(lensflareMode == 1) // hollywood flares
				{															
					for (var itera : int = 0; itera < hollywoodFlareBlurIterations; itera++ ) {
						_separableBlurMaterial.SetVector ("offsets", Vector4 ((sepBlurSpread * 1.0) / quarterRezColor.width, 0.0, 0.0, 0.0));	
						Graphics.Blit (thirdQuarterRezColor, secondQuarterRezColor, _separableBlurMaterial);
						_separableBlurMaterial.SetVector ("offsets", Vector4 ((sepBlurSpread * 1.0) / quarterRezColor.width, 0.0, 0.0, 0.0));	
						Graphics.Blit (secondQuarterRezColor, thirdQuarterRezColor, _separableBlurMaterial); 						
					}		
								
					_addBrightStuffBlendOneOneMaterial.SetFloat ("intensity", 1.0);
					Graphics.Blit (thirdQuarterRezColor, quarterRezColor, _addBrightStuffBlendOneOneMaterial); 
				}  
				else // 'both' (@NOTE: is weird, maybe just remove)
				{													
					for (var ix : int = 0; ix < hollywoodFlareBlurIterations; ix++ ) {
						_separableBlurMaterial.SetVector ("offsets", Vector4 ((sepBlurSpread * 1.0) / quarterRezColor.width, 0.0, 0.0, 0.0));	
						Graphics.Blit (thirdQuarterRezColor, secondQuarterRezColor, _separableBlurMaterial); 
						_separableBlurMaterial.SetVector ("offsets", Vector4 ((sepBlurSpread * 1.0) / quarterRezColor.width, 0.0, 0.0, 0.0));	
						Graphics.Blit (secondQuarterRezColor, thirdQuarterRezColor, _separableBlurMaterial); 							
					}		
				
					// vignette for lens flares
					_vignetteMaterial.SetFloat ("vignetteIntensity", 1.0);
					Graphics.Blit (thirdQuarterRezColor, secondQuarterRezColor, _vignetteMaterial); 
				
					// creating the flares
					// _lensFlareMaterial has One One Blend
					_lensFlareMaterial.SetVector ("colorA", Vector4(flareColorA.r,flareColorA.g,flareColorA.b,flareColorA.a) * flareColorA.a * lensflareIntensity);
					_lensFlareMaterial.SetVector ("colorB", Vector4(flareColorB.r,flareColorB.g,flareColorB.b,flareColorB.a) * flareColorB.a * lensflareIntensity);
					_lensFlareMaterial.SetVector ("colorC", Vector4(flareColorC.r,flareColorC.g,flareColorC.b,flareColorC.a) * flareColorC.a * lensflareIntensity);
					_lensFlareMaterial.SetVector ("colorD", Vector4(flareColorD.r,flareColorD.g,flareColorD.b,flareColorD.a) * flareColorD.a * lensflareIntensity);
					Graphics.Blit (secondQuarterRezColor, thirdQuarterRezColor, _lensFlareMaterial);		
				
					_addBrightStuffBlendOneOneMaterial.SetFloat ("intensity", 1.0);
					Graphics.Blit (thirdQuarterRezColor, quarterRezColor, _addBrightStuffBlendOneOneMaterial); 
				}																						
			}
		}		
		
		_addBrightStuffBlendOneOneMaterial.SetFloat("intensity", bloomIntensity);
		Graphics.Blit (quarterRezColor, destination, _addBrightStuffBlendOneOneMaterial);		
		
		RenderTexture.ReleaseTemporary (quarterRezColor);	
		RenderTexture.ReleaseTemporary (secondQuarterRezColor);	
		RenderTexture.ReleaseTemporary (thirdQuarterRezColor);		
	}

}
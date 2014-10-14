
@script ExecuteInEditMode
@script RequireComponent (Camera)
@script AddComponentMenu ("Image Effects/Depth of Field") 

enum DofQualitySetting {
	Low = 0,
	Medium = 1,
	High = 2,
}

class DepthOfField extends PostEffectsBase {
	
	public var quality : DofQualitySetting = DofQualitySetting.High;
	public var divider : float = 2.0;
	
	public var focalZDistance : float = 0.0;
	
	public var focalStartCurve : float = 1.175;
	public var focalEndCurve : float = 1.1;
	
	public var focalZStart : float = 0.0;
	public var focalZEnd : float = 10000.0;
	
	private var _focalDistance01 : float = 0.1;
	private var _focalStart01 : float = 0.0;
	private var _focalEnd01 : float = 1.0;
	
	public var focalFalloff : float = 1.0;
	
	public var objectFocus : Transform = null;
	public var focalSize : float = 0.075;
	
	public var enableBokeh : boolean = true;
	public var bokehThreshhold : float = 0.2;
	public var bokehFalloff : float = 0.2;
	public var noiseAmount : float = 1.5;
	
	public var blurIterations : int = 1;
	public var blurSpread : float = 1.35;
	
	public var foregroundBlurIterations : int = 1;
	public var foregroundBlurSpread : float = 1.0;
	public var foregroundBlurWeight : float = 1.0;
			
	public var weightedBlurShader : Shader;
	private var _weightedBlurMaterial : Material = null;	
	
	public var preDofShader : Shader;
	private var _preDofMaterial : Material = null;
    
    public var blurShader : Shader;
    private var _blurMaterial : Material = null;
	
	function CreateMaterials () {		
		_weightedBlurMaterial = CheckShaderAndCreateMaterial(weightedBlurShader,_weightedBlurMaterial);
		_blurMaterial = CheckShaderAndCreateMaterial(blurShader,_blurMaterial);
		_preDofMaterial = CheckShaderAndCreateMaterial(preDofShader,_preDofMaterial);             
	}
	
	function Start () {
		CreateMaterials();
		CheckSupport(true);
	}

	function OnEnable() {
		camera.depthTextureMode |= DepthTextureMode.Depth;		
	}
	
	function OnRenderImage (source : RenderTexture, destination : RenderTexture) {	
		// create materials if needed
		CreateMaterials ();	
		
		source.filterMode = FilterMode.Bilinear;
		// source.filterMode = FilterMode.Point;
      
        // determine area of focus   
		if(objectFocus) {
			var vpPoint = camera.WorldToViewportPoint(objectFocus.position);
			vpPoint.z = (vpPoint.z) / (camera.farClipPlane);
			_focalDistance01 = vpPoint.z;			
		} else {
			_focalDistance01 = camera.WorldToViewportPoint(focalZDistance * camera.transform.forward + camera.transform.position).z / (camera.farClipPlane);	
		}
		
		if(focalZEnd > camera.farClipPlane)
			focalZEnd = camera.farClipPlane;

		_focalStart01 = camera.WorldToViewportPoint(focalZStart * camera.transform.forward + camera.transform.position).z / (camera.farClipPlane);
		_focalEnd01 = camera.WorldToViewportPoint(focalZEnd * camera.transform.forward + camera.transform.position).z / (camera.farClipPlane);
		
		if(_focalDistance01 < _focalStart01)
			_focalDistance01 = _focalStart01+Mathf.Epsilon;	

		if(_focalEnd01<_focalStart01)
			_focalEnd01 = _focalStart01+Mathf.Epsilon;		
						
		
		// NOTE:
        //  we use the alpha channel for storing the COC which also means that
        //  unfortunately, alpha based image effects such as sun shafts, bloom or glow
        //  might not work as expected if placed *after* this image effect 
        		
		_preDofMaterial.SetFloat("focalDistance01", _focalDistance01);
		_preDofMaterial.SetFloat("focalFalloff", focalFalloff);
        _preDofMaterial.SetFloat("focalStart01",_focalStart01);
        _preDofMaterial.SetFloat("focalEnd01",_focalEnd01);
        _preDofMaterial.SetFloat("focalSize", focalSize * 0.5);        
		_preDofMaterial.SetFloat("_ForegroundBlurWeight", foregroundBlurWeight);
		_preDofMaterial.SetVector("_CurveParams", Vector4(focalStartCurve,focalEndCurve,0.0,0.0));
		var fgBokehFalloff : float = -bokehFalloff/(1.0*foregroundBlurIterations);
		_preDofMaterial.SetVector("_BokehThreshhold", Vector4(bokehThreshhold, (1.0/(1.0-bokehThreshhold)) * (1.0-fgBokehFalloff), fgBokehFalloff,noiseAmount));
		_preDofMaterial.SetVector("_InvRenderTargetSize", Vector4(1.0/(1.0*source.width),1.0/(1.0*source.height),0.0,0.0));
		
        var fgSource : RenderTexture = RenderTexture.GetTemporary (source.width, source.height, 0); 
		var oneEightUnblurredBg : RenderTexture = RenderTexture.GetTemporary (source.width/divider, source.height/divider, 0);         
        var oneEight : RenderTexture = RenderTexture.GetTemporary (source.width/divider, source.height/divider, 0); 
        var oneEight2 : RenderTexture = RenderTexture.GetTemporary (source.width/divider, source.height/divider, 0); 
        var oneEightTmp : RenderTexture = RenderTexture.GetTemporary (source.width/divider, source.height/divider, 0); 
        
        fgSource.filterMode = FilterMode.Bilinear;        
        oneEight.filterMode = FilterMode.Bilinear;
        oneEight2.filterMode = FilterMode.Bilinear;
        oneEightTmp.filterMode = FilterMode.Bilinear;
        oneEightUnblurredBg.filterMode = FilterMode.Bilinear;
		
		if(quality >= DofQualitySetting.High) { 
			// COC (foreground)
			Graphics.Blit(source, fgSource, _preDofMaterial, 11); 
			
			// better downsample (shouldn't be weighted)
			Graphics.Blit(fgSource, oneEight, _preDofMaterial, 12);		
			
			// foreground defocus
			if(foregroundBlurIterations<1) foregroundBlurIterations = 1;		
			
			var fgBlurPass : int = enableBokeh ? 9 : 6;
												
	        for(it33 = 0; it33 < foregroundBlurIterations; it33++) {
	        	_preDofMaterial.SetVector("_Vh", Vector4(foregroundBlurSpread,0.0,0,0));
				Graphics.Blit(oneEight, oneEightTmp, _preDofMaterial, fgBlurPass);
				_preDofMaterial.SetVector("_Vh", Vector4(0.0,foregroundBlurSpread,0,0));
				Graphics.Blit(oneEightTmp, oneEight, _preDofMaterial, fgBlurPass);
	        	
	        	if(enableBokeh) {
	        		_preDofMaterial.SetVector("_Vh", Vector4(foregroundBlurSpread,-foregroundBlurSpread,0,0));
					Graphics.Blit(oneEight, oneEightTmp, _preDofMaterial, fgBlurPass);
					_preDofMaterial.SetVector("_Vh", Vector4(-foregroundBlurSpread,-foregroundBlurSpread,0,0));
					Graphics.Blit(oneEightTmp, oneEight, _preDofMaterial, fgBlurPass);	
	        	}			
			} 	
	        // COC (background), where is my MRT!?
	       	Graphics.Blit(source, source, _preDofMaterial, 4);
	       		
	       	// better downsample
	       	Graphics.Blit(source, oneEightUnblurredBg, _preDofMaterial, 12);	
		} 
		else // medium & low quality
		{
			// calculate COC for BG & FG at the same time
			Graphics.Blit(source, source, _preDofMaterial, 3); 
				
	       	// better downsample (should actually be weighted)
	       	Graphics.Blit(source, oneEightUnblurredBg, _preDofMaterial, 12);				
		}
       	
       	if(blurIterations<1) blurIterations = 1;
       	
       	var bgBokehFalloff : float = -bokehFalloff/(1.0*blurIterations);
       	_weightedBlurMaterial.SetVector("_Threshhold", Vector4(bokehThreshhold, (1.0/(1.0-bokehThreshhold)) * (1.0-bgBokehFalloff), bgBokehFalloff,noiseAmount));

		if(quality >= DofQualitySetting.Medium) 
		{	
			//  blur background a little 
			_weightedBlurMaterial.SetVector ("offsets", Vector4 (0.0, (blurSpread*1.5)/source.height, 0.0,0.0));
			Graphics.Blit ( oneEightUnblurredBg, oneEightTmp, _weightedBlurMaterial,1);
			_weightedBlurMaterial.SetVector ("offsets", Vector4 ((blurSpread*1.5)/source.width,  0.0,0.0,0.0));		
			Graphics.Blit (oneEightTmp, oneEightUnblurredBg, _weightedBlurMaterial,1);	 	
			
			var bgBlurPass : int = enableBokeh ? 0 : 1;		
			
			// blur and evtly bokeh'ify background
			for(it=0; it<blurIterations;it++) 
			{
				_weightedBlurMaterial.SetVector ("offsets", Vector4 (0.0, (blurSpread)/source.height, 0.0,0.0));
				Graphics.Blit ( it==0 ?  oneEightUnblurredBg : oneEight2, oneEightTmp, _weightedBlurMaterial,bgBlurPass);
				_weightedBlurMaterial.SetVector ("offsets", Vector4 ((blurSpread)/source.width,  0.0,0.0,0.0));		
				Graphics.Blit (oneEightTmp, oneEight2, _weightedBlurMaterial,bgBlurPass);	 
				
				if(enableBokeh) {
					_weightedBlurMaterial.SetVector ("offsets", Vector4 ((blurSpread)/source.width,  (blurSpread)/source.height,0.0,0.0));		
					Graphics.Blit (oneEight2, oneEightTmp, _weightedBlurMaterial,bgBlurPass);	
					_weightedBlurMaterial.SetVector ("offsets", Vector4 ((blurSpread)/source.width,  -(blurSpread)/source.height,0.0,0.0));		
					Graphics.Blit (oneEightTmp, oneEight2, _weightedBlurMaterial,bgBlurPass);	
				}
			}
			
			// @TODO: do noise properly as soon as we have nice MRT support
			
			/*
			if(enableNoise) {
				_weightedBlurMaterial.SetVector ("offsets", Vector4 (0.0, (blurSpread*1.5)/source.height, 0.0,0.0));
				Graphics.Blit (oneEight2, oneEightTmp, _weightedBlurMaterial,1);
				_weightedBlurMaterial.SetVector ("offsets", Vector4 ((blurSpread*1.5)/source.width,  0.0,0.0,0.0));		
				Graphics.Blit (oneEightTmp, oneEightUnsharp, _weightedBlurMaterial,1);		
			}
			*/
			//Graphics.Blit ( oneEight2, oneEightTmp, _weightedBlurMaterial,4);
			//Graphics.Blit ( oneEightTmp, oneEight2, _weightedBlurMaterial,4);
		} 
		else {
			
			// on low quality, we don't care about borders or bokeh's, let's just blur it and
			// hope for the best contrast =)
			
			for(it=0; it<blurIterations;it++) {
				_blurMaterial.SetVector ("offsets", Vector4 (0.0, (blurSpread)/source.height, 0.0,0.0));				
				Graphics.Blit ( (it == 0) ? oneEightUnblurredBg : oneEight2, oneEightTmp, _blurMaterial);
				_blurMaterial.SetVector ("offsets", Vector4 ((blurSpread)/source.width,  0.0,0.0,0.0));		
				Graphics.Blit (oneEightTmp, oneEight2, _blurMaterial);	
			}         
		}       	      
				
		// Almost done ... all we need to do now is to generate the very
		// final image based on defocused foreground and background as well
		// as the generated COC values
			
		var fgBlurNeeded : boolean = (_focalDistance01>0.0) && (focalStartCurve>0.0);		
		
		_preDofMaterial.SetTexture ("_FgLowRez", oneEight);
		_preDofMaterial.SetTexture ("_BgLowRez", oneEight2); // can also be fg *and* bg blur in the case of medium/low quality
		_preDofMaterial.SetTexture ("_BgUnblurredTex", oneEightUnblurredBg);
		
		_weightedBlurMaterial.SetTexture ("_TapLow", oneEight2);
		_weightedBlurMaterial.SetTexture ("_TapMedium", oneEightUnblurredBg);				
			
		// some final BG calculations can be performed in low resolution: do it now
		Graphics.Blit(oneEight2,oneEight2,_weightedBlurMaterial,3);
								
		// final BG calculations
		if(quality > DofQualitySetting.Medium) 
				Graphics.Blit(source, fgBlurNeeded ? fgSource : destination, _preDofMaterial, 0); 
		else if(quality == DofQualitySetting.Medium)
			Graphics.Blit(source, destination, _preDofMaterial, 2);
		else if(quality == DofQualitySetting.Low)
			Graphics.Blit(source, destination, _preDofMaterial, 1);
		
		// final FG calculations
		if(quality > DofQualitySetting.Medium  && fgBlurNeeded) 
		{
			Graphics.Blit(fgSource, oneEightUnblurredBg, _preDofMaterial, 12); 			
			Graphics.Blit(fgSource, destination, _preDofMaterial, 10);  // FG BLUR
		}
		
		RenderTexture.ReleaseTemporary(fgSource);
		RenderTexture.ReleaseTemporary(oneEight);
		RenderTexture.ReleaseTemporary(oneEight2);
		RenderTexture.ReleaseTemporary(oneEightTmp);
		RenderTexture.ReleaseTemporary(oneEightUnblurredBg);
	}	
}



@script ExecuteInEditMode
@script AddComponentMenu ("Image Effects/Color Correction (Curves)")

enum ColorCorrectionMode {
	Simple = 0,
	Advanced = 1	
}

class ColorCorrectionCurves extends PostEffectsBase 
{
	public var redChannel : AnimationCurve;
	public var greenChannel : AnimationCurve;
	public var blueChannel : AnimationCurve;
	
	public var useDepthCorrection : boolean = false;
	
	public var zCurve : AnimationCurve;
	public var depthRedChannel : AnimationCurve;
	public var depthGreenChannel : AnimationCurve;
	public var depthBlueChannel : AnimationCurve;
	
	private var _ccMaterial : Material;
	private var _ccDepthMaterial : Material;
	private var _selectiveCcMaterial : Material;
	
	private var _rgbChannelTex : Texture2D;
	private var _rgbDepthChannelTex : Texture2D;
	
	private var _zCurve : Texture2D;
	
	public var selectiveCc : boolean = false;
	
	public var selectiveFromColor : Color = Color.white;
	public var selectiveToColor : Color = Color.white;
	
	public var mode : ColorCorrectionMode;
	
	public var updateTextures : boolean = true;		
	
	// SHADERS
	
	public var colorCorrectionCurvesShader : Shader = null;
	public var simpleColorCorrectionCurvesShader : Shader = null;
	public var colorCorrectionSelectiveShader : Shader = null;
		
	function Start () {
		updateTextures = true;
		
		CreateMaterials ();		
		CheckSupport(true);
	}
	
	function CreateMaterials () {
		_ccMaterial = CheckShaderAndCreateMaterial(simpleColorCorrectionCurvesShader,_ccMaterial);
		_ccDepthMaterial = CheckShaderAndCreateMaterial(colorCorrectionCurvesShader,_ccDepthMaterial);
		_selectiveCcMaterial = CheckShaderAndCreateMaterial(colorCorrectionSelectiveShader,_selectiveCcMaterial);
		
		// sample all curves, create textures
		if (!_rgbChannelTex) {
			_rgbChannelTex = new Texture2D(256, 4, TextureFormat.ARGB32, false);
			_rgbChannelTex.hideFlags = HideFlags.HideAndDontSave;
		}
		if (!_rgbDepthChannelTex) {
			_rgbDepthChannelTex = new Texture2D(256, 4, TextureFormat.ARGB32, false);
			_rgbDepthChannelTex.hideFlags = HideFlags.HideAndDontSave;
		}
		
		if (!_zCurve) {
			_zCurve = new Texture2D (256, 1, TextureFormat.ARGB32, false);
			_zCurve.hideFlags = HideFlags.HideAndDontSave;
		}	
		
		_rgbChannelTex.wrapMode = TextureWrapMode.Clamp;
		_rgbDepthChannelTex.wrapMode = TextureWrapMode.Clamp;
		_zCurve.wrapMode = TextureWrapMode.Clamp;		
	}
	
	function OnEnable() {
		if(useDepthCorrection)
			camera.depthTextureMode |= DepthTextureMode.Depth;	
	}
	
	function OnDisable () {
	}
	
	public function UpdateParameters() 
	{			
		if (updateTextures && redChannel && greenChannel && blueChannel) 
		{
			for (var i : float = 0.0; i <= 1.0; i += 1.0/255.0) 
			{
					var rCh : float = Mathf.Clamp(redChannel.Evaluate(i), 0.0,1.0);
					var gCh : float = Mathf.Clamp(greenChannel.Evaluate(i), 0.0,1.0);
					var bCh : float = Mathf.Clamp(blueChannel.Evaluate(i), 0.0,1.0);
					
					_rgbChannelTex.SetPixel( Mathf.Floor(i*255.0), 0, Color(rCh,rCh,rCh) );
					_rgbChannelTex.SetPixel( Mathf.Floor(i*255.0), 1, Color(gCh,gCh,gCh) );
					_rgbChannelTex.SetPixel( Mathf.Floor(i*255.0), 2, Color(bCh,bCh,bCh) );
					
					var zC : float = Mathf.Clamp(zCurve.Evaluate(i), 0.0,1.0);
						
					_zCurve.SetPixel( Mathf.Floor(i*255.0), 0, Color(zC,zC,zC) );
				
					rCh = Mathf.Clamp(depthRedChannel.Evaluate(i), 0.0,1.0);
					gCh = Mathf.Clamp(depthGreenChannel.Evaluate(i), 0.0,1.0);
					bCh = Mathf.Clamp(depthBlueChannel.Evaluate(i), 0.0,1.0);
					
					_rgbDepthChannelTex.SetPixel( Mathf.Floor(i*255.0), 0, Color(rCh,rCh,rCh) );
					_rgbDepthChannelTex.SetPixel( Mathf.Floor(i*255.0), 1, Color(gCh,gCh,gCh) );
					_rgbDepthChannelTex.SetPixel( Mathf.Floor(i*255.0), 2, Color(bCh,bCh,bCh) );
			}
			
			_rgbChannelTex.Apply();
			_rgbDepthChannelTex.Apply();

			_zCurve.Apply();
				
			updateTextures = false;
		}
	}
	
	function OnRenderImage (source : RenderTexture, destination : RenderTexture)
	{
		CreateMaterials ();
		UpdateParameters();	
		
	if(useDepthCorrection)
		camera.depthTextureMode |= DepthTextureMode.Depth;			
		
		var renderTarget2Use : RenderTexture = destination;
		
		if (selectiveCc) {
			// we need an extra render target
			renderTarget2Use = RenderTexture.GetTemporary (source.width, source.height);
		}
		
		if (useDepthCorrection) 
		{
			_ccDepthMaterial.SetTexture ("_RgbTex", _rgbChannelTex);
	
			_ccDepthMaterial.SetTexture ("_ZCurve", _zCurve);
			
			_ccDepthMaterial.SetTexture ("_RgbDepthTex", _rgbDepthChannelTex);
	
			Graphics.Blit (source, renderTarget2Use, _ccDepthMaterial); 	
		} 
		else 
		{
			_ccMaterial.SetTexture ("_RgbTex", _rgbChannelTex);
	
			Graphics.Blit (source, renderTarget2Use, _ccMaterial); 			
		}
		
		if (selectiveCc) {
			_selectiveCcMaterial.SetVector ("selColor", Vector4(selectiveFromColor.r,selectiveFromColor.g,selectiveFromColor.b,selectiveFromColor.a) );
			_selectiveCcMaterial.SetVector ("targetColor", Vector4(selectiveToColor.r,selectiveToColor.g,selectiveToColor.b,selectiveToColor.a) );
			Graphics.Blit (renderTarget2Use, destination, _selectiveCcMaterial); 	
			
			RenderTexture.ReleaseTemporary (renderTarget2Use);
		}
				
	}

}
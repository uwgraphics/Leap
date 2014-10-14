

@script ExecuteInEditMode

@script RequireComponent(Camera)
@script AddComponentMenu("Image Effects/Contrast Enhance (Unsharp Mask)")

class ContrastEnhance extends PostEffectsBase {
	public var intensity : float = 0.5;
	public var threshhold : float = 0.0;
	
	private var _separableBlurMaterial : Material;
	private var _contrastCompositeMaterial : Material;
	
	public var blurSpread : float = 1.0;
	
	public var separableBlurShader : Shader = null;
	public var contrastCompositeShader : Shader = null;

	function CreateMaterials () 
	{
		_contrastCompositeMaterial = CheckShaderAndCreateMaterial(contrastCompositeShader,_contrastCompositeMaterial);
		_separableBlurMaterial = CheckShaderAndCreateMaterial(separableBlurShader,_separableBlurMaterial);
	}
	
	function Start () 
	{
		CreateMaterials ();
		CheckSupport(false); 
	}
	
	function OnRenderImage (source : RenderTexture, destination : RenderTexture)
	{	
		CreateMaterials ();
		
		var halfRezColor : RenderTexture = RenderTexture.GetTemporary(source.width / 2.0, source.height / 2.0, 0);	
		
		var quarterRezColor : RenderTexture = RenderTexture.GetTemporary(source.width / 4.0, source.height / 4.0, 0);	
		var secondQuarterRezColor : RenderTexture = RenderTexture.GetTemporary(source.width / 4.0, source.height / 4.0, 0);	
			
		// do the downsample and stuff
		Graphics.Blit (source, halfRezColor);
		Graphics.Blit (halfRezColor, quarterRezColor); 
	
		// blurring
		_separableBlurMaterial.SetVector ("offsets", Vector4 (0.0, (blurSpread * 1.0) / quarterRezColor.height, 0.0, 0.0));	
		Graphics.Blit (quarterRezColor, secondQuarterRezColor, _separableBlurMaterial);				
		_separableBlurMaterial.SetVector ("offsets", Vector4 ((blurSpread * 1.0) / quarterRezColor.width, 0.0, 0.0, 0.0));	
		Graphics.Blit (secondQuarterRezColor, quarterRezColor, _separableBlurMaterial); 
	
		// comp
		_contrastCompositeMaterial.SetTexture ("_MainTexBlurred", quarterRezColor);
		_contrastCompositeMaterial.SetFloat ("intensity", intensity);
		_contrastCompositeMaterial.SetFloat ("threshhold", threshhold);
		Graphics.Blit (source, destination, _contrastCompositeMaterial); 
		
		RenderTexture.ReleaseTemporary (halfRezColor);	
		RenderTexture.ReleaseTemporary (quarterRezColor);	
		RenderTexture.ReleaseTemporary (secondQuarterRezColor);			
	}
}
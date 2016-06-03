
@script ExecuteInEditMode

@script RequireComponent (Camera)
@script AddComponentMenu ("Image Effects/Crease")

class Crease extends PostEffectsBase {

	public var intensity : float = 0.5;
	public var softness : int = 1;
	public var spread : float = 1.0;
	
	public var blurShader : Shader;
	private var _blurMaterial : Material = null;	
	
	public var depthFetchShader : Shader;
	private var _depthFetchMaterial : Material = null;
	
	public var creaseApplyShader : Shader;
	private var _creaseApplyMaterial : Material = null;	
	
	function CreateMaterials () 
	{
		_blurMaterial = CheckShaderAndCreateMaterial(blurShader, _blurMaterial);
		_depthFetchMaterial = CheckShaderAndCreateMaterial(depthFetchShader,_depthFetchMaterial);
		_creaseApplyMaterial = CheckShaderAndCreateMaterial(creaseApplyShader,_creaseApplyMaterial);
	}
	
	function Start () 
	{
		CreateMaterials();
		CheckSupport(true);
	}
	
	function OnEnable() {
		GetComponent.<Camera>().depthTextureMode |= DepthTextureMode.Depth;	
	}

	function OnRenderImage (source : RenderTexture, destination : RenderTexture)
	{	
		CreateMaterials ();

		var hrTex : RenderTexture = RenderTexture.GetTemporary (source.width, source.height, 0); 
		var lrTex1 : RenderTexture = RenderTexture.GetTemporary (source.width / 2, source.height / 2, 0); 
		var lrTex2 : RenderTexture = RenderTexture.GetTemporary (source.width / 2, source.height / 2, 0); 
		
		Graphics.Blit(source,hrTex,_depthFetchMaterial);
		
		Graphics.Blit(hrTex,lrTex1);
		
		for(var i : int = 0; i < softness; i++) {
			_blurMaterial.SetVector ("offsets", Vector4 (0.0, (spread) / lrTex1.height, 0.0, 0.0));
			Graphics.Blit (lrTex1, lrTex2, _blurMaterial);
			_blurMaterial.SetVector ("offsets", Vector4 ((spread) / lrTex1.width,  0.0, 0.0, 0.0));		
			Graphics.Blit (lrTex2, lrTex1, _blurMaterial);
		}
		
		_creaseApplyMaterial.SetTexture("_HrDepthTex",hrTex);
		_creaseApplyMaterial.SetTexture("_LrDepthTex",lrTex1);
		_creaseApplyMaterial.SetFloat("intensity",intensity);
		Graphics.Blit(source,destination,_creaseApplyMaterial);	

		RenderTexture.ReleaseTemporary(hrTex);
		RenderTexture.ReleaseTemporary(lrTex1);
		RenderTexture.ReleaseTemporary(lrTex2);
	}	
}


@script ExecuteInEditMode
@script AddComponentMenu ("Image Effects/Fisheye")

class Fisheye extends PostEffectsBase {
	public var strengthX : float = 0.05;
	public var strengthY : float = 0.05;

	public var fishEyeShader : Shader = null;
	private var _fisheyeMaterial : Material = null;	
	
	function CreateMaterials () {
		_fisheyeMaterial = CheckShaderAndCreateMaterial(fishEyeShader,_fisheyeMaterial);
	}
	
	function Start () {
		CreateMaterials ();
		CheckSupport(false);
	}
	
	function OnRenderImage (source : RenderTexture, destination : RenderTexture)
	{		
		CreateMaterials ();
		
		var ar : float = (source.width * 1.0) / (source.height * 1.0);
		
		_fisheyeMaterial.SetVector ("intensity", Vector4 (strengthX * ar, strengthY * ar, strengthX * ar, strengthY * ar));
		Graphics.Blit (source, destination, _fisheyeMaterial); 	
	}
}
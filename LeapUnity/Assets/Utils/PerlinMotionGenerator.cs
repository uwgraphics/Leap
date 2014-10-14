using UnityEngine;
using System;
using System.Collections;

/// <summary>
/// Random motion generator employing Perlin noise. 
/// </summary>
[Serializable]
public class PerlinMotionGenerator
{
	public enum Axis
	{
		X,
		Y,
		Z,
		XY,
		XZ,
		YZ,
		All,
		None
	}
	
	public enum TransformType
	{
		Rotation,
		Translation,
		Morph,
		All,
		None
	}
	
	public enum PostFunction
	{
		None,
		Abs,
		NegAbs
	}
	
	[Serializable]
	public class RateMagPreset
	{
		public float rate;
		public float rotationMagnitude;
		public float translationMagnitude;
	}
	
	/// <summary>
	/// Maps Perlin noise to a bone or morph transformation
	/// to synthesize random motion.
	/// </summary>
	[Serializable]
	public class TransformMapping
	{
		/// <summary>
		/// If true, random transform offsets will be applied relative to
		/// agent pose in the current frame, rather than in the frame when
		/// the generator was started.
		/// </summary>
		public bool relativeToCurPose = false;
		
		/// <summary>
		/// Which transformations to apply to the bone.
		/// </summary>
		public TransformType transfTypes = TransformType.All;
		
		/// <summary>
		/// About which axis the bone should be rotated.
		/// </summary>
		public Axis rotationAxis = Axis.All;
		
		/// <summary>
		/// Along which axis the bone should be translated.
		/// </summary>
		public Axis translationAxis = Axis.None;
		
		/// <summary>
		/// How fast the random motion is.
		/// </summary>
		public float rate = 0.1f;
		
		/// <summary>
		/// Magnitude of the rotation motion (in degrees).
		/// </summary>
		public float rotationMagnitude = 5.0f;
		
		/// <summary>
		/// Magnitude of the translation motion (in degrees).
		/// </summary>
		public float translationMagnitude = 0.05f;
		
		/// <summary>
		/// Function applied to sampled Perlin noise. 
		/// </summary>
		public PostFunction postFunc = PostFunction.None;
		
		/// <summary>
		/// Affected bone. 
		/// </summary>
		public Transform bone;
		
		/// <summary>
		/// Base magnitude of the morphing motion (0-1). 
		/// </summary>
		public float baseMorphMagnitude = 0.1f;
		
		/// <summary>
		/// Magnitude of the morphing motion (0-1). 
		/// </summary>
		public float morphMagnitude = 0.1f;
		
		/// <summary>
		/// Affected morph channel. 
		/// </summary>
		public string[] morphChannels = new string[0];
		
		/// <summary>
		/// Predefined rate and magnitude preset 1. 
		/// </summary>
		public RateMagPreset rateMagPreset1 = new RateMagPreset();
		
		/// <summary>
		/// Predefined rate and magnitude preset 2. 
		/// </summary>
		public RateMagPreset rateMagPreset2 = new RateMagPreset();
		
		[HideInInspector]
		public float perlinParam = 0;
		[HideInInspector]
		public float perlinOffset = 0;
		[HideInInspector]
		public Quaternion srcRot;
		[HideInInspector]
		public Vector3 srcPos;
		[HideInInspector]
		public float srcRate;
		[HideInInspector]
		public float srcRotMag;
		[HideInInspector]
		public float srcTransMag;
		[HideInInspector]
		public float trgRate;
		[HideInInspector]
		public float trgRotMag;
		[HideInInspector]
		public float trgTransMag;
	}
	
	/// <summary>
	/// Mappings of Perlin noise to transforms. 
	/// </summary>
	public TransformMapping[] transforms;
	
	/// <summary>
	/// How long it takes for random motion to blend in (in seconds). 
	/// </summary>
	public float blendInTime = 1;
	
	/// <summary>
	/// How long it takes for motion rate and magnitude to change (in seconds). 
	/// </summary>
	public float rateMagTime = 0.5f;
	
	/// <summary>
	/// Change motion generator rate and magnitude settings to
	/// the ones defined in preset 1.
	/// </summary>
	public void GoToPreset1()
	{
		presetChange = true;
		presetChangeTime = 0;
		
		foreach( TransformMapping transf in transforms )
		{
			transf.srcRate = transf.rate;
			transf.trgRate = transf.rateMagPreset1.rate;
			transf.srcRotMag = transf.rotationMagnitude;
			transf.trgRotMag = transf.rateMagPreset1.rotationMagnitude;
			transf.srcTransMag = transf.translationMagnitude;
			transf.trgTransMag = transf.rateMagPreset1.translationMagnitude;
		}
	}
	
	/// <summary>
	/// Change motion generator rate and magnitude settings to
	/// the ones defined in preset 2.
	/// </summary>
	public void GoToPreset2()
	{
		presetChange = true;
		presetChangeTime = 0;
		
		foreach( TransformMapping transf in transforms )
		{
			transf.srcRate = transf.rate;
			transf.trgRate = transf.rateMagPreset2.rate;
			transf.srcRotMag = transf.rotationMagnitude;
			transf.trgRotMag = transf.rateMagPreset2.rotationMagnitude;
			transf.srcTransMag = transf.translationMagnitude;
			transf.trgTransMag = transf.rateMagPreset2.translationMagnitude;
		}
	}

	private PerlinNoise perlinRotGen = null;
	private PerlinNoise perlinTransGen = null;
	private PerlinNoise perlinMorphGen = null;
	private MorphController morphCtrl;
	private ModelController mdlCtrl;
	
	private bool running = false;
	private float motionWeight = 1;
	private bool presetChange = false;
	private float presetChangeTime;
	
	/// <summary>
	/// If true, the random motion generator is running. 
	/// </summary>
	public bool Running
	{
		get
		{
			return running;
		}
	}
	
	/// <summary>
	/// Constructor. 
	/// </summary>
	public PerlinMotionGenerator()
	{
	}
	
	/// <summary>
	/// Initializes the random motion generator. 
	/// </summary>
	public void Init( GameObject agent )
	{
		if( perlinRotGen == null )
		{
			// Initialize Perlin noise generator
			perlinRotGen = new PerlinNoise( Mathf.RoundToInt(Time.realtimeSinceStartup*10000f) );
			perlinTransGen = new PerlinNoise( Mathf.RoundToInt(Time.realtimeSinceStartup*20000f) );
			perlinMorphGen = new PerlinNoise( Mathf.RoundToInt(Time.realtimeSinceStartup*30000f) );
			morphCtrl = agent.GetComponent<MorphController>();
			mdlCtrl = agent.GetComponent<ModelController>();
		}
	}
	
	/// <summary>
	/// Starts up the random motion generator. 
	/// </summary>
	public void Start()
	{
		running = true;
		motionWeight = 0;
		
		// Start up the generator
		foreach( TransformMapping transf in transforms )
		{
			if( transf.bone == null )
				continue;
			
			transf.srcRot = mdlCtrl.GetPrevRotation(transf.bone);
			transf.srcPos = mdlCtrl.GetPrevPosition(transf.bone);
			transf.perlinParam = 0;
			transf.perlinOffset = UnityEngine.Random.Range(0, 10000f);
		}
	}
	
	/// <summary>
	/// Stops the random motion generator. 
	/// </summary>
	public void Stop()
	{
		running = false;
	}
	
	/// <summary>
	/// Updates the random motion. 
	/// </summary>
	public void Update()
	{
		// Compute correct blend weight for the generated motion
		if( motionWeight < 1 )
			motionWeight += Time.deltaTime/blendInTime;
		else
			motionWeight = 1;
		
		// Are we changing from one motion generator preset to another?
		float pct = 0;
		if(presetChange)
		{
			presetChangeTime += Time.deltaTime;
			if( presetChangeTime > rateMagTime )
				presetChange = false;
			
			pct = presetChangeTime/rateMagTime;
		}
		
		// Update each mapped transform
		foreach( TransformMapping transf in transforms )
		{
			if( transf.transfTypes == TransformType.None )
				continue;
			
			if(presetChange)
			{
				// Update motion rates and magnitudes
				transf.rate = transf.srcRate*(1f-pct) + transf.trgRate*pct;
				transf.rotationMagnitude = transf.srcRotMag*(1f-pct) + transf.trgRotMag*pct;
				transf.translationMagnitude = transf.srcTransMag*(1f-pct) + transf.trgTransMag*pct;
			}
			
			transf.perlinParam += Time.deltaTime*transf.rate;
		}
	}
	
	/// <summary>
	/// Applies the random morphing motion. 
	/// </summary>
	public void Apply()
	{
		if(!running)
			return;
		
		foreach( TransformMapping transf in transforms )
		{
			if( transf.transfTypes == TransformType.None )
				continue;
			
			float t = transf.perlinParam + transf.perlinOffset;
			
			if( ( transf.transfTypes == TransformType.Morph ||
			     transf.transfTypes == TransformType.All ) &&
			   morphCtrl != null )
			{
				// Compute random morph weight
				
				foreach( string mcname in transf.morphChannels )
				{
					MorphChannel mc = morphCtrl.GetMorphChannel(mcname);
					if( mc != null )
						mc.weight += ( (float)perlinMorphGen.Noise(t,0,0)/2f + transf.baseMorphMagnitude ) *
							transf.morphMagnitude * motionWeight;
				}
			}
		}
	}
	
	/// <summary>
	/// Applies the random motion of the bones. 
	/// </summary>
	public void LateApply()
	{
		if(!running)
			return;
		
		for( int tri = 0; tri < transforms.Length; ++tri )
		{
			TransformMapping transf = transforms[tri];
			if( transf.bone == null || transf.transfTypes == TransformType.None )
				continue;
			
			if( transf.transfTypes == TransformType.Rotation ||
			   transf.transfTypes == TransformType.All )
			{
				// Compute random rotation
				Vector3 drotv = SampleRotation(tri);
				Quaternion drotq = Quaternion.Euler(drotv);
				if(transf.relativeToCurPose)
					transf.bone.localRotation *= drotq;
				else
					transf.bone.localRotation = transf.srcRot*drotq;
			}
			
			if( transf.transfTypes == TransformType.Translation ||
			   transf.transfTypes == TransformType.All )
			{
				// Compute random translation
				Vector3 dpos = SampleTranslation(tri);
				if(transf.relativeToCurPose)
					transf.bone.localPosition += dpos;
				else
					transf.bone.localPosition = transf.srcPos+dpos;
			}
		}
	}
	
	/// <summary>
	/// Samples current rotation values for specified transform mapping. 
	/// </summary>
	/// <param name="transfIndex">
	/// Transform mapping index.
	/// </param>
	/// <returns>
	/// Rotation values, given as Euler angles.
	/// </returns>
	public Vector3 SampleRotation( int transfIndex )
	{
		TransformMapping transf = transforms[transfIndex];
		Vector3 vr;
		
		vr.z = transf.rotationAxis == Axis.Z ||
			transf.rotationAxis == Axis.XZ ||
				transf.rotationAxis == Axis.YZ ||
				transf.rotationAxis == Axis.All ?
				SampleNoise(transfIndex,0) * transf.rotationMagnitude * motionWeight : 0;
		vr.x = transf.rotationAxis == Axis.X ||
			transf.rotationAxis == Axis.XY ||
				transf.rotationAxis == Axis.XZ ||
				transf.rotationAxis == Axis.All ?
				SampleNoise(transfIndex,1) * transf.rotationMagnitude * motionWeight : 0;
		vr.y = transf.rotationAxis == Axis.Y ||
			transf.rotationAxis == Axis.XY ||
				transf.rotationAxis == Axis.YZ ||
				transf.rotationAxis == Axis.All ?
				SampleNoise(transfIndex,2) * transf.rotationMagnitude * motionWeight : 0;
		
		return vr;
	}
	
	/// <summary>
	/// Samples current translation values for specified transform mapping. 
	/// </summary>
	/// <param name="transfIndex">
	/// Transform mapping index.
	/// </param>
	/// <returns>
	/// Translation values, given as Euler angles.
	/// </returns>
	public Vector3 SampleTranslation( int transfIndex )
	{
		TransformMapping transf = transforms[transfIndex];
		Vector3 vt;
		
		vt.x = transf.translationAxis == Axis.X ||
			transf.translationAxis == Axis.XY ||
				transf.translationAxis == Axis.XZ ||
				transf.translationAxis == Axis.All ?
				SampleNoise(transfIndex,0) * transf.translationMagnitude * motionWeight : 0;
		vt.y = transf.translationAxis == Axis.Y ||
			transf.translationAxis == Axis.XY ||
				transf.translationAxis == Axis.YZ ||
				transf.translationAxis == Axis.All ?
				SampleNoise(transfIndex,1) * transf.translationMagnitude * motionWeight : 0;
		vt.z = transf.translationAxis == Axis.Z ||
			transf.translationAxis == Axis.XZ ||
				transf.translationAxis == Axis.YZ ||
				transf.translationAxis == Axis.All ?
				SampleNoise(transfIndex,2) * transf.translationMagnitude * motionWeight : 0;
		
		return vt;
	}
	
	/// <summary>
	/// Samples postprocessed Perlin noise for specified transform mapping.
	/// </summary>
	/// <param name="transfIndex">
	/// Transform mapping index.
	/// </param>
	/// <param name="dim">
	/// Perlin noise dimension (0, 1 or 2).
	/// </param>
	/// <returns>
	/// Noise value.
	/// </returns>
	public float SampleNoise( int transfIndex, int dim )
	{
		TransformMapping transf = transforms[transfIndex];
		float t = transf.perlinParam + transf.perlinOffset;
		float val = 0;
		if( dim == 0 )
			val = (float)perlinTransGen.Noise(t,0,0);
		else if( dim == 1 )
			val = (float)perlinTransGen.Noise(0,t,0);
		else
			val = (float)perlinTransGen.Noise(0,0,t);
		
		if( transf.postFunc == PostFunction.Abs )
			val = Mathf.Abs(val);
		else if( transf.postFunc == PostFunction.NegAbs )
			val = -Mathf.Abs(val);
		
		return val;
	}
}

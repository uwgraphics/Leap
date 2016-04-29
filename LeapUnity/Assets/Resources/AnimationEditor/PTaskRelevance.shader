Shader "EyeGazeInference/PTaskRelevance"
{
    Properties
	{
		_LEyePosition ("Left Eye Position", Vector) = (0, 0, 0, 1)
		_REyePosition ("Right Eye Position", Vector) = (0, 0, 0, 1)
		_TaskRelevance ("Task Relevance", Float) = 0
	}

	SubShader
	{
		Pass
		{
			Lighting Off Fog { Mode Off }
			Cull Off

			CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#pragma target 3.0
				#include "UnityCG.cginc"
				#include "Assets/Utils/ShaderUtil.cginc"

				float4 _LEyePosition;
				float4 _REyePosition;
				float _TaskRelevance;

				struct v2f
				{
					float4 pos: SV_POSITION;
					float3 worldPos : TEXCOORD0;
					float3 worldNormal : TEXCOORD1;
				};
			
				v2f vert (appdata_base v)
				{
					v2f o;
					o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
					o.worldPos = mul(_Object2World, v.vertex).xyz;
					o.worldNormal = mul(_Object2World, float4(v.normal, 0)).xyz;
					
					return o;
				}
			
				float4 frag(v2f IN) : COLOR
				{
					// Compute eye center and direction
					float3 eyeCenter = (0.5 * (_LEyePosition + _REyePosition)).xyz;
					float3 eyeDir = normalize(eyeCenter - IN.worldPos).xyz;
					float3 n = normalize(IN.worldNormal);

					// Compute probability
					float p = _TaskRelevance * clamp(abs(dot(eyeDir, n)), 0, 1); // TODO: abs needed to handle models that have normals pointing inward
					return float4(p, p, p, 1);
				}
			ENDCG
		}
	}
}
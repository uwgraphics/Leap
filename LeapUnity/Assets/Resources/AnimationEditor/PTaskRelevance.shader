Shader "EyeGazeInference/PTaskRelevance"
{
    Properties
	{
		_IsTaskRelevant ("Is Task Relevant", Int) = 0
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

				int _IsTaskRelevant;
			
				float4 vert (float4 v : POSITION) : POSITION
				{
					float4 pos = mul(UNITY_MATRIX_MVP, v);
					
					return pos;
				}
			
				float4 frag(float4 pos : SV_POSITION) : COLOR
				{
					float p = _IsTaskRelevant ? 1 : 0;

					return float4(p, p, p, p);
				}
			ENDCG
		}
	}
}
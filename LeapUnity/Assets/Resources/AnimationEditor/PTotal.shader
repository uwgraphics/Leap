Shader "EyeGazeInference/PTotal"
{
	Properties
	{
		_MainTex ("Render Input", 2D) = "black" {}
		_TexPGazeShiftDir ("PGazeShiftDirection", 2D) = "black" {}
		_TexPTaskRel ("PTaskRelevance", 2D) = "black" {}
		_TexPHandCon ("PHandContact", 2D) = "black" {}
		_PGazeShiftDirWeight ("PGazeShiftDirection Weight", Float) = 0.4
		_PTaskRelWeight ("PTaskRelevance Weight", Float) = 0.25
		_PHandConWeight ("PHandContact Weight", Float) = 0.35
	}

	SubShader
	{
		ZTest Always Cull Off ZWrite Off Fog { Mode Off }

		Pass
		{
			CGPROGRAM
				#pragma vertex vert_img
				#pragma fragment frag
				#pragma target 3.0
				#include "UnityCG.cginc"
			
				sampler2D _MainTex;
				sampler2D _TexPGazeShiftDir;
				sampler2D _TexPTaskRel;
				sampler2D _TexPHandCon;
				float _PGazeShiftDirWeight;
				float _PTaskRelWeight;
				float _PHandConWeight;
			
				float4 frag(v2f_img IN) : COLOR
				{
					float4 pGazeShiftDir = tex2D(_TexPGazeShiftDir, IN.uv);
					float4 pTaskRel = tex2D(_TexPTaskRel, IN.uv);
					float4 pHandCon = tex2D(_TexPHandCon, IN.uv);
					float4 p = _PGazeShiftDirWeight * pGazeShiftDir + _PTaskRelWeight * pTaskRel + _PHandConWeight * pHandCon;

					return float4(p.r, p.g, p.b, 1);
				}
			ENDCG
		}
	}
}
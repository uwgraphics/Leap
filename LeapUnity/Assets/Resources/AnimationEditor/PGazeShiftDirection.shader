Shader "EyeGazeInference/PGazeShiftDirection"
{
    Properties
	{
		_LEyePosition ("Left Eye Position", Vector) = (0, 0, 0, 1)
		_REyePosition ("Right Eye Position", Vector) = (0, 0, 0, 1)
		_LEyeDirectionAhead ("Left Eye Ahead Direction", Vector) = (1, 0, 0, 0)
		_REyeDirectionAhead ("Right Eye Ahead Direction", Vector) = (1, 0, 0, 0)
		_OMR ("OMR", float) = 45
		_EyePathStartPosition ("Eye Path Start Position", Vector) = (0, 0, 0, 1)
		_EyePathEndPosition ("Eye Path End Position", Vector) = (0, 0, 0, 1)
		_HeadAlignPropensity ("Head Align Propensity", float) = 0.5
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
				float4 _LEyeDirectionAhead;
				float4 _REyeDirectionAhead;
				float _OMR;
				float4 _EyePathStartPosition;
				float4 _EyePathEndPosition;
				float _HeadAlignPropensity;

				struct v2f
				{
					float4 pos : SV_POSITION;
					float4 worldPos : TEXCOORD0;
				};
 
				v2f vert (float4 v : POSITION)
				{
					v2f o;
					o.pos = mul(UNITY_MATRIX_MVP, v);
					o.worldPos = mul(_Object2World, v);

					return o;
				}
			
				float4 frag(v2f IN) : COLOR
				{
					// Get pixel world position
					float4 worldPos = IN.worldPos;

					// Adjust gaze path by head alignment propensity
					float4 headAlignPropensity = clamp(_HeadAlignPropensity, 0, 1);
					float4 startPos = headAlignPropensity < 0.5f ?
						lerp(_EyePathStartPosition, _EyePathEndPosition, 1.0f - headAlignPropensity/0.5f) :
						_EyePathStartPosition;
					float4 endPos = headAlignPropensity >= 0.5f ?
						lerp(_EyePathStartPosition, _EyePathEndPosition, 1.0f - (headAlignPropensity - 0.5f)/0.5f) :
						_EyePathEndPosition;
					
					// Compute proximity of the current point to gaze shift path
					float4 eyeCenter = 0.5 * (_LEyePosition + _REyePosition);
					float3 v = normalize((worldPos - eyeCenter).xyz);
					float3 vs = normalize((startPos - eyeCenter).xyz);
					float3 ve = normalize((endPos - eyeCenter).xyz);
					float3 vp = vs;
					if (dot(vs, ve) < 0.99995f)
					{
						float3 n = normalize(cross(vs, ve));
						vp = normalize(v - dot(v, n) * n);

						float dps = dot(vp, vs);
						float dpe = dot(vp, ve);
						if (dps < 0.99995f && dpe < 0.99995f)
						{
							float3 vps = normalize(cross(vp, vs));
							float3 vpe = normalize(cross(vp, ve));
							float dpse = dot(vps, vpe);

							// Constrain projected vector to the slice defined by vs and ve
							if (sign(dpse) > 0)
							{
								if (abs(dps) >= abs(dpe))
									vp = vs;
								else if (abs(dps) <= abs(dpe))
									vp = ve;
							}
						}
					}
					float angle = angleBetween(v, vp);

					// Compute probability
					float p = clamp(1.0f - angle / _OMR, 0, 1);
					return float4(p, p, p, 1);
				}
			ENDCG
		}
	}
}
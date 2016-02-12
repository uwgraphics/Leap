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

				struct v2f
				{
					float4 pos : POSITION;
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

					// Compute eye center, directions, and angles
					float4 eyeCenter = 0.5 * (_LEyePosition + _REyePosition);
					float4 lEyeDir = normalize(worldPos - _LEyePosition);
					float4 rEyeDir = normalize(worldPos - _REyePosition);
					float lEyeAngle = angleBetween(lEyeDir, _LEyeDirectionAhead);
					float rEyeAngle = angleBetween(rEyeDir, _REyeDirectionAhead);
					
					// Compute target probability based on gaze shift path
					float4 c = float4(0, 0, 0, 0);
					//if (lEyeAngle <= _OMR || rEyeAngle <= _OMR)
					{
						// Compute target probability based on proximity to gaze shift path
						float4 projWorldPos = projectPointOntoLineSegment(worldPos, _EyePathStartPosition, _EyePathEndPosition);
						float4 v = normalize(worldPos - eyeCenter);
						float4 vp = normalize(projWorldPos - eyeCenter);
						float angle = angleBetween(v, vp);
						float p = clamp(1 - angle / _OMR, 0 , 1);

						c = float4(p, p, p, p);
					}

					return c;
				}
			ENDCG
		}
	}
}
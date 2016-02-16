Shader "EyeGazeInference/RenderWorldPosition"
{
	Properties
	{
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

			float _RenderWorldPosScale;
			int _RenderWorldPosAxis; // 0 - x-axis, 1 - y-axis, 2 - z-axis
 
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
				float v = 0f;
				if (_RenderWorldPosAxis == 0)
					v = IN.worldPos.x;
				else if (_RenderWorldPosAxis == 1)
					v = IN.worldPos.y;
				else // if (_RenderWorldPosAxis == 2)
					v = IN.worldPos.z;
				v /= _RenderWorldPosScale;

				float4 c = EncodeFloatRGBA(v);
                return c;
            }
            ENDCG
        }
	} 
	FallBack "Diffuse"
}

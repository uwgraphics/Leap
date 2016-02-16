Shader "EyeGazeInference/RenderGameObjectID"
{
	Properties
	{
		_GameObjectID ("Game Object ID", Float) = 0
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

			float _GameObjectID;
 
            float4 vert (float4 v : POSITION) : POSITION
			{
                float4 pos = mul(UNITY_MATRIX_MVP, v);

                return pos;
            }

            float4 frag(float4 pos : SV_POSITION) : COLOR
			{
				return EncodeFloatRGBA(_GameObjectID);
            }
            ENDCG
        }
	} 
	FallBack "Diffuse"
}

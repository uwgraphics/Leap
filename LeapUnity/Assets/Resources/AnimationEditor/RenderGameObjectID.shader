Shader "EyeGazeInference/RenderGameObjectID"
{
	Properties
	{
		_GameObjectID ("Game Object ID", Int) = -1
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

			int _GameObjectID;
 
            float4 vert (float4 v : POSITION) : POSITION
			{
                float4 pos = mul(UNITY_MATRIX_MVP, v);

                return pos;
            }

            float frag(float4 pos : SV_POSITION) : COLOR
			{
				return (float)_GameObjectID;
            }
            ENDCG
        }
	} 
	FallBack "Diffuse"
}

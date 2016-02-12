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
				float4 c = float4(IN.worldPos.x, IN.worldPos.y, IN.worldPos.z, IN.worldPos.w); 
                return c;
            }
            ENDCG
        }
	} 
	FallBack "Diffuse"
}

// Simplified VertexLit shader, optimized for high-poly meshes. Differences from regular VertexLit one:
// - less per-vertex work compared with Mobile-VertexLit
// - supports only DIRECTIONAL lights and ambient term, saves some vertex processing power
// - no per-material color
// - no specular
// - no emission

Shader "Mobile/VertexLit (Only Directional Lights)" {
	Properties {
		_MainTex ("Base (RGB)", 2D) = "white" {}
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 80
		
	Pass {
		Name "FORWARD"
		Tags { "LightMode" = "ForwardBase" }
CGPROGRAM
#pragma vertex vert_surf
#pragma fragment frag_surf
#pragma fragmentoption ARB_precision_hint_fastest
#pragma multi_compile_fwdbase
#include "HLSLSupport.cginc"
#define UNITY_PASS_FORWARDBASE
#include "UnityCG.cginc"
#include "Lighting.cginc"
#include "AutoLight.cginc"

#define INTERNAL_DATA
#define WorldReflectionVector(data,normal) data.worldRefl
#define WorldNormalVector(data,normal) normal

		inline float3 LightingLambertVS (float3 normal, float3 lightDir)
		{
			fixed diff = max (0, dot (normal, lightDir));
			
			return _LightColor0.rgb * (diff * 2);
		}

		#pragma debug
		//#pragma surface surf Lambert

		sampler2D _MainTex;

		struct Input {
			float2 uv_MainTex;
		};

		void surf (Input IN, inout SurfaceOutput o) {
			half4 c = tex2D (_MainTex, IN.uv_MainTex);
			o.Albedo = c.rgb;
			o.Alpha = c.a;
		}
		struct v2f_surf {
  float4 pos : SV_POSITION;
  float2 pack0 : TEXCOORD0;
  #ifdef LIGHTMAP_OFF
  fixed3 normal : TEXCOORD1;
  #endif
  #ifndef LIGHTMAP_OFF
  float2 lmap : TEXCOORD2;
  #endif
  #ifdef LIGHTMAP_OFF
  fixed3 vlight : TEXCOORD2;
  #endif
  LIGHTING_COORDS(3,4)
};
#ifndef LIGHTMAP_OFF
float4 unity_LightmapST;
float4 unity_LightmapFade;
#endif
float4 _MainTex_ST;
v2f_surf vert_surf (appdata_full v) {
	v2f_surf o;
	o.pos = mul (UNITY_MATRIX_MVP, v.vertex);
	o.pack0.xy = TRANSFORM_TEX(v.texcoord, _MainTex);
	#ifndef LIGHTMAP_OFF
	o.lmap.xy = v.texcoord1.xy * unity_LightmapST.xy + unity_LightmapST.zw;
	#endif
	float3 worldN = mul((float3x3)_Object2World, SCALED_NORMAL);
	#ifdef LIGHTMAP_OFF
	o.normal = worldN;
	#endif
	#ifdef LIGHTMAP_OFF
	
	o.vlight = ShadeSH9 (float4(worldN,1.0));
	o.vlight += LightingLambertVS (worldN, _WorldSpaceLightPos0.xyz);
	
	#endif // LIGHTMAP_OFF
	TRANSFER_VERTEX_TO_FRAGMENT(o);
	return o;
}
#ifndef LIGHTMAP_OFF
sampler2D unity_Lightmap;
#endif
fixed4 frag_surf (v2f_surf IN) : COLOR {
	Input surfIN;
	surfIN.uv_MainTex = IN.pack0.xy;
	SurfaceOutput o;
	o.Albedo = 0.0;
	o.Emission = 0.0;
	o.Specular = 0.0;
	o.Alpha = 0.0;
	o.Gloss = 0.0;
	#ifdef LIGHTMAP_OFF
	o.Normal = IN.normal;
	#endif
	surf (surfIN, o);
	fixed atten = LIGHT_ATTENUATION(IN);
	fixed4 c = 0;
	#ifdef LIGHTMAP_OFF
	c.rgb = o.Albedo * IN.vlight * atten;
	#endif // LIGHTMAP_OFF
	#ifndef LIGHTMAP_OFF
	fixed3 lm = DecodeLightmap (tex2D(unity_Lightmap, IN.lmap.xy));
	#ifdef SHADOWS_SCREEN
	c.rgb += o.Albedo * min(lm, atten*2);
	#else
	c.rgb += o.Albedo * lm;
	#endif
	c.a = o.Alpha;
	#endif // !LIGHTMAP_OFF
	return c;
}

ENDCG
	}
}

FallBack "Mobile/VertexLit"
}
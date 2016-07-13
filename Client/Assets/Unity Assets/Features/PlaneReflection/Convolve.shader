﻿Shader "Hidden/Volund/Convolve" {
Properties {
	_MainTex("Diffuse", 2D) = "white" {}
	_DepthScale("DepthScale", Float) = 1.0
	_DepthExponent("DepthExponent", Float) = 1.0
}

CGINCLUDE

#pragma only_renderers d3d11 d3d9 opengl
#pragma fragmentoption ARB_precision_hint_fastest
#pragma target 3.0

#include "UnityCG.cginc"

#define MASK_REFLECTION

struct v2f {
	float4 pos	: SV_POSITION;
	float2 uv	: TEXCOORD0;
};

v2f vert(appdata_img v)  {
	v2f o;
	o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
	o.uv = v.texcoord;
	return o;
}

uniform sampler2D _MainTex;
uniform float4 _MainTex_TexelSize;

uniform sampler2D _CameraDepthTexture;

uniform float _DepthScale;
uniform float _DepthExponent;
uniform float _SampleMip;
uniform float _CosPower;

#if PLANE_REFLECTION_CHEAPER
CBUFFER_START(cbCP)
static float cp[11] = {
1.f, 
0.988771077936f,
0.955336489126f,
0.900447102353f,
0.82533561491f,
0.731688868874f,
0.621609968271f,
0.497571047892f,
0.362357754477f,
0.219006687093f,
0.0707372016677f		
};
CBUFFER_END;

CBUFFER_START(cbCP0)
static float cp0[11] = {
1.f,
0.9771384556f,
0.9106683383f,
0.8067340794f,
0.6749311003f,
0.5274005349f,
0.3776807094f,
0.239419351f,
0.125058646f,
0.04459197474f,
0.00440634856f,
};
CBUFFER_END

CBUFFER_START(cbCP1)
static float cp1[11] = {
1.f,
0.9885031389f,
0.9542894416f,
0.8981837671f,
0.8215419042f,
0.7262234745f,
0.614557328f,
0.4893049673f,
0.3536363188f,
0.2111681196f,
0.06638033263f,
};
CBUFFER_END

CBUFFER_START(cbCP2)
static float cp2[11] = {
1.f,
0.9942349515f,
0.9768773933f,
0.9477255758f,
0.9063894881f,
0.8521874644f,
0.7839370689f,
0.6995033719f,
0.5946732875f,
0.459530325f,
0.2576438096f,
};
CBUFFER_END

CBUFFER_START(cbCP3)
static float cp3[11] = {
1.f,
0.9971133093f,
0.9883710808f,
0.9735119803f,
0.9520448982f,
0.9231400026f,
0.8854022074f,
0.8363631818f,
0.7711506257f,
0.6778866609f,
0.5075862583f,
};
CBUFFER_END
#endif

float4 frag(v2f i, const float2 dir) {
	float4 baseUV = i.uv.xyxy;
	baseUV.z = 0;
	baseUV.w = _SampleMip;
	
#if USE_DEPTH
	float depth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv));
	float sampleScale1 = saturate(pow(depth * _DepthScale, _DepthExponent));
#else
	float sampleScale1 = 1.f;
#endif
	float2 sampleScale = dir * _MainTex_TexelSize.xy * sampleScale1;

	float weight = 0.f;
	float4 color = 0.f;

	float4 uv = baseUV;

#if PLANE_REFLECTION_CHEAPER
	#if CP0
		for(int i = -27; i <= 27; i += 3) {
	#else
		for(int i = -30; i <= 30; i += 3) {
	#endif
#else
	for(int i = -32; i <= 32; i += 2) {
#endif
		float2 off = i * sampleScale;
		uv.xy = baseUV.xy + off;
		
		float4 s = tex2Dlod(_MainTex, uv);
#ifdef USE_MASK
		if(s.a > -0.5f)
			continue;
#endif

#if PLANE_REFLECTION_CHEAPER
	#if CP0
			float w = cp0[abs(i/3)];
	#elif CP1
			float w = cp1[abs(i/3)];
	#elif CP2
			float w = cp2[abs(i/3)];
	#elif CP3
			float w = cp3[abs(i/3)];
	#else
			float w = pow(cp[abs(i/3)], _CosPower);
	#endif
#else
		float c = clamp(i / 20.f, -1.57f, 1.57f);
		float w = pow(max(0.f, cos(c)), _CosPower);
#endif
		
#ifdef USE_MASK
		color += s * w;
#else
		color.rgb += s.rgb * w;
#endif
		weight += w;
	}

#ifdef USE_MASK
	if(weight == 0.f)
		return 1.f;
	else
		return color / weight;
#else
		return color.rgbb / weight;
#endif
}

float4 fragH(v2f i) : COLOR { return frag(i, float2(1.f, 0.f)); }
float4 fragV(v2f i) : COLOR { return frag(i, float2(0.f, 1.f)); }

ENDCG

SubShader {
	Cull Off ZTest Always ZWrite Off

	Pass {
		CGPROGRAM
		#pragma vertex vert
		#pragma fragment fragH
		#pragma multi_compile _ USE_DEPTH
		#pragma multi_compile _ USE_MASK
		#pragma multi_compile _ PLANE_REFLECTION_CHEAPER
		#pragma multi_compile _ CP0 CP1 CP2 CP3
		ENDCG
	}
	
	Pass {
		CGPROGRAM
		#pragma vertex vert
		#pragma fragment fragV
		#pragma multi_compile _ USE_DEPTH
		#pragma multi_compile _ USE_MASK
		#pragma multi_compile _ PLANE_REFLECTION_CHEAPER
		#pragma multi_compile _ CP0 CP1 CP2 CP3
		ENDCG
	}
}}

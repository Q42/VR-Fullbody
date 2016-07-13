﻿Shader "Hidden/Volund/Mask Plane" {
SubShader {
	Tags { "Queue"="AlphaTest+51" }

	Pass {
		ColorMask A
		Cull Off
		ZWrite Off
		ZTest Always

CGPROGRAM
#pragma vertex vert
#pragma fragment frag

#pragma only_renderers d3d11 d3d9 opengl
#pragma fragmentoption ARB_precision_hint_fastest

float4 vert(float4 vertex : POSITION) : SV_Position {
	return mul(UNITY_MATRIX_MVP, vertex);
}

float4 frag() : COLOR {
	return -1.f;
}
ENDCG

	}
}}
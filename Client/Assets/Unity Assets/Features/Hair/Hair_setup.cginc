#ifndef FILE_HAIR_SETUP_CGINC
#define FILE_HAIR_SETUP_CGINC

#define UNITY_SETUP_BRDF_INPUT SpecularSetup

#include "AutoLight.cginc"
#include "UnityStandardInput.cginc"
 
 // Override vertex input and texcoord functions
 // (must be after including UnityStandardInput, but before 
 //  including UnityStandardCore for this to resolve correctly)
struct HairVertexInput
{
	float4 vertex	: POSITION;
	half3 normal	: NORMAL;
	float2 uv0		: TEXCOORD0;
	float2 uv1		: TEXCOORD1;
#if defined(DYNAMICLIGHTMAP_ON) || defined(UNITY_PASS_META)
	float2 uv2		: TEXCOORD2;
#endif
#ifdef _TANGENT_TO_WORLD
	half4 tangent	: TANGENT;
#endif

	float4 color	: COLOR;
};
#define VertexInput HairVertexInput
float4 HairTexCoords(VertexInput v)
{
	float4 texcoord;
	texcoord.xy = TRANSFORM_TEX(v.uv0, _MainTex); // Always source from uv0
	texcoord.zw = TRANSFORM_TEX(((_UVSec == 0) ? v.uv0 : v.uv1), _DetailAlbedoMap);
	return texcoord;
}
#define TexCoords HairTexCoords

#include "UnityCG.cginc"
#include "Lighting.cginc"


//---------
// Actual hair shading parts below

sampler2D _KKFlowMap;
half _KKReflectionSmoothness;
half _KKReflectionGrayScale;
half4 _KKPrimarySpecularColor;
half _KKPrimarySpecularExponent;
half _KKPrimaryRootShift;
half4 _KKSecondarySpecularColor;
half _KKSecondarySpecularExponent;
half _KKSecondaryRootShift;
half3 _KKSpecularMixDirectFactors;
half3 _KKSpecularMixIndirectFactors;

half KKDiffuseApprox(half3 normal, half3 lightDir) {
	return max(0.f, dot(normal, lightDir) * 0.75f + 0.25f);
}

half3 BRDF_Unity_KK_ish(half3 baseColor, half3 specColor, half reflectivity, half roughness, half3 normal, half3 normalVertex, half3 viewDir, UnityLight light, UnityIndirect indirect, half3 specGI, half3 tanDir1, half3 tanDir2, half occlusion, half atten) {
	half3 halfDir = normalize (light.dir + viewDir);
	half nl = light.ndotl;
	half nh = BlinnTerm (normal, halfDir);
	half sp = RoughnessToSpecPower (roughness);
		
	half diffuseTerm = nl;
	half specularTerm = pow(nh, sp);
	
	// Poor man's KK. Not physically correct.
	half th1 = dot(tanDir1, halfDir);
	half th2 = dot(tanDir2, halfDir);
	
	half3 kkSpecTermPrimary = pow(sqrt(1.f - th1 * th1), _KKPrimarySpecularExponent) * _KKPrimarySpecularColor.rgb;
	half3 kkSpecTermSecondary = pow(sqrt(1.f - th2 * th2), _KKSecondarySpecularExponent) * _KKSecondarySpecularColor.rgb;
	half3 kkSpecTermBlinn = specularTerm * specColor;

	half kkDirectFactor = min(1.f, Luminance(indirect.diffuse) + nl * atten);
	_KKSpecularMixDirectFactors *= kkDirectFactor;
	
	half3 kkSpecTermDirect = kkSpecTermPrimary * _KKSpecularMixDirectFactors.x
		+ kkSpecTermSecondary * _KKSpecularMixDirectFactors.y
		+ kkSpecTermBlinn * _KKSpecularMixDirectFactors.z;
	kkSpecTermDirect *= light.color;
	
	half3 kkSpecTermIndirect = kkSpecTermPrimary * _KKSpecularMixIndirectFactors.x
		+ kkSpecTermSecondary * _KKSpecularMixIndirectFactors.y
		+ kkSpecTermBlinn * _KKSpecularMixIndirectFactors.z;			
	kkSpecTermIndirect *= specGI;

#ifdef DBG_HAIR_LIGHTING
	baseColor = 0.5f;
#endif
#ifdef DBG_HAIR_SPECULAR
	baseColor = 0;
#endif

	half3 diffuseColor = baseColor;
	half3 color = half3(0.f, 0.f, 0.f);	
	color += baseColor * (indirect.diffuse + light.color * diffuseTerm);
	color += (kkSpecTermIndirect + kkSpecTermDirect) * occlusion;
					
	return color;
}

half4 ShadeHair(
	const half3 tanFlow, const half grayMask, const half vtxOcclusion, const half atten,
	const half3 normalWorldVertex, const half3x3 tangentToWorldMatrix,
	UnityLight light, UnityIndirect indirect,
	half3 diffColor, half3 specColor, half3 eyeVec, half3 normalWorld, half oneMinusReflectivity, half oneMinusRoughness
)
{
	half3 worldTangent = mul(tangentToWorldMatrix, tanFlow);
	half3 tanDir = normalize(worldTangent + normalWorldVertex.xyz * _KKPrimaryRootShift);
	half3 tanDir2 = normalize(worldTangent + normalWorldVertex.xyz * _KKSecondaryRootShift);
	
#ifndef ADDITIVE_PASS
	half3 reflectVector = reflect(eyeVec, normalWorldVertex);
	
	indirect.specular = Unity_GlossyEnvironment(UNITY_PASS_TEXCUBE(unity_SpecCube0), unity_SpecCube0_HDR, reflectVector, _KKReflectionSmoothness);
	
	UNITY_BRANCH
	if (unity_SpecCube0_BoxMin.w < 0.99999f) {
		indirect.specular = lerp(
			Unity_GlossyEnvironment (UNITY_PASS_TEXCUBE(unity_SpecCube1), unity_SpecCube1_HDR, reflectVector, _KKReflectionSmoothness),
			indirect.specular,
			unity_SpecCube0_BoxMin.w
		);
	}
#endif
	
	diffColor *= grayMask;
	half specOcclusion = (1.f - min(1.f, _KKReflectionGrayScale * Luminance(diffColor))) * min(1.f, vtxOcclusion * 2.f);
	diffColor *= vtxOcclusion;

	half3 c = BRDF_Unity_KK_ish(diffColor, specColor, 1.f - oneMinusReflectivity, 1.f - oneMinusRoughness, normalWorld, normalWorldVertex, -eyeVec, light, indirect, indirect.specular, tanDir, tanDir2, specOcclusion, atten);
	return float4(c, 1.f);
}

// Include the rest of the standard shading functions
#include "Hair_UnityStandardCore.cginc"

#endif // FILE_HAIR_SETUP_CGINC

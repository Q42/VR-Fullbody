using System;
using UnityEngine;

namespace UnityEditor
{

class VolundMultiStandardShaderGUI : ShaderGUI
{
    private enum WorkflowMode
    {
        Specular,
        Metallic,
        Dielectric
    }

	public enum BlendMode
	{
		Opaque,
		Cutout,
		Fade,		// Old school alpha-blending mode, fresnel does not affect amount of transparency
		Transparent // Physically plausible transparency mode, implemented as alpha pre-multiply
	}

	private static class Styles
	{
		public static GUIStyle optionsButton = "PaneOptions";
		public static GUIContent uvSetLabel = new GUIContent("UV Set");
		public static GUIContent[] uvSetOptions = new GUIContent[] { new GUIContent("UV channel 0"), new GUIContent("UV channel 1") };

		public static string emptyTootip = "";
		public static GUIContent albedoText = new GUIContent("Albedo", "Albedo (RGB) and Transparency (A)");
		public static GUIContent alphaCutoffText = new GUIContent("Alpha Cutoff", "Threshold for alpha cutoff");
		public static GUIContent specularMapText = new GUIContent("Specular", "Specular (RGB) and Smoothness (A)");
		public static GUIContent metallicMapText = new GUIContent("Metallic", "Metallic (R) and Smoothness (A)");
		public static GUIContent smoothnessText = new GUIContent("Smoothness", "");
		public static GUIContent normalMapText = new GUIContent("Normal Map", "Normal Map");
		public static GUIContent orthoNormalizeText = new GUIContent("Orthonormalize", "Orthonormalize tangent base");
		public static GUIContent heightMapText = new GUIContent("Height Map", "Height Map (G)");
		public static GUIContent occlusionText = new GUIContent("Occlusion", "Occlusion (G)");
		public static GUIContent emissionText = new GUIContent("Emission", "Emission (RGB)");
		public static GUIContent detailMaskText = new GUIContent("Detail Mask", "Mask for Secondary Maps (A)");
		public static GUIContent detailAlbedoText = new GUIContent("Detail Albedo x2", "Albedo (RGB) multiplied by 2");
		public static GUIContent detailNormalMapText = new GUIContent("Normal Map", "Normal Map");
		public static GUIContent smoothnessInAlbedoText = new GUIContent("Smoothness in Albedo", "Smoothness is stored in Albedo (A); Specular is a single color.");

		public static string whiteSpaceString = " ";
		public static string primaryMapsText = "Main Maps";
		public static string secondaryMapsText = "Secondary Maps";
		public static string renderingMode = "Rendering Mode";
		public static string cullingMode = "Culling Mode";
		public static GUIContent emissiveWarning = new GUIContent ("Emissive value is animated but the material has not been configured to support emissive. Please make sure the material itself has some amount of emissive.");
		public static GUIContent emissiveColorWarning = new GUIContent ("Ensure emissive color is non-black for emission to have effect.");
		public static readonly string[] blendNames = Enum.GetNames (typeof (BlendMode));
		public static readonly string[] cullingNames = Enum.GetNames (typeof (UnityEngine.Rendering.CullMode));
	}

	MaterialProperty blendMode = null;
	MaterialProperty cullMode = null;
	MaterialProperty albedoMap = null;
	MaterialProperty albedoColor = null;
	MaterialProperty alphaCutoff = null;
	MaterialProperty specularMap = null;
	MaterialProperty specularColor = null;
	MaterialProperty metallicMap = null;
	MaterialProperty metallic = null;
	MaterialProperty smoothness = null;
	MaterialProperty smoothnessTweak1 = null;
	MaterialProperty smoothnessTweak2 = null;
	MaterialProperty smoothnessTweaks = null;
	MaterialProperty specularMapColorTweak = null;
	MaterialProperty bumpScale = null;
	MaterialProperty bumpMap = null;
	MaterialProperty orthoNormalize = null;
	MaterialProperty occlusionStrength = null;
	MaterialProperty occlusionMap = null;
	MaterialProperty heigtMapScale = null;
	MaterialProperty heightMap = null;
	MaterialProperty emissionColorForRendering = null;
	MaterialProperty emissionMap = null;
	MaterialProperty detailMask = null;
	MaterialProperty detailAlbedoMap = null;
	MaterialProperty detailNormalMapScale = null;
	MaterialProperty detailNormalMap = null;
	MaterialProperty uvSetSecondary = null;
	MaterialProperty smoothnessInAlbedo = null;

	MaterialEditor m_MaterialEditor;
	WorkflowMode m_WorkflowMode = WorkflowMode.Specular;
	ColorPickerHDRConfig m_ColorPickerHDRConfig = new ColorPickerHDRConfig(0f, 99f, 1/99f, 3f);

	bool m_FirstTimeApply = true;

	public void FindProperties (MaterialProperty[] props)
	{
		blendMode = FindProperty ("_Mode", props);
		cullMode = FindProperty ("_CullMode", props, false);
		albedoMap = FindProperty ("_MainTex", props);
		albedoColor = FindProperty ("_Color", props);
		alphaCutoff = FindProperty ("_Cutoff", props);
		specularMap = FindProperty ("_SpecGlossMap", props, false);
		specularColor = FindProperty ("_SpecColor", props, false);
		metallicMap = FindProperty ("_MetallicGlossMap", props, false);
		metallic = FindProperty ("_Metallic", props, false);
		if (specularMap != null && specularColor != null)
			m_WorkflowMode = WorkflowMode.Specular;
		else if (metallicMap != null && metallic != null)
			m_WorkflowMode = WorkflowMode.Metallic;
		else
			m_WorkflowMode = WorkflowMode.Dielectric;
		smoothness = FindProperty ("_Glossiness", props);
		smoothnessTweak1 = FindProperty ("_SmoothnessTweak1", props, false);
		smoothnessTweak2 = FindProperty ("_SmoothnessTweak2", props, false);
		smoothnessTweaks = FindProperty ("_SmoothnessTweaks", props, false);
		specularMapColorTweak = FindProperty ("_SpecularMapColorTweak", props, false);

		bumpScale = FindProperty ("_BumpScale", props);
		bumpMap = FindProperty ("_BumpMap", props);
		orthoNormalize = FindProperty ("_Orthonormalize", props, false);
		heigtMapScale = FindProperty ("_Parallax", props);
		heightMap = FindProperty("_ParallaxMap", props);
		occlusionStrength = FindProperty ("_OcclusionStrength", props);
		occlusionMap = FindProperty ("_OcclusionMap", props);
		emissionColorForRendering = FindProperty ("_EmissionColor", props);
		emissionMap = FindProperty ("_EmissionMap", props);
		detailMask = FindProperty ("_DetailMask", props);
		detailAlbedoMap = FindProperty ("_DetailAlbedoMap", props);
		detailNormalMapScale = FindProperty ("_DetailNormalMapScale", props);
		detailNormalMap = FindProperty ("_DetailNormalMap", props);
		uvSetSecondary = FindProperty ("_UVSec", props);
		smoothnessInAlbedo = FindProperty ("_SmoothnessInAlbedo", props, false);
	}
	
	public override void AssignNewShaderToMaterial (Material material, Shader oldShader, Shader newShader)
	{
		base.AssignNewShaderToMaterial(material, oldShader, newShader);

		// Re-run this in case the new shader needs custom setup.
		m_FirstTimeApply = true;
	}

	public override void OnGUI (MaterialEditor materialEditor, MaterialProperty[] props)
	{
		FindProperties (props); // MaterialProperties can be animated so we do not cache them but fetch them every event to ensure animated values are updated correctly
		m_MaterialEditor = materialEditor;
		Material material = materialEditor.target as Material;

		ShaderPropertiesGUI (material);

		// Make sure that needed keywords are set up if we're switching some existing
		// material to a standard shader.
		if (m_FirstTimeApply)
		{
			// Make sure we've updated this packed vector
			if(smoothnessTweak1 != null && smoothnessTweak2 != null && smoothnessTweaks != null) {
				var w = new Vector4(smoothnessTweak1.floatValue, smoothnessTweak2.floatValue);
				if(smoothnessTweaks.vectorValue != w)
					smoothnessTweaks.vectorValue = w;
			}

			SetMaterialKeywords (material, m_WorkflowMode);
			m_FirstTimeApply = false;

			// Repaint all in case we modified how things render
			SceneView.RepaintAll();
		}
	}

	public void ShaderPropertiesGUI (Material material)
	{
		// Use default labelWidth
		EditorGUIUtility.labelWidth = 0f;

		// Detect any changes to the material
		EditorGUI.BeginChangeCheck();
		{
			CullModePopup();
			BlendModePopup();
			OrthoNormalizeToggle();

			// Primary properties
			GUILayout.Label (Styles.primaryMapsText, EditorStyles.boldLabel);
			DoAlbedoArea(material);
			DoSpecularMetallicArea();
			m_MaterialEditor.TexturePropertySingleLine(Styles.normalMapText, bumpMap, bumpMap.textureValue != null ? bumpScale : null);
			m_MaterialEditor.TexturePropertySingleLine(Styles.heightMapText, heightMap, heightMap.textureValue != null ? heigtMapScale : null);
			m_MaterialEditor.TexturePropertySingleLine(Styles.occlusionText, occlusionMap, occlusionMap.textureValue != null ? occlusionStrength : null);
			DoEmissionArea(material);
			m_MaterialEditor.TexturePropertySingleLine(Styles.detailMaskText, detailMask);
			EditorGUI.BeginChangeCheck();
			m_MaterialEditor.TextureScaleOffsetProperty(albedoMap);
			if (EditorGUI.EndChangeCheck())
				emissionMap.textureScaleAndOffset = albedoMap.textureScaleAndOffset; // Apply the main texture scale and offset to the emission texture as well, for Enlighten's sake

			EditorGUILayout.Space();

			// Secondary properties
			GUILayout.Label(Styles.secondaryMapsText, EditorStyles.boldLabel);
			m_MaterialEditor.TexturePropertySingleLine(Styles.detailAlbedoText, detailAlbedoMap);
			m_MaterialEditor.TexturePropertySingleLine(Styles.detailNormalMapText, detailNormalMap, detailNormalMapScale);
			m_MaterialEditor.TextureScaleOffsetProperty(detailAlbedoMap);
			m_MaterialEditor.ShaderProperty(uvSetSecondary, Styles.uvSetLabel.text);
		}
		if (EditorGUI.EndChangeCheck())
		{
			foreach (var obj in blendMode.targets)
				MaterialChanged((Material)obj, m_WorkflowMode);
		}
	}


	void CullModePopup()
	{
		if(cullMode == null)
			return;
			
		EditorGUI.showMixedValue = cullMode.hasMixedValue;
		var mode = (UnityEngine.Rendering.CullMode)Mathf.RoundToInt(cullMode.floatValue);

		EditorGUI.BeginChangeCheck();
		mode = (UnityEngine.Rendering.CullMode)EditorGUILayout.Popup(Styles.cullingMode, (int)mode, Styles.cullingNames);
		if (EditorGUI.EndChangeCheck())
		{
			m_MaterialEditor.RegisterPropertyChangeUndo("Culling Mode");
			cullMode.floatValue = (float)mode;
		}

		EditorGUI.showMixedValue = false;
	}
	
	void OrthoNormalizeToggle()
	{
		if(orthoNormalize == null)
			return;
		
		EditorGUI.showMixedValue = orthoNormalize.hasMixedValue;
		var on = Mathf.RoundToInt(orthoNormalize.floatValue);
		
		EditorGUI.BeginChangeCheck();
			on = EditorGUILayout.Toggle(Styles.orthoNormalizeText, on == 1) ? 1 : 0;
		if (EditorGUI.EndChangeCheck())
		{
			m_MaterialEditor.RegisterPropertyChangeUndo("Orthonormalize");
			orthoNormalize.floatValue = (float)on;
		}
		
		EditorGUI.showMixedValue = false;
	}

	bool SmoothnessInAlbedoToggle()
	{
		if(smoothnessInAlbedo == null)
			return false;
		
		EditorGUI.showMixedValue = smoothnessInAlbedo.hasMixedValue;
		var on = Mathf.RoundToInt(smoothnessInAlbedo.floatValue);
		
		EditorGUI.BeginChangeCheck();
			on = EditorGUILayout.Toggle(Styles.smoothnessInAlbedoText, on == 1) ? 1 : 0;
		if (EditorGUI.EndChangeCheck())
		{
			m_MaterialEditor.RegisterPropertyChangeUndo("SmoothnessInAlbedo");
			smoothnessInAlbedo.floatValue = (float)on;
		}
		
		EditorGUI.showMixedValue = false;
		return (on == 1);
	}

	void BlendModePopup()
	{
		EditorGUI.showMixedValue = blendMode.hasMixedValue;
		var mode = (BlendMode)blendMode.floatValue;

		EditorGUI.BeginChangeCheck();
		mode = (BlendMode)EditorGUILayout.Popup(Styles.renderingMode, (int)mode, Styles.blendNames);
		if (EditorGUI.EndChangeCheck())
		{
			m_MaterialEditor.RegisterPropertyChangeUndo("Rendering Mode");
			blendMode.floatValue = (float)mode;
		}

		EditorGUI.showMixedValue = false;
	}

	void DoAlbedoArea(Material material)
	{
		m_MaterialEditor.TexturePropertySingleLine(Styles.albedoText, albedoMap, albedoColor);
		if (((BlendMode)material.GetFloat("_Mode") == BlendMode.Cutout))
		{
			m_MaterialEditor.ShaderProperty(alphaCutoff, Styles.alphaCutoffText.text, MaterialEditor.kMiniTextureFieldLabelIndentLevel+1);
		}
	}

	void DoEmissionArea(Material material)
	{
		float brightness = emissionColorForRendering.colorValue.maxColorComponent;
		bool showHelpBox = !HasValidEmissiveKeyword(material);
		bool showEmissionColorAndGIControls = brightness > 0.0f;
		
		bool hadEmissionTexture = emissionMap.textureValue != null;

		// Texture and HDR color controls
		m_MaterialEditor.TexturePropertyWithHDRColor(Styles.emissionText, emissionMap, emissionColorForRendering, m_ColorPickerHDRConfig, false);

		// If texture was assigned and color was black set color to white
		if (emissionMap.textureValue != null && !hadEmissionTexture && brightness <= 0f)
			emissionColorForRendering.colorValue = Color.white;

		// Dynamic Lightmapping mode
		if (showEmissionColorAndGIControls)
		{
			bool shouldEmissionBeEnabled = ShouldEmissionBeEnabled(emissionColorForRendering.colorValue);
			EditorGUI.BeginDisabledGroup(!shouldEmissionBeEnabled);

			m_MaterialEditor.LightmapEmissionProperty (MaterialEditor.kMiniTextureFieldLabelIndentLevel + 1);

			EditorGUI.EndDisabledGroup();
		}

		if (showHelpBox)
		{
			EditorGUILayout.HelpBox(Styles.emissiveWarning.text, MessageType.Warning);
		}
	}

	void DoSpecularMetallicArea()
	{
		if (m_WorkflowMode == WorkflowMode.Specular)
		{
			if (specularMap.textureValue == null) {
				if(smoothnessInAlbedo == null) {
					m_MaterialEditor.TexturePropertyTwoLines(Styles.specularMapText, specularMap, specularColor, Styles.smoothnessText, smoothness);
				} else {
					m_MaterialEditor.TexturePropertySingleLine(Styles.specularMapText, specularMap, specularColor);
					int indent = 3;
					EditorGUI.indentLevel += indent;
					if (!SmoothnessInAlbedoToggle())
						m_MaterialEditor.ShaderProperty(smoothness, Styles.smoothnessText.text);
					EditorGUI.indentLevel -= indent;
				}
			} else {
				m_MaterialEditor.TexturePropertySingleLine(Styles.specularMapText, specularMap);
				
				if(specularMapColorTweak != null)
					m_MaterialEditor.ColorProperty(specularMapColorTweak, specularMapColorTweak.displayName);

				if(smoothnessTweak1 != null && smoothnessTweak2 != null) {
					m_MaterialEditor.ShaderProperty(smoothnessTweak1, smoothnessTweak1.displayName);
					m_MaterialEditor.ShaderProperty(smoothnessTweak2, smoothnessTweak2.displayName);
					
					if(GUI.changed && smoothnessTweaks != null)
						smoothnessTweaks.vectorValue = new Vector4(smoothnessTweak1.floatValue, smoothnessTweak2.floatValue);
				}
			}
		}
		else if (m_WorkflowMode == WorkflowMode.Metallic)
		{
			if (metallicMap.textureValue == null)
				m_MaterialEditor.TexturePropertyTwoLines(Styles.metallicMapText, metallicMap, metallic, Styles.smoothnessText, smoothness);
			else
				m_MaterialEditor.TexturePropertySingleLine(Styles.metallicMapText, metallicMap);
		}
	}

	public static void SetupMaterialWithBlendMode(Material material, BlendMode blendMode)
	{
		switch (blendMode)
		{
			case BlendMode.Opaque:
				material.SetOverrideTag("RenderType", "");
				material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
				material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
				material.SetInt("_ZWrite", 1);
				material.DisableKeyword("_ALPHATEST_ON");
				material.DisableKeyword("_ALPHABLEND_ON");
				material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
				material.renderQueue = -1;
				break;
			case BlendMode.Cutout:
				material.SetOverrideTag("RenderType", "TransparentCutout");
				material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
				material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
				material.SetInt("_ZWrite", 1);
				material.EnableKeyword("_ALPHATEST_ON");
				material.DisableKeyword("_ALPHABLEND_ON");
				material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
				material.renderQueue = 2450;
				break;
			case BlendMode.Fade:
				material.SetOverrideTag("RenderType", "Transparent");
				material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
				material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
				material.SetInt("_ZWrite", 0);
				material.DisableKeyword("_ALPHATEST_ON");
				material.EnableKeyword("_ALPHABLEND_ON");
				material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
				material.renderQueue = 3000;
				break;
			case BlendMode.Transparent:
				material.SetOverrideTag("RenderType", "Transparent");
				material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
				material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
				material.SetInt("_ZWrite", 0);
				material.DisableKeyword("_ALPHATEST_ON");
				material.DisableKeyword("_ALPHABLEND_ON");
				material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
				material.renderQueue = 3000;
				break;
		}
	}
	
	static bool ShouldEmissionBeEnabled (Color color)
	{
		return color.maxColorComponent > (0.1f / 255.0f);
	}

	static void SetMaterialKeywords(Material material, WorkflowMode workflowMode)
	{
		// Note: keywords must be based on Material value not on MaterialProperty due to multi-edit & material animation
		// (MaterialProperty value might come from renderer material property block)
		SetKeyword (material, "_NORMALMAP", material.GetTexture ("_BumpMap") || material.GetTexture ("_DetailNormalMap"));
		SetKeyword (material, "ORTHONORMALIZE_TANGENT_BASE", material.HasProperty("__orthonormalize") && material.GetFloat("__orthonormalize") > 0.5f);
		SetKeyword (material, "SMOOTHNESS_IN_ALBEDO", material.HasProperty("__smoothnessinalbedo") && material.GetFloat("__smoothnessinalbedo") > 0.5f && !material.GetTexture ("_SpecGlossMap"));
		if (workflowMode == WorkflowMode.Specular)
			SetKeyword (material, "_SPECGLOSSMAP", material.GetTexture ("_SpecGlossMap"));
		else if (workflowMode == WorkflowMode.Metallic)
			SetKeyword (material, "_METALLICGLOSSMAP", material.GetTexture ("_MetallicGlossMap"));
		SetKeyword (material, "_PARALLAXMAP", material.GetTexture ("_ParallaxMap"));
		SetKeyword (material, "_DETAIL_MULX2", material.GetTexture ("_DetailAlbedoMap") || material.GetTexture ("_DetailNormalMap"));

		bool shouldEmissionBeEnabled = ShouldEmissionBeEnabled (material.GetColor("_EmissionColor"));
		SetKeyword (material, "_EMISSION", shouldEmissionBeEnabled);

		// Setup lightmap emissive flags
		MaterialGlobalIlluminationFlags flags = material.globalIlluminationFlags;
		if ((flags & (MaterialGlobalIlluminationFlags.BakedEmissive | MaterialGlobalIlluminationFlags.RealtimeEmissive)) != 0)
		{
			flags &= ~MaterialGlobalIlluminationFlags.EmissiveIsBlack;
			if (!shouldEmissionBeEnabled)
				flags |= MaterialGlobalIlluminationFlags.EmissiveIsBlack;

			material.globalIlluminationFlags = flags;
		}
	}

	bool HasValidEmissiveKeyword (Material material)
	{
		// Material animation might be out of sync with the material keyword.
		// So if the emission support is disabled on the material, but the property blocks have a value that requires it, then we need to show a warning.
		// (note: (Renderer MaterialPropertyBlock applies its values to emissionColorForRendering))
		bool hasEmissionKeyword = material.IsKeywordEnabled ("_EMISSION");
		if (!hasEmissionKeyword && ShouldEmissionBeEnabled (emissionColorForRendering.colorValue))
			return false;
		else
			return true;
	}

	static void MaterialChanged(Material material, WorkflowMode workflowMode)
	{
		SetupMaterialWithBlendMode(material, (BlendMode)material.GetFloat("_Mode"));

		SetMaterialKeywords(material, workflowMode);
	}

	static void SetKeyword(Material m, string keyword, bool state)
	{
		if (state)
			m.EnableKeyword (keyword);
		else
			m.DisableKeyword (keyword);
	}
}

} // namespace UnityEditor

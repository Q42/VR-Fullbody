//#define ALLOW_ATMOSPHERICS_DEPENDENCY
//#define ALLOW_UNIQUESHADOW_DEPENDENCY
#define PLANE_REFLECTION_CHEAPER
//#define USE_GLOBAL_KEYWORDS

// Can't use temp main buffer because Unity won't allow us to explicitly
// render to each of the mip levels in a temporary render texture.


using UnityEngine;
using System.Collections.Generic;

[ExecuteInEditMode]
public class PlaneReflection : MonoBehaviour {
	public enum Dimension {
		x128	= 128,
		x256	= 256,
		x512	= 512,
		x1024	= 1024,
		x2048	= 2048,
	}

	[HideInInspector] public Shader convolveShader;
	[HideInInspector] public Shader maskShader;

	public Dimension	reflectionMapSize = Dimension.x1024;
	public LayerMask	reflectLayerMask = ~0;
	public float		maxDistance = 80f;
	public float		clipPlaneOffset = 0.01f;
	public bool			clipSkyDome;
	public float		nearPlaneDistance = 0.1f;
	public float		farPlaneDistance = 25f;
	public float		mipShift;
	public bool			useMask = true;
	public bool			useDepth;
	public float		depthScale = 1.25f;
	public float		depthExponent = 2.25f;
	public Material[]	explicitMaterials;
	public bool 		disableScattering;
	public float 		scatterWorldFakePush = -1f;
	public float 		scatterHeightFakePush = -1f;
	public float 		shadowDistance = 200f;
	public Color		clearColor = Color.gray;

	int 						m_downscale = 0;
	Shader						m_lodShader;
	int 						m_lodShaderLod;
	bool						m_cookielessMainlight;
	
public RenderTexture			m_reflectionMap; //hacked public for easier debugging
	Camera						m_reflectionCamera;
	Camera						m_renderCamera;

	Material[]					m_materials;
	Shader[]					m_shaders;

	Material					m_convolveMaterial;

	bool 						m_isActive;
	Renderer					m_renderer;

	public void SetDownscale(int ds) {
		m_downscale = ds;
	}

	public void SetShaderLod(Shader shader, int lod) {
		m_lodShader = shader;
		m_lodShaderLod = lod;
	}

	public void SetCookielessMainlight(bool b) {
		m_cookielessMainlight = b;
	}

#if UNITY_EDITOR
	void OnValidate() {
		Awake();
		UnityEditor.SceneView.RepaintAll();
	}
#endif

	void Awake() {
		if(!maskShader)
			return;

		m_renderer = GetComponent<Renderer>();
		if(explicitMaterials != null && explicitMaterials.Length > 0)
			m_materials = explicitMaterials;
		else
			m_materials = m_renderer.sharedMaterials;

		m_shaders = m_shaders != null && m_shaders.Length == m_materials.Length ? m_shaders : new Shader[m_materials.Length];

		for(int i = 0, n = m_materials.Length; i < n; ++i)
			m_shaders[i] = m_materials[i].shader;

		m_convolveMaterial = m_convolveMaterial ?? new Material(convolveShader);
		if(useDepth) {
			m_convolveMaterial.EnableKeyword("USE_DEPTH");
			m_convolveMaterial.SetFloat("_DepthScale", depthScale);
			m_convolveMaterial.SetFloat("_DepthExponent", depthExponent);

		} else {
			m_convolveMaterial.DisableKeyword("USE_DEPTH");
		}

		if(useMask)
			m_convolveMaterial.EnableKeyword("USE_MASK");
		else
			m_convolveMaterial.DisableKeyword("USE_MASK");

#if PLANE_REFLECTION_CHEAPER
		m_convolveMaterial.EnableKeyword("PLANE_REFLECTION_CHEAPER");
#else
		m_convolveMaterial.DisableKeyword("PLANE_REFLECTION_CHEAPER");
#endif

		m_convolveMaterial.hideFlags = HideFlags.DontSave | HideFlags.NotEditable;

		EnsureReflectionCamera(null);
	}
	
	void OnDisable() {
#if USE_GLOBAL_KEYWORDS
		Shader.DisableKeyword("PLANE_REFLECTION");
#else
		foreach(var m in m_materials)
			m.DisableKeyword("PLANE_REFLECTION");
#endif

		m_isActive = false;
	}

	void OnDestroy() {
		Object.DestroyImmediate (m_convolveMaterial);
	}

	void OnBecameInvisible() {
		CheckCulling(null);
	}

	bool CheckCulling(Camera cam) {
		bool active = false;
		if(cam) {
			var d2 = Vector3.SqrMagnitude(transform.position - cam.transform.position);
			active = d2 < maxDistance * maxDistance;
		}

		if(active == m_isActive)
			return m_isActive;

		if(active) {
#if USE_GLOBAL_KEYWORDS
			Shader.EnableKeyword("PLANE_REFLECTION");
#else
			for(int i = 0, n = m_materials.Length; i < n; ++i)
				m_materials[i].EnableKeyword("PLANE_REFLECTION");
#endif
		} else {
#if USE_GLOBAL_KEYWORDS
			Shader.DisableKeyword("PLANE_REFLECTION");
#else
			for(int i = 0, n = m_materials.Length; i < n; ++i)
				m_materials[i].DisableKeyword("PLANE_REFLECTION");
#endif

			// This is probably temp, we'd like to keep this around, or at
			// the very least shared between instances!
			Object.DestroyImmediate(m_reflectionMap);
			m_reflectionMap = null;
		}

		return m_isActive = active;
	}

	public void OnWillRenderObject() {
		if(Camera.current == Camera.main)
			m_renderCamera = Camera.current;
#if UNITY_EDITOR
		else if(UnityEditor.SceneView.currentDrawingSceneView && UnityEditor.SceneView.currentDrawingSceneView.camera == Camera.current)
			m_renderCamera = Camera.current;
#endif
		else
			return;

		if(!CheckCulling(m_renderCamera)) {
			m_renderCamera = null;
			return;
		}

		var restoreNearClip = m_renderCamera.nearClipPlane;
		var restoreFarClip = m_renderCamera.farClipPlane;
		m_renderCamera.nearClipPlane = nearPlaneDistance;
		m_renderCamera.farClipPlane = farPlaneDistance + nearPlaneDistance;

		m_reflectionCamera = EnsureReflectionCamera(m_renderCamera);

		var reflectionMapDim = (int)reflectionMapSize >> m_downscale;
		var reflectionMap0 = RenderTexture.GetTemporary(reflectionMapDim, reflectionMapDim, 24, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Default);
		reflectionMap0.filterMode = FilterMode.Bilinear;

		// find out the reflection plane: position and normal in world space
		Vector3 pos = transform.position;
		Vector3 normal = Vector3.up;

		// Reflect camera around reflection plane
		float d = -Vector3.Dot (normal, pos) - clipPlaneOffset;
		Vector4 reflectionPlane = new Vector4 (normal.x, normal.y, normal.z, d);
	
		Matrix4x4 reflection = Matrix4x4.zero;
		CalculateReflectionMatrix(ref reflection, reflectionPlane);
		Vector3 newpos = reflection.MultiplyPoint(m_renderCamera.transform.position);
		m_reflectionCamera.worldToCameraMatrix = m_renderCamera.worldToCameraMatrix * reflection;
	
		// Setup oblique projection matrix so that near plane is our reflection
		// plane. This way we clip everything below/above it for free.
		Vector4 clipPlane = CameraSpacePlane(m_reflectionCamera, pos, normal, 1.0f, clipPlaneOffset);
		m_reflectionCamera.projectionMatrix = m_renderCamera.CalculateObliqueMatrix(clipPlane);

		m_reflectionCamera.cullingMask = reflectLayerMask;
		m_reflectionCamera.targetTexture = reflectionMap0;
		m_reflectionCamera.transform.position = newpos;
		Vector3 euler = m_renderCamera.transform.eulerAngles;
		m_reflectionCamera.transform.eulerAngles = new Vector3(-euler.x, euler.y, euler.z);

#if ALLOW_ATMOSPHERICS_DEPENDENCY
		bool scatteringOcclusionWasEnabled = Shader.IsKeywordEnabled("ATMOSPHERICS_OCCLUSION");
		Shader.DisableKeyword("ATMOSPHERICS_OCCLUSION");

		bool scatteringWasEnabled = Shader.IsKeywordEnabled("ATMOSPHERICS");
		if(disableScattering && scatteringWasEnabled)
			Shader.DisableKeyword("ATMOSPHERICS");

		float oldScatterPushW = float.MaxValue;
		float oldScatterPushH = float.MaxValue;
		// HACKY HACKS FOLLOW! 
		var s = AtmosphericScattering.instance;
		if(s && scatterWorldFakePush >= 0f) {
			oldScatterPushW = -Mathf.Pow(Mathf.Abs(s.worldNearScatterPush), s.worldScaleExponent) * Mathf.Sign(s.worldNearScatterPush);
			Shader.SetGlobalFloat("u_WorldNearScatterPush", -Mathf.Pow(Mathf.Abs(scatterWorldFakePush), s.worldScaleExponent) * Mathf.Sign(scatterWorldFakePush));
		}
		if(s && scatterWorldFakePush >= 0f) {
			oldScatterPushH = -Mathf.Pow(Mathf.Abs(s.heightNearScatterPush), s.worldScaleExponent) * Mathf.Sign(s.heightNearScatterPush);
			Shader.SetGlobalFloat("u_HeightNearScatterPush", -Mathf.Pow(Mathf.Abs(scatterHeightFakePush), s.worldScaleExponent) * Mathf.Sign(scatterHeightFakePush));
		}

		if(clipSkyDome) {
			Shader.EnableKeyword("CLIP_SKYDOME");
			Shader.SetGlobalFloat("u_SkyDomeClipHeight", transform.position.y + clipPlaneOffset);
		}
#endif

		int oldLodShaderLod = 0;
		if(m_lodShader) {
			oldLodShaderLod = m_lodShader.maximumLOD;
			m_lodShader.maximumLOD = m_lodShaderLod;
		}

#if ALLOW_UNIQUESHADOW_DEPENDENCY
		Light oldMainLight = null;
		Texture oldMainLightCookie = null;
		if(m_cookielessMainlight) {
			var mask = ~((1 << LayerMask.NameToLayer("Characters")) | (1 << LayerMask.NameToLayer("CharactersSkin")));

			// Try somewhat hard to find an active directional cookie light since it has a big impact.
            var cookieLight = UniqueShadowSun.instance && (UniqueShadowSun.instance.cullingMask & mask) != 0 ? UniqueShadowSun.instance : null;
			             if(!cookieLight) {
				var suns = GameObject.FindGameObjectsWithTag("Sun");
				for(int i = 0, n = suns.Length; i < n; ++i) {
					var sl = suns[i].GetComponent<Light>();
					if(sl && sl.enabled && (sl.cullingMask & mask)!= 0 && sl.cookie) {
						cookieLight = sl;
						break;
					}
				}
			}
			if(cookieLight) {
				oldMainLight = cookieLight;
				oldMainLightCookie = cookieLight.cookie;
				cookieLight.cookie = null;
			}
		}
#endif

		var oldShadowDist = QualitySettings.shadowDistance;
		QualitySettings.shadowDistance = shadowDistance;
		
		GL.invertCulling = true;
		m_reflectionCamera.Render();
		GL.invertCulling = false;

		QualitySettings.shadowDistance = oldShadowDist;

#if ALLOW_UNIQUESHADOW_DEPENDENCY
		if(oldMainLight)
			oldMainLight.cookie = oldMainLightCookie;
#endif

		if(m_lodShader)
			m_lodShader.maximumLOD = oldLodShaderLod;

		Convolve(m_reflectionCamera.targetTexture);
		m_reflectionCamera.targetTexture = null;

		m_renderCamera.nearClipPlane = restoreNearClip;
		m_renderCamera.farClipPlane = restoreFarClip;

#if ALLOW_ATMOSPHERICS_DEPENDENCY
		if(scatteringOcclusionWasEnabled)
			Shader.EnableKeyword("ATMOSPHERICS_OCCLUSION");

		if(disableScattering && scatteringWasEnabled)
			Shader.EnableKeyword("ATMOSPHERICS");

		if(oldScatterPushW != float.MaxValue)
			Shader.SetGlobalFloat("u_WorldNearScatterPush", oldScatterPushW);
		if(oldScatterPushH != float.MaxValue)
			Shader.SetGlobalFloat("u_HeightNearScatterPush", oldScatterPushH);

		if(clipSkyDome)
			Shader.DisableKeyword("CLIP_SKYDOME");
#endif

		float mipCount = Mathf.Max(0f, Mathf.Round(Mathf.Log ((float)m_reflectionMap.width, 2f)) - mipShift);
#if USE_GLOBAL_KEYWORDS
		Shader.SetGlobalFloat("_PlaneReflectionLodSteps", mipCount);
		Shader.SetGlobalTexture("_PlaneReflection", m_reflectionMap);
#else
		for(int i = 0, n = m_materials.Length; i < n; ++i) {
			var m = m_materials[i];
			if(useMask)
				m.shader = m_shaders[i];
			m.SetFloat("_PlaneReflectionLodSteps", mipCount);
			m.SetTexture("_PlaneReflection", m_reflectionMap);
		}
#endif

		m_reflectionCamera = null;
	}

	void Convolve(RenderTexture reflectionMap0) {
		// The simplest and most naive texture convolve the world ever saw. It sorta
		// gets the job done, though.

#if PLANE_REFLECTION_CHEAPER
		if(m_reflectionMap == null || m_reflectionMap.width != reflectionMap0.width >> 1) {
			Object.DestroyImmediate(m_reflectionMap);
			m_reflectionMap = new RenderTexture(reflectionMap0.width >> 1, reflectionMap0.width >> 1, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Default);
#else
		if(m_reflectionMap == null || m_reflectionMap.width != reflectionMap0.width) {
			Object.DestroyImmediate(m_reflectionMap);
			m_reflectionMap = new RenderTexture(reflectionMap0.width, reflectionMap0.width, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Default);
#endif
			m_reflectionMap.useMipMap = true;
			m_reflectionMap.generateMips = false;
			m_reflectionMap.filterMode = FilterMode.Trilinear;
			m_reflectionMap.hideFlags = HideFlags.DontSave | HideFlags.NotEditable;
		}

#if PLANE_REFLECTION_CHEAPER
		ConvolveStep(reflectionMap0, 0, m_reflectionMap, 0);
		RenderTexture.ReleaseTemporary(reflectionMap0);

		for(int i = 0, n = m_reflectionMap.width; (n >> i) > 1; ++i) 
			ConvolveStep(m_reflectionMap, i, m_reflectionMap, i+1);

		m_convolveMaterial.DisableKeyword("CP3");
#else
		ConvolveStep(reflectionMap0, 0, 0);
		RenderTexture.ReleaseTemporary(reflectionMap0);
		
		for(int i = 0, n = m_reflectionMap.width; (n >> i) > 1; ++i) 
			ConvolveStep(m_reflectionMap, i, i+1);
#endif
	}

#if PLANE_REFLECTION_CHEAPER
	void ConvolveStep(RenderTexture srcMap, int srcMip, RenderTexture dstMap, int dstMip) {
		var srcSize = srcMap.width >> srcMip;
		var tmp = RenderTexture.GetTemporary(srcSize >> 1, srcSize, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Default);

		if(dstMip == 0) {
			m_convolveMaterial.EnableKeyword("CP0");
		} else if(dstMip == 1) {
			m_convolveMaterial.DisableKeyword("CP0");
			m_convolveMaterial.EnableKeyword("CP1");
		} else if(dstMip == 2) {
			m_convolveMaterial.DisableKeyword("CP1");
			m_convolveMaterial.EnableKeyword("CP2");
		} else  {
			m_convolveMaterial.DisableKeyword("CP2");
			m_convolveMaterial.EnableKeyword("CP3");
		}


		var power = 2048 >> dstMip;
		m_convolveMaterial.SetFloat("_CosPower", (float)power / 1000f);
		
		m_convolveMaterial.SetFloat("_SampleMip", (float)srcMip);
		Graphics.SetRenderTarget(tmp, 0);
		Graphics.Blit(srcMap, m_convolveMaterial, 0);
		
		m_convolveMaterial.SetFloat("_SampleMip", 0f);
		Graphics.SetRenderTarget(dstMap, dstMip);
		// Graphics.Blit(tmp, m_convolveMaterial, 1);
		
		RenderTexture.ReleaseTemporary(tmp);
	}
#else
	void ConvolveStep(RenderTexture srcMap, int srcMip, int dstMip) {
		var srcSize = m_reflectionMap.width >> srcMip;
		var tmp = RenderTexture.GetTemporary(srcSize, srcSize, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Default);
		
		var power = 2048 >> dstMip;
		m_convolveMaterial.SetFloat("_CosPower", (float)power / 1000f);

		m_convolveMaterial.SetFloat("_SampleMip", (float)srcMip);
		Graphics.SetRenderTarget(tmp, 0);
		Graphics.Blit(srcMap, m_convolveMaterial, 0);
		
		m_convolveMaterial.SetFloat("_SampleMip", 0f);
		Graphics.SetRenderTarget(m_reflectionMap, dstMip);
		Graphics.Blit(tmp, m_convolveMaterial, 1);
		
		RenderTexture.ReleaseTemporary(tmp);
	}
#endif

	void OnRenderObject() {
		if(Camera.current != m_renderCamera)
			return;

		m_renderCamera = null;
	}

	Camera EnsureReflectionCamera(Camera renderCamera) {
		if(!m_reflectionCamera) {
			var goCam = new GameObject("#> _Planar Reflection Camera < ");
			goCam.hideFlags = HideFlags.DontSave | HideFlags.NotEditable | HideFlags.HideInHierarchy;

			m_reflectionCamera = goCam.AddComponent<Camera>();
			m_reflectionCamera.enabled = false;
		}

		if(renderCamera)
			m_reflectionCamera.CopyFrom(renderCamera);
		m_reflectionCamera.backgroundColor = clearColor;
		m_reflectionCamera.clearFlags = CameraClearFlags.SolidColor;
		m_reflectionCamera.depthTextureMode = useDepth ? DepthTextureMode.Depth : DepthTextureMode.None;
		m_reflectionCamera.useOcclusionCulling = false;

		return m_reflectionCamera;
	}
	
	// Given position/normal of the plane, calculates plane in camera space.
	static Vector4 CameraSpacePlane(Camera cam, Vector3 pos, Vector3 normal, float sideSign, float clipPlaneOffset) {
		Vector3 offsetPos = pos + normal * clipPlaneOffset;
		Matrix4x4 m = cam.worldToCameraMatrix;
		Vector3 cpos = m.MultiplyPoint( offsetPos );
		Vector3 cnormal = m.MultiplyVector( normal ).normalized * sideSign;
		return new Vector4( cnormal.x, cnormal.y, cnormal.z, -Vector3.Dot(cpos,cnormal) );
	}
	
	// Calculates reflection matrix around the given plane
	static void CalculateReflectionMatrix(ref Matrix4x4 reflectionMat, Vector4 plane) {
	    reflectionMat.m00 = (1F - 2F*plane[0]*plane[0]);
	    reflectionMat.m01 = (   - 2F*plane[0]*plane[1]);
	    reflectionMat.m02 = (   - 2F*plane[0]*plane[2]);
	    reflectionMat.m03 = (   - 2F*plane[3]*plane[0]);

	    reflectionMat.m10 = (   - 2F*plane[1]*plane[0]);
	    reflectionMat.m11 = (1F - 2F*plane[1]*plane[1]);
	    reflectionMat.m12 = (   - 2F*plane[1]*plane[2]);
	    reflectionMat.m13 = (   - 2F*plane[3]*plane[1]);
	
    	reflectionMat.m20 = (   - 2F*plane[2]*plane[0]);
    	reflectionMat.m21 = (   - 2F*plane[2]*plane[1]);
    	reflectionMat.m22 = (1F - 2F*plane[2]*plane[2]);
    	reflectionMat.m23 = (   - 2F*plane[3]*plane[2]);

    	reflectionMat.m30 = 0F;
    	reflectionMat.m31 = 0F;
    	reflectionMat.m32 = 0F;
    	reflectionMat.m33 = 1F;
	}

	void OnDrawGizmos() {
		Gizmos.DrawCube(transform.position - new Vector3(0f, 0.025f, 0f), new Vector3(2.0f, 0.05f, 2.0f));
		Gizmos.DrawSphere(transform.position, 0.5f);
	}
}
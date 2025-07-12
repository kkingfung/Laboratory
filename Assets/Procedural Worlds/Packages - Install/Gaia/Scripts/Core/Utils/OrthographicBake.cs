using Gaia.Pipeline.HDRP;
using Gaia.Pipeline.URP;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

#if PW_STORM_PRESENT
using ProceduralWorlds.Storm.Core;
#endif

#if UPPipeline
using UnityEngine.Rendering.Universal;
#endif
#if HDPipeline
using UnityEngine.Rendering.HighDefinition;
using static UnityEngine.Rendering.DebugUI;
#endif

namespace Gaia
{
    /// <summary>
    /// Utility class to provide the functionality of performing an "orthographic bake" from anywhere and return a render texture as result. 
    /// In an orthographic bake an orthographic camera is placed above the terrain pointing straight downwards to render the current view to a render texture.
    /// </summary>
    public class OrthographicBake
    {
        static Camera m_orthoCamera;
        public static RenderTexture m_tmpRenderTexture;
        private static string m_currentPath;
        private static List<Light> m_deactivatedLights = new List<Light>();
        private static GameObject m_bakeDirectionalLight;
        public static int m_HDLODBiasOverride = 1;

        private static GaiaSettings m_gaiaSettings;
        private static bool m_originalStormState;

        public static Camera GetOrthoCam()
        {
            return m_orthoCamera;
        }

        public static GaiaSettings GaiaSettings
        {
            get
            {
                if (m_gaiaSettings == null)
                {
                    m_gaiaSettings = GaiaUtils.GetGaiaSettings();
                }
                return m_gaiaSettings;
            }
        }

        public static Camera CreateOrthoCam(Vector3 position, float nearClipping, float farClipping, float size, LayerMask cullingMask, bool useStorm = false)
        {
            //existing ortho cam? Try to recycle
            GameObject gameObject = GameObject.Find("OrthoCaptureCam");

            if (gameObject == null)
            {
                gameObject = new GameObject("OrthoCaptureCam");
            }
            gameObject.transform.position = position;
            //facing straight downwards
            gameObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f);


            //existing Camera? Try to recycle
            Camera cam = gameObject.GetComponent<Camera>();

            if (cam == null)
            {
                cam = gameObject.AddComponent<Camera>();
            }

            //setup camera the way we need it for the ortho bake - adjust everything to default to make sure there is no interference
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.clear;
            cam.cullingMask = cullingMask;
            cam.orthographic = true;
            cam.orthographicSize = size;
            cam.nearClipPlane = nearClipping;
            cam.farClipPlane = farClipping;
            cam.rect = new Rect(0f, 0f, 1f, 1f);
            cam.depth = 0f;
            cam.renderingPath = RenderingPath.Forward; //Forward rendering required for orthographic
            cam.useOcclusionCulling = false;
#if UPPipeline
            if (m_gaiaSettings != null)
            {
                UniversalAdditionalCameraData UPAdditionalCameraData = cam.GetUniversalAdditionalCameraData();
                UPAdditionalCameraData.SetRenderer(m_gaiaSettings.m_URPOrthoBakeRendererIndex);
            }
#endif

#if HDPipeline
            HDAdditionalCameraData hdData = gameObject.GetComponent<HDAdditionalCameraData>();
            if (hdData == null)
            {
                hdData = cam.gameObject.AddComponent<UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData>();
            }
            hdData.volumeLayerMask = 1;
            hdData.backgroundColorHDR = Color.clear;
            hdData.clearColorMode = HDAdditionalCameraData.ClearColorMode.Color;

            FrameSettings frameSettings = new FrameSettings();
            frameSettings.lodBiasMode = LODBiasMode.OverrideQualitySettings;
            frameSettings.lodBias = m_HDLODBiasOverride;
#if UNITY_2022_3_OR_NEWER
            frameSettings.SetEnabled(FrameSettingsField.DecalLayers, true);
            frameSettings.SetEnabled(FrameSettingsField.RayTracing, true);
            frameSettings.SetEnabled(FrameSettingsField.CustomPass, false);
            frameSettings.SetEnabled(FrameSettingsField.Refraction, false);
            frameSettings.SetEnabled(FrameSettingsField.Distortion, false);
            frameSettings.SetEnabled(FrameSettingsField.StopNaN, false);
            frameSettings.SetEnabled(FrameSettingsField.DepthOfField, false);
            frameSettings.SetEnabled(FrameSettingsField.MotionBlur, false);
            frameSettings.SetEnabled(FrameSettingsField.PaniniProjection, false);
            frameSettings.SetEnabled(FrameSettingsField.Bloom, false);
            frameSettings.SetEnabled(FrameSettingsField.LensDistortion, false);
            frameSettings.SetEnabled(FrameSettingsField.ChromaticAberration, false);
            frameSettings.SetEnabled(FrameSettingsField.Vignette, false);
            frameSettings.SetEnabled(FrameSettingsField.ColorGrading, false);
            frameSettings.SetEnabled(FrameSettingsField.FilmGrain, false);
            frameSettings.SetEnabled(FrameSettingsField.Dithering, false);
            frameSettings.SetEnabled(FrameSettingsField.Antialiasing, false);
            frameSettings.SetEnabled(FrameSettingsField.Tonemapping, false);
            frameSettings.SetEnabled(FrameSettingsField.LensFlareDataDriven, false);
            frameSettings.SetEnabled(FrameSettingsField.AfterPostprocess, false);
            frameSettings.SetEnabled(FrameSettingsField.VirtualTexturing, false);
            frameSettings.SetEnabled(FrameSettingsField.Water, false);
            frameSettings.SetEnabled(FrameSettingsField.ShadowMaps, false);
            frameSettings.SetEnabled(FrameSettingsField.ContactShadows, false);
#if UNITY_6000_0_OR_NEWER
            frameSettings.SetEnabled(FrameSettingsField.AdaptiveProbeVolume, false);
#else
            frameSettings.SetEnabled(FrameSettingsField.ProbeVolume, false);
#endif
            frameSettings.SetEnabled(FrameSettingsField.Shadowmask, false);
            frameSettings.SetEnabled(FrameSettingsField.SSR, false);
            frameSettings.SetEnabled(FrameSettingsField.SSGI, false);
            frameSettings.SetEnabled(FrameSettingsField.SSAO, false);
            frameSettings.SetEnabled(FrameSettingsField.Transmission, true);
            frameSettings.SetEnabled(FrameSettingsField.Volumetrics, false);
            frameSettings.SetEnabled(FrameSettingsField.ReprojectionForVolumetrics, false);
            frameSettings.SetEnabled(FrameSettingsField.LightLayers, false);
            frameSettings.SetEnabled(FrameSettingsField.VolumetricClouds, false);
            frameSettings.SetEnabled(FrameSettingsField.AsyncCompute, false);
            frameSettings.SetEnabled(FrameSettingsField.LightListAsync, false);
            frameSettings.SetEnabled(FrameSettingsField.SSRAsync, false);
            frameSettings.SetEnabled(FrameSettingsField.SSAOAsync, false);
            frameSettings.SetEnabled(FrameSettingsField.ContactShadowsAsync, false);
            frameSettings.SetEnabled(FrameSettingsField.VolumeVoxelizationsAsync, false);
            frameSettings.SetEnabled(FrameSettingsField.FPTLForForwardOpaque, true);
            frameSettings.SetEnabled(FrameSettingsField.BigTilePrepass, true);
#if !UNITY_6000_0_OR_NEWER
            frameSettings.SetEnabled(FrameSettingsField.DeferredTile, true);
            frameSettings.SetEnabled(FrameSettingsField.ComputeLightEvaluation, true);
#endif

            frameSettings.SetEnabled(FrameSettingsField.ComputeLightVariants, true);
            frameSettings.SetEnabled(FrameSettingsField.ComputeMaterialVariants, true);
#endif
            hdData.customRenderingSettings = true;
            hdData.renderingPathCustomFrameSettings = frameSettings;
            hdData.renderingPathCustomFrameSettingsOverrideMask.mask[0] = true;

            hdData.renderingPathCustomFrameSettingsOverrideMask.mask[(int)FrameSettingsField.DecalLayers] = true;
            hdData.renderingPathCustomFrameSettingsOverrideMask.mask[(int)FrameSettingsField.RayTracing] = true;
            hdData.renderingPathCustomFrameSettingsOverrideMask.mask[(int)FrameSettingsField.CustomPass] = true;
            hdData.renderingPathCustomFrameSettingsOverrideMask.mask[(int)FrameSettingsField.Refraction] = true;
            hdData.renderingPathCustomFrameSettingsOverrideMask.mask[(int)FrameSettingsField.Distortion] = true;
            hdData.renderingPathCustomFrameSettingsOverrideMask.mask[(int)FrameSettingsField.StopNaN] = true;
            hdData.renderingPathCustomFrameSettingsOverrideMask.mask[(int)FrameSettingsField.DepthOfField] = true;
            hdData.renderingPathCustomFrameSettingsOverrideMask.mask[(int)FrameSettingsField.MotionBlur] = true;
            hdData.renderingPathCustomFrameSettingsOverrideMask.mask[(int)FrameSettingsField.PaniniProjection] = true;
            hdData.renderingPathCustomFrameSettingsOverrideMask.mask[(int)FrameSettingsField.Bloom] = true;
            hdData.renderingPathCustomFrameSettingsOverrideMask.mask[(int)FrameSettingsField.LensDistortion] = true;
            hdData.renderingPathCustomFrameSettingsOverrideMask.mask[(int)FrameSettingsField.ChromaticAberration] = true;
            hdData.renderingPathCustomFrameSettingsOverrideMask.mask[(int)FrameSettingsField.Vignette] = true;
            hdData.renderingPathCustomFrameSettingsOverrideMask.mask[(int)FrameSettingsField.ColorGrading] = true;
            hdData.renderingPathCustomFrameSettingsOverrideMask.mask[(int)FrameSettingsField.FilmGrain] = true;
            hdData.renderingPathCustomFrameSettingsOverrideMask.mask[(int)FrameSettingsField.Dithering] = true;
            hdData.renderingPathCustomFrameSettingsOverrideMask.mask[(int)FrameSettingsField.Antialiasing] = true;
            hdData.renderingPathCustomFrameSettingsOverrideMask.mask[(int)FrameSettingsField.Tonemapping] = true;
            hdData.renderingPathCustomFrameSettingsOverrideMask.mask[(int)FrameSettingsField.LensFlareDataDriven] = true;
            hdData.renderingPathCustomFrameSettingsOverrideMask.mask[(int)FrameSettingsField.AfterPostprocess] = true;
            hdData.renderingPathCustomFrameSettingsOverrideMask.mask[(int)FrameSettingsField.VirtualTexturing] = true;
            hdData.renderingPathCustomFrameSettingsOverrideMask.mask[(int)FrameSettingsField.Water] = true;
            hdData.renderingPathCustomFrameSettingsOverrideMask.mask[(int)FrameSettingsField.ShadowMaps] = true;
            hdData.renderingPathCustomFrameSettingsOverrideMask.mask[(int)FrameSettingsField.ContactShadows] = true;
#if UNITY_6000_0_OR_NEWER
            hdData.renderingPathCustomFrameSettingsOverrideMask.mask[(int)FrameSettingsField.AdaptiveProbeVolume] = true;
#else
            hdData.renderingPathCustomFrameSettingsOverrideMask.mask[(int)FrameSettingsField.ProbeVolume] = true;
#endif
            hdData.renderingPathCustomFrameSettingsOverrideMask.mask[(int)FrameSettingsField.Shadowmask] = true;
            hdData.renderingPathCustomFrameSettingsOverrideMask.mask[(int)FrameSettingsField.SSR] = true;
            hdData.renderingPathCustomFrameSettingsOverrideMask.mask[(int)FrameSettingsField.SSGI] = true;
            hdData.renderingPathCustomFrameSettingsOverrideMask.mask[(int)FrameSettingsField.SSAO] = true;
            hdData.renderingPathCustomFrameSettingsOverrideMask.mask[(int)FrameSettingsField.Transmission] = true;
            hdData.renderingPathCustomFrameSettingsOverrideMask.mask[(int)FrameSettingsField.Volumetrics] = true;
            hdData.renderingPathCustomFrameSettingsOverrideMask.mask[(int)FrameSettingsField.ReprojectionForVolumetrics] = true;
            hdData.renderingPathCustomFrameSettingsOverrideMask.mask[(int)FrameSettingsField.LightLayers] = true;
            hdData.renderingPathCustomFrameSettingsOverrideMask.mask[(int)FrameSettingsField.VolumetricClouds] = true;
            hdData.renderingPathCustomFrameSettingsOverrideMask.mask[(int)FrameSettingsField.AsyncCompute] = true;
            hdData.renderingPathCustomFrameSettingsOverrideMask.mask[(int)FrameSettingsField.LightListAsync] = true;
            hdData.renderingPathCustomFrameSettingsOverrideMask.mask[(int)FrameSettingsField.SSRAsync] = true;
            hdData.renderingPathCustomFrameSettingsOverrideMask.mask[(int)FrameSettingsField.SSAOAsync] = true;
            hdData.renderingPathCustomFrameSettingsOverrideMask.mask[(int)FrameSettingsField.ContactShadowsAsync] = true;
            hdData.renderingPathCustomFrameSettingsOverrideMask.mask[(int)FrameSettingsField.VolumeVoxelizationsAsync] = true;
            hdData.renderingPathCustomFrameSettingsOverrideMask.mask[(int)FrameSettingsField.FPTLForForwardOpaque] = true;
            hdData.renderingPathCustomFrameSettingsOverrideMask.mask[(int)FrameSettingsField.BigTilePrepass] = true;
#if !UNITY_6000_0_OR_NEWER
            hdData.renderingPathCustomFrameSettingsOverrideMask.mask[(int)FrameSettingsField.DeferredTile] = true;
            hdData.renderingPathCustomFrameSettingsOverrideMask.mask[(int)FrameSettingsField.ComputeLightEvaluation] = true;
#endif
            hdData.renderingPathCustomFrameSettingsOverrideMask.mask[(int)FrameSettingsField.ComputeLightVariants] = true;
            hdData.renderingPathCustomFrameSettingsOverrideMask.mask[(int)FrameSettingsField.ComputeMaterialVariants] = true;





            //hdData.renderingPathCustomFrameSettingsOverrideMask.mask[(int)FrameSettingsField.LODBiasMode] = true;
            //hdData.renderingPathCustomFrameSettingsOverrideMask.mask[(int)FrameSettingsField.LODBias] = true;
            //hdData.renderingPathCustomFrameSettingsOverrideMask.mask[(int)FrameSettingsField.PlanarProbe] = true;
            //hdData.renderingPathCustomFrameSettingsOverrideMask.mask[(int)FrameSettingsField.ReflectionProbe] = true;
            //hdData.renderingPathCustomFrameSettingsOverrideMask.mask[(int)FrameSettingsField.ExposureControl] = true;
            //hdData.renderingPathCustomFrameSettingsOverrideMask.mask[(int)FrameSettingsField.TransparentPostpass] = true;
#endif

            m_orthoCamera = cam;
#if PW_STORM_PRESENT
            if (useStorm)
            {

                if (StormWorld.Instance.isActiveAndEnabled)
                {
                    Debug.Log("Setting up camera for Storm Render");

                    StormWorld.Instance.CullingCamera = m_orthoCamera;
                    StormWorld.Instance.ForceFullRefresh(true);
                    int renders = 0;
                }
                else
                {
                    Debug.Log("Storm inactive - Skipping Storm for this capture");
                }
            }
#endif
            return cam;

        }

        public static void RemoveOrthoCam()
        {
            if (m_orthoCamera == null)
            {
                return;
            }

            if (m_orthoCamera.targetTexture != null)
            {
                RenderTexture.ReleaseTemporary(m_orthoCamera.targetTexture);
                //m_orthoCamera.targetTexture = null;
            }

            GameObject.DestroyImmediate(m_orthoCamera.gameObject);
        }

        /// <summary>
        /// PREPARES a terrain for baking - this will set up a camera that captures the terrain into a file or a render texture. After preparing the bake, you can still do other things to set the scene up for capturing, then use
        //  "PerformBakeNow" to write out the image / render texture.
        /// </summary>
        /// <param name="terrain"></param>
        /// <param name="Xresolution"></param>
        /// <param name="Yresolution"></param>
        /// <param name="cullingMask"></param>
        /// <param name="path"></param>
        /// <param name="useStorm"></param>
        public static void PrepareBakeTerrain(Terrain terrain, int Xresolution, int Yresolution, LayerMask cullingMask, string path = null, bool useStorm = false, bool sRGB= true)
        {
#if PW_STORM_PRESENT
            if (StormWorld.Instance != null)
            {
                m_originalStormState = StormWorld.Instance.isActiveAndEnabled;
                if (!useStorm)
                {
                    StormWorld.Instance.gameObject.SetActive(false);
                }
            }
#endif
            CreateOrthoCam(terrain.GetPosition() + new Vector3(terrain.terrainData.size.x / 2f, 0f, terrain.terrainData.size.z / 2f), -(terrain.terrainData.size.y + 200f), 1f, terrain.terrainData.size.x / 2f, cullingMask);
            RenderTextureDescriptor rtDesc = new RenderTextureDescriptor();
            rtDesc.autoGenerateMips = true;
            rtDesc.bindMS = false;
            rtDesc.colorFormat = RenderTextureFormat.ARGB32;
            rtDesc.depthBufferBits = 32;
            rtDesc.dimension = UnityEngine.Rendering.TextureDimension.Tex2D;
            rtDesc.enableRandomWrite = false;
            //rtDesc.graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R8_SRGB;
            rtDesc.height = Yresolution;
            rtDesc.memoryless = RenderTextureMemoryless.None;
            rtDesc.msaaSamples = 1;
            rtDesc.sRGB = sRGB;
            rtDesc.shadowSamplingMode = UnityEngine.Rendering.ShadowSamplingMode.None;
            rtDesc.useDynamicScale = false;
            rtDesc.useMipMap = false;
            rtDesc.volumeDepth = 1;
            rtDesc.vrUsage = VRTextureUsage.None;
            rtDesc.width = Xresolution;
            m_tmpRenderTexture = RenderTexture.GetTemporary(rtDesc);
            m_currentPath = path;
        }

        public static void PerformBakeNow()
        {
            if (m_currentPath != null)
            {
                RenderToPng(m_currentPath);
            }
            else
            {
                RenderToTemporary();
            }
            m_currentPath = null;
#if PW_STORM_PRESENT
            if (StormWorld.Instance != null)
            {
                StormWorld.Instance.gameObject.SetActive(m_originalStormState);
            }
#endif
        }

        private static void RenderToTemporary()
        {
            if (m_orthoCamera == null)
            {
                Debug.LogError("Orthographic Bake: Camera does not exist!");
                return;
            }

            m_orthoCamera.targetTexture = m_tmpRenderTexture;
            m_orthoCamera.Render();
            //In the SRPs we get a flipped image when rendering from a camera to a render textures, need to flip it on the Y-axis for our purposes
#if HDPipeline || UPPipeline
#if UPPipeline
      //In URP we only perform the flip if we are using deferred rendering, or when the "supports opaque texture" flag is on
      if (GaiaURPRuntimeUtils.GetRenderingPath() == RenderingMode.Deferred || GaiaURPRuntimeUtils.GetRenderingPath() == RenderingMode.ForwardPlus || (GaiaURPRuntimeUtils.GetRenderingPath() == RenderingMode.Forward && GaiaURPRuntimeUtils.SupportsOpaqueTexture())) 
      {
#endif
            Material flipMat = new Material(Shader.Find("Hidden/Gaia/FlipY"));
            flipMat.SetTexture("_InputTex", m_tmpRenderTexture);
            RenderTexture buffer = RenderTexture.GetTemporary(m_tmpRenderTexture.descriptor);
            Graphics.Blit(m_tmpRenderTexture, buffer, flipMat);
            Graphics.Blit(buffer, m_tmpRenderTexture);
            RenderTexture.ReleaseTemporary(buffer);
#if UPPipeline
      }
#endif
#endif
            RenderTexture.active = m_tmpRenderTexture;
        }

        private static void RenderToPng(string path)
        {
            RenderToTemporary();
            //ImageProcessing.WriteRenderTexture($"D:/{path.Substring(path.LastIndexOf("/"))}", m_tmpRenderTexture);
            ImageProcessing.WriteRenderTexture(path, m_tmpRenderTexture, GaiaConstants.ImageFileType.Png, TextureFormat.RGBA32);
            CleanUpRenderTexture();
        }

        /// <summary>
        /// switches off the water controlled by Gaia (if any)
        /// </summary>
        public static void WaterOff()
        {
            GameObject runtimeGO = GaiaUtils.GetRuntimeSceneObject();
            foreach (Transform t in runtimeGO.transform)
            {
                if (t.name.Contains("Water"))
                {
                    t.gameObject.SetActive(false);
                }
            }

        }

        /// <summary>
        /// switches off the water controlled by Gaia (if any)
        /// </summary>
        public static void WaterOn()
        {
            GameObject runtimeGO = GaiaUtils.GetRuntimeSceneObject();
            foreach (Transform t in runtimeGO.transform)
            {
                if (t.name.Contains("Water"))
                {
                    t.gameObject.SetActive(true);
                }
            }
        }

        /// <summary>
        /// switches off all active lights in the scene and stores the lights in a list to turn them back on later with LightsOn()
        /// </summary>
        public static void LightsOff()
        {
            m_deactivatedLights.Clear();
            var allLights = Resources.FindObjectsOfTypeAll<Light>();
            foreach (Light light in allLights)
            {
                if (light.isActiveAndEnabled)
                {
                    light.enabled = false;
                    m_deactivatedLights.Add(light);
                }
            }

            GameObject lightGO = GaiaUtils.GetLightingObject();
            if (lightGO != null)
            {
                for (int i = 0; i < lightGO.transform.childCount; i++)
                {
                    GameObject child = lightGO.transform.GetChild(i).gameObject;
                    child.SetActive(false);
                }
            }

        }

        /// <summary>
        /// turns all the lights on again that were disabled with LightsOff before
        /// </summary>
        public static void LightsOn()
        {
            foreach (Light light in m_deactivatedLights)
            {
                if (light != null)
                {
                    light.enabled = true;
                }
            }

            GameObject lightGO = GaiaUtils.GetLightingObject();
            if (lightGO != null)
            {
                for (int i = 0; i < lightGO.transform.childCount; i++)
                {
                    GameObject child = lightGO.transform.GetChild(i).gameObject;
                    child.SetActive(true);
                }
            }
        }

        public static void CleanUpRenderTexture()
        {
            m_orthoCamera.targetTexture = null;
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(m_tmpRenderTexture);
        }

        /// <summary>
        /// Creates a directional light pointing straight downwards on the y-axis with the given intensity & color. Use this together with LightsOn & LightsOff to better control lighting during the scene.
        /// </summary>
        /// <param name="intensity">Intensity for the directional light.</param>
        /// <param name="color">Color for the directional light.</param>
        public static void CreateBakeDirectionalLight(float intensity, Color color)
        {
            GameObject lightGO = GameObject.Find(GaiaConstants.BakeDirectionalLight);
            if (lightGO == null)
            {
                lightGO = new GameObject(GaiaConstants.BakeDirectionalLight);
            }
            m_bakeDirectionalLight = lightGO;
            Light light = lightGO.GetComponent<Light>();
            if (light == null)
            {
                light = lightGO.AddComponent<Light>();
            }
            light.shadows = LightShadows.None;
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(90, 0, 0);
            light.intensity = intensity;
#if HDPipeline
            HDAdditionalLightData lightData = light.GetComponent<HDAdditionalLightData>();
            if (lightData == null)
            {
                lightData = light.gameObject.AddComponent<HDAdditionalLightData>();
            }
            if (lightData != null)
            {
#if UNITY_6000_0_OR_NEWER
                light.lightUnit = LightUnit.Lux;
#else
                lightData.lightUnit = LightUnit.Lux;
#endif
                GaiaHDRPRuntimeUtils.SetLightIntensity(light, lightData, intensity);
            }

            //Add Volume component for basic  Visual Environment - othewise things like e.g. diffusion profiles do not render correctly into the bake 
            GameObject volumeGO = GameObject.Find(GaiaConstants.BakeVolume);
            if (volumeGO == null)
            {
                volumeGO = new GameObject(GaiaConstants.BakeVolume);
                volumeGO.transform.parent = lightGO.transform;
            }

            volumeGO.layer = 0;

            Volume volume = volumeGO.GetComponent<Volume>();
            if (volume == null)
            {
                volume = volumeGO.AddComponent<Volume>();
            }
#if UNITY_EDITOR
            volume.sharedProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(GaiaUtils.GetAssetPath(GaiaSettings.m_pipelineProfile.m_HDOrthoBakeVolumeObjectName + ".asset"));
#endif



#endif
            light.color = color;

        }

        /// <summary>
        /// Removes the bake directional light (Created with CreateBakeDirectionalLight()) from the scene again.
        /// </summary>
        public static void RemoveBakeDirectionalLight()
        {
            if (m_bakeDirectionalLight != null)
            {
                GameObject.DestroyImmediate(m_bakeDirectionalLight);
            }
        }
    }

}
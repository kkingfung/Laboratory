using System;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Rendering;
#endif
using UnityEngine;
using UnityEngine.Rendering;
#if UPPipeline
using UnityEngine.Rendering.Universal;
#endif

namespace Gaia
{
    public class GRC_UnityURPWater : GaiaRuntimeComponent
    {
        private const string URPWaterSystemHolderName = "Unity Water";
        public bool m_AddUnderWaterEffects = true;

        private GameObject m_currentWaterPrefab;
        private GameObject m_currentUnderwaterEffectsPrefab;
        private GUIContent m_generalHelpLink;

        private GRC_UnityURPWaterSettings m_waterSettings;
        public GRC_UnityURPWaterSettings WaterSettings
        {
            get
            {
                if (m_waterSettings == null)
                {
                    m_waterSettings = GetWaterSettings();
                }
                return m_waterSettings;
            }
        }

        private GUIContent m_panelLabel;
        public override GUIContent PanelLabel
        {
            get
            {
                if (m_panelLabel == null || m_panelLabel.text == "")
                {
                    m_panelLabel = new GUIContent("Unity URP Water", "Uses the Unity URP water system to create an ocean at the sea level of your scene. Only works in the Universal Render Pipeline.");
                }
                return m_panelLabel;
            }
        }

        public override void Initialize()
        {
            m_orderNumber = 500;

            if (m_generalHelpLink == null || m_generalHelpLink.text == "")
            {
                m_generalHelpLink = new GUIContent("Unity URP Water Module on Canopy", "Opens the Canopy Online Help Article for the Gaia Water Module");
            }
            if (WaterSettings != null)
            {
                m_currentWaterPrefab = WaterSettings.m_WaterPrefab;
                m_currentUnderwaterEffectsPrefab = WaterSettings.m_UnderWaterEffectsPrefab;
            }
        }

        public override void DrawUI()
        {
#if UNITY_EDITOR
            DisplayHelp("This runtime module will utilize the Unity URP water system to create an ocean at the current sea level of your scene. You can further enhance the water surface with additional tools from the URP water system, please see the link for more.", m_generalHelpLink, "https://canopy.procedural-worlds.com/library/tools/gaia-pro-2021/written-articles/creating_runtime/runtime-module-unity-urp-water-r174/");

            bool originalGUIState = GUI.enabled;

#if !UPPipeline
            EditorGUILayout.HelpBox("The Unity URP Water system requires the Universal render pipeline to be active in order to be used. Please install the Universal render pipeline in your project and configure Gaia to the Universal Pipeline from the Configuration tab.", MessageType.Warning);
            GUI.enabled = false;
#endif

            EditorGUI.BeginChangeCheck();
            {
                m_AddUnderWaterEffects = EditorGUILayout.Toggle(new GUIContent("Add Underwater Effects", "If enabled, an underwater effects prefab (defined in Water Settings) will be added as a child to the water object."), m_AddUnderWaterEffects);
                EditorGUILayout.Space();

                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Remove"))
                {
                    RemoveFromScene();
                }
                GUILayout.Space(15);
                if (GUILayout.Button("Apply"))
                {
                    AddToScene();
                }
                GUILayout.EndHorizontal();
            }
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(this);
            }

            GUI.enabled = originalGUIState;
#endif
        }

        public override void AddToScene()
        {
            ConfigureURPWater();

            // Load water prefab
            if (WaterSettings.m_WaterPrefab != null)
            {
                m_currentWaterPrefab = WaterSettings.m_WaterPrefab;
            }
            else
            {
                Debug.LogError("No water prefab was set in the Unity URP Water Settings. Please assign a water prefab in the scriptable object first before applying the water system.");
                return; // Essential: cannot proceed without water prefab
            }

            // Load underwater effects prefab if m_AddUnderWaterEffects is true
            m_currentUnderwaterEffectsPrefab = null; // Reset before attempting to load
            if (m_AddUnderWaterEffects)
            {
                if (WaterSettings.m_UnderWaterEffectsPrefab != null)
                {
                    m_currentUnderwaterEffectsPrefab = WaterSettings.m_UnderWaterEffectsPrefab;
                }
                else
                {
                    // Log a warning if effects are desired but prefab is missing. Water will still be added.
                    Debug.LogWarning("'Add Underwater Effects' is enabled, but no Underwater Effects Prefab is assigned in the Unity URP Water Settings. Underwater effects will not be added.");
                }
            }

            //Remove any old versions first
            RemoveFromScene();

            GameObject gaiaRuntimeObject = GaiaUtils.GetRuntimeSceneObject(true);

            // Create the parent holder GameObject
            GameObject waterSystemHolderGO = new GameObject(URPWaterSystemHolderName);
            waterSystemHolderGO.transform.SetParent(gaiaRuntimeObject.transform);

            float seaLevel = GaiaAPI.GetSeaLevel();
            waterSystemHolderGO.transform.position = new Vector3(0f, seaLevel, 0f);
            waterSystemHolderGO.transform.rotation = Quaternion.identity;
            waterSystemHolderGO.transform.localScale = Vector3.one;

            // Instantiate water prefab under the holder
            // m_currentWaterPrefab is guaranteed to be non-null here due to the early return.
            GameObject newWaterGO = Instantiate(m_currentWaterPrefab, waterSystemHolderGO.transform);
            newWaterGO.name = m_currentWaterPrefab.name; // Set name to prefab's original name

            // Instantiate underwater effects if enabled and prefab is available, also under the holder
            if (m_AddUnderWaterEffects && m_currentUnderwaterEffectsPrefab != null)
            {
                GameObject underwaterEffectsGO = Instantiate(m_currentUnderwaterEffectsPrefab, waterSystemHolderGO.transform);
                underwaterEffectsGO.name = m_currentUnderwaterEffectsPrefab.name; // Set name to prefab's original name

                Transform wallT = underwaterEffectsGO.transform.Find("Underwater Horizon/Horizon Wall");
                if (wallT != null)
                {
                    MeshRenderer mr = wallT.GetComponent<MeshRenderer>();
                    if (mr != null)
                    {
                        Material mat = mr.sharedMaterial;
                        if (mat != null)
                        {
                            mat.shader = Shader.Find("Universal Render Pipeline/Lit");
                        }
                    }
                }


            }

            // Deactivate sea plane on stamper, spawner, biome controller (ugly otherwise)
            // This part remains the same
            foreach (var spawner in FindObjectsByType<Spawner>(FindObjectsSortMode.None))
            {
                spawner.m_showSeaLevelPlane = false;
            }
            foreach (var stamper in FindObjectsByType<Stamper>(FindObjectsSortMode.None))
            {
                stamper.m_showSeaLevelPlane = false;
            }
            foreach (var biomeController in FindObjectsByType<BiomeController>(FindObjectsSortMode.None))
            {
                biomeController.m_showSeaLevelPlane = false;
            }
        }

        private void ConfigureURPWater()
        {
#if UPPipeline && UNITY_EDITOR
            UniversalRenderPipelineAsset rpAsset = GetURPAsset();
            if (rpAsset == null)
            {
                Debug.LogWarning("Gaia could not find an Universal Render Pipeline asset in the Graphics Settngs - the URP water might not work correctly!");
                return;
            }

            bool depthTextureEnabled = rpAsset.supportsCameraDepthTexture;
            bool opaqueTextureEnabled = rpAsset.supportsCameraOpaqueTexture;

            if (!depthTextureEnabled || !opaqueTextureEnabled)
            {
                if (EditorUtility.DisplayDialog("Render Pipeline Asset missing settings", "The URP Water system requires both 'Depth Texture' and 'Opaque Texture' to be enabled in your Universal Render Pipeline Asset. Without these settings, the water may not render correctly or be invisible.\r\n\r\nDo you want Gaia to enable these for you now?","Yes", "No"))
                {
                    rpAsset.supportsCameraDepthTexture = true;
                    rpAsset.supportsCameraOpaqueTexture = true;
                    EditorUtility.SetDirty(rpAsset);
                } 
            }
#endif
        }

#if UNITY_EDITOR && UPPipeline
        public UniversalRenderPipelineAsset GetURPAsset()
        {
            //Do we have a render pipeline asset at quality level? If yes, we take this one,
            //as it will override what is in the default settings in the "regular" pipeline asset
            UniversalRenderPipelineAsset asset = (UniversalRenderPipelineAsset)QualitySettings.GetRenderPipelineAssetAt(QualitySettings.GetQualityLevel());
            if (asset != null)
            {
                return asset;
            }
            //otherwise: Get the default asset
            if (GraphicsSettings.defaultRenderPipeline != null)
            {
                return (UniversalRenderPipelineAsset)GraphicsSettings.defaultRenderPipeline;
            }
            else
            {
                Debug.LogError("Error while getting Render Pipeline Asset for the URP Water Setup. Do you have a render pipeline asset assigned in the project Graphics Settings?");
                return null;
            }
        }
#endif

        public override void RemoveFromScene()
        {
            GameObject unityWaterSystemHolderObject = GaiaUtils.GetRuntimeChild(URPWaterSystemHolderName, false);

            if (unityWaterSystemHolderObject != null)
            {
                if (Application.isPlaying)
                {
                    GameObject.Destroy(unityWaterSystemHolderObject);
                }
                else
                {
                    GameObject.DestroyImmediate(unityWaterSystemHolderObject);
                }
            }

            //Re-activate sea plane on stamper, spawner, biome controller
            foreach (var spawner in FindObjectsByType<Spawner>(FindObjectsSortMode.None))
            {
                spawner.m_showSeaLevelPlane = true;
            }
            foreach (var stamper in FindObjectsByType<Stamper>(FindObjectsSortMode.None))
            {
                stamper.m_showSeaLevelPlane = true;
            }
            foreach (var biomeController in FindObjectsByType<BiomeController>(FindObjectsSortMode.None))
            {
                biomeController.m_showSeaLevelPlane = true;
            }
        }


        /// <summary>
        /// Return WaterSettings or null;
        /// </summary>
        /// <returns>Gaia settings or null if not found</returns>
        public static GRC_UnityURPWaterSettings GetWaterSettings()
        {
            return GaiaUtils.GetAsset("Unity URP Water Settings.asset", typeof(GRC_UnityURPWaterSettings)) as GRC_UnityURPWaterSettings;
        }
    }
}

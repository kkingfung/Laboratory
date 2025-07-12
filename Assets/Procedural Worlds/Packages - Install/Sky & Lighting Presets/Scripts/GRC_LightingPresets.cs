using ProceduralWorlds.Setup;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;



namespace Gaia
{
    public class lpBuiltInDropDownEntry { public int m_ID; public string m_name; public LightingPresetBuiltIn m_preset; }
    public class lpURPDropDownEntry { public int m_ID; public string m_name; public LightingPresetURP m_preset; }
    public class lpHDRPDropDownEntry { public int m_ID; public string m_name; public LightingPresetHDRP m_preset; }
    public class cameraDropDownEntry { public int m_ID; public Camera m_camera; }

    public class GRC_LightingPresets : GaiaRuntimeComponent
    {
        public bool m_addPPLayertoCamera = true;
        public bool m_addDynamicDepthOfField = true;
        public Camera m_camera;
        public bool m_quickBakeLighting = true;

        private int m_selectedID;
        private int m_cameraSelectedID;

        private List<LightingPresetBuiltIn> m_lightingPresetsBuiltIn = new List<LightingPresetBuiltIn>();
        private List<LightingPresetURP> m_lightingPresetsURP = new List<LightingPresetURP>();
        private List<LightingPresetHDRP> m_lightingPresetsHDRP = new List<LightingPresetHDRP>();

        private List<lpBuiltInDropDownEntry> m_lpBuiltInDropDownEntries = new List<lpBuiltInDropDownEntry>();
        private List<lpURPDropDownEntry> m_lpURPDropDownEntries = new List<lpURPDropDownEntry>();
        private List<lpHDRPDropDownEntry> m_lpHDRPDropDownEntries = new List<lpHDRPDropDownEntry>();

        private List<Camera> m_availableCameras = new List<Camera>();
        private List<cameraDropDownEntry> m_cameraDropDownEntries = new List<cameraDropDownEntry>();

        private GUIContent m_presetDropdownLabel;
        private GUIContent m_cameraDropdownLabel;
        private GUIContent m_generalHelpLink;

        private GUIContent m_panelLabel;
        public override GUIContent PanelLabel
        {
            get
            {
                if (m_panelLabel == null || m_panelLabel.text == "")
                {
                    m_panelLabel = new GUIContent("Lighting Presets", "Apply a lighting preset to your scene.");
                }
                return m_panelLabel;
            }
        }

        public override void Initialize()
        {
            m_orderNumber = 300;

            if (m_presetDropdownLabel == null || m_presetDropdownLabel.text == "")
            {
                m_presetDropdownLabel = new GUIContent("Lighting Preset", "Select a Lighting Preset from the list to apply to the scene as part of your lighting setup.");
            }
            if (m_generalHelpLink == null || m_generalHelpLink.text == "")
            {
                m_generalHelpLink = new GUIContent("Lighting Presets Module on Canopy", "Opens the Canopy Online Help Article for the Lighting Presets Module");
            }
            if (m_cameraDropdownLabel == null || m_cameraDropdownLabel.text == "")
            {
                m_cameraDropdownLabel = new GUIContent("Main Camera", "Select a Main Camera from the list to apply the DDOF Script to it.");
            }
            m_cameraSelectedID = -99;
            //Get all available cameras with main camera tag
            m_availableCameras.Clear();
            m_cameraDropDownEntries.Clear();
            Camera[] cameras = Camera.allCameras;
            for (int i = 0; i < cameras.Length; i++)
            {
                Camera cam = cameras[i];
                m_availableCameras.Add(cam);
                m_cameraDropDownEntries.Add(new cameraDropDownEntry { m_ID = i, m_camera = cam });
            }

            m_availableCameras.Sort(delegate (Camera x, Camera y)
            {
                if (x.name == null && y.name == null) return 0;
                else if (x.name == null) return -1;
                else if (y.name == null) return 1;
                else return x.name.CompareTo(y.name);
            });
            m_cameraDropDownEntries.Sort(delegate (cameraDropDownEntry x, cameraDropDownEntry y)
            {
                if (x.m_camera.name == null && y.m_camera.name == null) return 0;
                else if (x.m_camera.name == null) return -1;
                else if (y.m_camera.name == null) return 1;
                else return x.m_camera.name.CompareTo(y.m_camera.name);
            });

            for (int i = 0; i < m_cameraDropDownEntries.Count; i++)
            {
                cameraDropDownEntry entry = m_cameraDropDownEntries[i];
                entry.m_ID = i;
            }

            if (m_cameraSelectedID == -99)
            {
                m_cameraSelectedID = 0;
                for (int i = 0; i < m_availableCameras.Count; i++)
                {
                    Camera cam = m_availableCameras[i];
                    if (cam.gameObject.tag == "MainCamera")
                    {
                        m_cameraSelectedID = i;
                        break;
                    }
                }
            }
            //Get all lighting presets
#if UNITY_EDITOR

#if HDPipeline
            m_lightingPresetsHDRP.Clear();
            m_lpHDRPDropDownEntries.Clear();
            string[] allGUIDs = AssetDatabase.FindAssets("t:LightingPresetHDRP", new string[1] { SetupUtils.GetInstallRootPath() });
#elif UPPipeline
            m_lightingPresetsURP.Clear();
            m_lpURPDropDownEntries.Clear();
            string[] allGUIDs = AssetDatabase.FindAssets("t:LightingPresetURP", new string[1] { SetupUtils.GetInstallRootPath() });

#else
            m_lightingPresetsBuiltIn.Clear();
            m_lpBuiltInDropDownEntries.Clear();
            string[] allGUIDs = AssetDatabase.FindAssets("t:LightingPresetBuiltIn", new string[1] { SetupUtils.GetInstallRootPath() });
#endif


            for (int i = 0; i < allGUIDs.Length; i++)
            {

#if HDPipeline
                string guid = allGUIDs[i];
                LightingPresetHDRP lpHD = (LightingPresetHDRP)AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(guid), typeof(LightingPresetHDRP));
                m_lightingPresetsHDRP.Add(lpHD);
                m_lpHDRPDropDownEntries.Add(new lpHDRPDropDownEntry { m_ID = i, m_name = lpHD.m_displayName, m_preset = lpHD });
#elif UPPipeline
                string guid = allGUIDs[i];
                LightingPresetURP lpu = (LightingPresetURP)AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(guid), typeof(LightingPresetURP));
                m_lightingPresetsURP.Add(lpu);
                m_lpURPDropDownEntries.Add(new lpURPDropDownEntry { m_ID = i, m_name = lpu.m_displayName, m_preset = lpu });
#else
                string guid = allGUIDs[i];
                LightingPresetBuiltIn lpb = (LightingPresetBuiltIn)AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(guid), typeof(LightingPresetBuiltIn));
                m_lightingPresetsBuiltIn.Add(lpb);
                m_lpBuiltInDropDownEntries.Add(new lpBuiltInDropDownEntry { m_ID = i, m_name = lpb.m_displayName, m_preset = lpb });
#endif

            }

#if HDPipeline
            m_lightingPresetsHDRP.Sort(delegate (LightingPresetHDRP x, LightingPresetHDRP y)
            {
                if (x.m_displayName == null && y.m_displayName == null) return 0;
                else if (x.m_displayName == null) return -1;
                else if (y.m_displayName == null) return 1;
                else return x.m_displayName.CompareTo(y.m_displayName);
            });
            m_lpHDRPDropDownEntries.Sort(delegate (lpHDRPDropDownEntry x, lpHDRPDropDownEntry y)
            {
                if (x.m_preset.m_displayName == null && y.m_preset.m_displayName == null) return 0;
                else if (x.m_preset.m_displayName == null) return -1;
                else if (y.m_preset.m_displayName == null) return 1;
                else return x.m_preset.m_displayName.CompareTo(y.m_preset.m_displayName);
            });

            for (int i = 0; i < m_lpHDRPDropDownEntries.Count; i++)
            {
                lpHDRPDropDownEntry entry = m_lpHDRPDropDownEntries[i];
                entry.m_ID = i;
            }

#elif UPPipeline
            m_lightingPresetsURP.Sort(delegate (LightingPresetURP x, LightingPresetURP y)
            {
                if (x.m_displayName == null && y.m_displayName == null) return 0;
                else if (x.m_displayName == null) return -1;
                else if (y.m_displayName == null) return 1;
                else return x.m_displayName.CompareTo(y.m_displayName);
            });
            m_lpURPDropDownEntries.Sort(delegate (lpURPDropDownEntry x, lpURPDropDownEntry y)
            {
                if (x.m_preset.m_displayName == null && y.m_preset.m_displayName == null) return 0;
                else if (x.m_preset.m_displayName == null) return -1;
                else if (y.m_preset.m_displayName == null) return 1;
                else return x.m_preset.m_displayName.CompareTo(y.m_preset.m_displayName);
            });

            for (int i = 0; i < m_lpURPDropDownEntries.Count; i++)
            {
                lpURPDropDownEntry entry = m_lpURPDropDownEntries[i];
                entry.m_ID = i;
            }
#else

            m_lightingPresetsBuiltIn.Sort(delegate (LightingPresetBuiltIn x, LightingPresetBuiltIn y)
            {
                if (x.m_displayName == null && y.m_displayName == null) return 0;
                else if (x.m_displayName == null) return -1;
                else if (y.m_displayName == null) return 1;
                else return x.m_displayName.CompareTo(y.m_displayName);
            });
            m_lpBuiltInDropDownEntries.Sort(delegate (lpBuiltInDropDownEntry x, lpBuiltInDropDownEntry y)
            {
                if (x.m_preset.m_displayName == null && y.m_preset.m_displayName == null) return 0;
                else if (x.m_preset.m_displayName == null) return -1;
                else if (y.m_preset.m_displayName == null) return 1;
                else return x.m_preset.m_displayName.CompareTo(y.m_preset.m_displayName);
            });

            for (int i = 0; i < m_lpBuiltInDropDownEntries.Count; i++)
            {
                lpBuiltInDropDownEntry entry = m_lpBuiltInDropDownEntries[i];
                entry.m_ID = i;
            }
#endif

#endif

        }

        public override void DrawUI()
        {
            for (int i = 0; i < m_availableCameras.Count; i++)
            {
                if (m_availableCameras[i] == null)
                {
                    Initialize();
                }
            }

            if (m_availableCameras.Count != Camera.allCameras.Length)
            {
                Initialize();
            }

            bool originalGUIState = GUI.enabled;
#if UNITY_EDITOR
            EditorGUI.BeginChangeCheck();
            {
                string helpText = "The Lighting Preset Module allows you to quickly apply a pre-made lighting setup to your scene. This can be used to determine how your terrain would look like under different lighting conditions, or as a starting point to develop your lighting setup for the scene. It is possible to develop your own presets to the selection as well.";
                DisplayHelp(helpText, m_generalHelpLink, "https://canopy.procedural-worlds.com/library/tools/gaia-pro-2021/written-articles/creating_runtime/runtime-module-lighting-presets-r163/");
#if HDPipeline
                m_selectedID = EditorGUILayout.IntPopup("Lighting Preset", m_selectedID, m_lpHDRPDropDownEntries.Select(x => x.m_name).ToArray(), m_lpHDRPDropDownEntries.Select(x => x.m_ID).ToArray());
#elif UPPipeline
                m_selectedID = EditorGUILayout.IntPopup("Lighting Preset", m_selectedID, m_lpURPDropDownEntries.Select(x => x.m_name).ToArray(), m_lpURPDropDownEntries.Select(x => x.m_ID).ToArray());
#else
                m_selectedID = EditorGUILayout.IntPopup("Lighting Preset", m_selectedID, m_lpBuiltInDropDownEntries.Select(x => x.m_name).ToArray(), m_lpBuiltInDropDownEntries.Select(x => x.m_ID).ToArray());
#endif
                DisplayHelp("Select the lighting preset you want to apply here.");

                GUI.enabled = originalGUIState;
                m_addDynamicDepthOfField = EditorGUILayout.Toggle("Add DDOF Script", m_addDynamicDepthOfField);
                DisplayHelp("When enabled Gaia will add a Dynamic Depth of Field Script which will override the default Depth of Field settings to be more dynamic.");

                GUI.enabled = m_addDynamicDepthOfField;

                if (m_cameraSelectedID == -99)
                {
                    m_cameraSelectedID = 0;
                    for (int i = 0; i < m_availableCameras.Count; i++)
                    {
                        Camera cam = m_availableCameras[i];
                        if (cam.gameObject.tag == "MainCamera")
                        {
                            m_cameraSelectedID = i;
                            break;
                        }
                    }
                }
                m_cameraSelectedID = EditorGUILayout.IntPopup("Main Camera", m_cameraSelectedID, m_cameraDropDownEntries.Select(x => x.m_camera.name).ToArray(), m_cameraDropDownEntries.Select(x => x.m_ID).ToArray());
                DisplayHelp("Select the Main Camera to apply the DDOF script to it.");


#if !UNITY_POST_PROCESSING_STACK_V2
                GUI.enabled = false;
#endif
                m_addPPLayertoCamera = EditorGUILayout.Toggle("Add PP Layer", m_addPPLayertoCamera);
                DisplayHelp("Should Gaia automatically add a Post-Processing Layer component to the camera? Without the Post-Processing Layer on the camera, Post-Processing Effects do not work in the built-in rendering pipeline.");
                GUI.enabled = originalGUIState;

#if UPPipeline
                m_quickBakeLighting = EditorGUILayout.Toggle("Quick Bake Lighting", m_quickBakeLighting);
                DisplayHelp("Generates the basic light map data for this scene from the Rendering > Lighting window. This will result in much nicer looking shadows, because they will take a bit of the ambient environment lighting into account instead of being pitch black.");
#endif


                GUI.enabled = originalGUIState;
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Space(15);
                if (GUILayout.Button("Remove"))
                {
                    RemoveFromScene();
                }

                if (m_addDynamicDepthOfField == false)
                {
                    GUI.enabled = true;
                }
                else if (m_addDynamicDepthOfField = true && m_availableCameras.Count == 0)
                {
                    GUI.enabled = false;
                }
                else if (m_addDynamicDepthOfField = true && m_availableCameras.Count > 0)
                {
                    GUI.enabled = true;
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
#endif
        }

        public override void AddToScene()
        {
#if HDPipeline
            m_lightingPresetsHDRP[m_selectedID].m_enableDynamicDepthOfField = m_addDynamicDepthOfField;
            if (m_addDynamicDepthOfField == true)
            {
             if(m_availableCameras[m_cameraSelectedID] != null)
                {
                     m_lightingPresetsHDRP[m_selectedID].m_mainCamera = m_availableCameras[m_cameraSelectedID];
                }
                else
                {
                    Debug.LogError("No Camera Selected, Please add one");
                }  
                
            }
            m_lightingPresetsHDRP[m_selectedID].Apply();
#elif UPPipeline
            m_lightingPresetsURP[m_selectedID].m_enableDynamicDepthOfField = m_addDynamicDepthOfField;
            if (m_addDynamicDepthOfField == true)
            {
                if(m_availableCameras[m_cameraSelectedID] != null)
                {
                    m_lightingPresetsURP[m_selectedID].m_mainCamera = m_availableCameras[m_cameraSelectedID];
                }
                else
                {
                    Debug.LogError("No Camera Selected, Please add one or Desable the DDOF Script CheckBox");
                    return;
                }
            }
            m_lightingPresetsURP[m_selectedID].Apply(m_addPPLayertoCamera);

            if (m_quickBakeLighting)
            {
#if UNITY_EDITOR
                Lightmapping.bakedGI = false;
                Lightmapping.realtimeGI = false;
#if !UNITY_2023_2_OR_NEWER
            Lightmapping.giWorkflowMode = Lightmapping.GIWorkflowMode.OnDemand;
#endif
                Lightmapping.BakeAsync();
#endif
            }
#else
            m_lightingPresetsBuiltIn[m_selectedID].m_enableDynamicDepthOfField = m_addDynamicDepthOfField;
            if (m_addDynamicDepthOfField == true)
            {
                if (m_availableCameras[m_cameraSelectedID] != null)
                {
                    m_lightingPresetsBuiltIn[m_selectedID].m_mainCamera = m_availableCameras[m_cameraSelectedID];
                }
                else
                {
                    Debug.LogError("No Camera Selected, Please add one");
                }
            }

            m_lightingPresetsBuiltIn[m_selectedID].Apply(m_addPPLayertoCamera);
#endif
        }

        public override void RemoveFromScene()
        {
#if HDPipeline
            m_lightingPresetsHDRP[m_selectedID].RemoveFromScene();
#elif UPPipeline
            m_lightingPresetsURP[m_selectedID].RemoveFromScene();
#else
            m_lightingPresetsBuiltIn[m_selectedID].RemoveFromScene();
#endif
        }

    }
}

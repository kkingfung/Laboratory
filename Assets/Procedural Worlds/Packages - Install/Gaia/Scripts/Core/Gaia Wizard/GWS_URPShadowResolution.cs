using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using System;
#if UPPipeline
using UnityEngine.Rendering.Universal;
#endif
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Gaia
{
    public class GWS_URPShadowResolution : GWSetting
    {
        #if UPPipeline
        [SerializeField]
        float m_originalShadowDistanceValue = 300;
        float m_targetShadowDistanceValue = 300;

        [SerializeField]
        int m_originalShadowResolutionValue = 4096;
        int m_targetShadowResolutionValue = 4096;
#endif

        private void OnEnable()
        {
            m_RPBuiltIn = false;
            m_RPHDRP = false;
            m_RPURP = true;
            m_name = "Shadow Resolution";
            m_infoTextOK = $"The shadow resolution settings are set up well for an outdoor environment in URP.";
            m_infoTextIssue = $"The combination of max shadow distance, shadow resolution and shadow cascades will cause rough or pixelated shadows in your scene, this can cause flickering with wind movement. Please try to adjust your settings to allow for more resolution up close on the shadow texture.";
            m_link = "https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@17.0/manual/universalrp-asset.html#shadows";
            m_linkDisplayText = "Shadow settings in the URP Manual";
            m_canRestore = true;
            Initialize();
        }

        public override bool PerformCheck()
        {

#if UPPipeline
            UniversalRenderPipelineAsset urpAsset = (UniversalRenderPipelineAsset)GaiaUtils.GetRenderPipelineAsset();

            if (urpAsset != null)
            {
                // Shadow Distance
                float maxShadowDistance = urpAsset.shadowDistance;

                // Main Light Shadow Settings
                var mainLightShadows = urpAsset.supportsMainLightShadows;
                var mainLightShadowResolution = urpAsset.mainLightShadowmapResolution;
                if ((mainLightShadowResolution / maxShadowDistance) < 10)
                {
                    Status = GWSettingStatus.Warning;
                    return true;
                }
            }
            
#endif
            Status = GWSettingStatus.OK;
            return false;
        }

        public override bool FixNow(bool autoFix = false)
        {
#if UNITY_EDITOR && UPPipeline
#if UNITY_6000_0_OR_NEWER
            if (autoFix || EditorUtility.DisplayDialog("Set Shadow Resolution Values?",
            $"Do you want to set the shadow draw distance and resolution and cascade settings in the pipeline asset now?",
            "Continue", "Cancel"))
            {

                UniversalRenderPipelineAsset urpAsset = (UniversalRenderPipelineAsset)GaiaUtils.GetRenderPipelineAsset();

                if (urpAsset != null)
                {
                    m_originalShadowDistanceValue = urpAsset.shadowDistance;
                    urpAsset.shadowDistance = m_targetShadowDistanceValue;

                    m_originalShadowResolutionValue = urpAsset.mainLightShadowmapResolution;
                    urpAsset.mainLightShadowmapResolution = m_targetShadowResolutionValue;

                    urpAsset.shadowCascadeCount = 4;
                    urpAsset.cascade4Split = new Vector3(0.05f, 0.25f, 0.75f);

                    PerformCheck();
                    m_foldedOut = false;
                    return true;
                }
                else
                {
                    Debug.LogError("Error while accessing the URP Render Pipeline asset - is there a URP render pipeline asset assigned in the Project > Graphics settings?");
                }

            }
#else
            if (!autoFix)
            { 
                EditorUtility.DisplayDialog("Auto-Fix not possible!","Gaia can only fix this issue in Unity 6 or higher, because some of the render pipeline asset settings are not accessible via script in earlier versions\r\n\r\n. " +
                "To fix this issue manually, please take a look at the Shadow settings in your render pipeline asset and set up the following values:\r\n\r\n" +
                "Shadow Resolution: 4096 or higher\r\n" +
                "Max Shadow Distance: 300 or lower\r\n\r\n" +
                "Then adjust the shadow cascades while reviewing the shadows in your scene view.", "OK");
            }
#endif
#endif
            return false;
        }

        public override string GetOriginalValueString()
        {
#if UPPipeline
            return $"Distance: {m_originalShadowDistanceValue}, Resolution: {m_originalShadowResolutionValue}";
#else
            return "URP Pipeline not found!";
#endif
        }

        public override bool RestoreOriginalValue()
        {
#if UNITY_6000_0_OR_NEWER && UNITY_EDITOR && UPPipeline
            if (EditorUtility.DisplayDialog("Restore Shadow Draw Distance?",
            $"Do you want to restore he shadow draw distance, resolution and cascade settings to their original values {GetOriginalValueString()} in the render pipeline asset now?",
            "Continue", "Cancel"))
            {
                UniversalRenderPipelineAsset urpAsset = (UniversalRenderPipelineAsset)GaiaUtils.GetRenderPipelineAsset();

                if (urpAsset != null)
                {
                    urpAsset.shadowDistance = m_originalShadowDistanceValue;
                    urpAsset.mainLightShadowmapResolution = m_originalShadowResolutionValue;
                    return true;
                }
                else
                {
                    Debug.LogError("Error while accessing the URP Render Pipeline asset - is there a URP render pipeline asset assigned in the Project > Graphics settings?");
                }
                return false;
            }
#endif
            return false;
        }


    }
}

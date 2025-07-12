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
    public class GWS_URPShadowDistance : GWSetting
    {
        [SerializeField]
        float m_originalValue = 300;
        float m_targetValue = 300;

        private void OnEnable()
        {
            m_RPBuiltIn = false;
            m_RPHDRP = false;
            m_RPURP = true;
            m_name = "Shadow Distance";
            m_infoTextOK = $"The max shadow draw distance in the render pipeline asset is higher than {m_targetValue} meters - this should be adequate for outdoor environments.";
            m_infoTextIssue = $"The max shadow distance in the render pipeline asset is set to lower than {m_targetValue} meters - this is not good for rendering outdoor environments, as missing shadows in the distance are very noticeable. Unless for performance reasons, try to increase the value while watching the shadows on distant objects like trees to find a good value.";
            m_link = "https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@17.0/manual/universalrp-asset.html#shadows";
            m_linkDisplayText = "Shadow settings in the URP Manual";
            m_canRestore = true;
            Initialize();
        }

        public override bool PerformCheck()
        {

#if UPPipeline
            UniversalRenderPipelineAsset urpAsset = (UniversalRenderPipelineAsset)GaiaUtils.GetRenderPipelineAsset();

            if (urpAsset != null ) 
            {
                if (urpAsset.shadowDistance < m_targetValue)
                {
                    Status = GWSettingStatus.Warning;
                    return false;
                }
            }
            
#endif
            Status = GWSettingStatus.OK;
            return false;
        }

        public override bool FixNow(bool autoFix = false)
        {
#if UNITY_EDITOR && UPPipeline
            if (autoFix || EditorUtility.DisplayDialog("Set Max Shadow Draw Distance?",
            $"Do you want to set the shadow draw distance to {m_targetValue} in the render pipeline asset now?",
            "Continue", "Cancel"))
            {

                UniversalRenderPipelineAsset urpAsset = (UniversalRenderPipelineAsset)GaiaUtils.GetRenderPipelineAsset();

                if (urpAsset != null)
                {
                    m_originalValue = urpAsset.shadowDistance;
                    urpAsset.shadowDistance = m_targetValue;
                    PerformCheck();
                    m_foldedOut = false;
                    return true;
                }
                else
                {
                    Debug.LogError("Error while accessing the URP Render Pipeline asset - is there a URP render pipeline asset assigned in the Project > Graphics settings?");
                }

            }
#endif
            return false;
        }

        public override string GetOriginalValueString()
        {
            return m_originalValue.ToString();
        }

        public override bool RestoreOriginalValue()
        {
#if UNITY_EDITOR && UPPipeline
            if (EditorUtility.DisplayDialog("Restore Shadow Draw Distance?",
            $"Do you want to restore the max shadow draw distance to its original value {m_originalValue} in the render pipeline asset now?",
            "Continue", "Cancel"))
            {
                UniversalRenderPipelineAsset urpAsset = (UniversalRenderPipelineAsset)GaiaUtils.GetRenderPipelineAsset();

                if (urpAsset != null)
                {
                    urpAsset.shadowDistance = m_originalValue;
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

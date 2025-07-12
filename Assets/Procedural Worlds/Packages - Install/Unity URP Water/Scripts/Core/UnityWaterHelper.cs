using UnityEngine;
using Gaia;

public class UnityWaterHelper : MonoBehaviour
{
    // Reference to the script that broadcasts the underwater state
    private GaiaURPUnderwaterEffects m_gaiaUnderwaterEffects;
    private GameObject m_waterGameobject;

    void Awake()
    {
        m_gaiaUnderwaterEffects = GetComponentInChildren<GaiaURPUnderwaterEffects>();
        
        if (transform.gameObject != null)
        {
            m_waterGameobject = transform.gameObject;
        }
    }

    void OnEnable()
    {
        // Subscribe to the static event from GaiaURPUnderwaterEffects
        GaiaURPUnderwaterEffects.OnUnderwaterStatusChanged += HandleGaiaUnderwaterStateChanged;
        
        if (m_gaiaUnderwaterEffects != null)
        {
            HandleGaiaUnderwaterStateChanged(m_gaiaUnderwaterEffects.IsUnderwater);
        }
        else if (GaiaURPUnderwaterEffects.Instance != null)
        {
            HandleGaiaUnderwaterStateChanged(GaiaURPUnderwaterEffects.Instance.IsUnderwater);
        }
    }

    void OnDisable()
    {
        // Unsubscribe from the event to prevent memory leaks and errors
        GaiaURPUnderwaterEffects.OnUnderwaterStatusChanged -= HandleGaiaUnderwaterStateChanged;
    }

    /// <summary>
    /// This method is called whenever the underwater state changes.
    /// </summary>
    /// <param name="isNowUnderwater">The new underwater state.</param>
    private void HandleGaiaUnderwaterStateChanged(bool isNowUnderwater)
    {
        if (isNowUnderwater)
        {
            if (m_waterGameobject)
            {
                m_waterGameobject.transform.rotation = Quaternion.Euler(180f, 0f, 0f);
            }
        }
        else
        {
            if (m_waterGameobject)
            {
                m_waterGameobject.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            }
        }
    }

}
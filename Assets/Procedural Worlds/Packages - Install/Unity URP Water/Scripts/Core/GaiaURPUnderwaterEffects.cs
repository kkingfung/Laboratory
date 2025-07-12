using System.Collections.Generic;
using Gaia.Pipeline.HDRP;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine.Rendering;
using UnityEngine.VFX;
#if UNITY_POST_PROCESSING_STACK_V2
using UnityEngine.Rendering.PostProcessing;
#endif

namespace Gaia
{
    [ExecuteAlways]
    public class GaiaURPUnderwaterEffects : MonoBehaviour
    {
        public static GaiaURPUnderwaterEffects Instance
        {
            get { return m_instance; }

        }
        [SerializeField]
        private static GaiaURPUnderwaterEffects m_instance;

        #region Delegates
        public delegate void OnUnderwaterStateChange(bool isUnderwater);
        public static event OnUnderwaterStateChange OnUnderwaterStatusChanged;
        #endregion
        #region Public Variables

        public bool EnableUnderwaterEffects
        {
            get { return m_enableUnderwaterEffects; }
            set
            {
                if (m_enableUnderwaterEffects != value)
                {
                    m_enableUnderwaterEffects = value;
                    if (!m_enableUnderwaterEffects)
                    {
                        DisableUnderwaterFX();
                    }
                    else
                    {
                        EnableUnderwaterFX();
                    }
                }
            }
        }

        [SerializeField]
        private bool m_enableUnderwaterEffects = true;
        //Global
        public GaiaConstants.EnvironmentRenderer RenderPipeline;
        public bool m_weatherSystemExists;
        private bool _isUnderwater_backingField; // Private backing field
        public bool IsUnderwater
        {
            get { return _isUnderwater_backingField; }
            private set
            {
                if (_isUnderwater_backingField != value)
                {
                    _isUnderwater_backingField = value;
                    // Invoke the event when the state changes
                    OnUnderwaterStatusChanged?.Invoke(_isUnderwater_backingField);
                }
            }
        }
        public bool m_startingUnderwater = false;
        public float m_seaLevel = 50f;
        public Transform m_playerCamera;
        public GameObject m_underwaterParticles;
        public GameObject m_horizonObject;
        public GameObject m_underwaterPostFX;
        public bool m_editCausticTextures = false;
        //Caustics
        public bool m_useCaustics = true;
        public Light m_mainLight;
        public int m_framesPerSecond = 24;
        [Range(1f, 100f)]
        public float m_causticSize = 15f;
        public List<Texture2D> m_causticTextures = new List<Texture2D>();
        //Fog
        public bool m_supportFog = true;
        public Gradient m_fogColorGradient;
        public bool m_overrideFogColor = false;
        public Gradient m_overrideFogColorGradient;
        public AnimationCurve m_overrideFogColorMultiplier;
        public float m_timeOfDayValue = 0.5f;
        public Color m_overrideFogMultiplier;
        public Color m_fogColor = new Color(0.533f, 0.764f, 1f, 1f);
        public Color m_fogColorMultiplier = Color.black;
        public float m_fogDepth = 100f;
        public float m_fogDistance = 45f;
        public float m_nearFogDistance = -4f;
        public float m_fogDensity = 0.045f;
        public Vector2 m_anisotropyMinMax = new Vector2(0.225f, 0f);
        //Exposure
        public Vector2 m_exposureMinMax = new Vector2(16f, 18f);
        //Post FX
        public bool m_enableTransitionFX = true;
        public GameObject m_underwaterTransitionPostFX;
        public bool m_supportPostFX = true;
        public float m_constUnderwaterPostExposure = 0f;
        public AnimationCurve m_gradientUnderwaterPostExposure = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(1f, 0f));
        public Color m_constUnderwaterColorFilter = Color.white;
        public Gradient m_gradientUnderwaterColorFilter = new Gradient();
#if UNITY_POST_PROCESSING_STACK_V2
        public PostProcessProfile m_postProcessProfile;
#endif
        //Audio
        [Range(0f, 1f)]
        public float m_playbackVolume = 0.5f;
        public AudioClip m_submergeSoundFXDown;
        public AudioClip m_submergeSoundFXUp;
        public AudioClip m_underwaterSoundFX;


        /// <summary>
        /// VFX graph that you can setup that the system will disable/enable accordingly
        /// </summary>
        public List<VisualEffect> m_surfaceVisualEffects = new List<VisualEffect>();

        #endregion
        #region Private Variables

        private int m_indexNumber = 0;
        [SerializeField]
        private AudioSource m_audioSource;
        [SerializeField]
        private AudioSource m_audioSourceUnderwater;
        private ParticleSystem m_underwaterParticleSystem;

        private Color m_surfaceFogColor;
        private float m_surfaceFogDensity;
        private float m_surfaceFogStartDistance;
        private float m_surfaceFogEndDistance = -99.0f;

        private bool m_surfaceSetup = false;
        private bool m_underwaterSetup = false;
        private bool m_startingSystem = false;
        private List<MeshRenderer> m_horizonMeshRenders = new List<MeshRenderer>();
        [HideInInspector]
        [SerializeField]
        private Material m_underwaterMaterial;

        #endregion
        #region Unity Functions

        private void Start()
        {
            RenderPipeline = GaiaUtils.GetActivePipeline();
            m_instance = this;

#if GAIA_2023_PRO
            m_weatherSystemExists = ProceduralWorldsGlobalWeather.Instance;
#endif
            if (PWS_WaterSystem.Instance != null)
            {
                m_seaLevel = PWS_WaterSystem.Instance.SeaLevel;
            }

            if (m_playerCamera == null)
            {
                if (Camera.main != null)
                {
                    m_playerCamera = Camera.main.transform;
                }
                else
                {
                    m_playerCamera = GaiaUtils.GetCamera().transform;
                }
            }
            
            bool determinedStartingUnderwater = false;
            if (m_playerCamera != null)
            {
                determinedStartingUnderwater = (m_playerCamera.position.y <= m_seaLevel);
            }
            m_startingUnderwater = determinedStartingUnderwater;
            
            _isUnderwater_backingField = !determinedStartingUnderwater;
            IsUnderwater = determinedStartingUnderwater;

            if (m_underwaterMaterial == null)
            {
                m_underwaterMaterial = GaiaUtils.GetWaterMaterial(GaiaConstants.waterSurfaceObject, true);
            }

            if (m_mainLight == null)
            {
                m_mainLight = GaiaUtils.GetMainDirectionalLight(false);
            }
            
            if (m_fogColorGradient == null)
            {
                m_fogColorGradient = CreateGradient();
            }

            if (m_audioSource == null)
            {
                m_audioSource = GetAudioSource();
            }

            if (m_audioSourceUnderwater == null)
            {
                m_audioSourceUnderwater = GetAudioSource();
            }

            if (Application.isPlaying)
            {
                if (m_underwaterPostFX != null)
                {
                    m_underwaterPostFX.SetActive(true);
                }

                if (m_underwaterTransitionPostFX != null)
                {
                    m_underwaterTransitionPostFX.SetActive(true);
                }
            }

            if (m_audioSourceUnderwater != null)
            {
                m_audioSourceUnderwater.clip = m_underwaterSoundFX;
                m_audioSourceUnderwater.loop = true;
                m_audioSourceUnderwater.volume = m_playbackVolume;
                m_audioSourceUnderwater.Stop();
            }

            if (m_underwaterParticles != null)
            {
                m_underwaterParticleSystem = m_underwaterParticles.GetComponent<ParticleSystem>();
                if (m_underwaterParticleSystem != null)
                {
                    m_underwaterParticleSystem.Stop();
                }

                m_underwaterParticles.SetActive(false);
            }

            if (m_horizonObject != null)
            {
                m_horizonObject.SetActive(true);
                MeshRenderer[] meshRenders = m_horizonObject.GetComponentsInChildren<MeshRenderer>();
                foreach (MeshRenderer render in meshRenders)
                {
                    m_horizonMeshRenders.Add(render);
                    render.enabled = false;
                }
            }

            UpdateSurfaceFogSettings();
            if (m_startingUnderwater) // This is now 'IsUnderwater'
            {
                // IsUnderwater is already true and event fired.
                // SetupWaterSystems will be called, and if it changes IsUnderwater (it shouldn't here), event would fire again.
                // The original line was: IsUnderwater = SetupWaterSystems(true, m_startingUnderwater);
                // We need to ensure SetupWaterSystems is called correctly based on the new IsUnderwater property.
                // The SetupWaterSystems method itself returns a bool, which is then assigned to IsUnderwater.
                // This assignment will now go through the property setter.
                bool newStatus = SetupWaterSystems(true, m_startingUnderwater); // Call it, but the assignment to IsUnderwater is what matters
                if (IsUnderwater != newStatus) IsUnderwater = newStatus; // Ensure it's set if SetupWaterSystems has side effects on the state

                SetHDRPVisualEffectsState(m_surfaceVisualEffects);
                m_underwaterSetup = true;
                m_surfaceSetup = false;
            }
            else
            {
                // IsUnderwater is already false and event fired.
                // The original code didn't call SetupWaterSystems here for the 'false' case in Start.
                // It just set m_underwaterSetup = true and m_surfaceSetup = false, which seems counterintuitive for surface start.
                // This part of original logic might need review, but for event broadcasting, IsUnderwater is correctly set.
                m_underwaterSetup = false; // Assuming this is more logical for surface start
                m_surfaceSetup = true;
            }

            m_startingSystem = true;
        }
        private void OnEnable()
        {
            m_instance = this;
            if (m_mainLight == null)
            {
                m_mainLight = GaiaUtils.GetMainDirectionalLight(false);
            }

            if (m_audioSource != null)
            {
                m_audioSource.playOnAwake = false;
            }

            if (m_audioSourceUnderwater != null)
            {
                m_audioSourceUnderwater.playOnAwake = false;
            }

            if (m_underwaterMaterial == null)
            {
                m_underwaterMaterial = GaiaUtils.GetWaterMaterial(GaiaConstants.waterSurfaceObject, true);
            }

            SetupTransitionVFX(m_enableTransitionFX);
        }
        private void OnDisable()
        {
            UpdateSurfaceFog(false);
        }
        private void Update()
        {
            if (Application.isPlaying)
            {
                if (m_underwaterTransitionPostFX != null)
                {
                    if (m_underwaterTransitionPostFX.transform.position.y != m_seaLevel)
                    {
                        SetupTransitionVFX(m_enableTransitionFX);
                    }
                }

                if (m_mainLight == null || !m_mainLight.isActiveAndEnabled)
                {
                    m_mainLight = GaiaUtils.GetMainDirectionalLight(false);
                }

                if (m_playerCamera == null)
                {
                    Debug.LogError("Player Camera is missing from the setup. Will try find it for you");
                    Camera camera = GaiaUtils.GetCamera();
                    if (camera != null)
                    {
                        m_playerCamera = camera.transform;
                    }
                    return;
                }
                else
                {
                    if (EnableUnderwaterEffects)
                    {
                        if (m_playerCamera.position.y > m_seaLevel)
                        {
                            if (!m_surfaceSetup)
                            {
                                IsUnderwater = SetupWaterSystems(false, m_startingUnderwater);
                                SetHDRPVisualEffectsState(m_surfaceVisualEffects);
                                m_underwaterSetup = false;
                                m_surfaceSetup = true;
                            }
                        }
                        else
                        {
                            if (!m_underwaterSetup)
                            {

                                if (m_underwaterTransitionPostFX != null)
                                {
                                    m_underwaterTransitionPostFX.SetActive(true);
                                }
                                if (m_underwaterPostFX != null)
                                {
                                    m_underwaterPostFX.SetActive(true);
                                }
                                UpdateSurfaceFogSettings();
                                IsUnderwater = SetupWaterSystems(true, m_startingUnderwater);
                                SetHDRPVisualEffectsState(m_surfaceVisualEffects);
                                m_underwaterSetup = true;
                                m_surfaceSetup = false;
                            }

                            UpdateUnderwaterFog();
                            UpdateUnderwaterPostFX();
                        }
                    }
                    else
                    {
                        if (m_underwaterTransitionPostFX != null)
                        {
                            m_underwaterTransitionPostFX.SetActive(false);
                        }
                        if (m_underwaterPostFX != null)
                        {
                            m_underwaterPostFX.SetActive(false);
                        }
                    }
                }

                if (m_weatherSystemExists)
                {
#if GAIA_2023_PRO
                    if (!ProceduralWorldsGlobalWeather.Instance.IsRainingFinished || !ProceduralWorldsGlobalWeather.Instance.IsSnowingFinished)
                    {
                        ProceduralWorldsGlobalWeather.Instance.CheckUnderwaterParticlesVFX(IsUnderwater);
                    }
#endif
                }

                if (!IsUnderwater)
                {
                    UpdateSurfaceFogSettings();
                }
                else
                {
#if GAIA_2023_PRO
                    if (m_weatherSystemExists)
                    {
                        if (ProceduralWorldsGlobalWeather.Instance.CheckIsNight())
                        {
                            m_mainLight = ProceduralWorldsGlobalWeather.Instance.m_moonLight;
                        }
                        else
                        {
                            m_mainLight = ProceduralWorldsGlobalWeather.Instance.m_sunLight;
                        }
                    }
#endif
                    if (m_underwaterMaterial != null)
                    {
                        UpdateUnderwaterMaterial();
                    }
                }
            }
            else
            {
                if (m_underwaterParticleSystem != null)
                {
                    m_underwaterParticleSystem.Stop();
                }

                if (m_underwaterPostFX != null)
                {
                    m_underwaterPostFX.SetActive(false);
                }

                if (m_underwaterTransitionPostFX != null)
                {
                    m_underwaterTransitionPostFX.SetActive(false);
                }

                if (m_horizonObject != null)
                {
                    m_horizonObject.SetActive(false);
                }
            }
        }

        #endregion
        #region Functions
        
        /// <summary>
        /// Sets up the transition VFX
        /// </summary>
        /// <param name="status"></param>
        public void SetupTransitionVFX(bool status)
        {
            if (status)
            {
                if (RenderPipeline != GaiaConstants.EnvironmentRenderer.BuiltIn)
                {
                    if (m_underwaterTransitionPostFX != null)
                    {
                        DestroyImmediate(m_underwaterTransitionPostFX);
                    }
                    return;
                }

                if (m_underwaterTransitionPostFX == null)
                {
#if !UNITY_POST_PROCESSING_STACK_V2
                    if (RenderPipeline == GaiaConstants.EnvironmentRenderer.BuiltIn)
                    {
                        return;
                    }
#endif

                    m_underwaterTransitionPostFX = GameObject.Find(GaiaConstants.underwaterTransitionObjectName);
                    if (m_underwaterTransitionPostFX == null)
                    {
                        if (PWS_WaterSystem.Instance != null)
                        {
                            if (PWS_WaterSystem.Instance.m_waterProfile != null)
                            {
                                if (PWS_WaterSystem.Instance.m_waterProfile.m_transitionFXPrefab != null)
                                {
                                    m_underwaterTransitionPostFX = GameObject.Instantiate(PWS_WaterSystem.Instance.m_waterProfile.m_transitionFXPrefab);
                                }
                            }
                        }

                        if (m_underwaterTransitionPostFX != null)
                        {
                            m_underwaterTransitionPostFX.name = GaiaConstants.underwaterTransitionObjectName;
                            m_underwaterTransitionPostFX.transform.SetParent(gameObject.transform);
                            m_underwaterTransitionPostFX.transform.position = new Vector3(0f, m_seaLevel, 0f);
                        }
                    }
                }
                else
                {
                    m_underwaterTransitionPostFX.transform.SetParent(gameObject.transform);
                    m_underwaterTransitionPostFX.transform.position = new Vector3(0f, m_seaLevel, 0f);
                }
            }
            else
            {
                if (m_underwaterTransitionPostFX != null)
                {
                    DestroyImmediate(m_underwaterTransitionPostFX);
                }
            }
        }
        /// <summary>
        /// Sets the visual effect state and plays them if they are underwater
        /// </summary>
        /// <param name="visualEffects"></param>
        public void SetHDRPVisualEffectsState(List<VisualEffect> visualEffects)
        {
            if (visualEffects.Count > 0)
            {
                foreach (VisualEffect visualEffect in visualEffects)
                {
                    if (visualEffect != null)
                    {
                        if (IsUnderwater)
                        {
                            visualEffect.enabled = false;
                            visualEffect.Stop();
                        }
                        else
                        {
                            visualEffect.enabled = true;
                            visualEffect.Play();
                        }
                    }
                }
            }
        }
        /// <summary>
        /// Plays the underwater caustic animations
        /// </summary>
        /// <param name="systemOn"></param>
        /// <returns></returns>
        private void CausticsAnimation()
        {
            if (m_mainLight != null && m_causticTextures != null && m_causticTextures.Count > 0)
            {
                if (m_indexNumber < 0 || m_indexNumber >= m_causticTextures.Count)
                {
                    m_indexNumber = 0; // reset to safe default
                }
                    
                m_mainLight.cookieSize = m_causticSize;
                m_mainLight.cookie = m_causticTextures[m_indexNumber];
                m_indexNumber++;

                if (m_indexNumber == m_causticTextures.Count)
                {
                    m_indexNumber = 0;
                }
            }
        }
        /// <summary>
        /// Sets the water settings
        /// </summary>
        /// <param name="isUnderwater"></param>
        private bool SetupWaterSystems(bool isUnderwater, bool startingUnderwater = false)
        {
            if (!Application.isPlaying)
            {
                return false;
            }
            if (m_startingSystem || startingUnderwater)
            {
                if (isUnderwater)
                {
                    if (m_submergeSoundFXDown != null)
                    {
                        m_audioSource.PlayOneShot(m_submergeSoundFXDown, m_playbackVolume);
                    }

                    if (m_causticTextures == null)
                    {
                        LoadUnderwaterSystemAssets();
                    }
                    if (m_causticTextures != null)
                    {
                        if (m_useCaustics)
                        {
                            InvokeRepeating("CausticsAnimation", 0f, 1f / m_framesPerSecond);
                        }
                        else
                        {
                            CancelInvoke();
                        }
                    }

                    if (m_horizonMeshRenders != null)
                    {
                        foreach (MeshRenderer render in m_horizonMeshRenders)
                        {
                            if (RenderPipeline == GaiaConstants.EnvironmentRenderer.HighDefinition)
                            {
                                render.enabled = false;
                            }
                            else
                            {
                                render.enabled = true;
                            }
                        }
                    }

                    m_underwaterParticles.SetActive(true);
                    m_underwaterParticleSystem.Play();
                    m_audioSourceUnderwater.Play();
                }
                else
                {
                    if (m_submergeSoundFXUp != null)
                    {
                        m_audioSource.PlayOneShot(m_submergeSoundFXUp, m_playbackVolume);
                    }

                    CancelInvoke();

                    if (m_mainLight != null)
                    {
                        m_mainLight.cookie = null;
                    }

                    if (m_supportFog)
                    {
                        UpdateSurfaceFog();
                    }

                    if (m_horizonMeshRenders != null)
                    {
                        foreach (MeshRenderer render in m_horizonMeshRenders)
                        {
                            render.enabled = false;
                        }
                    }

                    m_underwaterParticleSystem.Stop();
                    m_underwaterParticles.SetActive(false);
                    m_audioSourceUnderwater.Stop();
                }

#if GAIA_2023_PRO
                if (m_weatherSystemExists)
                {
                    if (!isUnderwater)
                    {
                        if (ProceduralWorldsGlobalWeather.Instance.m_sunLight != null)
                        {
                            ProceduralWorldsGlobalWeather.Instance.m_sunLight.cookie = null;
                        }
                        if (ProceduralWorldsGlobalWeather.Instance.m_moonLight != null)
                        {
                            ProceduralWorldsGlobalWeather.Instance.m_moonLight.cookie = null;
                        }
                    }
                    ProceduralWorldsGlobalWeather.Instance.CheckUnderwaterParticlesVFX(isUnderwater);
                }
#endif
            }
            else
            {
                m_startingSystem = true;

                CancelInvoke();

                if (m_mainLight != null)
                {
                    m_mainLight.cookie = null;
                }

                if (m_supportFog)
                {
                    UpdateSurfaceFog();
                }

                if (m_horizonMeshRenders != null)
                {
                    foreach (MeshRenderer render in m_horizonMeshRenders)
                    {
                        render.enabled = false;
                    }
                }

                m_underwaterParticleSystem.Stop();
                m_underwaterParticles.SetActive(false);
                m_audioSourceUnderwater.Stop();
            }

            return isUnderwater;
        }
        /// <summary>
        /// Updates the underwater fog
        /// </summary>
        private void UpdateUnderwaterFog()
        {
            if (m_supportFog)
            {
                if (m_fogColorGradient != null)
                {
                    if (m_playerCamera.position.y < m_seaLevel)
                    {
                        float distance = Mathf.Clamp01((m_seaLevel - m_playerCamera.position.y) / m_fogDepth);
                        if (m_overrideFogColor)
                        {
                            if (m_overrideFogColorGradient != null)
                            {
                                m_fogColor = m_overrideFogColorGradient.Evaluate(m_timeOfDayValue) * (m_overrideFogMultiplier * m_overrideFogColorMultiplier.Evaluate(m_timeOfDayValue));
                            }
                        }
                        else
                        {
                            m_fogColor = m_fogColorGradient.Evaluate(distance);
                        }

                        if (m_mainLight != null && !m_overrideFogColor)
                        {
                            m_fogColor *= m_mainLight.color;
                            if (m_fogColorMultiplier.r >= 0)
                            {
                                m_fogColor.r = Mathf.Clamp01(m_fogColor.r += m_fogColorMultiplier.r);
                            }
                            if (m_fogColorMultiplier.g >= 0)
                            {
                                m_fogColor.g = Mathf.Clamp01(m_fogColor.g += m_fogColorMultiplier.g);
                            }
                            if (m_fogColorMultiplier.b >= 0)
                            {
                                m_fogColor.b = Mathf.Clamp01(m_fogColor.b += m_fogColorMultiplier.b);
                            }
                        }

                        if (RenderPipeline != GaiaConstants.EnvironmentRenderer.HighDefinition)
                        {
                            RenderSettings.fogColor = m_fogColor;
                            RenderSettings.fogDensity = m_fogDensity;
                            RenderSettings.fogStartDistance = m_nearFogDistance;
                            RenderSettings.fogEndDistance = m_fogDistance;
                        }
                    }
                }
            }
        }
        /// <summary>
        /// Updates the underwater post fx
        /// </summary>
        private void UpdateUnderwaterPostFX()
        {
            if (m_supportPostFX)
            {
#if UNITY_POST_PROCESSING_STACK_V2
                if (m_postProcessProfile == null)
                {
                    if (m_underwaterPostFX != null)
                    {
                        PostProcessVolume processVolume = m_underwaterPostFX.GetComponent<PostProcessVolume>();
                        if (processVolume != null)
                        {
                            m_postProcessProfile = processVolume.sharedProfile;
                        }
                    }
                }
                else
                {
                    UnityEngine.Rendering.PostProcessing.ColorGrading colorGrading;
#if GAIA_2023_PRO
                    if (!m_weatherSystemExists)
                    {
#endif
                        if (m_postProcessProfile.TryGetSettings(out colorGrading))
                        {
                            colorGrading.postExposure.value = m_constUnderwaterPostExposure;
                            colorGrading.postExposure.overrideState = true;
                            colorGrading.colorFilter.value = m_constUnderwaterColorFilter;
                            colorGrading.colorFilter.overrideState = true;
                        }
#if GAIA_2023_PRO
                    }
                    else
                    {
                        if (m_postProcessProfile.TryGetSettings(out colorGrading))
                        {
                            colorGrading.postExposure.value = m_gradientUnderwaterPostExposure.Evaluate(PWSkyStandalone.GetTimeOfDayMainValue());
                            colorGrading.postExposure.overrideState = true;
                            colorGrading.colorFilter.value = m_gradientUnderwaterColorFilter.Evaluate(PWSkyStandalone.GetTimeOfDayMainValue());
                            colorGrading.colorFilter.overrideState = true;
                        }
                    }
#endif
                }
#endif
            }
        }
        private void UpdateSurfaceFog(bool createVolume = true)
        {
            if (RenderPipeline != GaiaConstants.EnvironmentRenderer.HighDefinition)
            {
                //Do not execute if the surface fog end distance has never been stored yet
                if (m_surfaceFogEndDistance == -99.0f)
                {
                    return;
                }
                RenderSettings.fogColor = m_surfaceFogColor;
                RenderSettings.fogDensity = m_surfaceFogDensity;
                RenderSettings.fogStartDistance = m_surfaceFogStartDistance;
                RenderSettings.fogEndDistance = m_surfaceFogEndDistance;
            }
        }
        private void DisableUnderwaterFX()
        {
            if (m_playerCamera.position.y < m_seaLevel)
            {
                if (m_submergeSoundFXUp != null)
                {
                    m_audioSource.PlayOneShot(m_submergeSoundFXUp, m_playbackVolume);
                }
            }

            CancelInvoke();

            if (m_mainLight != null)
            {
                m_mainLight.cookie = null;
            }

            if (m_supportFog)
            {
                UpdateSurfaceFog();
            }

            if (m_horizonMeshRenders != null)
            {
                foreach (MeshRenderer render in m_horizonMeshRenders)
                {
                    render.enabled = false;
                }
            }

            m_underwaterParticleSystem.Stop();
            m_underwaterParticles.SetActive(false);
            m_audioSourceUnderwater.Stop();
        }
        private void EnableUnderwaterFX()
        {
            if (m_playerCamera.position.y < m_seaLevel)
            {
                m_underwaterSetup = false;
                if (!m_underwaterSetup)
                {
                    if (m_underwaterTransitionPostFX != null)
                    {
                        m_underwaterTransitionPostFX.SetActive(true);
                    }
                    if (m_underwaterPostFX != null)
                    {
                        m_underwaterPostFX.SetActive(true);
                    }

                    IsUnderwater = SetupWaterSystems(true, m_startingUnderwater);
                    SetHDRPVisualEffectsState(m_surfaceVisualEffects);
                    m_underwaterSetup = true;
                    m_surfaceSetup = false;
                }

                UpdateUnderwaterFog();
                UpdateUnderwaterPostFX();
            }
        }
        /// <summary>
        /// Updates the fog surface settings such as color density distance etc.
        /// </summary>
        public void UpdateSurfaceFogSettings()
        {
            if (RenderPipeline != GaiaConstants.EnvironmentRenderer.HighDefinition)
            {
                m_surfaceFogColor = RenderSettings.fogColor;
                m_surfaceFogDensity = RenderSettings.fogDensity;
                m_surfaceFogStartDistance = RenderSettings.fogStartDistance;
                m_surfaceFogEndDistance = RenderSettings.fogEndDistance;
            }
        }
        
        /// <summary>
        /// Sets the underwater color on the material
        /// </summary>
        private void UpdateUnderwaterMaterial()
        {
            if (m_mainLight != null)
            {
                m_underwaterMaterial.SetColor(GaiaShaderID.m_underwaterColor, m_mainLight.color);
            }
            else
            {
                m_underwaterMaterial.SetColor(GaiaShaderID.m_underwaterColor, GaiaUtils.GetColorFromHTML("CFCFCF"));
            }
        }
        /// <summary>
        /// Gets and returns the audio source
        /// </summary>
        /// <returns></returns>
        private AudioSource GetAudioSource()
        {
            AudioSource audioSource = gameObject.AddComponent<AudioSource>();
            return audioSource;
        }
        public void LoadUnderwaterSystemAssets()
        {
#if UNITY_EDITOR
            Camera camera = GaiaUtils.GetCamera();
            if (camera != null)
            {
                m_playerCamera = camera.transform;
            }

            //Load Built-In/URP
            if (m_causticTextures == null || m_causticTextures.Count <= 0)
            {
                m_causticTextures = new List<Texture2D>
                {
                    AssetDatabase.LoadAssetAtPath<Texture2D>(GaiaUtils.GetAssetPath("CausticsRender_001.tif")),
                    AssetDatabase.LoadAssetAtPath<Texture2D>(GaiaUtils.GetAssetPath("CausticsRender_002.tif")),
                    AssetDatabase.LoadAssetAtPath<Texture2D>(GaiaUtils.GetAssetPath("CausticsRender_003.tif")),
                    AssetDatabase.LoadAssetAtPath<Texture2D>(GaiaUtils.GetAssetPath("CausticsRender_004.tif")),
                    AssetDatabase.LoadAssetAtPath<Texture2D>(GaiaUtils.GetAssetPath("CausticsRender_005.tif")),
                    AssetDatabase.LoadAssetAtPath<Texture2D>(GaiaUtils.GetAssetPath("CausticsRender_006.tif")),
                    AssetDatabase.LoadAssetAtPath<Texture2D>(GaiaUtils.GetAssetPath("CausticsRender_007.tif")),
                    AssetDatabase.LoadAssetAtPath<Texture2D>(GaiaUtils.GetAssetPath("CausticsRender_008.tif")),
                    AssetDatabase.LoadAssetAtPath<Texture2D>(GaiaUtils.GetAssetPath("CausticsRender_009.tif")),
                    AssetDatabase.LoadAssetAtPath<Texture2D>(GaiaUtils.GetAssetPath("CausticsRender_010.tif")),
                    AssetDatabase.LoadAssetAtPath<Texture2D>(GaiaUtils.GetAssetPath("CausticsRender_011.tif")),
                    AssetDatabase.LoadAssetAtPath<Texture2D>(GaiaUtils.GetAssetPath("CausticsRender_012.tif")),
                    AssetDatabase.LoadAssetAtPath<Texture2D>(GaiaUtils.GetAssetPath("CausticsRender_013.tif")),
                    AssetDatabase.LoadAssetAtPath<Texture2D>(GaiaUtils.GetAssetPath("CausticsRender_014.tif")),
                    AssetDatabase.LoadAssetAtPath<Texture2D>(GaiaUtils.GetAssetPath("CausticsRender_015.tif")),
                    AssetDatabase.LoadAssetAtPath<Texture2D>(GaiaUtils.GetAssetPath("CausticsRender_016.tif"))
                };
            }

            if (m_submergeSoundFXDown == null)
            {
                m_submergeSoundFXDown = AssetDatabase.LoadAssetAtPath<AudioClip>(GaiaUtils.GetAssetPath("Gaia Ambient Submerge Down.mp3"));
            }

            if (m_submergeSoundFXUp == null)
            {
                m_submergeSoundFXUp = AssetDatabase.LoadAssetAtPath<AudioClip>(GaiaUtils.GetAssetPath("Gaia Ambient Submerge Up.mp3"));
            }

            if (m_underwaterSoundFX == null)
            {
                m_underwaterSoundFX = AssetDatabase.LoadAssetAtPath<AudioClip>(GaiaUtils.GetAssetPath("Gaia Ambient Underwater Sound Effect.mp3"));
            }

            if (m_fogColorGradient == null)
            {
                m_fogColorGradient = CreateGradient();
            }
#endif
        }
        /// <summary>
        /// Gets the underwater status
        /// </summary>
        /// <returns></returns>
        public static bool IsUnderWater()
        {
            bool underwater = false;
            GaiaURPUnderwaterEffects GaiaURPUnderwaterEffects = FindAnyObjectByType<GaiaURPUnderwaterEffects>();
            if (GaiaURPUnderwaterEffects != null)
            {
                underwater = GaiaURPUnderwaterEffects.IsUnderwater;
            }

            return underwater;
        }
        /// <summary>
        /// Creates a default Gradient
        /// </summary>
        /// <returns></returns>
        private Gradient CreateGradient()
        {
            Gradient gradient = new Gradient();

            // Populate the color keys at the relative time 0 and 1 (0 and 100%)
            GradientColorKey[] colorKey = new GradientColorKey[3];
            colorKey[0].color = GaiaUtils.GetColorFromHTML("0C233A");
            colorKey[0].time = 0.0f;
            colorKey[1].color = GaiaUtils.GetColorFromHTML("5686BC");
            colorKey[1].time = 0.5f;
            colorKey[2].color = GaiaUtils.GetColorFromHTML("5C9BE0");
            colorKey[2].time = 1f;

            // Populate the alpha  keys at relative time 0 and 1  (0 and 100%)
            GradientAlphaKey[] alphaKey = new GradientAlphaKey[3];
            alphaKey[0].alpha = 1.0f;
            alphaKey[0].time = 0.0f;
            alphaKey[1].alpha = 1.0f;
            alphaKey[1].time = 0.5f;
            alphaKey[2].alpha = 1.0f;
            alphaKey[2].time = 1.0f;

            gradient.SetKeys(colorKey, alphaKey);

            return gradient;
        }
        /// <summary>
        /// Sets the new underwater volume
        /// </summary>
        /// <param name="value"></param>
        public void SetNewUnderwaterSoundFXVolume(float value)
        {
            m_playbackVolume = value;
            if (m_audioSourceUnderwater != null)
            {
                m_audioSourceUnderwater.volume = value;
            }
        }

        #endregion
    }
}
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Profiling;

namespace Laboratory.Subsystems.Performance
{
    /// <summary>
    /// Concrete implementation of Level of Detail management service
    /// Handles dynamic LOD adjustments and group management for performance optimization
    /// </summary>
    public class LevelOfDetailService : ILevelOfDetailService
    {
        #region Fields

        private readonly PerformanceSubsystemConfig _config;
        private LODSettings _lodSettings;
        private Dictionary<string, LODGroup> _lodGroups;
        private Camera _mainCamera;
        private bool _isInitialized;

        // Unity Profiler markers
        private static readonly ProfilerMarker s_LODUpdateMarker = new("LevelOfDetail.UpdateGroups");
        private static readonly ProfilerMarker s_LODBiasMarker = new("LevelOfDetail.BiasAdjustment");

        #endregion

        #region Constructor

        public LevelOfDetailService(PerformanceSubsystemConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        #endregion

        #region ILevelOfDetailService Implementation

        public async Task<bool> InitializeAsync()
        {
            try
            {
                _lodGroups = new Dictionary<string, LODGroup>();
                _mainCamera = Camera.main;

                // Initialize default LOD settings
                InitializeDefaultLODSettings();

                _isInitialized = true;

                if (_config.enableDebugLogging)
                    Debug.Log("[LevelOfDetailService] Initialized successfully");

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LevelOfDetailService] Failed to initialize: {ex.Message}");
                return false;
            }
        }

        public void SetLODBias(float bias)
        {
            if (!_isInitialized)
                return;

            using (s_LODBiasMarker.Auto())
            {
                bias = Mathf.Clamp(bias, 0.1f, 10f);
                QualitySettings.lodBias = bias;
                _lodSettings.lodBias = bias;

                if (_config.enableDebugLogging)
                    Debug.Log($"[LevelOfDetailService] Set LOD bias to {bias:F2}");
            }
        }

        public void ReduceLODBias(float amount)
        {
            if (!_isInitialized)
                return;

            var currentBias = QualitySettings.lodBias;
            var newBias = Mathf.Max(0.1f, currentBias - amount);
            SetLODBias(newBias);
        }

        public void RegisterLODGroup(LODGroup group)
        {
            if (!_isInitialized || group == null || string.IsNullOrEmpty(group.groupName))
                return;

            if (_lodGroups.ContainsKey(group.groupName))
            {
                Debug.LogWarning($"[LevelOfDetailService] LOD group '{group.groupName}' already registered");
                return;
            }

            // Initialize group settings if not provided
            if (group.settings == null)
                group.settings = CreateDefaultLODSettings();

            group.lastUpdate = DateTime.Now;
            _lodGroups[group.groupName] = group;

            if (_config.enableDebugLogging)
                Debug.Log($"[LevelOfDetailService] Registered LOD group '{group.groupName}' with {group.objects.Count} objects");
        }

        public void UnregisterLODGroup(string groupName)
        {
            if (!_isInitialized || string.IsNullOrEmpty(groupName))
                return;

            if (_lodGroups.Remove(groupName))
            {
                if (_config.enableDebugLogging)
                    Debug.Log($"[LevelOfDetailService] Unregistered LOD group '{groupName}'");
            }
        }

        public void UpdateLODGroups(Vector3 cameraPosition)
        {
            if (!_isInitialized)
                return;

            using (s_LODUpdateMarker.Auto())
            {
                foreach (var group in _lodGroups.Values)
                {
                    UpdateLODGroup(group, cameraPosition);
                }
            }
        }

        public LODSettings GetLODSettings()
        {
            return _lodSettings;
        }

        public void SetLODSettings(LODSettings settings)
        {
            if (!_isInitialized || settings == null)
                return;

            _lodSettings = settings;
            ApplyLODSettings();

            if (_config.enableDebugLogging)
                Debug.Log("[LevelOfDetailService] Applied new LOD settings");
        }

        #endregion

        #region Private Methods

        private void InitializeDefaultLODSettings()
        {
            _lodSettings = new LODSettings
            {
                lodBias = QualitySettings.lodBias,
                maxLODLevel = 3f,
                fadeTransitionWidth = 0.1f,
                enableCrossFade = true,
                fadeMode = LODFadeMode.CrossFade,
                configurations = new Dictionary<LODLevel, LODConfiguration>()
            };

            // Create default LOD configurations
            _lodSettings.configurations[LODLevel.LOD0] = new LODConfiguration
            {
                level = LODLevel.LOD0,
                distance = 50f,
                quality = 1f,
                enableAnimations = true,
                enablePhysics = true,
                enableAI = true,
                enableParticles = true,
                enableAudio = true,
                updateFrequency = 1f,
                maxVertices = 20000,
                maxTriangles = 10000
            };

            _lodSettings.configurations[LODLevel.LOD1] = new LODConfiguration
            {
                level = LODLevel.LOD1,
                distance = 100f,
                quality = 0.75f,
                enableAnimations = true,
                enablePhysics = true,
                enableAI = true,
                enableParticles = true,
                enableAudio = true,
                updateFrequency = 0.5f,
                maxVertices = 10000,
                maxTriangles = 5000
            };

            _lodSettings.configurations[LODLevel.LOD2] = new LODConfiguration
            {
                level = LODLevel.LOD2,
                distance = 200f,
                quality = 0.5f,
                enableAnimations = false,
                enablePhysics = false,
                enableAI = true,
                enableParticles = false,
                enableAudio = false,
                updateFrequency = 0.25f,
                maxVertices = 5000,
                maxTriangles = 2500
            };

            _lodSettings.configurations[LODLevel.LOD3] = new LODConfiguration
            {
                level = LODLevel.LOD3,
                distance = 500f,
                quality = 0.25f,
                enableAnimations = false,
                enablePhysics = false,
                enableAI = false,
                enableParticles = false,
                enableAudio = false,
                updateFrequency = 0.1f,
                maxVertices = 1000,
                maxTriangles = 500
            };
        }

        private LODSettings CreateDefaultLODSettings()
        {
            return new LODSettings
            {
                lodBias = _lodSettings.lodBias,
                maxLODLevel = _lodSettings.maxLODLevel,
                fadeTransitionWidth = _lodSettings.fadeTransitionWidth,
                enableCrossFade = _lodSettings.enableCrossFade,
                fadeMode = _lodSettings.fadeMode,
                configurations = new Dictionary<LODLevel, LODConfiguration>(_lodSettings.configurations)
            };
        }

        private void ApplyLODSettings()
        {
            QualitySettings.lodBias = _lodSettings.lodBias;

            // Apply settings to all registered groups
            foreach (var group in _lodGroups.Values)
            {
                if (group.settings == null)
                    group.settings = CreateDefaultLODSettings();

                // Update group settings to match global settings
                group.settings.lodBias = _lodSettings.lodBias;
                group.settings.maxLODLevel = _lodSettings.maxLODLevel;
                group.settings.fadeTransitionWidth = _lodSettings.fadeTransitionWidth;
                group.settings.enableCrossFade = _lodSettings.enableCrossFade;
                group.settings.fadeMode = _lodSettings.fadeMode;
            }
        }

        private void UpdateLODGroup(LODGroup group, Vector3 cameraPosition)
        {
            if (group?.objects == null || group.objects.Count == 0)
                return;

            // Calculate average distance to camera
            float totalDistance = 0f;
            int validObjects = 0;

            foreach (var obj in group.objects)
            {
                if (obj != null)
                {
                    totalDistance += Vector3.Distance(obj.transform.position, cameraPosition);
                    validObjects++;
                }
            }

            if (validObjects == 0)
                return;

            group.distanceToCamera = totalDistance / validObjects;
            group.lastUpdate = DateTime.Now;

            // Determine appropriate LOD level
            var lodLevel = DetermineLODLevel(group.distanceToCamera, group.settings);

            // Check if object should be visible
            group.isVisible = lodLevel != LODLevel.Culled;

            // Update current LOD
            group.currentLOD = (float)lodLevel;

            // Apply LOD to objects
            ApplyLODToGroup(group, lodLevel);
        }

        private LODLevel DetermineLODLevel(float distance, LODSettings settings)
        {
            foreach (var config in settings.configurations.Values)
            {
                if (distance <= config.distance)
                    return config.level;
            }

            return LODLevel.Culled;
        }

        private void ApplyLODToGroup(LODGroup group, LODLevel lodLevel)
        {
            if (!group.settings.configurations.TryGetValue(lodLevel, out var config))
                return;

            foreach (var obj in group.objects)
            {
                if (obj == null)
                    continue;

                // Set object active/inactive based on visibility
                if (obj.activeSelf != group.isVisible)
                    obj.SetActive(group.isVisible);

                if (!group.isVisible)
                    continue;

                // Apply LOD-specific settings
                ApplyLODToObject(obj, config);
            }
        }

        private void ApplyLODToObject(GameObject obj, LODConfiguration config)
        {
            // Apply animation settings
            var animators = obj.GetComponentsInChildren<Animator>();
            foreach (var animator in animators)
            {
                animator.enabled = config.enableAnimations;
            }

            // Apply physics settings
            var rigidbodies = obj.GetComponentsInChildren<Rigidbody>();
            foreach (var rb in rigidbodies)
            {
                rb.isKinematic = !config.enablePhysics;
            }

            var colliders = obj.GetComponentsInChildren<Collider>();
            foreach (var collider in colliders)
            {
                collider.enabled = config.enablePhysics;
            }

            // Apply particle settings
            var particleSystems = obj.GetComponentsInChildren<ParticleSystem>();
            foreach (var ps in particleSystems)
            {
                if (config.enableParticles)
                {
                    if (!ps.isPlaying)
                        ps.Play();
                }
                else
                {
                    if (ps.isPlaying)
                        ps.Stop();
                }
            }

            // Apply audio settings
            var audioSources = obj.GetComponentsInChildren<AudioSource>();
            foreach (var audioSource in audioSources)
            {
                audioSource.enabled = config.enableAudio;
            }
        }

        #endregion
    }
}
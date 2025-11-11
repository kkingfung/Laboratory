using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Profiling;

namespace Laboratory.Subsystems.Performance
{
    /// <summary>
    /// Concrete implementation of quality scaling service
    /// Handles dynamic quality adjustments based on performance metrics
    /// </summary>
    public class QualityScalingService : IQualityScalingService
    {
        #region Events

        public event Action<QualityLevelChange> OnQualityLevelChanged;

        #endregion

        #region Fields

        private readonly PerformanceSubsystemConfig _config;
        private QualityScalingSettings _qualitySettings;
        private Dictionary<QualityLevel, QualityConfiguration> _qualityConfigurations;
        private bool _isInitialized;

        // Unity Profiler markers
        private static readonly ProfilerMarker s_QualityScalingMarker = new("QualityScaling.Update");
        private static readonly ProfilerMarker s_QualityApplicationMarker = new("QualityScaling.Apply");

        #endregion

        #region Constructor

        public QualityScalingService(PerformanceSubsystemConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        #endregion

        #region IQualityScalingService Implementation

        public async Task<bool> InitializeAsync()
        {
            try
            {
                _qualitySettings = new QualityScalingSettings
                {
                    enableDynamicScaling = _config.enableDynamicQualityScaling,
                    currentLevel = QualityLevel.High,
                    targetLevel = QualityLevel.High,
                    minLevel = QualityLevel.Low,
                    maxLevel = QualityLevel.Ultra,
                    scalingAggressiveness = 0.5f,
                    scalingThreshold = 0.8f
                };

                InitializeQualityConfigurations();

                // Apply initial quality settings
                ApplyQualityConfiguration(_qualitySettings.currentLevel);

                _isInitialized = true;

                if (_config.enableDebugLogging)
                    UnityEngine.Debug.Log("[QualityScalingService] Initialized successfully");

                return true;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[QualityScalingService] Failed to initialize: {ex.Message}");
                return false;
            }
        }

        public void SetQualityLevel(QualityLevel level)
        {
            if (!_isInitialized)
                return;

            using (s_QualityScalingMarker.Auto())
            {
                var previousLevel = _qualitySettings.currentLevel;
                _qualitySettings.currentLevel = level;
                _qualitySettings.targetLevel = level;

                ApplyQualityConfiguration(level);

                // Broadcast quality level change
                var qualityChange = new QualityLevelChange
                {
                    previousLevel = previousLevel,
                    newLevel = level,
                    timestamp = DateTime.Now,
                    reason = "Manual quality change",
                    wasAutomatic = false,
                    performanceGain = EstimatePerformanceGain(previousLevel, level)
                };

                OnQualityLevelChanged?.Invoke(qualityChange);

                if (_config.enableDebugLogging)
                    UnityEngine.Debug.Log($"[QualityScalingService] Quality level changed from {previousLevel} to {level}");
            }
        }

        public void ReduceQualityLevel()
        {
            if (!_isInitialized)
                return;

            var currentLevel = _qualitySettings.currentLevel;
            var newLevel = GetLowerQualityLevel(currentLevel);

            if (newLevel != currentLevel && newLevel >= _qualitySettings.minLevel)
            {
                var previousLevel = currentLevel;
                _qualitySettings.currentLevel = newLevel;

                ApplyQualityConfiguration(newLevel);

                // Broadcast quality level change
                var qualityChange = new QualityLevelChange
                {
                    previousLevel = previousLevel,
                    newLevel = newLevel,
                    timestamp = DateTime.Now,
                    reason = "Automatic quality reduction for performance",
                    wasAutomatic = true,
                    performanceGain = EstimatePerformanceGain(previousLevel, newLevel)
                };

                OnQualityLevelChanged?.Invoke(qualityChange);

                if (_config.enableDebugLogging)
                    UnityEngine.Debug.Log($"[QualityScalingService] Reduced quality level from {previousLevel} to {newLevel}");
            }
        }

        public void IncreaseQualityLevel()
        {
            if (!_isInitialized)
                return;

            var currentLevel = _qualitySettings.currentLevel;
            var newLevel = GetHigherQualityLevel(currentLevel);

            if (newLevel != currentLevel && newLevel <= _qualitySettings.maxLevel)
            {
                var previousLevel = currentLevel;
                _qualitySettings.currentLevel = newLevel;

                ApplyQualityConfiguration(newLevel);

                // Broadcast quality level change
                var qualityChange = new QualityLevelChange
                {
                    previousLevel = previousLevel,
                    newLevel = newLevel,
                    timestamp = DateTime.Now,
                    reason = "Automatic quality increase due to good performance",
                    wasAutomatic = true,
                    performanceGain = EstimatePerformanceGain(previousLevel, newLevel)
                };

                OnQualityLevelChanged?.Invoke(qualityChange);

                if (_config.enableDebugLogging)
                    UnityEngine.Debug.Log($"[QualityScalingService] Increased quality level from {previousLevel} to {newLevel}");
            }
        }

        public QualityLevel GetCurrentQualityLevel()
        {
            return _isInitialized ? _qualitySettings.currentLevel : QualityLevel.Medium;
        }

        public QualityConfiguration GetQualityConfiguration(QualityLevel level)
        {
            if (!_isInitialized || !_qualityConfigurations.TryGetValue(level, out var config))
                return null;

            return config;
        }

        public void SetQualityConfiguration(QualityLevel level, QualityConfiguration config)
        {
            if (!_isInitialized || config == null)
                return;

            _qualityConfigurations[level] = config;

            // If this is the current quality level, apply it immediately
            if (level == _qualitySettings.currentLevel)
            {
                ApplyQualityConfiguration(level);
            }

            if (_config.enableDebugLogging)
                UnityEngine.Debug.Log($"[QualityScalingService] Updated quality configuration for {level}");
        }

        public void EnableDynamicScaling(bool enable)
        {
            if (!_isInitialized)
                return;

            _qualitySettings.enableDynamicScaling = enable;

            if (_config.enableDebugLogging)
                UnityEngine.Debug.Log($"[QualityScalingService] Dynamic scaling {(enable ? "enabled" : "disabled")}");
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Evaluates performance and adjusts quality if dynamic scaling is enabled
        /// </summary>
        public void EvaluateAndAdjustQuality(PerformanceMetrics metrics)
        {
            if (!_isInitialized || !_qualitySettings.enableDynamicScaling || metrics == null)
                return;

            using (s_QualityScalingMarker.Auto())
            {
                var performanceRatio = metrics.frameRate / _config.targetFrameRate;
                var memoryRatio = metrics.memoryUsedMB / _config.memoryBudgetMB;
                var drawCallRatio = metrics.drawCalls / (float)_config.drawCallBudget;

                // Calculate overall performance score (0-1, where 1 is perfect)
                var performanceScore = CalculatePerformanceScore(performanceRatio, memoryRatio, drawCallRatio);

                // Determine if quality adjustment is needed
                if (performanceScore < _qualitySettings.scalingThreshold)
                {
                    // Performance is below threshold, consider reducing quality
                    var aggressiveness = _qualitySettings.scalingAggressiveness;
                    var reductionProbability = (1f - performanceScore) * aggressiveness;

                    if (UnityEngine.Random.value < reductionProbability)
                    {
                        ReduceQualityLevel();
                    }
                }
                else if (performanceScore > 0.95f) // Performance is excellent
                {
                    // Consider increasing quality if we have headroom
                    var increaseProbability = (performanceScore - 0.95f) * 20f * _qualitySettings.scalingAggressiveness;

                    if (UnityEngine.Random.value < increaseProbability)
                    {
                        IncreaseQualityLevel();
                    }
                }
            }
        }

        #endregion

        #region Private Methods

        private void InitializeQualityConfigurations()
        {
            _qualityConfigurations = new Dictionary<QualityLevel, QualityConfiguration>();

            // Potato quality - absolute minimum
            _qualityConfigurations[QualityLevel.Potato] = new QualityConfiguration
            {
                level = QualityLevel.Potato,
                configurationName = "Potato",
                lodBias = 0.3f,
                shadowResolution = 256,
                shadowDistance = 25f,
                shadowQuality = ShadowQuality.Disable,
                textureQuality = 3,
                anisotropicTextures = 0,
                enableAntiAliasing = false,
                antiAliasingLevel = 0,
                enableRealTimeReflections = false,
                particleRaycastBudget = 256f,
                maxParticleSystemCount = 5,
                renderScale = 0.5f,
                enablePostProcessing = false,
                enableVolumetricLighting = false,
                enableScreenSpaceReflections = false
            };

            // Low quality
            _qualityConfigurations[QualityLevel.Low] = new QualityConfiguration
            {
                level = QualityLevel.Low,
                configurationName = "Low",
                lodBias = 0.5f,
                shadowResolution = 512,
                shadowDistance = 50f,
                shadowQuality = ShadowQuality.HardOnly,
                textureQuality = 2,
                anisotropicTextures = 0,
                enableAntiAliasing = false,
                antiAliasingLevel = 0,
                enableRealTimeReflections = false,
                particleRaycastBudget = 512f,
                maxParticleSystemCount = 10,
                renderScale = 0.75f,
                enablePostProcessing = false,
                enableVolumetricLighting = false,
                enableScreenSpaceReflections = false
            };

            // Medium quality
            _qualityConfigurations[QualityLevel.Medium] = new QualityConfiguration
            {
                level = QualityLevel.Medium,
                configurationName = "Medium",
                lodBias = 0.7f,
                shadowResolution = 1024,
                shadowDistance = 100f,
                shadowQuality = ShadowQuality.All,
                textureQuality = 1,
                anisotropicTextures = 2,
                enableAntiAliasing = true,
                antiAliasingLevel = 2,
                enableRealTimeReflections = false,
                particleRaycastBudget = 1024f,
                maxParticleSystemCount = 25,
                renderScale = 0.9f,
                enablePostProcessing = true,
                enableVolumetricLighting = false,
                enableScreenSpaceReflections = false
            };

            // High quality
            _qualityConfigurations[QualityLevel.High] = new QualityConfiguration
            {
                level = QualityLevel.High,
                configurationName = "High",
                lodBias = 1f,
                shadowResolution = 2048,
                shadowDistance = 150f,
                shadowQuality = ShadowQuality.All,
                textureQuality = 0,
                anisotropicTextures = 4,
                enableAntiAliasing = true,
                antiAliasingLevel = 4,
                enableRealTimeReflections = true,
                particleRaycastBudget = 2048f,
                maxParticleSystemCount = 40,
                renderScale = 1f,
                enablePostProcessing = true,
                enableVolumetricLighting = true,
                enableScreenSpaceReflections = true
            };

            // Ultra quality
            _qualityConfigurations[QualityLevel.Ultra] = new QualityConfiguration
            {
                level = QualityLevel.Ultra,
                configurationName = "Ultra",
                lodBias = 1.5f,
                shadowResolution = 4096,
                shadowDistance = 200f,
                shadowQuality = ShadowQuality.All,
                textureQuality = 0,
                anisotropicTextures = 16,
                enableAntiAliasing = true,
                antiAliasingLevel = 8,
                enableRealTimeReflections = true,
                particleRaycastBudget = 4096f,
                maxParticleSystemCount = 50,
                renderScale = 1f,
                enablePostProcessing = true,
                enableVolumetricLighting = true,
                enableScreenSpaceReflections = true
            };

            _qualitySettings.configurations = _qualityConfigurations;
        }

        private void ApplyQualityConfiguration(QualityLevel level)
        {
            if (!_qualityConfigurations.TryGetValue(level, out var config))
                return;

            using (s_QualityApplicationMarker.Auto())
            {
                // Apply Unity quality settings
                QualitySettings.lodBias = config.lodBias;
                QualitySettings.shadowResolution = (ShadowResolution)Enum.Parse(typeof(ShadowResolution), config.shadowResolution.ToString());
                QualitySettings.shadowDistance = config.shadowDistance;
                QualitySettings.shadows = config.shadowQuality;
                QualitySettings.globalTextureMipmapLimit = config.textureQuality;
                QualitySettings.anisotropicFiltering = config.anisotropicTextures > 0 ? AnisotropicFiltering.Enable : AnisotropicFiltering.Disable;

                // Apply anti-aliasing
                if (config.enableAntiAliasing)
                {
                    QualitySettings.antiAliasing = config.antiAliasingLevel;
                }
                else
                {
                    QualitySettings.antiAliasing = 0;
                }

                // Apply render pipeline specific settings
                ApplyRenderPipelineSettings(config);

                if (_config.enableDebugLogging)
                    UnityEngine.Debug.Log($"[QualityScalingService] Applied {config.configurationName} quality configuration");
            }
        }

        private void ApplyRenderPipelineSettings(QualityConfiguration config)
        {
            // Apply settings specific to the current render pipeline
            // This would need to be adapted based on whether you're using URP, HDRP, or Built-in RP

            // For Built-in Render Pipeline:
            QualitySettings.pixelLightCount = config.enableRealTimeReflections ? 4 : 2;
            QualitySettings.particleRaycastBudget = (int)config.particleRaycastBudget;

            // For URP/HDRP, you would access the render pipeline asset and modify settings
            // This is a simplified example - actual implementation would depend on your render pipeline
        }

        private QualityLevel GetLowerQualityLevel(QualityLevel currentLevel)
        {
            switch (currentLevel)
            {
                case QualityLevel.Ultra: return QualityLevel.High;
                case QualityLevel.High: return QualityLevel.Medium;
                case QualityLevel.Medium: return QualityLevel.Low;
                case QualityLevel.Low: return QualityLevel.Potato;
                case QualityLevel.Potato: return QualityLevel.Potato;
                default: return QualityLevel.Medium;
            }
        }

        private QualityLevel GetHigherQualityLevel(QualityLevel currentLevel)
        {
            switch (currentLevel)
            {
                case QualityLevel.Potato: return QualityLevel.Low;
                case QualityLevel.Low: return QualityLevel.Medium;
                case QualityLevel.Medium: return QualityLevel.High;
                case QualityLevel.High: return QualityLevel.Ultra;
                case QualityLevel.Ultra: return QualityLevel.Ultra;
                default: return QualityLevel.Medium;
            }
        }

        private float EstimatePerformanceGain(QualityLevel fromLevel, QualityLevel toLevel)
        {
            var fromIndex = (int)fromLevel;
            var toIndex = (int)toLevel;
            var levelDifference = fromIndex - toIndex; // Positive means quality reduction

            // Estimate 15% performance gain per quality level reduction
            return levelDifference * 15f;
        }

        private float CalculatePerformanceScore(float performanceRatio, float memoryRatio, float drawCallRatio)
        {
            // Weight the different metrics
            var frameRateWeight = 0.5f;
            var memoryWeight = 0.3f;
            var drawCallWeight = 0.2f;

            // Normalize ratios (clamp to prevent negative scores)
            var frameRateScore = Mathf.Clamp01(performanceRatio);
            var memoryScore = Mathf.Clamp01(1f - memoryRatio);
            var drawCallScore = Mathf.Clamp01(1f - drawCallRatio);

            // Calculate weighted average
            var overallScore = (frameRateScore * frameRateWeight) +
                              (memoryScore * memoryWeight) +
                              (drawCallScore * drawCallWeight);

            return Mathf.Clamp01(overallScore);
        }

        #endregion
    }
}
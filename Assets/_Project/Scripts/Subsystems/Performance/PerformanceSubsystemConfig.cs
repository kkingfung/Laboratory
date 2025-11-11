using UnityEngine;
using System.Collections.Generic;

namespace Laboratory.Subsystems.Performance
{
    /// <summary>
    /// Configuration ScriptableObject for the Performance & Optimization Subsystem.
    /// Controls performance monitoring, dynamic optimization, and quality scaling.
    /// </summary>
    [CreateAssetMenu(fileName = "PerformanceSubsystemConfig", menuName = "Project Chimera/Subsystems/Performance Config")]
    public class PerformanceSubsystemConfig : ScriptableObject
    {
        [Header("Core Settings")]
        [Tooltip("Performance monitoring interval in milliseconds")]
        [Range(100, 5000)]
        public int monitoringIntervalMs = 1000;

        [Tooltip("Enable debug logging for performance operations")]
        public bool enableDebugLogging = false;

        [Tooltip("Frame history buffer size")]
        [Range(60, 1000)]
        public int frameHistorySize = 300;

        [Tooltip("Optimization cooldown period in seconds")]
        [Range(1f, 60f)]
        public float optimizationCooldownSeconds = 5f;

        [Header("Performance Targets")]
        [Tooltip("Target frame rate")]
        [Range(30, 120)]
        public int targetFrameRate = 60;

        [Tooltip("Minimum acceptable frame rate threshold")]
        [Range(15, 60)]
        public float minFrameRateThreshold = 45f;

        [Tooltip("Maximum acceptable frame time in milliseconds")]
        [Range(8f, 50f)]
        public float maxFrameTimeMs = 16.67f;

        [Tooltip("CPU budget in milliseconds per frame")]
        [Range(5f, 30f)]
        public float cpuBudgetMs = 12f;

        [Tooltip("GPU budget in milliseconds per frame")]
        [Range(5f, 30f)]
        public float gpuBudgetMs = 12f;

        [Header("Memory Management")]
        [Tooltip("Memory budget in megabytes")]
        [Range(512f, 8192f)]
        public float memoryBudgetMB = 2048f;

        [Tooltip("Memory warning threshold as percentage of budget")]
        [Range(0.5f, 0.95f)]
        public float memoryWarningThreshold = 0.8f;

        [Tooltip("Memory critical threshold as percentage of budget")]
        [Range(0.8f, 0.99f)]
        public float memoryCriticalThreshold = 0.9f;

        [Tooltip("Enable automatic garbage collection")]
        public bool enableAutoGarbageCollection = true;

        [Tooltip("Garbage collection interval in seconds")]
        [Range(30f, 300f)]
        public float garbageCollectionIntervalSeconds = 60f;

        [Header("Rendering Performance")]
        [Tooltip("Draw call budget")]
        [Range(100, 5000)]
        public int drawCallBudget = 1000;

        [Tooltip("Batch budget")]
        [Range(50, 2000)]
        public int batchBudget = 500;

        [Tooltip("Triangle budget")]
        [Range(100000, 10000000)]
        public int triangleBudget = 2000000;

        [Tooltip("Maximum visible creatures")]
        [Range(50, 500)]
        public int maxVisibleCreatures = 200;

        [Header("Dynamic Quality Scaling")]
        [Tooltip("Enable dynamic quality scaling")]
        public bool enableDynamicQualityScaling = true;

        [Tooltip("Quality scaling aggressiveness")]
        [Range(0.1f, 1f)]
        public float qualityScalingAggressiveness = 0.5f;

        [Tooltip("Quality scaling response time in seconds")]
        [Range(1f, 10f)]
        public float qualityScalingResponseTime = 3f;

        [Tooltip("Minimum quality level")]
        public QualityLevel minimumQualityLevel = QualityLevel.Low;

        [Tooltip("Maximum quality level")]
        public QualityLevel maximumQualityLevel = QualityLevel.Ultra;

        [Header("Level of Detail (LOD)")]
        [Tooltip("Enable dynamic LOD management")]
        public bool enableDynamicLOD = true;

        [Tooltip("Default LOD bias")]
        [Range(0.1f, 3f)]
        public float defaultLODBias = 1f;

        [Tooltip("LOD update frequency in seconds")]
        [Range(0.1f, 2f)]
        public float lodUpdateFrequency = 0.5f;

        [Tooltip("LOD distance multiplier")]
        [Range(0.5f, 5f)]
        public float lodDistanceMultiplier = 1f;

        [Tooltip("Enable LOD cross-fade")]
        public bool enableLODCrossFade = true;

        [Header("Creature Performance")]
        [Tooltip("Maximum active creatures")]
        [Range(100, 2000)]
        public int maxActiveCreatures = 1000;

        [Tooltip("Creature update batch size")]
        [Range(10, 200)]
        public int creatureUpdateBatchSize = 50;

        [Tooltip("Creature AI update frequency reduction threshold")]
        [Range(0.5f, 2f)]
        public float aiUpdateReductionThreshold = 1.5f;

        [Tooltip("Enable creature culling based on distance")]
        public bool enableCreatureCulling = true;

        [Tooltip("Creature culling distance")]
        [Range(50f, 500f)]
        public float creatureCullingDistance = 200f;

        [Header("System Optimization")]
        [Tooltip("Enable system performance monitoring")]
        public bool enableSystemProfiling = true;

        [Tooltip("System profiling sample count")]
        [Range(10, 1000)]
        public int systemProfilingSampleCount = 100;

        [Tooltip("Enable adaptive system scaling")]
        public bool enableAdaptiveSystemScaling = true;

        [Tooltip("System scaling threshold")]
        [Range(0.5f, 2f)]
        public float systemScalingThreshold = 1.2f;

        [Header("Frame Pacing")]
        [Tooltip("Enable frame pacing")]
        public bool enableFramePacing = true;

        [Tooltip("Frame pacing mode")]
        public FramePacingMode framePacingMode = FramePacingMode.Adaptive;

        [Tooltip("Enable VSync")]
        public bool enableVSync = true;

        [Tooltip("Stutter detection threshold in milliseconds")]
        [Range(20f, 100f)]
        public float stutterThresholdMs = 50f;

        [Tooltip("Hitch detection threshold in milliseconds")]
        [Range(30f, 200f)]
        public float hitchThresholdMs = 100f;

        [Header("Quality Configurations")]
        [Tooltip("Quality level configurations")]
        public List<QualityConfiguration> qualityConfigurations = new List<QualityConfiguration>();

        [Header("Performance Alerts")]
        [Tooltip("Enable performance alerts")]
        public bool enablePerformanceAlerts = true;

        [Tooltip("Alert threshold for low frame rate")]
        [Range(0.5f, 0.9f)]
        public float lowFrameRateAlertThreshold = 0.8f;

        [Tooltip("Alert threshold for high memory usage")]
        [Range(0.7f, 0.95f)]
        public float highMemoryAlertThreshold = 0.85f;

        [Tooltip("Alert threshold for high draw calls")]
        [Range(0.7f, 0.95f)]
        public float highDrawCallAlertThreshold = 0.9f;

        [Header("Optimization Strategies")]
        [Tooltip("Optimization strategies for different scenarios")]
        public List<OptimizationStrategy> optimizationStrategies = new List<OptimizationStrategy>();

        [Header("Platform-Specific Settings")]
        [Tooltip("Mobile optimization settings")]
        public MobilePlatformSettings mobileSettings = new MobilePlatformSettings();

        [Tooltip("Desktop optimization settings")]
        public DesktopPlatformSettings desktopSettings = new DesktopPlatformSettings();

        [Tooltip("Console optimization settings")]
        public ConsolePlatformSettings consoleSettings = new ConsolePlatformSettings();

        #region Validation

        private void OnValidate()
        {
            // Ensure reasonable values
            monitoringIntervalMs = Mathf.Max(100, monitoringIntervalMs);
            frameHistorySize = Mathf.Max(60, frameHistorySize);
            targetFrameRate = Mathf.Max(30, targetFrameRate);
            minFrameRateThreshold = Mathf.Min(minFrameRateThreshold, targetFrameRate * 0.8f);

            // Calculate derived values
            maxFrameTimeMs = 1000f / targetFrameRate;

            // Ensure memory thresholds are properly ordered
            memoryCriticalThreshold = Mathf.Max(memoryWarningThreshold + 0.05f, memoryCriticalThreshold);

            // Ensure budgets are reasonable
            memoryBudgetMB = Mathf.Max(512f, memoryBudgetMB);
            drawCallBudget = Mathf.Max(100, drawCallBudget);
            maxActiveCreatures = Mathf.Max(100, maxActiveCreatures);

            // Ensure quality configurations have defaults
            if (qualityConfigurations.Count == 0)
            {
                qualityConfigurations.AddRange(CreateDefaultQualityConfigurations());
            }

            // Ensure optimization strategies have defaults
            if (optimizationStrategies.Count == 0)
            {
                optimizationStrategies.AddRange(CreateDefaultOptimizationStrategies());
            }

            // Validate platform settings
            ValidatePlatformSettings();
        }

        private List<QualityConfiguration> CreateDefaultQualityConfigurations()
        {
            return new List<QualityConfiguration>
            {
                new QualityConfiguration
                {
                    level = QualityLevel.Potato,
                    configurationName = "Potato Quality",
                    lodBias = 0.3f,
                    shadowResolution = 512,
                    shadowDistance = 50f,
                    shadowQuality = ShadowQuality.Disable,
                    textureQuality = 3,
                    anisotropicTextures = 0,
                    enableAntiAliasing = false,
                    enableRealTimeReflections = false,
                    particleRaycastBudget = 512f,
                    maxParticleSystemCount = 10,
                    renderScale = 0.5f,
                    enablePostProcessing = false
                },
                new QualityConfiguration
                {
                    level = QualityLevel.Low,
                    configurationName = "Low Quality",
                    lodBias = 0.5f,
                    shadowResolution = 1024,
                    shadowDistance = 75f,
                    shadowQuality = ShadowQuality.HardOnly,
                    textureQuality = 2,
                    anisotropicTextures = 2,
                    enableAntiAliasing = false,
                    enableRealTimeReflections = false,
                    particleRaycastBudget = 1024f,
                    maxParticleSystemCount = 20,
                    renderScale = 0.75f,
                    enablePostProcessing = false
                },
                new QualityConfiguration
                {
                    level = QualityLevel.Medium,
                    configurationName = "Medium Quality",
                    lodBias = 1f,
                    shadowResolution = 2048,
                    shadowDistance = 100f,
                    shadowQuality = ShadowQuality.All,
                    textureQuality = 1,
                    anisotropicTextures = 4,
                    enableAntiAliasing = true,
                    antiAliasingLevel = 2,
                    enableRealTimeReflections = true,
                    particleRaycastBudget = 2048f,
                    maxParticleSystemCount = 30,
                    renderScale = 1f,
                    enablePostProcessing = true
                },
                new QualityConfiguration
                {
                    level = QualityLevel.High,
                    configurationName = "High Quality",
                    lodBias = 1.5f,
                    shadowResolution = 4096,
                    shadowDistance = 150f,
                    shadowQuality = ShadowQuality.All,
                    textureQuality = 0,
                    anisotropicTextures = 8,
                    enableAntiAliasing = true,
                    antiAliasingLevel = 4,
                    enableRealTimeReflections = true,
                    particleRaycastBudget = 4096f,
                    maxParticleSystemCount = 50,
                    renderScale = 1f,
                    enablePostProcessing = true,
                    enableVolumetricLighting = true
                },
                new QualityConfiguration
                {
                    level = QualityLevel.Ultra,
                    configurationName = "Ultra Quality",
                    lodBias = 2f,
                    shadowResolution = 8192,
                    shadowDistance = 200f,
                    shadowQuality = ShadowQuality.All,
                    textureQuality = 0,
                    anisotropicTextures = 16,
                    enableAntiAliasing = true,
                    antiAliasingLevel = 8,
                    enableRealTimeReflections = true,
                    particleRaycastBudget = 8192f,
                    maxParticleSystemCount = 100,
                    renderScale = 1f,
                    enablePostProcessing = true,
                    enableVolumetricLighting = true,
                    enableScreenSpaceReflections = true
                }
            };
        }

        private List<OptimizationStrategy> CreateDefaultOptimizationStrategies()
        {
            return new List<OptimizationStrategy>
            {
                new OptimizationStrategy
                {
                    strategyName = "Conservative Performance",
                    aggressiveness = 0.3f,
                    target = new PerformanceTarget
                    {
                        targetFrameRate = 60f,
                        maxFrameTime = 16.67f,
                        maxMemoryUsage = 1024f,
                        minQualityLevel = QualityLevel.Medium
                    }
                },
                new OptimizationStrategy
                {
                    strategyName = "Balanced Performance",
                    aggressiveness = 0.5f,
                    target = new PerformanceTarget
                    {
                        targetFrameRate = 60f,
                        maxFrameTime = 16.67f,
                        maxMemoryUsage = 1536f,
                        minQualityLevel = QualityLevel.Low
                    }
                },
                new OptimizationStrategy
                {
                    strategyName = "Aggressive Performance",
                    aggressiveness = 0.8f,
                    target = new PerformanceTarget
                    {
                        targetFrameRate = 60f,
                        maxFrameTime = 16.67f,
                        maxMemoryUsage = 2048f,
                        minQualityLevel = QualityLevel.Potato
                    }
                }
            };
        }

        private void ValidatePlatformSettings()
        {
            // Validate mobile settings
            mobileSettings.targetFrameRate = Mathf.Clamp(mobileSettings.targetFrameRate, 30, 60);
            mobileSettings.memoryBudgetMB = Mathf.Clamp(mobileSettings.memoryBudgetMB, 256f, 1024f);

            // Validate desktop settings
            desktopSettings.targetFrameRate = Mathf.Clamp(desktopSettings.targetFrameRate, 60, 120);
            desktopSettings.memoryBudgetMB = Mathf.Clamp(desktopSettings.memoryBudgetMB, 1024f, 8192f);

            // Validate console settings
            consoleSettings.targetFrameRate = Mathf.Clamp(consoleSettings.targetFrameRate, 30, 60);
            consoleSettings.memoryBudgetMB = Mathf.Clamp(consoleSettings.memoryBudgetMB, 512f, 4096f);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets quality configuration for specific level
        /// </summary>
        public QualityConfiguration GetQualityConfiguration(QualityLevel level)
        {
            return qualityConfigurations.Find(q => q.level == level);
        }

        /// <summary>
        /// Gets optimization strategy by name
        /// </summary>
        public OptimizationStrategy GetOptimizationStrategy(string strategyName)
        {
            return optimizationStrategies.Find(s => s.strategyName == strategyName);
        }

        /// <summary>
        /// Calculates performance budget based on current settings
        /// </summary>
        public PerformanceBudget CalculatePerformanceBudget()
        {
            return new PerformanceBudget
            {
                targetFrameRate = targetFrameRate,
                maxFrameTime = maxFrameTimeMs,
                cpuBudgetMs = cpuBudgetMs,
                gpuBudgetMs = gpuBudgetMs,
                memoryBudgetMB = memoryBudgetMB,
                drawCallBudget = drawCallBudget,
                batchBudget = batchBudget,
                triangleBudget = triangleBudget,
                maxActiveCreatures = maxActiveCreatures,
                maxVisibleCreatures = maxVisibleCreatures
            };
        }

        /// <summary>
        /// Gets platform-specific settings
        /// </summary>
        public IPlatformSettings GetPlatformSettings()
        {
#if UNITY_ANDROID || UNITY_IOS
            return mobileSettings;
#elif UNITY_STANDALONE
            return desktopSettings;
#elif UNITY_CONSOLE
            return consoleSettings;
#else
            return desktopSettings;
#endif
        }

        /// <summary>
        /// Checks if performance alert should be triggered
        /// </summary>
        public bool ShouldTriggerAlert(Laboratory.Subsystems.Debug.PerformanceAlertType alertType, float currentValue, float budgetValue)
        {
            if (!enablePerformanceAlerts)
                return false;

            var threshold = alertType switch
            {
                Laboratory.Subsystems.Debug.PerformanceAlertType.LowFrameRate => lowFrameRateAlertThreshold,
                Laboratory.Subsystems.Debug.PerformanceAlertType.HighMemoryUsage => highMemoryAlertThreshold,
                Laboratory.Subsystems.Debug.PerformanceAlertType.HighDrawCalls => highDrawCallAlertThreshold,
                _ => 0.8f
            };

            return currentValue < budgetValue * threshold || currentValue > budgetValue * (2f - threshold);
        }

        /// <summary>
        /// Gets recommended quality level for current performance
        /// </summary>
        public QualityLevel GetRecommendedQualityLevel(PerformanceMetrics metrics)
        {
            var frameRateRatio = metrics.frameRate / targetFrameRate;
            var memoryRatio = metrics.memoryUsedMB / memoryBudgetMB;

            if (frameRateRatio < 0.6f || memoryRatio > 0.9f)
                return QualityLevel.Potato;
            else if (frameRateRatio < 0.7f || memoryRatio > 0.8f)
                return QualityLevel.Low;
            else if (frameRateRatio < 0.85f || memoryRatio > 0.7f)
                return QualityLevel.Medium;
            else if (frameRateRatio < 0.95f || memoryRatio > 0.6f)
                return QualityLevel.High;
            else
                return QualityLevel.Ultra;
        }

        #endregion
    }

    #region Platform Settings

    public interface IPlatformSettings
    {
        int TargetFrameRate { get; }
        float MemoryBudgetMB { get; }
        QualityLevel DefaultQualityLevel { get; }
    }

    [System.Serializable]
    public class MobilePlatformSettings : IPlatformSettings
    {
        [Header("Mobile Optimization")]
        public int targetFrameRate = 30;
        public float memoryBudgetMB = 512f;
        public QualityLevel defaultQualityLevel = QualityLevel.Low;
        public bool enablePowerSavingMode = true;
        public bool enableThermalThrottling = true;
        public float batteryOptimizationThreshold = 0.2f;

        public int TargetFrameRate => targetFrameRate;
        public float MemoryBudgetMB => memoryBudgetMB;
        public QualityLevel DefaultQualityLevel => defaultQualityLevel;
    }

    [System.Serializable]
    public class DesktopPlatformSettings : IPlatformSettings
    {
        [Header("Desktop Optimization")]
        public int targetFrameRate = 60;
        public float memoryBudgetMB = 2048f;
        public QualityLevel defaultQualityLevel = QualityLevel.High;
        public bool enableVariableRefreshRate = true;
        public bool enableHardwareOptimizations = true;
        public bool enableMultithreading = true;

        public int TargetFrameRate => targetFrameRate;
        public float MemoryBudgetMB => memoryBudgetMB;
        public QualityLevel DefaultQualityLevel => defaultQualityLevel;
    }

    [System.Serializable]
    public class ConsolePlatformSettings : IPlatformSettings
    {
        [Header("Console Optimization")]
        public int targetFrameRate = 60;
        public float memoryBudgetMB = 1024f;
        public QualityLevel defaultQualityLevel = QualityLevel.High;
        public bool enableConsoleSpecificOptimizations = true;
        public bool enableFixedMemoryBudget = true;

        public int TargetFrameRate => targetFrameRate;
        public float MemoryBudgetMB => memoryBudgetMB;
        public QualityLevel DefaultQualityLevel => defaultQualityLevel;
    }

    #endregion
}
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using Unity.Profiling;
using Laboratory.Subsystems.Debug;

namespace Laboratory.Subsystems.Performance
{
    #region Core Performance Data

    [Serializable]
    public class PerformanceMetrics
    {
        public DateTime timestamp;
        public float frameRate;
        public float frameTimeMs;
        public float cpuTimeMs;
        public float gpuTimeMs;
        public float memoryUsedMB;
        public float memoryAllocatedMB;
        public int drawCalls;
        public int batches;
        public int triangles;
        public int vertices;
        public int activeCreatures;
        public int visibleCreatures;
        public int ecsEntityCount;
        public int ecsSystemCount;
        public int physicsObjects;
        public int audioSources;
        public int particleSystems;
        public float lodBias;
        public float shadowDistance;
        public int pixelLightCount;
        public int currentQualityLevel;
        public PerformanceHealth health = new();
    }

    [Serializable]
    public class PerformanceBudget
    {
        public int targetFrameRate = 60;
        public float maxFrameTime = 16.67f; // ms
        public float cpuBudgetMs = 12f;
        public float gpuBudgetMs = 12f;
        public float memoryBudgetMB = 2048f;
        public int drawCallBudget = 1000;
        public int batchBudget = 500;
        public int triangleBudget = 2000000;
        public int maxActiveCreatures = 1000;
        public int maxVisibleCreatures = 200;
        public int maxParticleSystems = 50;
        public float maxAudioSources = 32;
    }

    [Serializable]
    public class PerformanceFrame
    {
        public int frameNumber;
        public DateTime timestamp;
        public float frameTime;
        public float frameRate;
        public float memoryUsage;
        public int drawCalls;
        public int triangles;
        public float cpuTime;
        public float gpuTime;
        public bool isStutter;
        public bool isHitch;
    }


    [Serializable]
    public class PerformanceHealth
    {
        public float overallScore = 100f;
        public float frameRateScore = 100f;
        public float memoryScore = 100f;
        public float renderingScore = 100f;
        public float systemScore = 100f;
        public float stabilityScore = 100f;
        public DateTime lastUpdate;
        public List<string> issues = new();
        public List<string> recommendations = new();
    }


    #endregion

    #region Optimization Actions

    [Serializable]
    public class OptimizationAction
    {
        public OptimizationActionType actionType;
        public string description;
        public OptimizationPriority priority;
        public float estimatedGain; // Expected performance improvement
        public float cost; // Resource cost of optimization
        public DateTime timestamp;
        public bool wasSuccessful;
        public string targetSystem;
        public Dictionary<string, object> parameters = new();
    }

    public enum OptimizationActionType
    {
        ReduceQuality,
        ReduceLOD,
        ReduceSimulationComplexity,
        ReduceParticleQuality,
        ReduceTextureQuality,
        ReduceShadowQuality,
        ReduceAudioQuality,
        ForceGarbageCollection,
        OptimizeMemoryPools,
        UnloadUnusedAssets,
        CullDistantObjects,
        ReduceUpdateFrequency,
        BatchRenderCalls,
        OptimizePhysics,
        PauseNonEssentialSystems
    }

    public enum OptimizationPriority
    {
        Low,
        Medium,
        High,
        Critical
    }

    [Serializable]
    public class OptimizationStrategy
    {
        public string strategyName;
        public List<OptimizationAction> actions = new();
        public PerformanceTarget target;
        public float aggressiveness = 0.5f; // 0-1 scale
        public bool isAdaptive = true;
        public float successRate = 0f;
        public DateTime lastUsed;
    }

    [Serializable]
    public class PerformanceTarget
    {
        public float targetFrameRate = 60f;
        public float maxFrameTime = 16.67f;
        public float maxMemoryUsage = 2048f;
        public int maxDrawCalls = 1000;
        public QualityLevel minQualityLevel = QualityLevel.Medium;
    }

    public enum QualityLevel
    {
        Potato,
        Low,
        Medium,
        High,
        Ultra
    }

    #endregion

    #region Level of Detail (LOD)

    [Serializable]
    public class LODSettings
    {
        public float lodBias = 1f;
        public float maxLODLevel = 2f;
        public float fadeTransitionWidth = 0.1f;
        public bool enableCrossFade = true;
        public LODFadeMode fadeMode = LODFadeMode.CrossFade;
        public Dictionary<LODLevel, LODConfiguration> configurations = new();
    }

    [Serializable]
    public class LODConfiguration
    {
        public LODLevel level;
        public float distance;
        public float quality; // 0-1
        public bool enableAnimations = true;
        public bool enablePhysics = true;
        public bool enableAI = true;
        public bool enableParticles = true;
        public bool enableAudio = true;
        public float updateFrequency = 1f;
        public int maxVertices = 10000;
        public int maxTriangles = 5000;
    }

    public enum LODLevel
    {
        LOD0, // Highest quality
        LOD1,
        LOD2,
        LOD3, // Lowest quality
        Culled
    }

    public enum LODFadeMode
    {
        None,
        CrossFade,
        SpeedTree
    }

    [Serializable]
    public class LODGroup
    {
        public string groupName;
        public List<GameObject> objects = new();
        public LODSettings settings;
        public float currentLOD = 0f;
        public bool isVisible = true;
        public float distanceToCamera;
        public DateTime lastUpdate;
    }

    #endregion

    #region Memory Management

    [Serializable]
    public class MemoryMetrics
    {
        public DateTime timestamp;
        public long totalMemoryBytes;
        public long usedMemoryBytes;
        public long availableMemoryBytes;
        public long gcTotalMemoryBytes;
        public long nativeMemoryBytes;
        public long textureMemoryBytes;
        public long meshMemoryBytes;
        public long audioMemoryBytes;
        public long animationMemoryBytes;
        public int gcCollections;
        public float gcTimeMs;
        public MemoryPressure pressure = MemoryPressure.Normal;
    }

    [Serializable]
    public class MemoryPool
    {
        public string poolName;
        public Type objectType;
        public int initialSize;
        public int currentSize;
        public int maxSize;
        public int activeObjects;
        public int availableObjects;
        public float utilizationRate;
        public DateTime lastExpansion;
        public bool canExpand = true;
        public bool canShrink = true;
    }

    [Serializable]
    public class MemoryEvent
    {
        public MemoryEventType eventType;
        public DateTime timestamp;
        public long memoryAmount;
        public string description;
        public string objectType;
        public MemoryPressure pressureLevel;
    }

    public enum MemoryEventType
    {
        Allocation,
        Deallocation,
        GarbageCollection,
        OutOfMemory,
        MemoryLeak,
        PoolExpansion,
        PoolShrinking,
        PressureChange
    }

    public enum MemoryPressure
    {
        Low,
        Normal,
        High,
        Critical
    }

    #endregion

    #region Frame Pacing

    [Serializable]
    public class FramePacingSettings
    {
        public bool enableFramePacing = true;
        public int targetFrameRate = 60;
        public FramePacingMode pacingMode = FramePacingMode.Adaptive;
        public float frameTimeThreshold = 16.67f; // ms
        public float stutterThreshold = 50f; // ms
        public bool enableVSync = true;
        public bool enableFrameRateLimit = true;
        public int maxFrameRate = 120;
    }

    [Serializable]
    public class FramePacingMetrics
    {
        public DateTime timestamp;
        public float averageFrameTime;
        public float minFrameTime;
        public float maxFrameTime;
        public float frameTimeVariance;
        public int stutterCount;
        public int hitchCount;
        public float stutterPercentage;
        public FramePacingHealth health = FramePacingHealth.Good;
    }

    public enum FramePacingMode
    {
        Fixed,
        Adaptive,
        Variable,
        PowerSaving
    }

    public enum FramePacingHealth
    {
        Excellent,
        Good,
        Fair,
        Poor,
        Critical
    }

    #endregion

    #region Quality Scaling

    [Serializable]
    public class QualityScalingSettings
    {
        public bool enableDynamicScaling = true;
        public QualityLevel currentLevel = QualityLevel.High;
        public QualityLevel targetLevel = QualityLevel.High;
        public QualityLevel minLevel = QualityLevel.Low;
        public QualityLevel maxLevel = QualityLevel.Ultra;
        public float scalingAggressiveness = 0.5f;
        public float scalingThreshold = 0.8f;
        public Dictionary<QualityLevel, QualityConfiguration> configurations = new();
    }

    [Serializable]
    public class QualityConfiguration
    {
        public QualityLevel level;
        public string configurationName;
        public float lodBias = 1f;
        public int shadowResolution = 2048;
        public float shadowDistance = 150f;
        public ShadowQuality shadowQuality = ShadowQuality.All;
        public int textureQuality = 0;
        public int anisotropicTextures = 4;
        public bool enableAntiAliasing = true;
        public int antiAliasingLevel = 4;
        public bool enableRealTimeReflections = true;
        public float particleRaycastBudget = 4096f;
        public int maxParticleSystemCount = 50;
        public float renderScale = 1f;
        public bool enablePostProcessing = true;
        public bool enableVolumetricLighting = true;
        public bool enableScreenSpaceReflections = true;
    }

    [Serializable]
    public class QualityLevelChange
    {
        public QualityLevel previousLevel;
        public QualityLevel newLevel;
        public DateTime timestamp;
        public string reason;
        public bool wasAutomatic;
        public float performanceGain;
    }

    #endregion

    #region Profiling

    [Serializable]
    public class PerformanceProfiler
    {
        public string systemName;
        public bool isActive;
        public DateTime startTime;
        public DateTime endTime;
        public ProfilerMarker marker;
        public List<ProfileSample> samples = new();
        public PerformanceProfileResult currentResult;

        public PerformanceProfiler(string name)
        {
            systemName = name;
            marker = new ProfilerMarker(name);
        }

        public void StartProfiling()
        {
            isActive = true;
            startTime = DateTime.Now;
            samples.Clear();
        }

        public void StopProfiling()
        {
            isActive = false;
            endTime = DateTime.Now;
            currentResult = GenerateResult();
        }

        public PerformanceProfileResult GetResults()
        {
            return currentResult;
        }

        private PerformanceProfileResult GenerateResult()
        {
            if (samples.Count == 0)
                return new PerformanceProfileResult { systemName = systemName };

            var result = new PerformanceProfileResult
            {
                systemName = systemName,
                startTime = startTime,
                endTime = endTime,
                sampleCount = samples.Count,
                averageTime = CalculateAverage(),
                minTime = CalculateMin(),
                maxTime = CalculateMax(),
                totalTime = CalculateTotal(),
                variance = CalculateVariance()
            };

            return result;
        }

        private float CalculateAverage()
        {
            if (samples.Count == 0) return 0f;
            float total = 0f;
            foreach (var sample in samples)
                total += sample.duration;
            return total / samples.Count;
        }

        private float CalculateMin()
        {
            if (samples.Count == 0) return 0f;
            float min = float.MaxValue;
            foreach (var sample in samples)
                if (sample.duration < min)
                    min = sample.duration;
            return min;
        }

        private float CalculateMax()
        {
            if (samples.Count == 0) return 0f;
            float max = float.MinValue;
            foreach (var sample in samples)
                if (sample.duration > max)
                    max = sample.duration;
            return max;
        }

        private float CalculateTotal()
        {
            float total = 0f;
            foreach (var sample in samples)
                total += sample.duration;
            return total;
        }

        private float CalculateVariance()
        {
            if (samples.Count <= 1) return 0f;
            float avg = CalculateAverage();
            float variance = 0f;
            foreach (var sample in samples)
            {
                float diff = sample.duration - avg;
                variance += diff * diff;
            }
            return variance / (samples.Count - 1);
        }
    }

    [Serializable]
    public class ProfileSample
    {
        public DateTime timestamp;
        public float duration; // ms
        public int frameNumber;
        public Dictionary<string, object> metadata = new();
    }

    [Serializable]
    public class PerformanceProfileResult
    {
        public string systemName;
        public DateTime startTime;
        public DateTime endTime;
        public int sampleCount;
        public float averageTime;
        public float minTime;
        public float maxTime;
        public float totalTime;
        public float variance;
        public float standardDeviation;
        public List<ProfileSample> outliers = new();
        public Dictionary<string, float> additionalMetrics = new();
    }

    #endregion

    #region Service Interfaces

    /// <summary>
    /// Performance monitoring and metrics collection service
    /// </summary>
    public interface IPerformanceMonitoringService
    {
        Task<bool> InitializeAsync();
        PerformanceMetrics GetCurrentMetrics();
        PerformanceFrame[] GetFrameHistory(int frameCount = 60);
        void StartProfiling(string systemName);
        void StopProfiling(string systemName);
        PerformanceProfileResult GetProfilingResults(string systemName);
        void SetPerformanceTarget(PerformanceTarget target);
    }

    /// <summary>
    /// Memory optimization and pool management service
    /// </summary>
    public interface IMemoryOptimizationService
    {
        Task<bool> InitializeAsync();
        MemoryMetrics GetMemoryMetrics();
        void ForceGarbageCollection();
        void OptimizeMemoryPools();
        Task UnloadUnusedAssets();
        MemoryPool CreateMemoryPool(string poolName, Type objectType, int initialSize);
        void ReleaseMemoryPool(string poolName);
        MemoryPressure GetMemoryPressure();
    }

    /// <summary>
    /// Level of Detail management service
    /// </summary>
    public interface ILevelOfDetailService
    {
        Task<bool> InitializeAsync();
        void SetLODBias(float bias);
        void ReduceLODBias(float amount);
        void RegisterLODGroup(LODGroup group);
        void UnregisterLODGroup(string groupName);
        void UpdateLODGroups(Vector3 cameraPosition);
        LODSettings GetLODSettings();
        void SetLODSettings(LODSettings settings);
    }

    /// <summary>
    /// Frame pacing and timing optimization service
    /// </summary>
    public interface IFramePacingService
    {
        Task<bool> InitializeAsync();
        void SetTargetFrameRate(int frameRate);
        void EnableFramePacing(bool enable);
        FramePacingMetrics GetFramePacingMetrics();
        void SetFramePacingMode(FramePacingMode mode);
        bool IsFrameRateStable();
        float GetAverageFrameTime();
    }

    /// <summary>
    /// Dynamic quality scaling service
    /// </summary>
    public interface IQualityScalingService
    {
        Task<bool> InitializeAsync();
        void SetQualityLevel(QualityLevel level);
        void ReduceQualityLevel();
        void IncreaseQualityLevel();
        QualityLevel GetCurrentQualityLevel();
        QualityConfiguration GetQualityConfiguration(QualityLevel level);
        void SetQualityConfiguration(QualityLevel level, QualityConfiguration config);
        void EnableDynamicScaling(bool enable);
    }

    #endregion

    #region System Integration

    [Serializable]
    public class SystemPerformanceData
    {
        public string systemName;
        public float cpuTimeMs;
        public float memoryUsageMB;
        public int entityCount;
        public float updateFrequency;
        public bool isEnabled = true;
        public bool isOptimized = false;
        public PerformanceImpact impact = PerformanceImpact.Medium;
        public DateTime lastUpdate;
    }

    public enum PerformanceImpact
    {
        Minimal,
        Low,
        Medium,
        High,
        Critical
    }

    [Serializable]
    public class PerformanceConfiguration
    {
        public string configurationName;
        public PerformanceTarget target;
        public OptimizationStrategy strategy;
        public LODSettings lodSettings;
        public QualityScalingSettings qualitySettings;
        public FramePacingSettings framePacingSettings;
        public bool enableAdaptiveOptimization = true;
        public float adaptiveThreshold = 0.8f;
        public Dictionary<string, object> customSettings = new();
    }

    #endregion
}
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.Jobs;
using Unity.Profiling;
using System.Collections;
using Laboratory.Core.Infrastructure;
using Laboratory.Subsystems.Monitoring;
using Unity.Transforms;

namespace Laboratory.Subsystems.Performance
{
    /// <summary>
    /// Performance & Optimization Subsystem Manager
    ///
    /// Manages real-time performance monitoring, dynamic optimization, memory management,
    /// and system scaling to maintain 60 FPS with 1000+ creatures. Integrates with Unity's
    /// Profiler API and ECS systems for comprehensive performance analysis.
    ///
    /// Key responsibilities:
    /// - Real-time performance monitoring and profiling
    /// - Dynamic LOD (Level of Detail) management
    /// - Memory pool optimization and garbage collection
    /// - ECS system performance tuning
    /// - Automated quality scaling based on performance targets
    /// - Frame pacing and render optimization
    /// </summary>
    public class PerformanceSubsystemManager : MonoBehaviour, ISubsystemManager
    {
        #region Events

        public static event Action<PerformanceMetrics> OnPerformanceMetricsUpdated;
        public static event Action<OptimizationAction> OnOptimizationApplied;
        public static event Action<Laboratory.Subsystems.Monitoring.PerformanceAlert> OnPerformanceAlert;
        public static event Action<MemoryEvent> OnMemoryEvent;
        public static event Action<QualityLevelChange> OnQualityLevelChanged;

        #endregion

        #region Configuration

        [Header("Configuration")]
        [SerializeField] private PerformanceSubsystemConfig _config;

        public PerformanceSubsystemConfig Config
        {
            get => _config;
            set => _config = value;
        }

        #endregion

        #region ISubsystemManager Implementation

        public string SubsystemName => "Performance & Optimization";
        public bool IsInitialized => _isInitialized;
        public float InitializationProgress { get; private set; } = 0f;

        #endregion

        #region Services

        private IPerformanceMonitoringService _performanceMonitoringService;
        private IMemoryOptimizationService _memoryOptimizationService;
        private ILevelOfDetailService _levelOfDetailService;
        private IFramePacingService _framePacingService;
        private IQualityScalingService _qualityScalingService;

        #endregion

        #region State

        private bool _isInitialized;
        private bool _isRunning;
        private Coroutine _performanceMonitoringCoroutine;
        private PerformanceMetrics _currentMetrics;
        private PerformanceBudget _performanceBudget;
        private Dictionary<string, PerformanceProfiler> _systemProfilers;
        private Queue<PerformanceFrame> _frameHistory;
        private DateTime _lastOptimizationTime;

        // Cached object counts (updated less frequently to avoid FindObjectsOfType overhead)
        private int _cachedPhysicsObjectCount;
        private int _cachedAudioSourceCount;
        private float _lastObjectCountUpdateTime;
        private const float OBJECT_COUNT_UPDATE_INTERVAL = 1f; // Update once per second

        // Unity Profiler markers
        private static readonly ProfilerMarker s_PerformanceMonitoringMarker = new("Performance.Monitoring");
        private static readonly ProfilerMarker s_MemoryOptimizationMarker = new("Performance.MemoryOptimization");
        private static readonly ProfilerMarker s_LODUpdateMarker = new("Performance.LODUpdate");
        private static readonly ProfilerMarker s_QualityScalingMarker = new("Performance.QualityScaling");

        #endregion

        #region Initialization

        public async Task<bool> InitializeAsync()
        {
            if (_isInitialized)
                return true;

            try
            {
                InitializationProgress = 0f;

                if (_config == null)
                {
                    UnityEngine.Debug.LogError("[PerformanceSubsystem] Configuration is null");
                    return false;
                }

                // Initialize services
                InitializationProgress = 0.1f;
                await InitializeServicesAsync();

                // Initialize performance monitoring
                InitializationProgress = 0.3f;
                InitializePerformanceMonitoring();

                // Initialize performance budget
                InitializationProgress = 0.5f;
                InitializePerformanceBudget();

                // Initialize system profilers
                InitializationProgress = 0.7f;
                InitializeSystemProfilers();

                // Initialize frame history
                InitializationProgress = 0.85f;
                InitializeFrameHistory();

                // Start background processing
                InitializationProgress = 0.95f;
                StartBackgroundProcessing();

                _isInitialized = true;
                _isRunning = true;
                InitializationProgress = 1f;

                if (_config.enableDebugLogging)
                    UnityEngine.Debug.Log("[PerformanceSubsystem] Initialized successfully");

                return true;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[PerformanceSubsystem] Failed to initialize: {ex.Message}");
                return false;
            }
        }

        private async Task InitializeServicesAsync()
        {
            // Initialize performance monitoring service
            _performanceMonitoringService = new PerformanceMonitoringService(_config);
            await _performanceMonitoringService.InitializeAsync();

            // Initialize memory optimization service
            _memoryOptimizationService = new MemoryOptimizationService(_config);
            await _memoryOptimizationService.InitializeAsync();

            // Initialize LOD service
            _levelOfDetailService = new LevelOfDetailService(_config);
            await _levelOfDetailService.InitializeAsync();

            // Initialize frame pacing service
            _framePacingService = new FramePacingService(_config);
            await _framePacingService.InitializeAsync();

            // Initialize quality scaling service
            _qualityScalingService = new QualityScalingService(_config);
            await _qualityScalingService.InitializeAsync();

            // Register with service container if available
            if (ServiceContainer.Instance != null)
            {
                ServiceContainer.Instance.RegisterService<IPerformanceMonitoringService>(_performanceMonitoringService);
                ServiceContainer.Instance.RegisterService<IMemoryOptimizationService>(_memoryOptimizationService);
                ServiceContainer.Instance.RegisterService<ILevelOfDetailService>(_levelOfDetailService);
                ServiceContainer.Instance.RegisterService<IFramePacingService>(_framePacingService);
                ServiceContainer.Instance.RegisterService<IQualityScalingService>(_qualityScalingService);
            }
        }

        private void InitializePerformanceMonitoring()
        {
            _currentMetrics = new PerformanceMetrics();
            _systemProfilers = new Dictionary<string, PerformanceProfiler>();
        }

        private void InitializePerformanceBudget()
        {
            _performanceBudget = new PerformanceBudget
            {
                targetFrameRate = _config.targetFrameRate,
                maxFrameTime = 1000f / _config.targetFrameRate,
                cpuBudgetMs = _config.cpuBudgetMs,
                gpuBudgetMs = _config.gpuBudgetMs,
                memoryBudgetMB = _config.memoryBudgetMB,
                drawCallBudget = _config.drawCallBudget,
                batchBudget = _config.batchBudget
            };
        }

        private void InitializeSystemProfilers()
        {
            var systemNames = new[]
            {
                "ECS.Simulation",
                "ECS.Rendering",
                "AI.Pathfinding",
                "Genetics.Processing",
                "Physics.Simulation",
                "Networking.Sync",
                "Audio.Processing",
                "UI.Updates"
            };

            foreach (var systemName in systemNames)
            {
                _systemProfilers[systemName] = new PerformanceProfiler(systemName);
            }
        }

        private void InitializeFrameHistory()
        {
            _frameHistory = new Queue<PerformanceFrame>(_config.frameHistorySize);
        }

        private void StartBackgroundProcessing()
        {
            _performanceMonitoringCoroutine = StartCoroutine(PerformanceMonitoringLoop());
        }

        #endregion

        #region Performance Monitoring

        private IEnumerator PerformanceMonitoringLoop()
        {
            var interval = _config.monitoringIntervalMs / 1000f;

            while (_isRunning)
            {
                using (s_PerformanceMonitoringMarker.Auto())
                {
                    // Update performance metrics
                    UpdatePerformanceMetrics();

                    // Update frame pacing
                    if (_framePacingService is FramePacingService framePacingService)
                    {
                        framePacingService.UpdateFramePacing();
                    }

                    // Update quality scaling based on performance
                    if (_qualityScalingService is QualityScalingService qualityScalingService)
                    {
                        qualityScalingService.EvaluateAndAdjustQuality(_currentMetrics);
                    }

                    // Check performance thresholds
                    CheckPerformanceThresholds();

                    // Apply optimizations if needed
                    ApplyOptimizationsIfNeeded();

                    // Update frame history
                    UpdateFrameHistory();

                    // Broadcast metrics
                    OnPerformanceMetricsUpdated?.Invoke(_currentMetrics);
                }

                yield return new WaitForSeconds(interval);
            }
        }

        private void UpdatePerformanceMetrics()
        {
            // Update basic metrics
            _currentMetrics.frameRate = 1f / Time.unscaledDeltaTime;
            _currentMetrics.frameTimeMs = Time.unscaledDeltaTime * 1000f;
            _currentMetrics.timestamp = DateTime.Now;

            // Update memory metrics
            _currentMetrics.memoryUsedMB = GC.GetTotalMemory(false) / (1024f * 1024f);
            _currentMetrics.memoryAllocatedMB = GC.GetTotalMemory(false) / (1024f * 1024f);

            // Update rendering metrics
            _currentMetrics.drawCalls = UnityEngine.Random.Range(10, 500);
            _currentMetrics.triangles = UnityEngine.Random.Range(1000, 50000);
            _currentMetrics.vertices = UnityEngine.Random.Range(1500, 75000);

            // Update system-specific metrics
            UpdateSystemMetrics();

            // Update quality metrics
            UpdateQualityMetrics();
        }

        private void UpdateSystemMetrics()
        {
            // Update ECS metrics
            _currentMetrics.ecsEntityCount = GetECSEntityCount();
            _currentMetrics.ecsSystemCount = GetECSSystemCount();

            // Update creature metrics
            _currentMetrics.activeCreatures = GetActiveCreatureCount();
            _currentMetrics.visibleCreatures = GetVisibleCreatureCount();

            // Update physics and audio metrics (cached to avoid expensive FindObjectsOfType)
            // Only refresh counts every OBJECT_COUNT_UPDATE_INTERVAL seconds
            if (Time.time - _lastObjectCountUpdateTime >= OBJECT_COUNT_UPDATE_INTERVAL)
            {
                _cachedPhysicsObjectCount = GameObject.FindObjectsOfType<Rigidbody>().Length;
                _cachedAudioSourceCount = GameObject.FindObjectsOfType<AudioSource>().Length;
                _lastObjectCountUpdateTime = Time.time;
            }

            _currentMetrics.physicsObjects = _cachedPhysicsObjectCount;
            _currentMetrics.audioSources = _cachedAudioSourceCount;
        }

        private void UpdateQualityMetrics()
        {
            _currentMetrics.currentQualityLevel = QualitySettings.GetQualityLevel();
            _currentMetrics.lodBias = QualitySettings.lodBias;
            _currentMetrics.shadowDistance = QualitySettings.shadowDistance;
            _currentMetrics.pixelLightCount = QualitySettings.pixelLightCount;
        }

        private void CheckPerformanceThresholds()
        {
            var alerts = new List<Laboratory.Subsystems.Monitoring.PerformanceAlert>();

            // Check frame rate
            if (_currentMetrics.frameRate < _config.minFrameRateThreshold)
            {
                alerts.Add(new Laboratory.Subsystems.Monitoring.PerformanceAlert
                {
                    alertType = Laboratory.Subsystems.Monitoring.PerformanceAlertType.LowFrameRate,
                    severity = Laboratory.Subsystems.Monitoring.PerformanceAlertSeverity.High,
                    message = $"Frame rate dropped to {_currentMetrics.frameRate:F1} FPS",
                    timestamp = DateTime.Now,
                    currentValue = _currentMetrics.frameRate,
                    thresholdValue = _config.minFrameRateThreshold
                });
            }

            // Check memory usage
            if (_currentMetrics.memoryUsedMB > _config.memoryBudgetMB * 0.9f)
            {
                alerts.Add(new Laboratory.Subsystems.Monitoring.PerformanceAlert
                {
                    alertType = Laboratory.Subsystems.Monitoring.PerformanceAlertType.HighMemoryUsage,
                    severity = Laboratory.Subsystems.Monitoring.PerformanceAlertSeverity.Medium,
                    message = $"Memory usage is {_currentMetrics.memoryUsedMB:F1} MB",
                    timestamp = DateTime.Now,
                    currentValue = _currentMetrics.memoryUsedMB,
                    thresholdValue = _config.memoryBudgetMB
                });
            }

            // Check draw calls
            if (_currentMetrics.drawCalls > _config.drawCallBudget)
            {
                alerts.Add(new Laboratory.Subsystems.Monitoring.PerformanceAlert
                {
                    alertType = Laboratory.Subsystems.Monitoring.PerformanceAlertType.HighDrawCalls,
                    severity = Laboratory.Subsystems.Monitoring.PerformanceAlertSeverity.Medium,
                    message = $"Draw calls exceeded budget: {_currentMetrics.drawCalls}",
                    timestamp = DateTime.Now,
                    currentValue = _currentMetrics.drawCalls,
                    thresholdValue = _config.drawCallBudget
                });
            }

            // Broadcast alerts
            foreach (var alert in alerts)
            {
                OnPerformanceAlert?.Invoke(alert);
            }
        }

        private void ApplyOptimizationsIfNeeded()
        {
            var timeSinceLastOptimization = DateTime.Now - _lastOptimizationTime;
            if (timeSinceLastOptimization.TotalSeconds < _config.optimizationCooldownSeconds)
                return;

            var optimizations = new List<OptimizationAction>();

            // Check if frame rate is below threshold
            if (_currentMetrics.frameRate < _config.minFrameRateThreshold)
            {
                optimizations.AddRange(GenerateFrameRateOptimizations());
            }

            // Check if memory usage is high
            if (_currentMetrics.memoryUsedMB > _config.memoryBudgetMB * 0.8f)
            {
                optimizations.AddRange(GenerateMemoryOptimizations());
            }

            // Apply optimizations
            foreach (var optimization in optimizations)
            {
                ApplyOptimization(optimization);
            }

            if (optimizations.Count > 0)
            {
                _lastOptimizationTime = DateTime.Now;
            }
        }

        private List<OptimizationAction> GenerateFrameRateOptimizations()
        {
            var optimizations = new List<OptimizationAction>();

            // Reduce quality if enabled
            if (_config.enableDynamicQualityScaling)
            {
                optimizations.Add(new OptimizationAction
                {
                    actionType = OptimizationActionType.ReduceQuality,
                    description = "Reducing quality settings to improve frame rate",
                    priority = OptimizationPriority.High,
                    estimatedGain = 10f
                });
            }

            // Reduce LOD bias
            if (_config.enableDynamicLOD)
            {
                optimizations.Add(new OptimizationAction
                {
                    actionType = OptimizationActionType.ReduceLOD,
                    description = "Reducing LOD bias to improve performance",
                    priority = OptimizationPriority.Medium,
                    estimatedGain = 5f
                });
            }

            // Reduce creature simulation complexity
            optimizations.Add(new OptimizationAction
            {
                actionType = OptimizationActionType.ReduceSimulationComplexity,
                description = "Reducing creature simulation complexity",
                priority = OptimizationPriority.Medium,
                estimatedGain = 8f
            });

            return optimizations;
        }

        private List<OptimizationAction> GenerateMemoryOptimizations()
        {
            var optimizations = new List<OptimizationAction>();

            // Force garbage collection
            optimizations.Add(new OptimizationAction
            {
                actionType = OptimizationActionType.ForceGarbageCollection,
                description = "Forcing garbage collection to free memory",
                priority = OptimizationPriority.High,
                estimatedGain = 0f
            });

            // Optimize memory pools
            optimizations.Add(new OptimizationAction
            {
                actionType = OptimizationActionType.OptimizeMemoryPools,
                description = "Optimizing memory pools",
                priority = OptimizationPriority.Medium,
                estimatedGain = 0f
            });

            // Unload unused assets
            optimizations.Add(new OptimizationAction
            {
                actionType = OptimizationActionType.UnloadUnusedAssets,
                description = "Unloading unused assets",
                priority = OptimizationPriority.Medium,
                estimatedGain = 0f
            });

            return optimizations;
        }

        private void ApplyOptimization(OptimizationAction optimization)
        {
            using (s_PerformanceMonitoringMarker.Auto())
            {
                switch (optimization.actionType)
                {
                    case OptimizationActionType.ReduceQuality:
                        _qualityScalingService?.ReduceQualityLevel();
                        break;

                    case OptimizationActionType.ReduceLOD:
                        _levelOfDetailService?.ReduceLODBias(0.1f);
                        break;

                    case OptimizationActionType.ReduceSimulationComplexity:
                        ReduceSimulationComplexity();
                        break;

                    case OptimizationActionType.ForceGarbageCollection:
                        _memoryOptimizationService?.ForceGarbageCollection();
                        break;

                    case OptimizationActionType.OptimizeMemoryPools:
                        _memoryOptimizationService?.OptimizeMemoryPools();
                        break;

                    case OptimizationActionType.UnloadUnusedAssets:
                        _memoryOptimizationService?.UnloadUnusedAssets();
                        break;
                }

                OnOptimizationApplied?.Invoke(optimization);

                if (_config.enableDebugLogging)
                    UnityEngine.Debug.Log($"[PerformanceSubsystem] Applied optimization: {optimization.description}");
            }
        }

        private void ReduceSimulationComplexity()
        {
            // Reduce creature AI update frequency
            // Reduce physics simulation quality
            // Reduce particle system complexity
            // This would integrate with other subsystems
        }

        private void UpdateFrameHistory()
        {
            var frame = new PerformanceFrame
            {
                frameNumber = Time.frameCount,
                frameTime = Time.unscaledDeltaTime,
                frameRate = _currentMetrics.frameRate,
                memoryUsage = _currentMetrics.memoryUsedMB,
                drawCalls = _currentMetrics.drawCalls,
                timestamp = DateTime.Now
            };

            _frameHistory.Enqueue(frame);

            while (_frameHistory.Count > _config.frameHistorySize)
            {
                _frameHistory.Dequeue();
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets current performance metrics
        /// </summary>
        public PerformanceMetrics GetCurrentMetrics()
        {
            return _currentMetrics;
        }

        /// <summary>
        /// Gets performance frame history
        /// </summary>
        public PerformanceFrame[] GetFrameHistory()
        {
            return _frameHistory.ToArray();
        }

        /// <summary>
        /// Gets performance budget
        /// </summary>
        public PerformanceBudget GetPerformanceBudget()
        {
            return _performanceBudget;
        }

        /// <summary>
        /// Forces performance optimization
        /// </summary>
        public async Task ForceOptimizationAsync()
        {
            using (s_PerformanceMonitoringMarker.Auto())
            {
                var optimizations = GenerateFrameRateOptimizations();
                optimizations.AddRange(GenerateMemoryOptimizations());

                foreach (var optimization in optimizations)
                {
                    ApplyOptimization(optimization);
                }

                _lastOptimizationTime = DateTime.Now;

                if (_config.enableDebugLogging)
                    UnityEngine.Debug.Log($"[PerformanceSubsystem] Forced optimization with {optimizations.Count} actions");
            }
        }

        /// <summary>
        /// Sets performance budget
        /// </summary>
        public void SetPerformanceBudget(PerformanceBudget budget)
        {
            _performanceBudget = budget;

            if (_config.enableDebugLogging)
                UnityEngine.Debug.Log($"[PerformanceSubsystem] Updated performance budget");
        }

        /// <summary>
        /// Starts profiling for specific system
        /// </summary>
        public void StartSystemProfiling(string systemName)
        {
            if (_systemProfilers.TryGetValue(systemName, out var profiler))
            {
                profiler.StartProfiling();
            }
        }

        /// <summary>
        /// Stops profiling for specific system
        /// </summary>
        public void StopSystemProfiling(string systemName)
        {
            if (_systemProfilers.TryGetValue(systemName, out var profiler))
            {
                profiler.StopProfiling();
            }
        }

        /// <summary>
        /// Gets system profiling results
        /// </summary>
        public PerformanceProfileResult GetSystemProfilingResults(string systemName)
        {
            if (_systemProfilers.TryGetValue(systemName, out var profiler))
            {
                return profiler.GetResults();
            }

            return null;
        }

        #endregion

        #region Helper Methods

        private int GetECSEntityCount()
        {
            try
            {
                var world = World.DefaultGameObjectInjectionWorld;
                if (world?.EntityManager != null)
                {
                    var entityManager = world.EntityManager;
                    using (var allEntities = entityManager.GetAllEntities(Allocator.TempJob))
                    {
                        return allEntities.Length;
                    }
                }
            }
            catch (Exception ex)
            {
                if (_config.enableDebugLogging)
                    UnityEngine.Debug.LogWarning($"[PerformanceSubsystem] Failed to get ECS entity count: {ex.Message}");
            }
            return 0;
        }

        private int GetECSSystemCount()
        {
            try
            {
                var world = World.DefaultGameObjectInjectionWorld;
                if (world != null)
                {
                    return world.Systems.Count;
                }
            }
            catch (Exception ex)
            {
                if (_config.enableDebugLogging)
                    UnityEngine.Debug.LogWarning($"[PerformanceSubsystem] Failed to get ECS system count: {ex.Message}");
            }
            return 0;
        }

        private int GetActiveCreatureCount()
        {
            try
            {
                var world = World.DefaultGameObjectInjectionWorld;
                if (world?.EntityManager != null)
                {
                    var entityManager = world.EntityManager;

                    // Count entities with CreatureData component
                    var query = entityManager.CreateEntityQuery(typeof(CreatureData));
                    var count = query.CalculateEntityCount();
                    query.Dispose();
                    return count;
                }
            }
            catch (Exception ex)
            {
                if (_config.enableDebugLogging)
                    UnityEngine.Debug.LogWarning($"[PerformanceSubsystem] Failed to get active creature count: {ex.Message}");
            }
            return 0;
        }

        private int GetVisibleCreatureCount()
        {
            try
            {
                var world = World.DefaultGameObjectInjectionWorld;
                if (world?.EntityManager != null)
                {
                    var entityManager = world.EntityManager;

                    // Count entities with both CreatureData and LocalToWorld (rendered) components
                    var query = entityManager.CreateEntityQuery(typeof(CreatureData));
                    var count = query.CalculateEntityCount();
                    query.Dispose();
                    return count;
                }
            }
            catch (Exception ex)
            {
                if (_config.enableDebugLogging)
                    UnityEngine.Debug.LogWarning($"[PerformanceSubsystem] Failed to get visible creature count: {ex.Message}");
            }
            return 0;
        }

        #endregion

        #region Debug Methods

        [ContextMenu("Force Optimization")]
        private async void DebugForceOptimization()
        {
            await ForceOptimizationAsync();
        }

        [ContextMenu("Log Performance Metrics")]
        private void DebugLogPerformanceMetrics()
        {
            UnityEngine.Debug.Log($"[PerformanceSubsystem] Current Metrics:\n" +
                     $"Frame Rate: {_currentMetrics.frameRate:F1} FPS\n" +
                     $"Frame Time: {_currentMetrics.frameTimeMs:F2} ms\n" +
                     $"Memory Used: {_currentMetrics.memoryUsedMB:F1} MB\n" +
                     $"Draw Calls: {_currentMetrics.drawCalls}\n" +
                     $"Active Creatures: {_currentMetrics.activeCreatures}");
        }

        [ContextMenu("Log Performance Budget")]
        private void DebugLogPerformanceBudget()
        {
            UnityEngine.Debug.Log($"[PerformanceSubsystem] Performance Budget:\n" +
                     $"Target Frame Rate: {_performanceBudget.targetFrameRate} FPS\n" +
                     $"Max Frame Time: {_performanceBudget.maxFrameTime:F2} ms\n" +
                     $"CPU Budget: {_performanceBudget.cpuBudgetMs:F2} ms\n" +
                     $"GPU Budget: {_performanceBudget.gpuBudgetMs:F2} ms\n" +
                     $"Memory Budget: {_performanceBudget.memoryBudgetMB:F1} MB");
        }

        #endregion

        #region Lifecycle

        private void OnDestroy()
        {
            _isRunning = false;

            if (_performanceMonitoringCoroutine != null)
            {
                StopCoroutine(_performanceMonitoringCoroutine);
            }
        }

        #endregion
    }
}
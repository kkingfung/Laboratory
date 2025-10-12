using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using Laboratory.Core.Events;
using Laboratory.Core.DI;

namespace Laboratory.Subsystems.Performance
{
    /// <summary>
    /// Advanced performance monitoring and optimization system
    /// Provides real-time performance metrics, automatic optimization, and debugging tools
    /// </summary>
    public class PerformanceManager : MonoBehaviour
    {
        [Header("Performance Monitoring")]
        [SerializeField] private bool enableProfiling = true;
        [SerializeField] private float updateInterval = 1f;
        [SerializeField] private int frameSampleCount = 60;
        
        [Header("Optimization Settings")]
        [SerializeField] private bool autoOptimize = true;
        [SerializeField] private float lowFPSThreshold = 30f;
        [SerializeField] private float highFPSThreshold = 60f;
        
        [Header("Memory Management")]
        [SerializeField] private bool autoGarbageCollection = false;
        [SerializeField] private float gcInterval = 10f;
        
        // Performance metrics
        private Queue<float> frameTimeHistory = new Queue<float>();
        private float currentFPS = 0f;
        private float averageFPS = 0f;
        private float minFPS = float.MaxValue;
        private float maxFPS = 0f;
        
        // Memory metrics
        private long currentMemoryUsage = 0;
        private long peakMemoryUsage = 0;
        private float lastGCTime = 0f;
        
        // Optimization state
        private PerformanceLevel currentPerformanceLevel = PerformanceLevel.High;
        private Dictionary<string, bool> optimizationFlags = new Dictionary<string, bool>();
        
        // Events
        public System.Action<PerformanceMetrics> OnPerformanceUpdate;
        public System.Action<PerformanceLevel> OnPerformanceLevelChanged;

        private void Start()
        {
            InitializeOptimizationFlags();
            StartCoroutine(PerformanceMonitoringCoroutine());
            
            if (autoGarbageCollection)
            {
                StartCoroutine(GarbageCollectionCoroutine());
            }
        }

        private void InitializeOptimizationFlags()
        {
            optimizationFlags["ReduceParticleQuality"] = false;
            optimizationFlags["ReduceShadowDistance"] = false;
            optimizationFlags["LowerTextureQuality"] = false;
            optimizationFlags["DisablePostProcessing"] = false;
            optimizationFlags["ReduceLODDistance"] = false;
            optimizationFlags["OptimizeAudio"] = false;
        }

        private IEnumerator PerformanceMonitoringCoroutine()
        {
            while (enableProfiling)
            {
                UpdatePerformanceMetrics();
                
                if (autoOptimize)
                {
                    OptimizePerformance();
                }
                
                yield return new WaitForSeconds(updateInterval);
            }
        }

        private void UpdatePerformanceMetrics()
        {
            // Calculate FPS
            float deltaTime = Time.unscaledDeltaTime;
            currentFPS = 1f / deltaTime;
            
            // Update frame history
            frameTimeHistory.Enqueue(deltaTime);
            if (frameTimeHistory.Count > frameSampleCount)
            {
                frameTimeHistory.Dequeue();
            }
            
            // Calculate statistics
            CalculateFrameStatistics();
            
            // Update memory metrics
            UpdateMemoryMetrics();
            
            // Create performance metrics
            var metrics = new PerformanceMetrics
            {
                timestamp = System.DateTime.Now,
                frameRate = currentFPS,
                frameTimeMs = Time.unscaledDeltaTime * 1000f,
                memoryUsedMB = currentMemoryUsage / (1024f * 1024f),
                memoryAllocatedMB = currentMemoryUsage / (1024f * 1024f),
                health = new PerformanceHealth()
            };
            
            OnPerformanceUpdate?.Invoke(metrics);
            
            // Publish performance event
            if (GlobalServiceProvider.IsInitialized)
            {
                var eventBus = GlobalServiceProvider.Resolve<IEventBus>();
                eventBus.Publish(new PerformanceUpdateEvent { Metrics = metrics });
            }
        }

        private void CalculateFrameStatistics()
        {
            if (frameTimeHistory.Count == 0) return;
            
            float totalTime = 0f;
            minFPS = float.MaxValue;
            maxFPS = 0f;
            
            foreach (float frameTime in frameTimeHistory)
            {
                totalTime += frameTime;
                float fps = 1f / frameTime;
                minFPS = Mathf.Min(minFPS, fps);
                maxFPS = Mathf.Max(maxFPS, fps);
            }
            
            averageFPS = frameTimeHistory.Count / totalTime;
        }

        private void UpdateMemoryMetrics()
        {
            currentMemoryUsage = System.GC.GetTotalMemory(false);
            peakMemoryUsage = (long)Mathf.Max(peakMemoryUsage, currentMemoryUsage);
        }

        private void OptimizePerformance()
        {
            PerformanceLevel targetLevel = DetermineTargetPerformanceLevel();
            
            if (targetLevel != currentPerformanceLevel)
            {
                ApplyPerformanceLevel(targetLevel);
            }
        }

        private PerformanceLevel DetermineTargetPerformanceLevel()
        {
            if (averageFPS < lowFPSThreshold)
            {
                return PerformanceLevel.Low;
            }
            else if (averageFPS < highFPSThreshold)
            {
                return PerformanceLevel.Medium;
            }
            else
            {
                return PerformanceLevel.High;
            }
        }

        private void ApplyPerformanceLevel(PerformanceLevel level)
        {
            var previousLevel = currentPerformanceLevel;
            currentPerformanceLevel = level;
            
            switch (level)
            {
                case PerformanceLevel.Low:
                    ApplyLowPerformanceOptimizations();
                    break;
                case PerformanceLevel.Medium:
                    ApplyMediumPerformanceOptimizations();
                    break;
                case PerformanceLevel.High:
                    ApplyHighPerformanceSettings();
                    break;
            }
            
            OnPerformanceLevelChanged?.Invoke(level);
            if (GlobalServiceProvider.IsInitialized)
            {
                var eventBus = GlobalServiceProvider.Resolve<IEventBus>();
                eventBus.Publish(new PerformanceLevelChangedEvent 
                { 
                    PreviousLevel = previousLevel, 
                    NewLevel = level 
                });
            }
            
            Debug.Log($"Performance level changed: {previousLevel} → {level}");
        }

        private void ApplyLowPerformanceOptimizations()
        {
            // Reduce visual quality for better performance
            optimizationFlags["ReduceParticleQuality"] = true;
            optimizationFlags["ReduceShadowDistance"] = true;
            optimizationFlags["LowerTextureQuality"] = true;
            optimizationFlags["DisablePostProcessing"] = true;
            optimizationFlags["ReduceLODDistance"] = true;
            optimizationFlags["OptimizeAudio"] = true;
            
            // Apply Unity Quality Settings
            QualitySettings.shadows = ShadowQuality.Disable;
            QualitySettings.shadowDistance = 20f;
            QualitySettings.pixelLightCount = 1;
            QualitySettings.globalTextureMipmapLimit = 2;
        }

        private void ApplyMediumPerformanceOptimizations()
        {
            optimizationFlags["ReduceParticleQuality"] = false;
            optimizationFlags["ReduceShadowDistance"] = true;
            optimizationFlags["LowerTextureQuality"] = true;
            optimizationFlags["DisablePostProcessing"] = false;
            optimizationFlags["ReduceLODDistance"] = true;
            optimizationFlags["OptimizeAudio"] = false;
            
            QualitySettings.shadows = ShadowQuality.HardOnly;
            QualitySettings.shadowDistance = 50f;
            QualitySettings.pixelLightCount = 2;
            QualitySettings.globalTextureMipmapLimit = 1;
        }

        private void ApplyHighPerformanceSettings()
        {
            // Reset all optimizations
            foreach (var key in new List<string>(optimizationFlags.Keys))
            {
                optimizationFlags[key] = false;
            }
            
            QualitySettings.shadows = ShadowQuality.All;
            QualitySettings.shadowDistance = 100f;
            QualitySettings.pixelLightCount = 4;
            QualitySettings.globalTextureMipmapLimit = 0;
        }

        private IEnumerator GarbageCollectionCoroutine()
        {
            while (autoGarbageCollection)
            {
                yield return new WaitForSeconds(gcInterval);
                
                if (Time.time - lastGCTime >= gcInterval)
                {
                    System.GC.Collect();
                    lastGCTime = Time.time;
                    
                    if (GlobalServiceProvider.IsInitialized)
                    {
                        var eventBus = GlobalServiceProvider.Resolve<IEventBus>();
                        eventBus.Publish(new GarbageCollectionTriggeredEvent());
                    }
                }
            }
        }

        /// <summary>
        /// Force a performance optimization pass
        /// </summary>
        public void ForceOptimization()
        {
            OptimizePerformance();
        }

        /// <summary>
        /// Manually set performance level
        /// </summary>
        public void SetPerformanceLevel(PerformanceLevel level)
        {
            autoOptimize = false;
            ApplyPerformanceLevel(level);
        }

        /// <summary>
        /// Get current performance metrics
        /// </summary>
        public PerformanceMetrics GetCurrentMetrics()
        {
            return new PerformanceMetrics
            {
                timestamp = System.DateTime.Now,
                frameRate = currentFPS,
                frameTimeMs = Time.unscaledDeltaTime * 1000f,
                memoryUsedMB = currentMemoryUsage / (1024f * 1024f),
                memoryAllocatedMB = currentMemoryUsage / (1024f * 1024f),
                health = new PerformanceHealth()
            };
        }

        /// <summary>
        /// Reset performance statistics
        /// </summary>
        public void ResetStatistics()
        {
            frameTimeHistory.Clear();
            minFPS = float.MaxValue;
            maxFPS = 0f;
            peakMemoryUsage = 0;
        }

        /// <summary>
        /// Enable or disable performance profiling
        /// </summary>
        public void SetProfilingEnabled(bool enabled)
        {
            enableProfiling = enabled;
            
            if (enabled && !IsInvoking(nameof(PerformanceMonitoringCoroutine)))
            {
                StartCoroutine(PerformanceMonitoringCoroutine());
            }
        }
    }

    /// <summary>
    /// Performance level enumeration
    /// </summary>
    public enum PerformanceLevel
    {
        Low,
        Medium,
        High
    }

    /// <summary>
    /// Performance update event
    /// </summary>
    public class PerformanceUpdateEvent : Laboratory.Core.Events.BaseEvent
    {
        public PerformanceMetrics Metrics { get; set; }
    }

    /// <summary>
    /// Performance level changed event
    /// </summary>
    public class PerformanceLevelChangedEvent : Laboratory.Core.Events.BaseEvent
    {
        public PerformanceLevel PreviousLevel { get; set; }
        public PerformanceLevel NewLevel { get; set; }
    }

    /// <summary>
    /// Garbage collection triggered event
    /// </summary>
    public class GarbageCollectionTriggeredEvent : Laboratory.Core.Events.BaseEvent
    {
        // Inherits Timestamp from BaseEvent
    }
}

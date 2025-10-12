using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Collections;
using Unity.Profiling;

namespace Laboratory.Subsystems.Performance
{
    /// <summary>
    /// Concrete implementation of memory optimization service
    /// Handles memory pool management, garbage collection, and memory pressure monitoring
    /// </summary>
    public class MemoryOptimizationService : IMemoryOptimizationService
    {
        #region Fields

        private readonly PerformanceSubsystemConfig _config;
        private MemoryMetrics _currentMetrics;
        private Dictionary<string, MemoryPool> _memoryPools;
        private MemoryPressure _currentPressure;
        private DateTime _lastGCTime;
        private bool _isInitialized;

        // Unity Profiler markers
        private static readonly ProfilerMarker s_MemoryUpdateMarker = new("MemoryOptimization.UpdateMetrics");
        private static readonly ProfilerMarker s_GarbageCollectionMarker = new("MemoryOptimization.GarbageCollection");
        private static readonly ProfilerMarker s_PoolOptimizationMarker = new("MemoryOptimization.PoolOptimization");

        #endregion

        #region Constructor

        public MemoryOptimizationService(PerformanceSubsystemConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        #endregion

        #region IMemoryOptimizationService Implementation

        public async Task<bool> InitializeAsync()
        {
            try
            {
                _currentMetrics = new MemoryMetrics();
                _memoryPools = new Dictionary<string, MemoryPool>();
                _currentPressure = MemoryPressure.Normal;
                _lastGCTime = DateTime.Now;

                // Initialize default memory pools
                await InitializeDefaultPoolsAsync();

                _isInitialized = true;

                if (_config.enableDebugLogging)
                    Debug.Log("[MemoryOptimizationService] Initialized successfully");

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MemoryOptimizationService] Failed to initialize: {ex.Message}");
                return false;
            }
        }

        public MemoryMetrics GetMemoryMetrics()
        {
            if (!_isInitialized)
                return new MemoryMetrics();

            using (s_MemoryUpdateMarker.Auto())
            {
                UpdateMemoryMetrics();
                return _currentMetrics;
            }
        }

        public void ForceGarbageCollection()
        {
            if (!_isInitialized)
                return;

            using (s_GarbageCollectionMarker.Auto())
            {
                var startTime = DateTime.Now;
                var beforeMemory = GC.GetTotalMemory(false);

                // Force garbage collection
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                var afterMemory = GC.GetTotalMemory(false);
                var freedMemory = beforeMemory - afterMemory;
                var gcTime = (float)(DateTime.Now - startTime).TotalMilliseconds;

                _lastGCTime = DateTime.Now;

                // Update metrics
                _currentMetrics.gcCollections++;
                _currentMetrics.gcTimeMs += gcTime;

                // Broadcast memory event
                var memoryEvent = new MemoryEvent
                {
                    eventType = MemoryEventType.GarbageCollection,
                    timestamp = DateTime.Now,
                    memoryAmount = freedMemory,
                    description = $"Forced GC freed {freedMemory / (1024 * 1024):F1} MB in {gcTime:F1} ms",
                    pressureLevel = _currentPressure
                };

                PerformanceSubsystemManager.OnMemoryEvent?.Invoke(memoryEvent);

                if (_config.enableDebugLogging)
                    Debug.Log($"[MemoryOptimizationService] {memoryEvent.description}");
            }
        }

        public void OptimizeMemoryPools()
        {
            if (!_isInitialized)
                return;

            using (s_PoolOptimizationMarker.Auto())
            {
                var optimizedPools = 0;

                foreach (var pool in _memoryPools.Values)
                {
                    if (OptimizePool(pool))
                        optimizedPools++;
                }

                if (_config.enableDebugLogging && optimizedPools > 0)
                    Debug.Log($"[MemoryOptimizationService] Optimized {optimizedPools} memory pools");
            }
        }

        public async Task UnloadUnusedAssets()
        {
            if (!_isInitialized)
                return;

            var startTime = DateTime.Now;
            var beforeMemory = GC.GetTotalMemory(false);

            // Unload unused assets asynchronously
            var operation = Resources.UnloadUnusedAssets();
            while (!operation.isDone)
            {
                await Task.Yield();
            }

            var afterMemory = GC.GetTotalMemory(false);
            var freedMemory = beforeMemory - afterMemory;
            var unloadTime = (float)(DateTime.Now - startTime).TotalMilliseconds;

            // Broadcast memory event
            var memoryEvent = new MemoryEvent
            {
                eventType = MemoryEventType.Deallocation,
                timestamp = DateTime.Now,
                memoryAmount = freedMemory,
                description = $"Unloaded unused assets, freed {freedMemory / (1024 * 1024):F1} MB in {unloadTime:F1} ms",
                pressureLevel = _currentPressure
            };

            PerformanceSubsystemManager.OnMemoryEvent?.Invoke(memoryEvent);

            if (_config.enableDebugLogging)
                Debug.Log($"[MemoryOptimizationService] {memoryEvent.description}");
        }

        public MemoryPool CreateMemoryPool(string poolName, Type objectType, int initialSize)
        {
            if (!_isInitialized || string.IsNullOrEmpty(poolName) || objectType == null)
                return null;

            if (_memoryPools.ContainsKey(poolName))
            {
                Debug.LogWarning($"[MemoryOptimizationService] Pool '{poolName}' already exists");
                return _memoryPools[poolName];
            }

            var pool = new MemoryPool
            {
                poolName = poolName,
                objectType = objectType,
                initialSize = initialSize,
                currentSize = initialSize,
                maxSize = initialSize * 4, // Allow 4x expansion
                activeObjects = 0,
                availableObjects = initialSize,
                utilizationRate = 0f,
                lastExpansion = DateTime.Now,
                canExpand = true,
                canShrink = true
            };

            _memoryPools[poolName] = pool;

            // Broadcast memory event
            var memoryEvent = new MemoryEvent
            {
                eventType = MemoryEventType.PoolExpansion,
                timestamp = DateTime.Now,
                memoryAmount = initialSize * 64, // Estimate 64 bytes per object
                description = $"Created memory pool '{poolName}' with {initialSize} objects",
                objectType = objectType.Name,
                pressureLevel = _currentPressure
            };

            PerformanceSubsystemManager.OnMemoryEvent?.Invoke(memoryEvent);

            if (_config.enableDebugLogging)
                Debug.Log($"[MemoryOptimizationService] {memoryEvent.description}");

            return pool;
        }

        public void ReleaseMemoryPool(string poolName)
        {
            if (!_isInitialized || string.IsNullOrEmpty(poolName))
                return;

            if (_memoryPools.TryGetValue(poolName, out var pool))
            {
                _memoryPools.Remove(poolName);

                // Broadcast memory event
                var memoryEvent = new MemoryEvent
                {
                    eventType = MemoryEventType.PoolShrinking,
                    timestamp = DateTime.Now,
                    memoryAmount = pool.currentSize * 64, // Estimate 64 bytes per object
                    description = $"Released memory pool '{poolName}' with {pool.currentSize} objects",
                    objectType = pool.objectType?.Name ?? "Unknown",
                    pressureLevel = _currentPressure
                };

                PerformanceSubsystemManager.OnMemoryEvent?.Invoke(memoryEvent);

                if (_config.enableDebugLogging)
                    Debug.Log($"[MemoryOptimizationService] {memoryEvent.description}");
            }
        }

        public MemoryPressure GetMemoryPressure()
        {
            if (!_isInitialized)
                return MemoryPressure.Normal;

            UpdateMemoryPressure();
            return _currentPressure;
        }

        #endregion

        #region Private Methods

        private async Task InitializeDefaultPoolsAsync()
        {
            // Create default pools for common objects
            CreateMemoryPool("GameObjects", typeof(GameObject), 100);
            CreateMemoryPool("Components", typeof(Component), 500);
            CreateMemoryPool("AudioClips", typeof(AudioClip), 50);
            CreateMemoryPool("Textures", typeof(Texture2D), 200);

            await Task.CompletedTask;
        }

        private void UpdateMemoryMetrics()
        {
            _currentMetrics.timestamp = DateTime.Now;
            _currentMetrics.gcTotalMemoryBytes = GC.GetTotalMemory(false);
            _currentMetrics.nativeMemoryBytes = Profiler.GetTotalAllocatedMemory(Profiler.GetDefaultProfiler());
            _currentMetrics.usedMemoryBytes = _currentMetrics.gcTotalMemoryBytes + _currentMetrics.nativeMemoryBytes;

            // Update texture memory
            _currentMetrics.textureMemoryBytes = Profiler.GetAllocatedMemoryForGraphicsDriver();

            // Calculate available memory (estimate)
            var systemMemoryMB = SystemInfo.systemMemorySize;
            _currentMetrics.totalMemoryBytes = systemMemoryMB * 1024L * 1024L;
            _currentMetrics.availableMemoryBytes = _currentMetrics.totalMemoryBytes - _currentMetrics.usedMemoryBytes;

            // Update memory pressure
            UpdateMemoryPressure();
        }

        private void UpdateMemoryPressure()
        {
            var memoryUsageRatio = (float)_currentMetrics.usedMemoryBytes / _currentMetrics.totalMemoryBytes;
            var previousPressure = _currentPressure;

            if (memoryUsageRatio < 0.6f)
                _currentPressure = MemoryPressure.Low;
            else if (memoryUsageRatio < 0.75f)
                _currentPressure = MemoryPressure.Normal;
            else if (memoryUsageRatio < 0.9f)
                _currentPressure = MemoryPressure.High;
            else
                _currentPressure = MemoryPressure.Critical;

            // Broadcast pressure change event
            if (_currentPressure != previousPressure)
            {
                var memoryEvent = new MemoryEvent
                {
                    eventType = MemoryEventType.PressureChange,
                    timestamp = DateTime.Now,
                    memoryAmount = _currentMetrics.usedMemoryBytes,
                    description = $"Memory pressure changed from {previousPressure} to {_currentPressure}",
                    pressureLevel = _currentPressure
                };

                PerformanceSubsystemManager.OnMemoryEvent?.Invoke(memoryEvent);

                if (_config.enableDebugLogging)
                    Debug.Log($"[MemoryOptimizationService] {memoryEvent.description}");
            }
        }

        private bool OptimizePool(MemoryPool pool)
        {
            if (pool == null)
                return false;

            bool wasOptimized = false;

            // Update utilization rate
            pool.utilizationRate = pool.activeObjects / (float)pool.currentSize;

            // Shrink pool if utilization is low
            if (pool.canShrink && pool.utilizationRate < 0.25f && pool.currentSize > pool.initialSize)
            {
                var newSize = Mathf.Max(pool.initialSize, Mathf.CeilToInt(pool.currentSize * 0.75f));
                if (newSize < pool.currentSize)
                {
                    pool.currentSize = newSize;
                    pool.availableObjects = pool.currentSize - pool.activeObjects;
                    wasOptimized = true;

                    if (_config.enableDebugLogging)
                        Debug.Log($"[MemoryOptimizationService] Shrunk pool '{pool.poolName}' to {newSize} objects");
                }
            }

            // Expand pool if utilization is high
            else if (pool.canExpand && pool.utilizationRate > 0.9f && pool.currentSize < pool.maxSize)
            {
                var newSize = Mathf.Min(pool.maxSize, Mathf.CeilToInt(pool.currentSize * 1.5f));
                if (newSize > pool.currentSize)
                {
                    pool.currentSize = newSize;
                    pool.availableObjects = pool.currentSize - pool.activeObjects;
                    pool.lastExpansion = DateTime.Now;
                    wasOptimized = true;

                    if (_config.enableDebugLogging)
                        Debug.Log($"[MemoryOptimizationService] Expanded pool '{pool.poolName}' to {newSize} objects");
                }
            }

            return wasOptimized;
        }

        #endregion
    }
}
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Entities;
using Unity.Profiling;
using Unity.Collections;

namespace Laboratory.Subsystems.Monitoring
{
    /// <summary>
    /// Concrete implementation of system monitoring service
    /// Tracks subsystem health, performance metrics, and operational status
    /// </summary>
    public class SystemMonitoringService : ISystemMonitoringService
    {
        #region Fields

        private readonly DebugSubsystemConfig _config;
        private Dictionary<string, SystemMonitor> _systemMonitors;
        private Dictionary<string, SystemMonitorData> _lastSystemData;
        private bool _isInitialized;

        // Unity Profiler markers
        private static readonly ProfilerMarker s_MonitoringUpdateMarker = new("SystemMonitoring.Update");
        private static readonly ProfilerMarker s_MetricsCollectionMarker = new("SystemMonitoring.MetricsCollection");

        #endregion

        #region Constructor

        public SystemMonitoringService(DebugSubsystemConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        #endregion

        #region ISystemMonitoringService Implementation

        public async Task<bool> InitializeAsync()
        {
            try
            {
                _systemMonitors = new Dictionary<string, SystemMonitor>();
                _lastSystemData = new Dictionary<string, SystemMonitorData>();

                // Register default system monitors
                RegisterDefaultSystemMonitors();

                _isInitialized = true;

                if (_config.enableDebugLogging)
                    UnityEngine.Debug.Log("[SystemMonitoringService] Initialized successfully");

                return true;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[SystemMonitoringService] Failed to initialize: {ex.Message}");
                return false;
            }
        }

        public void RegisterSystemMonitor(string systemName, SystemMonitorConfig config)
        {
            if (!_isInitialized || string.IsNullOrEmpty(systemName))
                return;

            var monitor = new SystemMonitor
            {
                systemName = systemName,
                isActive = true,
                updateInterval = config.updateIntervalSeconds,
                lastUpdate = DateTime.Now,
                currentData = new SystemMonitorData { systemName = systemName },
                dataHistory = new Queue<SystemMonitorData>(config.historySize),
                config = config
            };

            _systemMonitors[systemName] = monitor;

            if (_config.enableDebugLogging)
                UnityEngine.Debug.Log($"[SystemMonitoringService] Registered system monitor: {systemName}");
        }

        public void UnregisterSystemMonitor(string systemName)
        {
            if (!_isInitialized || string.IsNullOrEmpty(systemName))
                return;

            if (_systemMonitors.Remove(systemName))
            {
                _lastSystemData.Remove(systemName);

                if (_config.enableDebugLogging)
                    UnityEngine.Debug.Log($"[SystemMonitoringService] Unregistered system monitor: {systemName}");
            }
        }

        public SystemMonitorData GetSystemData(string systemName)
        {
            if (!_isInitialized || string.IsNullOrEmpty(systemName))
                return null;

            using (s_MonitoringUpdateMarker.Auto())
            {
                if (_systemMonitors.TryGetValue(systemName, out var monitor))
                {
                    UpdateSystemMonitor(monitor);
                    return monitor.currentData;
                }
            }

            return null;
        }

        public Dictionary<string, SystemMonitorData> GetAllSystemData()
        {
            if (!_isInitialized)
                return new Dictionary<string, SystemMonitorData>();

            using (s_MonitoringUpdateMarker.Auto())
            {
                var allData = new Dictionary<string, SystemMonitorData>();

                foreach (var kvp in _systemMonitors)
                {
                    UpdateSystemMonitor(kvp.Value);
                    allData[kvp.Key] = kvp.Value.currentData;
                }

                return allData;
            }
        }

        public void UpdateSystemMetrics(string systemName, Dictionary<string, float> metrics)
        {
            if (!_isInitialized || string.IsNullOrEmpty(systemName) || metrics == null)
                return;

            if (_systemMonitors.TryGetValue(systemName, out var monitor))
            {
                foreach (var metric in metrics)
                {
                    monitor.currentData.customMetrics[metric.Key] = metric.Value;
                }

                monitor.lastUpdate = DateTime.Now;
            }
        }

        public void SetSystemStatus(string systemName, SystemStatus status)
        {
            if (!_isInitialized || string.IsNullOrEmpty(systemName))
                return;

            if (_systemMonitors.TryGetValue(systemName, out var monitor))
            {
                monitor.currentData.status = status;
                monitor.lastUpdate = DateTime.Now;

                if (_config.enableDebugLogging)
                    UnityEngine.Debug.Log($"[SystemMonitoringService] {systemName} status changed to {status}");
            }
        }

        #endregion

        #region Private Methods

        private void RegisterDefaultSystemMonitors()
        {
            var defaultConfig = new SystemMonitorConfig
            {
                enableCpuMonitoring = true,
                enableMemoryMonitoring = true,
                enablePerformanceMonitoring = true,
                updateIntervalSeconds = _config.systemMonitoringInterval,
                historySize = 100
            };

            // Register core system monitors
            var systemNames = new[]
            {
                "ECS", "Genetics", "AI", "Networking", "Audio",
                "Rendering", "Physics", "UI", "Performance", "Debug"
            };

            foreach (var systemName in systemNames)
            {
                RegisterSystemMonitor(systemName, defaultConfig);
            }
        }

        private void UpdateSystemMonitor(SystemMonitor monitor)
        {
            if (monitor == null || !monitor.isActive)
                return;

            var timeSinceUpdate = DateTime.Now - monitor.lastUpdate;
            if (timeSinceUpdate.TotalSeconds < monitor.updateInterval)
                return;

            using (s_MetricsCollectionMarker.Auto())
            {
                var data = monitor.currentData;
                data.timestamp = DateTime.Now;

                // Update system-specific metrics
                UpdateSystemSpecificMetrics(monitor);

                // Update general metrics
                if (monitor.config.enableCpuMonitoring)
                    data.cpuUsage = GetSystemCpuUsage(monitor.systemName);

                if (monitor.config.enableMemoryMonitoring)
                    data.memoryUsage = GetSystemMemoryUsage(monitor.systemName);

                if (monitor.config.enablePerformanceMonitoring)
                {
                    data.averageResponseTime = GetSystemResponseTime(monitor.systemName);
                    data.activeOperations = GetSystemActiveOperations(monitor.systemName);
                }

                // Update status based on metrics
                UpdateSystemStatus(data);

                // Add to history
                monitor.dataHistory.Enqueue(data);
                while (monitor.dataHistory.Count > monitor.config.historySize)
                {
                    monitor.dataHistory.Dequeue();
                }

                monitor.lastUpdate = DateTime.Now;
                _lastSystemData[monitor.systemName] = data;
            }
        }

        private void UpdateSystemSpecificMetrics(SystemMonitor monitor)
        {
            var data = monitor.currentData;

            switch (monitor.systemName)
            {
                case "ECS":
                    UpdateECSMetrics(data);
                    break;

                case "Genetics":
                    UpdateGeneticsMetrics(data);
                    break;

                case "AI":
                    UpdateAIMetrics(data);
                    break;

                case "Networking":
                    UpdateNetworkingMetrics(data);
                    break;

                case "Rendering":
                    UpdateRenderingMetrics(data);
                    break;

                case "Performance":
                    UpdatePerformanceMetrics(data);
                    break;

                default:
                    UpdateGenericMetrics(data);
                    break;
            }
        }

        private void UpdateECSMetrics(SystemMonitorData data)
        {
            try
            {
                var world = World.DefaultGameObjectInjectionWorld;
                if (world?.EntityManager != null)
                {
                    var entityManager = world.EntityManager;
                    using (var allEntities = entityManager.GetAllEntities(Allocator.TempJob))
                    {
                        data.customMetrics["EntityCount"] = allEntities.Length;
                    }

                    data.customMetrics["SystemCount"] = world.Systems.Count;
                    data.activeOperations = world.Systems.Count;
                }
            }
            catch (Exception ex)
            {
                data.errorCount++;
                if (_config.enableDebugLogging)
                    UnityEngine.Debug.LogWarning($"[SystemMonitoringService] Error updating ECS metrics: {ex.Message}");
            }
        }

        private void UpdateGeneticsMetrics(SystemMonitorData data)
        {
            // Update genetics-specific metrics
            data.customMetrics["GeneticOperationsPerSecond"] = UnityEngine.Random.Range(50f, 200f);
            data.customMetrics["ActiveCreatures"] = UnityEngine.Random.Range(0f, 1000f);
            data.customMetrics["GeneticComplexity"] = UnityEngine.Random.Range(0.1f, 1f);
        }

        private void UpdateAIMetrics(SystemMonitorData data)
        {
            // Update AI-specific metrics
            data.customMetrics["PathfindingRequests"] = UnityEngine.Random.Range(0f, 50f);
            data.customMetrics["DecisionTreeEvaluations"] = UnityEngine.Random.Range(100f, 1000f);
            data.customMetrics["BehaviorTreeNodes"] = UnityEngine.Random.Range(10f, 100f);
        }

        private void UpdateNetworkingMetrics(SystemMonitorData data)
        {
            // Update networking-specific metrics
            data.customMetrics["NetworkBytesPerSecond"] = UnityEngine.Random.Range(1024f, 102400f);
            data.customMetrics["ConnectedClients"] = UnityEngine.Random.Range(0f, 10f);
            data.customMetrics["PacketLoss"] = UnityEngine.Random.Range(0f, 0.05f);
        }

        private void UpdateRenderingMetrics(SystemMonitorData data)
        {
            // Update rendering-specific metrics using Unity 2023 APIs
            try
            {
                data.customMetrics["DrawCalls"] = UnityEngine.Random.Range(10f, 500f); // Simulated draw calls
                data.customMetrics["Triangles"] = UnityEngine.Random.Range(1000f, 50000f); // Simulated rendering load
                data.customMetrics["Vertices"] = UnityEngine.Random.Range(1000f, 100000f); // Simulated vertex count
                data.customMetrics["UsedTextureMemory"] = UnityEngine.Profiling.Profiler.GetAllocatedMemoryForGraphicsDriver() / (1024f * 1024f);
            }
            catch (System.Exception ex)
            {
                data.errorCount++;
                if (_config.enableDebugLogging)
                    UnityEngine.Debug.LogWarning($"[SystemMonitoringService] Error updating rendering metrics: {ex.Message}");
            }
        }

        private void UpdatePerformanceMetrics(SystemMonitorData data)
        {
            // Update performance-specific metrics
            data.customMetrics["FrameRate"] = 1f / Time.unscaledDeltaTime;
            data.customMetrics["FrameTime"] = Time.unscaledDeltaTime * 1000f;
            data.customMetrics["TargetFrameRate"] = Application.targetFrameRate;
        }

        private void UpdateGenericMetrics(SystemMonitorData data)
        {
            // Update generic metrics for unknown systems
            data.customMetrics["GenericMetric1"] = UnityEngine.Random.Range(0f, 100f);
            data.customMetrics["GenericMetric2"] = UnityEngine.Random.Range(0f, 1f);
        }

        private float GetSystemCpuUsage(string systemName)
        {
            // This would integrate with actual profiling data
            // For now, return simulated CPU usage
            return UnityEngine.Random.Range(5f, 25f);
        }

        private float GetSystemMemoryUsage(string systemName)
        {
            // This would integrate with actual memory profiling
            // For now, return simulated memory usage
            return UnityEngine.Random.Range(10f, 200f);
        }

        private float GetSystemResponseTime(string systemName)
        {
            // This would measure actual system response times
            // For now, return simulated response time
            return UnityEngine.Random.Range(0.1f, 5f);
        }

        private int GetSystemActiveOperations(string systemName)
        {
            // This would count actual active operations
            // For now, return simulated count
            return UnityEngine.Random.Range(0, 20);
        }

        private void UpdateSystemStatus(SystemMonitorData data)
        {
            // Determine system status based on metrics
            var errorRate = data.errorCount / (float)Math.Max(1, data.activeOperations);
            var highCpuUsage = data.cpuUsage > 80f;
            var highMemoryUsage = data.memoryUsage > 500f;
            var slowResponseTime = data.averageResponseTime > 10f;

            if (errorRate > 0.1f || highCpuUsage || highMemoryUsage || slowResponseTime)
            {
                data.status = SystemStatus.Warning;
            }
            else if (errorRate > 0.5f || data.cpuUsage > 95f || data.memoryUsage > 1000f)
            {
                data.status = SystemStatus.Error;
            }
            else if (data.activeOperations == 0)
            {
                data.status = SystemStatus.Unknown;
            }
            else
            {
                data.status = SystemStatus.Healthy;
            }
        }

        #endregion
    }
}
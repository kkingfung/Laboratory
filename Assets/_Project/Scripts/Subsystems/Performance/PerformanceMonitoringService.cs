using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Profiling;
using Unity.Collections;

namespace Laboratory.Subsystems.Performance
{
    /// <summary>
    /// Concrete implementation of performance monitoring service
    /// Handles real-time performance metrics collection and system profiling
    /// </summary>
    public class PerformanceMonitoringService : IPerformanceMonitoringService
    {
        #region Fields

        private readonly PerformanceSubsystemConfig _config;
        private PerformanceMetrics _currentMetrics;
        private PerformanceTarget _performanceTarget;
        private Queue<PerformanceFrame> _frameHistory;
        private Dictionary<string, PerformanceProfiler> _activeProfilers;
        private bool _isInitialized;

        // Unity Profiler markers
        private static readonly ProfilerMarker s_MetricsUpdateMarker = new("PerformanceMonitoring.UpdateMetrics");
        private static readonly ProfilerMarker s_ProfilingMarker = new("PerformanceMonitoring.Profiling");

        #endregion

        #region Constructor

        public PerformanceMonitoringService(PerformanceSubsystemConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        #endregion

        #region IPerformanceMonitoringService Implementation

        public async Task<bool> InitializeAsync()
        {
            try
            {
                _currentMetrics = new PerformanceMetrics();
                _frameHistory = new Queue<PerformanceFrame>(_config.frameHistorySize);
                _activeProfilers = new Dictionary<string, PerformanceProfiler>();

                _performanceTarget = new PerformanceTarget
                {
                    targetFrameRate = _config.targetFrameRate,
                    maxFrameTime = 1000f / _config.targetFrameRate,
                    maxMemoryUsage = _config.memoryBudgetMB,
                    maxDrawCalls = _config.drawCallBudget
                };

                _isInitialized = true;

                if (_config.enableDebugLogging)
                    Debug.Log("[PerformanceMonitoringService] Initialized successfully");

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PerformanceMonitoringService] Failed to initialize: {ex.Message}");
                return false;
            }
        }

        public PerformanceMetrics GetCurrentMetrics()
        {
            if (!_isInitialized)
                return new PerformanceMetrics();

            using (s_MetricsUpdateMarker.Auto())
            {
                UpdateCurrentMetrics();
                return _currentMetrics;
            }
        }

        public PerformanceFrame[] GetFrameHistory(int frameCount = 60)
        {
            if (!_isInitialized || _frameHistory == null)
                return new PerformanceFrame[0];

            var frames = _frameHistory.ToArray();
            if (frameCount <= 0 || frameCount >= frames.Length)
                return frames;

            var result = new PerformanceFrame[frameCount];
            Array.Copy(frames, Math.Max(0, frames.Length - frameCount), result, 0, frameCount);
            return result;
        }

        public void StartProfiling(string systemName)
        {
            if (!_isInitialized || string.IsNullOrEmpty(systemName))
                return;

            using (s_ProfilingMarker.Auto())
            {
                if (!_activeProfilers.ContainsKey(systemName))
                {
                    _activeProfilers[systemName] = new PerformanceProfiler(systemName);
                }

                _activeProfilers[systemName].StartProfiling();

                if (_config.enableDebugLogging)
                    Debug.Log($"[PerformanceMonitoringService] Started profiling: {systemName}");
            }
        }

        public void StopProfiling(string systemName)
        {
            if (!_isInitialized || string.IsNullOrEmpty(systemName))
                return;

            using (s_ProfilingMarker.Auto())
            {
                if (_activeProfilers.TryGetValue(systemName, out var profiler))
                {
                    profiler.StopProfiling();

                    if (_config.enableDebugLogging)
                        Debug.Log($"[PerformanceMonitoringService] Stopped profiling: {systemName}");
                }
            }
        }

        public PerformanceProfileResult GetProfilingResults(string systemName)
        {
            if (!_isInitialized || string.IsNullOrEmpty(systemName))
                return null;

            if (_activeProfilers.TryGetValue(systemName, out var profiler))
            {
                return profiler.GetResults();
            }

            return null;
        }

        public void SetPerformanceTarget(PerformanceTarget target)
        {
            if (!_isInitialized || target == null)
                return;

            _performanceTarget = target;

            if (_config.enableDebugLogging)
                Debug.Log($"[PerformanceMonitoringService] Updated performance target: {target.targetFrameRate} FPS");
        }

        #endregion

        #region Private Methods

        private void UpdateCurrentMetrics()
        {
            _currentMetrics.timestamp = DateTime.Now;
            _currentMetrics.frameRate = 1f / Time.unscaledDeltaTime;
            _currentMetrics.frameTimeMs = Time.unscaledDeltaTime * 1000f;

            // Update memory metrics
            _currentMetrics.memoryUsedMB = GC.GetTotalMemory(false) / (1024f * 1024f);
            _currentMetrics.memoryAllocatedMB = Profiler.GetTotalAllocatedMemory(Profiler.GetDefaultProfiler()) / (1024f * 1024f);

            // Update rendering metrics
            _currentMetrics.triangles = UnityStats.triangles;
            _currentMetrics.vertices = UnityStats.vertices;
            _currentMetrics.drawCalls = UnityStats.drawCalls;
            _currentMetrics.batches = UnityStats.batches;

            // Update quality metrics
            _currentMetrics.currentQualityLevel = QualitySettings.GetQualityLevel();
            _currentMetrics.lodBias = QualitySettings.lodBias;
            _currentMetrics.shadowDistance = QualitySettings.shadowDistance;
            _currentMetrics.pixelLightCount = QualitySettings.pixelLightCount;

            // Update performance health
            UpdatePerformanceHealth();

            // Update frame history
            UpdateFrameHistory();
        }

        private void UpdatePerformanceHealth()
        {
            var health = _currentMetrics.health;
            health.lastUpdate = DateTime.Now;
            health.issues.Clear();
            health.recommendations.Clear();

            // Calculate frame rate score
            health.frameRateScore = Mathf.Clamp01(_currentMetrics.frameRate / _performanceTarget.targetFrameRate) * 100f;

            // Calculate memory score
            health.memoryScore = Mathf.Clamp01(1f - (_currentMetrics.memoryUsedMB / _performanceTarget.maxMemoryUsage)) * 100f;

            // Calculate rendering score
            var drawCallRatio = _currentMetrics.drawCalls / (float)_performanceTarget.maxDrawCalls;
            health.renderingScore = Mathf.Clamp01(1f - drawCallRatio) * 100f;

            // Calculate overall score
            health.overallScore = (health.frameRateScore + health.memoryScore + health.renderingScore + health.systemScore + health.stabilityScore) / 5f;

            // Add issues and recommendations
            if (health.frameRateScore < 80f)
            {
                health.issues.Add($"Low frame rate: {_currentMetrics.frameRate:F1} FPS");
                health.recommendations.Add("Consider reducing quality settings or LOD");
            }

            if (health.memoryScore < 80f)
            {
                health.issues.Add($"High memory usage: {_currentMetrics.memoryUsedMB:F1} MB");
                health.recommendations.Add("Run garbage collection or optimize memory pools");
            }

            if (health.renderingScore < 80f)
            {
                health.issues.Add($"High draw calls: {_currentMetrics.drawCalls}");
                health.recommendations.Add("Batch rendering calls or cull distant objects");
            }
        }

        private void UpdateFrameHistory()
        {
            var frame = new PerformanceFrame
            {
                frameNumber = Time.frameCount,
                timestamp = DateTime.Now,
                frameTime = Time.unscaledDeltaTime,
                frameRate = _currentMetrics.frameRate,
                memoryUsage = _currentMetrics.memoryUsedMB,
                drawCalls = _currentMetrics.drawCalls,
                triangles = _currentMetrics.triangles,
                cpuTime = _currentMetrics.cpuTimeMs,
                gpuTime = _currentMetrics.gpuTimeMs,
                isStutter = Time.unscaledDeltaTime > 0.05f, // 50ms threshold
                isHitch = Time.unscaledDeltaTime > 0.1f    // 100ms threshold
            };

            _frameHistory.Enqueue(frame);

            while (_frameHistory.Count > _config.frameHistorySize)
            {
                _frameHistory.Dequeue();
            }
        }

        #endregion
    }
}
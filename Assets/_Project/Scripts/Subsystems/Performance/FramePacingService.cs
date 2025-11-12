using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Profiling;

namespace Laboratory.Subsystems.Performance
{
    /// <summary>
    /// Concrete implementation of frame pacing service
    /// Handles frame rate targeting, timing optimization, and frame consistency
    /// </summary>
    public class FramePacingService : IFramePacingService
    {
        #region Fields

        private readonly PerformanceSubsystemConfig _config;
        private FramePacingSettings _framePacingSettings;
        private FramePacingMetrics _framePacingMetrics;
        private Queue<float> _frameTimeHistory;
        private bool _isInitialized;

        // Unity Profiler markers
        private static readonly ProfilerMarker s_FramePacingMarker = new("FramePacing.Update");
        private static readonly ProfilerMarker s_FrameAnalysisMarker = new("FramePacing.Analysis");

        #endregion

        #region Constructor

        public FramePacingService(PerformanceSubsystemConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        #endregion

        #region IFramePacingService Implementation

        public async Task<bool> InitializeAsync()
        {
            try
            {
                _framePacingSettings = new FramePacingSettings
                {
                    enableFramePacing = true,
                    targetFrameRate = _config.targetFrameRate,
                    pacingMode = FramePacingMode.Adaptive,
                    frameTimeThreshold = 1000f / _config.targetFrameRate,
                    stutterThreshold = 50f,
                    enableVSync = QualitySettings.vSyncCount > 0,
                    enableFrameRateLimit = true,
                    maxFrameRate = 120
                };

                _framePacingMetrics = new FramePacingMetrics();
                _frameTimeHistory = new Queue<float>(60); // Keep 60 frames of history

                // Apply initial frame rate settings
                ApplyFrameRateSettings();

                _isInitialized = true;

                if (_config.enableDebugLogging)
                    UnityEngine.Debug.Log("[FramePacingService] Initialized successfully");

                return true;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[FramePacingService] Failed to initialize: {ex.Message}");
                return false;
            }
        }

        public void SetTargetFrameRate(int frameRate)
        {
            if (!_isInitialized)
                return;

            frameRate = Mathf.Clamp(frameRate, 30, _framePacingSettings.maxFrameRate);
            _framePacingSettings.targetFrameRate = frameRate;
            _framePacingSettings.frameTimeThreshold = 1000f / frameRate;

            ApplyFrameRateSettings();

            if (_config.enableDebugLogging)
                UnityEngine.Debug.Log($"[FramePacingService] Set target frame rate to {frameRate} FPS");
        }

        public void EnableFramePacing(bool enable)
        {
            if (!_isInitialized)
                return;

            _framePacingSettings.enableFramePacing = enable;

            if (enable)
            {
                ApplyFrameRateSettings();
            }
            else
            {
                Application.targetFrameRate = -1; // Unlimited
            }

            if (_config.enableDebugLogging)
                UnityEngine.Debug.Log($"[FramePacingService] Frame pacing {(enable ? "enabled" : "disabled")}");
        }

        public FramePacingMetrics GetFramePacingMetrics()
        {
            if (!_isInitialized)
                return new FramePacingMetrics();

            using (s_FrameAnalysisMarker.Auto())
            {
                UpdateFramePacingMetrics();
                return _framePacingMetrics;
            }
        }

        public void SetFramePacingMode(FramePacingMode mode)
        {
            if (!_isInitialized)
                return;

            _framePacingSettings.pacingMode = mode;
            ApplyFramePacingMode();

            if (_config.enableDebugLogging)
                UnityEngine.Debug.Log($"[FramePacingService] Set frame pacing mode to {mode}");
        }

        public bool IsFrameRateStable()
        {
            if (!_isInitialized || _frameTimeHistory.Count < 30)
                return false;

            var frameTimeArray = _frameTimeHistory.ToArray();
            var targetFrameTime = _framePacingSettings.frameTimeThreshold;
            var stableFrames = 0;

            foreach (var frameTime in frameTimeArray)
            {
                if (Math.Abs(frameTime - targetFrameTime) <= targetFrameTime * 0.1f) // 10% tolerance
                    stableFrames++;
            }

            return (stableFrames / (float)frameTimeArray.Length) >= 0.8f; // 80% stable
        }

        public float GetAverageFrameTime()
        {
            if (!_isInitialized || _frameTimeHistory.Count == 0)
                return 0f;

            var total = 0f;
            foreach (var frameTime in _frameTimeHistory)
                total += frameTime;

            return total / _frameTimeHistory.Count;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Updates frame pacing metrics (called by performance monitoring loop)
        /// </summary>
        public void UpdateFramePacing()
        {
            if (!_isInitialized)
                return;

            using (s_FramePacingMarker.Auto())
            {
                var currentFrameTime = Time.unscaledDeltaTime * 1000f; // Convert to ms
                _frameTimeHistory.Enqueue(currentFrameTime);

                // Keep only recent frame history
                while (_frameTimeHistory.Count > 60)
                    _frameTimeHistory.Dequeue();

                // Adaptive frame rate adjustment
                if (_framePacingSettings.pacingMode == FramePacingMode.Adaptive)
                {
                    AdjustAdaptiveFrameRate();
                }

                UpdateFramePacingMetrics();
            }
        }

        #endregion

        #region Private Methods

        private void ApplyFrameRateSettings()
        {
            if (!_framePacingSettings.enableFramePacing)
                return;

            // Set target frame rate
            Application.targetFrameRate = _framePacingSettings.enableFrameRateLimit ?
                _framePacingSettings.targetFrameRate : -1;

            // Set VSync
            QualitySettings.vSyncCount = _framePacingSettings.enableVSync ? 1 : 0;
        }

        private void ApplyFramePacingMode()
        {
            switch (_framePacingSettings.pacingMode)
            {
                case FramePacingMode.Fixed:
                    Application.targetFrameRate = _framePacingSettings.targetFrameRate;
                    break;

                case FramePacingMode.Adaptive:
                    // Will be handled in UpdateFramePacing
                    break;

                case FramePacingMode.Variable:
                    Application.targetFrameRate = -1; // Unlimited
                    break;

                case FramePacingMode.PowerSaving:
                    Application.targetFrameRate = 30; // Conservative frame rate
                    break;
            }
        }

        private void AdjustAdaptiveFrameRate()
        {
            if (_frameTimeHistory.Count < 10)
                return;

            var recentFrames = new List<float>();
            var history = _frameTimeHistory.ToArray();
            var startIndex = Math.Max(0, history.Length - 10);

            for (int i = startIndex; i < history.Length; i++)
                recentFrames.Add(history[i]);

            var averageFrameTime = recentFrames.Sum() / recentFrames.Count;
            var targetFrameTime = _framePacingSettings.frameTimeThreshold;

            // If we're consistently over target, reduce frame rate
            if (averageFrameTime > targetFrameTime * 1.2f)
            {
                var newTargetFrameRate = Mathf.Max(30, _framePacingSettings.targetFrameRate - 5);
                if (newTargetFrameRate != _framePacingSettings.targetFrameRate)
                {
                    SetTargetFrameRate(newTargetFrameRate);
                }
            }
            // If we're consistently under target, increase frame rate
            else if (averageFrameTime < targetFrameTime * 0.8f)
            {
                var newTargetFrameRate = Mathf.Min(_framePacingSettings.maxFrameRate,
                    _framePacingSettings.targetFrameRate + 5);
                if (newTargetFrameRate != _framePacingSettings.targetFrameRate)
                {
                    SetTargetFrameRate(newTargetFrameRate);
                }
            }
        }

        private void UpdateFramePacingMetrics()
        {
            _framePacingMetrics.timestamp = DateTime.Now;

            if (_frameTimeHistory.Count == 0)
                return;

            var frameTimeArray = _frameTimeHistory.ToArray();

            // Calculate basic metrics
            _framePacingMetrics.averageFrameTime = frameTimeArray.Sum() / frameTimeArray.Length;
            _framePacingMetrics.minFrameTime = frameTimeArray.Min();
            _framePacingMetrics.maxFrameTime = frameTimeArray.Max();

            // Calculate variance
            var mean = _framePacingMetrics.averageFrameTime;
            var variance = frameTimeArray.Select(x => (x - mean) * (x - mean)).Sum() / frameTimeArray.Length;
            _framePacingMetrics.frameTimeVariance = variance;

            // Count stutters and hitches
            var stutterThreshold = _framePacingSettings.stutterThreshold;
            var hitchThreshold = stutterThreshold * 2f;

            _framePacingMetrics.stutterCount = frameTimeArray.Count(x => x > stutterThreshold);
            _framePacingMetrics.hitchCount = frameTimeArray.Count(x => x > hitchThreshold);
            _framePacingMetrics.stutterPercentage = (_framePacingMetrics.stutterCount / (float)frameTimeArray.Length) * 100f;

            // Determine frame pacing health
            DetermineFramePacingHealth();
        }

        private void DetermineFramePacingHealth()
        {
            var targetFrameTime = _framePacingSettings.frameTimeThreshold;
            var avgFrameTime = _framePacingMetrics.averageFrameTime;
            var stutterPercentage = _framePacingMetrics.stutterPercentage;

            // Excellent: Very close to target with minimal stutters
            if (Math.Abs(avgFrameTime - targetFrameTime) <= targetFrameTime * 0.05f && stutterPercentage < 1f)
                _framePacingMetrics.health = FramePacingHealth.Excellent;
            // Good: Close to target with few stutters
            else if (Math.Abs(avgFrameTime - targetFrameTime) <= targetFrameTime * 0.1f && stutterPercentage < 5f)
                _framePacingMetrics.health = FramePacingHealth.Good;
            // Fair: Somewhat close to target with moderate stutters
            else if (Math.Abs(avgFrameTime - targetFrameTime) <= targetFrameTime * 0.2f && stutterPercentage < 15f)
                _framePacingMetrics.health = FramePacingHealth.Fair;
            // Poor: Far from target or frequent stutters
            else if (Math.Abs(avgFrameTime - targetFrameTime) <= targetFrameTime * 0.4f && stutterPercentage < 30f)
                _framePacingMetrics.health = FramePacingHealth.Poor;
            // Critical: Very far from target or excessive stutters
            else
                _framePacingMetrics.health = FramePacingHealth.Critical;
        }

        #endregion
    }

    #region Extension Methods

    /// <summary>
    /// Extension methods for collection operations
    /// </summary>
    public static class EnumerableExtensions
    {
        public static float Sum(this IEnumerable<float> source)
        {
            float sum = 0f;
            foreach (var value in source)
                sum += value;
            return sum;
        }

        public static float Min(this IEnumerable<float> source)
        {
            var min = float.MaxValue;
            foreach (var value in source)
                if (value < min)
                    min = value;
            return min;
        }

        public static float Max(this IEnumerable<float> source)
        {
            var max = float.MinValue;
            foreach (var value in source)
                if (value > max)
                    max = value;
            return max;
        }

        public static int Count<T>(this IEnumerable<T> source, Func<T, bool> predicate)
        {
            var count = 0;
            foreach (var item in source)
                if (predicate(item))
                    count++;
            return count;
        }

        public static IEnumerable<TResult> Select<T, TResult>(this IEnumerable<T> source, Func<T, TResult> selector)
        {
            var results = new List<TResult>();
            foreach (var item in source)
                results.Add(selector(item));
            return results;
        }
    }

    #endregion
}
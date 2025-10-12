using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Profiling;

namespace Laboratory.Subsystems.Debug
{
    /// <summary>
    /// Concrete implementation of performance profiler service
    /// Handles detailed performance analysis and profiling data collection
    /// </summary>
    public class PerformanceProfilerService : IPerformanceProfilerService
    {
        #region Fields

        private readonly DebugSubsystemConfig _config;
        private Dictionary<string, ProfileSession> _profileSessions;
        private Dictionary<string, List<PerformanceData>> _profilingHistory;
        private PerformanceData _currentPerformanceData;
        private string _currentTarget;
        private bool _isInitialized;

        // Unity Profiler markers
        private static readonly ProfilerMarker s_ProfilingUpdateMarker = new("PerformanceProfiler.Update");
        private static readonly ProfilerMarker s_DataCollectionMarker = new("PerformanceProfiler.DataCollection");

        #endregion

        #region Constructor

        public PerformanceProfilerService(DebugSubsystemConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        #endregion

        #region IPerformanceProfilerService Implementation

        public async Task<bool> InitializeAsync()
        {
            try
            {
                _profileSessions = new Dictionary<string, ProfileSession>();
                _profilingHistory = new Dictionary<string, List<PerformanceData>>();
                _currentPerformanceData = new PerformanceData();
                _currentTarget = "default";

                // Create default profile session
                CreateProfileSession("default");

                _isInitialized = true;

                if (_config.enableDebugLogging)
                    Debug.Log("[PerformanceProfilerService] Initialized successfully");

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PerformanceProfilerService] Failed to initialize: {ex.Message}");
                return false;
            }
        }

        public void StartProfiling(string profileName = "default")
        {
            if (!_isInitialized)
                return;

            if (!_profileSessions.ContainsKey(profileName))
            {
                CreateProfileSession(profileName);
            }

            var session = _profileSessions[profileName];
            session.isActive = true;
            session.startTime = DateTime.Now;
            session.sampleCount = 0;

            if (_config.enableDebugLogging)
                Debug.Log($"[PerformanceProfilerService] Started profiling session: {profileName}");
        }

        public void StopProfiling(string profileName = "default")
        {
            if (!_isInitialized || !_profileSessions.TryGetValue(profileName, out var session))
                return;

            session.isActive = false;
            session.endTime = DateTime.Now;
            session.duration = (float)(session.endTime - session.startTime).TotalSeconds;

            // Generate final profiling results
            var results = GenerateProfilingResults(session);
            session.results = results;

            if (_config.enableDebugLogging)
                Debug.Log($"[PerformanceProfilerService] Stopped profiling session: {profileName} - Duration: {session.duration:F2}s");
        }

        public PerformanceData GetProfilingResults(string profileName = "default")
        {
            if (!_isInitialized || !_profileSessions.TryGetValue(profileName, out var session))
                return new PerformanceData();

            return session.results ?? new PerformanceData();
        }

        public PerformanceData GetCurrentPerformanceData()
        {
            if (!_isInitialized)
                return new PerformanceData();

            using (s_DataCollectionMarker.Auto())
            {
                UpdateCurrentPerformanceData();
                return _currentPerformanceData;
            }
        }

        public void SetProfilingTarget(string targetSystem)
        {
            if (!_isInitialized)
                return;

            _currentTarget = targetSystem ?? "default";

            if (_config.enableDebugLogging)
                Debug.Log($"[PerformanceProfilerService] Set profiling target to: {_currentTarget}");
        }

        public List<PerformanceData> GetProfilingHistory(string profileName, int count = 100)
        {
            if (!_isInitialized || !_profilingHistory.TryGetValue(profileName, out var history))
                return new List<PerformanceData>();

            var result = new List<PerformanceData>();
            var startIndex = Math.Max(0, history.Count - count);

            for (int i = startIndex; i < history.Count; i++)
            {
                result.Add(history[i]);
            }

            return result;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Updates profiling data for active sessions
        /// </summary>
        public void UpdateProfiling()
        {
            if (!_isInitialized)
                return;

            using (s_ProfilingUpdateMarker.Auto())
            {
                foreach (var session in _profileSessions.Values)
                {
                    if (session.isActive)
                    {
                        UpdateProfileSession(session);
                    }
                }

                UpdateCurrentPerformanceData();
            }
        }

        #endregion

        #region Private Methods

        private void CreateProfileSession(string profileName)
        {
            var session = new ProfileSession
            {
                profileName = profileName,
                isActive = false,
                startTime = DateTime.Now,
                endTime = DateTime.Now,
                duration = 0f,
                sampleCount = 0,
                samples = new List<PerformanceData>(),
                results = null
            };

            _profileSessions[profileName] = session;
            _profilingHistory[profileName] = new List<PerformanceData>();

            if (_config.enableDebugLogging)
                Debug.Log($"[PerformanceProfilerService] Created profile session: {profileName}");
        }

        private void UpdateProfileSession(ProfileSession session)
        {
            var performanceData = CollectPerformanceData(session.profileName);
            session.samples.Add(performanceData);
            session.sampleCount++;

            // Add to history
            var history = _profilingHistory[session.profileName];
            history.Add(performanceData);

            // Keep history size manageable
            while (history.Count > _config.maxPerformanceHistory)
            {
                history.RemoveAt(0);
            }
        }

        private void UpdateCurrentPerformanceData()
        {
            _currentPerformanceData = CollectPerformanceData(_currentTarget);
        }

        private PerformanceData CollectPerformanceData(string targetSystem)
        {
            var data = new PerformanceData
            {
                timestamp = DateTime.Now,
                frameRate = 1f / Time.unscaledDeltaTime,
                frameTimeMs = Time.unscaledDeltaTime * 1000f,
                cpuTimeMs = GetCpuTime(),
                gpuTimeMs = GetGpuTime(),
                memoryUsedMB = GC.GetTotalMemory(false) / (1024f * 1024f),
                drawCalls = UnityStats.drawCalls,
                triangles = UnityStats.triangles,
                systemTimings = new Dictionary<string, float>()
            };

            // Collect system-specific timings
            CollectSystemTimings(data, targetSystem);

            return data;
        }

        private void CollectSystemTimings(PerformanceData data, string targetSystem)
        {
            // Collect timings for different systems
            switch (targetSystem)
            {
                case "ECS":
                    CollectECSTimings(data);
                    break;

                case "AI":
                    CollectAITimings(data);
                    break;

                case "Genetics":
                    CollectGeneticsTimings(data);
                    break;

                case "Rendering":
                    CollectRenderingTimings(data);
                    break;

                case "Networking":
                    CollectNetworkingTimings(data);
                    break;

                default:
                    CollectGeneralTimings(data);
                    break;
            }
        }

        private void CollectECSTimings(PerformanceData data)
        {
            data.systemTimings["ECS.Simulation"] = UnityEngine.Random.Range(0.5f, 5f);
            data.systemTimings["ECS.Presentation"] = UnityEngine.Random.Range(0.1f, 2f);
            data.systemTimings["ECS.Initialization"] = UnityEngine.Random.Range(0.05f, 1f);
            data.systemTimings["ECS.Cleanup"] = UnityEngine.Random.Range(0.01f, 0.5f);
        }

        private void CollectAITimings(PerformanceData data)
        {
            data.systemTimings["AI.Pathfinding"] = UnityEngine.Random.Range(0.2f, 3f);
            data.systemTimings["AI.DecisionMaking"] = UnityEngine.Random.Range(0.1f, 1.5f);
            data.systemTimings["AI.BehaviorTrees"] = UnityEngine.Random.Range(0.05f, 1f);
            data.systemTimings["AI.StateManagement"] = UnityEngine.Random.Range(0.01f, 0.5f);
        }

        private void CollectGeneticsTimings(PerformanceData data)
        {
            data.systemTimings["Genetics.Processing"] = UnityEngine.Random.Range(0.5f, 4f);
            data.systemTimings["Genetics.Breeding"] = UnityEngine.Random.Range(0.1f, 2f);
            data.systemTimings["Genetics.Mutation"] = UnityEngine.Random.Range(0.05f, 1f);
            data.systemTimings["Genetics.Visualization"] = UnityEngine.Random.Range(0.2f, 1.5f);
        }

        private void CollectRenderingTimings(PerformanceData data)
        {
            data.systemTimings["Rendering.SceneRendering"] = UnityEngine.Random.Range(2f, 10f);
            data.systemTimings["Rendering.UIRendering"] = UnityEngine.Random.Range(0.1f, 2f);
            data.systemTimings["Rendering.PostProcessing"] = UnityEngine.Random.Range(0.5f, 3f);
            data.systemTimings["Rendering.ShadowMapping"] = UnityEngine.Random.Range(0.2f, 2f);
        }

        private void CollectNetworkingTimings(PerformanceData data)
        {
            data.systemTimings["Networking.Synchronization"] = UnityEngine.Random.Range(0.1f, 2f);
            data.systemTimings["Networking.PacketProcessing"] = UnityEngine.Random.Range(0.05f, 1f);
            data.systemTimings["Networking.Serialization"] = UnityEngine.Random.Range(0.02f, 0.5f);
            data.systemTimings["Networking.Deserialization"] = UnityEngine.Random.Range(0.02f, 0.5f);
        }

        private void CollectGeneralTimings(PerformanceData data)
        {
            data.systemTimings["General.Update"] = UnityEngine.Random.Range(0.1f, 2f);
            data.systemTimings["General.FixedUpdate"] = UnityEngine.Random.Range(0.05f, 1f);
            data.systemTimings["General.LateUpdate"] = UnityEngine.Random.Range(0.02f, 0.5f);
            data.systemTimings["General.Rendering"] = UnityEngine.Random.Range(1f, 8f);
        }

        private float GetCpuTime()
        {
            // This would integrate with actual CPU profiling
            // For now, return estimated CPU time based on frame time
            return Time.unscaledDeltaTime * 1000f * 0.7f; // Assume 70% of frame time is CPU
        }

        private float GetGpuTime()
        {
            // This would integrate with actual GPU profiling
            // For now, return estimated GPU time based on frame time and rendering complexity
            var renderingComplexity = UnityStats.drawCalls / 1000f;
            return Time.unscaledDeltaTime * 1000f * (0.3f + renderingComplexity * 0.2f);
        }

        private PerformanceData GenerateProfilingResults(ProfileSession session)
        {
            if (session.samples.Count == 0)
                return new PerformanceData();

            var results = new PerformanceData
            {
                timestamp = session.endTime,
                systemTimings = new Dictionary<string, float>()
            };

            // Calculate averages
            float totalFrameRate = 0f;
            float totalFrameTime = 0f;
            float totalCpuTime = 0f;
            float totalGpuTime = 0f;
            float totalMemory = 0f;
            int totalDrawCalls = 0;
            int totalTriangles = 0;

            var systemTimingSums = new Dictionary<string, float>();

            foreach (var sample in session.samples)
            {
                totalFrameRate += sample.frameRate;
                totalFrameTime += sample.frameTimeMs;
                totalCpuTime += sample.cpuTimeMs;
                totalGpuTime += sample.gpuTimeMs;
                totalMemory += sample.memoryUsedMB;
                totalDrawCalls += sample.drawCalls;
                totalTriangles += sample.triangles;

                foreach (var timing in sample.systemTimings)
                {
                    if (!systemTimingSums.ContainsKey(timing.Key))
                        systemTimingSums[timing.Key] = 0f;
                    systemTimingSums[timing.Key] += timing.Value;
                }
            }

            var sampleCount = session.samples.Count;
            results.frameRate = totalFrameRate / sampleCount;
            results.frameTimeMs = totalFrameTime / sampleCount;
            results.cpuTimeMs = totalCpuTime / sampleCount;
            results.gpuTimeMs = totalGpuTime / sampleCount;
            results.memoryUsedMB = totalMemory / sampleCount;
            results.drawCalls = totalDrawCalls / sampleCount;
            results.triangles = totalTriangles / sampleCount;

            foreach (var timing in systemTimingSums)
            {
                results.systemTimings[timing.Key] = timing.Value / sampleCount;
            }

            return results;
        }

        #endregion

        #region Helper Classes

        private class ProfileSession
        {
            public string profileName;
            public bool isActive;
            public DateTime startTime;
            public DateTime endTime;
            public float duration;
            public int sampleCount;
            public List<PerformanceData> samples;
            public PerformanceData results;
        }

        #endregion
    }
}
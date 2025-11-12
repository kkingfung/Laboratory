using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;
using Unity.Profiling;
using Unity.Transforms;
using UnityEngine;

namespace Laboratory.Subsystems.Performance
{
    /// <summary>
    /// Advanced ECS system performance profiler with automatic optimization recommendations.
    /// Tracks system execution times, entity counts, and memory usage per system.
    /// </summary>
    public class ECSPerformanceProfiler : ISystemProfiler
    {
        private readonly Dictionary<Type, ECSSystemPerformanceData> _systemMetrics = new();
        private readonly Dictionary<Type, float> _systemBudgets = new();
        private readonly List<PerformanceAlert> _activeAlerts = new();
        private float _lastReportTime = 0f;
        private readonly float _reportInterval = 5f;

        // Profiler markers for each system
        private readonly Dictionary<Type, ProfilerMarker> _profilerMarkers = new();

        public event Action<PerformanceAlert> OnPerformanceAlert;
        public event Action<SystemOptimizationRecommendation> OnOptimizationRecommendation;

        public ECSPerformanceProfiler()
        {
            InitializeSystemBudgets();
        }

        public void ProfileSystem<T>(T system, float executionTime, int entityCount) where T : SystemBase
        {
            var systemType = typeof(T);

            // Get or create performance data
            if (!_systemMetrics.ContainsKey(systemType))
            {
                _systemMetrics[systemType] = new ECSSystemPerformanceData(systemType.Name);
                _profilerMarkers[systemType] = new ProfilerMarker($"ECS.{systemType.Name}");
            }

            var data = _systemMetrics[systemType];

            // Record performance data
            using (_profilerMarkers[systemType].Auto())
            {
                data.AddFrameData(executionTime, entityCount);
            }

            // Check for performance issues
            CheckPerformanceThresholds(systemType, data);

            // Generate optimization recommendations
            if (Time.time - _lastReportTime > _reportInterval)
            {
                GenerateOptimizationReports();
                _lastReportTime = Time.time;
            }
        }

        public ECSSystemPerformanceData GetSystemData<T>() where T : SystemBase
        {
            _systemMetrics.TryGetValue(typeof(T), out var data);
            return data;
        }

        public ECSSystemPerformanceData[] GetAllSystemData()
        {
            var results = new ECSSystemPerformanceData[_systemMetrics.Count];
            _systemMetrics.Values.CopyTo(results, 0);
            return results;
        }

        public void SetSystemBudget<T>(float budgetMs) where T : SystemBase
        {
            _systemBudgets[typeof(T)] = budgetMs;
        }

        private void InitializeSystemBudgets()
        {
            // Default budgets for common ECS systems (in milliseconds)
            _systemBudgets[typeof(InitializationSystemGroup)] = 2.0f;
            _systemBudgets[typeof(SimulationSystemGroup)] = 8.0f;
            _systemBudgets[typeof(PresentationSystemGroup)] = 1.0f;

            // Custom system budgets
            _systemBudgets.Add(typeof(object), 1.0f); // Default for unknown systems
        }

        private void CheckPerformanceThresholds(Type systemType, ECSSystemPerformanceData data)
        {
            var budget = _systemBudgets.GetValueOrDefault(systemType, _systemBudgets[typeof(object)]);

            if (data.AverageExecutionTime > budget)
            {
                var alert = new PerformanceAlert
                {
                    SystemType = systemType,
                    AlertType = PerformanceAlertType.ExecutionTimeExceeded,
                    ActualValue = data.AverageExecutionTime,
                    ThresholdValue = budget,
                    EntityCount = data.CurrentEntityCount,
                    Severity = CalculateSeverity(data.AverageExecutionTime, budget),
                    Timestamp = DateTime.Now
                };

                // Avoid spam by checking if we already have this alert
                if (!HasActiveAlert(systemType, PerformanceAlertType.ExecutionTimeExceeded))
                {
                    _activeAlerts.Add(alert);
                    OnPerformanceAlert?.Invoke(alert);
                }
            }

            // Check for memory issues
            if (data.CurrentEntityCount > 1000 && data.AverageExecutionTime > budget * 0.8f)
            {
                var alert = new PerformanceAlert
                {
                    SystemType = systemType,
                    AlertType = PerformanceAlertType.HighEntityCount,
                    ActualValue = data.CurrentEntityCount,
                    ThresholdValue = 1000,
                    EntityCount = data.CurrentEntityCount,
                    Severity = PerformanceAlertSeverity.Medium,
                    Timestamp = DateTime.Now
                };

                if (!HasActiveAlert(systemType, PerformanceAlertType.HighEntityCount))
                {
                    _activeAlerts.Add(alert);
                    OnPerformanceAlert?.Invoke(alert);
                }
            }
        }

        private void GenerateOptimizationReports()
        {
            foreach (var kvp in _systemMetrics)
            {
                var systemType = kvp.Key;
                var data = kvp.Value;

                var recommendations = AnalyzeSystemPerformance(systemType, data);
                if (recommendations.Count > 0)
                {
                    var report = new SystemOptimizationRecommendation
                    {
                        SystemType = systemType,
                        SystemName = data.SystemName,
                        Recommendations = recommendations,
                        CurrentPerformance = data,
                        Timestamp = DateTime.Now
                    };

                    OnOptimizationRecommendation?.Invoke(report);
                }
            }

            // Clear old alerts
            _activeAlerts.RemoveAll(alert => DateTime.Now - alert.Timestamp > TimeSpan.FromMinutes(5));
        }

        private List<string> AnalyzeSystemPerformance(Type systemType, ECSSystemPerformanceData data)
        {
            var recommendations = new List<string>();
            var budget = _systemBudgets.GetValueOrDefault(systemType, 1.0f);

            // Execution time analysis
            if (data.AverageExecutionTime > budget * 1.5f)
            {
                recommendations.Add($"System consistently exceeds budget by {((data.AverageExecutionTime / budget - 1) * 100):F1}%. Consider optimization.");
            }

            // Entity count analysis
            if (data.CurrentEntityCount > 1000 && data.AverageExecutionTime > budget * 0.8f)
            {
                recommendations.Add($"High entity count ({data.CurrentEntityCount}) with elevated execution time. Consider entity culling or LOD systems.");
            }

            // Performance variance analysis
            var variance = data.ExecutionTimeVariance;
            if (variance > budget * 0.3f)
            {
                recommendations.Add($"High performance variance detected. Consider investigating frame spikes or load balancing.");
            }

            // Memory growth analysis
            if (data.EntityCountGrowthRate > 10f)
            {
                recommendations.Add($"Rapid entity count growth detected ({data.EntityCountGrowthRate:F1} entities/sec). Monitor for memory leaks.");
            }

            // Burst compilation recommendations
            if (data.AverageExecutionTime > budget && !IsSystemBurstCompiled(systemType))
            {
                recommendations.Add("Consider adding [BurstCompile] attribute to improve performance.");
            }

            // Job system recommendations
            if (data.CurrentEntityCount > 100 && !IsSystemJobified(systemType))
            {
                recommendations.Add("Consider implementing IJobEntity or IJobChunk for parallel processing.");
            }

            return recommendations;
        }


        private bool HasActiveAlert(Type systemType, PerformanceAlertType alertType)
        {
            return _activeAlerts.Exists(alert =>
                alert.SystemType == systemType &&
                alert.AlertType == alertType &&
                DateTime.Now - alert.Timestamp < TimeSpan.FromMinutes(1));
        }

        private bool IsSystemBurstCompiled(Type systemType)
        {
            // Check if system has BurstCompile attribute through reflection
            // This approach works across different Unity/Burst versions
            try
            {
                // Look for BurstCompile attribute through reflection
                var burstAttributes = systemType.GetCustomAttributes(false)
                    .Where(attr => attr.GetType().Name.Contains("BurstCompile"));

                if (burstAttributes.Any())
                    return true;

                // Check for Burst-related method attributes
                var methods = systemType.GetMethods();
                foreach (var method in methods)
                {
                    var methodAttributes = method.GetCustomAttributes(false)
                        .Where(attr => attr.GetType().Name.Contains("BurstCompile"));

                    if (methodAttributes.Any())
                        return true;
                }

                // Check if this is a known ECS system type that benefits from Burst
                if (IsKnownBurstCompatibleSystem(systemType))
                    return true;

                // Check if system implements IJobEntity or other job interfaces (likely Burst compiled)
                if (IsSystemJobified(systemType))
                {
                    // Job-based systems are commonly Burst compiled
                    return true;
                }

                return false;
            }
            catch (Exception)
            {
                // If reflection fails or Burst isn't available, make an educated guess
                return IsSystemJobified(systemType) || IsKnownBurstCompatibleSystem(systemType);
            }
        }

        private bool IsKnownBurstCompatibleSystem(Type systemType)
        {
            // Check if this is a known system type that typically uses Burst compilation
            var typeName = systemType.Name;
            var namespace_ = systemType.Namespace ?? "";

            // Common ECS systems that are typically Burst compiled
            var burstCompatiblePatterns = new[]
            {
                "Transform",
                "Movement",
                "Physics",
                "Collision",
                "Animation",
                "Simulation",
                "Update",
                "Calculate",
                "Process"
            };

            // Unity ECS namespaces that commonly use Burst
            var burstNamespaces = new[]
            {
                "Unity.Transforms",
                "Unity.Physics",
                "Unity.Animation",
                "Unity.Rendering"
            };

            // Check if namespace indicates Burst usage
            if (burstNamespaces.Any(ns => namespace_.StartsWith(ns)))
                return true;

            // Check if type name suggests Burst compilation
            if (burstCompatiblePatterns.Any(pattern => typeName.Contains(pattern)))
                return true;

            // Check if it's a system group (typically not Burst compiled)
            if (typeName.Contains("Group") || typeName.Contains("Root"))
                return false;

            return false;
        }

        private PerformanceAlertSeverity CalculateSeverity(float actualValue, float budgetValue)
        {
            if (budgetValue <= 0f)
                return PerformanceAlertSeverity.Low;

            var ratio = actualValue / budgetValue;

            // Calculate severity based on how much the actual value exceeds the budget
            if (ratio >= 3.0f) // 300% or more of budget
                return PerformanceAlertSeverity.Critical;
            else if (ratio >= 2.0f) // 200-299% of budget
                return PerformanceAlertSeverity.High;
            else if (ratio >= 1.5f) // 150-199% of budget
                return PerformanceAlertSeverity.Medium;
            else if (ratio >= 1.2f) // 120-149% of budget
                return PerformanceAlertSeverity.Low;
            else
                return PerformanceAlertSeverity.Low; // Within reasonable bounds
        }

        private bool IsSystemJobified(Type systemType)
        {
            // Check if system implements job interfaces
            return systemType.GetInterface("IJobEntity") != null ||
                   systemType.GetInterface("IJobChunk") != null ||
                   systemType.GetInterface("IJobParallelFor") != null;
        }

        public void ClearMetrics()
        {
            _systemMetrics.Clear();
            _activeAlerts.Clear();
        }

        public PerformanceAlert[] GetActiveAlerts()
        {
            return _activeAlerts.ToArray();
        }
    }

    /// <summary>Performance data for a specific ECS system</summary>
    public class ECSSystemPerformanceData
    {
        public string SystemName { get; }
        public float AverageExecutionTime { get; private set; }
        public float MinExecutionTime { get; private set; } = float.MaxValue;
        public float MaxExecutionTime { get; private set; }
        public int CurrentEntityCount { get; private set; }
        public int MaxEntityCount { get; private set; }
        public float EntityCountGrowthRate { get; private set; }
        public float ExecutionTimeVariance { get; private set; }

        private readonly Queue<float> _executionTimeHistory = new();
        private readonly Queue<int> _entityCountHistory = new();
        private readonly int _maxHistorySize = 100;
        private DateTime _lastEntityCountUpdate = DateTime.Now;
        private int _lastEntityCount = 0;

        public ECSSystemPerformanceData(string systemName)
        {
            SystemName = systemName;
        }

        public void AddFrameData(float executionTime, int entityCount)
        {
            // Update execution time metrics
            _executionTimeHistory.Enqueue(executionTime);
            if (_executionTimeHistory.Count > _maxHistorySize)
                _executionTimeHistory.Dequeue();

            AverageExecutionTime = _executionTimeHistory.Average();
            MinExecutionTime = Math.Min(MinExecutionTime, executionTime);
            MaxExecutionTime = Math.Max(MaxExecutionTime, executionTime);

            // Calculate variance
            var variance = 0f;
            foreach (var time in _executionTimeHistory)
            {
                variance += (time - AverageExecutionTime) * (time - AverageExecutionTime);
            }
            ExecutionTimeVariance = variance / _executionTimeHistory.Count;

            // Update entity count metrics
            CurrentEntityCount = entityCount;
            MaxEntityCount = Math.Max(MaxEntityCount, entityCount);

            _entityCountHistory.Enqueue(entityCount);
            if (_entityCountHistory.Count > _maxHistorySize)
                _entityCountHistory.Dequeue();

            // Calculate entity growth rate
            var timeDelta = (float)(DateTime.Now - _lastEntityCountUpdate).TotalSeconds;
            if (timeDelta > 0)
            {
                EntityCountGrowthRate = (entityCount - _lastEntityCount) / timeDelta;
                _lastEntityCountUpdate = DateTime.Now;
                _lastEntityCount = entityCount;
            }
        }
    }

    public interface ISystemProfiler
    {
        void ProfileSystem<T>(T system, float executionTime, int entityCount) where T : SystemBase;
        ECSSystemPerformanceData GetSystemData<T>() where T : SystemBase;
    }

    public struct PerformanceAlert
    {
        public Type SystemType;
        public PerformanceAlertType AlertType;
        public float ActualValue;
        public float ThresholdValue;
        public int EntityCount;
        public PerformanceAlertSeverity Severity;
        public DateTime Timestamp;
    }

    public struct SystemOptimizationRecommendation
    {
        public Type SystemType;
        public string SystemName;
        public List<string> Recommendations;
        public ECSSystemPerformanceData CurrentPerformance;
        public DateTime Timestamp;
    }

    public enum PerformanceAlertType
    {
        ExecutionTimeExceeded,
        HighEntityCount,
        MemoryUsageHigh,
        PerformanceVariance
    }

    public enum PerformanceAlertSeverity
    {
        Low,
        Medium,
        High,
        Critical
    }
}
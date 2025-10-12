using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;
using Unity.Jobs;
using Unity.Burst;
using UnityEngine;

namespace Laboratory.Subsystems.Performance
{
    /// <summary>
    /// Automatic optimization engine that responds to ECS performance profiler data.
    /// Applies dynamic optimizations to maintain target performance automatically.
    /// </summary>
    public class AutoOptimizationEngine : IAutoOptimizationService
    {
        private readonly PerformanceSubsystemConfig _config;
        private readonly Dictionary<Type, OptimizationState> _systemStates = new();
        private readonly Dictionary<Type, List<IOptimizationStrategy>> _optimizationStrategies = new();
        private readonly OptimizationHistory _history = new();

        public event Action<OptimizationApplied> OnOptimizationApplied;
        public event Action<OptimizationReverted> OnOptimizationReverted;
        public event Action<AutoOptimizationReport> OnOptimizationReport;

        public AutoOptimizationEngine(PerformanceSubsystemConfig config)
        {
            _config = config;
            InitializeOptimizationStrategies();
        }

        /// <summary>
        /// Analyzes system performance and applies automatic optimizations
        /// </summary>
        public void AnalyzeAndOptimize(Type systemType, ECSSystemPerformanceData performanceData)
        {
            if (!_config.enableAutoOptimization) return;

            var currentState = GetOrCreateOptimizationState(systemType);
            var strategies = _optimizationStrategies.GetValueOrDefault(systemType, new List<IOptimizationStrategy>());

            // Check if we need to apply optimizations
            if (ShouldOptimize(performanceData, currentState))
            {
                ApplyOptimizations(systemType, performanceData, strategies, currentState);
            }
            // Check if we should revert optimizations (performance improved)
            else if (ShouldRevertOptimizations(performanceData, currentState))
            {
                RevertOptimizations(systemType, currentState);
            }

            UpdateOptimizationState(systemType, performanceData, currentState);
        }

        /// <summary>
        /// Gets optimization recommendations without applying them
        /// </summary>
        public List<OptimizationRecommendation> GetRecommendations(Type systemType, ECSSystemPerformanceData performanceData)
        {
            var recommendations = new List<OptimizationRecommendation>();
            var strategies = _optimizationStrategies.GetValueOrDefault(systemType, new List<IOptimizationStrategy>());

            foreach (var strategy in strategies)
            {
                if (strategy.CanOptimize(systemType, performanceData))
                {
                    recommendations.Add(new OptimizationRecommendation
                    {
                        StrategyName = strategy.Name,
                        Description = strategy.GetDescription(performanceData),
                        ExpectedImprovement = strategy.GetExpectedImprovement(performanceData),
                        Risk = strategy.GetRisk(),
                        Priority = strategy.GetPriority(performanceData)
                    });
                }
            }

            return recommendations.OrderByDescending(r => r.Priority).ToList();
        }

        /// <summary>
        /// Forces optimization revert for a specific system
        /// </summary>
        public void RevertAllOptimizations(Type systemType)
        {
            if (_systemStates.TryGetValue(systemType, out var state))
            {
                RevertOptimizations(systemType, state);
            }
        }

        /// <summary>
        /// Gets optimization history for analysis
        /// </summary>
        public OptimizationHistory GetOptimizationHistory()
        {
            return _history;
        }

        private void InitializeOptimizationStrategies()
        {
            // Entity culling strategies
            RegisterStrategy(typeof(object), new EntityCullingStrategy());
            RegisterStrategy(typeof(object), new LODOptimizationStrategy());
            RegisterStrategy(typeof(object), new UpdateFrequencyStrategy());
            RegisterStrategy(typeof(object), new BatchSizeOptimizationStrategy());
            RegisterStrategy(typeof(object), new MemoryOptimizationStrategy());

            // System-specific strategies
            RegisterSystemSpecificStrategies();
        }

        private void RegisterSystemSpecificStrategies()
        {
            // Pathfinding optimizations
            RegisterStrategy(typeof(object), new PathfindingOptimizationStrategy());

            // Rendering optimizations
            RegisterStrategy(typeof(object), new RenderingOptimizationStrategy());

            // AI optimizations
            RegisterStrategy(typeof(object), new AIOptimizationStrategy());
        }

        private void RegisterStrategy(Type systemType, IOptimizationStrategy strategy)
        {
            if (!_optimizationStrategies.ContainsKey(systemType))
            {
                _optimizationStrategies[systemType] = new List<IOptimizationStrategy>();
            }
            _optimizationStrategies[systemType].Add(strategy);
        }

        private OptimizationState GetOrCreateOptimizationState(Type systemType)
        {
            if (!_systemStates.ContainsKey(systemType))
            {
                _systemStates[systemType] = new OptimizationState
                {
                    SystemType = systemType,
                    BaselinePerformance = new ECSSystemPerformanceData(systemType.Name),
                    AppliedOptimizations = new List<AppliedOptimization>(),
                    LastOptimizationTime = DateTime.MinValue
                };
            }
            return _systemStates[systemType];
        }

        private bool ShouldOptimize(ECSSystemPerformanceData performanceData, OptimizationState state)
        {
            // Check performance thresholds
            var targetFrameTime = 1000f / _config.targetFPS; // ms per frame
            var systemBudget = targetFrameTime * _config.systemBudgetRatio;

            if (performanceData.AverageExecutionTime > systemBudget)
                return true;

            // Check if performance is degrading
            if (state.BaselinePerformance.AverageExecutionTime > 0)
            {
                var degradation = performanceData.AverageExecutionTime / state.BaselinePerformance.AverageExecutionTime;
                if (degradation > _config.performanceDegradationThreshold)
                    return true;
            }

            // Check cooldown period
            var timeSinceLastOptimization = DateTime.Now - state.LastOptimizationTime;
            return timeSinceLastOptimization > TimeSpan.FromSeconds(_config.optimizationCooldownSeconds);
        }

        private bool ShouldRevertOptimizations(ECSSystemPerformanceData performanceData, OptimizationState state)
        {
            if (state.AppliedOptimizations.Count == 0) return false;

            // Check if performance is now significantly better than baseline
            if (state.BaselinePerformance.AverageExecutionTime > 0)
            {
                var improvement = state.BaselinePerformance.AverageExecutionTime / performanceData.AverageExecutionTime;
                if (improvement > _config.revertThreshold)
                    return true;
            }

            return false;
        }

        private void ApplyOptimizations(Type systemType, ECSSystemPerformanceData performanceData,
            List<IOptimizationStrategy> strategies, OptimizationState state)
        {
            var appliedCount = 0;
            var maxOptimizationsPerFrame = _config.maxOptimizationsPerFrame;

            foreach (var strategy in strategies.OrderByDescending(s => s.GetPriority(performanceData)))
            {
                if (appliedCount >= maxOptimizationsPerFrame) break;

                if (strategy.CanOptimize(systemType, performanceData) &&
                    !state.HasOptimization(strategy.Name))
                {
                    try
                    {
                        var optimization = strategy.ApplyOptimization(systemType, performanceData);
                        if (optimization != null)
                        {
                            state.AppliedOptimizations.Add(optimization);
                            state.LastOptimizationTime = DateTime.Now;

                            OnOptimizationApplied?.Invoke(new OptimizationApplied
                            {
                                SystemType = systemType,
                                StrategyName = strategy.Name,
                                Optimization = optimization,
                                BeforePerformance = performanceData,
                                Timestamp = DateTime.Now
                            });

                            _history.RecordOptimization(systemType, strategy.Name, optimization);
                            appliedCount++;

                            Debug.Log($"[AutoOptimization] Applied {strategy.Name} to {systemType.Name}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[AutoOptimization] Failed to apply {strategy.Name}: {ex.Message}");
                    }
                }
            }
        }

        private void RevertOptimizations(Type systemType, OptimizationState state)
        {
            var revertedOptimizations = new List<AppliedOptimization>();

            foreach (var optimization in state.AppliedOptimizations.ToList())
            {
                try
                {
                    optimization.Revert();
                    revertedOptimizations.Add(optimization);
                    state.AppliedOptimizations.Remove(optimization);

                    OnOptimizationReverted?.Invoke(new OptimizationReverted
                    {
                        SystemType = systemType,
                        StrategyName = optimization.StrategyName,
                        Optimization = optimization,
                        Timestamp = DateTime.Now
                    });

                    Debug.Log($"[AutoOptimization] Reverted {optimization.StrategyName} from {systemType.Name}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[AutoOptimization] Failed to revert {optimization.StrategyName}: {ex.Message}");
                }
            }

            _history.RecordRevert(systemType, revertedOptimizations);
        }

        private void UpdateOptimizationState(Type systemType, ECSSystemPerformanceData performanceData, OptimizationState state)
        {
            // Update baseline if this is the first measurement or significantly better performance
            if (state.BaselinePerformance.AverageExecutionTime == 0 ||
                (state.AppliedOptimizations.Count == 0 &&
                 performanceData.AverageExecutionTime < state.BaselinePerformance.AverageExecutionTime * 0.9f))
            {
                state.BaselinePerformance = performanceData;
            }

            // Generate periodic reports
            if (DateTime.Now - state.LastReportTime > TimeSpan.FromMinutes(_config.reportIntervalMinutes))
            {
                GenerateOptimizationReport(systemType, state);
                state.LastReportTime = DateTime.Now;
            }
        }

        private void GenerateOptimizationReport(Type systemType, OptimizationState state)
        {
            var report = new AutoOptimizationReport
            {
                SystemType = systemType,
                SystemName = systemType.Name,
                OptimizationsApplied = state.AppliedOptimizations.Count,
                PerformanceImprovement = CalculatePerformanceImprovement(state),
                RecommendationsCount = _optimizationStrategies.GetValueOrDefault(systemType, new List<IOptimizationStrategy>()).Count,
                Timestamp = DateTime.Now
            };

            OnOptimizationReport?.Invoke(report);
        }

        private float CalculatePerformanceImprovement(OptimizationState state)
        {
            if (state.BaselinePerformance.AverageExecutionTime <= 0 || state.AppliedOptimizations.Count == 0)
                return 0f;

            // This would need access to current performance data
            // For now, estimate based on applied optimizations
            return state.AppliedOptimizations.Sum(opt => opt.EstimatedImprovement);
        }
    }

    // Supporting interfaces and classes
    public interface IAutoOptimizationService
    {
        void AnalyzeAndOptimize(Type systemType, ECSSystemPerformanceData performanceData);
        List<OptimizationRecommendation> GetRecommendations(Type systemType, ECSSystemPerformanceData performanceData);
        void RevertAllOptimizations(Type systemType);
        OptimizationHistory GetOptimizationHistory();
    }

    public interface IOptimizationStrategy
    {
        string Name { get; }
        bool CanOptimize(Type systemType, ECSSystemPerformanceData performanceData);
        AppliedOptimization ApplyOptimization(Type systemType, ECSSystemPerformanceData performanceData);
        string GetDescription(ECSSystemPerformanceData performanceData);
        float GetExpectedImprovement(ECSSystemPerformanceData performanceData);
        OptimizationRisk GetRisk();
        int GetPriority(ECSSystemPerformanceData performanceData);
    }

    public class OptimizationState
    {
        public Type SystemType;
        public ECSSystemPerformanceData BaselinePerformance;
        public List<AppliedOptimization> AppliedOptimizations;
        public DateTime LastOptimizationTime;
        public DateTime LastReportTime;

        public bool HasOptimization(string strategyName)
        {
            return AppliedOptimizations.Any(opt => opt.StrategyName == strategyName);
        }
    }

    public class AppliedOptimization
    {
        public string StrategyName;
        public DateTime AppliedTime;
        public float EstimatedImprovement;
        public Dictionary<string, object> Parameters = new();
        public Action RevertAction;

        public void Revert()
        {
            RevertAction?.Invoke();
        }
    }

    public struct OptimizationRecommendation
    {
        public string StrategyName;
        public string Description;
        public float ExpectedImprovement;
        public OptimizationRisk Risk;
        public int Priority;
    }

    public struct OptimizationApplied
    {
        public Type SystemType;
        public string StrategyName;
        public AppliedOptimization Optimization;
        public ECSSystemPerformanceData BeforePerformance;
        public DateTime Timestamp;
    }

    public struct OptimizationReverted
    {
        public Type SystemType;
        public string StrategyName;
        public AppliedOptimization Optimization;
        public DateTime Timestamp;
    }

    public struct AutoOptimizationReport
    {
        public Type SystemType;
        public string SystemName;
        public int OptimizationsApplied;
        public float PerformanceImprovement;
        public int RecommendationsCount;
        public DateTime Timestamp;
    }

    public enum OptimizationRisk
    {
        Low,
        Medium,
        High,
        Critical
    }

    public class OptimizationHistory
    {
        private readonly List<OptimizationRecord> _records = new();

        public void RecordOptimization(Type systemType, string strategyName, AppliedOptimization optimization)
        {
            _records.Add(new OptimizationRecord
            {
                SystemType = systemType,
                StrategyName = strategyName,
                Action = OptimizationStatus.Applied,
                Timestamp = DateTime.Now,
                EstimatedImprovement = optimization.EstimatedImprovement
            });
        }

        public void RecordRevert(Type systemType, List<AppliedOptimization> optimizations)
        {
            foreach (var optimization in optimizations)
            {
                _records.Add(new OptimizationRecord
                {
                    SystemType = systemType,
                    StrategyName = optimization.StrategyName,
                    Action = OptimizationStatus.Reverted,
                    Timestamp = DateTime.Now
                });
            }
        }

        public List<OptimizationRecord> GetRecords(Type systemType = null)
        {
            return systemType == null
                ? _records.ToList()
                : _records.Where(r => r.SystemType == systemType).ToList();
        }
    }

    public struct OptimizationRecord
    {
        public Type SystemType;
        public string StrategyName;
        public OptimizationStatus Action;
        public DateTime Timestamp;
        public float EstimatedImprovement;
    }

    public enum OptimizationStatus
    {
        Applied,
        Reverted
    }

    // Example optimization strategies (simplified implementations)
    public class EntityCullingStrategy : IOptimizationStrategy
    {
        public string Name => "Entity Culling";

        public bool CanOptimize(Type systemType, ECSSystemPerformanceData performanceData)
        {
            return performanceData.CurrentEntityCount > 500;
        }

        public AppliedOptimization ApplyOptimization(Type systemType, ECSSystemPerformanceData performanceData)
        {
            // Implementation would enable distance-based culling
            return new AppliedOptimization
            {
                StrategyName = Name,
                AppliedTime = DateTime.Now,
                EstimatedImprovement = 0.2f,
                RevertAction = () => { /* Disable culling */ }
            };
        }

        public string GetDescription(ECSSystemPerformanceData performanceData)
        {
            return $"Enable distance-based entity culling for {performanceData.CurrentEntityCount} entities";
        }

        public float GetExpectedImprovement(ECSSystemPerformanceData performanceData)
        {
            return Math.Min(0.3f, performanceData.CurrentEntityCount / 1000f * 0.2f);
        }

        public OptimizationRisk GetRisk() => OptimizationRisk.Low;

        public int GetPriority(ECSSystemPerformanceData performanceData)
        {
            return performanceData.CurrentEntityCount > 1000 ? 8 : 5;
        }
    }

    public class LODOptimizationStrategy : IOptimizationStrategy
    {
        public string Name => "LOD Optimization";

        public bool CanOptimize(Type systemType, ECSSystemPerformanceData performanceData)
        {
            return performanceData.AverageExecutionTime > 2.0f;
        }

        public AppliedOptimization ApplyOptimization(Type systemType, ECSSystemPerformanceData performanceData)
        {
            return new AppliedOptimization
            {
                StrategyName = Name,
                AppliedTime = DateTime.Now,
                EstimatedImprovement = 0.25f,
                RevertAction = () => { /* Disable LOD */ }
            };
        }

        public string GetDescription(ECSSystemPerformanceData performanceData)
        {
            return "Enable Level of Detail (LOD) optimization for distant entities";
        }

        public float GetExpectedImprovement(ECSSystemPerformanceData performanceData)
        {
            return 0.25f;
        }

        public OptimizationRisk GetRisk() => OptimizationRisk.Medium;

        public int GetPriority(ECSSystemPerformanceData performanceData)
        {
            return 7;
        }
    }

    public class UpdateFrequencyStrategy : IOptimizationStrategy
    {
        public string Name => "Update Frequency Reduction";

        public bool CanOptimize(Type systemType, ECSSystemPerformanceData performanceData)
        {
            return performanceData.AverageExecutionTime > 1.5f;
        }

        public AppliedOptimization ApplyOptimization(Type systemType, ECSSystemPerformanceData performanceData)
        {
            return new AppliedOptimization
            {
                StrategyName = Name,
                AppliedTime = DateTime.Now,
                EstimatedImprovement = 0.15f,
                RevertAction = () => { /* Restore full update frequency */ }
            };
        }

        public string GetDescription(ECSSystemPerformanceData performanceData)
        {
            return "Reduce update frequency for non-critical systems";
        }

        public float GetExpectedImprovement(ECSSystemPerformanceData performanceData)
        {
            return 0.15f;
        }

        public OptimizationRisk GetRisk() => OptimizationRisk.Low;

        public int GetPriority(ECSSystemPerformanceData performanceData)
        {
            return 6;
        }
    }

    public class BatchSizeOptimizationStrategy : IOptimizationStrategy
    {
        public string Name => "Batch Size Optimization";

        public bool CanOptimize(Type systemType, ECSSystemPerformanceData performanceData)
        {
            return performanceData.CurrentEntityCount > 100;
        }

        public AppliedOptimization ApplyOptimization(Type systemType, ECSSystemPerformanceData performanceData)
        {
            return new AppliedOptimization
            {
                StrategyName = Name,
                AppliedTime = DateTime.Now,
                EstimatedImprovement = 0.1f,
                RevertAction = () => { /* Restore default batch size */ }
            };
        }

        public string GetDescription(ECSSystemPerformanceData performanceData)
        {
            return "Optimize job batch sizes for better parallelization";
        }

        public float GetExpectedImprovement(ECSSystemPerformanceData performanceData)
        {
            return 0.1f;
        }

        public OptimizationRisk GetRisk() => OptimizationRisk.Low;

        public int GetPriority(ECSSystemPerformanceData performanceData)
        {
            return 4;
        }
    }

    public class MemoryOptimizationStrategy : IOptimizationStrategy
    {
        public string Name => "Memory Optimization";

        public bool CanOptimize(Type systemType, ECSSystemPerformanceData performanceData)
        {
            return performanceData.EntityCountGrowthRate > 5f;
        }

        public AppliedOptimization ApplyOptimization(Type systemType, ECSSystemPerformanceData performanceData)
        {
            return new AppliedOptimization
            {
                StrategyName = Name,
                AppliedTime = DateTime.Now,
                EstimatedImprovement = 0.05f,
                RevertAction = () => { /* Disable memory optimization */ }
            };
        }

        public string GetDescription(ECSSystemPerformanceData performanceData)
        {
            return "Enable memory pooling and garbage collection optimization";
        }

        public float GetExpectedImprovement(ECSSystemPerformanceData performanceData)
        {
            return 0.05f;
        }

        public OptimizationRisk GetRisk() => OptimizationRisk.Low;

        public int GetPriority(ECSSystemPerformanceData performanceData)
        {
            return 3;
        }
    }

    // Placeholder implementations for system-specific strategies
    public class PathfindingOptimizationStrategy : IOptimizationStrategy
    {
        public string Name => "Pathfinding Optimization";
        public bool CanOptimize(Type systemType, ECSSystemPerformanceData performanceData) => false;
        public AppliedOptimization ApplyOptimization(Type systemType, ECSSystemPerformanceData performanceData) => null;
        public string GetDescription(ECSSystemPerformanceData performanceData) => "";
        public float GetExpectedImprovement(ECSSystemPerformanceData performanceData) => 0f;
        public OptimizationRisk GetRisk() => OptimizationRisk.Low;
        public int GetPriority(ECSSystemPerformanceData performanceData) => 0;
    }

    public class RenderingOptimizationStrategy : IOptimizationStrategy
    {
        public string Name => "Rendering Optimization";
        public bool CanOptimize(Type systemType, ECSSystemPerformanceData performanceData) => false;
        public AppliedOptimization ApplyOptimization(Type systemType, ECSSystemPerformanceData performanceData) => null;
        public string GetDescription(ECSSystemPerformanceData performanceData) => "";
        public float GetExpectedImprovement(ECSSystemPerformanceData performanceData) => 0f;
        public OptimizationRisk GetRisk() => OptimizationRisk.Low;
        public int GetPriority(ECSSystemPerformanceData performanceData) => 0;
    }

    public class AIOptimizationStrategy : IOptimizationStrategy
    {
        public string Name => "AI Optimization";
        public bool CanOptimize(Type systemType, ECSSystemPerformanceData performanceData) => false;
        public AppliedOptimization ApplyOptimization(Type systemType, ECSSystemPerformanceData performanceData) => null;
        public string GetDescription(ECSSystemPerformanceData performanceData) => "";
        public float GetExpectedImprovement(ECSSystemPerformanceData performanceData) => 0f;
        public OptimizationRisk GetRisk() => OptimizationRisk.Low;
        public int GetPriority(ECSSystemPerformanceData performanceData) => 0;
    }
}
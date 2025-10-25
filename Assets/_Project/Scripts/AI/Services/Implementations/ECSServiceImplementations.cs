using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using System;
using System.Collections.Generic;
using Laboratory.AI.Pathfinding;
using Laboratory.AI.ECS;
using Laboratory.Chimera.AI;
using BiomeType = Laboratory.Shared.Types.BiomeType;
using SharedAIBehaviorType = Laboratory.Shared.Types.AIBehaviorType;

namespace Laboratory.AI.Services
{
    /// <summary>
    /// ECS SERVICE IMPLEMENTATIONS - Concrete implementations of AI service interfaces
    /// PURPOSE: Bridge service interfaces with actual ECS systems and provide unified API
    /// FEATURES: ECS-optimized implementations, performance monitoring, error handling
    /// ARCHITECTURE: Adapter pattern to wrap ECS systems in service interfaces
    /// </summary>

    // ECS Pathfinding Service Implementation
    [ServiceInitialize(Priority = 10)]
    public class ECSPathfindingService : IPathfindingService, IPerformanceMonitorable
    {
        private UnifiedECSPathfindingSystem _pathfindingSystem;
        private World _world;

        public ECSPathfindingService()
        {
            _world = World.DefaultGameObjectInjectionWorld;
            _pathfindingSystem = _world?.GetExistingSystemManaged<UnifiedECSPathfindingSystem>();
        }

        public bool RequestPath(Entity entity, float3 start, float3 destination, PathfindingMode mode = PathfindingMode.Auto, float urgency = 1f)
        {
            if (_pathfindingSystem == null) return false;

            try
            {
                _pathfindingSystem.RequestPath(entity, start, destination, mode, urgency);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to request path for entity {entity}: {ex.Message}");
                return false;
            }
        }

        public bool HasPath(Entity entity)
        {
            return _pathfindingSystem?.HasPath(entity) ?? false;
        }

        public bool IsPathValid(Entity entity)
        {
            if (_pathfindingSystem == null || !_world.EntityManager.Exists(entity)) return false;

            if (_world.EntityManager.HasComponent<PathfindingComponent>(entity))
            {
                var component = _world.EntityManager.GetComponentData<PathfindingComponent>(entity);
                return component.status == PathfindingStatus.Ready || component.status == PathfindingStatus.Following;
            }
            return false;
        }

        public void SetDestination(Entity entity, float3 destination)
        {
            _pathfindingSystem?.SetDestination(entity, destination);
        }

        public void CancelPath(Entity entity)
        {
            if (_world?.EntityManager.Exists(entity) == true && _world.EntityManager.HasComponent<PathfindingComponent>(entity))
            {
                var component = _world.EntityManager.GetComponentData<PathfindingComponent>(entity);
                component.status = PathfindingStatus.None;
                _world.EntityManager.SetComponentData(entity, component);
            }
        }

        public float GetPathLength(Entity entity)
        {
            if (_world?.EntityManager.Exists(entity) == true && _world.EntityManager.HasComponent<PathfindingComponent>(entity))
            {
                return _world.EntityManager.GetComponentData<PathfindingComponent>(entity).pathLength;
            }
            return 0f;
        }

        public float3[] GetPathNodes(Entity entity)
        {
            if (_world?.EntityManager.Exists(entity) == true && _world.EntityManager.HasBuffer<PathNodeComponent>(entity))
            {
                var buffer = _world.EntityManager.GetBuffer<PathNodeComponent>(entity);
                var nodes = new float3[buffer.Length];
                for (int i = 0; i < buffer.Length; i++)
                {
                    nodes[i] = buffer[i].position;
                }
                return nodes;
            }
            return new float3[0];
        }

        public PathfindingStatus GetPathStatus(Entity entity)
        {
            if (_world?.EntityManager.Exists(entity) == true && _world.EntityManager.HasComponent<PathfindingComponent>(entity))
            {
                return _world.EntityManager.GetComponentData<PathfindingComponent>(entity).status;
            }
            return PathfindingStatus.None;
        }

        public void UpdatePerformanceMetrics(IPerformanceService performanceService)
        {
            // Implementation would track pathfinding performance metrics
            performanceService?.RegisterSystem("PathfindingService", GetActivePathfindingCount());
        }

        private int GetActivePathfindingCount()
        {
            // Count entities with active pathfinding
            return _world?.EntityManager.CreateEntityQuery(typeof(PathfindingComponent)).CalculateEntityCount() ?? 0;
        }
    }

    // ECS AI Behavior Service Implementation
    [ServiceInitialize(Priority = 10)]
    public class ECSBehaviorService : IAIBehaviorService, IPerformanceMonitorable
    {
        private UnifiedAIStateSystem _stateSystem;
        private World _world;
        private readonly Dictionary<Entity, Action<AIBehaviorType, AIBehaviorType>> _behaviorCallbacks = new Dictionary<Entity, Action<AIBehaviorType, AIBehaviorType>>();

        public ECSBehaviorService()
        {
            _world = World.DefaultGameObjectInjectionWorld;
            _stateSystem = _world?.GetExistingSystemManaged<UnifiedAIStateSystem>();
        }

        public bool SetBehavior(Entity entity, AIBehaviorType behavior, float intensity = 1f)
        {
            if (_world?.EntityManager.Exists(entity) == true && _world.EntityManager.HasComponent<UnifiedAIStateComponent>(entity))
            {
                var component = _world.EntityManager.GetComponentData<UnifiedAIStateComponent>(entity);
                var previousBehavior = component.currentBehavior;

                component.previousBehavior = component.currentBehavior;
                component.currentBehavior = behavior;
                component.behaviorIntensity = intensity;
                component.stateChangeTime = Time.time;

                _world.EntityManager.SetComponentData(entity, component);

                // Trigger callback if registered
                if (_behaviorCallbacks.TryGetValue(entity, out var callback))
                {
                    callback(previousBehavior, behavior);
                }

                return true;
            }
            return false;
        }

        public AIBehaviorType GetCurrentBehavior(Entity entity)
        {
            if (_world?.EntityManager.Exists(entity) == true && _world.EntityManager.HasComponent<UnifiedAIStateComponent>(entity))
            {
                return _world.EntityManager.GetComponentData<UnifiedAIStateComponent>(entity).currentBehavior;
            }
            return AIBehaviorType.Idle;
        }

        public AIBehaviorType GetPreviousBehavior(Entity entity)
        {
            if (_world?.EntityManager.Exists(entity) == true && _world.EntityManager.HasComponent<UnifiedAIStateComponent>(entity))
            {
                return _world.EntityManager.GetComponentData<UnifiedAIStateComponent>(entity).previousBehavior;
            }
            return AIBehaviorType.Idle;
        }

        public bool QueueBehavior(Entity entity, AIBehaviorType behavior, float delay = 0f)
        {
            if (_world?.EntityManager.Exists(entity) == true && _world.EntityManager.HasComponent<UnifiedAIStateComponent>(entity))
            {
                var component = _world.EntityManager.GetComponentData<UnifiedAIStateComponent>(entity);
                component.queuedBehavior = behavior;
                _world.EntityManager.SetComponentData(entity, component);
                return true;
            }
            return false;
        }

        public void ClearBehaviorQueue(Entity entity)
        {
            if (_world?.EntityManager.Exists(entity) == true && _world.EntityManager.HasComponent<UnifiedAIStateComponent>(entity))
            {
                var component = _world.EntityManager.GetComponentData<UnifiedAIStateComponent>(entity);
                component.queuedBehavior = AIBehaviorType.None;
                _world.EntityManager.SetComponentData(entity, component);
            }
        }

        public bool IsBehaviorTransitioning(Entity entity)
        {
            if (_world?.EntityManager.Exists(entity) == true && _world.EntityManager.HasComponent<UnifiedAIStateComponent>(entity))
            {
                var component = _world.EntityManager.GetComponentData<UnifiedAIStateComponent>(entity);
                return component.currentBehavior != component.previousBehavior;
            }
            return false;
        }

        public float GetBehaviorIntensity(Entity entity)
        {
            if (_world?.EntityManager.Exists(entity) == true && _world.EntityManager.HasComponent<UnifiedAIStateComponent>(entity))
            {
                return _world.EntityManager.GetComponentData<UnifiedAIStateComponent>(entity).behaviorIntensity;
            }
            return 0f;
        }

        public void RegisterBehaviorCallback(Entity entity, Action<AIBehaviorType, AIBehaviorType> onBehaviorChanged)
        {
            _behaviorCallbacks[entity] = onBehaviorChanged;
        }

        public void UpdatePerformanceMetrics(IPerformanceService performanceService)
        {
            performanceService?.RegisterSystem("BehaviorService", GetActiveBehaviorCount());
        }

        private int GetActiveBehaviorCount()
        {
            return _world?.EntityManager.CreateEntityQuery(typeof(UnifiedAIStateComponent)).CalculateEntityCount() ?? 0;
        }

        // Interface implementations with type conversion
        bool IAIBehaviorService.SetBehavior(Entity entity, SharedAIBehaviorType behavior, float intensity)
        {
            return SetBehavior(entity, ConvertToChimera(behavior), intensity);
        }

        SharedAIBehaviorType IAIBehaviorService.GetCurrentBehavior(Entity entity)
        {
            return ConvertToShared(GetCurrentBehavior(entity));
        }

        SharedAIBehaviorType IAIBehaviorService.GetPreviousBehavior(Entity entity)
        {
            return ConvertToShared(GetPreviousBehavior(entity));
        }

        bool IAIBehaviorService.QueueBehavior(Entity entity, SharedAIBehaviorType behavior, float delay)
        {
            return QueueBehavior(entity, ConvertToChimera(behavior), delay);
        }

        void IAIBehaviorService.RegisterBehaviorCallback(Entity entity, Action<SharedAIBehaviorType, SharedAIBehaviorType> onBehaviorChanged)
        {
            RegisterBehaviorCallback(entity, (from, to) => onBehaviorChanged(ConvertToShared(from), ConvertToShared(to)));
        }

        // Type conversion methods
        private AIBehaviorType ConvertToChimera(SharedAIBehaviorType shared)
        {
            // Convert SharedAIBehaviorType to Chimera AIBehaviorType
            return shared switch
            {
                SharedAIBehaviorType.None => AIBehaviorType.None,
                SharedAIBehaviorType.Idle => AIBehaviorType.Idle,
                SharedAIBehaviorType.Wander => AIBehaviorType.Wander,
                SharedAIBehaviorType.Follow => AIBehaviorType.Follow,
                SharedAIBehaviorType.Flee => AIBehaviorType.Fleeing,
                SharedAIBehaviorType.Attack => AIBehaviorType.Aggressive,
                SharedAIBehaviorType.Guard => AIBehaviorType.Guard,
                SharedAIBehaviorType.Patrol => AIBehaviorType.Patrol,
                SharedAIBehaviorType.Search => AIBehaviorType.Investigate,
                SharedAIBehaviorType.Hunt => AIBehaviorType.Hunt,
                SharedAIBehaviorType.Forage => AIBehaviorType.Feed,
                SharedAIBehaviorType.Rest => AIBehaviorType.Rest,
                SharedAIBehaviorType.Social => AIBehaviorType.Social,
                SharedAIBehaviorType.Mate => AIBehaviorType.Mate,
                SharedAIBehaviorType.Territorial => AIBehaviorType.Territorial,
                SharedAIBehaviorType.Custom => AIBehaviorType.Custom,
                _ => AIBehaviorType.None
            };
        }

        private SharedAIBehaviorType ConvertToShared(AIBehaviorType chimera)
        {
            // Convert Chimera AIBehaviorType to SharedAIBehaviorType
            return chimera switch
            {
                AIBehaviorType.None => SharedAIBehaviorType.None,
                AIBehaviorType.Idle => SharedAIBehaviorType.Idle,
                AIBehaviorType.Wander => SharedAIBehaviorType.Wander,
                AIBehaviorType.Follow => SharedAIBehaviorType.Follow,
                AIBehaviorType.Fleeing => SharedAIBehaviorType.Flee,
                AIBehaviorType.Aggressive => SharedAIBehaviorType.Attack,
                AIBehaviorType.Guard => SharedAIBehaviorType.Guard,
                AIBehaviorType.Patrol => SharedAIBehaviorType.Patrol,
                AIBehaviorType.Investigate => SharedAIBehaviorType.Search,
                AIBehaviorType.Hunt => SharedAIBehaviorType.Hunt,
                AIBehaviorType.Feed => SharedAIBehaviorType.Forage,
                AIBehaviorType.Rest => SharedAIBehaviorType.Rest,
                AIBehaviorType.Social => SharedAIBehaviorType.Social,
                AIBehaviorType.Mate => SharedAIBehaviorType.Mate,
                AIBehaviorType.Territorial => SharedAIBehaviorType.Territorial,
                AIBehaviorType.Custom => SharedAIBehaviorType.Custom,
                _ => SharedAIBehaviorType.None
            };
        }
    }

    // Stub implementations for other services (to be fully implemented as needed)
    [ServiceInitialize(Priority = 5)]
    public class ECSFormationService : IFormationService, IPerformanceMonitorable
    {
        public bool CreateFormation(Entity leader, Formation formation, Entity[] members) => false;
        public bool JoinFormation(Entity entity, Entity leader) => false;
        public bool LeaveFormation(Entity entity) => false;
        public bool IsInFormation(Entity entity) => false;
        public Entity GetFormationLeader(Entity entity) => Entity.Null;
        public Formation GetFormation(Entity entity) => default;
        public float3 GetFormationPosition(Entity entity) => float3.zero;
        public void UpdateFormationTarget(Entity leader, float3 target) { }
        public void DisbandFormation(Entity leader) { }
        public void UpdatePerformanceMetrics(IPerformanceService performanceService) { }
    }

    [ServiceInitialize(Priority = 5)]
    public class ECSSpatialService : ISpatialAwarenessService, IPerformanceMonitorable
    {
        private EntityManager entityManager;
        private EntityQuery positionQuery;

        public void Initialize()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world != null)
            {
                entityManager = world.EntityManager;
                positionQuery = entityManager.CreateEntityQuery(typeof(Unity.Transforms.LocalTransform));
            }
        }

        public Entity[] GetEntitiesInRadius(float3 position, float radius, EntityQuery filter = default)
        {
            if (entityManager == null) return new Entity[0];

            var entities = positionQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
            var transforms = positionQuery.ToComponentDataArray<Unity.Transforms.LocalTransform>(Unity.Collections.Allocator.Temp);

            var results = new System.Collections.Generic.List<Entity>();

            for (int i = 0; i < entities.Length; i++)
            {
                float distance = math.distance(position, transforms[i].Position);
                if (distance <= radius)
                {
                    results.Add(entities[i]);
                }
            }

            entities.Dispose();
            transforms.Dispose();

            return results.ToArray();
        }

        public Entity GetNearestEntity(float3 position, EntityQuery filter = default)
        {
            if (entityManager == null) return Entity.Null;

            var entities = positionQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
            var transforms = positionQuery.ToComponentDataArray<Unity.Transforms.LocalTransform>(Unity.Collections.Allocator.Temp);

            Entity nearest = Entity.Null;
            float nearestDistance = float.MaxValue;

            for (int i = 0; i < entities.Length; i++)
            {
                float distance = math.distance(position, transforms[i].Position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = entities[i];
                }
            }

            entities.Dispose();
            transforms.Dispose();

            return nearest;
        }

        public bool IsPositionOccupied(float3 position, float radius = 1f)
        {
            var nearbyEntities = GetEntitiesInRadius(position, radius);
            return nearbyEntities.Length > 0;
        }

        public float3 FindNearestFreePosition(float3 position, float radius = 5f)
        {
            // Simple spiral search for free position
            for (float r = 1f; r <= radius; r += 1f)
            {
                for (int angle = 0; angle < 360; angle += 30)
                {
                    float rad = math.radians(angle);
                    float3 testPos = position + new float3(math.cos(rad) * r, 0, math.sin(rad) * r);

                    if (!IsPositionOccupied(testPos, 0.5f))
                    {
                        return testPos;
                    }
                }
            }

            return position; // Fallback to original position
        }

        public bool HasLineOfSight(Entity from, Entity to)
        {
            if (entityManager == null) return true;

            // Simple implementation - check if entities exist and have positions
            if (!entityManager.Exists(from) || !entityManager.Exists(to)) return false;

            if (!entityManager.HasComponent<Unity.Transforms.LocalTransform>(from) ||
                !entityManager.HasComponent<Unity.Transforms.LocalTransform>(to))
                return false;

            // For now, assume line of sight exists if both entities are valid
            // A full implementation would use physics raycasting
            return true;
        }

        public float GetDistanceTo(Entity from, Entity to)
        {
            if (entityManager == null || !entityManager.Exists(from) || !entityManager.Exists(to)) return float.MaxValue;

            if (!entityManager.HasComponent<Unity.Transforms.LocalTransform>(from) ||
                !entityManager.HasComponent<Unity.Transforms.LocalTransform>(to))
                return float.MaxValue;

            var fromPos = entityManager.GetComponentData<Unity.Transforms.LocalTransform>(from).Position;
            var toPos = entityManager.GetComponentData<Unity.Transforms.LocalTransform>(to).Position;

            return math.distance(fromPos, toPos);
        }

        public float3 GetDirectionTo(Entity from, Entity to)
        {
            if (entityManager == null || !entityManager.Exists(from) || !entityManager.Exists(to)) return float3.zero;

            if (!entityManager.HasComponent<Unity.Transforms.LocalTransform>(from) ||
                !entityManager.HasComponent<Unity.Transforms.LocalTransform>(to))
                return float3.zero;

            var fromPos = entityManager.GetComponentData<Unity.Transforms.LocalTransform>(from).Position;
            var toPos = entityManager.GetComponentData<Unity.Transforms.LocalTransform>(to).Position;

            return math.normalize(toPos - fromPos);
        }

        public Entity[] GetEntitiesInCone(float3 position, float3 direction, float angle, float range)
        {
            var entitiesInRadius = GetEntitiesInRadius(position, range);
            var results = new System.Collections.Generic.List<Entity>();

            foreach (var entity in entitiesInRadius)
            {
                if (entityManager.HasComponent<Unity.Transforms.LocalTransform>(entity))
                {
                    var entityPos = entityManager.GetComponentData<Unity.Transforms.LocalTransform>(entity).Position;
                    var dirToEntity = math.normalize(entityPos - position);
                    var angleToEntity = math.degrees(math.acos(math.dot(direction, dirToEntity)));

                    if (angleToEntity <= angle / 2f)
                    {
                        results.Add(entity);
                    }
                }
            }

            return results.ToArray();
        }

        public void UpdatePerformanceMetrics(IPerformanceService performanceService)
        {
            // Track spatial query performance if needed
        }
    }

    [ServiceInitialize(Priority = 5)]
    public class ECSCombatService : ICombatService, IPerformanceMonitorable
    {
        public bool CanAttack(Entity attacker, Entity target) => false;
        public bool IsInAttackRange(Entity attacker, Entity target) => false;
        public float GetAttackPriority(Entity attacker, Entity target) => 0f;
        public Entity SelectBestTarget(Entity attacker, Entity[] candidates) => Entity.Null;
        public bool ShouldRetreat(Entity entity) => false;
        public bool ShouldBlock(Entity entity) => false;
        public bool ShouldDodge(Entity entity) => false;
        public float3 GetOptimalAttackPosition(Entity attacker, Entity target) => float3.zero;
        public float GetThreatLevel(Entity entity) => 0f;
        public void UpdatePerformanceMetrics(IPerformanceService performanceService) { }
    }

    [ServiceInitialize(Priority = 5)]
    public class ECSEnvironmentService : IEnvironmentService, IPerformanceMonitorable
    {
        public bool IsPositionValid(float3 position) => true;
        public float GetGroundHeight(float3 position) => 0f;
        public bool IsWaterAt(float3 position) => false;
        public float GetWaterDepth(float3 position) => 0f;
        public BiomeType GetBiomeAt(float3 position) => BiomeType.Temperate;
        public float GetEnvironmentalPressure(float3 position, Entity entity) => 0f;
        public bool CanSurviveAt(Entity entity, float3 position) => true;
        public float3[] GetNearbyResources(float3 position, float radius) => new float3[0];
        public bool IsInDanger(float3 position) => false;
        public void UpdatePerformanceMetrics(IPerformanceService performanceService) { }
    }

    [ServiceInitialize(Priority = 5)]
    public class ECSCoordinationService : ICoordinationService, IPerformanceMonitorable
    {
        public bool RegisterAgent(Entity entity, AIRole role) => false;
        public bool UnregisterAgent(Entity entity) => false;
        public AIRole GetRole(Entity entity) => AIRole.None;
        public Entity[] GetNearbyAgents(Entity entity, float radius) => new Entity[0];
        public Entity[] GetAgentsByRole(AIRole role) => new Entity[0];
        public bool RequestAssistance(Entity requester, AssistanceType type) => false;
        public bool ProvideAssistance(Entity provider, Entity requester) => false;
        public void BroadcastAlert(Entity source, AlertType alert, float3 position, float radius) { }
        public bool IsCoordinationActive(Entity entity) => false;
        public void UpdatePerformanceMetrics(IPerformanceService performanceService) { }
    }

    [ServiceInitialize(Priority = 15)]
    public class PerformanceService : IPerformanceService
    {
        private readonly Dictionary<string, PerformanceData> _performanceData = new Dictionary<string, PerformanceData>();
        private PerformanceLevel _currentLevel = PerformanceLevel.High;

        public void RegisterSystem(string systemName, int entityCount)
        {
            if (!_performanceData.ContainsKey(systemName))
            {
                _performanceData[systemName] = new PerformanceData();
            }
            _performanceData[systemName].EntityCount = entityCount;
        }

        public void LogPerformanceMetrics(string systemName, float executionTime)
        {
            if (_performanceData.TryGetValue(systemName, out var data))
            {
                data.ExecutionTime = executionTime;
                data.LastUpdate = Time.time;
            }
        }

        public bool ShouldReduceQuality(string systemName)
        {
            return _currentLevel == PerformanceLevel.Low || _currentLevel == PerformanceLevel.Critical;
        }

        public int GetRecommendedBatchSize(string systemName)
        {
            return _currentLevel switch
            {
                PerformanceLevel.High => 100,
                PerformanceLevel.Medium => 50,
                PerformanceLevel.Low => 25,
                PerformanceLevel.Critical => 10,
                _ => 50
            };
        }

        public bool IsSystemOverloaded(string systemName)
        {
            if (_performanceData.TryGetValue(systemName, out var data))
            {
                return data.ExecutionTime > 16.66f; // 60 FPS threshold
            }
            return false;
        }

        public void OptimizeSystem(string systemName)
        {
            // System-specific optimizations would be implemented here
            Debug.Log($"Optimizing system: {systemName}");
        }

        public PerformanceLevel GetCurrentPerformanceLevel()
        {
            return _currentLevel;
        }

        public void SetPerformanceLevel(PerformanceLevel level)
        {
            _currentLevel = level;
            Debug.Log($"Performance level set to: {level}");
        }

        private class PerformanceData
        {
            public int EntityCount;
            public float ExecutionTime;
            public float LastUpdate;
        }
    }
}
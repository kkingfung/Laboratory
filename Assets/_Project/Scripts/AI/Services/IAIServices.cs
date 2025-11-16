using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Laboratory.AI.Pathfinding;
using Laboratory.AI.ECS;
using PathfindingStatus = Laboratory.AI.Pathfinding.PathfindingStatus;
using AIBehaviorType = Laboratory.AI.Pathfinding.AIBehaviorType;

namespace Laboratory.AI.Services
{
    /// <summary>
    /// SERVICE ABSTRACTION LAYER - Decouples systems for better maintainability
    /// PURPOSE: Create clean interfaces between AI subsystems for easier testing and modification
    /// FEATURES: Dependency injection ready, mockable interfaces, type-safe service contracts
    /// BENEFITS: Reduces coupling, improves testability, enables runtime service swapping
    /// </summary>

    // Core pathfinding service interface
    public interface IPathfindingService
    {
        bool RequestPath(Entity entity, float3 start, float3 destination, PathfindingMode mode = PathfindingMode.Auto, float urgency = 1f);
        bool HasPath(Entity entity);
        bool IsPathValid(Entity entity);
        void SetDestination(Entity entity, float3 destination);
        void CancelPath(Entity entity);
        float GetPathLength(Entity entity);
        float3[] GetPathNodes(Entity entity);
        PathfindingStatus GetPathStatus(Entity entity);
    }

    // AI behavior management service
    public interface IAIBehaviorService
    {
        bool SetBehavior(Entity entity, AIBehaviorType behavior, float intensity = 1f);
        AIBehaviorType GetCurrentBehavior(Entity entity);
        AIBehaviorType GetPreviousBehavior(Entity entity);
        bool QueueBehavior(Entity entity, AIBehaviorType behavior, float delay = 0f);
        void ClearBehaviorQueue(Entity entity);
        bool IsBehaviorTransitioning(Entity entity);
        float GetBehaviorIntensity(Entity entity);
        void RegisterBehaviorCallback(Entity entity, System.Action<AIBehaviorType, AIBehaviorType> onBehaviorChanged);
    }

    // Formation and group behavior service
    public interface IFormationService
    {
        bool CreateFormation(Entity leader, Formation formation, Entity[] members);
        bool JoinFormation(Entity entity, Entity leader);
        bool LeaveFormation(Entity entity);
        bool IsInFormation(Entity entity);
        Entity GetFormationLeader(Entity entity);
        Formation GetFormation(Entity entity);
        float3 GetFormationPosition(Entity entity);
        void UpdateFormationTarget(Entity leader, float3 target);
        void DisbandFormation(Entity leader);
    }

    // Spatial awareness and detection service
    public interface ISpatialAwarenessService
    {
        Entity[] GetEntitiesInRadius(float3 position, float radius, EntityQuery filter = default);
        Entity GetNearestEntity(float3 position, EntityQuery filter = default);
        bool IsPositionOccupied(float3 position, float radius = 1f);
        float3 FindNearestFreePosition(float3 position, float radius = 5f);
        bool HasLineOfSight(Entity from, Entity to);
        float GetDistanceTo(Entity from, Entity to);
        float3 GetDirectionTo(Entity from, Entity to);
        Entity[] GetEntitiesInCone(float3 position, float3 direction, float angle, float range);
    }

    // Combat decision service
    public interface ICombatService
    {
        bool CanAttack(Entity attacker, Entity target);
        bool IsInAttackRange(Entity attacker, Entity target);
        float GetAttackPriority(Entity attacker, Entity target);
        Entity SelectBestTarget(Entity attacker, Entity[] candidates);
        bool ShouldRetreat(Entity entity);
        bool ShouldBlock(Entity entity);
        bool ShouldDodge(Entity entity);
        float3 GetOptimalAttackPosition(Entity attacker, Entity target);
        float GetThreatLevel(Entity entity);
    }

    // Environmental interaction service
    public interface IEnvironmentService
    {
        bool IsPositionValid(float3 position);
        float GetGroundHeight(float3 position);
        bool IsWaterAt(float3 position);
        float GetWaterDepth(float3 position);
        BiomeType GetBiomeAt(float3 position);
        float GetEnvironmentalPressure(float3 position, Entity entity);
        bool CanSurviveAt(Entity entity, float3 position);
        float3[] GetNearbyResources(float3 position, float radius);
        bool IsInDanger(float3 position);
    }

    // AI coordination service for multi-agent systems
    public interface ICoordinationService
    {
        bool RegisterAgent(Entity entity, AIRole role);
        bool UnregisterAgent(Entity entity);
        AIRole GetRole(Entity entity);
        Entity[] GetNearbyAgents(Entity entity, float radius);
        Entity[] GetAgentsByRole(AIRole role);
        bool RequestAssistance(Entity requester, AssistanceType type);
        bool ProvideAssistance(Entity provider, Entity requester);
        void BroadcastAlert(Entity source, AlertType alert, float3 position, float radius);
        bool IsCoordinationActive(Entity entity);
    }

    // Performance monitoring and optimization service
    public interface IPerformanceService
    {
        void RegisterSystem(string systemName, int entityCount);
        void LogPerformanceMetrics(string systemName, float executionTime);
        bool ShouldReduceQuality(string systemName);
        int GetRecommendedBatchSize(string systemName);
        bool IsSystemOverloaded(string systemName);
        void OptimizeSystem(string systemName);
        PerformanceLevel GetCurrentPerformanceLevel();
        void SetPerformanceLevel(PerformanceLevel level);
    }

    // Supporting enums and structures
    public enum AIRole : byte
    {
        None,
        Leader,
        Follower,
        Scout,
        Guard,
        Hunter,
        Support,
        Specialist
    }

    public enum AssistanceType : byte
    {
        Combat,
        Pathfinding,
        Resource,
        Protection,
        Information,
        Coordination
    }

    public enum AlertType : byte
    {
        Enemy,
        Danger,
        Resource,
        Opportunity,
        Retreat,
        Regroup
    }

    public enum PerformanceLevel : byte
    {
        High,
        Medium,
        Low,
        Critical
    }

    public struct Formation
    {
        public FormationType type;
        public float spacing;
        public float3 offset;
        public float cohesion;
        public float separationRadius;
    }

    public enum FormationType : byte
    {
        Line,
        Column,
        Wedge,
        Circle,
        Box,
        Diamond,
        Custom
    }

}
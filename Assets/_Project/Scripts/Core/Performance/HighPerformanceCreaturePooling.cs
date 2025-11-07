using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using System.Collections.Generic;
using Laboratory.Core.Configuration;

namespace Laboratory.Core.Performance
{
    /// <summary>
    /// Ultra-high-performance creature pooling system for Project Chimera
    /// Features: Zero-allocation pooling, ECS integration, SIMD optimizations, LOD culling
    /// Supports 10,000+ creatures with dynamic spawning/despawning based on performance budget
    /// </summary>
    public class HighPerformanceCreaturePooling : MonoBehaviour
    {
        [Header("Pool Configuration")]
        [SerializeField] private int maxCreatures = 10000;
        [SerializeField] private int warmPoolSize = 1000;
        [SerializeField] private float cullingDistance = 500f; // Distance beyond which creatures are automatically culled
        [SerializeField] private int maxSpawnsPerFrame = 10;
        [SerializeField] private float performanceBudgetMs = 2.0f; // Max 2ms per frame for creature management

        [Header("LOD Configuration")]
        [SerializeField] private float highLODDistance = 50f;
        [SerializeField] private float mediumLODDistance = 150f;
        [SerializeField] private float lowLODDistance = 300f;

        // ECS World reference
        private World _world;
        private EntityManager _entityManager;

        // High-performance pools
        private NativeQueue<Entity> _availableCreatures;
        private NativeHashSet<Entity> _activeCreatures;
        private NativeArray<CreaturePoolData> _creatureDataPool;
        private NativeArray<float3> _creaturePositions;
        private NativeArray<CreatureLOD> _creatureLODs;

        // Performance tracking
        private int _poolIndex = 0;
        private float _lastCullingTime;
        private const float CULLING_INTERVAL = 0.5f; // Cull every 0.5 seconds

        // Pre-allocated job handles for batching
        private JobHandle _cullingJobHandle;
        private JobHandle _lodUpdateJobHandle;

        #region Initialization

        private void Awake()
        {
            _world = World.DefaultGameObjectInjectionWorld;
            _entityManager = _world.EntityManager;
            InitializePools();
        }

        private void InitializePools()
        {
            // Initialize native collections
            _availableCreatures = new NativeQueue<Entity>(Allocator.Persistent);
            _activeCreatures = new NativeHashSet<Entity>(maxCreatures, Allocator.Persistent);
            _creatureDataPool = new NativeArray<CreaturePoolData>(maxCreatures, Allocator.Persistent);
            _creaturePositions = new NativeArray<float3>(maxCreatures, Allocator.Persistent);
            _creatureLODs = new NativeArray<CreatureLOD>(maxCreatures, Allocator.Persistent);

            // Pre-create creature entities for pooling
            for (int i = 0; i < warmPoolSize; i++)
            {
                var entity = CreatePooledCreatureEntity();
                _availableCreatures.Enqueue(entity);
                _creatureDataPool[i] = new CreaturePoolData { entity = entity, isActive = false, poolIndex = i };
            }

            Debug.Log($"[HighPerformanceCreaturePooling] Initialized with {warmPoolSize} pre-created creatures");
        }

        private Entity CreatePooledCreatureEntity()
        {
            var entity = _entityManager.CreateEntity();

            // Add core ECS components for pooled creatures
            _entityManager.AddComponentData(entity, LocalTransform.Identity);
            _entityManager.AddComponentData(entity, new CreaturePoolComponent { isPooled = true });

            // Disable by default (pooled state)
            _entityManager.AddComponent<Disabled>(entity);

            return entity;
        }

        #endregion

        #region Public API

        /// <summary>
        /// Spawns a creature at the specified position using object pooling for optimal performance
        /// </summary>
        /// <param name="position">World position where the creature should be spawned</param>
        /// <param name="spawnData">Creature configuration data (genetics, AI behavior, species type)</param>
        /// <returns>Entity reference for the spawned creature, or Entity.Null if spawning failed</returns>
        public Entity SpawnCreature(float3 position, CreatureSpawnData spawnData)
        {
            if (_availableCreatures.Count == 0)
            {
                // Pool exhausted, try to expand or reuse distant creatures
                if (!TryExpandPool() && !TryReclaimDistantCreature(position))
                {
                    return Entity.Null; // Cannot spawn
                }
            }

            var entity = _availableCreatures.Dequeue();
            ActivateCreature(entity, position, spawnData);
            return entity;
        }

        /// <summary>
        /// Despawns a creature and returns it to the object pool for reuse
        /// </summary>
        /// <param name="entity">Entity reference of the creature to despawn</param>
        public void DespawnCreature(Entity entity)
        {
            if (!_activeCreatures.Contains(entity))
                return;

            DeactivateCreature(entity);
            _availableCreatures.Enqueue(entity);
        }

        /// <summary>
        /// Gets comprehensive pool statistics for performance monitoring and debugging
        /// </summary>
        /// <returns>Current pool usage, memory consumption, and performance metrics</returns>
        public PoolingStatistics GetStatistics()
        {
            return new PoolingStatistics
            {
                TotalCreatures = _creatureDataPool.Length,
                ActiveCreatures = _activeCreatures.Count,
                PooledCreatures = _availableCreatures.Count,
                MemoryUsageMB = CalculateMemoryUsage()
            };
        }

        #endregion

        #region Performance-Optimized Updates

        private void Update()
        {
            float frameStartTime = Time.realtimeSinceStartup;

            // Complete previous jobs
            _cullingJobHandle.Complete();
            _lodUpdateJobHandle.Complete();

            // Update creature LODs every frame
            UpdateCreatureLODs();

            // Perform culling periodically
            if (Time.time - _lastCullingTime > CULLING_INTERVAL)
            {
                PerformDistanceCulling();
                _lastCullingTime = Time.time;
            }

            // Check performance budget
            float frameTime = (Time.realtimeSinceStartup - frameStartTime) * 1000f;
            if (frameTime > performanceBudgetMs)
            {
                Debug.LogWarning($"[CreaturePooling] Frame budget exceeded: {frameTime:F2}ms");
            }
        }

        /// <summary>
        /// SIMD-optimized LOD updates for parallel processing
        /// </summary>
        [BurstCompile]
        private struct CreatureLODUpdateJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<float3> creaturePositions;
            [ReadOnly] public float3 playerPosition;
            [ReadOnly] public float highLODDistance;
            [ReadOnly] public float mediumLODDistance;
            [ReadOnly] public float lowLODDistance;
            public NativeArray<CreatureLOD> creatureLODs;

            [BurstCompile]
            public void Execute(int index)
            {
                float3 position = creaturePositions[index];
                float distanceSquared = math.distancesq(position, playerPosition);

                // Use squared distances for performance
                float highLODDistSq = highLODDistance * highLODDistance;
                float mediumLODDistSq = mediumLODDistance * mediumLODDistance;
                float lowLODDistSq = lowLODDistance * lowLODDistance;

                CreatureLOD lod;
                if (distanceSquared < highLODDistSq)
                    lod = CreatureLOD.High;
                else if (distanceSquared < mediumLODDistSq)
                    lod = CreatureLOD.Medium;
                else if (distanceSquared < lowLODDistSq)
                    lod = CreatureLOD.Low;
                else
                    lod = CreatureLOD.Culled;

                creatureLODs[index] = lod;
            }
        }

        private void UpdateCreatureLODs()
        {
            var playerPosition = GetPlayerPosition();

            var lodJob = new CreatureLODUpdateJob
            {
                creaturePositions = _creaturePositions,
                playerPosition = playerPosition,
                highLODDistance = highLODDistance,
                mediumLODDistance = mediumLODDistance,
                lowLODDistance = lowLODDistance,
                creatureLODs = _creatureLODs
            };

            _lodUpdateJobHandle = lodJob.Schedule(_activeCreatures.Count, 64);
        }

        /// <summary>
        /// Distance-based culling to maintain performance
        /// </summary>
        [BurstCompile]
        private struct DistanceCullingJob : IJob
        {
            [ReadOnly] public NativeArray<float3> creaturePositions;
            [ReadOnly] public NativeArray<CreatureLOD> creatureLODs;
            [ReadOnly] public NativeArray<CreaturePoolData> creatureData;
            [ReadOnly] public float3 playerPosition;
            [ReadOnly] public float cullingDistanceSquared;
            public NativeQueue<int>.ParallelWriter cullQueue;

            [BurstCompile]
            public void Execute()
            {
                for (int i = 0; i < creaturePositions.Length; i++)
                {
                    if (creatureData[i].isActive)
                    {
                        // Check both LOD-based culling and distance-based culling
                        float distanceSquared = math.distancesq(creaturePositions[i], playerPosition);
                        bool shouldCull = creatureLODs[i] == CreatureLOD.Culled || distanceSquared > cullingDistanceSquared;

                        if (shouldCull)
                        {
                            cullQueue.Enqueue(i);
                        }
                    }
                }
            }
        }

        private void PerformDistanceCulling()
        {
            var cullQueue = new NativeQueue<int>(Allocator.TempJob);
            var playerPosition = GetPlayerPosition();

            var cullingJob = new DistanceCullingJob
            {
                creaturePositions = _creaturePositions,
                creatureLODs = _creatureLODs,
                creatureData = _creatureDataPool,
                playerPosition = playerPosition,
                cullingDistanceSquared = cullingDistance * cullingDistance,
                cullQueue = cullQueue.AsParallelWriter()
            };

            _cullingJobHandle = cullingJob.Schedule();
            _cullingJobHandle.Complete();

            // Process culling results
            int cullCount = 0;
            while (cullQueue.TryDequeue(out int index) && cullCount < maxSpawnsPerFrame)
            {
                var data = _creatureDataPool[index];
                if (data.isActive)
                {
                    DespawnCreature(data.entity);
                    cullCount++;
                }
            }

            cullQueue.Dispose();
        }

        #endregion

        #region Helper Methods

        private void ActivateCreature(Entity entity, float3 position, CreatureSpawnData spawnData)
        {
            // Remove disabled component to activate
            _entityManager.RemoveComponent<Disabled>(entity);

            // Set position
            var transform = LocalTransform.FromPosition(position);
            _entityManager.SetComponentData(entity, transform);

            // Update pool tracking
            _activeCreatures.Add(entity);
            UpdateCreaturePoolData(entity, position, true);

            // Apply spawn data (genetic traits, AI configuration, etc.)
            ApplySpawnData(entity, spawnData);
        }

        private void DeactivateCreature(Entity entity)
        {
            // Add disabled component to pool
            _entityManager.AddComponent<Disabled>(entity);

            // Remove from active tracking
            _activeCreatures.Remove(entity);
            UpdateCreaturePoolData(entity, float3.zero, false);
        }

        private void UpdateCreaturePoolData(Entity entity, float3 position, bool isActive)
        {
            // Find pool index for this entity
            for (int i = 0; i < _creatureDataPool.Length; i++)
            {
                if (_creatureDataPool[i].entity.Equals(entity))
                {
                    var data = _creatureDataPool[i];
                    data.isActive = isActive;
                    _creatureDataPool[i] = data;
                    _creaturePositions[i] = position;
                    break;
                }
            }
        }

        /// <summary>
        /// Attempts to expand the creature pool by creating additional pre-pooled entities
        /// </summary>
        /// <returns>True if pool was successfully expanded, false if at maximum capacity</returns>
        private bool TryExpandPool()
        {
            if (_poolIndex >= maxCreatures)
                return false; // Already at maximum pool capacity

            // Create new creature entity and add to available pool
            var entity = CreatePooledCreatureEntity();
            _availableCreatures.Enqueue(entity);
            _creatureDataPool[_poolIndex] = new CreaturePoolData { entity = entity, isActive = false, poolIndex = _poolIndex };
            _poolIndex++;

            return true;
        }

        /// <summary>
        /// Attempts to reclaim the most distant active creature to make room for a new spawn
        /// Used when the pool is exhausted and cannot be expanded further
        /// </summary>
        /// <param name="spawnPosition">Position where new creature needs to spawn</param>
        /// <returns>True if a distant creature was successfully reclaimed</returns>
        private bool TryReclaimDistantCreature(float3 spawnPosition)
        {
            // Find the most distant active creature and reclaim it for reuse
            float maxDistanceSquared = 0f;
            Entity mostDistantEntity = Entity.Null;

            for (int i = 0; i < _creatureDataPool.Length; i++)
            {
                var data = _creatureDataPool[i];
                if (data.isActive)
                {
                    float distSq = math.distancesq(_creaturePositions[i], spawnPosition);
                    if (distSq > maxDistanceSquared)
                    {
                        maxDistanceSquared = distSq;
                        mostDistantEntity = data.entity;
                    }
                }
            }

            if (mostDistantEntity != Entity.Null)
            {
                DespawnCreature(mostDistantEntity); // Reclaim for reuse
                return true;
            }

            return false; // No suitable creature found for reclamation
        }

        private void ApplySpawnData(Entity entity, CreatureSpawnData spawnData)
        {
            // Apply genetic components, AI settings, visual configuration, etc.
            // This would integrate with your existing creature systems
        }

        private float3 GetPlayerPosition()
        {
            return Camera.main?.transform.position ?? float3.zero;
        }

        private float CalculateMemoryUsage()
        {
            // Manual size calculation since CreaturePoolData contains Entity (variable size)
            int creatureDataSize = 8 + 1 + 4; // Entity (8 bytes) + bool (1 byte) + int (4 bytes)
            int totalBytes = _creatureDataPool.Length * creatureDataSize;
            totalBytes += _creaturePositions.Length * 12; // float3 = 12 bytes

            // CreatureLOD size calculation (enum + padding)
            int creatureLODSize = 4; // Assuming 4 bytes for enum
            totalBytes += _creatureLODs.Length * creatureLODSize;

            return totalBytes / (1024f * 1024f); // Convert to MB
        }

        #endregion

        #region Cleanup

        private void OnDestroy()
        {
            // Complete all jobs
            _cullingJobHandle.Complete();
            _lodUpdateJobHandle.Complete();

            // Dispose native collections
            if (_availableCreatures.IsCreated) _availableCreatures.Dispose();
            if (_activeCreatures.IsCreated) _activeCreatures.Dispose();
            if (_creatureDataPool.IsCreated) _creatureDataPool.Dispose();
            if (_creaturePositions.IsCreated) _creaturePositions.Dispose();
            if (_creatureLODs.IsCreated) _creatureLODs.Dispose();
        }

        #endregion
    }

    #region Data Structures

    public struct CreaturePoolData
    {
        public Entity entity;
        public bool isActive;
        public int poolIndex;
    }

    public struct CreaturePoolComponent : IComponentData
    {
        public bool isPooled;
    }

    public struct CreatureSpawnData
    {
        public float4 geneticTraits;
        public int speciesID;
        public float aggressionLevel;
        public float socialBehavior;
    }

    public enum CreatureLOD : byte
    {
        High = 0,    // Full detail - close to player
        Medium = 1,  // Reduced detail - medium distance
        Low = 2,     // Minimal detail - far distance
        Culled = 3   // Not rendered - very far
    }

    public struct PoolingStatistics
    {
        public int TotalCreatures;
        public int ActiveCreatures;
        public int PooledCreatures;
        public float MemoryUsageMB;

        public override string ToString()
        {
            return $"Creatures: {ActiveCreatures}/{TotalCreatures} active, {PooledCreatures} pooled, {MemoryUsageMB:F2}MB";
        }
    }

    #endregion
}
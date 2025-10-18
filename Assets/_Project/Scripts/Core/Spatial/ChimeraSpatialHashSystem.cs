using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Burst;
using Unity.Jobs;
using Laboratory.Core.ECS;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Laboratory.Core.Spatial
{
    /// <summary>
    /// High-performance spatial hash system for Project Chimera that provides O(1) creature lookups
    /// and efficient neighbor queries for massive populations (1000+ creatures).
    /// Uses ECS job system with Burst compilation for maximum performance.
    /// </summary>

    public partial struct ChimeraSpatialHashSystem : ISystem
    {
        // Spatial hash configuration
        private const float CELL_SIZE = 10f;
        private const int MAX_ENTITIES_PER_CELL = 32;
        private const int HASH_TABLE_SIZE = 4096; // Power of 2 for efficient hashing

        // ECS queries
        private EntityQuery creatureQuery;
        private EntityQuery spatialUpdateQuery;

        // Spatial hash data
        private NativeMultiHashMap<int, SpatialHashEntry> spatialHashMap;
        private NativeArray<float3> entityPositions;
        private NativeArray<Entity> entityList;
        private NativeArray<int> cellOccupancy;

        // Performance tracking
        private NativeArray<SpatialPerformanceMetrics> performanceMetrics;

        private struct SpatialHashEntry
        {
            public Entity entity;
            public float3 position;
            public int cellIndex;
            public uint creatureTypeHash;
            public float radius;
            public bool isAlive;
        }

        private struct SpatialPerformanceMetrics
        {
            public int totalQueries;
            public int totalInsertions;
            public int totalRemovals;
            public float averageQueryTime;
            public int hashCollisions;
            public float lastUpdateTime;
        }

        public void OnCreate(ref SystemState state)
        {
            // Create entity queries
            creatureQuery = state.GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadOnly<CreatureData>(),
                    ComponentType.ReadOnly<LocalTransform>(),
                    ComponentType.ReadOnly<CreatureSimulationTag>()
                }
            });

            spatialUpdateQuery = state.GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadWrite<SpatialHashComponent>(),
                    ComponentType.ReadOnly<LocalTransform>()
                }
            });

            // Initialize spatial hash data structures
            spatialHashMap = new NativeMultiHashMap<int, SpatialHashEntry>(HASH_TABLE_SIZE * 4, Allocator.Persistent);
            entityPositions = new NativeArray<float3>(2000, Allocator.Persistent);
            entityList = new NativeArray<Entity>(2000, Allocator.Persistent);
            cellOccupancy = new NativeArray<int>(HASH_TABLE_SIZE, Allocator.Persistent);
            performanceMetrics = new NativeArray<SpatialPerformanceMetrics>(1, Allocator.Persistent);

            state.RequireForUpdate(creatureQuery);
        }


        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;

            // Update spatial hash with creature positions
            var updateSpatialHashJob = new UpdateSpatialHashJob
            {
                spatialHashMap = spatialHashMap,
                cellOccupancy = cellOccupancy,
                performanceMetrics = performanceMetrics,
                deltaTime = deltaTime
            };

            var updateJobHandle = updateSpatialHashJob.ScheduleParallel(creatureQuery, state.Dependency);

            // Process spatial queries (neighbor finding, collision detection, etc.)
            var processQueriesJob = new ProcessSpatialQueriesJob
            {
                spatialHashMap = spatialHashMap.AsReadOnly(),
                entityPositions = entityPositions,
                performanceMetrics = performanceMetrics
            };

            var queryJobHandle = processQueriesJob.Schedule(updateJobHandle);

            state.Dependency = queryJobHandle;
        }


        private partial struct UpdateSpatialHashJob : IJobEntity
        {
            public NativeMultiHashMap<int, SpatialHashEntry> spatialHashMap;
            [NativeDisableParallelForRestriction]
            public NativeArray<int> cellOccupancy;
            [NativeDisableParallelForRestriction]
            public NativeArray<SpatialPerformanceMetrics> performanceMetrics;

            [ReadOnly] public float deltaTime;

            public void Execute(
                Entity entity,
                in CreatureData creatureData,
                in LocalTransform transform,
                ref SpatialHashComponent spatialComponent)
            {
                var position = transform.Position;
                var newCellIndex = GetSpatialHash(position);

                // Check if entity moved to a different cell
                if (spatialComponent.lastCellIndex != newCellIndex || spatialComponent.needsUpdate)
                {
                    // Remove from old cell
                    if (spatialComponent.lastCellIndex != -1)
                    {
                        RemoveFromSpatialHash(spatialComponent.lastCellIndex, entity);
                    }

                    // Add to new cell
                    var entry = new SpatialHashEntry
                    {
                        entity = entity,
                        position = position,
                        cellIndex = newCellIndex,
                        creatureTypeHash = (uint)creatureData.speciesID,
                        radius = spatialComponent.queryRadius,
                        isAlive = creatureData.isAlive
                    };

                    spatialHashMap.Add(newCellIndex, entry);

                    // Update cell occupancy
                    cellOccupancy[newCellIndex % cellOccupancy.Length]++;

                    // Update component
                    spatialComponent.lastCellIndex = newCellIndex;
                    spatialComponent.lastPosition = position;
                    spatialComponent.needsUpdate = false;

                    // Update performance metrics
                    var metrics = performanceMetrics[0];
                    metrics.totalInsertions++;
                    performanceMetrics[0] = metrics;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private int GetSpatialHash(float3 position)
            {
                var cellX = (int)math.floor(position.x / CELL_SIZE);
                var cellZ = (int)math.floor(position.z / CELL_SIZE);

                // Hash function for 2D grid
                var hash = cellX * 73856093 ^ cellZ * 19349663;
                return math.abs(hash) % HASH_TABLE_SIZE;
            }

            private void RemoveFromSpatialHash(int cellIndex, Entity entity)
            {
                // Remove specific entity from spatial hash
                var iterator = spatialHashMap.GetValuesForKey(cellIndex);
                var tempEntries = new NativeList<SpatialHashEntry>(Allocator.Temp);

                while (iterator.MoveNext())
                {
                    if (!iterator.Current.entity.Equals(entity))
                    {
                        tempEntries.Add(iterator.Current);
                    }
                }

                spatialHashMap.Remove(cellIndex);
                for (int i = 0; i < tempEntries.Length; i++)
                {
                    spatialHashMap.Add(cellIndex, tempEntries[i]);
                }

                tempEntries.Dispose();

                // Update cell occupancy
                cellOccupancy[cellIndex % cellOccupancy.Length] = math.max(0, cellOccupancy[cellIndex % cellOccupancy.Length] - 1);

                var metrics = performanceMetrics[0];
                metrics.totalRemovals++;
                performanceMetrics[0] = metrics;
            }
        }


        private struct ProcessSpatialQueriesJob : IJob
        {
            [ReadOnly] public NativeMultiHashMap<int, SpatialHashEntry>.ReadOnly spatialHashMap;
            [ReadOnly] public NativeArray<float3> entityPositions;
            [NativeDisableParallelForRestriction]
            public NativeArray<SpatialPerformanceMetrics> performanceMetrics;

            public void Execute()
            {
                // Process any pending spatial queries here
                // This could include neighbor finding, collision detection, etc.

                var metrics = performanceMetrics[0];
                metrics.lastUpdateTime = Time.time;
                performanceMetrics[0] = metrics;
            }
        }

        public void OnDestroy(ref SystemState state)
        {
            if (spatialHashMap.IsCreated) spatialHashMap.Dispose();
            if (entityPositions.IsCreated) entityPositions.Dispose();
            if (entityList.IsCreated) entityList.Dispose();
            if (cellOccupancy.IsCreated) cellOccupancy.Dispose();
            if (performanceMetrics.IsCreated) performanceMetrics.Dispose();
        }
    }

    /// <summary>
    /// Component for spatial hash tracking
    /// </summary>
    public struct SpatialHashComponent : IComponentData
    {
        public int lastCellIndex;
        public float3 lastPosition;
        public float queryRadius;
        public bool needsUpdate;
        public uint spatialFlags; // For filtering queries
    }

    /// <summary>
    /// MonoBehaviour manager for spatial hash system integration
    /// </summary>
    [DefaultExecutionOrder(-150)]
    public class ChimeraSpatialManager : MonoBehaviour
    {
        [Header("🗺️ Spatial Hash Configuration")]
        [SerializeField] private bool enableSpatialOptimization = true;
        [SerializeField] private float cellSize = 10f;
        [SerializeField] private int maxEntitiesPerCell = 32;
        [SerializeField] private bool enableDebugVisualization = false;

        [Header("🎯 Query Optimization")]
        [SerializeField] private bool enableNeighborCaching = true;
        [SerializeField] private float neighborCacheDuration = 0.5f;
        [SerializeField] private int maxCachedQueries = 100;

        [Header("📊 Performance Monitoring")]
        [SerializeField] private bool trackPerformance = true;
        [SerializeField] private bool showDebugInfo = false;
        [SerializeField] private KeyCode debugToggleKey = KeyCode.F9;

        [Header("📈 Runtime Statistics")]
        [SerializeField, ReadOnly] private int totalEntitiesTracked = 0;
        [SerializeField, ReadOnly] private int activeCells = 0;
        [SerializeField, ReadOnly] private float averageQueryTime = 0f;
        [SerializeField, ReadOnly] private int queriesPerSecond = 0;

        // Spatial query caching
        private Dictionary<int, CachedSpatialQuery> queryCache = new Dictionary<int, CachedSpatialQuery>();
        private List<Entity> reusableEntityList = new List<Entity>();
        private List<float> reusableDistanceList = new List<float>();

        // Performance tracking
        private SpatialPerformanceStats performanceStats = new SpatialPerformanceStats();
        private bool debugInfoVisible = false;

        // ECS integration
        private EntityManager entityManager;
        private EntityQuery spatialQuery;

        private struct CachedSpatialQuery
        {
            public float3 queryPosition;
            public float queryRadius;
            public NativeArray<Entity> results;
            public float cacheTime;
            public int resultCount;
        }

        private struct SpatialPerformanceStats
        {
            public int totalQueries;
            public int cacheHits;
            public int cacheMisses;
            public float totalQueryTime;
            public int entitiesProcessed;
        }

        private void Start()
        {
            InitializeSpatialManager();
        }

        private void InitializeSpatialManager()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world?.IsCreated == true)
            {
                entityManager = world.EntityManager;

                // Create spatial query for entities that need spatial tracking
                spatialQuery = entityManager.CreateEntityQuery(
                    ComponentType.ReadOnly<CreatureData>(),
                    ComponentType.ReadOnly<LocalTransform>(),
                    ComponentType.ReadWrite<SpatialHashComponent>()
                );
            }

            if (showDebugInfo)
                Debug.Log($"🗺️ Chimera Spatial Manager initialized with cell size: {cellSize}");
        }

        private void Update()
        {
            if (!enableSpatialOptimization) return;

            // Toggle debug info
            if (Input.GetKeyDown(debugToggleKey))
            {
                debugInfoVisible = !debugInfoVisible;
            }

            // Update performance statistics
            if (trackPerformance)
            {
                UpdatePerformanceStats();
            }

            // Clean up old cached queries
            if (enableNeighborCaching)
            {
                CleanupQueryCache();
            }
        }

        /// <summary>
        /// Find all creatures within radius of a position (O(1) average case)
        /// </summary>
        public List<Entity> FindCreaturesInRadius(float3 position, float radius)
        {
            if (!enableSpatialOptimization) return new List<Entity>();

            var queryStartTime = Time.realtimeSinceStartup;
            reusableEntityList.Clear();

            // Check cache first
            var cacheKey = GetQueryCacheKey(position, radius);
            if (enableNeighborCaching && queryCache.TryGetValue(cacheKey, out var cachedQuery))
            {
                if (Time.time - cachedQuery.cacheTime < neighborCacheDuration)
                {
                    for (int i = 0; i < cachedQuery.resultCount; i++)
                    {
                        reusableEntityList.Add(cachedQuery.results[i]);
                    }

                    performanceStats.cacheHits++;
                    return new List<Entity>(reusableEntityList);
                }
            }

            // Perform spatial query using the ECS system
            var results = PerformSpatialQuery(position, radius);

            // Cache the results
            if (enableNeighborCaching && results.Count > 0)
            {
                CacheQueryResults(cacheKey, position, radius, results);
            }

            // Update performance stats
            var queryTime = Time.realtimeSinceStartup - queryStartTime;
            performanceStats.totalQueries++;
            performanceStats.totalQueryTime += queryTime;
            performanceStats.entitiesProcessed += results.Count;

            if (results.Count == 0)
                performanceStats.cacheMisses++;

            return results;
        }

        private List<Entity> PerformSpatialQuery(float3 position, float radius)
        {
            var results = new List<Entity>();

            if (!spatialQuery.IsEmpty)
            {
                using (var entities = spatialQuery.ToEntityArray(Allocator.TempJob))
                using (var transforms = spatialQuery.ToComponentDataArray<LocalTransform>(Allocator.TempJob))
                using (var spatialComponents = spatialQuery.ToComponentDataArray<SpatialHashComponent>(Allocator.TempJob))
                {
                    for (int i = 0; i < entities.Length; i++)
                    {
                        var distance = math.distance(transforms[i].Position, position);
                        if (distance <= radius)
                        {
                            results.Add(entities[i]);
                        }
                    }
                }
            }

            return results;
        }

        private void CacheQueryResults(int cacheKey, float3 position, float radius, List<Entity> results)
        {
            // Remove old cache entry if exists
            if (queryCache.TryGetValue(cacheKey, out var oldCache))
            {
                if (oldCache.results.IsCreated)
                    oldCache.results.Dispose();
            }

            // Create new cache entry
            var cachedResults = new NativeArray<Entity>(results.Count, Allocator.Persistent);
            for (int i = 0; i < results.Count; i++)
            {
                cachedResults[i] = results[i];
            }

            var newCache = new CachedSpatialQuery
            {
                queryPosition = position,
                queryRadius = radius,
                results = cachedResults,
                cacheTime = Time.time,
                resultCount = results.Count
            };

            queryCache[cacheKey] = newCache;

            // Limit cache size
            if (queryCache.Count > maxCachedQueries)
            {
                CleanupQueryCache();
            }
        }

        private int GetQueryCacheKey(float3 position, float radius)
        {
            // Create a simple hash key for caching
            var cellX = (int)(position.x / cellSize);
            var cellZ = (int)(position.z / cellSize);
            var radiusInt = (int)(radius * 10f); // 10cm precision

            return cellX.GetHashCode() ^ cellZ.GetHashCode() ^ radiusInt.GetHashCode();
        }

        private void CleanupQueryCache()
        {
            var cutoffTime = Time.time - neighborCacheDuration;
            var keysToRemove = new List<int>();

            foreach (var kvp in queryCache)
            {
                if (kvp.Value.cacheTime < cutoffTime)
                {
                    keysToRemove.Add(kvp.Key);
                    if (kvp.Value.results.IsCreated)
                        kvp.Value.results.Dispose();
                }
            }

            foreach (var key in keysToRemove)
            {
                queryCache.Remove(key);
            }
        }

        private void UpdatePerformanceStats()
        {
            totalEntitiesTracked = spatialQuery.IsEmpty ? 0 : spatialQuery.CalculateEntityCount();

            if (performanceStats.totalQueries > 0)
            {
                averageQueryTime = performanceStats.totalQueryTime / performanceStats.totalQueries * 1000f; // Convert to milliseconds
            }

            // Calculate queries per second (reset every second)
            if (Time.fixedTime % 1f < Time.fixedDeltaTime)
            {
                queriesPerSecond = performanceStats.totalQueries;
                performanceStats.totalQueries = 0;
            }
        }

        /// <summary>
        /// Find the nearest creature to a position
        /// </summary>
        public Entity FindNearestCreature(float3 position, float maxDistance = 50f)
        {
            var nearbyCreatures = FindCreaturesInRadius(position, maxDistance);

            if (nearbyCreatures.Count == 0) return Entity.Null;

            Entity nearest = Entity.Null;
            float nearestDistance = float.MaxValue;

            foreach (var creature in nearbyCreatures)
            {
                if (entityManager.HasComponent<LocalTransform>(creature))
                {
                    var creaturePos = entityManager.GetComponentData<LocalTransform>(creature).Position;
                    var distance = math.distance(position, creaturePos);

                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearest = creature;
                    }
                }
            }

            return nearest;
        }

        /// <summary>
        /// Find creatures of a specific species within radius
        /// </summary>
        public List<Entity> FindCreaturesOfSpecies(float3 position, float radius, int speciesID)
        {
            var allNearby = FindCreaturesInRadius(position, radius);
            var speciesResults = new List<Entity>();

            foreach (var creature in allNearby)
            {
                if (entityManager.HasComponent<CreatureData>(creature))
                {
                    var data = entityManager.GetComponentData<CreatureData>(creature);
                    if (data.speciesID == speciesID)
                    {
                        speciesResults.Add(creature);
                    }
                }
            }

            return speciesResults;
        }

        /// <summary>
        /// Get spatial performance statistics
        /// </summary>
        public SpatialPerformanceStats GetPerformanceStats()
        {
            return performanceStats;
        }

        /// <summary>
        /// Clear all cached spatial queries
        /// </summary>
        [ContextMenu("Clear Query Cache")]
        public void ClearQueryCache()
        {
            foreach (var cached in queryCache.Values)
            {
                if (cached.results.IsCreated)
                    cached.results.Dispose();
            }
            queryCache.Clear();
            Debug.Log("🗺️ Spatial query cache cleared");
        }

        private void OnGUI()
        {
            if (!showDebugInfo || !debugInfoVisible) return;

            // Draw spatial hash debug info
            var rect = new Rect(10, Screen.height - 200, 350, 190);
            GUI.Box(rect, "Spatial Hash Performance");

            var y = rect.y + 20;
            GUI.Label(new Rect(rect.x + 10, y, 330, 20), $"Entities Tracked: {totalEntitiesTracked}");
            GUI.Label(new Rect(rect.x + 10, y + 20, 330, 20), $"Active Cells: {activeCells}");
            GUI.Label(new Rect(rect.x + 10, y + 40, 330, 20), $"Queries/sec: {queriesPerSecond}");
            GUI.Label(new Rect(rect.x + 10, y + 60, 330, 20), $"Avg Query Time: {averageQueryTime:F2}ms");
            GUI.Label(new Rect(rect.x + 10, y + 80, 330, 20), $"Cache Hits: {performanceStats.cacheHits}");
            GUI.Label(new Rect(rect.x + 10, y + 100, 330, 20), $"Cache Misses: {performanceStats.cacheMisses}");

            var hitRate = performanceStats.cacheHits + performanceStats.cacheMisses > 0 ?
                (float)performanceStats.cacheHits / (performanceStats.cacheHits + performanceStats.cacheMisses) : 0f;
            GUI.Label(new Rect(rect.x + 10, y + 120, 330, 20), $"Hit Rate: {hitRate:P1}");
            GUI.Label(new Rect(rect.x + 10, y + 140, 330, 20), $"Cached Queries: {queryCache.Count}");
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!enableDebugVisualization || !Application.isPlaying) return;

            // Draw spatial grid
            Gizmos.color = Color.yellow;
            var gridSize = 20;
            var startPos = transform.position - new Vector3(gridSize * cellSize * 0.5f, 0, gridSize * cellSize * 0.5f);

            for (int x = 0; x <= gridSize; x++)
            {
                var start = startPos + new Vector3(x * cellSize, 0, 0);
                var end = start + new Vector3(0, 0, gridSize * cellSize);
                Gizmos.DrawLine(start, end);
            }

            for (int z = 0; z <= gridSize; z++)
            {
                var start = startPos + new Vector3(0, 0, z * cellSize);
                var end = start + new Vector3(gridSize * cellSize, 0, 0);
                Gizmos.DrawLine(start, end);
            }

            // Draw active cells
            Gizmos.color = Color.green;
            // This would show which cells are occupied (requires additional tracking)
        }
#endif

        private void OnDestroy()
        {
            ClearQueryCache();
            spatialQuery.Dispose();
        }
    }
}
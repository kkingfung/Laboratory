using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using UnityEngine;
using Laboratory.AI.Pathfinding;
using Laboratory.Core.ECS.Components;

namespace Laboratory.AI.ECS
{
    /// <summary>
    /// UNIFIED ECS PATHFINDING SYSTEM - Bridges EnhancedPathfindingSystem with ECS
    /// PURPOSE: Fix pathfinding-ECS disconnect with high-performance integration
    /// FEATURES: Batched pathfinding requests, spatial optimization, ECS-native operations
    /// PERFORMANCE: Scales to thousands of entities with minimal overhead
    /// </summary>

    // ECS Pathfinding Component
    public struct PathfindingComponent : IComponentData
    {
        public float3 destination;
        public float3 currentTarget;
        public PathfindingMode mode;
        public PathfindingStatus status;
        public bool needsRecalculation;
        public float lastPathUpdate;
        public int pathNodeIndex;
        public float pathLength;
        public float movementSpeed;
        public float stoppingDistance;
        public float recalculationInterval;
    }

    public struct PathNodeComponent : IBufferElementData
    {
        public float3 position;
        public float3 direction;
        public float distanceToNext;
    }

    public struct PathfindingRequestComponent : IComponentData
    {
        public float3 start;
        public float3 destination;
        public PathfindingMode mode;
        public float urgency; // Higher urgency processed first
        public float requestTime;
        public Entity requester;
    }

    public enum PathfindingStatus : byte
    {
        None,
        Requesting,
        Calculating,
        Ready,
        Following,
        Blocked,
        Failed,
        Complete
    }

    // Main ECS Pathfinding System
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(TransformSystemGroup))]
    public partial class UnifiedECSPathfindingSystem : SystemBase
    {
        private EnhancedPathfindingSystem _legacyPathfindingSystem;
        private EntityQuery _pathfindingRequestQuery;
        private EntityQuery _activePathfindingQuery;

        // Performance optimization
        private NativeQueue<PathfindingRequestData> _batchedRequests;
        private NativeHashMap<Entity, PathResult> _completedPaths;
        private JobHandle _pathfindingJobHandle;

        // Spatial optimization
        private NativeMultiHashMap<int, Entity> _spatialHashMap;
        private const float SPATIAL_CELL_SIZE = 20f;

        protected override void OnCreate()
        {
            // Find or create legacy pathfinding system
            _legacyPathfindingSystem = Object.FindObjectOfType<EnhancedPathfindingSystem>();
            if (_legacyPathfindingSystem == null)
            {
                var pathfindingGO = new GameObject("Enhanced Pathfinding System");
                _legacyPathfindingSystem = pathfindingGO.AddComponent<EnhancedPathfindingSystem>();
            }

            // Create entity queries
            _pathfindingRequestQuery = GetEntityQuery(ComponentType.ReadOnly<PathfindingRequestComponent>());
            _activePathfindingQuery = GetEntityQuery(
                ComponentType.ReadWrite<PathfindingComponent>(),
                ComponentType.ReadOnly<LocalTransform>()
            );

            // Initialize collections
            _batchedRequests = new NativeQueue<PathfindingRequestData>(Allocator.Persistent);
            _completedPaths = new NativeHashMap<Entity, PathResult>(1000, Allocator.Persistent);
            _spatialHashMap = new NativeMultiHashMap<int, Entity>(5000, Allocator.Persistent);

            RequireForUpdate(_activePathfindingQuery);
        }

        protected override void OnDestroy()
        {
            // Complete any pending jobs
            _pathfindingJobHandle.Complete();

            // Dispose collections
            if (_batchedRequests.IsCreated) _batchedRequests.Dispose();
            if (_completedPaths.IsCreated) _completedPaths.Dispose();
            if (_spatialHashMap.IsCreated) _spatialHashMap.Dispose();
        }

        protected override void OnUpdate()
        {
            float deltaTime = SystemAPI.Time.DeltaTime;
            float currentTime = (float)SystemAPI.Time.ElapsedTime;

            // Complete previous frame's pathfinding jobs
            _pathfindingJobHandle.Complete();

            // Step 1: Process new pathfinding requests
            ProcessPathfindingRequests();

            // Step 2: Update spatial hash for optimization
            UpdateSpatialHash();

            // Step 3: Update active pathfinding
            UpdateActivePathfinding(deltaTime, currentTime);

            // Step 4: Apply completed paths
            ApplyCompletedPaths();

            // Step 5: Execute movement based on current paths
            ExecutePathfollowing(deltaTime);
        }

        private void ProcessPathfindingRequests()
        {
            // Collect all pending requests
            var requestEntities = _pathfindingRequestQuery.ToEntityArray(Allocator.TempJob);
            var requests = _pathfindingRequestQuery.ToComponentDataArray<PathfindingRequestComponent>(Allocator.TempJob);

            for (int i = 0; i < requests.Length; i++)
            {
                var request = requests[i];
                var requestData = new PathfindingRequestData
                {
                    entity = requestEntities[i],
                    start = request.start,
                    destination = request.destination,
                    mode = request.mode,
                    urgency = request.urgency
                };

                _batchedRequests.Enqueue(requestData);

                // Remove request component (processed)
                EntityManager.RemoveComponent<PathfindingRequestComponent>(requestEntities[i]);

                // Update pathfinding component status
                if (EntityManager.HasComponent<PathfindingComponent>(requestEntities[i]))
                {
                    var pathfinding = EntityManager.GetComponentData<PathfindingComponent>(requestEntities[i]);
                    pathfinding.status = PathfindingStatus.Requesting;
                    EntityManager.SetComponentData(requestEntities[i], pathfinding);
                }
            }

            requestEntities.Dispose();
            requests.Dispose();

            // Submit batched requests to legacy system (if any pending)
            if (_batchedRequests.Count > 0)
            {
                SubmitBatchedRequests();
            }
        }

        private void SubmitBatchedRequests()
        {
            // Process up to 20 requests per frame for performance
            int processedCount = 0;
            const int MAX_REQUESTS_PER_FRAME = 20;

            while (_batchedRequests.Count > 0 && processedCount < MAX_REQUESTS_PER_FRAME)
            {
                var request = _batchedRequests.Dequeue();

                // Submit to legacy pathfinding system
                var pathRequest = new PathfindingRequest
                {
                    start = request.start,
                    destination = request.destination,
                    mode = request.mode,
                    callback = (path) => OnPathCalculated(request.entity, path)
                };

                _legacyPathfindingSystem.RequestPath(pathRequest);
                processedCount++;
            }
        }

        private void OnPathCalculated(Entity entity, Vector3[] path)
        {
            if (path != null && path.Length > 0)
            {
                var pathResult = new PathResult
                {
                    entity = entity,
                    nodes = new NativeArray<float3>(path.Length, Allocator.Persistent),
                    isValid = true
                };

                // Convert Vector3[] to float3[]
                for (int i = 0; i < path.Length; i++)
                {
                    pathResult.nodes[i] = path[i];
                }

                _completedPaths.TryAdd(entity, pathResult);
            }
            else
            {
                // Path failed
                var failedResult = new PathResult
                {
                    entity = entity,
                    nodes = new NativeArray<float3>(0, Allocator.Persistent),
                    isValid = false
                };

                _completedPaths.TryAdd(entity, failedResult);
            }
        }

        private void UpdateSpatialHash()
        {
            _spatialHashMap.Clear();

            var spatialHashJob = new SpatialHashJob
            {
                spatialHash = _spatialHashMap.AsParallelWriter(),
                cellSize = SPATIAL_CELL_SIZE
            };

            Dependency = spatialHashJob.ScheduleParallel(_activePathfindingQuery, Dependency);
        }

        private void UpdateActivePathfinding(float deltaTime, float currentTime)
        {
            var updateJob = new PathfindingUpdateJob
            {
                deltaTime = deltaTime,
                currentTime = currentTime,
                spatialHash = _spatialHashMap
            };

            Dependency = updateJob.ScheduleParallel(_activePathfindingQuery, Dependency);
        }

        private void ApplyCompletedPaths()
        {
            // Apply completed paths to entities
            foreach (var kvp in _completedPaths)
            {
                var entity = kvp.Key;
                var pathResult = kvp.Value;

                if (EntityManager.Exists(entity) && EntityManager.HasComponent<PathfindingComponent>(entity))
                {
                    var pathfinding = EntityManager.GetComponentData<PathfindingComponent>(entity);

                    if (pathResult.isValid)
                    {
                        // Add path nodes to entity
                        var pathBuffer = EntityManager.GetBuffer<PathNodeComponent>(entity);
                        pathBuffer.Clear();

                        for (int i = 0; i < pathResult.nodes.Length; i++)
                        {
                            pathBuffer.Add(new PathNodeComponent
                            {
                                position = pathResult.nodes[i],
                                direction = i < pathResult.nodes.Length - 1
                                    ? math.normalize(pathResult.nodes[i + 1] - pathResult.nodes[i])
                                    : float3.zero,
                                distanceToNext = i < pathResult.nodes.Length - 1
                                    ? math.distance(pathResult.nodes[i], pathResult.nodes[i + 1])
                                    : 0f
                            });
                        }

                        pathfinding.status = PathfindingStatus.Ready;
                        pathfinding.pathNodeIndex = 0;
                        pathfinding.pathLength = CalculatePathLength(pathResult.nodes);
                    }
                    else
                    {
                        pathfinding.status = PathfindingStatus.Failed;
                    }

                    EntityManager.SetComponentData(entity, pathfinding);
                }

                // Dispose the native array
                if (pathResult.nodes.IsCreated)
                    pathResult.nodes.Dispose();
            }

            _completedPaths.Clear();
        }

        private void ExecutePathfollowing(float deltaTime)
        {
            var pathfollowingJob = new PathfollowingJob
            {
                deltaTime = deltaTime
            };

            Dependency = pathfollowingJob.ScheduleParallel(_activePathfindingQuery, Dependency);
        }

        private float CalculatePathLength(NativeArray<float3> nodes)
        {
            float totalLength = 0f;
            for (int i = 0; i < nodes.Length - 1; i++)
            {
                totalLength += math.distance(nodes[i], nodes[i + 1]);
            }
            return totalLength;
        }

        // Public API for requesting pathfinding
        public void RequestPath(Entity entity, float3 start, float3 destination,
                               PathfindingMode mode = PathfindingMode.Auto, float urgency = 1f)
        {
            var request = new PathfindingRequestComponent
            {
                start = start,
                destination = destination,
                mode = mode,
                urgency = urgency,
                requestTime = (float)SystemAPI.Time.ElapsedTime,
                requester = entity
            };

            EntityManager.AddComponentData(entity, request);
        }

        public bool HasPath(Entity entity)
        {
            if (EntityManager.HasComponent<PathfindingComponent>(entity))
            {
                var pathfinding = EntityManager.GetComponentData<PathfindingComponent>(entity);
                return pathfinding.status == PathfindingStatus.Ready ||
                       pathfinding.status == PathfindingStatus.Following;
            }
            return false;
        }

        public void SetDestination(Entity entity, float3 destination)
        {
            if (EntityManager.HasComponent<PathfindingComponent>(entity))
            {
                var pathfinding = EntityManager.GetComponentData<PathfindingComponent>(entity);
                pathfinding.destination = destination;
                pathfinding.needsRecalculation = true;
                EntityManager.SetComponentData(entity, pathfinding);
            }
        }
    }

    // Supporting Jobs
    [BurstCompile]
    struct SpatialHashJob : IJobEntityBatch
    {
        [WriteOnly] public NativeMultiHashMap<int, Entity>.ParallelWriter spatialHash;
        [ReadOnly] public float cellSize;

        public void Execute(ArchetypeChunk batchInChunk, int batchIndex)
        {
            var entities = batchInChunk.GetNativeArray(GetEntityTypeHandle());
            var transforms = batchInChunk.GetNativeArray(GetComponentTypeHandle<LocalTransform>(true));

            for (int i = 0; i < batchInChunk.Count; i++)
            {
                int spatialKey = CalculateSpatialHash(transforms[i].Position, cellSize);
                spatialHash.Add(spatialKey, entities[i]);
            }
        }

        private static int CalculateSpatialHash(float3 position, float cellSize)
        {
            int3 cell = (int3)math.floor(position / cellSize);
            return (cell.x * 73856093) ^ (cell.y * 19349663) ^ (cell.z * 83492791);
        }
    }

    [BurstCompile]
    struct PathfindingUpdateJob : IJobEntityBatch
    {
        [ReadOnly] public float deltaTime;
        [ReadOnly] public float currentTime;
        [ReadOnly] public NativeMultiHashMap<int, Entity> spatialHash;

        public void Execute(ArchetypeChunk batchInChunk, int batchIndex)
        {
            var pathfindingComponents = batchInChunk.GetNativeArray(GetComponentTypeHandle<PathfindingComponent>(false));
            var transforms = batchInChunk.GetNativeArray(GetComponentTypeHandle<LocalTransform>(true));

            for (int i = 0; i < batchInChunk.Count; i++)
            {
                var pathfinding = pathfindingComponents[i];
                var transform = transforms[i];

                // Check if path needs recalculation
                if (pathfinding.needsRecalculation &&
                    currentTime - pathfinding.lastPathUpdate > pathfinding.recalculationInterval)
                {
                    pathfinding.needsRecalculation = true;
                    pathfinding.status = PathfindingStatus.Requesting;
                    pathfinding.lastPathUpdate = currentTime;
                }

                // Update path following status
                if (pathfinding.status == PathfindingStatus.Ready)
                {
                    pathfinding.status = PathfindingStatus.Following;
                }

                pathfindingComponents[i] = pathfinding;
            }
        }
    }

    [BurstCompile]
    struct PathfollowingJob : IJobEntityBatch
    {
        [ReadOnly] public float deltaTime;

        public void Execute(ArchetypeChunk batchInChunk, int batchIndex)
        {
            var pathfindingComponents = batchInChunk.GetNativeArray(GetComponentTypeHandle<PathfindingComponent>(false));
            var transforms = batchInChunk.GetNativeArray(GetComponentTypeHandle<LocalTransform>(false));
            var pathBuffers = batchInChunk.GetBufferAccessor(GetBufferTypeHandle<PathNodeComponent>(true));

            for (int i = 0; i < batchInChunk.Count; i++)
            {
                var pathfinding = pathfindingComponents[i];
                var transform = transforms[i];
                var pathNodes = pathBuffers[i];

                if (pathfinding.status == PathfindingStatus.Following && pathNodes.Length > 0)
                {
                    // Move towards current path node
                    if (pathfinding.pathNodeIndex < pathNodes.Length)
                    {
                        var targetNode = pathNodes[pathfinding.pathNodeIndex];
                        var direction = math.normalize(targetNode.position - transform.Position);
                        var distanceToTarget = math.distance(transform.Position, targetNode.position);

                        if (distanceToTarget <= pathfinding.stoppingDistance)
                        {
                            // Move to next node
                            pathfinding.pathNodeIndex++;

                            if (pathfinding.pathNodeIndex >= pathNodes.Length)
                            {
                                // Path complete
                                pathfinding.status = PathfindingStatus.Complete;
                            }
                        }
                        else
                        {
                            // Move towards target
                            var moveDistance = pathfinding.movementSpeed * deltaTime;
                            var newPosition = transform.Position + direction * math.min(moveDistance, distanceToTarget);

                            transform.Position = newPosition;
                            transform.Rotation = quaternion.LookRotationSafe(direction, math.up());
                        }
                    }
                }

                pathfindingComponents[i] = pathfinding;
                transforms[i] = transform;
            }
        }
    }

    // Supporting data structures
    struct PathfindingRequestData
    {
        public Entity entity;
        public float3 start;
        public float3 destination;
        public PathfindingMode mode;
        public float urgency;
    }

    struct PathResult
    {
        public Entity entity;
        public NativeArray<float3> nodes;
        public bool isValid;
    }

    // Extension to legacy pathfinding system
    public class PathfindingRequest
    {
        public Vector3 start;
        public Vector3 destination;
        public PathfindingMode mode;
        public System.Action<Vector3[]> callback;
    }
}
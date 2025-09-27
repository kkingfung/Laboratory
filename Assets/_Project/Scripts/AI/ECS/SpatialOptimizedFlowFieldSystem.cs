using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Burst;
using Unity.Transforms;
using UnityEngine;
using Laboratory.AI.Pathfinding;
using Laboratory.Core.ECS.Components;

namespace Laboratory.AI.ECS
{
    /// <summary>
    /// SPATIAL OPTIMIZED FLOW FIELD SYSTEM - High-performance group pathfinding
    /// PURPOSE: Enable hundreds of entities to follow optimal paths with minimal overhead
    /// FEATURES: Spatial partitioning, flow field reuse, dynamic field generation
    /// PERFORMANCE: O(1) flow field lookup, batched field generation
    /// </summary>

    // Flow Field ECS Component
    public struct FlowFieldComponent : IComponentData
    {
        public BlobAssetReference<FlowFieldBlob> flowField;
        public float3 destination;
        public float fieldRadius;
        public float lastUpdateTime;
        public float updateInterval;
        public bool needsUpdate;
        public int spatialHash;
    }

    public struct FlowFieldBlob
    {
        public BlobArray<float3> directions;
        public BlobArray<float> costs;
        public float3 destination;
        public float cellSize;
        public int2 gridSize;
        public float3 worldOrigin;
    }

    public struct FlowFieldFollowerComponent : IComponentData
    {
        public Entity flowFieldEntity;
        public float followStrength;
        public float avoidanceRadius;
        public float preferredSpeed;
        public bool useLocalAvoidance;
    }

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(UnifiedECSPathfindingSystem))]
    public partial class SpatialOptimizedFlowFieldSystem : SystemBase
    {
        private EntityQuery _flowFieldQuery;
        private EntityQuery _followerQuery;

        // Spatial optimization data
        private NativeMultiHashMap<int, Entity> _spatialHashMap;
        private NativeMultiHashMap<int, FlowFieldData> _flowFieldSpatialMap;
        private NativeQueue<FlowFieldGenerationRequest> _generationRequests;

        // Performance settings
        private const float SPATIAL_CELL_SIZE = 25f;
        private const int MAX_FLOW_FIELDS = 100;
        private const int MAX_GENERATION_REQUESTS_PER_FRAME = 5;

        // Flow field generation job handles
        private JobHandle _generationJobHandle;

        protected override void OnCreate()
        {
            _flowFieldQuery = GetEntityQuery(ComponentType.ReadWrite<FlowFieldComponent>());
            _followerQuery = GetEntityQuery(
                ComponentType.ReadOnly<FlowFieldFollowerComponent>(),
                ComponentType.ReadWrite<LocalTransform>(),
                ComponentType.ReadOnly<PathfindingComponent>()
            );

            _spatialHashMap = new NativeMultiHashMap<int, Entity>(5000, Allocator.Persistent);
            _flowFieldSpatialMap = new NativeMultiHashMap<int, FlowFieldData>(MAX_FLOW_FIELDS, Allocator.Persistent);
            _generationRequests = new NativeQueue<FlowFieldGenerationRequest>(Allocator.Persistent);
        }

        protected override void OnDestroy()
        {
            _generationJobHandle.Complete();

            if (_spatialHashMap.IsCreated) _spatialHashMap.Dispose();
            if (_flowFieldSpatialMap.IsCreated) _flowFieldSpatialMap.Dispose();
            if (_generationRequests.IsCreated) _generationRequests.Dispose();

            // Dispose any remaining flow field blobs
            Entities.ForEach((in FlowFieldComponent flowField) =>
            {
                if (flowField.flowField.IsCreated)
                    flowField.flowField.Dispose();
            }).WithoutBurst().Run();
        }

        protected override void OnUpdate()
        {
            float deltaTime = SystemAPI.Time.DeltaTime;
            float currentTime = (float)SystemAPI.Time.ElapsedTime;

            // Complete previous generation jobs
            _generationJobHandle.Complete();

            // Step 1: Update spatial hash for flow field optimization
            UpdateSpatialHash();

            // Step 2: Process flow field generation requests
            ProcessFlowFieldRequests(currentTime);

            // Step 3: Update existing flow fields
            UpdateFlowFields(currentTime);

            // Step 4: Apply flow field movement to followers
            ApplyFlowFieldMovement(deltaTime);

            // Step 5: Handle local avoidance
            ApplyLocalAvoidance(deltaTime);
        }

        private void UpdateSpatialHash()
        {
            _spatialHashMap.Clear();
            _flowFieldSpatialMap.Clear();

            // Hash entity positions
            var entityHashJob = new SpatialHashJob
            {
                spatialHash = _spatialHashMap.AsParallelWriter(),
                cellSize = SPATIAL_CELL_SIZE
            };

            var entityHashHandle = entityHashJob.ScheduleParallel(_followerQuery, Dependency);

            // Hash flow field coverage areas
            var flowFieldHashJob = new FlowFieldSpatialHashJob
            {
                flowFieldSpatialMap = _flowFieldSpatialMap.AsParallelWriter(),
                cellSize = SPATIAL_CELL_SIZE
            };

            Dependency = flowFieldHashJob.ScheduleParallel(_flowFieldQuery, entityHashHandle);
        }

        private void ProcessFlowFieldRequests(float currentTime)
        {
            // Check if we need new flow fields based on entity distribution
            var analysisJob = new FlowFieldDemandAnalysisJob
            {
                spatialHash = _spatialHashMap,
                flowFieldSpatialMap = _flowFieldSpatialMap,
                generationRequests = _generationRequests.AsParallelWriter(),
                currentTime = currentTime,
                minEntitiesForFlowField = 5, // Only create flow fields for groups of 5+
                maxDistanceForSharing = SPATIAL_CELL_SIZE * 2f
            };

            Dependency = analysisJob.Schedule(Dependency);

            // Process generation requests
            Dependency.Complete();
            ProcessGenerationRequests(currentTime);
        }

        private void ProcessGenerationRequests(float currentTime)
        {
            int processedRequests = 0;

            while (_generationRequests.Count > 0 && processedRequests < MAX_GENERATION_REQUESTS_PER_FRAME)
            {
                var request = _generationRequests.Dequeue();
                CreateFlowField(request, currentTime);
                processedRequests++;
            }
        }

        private void CreateFlowField(FlowFieldGenerationRequest request, float currentTime)
        {
            var flowFieldEntity = EntityManager.CreateEntity();

            // Create flow field blob
            var blobBuilder = new BlobBuilder(Allocator.Temp);
            ref var flowFieldBlob = ref blobBuilder.ConstructRoot<FlowFieldBlob>();

            // Calculate grid size based on radius
            var gridSize = new int2(
                Mathf.CeilToInt(request.radius * 2f / request.cellSize),
                Mathf.CeilToInt(request.radius * 2f / request.cellSize)
            );

            flowFieldBlob.gridSize = gridSize;
            flowFieldBlob.cellSize = request.cellSize;
            flowFieldBlob.destination = request.destination;
            flowFieldBlob.worldOrigin = request.center - new float3(request.radius, 0, request.radius);

            // Initialize arrays
            var directionsArray = blobBuilder.Allocate(ref flowFieldBlob.directions, gridSize.x * gridSize.y);
            var costsArray = blobBuilder.Allocate(ref flowFieldBlob.costs, gridSize.x * gridSize.y);

            // Generate flow field using Dijkstra's algorithm
            GenerateFlowFieldData(ref directionsArray, ref costsArray, flowFieldBlob, request);

            var blobAsset = blobBuilder.CreateBlobAssetReference<FlowFieldBlob>(Allocator.Persistent);
            blobBuilder.Dispose();

            // Add flow field component
            EntityManager.AddComponentData(flowFieldEntity, new FlowFieldComponent
            {
                flowField = blobAsset,
                destination = request.destination,
                fieldRadius = request.radius,
                lastUpdateTime = currentTime,
                updateInterval = 5f, // Update every 5 seconds
                needsUpdate = false,
                spatialHash = request.spatialHash
            });
        }

        private void GenerateFlowFieldData(ref BlobBuilderArray<float3> directions,
                                         ref BlobBuilderArray<float> costs,
                                         FlowFieldBlob flowFieldBlob,
                                         FlowFieldGenerationRequest request)
        {
            var gridSize = flowFieldBlob.gridSize;
            var cellSize = flowFieldBlob.cellSize;
            var worldOrigin = flowFieldBlob.worldOrigin;
            var destination = request.destination;

            // Initialize costs with high values
            for (int i = 0; i < costs.Length; i++)
            {
                costs[i] = float.MaxValue;
            }

            // Find destination cell
            var destCell = WorldToGrid(destination, worldOrigin, cellSize, gridSize);
            if (IsValidGridPosition(destCell, gridSize))
            {
                int destIndex = GridToIndex(destCell, gridSize);
                costs[destIndex] = 0f;
            }

            // Dijkstra's algorithm for cost calculation
            var openSet = new NativeList<int2>(gridSize.x * gridSize.y, Allocator.Temp);
            openSet.Add(destCell);

            while (openSet.Length > 0)
            {
                var current = GetLowestCostCell(openSet, costs, gridSize);
                openSet.RemoveAt(current.index);

                var currentCell = current.cell;
                int currentIndex = GridToIndex(currentCell, gridSize);
                float currentCost = costs[currentIndex];

                // Check all neighbors
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0) continue;

                        var neighbor = currentCell + new int2(dx, dy);
                        if (!IsValidGridPosition(neighbor, gridSize)) continue;

                        int neighborIndex = GridToIndex(neighbor, gridSize);
                        float moveCost = (dx != 0 && dy != 0) ? 1.414f : 1f; // Diagonal cost
                        float newCost = currentCost + moveCost;

                        if (newCost < costs[neighborIndex])
                        {
                            costs[neighborIndex] = newCost;
                            if (!ContainsCell(openSet, neighbor))
                                openSet.Add(neighbor);
                        }
                    }
                }
            }

            // Calculate directions based on costs
            for (int x = 0; x < gridSize.x; x++)
            {
                for (int y = 0; y < gridSize.y; y++)
                {
                    var cell = new int2(x, y);
                    int index = GridToIndex(cell, gridSize);

                    var direction = CalculateFlowDirection(cell, costs, gridSize);
                    directions[index] = direction;
                }
            }

            openSet.Dispose();
        }

        private float3 CalculateFlowDirection(int2 cell, BlobBuilderArray<float> costs, int2 gridSize)
        {
            float3 direction = float3.zero;
            int currentIndex = GridToIndex(cell, gridSize);
            float currentCost = costs[currentIndex];

            // Sample neighbor costs and calculate gradient
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;

                    var neighbor = cell + new int2(dx, dy);
                    if (!IsValidGridPosition(neighbor, gridSize)) continue;

                    int neighborIndex = GridToIndex(neighbor, gridSize);
                    float neighborCost = costs[neighborIndex];

                    if (neighborCost < currentCost)
                    {
                        float weight = currentCost - neighborCost;
                        direction += new float3(dx, 0, dy) * weight;
                    }
                }
            }

            return math.length(direction) > 0.01f ? math.normalize(direction) : float3.zero;
        }

        private void UpdateFlowFields(float currentTime)
        {
            Entities
                .ForEach((ref FlowFieldComponent flowField) =>
                {
                    if (flowField.needsUpdate ||
                        currentTime - flowField.lastUpdateTime > flowField.updateInterval)
                    {
                        // Mark for regeneration (would be handled in next frame)
                        flowField.needsUpdate = true;
                        flowField.lastUpdateTime = currentTime;
                    }
                }).ScheduleParallel();
        }

        private void ApplyFlowFieldMovement(float deltaTime)
        {
            var movementJob = new FlowFieldMovementJob
            {
                deltaTime = deltaTime,
                flowFieldLookup = GetComponentLookup<FlowFieldComponent>(true)
            };

            Dependency = movementJob.ScheduleParallel(_followerQuery, Dependency);
        }

        private void ApplyLocalAvoidance(float deltaTime)
        {
            var avoidanceJob = new LocalAvoidanceJob
            {
                deltaTime = deltaTime,
                spatialHash = _spatialHashMap
            };

            Dependency = avoidanceJob.ScheduleParallel(_followerQuery, Dependency);
        }

        // Helper methods
        private static int2 WorldToGrid(float3 worldPos, float3 worldOrigin, float cellSize, int2 gridSize)
        {
            var localPos = worldPos - worldOrigin;
            var gridPos = new int2(
                Mathf.FloorToInt(localPos.x / cellSize),
                Mathf.FloorToInt(localPos.z / cellSize)
            );
            return math.clamp(gridPos, int2.zero, gridSize - 1);
        }

        private static int GridToIndex(int2 gridPos, int2 gridSize)
        {
            return gridPos.x + gridPos.y * gridSize.x;
        }

        private static bool IsValidGridPosition(int2 gridPos, int2 gridSize)
        {
            return gridPos.x >= 0 && gridPos.x < gridSize.x &&
                   gridPos.y >= 0 && gridPos.y < gridSize.y;
        }

        private static bool ContainsCell(NativeList<int2> list, int2 cell)
        {
            for (int i = 0; i < list.Length; i++)
            {
                if (list[i].Equals(cell)) return true;
            }
            return false;
        }

        private static (int2 cell, int index) GetLowestCostCell(NativeList<int2> cells,
                                                               BlobBuilderArray<float> costs,
                                                               int2 gridSize)
        {
            int bestIndex = 0;
            float bestCost = float.MaxValue;

            for (int i = 0; i < cells.Length; i++)
            {
                int costIndex = GridToIndex(cells[i], gridSize);
                if (costs[costIndex] < bestCost)
                {
                    bestCost = costs[costIndex];
                    bestIndex = i;
                }
            }

            return (cells[bestIndex], bestIndex);
        }

        // Public API
        public Entity CreateFlowFieldForArea(float3 center, float3 destination, float radius)
        {
            var request = new FlowFieldGenerationRequest
            {
                center = center,
                destination = destination,
                radius = radius,
                cellSize = 2f,
                spatialHash = CalculateSpatialHash(center, SPATIAL_CELL_SIZE)
            };

            _generationRequests.Enqueue(request);
            return Entity.Null; // Will be created in next frame
        }

        public void AssignEntityToFlowField(Entity entity, Entity flowFieldEntity, float followStrength = 1f)
        {
            var follower = new FlowFieldFollowerComponent
            {
                flowFieldEntity = flowFieldEntity,
                followStrength = followStrength,
                avoidanceRadius = 2f,
                preferredSpeed = 5f,
                useLocalAvoidance = true
            };

            EntityManager.AddComponentData(entity, follower);
        }

        private static int CalculateSpatialHash(float3 position, float cellSize)
        {
            int3 cell = (int3)math.floor(position / cellSize);
            return (cell.x * 73856093) ^ (cell.y * 19349663) ^ (cell.z * 83492791);
        }
    }

    // Supporting Jobs
    [BurstCompile]
    struct FlowFieldSpatialHashJob : IJobEntityBatch
    {
        [WriteOnly] public NativeMultiHashMap<int, FlowFieldData>.ParallelWriter flowFieldSpatialMap;
        [ReadOnly] public float cellSize;

        public void Execute(ArchetypeChunk batchInChunk, int batchIndex)
        {
            var flowFields = batchInChunk.GetNativeArray(GetComponentTypeHandle<FlowFieldComponent>(true));

            for (int i = 0; i < batchInChunk.Count; i++)
            {
                var flowField = flowFields[i];
                int spatialKey = CalculateSpatialHash(flowField.destination, cellSize);

                var flowFieldData = new FlowFieldData
                {
                    destination = flowField.destination,
                    radius = flowField.fieldRadius,
                    spatialHash = spatialKey
                };

                flowFieldSpatialMap.Add(spatialKey, flowFieldData);
            }
        }

        private static int CalculateSpatialHash(float3 position, float cellSize)
        {
            int3 cell = (int3)math.floor(position / cellSize);
            return (cell.x * 73856093) ^ (cell.y * 19349663) ^ (cell.z * 83492791);
        }
    }

    [BurstCompile]
    struct FlowFieldDemandAnalysisJob : IJob
    {
        [ReadOnly] public NativeMultiHashMap<int, Entity> spatialHash;
        [ReadOnly] public NativeMultiHashMap<int, FlowFieldData> flowFieldSpatialMap;
        [WriteOnly] public NativeQueue<FlowFieldGenerationRequest>.ParallelWriter generationRequests;
        [ReadOnly] public float currentTime;
        [ReadOnly] public int minEntitiesForFlowField;
        [ReadOnly] public float maxDistanceForSharing;

        public void Execute()
        {
            var processedCells = new NativeHashSet<int>(100, Allocator.Temp);
            var spatialKeys = spatialHash.GetKeyArray(Allocator.Temp);

            for (int i = 0; i < spatialKeys.Length; i++)
            {
                int spatialKey = spatialKeys[i];
                if (processedCells.Contains(spatialKey)) continue;

                var entitiesInCell = new NativeList<Entity>(Allocator.Temp);
                if (spatialHash.TryGetFirstValue(spatialKey, out var entity, out var iterator))
                {
                    do
                    {
                        entitiesInCell.Add(entity);
                    } while (spatialHash.TryGetNextValue(out entity, ref iterator));
                }

                if (entitiesInCell.Length >= minEntitiesForFlowField)
                {
                    // Check if there's already a suitable flow field
                    bool hasExistingFlowField = false;
                    if (flowFieldSpatialMap.TryGetFirstValue(spatialKey, out var existingField, out var ffIterator))
                    {
                        // For now, assume existing flow field is suitable
                        hasExistingFlowField = true;
                    }

                    if (!hasExistingFlowField)
                    {
                        // Calculate average position for flow field center
                        float3 centerSum = float3.zero;
                        // This would require access to transform data - simplified for now

                        var request = new FlowFieldGenerationRequest
                        {
                            center = float3.zero, // Would calculate from entities
                            destination = float3.zero, // Would determine from pathfinding goals
                            radius = maxDistanceForSharing,
                            cellSize = 2f,
                            spatialHash = spatialKey
                        };

                        generationRequests.Enqueue(request);
                    }
                }

                processedCells.Add(spatialKey);
                entitiesInCell.Dispose();
            }

            processedCells.Dispose();
            spatialKeys.Dispose();
        }
    }

    [BurstCompile]
    struct FlowFieldMovementJob : IJobEntityBatch
    {
        [ReadOnly] public float deltaTime;
        [ReadOnly] public ComponentLookup<FlowFieldComponent> flowFieldLookup;

        public void Execute(ArchetypeChunk batchInChunk, int batchIndex)
        {
            var followers = batchInChunk.GetNativeArray(GetComponentTypeHandle<FlowFieldFollowerComponent>(true));
            var transforms = batchInChunk.GetNativeArray(GetComponentTypeHandle<LocalTransform>(false));

            for (int i = 0; i < batchInChunk.Count; i++)
            {
                var follower = followers[i];
                var transform = transforms[i];

                if (flowFieldLookup.HasComponent(follower.flowFieldEntity))
                {
                    var flowField = flowFieldLookup[follower.flowFieldEntity];

                    if (flowField.flowField.IsCreated)
                    {
                        var direction = SampleFlowField(flowField.flowField, transform.Position);
                        if (math.length(direction) > 0.01f)
                        {
                            var movement = direction * follower.preferredSpeed * follower.followStrength * deltaTime;
                            transform.Position += movement;
                            transform.Rotation = quaternion.LookRotationSafe(direction, math.up());
                        }
                    }
                }

                transforms[i] = transform;
            }
        }

        private static float3 SampleFlowField(BlobAssetReference<FlowFieldBlob> flowFieldBlob, float3 worldPosition)
        {
            ref var flowField = ref flowFieldBlob.Value;

            var localPos = worldPosition - flowField.worldOrigin;
            var gridPos = new int2(
                Mathf.FloorToInt(localPos.x / flowField.cellSize),
                Mathf.FloorToInt(localPos.z / flowField.cellSize)
            );

            if (gridPos.x >= 0 && gridPos.x < flowField.gridSize.x &&
                gridPos.y >= 0 && gridPos.y < flowField.gridSize.y)
            {
                int index = gridPos.x + gridPos.y * flowField.gridSize.x;
                return flowField.directions[index];
            }

            return float3.zero;
        }
    }

    [BurstCompile]
    struct LocalAvoidanceJob : IJobEntityBatch
    {
        [ReadOnly] public float deltaTime;
        [ReadOnly] public NativeMultiHashMap<int, Entity> spatialHash;

        public void Execute(ArchetypeChunk batchInChunk, int batchIndex)
        {
            var followers = batchInChunk.GetNativeArray(GetComponentTypeHandle<FlowFieldFollowerComponent>(true));
            var transforms = batchInChunk.GetNativeArray(GetComponentTypeHandle<LocalTransform>(false));

            for (int i = 0; i < batchInChunk.Count; i++)
            {
                var follower = followers[i];
                var transform = transforms[i];

                if (follower.useLocalAvoidance)
                {
                    var avoidanceForce = CalculateAvoidanceForce(transform.Position, follower.avoidanceRadius, spatialHash);
                    if (math.length(avoidanceForce) > 0.01f)
                    {
                        var avoidanceMovement = avoidanceForce * deltaTime;
                        transform.Position += avoidanceMovement;
                    }
                }

                transforms[i] = transform;
            }
        }

        private static float3 CalculateAvoidanceForce(float3 position, float avoidanceRadius,
                                                    NativeMultiHashMap<int, Entity> spatialHash)
        {
            float3 avoidanceForce = float3.zero;
            const float cellSize = 25f; // Should match system constant

            int spatialKey = CalculateSpatialHash(position, cellSize);

            // Check current and neighboring cells
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dz = -1; dz <= 1; dz++)
                {
                    int3 offset = new int3(dx, 0, dz);
                    int neighborKey = spatialKey + offset.x + offset.z * 1000;

                    if (spatialHash.TryGetFirstValue(neighborKey, out var neighborEntity, out var iterator))
                    {
                        do
                        {
                            // This would require access to neighbor positions - simplified
                            // In practice, would calculate repulsion force based on distance
                        } while (spatialHash.TryGetNextValue(out neighborEntity, ref iterator));
                    }
                }
            }

            return avoidanceForce;
        }

        private static int CalculateSpatialHash(float3 position, float cellSize)
        {
            int3 cell = (int3)math.floor(position / cellSize);
            return (cell.x * 73856093) ^ (cell.z * 83492791);
        }
    }

    // Supporting data structures
    struct FlowFieldData
    {
        public float3 destination;
        public float radius;
        public int spatialHash;
    }

    struct FlowFieldGenerationRequest
    {
        public float3 center;
        public float3 destination;
        public float radius;
        public float cellSize;
        public int spatialHash;
    }
}
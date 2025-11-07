using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Transforms;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using UnityEngine;
using UnityEngine.Profiling;
using Laboratory.AI.Pathfinding;
using Laboratory.Core.Configuration;
using Laboratory.Core.Performance;

namespace Laboratory.AI.ECS
{
    /// <summary>
    /// FLOW FIELD SYSTEM - High-Performance Pathfinding
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

        // LOD system
        public PathfindingLOD currentLOD;
        public float distanceToPlayer;
        public float lastLODUpdateTime;
    }

    /// <summary>
    /// Level of Detail for pathfinding optimization
    /// </summary>
    public enum PathfindingLOD : byte
    {
        High = 0,    // 60 FPS - Close to player, detailed pathfinding
        Medium = 1,  // 30 FPS - Medium distance, reduced precision
        Low = 2,     // 15 FPS - Far from player, coarse pathfinding
        Minimal = 3  // 5 FPS - Very far, minimal updates
    }

    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(Unity.Transforms.TransformSystemGroup))]
    public partial class FlowFieldSystem : SystemBase
    {
        private EntityQuery _flowFieldQuery;
        private EntityQuery _followerQuery;

        // Spatial optimization data
        private NativeParallelMultiHashMap<int, Entity> _spatialHashMap;
        private NativeParallelMultiHashMap<int, FlowFieldData> _flowFieldSpatialMap;
        private NativeQueue<FlowFieldGenerationRequest> _generationRequests;

        // Performance settings (Burst-compatible)
        private BurstCompatibleConfigs.PerformanceConfigData _performanceConfig;

        // Flow field generation job handles
        private JobHandle _generationJobHandle;

        // NativeArray pooling system for memory optimization
        private NativeArrayPool<int2> _int2ArrayPool;
        private NativeArrayPool<float> _floatArrayPool;
        private NativeArrayPool<float3> _float3ArrayPool;

        // Flow field caching for performance
        private NativeHashMap<uint, Entity> _flowFieldCache; // Hash -> FlowField Entity
        private const float CACHE_GRID_TOLERANCE = 0.5f;

        protected override void OnCreate()
        {
            LoadConfiguration();

            _flowFieldQuery = GetEntityQuery(ComponentType.ReadWrite<FlowFieldComponent>());
            _followerQuery = GetEntityQuery(
                ComponentType.ReadOnly<FlowFieldFollowerComponent>(),
                ComponentType.ReadWrite<LocalTransform>()
            );

            _spatialHashMap = new NativeParallelMultiHashMap<int, Entity>(5000, Allocator.Persistent);
            _flowFieldSpatialMap = new NativeParallelMultiHashMap<int, FlowFieldData>(_performanceConfig.maxFlowFields, Allocator.Persistent);
            _generationRequests = new NativeQueue<FlowFieldGenerationRequest>(Allocator.Persistent);
            _flowFieldCache = new NativeHashMap<uint, Entity>(_performanceConfig.maxFlowFields, Allocator.Persistent);

            // Initialize NativeArray pools for zero-allocation pathfinding
            _int2ArrayPool = new NativeArrayPool<int2>(Allocator.Persistent, 50);
            _floatArrayPool = new NativeArrayPool<float>(Allocator.Persistent, 50);
            _float3ArrayPool = new NativeArrayPool<float3>(Allocator.Persistent, 50);
        }

        private void LoadConfiguration()
        {
            try
            {
                var config = Config.Performance;
                _performanceConfig = BurstCompatibleConfigs.PerformanceConfigData.ExtractGlobal(config);
            }
            catch (System.Exception)
            {
                // Use defaults if configuration fails
                _performanceConfig = BurstCompatibleConfigs.PerformanceConfigData.CreateDefault();
            }
        }

        protected override void OnDestroy()
        {
            _generationJobHandle.Complete();

            if (_spatialHashMap.IsCreated) _spatialHashMap.Dispose();
            if (_flowFieldSpatialMap.IsCreated) _flowFieldSpatialMap.Dispose();
            if (_generationRequests.IsCreated) _generationRequests.Dispose();
            if (_flowFieldCache.IsCreated) _flowFieldCache.Dispose();

            // Dispose NativeArray pools
            _int2ArrayPool?.Dispose();
            _floatArrayPool?.Dispose();
            _float3ArrayPool?.Dispose();

            // Dispose any remaining flow field blobs
            Entities.ForEach((in FlowFieldComponent flowField) =>
            {
                if (flowField.flowField.IsCreated)
                    flowField.flowField.Dispose();
            }).WithoutBurst().Run();
        }

        protected override void OnUpdate()
        {
            Profiler.BeginSample("FlowFieldSystem.OnUpdate");

            float deltaTime = SystemAPI.Time.DeltaTime;
            float currentTime = (float)SystemAPI.Time.ElapsedTime;

            // Complete previous generation jobs
            Profiler.BeginSample("FlowFieldSystem.CompleteJobs");
            _generationJobHandle.Complete();
            Profiler.EndSample();

            // Step 1: Update spatial hash for flow field optimization
            Profiler.BeginSample("FlowFieldSystem.UpdateSpatialHash");
            UpdateSpatialHash();
            Profiler.EndSample();

            // Step 2: Process flow field generation requests
            Profiler.BeginSample("FlowFieldSystem.ProcessRequests");
            ProcessFlowFieldRequests(currentTime);
            Profiler.EndSample();

            // Step 3: Update existing flow fields
            Profiler.BeginSample("FlowFieldSystem.UpdateFlowFields");
            UpdateFlowFields(currentTime);
            Profiler.EndSample();

            // Step 4: Apply flow field movement to followers
            Profiler.BeginSample("FlowFieldSystem.ApplyMovement");
            ApplyFlowFieldMovement(deltaTime);
            Profiler.EndSample();

            // Step 5: Handle local avoidance
            Profiler.BeginSample("FlowFieldSystem.LocalAvoidance");
            ApplyLocalAvoidance(deltaTime);
            Profiler.EndSample();

            Profiler.EndSample();
        }

        /// <summary>
        /// Optimized spatial hashing job
        /// </summary>
        [BurstCompile]
        private struct SpatialHashingJob : IJobChunk
        {
            public NativeParallelMultiHashMap<int, Entity>.ParallelWriter spatialHashMap;
            [ReadOnly] public ComponentTypeHandle<LocalTransform> transformTypeHandle;
            [ReadOnly] public EntityTypeHandle entityTypeHandle;
            [ReadOnly] public float spatialCellSize;

            [BurstCompile]
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in Unity.Burst.Intrinsics.v128 chunkEnabledMask)
            {
                var transforms = chunk.GetNativeArray(ref transformTypeHandle);
                var entities = chunk.GetNativeArray(entityTypeHandle);

                // Pre-calculate inverse cell size for optimization
                float invCellSize = 1f / spatialCellSize;

                for (int i = 0; i < chunk.Count; i++)
                {
                    var position = transforms[i].Position;
                    var entity = entities[i];

                    // Optimized spatial hash calculation
                    int3 cell = (int3)(position * invCellSize);
                    int spatialKey = (cell.x * 73856093) ^ (cell.y * 19349663) ^ (cell.z * 83492791);
                    spatialHashMap.Add(spatialKey, entity);
                }
            }
        }

        private void UpdateSpatialHash()
        {
            _spatialHashMap.Clear();
            _flowFieldSpatialMap.Clear();

            // Use optimized job for follower spatial hashing
            var spatialHashingJob = new SpatialHashingJob
            {
                spatialHashMap = _spatialHashMap.AsParallelWriter(),
                transformTypeHandle = GetComponentTypeHandle<LocalTransform>(true),
                entityTypeHandle = GetEntityTypeHandle(),
                spatialCellSize = _performanceConfig.spatialHashCellSize
            };

            var followerQuery = GetEntityQuery(ComponentType.ReadOnly<LocalTransform>(), ComponentType.ReadOnly<FlowFieldFollowerComponent>());
            this.Dependency = spatialHashingJob.ScheduleParallel(followerQuery, this.Dependency);

            // Simple flow field spatial hashing (less entities, so keep simple)
            var flowFieldSpatialMap = _flowFieldSpatialMap;
            var spatialCellSize = _performanceConfig.spatialHashCellSize;

            Entities
                .WithAll<FlowFieldComponent>()
                .ForEach((in FlowFieldComponent flowField) =>
                {
                    // Optimized spatial hash calculation with inverse multiplication
                    float3 normalizedPos = flowField.destination / spatialCellSize;
                    int3 cell = (int3)normalizedPos;
                    int spatialKey = (cell.x * 73856093) ^ (cell.y * 19349663) ^ (cell.z * 83492791);
                    var flowFieldData = new FlowFieldData
                    {
                        destination = flowField.destination,
                        radius = flowField.fieldRadius,
                        spatialHash = spatialKey
                    };
                    flowFieldSpatialMap.Add(spatialKey, flowFieldData);
                }).ScheduleParallel();
        }

        private void ProcessFlowFieldRequests(float currentTime)
        {
            // Simplified flow field generation - process requests directly
            ProcessGenerationRequests(currentTime);
        }

        private void ProcessGenerationRequests(float currentTime)
        {
            int processedRequests = 0;

            while (_generationRequests.Count > 0 && processedRequests < _performanceConfig.maxFlowFieldRequestsPerFrame)
            {
                var request = _generationRequests.Dequeue();
                CreateFlowField(request, currentTime);
                processedRequests++;
            }
        }

        private void CreateFlowField(FlowFieldGenerationRequest request, float currentTime)
        {
            // Generate cache key for this flow field request
            uint cacheKey = GenerateFlowFieldCacheKey(request.center, request.destination, request.radius);

            // Check if we already have a similar flow field cached
            if (_flowFieldCache.TryGetValue(cacheKey, out Entity cachedEntity) && EntityManager.Exists(cachedEntity))
            {
                // Reuse existing flow field - just update timestamp
                var cachedFlowField = EntityManager.GetComponentData<FlowFieldComponent>(cachedEntity);
                cachedFlowField.lastUpdateTime = currentTime;
                EntityManager.SetComponentData(cachedEntity, cachedFlowField);
                return;
            }

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
            GenerateFlowFieldData(ref directionsArray, ref costsArray, ref flowFieldBlob, request);

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

            // Cache the flow field for reuse
            _flowFieldCache.TryAdd(cacheKey, flowFieldEntity);
        }

        /// <summary>
        /// Job for parallel flow field generation using Dijkstra's algorithm
        /// </summary>
        [BurstCompile]
        private struct FlowFieldGenerationJob : IJob
        {
            public NativeArray<float3> directions;
            public NativeArray<float> costs;
            [ReadOnly] public int2 gridSize;
            [ReadOnly] public float cellSize;
            [ReadOnly] public float3 worldOrigin;
            [ReadOnly] public float3 destination;
            [ReadOnly] public NativeArray<int2> tempOpenSet;

            [BurstCompile]
            public void Execute()
            {
                // Initialize costs with high values
                for (int i = 0; i < costs.Length; i++)
                {
                    costs[i] = float.MaxValue;
                }

                // Find destination cell
                var destCell = WorldToGrid(destination, worldOrigin, cellSize, gridSize);
                if (!IsValidGridPosition(destCell, gridSize))
                    return;

                int destIndex = GridToIndex(destCell, gridSize);
                costs[destIndex] = 0f;

                // Use native array for open set (passed from main thread pool)
                var openSet = tempOpenSet;
                int openSetCount = 0;

                // Initialize with destination
                openSet[openSetCount++] = destCell;

                // Dijkstra's algorithm
                while (openSetCount > 0)
                {
                    var current = GetLowestCostCell(openSet, openSetCount, costs, gridSize);
                    // Remove by moving last element to current position
                    openSet[current.index] = openSet[--openSetCount];

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
                            float moveCost = (dx != 0 && dy != 0) ? 1.414f : 1f;
                            float newCost = currentCost + moveCost;

                            if (newCost < costs[neighborIndex])
                            {
                                costs[neighborIndex] = newCost;
                                if (!ContainsCell(openSet, openSetCount, neighbor))
                                {
                                    openSet[openSetCount++] = neighbor;
                                }
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
                        var direction = CalculateFlowDirection(cell, gridSize);
                        directions[index] = direction;
                    }
                }
            }

            private float3 CalculateFlowDirection(int2 cell, int2 gridSize)
            {
                float3 direction = new float3(0, 0, 0);
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

                return math.length(direction) > 0.01f ? math.normalize(direction) : new float3(0, 0, 0);
            }

            private static int2 WorldToGrid(float3 worldPos, float3 worldOrigin, float cellSize, int2 gridSize)
            {
                var localPos = worldPos - worldOrigin;
                var gridPos = new int2(
                    Mathf.FloorToInt(localPos.x / cellSize),
                    Mathf.FloorToInt(localPos.z / cellSize)
                );
                return math.clamp(gridPos, new int2(0, 0), gridSize - 1);
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

            private static bool ContainsCell(NativeArray<int2> array, int count, int2 cell)
            {
                for (int i = 0; i < count; i++)
                {
                    if (array[i].Equals(cell)) return true;
                }
                return false;
            }

            private static (int2 cell, int index) GetLowestCostCell(NativeArray<int2> cells, int count, NativeArray<float> costs, int2 gridSize)
            {
                int bestIndex = 0;
                float bestCost = float.MaxValue;

                for (int i = 0; i < count; i++)
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
        }

        /// <summary>
        /// Async flow field generation with job scheduling
        /// </summary>
        private JobHandle GenerateFlowFieldDataAsync(ref BlobBuilderArray<float3> directions,
                                                    ref BlobBuilderArray<float> costs,
                                                    ref FlowFieldBlob flowFieldBlob,
                                                    FlowFieldGenerationRequest request,
                                                    JobHandle dependency)
        {
            // Create temporary NativeArrays for async job execution
            var tempDirections = new NativeArray<float3>(directions.Length, Allocator.TempJob);
            var tempCosts = new NativeArray<float>(costs.Length, Allocator.TempJob);
            var tempOpenSet = _int2ArrayPool.Get(flowFieldBlob.gridSize.x * flowFieldBlob.gridSize.y);

            // Create and schedule job asynchronously
            var job = new FlowFieldGenerationJob
            {
                directions = tempDirections,
                costs = tempCosts,
                gridSize = flowFieldBlob.gridSize,
                cellSize = flowFieldBlob.cellSize,
                worldOrigin = flowFieldBlob.worldOrigin,
                destination = request.destination,
                tempOpenSet = tempOpenSet
            };

            // Schedule async execution
            var jobHandle = job.Schedule(dependency);

            // Note: Manual cleanup needed - tempDirections and tempCosts will auto-dispose with TempJob
            // tempOpenSet needs manual return to pool after job completion
            return jobHandle;
        }

        private void GenerateFlowFieldData(ref BlobBuilderArray<float3> directions,
                                         ref BlobBuilderArray<float> costs,
                                         ref FlowFieldBlob flowFieldBlob,
                                         FlowFieldGenerationRequest request)
        {
            // Create temporary NativeArrays for job execution
            var tempDirections = new NativeArray<float3>(directions.Length, Allocator.Temp);
            var tempCosts = new NativeArray<float>(costs.Length, Allocator.Temp);
            var tempOpenSet = _int2ArrayPool.Get(flowFieldBlob.gridSize.x * flowFieldBlob.gridSize.y);

            var job = new FlowFieldGenerationJob
            {
                directions = tempDirections,
                costs = tempCosts,
                gridSize = flowFieldBlob.gridSize,
                cellSize = flowFieldBlob.cellSize,
                worldOrigin = flowFieldBlob.worldOrigin,
                destination = request.destination,
                tempOpenSet = tempOpenSet
            };

            job.Execute();

            // Copy results back to BlobBuilderArrays
            for (int i = 0; i < directions.Length; i++)
            {
                directions[i] = tempDirections[i];
            }
            for (int i = 0; i < costs.Length; i++)
            {
                costs[i] = tempCosts[i];
            }

            // Cleanup
            tempDirections.Dispose();
            tempCosts.Dispose();
            _int2ArrayPool.Return(tempOpenSet);
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

        /// <summary>
        /// LOD-based movement job for hierarchical pathfinding optimization
        /// </summary>
        [BurstCompile]
        private struct LODFlowFieldMovementJob : IJobChunk
        {
            public ComponentTypeHandle<LocalTransform> transformTypeHandle;
            [ReadOnly] public ComponentTypeHandle<FlowFieldFollowerComponent> followerTypeHandle;
            [ReadOnly] public ComponentLookup<FlowFieldComponent> flowFieldLookup;
            [ReadOnly] public float deltaTime;
            [ReadOnly] public float currentTime;

            [BurstCompile]
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in Unity.Burst.Intrinsics.v128 chunkEnabledMask)
            {
                var transforms = chunk.GetNativeArray(ref transformTypeHandle);
                var followers = chunk.GetNativeArray(ref followerTypeHandle);

                for (int i = 0; i < chunk.Count; i++)
                {
                    var transform = transforms[i];
                    var follower = followers[i];

                    // LOD-based update frequency
                    float updateInterval = GetLODUpdateInterval(follower.currentLOD);
                    if (currentTime - follower.lastLODUpdateTime < updateInterval)
                        continue;

                    // Get flow field direction with LOD-based precision
                    float3 flowDirection = GetFlowDirectionWithLOD(transform.Position, follower, flowFieldLookup);

                    // Apply movement with LOD-adjusted speed
                    float lodSpeedMultiplier = GetLODSpeedMultiplier(follower.currentLOD);
                    if (math.lengthsq(flowDirection) > 0.01f)
                    {
                        var movement = flowDirection * follower.preferredSpeed * lodSpeedMultiplier * deltaTime;
                        transform.Position += movement;
                    }
                    else
                    {
                        // Fallback movement with LOD consideration
                        var movement = new float3(0, 0, follower.preferredSpeed * lodSpeedMultiplier * deltaTime * 0.1f);
                        transform.Position += movement;
                    }

                    transforms[i] = transform;
                }
            }

            private static float GetLODUpdateInterval(PathfindingLOD lod)
            {
                return lod switch
                {
                    PathfindingLOD.High => 0.016f,    // 60 FPS
                    PathfindingLOD.Medium => 0.033f,  // 30 FPS
                    PathfindingLOD.Low => 0.066f,     // 15 FPS
                    PathfindingLOD.Minimal => 0.2f,   // 5 FPS
                    _ => 0.033f
                };
            }

            private static float GetLODSpeedMultiplier(PathfindingLOD lod)
            {
                return lod switch
                {
                    PathfindingLOD.High => 1.0f,     // Full speed
                    PathfindingLOD.Medium => 0.8f,   // Slightly reduced
                    PathfindingLOD.Low => 0.6f,      // Reduced speed
                    PathfindingLOD.Minimal => 0.4f,  // Minimal speed
                    _ => 1.0f
                };
            }

            private static float3 GetFlowDirectionWithLOD(float3 position, FlowFieldFollowerComponent follower, ComponentLookup<FlowFieldComponent> flowFieldLookup)
            {
                if (!flowFieldLookup.HasComponent(follower.flowFieldEntity))
                    return new float3(0, 0, 0);

                var flowFieldComp = flowFieldLookup[follower.flowFieldEntity];
                if (!flowFieldComp.flowField.IsCreated)
                    return new float3(0, 0, 0);

                // Sample with LOD-based precision
                float3 samplePosition = position;

                // For lower LODs, quantize the sample position to reduce precision
                if (follower.currentLOD >= PathfindingLOD.Medium)
                {
                    float quantization = follower.currentLOD == PathfindingLOD.Medium ? 0.5f :
                                       follower.currentLOD == PathfindingLOD.Low ? 1.0f : 2.0f;
                    samplePosition = math.round(samplePosition / quantization) * quantization;
                }

                return SampleFlowField(flowFieldComp.flowField, samplePosition);
            }
        }

        private void ApplyFlowFieldMovement(float deltaTime)
        {
            float currentTime = (float)SystemAPI.Time.ElapsedTime;

            // Use LOD-optimized job for better performance
            var lodMovementJob = new LODFlowFieldMovementJob
            {
                transformTypeHandle = GetComponentTypeHandle<LocalTransform>(),
                followerTypeHandle = GetComponentTypeHandle<FlowFieldFollowerComponent>(true),
                flowFieldLookup = GetComponentLookup<FlowFieldComponent>(true),
                deltaTime = deltaTime,
                currentTime = currentTime
            };

            var followerQuery = GetEntityQuery(ComponentType.ReadWrite<LocalTransform>(), ComponentType.ReadOnly<FlowFieldFollowerComponent>());
            this.Dependency = lodMovementJob.ScheduleParallel(followerQuery, this.Dependency);
        }

        private void ApplyLocalAvoidance(float deltaTime)
        {
            Entities
                .WithAll<FlowFieldFollowerComponent>()
                .ForEach((ref LocalTransform transform, in FlowFieldFollowerComponent follower) =>
                {
                    if (follower.useLocalAvoidance)
                    {
                        // Simple avoidance - just add small offset
                        transform.Position += new float3(0.1f, 0, 0) * deltaTime;
                    }
                }).ScheduleParallel();
        }

        // Helper methods
        private static int2 WorldToGrid(float3 worldPos, float3 worldOrigin, float cellSize, int2 gridSize)
        {
            var localPos = worldPos - worldOrigin;
            var gridPos = new int2(
                Mathf.FloorToInt(localPos.x / cellSize),
                Mathf.FloorToInt(localPos.z / cellSize)
            );
            return math.clamp(gridPos, new int2(0, 0), gridSize - 1);
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

        private static bool ContainsCell(NativeArray<int2> array, int count, int2 cell)
        {
            for (int i = 0; i < count; i++)
            {
                if (array[i].Equals(cell)) return true;
            }
            return false;
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
                spatialHash = CalculateSpatialHash(center, _performanceConfig.spatialHashCellSize)
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

        /// <summary>
        /// Generates a cache key for flow field reuse based on discretized positions
        /// </summary>
        private uint GenerateFlowFieldCacheKey(float3 center, float3 destination, float radius)
        {
            // Discretize positions to grid for cache coherency
            int3 centerGrid = (int3)(center / CACHE_GRID_TOLERANCE);
            int3 destGrid = (int3)(destination / CACHE_GRID_TOLERANCE);
            int radiusGrid = (int)(radius / CACHE_GRID_TOLERANCE);

            // Combine into hash
            uint hash = (uint)(centerGrid.x * 73856093) ^ (uint)(centerGrid.y * 19349663) ^ (uint)(centerGrid.z * 83492791);
            hash ^= (uint)(destGrid.x * 13) ^ (uint)(destGrid.y * 17) ^ (uint)(destGrid.z * 19);
            hash ^= (uint)(radiusGrid * 23);

            return hash;
        }

        public static float3 SampleFlowField(BlobAssetReference<FlowFieldBlob> flowFieldBlob, float3 worldPosition)
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

            return new float3(0, 0, 0);
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
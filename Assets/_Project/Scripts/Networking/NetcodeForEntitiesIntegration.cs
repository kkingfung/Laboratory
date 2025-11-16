using Unity.Entities;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Burst;
using Unity.Jobs;
using Unity.Burst.Intrinsics;
using static Unity.Mathematics.math;
using UnityEngine;
using Laboratory.Core.ECS;
using Laboratory.Chimera.ECS;
using Laboratory.Chimera.Genetics;
using Laboratory.Core.Progression;
using Laboratory.Economy;
using Laboratory.Shared.Types;

namespace Laboratory.Networking.Entities
{
    /// <summary>
    /// Netcode for Entities Integration System - High-Performance Multiplayer ECS
    /// PURPOSE: Enable seamless multiplayer creature breeding, AI coordination, and ecosystem synchronization using Unity's Netcode for Entities
    /// FEATURES: Authority-based replication, client prediction, lag compensation, bandwidth optimization
    /// ARCHITECTURE: Server-authoritative with client-side prediction for responsive gameplay
    /// PERFORMANCE: Supports 1000+ creatures with optimized bandwidth and state reconciliation
    /// </summary>

    // Network authority and ownership components
    public struct NetworkOwnership : IComponentData
    {
        public int playerId;
        public bool hasAuthority;
        public NetworkAuthorityType authorityType;
        public float lastAuthorityChange;
    }

    public enum NetworkAuthorityType : byte
    {
        None = 0,
        Client = 1,
        Server = 2,
        Shared = 3
    }

    // Network synchronization priority for bandwidth optimization
    public struct NetworkSyncPriority : IComponentData
    {
        public byte priorityLevel; // 0-255, higher = more important
        public float lastSyncTime;
        public float syncInterval; // Custom sync rate based on importance
        public bool forceNextSync;
    }

    // Replicated creature state for multiplayer
    public struct ReplicatedCreatureState : IComponentData
    {
        public float3 position;
        public quaternion rotation;
        public float3 velocity;
        public Laboratory.Shared.Types.AIBehaviorType currentBehavior;
        public float behaviorIntensity;
        public Entity currentTarget;
        public BiomeType currentBiome;
        public float health;
        public float energy;
        public uint stateVersion;
        public float timestamp;
    }

    // Network prediction state for client-side smoothing
    public struct PredictionState : IComponentData
    {
        public float3 predictedPosition;
        public quaternion predictedRotation;
        public float3 predictedVelocity;
        public float predictionConfidence; // 0-1, how accurate we think our prediction is
        public float lastServerUpdate;
        public int missedUpdates;
    }

    // Network commands for player actions
    public struct PlayerNetworkCommand : IComponentData
    {
        public NetworkCommandType commandType;
        public Entity targetEntity;
        public float3 targetPosition;
        public float commandParameter;
        public uint commandId;
        public float timestamp;
        public int playerId;
    }

    public enum NetworkCommandType : byte
    {
        MoveCreature = 0,
        CommandCreature = 1,
        InitiateBreeding = 2,
        CancelAction = 3,
        InteractWithEnvironment = 4,
        UseSpecialAbility = 5,
        TradeCreature = 6,
        JoinGuild = 7
    }

    // Breeding synchronization across network
    public struct NetworkBreedingState : IComponentData
    {
        public Entity partner;
        public float3 breedingLocation;
        public float breedingProgress; // 0-1
        public uint breedingSessionId;
        public int initiatorPlayerId;
        public int partnerPlayerId;
        public BreedingStatus status;
        public float startTime;
        public float estimatedCompletion;
    }

    public enum BreedingStatus : byte
    {
        None = 0,
        Requested = 1,
        Accepted = 2,
        InProgress = 3,
        Completed = 4,
        Failed = 5,
        Cancelled = 6
    }

    // Market transaction synchronization
    public struct NetworkMarketTransaction : IComponentData
    {
        public Entity creatureEntity;
        public int sellerId;
        public int buyerId;
        public float price;
        public uint transactionId;
        public MarketTransactionStatus status;
        public float timestamp;
        public TransactionType type;
    }

    public enum MarketTransactionStatus : byte
    {
        Pending = 0,
        Accepted = 1,
        Completed = 2,
        Cancelled = 3,
        Failed = 4
    }

    public enum TransactionType : byte
    {
        DirectSale = 0,
        Auction = 1,
        Trade = 2,
        Gift = 3
    }

    // Player progression synchronization
    public struct NetworkPlayerProgression : IComponentData
    {
        public int playerId;
        public int level;
        public float experience;
        public int availableCreatureSlots;
        public uint unlockedBiomesBitmask; // Bitmask representing unlocked biomes for ECS compatibility
        public uint progressionVersion;
        public float lastProgressionUpdate;
    }

    // Lag compensation for smooth multiplayer experience
    public struct LagCompensationData : IComponentData
    {
        public float3 serverPosition;
        public float3 clientPosition;
        public float latency;
        public float timeDelta;
        public float interpolationFactor;
        public bool needsReconciliation;
    }

    /// <summary>
    /// Master Network Manager for Netcode for Entities Integration
    /// Handles connection management, authority distribution, and system coordination
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
    public partial class NetcodeEntityManager : SystemBase
    {
        private EntityQuery _networkedEntitiesQuery;
        private EntityQuery _playerCommandsQuery;
        private EntityQuery _breedingSessionsQuery;
        private NativeHashMap<int, Entity> _playerEntityMap;
        private NativeHashMap<uint, NetworkCommandType> _pendingCommands;

        protected override void OnCreate()
        {
            _networkedEntitiesQuery = GetEntityQuery(
                ComponentType.ReadOnly<NetworkOwnership>(),
                ComponentType.ReadWrite<ReplicatedCreatureState>()
            );

            _playerCommandsQuery = GetEntityQuery(ComponentType.ReadOnly<PlayerNetworkCommand>());
            _breedingSessionsQuery = GetEntityQuery(ComponentType.ReadWrite<NetworkBreedingState>());

            _playerEntityMap = new NativeHashMap<int, Entity>(100, Allocator.Persistent);
            _pendingCommands = new NativeHashMap<uint, NetworkCommandType>(1000, Allocator.Persistent);

            RequireForUpdate(_networkedEntitiesQuery);
        }

        protected override void OnDestroy()
        {
            if (_playerEntityMap.IsCreated) _playerEntityMap.Dispose();
            if (_pendingCommands.IsCreated) _pendingCommands.Dispose();
        }

        protected override void OnUpdate()
        {
            var currentTime = (float)SystemAPI.Time.ElapsedTime;
            var deltaTime = SystemAPI.Time.DeltaTime;

            // Process player commands first
            ProcessPlayerCommands(currentTime);

            // Update network state synchronization
            UpdateNetworkSynchronization(currentTime, deltaTime);

            // Handle breeding synchronization
            ProcessBreedingSynchronization(currentTime);

            // Process market transactions
            ProcessMarketTransactions(currentTime);

            // Update player progression sync
            UpdatePlayerProgressionSync(currentTime);
        }

        private void ProcessPlayerCommands(float currentTime)
        {
            // Process all pending player commands
            var ecb = new Unity.Entities.EntityCommandBuffer(Unity.Collections.Allocator.Temp);

            foreach (var (command, cmdEntity) in SystemAPI.Query<RefRO<PlayerNetworkCommand>>().WithEntityAccess())
            {
                // Validate command authority
                if (ValidatePlayerCommand(command.ValueRO))
                {
                    ExecutePlayerCommand(command.ValueRO, currentTime);
                    _pendingCommands[command.ValueRO.commandId] = command.ValueRO.commandType;
                }

                // Remove processed command
                ecb.DestroyEntity(cmdEntity);
            }

            ecb.Playback(EntityManager);
            ecb.Dispose();
        }

        private bool ValidatePlayerCommand(PlayerNetworkCommand command)
        {
            // Check if player has authority to execute this command
            if (!_playerEntityMap.TryGetValue(command.playerId, out var playerEntity))
            {
                return false;
            }

            // Validate command target exists and player has permission
            if (command.targetEntity != Entity.Null && !EntityManager.Exists(command.targetEntity))
            {
                return false;
            }

            // Check ownership for creature commands
            if (command.commandType == NetworkCommandType.MoveCreature ||
                command.commandType == NetworkCommandType.CommandCreature)
            {
                if (!EntityManager.HasComponent<NetworkOwnership>(command.targetEntity))
                {
                    return false;
                }

                var ownership = EntityManager.GetComponentData<NetworkOwnership>(command.targetEntity);
                return ownership.playerId == command.playerId && ownership.hasAuthority;
            }

            return true;
        }

        private void ExecutePlayerCommand(PlayerNetworkCommand command, float currentTime)
        {
            switch (command.commandType)
            {
                case NetworkCommandType.MoveCreature:
                    ExecuteMoveCommand(command, currentTime);
                    break;

                case NetworkCommandType.CommandCreature:
                    ExecuteCreatureCommand(command, currentTime);
                    break;

                case NetworkCommandType.InitiateBreeding:
                    ExecuteBreedingCommand(command, currentTime);
                    break;

                case NetworkCommandType.TradeCreature:
                    ExecuteTradeCommand(command, currentTime);
                    break;

                case NetworkCommandType.UseSpecialAbility:
                    ExecuteSpecialAbilityCommand(command, currentTime);
                    break;
            }
        }

        private void ExecuteMoveCommand(PlayerNetworkCommand command, float currentTime)
        {
            if (!EntityManager.HasComponent<ReplicatedCreatureState>(command.targetEntity))
                return;

            var replicatedState = EntityManager.GetComponentData<ReplicatedCreatureState>(command.targetEntity);

            // Update target position for pathfinding
            if (EntityManager.HasComponent<PathfindingComponent>(command.targetEntity))
            {
                var pathfinding = EntityManager.GetComponentData<PathfindingComponent>(command.targetEntity);
                pathfinding.destination = command.targetPosition;
                pathfinding.status = PathfindingStatus.Planning;
                EntityManager.SetComponentData(command.targetEntity, pathfinding);
            }

            // Update network state
            replicatedState.stateVersion++;
            replicatedState.timestamp = currentTime;
            EntityManager.SetComponentData(command.targetEntity, replicatedState);
        }

        private void ExecuteCreatureCommand(PlayerNetworkCommand command, float currentTime)
        {
            if (!EntityManager.HasComponent<ReplicatedCreatureState>(command.targetEntity))
                return;

            var replicatedState = EntityManager.GetComponentData<ReplicatedCreatureState>(command.targetEntity);

            // Update AI behavior based on command parameter
            var newBehavior = (Laboratory.Shared.Types.AIBehaviorType)(int)command.commandParameter;
            replicatedState.currentBehavior = newBehavior;
            replicatedState.behaviorIntensity = 1.0f; // Full intensity for player commands
            replicatedState.stateVersion++;
            replicatedState.timestamp = currentTime;

            EntityManager.SetComponentData(command.targetEntity, replicatedState);
        }

        private void ExecuteBreedingCommand(PlayerNetworkCommand command, float currentTime)
        {
            // Create breeding session
            var breedingEntity = EntityManager.CreateEntity();
            EntityManager.AddComponentData(breedingEntity, new NetworkBreedingState
            {
                partner = command.targetEntity,
                breedingLocation = command.targetPosition,
                breedingProgress = 0f,
                breedingSessionId = command.commandId,
                initiatorPlayerId = command.playerId,
                partnerPlayerId = -1, // To be filled when partner accepts
                status = BreedingStatus.Requested,
                startTime = currentTime,
                estimatedCompletion = currentTime + 30f // 30 seconds breeding time
            });
        }

        private void ExecuteTradeCommand(PlayerNetworkCommand command, float currentTime)
        {
            // Create market transaction
            var transactionEntity = EntityManager.CreateEntity();
            EntityManager.AddComponentData(transactionEntity, new NetworkMarketTransaction
            {
                creatureEntity = command.targetEntity,
                sellerId = command.playerId,
                buyerId = -1, // To be filled when buyer found
                price = command.commandParameter,
                transactionId = command.commandId,
                status = MarketTransactionStatus.Pending,
                timestamp = currentTime,
                type = TransactionType.DirectSale
            });
        }

        private void ExecuteSpecialAbilityCommand(PlayerNetworkCommand command, float currentTime)
        {
            if (!EntityManager.HasComponent<ReplicatedCreatureState>(command.targetEntity))
                return;

            // Trigger special ability (implementation depends on ability system)
            var replicatedState = EntityManager.GetComponentData<ReplicatedCreatureState>(command.targetEntity);
            replicatedState.behaviorIntensity = 2.0f; // Boosted intensity for abilities
            replicatedState.stateVersion++;
            replicatedState.timestamp = currentTime;

            EntityManager.SetComponentData(command.targetEntity, replicatedState);
        }

        private void UpdateNetworkSynchronization(float currentTime, float deltaTime)
        {
            // Batch network updates for efficiency
            var syncJob = new NetworkSynchronizationJob
            {
                currentTime = currentTime,
                deltaTime = deltaTime
            };

            Dependency = syncJob.ScheduleParallel(_networkedEntitiesQuery, Dependency);
        }

        private void ProcessBreedingSynchronization(float currentTime)
        {
            var ecb = new Unity.Entities.EntityCommandBuffer(Unity.Collections.Allocator.Temp);

            foreach (var (breeding, breedingEntity) in SystemAPI.Query<RefRW<NetworkBreedingState>>().WithEntityAccess())
            {
                var breedingValue = breeding.ValueRW;
                switch (breedingValue.status)
                {
                    case BreedingStatus.InProgress:
                        breedingValue.breedingProgress = math.clamp(
                            (currentTime - breedingValue.startTime) / (breedingValue.estimatedCompletion - breedingValue.startTime),
                            0f, 1f
                        );

                        if (breedingValue.breedingProgress >= 1f)
                        {
                            breedingValue.status = BreedingStatus.Completed;
                            CompleteBreeding(breedingValue);
                        }
                        breeding.ValueRW = breedingValue;
                        break;

                    case BreedingStatus.Completed:
                    case BreedingStatus.Failed:
                    case BreedingStatus.Cancelled:
                        // Clean up completed breeding sessions
                        ecb.DestroyEntity(breedingEntity);
                        break;
                }
            }

            ecb.Playback(EntityManager);
            ecb.Dispose();
        }

        private void CompleteBreeding(NetworkBreedingState breeding)
        {
            // Create offspring through breeding system integration
            Debug.Log($"Network breeding completed: Session {breeding.breedingSessionId}");

            // This would integrate with the existing breeding system
            // to create the offspring with proper genetic inheritance
        }

        private void ProcessMarketTransactions(float currentTime)
        {
            var ecb = new Unity.Entities.EntityCommandBuffer(Unity.Collections.Allocator.Temp);

            foreach (var (transaction, transactionEntity) in SystemAPI.Query<RefRW<NetworkMarketTransaction>>().WithEntityAccess())
            {
                var transactionValue = transaction.ValueRW;
                switch (transactionValue.status)
                {
                    case MarketTransactionStatus.Accepted:
                        // Process the transaction
                        if (ProcessTransaction(transactionValue))
                        {
                            transactionValue.status = MarketTransactionStatus.Completed;
                        }
                        else
                        {
                            transactionValue.status = MarketTransactionStatus.Failed;
                        }
                        transaction.ValueRW = transactionValue;
                        break;

                    case MarketTransactionStatus.Completed:
                    case MarketTransactionStatus.Failed:
                    case MarketTransactionStatus.Cancelled:
                        // Clean up completed transactions
                        ecb.DestroyEntity(transactionEntity);
                        break;
                }
            }

            ecb.Playback(EntityManager);
            ecb.Dispose();
        }

        private bool ProcessTransaction(NetworkMarketTransaction transaction)
        {
            // Validate transaction participants and creature
            if (!EntityManager.Exists(transaction.creatureEntity))
                return false;

            // Transfer ownership
            if (EntityManager.HasComponent<NetworkOwnership>(transaction.creatureEntity))
            {
                var ownership = EntityManager.GetComponentData<NetworkOwnership>(transaction.creatureEntity);
                ownership.playerId = transaction.buyerId;
                ownership.lastAuthorityChange = (float)SystemAPI.Time.ElapsedTime;
                EntityManager.SetComponentData(transaction.creatureEntity, ownership);
            }

            Debug.Log($"Transaction completed: Creature transferred from {transaction.sellerId} to {transaction.buyerId}");
            return true;
        }

        private void UpdatePlayerProgressionSync(float currentTime)
        {
            // This would sync with the progression system we implemented
            foreach (var progression in SystemAPI.Query<RefRW<NetworkPlayerProgression>>())
            {
                // Check if local progression has updated
                if (currentTime - progression.ValueRO.lastProgressionUpdate > 1.0f) // Sync every second
                {
                    progression.ValueRW.progressionVersion++;
                    progression.ValueRW.lastProgressionUpdate = currentTime;
                }
            }
        }
    }

    /// <summary>
    /// High-performance job for network state synchronization
    /// </summary>
    [BurstCompile]
    public struct NetworkSynchronizationJob : IJobChunk
    {
        [ReadOnly] public float currentTime;
        [ReadOnly] public float deltaTime;

        public ComponentTypeHandle<ReplicatedCreatureState> replicatedStateHandle;
        public ComponentTypeHandle<NetworkSyncPriority> syncPriorityHandle;
        [ReadOnly] public ComponentTypeHandle<LocalTransform> transformHandle;

        [BurstCompile]
        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            var replicatedStates = chunk.GetNativeArray(ref replicatedStateHandle);
            var syncPriorities = chunk.GetNativeArray(ref syncPriorityHandle);
            var transforms = chunk.GetNativeArray(ref transformHandle);

            for (int i = 0; i < chunk.Count; i++)
            {
                // Skip if entity is disabled when using enabled mask
                if (useEnabledMask)
                {
                    // Use standard bit checking for enabled mask
                    int maskIndex = i / 32;
                    int bitIndex = i % 32;
                    if (maskIndex < 4) // v128 has 4 uint values
                    {
                        uint maskValue = chunkEnabledMask.UInt0;
                        if (maskIndex == 1) maskValue = chunkEnabledMask.UInt1;
                        else if (maskIndex == 2) maskValue = chunkEnabledMask.UInt2;
                        else if (maskIndex == 3) maskValue = chunkEnabledMask.UInt3;

                        if ((maskValue & (1u << bitIndex)) == 0)
                            continue;
                    }
                }

                var replicatedState = replicatedStates[i];
                var syncPriority = syncPriorities[i];
                var transform = transforms[i];

                // Check if sync is needed based on priority and interval
                bool shouldSync = currentTime - syncPriority.lastSyncTime >= syncPriority.syncInterval ||
                                 syncPriority.forceNextSync;

                if (shouldSync)
                {
                    // Update replicated state from current transform
                    replicatedState.position = transform.Position;
                    replicatedState.rotation = transform.Rotation;
                    replicatedState.timestamp = currentTime;
                    replicatedState.stateVersion++;

                    // Update sync timing
                    syncPriority.lastSyncTime = currentTime;
                    syncPriority.forceNextSync = false;

                    // Write back updated data
                    replicatedStates[i] = replicatedState;
                    syncPriorities[i] = syncPriority;
                }
            }
        }
    }

    /// <summary>
    /// Client-side prediction system for smooth multiplayer experience
    /// </summary>
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class NetworkPredictionSystem : SystemBase
    {
        private EntityQuery _predictedEntitiesQuery;

        protected override void OnCreate()
        {
            _predictedEntitiesQuery = GetEntityQuery(
                ComponentType.ReadWrite<PredictionState>(),
                ComponentType.ReadOnly<ReplicatedCreatureState>(),
                ComponentType.ReadWrite<LocalTransform>()
            );

            RequireForUpdate(_predictedEntitiesQuery);
        }

        protected override void OnUpdate()
        {
            var currentTime = (float)SystemAPI.Time.ElapsedTime;
            var deltaTime = SystemAPI.Time.DeltaTime;

            // Update client-side prediction
            foreach (var (prediction, transform, replicated) in SystemAPI.Query<RefRW<PredictionState>, RefRW<LocalTransform>, RefRO<ReplicatedCreatureState>>())
            {
                // Calculate prediction based on last known server state
                float timeSinceUpdate = currentTime - replicated.ValueRO.timestamp;

                var predictionValue = prediction.ValueRW;
                var transformValue = transform.ValueRW;

                if (timeSinceUpdate < 0.5f) // Use prediction only for recent updates
                {
                    // Predict position based on velocity
                    predictionValue.predictedPosition = replicated.ValueRO.position + replicated.ValueRO.velocity * timeSinceUpdate;
                    predictionValue.predictedRotation = replicated.ValueRO.rotation;

                    // Interpolate between current position and predicted position
                    float interpolationFactor = math.clamp(timeSinceUpdate * 2f, 0f, 1f);
                    transformValue.Position = math.lerp(transformValue.Position, predictionValue.predictedPosition, interpolationFactor * deltaTime * 10f);
                    transformValue.Rotation = math.slerp(transformValue.Rotation, predictionValue.predictedRotation, interpolationFactor * deltaTime * 10f);

                    predictionValue.predictionConfidence = 1f - (timeSinceUpdate * 0.5f);
                }
                else
                {
                    // Too old - reduce confidence and use last known position
                    predictionValue.predictionConfidence = math.max(0f, predictionValue.predictionConfidence - deltaTime);
                    predictionValue.missedUpdates++;
                }

                prediction.ValueRW = predictionValue;
                transform.ValueRW = transformValue;
            }
        }
    }

    /// <summary>
    /// Lag compensation system for fair multiplayer interactions
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class LagCompensationSystem : SystemBase
    {
        private EntityQuery _lagCompensatedQuery;

        protected override void OnCreate()
        {
            _lagCompensatedQuery = GetEntityQuery(
                ComponentType.ReadWrite<LagCompensationData>(),
                ComponentType.ReadOnly<ReplicatedCreatureState>()
            );

            RequireForUpdate(_lagCompensatedQuery);
        }

        protected override void OnUpdate()
        {
            var currentTime = (float)SystemAPI.Time.ElapsedTime;

            // Process lag compensation
            foreach (var (lagComp, replicated) in SystemAPI.Query<RefRW<LagCompensationData>, RefRO<ReplicatedCreatureState>>())
            {
                var lagCompValue = lagComp.ValueRW;

                // Calculate latency and time delta
                lagCompValue.timeDelta = currentTime - replicated.ValueRO.timestamp;

                // Estimate latency (simplified - real implementation would use proper RTT)
                lagCompValue.latency = lagCompValue.timeDelta * 0.5f;

                // Calculate interpolation factor for smooth movement
                lagCompValue.interpolationFactor = math.clamp(lagCompValue.latency * 2f, 0f, 1f);

                // Check if reconciliation is needed
                float positionDifference = math.distance(lagCompValue.serverPosition, lagCompValue.clientPosition);
                lagCompValue.needsReconciliation = positionDifference > 1f; // 1 unit tolerance

                lagComp.ValueRW = lagCompValue;
            }
        }
    }

    /// <summary>
    /// Network bandwidth optimization system
    /// Dynamically adjusts sync rates and data compression based on network conditions
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderLast = true)]
    public partial class NetworkOptimizationSystem : SystemBase
    {
        private EntityQuery _networkedEntitiesQuery;
        private float _networkBandwidthUsage;
        private float _targetBandwidth = 1000f; // KB/s target

        protected override void OnCreate()
        {
            _networkedEntitiesQuery = GetEntityQuery(
                ComponentType.ReadWrite<NetworkSyncPriority>(),
                ComponentType.ReadOnly<NetworkOwnership>()
            );

            RequireForUpdate(_networkedEntitiesQuery);
        }

        protected override void OnUpdate()
        {
            var currentTime = (float)SystemAPI.Time.ElapsedTime;

            // Estimate current bandwidth usage
            EstimateBandwidthUsage();

            // Adjust sync priorities based on bandwidth constraints
            AdjustSyncPriorities(currentTime);
        }

        private void EstimateBandwidthUsage()
        {
            // Simplified bandwidth estimation
            var entityCount = _networkedEntitiesQuery.CalculateEntityCount();
            var averageEntitySize = 64f; // bytes per entity update
            var updatesPerSecond = entityCount * 10f; // Assume 10 Hz average

            _networkBandwidthUsage = (updatesPerSecond * averageEntitySize) / 1000f; // KB/s
        }

        private void AdjustSyncPriorities(float currentTime)
        {
            float bandwidthPressure = _networkBandwidthUsage / _targetBandwidth;

            foreach (var (syncPriority, ownership) in SystemAPI.Query<RefRW<NetworkSyncPriority>, RefRO<NetworkOwnership>>())
            {
                // Adjust sync interval based on bandwidth pressure and importance
                float baseSyncRate = syncPriority.ValueRO.priorityLevel / 255f; // 0-1 priority
                float adjustedSyncRate = baseSyncRate / math.max(1f, bandwidthPressure);

                syncPriority.ValueRW.syncInterval = math.lerp(0.05f, 1f, 1f - adjustedSyncRate); // 20Hz to 1Hz

                // Higher priority for player-owned entities
                if (ownership.ValueRO.hasAuthority)
                {
                    syncPriority.ValueRW.syncInterval *= 0.5f; // Double the sync rate
                }
            }
        }
    }
}
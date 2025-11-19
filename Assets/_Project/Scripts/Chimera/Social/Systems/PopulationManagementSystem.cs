using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Laboratory.Chimera.ECS;

namespace Laboratory.Chimera.Social
{
    /// <summary>
    /// POPULATION MANAGEMENT SYSTEM
    ///
    /// Manages chimera population capacity and unlocks
    ///
    /// Core Mechanics:
    /// 1. Players start with capacity for 1 chimera
    /// 2. Building strong bonds unlocks capacity for more (max 5)
    /// 3. Sending chimeras away permanently reduces max capacity
    /// 4. System validates acquisitions against capacity limits
    /// 5. Tracks all chimera bonds to determine unlock eligibility
    ///
    /// Responsibilities:
    /// - Track player chimera capacity (current vs max)
    /// - Monitor bond strengths for capacity unlock requirements
    /// - Process acquisition requests (validate against capacity)
    /// - Process release requests (apply permanent capacity reduction)
    /// - Emit events for unlocks, warnings, and capacity changes
    ///
    /// Design Philosophy:
    /// "Quality over Quantity - Every chimera matters"
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(EnhancedBondingSystem))]
    public partial class PopulationManagementSystem : SystemBase
    {
        private EndSimulationEntityCommandBufferSystem _ecbSystem;

        // Update intervals
        private const float CAPACITY_CHECK_INTERVAL = 5.0f;  // Check capacity unlocks every 5 seconds
        private const float BOND_TRACKER_UPDATE_INTERVAL = 2.0f; // Update bond tracking every 2 seconds

        protected override void OnCreate()
        {
            _ecbSystem = World.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
            Debug.Log("Population Management System initialized - quality over quantity!");
        }

        protected override void OnUpdate()
        {
            float deltaTime = SystemAPI.Time.DeltaTime;
            float currentTime = (float)SystemAPI.Time.ElapsedTime;

            // Initialize population capacity for new players
            InitializePopulationCapacity();

            // Update bond trackers for capacity calculations
            UpdateBondTrackers(currentTime);

            // Check for capacity unlock eligibility
            CheckCapacityUnlocks(currentTime);

            // Process chimera acquisition requests
            ProcessAcquisitionRequests(currentTime);

            // Process chimera release requests
            ProcessReleaseRequests(currentTime);

            // Update population warnings
            UpdatePopulationWarnings(currentTime);

            // Clean up old events
            CleanupOldEvents(currentTime);
        }

        /// <summary>
        /// Initializes population capacity for players who don't have it yet
        /// </summary>
        private void InitializePopulationCapacity()
        {
            var ecb = _ecbSystem.CreateCommandBuffer();

            // For now, create a singleton for the player
            // In multiplayer, this would be per-player
            var singletonQuery = SystemAPI.QueryBuilder()
                .WithAll<ChimeraPopulationCapacity>()
                .Build();

            if (singletonQuery.CalculateEntityCount() == 0)
            {
                var playerEntity = EntityManager.CreateEntity();
                ecb.AddComponent(playerEntity, new ChimeraPopulationCapacity
                {
                    currentCapacity = 0,
                    maxCapacity = 1,                 // Start with capacity for 1 chimera
                    baseMaxCapacity = 5,             // Can eventually unlock up to 5
                    capacityUnlocked = 1,            // Started with 1 slot
                    capacityLostPermanently = 0,
                    strongBondsRequired = CapacityUnlockThresholds.UNLOCK_SLOT_2_BONDS_REQUIRED,
                    bondStrengthRequired = CapacityUnlockThresholds.UNLOCK_SLOT_2_BOND_STRENGTH,
                    canUnlockNext = false,
                    totalChimerasEverOwned = 0,
                    totalChimerasSentAway = 0,
                    totalChimerasNaturalDeath = 0,
                    currentAliveChimeras = 0,
                    atCapacity = false,
                    hasLostCapacity = false
                });

                // Add buffer for tracking individual chimera bonds
                ecb.AddBuffer<ChimeraBondTracker>(playerEntity);

                Debug.Log("Population capacity initialized: 1/1 chimeras (can unlock up to 5)");
            }
        }

        /// <summary>
        /// Updates bond tracking for all chimeras to determine capacity unlock eligibility
        /// </summary>
        private void UpdateBondTrackers(float currentTime)
        {
            foreach (var (capacity, bondTrackerBuffer, entity) in
                SystemAPI.Query<RefRW<ChimeraPopulationCapacity>, DynamicBuffer<ChimeraBondTracker>>().WithEntityAccess())
            {
                // Clear old trackers and rebuild from current chimeras
                bondTrackerBuffer.Clear();

                int aliveCount = 0;
                int strongBondCount = 0;
                float totalBondStrength = 0f;

                // Query all chimeras with bond data
                foreach (var (bondData, chimeraIdentity, chimeraEntity) in
                    SystemAPI.Query<RefRO<Laboratory.Chimera.ECS.CreatureBondData>,
                        RefRO<CreatureIdentityComponent>>().WithEntityAccess())
                {
                    float bondStrength = bondData.ValueRO.bondStrength;
                    totalBondStrength += bondStrength;
                    aliveCount++;

                    // Determine if this bond is strong enough to count for capacity
                    // Use the next unlock requirement as threshold
                    var (_, requiredStrength) = CapacityUnlockThresholds.GetUnlockRequirements(
                        capacity.ValueRO.capacityUnlocked + 1
                    );

                    bool countsForCapacity = bondStrength >= requiredStrength;
                    if (countsForCapacity)
                        strongBondCount++;

                    // Add to tracker buffer
                    bondTrackerBuffer.Add(new ChimeraBondTracker
                    {
                        chimeraEntity = chimeraEntity,
                        bondStrength = bondStrength,
                        peakBondStrength = bondStrength, // TODO: Track peak over time
                        bondTrend = 0f, // TODO: Calculate trend
                        countsForCapacity = countsForCapacity,
                        chimeraName = "Chimera" // TODO: Get actual name
                    });
                }

                // Update capacity tracking
                capacity.ValueRW.currentAliveChimeras = aliveCount;
                capacity.ValueRW.currentCapacity = aliveCount;
                capacity.ValueRW.atCapacity = aliveCount >= capacity.ValueRO.maxCapacity;

                // Calculate if player can unlock next capacity tier
                var (requiredBonds, requiredBondStrength) = CapacityUnlockThresholds.GetUnlockRequirements(
                    capacity.ValueRO.capacityUnlocked + 1
                );

                capacity.ValueRW.strongBondsRequired = requiredBonds;
                capacity.ValueRW.bondStrengthRequired = requiredBondStrength;
                capacity.ValueRW.canUnlockNext = strongBondCount >= requiredBonds &&
                                                 capacity.ValueRO.capacityUnlocked < 5;
            }
        }

        /// <summary>
        /// Checks if player has met requirements to unlock next capacity tier
        /// </summary>
        private void CheckCapacityUnlocks(float currentTime)
        {
            var ecb = _ecbSystem.CreateCommandBuffer();

            foreach (var (capacity, bondTrackerBuffer, entity) in
                SystemAPI.Query<RefRW<ChimeraPopulationCapacity>, DynamicBuffer<ChimeraBondTracker>>().WithEntityAccess())
            {
                if (!capacity.ValueRO.canUnlockNext)
                    continue;

                if (capacity.ValueRO.capacityUnlocked >= 5)
                    continue; // Already at max

                // Count strong bonds
                int strongBondCount = 0;
                float totalBondStrength = 0f;
                foreach (var tracker in bondTrackerBuffer)
                {
                    if (tracker.countsForCapacity)
                    {
                        strongBondCount++;
                        totalBondStrength += tracker.bondStrength;
                    }
                }

                var (requiredBonds, requiredStrength) = CapacityUnlockThresholds.GetUnlockRequirements(
                    capacity.ValueRO.capacityUnlocked + 1
                );

                // Check if requirements met
                if (strongBondCount >= requiredBonds)
                {
                    // UNLOCK NEXT TIER!
                    int previousCapacity = capacity.ValueRO.maxCapacity;
                    int newCapacity = capacity.ValueRO.capacityUnlocked + 1;

                    capacity.ValueRW.capacityUnlocked = newCapacity;
                    capacity.ValueRW.maxCapacity = math.min(newCapacity, 5);
                    capacity.ValueRW.canUnlockNext = false; // Reset until next tier requirements met
                    capacity.ValueRW.atCapacity = capacity.ValueRO.currentCapacity >= capacity.ValueRO.maxCapacity;

                    // Update requirements for next tier
                    var (nextRequiredBonds, nextRequiredStrength) = CapacityUnlockThresholds.GetUnlockRequirements(
                        newCapacity + 1
                    );
                    capacity.ValueRW.strongBondsRequired = nextRequiredBonds;
                    capacity.ValueRW.bondStrengthRequired = nextRequiredStrength;

                    // Emit unlock event
                    var unlockEvent = EntityManager.CreateEntity();
                    ecb.AddComponent(unlockEvent, new CapacityUnlockEvent
                    {
                        playerEntity = entity,
                        previousMaxCapacity = previousCapacity,
                        newMaxCapacity = newCapacity,
                        strongBondsCount = strongBondCount,
                        averageBondStrength = totalBondStrength / strongBondCount,
                        timestamp = currentTime,
                        achievementText = CapacityUnlockThresholds.GetUnlockAchievementText(newCapacity)
                    });

                    Debug.LogWarning($"CAPACITY UNLOCKED! {previousCapacity} → {newCapacity} chimeras! " +
                                   $"{CapacityUnlockThresholds.GetUnlockAchievementText(newCapacity)}");
                }
            }
        }

        /// <summary>
        /// Processes requests to acquire new chimeras
        /// </summary>
        private void ProcessAcquisitionRequests(float currentTime)
        {
            var ecb = _ecbSystem.CreateCommandBuffer();

            foreach (var (request, entity) in
                SystemAPI.Query<RefRO<ChimeraAcquisitionRequest>>().WithEntityAccess())
            {
                var playerEntity = request.ValueRO.playerEntity;

                if (!EntityManager.Exists(playerEntity))
                {
                    ecb.DestroyEntity(entity);
                    continue;
                }

                if (!EntityManager.HasComponent<ChimeraPopulationCapacity>(playerEntity))
                {
                    Debug.LogError("Player does not have population capacity component!");
                    ecb.DestroyEntity(entity);
                    continue;
                }

                var capacity = EntityManager.GetComponentData<ChimeraPopulationCapacity>(playerEntity);

                // Check if player has room
                if (capacity.currentCapacity >= capacity.maxCapacity)
                {
                    // DENIED - at capacity
                    Debug.LogWarning($"Cannot acquire chimera: At capacity ({capacity.currentCapacity}/{capacity.maxCapacity})");

                    // Emit warning
                    var warningEvent = EntityManager.CreateEntity();
                    ecb.AddComponent(warningEvent, new PopulationWarning
                    {
                        playerEntity = playerEntity,
                        warningType = PopulationWarningType.AtCapacity,
                        severity = 1.0f,
                        timestamp = currentTime,
                        message = $"Cannot acquire more chimeras! At capacity: {capacity.currentCapacity}/{capacity.maxCapacity}"
                    });

                    ecb.DestroyEntity(entity);
                    continue;
                }

                // APPROVED - acquire chimera
                capacity.currentCapacity++;
                capacity.totalChimerasEverOwned++;
                capacity.atCapacity = capacity.currentCapacity >= capacity.maxCapacity;
                EntityManager.SetComponentData(playerEntity, capacity);

                Debug.Log($"Chimera acquired! Population: {capacity.currentCapacity}/{capacity.maxCapacity} " +
                         $"(Method: {request.ValueRO.method})");

                ecb.DestroyEntity(entity);
            }
        }

        /// <summary>
        /// Processes requests to release/send away chimeras
        /// </summary>
        private void ProcessReleaseRequests(float currentTime)
        {
            var ecb = _ecbSystem.CreateCommandBuffer();

            foreach (var (request, entity) in
                SystemAPI.Query<RefRO<ChimeraReleaseRequest>>().WithEntityAccess())
            {
                var playerEntity = request.ValueRO.playerEntity;
                var chimeraEntity = request.ValueRO.chimeraEntity;

                if (!EntityManager.Exists(playerEntity) || !EntityManager.Exists(chimeraEntity))
                {
                    ecb.DestroyEntity(entity);
                    continue;
                }

                var capacity = EntityManager.GetComponentData<ChimeraPopulationCapacity>(playerEntity);

                // Update population count
                capacity.currentCapacity = math.max(0, capacity.currentCapacity - 1);
                capacity.atCapacity = false;

                // PERMANENT CAPACITY REDUCTION (unless temporary rehoming)
                if (!request.ValueRO.isTemporary)
                {
                    capacity.totalChimerasSentAway++;
                    capacity.capacityLostPermanently++;

                    // Reduce max capacity permanently
                    int previousMaxCapacity = capacity.maxCapacity;
                    capacity.maxCapacity = math.max(1, capacity.maxCapacity - 1);
                    capacity.hasLostCapacity = true;

                    // Emit capacity reduction event
                    var reductionEvent = EntityManager.CreateEntity();
                    ecb.AddComponent(reductionEvent, new CapacityReductionEvent
                    {
                        playerEntity = playerEntity,
                        chimeraEntity = chimeraEntity,
                        previousMaxCapacity = previousMaxCapacity,
                        newMaxCapacity = capacity.maxCapacity,
                        reason = ConvertReleaseReasonToReductionReason(request.ValueRO.reason),
                        timestamp = currentTime,
                        warningText = $"CAPACITY PERMANENTLY REDUCED: {previousMaxCapacity} → {capacity.maxCapacity}. " +
                                     "You can never get this slot back."
                    });

                    Debug.LogError($"PERMANENT CAPACITY REDUCTION! {previousMaxCapacity} → {capacity.maxCapacity} " +
                                  $"(Reason: {request.ValueRO.reason})");
                }
                else
                {
                    Debug.Log($"Chimera temporarily rehomed. Capacity preserved: {capacity.maxCapacity}");
                }

                EntityManager.SetComponentData(playerEntity, capacity);
                ecb.DestroyEntity(entity);
            }
        }

        /// <summary>
        /// Updates population warnings for player awareness
        /// </summary>
        private void UpdatePopulationWarnings(float currentTime)
        {
            var ecb = _ecbSystem.CreateCommandBuffer();

            foreach (var (capacity, bondTrackerBuffer, entity) in
                SystemAPI.Query<RefRW<ChimeraPopulationCapacity>, DynamicBuffer<ChimeraBondTracker>>().WithEntityAccess())
            {
                // Check if player can unlock more capacity
                if (capacity.ValueRO.canUnlockNext && capacity.ValueRO.capacityUnlocked < 5)
                {
                    var warningEvent = EntityManager.CreateEntity();
                    ecb.AddComponent(warningEvent, new PopulationWarning
                    {
                        playerEntity = entity,
                        warningType = PopulationWarningType.CanUnlockMore,
                        severity = 0.5f,
                        timestamp = currentTime,
                        message = $"Strong bonds achieved! You can unlock chimera slot {capacity.ValueRO.capacityUnlocked + 1}!"
                    });
                }

                // Warn if nearing capacity
                if (capacity.ValueRO.currentCapacity == capacity.ValueRO.maxCapacity - 1)
                {
                    var warningEvent = EntityManager.CreateEntity();
                    ecb.AddComponent(warningEvent, new PopulationWarning
                    {
                        playerEntity = entity,
                        warningType = PopulationWarningType.NearingCapacity,
                        severity = 0.7f,
                        timestamp = currentTime,
                        message = $"Nearing capacity: {capacity.ValueRO.currentCapacity}/{capacity.ValueRO.maxCapacity}"
                    });
                }

                // Check for weak bonds
                int weakBondCount = 0;
                foreach (var tracker in bondTrackerBuffer)
                {
                    if (tracker.bondStrength < 0.4f)
                        weakBondCount++;
                }

                if (weakBondCount > 0 && bondTrackerBuffer.Length > 0)
                {
                    float weakRatio = (float)weakBondCount / bondTrackerBuffer.Length;
                    if (weakRatio > 0.5f)
                    {
                        var warningEvent = EntityManager.CreateEntity();
                        ecb.AddComponent(warningEvent, new PopulationWarning
                        {
                            playerEntity = entity,
                            warningType = PopulationWarningType.WeakBonds,
                            severity = 0.8f,
                            timestamp = currentTime,
                            message = $"Warning: {weakBondCount} chimeras have weak bonds!"
                        });
                    }
                }
            }
        }

        /// <summary>
        /// Cleans up old events
        /// </summary>
        private void CleanupOldEvents(float currentTime)
        {
            var ecb = _ecbSystem.CreateCommandBuffer();

            // Clean up old unlock events (after 10 seconds)
            foreach (var (unlockEvent, entity) in
                SystemAPI.Query<RefRO<CapacityUnlockEvent>>().WithEntityAccess())
            {
                if (currentTime - unlockEvent.ValueRO.timestamp > 10f)
                {
                    ecb.DestroyEntity(entity);
                }
            }

            // Clean up old warnings (after 30 seconds)
            foreach (var (warning, entity) in
                SystemAPI.Query<RefRO<PopulationWarning>>().WithEntityAccess())
            {
                if (currentTime - warning.ValueRO.timestamp > 30f)
                {
                    ecb.DestroyEntity(entity);
                }
            }
        }

        // Helper methods

        private CapacityReductionReason ConvertReleaseReasonToReductionReason(ReleaseReason releaseReason)
        {
            return releaseReason switch
            {
                ReleaseReason.PlayerChoice => CapacityReductionReason.SentAway,
                ReleaseReason.NoCapacity => CapacityReductionReason.Abandoned,
                ReleaseReason.PoorBond => CapacityReductionReason.Abandoned,
                ReleaseReason.ChimeraChoice => CapacityReductionReason.Abandoned,
                _ => CapacityReductionReason.SentAway
            };
        }
    }
}

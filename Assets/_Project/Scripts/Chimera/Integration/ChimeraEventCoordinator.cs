using Unity.Entities;
using UnityEngine;
using Laboratory.Chimera.Consciousness.Core;
using Laboratory.Chimera.Progression;
using Laboratory.Chimera.Activities;
using Laboratory.Chimera.Social;
using Laboratory.Chimera.Core;

namespace Laboratory.Chimera.Integration
{
    /// <summary>
    /// CHIMERA EVENT COORDINATOR
    ///
    /// Coordinates cross-system events and state synchronization
    ///
    /// Responsibilities:
    /// - Emit events that affect multiple systems
    /// - Coordinate responses across different systems
    /// - Handle cascading effects (e.g., activity success → emotional response → bond change)
    /// - Ensure proper event ordering and dependencies
    ///
    /// Design Philosophy:
    /// "Events ripple through the system - one action triggers many reactions"
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PartnershipActivitySystem))]
    public partial class ChimeraEventCoordinator : SystemBase
    {
        private EndSimulationEntityCommandBufferSystem _ecbSystem;

        protected override void OnCreate()
        {
            _ecbSystem = World.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
            Debug.Log("Chimera Event Coordinator initialized - cross-system integration active!");
        }

        protected override void OnUpdate()
        {
            float currentTime = (float)SystemAPI.Time.ElapsedTime;

            // Coordinate activity results with emotional/bonding systems
            ProcessActivityResultEvents(currentTime);

            // Coordinate equipment changes with emotional responses
            ProcessEquipmentChangeEvents(currentTime);

            // Coordinate life stage transitions with personality/bonding changes
            ProcessLifeStageTransitions(currentTime);

            // Coordinate breeding results with personality initialization
            ProcessBreedingResults(currentTime);
        }

        /// <summary>
        /// Processes activity results and triggers emotional + bonding responses
        /// </summary>
        private void ProcessActivityResultEvents(float currentTime)
        {
            var ecb = _ecbSystem.CreateCommandBuffer();

            foreach (var (result, entity) in
                SystemAPI.Query<RefRO<PartnershipActivityResult>>().WithEntityAccess())
            {
                var chimeraEntity = result.ValueRO.chimeraEntity;
                if (!EntityManager.Exists(chimeraEntity))
                    continue;

                // Create positive memory from successful activity results
                if (result.ValueRO.status >= ActivityResultStatus.Silver) // Silver or better = positive memory
                {
                    if (EntityManager.HasBuffer<PositiveMemory>(chimeraEntity))
                    {
                        var memoryBuffer = EntityManager.GetBuffer<PositiveMemory>(chimeraEntity);

                        PositiveMemoryType memoryType = result.ValueRO.status switch
                        {
                            ActivityResultStatus.Platinum => PositiveMemoryType.PerfectCooperation,
                            ActivityResultStatus.Gold => PositiveMemoryType.FirstVictory,
                            ActivityResultStatus.Silver => PositiveMemoryType.SharedDiscovery,
                            _ => PositiveMemoryType.SharedDiscovery
                        };

                        // Get chimera's current life stage
                        LifeStage currentStage = LifeStage.Adult; // Default
                        if (EntityManager.HasComponent<CreatureIdentityComponent>(chimeraEntity))
                        {
                            currentStage = EntityManager.GetComponentData<CreatureIdentityComponent>(chimeraEntity).CurrentLifeStage;
                        }

                        memoryBuffer.Add(new PositiveMemory
                        {
                            type = memoryType,
                            intensityWhenCreated = result.ValueRO.playerPerformance,
                            ageWhenCreated = currentStage,
                            timestamp = currentTime,
                            currentStrength = result.ValueRO.playerPerformance,
                            description = "Activity success"
                        });

                        // Limit buffer size
                        if (memoryBuffer.Length > 10)
                        {
                            memoryBuffer.RemoveAt(0);
                        }
                    }
                }

                // Update bond strength based on activity success
                if (EntityManager.HasComponent<PartnershipSkillComponent>(result.ValueRO.partnershipEntity))
                {
                    var skill = EntityManager.GetComponentData<PartnershipSkillComponent>(result.ValueRO.partnershipEntity);

                    float bondChange = result.ValueRO.bondStrengthChange;

                    // Age-based bond sensitivity (Phase 2)
                    if (EntityManager.HasComponent<CreatureIdentityComponent>(chimeraEntity))
                    {
                        var identity = EntityManager.GetComponentData<CreatureIdentityComponent>(chimeraEntity);
                        float ageSensitivity = CalculateAgeSensitivity(identity.CurrentLifeStage);
                        bondChange *= ageSensitivity;
                    }

                    skill.trustLevel = Unity.Mathematics.math.clamp(
                        skill.trustLevel + bondChange, 0f, 1f);

                    EntityManager.SetComponentData(result.ValueRO.partnershipEntity, skill);
                }

                // Create personality change event if applicable
                if (result.ValueRO.cooperationImproved)
                {
                    // Get current affection to calculate change
                    byte currentAffection = 50; // Default
                    if (EntityManager.HasComponent<CreaturePersonality>(chimeraEntity))
                    {
                        currentAffection = EntityManager.GetComponentData<CreaturePersonality>(chimeraEntity).Affection;
                    }

                    byte newAffection = (byte)Unity.Mathematics.math.min(100, currentAffection + (result.ValueRO.skillGained * 5f));

                    var changeEventEntity = EntityManager.CreateEntity();
                    ecb.AddComponent(changeEventEntity, new PersonalityChangeEvent
                    {
                        targetCreature = chimeraEntity,
                        traitChanged = PersonalityTraitType.Affection,
                        previousValue = currentAffection,
                        newValue = newAffection,
                        changeIntensity = result.ValueRO.skillGained,
                        timestamp = currentTime,
                        changeReason = "Activity cooperation"
                    });
                }
            }
        }

        /// <summary>
        /// Processes equipment changes and triggers emotional responses
        /// </summary>
        private void ProcessEquipmentChangeEvents(float currentTime)
        {
            var ecb = _ecbSystem.CreateCommandBuffer();

            foreach (var (equipEvent, entity) in
                SystemAPI.Query<RefRO<Equipment.EquipmentPersonalityChangeEvent>>().WithEntityAccess())
            {
                var chimeraEntity = equipEvent.ValueRO.chimeraEntity;
                if (!EntityManager.Exists(chimeraEntity))
                {
                    ecb.DestroyEntity(entity);
                    continue;
                }

                // Create positive or negative memory based on equipment fit
                if (equipEvent.ValueRO.personalityFit > 0.7f)
                {
                    // Likes equipment - positive memory
                    if (EntityManager.HasBuffer<PositiveMemory>(chimeraEntity))
                    {
                        var memoryBuffer = EntityManager.GetBuffer<PositiveMemory>(chimeraEntity);

                        LifeStage currentStage = LifeStage.Adult;
                        if (EntityManager.HasComponent<CreatureIdentityComponent>(chimeraEntity))
                        {
                            currentStage = EntityManager.GetComponentData<CreatureIdentityComponent>(chimeraEntity).CurrentLifeStage;
                        }

                        memoryBuffer.Add(new PositiveMemory
                        {
                            type = PositiveMemoryType.ReceivedGift,
                            intensityWhenCreated = equipEvent.ValueRO.personalityFit,
                            ageWhenCreated = currentStage,
                            timestamp = currentTime,
                            currentStrength = equipEvent.ValueRO.personalityFit,
                            description = "Equipment suits personality"
                        });
                    }
                }
                else if (equipEvent.ValueRO.personalityFit < 0.3f)
                {
                    // Dislikes equipment - create mild bond damage
                    var damageEvent = EntityManager.CreateEntity();
                    ecb.AddComponent(damageEvent, new BondDamageEvent
                    {
                        targetCreature = chimeraEntity,
                        damageType = BondDamageType.Neglect, // Wrong equipment feels like neglect
                        rawDamageAmount = 0.05f * (1f - equipEvent.ValueRO.personalityFit),
                        creatureAgeAtDamage = EntityManager.HasComponent<CreatureIdentityComponent>(chimeraEntity)
                            ? EntityManager.GetComponentData<CreatureIdentityComponent>(chimeraEntity).CurrentLifeStage
                            : LifeStage.Adult,
                        timestamp = currentTime
                    });
                }

                // Update personality based on equipment fit
                if (EntityManager.HasComponent<CreaturePersonality>(chimeraEntity))
                {
                    var personality = EntityManager.GetComponentData<CreaturePersonality>(chimeraEntity);

                    // Good equipment fit = happiness boost
                    if (equipEvent.ValueRO.personalityFit > 0.7f)
                    {
                        personality.HappinessLevel = Unity.Mathematics.math.min(
                            1f, personality.HappinessLevel + 0.1f);
                    }
                    // Poor fit = stress
                    else if (equipEvent.ValueRO.personalityFit < 0.3f)
                    {
                        personality.StressLevel = Unity.Mathematics.math.min(
                            1f, personality.StressLevel + 0.05f);
                    }

                    EntityManager.SetComponentData(chimeraEntity, personality);
                }

                ecb.DestroyEntity(entity);
            }
        }

        /// <summary>
        /// Processes life stage transitions and triggers appropriate system updates
        /// </summary>
        private void ProcessLifeStageTransitions(float currentTime)
        {
            var ecb = _ecbSystem.CreateCommandBuffer();

            foreach (var (transitionEvent, entity) in
                SystemAPI.Query<RefRO<AgeStageProgressionEvent>>().WithEntityAccess())
            {
                var chimeraEntity = transitionEvent.ValueRO.creature;
                if (!EntityManager.Exists(chimeraEntity))
                {
                    ecb.DestroyEntity(entity);
                    continue;
                }

                // Special handling for transition to elderly
                if (transitionEvent.ValueRO.newStage == LifeStage.Elderly)
                {
                    Debug.Log($"Chimera transitioning to ELDERLY - personality will lock!");

                    // Create positive memory for this milestone
                    if (EntityManager.HasBuffer<PositiveMemory>(chimeraEntity))
                    {
                        var memoryBuffer = EntityManager.GetBuffer<PositiveMemory>(chimeraEntity);
                        memoryBuffer.Add(new PositiveMemory
                        {
                            type = PositiveMemoryType.MilestoneReached,
                            intensityWhenCreated = 1.0f,
                            ageWhenCreated = LifeStage.Elderly,
                            timestamp = currentTime,
                            currentStrength = 1.0f,
                            description = "Reached elderly wisdom"
                        });
                    }
                }

                // Special handling for baby → child (first major transition)
                if (transitionEvent.ValueRO.previousStage == LifeStage.Baby &&
                    transitionEvent.ValueRO.newStage == LifeStage.Child)
                {
                    Debug.Log("Chimera growing up! Baby → Child transition");

                    // Slight independence boost as they grow
                    if (EntityManager.HasComponent<CreaturePersonality>(chimeraEntity))
                    {
                        var personality = EntityManager.GetComponentData<CreaturePersonality>(chimeraEntity);
                        personality.Independence = (byte)Unity.Mathematics.math.min(100, personality.Independence + 5);
                        EntityManager.SetComponentData(chimeraEntity, personality);
                    }
                }

                ecb.DestroyEntity(entity);
            }
        }

        /// <summary>
        /// Processes breeding results and initializes offspring systems
        /// </summary>
        private void ProcessBreedingResults(float currentTime)
        {
            var ecb = _ecbSystem.CreateCommandBuffer();

            foreach (var (breedingResult, entity) in
                SystemAPI.Query<RefRO<Genetics.PersonalityBreedingResult>>().WithEntityAccess())
            {
                var offspringEntity = breedingResult.ValueRO.offspringEntity;
                if (!EntityManager.Exists(offspringEntity))
                {
                    ecb.DestroyEntity(entity);
                    continue;
                }

                Debug.Log($"Offspring born! Personality: {breedingResult.ValueRO.summary}");

                // Initialize CreatureIdentityComponent for offspring
                if (!EntityManager.HasComponent<CreatureIdentityComponent>(offspringEntity))
                {
                    ecb.AddComponent(offspringEntity, new CreatureIdentityComponent
                    {
                        CreatureID = System.Guid.NewGuid().ToString(),
                        SpeciesID = 1, // TODO: Get from parents
                        CurrentLifeStage = LifeStage.Baby, // Always born as baby
                        AgePercentage = 0f,
                        BirthTime = currentTime,
                        MaxLifespan = 86400f // 24 hours default
                    });
                }

                // Initialize PersonalityStabilityComponent
                if (!EntityManager.HasComponent<PersonalityStabilityComponent>(offspringEntity))
                {
                    ecb.AddComponent(offspringEntity, new PersonalityStabilityComponent
                    {
                        currentLifeStage = LifeStage.Baby,
                        personalityMalleability = 1.0f, // Baby = 100% malleable
                        reversionSpeed = 0f, // No reversion for babies
                        hasLockedBaseline = false,
                        timeSinceLastPersonalityChange = 0f,
                        timeSinceBaselineLock = 0f,
                        deviationMagnitude = 0f
                    });
                }

                // Initialize EmotionalIndicatorComponent
                if (!EntityManager.HasComponent<EmotionalIndicatorComponent>(offspringEntity))
                {
                    ecb.AddComponent(offspringEntity, new EmotionalIndicatorComponent
                    {
                        currentIcon = EmotionalIcon.Neutral,
                        previousIcon = EmotionalIcon.Neutral,
                        currentEmoji = "😐",
                        emotionDescription = "Curious newborn",
                        emotionalIntensity = 0.5f,
                        timeSinceLastChange = 0f,
                        isFluctuating = false,
                        isSerene = false,
                        displayPriority = 0.5f
                    });
                }

                // Initialize positive memory buffer
                if (!EntityManager.HasBuffer<PositiveMemory>(offspringEntity))
                {
                    ecb.AddBuffer<PositiveMemory>(offspringEntity);
                }

                // Initialize emotional scar buffer
                if (!EntityManager.HasBuffer<EmotionalScar>(offspringEntity))
                {
                    ecb.AddBuffer<EmotionalScar>(offspringEntity);
                }

                // Initialize PartnershipSkillComponent (for future partnerships)
                if (!EntityManager.HasComponent<PartnershipSkillComponent>(offspringEntity))
                {
                    ecb.AddComponent(offspringEntity, new PartnershipSkillComponent
                    {
                        cooperationLevel = 0.5f, // Starting cooperation
                        trustLevel = 0.3f, // Low trust initially
                        understandingLevel = 0.1f, // New partnership
                        actionMastery = 0f,
                        strategyMastery = 0f,
                        puzzleMastery = 0f,
                        racingMastery = 0f,
                        rhythmMastery = 0f,
                        explorationMastery = 0f,
                        economicsMastery = 0f,
                        totalActivitiesCompleted = 0,
                        genresExplored = 0,
                        recentSuccessRate = 0.5f,
                        improvementTrend = 0f
                    });
                }

                ecb.DestroyEntity(entity);
            }
        }

        // Helper methods

        private float CalculateAgeSensitivity(LifeStage stage)
        {
            return stage switch
            {
                LifeStage.Baby => 2.0f,      // Extra sensitive
                LifeStage.Child => 1.5f,     // Very sensitive
                LifeStage.Teen => 1.2f,      // Heightened sensitivity
                LifeStage.Adult => 1.0f,     // Standard
                LifeStage.Elderly => 0.8f,   // Less reactive but deeply affected
                _ => 1.0f
            };
        }
    }
}

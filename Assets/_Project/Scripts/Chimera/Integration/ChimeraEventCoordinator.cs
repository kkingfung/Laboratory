using Unity.Entities;
using UnityEngine;
using Laboratory.Chimera.Consciousness.Core;
using Laboratory.Chimera.Progression;
using Laboratory.Chimera.Activities;
using Laboratory.Chimera.Social;

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

                // Create emotional context from activity result
                if (EntityManager.HasBuffer<EmotionalContextEntry>(chimeraEntity))
                {
                    var contextBuffer = EntityManager.GetBuffer<EmotionalContextEntry>(chimeraEntity);

                    EmotionalContextType contextType = result.ValueRO.status switch
                    {
                        ActivityResultStatus.Perfect => EmotionalContextType.ActivityPerfect,
                        ActivityResultStatus.Success => EmotionalContextType.ActivitySuccess,
                        ActivityResultStatus.Partial => EmotionalContextType.ActivityPartial,
                        ActivityResultStatus.Failed => EmotionalContextType.ActivityFailed,
                        _ => EmotionalContextType.ActivityPartial
                    };

                    contextBuffer.Add(new EmotionalContextEntry
                    {
                        contextType = contextType,
                        intensity = result.ValueRO.playerPerformance,
                        timestamp = currentTime,
                        relatedEntity = result.ValueRO.partnershipEntity
                    });

                    // Limit buffer size
                    if (contextBuffer.Length > 10)
                    {
                        contextBuffer.RemoveAt(0);
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
                    var changeEventEntity = EntityManager.CreateEntity();
                    ecb.AddComponent(changeEventEntity, new PersonalityChangeEvent
                    {
                        targetCreature = chimeraEntity,
                        changeType = PersonalityChangeType.ExperienceBased,
                        traitAffected = PersonalityTrait.Affection, // Activities improve affection
                        magnitude = result.ValueRO.skillGained * 10f,
                        source = result.ValueRO.partnershipEntity,
                        timestamp = currentTime
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

                // Create emotional context
                if (EntityManager.HasBuffer<EmotionalContextEntry>(chimeraEntity))
                {
                    var contextBuffer = EntityManager.GetBuffer<EmotionalContextEntry>(chimeraEntity);

                    EmotionalContextType contextType = equipEvent.ValueRO.personalityFit > 0.7f
                        ? EmotionalContextType.GiftReceived  // Likes equipment
                        : EmotionalContextType.Discomfort;    // Dislikes equipment

                    contextBuffer.Add(new EmotionalContextEntry
                    {
                        contextType = contextType,
                        intensity = equipEvent.ValueRO.personalityFit,
                        timestamp = currentTime,
                        relatedEntity = Entity.Null
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
                SystemAPI.Query<RefRO<LifeStageTransitionEvent>>().WithEntityAccess())
            {
                var chimeraEntity = transitionEvent.ValueRO.chimeraEntity;
                if (!EntityManager.Exists(chimeraEntity))
                {
                    ecb.DestroyEntity(entity);
                    continue;
                }

                // Special handling for transition to elderly
                if (transitionEvent.ValueRO.newStage == LifeStage.Elderly)
                {
                    Debug.Log($"Chimera transitioning to ELDERLY - personality will lock!");

                    // Create emotional context for this milestone
                    if (EntityManager.HasBuffer<EmotionalContextEntry>(chimeraEntity))
                    {
                        var contextBuffer = EntityManager.GetBuffer<EmotionalContextEntry>(chimeraEntity);
                        contextBuffer.Add(new EmotionalContextEntry
                        {
                            contextType = EmotionalContextType.BondingMoment, // Milestone
                            intensity = 1.0f,
                            timestamp = currentTime,
                            relatedEntity = Entity.Null
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
                        intensity = 0.5f,
                        timeSinceLastChange = 0f,
                        displayUntil = currentTime + 30f
                    });
                }

                // Initialize emotional context buffer
                if (!EntityManager.HasBuffer<EmotionalContextEntry>(offspringEntity))
                {
                    ecb.AddBuffer<EmotionalContextEntry>(offspringEntity);
                }

                // Initialize PartnershipSkillComponent (for future partnerships)
                if (!EntityManager.HasComponent<PartnershipSkillComponent>(offspringEntity))
                {
                    ecb.AddComponent(offspringEntity, new PartnershipSkillComponent
                    {
                        cooperationLevel = 0.5f, // Starting cooperation
                        trustLevel = 0.3f, // Low trust initially
                        bondQuality = 0.2f, // New bond
                        actionMastery = 0f,
                        strategyMastery = 0f,
                        puzzleMastery = 0f,
                        racingMastery = 0f,
                        rhythmMastery = 0f,
                        explorationMastery = 0f,
                        economicsMastery = 0f,
                        totalActivitiesCompleted = 0,
                        lifetimePlaytime = 0f,
                        partnershipStartTime = currentTime
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
